using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class TreatmentItemsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITreatmentPlanService _treatmentPlanService;
        private readonly ILogger<TreatmentItemsController> _logger;

        public TreatmentItemsController(
            DentalRepository repository, 
            ITreatmentPlanService treatmentPlanService,
            ILogger<TreatmentItemsController> logger)
        {
            _repository = repository;
            _treatmentPlanService = treatmentPlanService;
            _logger = logger;
        }

        // ==========================================
        // 1. GET TREATMENT ITEMS (Plan view vs Invoices view)
        // ==========================================
        [HttpGet("patients/{id:int}/treatment-items")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetTreatmentItems(int id, [FromQuery] string? view, [FromQuery] string? status)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                // If specific view requested (plan or invoices)
                if (!string.IsNullOrWhiteSpace(view))
                {
                    var items = await _treatmentPlanService.GetTreatmentItemsAsync(id, view);
                    return Ok(items);
                }

                // If status filter provided
                if (!string.IsNullOrWhiteSpace(status))
                {
                    string[] statuses = status.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim().ToLower())
                                             .ToArray();
                    var items = await _repository.GetTreatmentItemsAsync(id, statuses);
                    return Ok(items);
                }

                // Default: return plan view items
                var defaultItems = await _treatmentPlanService.GetTreatmentItemsAsync(id, "plan");
                return Ok(defaultItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving treatment items for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 2. CREATE TREATMENT ITEM (Manual, Voice, Directory)
        // ==========================================
        [HttpPost("patients/{id:int}/treatment-items")]
        public async Task<IActionResult> CreateTreatmentItem(int id, [FromBody] CreateTreatmentItemRequest request)
        {
            if (id <= 0 || request == null)
            {
                return BadRequest(new { message = "Valid patient ID and request body are required." });
            }

            // Zero or negative fee validation
            if ((request.FinalPrice.HasValue && request.FinalPrice.Value <= 0) ||
                (request.EstimatedPrice.HasValue && request.EstimatedPrice.Value <= 0))
            {
                return BadRequest(new { message = "Procedure fee must be greater than zero. Procedures cannot be added with a zero or negative fee." });
            }

            request.PatientId = id;
            try
            {
                var item = await _treatmentPlanService.CreateTreatmentItemAsync(request);
                return Ok(new
                {
                    success = true,
                    item,
                    message = $"Treatment '{item.ProcedureName}' added to plan successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating treatment item for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 3. UPDATE TREATMENT ITEM (Status Transitions & Price Updates)
        // ==========================================
        [HttpPatch("treatment-items/{id:long}")]
        public async Task<IActionResult> UpdateTreatmentItem(long id, [FromBody] UpdateTreatmentItemRequest request)
        {
            if (id <= 0 || request == null)
            {
                return BadRequest(new { message = "Valid treatment item ID and payload are required." });
            }

            if (request.FinalPrice.HasValue && request.FinalPrice.Value <= 0)
            {
                return BadRequest(new { message = "Procedure fee must be greater than zero. Zero or negative fee is not allowed." });
            }

            try
            {
                bool success = await _treatmentPlanService.UpdateTreatmentItemStatusAsync(id, request);
                if (!success) return NotFound(new { message = $"Treatment item #{id} not found." });

                var updatedItem = await _treatmentPlanService.GetTreatmentItemByIdAsync(id);
                return Ok(new
                {
                    success = true,
                    item = updatedItem,
                    message = $"Treatment updated successfully (Status: {updatedItem?.Status})."
                });
            }
            catch (UnauthorizedAccessException uex)
            {
                return StatusCode(403, new { message = uex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating treatment item #{TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 4. LINK APPOINTMENT TO TREATMENT ITEM
        // ==========================================
        [HttpPost("treatment-items/{id:long}/appointments")]
        public async Task<IActionResult> LinkAppointment(long id, [FromBody] LinkAppointmentRequest request)
        {
            if (id <= 0 || request == null)
            {
                return BadRequest(new { message = "Valid treatment item ID and request body are required." });
            }

            try
            {
                bool success = await _treatmentPlanService.LinkAppointmentAsync(id, request);
                return Ok(new
                {
                    success,
                    message = "Appointment linked to treatment item successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking appointment to treatment item #{TreatmentItemId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 5. UNPLANNED FINDINGS FROM TEETHSTATE
        // ==========================================
        [HttpGet("patients/{id:int}/findings/unplanned")]
        public async Task<IActionResult> GetUnplannedFindings(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var findings = await _treatmentPlanService.GetUnplannedFindingsAsync(id);
                return Ok(findings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unplanned findings for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 6. RECORD OBSERVATIONS -> TREATMENT ITEMS (Idempotent)
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
                // Save clinical observations into TreatmentItems via shared service
                var createdList = new List<TreatmentItem>();
                foreach (var obs in request.Observations)
                {
                    var item = await _treatmentPlanService.CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                    {
                        PatientId = id,
                        DoctorId = request.DoctorId,
                        Name = !string.IsNullOrWhiteSpace(obs.ProcedureName) ? obs.ProcedureName : "Restorative Care",
                        ProcedureCode = obs.ProcedureCode,
                        ToothNo = obs.ToothNumber,
                        Source = "chart_save",
                        FinalPrice = obs.FeeOverride.HasValue ? (int)obs.FeeOverride.Value : null,
                        Status = "proposed",
                        PriceNote = obs.Notes
                    });
                    createdList.Add(item);
                }

                // Also call existing repository method for auto appointment schedule if appropriate
                Appointment? autoAppointment = null;
                try
                {
                    var (_, appt) = await _repository.SaveObservationsAsync(request);
                    autoAppointment = appt;
                }
                catch (Exception repEx)
                {
                    _logger.LogWarning(repEx, "Observation auto-appointment notice");
                }

                string apptMsg = autoAppointment != null 
                    ? $" Next appointment: {autoAppointment.PreferredDate:dd MMM yyyy, hh:mm tt}." 
                    : "";

                return Ok(new
                {
                    success = true,
                    count = createdList.Count,
                    createdItems = createdList,
                    autoAppointment,
                    message = $"{createdList.Count} treatments synchronized to plan.{apptMsg}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving observations for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 7. RECORD FLEXIBLE PAYMENT WITH ALLOCATIONS
        // ==========================================
        [HttpPost("patients/{id:int}/payments")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordFlexiblePaymentRequest request)
        {
            if (id <= 0 || request == null)
            {
                return BadRequest(new { message = "Valid patient ID and payment request are required." });
            }

            request.PatientId = id;
            try
            {
                var (success, message, receiptNo) = await _treatmentPlanService.RecordFlexiblePaymentAsync(request);
                if (!success) return BadRequest(new { message });

                var summary = await _treatmentPlanService.GetAccountSummaryAsync(id);
                return Ok(new
                {
                    success = true,
                    receiptNumber = receiptNo,
                    message,
                    summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 8. ALLOCATE UNALLOCATED CREDIT
        // ==========================================
        [HttpPost("payments/allocations")]
        public async Task<IActionResult> AllocateCredit([FromBody] AllocateCreditRequest request)
        {
            if (request == null || request.PatientId <= 0 || request.Allocations == null || !request.Allocations.Any())
            {
                return BadRequest(new { message = "Valid patient ID and allocations list required." });
            }

            try
            {
                var (success, message) = await _treatmentPlanService.AllocateCreditAsync(request);
                if (!success) return BadRequest(new { message });

                var summary = await _treatmentPlanService.GetAccountSummaryAsync(request.PatientId);
                return Ok(new
                {
                    success = true,
                    message,
                    summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating credit for patient {PatientId}", request.PatientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 9. VOID PAYMENT (Clinicians & Admins, mandatory reason)
        // ==========================================
        [HttpPost("payments/void")]
        public async Task<IActionResult> VoidPayment([FromBody] VoidPaymentRequest request)
        {
            if (request == null || request.PaymentId <= 0)
            {
                return BadRequest(new { message = "Valid payment ID is required." });
            }

            try
            {
                var (success, message) = await _treatmentPlanService.VoidPaymentAsync(request);
                if (!success) return BadRequest(new { message });

                return Ok(new
                {
                    success = true,
                    message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding payment #{PaymentId}", request.PaymentId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 9B. DELETE / VOID PAYMENT BY ID ALIAS
        // ==========================================
        [HttpDelete("patients/{patientId:int}/payments/{paymentId:long}")]
        public async Task<IActionResult> DeletePayment(int patientId, long paymentId, [FromQuery] string? reason)
        {
            if (patientId <= 0 || paymentId <= 0)
            {
                return BadRequest(new { message = "Valid patient ID and payment ID are required." });
            }

            try
            {
                var request = new VoidPaymentRequest
                {
                    PaymentId = paymentId,
                    Reason = !string.IsNullOrWhiteSpace(reason) ? reason : "Payment deleted/voided by clinician",
                    UserId = 1,
                    UserRole = "Doctor"
                };

                var (success, message) = await _treatmentPlanService.VoidPaymentAsync(request);
                if (!success) return BadRequest(new { message });

                var summary = await _treatmentPlanService.GetAccountSummaryAsync(patientId);
                return Ok(new
                {
                    success = true,
                    message,
                    summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment #{PaymentId} for patient {PatientId}", paymentId, patientId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 9C. PATIENT PAYMENTS HISTORY
        // ==========================================
        [HttpGet("patients/{id:int}/payments")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetPatientPayments(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var payments = await _treatmentPlanService.GetPatientPaymentsAsync(id);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 10. PATIENT ACCOUNT SUMMARY
        // ==========================================
        [HttpGet("patients/{id:int}/account-summary")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetAccountSummary(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var summary = await _treatmentPlanService.GetAccountSummaryAsync(id);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account summary for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 11. ACTIVE PLANS FOR HEADER CHIPS
        // ==========================================
        [HttpGet("patients/{id:int}/active-plans")]
        public async Task<IActionResult> GetActivePlans(int id)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                var chips = await _treatmentPlanService.GetActivePlansAsync(id);
                return Ok(chips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active plans for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 12. DIRECTORY MODALITIES AUTO-SYNC
        // ==========================================
        [HttpPost("patients/{id:int}/sync-directory")]
        public async Task<IActionResult> SyncDirectoryModalities(int id, [FromQuery] int? doctorId)
        {
            if (id <= 0) return BadRequest(new { message = "Valid patient ID is required." });

            try
            {
                int count = await _treatmentPlanService.SyncDirectoryModalitiesAsync(id, doctorId ?? 1);
                return Ok(new
                {
                    success = true,
                    syncedCount = count,
                    message = count > 0 
                        ? $"Synchronized {count} modality treatment(s) from directory into active plan."
                        : "Directory modalities are already up to date."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing directory modalities for patient {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 13. PROCEDURES CATALOG
        // ==========================================
        [HttpGet("treatment-catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            try
            {
                var catalog = await _treatmentPlanService.GetCatalogAsync();
                return Ok(catalog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving treatment catalog");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // 14. LEGACY ENDPOINTS PRESERVED FOR FULL COMPATIBILITY
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

        [HttpPatch("treatment-items/{id:long}/start")]
        public async Task<IActionResult> StartTreatmentItem(long id, [FromQuery] int? userId)
        {
            return await UpdateTreatmentItem(id, new UpdateTreatmentItemRequest { Status = "in_progress", UserId = userId ?? 1, UserRole = "Doctor" });
        }

        [HttpPatch("treatment-items/{id:long}/complete")]
        public async Task<IActionResult> CompleteTreatmentItem(long id, [FromQuery] int? userId)
        {
            return await UpdateTreatmentItem(id, new UpdateTreatmentItemRequest { Status = "completed", UserId = userId ?? 1, UserRole = "Doctor" });
        }

        [HttpPatch("treatment-items/{id:long}/cancel")]
        public async Task<IActionResult> CancelTreatmentItem(long id, [FromBody] CancelTreatmentItemRequest? request)
        {
            return await UpdateTreatmentItem(id, new UpdateTreatmentItemRequest { Status = "cancelled", ClosedReason = request?.Reason, UserId = request?.UserId ?? 1 });
        }

        [HttpGet("patients/{id:int}/billing-summary")]
        public async Task<IActionResult> GetBillingSummary(int id)
        {
            return await GetAccountSummary(id);
        }
    }
}
