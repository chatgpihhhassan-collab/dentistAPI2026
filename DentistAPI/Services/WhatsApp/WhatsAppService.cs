using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using DentistAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _connectionString;

        // OpenWA Configuration
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _sessionId;
        private readonly string _webhookSecret;
        private readonly string _clinicName;
        private readonly string _clinicAddress;
        private readonly string _clinicPhone;
        private readonly int _rateLimitMsgsPerMinute;
        private readonly int _minDelaySeconds;
        private readonly int _maxDelaySeconds;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration config,
            IEmailService emailService,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _emailService = emailService;
            _logger = logger;
            _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;

            // Load config with environment variable overrides
            _baseUrl = Environment.GetEnvironmentVariable("OPENWA_BASE_URL") 
                       ?? config["OpenWA:BaseUrl"] 
                       ?? "http://localhost:2785";

            _apiKey = Environment.GetEnvironmentVariable("OPENWA_API_KEY") 
                      ?? config["OpenWA:ApiKey"] 
                      ?? "dentia_prod_secret_key_998877";

            _sessionId = Environment.GetEnvironmentVariable("OPENWA_SESSION_ID") 
                         ?? config["OpenWA:SessionId"] 
                         ?? "dentia-clinic-bot";

            _webhookSecret = Environment.GetEnvironmentVariable("OPENWA_WEBHOOK_SECRET") 
                             ?? config["OpenWA:WebhookSecret"] 
                             ?? "dentia_webhook_hmac_secret_554433";

            _clinicName = config["OpenWA:ClinicName"] ?? "Dentia Premier Dental Hospital";
            _clinicAddress = config["OpenWA:ClinicAddress"] ?? "Suite 402, Medical City, Blue Area, Islamabad";
            _clinicPhone = config["OpenWA:ClinicPhone"] ?? "+92 51 2233445";

            _rateLimitMsgsPerMinute = int.TryParse(config["OpenWA:RateLimitMsgsPerMinute"], out var r) ? r : 5;
            _minDelaySeconds = int.TryParse(config["OpenWA:MinDelaySeconds"], out var minD) ? minD : 2;
            _maxDelaySeconds = int.TryParse(config["OpenWA:MaxDelaySeconds"], out var maxD) ? maxD : 6;
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string relativePath)
        {
            var cleanBase = _baseUrl.TrimEnd('/');
            var cleanPath = relativePath.TrimStart('/');
            var request = new HttpRequestMessage(method, $"{cleanBase}/{cleanPath}");
            request.Headers.Add("X-API-Key", _apiKey);
            return request;
        }

        public async Task<WhatsAppSessionStatusDto> GetSessionStatusAsync()
        {
            var dto = new WhatsAppSessionStatusDto
            {
                SessionName = _sessionId,
                GatewayUrl = _baseUrl,
                Status = "DISCONNECTED",
                IsConnected = false
            };

            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/sessions/{_sessionId}/status");
                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    string status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "UNKNOWN" : "UNKNOWN";
                    dto.Status = status.ToUpperInvariant();
                    dto.IsConnected = dto.Status == "WORKING" || dto.Status == "CONNECTED";

                    if (root.TryGetProperty("me", out var me) && me.ValueKind == JsonValueKind.Object)
                    {
                        if (me.TryGetProperty("id", out var idProp)) dto.ConnectedPhoneNumber = idProp.GetString();
                        if (me.TryGetProperty("pushname", out var pnProp)) dto.PushName = pnProp.GetString();
                    }

                    dto.LastPingAt = DateTime.UtcNow;

                    // Sync state into [dentist].[WhatsAppSessions]
                    await UpsertSessionRecordAsync(dto.Status, dto.ConnectedPhoneNumber, dto.PushName, dto.IsConnected);
                }
                else
                {
                    dto.Status = "DISCONNECTED";
                    dto.IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenWA gateway unreachable at {BaseUrl}", _baseUrl);
                dto.Status = "DISCONNECTED";
                dto.IsConnected = false;
            }

            // Enrich with database queue stats if tables exist
            try
            {
                using var conn = CreateConnection();
                var pendingCount = await conn.ExecuteScalarAsync<int>(@"
                    IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NOT NULL
                        SELECT COUNT(*) FROM [dentist].[WhatsAppMessages] WHERE Status IN ('Pending', 'Retrying')
                    ELSE
                        SELECT 0");

                var sentToday = await conn.ExecuteScalarAsync<int>(@"
                    IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NOT NULL
                        SELECT COUNT(*) FROM [dentist].[WhatsAppMessages] WHERE Status = 'Sent' AND CAST(SentAt AS DATE) = CAST(GETUTCDATE() AS DATE)
                    ELSE
                        SELECT 0");

                var failedToday = await conn.ExecuteScalarAsync<int>(@"
                    IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NOT NULL
                        SELECT COUNT(*) FROM [dentist].[WhatsAppMessages] WHERE Status = 'Failed' AND CAST(UpdatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
                    ELSE
                        SELECT 0");

                var totalCount = await conn.ExecuteScalarAsync<int>(@"
                    IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NOT NULL
                        SELECT COUNT(*) FROM [dentist].[WhatsAppMessages]
                    ELSE
                        SELECT 0");

                dto.PendingQueueCount = pendingCount;
                dto.SentTodayCount = sentToday;
                dto.FailedTodayCount = failedToday;
                dto.TotalLoggedMessages = totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not query WhatsAppMessages queue stats: {Message}", ex.Message);
            }

            return dto;
        }

        public async Task<string?> GetQRCodeAsync()
        {
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/sessions/{_sessionId}/qr");
                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (contentType.Contains("image"))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
                }

                var jsonStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("qr", out var qrElem))
                {
                    var qrVal = qrElem.GetString();
                    if (!string.IsNullOrEmpty(qrVal) && !qrVal.StartsWith("data:"))
                    {
                        return $"data:image/png;base64,{qrVal}";
                    }
                    return qrVal;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve QR code from OpenWA session {SessionId}", _sessionId);
                return null;
            }
        }

        public async Task<bool> StartSessionAsync()
        {
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/sessions/{_sessionId}/start");
                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start OpenWA session {SessionId}", _sessionId);
                return false;
            }
        }

        public async Task<WhatsAppSendResult> SendTextAsync(
            string phone, 
            string text, 
            string messageType = "Manual", 
            int? patientId = null, 
            int? appointmentId = null, 
            int? doctorId = null)
        {
            var norm = WhatsAppPhoneNormalizer.NormalizePakistani(phone);
            if (!norm.IsValid)
            {
                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = norm.Error ?? "Invalid phone number format."
                };
            }

            string directUrl = norm.IsValid ? $"https://wa.me/{norm.NormalizedDigits}?text={Uri.EscapeDataString(text)}" : string.Empty;

            // Insert into queue / audit log
            int messageId = await InsertMessageRecordAsync(new WhatsAppMessageEntity
            {
                PatientID = patientId,
                AppointmentID = appointmentId,
                DoctorID = doctorId,
                RecipientPhone = norm.NormalizedDigits,
                ChatID = norm.ChatId,
                MessageBody = text,
                MessageType = messageType,
                Status = "Sending",
                ScheduledFor = DateTime.UtcNow
            });

            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/sessions/{_sessionId}/messages/send-text");
                var payload = new OpenWASendTextRequest
                {
                    ChatId = norm.ChatId,
                    Text = text
                };
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string? openWaId = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("id", out var idElem))
                        {
                            openWaId = idElem.GetString();
                        }
                    }
                    catch { }

                    await UpdateMessageStatusAsync(messageId, "Sent", openWaId, null, DateTime.UtcNow);

                    return new WhatsAppSendResult
                    {
                        Success = true,
                        MessageId = openWaId ?? messageId.ToString(),
                        ChatId = norm.ChatId,
                        Status = "Sent",
                        DirectWhatsAppUrl = directUrl
                    };
                }
                else
                {
                    string errorMsg = $"OpenWA HTTP {(int)response.StatusCode}: {content}";
                    await UpdateMessageStatusAsync(messageId, "Failed", null, errorMsg, null);

                    return new WhatsAppSendResult
                    {
                        Success = false,
                        ErrorMessage = errorMsg,
                        ChatId = norm.ChatId,
                        Status = "Failed",
                        DirectWhatsAppUrl = directUrl
                    };
                }
            }
            catch (Exception ex)
            {
                string friendlyError = ex.Message;
                if (ex.Message.Contains("refused") || ex.Message.Contains("2785") || ex.InnerException?.Message.Contains("refused") == true)
                {
                    friendlyError = "WhatsApp notification service is currently in standby. The notification has been recorded in clinical history and can be dispatched instantly via 1-click 'Open in WhatsApp' (wa.me).";
                }
                _logger.LogError(ex, "Failed to send WhatsApp message via OpenWA to {ChatId}: {Error}", norm.ChatId, friendlyError);
                await UpdateMessageStatusAsync(messageId, "Failed", null, friendlyError, null);

                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = friendlyError,
                    ChatId = norm.ChatId,
                    Status = "Failed",
                    DirectWhatsAppUrl = directUrl
                };
            }
        }

        public async Task<WhatsAppSendResult> SendMediaAsync(string phone, string fileBase64OrUrl, string? caption = null, string? filename = null)
        {
            var norm = WhatsAppPhoneNormalizer.NormalizePakistani(phone);
            if (!norm.IsValid)
            {
                return new WhatsAppSendResult { Success = false, ErrorMessage = norm.Error };
            }

            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/sessions/{_sessionId}/messages/send-media");
                var payload = new OpenWASendMediaRequest
                {
                    ChatId = norm.ChatId,
                    File = fileBase64OrUrl,
                    Caption = caption,
                    Filename = filename ?? "dental_document.pdf"
                };
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return new WhatsAppSendResult
                {
                    Success = response.IsSuccessStatusCode,
                    ChatId = norm.ChatId,
                    Status = response.IsSuccessStatusCode ? "Sent" : "Failed",
                    ErrorMessage = response.IsSuccessStatusCode ? null : content
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp media to {ChatId}", norm.ChatId);
                return new WhatsAppSendResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<int> QueueAppointmentConfirmationAsync(Appointment appointment, Patient patient, Doctor? doctor)
        {
            if (patient == null)
            {
                _logger.LogWarning("WhatsApp appointment confirmation skipped: Patient is null.");
                return 0;
            }

            // 1. Strict Opt-in verification: Never send to patients who did not opt-in
            if (!patient.WhatsAppOptIn)
            {
                _logger.LogInformation("WhatsApp confirmation skipped for Patient #{PatientId} ({Name}): WhatsAppOptIn is false.", 
                    patient.PatientID, $"{patient.FirstName} {patient.LastName}");
                return 0;
            }

            // 2. Validate cell number
            var norm = WhatsAppPhoneNormalizer.NormalizePakistani(patient.Phone);
            if (!norm.IsValid)
            {
                _logger.LogWarning("WhatsApp confirmation skipped for Patient #{PatientId}: Phone '{Phone}' could not be normalized.", 
                    patient.PatientID, patient.Phone);
                return 0;
            }

            // 3. Format clinical confirmation template
            string messageBody = BuildAppointmentConfirmationTemplate(appointment, patient, doctor);

            // 4. Calculate random jitter delay (2 to 6 seconds) to prevent bot detection
            var randomDelaySeconds = Random.Shared.Next(_minDelaySeconds, _maxDelaySeconds + 1);
            var scheduledFor = DateTime.UtcNow.AddSeconds(randomDelaySeconds);

            var messageEntity = new WhatsAppMessageEntity
            {
                PatientID = patient.PatientID,
                AppointmentID = appointment.AppointmentID,
                DoctorID = appointment.DoctorID ?? doctor?.DoctorID,
                RecipientPhone = norm.NormalizedDigits,
                ChatID = norm.ChatId,
                MessageBody = messageBody,
                MessageType = "AppointmentBooking",
                Status = "Pending",
                SessionName = _sessionId,
                ScheduledFor = scheduledFor
            };

            int messageId = await InsertMessageRecordAsync(messageEntity);
            _logger.LogInformation("WhatsApp confirmation queued (MessageID: {MessageId}) for Patient #{PatientId} to {ChatId} scheduled for {ScheduledFor:u}", 
                messageId, patient.PatientID, norm.ChatId, scheduledFor);

            return messageId;
        }

        private string BuildAppointmentConfirmationTemplate(Appointment appointment, Patient patient, Doctor? doctor)
        {
            var doctorTitle = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}" : "Your Dental Specialist";
            if (doctor != null && !string.IsNullOrEmpty(doctor.Specialization))
            {
                doctorTitle += $" ({doctor.Specialization})";
            }

            var procedure = !string.IsNullOrWhiteSpace(appointment.Reason) 
                ? appointment.Reason 
                : "Comprehensive Dental Examination & Consultation";

            var apptDate = appointment.PreferredDate.ToString("dddd, MMMM d, yyyy");
            var apptTime = appointment.PreferredDate.ToString("hh:mm tt");

            var sb = new StringBuilder();
            sb.AppendLine($"🦷 *{_clinicName.ToUpperInvariant()}*");
            sb.AppendLine("📋 *Appointment Confirmation*");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"Dear *{patient.FirstName} {patient.LastName}*,");
            sb.AppendLine();
            sb.AppendLine("Your clinical appointment has been scheduled successfully:");
            sb.AppendLine();
            sb.AppendLine($"📅 *Date:* {apptDate}");
            sb.AppendLine($"⏰ *Time:* {apptTime}");
            sb.AppendLine($"👨‍⚕️ *Doctor:* {doctorTitle}");
            sb.AppendLine($"🩺 *Procedure/Reason:* {procedure}");
            sb.AppendLine($"📍 *Location:* {_clinicAddress}");
            sb.AppendLine();
            sb.AppendLine("*Important Patient Guidelines:*");
            sb.AppendLine("• Please arrive 10 minutes prior to your allocated slot.");
            sb.AppendLine("• Bring any recent dental radiographs, reports, or prescription cards.");
            sb.AppendLine("• If you are feeling unwell or have a fever, please notify us to reschedule.");
            sb.AppendLine();
            sb.AppendLine($"📞 For queries or rescheduling: {_clinicPhone}");
            sb.AppendLine("Reply *STOP* to unsubscribe from automated notifications.");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.Append("_Dentia Dental Healthcare Operating System_");

            return sb.ToString();
        }

        public async Task<int> ProcessPendingQueueBatchAsync()
        {
            int processedCount = 0;
            try
            {
                using var conn = CreateConnection();

                // Check table existence first
                var tableExists = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM sys.tables t 
                    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id 
                    WHERE s.name = 'dentist' AND t.name = 'WhatsAppMessages'");

                if (tableExists == 0) return 0;

                // Check Gateway Status
                var gatewayStatus = await GetSessionStatusAsync();
                bool isGatewayAvailable = gatewayStatus.IsConnected;

                // Fetch top pending/retrying messages (up to rate limit: e.g. 5)
                string fetchSql = @"
                    SELECT TOP (@BatchSize) m.*, 
                           p.FirstName + ' ' + p.LastName AS PatientName,
                           d.FirstName + ' ' + d.LastName AS DoctorName,
                           p.Email AS PatientEmail
                    FROM [dentist].[WhatsAppMessages] m
                    LEFT JOIN [dentist].[Patients] p ON p.PatientID = m.PatientID
                    LEFT JOIN [dentist].[Doctors] d ON d.DoctorID = m.DoctorID
                    WHERE m.Status IN ('Pending', 'Retrying') 
                      AND m.ScheduledFor <= SYSUTCDATETIME()
                    ORDER BY m.ScheduledFor ASC";

                var pendingMessages = (await conn.QueryAsync<dynamic>(fetchSql, new { BatchSize = _rateLimitMsgsPerMinute })).ToList();

                if (!pendingMessages.Any()) return 0;

                foreach (var msg in pendingMessages)
                {
                    int messageId = (int)msg.MessageID;
                    string chatId = (string)msg.ChatID;
                    string body = (string)msg.MessageBody;
                    int retryCount = (int)msg.RetryCount;
                    int maxRetries = (int)msg.MaxRetries;
                    string? patientEmail = (string?)msg.PatientEmail;

                    // If gateway is unavailable, trigger automatic fallback for critical appointment bookings
                    if (!isGatewayAvailable)
                    {
                        _logger.LogWarning("WhatsApp gateway unavailable. Checking fallback for MessageID {MessageId}...", messageId);

                        bool fallbackSent = false;
                        if (!string.IsNullOrEmpty(patientEmail))
                        {
                            try
                            {
                                var emailResult = await _emailService.SendEmailAsync(
                                    patientEmail, 
                                    $"Appointment Confirmation - {_clinicName}", 
                                    $"<pre style='font-family:sans-serif;'>{body}</pre>");
                                fallbackSent = emailResult.Success;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Email fallback failed for MessageID {MessageId}", messageId);
                            }
                        }

                        // Mark message as retrying with backoff or failed
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            await UpdateMessageStatusAsync(messageId, "Failed", null, 
                                $"Gateway disconnected. Fallback Email: {(fallbackSent ? "Sent" : "Unavailable")}", null);
                        }
                        else
                        {
                            var backoffTime = DateTime.UtcNow.AddMinutes(Math.Pow(2, retryCount));
                            await conn.ExecuteAsync(@"
                                UPDATE [dentist].[WhatsAppMessages]
                                SET Status = 'Retrying',
                                    RetryCount = @RetryCount,
                                    ScheduledFor = @ScheduledFor,
                                    ErrorMessage = @Error,
                                    UpdatedAt = SYSUTCDATETIME()
                                WHERE MessageID = @MessageID",
                                new { RetryCount = retryCount, ScheduledFor = backoffTime, Error = "Gateway disconnected; backoff scheduled.", MessageID = messageId });
                        }
                        continue;
                    }

                    // Enforce random jitter delay between successive messages (2 to 6 seconds)
                    var jitter = Random.Shared.Next(_minDelaySeconds, _maxDelaySeconds + 1);
                    await Task.Delay(TimeSpan.FromSeconds(jitter));

                    // Execute HTTP POST to OpenWA
                    try
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/sessions/{_sessionId}/messages/send-text");
                        var payload = new OpenWASendTextRequest { ChatId = chatId, Text = body };
                        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                        using var response = await _httpClient.SendAsync(request);
                        var responseText = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            string? openWaId = null;
                            try
                            {
                                using var doc = JsonDocument.Parse(responseText);
                                if (doc.RootElement.TryGetProperty("id", out var idElem))
                                {
                                    openWaId = idElem.GetString();
                                }
                            }
                            catch { }

                            await UpdateMessageStatusAsync(messageId, "Sent", openWaId, null, DateTime.UtcNow);
                            processedCount++;
                            _logger.LogInformation("Successfully dispatched WhatsApp MessageID {MessageId} to {ChatId}", messageId, chatId);
                        }
                        else
                        {
                            retryCount++;
                            string err = $"OpenWA HTTP {(int)response.StatusCode}: {responseText}";
                            if (retryCount >= maxRetries)
                            {
                                await UpdateMessageStatusAsync(messageId, "Failed", null, err, null);
                            }
                            else
                            {
                                var backoff = DateTime.UtcNow.AddMinutes(Math.Pow(2, retryCount));
                                await conn.ExecuteAsync(@"
                                    UPDATE [dentist].[WhatsAppMessages]
                                    SET Status = 'Retrying',
                                        RetryCount = @RetryCount,
                                        ScheduledFor = @ScheduledFor,
                                        ErrorMessage = @Error,
                                        UpdatedAt = SYSUTCDATETIME()
                                    WHERE MessageID = @MessageID",
                                    new { RetryCount = retryCount, ScheduledFor = backoff, Error = err, MessageID = messageId });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while sending WhatsApp MessageID {MessageId}", messageId);
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            await UpdateMessageStatusAsync(messageId, "Failed", null, ex.Message, null);
                        }
                        else
                        {
                            var backoff = DateTime.UtcNow.AddMinutes(Math.Pow(2, retryCount));
                            await conn.ExecuteAsync(@"
                                UPDATE [dentist].[WhatsAppMessages]
                                SET Status = 'Retrying',
                                    RetryCount = @RetryCount,
                                    ScheduledFor = @ScheduledFor,
                                    ErrorMessage = @Error,
                                    UpdatedAt = SYSUTCDATETIME()
                                WHERE MessageID = @MessageID",
                                new { RetryCount = retryCount, ScheduledFor = backoff, Error = ex.Message, MessageID = messageId });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ProcessPendingQueueBatchAsync");
            }

            return processedCount;
        }

        public async Task<IEnumerable<WhatsAppMessageEntity>> GetRecentMessageLogsAsync(int limit = 50, string? status = null)
        {
            try
            {
                using var conn = CreateConnection();

                var tableExists = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM sys.tables t 
                    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id 
                    WHERE s.name = 'dentist' AND t.name = 'WhatsAppMessages'");

                if (tableExists == 0) return Enumerable.Empty<WhatsAppMessageEntity>();

                string sql = @"
                    SELECT TOP (@Limit) 
                        m.MessageID, m.PatientID, m.AppointmentID, m.DoctorID,
                        m.RecipientPhone, m.ChatID, m.MessageBody, m.MessageType,
                        m.Status, m.OpenWAMessageID, m.SessionName, m.RetryCount,
                        m.MaxRetries, m.ErrorMessage, m.ScheduledFor, m.SentAt,
                        m.DeliveredAt, m.ReadAt, m.CreatedAt, m.UpdatedAt,
                        p.FirstName + ' ' + p.LastName AS PatientName,
                        d.FirstName + ' ' + d.LastName AS DoctorName
                    FROM [dentist].[WhatsAppMessages] m
                    LEFT JOIN [dentist].[Patients] p ON p.PatientID = m.PatientID
                    LEFT JOIN [dentist].[Doctors] d ON d.DoctorID = m.DoctorID
                    WHERE (@Status IS NULL OR m.Status = @Status)
                    ORDER BY m.MessageID DESC";

                return await conn.QueryAsync<WhatsAppMessageEntity>(sql, new { Limit = limit, Status = string.IsNullOrWhiteSpace(status) || status.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? null : status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve WhatsApp message logs");
                return Enumerable.Empty<WhatsAppMessageEntity>();
            }
        }

        public async Task<bool> UpdatePatientOptInStatusAsync(int patientId, bool optIn)
        {
            var result = await UpdatePatientOptInStatusWithResultAsync(patientId, optIn);
            return result.Success;
        }

        public async Task<WhatsAppOptInResult> UpdatePatientOptInStatusWithResultAsync(int patientId, bool optIn)
        {
            _logger.LogInformation("[WhatsAppService] Step 1: Initiating WhatsApp opt-in status change to {OptIn} for Patient #{PatientId}", optIn, patientId);
            try
            {
                using var conn = CreateConnection();
                string sql = @"
                    UPDATE [dentist].[Patients]
                    SET WhatsAppOptIn = @OptIn,
                        WhatsAppOptInAt = CASE WHEN @OptIn = 1 THEN SYSUTCDATETIME() ELSE WhatsAppOptInAt END
                    WHERE PatientID = @PatientID";

                int affected = await conn.ExecuteAsync(sql, new { PatientID = patientId, OptIn = optIn });
                if (affected == 0)
                {
                    _logger.LogWarning("[WhatsAppService] Patient #{PatientId} was not found to update opt-in status.", patientId);
                    return new WhatsAppOptInResult { Success = false, PatientId = patientId, OptIn = optIn, Message = "Patient record not found." };
                }

                _logger.LogInformation("[WhatsAppService] Step 2: Patient #{PatientId} opt-in database record updated to {OptIn}.", patientId, optIn);

                // If patient turned OFF WhatsApp, no retroactive appointment to dispatch
                if (!optIn)
                {
                    _logger.LogInformation("[WhatsAppService] Patient #{PatientId} opted out of WhatsApp updates.", patientId);
                    return new WhatsAppOptInResult 
                    { 
                        Success = true, 
                        PatientId = patientId, 
                        OptIn = false, 
                        UpcomingAppointmentQueued = false, 
                        Message = "Patient opted out of WhatsApp updates." 
                    };
                }

                // If patient activated WhatsApp, search for upcoming appointments (Today and Future) for retroactive dispatch
                _logger.LogInformation("[WhatsAppService] Step 3: Patient #{PatientId} activated WhatsApp. Querying database for upcoming appointments (Today and Future)...", patientId);

                var patient = await conn.QuerySingleOrDefaultAsync<Patient>(
                    "SELECT TOP 1 * FROM [dentist].[Patients] WHERE PatientID = @PatientId", 
                    new { PatientId = patientId });

                if (patient == null)
                {
                    _logger.LogWarning("[WhatsAppService] Patient #{PatientId} could not be re-queried.", patientId);
                    return new WhatsAppOptInResult { Success = true, PatientId = patientId, OptIn = true, UpcomingAppointmentQueued = false, Message = "WhatsApp activated." };
                }

                // Make sure opt-in flag is active in memory for queue method
                patient.WhatsAppOptIn = true;

                string upcomingSql = @"
                    SELECT TOP 1 * 
                    FROM [dentist].[Appointments]
                    WHERE PatientID = @PatientId
                      AND Status IN ('Pending', 'Confirmed')
                      AND PreferredDate >= CAST(GETUTCDATE() AS DATE)
                    ORDER BY PreferredDate ASC";

                var upcomingAppt = await conn.QuerySingleOrDefaultAsync<Appointment>(upcomingSql, new { PatientId = patientId });

                if (upcomingAppt == null)
                {
                    _logger.LogInformation("[WhatsAppService] Step 4: No upcoming appointments found for Patient #{PatientId}. No retroactive message needed.", patientId);
                    return new WhatsAppOptInResult 
                    { 
                        Success = true, 
                        PatientId = patientId, 
                        OptIn = true, 
                        UpcomingAppointmentQueued = false, 
                        Message = "Patient opted in for WhatsApp updates. No upcoming appointments pending." 
                    };
                }

                _logger.LogInformation("[WhatsAppService] Step 4: Found upcoming appointment #{AppointmentId} scheduled on {PreferredDate:u} for Patient #{PatientId}", 
                    upcomingAppt.AppointmentID, upcomingAppt.PreferredDate, patientId);

                // Check if a confirmation message was already queued or sent recently
                int existingCount = await conn.ExecuteScalarAsync<int>(@"
                    IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NOT NULL
                        SELECT COUNT(*) FROM [dentist].[WhatsAppMessages]
                        WHERE AppointmentID = @AppointmentId
                          AND Status IN ('Pending', 'Queued', 'Sending', 'Sent', 'Delivered')
                    ELSE
                        SELECT 0",
                    new { AppointmentId = upcomingAppt.AppointmentID });
                bool alreadyQueued = existingCount > 0;

                if (alreadyQueued)
                {
                    _logger.LogInformation("[WhatsAppService] Appointment #{AppointmentId} already has an active or sent WhatsApp message. Skipping duplicate.", upcomingAppt.AppointmentID);
                    return new WhatsAppOptInResult
                    {
                        Success = true,
                        PatientId = patientId,
                        OptIn = true,
                        UpcomingAppointmentQueued = false,
                        QueuedAppointmentId = upcomingAppt.AppointmentID,
                        QueuedAppointmentDate = upcomingAppt.PreferredDate,
                        Message = $"WhatsApp activated. Appointment on {upcomingAppt.PreferredDate:MMM dd, yyyy} was already confirmed previously."
                    };
                }

                // Fetch Doctor if assigned
                Doctor? doctor = null;
                if (upcomingAppt.DoctorID.HasValue && upcomingAppt.DoctorID.Value > 0)
                {
                    doctor = await conn.QuerySingleOrDefaultAsync<Doctor>(
                        "SELECT TOP 1 * FROM [dentist].[Doctors] WHERE DoctorID = @DoctorId",
                        new { DoctorId = upcomingAppt.DoctorID.Value });
                }

                _logger.LogInformation("[WhatsAppService] Step 5: Queuing retroactive confirmation for appointment #{AppointmentId} to recipient '{Phone}'...", 
                    upcomingAppt.AppointmentID, patient.Phone);

                int messageId = await QueueAppointmentConfirmationAsync(upcomingAppt, patient, doctor);

                _logger.LogInformation("[WhatsAppService] Step 6: Retroactive WhatsApp message #{MessageId} created successfully for appointment #{AppointmentId}.", 
                    messageId, upcomingAppt.AppointmentID);

                return new WhatsAppOptInResult
                {
                    Success = true,
                    PatientId = patientId,
                    OptIn = true,
                    UpcomingAppointmentQueued = messageId > 0,
                    QueuedAppointmentId = upcomingAppt.AppointmentID,
                    QueuedAppointmentDate = upcomingAppt.PreferredDate,
                    Message = messageId > 0 
                        ? $"WhatsApp activated! Confirmation queued for upcoming appointment on {upcomingAppt.PreferredDate:dddd, MMM dd}."
                        : "WhatsApp activated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhatsAppService] Error updating WhatsAppOptIn status for Patient #{PatientId}", patientId);
                return new WhatsAppOptInResult { Success = false, PatientId = patientId, OptIn = optIn, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<int> ResendAppointmentConfirmationAsync(int appointmentId)
        {
            _logger.LogInformation("[WhatsAppService] Step 1: Manual resend triggered for appointment #{AppointmentId}", appointmentId);
            try
            {
                using var conn = CreateConnection();
                var appt = await conn.QuerySingleOrDefaultAsync<Appointment>(
                    "SELECT TOP 1 * FROM [dentist].[Appointments] WHERE AppointmentID = @AppointmentId",
                    new { AppointmentId = appointmentId });

                if (appt == null)
                {
                    _logger.LogWarning("[WhatsAppService] Resend failed: Appointment #{AppointmentId} not found.", appointmentId);
                    return 0;
                }

                Patient? patient = null;
                if (appt.PatientID.HasValue && appt.PatientID.Value > 0)
                {
                    patient = await conn.QuerySingleOrDefaultAsync<Patient>(
                        "SELECT TOP 1 * FROM [dentist].[Patients] WHERE PatientID = @PatientId",
                        new { PatientId = appt.PatientID.Value });
                }

                if (patient == null)
                {
                    if (string.IsNullOrWhiteSpace(appt.Phone))
                    {
                        _logger.LogWarning("[WhatsAppService] Resend failed: No phone number associated with appointment #{AppointmentId}.", appointmentId);
                        return 0;
                    }

                    patient = new Patient
                    {
                        FirstName = appt.FullName?.Split(' ').FirstOrDefault() ?? "Patient",
                        LastName = appt.FullName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                        Phone = appt.Phone,
                        WhatsAppOptIn = true
                    };
                }

                // Enforce WhatsApp OptIn for resend
                patient.WhatsAppOptIn = true;

                Doctor? doctor = null;
                if (appt.DoctorID.HasValue && appt.DoctorID.Value > 0)
                {
                    doctor = await conn.QuerySingleOrDefaultAsync<Doctor>(
                        "SELECT TOP 1 * FROM [dentist].[Doctors] WHERE DoctorID = @DoctorId",
                        new { DoctorId = appt.DoctorID.Value });
                }

                _logger.LogInformation("[WhatsAppService] Step 2: Queuing confirmation message for Appointment #{AppointmentId} to recipient '{Phone}'...", 
                    appointmentId, patient.Phone);

                int messageId = await QueueAppointmentConfirmationAsync(appt, patient, doctor);
                _logger.LogInformation("[WhatsAppService] Step 3: Resend queued successfully with MessageID #{MessageId}", messageId);
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhatsAppService] Error resending appointment confirmation for #{AppointmentId}", appointmentId);
                return 0;
            }
        }

        public async Task<bool> ProcessIncomingWebhookAsync(OpenWAWebhookPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.Event))
                return false;

            try
            {
                using var conn = CreateConnection();

                // 1. Session Status Event
                if (payload.Event.Equals("session.status", StringComparison.OrdinalIgnoreCase))
                {
                    string status = payload.Data.TryGetProperty("status", out var s) ? s.GetString() ?? "DISCONNECTED" : "DISCONNECTED";
                    string? phone = payload.Data.TryGetProperty("phone", out var p) ? p.GetString() : null;
                    string? pushName = payload.Data.TryGetProperty("pushname", out var pn) ? pn.GetString() : null;
                    bool isHealthy = status.Equals("WORKING", StringComparison.OrdinalIgnoreCase);

                    await UpsertSessionRecordAsync(status, phone, pushName, isHealthy);
                    return true;
                }

                // 2. Incoming Message Event (Handle STOP unsubscribe)
                if (payload.Event.Equals("message.received", StringComparison.OrdinalIgnoreCase))
                {
                    string? from = payload.Data.TryGetProperty("from", out var fromProp) ? fromProp.GetString() : null;
                    string? text = payload.Data.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;

                    if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(text))
                    {
                        var trimmed = text.Trim().ToUpperInvariant();
                        if (trimmed == "STOP" || trimmed == "UNSUBSCRIBE" || trimmed == "OPT OUT" || trimmed == "CANCEL")
                        {
                            // Patient requested opt-out
                            var rawPhone = from.Replace("@c.us", "");
                            var norm = WhatsAppPhoneNormalizer.NormalizePakistani(rawPhone);

                            await conn.ExecuteAsync(@"
                                UPDATE [dentist].[Patients]
                                SET WhatsAppOptIn = 0
                                WHERE REPLACE(REPLACE(REPLACE(REPLACE(Phone, ' ', ''), '-', ''), '+', ''), '0', '') LIKE '%' + RIGHT(@Digits, 9)
                                   OR Phone = @Phone",
                                new { Digits = norm.NormalizedDigits, Phone = rawPhone });

                            _logger.LogInformation("Patient with WhatsApp {From} successfully opted out via STOP keyword.", from);
                        }
                    }
                    return true;
                }

                // 3. Message Status Event (Delivered / Read)
                if (payload.Event.Equals("message.status", StringComparison.OrdinalIgnoreCase))
                {
                    string? id = payload.Data.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    string? ack = payload.Data.TryGetProperty("ack", out var ackProp) ? ackProp.GetString() : null;

                    if (!string.IsNullOrEmpty(id))
                    {
                        string status = ack switch
                        {
                            "READ" or "3" => "Read",
                            "DELIVERED" or "2" => "Delivered",
                            _ => "Sent"
                        };

                        await conn.ExecuteAsync(@"
                            UPDATE [dentist].[WhatsAppMessages]
                            SET Status = @Status,
                                DeliveredAt = CASE WHEN @Status = 'Delivered' AND DeliveredAt IS NULL THEN SYSUTCDATETIME() ELSE DeliveredAt END,
                                ReadAt = CASE WHEN @Status = 'Read' AND ReadAt IS NULL THEN SYSUTCDATETIME() ELSE ReadAt END,
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE OpenWAMessageID = @ID",
                            new { Status = status, ID = id });
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming OpenWA webhook event {Event}", payload.Event);
            }

            return true;
        }

        private async Task<int> InsertMessageRecordAsync(WhatsAppMessageEntity entity)
        {
            try
            {
                using var conn = CreateConnection();

                // Ensure table exists
                await EnsureTablesCreatedAsync(conn);

                string sql = @"
                    INSERT INTO [dentist].[WhatsAppMessages] 
                    (PatientID, AppointmentID, DoctorID, RecipientPhone, ChatID, MessageBody, MessageType, Status, SessionName, ScheduledFor, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.MessageID
                    VALUES 
                    (@PatientID, @AppointmentID, @DoctorID, @RecipientPhone, @ChatID, @MessageBody, @MessageType, @Status, @SessionName, @ScheduledFor, SYSUTCDATETIME(), SYSUTCDATETIME())";

                return await conn.QuerySingleAsync<int>(sql, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert WhatsApp message record");
                return 0;
            }
        }

        private async Task UpdateMessageStatusAsync(int messageId, string status, string? openWaId, string? error, DateTime? sentAt)
        {
            if (messageId <= 0) return;
            try
            {
                using var conn = CreateConnection();
                string sql = @"
                    UPDATE [dentist].[WhatsAppMessages]
                    SET Status = @Status,
                        OpenWAMessageID = COALESCE(@OpenWaId, OpenWAMessageID),
                        ErrorMessage = @Error,
                        SentAt = COALESCE(@SentAt, SentAt),
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE MessageID = @MessageID";

                await conn.ExecuteAsync(sql, new { Status = status, OpenWaId = openWaId, Error = error, SentAt = sentAt, MessageID = messageId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update WhatsApp message status for MessageID {MessageId}", messageId);
            }
        }

        private async Task UpsertSessionRecordAsync(string status, string? phone, string? pushName, bool isHealthy)
        {
            try
            {
                using var conn = CreateConnection();
                await EnsureTablesCreatedAsync(conn);

                string sql = @"
                    IF EXISTS (SELECT 1 FROM [dentist].[WhatsAppSessions] WHERE SessionName = @SessionName)
                    BEGIN
                        UPDATE [dentist].[WhatsAppSessions]
                        SET Status = @Status,
                            ConnectedPhoneNumber = COALESCE(@Phone, ConnectedPhoneNumber),
                            PushName = COALESCE(@PushName, PushName),
                            IsHealthy = @IsHealthy,
                            LastPingAt = SYSUTCDATETIME(),
                            LastStatusChangeAt = SYSUTCDATETIME(),
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE SessionName = @SessionName;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [dentist].[WhatsAppSessions] 
                        (SessionName, Status, ConnectedPhoneNumber, PushName, IsHealthy, LastPingAt, LastStatusChangeAt, CreatedAt, UpdatedAt)
                        VALUES 
                        (@SessionName, @Status, @Phone, @PushName, @IsHealthy, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());
                    END";

                await conn.ExecuteAsync(sql, new { SessionName = _sessionId, Status = status, Phone = phone, PushName = pushName, IsHealthy = isHealthy });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to upsert session record for {SessionId}: {Message}", _sessionId, ex.Message);
            }
        }

        private async Task EnsureTablesCreatedAsync(IDbConnection conn)
        {
            string ddl = @"
                IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
                    EXEC('CREATE SCHEMA dentist');

                IF OBJECT_ID('[dentist].[WhatsAppMessages]', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dentist].[WhatsAppMessages] (
                        [MessageID]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PatientID]          INT NULL,
                        [AppointmentID]      INT NULL,
                        [DoctorID]           INT NULL,
                        [RecipientPhone]     NVARCHAR(30) NOT NULL,
                        [ChatID]             NVARCHAR(60) NOT NULL,
                        [MessageBody]        NVARCHAR(MAX) NOT NULL,
                        [MessageType]        NVARCHAR(50) NOT NULL DEFAULT 'AppointmentBooking',
                        [Status]             NVARCHAR(30) NOT NULL DEFAULT 'Pending',
                        [OpenWAMessageID]    NVARCHAR(120) NULL,
                        [SessionName]        NVARCHAR(100) NOT NULL DEFAULT 'dentia-clinic-bot',
                        [RetryCount]         INT NOT NULL DEFAULT 0,
                        [MaxRetries]         INT NOT NULL DEFAULT 3,
                        [ErrorMessage]       NVARCHAR(MAX) NULL,
                        [ScheduledFor]       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [SentAt]             DATETIME2 NULL,
                        [DeliveredAt]        DATETIME2 NULL,
                        [ReadAt]             DATETIME2 NULL,
                        [CreatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [UpdatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END

                IF OBJECT_ID('[dentist].[WhatsAppSessions]', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dentist].[WhatsAppSessions] (
                        [SessionRecordID]      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SessionName]          NVARCHAR(100) NOT NULL UNIQUE,
                        [Status]               NVARCHAR(50) NOT NULL DEFAULT 'DISCONNECTED',
                        [QRCodeData]           NVARCHAR(MAX) NULL,
                        [ConnectedPhoneNumber] NVARCHAR(50) NULL,
                        [PushName]             NVARCHAR(100) NULL,
                        [IsHealthy]            BIT NOT NULL DEFAULT 0,
                        [LastPingAt]           DATETIME2 NULL,
                        [LastStatusChangeAt]   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [CreatedAt]            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        [UpdatedAt]            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                    );
                END";

            await conn.ExecuteAsync(ddl);
        }

        public async Task<bool> RetryMessageAsync(int messageId)
        {
            try
            {
                using var conn = CreateConnection();
                var rows = await conn.ExecuteAsync(@"
                    UPDATE [dentist].[WhatsAppMessages]
                    SET [Status] = 'Pending',
                        [RetryCount] = 0,
                        [ScheduledFor] = SYSUTCDATETIME(),
                        [ErrorMessage] = NULL,
                        [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [MessageID] = @MessageID",
                    new { MessageID = messageId });

                _logger.LogInformation("WhatsApp message #{MessageId} reset to Pending state for retry.", messageId);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset WhatsApp message #{MessageId} for retry.", messageId);
                return false;
            }
        }

        public async Task<int> RetryAllFailedMessagesAsync()
        {
            try
            {
                using var conn = CreateConnection();
                var count = await conn.ExecuteAsync(@"
                    UPDATE [dentist].[WhatsAppMessages]
                    SET [Status] = 'Pending',
                        [RetryCount] = 0,
                        [ScheduledFor] = SYSUTCDATETIME(),
                        [ErrorMessage] = NULL,
                        [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [Status] = 'Failed' AND DATEDIFF(day, [CreatedAt], SYSUTCDATETIME()) <= 2");

                _logger.LogInformation("Reset {Count} failed WhatsApp message(s) to Pending state for retry.", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset all failed WhatsApp messages for retry.");
                return 0;
            }
        }
    }
}

