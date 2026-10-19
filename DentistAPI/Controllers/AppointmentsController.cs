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
                    string query = @"
                        SELECT DISTINCT 
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
                            i.InvoiceNumber,
                            i.TotalAmount,
                            i.Currency,
                            i.Status AS InvoiceStatus,
                            pay.PaymentMethod
                        FROM [dentist].[Appointments] a
                        LEFT JOIN [dentist].[Patients] p ON p.PatientID = a.PatientID
                        LEFT JOIN [dentist].[Invoices] i ON i.AppointmentID = a.AppointmentID
                        LEFT JOIN (
                            SELECT InvoiceID, MAX(PaymentMethod) as PaymentMethod 
                            FROM [dentist].[Payments] 
                            GROUP BY InvoiceID
                        ) pay ON pay.InvoiceID = i.InvoiceID
                        WHERE (@DoctorId IS NULL 
                           OR a.DoctorID = @DoctorId 
                           OR (a.DoctorID IS NULL AND p.DoctorID = @DoctorId))
                        ORDER BY a.PreferredDate ASC";

                    var appointments = (await conn.QueryAsync<Appointment>(query, new { DoctorId = doctorId })).ToList();

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

                    string insertSql;
                    if (hasPatientIdCol && patientIdToSave.HasValue)
                    {
                        insertSql = @"
                            INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID, PatientID)
                            VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID, @PatientID);
                            SELECT SCOPE_IDENTITY();";
                    }
                    else
                    {
                        insertSql = @"
                            INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID)
                            VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID);
                            SELECT SCOPE_IDENTITY();";
                    }

                    var cmd = new SqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@FullName", appointment.FullName);
                    cmd.Parameters.AddWithValue("@Phone", appointment.Phone);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(appointment.Email) ? (object)DBNull.Value : appointment.Email);
                    cmd.Parameters.AddWithValue("@DoctorID", appointment.DoctorID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreferredDate", appointment.PreferredDate);
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
                        string subject = "Appointment Request Received";
                        string body = $"<h3>Hi {appointment.FullName},</h3><p>Your appointment request for {appointment.PreferredDate:f} has been received and is currently Pending.</p>";
                        var result = await _emailService.SendEmailAsync(appointment.Email, subject, body);
                        emailSent = result.Success;
                        emailError = result.ErrorMessage;
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

            // Whitelist valid statuses
            var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Pending", "Confirmed", "In-Progress", "Completed", "Cancelled", "No-Show"
            };

            string newStatus = string.IsNullOrWhiteSpace(appointmentUpdate.Status) ? "Pending" : appointmentUpdate.Status.Trim();
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

                    var getCmd = new SqlCommand("SELECT FullName, Email, PreferredDate FROM [dentist].[Appointments] WHERE AppointmentID = @Id", conn);
                    getCmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await getCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            patientName = reader["FullName"].ToString();
                            patientEmail = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                            prefDate = reader["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(reader["PreferredDate"]) : DateTime.MinValue;
                        }
                    }

                    var cmd = new SqlCommand(@"
                        UPDATE [dentist].[Appointments] 
                        SET Status = @Status, 
                            Reason = COALESCE(@Reason, Reason),
                            Notes = CASE WHEN @Notes IS NOT NULL THEN @Notes ELSE Notes END
                        WHERE AppointmentID = @Id
                    ", conn);
                    
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(appointmentUpdate.Reason) ? (object)DBNull.Value : _dataNorm.SanitizeText(appointmentUpdate.Reason, 500));
                    cmd.Parameters.AddWithValue("@Notes", appointmentUpdate.Notes != null ? (object)_dataNorm.SanitizeText(appointmentUpdate.Notes, 1000) : DBNull.Value);
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
                        string subject = $"Appointment Update: {newStatus}";
                        string body = $"<h3>Hi {patientName},</h3><p>Your appointment on {prefDate:f} has been updated.</p><p><b>New Status:</b> {newStatus}</p>";
                        if (!string.IsNullOrEmpty(appointmentUpdate.Reason)) {
                            body += $"<p><b>Note:</b> {appointmentUpdate.Reason}</p>";
                        }
                        var result = await _emailService.SendEmailAsync(patientEmail, subject, body);
                        emailSent = result.Success;
                        emailError = result.ErrorMessage;
                    }

                    return Ok(new { message = "Status updated successfully.", emailSent, emailError });
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
    }
}

