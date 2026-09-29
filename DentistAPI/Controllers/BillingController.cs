using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/billing")]
    public class BillingController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<BillingController> _logger;

        public BillingController(
            DentalRepository repository, 
            ITokenService tokenService,
            ILogger<BillingController> logger)
        {
            _repository = repository;
            _tokenService = tokenService;
            _logger = logger;
        }

        private int? GetAuthenticatedPatientId()
        {
            string? authHeader = Request.Headers["Authorization"].ToString();
            string? token = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7).Trim();
            }
            else if (Request.Headers.TryGetValue("X-Patient-Token", out var customToken))
            {
                token = customToken.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(token) || !_tokenService.ValidatePatientToken(token, out var payload) || payload == null)
            {
                return null;
            }

            return payload.PatientId;
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var invoices = await _repository.GetPatientInvoicesAsync(patientId.Value);
            return Ok(invoices);
        }

        [HttpGet("invoices/{id}")]
        public async Task<IActionResult> GetInvoice(long id)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var invoice = await _repository.GetInvoiceDetailsAsync(id, patientId.Value);
            if (invoice == null) return NotFound(new { message = "Invoice not found or access denied." });

            return Ok(invoice);
        }

        [HttpPost("pay-online")]
        public async Task<IActionResult> PayOnline([FromBody] OnlinePaymentRequest request)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            if (request == null || request.InvoiceID <= 0 || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid payment parameters." });
            }

            var invoice = await _repository.GetInvoiceDetailsAsync(request.InvoiceID, patientId.Value);
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found or you do not have permission to pay it." });
            }

            if (invoice.Status == "Paid")
            {
                return BadRequest(new { message = "This invoice has already been fully paid." });
            }

            // Simulate Gateway transaction (Stripe/Card provider)
            string chargeId = $"ch_card_{Guid.NewGuid():N}".Substring(0, 24);
            string notes = $"Online card payment by {request.CardHolderName} (Card ending in {request.CardLast4})";

            var (receiptNo, newStatus) = await _repository.ProcessPaymentStoredProcAsync(
                request.InvoiceID,
                patientId.Value,
                request.Amount,
                "Online_Card",
                chargeId,
                "Stripe",
                null,
                notes
            );

            await _repository.LogPatientPortalActivityAsync(
                patientId.Value,
                "PAY_ONLINE",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                $"Paid ${request.Amount:F2} online for invoice {invoice.InvoiceNumber}. Receipt #{receiptNo}"
            );

            return Ok(new
            {
                success = true,
                receiptNumber = receiptNo,
                invoiceNumber = invoice.InvoiceNumber,
                amountPaid = request.Amount,
                invoiceStatus = newStatus,
                transactionReference = chargeId,
                paymentDate = DateTime.UtcNow,
                message = $"Payment of ${request.Amount:F2} processed successfully! Receipt #{receiptNo} generated."
            });
        }

        [HttpPost("generate-cash-voucher")]
        public async Task<IActionResult> GenerateCashVoucher([FromBody] CashVoucherRequest request)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            if (request == null || request.InvoiceID <= 0 || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid voucher request parameters." });
            }

            var invoice = await _repository.GetInvoiceDetailsAsync(request.InvoiceID, patientId.Value);
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found." });
            }

            var (receiptNo, newStatus) = await _repository.ProcessPaymentStoredProcAsync(
                request.InvoiceID,
                patientId.Value,
                request.Amount,
                "Cash",
                null,
                "ClinicCashDesk",
                null,
                request.Notes ?? "Cash payment voucher generated in Patient Portal"
            );

            // Voucher code matches receipt or CSH pattern
            string voucherCode = receiptNo.Replace("REC-", "CSH-");

            await _repository.LogPatientPortalActivityAsync(
                patientId.Value,
                "GENERATE_CASH_VOUCHER",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                $"Generated cash voucher {voucherCode} for invoice {invoice.InvoiceNumber}"
            );

            return Ok(new
            {
                success = true,
                voucherCode = voucherCode,
                invoiceNumber = invoice.InvoiceNumber,
                amountDue = request.Amount,
                invoiceStatus = newStatus,
                instructions = "Please present this voucher code or QR code at the Dentia Clinic front desk when paying with cash.",
                message = $"Cash voucher {voucherCode} generated successfully. Please present to clinic reception."
            });
        }

        [HttpPost("confirm-cash")]
        public async Task<IActionResult> ConfirmCashPayment([FromBody] ConfirmCashRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CashVoucherCode))
            {
                return BadRequest(new { message = "Cash voucher code is required." });
            }

            try
            {
                var result = await _repository.ConfirmCashPaymentStoredProcAsync(
                    request.CashVoucherCode.Trim(),
                    request.StaffDoctorID > 0 ? request.StaffDoctorID : 1,
                    request.ConfirmedAmount
                );

                if (result == null)
                {
                    return NotFound(new { message = "Pending cash voucher not found or already confirmed." });
                }

                return Ok(new
                {
                    success = true,
                    voucherCode = request.CashVoucherCode,
                    message = "Cash payment confirmed and invoice status updated to Paid."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming cash payment");
                string safeMsg = (ex is InvalidOperationException || ex is ArgumentException) 
                    ? ex.Message 
                    : "An error occurred while confirming the cash payment. Please try again.";
                return BadRequest(new { message = safeMsg });
            }
        }

        [HttpGet("payments")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var payments = await _repository.GetPatientPaymentsAsync(patientId.Value);
            return Ok(payments);
        }

        [HttpGet("patient/{patientId}/treatment-report")]
        public async Task<IActionResult> GetPatientTreatmentReport(int patientId)
        {
            if (patientId <= 0)
            {
                return BadRequest(new { message = "Valid patient ID is required." });
            }

            var report = await _repository.GetPatientTreatmentReportAsync(patientId);
            if (report == null)
            {
                return NotFound(new { message = $"Patient with ID #{patientId} was not found." });
            }

            return Ok(report);
        }

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoiceFromTreatments([FromBody] CreateTreatmentInvoiceRequest request)
        {
            if (request == null || request.PatientId <= 0 || request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { message = "Invalid request. At least one treatment item is required." });
            }

            try
            {
                var invoice = await _repository.CreateInvoiceFromTreatmentsAsync(request);
                return Ok(new
                {
                    success = true,
                    invoice,
                    message = $"Invoice {invoice.InvoiceNumber} created successfully for {request.Items.Count} treatment items!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice from treatments");
                string safeMsg = (ex is InvalidOperationException || ex is ArgumentException) 
                    ? ex.Message 
                    : "An error occurred while creating the invoice. Please try again.";
                return BadRequest(new { message = safeMsg });
            }
        }

        [HttpPost("invoices/{id}/record-payment")]
        public async Task<IActionResult> RecordPayment(long id, [FromBody] RecordPaymentRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Valid invoice ID is required." });
            }

            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new { message = "Payment amount must be greater than zero." });
            }

            var (success, message, receiptNo, newBalance, newStatus) = await _repository.RecordInvoicePaymentAsync(
                id,
                request.Amount,
                request.PaymentMethod ?? "Cash_Counter",
                request.Notes,
                request.DoctorId
            );

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new
            {
                success = true,
                receiptNumber = receiptNo,
                balanceAmount = newBalance,
                status = newStatus,
                message
            });
        }
    }
}

