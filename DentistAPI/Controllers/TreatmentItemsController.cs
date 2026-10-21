using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentistAPI.Repositories;
using DentistAPI.Models;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class TreatmentItemsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ILogger<TreatmentItemsController> _logger;

        public TreatmentItemsController(DentalRepository repository, ILogger<TreatmentItemsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // ==========================================
        // 1. PATIENT CHECK-IN & AUTO CONSULTATION FEE
        // ==========================================
        [HttpPost("patients/{id:int}/checkin")]
        public async Task<IActionResult> CheckinPatient(int id, [FromBody] CheckinPatientRequest? request)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            int doctorId = request?.DoctorId ?? 1;
            int? apptId = request?.AppointmentId;

            try
            {
                var (visit, charge, alreadyCheckedIn) = await _repository.CheckinPatientAsync(id, doctorId, apptId);
                return Ok(new
                {
                    success = true,
                    alreadyCheckedIn,
                    visit,
                    charge,
                    message = alreadyCheckedIn 
                        ? "Patient was already checked in today." 
                        : $"Patient successfully checked in! Consultation fee of {charge.Currency} {charge.Amount:N2} added to ledger."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 2. RECORD X-RAY CHARGE AUTOMATICALLY
        // ==========================================
        [HttpPost("patients/{id:int}/xrays/charge")]
        public async Task<IActionResult> ChargeXray(int id, [FromQuery] int? doctorId, [FromQuery] int? radiographId)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var charge = await _repository.CreateXrayChargeAsync(id, doctorId ?? 1, radiographId);
                return Ok(new
                {
                    success = true,
                    charge,
                    message = $"X-Ray fee of {charge.Currency} {charge.Amount:N2} added to patient unbilled charges."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording X-ray charge for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 3. RECORD OBSERVATIONS -> TREATMENT ITEMS + AUTO APPOINTMENT
        // ==========================================
        [HttpPost("patients/{id:int}/observations")]
        public async Task<IActionResult> RecordObservations(int id, [FromBody] RecordObservationRequest request)
        {
            if (id <= 0 || request == null || request.Observations == null || !request.Observations.Any())
            {
                return BadRequest(new { message = "Valid patient ID and at least one observation is required." });
            }

            request.PatientId = id;
            if (request.DoctorId <= 0) request.DoctorId = 1;

            try
            {
                var (createdItems, autoAppointment) = await _repository.SaveObservationsAsync(request);

                string apptMsg = autoAppointment != null 
                    ? $"Next follow-up appointment automatically scheduled for {autoAppointment.PreferredDate:dd MMM yyyy, hh:mm tt}." 
                    : "Linked to existing scheduled appointment.";

                return Ok(new
                {
                    success = true,
                    count = createdItems.Count,
                    createdItems,
                    autoAppointment,
                    message = $"{createdItems.Count} treatments added to plan. {apptMsg}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving observations for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 4. GET TREATMENT ITEMS (FILTERED BY STATUSES)
        // ==========================================
        [HttpGet("patients/{id:int}/treatment-items")]
        public async Task<IActionResult> GetTreatmentItems(int id, [FromQuery] string? status)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            string[]? statuses = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                statuses = status.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim().ToLower())
                                 .ToArray();
            }

            try
            {
                var items = await _repository.GetTreatmentItemsAsync(id, statuses);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving treatment items for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 5. GET UNBILLED CHARGES (CONSULTATION, X-RAY)
        // ==========================================
        [HttpGet("patients/{id:int}/charges/unbilled")]
        public async Task<IActionResult> GetUnbilledCharges(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var charges = await _repository.GetUnbilledChargesAsync(id);
                return Ok(charges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unbilled charges for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 6. START TREATMENT: planned -> in_progress
        // ==========================================
        [HttpPatch("treatment-items/{id:long}/start")]
        public async Task<IActionResult> StartTreatmentItem(long id, [FromQuery] int? userId)
        {
            if (id <= 0) return BadRequest(new { message = "Valid treatment item ID is required." });

            try
            {
                bool success = await _repository.StartTreatmentItemAsync(id, userId ?? 1);
                return Ok(new
                {
                    success,
                    treatmentItemId = id,
                    status = "in_progress",
                    message = "Treatment marked as in_progress."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting treatment item {TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 7. COMPLETE TREATMENT: in_progress -> completed
        // ==========================================
        [HttpPatch("treatment-items/{id:long}/complete")]
        public async Task<IActionResult> CompleteTreatmentItem(long id, [FromQuery] int? userId)
        {
            if (id <= 0) return BadRequest(new { message = "Valid treatment item ID is required." });

            try
            {
                bool success = await _repository.CompleteTreatmentItemAsync(id, userId ?? 1);
                return Ok(new
                {
                    success,
                    treatmentItemId = id,
                    status = "completed",
                    message = "Treatment completed successfully! It has been moved to Treatment & Invoices tab."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing treatment item {TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 8. CANCEL TREATMENT: planned/in_progress -> cancelled
        // ==========================================
        [HttpPatch("treatment-items/{id:long}/cancel")]
        public async Task<IActionResult> CancelTreatmentItem(long id, [FromBody] CancelTreatmentItemRequest? request)
        {
            if (id <= 0) return BadRequest(new { message = "Valid treatment item ID is required." });

            try
            {
                bool success = await _repository.CancelTreatmentItemAsync(id, request?.UserId ?? 1, request?.Reason);
                return Ok(new
                {
                    success,
                    treatmentItemId = id,
                    status = "cancelled",
                    message = "Treatment has been cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling treatment item {TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 9. RECORD ADVANCE PAYMENT AGAINST IN-PROGRESS ITEM
        // ==========================================
        [HttpPost("treatment-items/{id:long}/payments")]
        public async Task<IActionResult> RecordAdvancePayment(long id, [FromBody] AdvancePaymentRequest request)
        {
            if (id <= 0 || request == null || request.Amount <= 0)
            {
                return BadRequest(new { message = "Valid treatment item ID and payment amount > 0 are required." });
            }

            try
            {
                string receiptNo = await _repository.RecordAdvancePaymentAsync(id, request);
                return Ok(new
                {
                    success = true,
                    receiptNumber = receiptNo,
                    amount = request.Amount,
                    message = $"Advance payment of Rs {request.Amount:N2} recorded successfully! Receipt #{receiptNo}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording advance payment for item {TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 10. GENERATE UNIFIED MASTER INVOICE
        // ==========================================
        [HttpPost("patients/{id:int}/invoices")]
        public async Task<IActionResult> GenerateUnifiedInvoice(int id, [FromBody] GenerateUnifiedInvoiceRequest request)
        {
            if (id <= 0 || request == null) return BadRequest(new { message = "Valid request payload is required." });

            request.PatientId = id;
            if (request.DoctorId <= 0) request.DoctorId = 1;

            try
            {
                var (invoiceNo, total, advancesApplied, balance) = await _repository.GenerateUnifiedInvoiceAsync(request);
                return Ok(new
                {
                    success = true,
                    invoiceNumber = invoiceNo,
                    totalAmount = total,
                    advancesApplied,
                    balanceAmount = balance,
                    status = balance <= 0 ? "Paid" : (advancesApplied > 0 ? "Partially Paid" : "Unpaid"),
                    message = $"Master Invoice {invoiceNo} generated successfully! Balance due: Rs {balance:N2}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating master invoice for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 11. BILLING SUMMARY (4 CARDS + COUNTERS)
        // ==========================================
        [HttpGet("patients/{id:int}/billing-summary")]
        public async Task<IActionResult> GetBillingSummary(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var summary = await _repository.GetPatientBillingSummaryAsync(id);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing summary for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
