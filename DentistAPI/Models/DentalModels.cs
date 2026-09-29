using System;

namespace DentistAPI.Models
{
    public class Patient
    {
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DOB { get; set; }
        public string Phone { get; set; }
        public string? NHINumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? Region { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string? TreatmentStage { get; set; }
        public string? TargetShade { get; set; }
        public string DentitionType { get; set; } = "Adult"; // 'Adult' | 'Pediatric' | 'Mixed'
        [System.Text.Json.Serialization.JsonIgnore]
        public byte[]? ProfileImage { get; set; }
        public string? ProfileImageMimeType { get; set; }
        public string? ProfileImageDataUrl 
        { 
            get 
            {
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var mime = !string.IsNullOrEmpty(ProfileImageMimeType) ? ProfileImageMimeType : "image/jpeg";
                    return $"data:{mime};base64,{Convert.ToBase64String(ProfileImage)}";
                }
                return null;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Contains(","))
                {
                    try
                    {
                        var parts = value.Split(new[] { ',' }, 2);
                        if (parts.Length == 2)
                        {
                            var header = parts[0]; // e.g. data:image/png;base64
                            var base64 = parts[1];
                            if (header.Contains(":") && header.Contains(";"))
                            {
                                var mime = header.Split(':')[1].Split(';')[0];
                                ProfileImageMimeType = mime;
                            }
                            ProfileImage = Convert.FromBase64String(base64);
                        }
                    }
                    catch { }
                }
            }
        }
        public string? ReferenceNumber { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsPortalActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

    public class TeethState
    {
        public int TeethStateID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; } // e.g. "1", "32", "A", "E", "T"
        public string DentitionCategory { get; set; } = "Adult"; // 'Adult' | 'Pediatric' | 'Mixed'
        public string ConditionColor { get; set; }
        public string ConditionStatus { get; set; }
        public string? Comments { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TreatmentHistory
    {
        public int HistoryID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string DentitionCategory { get; set; } = "Adult"; // 'Adult' | 'Pediatric' | 'Mixed'
        public string TreatmentPerformed { get; set; }
        public string Comments { get; set; }
        public DateTime ActionDate { get; set; }
    }

    public class Prescription
    {
        public int PrescriptionID { get; set; }
        public int PatientID { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public DateTime PrescribedDate { get; set; }
    }

    public class ChatHistoryRecord
    {
        public int ChatID { get; set; }
        public int PatientID { get; set; }
        public string Transcript { get; set; } = string.Empty;
        public string ParsedAction { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class Appointment
    {
        public int AppointmentID { get; set; }
        public int? PatientID { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime PreferredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public int? DoctorID { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Currency { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public System.Collections.Generic.List<InvoiceItem> Items { get; set; } = new();
    }

    public class SelectedProcedureDto
    {
        public string? ProcedureCode { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public decimal Price 
        { 
            get => Fee; 
            set { if (Fee == 0) Fee = value; } 
        }
        public decimal StandardFee
        {
            get => Fee;
            set { if (Fee == 0) Fee = value; }
        }
        public string? Category { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class PatientBookAppointmentRequest
    {
        public DateTime PreferredDate { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public int? DoctorID { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // "Cash" | "Online_Card"
        public decimal ConsultationFee { get; set; } = 85.00m;
        public string? Currency { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardHolderName { get; set; }
        public System.Collections.Generic.List<SelectedProcedureDto>? Procedures { get; set; }
    }

    public class UpdateTreatmentPlanRequest
    {
        public System.Collections.Generic.List<SelectedProcedureDto> Procedures { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class UpdateStatusDto
    {
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class ClinicalLog
    {
        public int LogID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public string Message { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ClinicalLogRequest
    {
        public int? DoctorId { get; set; }
        public int DoctorID { get; set; }
        public string? Message { get; set; }
        public string? LogType { get; set; }
        public string? Action { get; set; }
    }

    public class Radiograph
    {
        public int RadiographID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public DateTime UploadedAt { get; set; }
        public string? AnalysisSummary { get; set; }
    }

    public class DiagnosticAssessmentRecord
    {
        public int AssessmentID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string SuiteCategory { get; set; } = string.Empty;
        public string AssessmentJson { get; set; } = string.Empty;
        public string? CdtCode { get; set; }
        public string? DiagnosisSummary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DiagnosticAssessmentDto
    {
        public int? DoctorId { get; set; }
        public string? SuiteCategory { get; set; }
        public string? AssessmentJson { get; set; }
        public string? CdtCode { get; set; }
        public string? DiagnosisSummary { get; set; }
    }

    // ==========================================
    // 🦷 PATIENT PORTAL & BILLING MODELS
    // ==========================================

    public class Invoice
    {
        public long InvoiceID { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public int? AppointmentID { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = "Unpaid";
        public string Currency { get; set; } = "NZD";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? DoctorName { get; set; }
        public System.Collections.Generic.List<InvoiceItem> Items { get; set; } = new();
    }

    public class InvoiceItem
    {
        public long InvoiceItemID { get; set; }
        public long InvoiceID { get; set; }
        public string? ProcedureCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? ToothNumber { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Payment
    {
        public long PaymentID { get; set; }
        public string PaymentReceiptNo { get; set; } = string.Empty;
        public long InvoiceID { get; set; }
        public int PatientID { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Online_Card"; // 'Online_Card', 'Cash', 'Bank_Transfer'
        public string PaymentStatus { get; set; } = "Success"; // 'Success', 'Pending_Cash_Verification', 'Failed'
        public string? TransactionReference { get; set; }
        public string? PaymentGateway { get; set; }
        public string? CashVoucherCode { get; set; }
        public int? ReceivedByDoctorID { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
    }

    public class PatientLoginRequest
    {
        public string Identifier { get; set; } = string.Empty; // Reference Number (e.g. DEN-2026-00035) OR Email OR Phone
        public string Password { get; set; } = string.Empty;
    }

    public class PatientRegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? DOB { get; set; } // YYYY-MM-DD
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string Password { get; set; } = string.Empty;
        public int DoctorID { get; set; } = 1;
        public string Region { get; set; } = "NZ";
    }

    public class PatientActivateRequest
    {
        public string ReferenceNumber { get; set; } = string.Empty; // e.g. DEN-2026-00035
        public string DOB { get; set; } = string.Empty; // YYYY-MM-DD
        public string? PhoneLast4 { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }

    public class PatientAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public int PatientID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int DoctorID { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string? TreatmentStage { get; set; }
        public string? DentitionType { get; set; }
        public string? ProfileImageDataUrl { get; set; }
    }

    public class OnlinePaymentRequest
    {
        public long InvoiceID { get; set; }
        public decimal Amount { get; set; }
        public string CardHolderName { get; set; } = string.Empty;
        public string CardLast4 { get; set; } = "4242";
        public string? PaymentMethod { get; set; } = "Online_Card";
    }

    public class CashVoucherRequest
    {
        public long InvoiceID { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class ConfirmCashRequest
    {
        public string CashVoucherCode { get; set; } = string.Empty;
        public int StaffDoctorID { get; set; }
        public decimal? ConfirmedAmount { get; set; }
    }

    public class DoctorFeeScheduleItem
    {
        public int FeeScheduleID { get; set; }
        public int DoctorID { get; set; }
        public string Currency { get; set; } = "NZD";
        public string ProcedureCode { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string EstimatedDuration { get; set; } = "45 mins";
        public decimal StandardFee { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateDoctorFeeScheduleRequest
    {
        public string Currency { get; set; } = "NZD";
        public List<DoctorFeeScheduleItem> Procedures { get; set; } = new List<DoctorFeeScheduleItem>();
    }

    // ============================================================================
    // 📸 DENTIA IMAGING, GROQ VISION & VOICESTUDIO MODELS
    // ============================================================================

    public class RadiographRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string? ToothKey { get; set; }
        public string Modality { get; set; } = "intraoral_photo";
        public string SourceDeviceType { get; set; } = "intraoral_camera";
        public string SourceDeviceBrand { get; set; } = "Standard";
        public string SourceDeviceModel { get; set; } = "Camera";
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string MimeType { get; set; } = "image/jpeg";
        public long? FileSizeBytes { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        public int? AppointmentId { get; set; }
        public int UploadedBy { get; set; } = 1;
        public string AnalysisStatus { get; set; } = "pending";
        public string? AnalysisError { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AIFindingRecord
    {
        public int Id { get; set; }
        public int RadiographId { get; set; }
        public int PatientId { get; set; }
        public string ToothNumber { get; set; } = string.Empty;
        public string NumberingSystem { get; set; } = "Universal";
        public string Surfaces { get; set; } = "[]";
        public string FindingText { get; set; } = string.Empty;
        public string SuggestedCondition { get; set; } = string.Empty;
        public string? SuggestedCdtCode { get; set; }
        public double Confidence { get; set; }
        public string Status { get; set; } = "pending";
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? AppliedTeethStateId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AINoteDraftRecord
    {
        public int Id { get; set; }
        public int? RadiographId { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string SourceType { get; set; } = "radiograph_ai";
        public string? SourceDeviceLabel { get; set; }
        public string SoapJson { get; set; } = "{}";
        public string Status { get; set; } = "draft";
        public int? SignedBy { get; set; }
        public DateTime? SignedAt { get; set; }
        public long? FinalDentalNoteId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DoctorVoiceProfile
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string VoiceModelId { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
        public string? SampleAudioUrl { get; set; }
        public string PreferredLanguage { get; set; } = "en";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PatientAudioMessage
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public long? NoteId { get; set; }
        public string MessageType { get; set; } = "post_op_instructions";
        public string AudioFileUrl { get; set; } = string.Empty;
        public int? DurationSeconds { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime? ListenedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // DTOs for Imaging, AI Findings & VoiceStudio
    public class ImagingUploadDto
    {
        public int PatientId { get; set; }
        public string? ToothKey { get; set; }
        public string? Modality { get; set; }
        public string? SourceDeviceType { get; set; }
        public string? SourceDeviceBrand { get; set; }
        public string? SourceDeviceModel { get; set; }
        public int? AppointmentId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? CapturedAt { get; set; }
        public bool AutoAnalyze { get; set; } = true;
    }

    public class ReviewFindingRequest
    {
        public string Action { get; set; } = "accept"; // 'accept' | 'edit_accept' | 'dismiss'
        public int ReviewedBy { get; set; }
        public FindingEditPayload? Edits { get; set; }
    }

    public class FindingEditPayload
    {
        public string? ToothNumber { get; set; }
        public string? SuggestedCondition { get; set; }
        public string? SuggestedCdtCode { get; set; }
        public string[]? Surfaces { get; set; }
    }

    public class SignNoteRequest
    {
        public int DoctorId { get; set; }
        public SoapSectionsPayload? EditedSoap { get; set; }
    }

    public class SoapSectionsPayload
    {
        public string? ChiefComplaint { get; set; }
        public string? MedicalHistory { get; set; }
        public string? ObjectiveExam { get; set; }
        public string? Assessment { get; set; }
        public string? ProceduresPerformed { get; set; }
        public string? PostOpInstructions { get; set; }
        public string? FollowUpPlan { get; set; }
    }

    public class PostOpAudioRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; } = 2;
        public long? NoteId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? DoctorVoiceId { get; set; }
    }

    // --- PER-PATIENT INVOICE & CHART TREATMENT REPORT DTOs ---

    public class PatientTreatmentReportDto
    {
        public PatientReportDemographicsDto Patient { get; set; } = new();
        public FinancialSummaryDto Summary { get; set; } = new();
        public System.Collections.Generic.List<ChartTreatmentDto> ChartTreatments { get; set; } = new();
        public System.Collections.Generic.List<InvoiceReportDto> Invoices { get; set; } = new();
    }

    public class PatientReportDemographicsDto
    {
        public int PatientId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string Currency { get; set; } = "NZD";
        public DateTime? Dob { get; set; }
        public string? Gender { get; set; }
    }

    public class FinancialSummaryDto
    {
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public int InvoiceCount { get; set; }
        public int UnpaidCount { get; set; }
        public string Currency { get; set; } = "NZD";
        public decimal UnbilledCompletedTotal { get; set; }
        public int UnbilledCompletedCount { get; set; }
    }

    public class ChartTreatmentDto
    {
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string ConditionStatus { get; set; } = string.Empty;
        public string ConditionColor { get; set; } = string.Empty;
        public string? CdtCode { get; set; }
        public string? TreatmentName { get; set; }
        public string Status { get; set; } = "Planned"; // "Completed" | "Planned" | "In Progress"
        public decimal Fee { get; set; }
        public bool IsEstimatedFee { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        public string? Comments { get; set; }
    }

    public class InvoiceReportDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? DoctorName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = "NZD";
        public string? Notes { get; set; }
        public System.Collections.Generic.List<InvoiceItem> Items { get; set; } = new();
        public System.Collections.Generic.List<PaymentReceiptDto> Payments { get; set; } = new();
    }

    public class PaymentReceiptDto
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? TransactionReference { get; set; }
    }

    public class RecordPaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash_Counter"; // "Cash_Counter" | "POS_Card" | "Bank_Transfer"
        public string? Notes { get; set; }
        public int? DoctorId { get; set; }
    }

    public class CreateTreatmentInvoiceRequest
    {
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public System.Collections.Generic.List<CreateInvoiceTreatmentItem> Items { get; set; } = new();
        public string? Notes { get; set; }
        public string? Currency { get; set; }
    }

    public class CreateInvoiceTreatmentItem
    {
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? ProcedureCode { get; set; }
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ==========================================
    // CLINICAL SPECIALTIES: IMPLANT PLANNING
    // ==========================================
    public class ImplantPlanRecord
    {
        public int ImplantPlanID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? ImplantBrand { get; set; }
        public decimal ImplantLength { get; set; } // numeric, mm
        public decimal ImplantDiameter { get; set; } // numeric, mm
        public string BoneQuality { get; set; } = "D2"; // D1, D2, D3, D4 (Lekholm & Zarb)
        public decimal? BoneHeightAvailable { get; set; } // numeric, mm
        public decimal? BoneWidthAvailable { get; set; } // numeric, mm
        public bool GraftingRequired { get; set; }
        public string SinusLiftStatus { get; set; } = "None"; // None, Required, Completed, Crestal_Planned, Lateral_Window_Planned
        public string? CbctReferenceUrl { get; set; }
        public string? DigitalPlanningNotes { get; set; }
        public bool GuidedSurgeryFlag { get; set; }
        public string PlanStatus { get; set; } = "Planned"; // Planned, Surgically Placed, Restored, Completed, Cancelled
        public DateTime? PlannedDate { get; set; }
        public DateTime? PlacementDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ImplantPlanDto
    {
        public int? ImplantPlanID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public int ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? ImplantBrand { get; set; }
        public decimal ImplantLength { get; set; }
        public decimal ImplantDiameter { get; set; }
        public string BoneQuality { get; set; } = "D2";
        public decimal? BoneHeightAvailable { get; set; }
        public decimal? BoneWidthAvailable { get; set; }
        public bool GraftingRequired { get; set; }
        public string SinusLiftStatus { get; set; } = "None";
        public string? CbctReferenceUrl { get; set; }
        public string? DigitalPlanningNotes { get; set; }
        public bool GuidedSurgeryFlag { get; set; }
        public string PlanStatus { get; set; } = "Planned";
        public DateTime? PlannedDate { get; set; }
        public DateTime? PlacementDate { get; set; }
    }

    // ==========================================
    // CLINICAL SPECIALTIES: BIOPSY & PATHOLOGY
    // ==========================================
    public class BiopsyRecord
    {
        public int BiopsyID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string BiopsyType { get; set; } = "Incisional"; // Incisional / Excisional
        public string SiteOfBiopsy { get; set; } = string.Empty; // Tooth #, Quadrant, Soft tissue region
        public int? ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? ClinicalImpression { get; set; }
        public string? PathologyLabName { get; set; }
        public string? SpecimenReference { get; set; }
        public DateTime BiopsyDate { get; set; } = DateTime.UtcNow.Date;
        public string Status { get; set; } = "Specimen Sent"; // Specimen Sent, Processing, Report Received, Benign, Premalignant, Malignant
        public string? HistopathologyDiagnosis { get; set; }
        public string? ResultsNotes { get; set; }
        public bool FollowUpRequired { get; set; } = true;
        public DateTime? FollowUpDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BiopsyDto
    {
        public int? BiopsyID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string BiopsyType { get; set; } = "Incisional";
        public string SiteOfBiopsy { get; set; } = string.Empty;
        public int? ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? ClinicalImpression { get; set; }
        public string? PathologyLabName { get; set; }
        public string? SpecimenReference { get; set; }
        public DateTime? BiopsyDate { get; set; }
        public string Status { get; set; } = "Specimen Sent";
        public string? HistopathologyDiagnosis { get; set; }
        public string? ResultsNotes { get; set; }
        public bool FollowUpRequired { get; set; } = true;
        public DateTime? FollowUpDate { get; set; }
    }

    // ==========================================
    // CLINICAL SPECIALTIES: ORTHODONTICS - CLEAR ALIGNERS
    // ==========================================
    public class OrthoAlignerTreatmentRecord
    {
        public int OrthoAlignerID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string AlignerBrand { get; set; } = "Invisalign"; // Invisalign, ClearCorrect, Spark, AngelAlign, In-House, Other
        public int TotalStages { get; set; } = 1;
        public int CurrentStage { get; set; } = 1;
        public bool AttachmentsRequired { get; set; }
        public string? AttachmentNotes { get; set; }
        public bool IprRequired { get; set; }
        public string? IprDetails { get; set; }
        public string WearSchedule { get; set; } = "7 Days/Tray"; // 7 Days/Tray, 10 Days/Tray, 14 Days/Tray, 20-22 Hours/Day
        public string? RefinementScanTracking { get; set; }
        public int RefinementCount { get; set; } = 0;
        public string Arch { get; set; } = "Dual"; // Upper, Lower, Dual
        public string Status { get; set; } = "Active"; // Planned, Active, Refinement Needed, Retention, Completed, Discontinued
        public DateTime? StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public string? ClinicalNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OrthoAlignerTreatmentDto
    {
        public int? OrthoAlignerID { get; set; }
        public int PatientID { get; set; }
        public int? DoctorID { get; set; }
        public string AlignerBrand { get; set; } = "Invisalign";
        public int TotalStages { get; set; } = 1;
        public int CurrentStage { get; set; } = 1;
        public bool AttachmentsRequired { get; set; }
        public string? AttachmentNotes { get; set; }
        public bool IprRequired { get; set; }
        public string? IprDetails { get; set; }
        public string WearSchedule { get; set; } = "7 Days/Tray";
        public string? RefinementScanTracking { get; set; }
        public int RefinementCount { get; set; } = 0;
        public string Arch { get; set; } = "Dual";
        public string Status { get; set; } = "Active";
        public DateTime? StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public string? ClinicalNotes { get; set; }
    }
}

