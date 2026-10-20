using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Models;
using DentistAPI.Services;
using DentistAPI.Repositories;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly DentalRepository _dentalRepository;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(
            IWhatsAppService whatsAppService,
            DentalRepository dentalRepository,
            ILogger<WhatsAppController> logger)
        {
            _whatsAppService = whatsAppService;
            _dentalRepository = dentalRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the live session connection status, battery, and queue telemetry.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _whatsAppService.GetSessionStatusAsync();
            return Ok(status);
        }

        /// <summary>
        /// Retrieves the pairing QR code from OpenWA.
        /// </summary>
        [HttpGet("qr")]
        public async Task<IActionResult> GetQRCode()
        {
            var qr = await _whatsAppService.GetQRCodeAsync();
            if (string.IsNullOrEmpty(qr))
            {
                return Ok(new { qr = (string?)null, message = "No active QR code required or session already linked." });
            }
            return Ok(new { qr });
        }

        /// <summary>
        /// Starts or initializes the OpenWA session.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartSession()
        {
            bool success = await _whatsAppService.StartSessionAsync();
            return Ok(new { success, message = success ? "Session start signal sent successfully." : "Failed to trigger session start." });
        }

        /// <summary>
        /// Fetches the recent outbound/inbound WhatsApp messages with optional status filter.
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, [FromQuery] string? status = null)
        {
            if (limit <= 0 || limit > 200) limit = 50;
            var logs = await _whatsAppService.GetRecentMessageLogsAsync(limit, status);
            return Ok(logs);
        }

        /// <summary>
        /// Dispatches a manual test WhatsApp message from the Admin dashboard.
        /// </summary>
        [HttpPost("test-send")]
        public async Task<IActionResult> TestSend([FromBody] SendWhatsAppTestRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { message = "Recipient phone number is required." });
            }

            string testText = !string.IsNullOrWhiteSpace(request.Message)
                ? request.Message.Trim()
                : "🦷 *Dentia Dental Hospital Test Alert*\nThis is a verification message sent from the Dentia Clinical WhatsApp Gateway via OpenWA.\nTime: " + DateTime.UtcNow.ToString("g") + " UTC";

            var result = await _whatsAppService.SendTextAsync(request.Phone, testText, "ManualTest");
            if (result.Success)
            {
                return Ok(new { success = true, result });
            }
            return BadRequest(new { success = false, message = result.ErrorMessage, result });
        }

        /// <summary>
        /// Toggles a patient's WhatsApp opt-in status and dispatches retroactive appointment confirmation if activated.
        /// </summary>
        [HttpPut("patients/{patientId:int}/opt-in")]
        [HttpPatch("patients/{patientId:int}/opt-in")]
        public async Task<IActionResult> UpdatePatientOptIn(int patientId, [FromBody] UpdateOptInRequest request)
        {
            _logger.LogInformation("[WhatsAppController] Received opt-in update request for Patient #{PatientId}: OptIn={OptIn}", patientId, request?.OptIn);
            if (request == null)
            {
                _logger.LogWarning("[WhatsAppController] Opt-in payload is null for Patient #{PatientId}", patientId);
                return BadRequest(new { message = "Opt-in state payload required." });
            }

            var patient = await _dentalRepository.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("[WhatsAppController] Patient #{PatientId} not found.", patientId);
                return NotFound(new { message = "Patient not found." });
            }

            var result = await _whatsAppService.UpdatePatientOptInStatusWithResultAsync(patientId, request.OptIn);
            _logger.LogInformation("[WhatsAppController] Opt-in result for Patient #{PatientId}: Success={Success}, UpcomingAppointmentQueued={Queued}", 
                patientId, result.Success, result.UpcomingAppointmentQueued);

            return Ok(new { 
                success = result.Success, 
                patientId = result.PatientId, 
                whatsAppOptIn = result.OptIn,
                upcomingAppointmentQueued = result.UpcomingAppointmentQueued,
                queuedAppointmentId = result.QueuedAppointmentId,
                queuedAppointmentDate = result.QueuedAppointmentDate,
                message = result.Message
            });
        }

        /// <summary>
        /// Manually resends the WhatsApp appointment confirmation to the patient.
        /// </summary>
        [HttpPost("appointments/{appointmentId:int}/resend")]
        public async Task<IActionResult> ResendAppointmentConfirmation(int appointmentId)
        {
            _logger.LogInformation("[WhatsAppController] Received manual resend request for Appointment #{AppointmentId}", appointmentId);
            int messageId = await _whatsAppService.ResendAppointmentConfirmationAsync(appointmentId);
            if (messageId > 0)
            {
                _logger.LogInformation("[WhatsAppController] Resend successful for Appointment #{AppointmentId}. MessageID: {MessageId}", appointmentId, messageId);
                return Ok(new { success = true, appointmentId, messageId, message = "WhatsApp confirmation queued successfully." });
            }

            _logger.LogWarning("[WhatsAppController] Resend could not be completed for Appointment #{AppointmentId}", appointmentId);
            return BadRequest(new { success = false, appointmentId, message = "Could not queue WhatsApp reminder. Please check patient phone and WhatsApp opt-in status." });
        }

        /// <summary>
        /// Gets patient WhatsApp eligibility and normalized direct link.
        /// </summary>
        [HttpGet("patients/{patientId:int}/status")]
        public async Task<IActionResult> GetPatientWhatsAppStatus(int patientId)
        {
            var patient = await _dentalRepository.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            var norm = WhatsAppPhoneNormalizer.NormalizePakistani(patient.Phone);
            var directChatUrl = norm.IsValid ? WhatsAppPhoneNormalizer.BuildDirectChatUrl(patient.Phone) : null;

            return Ok(new
            {
                patientId = patient.PatientID,
                rawPhone = patient.Phone,
                normalizedPhone = norm.NormalizedDigits,
                chatId = norm.ChatId,
                isValidPhone = norm.IsValid,
                whatsAppOptIn = patient.WhatsAppOptIn,
                directChatUrl
            });
        }

        /// <summary>
        /// Resets a specific failed WhatsApp message back to Pending so the queue retries sending it.
        /// </summary>
        [HttpPost("messages/{messageId:int}/retry")]
        public async Task<IActionResult> RetryMessage(int messageId)
        {
            _logger.LogInformation("[WhatsAppController] Re-queueing WhatsApp message #{MessageId} for immediate retry", messageId);
            bool success = await _whatsAppService.RetryMessageAsync(messageId);
            if (success)
            {
                return Ok(new { success = true, messageId, message = $"Message #{messageId} re-queued for transmission." });
            }
            return BadRequest(new { success = false, messageId, message = $"Could not re-queue message #{messageId}." });
        }

        /// <summary>
        /// Resets all failed WhatsApp messages back to Pending.
        /// </summary>
        [HttpPost("messages/retry-failed")]
        public async Task<IActionResult> RetryAllFailed()
        {
            _logger.LogInformation("[WhatsAppController] Re-queueing all failed WhatsApp messages for retry");
            int count = await _whatsAppService.RetryAllFailedMessagesAsync();
            return Ok(new { success = true, retriedCount = count, message = $"{count} failed message(s) re-queued for transmission." });
        }
    }
}

