using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppWebhookController> _logger;
        private readonly string _webhookSecret;

        public WhatsAppWebhookController(
            IWhatsAppService whatsAppService,
            IConfiguration config,
            ILogger<WhatsAppWebhookController> logger)
        {
            _whatsAppService = whatsAppService;
            _config = config;
            _logger = logger;
            _webhookSecret = Environment.GetEnvironmentVariable("OPENWA_WEBHOOK_SECRET") 
                             ?? config["OpenWA:WebhookSecret"] 
                             ?? "dentia_webhook_hmac_secret_554433";
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // Read raw body for HMAC signature verification
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest(new { message = "Empty webhook payload." });
            }

            // Extract signature from headers
            string? signatureHeader = Request.Headers["X-OpenWA-Signature"].ToString();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
            }
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["X-Hub-Signature"].ToString();
            }
            if (string.IsNullOrEmpty(signatureHeader))
            {
                signatureHeader = Request.Headers["x-signature"].ToString();
            }

            // Verify HMAC-SHA256 signature if signature header is provided or secret is set
            if (!string.IsNullOrEmpty(signatureHeader))
            {
                if (!VerifyHmacSignature(rawBody, signatureHeader, _webhookSecret))
                {
                    _logger.LogWarning("Rejecting WhatsApp webhook: Invalid HMAC signature.");
                    return Unauthorized(new { message = "Invalid HMAC signature." });
                }
            }

            try
            {
                var payload = JsonSerializer.Deserialize<OpenWAWebhookPayload>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload != null)
                {
                    bool handled = await _whatsAppService.ProcessIncomingWebhookAsync(payload);
                    return Ok(new { success = handled, receivedEvent = payload.Event });
                }

                return BadRequest(new { message = "Failed to deserialize webhook payload." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process WhatsApp webhook payload.");
                return StatusCode(500, new { message = "Error processing webhook." });
            }
        }

        private static bool VerifyHmacSignature(string rawBody, string signatureHeader, string secret)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signatureHeader))
                return false;

            // Strip "sha256=" prefix if present
            string expectedHex = signatureHeader.Trim();
            if (expectedHex.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                expectedHex = expectedHex.Substring(7);
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            string computedHex = Convert.ToHexString(hash).ToLowerInvariant();

            // Timing-safe comparison to prevent side-channel attacks
            byte[] a = Encoding.UTF8.GetBytes(computedHex);
            byte[] b = Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant());

            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
