using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:int}/treatment-sessions")]
    public class TreatmentSessionsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ITreatmentPlanService _treatmentPlanService;
        private readonly ILogger<TreatmentSessionsController> _logger;

        public TreatmentSessionsController(
            IConfiguration configuration,
            ITreatmentPlanService treatmentPlanService,
            ILogger<TreatmentSessionsController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
            _treatmentPlanService = treatmentPlanService;
            _logger = logger;
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        // =========================================================================
        // 1. START TREATMENT SESSION (Creates or resumes chairside session & auto-bills consultation)
        // =========================================================================
        [HttpPost("start")]
        public async Task<IActionResult> StartSession(int patientId, [FromBody] StartTreatmentSessionRequest? request)
        {
            if (patientId <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            int doctorId = request?.DoctorId ?? 1;
            int? appointmentId = request?.AppointmentId;
            string notes = request?.Notes ?? "Chairside clinical treatment session started on Odontogram";

            try
            {
                using var conn = CreateConnection();

                // 1. Ensure TreatmentSessions table exists (defensive check)
                await EnsureTablesExistAsync(conn);

                // 2. Check for an existing active session today
                var existingActive = await conn.QueryFirstOrDefaultAsync<TreatmentSession>(@"
                    SELECT TOP 1 * FROM [dentist].[TreatmentSessions]
                    WHERE PatientID = @PatientId AND Status = 'active'
                    ORDER BY StartedAt DESC",
                    new { PatientId = patientId });

                long sessionId;
                DateTime startedAt;
                bool isNewSession = false;

                if (existingActive != null)
                {
                    sessionId = existingActive.SessionID;
                    startedAt = existingActive.StartedAt;
                }
                else
                {
                    startedAt = DateTime.UtcNow;
                    isNewSession = true;

                    // Initial Action Log entry
                    var initialLogs = new List<TreatmentSessionActionLog>
                    {
                        new TreatmentSessionActionLog
                        {
                            Timestamp = startedAt,
                            ElapsedSeconds = 0,
                            ActionType = "session_started",
                            Title = "Session Started",
                            Description = $"Chairside clinical session initiated by Doctor #{doctorId}.",
                            Metadata = new Dictionary<string, object>
                            {
                                { "doctorId", doctorId },
                                { "patientId", patientId },
                                { "appointmentId", appointmentId ?? 0 }
                            }
                        }
                    };

                    string insertSql = @"
                        INSERT INTO [dentist].[TreatmentSessions] 
                        (PatientID, DoctorID, AppointmentID, StartedAt, DurationSeconds, FormattedDuration, Status, ActionCount, SummaryNotes, ActionLogsJson, CreatedAt, UpdatedAt)
                        VALUES 
                        (@PatientId, @DoctorId, @AppointmentId, @StartedAt, 0, '00:00', 'active', 1, @SummaryNotes, @ActionLogsJson, SYSUTCDATETIME(), SYSUTCDATETIME());
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                    sessionId = await conn.ExecuteScalarAsync<long>(insertSql, new
                    {
                        PatientId = patientId,
                        DoctorId = doctorId,
                        AppointmentId = appointmentId,
                        StartedAt = startedAt,
                        SummaryNotes = notes,
                        ActionLogsJson = JsonSerializer.Serialize(initialLogs)
                    });
                }

                // 3. Auto-ensure Consultation Fee exists in Treatment Plan for new patient
                bool consultationFeeAdded = false;
                int consultationFeeAmount = 0;
                string consultationMessage = string.Empty;

                try
                {
                    var diagResult = await _treatmentPlanService.EnsureDiagnosticFeeAsync(patientId, new AutoDiagnosticFeeRequest
                    {
                        FeeType = "consultation",
                        DoctorId = doctorId,
                        Notes = "Auto-billed initial consultation fee on session start"
                    });

                    if (diagResult.Added)
                    {
                        consultationFeeAdded = true;
                        consultationFeeAmount = diagResult.Fee;
                        consultationMessage = diagResult.Message ?? "Initial Consultation fee added to Treatment Plan.";

                        // Append consultation action log to session
                        await AppendActionLogInternalAsync(conn, sessionId, patientId, doctorId, new TreatmentSessionActionLog
                        {
                            Timestamp = DateTime.UtcNow,
                            ElapsedSeconds = Math.Max(0, (int)(DateTime.UtcNow - startedAt).TotalSeconds),
                            ActionType = "consultation_billed",
                            Title = "Consultation Fee Added",
                            Description = $"Added 'Dental Consultation & Diagnosis' ({consultationFeeAmount:N0} PKR) to Treatment Plan.",
                            Metadata = new Dictionary<string, object>
                            {
                                { "fee", consultationFeeAmount },
                                { "procedureCode", "D0150" },
                                { "itemId", diagResult.ItemId ?? 0 }
                            }
                        });
                    }
                }
                catch (Exception diagEx)
                {
                    _logger.LogWarning(diagEx, "Non-blocking consultation fee check during session start for Patient {PatientId}", patientId);
                }

                return Ok(new
                {
                    success = true,
                    isNewSession,
                    sessionId,
                    patientId,
                    doctorId,
                    startedAt,
                    status = "active",
                    consultationFeeAdded,
                    consultationFee = consultationFeeAmount,
                    message = isNewSession ? "Chairside treatment session started." : "Resumed active chairside session."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting treatment session for patient {PatientId}", patientId);
                return BadRequest(new { message = "Failed to start treatment session: " + ex.Message });
            }
        }

        // =========================================================================
        // 2. END TREATMENT SESSION (Records total duration & full action audit logs)
        // =========================================================================
        [HttpPost("{sessionId:long}/end")]
        [HttpPost("end")]
        public async Task<IActionResult> EndSession(int patientId, long? sessionId, [FromBody] EndTreatmentSessionRequest? request)
        {
            if (patientId <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            int durationSecs = request?.DurationSeconds ?? 0;
            string formatted = !string.IsNullOrWhiteSpace(request?.FormattedDuration)
                ? request.FormattedDuration
                : $"{durationSecs / 60}m {durationSecs % 60}s";
            string summary = request?.SummaryNotes ?? "Chairside clinical session concluded.";
            var clientActions = request?.Actions ?? new List<TreatmentSessionActionLog>();

            try
            {
                using var conn = CreateConnection();
                await EnsureTablesExistAsync(conn);

                // If sessionId not passed in route, locate latest active session
                long effectiveSessionId = sessionId.GetValueOrDefault(0);
                if (effectiveSessionId <= 0)
                {
                    effectiveSessionId = await conn.QueryFirstOrDefaultAsync<long>(@"
                        SELECT TOP 1 SessionID FROM [dentist].[TreatmentSessions]
                        WHERE PatientID = @PatientId AND Status = 'active'
                        ORDER BY StartedAt DESC",
                        new { PatientId = patientId });
                }

                // If still no session found, locate the latest session overall
                if (effectiveSessionId <= 0)
                {
                    effectiveSessionId = await conn.QueryFirstOrDefaultAsync<long>(@"
                        SELECT TOP 1 SessionID FROM [dentist].[TreatmentSessions]
                        WHERE PatientID = @PatientId
                        ORDER BY StartedAt DESC",
                        new { PatientId = patientId });
                }

                DateTime endedAt = DateTime.UtcNow;

                if (effectiveSessionId > 0)
                {
                    // Fetch existing logs and merge with incoming actions
                    var existingRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT ActionLogsJson, DoctorID, AppointmentID FROM [dentist].[TreatmentSessions] WHERE SessionID = @Id",
                        new { Id = effectiveSessionId });

                    int doctorId = existingRow != null && existingRow.DoctorID != null ? (int)existingRow.DoctorID : 1;
                    int? appointmentId = existingRow != null ? (int?)existingRow.AppointmentID : null;

                    var mergedLogs = new List<TreatmentSessionActionLog>();
                    if (existingRow != null && !string.IsNullOrWhiteSpace((string)existingRow.ActionLogsJson))
                    {
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<List<TreatmentSessionActionLog>>((string)existingRow.ActionLogsJson);
                            if (parsed != null) mergedLogs.AddRange(parsed);
                        }
                        catch { }
                    }

                    // Append client actions avoiding exact duplicate IDs
                    var existingIds = new HashSet<string>(mergedLogs.Select(l => l.Id));
                    foreach (var act in clientActions)
                    {
                        if (!string.IsNullOrWhiteSpace(act.Id) && !existingIds.Contains(act.Id))
                        {
                            mergedLogs.Add(act);
                            existingIds.Add(act.Id);
                        }
                    }

                    // Append completion action
                    mergedLogs.Add(new TreatmentSessionActionLog
                    {
                        Timestamp = endedAt,
                        ElapsedSeconds = durationSecs,
                        ActionType = "session_completed",
                        Title = "Session Completed",
                        Description = $"Session ended. Duration: {formatted}. Total actions: {mergedLogs.Count + 1}.",
                        Metadata = new Dictionary<string, object>
                        {
                            { "durationSeconds", durationSecs },
                            { "formattedDuration", formatted }
                        }
                    });

                    string updateSql = @"
                        UPDATE [dentist].[TreatmentSessions]
                        SET EndedAt = @EndedAt,
                            DurationSeconds = @DurationSeconds,
                            FormattedDuration = @FormattedDuration,
                            Status = 'completed',
                            ActionCount = @ActionCount,
                            SummaryNotes = @SummaryNotes,
                            ActionLogsJson = @ActionLogsJson,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE SessionID = @SessionId";

                    await conn.ExecuteAsync(updateSql, new
                    {
                        EndedAt = endedAt,
                        DurationSeconds = durationSecs,
                        FormattedDuration = formatted,
                        ActionCount = mergedLogs.Count,
                        SummaryNotes = summary,
                        ActionLogsJson = JsonSerializer.Serialize(mergedLogs),
                        SessionId = effectiveSessionId
                    });

                    // Also mirror to Appointments table if appointmentId is set or found
                    try
                    {
                        if (!appointmentId.HasValue || appointmentId.Value <= 0)
                        {
                            appointmentId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                                SELECT TOP 1 AppointmentID FROM [dentist].[Appointments]
                                WHERE PatientID = @PatientId
                                ORDER BY 
                                    CASE WHEN CAST(PreferredDate AS DATE) = CAST(GETDATE() AS DATE) THEN 0 ELSE 1 END,
                                    PreferredDate DESC",
                                new { PatientId = patientId });
                        }

                        if (appointmentId.HasValue && appointmentId.Value > 0)
                        {
                            string timerNote = $"[Treatment Session: {formatted} ({durationSecs}s) on {DateTime.UtcNow:yyyy-MM-dd HH:mm}]";
                            await conn.ExecuteAsync(@"
                                UPDATE [dentist].[Appointments]
                                SET TreatmentDurationSeconds = COALESCE(TreatmentDurationSeconds, 0) + @Duration,
                                    TreatmentDurationFormatted = @Formatted,
                                    TreatmentEndedAt = @Ended,
                                    Notes = CASE 
                                        WHEN Notes IS NULL OR Notes = '' THEN @Note 
                                        ELSE CONCAT(Notes, CHAR(10), @Note) 
                                    END
                                WHERE AppointmentID = @ApptId",
                                new
                                {
                                    Duration = durationSecs,
                                    Formatted = formatted,
                                    Ended = endedAt,
                                    Note = timerNote,
                                    ApptId = appointmentId.Value
                                });
                        }
                    }
                    catch (Exception apptEx)
                    {
                        _logger.LogWarning(apptEx, "Non-blocking appointment sync on session end for Patient {PatientId}", patientId);
                    }

                    return Ok(new
                    {
                        success = true,
                        sessionId = effectiveSessionId,
                        formattedDuration = formatted,
                        durationSeconds = durationSecs,
                        actionCount = mergedLogs.Count,
                        message = $"Session completed. Total duration ({formatted}) recorded with full clinical audit logs."
                    });
                }
                else
                {
                    // Fallback: Create completed session record directly if none existed
                    var fallbackLogs = new List<TreatmentSessionActionLog>
                    {
                        new TreatmentSessionActionLog
                        {
                            Timestamp = endedAt.AddSeconds(-durationSecs),
                            ElapsedSeconds = 0,
                            ActionType = "session_started",
                            Title = "Session Started",
                            Description = "Chairside treatment started."
                        },
                        new TreatmentSessionActionLog
                        {
                            Timestamp = endedAt,
                            ElapsedSeconds = durationSecs,
                            ActionType = "session_completed",
                            Title = "Session Completed",
                            Description = $"Session recorded. Duration: {formatted}."
                        }
                    };

                    string insertDirectSql = @"
                        INSERT INTO [dentist].[TreatmentSessions]
                        (PatientID, DoctorID, StartedAt, EndedAt, DurationSeconds, FormattedDuration, Status, ActionCount, SummaryNotes, ActionLogsJson, CreatedAt, UpdatedAt)
                        VALUES
                        (@PatientId, 1, @StartedAt, @EndedAt, @DurationSeconds, @FormattedDuration, 'completed', 2, @SummaryNotes, @ActionLogsJson, SYSUTCDATETIME(), SYSUTCDATETIME());
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                    long newId = await conn.ExecuteScalarAsync<long>(insertDirectSql, new
                    {
                        PatientId = patientId,
                        StartedAt = endedAt.AddSeconds(-durationSecs),
                        EndedAt = endedAt,
                        DurationSeconds = durationSecs,
                        FormattedDuration = formatted,
                        SummaryNotes = summary,
                        ActionLogsJson = JsonSerializer.Serialize(fallbackLogs)
                    });

                    return Ok(new
                    {
                        success = true,
                        sessionId = newId,
                        formattedDuration = formatted,
                        durationSeconds = durationSecs,
                        actionCount = 2,
                        message = $"Treatment duration ({formatted}) recorded with audit log."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending treatment session for patient {PatientId}", patientId);
                return BadRequest(new { message = "Failed to end treatment session: " + ex.Message });
            }
        }

        // =========================================================================
        // 3. LOG CLINICAL ACTION DURING ACTIVE SESSION (Real-time or buffered)
        // =========================================================================
        [HttpPost("{sessionId:long}/actions")]
        public async Task<IActionResult> LogSessionAction(int patientId, long sessionId, [FromBody] LogTreatmentSessionActionRequest request)
        {
            if (patientId <= 0 || sessionId <= 0 || request == null) 
                return BadRequest(new { message = "Invalid parameters." });

            try
            {
                using var conn = CreateConnection();
                var session = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT DoctorID FROM [dentist].[TreatmentSessions] WHERE SessionID = @Id",
                    new { Id = sessionId });

                int docId = session != null && session.DoctorID != null ? (int)session.DoctorID : 1;

                var actionLog = new TreatmentSessionActionLog
                {
                    Timestamp = DateTime.UtcNow,
                    ElapsedSeconds = request.ElapsedSeconds,
                    ActionType = request.ActionType,
                    Title = request.Title,
                    Description = request.Description,
                    Metadata = request.Metadata
                };

                await AppendActionLogInternalAsync(conn, sessionId, patientId, docId, actionLog);

                return Ok(new { success = true, actionId = actionLog.Id, loggedAt = actionLog.Timestamp });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging action for session {SessionId}", sessionId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================================
        // 4. GET LATEST TREATMENT SESSION (Fast endpoint for ChartPage header banner)
        // =========================================================================
        [HttpGet("latest")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetLatestSession(int patientId)
        {
            if (patientId <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                using var conn = CreateConnection();
                await EnsureTablesExistAsync(conn);

                string query = @"
                    SELECT TOP 1 
                        s.SessionID, s.PatientID, s.DoctorID, s.AppointmentID, 
                        s.StartedAt, s.EndedAt, s.DurationSeconds, s.FormattedDuration, 
                        s.Status, s.ActionCount, s.SummaryNotes, s.ActionLogsJson,
                        d.FirstName, d.LastName
                    FROM [dentist].[TreatmentSessions] s
                    LEFT JOIN [dentist].[Doctors] d ON s.DoctorID = d.DoctorID
                    WHERE s.PatientID = @PatientId
                    ORDER BY s.StartedAt DESC";

                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(query, new { PatientId = patientId });
                if (row == null)
                {
                    return Ok(new LatestTreatmentSessionDto
                    {
                        HasSession = false,
                        PatientId = patientId,
                        DurationSeconds = 0,
                        FormattedDuration = "00:00"
                    });
                }

                string docName = string.Empty;
                if (!string.IsNullOrWhiteSpace((string)row.FirstName) || !string.IsNullOrWhiteSpace((string)row.LastName))
                {
                    docName = $"Dr. {((string)row.FirstName ?? "").Trim()} {((string)row.LastName ?? "").Trim()}".Trim();
                }

                List<TreatmentSessionActionLog>? logs = null;
                if (!string.IsNullOrWhiteSpace((string)row.ActionLogsJson))
                {
                    try
                    {
                        logs = JsonSerializer.Deserialize<List<TreatmentSessionActionLog>>((string)row.ActionLogsJson);
                    }
                    catch { }
                }

                return Ok(new LatestTreatmentSessionDto
                {
                    HasSession = true,
                    SessionId = (long)row.SessionID,
                    PatientId = (int)row.PatientID,
                    DoctorId = (int)row.DoctorID,
                    DoctorName = docName,
                    StartedAt = (DateTime)row.StartedAt,
                    EndedAt = (DateTime?)row.EndedAt,
                    DurationSeconds = (int)(row.DurationSeconds ?? 0),
                    FormattedDuration = (string)row.FormattedDuration ?? "00:00",
                    Status = (string)row.Status ?? "completed",
                    ActionCount = (int)(row.ActionCount ?? (logs?.Count ?? 0)),
                    SummaryNotes = (string)row.SummaryNotes,
                    ActionLogs = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest session for patient {PatientId}", patientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================================
        // 5. GET ALL TREATMENT SESSIONS (Full history for session audit log modal)
        // =========================================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetAllSessions(int patientId)
        {
            if (patientId <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                using var conn = CreateConnection();
                await EnsureTablesExistAsync(conn);

                string query = @"
                    SELECT 
                        s.SessionID, s.PatientID, s.DoctorID, s.AppointmentID, 
                        s.StartedAt, s.EndedAt, s.DurationSeconds, s.FormattedDuration, 
                        s.Status, s.ActionCount, s.SummaryNotes, s.ActionLogsJson,
                        d.FirstName, d.LastName
                    FROM [dentist].[TreatmentSessions] s
                    LEFT JOIN [dentist].[Doctors] d ON s.DoctorID = d.DoctorID
                    WHERE s.PatientID = @PatientId
                    ORDER BY s.StartedAt DESC";

                var rows = await conn.QueryAsync<dynamic>(query, new { PatientId = patientId });

                var result = rows.Select(r =>
                {
                    string docName = string.Empty;
                    if (!string.IsNullOrWhiteSpace((string)r.FirstName) || !string.IsNullOrWhiteSpace((string)r.LastName))
                    {
                        docName = $"Dr. {((string)r.FirstName ?? "").Trim()} {((string)r.LastName ?? "").Trim()}".Trim();
                    }

                    List<TreatmentSessionActionLog>? logs = null;
                    if (!string.IsNullOrWhiteSpace((string)r.ActionLogsJson))
                    {
                        try
                        {
                            logs = JsonSerializer.Deserialize<List<TreatmentSessionActionLog>>((string)r.ActionLogsJson);
                        }
                        catch { }
                    }

                    return new LatestTreatmentSessionDto
                    {
                        HasSession = true,
                        SessionId = (long)r.SessionID,
                        PatientId = (int)r.PatientID,
                        DoctorId = (int)r.DoctorID,
                        DoctorName = docName,
                        StartedAt = (DateTime)r.StartedAt,
                        EndedAt = (DateTime?)r.EndedAt,
                        DurationSeconds = (int)(r.DurationSeconds ?? 0),
                        FormattedDuration = (string)r.FormattedDuration ?? "00:00",
                        Status = (string)r.Status ?? "completed",
                        ActionCount = (int)(r.ActionCount ?? (logs?.Count ?? 0)),
                        SummaryNotes = (string)r.SummaryNotes,
                        ActionLogs = logs
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sessions for patient {PatientId}", patientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================================
        // HELPER METHODS
        // =========================================================================
        private static async Task AppendActionLogInternalAsync(
            IDbConnection conn, 
            long sessionId, 
            int patientId, 
            int doctorId, 
            TreatmentSessionActionLog actionLog)
        {
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT ActionLogsJson, ActionCount FROM [dentist].[TreatmentSessions] WHERE SessionID = @Id",
                new { Id = sessionId });

            var logs = new List<TreatmentSessionActionLog>();
            if (row != null && !string.IsNullOrWhiteSpace((string)row.ActionLogsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<TreatmentSessionActionLog>>((string)row.ActionLogsJson);
                    if (parsed != null) logs.AddRange(parsed);
                }
                catch { }
            }

            logs.Add(actionLog);

            await conn.ExecuteAsync(@"
                UPDATE [dentist].[TreatmentSessions]
                SET ActionLogsJson = @Json,
                    ActionCount = @Count,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE SessionID = @SessionId",
                new
                {
                    Json = JsonSerializer.Serialize(logs),
                    Count = logs.Count,
                    SessionId = sessionId
                });

            // Also insert normalized row into TreatmentSessionLogs
            try
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO [dentist].[TreatmentSessionLogs]
                    (SessionID, PatientID, DoctorID, ActionType, ElapsedSeconds, Description, MetadataJson, Timestamp)
                    VALUES
                    (@SessionId, @PatientId, @DoctorId, @ActionType, @ElapsedSeconds, @Description, @MetadataJson, @Timestamp)",
                    new
                    {
                        SessionId = sessionId,
                        PatientId = patientId,
                        DoctorId = doctorId,
                        ActionType = actionLog.ActionType,
                        ElapsedSeconds = actionLog.ElapsedSeconds,
                        Description = actionLog.Description,
                        MetadataJson = actionLog.Metadata != null ? JsonSerializer.Serialize(actionLog.Metadata) : null,
                        Timestamp = actionLog.Timestamp
                    });
            }
            catch { }
        }

        private static async Task EnsureTablesExistAsync(IDbConnection conn)
        {
            string ensureSql = @"
                IF OBJECT_ID('dentist.TreatmentSessions', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dentist].[TreatmentSessions]
                    (
                        [SessionID]          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PatientID]          INT NOT NULL,
                        [DoctorID]           INT NOT NULL,
                        [AppointmentID]      INT NULL,
                        [StartedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [EndedAt]            DATETIME2 NULL,
                        [DurationSeconds]    INT NOT NULL DEFAULT 0,
                        [FormattedDuration]  NVARCHAR(50) NOT NULL DEFAULT '00:00',
                        [Status]             NVARCHAR(30) NOT NULL DEFAULT 'active',
                        [ActionCount]        INT NOT NULL DEFAULT 0,
                        [SummaryNotes]       NVARCHAR(MAX) NULL,
                        [ActionLogsJson]     NVARCHAR(MAX) NULL,
                        [CreatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [UpdatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END;

                IF OBJECT_ID('dentist.TreatmentSessionLogs', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dentist].[TreatmentSessionLogs]
                    (
                        [LogID]           BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SessionID]       BIGINT NOT NULL,
                        [PatientID]       INT NOT NULL,
                        [DoctorID]        INT NOT NULL,
                        [ActionType]      NVARCHAR(60) NOT NULL,
                        [ElapsedSeconds]  INT NOT NULL DEFAULT 0,
                        [Description]     NVARCHAR(500) NOT NULL,
                        [MetadataJson]    NVARCHAR(MAX) NULL,
                        [Timestamp]       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END;";

            await conn.ExecuteAsync(ensureSql);
        }
    }
}
