using DentistAPI.Repositories;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dapper;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    public class VoiceBookingRequest
    {
        public long PatientId { get; set; }
        public int DoctorId { get; set; }
        public string VoiceDateText { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IEmailService _emailService;
        private readonly IGeminiDentalNotesService _geminiService;
        private readonly DentalRepository _dentalRepository;
        private readonly ILogger<AppointmentsController> _logger;
        private readonly IDataNormalizationService _dataNorm;
        private readonly IWhatsAppService _whatsAppService;

        public AppointmentsController(
            IConfiguration configuration, 
            IEmailService emailService,
            IGeminiDentalNotesService geminiService,
            DentalRepository dentalRepository,
            ILogger<AppointmentsController> logger,
            IDataNormalizationService dataNorm,
            IWhatsAppService whatsAppService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailService = emailService;
            _geminiService = geminiService;
            _dentalRepository = dentalRepository;
            _logger = logger;
            _dataNorm = dataNorm;
            _whatsAppService = whatsAppService;
        }

        [HttpPost("book-voice")]
        public async Task<IActionResult> BookVoiceAppointment([FromBody] VoiceBookingRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.VoiceDateText))
            {
                return BadRequest(new ValidationErrorEnvelope("Voice booking request is required.", null, "PAYLOAD_REQUIRED"));
            }

            try
            {
                // 1. Parse date using Gemini
                var parsedDate = await _geminiService.ParseVoiceDateTimeAsync(request.VoiceDateText);
                if (parsedDate == null)
                {
                    return BadRequest(new { message = "Could not parse a valid date and time from your input. Please try again." });
                }

                // Operating Hours Check (08:00 to 20:00)
                if (parsedDate.Value.Hour < 8 || parsedDate.Value.Hour >= 20)
                {
                    return BadRequest(ValidationErrorEnvelope.Create(
                        "Appointments can only be scheduled within clinic operating hours (08:00 AM to 08:00 PM).",
                        "preferredDate",
                        $"Requested time ({parsedDate.Value:t}) is outside operating hours (08:00 AM - 08:00 PM)."
                    ));
                }

                // 2. Fetch patient details to fill FullName, Phone, Email
                var patient = await _dentalRepository.GetPatientByIdAsync((int)request.PatientId);
                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found." });
                }

                // 3. Save appointment to database
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Check availability scoped to Doctor and within 30 min window
                    var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM [dentist].[Appointments] 
                        WHERE (@DoctorId = 0 OR DoctorID = @DoctorId)
                          AND Status NOT IN ('Cancelled', 'No-Show', 'Rejected')
                          AND ABS(DATEDIFF(MINUTE, PreferredDate, @CheckDate)) < 30", conn);
                    checkCmd.Parameters.AddWithValue("@DoctorId", request.DoctorId);
                    checkCmd.Parameters.AddWithValue("@CheckDate", parsedDate.Value);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return BadRequest(new { message = $"That time slot ({parsedDate.Value:g}) is already allocated. Please choose another time." });
                    }

                    // Insert
                    var cmd = new SqlCommand(@"
                        INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, CreatedAt, Reason, DoctorID)
                        VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Confirmed', GETDATE(), @Reason, @DoctorID);
                        SELECT SCOPE_IDENTITY();
                    ", conn);
                    cmd.Parameters.AddWithValue("@FullName", $"{patient.FirstName} {patient.LastName}");
                    cmd.Parameters.AddWithValue("@Phone", patient.Phone ?? "N/A");
                    cmd.Parameters.AddWithValue("@Email", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreferredDate", parsedDate.Value);
                    cmd.Parameters.AddWithValue("@Reason", "AI voice follow-up appointment");
                    cmd.Parameters.AddWithValue("@DoctorID", request.DoctorId > 0 ? (object)request.DoctorId : DBNull.Value);

                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    // Queue WhatsApp appointment confirmation if patient opted in
                    int whatsAppMessageId = 0;
                    try
                    {
                        Doctor? doc = null;
                        if (request.DoctorId > 0)
                        {
                            doc = await _dentalRepository.GetDoctorByIdAsync(request.DoctorId);
                        }
                        var appt = new Appointment
                        {
                            AppointmentID = id,
                            FullName = $"{patient.FirstName} {patient.LastName}",
                            Phone = patient.Phone,
                            PreferredDate = parsedDate.Value,
                            DoctorID = request.DoctorId > 0 ? request.DoctorId : null,
                            Reason = "AI voice follow-up appointment"
                        };
                        whatsAppMessageId = await _whatsAppService.QueueAppointmentConfirmationAsync(appt, patient, doc);
                    }
                    catch (Exception waEx)
                    {
                        _logger.LogWarning(waEx, "Could not queue WhatsApp message for voice appointment {AppointmentId}", id);
                    }

                    return Ok(new { 
                        appointmentId = id, 
                        preferredDate = parsedDate.Value, 
                        whatsAppQueued = whatsAppMessageId > 0,
                        whatsAppMessageId = whatsAppMessageId > 0 ? whatsAppMessageId : (int?)null,
                        message = $"Appointment successfully scheduled for {parsedDate.Value:dddd, MMMM d, yyyy 'at' h:mm tt}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking voice appointment");
                return StatusCode(500, new { message = "An error occurred while booking the voice appointment. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] int? doctorId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    bool hasWhatsAppTable = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'[dentist].[WhatsAppMessages]')") > 0;

                    bool hasInvoicesTable = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'[dentist].[Invoices]')") > 0;

                    bool hasPaymentsTable = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'[dentist].[Payments]')") > 0;

                    string waApply = hasWhatsAppTable ? @"
                        OUTER APPLY (
                            SELECT TOP 1 m.Status, m.SentAt 
                            FROM [dentist].[WhatsAppMessages] m
                            WHERE m.AppointmentID = a.AppointmentID 
                            ORDER BY m.MessageID DESC
                        ) wa" : @"
                        OUTER APPLY (
                            SELECT CAST(NULL AS NVARCHAR(50)) AS Status, CAST(NULL AS DATETIME2) AS SentAt
                        ) wa";

                    string invApply = hasInvoicesTable ? @"
                        OUTER APPLY (
                            SELECT TOP 1 i.InvoiceID, i.InvoiceNumber, i.TotalAmount, i.Currency, i.Status
                            FROM [dentist].[Invoices] i
                            WHERE i.AppointmentID = a.AppointmentID
                            ORDER BY i.InvoiceID DESC
                        ) inv" : @"
                        OUTER APPLY (
                            SELECT CAST(NULL AS BIGINT) AS InvoiceID, CAST(NULL AS NVARCHAR(50)) AS InvoiceNumber, CAST(NULL AS DECIMAL(18,2)) AS TotalAmount, CAST(NULL AS NVARCHAR(10)) AS Currency, CAST(NULL AS NVARCHAR(30)) AS Status
                        ) inv";

                    string payApply = (hasPaymentsTable && hasInvoicesTable) ? @"
                        OUTER APPLY (
                            SELECT TOP 1 paym.PaymentMethod 
                            FROM [dentist].[Payments] paym
                            WHERE paym.InvoiceID = inv.InvoiceID
                            ORDER BY paym.PaymentID DESC
                        ) pay" : @"
                        OUTER APPLY (
                            SELECT CAST(NULL AS NVARCHAR(50)) AS PaymentMethod
                        ) pay";

                    string query = $@"
                        SELECT 
                            a.AppointmentID,
                            a.PatientID,
                            a.FullName,
                            a.Phone,
                            a.Email,
                            a.PreferredDate,
                            a.CreatedAt,
                            a.Status,
                            a.Reason,
                            a.Notes,
                            a.DoctorID,
                            p.WhatsAppOptIn,
                            wa.Status AS WhatsAppStatus,
                            wa.SentAt AS WhatsAppSentAt,
                            inv.InvoiceNumber,
                            inv.TotalAmount,
                            inv.Currency,
                            inv.Status AS InvoiceStatus,
                            pay.PaymentMethod
                        FROM [dentist].[Appointments] a
                        LEFT JOIN [dentist].[Patients] p ON p.PatientID = a.PatientID
                        {waApply}
                        {invApply}
                        {payApply}
                        WHERE (@DoctorId IS NULL 
                           OR a.DoctorID = @DoctorId 
                           OR (a.DoctorID IS NULL AND p.DoctorID = @DoctorId))
                        ORDER BY a.PreferredDate ASC";

                    var appointments = (await conn.QueryAsync<Appointment>(query, new { DoctorId = doctorId })).ToList();
                    foreach (var appt in appointments)
                    {
                        if (string.IsNullOrEmpty(appt.WhatsAppStatus) && appt.WhatsAppOptIn == false)
                        {
                            appt.WhatsAppStatus = "Skipped_NoOptIn";
                        }
                    }
                    _logger.LogInformation("[AppointmentsController] Fetched {Count} appointments with WhatsApp telemetry for Doctor #{DoctorId}", appointments.Count, doctorId);

                    if (appointments.Any())
                    {
                        var apptIds = appointments.Select(a => a.AppointmentID).ToList();
                        try
                        {
                            string itemsSql = @"
                                SELECT ii.InvoiceItemID, ii.InvoiceID, ii.ProcedureCode, ii.Description, ii.Quantity, ii.UnitPrice, ii.TotalPrice, i.AppointmentID
                                FROM [dentist].[InvoiceItems] ii
                                INNER JOIN [dentist].[Invoices] i ON i.InvoiceID = ii.InvoiceID
                                WHERE i.AppointmentID IN @ApptIds";

                            var items = await conn.QueryAsync<dynamic>(itemsSql, new { ApptIds = apptIds });
                            var itemsByAppt = items.GroupBy(it => (int)it.AppointmentID).ToDictionary(g => g.Key, g => g.ToList());

                            foreach (var appt in appointments)
                            {
                                if (itemsByAppt.TryGetValue(appt.AppointmentID, out var apptItems))
                                {
                                    appt.Items = apptItems.Select(it => new InvoiceItem
                                    {
                                        InvoiceItemID = (long)it.InvoiceItemID,
                                        InvoiceID = (long)it.InvoiceID,
                                        ProcedureCode = (string?)it.ProcedureCode,
                                        Description = (string)it.Description,
                                        Quantity = (int)it.Quantity,
                                        UnitPrice = (decimal)it.UnitPrice,
                                        TotalPrice = (decimal)it.TotalPrice
                                    }).ToList();
                                }
                            }
                        }
                        catch (Exception itemEx)
                        {
                            Console.WriteLine($"[Warning] Could not load invoice items for doctor appointments: {itemEx.Message}");
                        }
                    }

                    return Ok(appointments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving appointments. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] Appointment appointment)
        {
            if (appointment == null)
            {
                return BadRequest(new ValidationErrorEnvelope("Appointment payload cannot be empty.", null, "PAYLOAD_REQUIRED"));
            }

            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Full Name validation & sanitization
            appointment.FullName = _dataNorm.SanitizeText(appointment.FullName, 100);
            if (string.IsNullOrWhiteSpace(appointment.FullName) || appointment.FullName.Length < 2)
            {
                errors["fullName"] = "Patient full name must be at least 2 characters.";
            }

            // Phone normalization
            var phoneResult = _dataNorm.NormalizePakistaniPhone(appointment.Phone);
            if (!phoneResult.IsValid)
            {
                errors["phone"] = phoneResult.ErrorMessage ?? "Invalid Pakistani phone number.";
            }
            else
            {
                appointment.Phone = phoneResult.Normalized;
            }

            // Email normalization
            if (!string.IsNullOrWhiteSpace(appointment.Email))
            {
                appointment.Email = _dataNorm.NormalizeEmail(appointment.Email);
                if (appointment.Email.Length > 254 || !System.Text.RegularExpressions.Regex.IsMatch(appointment.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    errors["email"] = "Invalid email address format.";
                }
            }

            // Date validation
            if (appointment.PreferredDate == DateTime.MinValue)
            {
                errors["preferredDate"] = "Preferred appointment date and time is required.";
            }
            else
            {
                if (appointment.PreferredDate < DateTime.UtcNow.AddMinutes(-15))
                {
                    errors["preferredDate"] = "Appointment date cannot be in the past.";
                }
                else if (appointment.PreferredDate.Hour < 8 || appointment.PreferredDate.Hour >= 20)
                {
                    errors["preferredDate"] = "Appointments can only be scheduled within clinic operating hours (08:00 AM to 08:00 PM).";
                }
            }

            if (errors.Count > 0)
            {
                return BadRequest(new ValidationErrorEnvelope("Validation failed for one or more appointment fields.", errors));
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Collision check scoped to doctor and 30-minute interval
                    var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM [dentist].[Appointments] 
                        WHERE (@DoctorID IS NULL OR DoctorID = @DoctorID)
                          AND Status NOT IN ('Cancelled', 'No-Show', 'Rejected')
                          AND ABS(DATEDIFF(MINUTE, PreferredDate, @CheckDate)) < 30", conn);
                    checkCmd.Parameters.AddWithValue("@DoctorID", appointment.DoctorID.HasValue && appointment.DoctorID.Value > 0 ? (object)appointment.DoctorID.Value : DBNull.Value);
                    checkCmd.Parameters.AddWithValue("@CheckDate", appointment.PreferredDate);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return BadRequest(ValidationErrorEnvelope.Create(
                            "That time slot is already allocated for this doctor. Please choose another time.",
                            "preferredDate",
                            $"Time slot near {appointment.PreferredDate:t} is occupied."
                        ));
                    }

                    bool hasPatientIdCol = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') AND name = N'PatientID'") > 0;

                    int? patientIdToSave = appointment.PatientID;
                    Patient? matchedPatient = null;
                    if (patientIdToSave.HasValue && patientIdToSave.Value > 0)
                    {
                        matchedPatient = await _dentalRepository.GetPatientByIdAsync(patientIdToSave.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(appointment.Phone))
                    {
                        matchedPatient = await _dentalRepository.FindDuplicatePatientAsync(
                            appointment.DoctorID ?? 0, 
                            appointment.FullName?.Split(' ').FirstOrDefault() ?? "", 
                            appointment.FullName?.Split(' ').LastOrDefault() ?? "", 
                            null, 
                            appointment.Phone);
                        if (matchedPatient != null)
                        {
                            patientIdToSave = matchedPatient.PatientID;
                        }
                    }

                    bool hasReasonCol = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') AND name = N'Reason'") > 0;

                    string insertSql;
                    if (hasPatientIdCol && patientIdToSave.HasValue)
                    {
                        if (hasReasonCol)
                        {
                            insertSql = @"
                                INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID, PatientID, Reason)
                                VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID, @PatientID, @Reason);
                                SELECT SCOPE_IDENTITY();";
                        }
                        else
                        {
                            insertSql = @"
                                INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID, PatientID)
                                VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID, @PatientID);
                                SELECT SCOPE_IDENTITY();";
                        }
                    }
                    else
                    {
                        if (hasReasonCol)
                        {
                            insertSql = @"
                                INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID, Reason)
                                VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID, @Reason);
                                SELECT SCOPE_IDENTITY();";
                        }
                        else
                        {
                            insertSql = @"
                                INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID)
                                VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID);
                                SELECT SCOPE_IDENTITY();";
                        }
                    }

                    var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@FullName", appointment.FullName);
                    cmd.Parameters.AddWithValue("@Phone", appointment.Phone);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(appointment.Email) ? (object)DBNull.Value : appointment.Email);
                    cmd.Parameters.AddWithValue("@DoctorID", appointment.DoctorID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreferredDate", appointment.PreferredDate);
                    if (hasReasonCol)
                    {
                        cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(appointment.Reason) ? (object)DBNull.Value : appointment.Reason);
                    }
                    if (hasPatientIdCol && patientIdToSave.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientIdToSave.Value);
                    }

                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    appointment.AppointmentID = id;
                    appointment.PatientID = patientIdToSave;
                    appointment.Status = "Pending";

                    // WhatsApp confirmation queueing
                    int whatsAppMessageId = 0;
                    bool whatsAppQueued = false;
                    try
                    {
                        var patientForWhatsApp = matchedPatient;
                        if (patientForWhatsApp == null && !string.IsNullOrWhiteSpace(appointment.Phone))
                        {
                            patientForWhatsApp = new Patient
                            {
                                FirstName = appointment.FullName?.Split(' ').FirstOrDefault() ?? "Patient",
                                LastName = appointment.FullName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                Phone = appointment.Phone,
                                WhatsAppOptIn = true
                            };
                        }

                        if (patientForWhatsApp != null)
                        {
                            patientForWhatsApp.WhatsAppOptIn = true;
                            if (patientForWhatsApp.PatientID > 0)
                            {
                                try
                                {
                                    using var optCmd = new SqlCommand("UPDATE [dentist].[Patients] SET WhatsAppOptIn = 1 WHERE PatientID = @Pid AND (WhatsAppOptIn = 0 OR WhatsAppOptIn IS NULL)", conn);
                                    optCmd.Parameters.AddWithValue("@Pid", patientForWhatsApp.PatientID);
                                    await optCmd.ExecuteNonQueryAsync();
                                }
                                catch { }
                            }

                            Doctor? doc = null;
                            if (appointment.DoctorID.HasValue && appointment.DoctorID.Value > 0)
                            {
                                doc = await _dentalRepository.GetDoctorByIdAsync(appointment.DoctorID.Value);
                            }
                            whatsAppMessageId = await _whatsAppService.QueueAppointmentConfirmationAsync(appointment, patientForWhatsApp, doc);
                            whatsAppQueued = whatsAppMessageId > 0;
                        }
                    }
                    catch (Exception waEx)
                    {
                        _logger.LogWarning(waEx, "Could not queue WhatsApp message for appointment {AppointmentId}", id);
                    }

                    bool emailSent = false;
                    string emailError = null;

                    if (!string.IsNullOrEmpty(appointment.Email))
                    {
                        emailSent = true;
                        var apptEmail = appointment.Email;
                        var apptName = appointment.FullName;
                        var apptDate = appointment.PreferredDate;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string subject = "Appointment Request Received";
                                string body = $"<h3>Hi {apptName},</h3><p>Your appointment request for {apptDate:f} has been received and is currently Pending.</p>";
                                await _emailService.SendEmailAsync(apptEmail, subject, body);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogWarning(emailEx, "Background appointment confirmation email failed for {Email}", apptEmail);
                            }
                        });
                    }

                    return Ok(new { appointment, emailSent, emailError, whatsAppQueued, whatsAppMessageId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, new { message = "An error occurred while creating the appointment. Please try again." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateStatusDto appointmentUpdate)
        {
            if (appointmentUpdate == null)
            {
                return BadRequest(new ValidationErrorEnvelope("Update payload is required.", null, "PAYLOAD_REQUIRED"));
            }

            // Status normalization (APT-001)
            string rawStatus = appointmentUpdate.Status?.Trim() ?? string.Empty;
            string newStatus = rawStatus;
            if (rawStatus.Equals("Rejected / Cancelled", StringComparison.OrdinalIgnoreCase) ||
                rawStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
                rawStatus.Equals("Cancel", StringComparison.OrdinalIgnoreCase) ||
                rawStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = "Cancelled";
            }
            else if (rawStatus.Equals("Done", StringComparison.OrdinalIgnoreCase) ||
                     rawStatus.Equals("Completed / Done", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = "Completed";
            }
            else if (rawStatus.Equals("Re-scheduled", StringComparison.OrdinalIgnoreCase) ||
                     rawStatus.Equals("Rescheduled", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = "Confirmed";
            }
            else if (string.IsNullOrWhiteSpace(rawStatus))
            {
                newStatus = "Pending";
            }

            // Whitelist valid statuses
            var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Pending", "Confirmed", "In-Progress", "Completed", "Cancelled", "No-Show", "Rejected"
            };

            if (!allowedStatuses.Contains(newStatus))
            {
                return BadRequest(ValidationErrorEnvelope.Create(
                    $"Invalid status '{newStatus}'. Allowed statuses: {string.Join(", ", allowedStatuses)}",
                    "status",
                    $"Must be one of: {string.Join(", ", allowedStatuses)}"
                ));
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    
                    string patientEmail = null;
                    string patientName = null;
                    DateTime prefDate = DateTime.MinValue;
                    int? existingDoctorId = null;

                    var getCmd = new SqlCommand("SELECT FullName, Email, PreferredDate, DoctorID FROM [dentist].[Appointments] WHERE AppointmentID = @Id", conn);
                    getCmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await getCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            patientName = reader["FullName"].ToString();
                            patientEmail = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                            prefDate = reader["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(reader["PreferredDate"]) : DateTime.MinValue;
                            existingDoctorId = reader["DoctorID"] != DBNull.Value ? Convert.ToInt32(reader["DoctorID"]) : (int?)null;
                        }
                    }

                    // APT-003: Support rescheduling with slot collision check
                    bool isReschedule = appointmentUpdate.PreferredDate.HasValue && appointmentUpdate.PreferredDate.Value > DateTime.MinValue;
                    DateTime targetDate = isReschedule ? appointmentUpdate.PreferredDate.Value : prefDate;
                    int? targetDoctor = (appointmentUpdate.DoctorID.HasValue && appointmentUpdate.DoctorID.Value > 0) 
                        ? appointmentUpdate.DoctorID.Value 
                        : existingDoctorId;

                    if (isReschedule && Math.Abs((targetDate - prefDate).TotalMinutes) > 5)
                    {
                        var checkCmd = new SqlCommand(@"
                            SELECT COUNT(*) 
                            FROM [dentist].[Appointments] 
                            WHERE AppointmentID != @Id
                              AND (@DoctorID IS NULL OR DoctorID = @DoctorID)
                              AND Status NOT IN ('Cancelled', 'No-Show', 'Rejected')
                              AND ABS(DATEDIFF(MINUTE, PreferredDate, @CheckDate)) < 30", conn);
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        checkCmd.Parameters.AddWithValue("@DoctorID", targetDoctor.HasValue ? (object)targetDoctor.Value : DBNull.Value);
                        checkCmd.Parameters.AddWithValue("@CheckDate", targetDate);
                        int conflictCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (conflictCount > 0)
                        {
                            return BadRequest(new { message = $"That time slot ({targetDate:g}) is already occupied. Please choose another time." });
                        }
                    }

                    bool hasNotesCol = false;
                    using (var checkColCmd = new SqlCommand("SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID('dentist.Appointments') AND name = 'Notes'", conn))
                    {
                        hasNotesCol = Convert.ToInt32(await checkColCmd.ExecuteScalarAsync()) > 0;
                    }

                    string updateSql = hasNotesCol
                        ? @"UPDATE [dentist].[Appointments] 
                            SET Status = @Status, 
                                Reason = COALESCE(@Reason, Reason),
                                Notes = CASE WHEN @Notes IS NOT NULL THEN @Notes ELSE Notes END,
                                PreferredDate = CASE WHEN @PreferredDate IS NOT NULL THEN @PreferredDate ELSE PreferredDate END,
                                DoctorID = CASE WHEN @DoctorID IS NOT NULL THEN @DoctorID ELSE DoctorID END
                            WHERE AppointmentID = @Id"
                        : @"UPDATE [dentist].[Appointments] 
                            SET Status = @Status, 
                                Reason = COALESCE(@Reason, Reason),
                                PreferredDate = CASE WHEN @PreferredDate IS NOT NULL THEN @PreferredDate ELSE PreferredDate END,
                                DoctorID = CASE WHEN @DoctorID IS NOT NULL THEN @DoctorID ELSE DoctorID END
                            WHERE AppointmentID = @Id";

                    var cmd = new SqlCommand(updateSql, conn);
                    
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(appointmentUpdate.Reason) ? (object)DBNull.Value : _dataNorm.SanitizeText(appointmentUpdate.Reason, 500));
                    if (hasNotesCol)
                    {
                        cmd.Parameters.AddWithValue("@Notes", appointmentUpdate.Notes != null ? (object)_dataNorm.SanitizeText(appointmentUpdate.Notes, 1000) : DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@PreferredDate", isReschedule ? (object)targetDate : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DoctorID", (appointmentUpdate.DoctorID.HasValue && appointmentUpdate.DoctorID.Value > 0) ? (object)appointmentUpdate.DoctorID.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = "Appointment not found." });
                    }

                    bool emailSent = false;
                    string emailError = null;

                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        emailSent = true;
                        var pEmail = patientEmail;
                        var pName = patientName;
                        var pDate = targetDate;
                        var pStatus = newStatus;
                        var pReason = appointmentUpdate.Reason;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string subject = $"Appointment Update: {pStatus}";
                                string body = $"<h3>Hi {pName},</h3><p>Your appointment on {pDate:f} has been updated.</p><p><b>New Status:</b> {pStatus}</p>";
                                if (!string.IsNullOrEmpty(pReason)) {
                                    body += $"<p><b>Note:</b> {pReason}</p>";
                                }
                                await _emailService.SendEmailAsync(pEmail, subject, body);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Background email notification failed for appointment {AppointmentId}", id);
                            }
                        });
                    }

                    return Ok(new { success = true, message = "Status updated successfully.", status = newStatus, preferredDate = targetDate, doctorId = targetDoctor, emailSent, emailError });
                }
            }
            catch (SqlException ex) when (ex.Number == 207) // Invalid column name 'Reason'
            {
                return BadRequest(new { message = "SQL Update Required: Please run the SQL command to add the 'Reason' column." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status for ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the appointment status. Please try again." });
            }
        }

        [HttpPut("{id}/treatment-plan")]
        public async Task<IActionResult> UpdateTreatmentPlan(int id, [FromBody] UpdateTreatmentPlanRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Valid appointment ID is required." });
            }

            var (success, message, updatedAppt) = await _dentalRepository.UpdateAppointmentTreatmentPlanDoctorAsync(
                id,
                null,
                request?.Procedures,
                request?.Notes
            );

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new
            {
                message = "Doctor treatment plan and notes updated successfully.",
                appointment = updatedAppt
            });
        }

        // ==========================================
        // 6. RECORD TREATMENT SESSION TIMER
        // ==========================================
        [HttpPost("{id}/record-treatment-time")]
        [HttpPost("record-treatment-time")]
        public async Task<IActionResult> RecordAppointmentTreatmentTime(int id, [FromBody] RecordTreatmentTimeRequest request)
        {
            int apptId = id > 0 ? id : (request?.AppointmentId ?? 0);
            int durationSecs = request?.DurationSeconds ?? 0;
            string formatted = !string.IsNullOrWhiteSpace(request?.DurationFormatted) 
                ? request.DurationFormatted 
                : $"{durationSecs / 60}m {durationSecs % 60}s";
            DateTime ended = request?.EndedAt ?? DateTime.UtcNow;
            DateTime started = request?.StartedAt ?? ended.AddSeconds(-durationSecs);
            int patientId = request?.PatientId ?? 0;
            int doctorId = request?.DoctorId ?? 1;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // If apptId was not passed, locate the patient's active/today or most recent appointment
                if (apptId <= 0 && patientId > 0)
                {
                    apptId = await conn.QueryFirstOrDefaultAsync<int>(@"
                        SELECT TOP 1 AppointmentID 
                        FROM [dentist].[Appointments] 
                        WHERE PatientID = @PatientId 
                        ORDER BY 
                            CASE WHEN CAST(PreferredDate AS DATE) = CAST(GETDATE() AS DATE) THEN 0 ELSE 1 END,
                            PreferredDate DESC",
                        new { PatientId = patientId });
                }

                if (apptId > 0)
                {
                    var appt = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT AppointmentID, PatientID, DoctorID, Notes, TreatmentDurationSeconds FROM [dentist].[Appointments] WHERE AppointmentID = @Id",
                        new { Id = apptId });

                    if (appt != null)
                    {
                        if (patientId <= 0 && appt.PatientID != null) patientId = (int)appt.PatientID;
                        if (doctorId <= 1 && appt.DoctorID != null) doctorId = (int)appt.DoctorID;

                        int accumulatedSeconds = ((int)(appt.TreatmentDurationSeconds ?? 0)) + durationSecs;
                        string timerNote = $"[Treatment Session: {formatted} ({durationSecs}s) on {DateTime.UtcNow:yyyy-MM-dd HH:mm}]";
                        string existingNotes = (string)appt.Notes ?? string.Empty;
                        string updatedNotes = string.IsNullOrWhiteSpace(existingNotes) 
                            ? timerNote 
                            : $"{existingNotes}\n{timerNote}";

                        await conn.ExecuteAsync(@"
                            UPDATE [dentist].[Appointments]
                            SET TreatmentDurationSeconds = @Accumulated,
                                TreatmentDurationFormatted = @Formatted,
                                TreatmentStartedAt = COALESCE(TreatmentStartedAt, @Started),
                                TreatmentEndedAt = @Ended,
                                Notes = @Notes
                            WHERE AppointmentID = @Id",
                            new 
                            { 
                                Accumulated = accumulatedSeconds, 
                                Formatted = formatted, 
                                Started = started, 
                                Ended = ended, 
                                Notes = updatedNotes, 
                                Id = apptId 
                            });

                        // Audit Log
                        await conn.ExecuteAsync(@"
                            INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, OldValue, NewValue, UserID, UserRole, CreatedAt)
                            VALUES ('Appointment', @Id, 'treatment_time_recorded', CAST(@OldVal AS NVARCHAR(MAX)), CAST(@NewVal AS NVARCHAR(MAX)), @UserId, 'Doctor', SYSUTCDATETIME())",
                            new
                            {
                                Id = apptId,
                                OldVal = (int)(appt.TreatmentDurationSeconds ?? 0),
                                NewVal = accumulatedSeconds,
                                UserId = doctorId
                            }).ConfigureAwait(false);
                    }
                }

                // Clinical Log & Treatment Session for Patient
                if (patientId > 0)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO [dentist].[ClinicalLogs] (PatientID, DoctorID, Action, Message, CreatedAt)
                        VALUES (@PatientId, @DoctorId, 'TREATMENT_TIMER_RECORDED', @Message, SYSUTCDATETIME())",
                        new
                        {
                            PatientId = patientId,
                            DoctorId = doctorId,
                            Message = $"Treatment session recorded. Duration: {formatted} ({durationSecs}s) for Patient #{patientId}, Appointment #{(apptId > 0 ? apptId.ToString() : "N/A")}."
                        }).ConfigureAwait(false);

                    // Ensure TreatmentSessions table also holds this session record
                    try
                    {
                        await conn.ExecuteAsync(@"
                            IF OBJECT_ID('dentist.TreatmentSessions', 'U') IS NOT NULL
                            BEGIN
                                INSERT INTO [dentist].[TreatmentSessions]
                                (PatientID, DoctorID, AppointmentID, StartedAt, EndedAt, DurationSeconds, FormattedDuration, Status, ActionCount, SummaryNotes, CreatedAt, UpdatedAt)
                                VALUES
                                (@PatientId, @DoctorId, @ApptId, @Started, @Ended, @Duration, @Formatted, 'completed', 1, @Notes, SYSUTCDATETIME(), SYSUTCDATETIME());
                            END",
                            new
                            {
                                PatientId = patientId,
                                DoctorId = doctorId,
                                ApptId = apptId > 0 ? (int?)apptId : null,
                                Started = started,
                                Ended = ended,
                                Duration = durationSecs,
                                Formatted = formatted,
                                Notes = $"Treatment session recorded ({formatted}) on Odontogram"
                            }).ConfigureAwait(false);
                    }
                    catch { }
                }

                return Ok(new
                {
                    success = true,
                    appointmentId = apptId > 0 ? apptId : (int?)null,
                    patientId = patientId,
                    durationSeconds = durationSecs,
                    durationFormatted = formatted,
                    message = $"Treatment duration of {formatted} appended successfully for Patient #{patientId}{(apptId > 0 ? $" (Appointment #{apptId})" : "")}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording treatment time for Appointment #{ApptId}, Patient #{PatientId}", apptId, patientId);
                return StatusCode(500, new { message = "Failed to record treatment time: " + ex.Message });
            }
        }
    }

    public class RecordTreatmentTimeRequest
    {
        public int? AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public int DurationSeconds { get; set; }
        public string? DurationFormatted { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? Notes { get; set; }
    }
}

