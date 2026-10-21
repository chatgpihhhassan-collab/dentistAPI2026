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
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public string ToothKey { get; set; } = "1";
        public string ProcedureCode { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Currency { get; set; } = "PKR";
        public string Status { get; set; } = "planned"; // 'planned', 'in_progress', 'completed', 'invoiced', 'cancelled'
        public long? PlannedVisitID { get; set; }
        public int? AppointmentID { get; set; }
        public DateTime? StartedAt { get; set; }
        public long? CompletedVisitID { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? InvoiceID { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public decimal AdvancePaidAmount { get; set; } = 0; // Aggregated sum of kind = 'advance' payments
        public DateTime? ScheduledDate { get; set; } // Joined from Appointments
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
        public string PaymentMethod { get; set; } = "cash"; // 'cash', 'card', 'online'
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
}
