using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    public class DentalNoteSession
    {
        public long SessionId { get; set; }
        public long PatientId { get; set; }
        public long DentistId { get; set; }
        public string Status { get; set; } = "draft";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AudioRecording
    {
        public long AudioId { get; set; }
        public long SessionId { get; set; }
        public string StorageUri { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string Sha256Hash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Transcript
    {
        public long TranscriptId { get; set; }
        public long SessionId { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string CleanedText { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DentalNote
    {
        public long NoteId { get; set; }
        public long SessionId { get; set; }
        public long? AudioId { get; set; }
        public long PatientId { get; set; }
        public long DentistId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string ChiefComplaint { get; set; } = string.Empty;
        public string History { get; set; } = string.Empty;
        public string Examination { get; set; } = string.Empty;
        public string Assessment { get; set; } = string.Empty;
        public string TreatmentPerformed { get; set; } = string.Empty;
        public string PostOpAdvice { get; set; } = string.Empty;
        public string FollowUp { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
        public bool IsDeleted { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public long? ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public List<DentalNotePrescription> Prescriptions { get; set; } = new();
        public List<DentalNoteTreatmentPlan> TreatmentPlans { get; set; } = new();
        public List<AIWarning> AIWarnings { get; set; } = new();
    }

    public class DentalNotePrescription
    {
        public long PrescriptionId { get; set; }
        public long NoteId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
    }

    public class DentalNoteTreatmentPlan
    {
        public long PlanId { get; set; }
        public long NoteId { get; set; }
        public int SequenceNo { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string ToothOrSite { get; set; } = string.Empty;
        public string Timing { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class AIWarning
    {
        public long WarningId { get; set; }
        public long NoteId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "warning";
        public bool Resolved { get; set; } = false;
    }

    public class AIAuditLog
    {
        public long AuditId { get; set; }
        public long NoteId { get; set; }
        public long UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ModelName { get; set; }
        public string? PromptVersion { get; set; }
        public string? InputHash { get; set; }
        public string? OutputHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VoiceToothUpdate
    {
        public int ToothNumber { get; set; }
        public string Status { get; set; }
    }

    public class GeminiScribeResult
    {
        public string Status { get; set; } = "complete";
        public System.Collections.Generic.List<string> MissingFields { get; set; } = new();
        public string DoctorPrompt { get; set; } = string.Empty;
        public DentalNote DraftNote { get; set; } = new();
        public System.Collections.Generic.List<VoiceToothUpdate> TeethUpdates { get; set; } = new();
    }

    public class RadiographNoteRequest
    {
        public long PatientId { get; set; }
        public long DentistId { get; set; }
        public int? RadiographId { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string Modality { get; set; } = "Radiograph";
        public string Summary { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? Assessment { get; set; }
        public string? TreatmentPerformed { get; set; }
        public string? PostOpAdvice { get; set; }
        public string? FollowUp { get; set; }
        public List<ToothFindingPayload>? Findings { get; set; } = new();
    }

    public class ToothFindingPayload
    {
        public int ToothNumber { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public int? Confidence { get; set; }
        public string? CdtCode { get; set; }
        public string? Procedure { get; set; }
        public string? Color { get; set; }
        public string? Surface { get; set; }
    }
}
