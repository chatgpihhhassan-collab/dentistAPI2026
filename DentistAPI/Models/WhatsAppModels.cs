using System;
using System.Text.Json.Serialization;

namespace DentistAPI.Models
{
    /// <summary>
    /// Represents a WhatsApp message in the [dentist].[WhatsAppMessages] audit and rate-limited queue table.
    /// </summary>
    public class WhatsAppMessageEntity
    {
        public int MessageID { get; set; }
        public int? PatientID { get; set; }
        public int? AppointmentID { get; set; }
        public int? DoctorID { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string ChatID { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public string MessageType { get; set; } = "AppointmentBooking"; // 'AppointmentBooking', 'ManualTest', 'Reminder', 'SystemNotice'
        public string Status { get; set; } = "Pending"; // 'Pending', 'Queued', 'Sending', 'Sent', 'Delivered', 'Read', 'Failed', 'Skipped_NoOptIn'
        public string? OpenWAMessageID { get; set; }
        public string SessionName { get; set; } = "dentia-clinic-bot";
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public string? ErrorMessage { get; set; }
        public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Joined/computed metadata for UI display
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
    }

    /// <summary>
    /// Represents the gateway session state in [dentist].[WhatsAppSessions].
    /// </summary>
    public class WhatsAppSessionEntity
    {
        public int SessionRecordID { get; set; }
        public string SessionName { get; set; } = "dentia-clinic-bot";
        public string Status { get; set; } = "DISCONNECTED"; // 'DISCONNECTED', 'SCAN_QR_CODE', 'WORKING', 'STARTING', 'FAILED'
        public string? QRCodeData { get; set; }
        public string? ConnectedPhoneNumber { get; set; }
        public string? PushName { get; set; }
        public bool IsHealthy { get; set; } = false;
        public DateTime? LastPingAt { get; set; }
        public DateTime LastStatusChangeAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request payload sent to OpenWA POST /api/sessions/{sessionId}/messages/send-text
    /// </summary>
    public class OpenWASendTextRequest
    {
        [JsonPropertyName("chatId")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request payload sent to OpenWA POST /api/sessions/{sessionId}/messages/send-media
    /// </summary>
    public class OpenWASendMediaRequest
    {
        [JsonPropertyName("chatId")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty; // base64 or URL

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }
    }

    /// <summary>
    /// Response from OpenWA session endpoints
    /// </summary>
    public class OpenWASessionResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("me")]
        public OpenWAMeInfo? Me { get; set; }
    }

    public class OpenWAMeInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("pushname")]
        public string? Pushname { get; set; }
    }

    /// <summary>
    /// Incoming OpenWA Webhook event payload
    /// </summary>
    public class OpenWAWebhookPayload
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; } // 'message.received', 'session.status', 'message.status'

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("data")]
        public System.Text.Json.JsonElement Data { get; set; }

        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }
    }

    /// <summary>
    /// Live session status summary for Admin Dashboard
    /// </summary>
    public class WhatsAppSessionStatusDto
    {
        public string SessionName { get; set; } = "dentia-clinic-bot";
        public string Status { get; set; } = "DISCONNECTED"; // 'WORKING', 'SCAN_QR_CODE', 'DISCONNECTED', 'FAILED'
        public bool IsConnected { get; set; } = false;
        public string? ConnectedPhoneNumber { get; set; }
        public string? PushName { get; set; }
        public string? QRCodeData { get; set; }
        public DateTime? LastPingAt { get; set; }
        public int PendingQueueCount { get; set; } = 0;
        public int SentTodayCount { get; set; } = 0;
        public int FailedTodayCount { get; set; } = 0;
        public int TotalLoggedMessages { get; set; } = 0;
        public string GatewayUrl { get; set; } = "http://localhost:2785";
    }

    /// <summary>
    /// DTO for manual test dispatch from Admin Dashboard
    /// </summary>
    public class SendWhatsAppTestRequest
    {
        public string Phone { get; set; } = string.Empty;
        public string? Message { get; set; }
    }

    /// <summary>
    /// DTO for toggling a patient's WhatsApp opt-in state
    /// </summary>
    public class UpdateOptInRequest
    {
        public bool OptIn { get; set; }
    }

    /// <summary>
    /// Service result for WhatsApp dispatch operations
    /// </summary>
    public class WhatsAppSendResult
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? ChatId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public bool FallbackToEmailTriggered { get; set; }
    }
}
