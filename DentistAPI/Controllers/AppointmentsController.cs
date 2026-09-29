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

        public AppointmentsController(
            IConfiguration configuration, 
            IEmailService emailService,
            IGeminiDentalNotesService geminiService,
            DentalRepository dentalRepository,
            ILogger<AppointmentsController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailService = emailService;
            _geminiService = geminiService;
            _dentalRepository = dentalRepository;
            _logger = logger;
        }

        [HttpPost("book-voice")]
        public async Task<IActionResult> BookVoiceAppointment([FromBody] VoiceBookingRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.VoiceDateText))
            {
                return BadRequest("Invalid voice booking data.");
            }

            try
            {
                // 1. Parse date using Gemini
                var parsedDate = await _geminiService.ParseVoiceDateTimeAsync(request.VoiceDateText);
                if (parsedDate == null)
                {
                    return BadRequest(new { message = "Could not parse a valid date and time from your input. Please try again." });
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

                    // Check availability
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM [dentist].[Appointments] WHERE PreferredDate = @CheckDate", conn);
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
                    cmd.Parameters.AddWithValue("@DoctorID", request.DoctorId);

                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return Ok(new { 
                        appointmentId = id, 
                        preferredDate = parsedDate.Value, 
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
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    if (appointment.PreferredDate != DateTime.MinValue)
                    {
                        var checkCmd = new SqlCommand("SELECT COUNT(*) FROM [dentist].[Appointments] WHERE PreferredDate = @CheckDate", conn);
                        checkCmd.Parameters.AddWithValue("@CheckDate", appointment.PreferredDate);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            return BadRequest(new { message = "That time is already allocated. Please choose another time." });
                        }
                    }

                    var cmd = new SqlCommand(@"
                        INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, DoctorID)
                        VALUES (@FullName, @Phone, @Email, @PreferredDate, 'Pending', @DoctorID);
                        SELECT SCOPE_IDENTITY();
                    ", conn);
                    cmd.Parameters.AddWithValue("@FullName", appointment.FullName);
                    cmd.Parameters.AddWithValue("@Phone", appointment.Phone);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(appointment.Email) ? (object)DBNull.Value : appointment.Email);
                    cmd.Parameters.AddWithValue("@DoctorID", appointment.DoctorID ?? (object)DBNull.Value);
                    if (appointment.PreferredDate == DateTime.MinValue)
                        cmd.Parameters.AddWithValue("@PreferredDate", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@PreferredDate", appointment.PreferredDate);

                    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    appointment.AppointmentID = id;
                    appointment.Status = "Pending";

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

                    return Ok(new { appointment, emailSent, emailError });
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
                    
                    cmd.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(appointmentUpdate.Status) ? "Pending" : appointmentUpdate.Status);
                    cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(appointmentUpdate.Reason) ? (object)DBNull.Value : appointmentUpdate.Reason);
                    cmd.Parameters.AddWithValue("@Notes", appointmentUpdate.Notes != null ? (object)appointmentUpdate.Notes : DBNull.Value);
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
                        string subject = $"Appointment Update: {appointmentUpdate.Status}";
                        string body = $"<h3>Hi {patientName},</h3><p>Your appointment on {prefDate:f} has been updated.</p><p><b>New Status:</b> {appointmentUpdate.Status}</p>";
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

