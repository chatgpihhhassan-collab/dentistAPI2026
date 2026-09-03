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
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime PreferredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public int? DoctorID { get; set; }
    }

    public class UpdateStatusDto
    {
        public string? Status { get; set; }
        public string? Reason { get; set; }
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
        public int DoctorID { get; set; }
        public string Message { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
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
}
