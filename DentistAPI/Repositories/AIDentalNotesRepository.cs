using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DentistAPI.Models;

namespace DentistAPI.Repositories
{
    public interface IAIDentalNotesRepository
    {
        Task<long> CreateSessionAsync(DentalNoteSession session);
        Task<long> SaveDentalNoteAsync(DentalNote note);
        Task<DentalNote?> GetDentalNoteByIdAsync(long noteId);
        Task UpdateDentalNoteAsync(DentalNote note);
        Task CreateTranscriptAsync(long sessionId, string text);
        Task<long> SaveAudioRecordingAsync(long sessionId, string mimeType, int durationSeconds, string sha256Hash);
        Task<IEnumerable<DentalNote>> GetDentalNotesByPatientIdAsync(int patientId, int? dentistId = null, bool includeDeleted = false);
        Task<bool> SoftDeleteDentalNoteAsync(long noteId, bool isDeleted = true);
        Task<AudioRecording?> GetLatestAudioRecordingByPatientIdAsync(long patientId);
        Task<DentalNote?> GetDentalNoteByAudioIdAsync(long audioId);
        Task<string?> GetTranscriptTextBySessionIdAsync(long sessionId);
    }

    public class AIDentalNotesRepository : IAIDentalNotesRepository
    {
        private readonly string _connectionString;

        // Protected no-arg constructor allows Moq to subclass this for unit testing
        protected AIDentalNotesRepository() { _connectionString = string.Empty; }

        public AIDentalNotesRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=DentistDB;Trusted_Connection=True;MultipleActiveResultSets=true";
            InitializeSchema();
        }

        private void InitializeSchema()
        {
            try
            {
                using var connection = CreateConnection();
                // Schema check
                connection.Execute(@"
                    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
                    BEGIN
                        EXEC('CREATE SCHEMA dentist');
                    END
                ");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"AI Notes schema check: {ex.Message}");
            }
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public virtual async Task<long> CreateSessionAsync(DentalNoteSession session)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[DentalNoteSessions] (PatientId, DentistId, Status, StartedAt, CreatedAt)
                OUTPUT INSERTED.SessionId
                VALUES (@PatientId, @DentistId, @Status, @StartedAt, @CreatedAt)";
            return await connection.QuerySingleAsync<long>(query, session);
        }

        public virtual async Task<long> SaveDentalNoteAsync(DentalNote note)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[DentalNotes] (SessionId, AudioId, PatientId, DentistId, Summary, ChiefComplaint, History, Examination, Assessment, TreatmentPerformed, PostOpAdvice, FollowUp, Status, IsDeleted, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.NoteId
                VALUES (@SessionId, @AudioId, @PatientId, @DentistId, @Summary, @ChiefComplaint, @History, @Examination, @Assessment, @TreatmentPerformed, @PostOpAdvice, @FollowUp, @Status, @IsDeleted, @CreatedAt, @UpdatedAt)";
            
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            var noteId = await connection.QuerySingleAsync<long>(query, note);
            note.NoteId = noteId;

            // Save prescriptions
            if (note.Prescriptions != null && note.Prescriptions.Count > 0)
            {
                string presQuery = @"
                    INSERT INTO [dentist].[DentalNotePrescriptions] (NoteId, MedicationName, Strength, Dose, Route, Frequency, Duration, Instructions, Confidence)
                    VALUES (@NoteId, @MedicationName, @Strength, @Dose, @Route, @Frequency, @Duration, @Instructions, @Confidence)";
                foreach (var p in note.Prescriptions)
                {
                    p.NoteId = noteId;
                    await connection.ExecuteAsync(presQuery, p);
                }
            }

            // Save treatment plans
            if (note.TreatmentPlans != null && note.TreatmentPlans.Count > 0)
            {
                string planQuery = @"
                    INSERT INTO [dentist].[DentalNoteTreatmentPlans] (NoteId, SequenceNo, ProcedureName, ToothOrSite, Timing, Notes)
                    VALUES (@NoteId, @SequenceNo, @ProcedureName, @ToothOrSite, @Timing, @Notes)";
                foreach (var tp in note.TreatmentPlans)
                {
                    tp.NoteId = noteId;
                    await connection.ExecuteAsync(planQuery, tp);
                }
            }

            // Save warnings
            if (note.AIWarnings != null && note.AIWarnings.Count > 0)
            {
                string warnQuery = @"
                    INSERT INTO [dentist].[AIWarnings] (NoteId, FieldName, Message, Severity, Resolved)
                    VALUES (@NoteId, @FieldName, @Message, @Severity, @Resolved)";
                foreach (var w in note.AIWarnings)
                {
                    w.NoteId = noteId;
                    await connection.ExecuteAsync(warnQuery, w);
                }
            }

            return noteId;
        }

        public virtual async Task CreateTranscriptAsync(long sessionId, string text)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[Transcripts] (SessionId, RawText, CleanedText, Language, CreatedAt)
                VALUES (@SessionId, @RawText, @CleanedText, 'en', @CreatedAt)";
            await connection.ExecuteAsync(query, new { SessionId = sessionId, RawText = text, CleanedText = text, CreatedAt = System.DateTime.UtcNow });
        }
        
        public virtual async Task<IEnumerable<DentalNote>> GetDentalNotesByPatientIdAsync(int patientId, int? dentistId = null, bool includeDeleted = false)
        {
            using var connection = CreateConnection();
            string query = @"
                SELECT * FROM [dentist].[DentalNotes] 
                WHERE PatientId = @PatientId 
                  AND (@DentistId IS NULL OR DentistId = @DentistId)
                  AND (@IncludeDeleted = 1 OR IsDeleted = 0)
                ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<DentalNote>(query, new { PatientId = patientId, DentistId = dentistId, IncludeDeleted = includeDeleted ? 1 : 0 });
        }

        public virtual async Task<bool> SoftDeleteDentalNoteAsync(long noteId, bool isDeleted = true)
        {
            using var connection = CreateConnection();
            string query = "UPDATE [dentist].[DentalNotes] SET IsDeleted = @IsDeleted, UpdatedAt = GETDATE() WHERE NoteId = @NoteId";
            int affected = await connection.ExecuteAsync(query, new { NoteId = noteId, IsDeleted = isDeleted ? 1 : 0 });
            return affected > 0;
        }

        public virtual async Task<long> SaveAudioRecordingAsync(long sessionId, string mimeType, int durationSeconds, string sha256Hash)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[AudioRecordings] (SessionId, StorageUri, MimeType, DurationSeconds, Sha256Hash, CreatedAt)
                OUTPUT INSERTED.AudioId
                VALUES (@SessionId, @StorageUri, @MimeType, @DurationSeconds, @Sha256Hash, @CreatedAt)";
            return await connection.QuerySingleAsync<long>(query, new
            {
                SessionId = sessionId,
                StorageUri = $"memory://session/{sessionId}",
                MimeType = mimeType,
                DurationSeconds = durationSeconds,
                Sha256Hash = sha256Hash,
                CreatedAt = System.DateTime.UtcNow
            });
        }

        public virtual async Task<DentalNote?> GetDentalNoteByIdAsync(long noteId)
        {
            using var connection = CreateConnection();
            string query = "SELECT * FROM [dentist].[DentalNotes] WHERE NoteId = @NoteId";
            var note = await connection.QuerySingleOrDefaultAsync<DentalNote>(query, new { NoteId = noteId });
            
            if (note != null)
            {
                var pres = await connection.QueryAsync<DentalNotePrescription>(
                    "SELECT * FROM [dentist].[DentalNotePrescriptions] WHERE NoteId = @NoteId", new { NoteId = noteId });
                note.Prescriptions = new List<DentalNotePrescription>(pres);

                var plans = await connection.QueryAsync<DentalNoteTreatmentPlan>(
                    "SELECT * FROM [dentist].[DentalNoteTreatmentPlans] WHERE NoteId = @NoteId", new { NoteId = noteId });
                note.TreatmentPlans = new List<DentalNoteTreatmentPlan>(plans);

                var warnings = await connection.QueryAsync<AIWarning>(
                    "SELECT * FROM [dentist].[AIWarnings] WHERE NoteId = @NoteId", new { NoteId = noteId });
                note.AIWarnings = new List<AIWarning>(warnings);
            }
            return note;
        }

        public virtual async Task UpdateDentalNoteAsync(DentalNote note)
        {
            using var connection = CreateConnection();
            string query = @"
                UPDATE [dentist].[DentalNotes] 
                SET Summary = @Summary,
                    ChiefComplaint = @ChiefComplaint,
                    History = @History,
                    Examination = @Examination,
                    Assessment = @Assessment,
                    TreatmentPerformed = @TreatmentPerformed,
                    PostOpAdvice = @PostOpAdvice,
                    FollowUp = @FollowUp,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt
                WHERE NoteId = @NoteId";
            
            note.UpdatedAt = DateTime.UtcNow;
            await connection.ExecuteAsync(query, note);

            // Re-sync prescriptions (delete and re-insert)
            await connection.ExecuteAsync("DELETE FROM [dentist].[DentalNotePrescriptions] WHERE NoteId = @NoteId", new { NoteId = note.NoteId });
            if (note.Prescriptions != null && note.Prescriptions.Count > 0)
            {
                string presQuery = @"
                    INSERT INTO [dentist].[DentalNotePrescriptions] (NoteId, MedicationName, Strength, Dose, Route, Frequency, Duration, Instructions, Confidence)
                    VALUES (@NoteId, @MedicationName, @Strength, @Dose, @Route, @Frequency, @Duration, @Instructions, @Confidence)";
                foreach (var p in note.Prescriptions)
                {
                    p.NoteId = note.NoteId;
                    await connection.ExecuteAsync(presQuery, p);
                }
            }

            // Re-sync treatment plans (delete and re-insert)
            await connection.ExecuteAsync("DELETE FROM [dentist].[DentalNoteTreatmentPlans] WHERE NoteId = @NoteId", new { NoteId = note.NoteId });
            if (note.TreatmentPlans != null && note.TreatmentPlans.Count > 0)
            {
                string planQuery = @"
                    INSERT INTO [dentist].[DentalNoteTreatmentPlans] (NoteId, SequenceNo, ProcedureName, ToothOrSite, Timing, Notes)
                    VALUES (@NoteId, @SequenceNo, @ProcedureName, @ToothOrSite, @Timing, @Notes)";
                foreach (var tp in note.TreatmentPlans)
                {
                    tp.NoteId = note.NoteId;
                    await connection.ExecuteAsync(planQuery, tp);
                }
            }

            // Re-sync warnings (delete and re-insert)
            await connection.ExecuteAsync("DELETE FROM [dentist].[AIWarnings] WHERE NoteId = @NoteId", new { NoteId = note.NoteId });
            if (note.AIWarnings != null && note.AIWarnings.Count > 0)
            {
                string warningQuery = @"
                    INSERT INTO [dentist].[AIWarnings] (NoteId, FieldName, Message, Severity, Resolved)
                    VALUES (@NoteId, @FieldName, @Message, @Severity, @Resolved)";
                foreach (var w in note.AIWarnings)
                {
                    w.NoteId = note.NoteId;
                    await connection.ExecuteAsync(warningQuery, w);
                }
            }
        }

        public virtual async Task<AudioRecording?> GetLatestAudioRecordingByPatientIdAsync(long patientId)
        {
            using var connection = CreateConnection();
            string query = @"
                SELECT TOP 1 ar.* 
                FROM [dentist].[AudioRecordings] ar
                INNER JOIN [dentist].[DentalNoteSessions] dns ON ar.SessionId = dns.SessionId
                WHERE dns.PatientId = @PatientId
                ORDER BY ar.CreatedAt DESC, ar.AudioId DESC";
            return await connection.QueryFirstOrDefaultAsync<AudioRecording>(query, new { PatientId = patientId });
        }

        public virtual async Task<DentalNote?> GetDentalNoteByAudioIdAsync(long audioId)
        {
            using var connection = CreateConnection();
            string query = "SELECT * FROM [dentist].[DentalNotes] WHERE AudioId = @AudioId";
            var note = await connection.QueryFirstOrDefaultAsync<DentalNote>(query, new { AudioId = audioId });
            if (note != null)
            {
                var prescriptions = await connection.QueryAsync<DentalNotePrescription>(
                    "SELECT * FROM [dentist].[DentalNotePrescriptions] WHERE NoteId = @NoteId", new { NoteId = note.NoteId });
                note.Prescriptions = prescriptions.AsList();
            }
            return note;
        }

        public virtual async Task<string?> GetTranscriptTextBySessionIdAsync(long sessionId)
        {
            using var connection = CreateConnection();
            string query = "SELECT RawText FROM [dentist].[Transcripts] WHERE SessionId = @SessionId";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { SessionId = sessionId });
        }
    }
}
