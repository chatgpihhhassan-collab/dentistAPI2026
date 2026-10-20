using System.Collections.Generic;
using System.Threading.Tasks;
using DentistAPI.Models;

namespace DentistAPI.Services
{
    public interface IWhatsAppService
    {
        /// <summary>
        /// Sends an immediate or priority text message via OpenWA.
        /// </summary>
        Task<WhatsAppSendResult> SendTextAsync(string phone, string text, string messageType = "Manual", int? patientId = null, int? appointmentId = null, int? doctorId = null);

        /// <summary>
        /// Sends media (image, PDF, dental document) via OpenWA.
        /// </summary>
        Task<WhatsAppSendResult> SendMediaAsync(string phone, string fileBase64OrUrl, string? caption = null, string? filename = null);

        /// <summary>
        /// Retrieves the live session status and gateway telemetry.
        /// </summary>
        Task<WhatsAppSessionStatusDto> GetSessionStatusAsync();

        /// <summary>
        /// Retrieves the base64 QR code image/data from OpenWA for pairing.
        /// </summary>
        Task<string?> GetQRCodeAsync();

        /// <summary>
        /// Starts or initializes the OpenWA session.
        /// </summary>
        Task<bool> StartSessionAsync();

        /// <summary>
        /// Queues a formatted appointment confirmation WhatsApp message for an opted-in patient.
        /// Returns the created MessageID, or 0 if skipped due to opt-out.
        /// </summary>
        Task<int> QueueAppointmentConfirmationAsync(Appointment appointment, Patient patient, Doctor? doctor);

        /// <summary>
        /// Fetches recent outgoing and incoming WhatsApp message audit logs.
        /// </summary>
        Task<IEnumerable<WhatsAppMessageEntity>> GetRecentMessageLogsAsync(int limit = 50, string? status = null);

        /// <summary>
        /// Updates the WhatsApp Opt-In status of a patient in the database.
        /// </summary>
        Task<bool> UpdatePatientOptInStatusAsync(int patientId, bool optIn);

        /// <summary>
        /// Updates patient WhatsApp opt-in state, and if activated, checks for and retroactively queues upcoming appointment confirmations.
        /// </summary>
        Task<WhatsAppOptInResult> UpdatePatientOptInStatusWithResultAsync(int patientId, bool optIn);

        /// <summary>
        /// Manually resends or re-queues an appointment confirmation message on WhatsApp.
        /// </summary>
        Task<int> ResendAppointmentConfirmationAsync(int appointmentId);

        /// <summary>
        /// Processes webhook events received from OpenWA with HMAC verification.
        /// </summary>
        Task<bool> ProcessIncomingWebhookAsync(OpenWAWebhookPayload payload);

        /// <summary>
        /// Background worker method: Processes a batch of pending/retrying messages respecting rate limits (5/min, 2-6s jitter).
        /// </summary>
        Task<int> ProcessPendingQueueBatchAsync();

        /// <summary>
        /// Resets a failed or retrying WhatsApp message back to Pending state for queue reprocessing.
        /// </summary>
        Task<bool> RetryMessageAsync(int messageId);

        /// <summary>
        /// Resets all failed WhatsApp messages created within the last 48 hours back to Pending state.
        /// </summary>
        Task<int> RetryAllFailedMessagesAsync();
    }
}
