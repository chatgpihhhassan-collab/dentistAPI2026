using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    public class TreatmentSession
    {
        public long SessionID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int? AppointmentID { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string FormattedDuration { get; set; } = "00:00";
        public string Status { get; set; } = "active";
        public int ActionCount { get; set; }
        public string? SummaryNotes { get; set; }
        public string? ActionLogsJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TreatmentSessionActionLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ElapsedSeconds { get; set; }
        public string ActionType { get; set; } = "clinical_action";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class StartTreatmentSessionRequest
    {
        public int? DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string? Notes { get; set; }
    }

    public class EndTreatmentSessionRequest
    {
        public int DurationSeconds { get; set; }
        public string? FormattedDuration { get; set; }
        public string? SummaryNotes { get; set; }
        public List<TreatmentSessionActionLog>? Actions { get; set; }
    }

    public class LogTreatmentSessionActionRequest
    {
        public string ActionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ElapsedSeconds { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class LatestTreatmentSessionDto
    {
        public bool HasSession { get; set; }
        public long SessionId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string FormattedDuration { get; set; } = "00:00";
        public string Status { get; set; } = "completed";
        public int ActionCount { get; set; }
        public string? SummaryNotes { get; set; }
        public List<TreatmentSessionActionLog>? ActionLogs { get; set; }
    }
}
