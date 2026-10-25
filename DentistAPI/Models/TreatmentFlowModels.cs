using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    public class Visit
    {
        public long VisitID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int? AppointmentID { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = "checked_in"; // 'checked_in', 'done', 'cancelled'
        public DateTime CreatedAt { get; set; }
    }

    public class Charge
    {
        public long ChargeID { get; set; }
        public int PatientID { get; set; }
        public long VisitID { get; set; }
        public string ChargeType { get; set; } = "consultation"; // 'consultation', 'xray'
        public int? RadiographID { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public long? InvoiceID { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TreatmentItem
    {
        public long TreatmentItemID { get; set; }
        public long Id => TreatmentItemID; // JSON alias

        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public int ToothNo => ToothNumber; // JSON alias

        public string ToothKey { get; set; } = "1";
        public string ProcedureCode { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string Name => ProcedureName; // JSON alias

        public decimal Fee { get; set; }
        public string Currency { get; set; } = "PKR";
        public string Status { get; set; } = "proposed"; // 'draft', 'proposed', 'accepted', 'in_progress', 'completed', 'declined', 'cancelled'
        public long? PlannedVisitID { get; set; }
        public int? AppointmentID { get; set; }
        public DateTime? StartedAt { get; set; }
        public long? CompletedVisitID { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? InvoiceID { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public decimal AdvancePaidAmount { get; set; } = 0;
        public DateTime? ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- NEW ARCHITECTURE EXTENSIONS ---
        public int? CatalogID { get; set; }
        public string? Category { get; set; } // consultation, whitening, ortho, endo, restorative, surgery, hygiene, general
        public string? Surface { get; set; }
        public string Source { get; set; } = "system"; // chart_save, manual, voice, directory, system
        public string? SourceRef { get; set; }
        public string? DedupeKey { get; set; }
        public int? EstimatedPrice { get; set; }
        public int Discount { get; set; } = 0;
        public int? FinalPrice { get; set; }
        public string? PriceNote { get; set; }
        public int? PlannedVisits { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? CompletedBy { get; set; }
        public string? ClosedReason { get; set; }

        // --- COMPUTED FINANCIAL ATTRIBUTES (Never stored on item row) ---
        public int Paid { get; set; } = 0;
        public int Balance { get; set; } = 0;
        public string PaymentStatus { get; set; } = "unpaid"; // unpaid, partial, paid, overpaid
        public List<PaymentAllocationDetailDto> Allocations { get; set; } = new();
        public List<TreatmentItemAppointmentDto> Appointments { get; set; } = new();
    }

    public class TreatmentItemAppointment
    {
        public long ID { get; set; }
        public long TreatmentItemID { get; set; }
        public int? AppointmentID { get; set; }
        public long? VisitID { get; set; }
        public string Role { get; set; } = "planned"; // 'planned', 'performed'
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TreatmentItemAppointmentDto
    {
        public long ID { get; set; }
        public long TreatmentItemID { get; set; }
        public int? AppointmentID { get; set; }
        public long? VisitID { get; set; }
        public string Role { get; set; } = "planned";
        public string? Notes { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? DoctorName { get; set; }
    }

    public class PaymentAllocation
    {
        public long ID { get; set; }
        public long PaymentID { get; set; }
        public long TreatmentItemID { get; set; }
        public int Amount { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentAllocationDetailDto
    {
        public long AllocationID { get; set; }
        public long PaymentID { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string PaymentMethod { get; set; } = "cash";
        public DateTime PaymentDate { get; set; }
        public string? ReceivedByName { get; set; }
        public int? AppointmentID { get; set; }
        public long? VisitID { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentDetailDto
    {
        public long PaymentID { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public int PatientID { get; set; }
        public int Amount { get; set; }
        public string Method { get; set; } = "cash";
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "active";
        public string? PaymentStatus { get; set; }
        public string? VoidReason { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string ReceivedByName { get; set; } = "Front Desk";
        public int AllocatedAmount { get; set; }
        public int UnallocatedAmount { get; set; }
    }

    public class TreatmentCatalogItem
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ProcedureCode { get; set; }
        public int DefaultPrice { get; set; }
        public int? DefaultVisits { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTreatmentItemRequest
    {
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public int? CatalogId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? ProcedureCode { get; set; }
        public int? ToothNo { get; set; }
        public string? Surface { get; set; }
        public string Source { get; set; } = "manual"; // chart_save, manual, voice, directory, system
        public string? SourceRef { get; set; }
        public int? EstimatedPrice { get; set; }
        public int Discount { get; set; } = 0;
        public int? FinalPrice { get; set; }
        public string? PriceNote { get; set; }
        public int? PlannedVisits { get; set; }
        public int? CreatedBy { get; set; }
        public string Status { get; set; } = "proposed"; // 'draft' for voice, 'proposed' for chart/manual
    }

    public class UpdateTreatmentItemRequest
    {
        public string? Status { get; set; }
        public int? FinalPrice { get; set; }
        public string? PriceNote { get; set; }
        public string? ClosedReason { get; set; }
        public string? TransitionNotes { get; set; }
        public int UserId { get; set; } = 1;
        public string UserRole { get; set; } = "Doctor"; // Doctor, Receptionist, Admin
    }

    public class RecordFlexiblePaymentRequest
    {
        public int PatientId { get; set; }
        public long Amount { get; set; }
        public string Method { get; set; } = "cash"; // cash, card, bank, easypaisa, jazzcash
        public int? AppointmentId { get; set; }
        public long? VisitId { get; set; }
        public int ReceivedBy { get; set; } = 1;
        public string? Notes { get; set; }
        public List<PaymentAllocationRequestItem>? Allocations { get; set; }
    }

    public class PaymentAllocationRequestItem
    {
        public long TreatmentItemId { get; set; }
        public long Amount { get; set; }
    }

    public class AllocateCreditRequest
    {
        public int PatientId { get; set; }
        public int UserId { get; set; } = 1;
        public List<PaymentAllocationRequestItem> Allocations { get; set; } = new();
    }

    public class VoidPaymentRequest
    {
        public long PaymentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int UserId { get; set; } = 1;
        public string UserRole { get; set; } = "Admin";
    }

    public class LinkAppointmentRequest
    {
        public int? AppointmentId { get; set; }
        public long? VisitId { get; set; }
        public string Role { get; set; } = "planned"; // planned | performed
        public string? Notes { get; set; }
    }

    public class AccountSummaryDto
    {
        public int CompletedWorkValue { get; set; }
        public int TotalPaid { get; set; }
        public int OutstandingNow { get; set; }
        public int AdvanceHeld { get; set; }
        public int PlannedValue { get; set; }
        public int UnallocatedCredit { get; set; }
        public string Currency { get; set; } = "PKR";
    }

    public class ActivePlanChipDto
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProgressText { get; set; } = string.Empty;
        public int Count { get; set; }
        public int? BalanceDue { get; set; }
        public int? AdvanceCredit { get; set; }
    }

    public class UnplannedFindingDto
    {
        public int ToothNumber { get; set; }
        public string ToothKey { get; set; } = string.Empty;
        public string ConditionStatus { get; set; } = string.Empty;
        public string ConditionColor { get; set; } = "#EF4444";
        public string? Comments { get; set; }
        public string SuggestedProcedureCode { get; set; } = "D2391";
        public string SuggestedProcedureName { get; set; } = "Restorative Filling";
        public int EstimatedPrice { get; set; } = 10000;
        public int? CatalogId { get; set; }
    }

    public class AuditLogRecord
    {
        public long AuditLogID { get; set; }
        public string Entity { get; set; } = string.Empty;
        public long EntityID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public int UserID { get; set; }
        public string UserRole { get; set; } = "Doctor";
        public string? IPAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CheckinPatientRequest
    {
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
    }

    public class RecordObservationRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public List<ToothObservationItem> Observations { get; set; } = new();
    }

    public class ToothObservationItem
    {
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string ProcedureCode { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public decimal? FeeOverride { get; set; }
        public string? Notes { get; set; }
    }

    public class AdvancePaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "cash";
        public int ReceivedByUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class CancelTreatmentItemRequest
    {
        public int UserId { get; set; }
        public string? Reason { get; set; }
    }

    public class GenerateUnifiedInvoiceRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public List<long> TreatmentItemIds { get; set; } = new();
        public List<long> ChargeIds { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class BillingSummaryDto
    {
        public decimal TotalInvoiced { get; set; }
        public decimal TotalSettled { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int PlannedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int InvoicedCount { get; set; }
        public decimal TotalPlannedCareValue { get; set; }
        public decimal TotalAdvanceReceived { get; set; }
        public string Currency { get; set; } = "PKR";
    }

    // ==========================================
    // 🛡️ REUSABLE TREATMENT POLICY PERMISSION MODELS
    // ==========================================

    public static class TreatmentPolicyErrorCodes
    {
        public const string DoctorLocked = "DOCTOR_LOCKED";
        public const string ActiveTreatmentExists = "ACTIVE_TREATMENT_EXISTS";
        public const string ChartLocked = "CHART_LOCKED";
        public const string TreatmentLocked = "TREATMENT_LOCKED";
        public const string InvalidState = "INVALID_STATE";
    }

    public class PolicyResult
    {
        public bool Allowed { get; set; } = true;
        public string? ErrorCode { get; set; }
        public string? ReasonCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageUrdu { get; set; }
        public int? ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? TreatmentName { get; set; }
        public int? ActiveDoctorId { get; set; }
        public string? ActiveDoctorName { get; set; }
        public int NetPaidAmount { get; set; } = 0;
        public long? ActiveTreatmentItemId { get; set; }

        public static PolicyResult Allow(string message = "Action permitted.") => new() { Allowed = true, Message = message };

        public static PolicyResult Block(
            string errorCode,
            string message,
            string? messageUrdu = null,
            int? toothNumber = null,
            string? toothKey = null,
            string? treatmentName = null,
            int? activeDoctorId = null,
            string? activeDoctorName = null,
            int netPaidAmount = 0,
            long? activeTreatmentItemId = null,
            string? reasonCode = null)
        {
            return new PolicyResult
            {
                Allowed = false,
                ErrorCode = errorCode,
                ReasonCode = reasonCode ?? errorCode,
                Message = message,
                MessageUrdu = messageUrdu,
                ToothNumber = toothNumber,
                ToothKey = toothKey,
                TreatmentName = treatmentName,
                ActiveDoctorId = activeDoctorId,
                ActiveDoctorName = activeDoctorName,
                NetPaidAmount = netPaidAmount,
                ActiveTreatmentItemId = activeTreatmentItemId
            };
        }
    }

    public class TreatmentPolicyException : Exception
    {
        public PolicyResult Result { get; }
        public TreatmentPolicyException(PolicyResult result) : base(result.Message)
        {
            Result = result;
        }
    }

    public class PatientTreatmentPolicyDto
    {
        public int PatientId { get; set; }
        public bool HasActiveTreatment { get; set; }
        public long? ActiveTreatmentItemId { get; set; }
        public int? ActiveToothNumber { get; set; }
        public string? ActiveToothKey { get; set; }
        public string? ActiveTreatmentName { get; set; }
        public int? ActiveDoctorId { get; set; }
        public string? ActiveDoctorName { get; set; }
        public int ActiveNetPaid { get; set; }
        public bool CanChangeDoctor { get; set; } = true;
        public string? DoctorLockReason { get; set; }
        public string? DoctorLockReasonUrdu { get; set; }
        public bool CanStartNewTreatment { get; set; } = true;
        public string? StartLockReason { get; set; }
        public string? StartLockReasonUrdu { get; set; }
        public List<int> LockedToothNumbers { get; set; } = new();
        public List<string> LockedToothKeys { get; set; } = new();
        public int TotalOutstandingBalance { get; set; }
    }

    public class AutoDiagnosticFeeRequest
    {
        public string FeeType { get; set; } = "consultation"; // "consultation" | "rvg_xray" | "opg_xray"
        public int? ToothNumber { get; set; }
        public int? DoctorId { get; set; }
        public string? SourceRef { get; set; }
        public string? Notes { get; set; }
    }

    public class AutoDiagnosticFeeResult
    {
        public bool Added { get; set; }
        public int Fee { get; set; }
        public string? Name { get; set; }
        public string? ProcedureCode { get; set; }
        public long? ItemId { get; set; }
        public string? Message { get; set; }
    }

    public class PatientModalitySyncResult
    {
        public int PatientId { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string? TreatmentStage { get; set; }
        public string? TargetShade { get; set; }
        public bool ModalityReset { get; set; }
        public int ActiveItemsCount { get; set; }
        public string? Message { get; set; }
    }

    public class PatientModalitiesStatusDto
    {
        public int PatientId { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string? TreatmentStage { get; set; }
        public string? TargetShade { get; set; }
        public ModalityDetailDto Braces { get; set; } = new();
        public ModalityDetailDto Whitening { get; set; } = new();
        public ModalityDetailDto Cavity { get; set; } = new();
    }

    public class ModalityDetailDto
    {
        public bool Active { get; set; }
        public string Status { get; set; } = "none"; // "active", "in_progress", "planned", "completed", "none"
        public string Title { get; set; } = string.Empty;
        public string? Stage { get; set; }
        public string? Specification { get; set; } // Wire or Shade or Material
        public int PlannedCount { get; set; }
        public List<string> AffectedTeeth { get; set; } = new();
        public long? TreatmentItemId { get; set; }
        public int? TotalFee { get; set; }
        public int? PaidAmount { get; set; }
    }
}

