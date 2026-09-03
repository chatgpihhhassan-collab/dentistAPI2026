using DentistAPI.Repositories;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        public AppointmentsController(
            IConfiguration configuration, 
            IEmailService emailService,
            IGeminiDentalNotesService geminiService,
            DentalRepository dentalRepository)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _emailService = emailService;
            _geminiService = geminiService;
            _dentalRepository = dentalRepository;
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
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAppointments([FromQuery] int? doctorId)
        {
            var appointments = new List<Appointment>();
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM [dentist].[Appointments] WHERE (@DoctorId IS NULL OR DoctorID = @DoctorId) ORDER BY PreferredDate ASC";
                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DoctorId", (object?)doctorId ?? DBNull.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        bool hasReason = Enumerable.Range(0, reader.FieldCount).Any(i => reader.GetName(i).Equals("Reason", StringComparison.OrdinalIgnoreCase));
                        while (reader.Read())
                        {
                            appointments.Add(new Appointment
                            {
                                AppointmentID = Convert.ToInt32(reader["AppointmentID"]),
                                FullName = reader["FullName"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "",
                                PreferredDate = reader["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(reader["PreferredDate"]) : DateTime.MinValue,
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : "Pending",
                                Reason = hasReason && reader["Reason"] != DBNull.Value ? reader["Reason"].ToString() : "",
                                DoctorID = reader["DoctorID"] != DBNull.Value ? Convert.ToInt32(reader["DoctorID"]) : null
                            });
                        }
                    }
                }
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
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
                return StatusCode(500, new { message = ex.Message });
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
                        SET Status = @Status, Reason = @Reason
                        WHERE AppointmentID = @Id
                    ", conn);
                    
                    cmd.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(appointmentUpdate.Status) ? "Pending" : appointmentUpdate.Status);
                    cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(appointmentUpdate.Reason) ? (object)DBNull.Value : appointmentUpdate.Reason);
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
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
