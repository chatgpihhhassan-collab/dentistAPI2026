using System;
using System.IO;
using System.Text;

namespace DentistAPI.Services
{
    public interface IAuditLogService
    {
        void LogAccess(int? actorId, string actorRole, int patientId, string action, string details, string? ipAddress);
    }

    public class AuditLogService : IAuditLogService
    {
        private static readonly object _lock = new();

        public void LogAccess(int? actorId, string actorRole, int patientId, string action, string details, string? ipAddress)
        {
            try
            {
                string logsDir = DbCallLogger.GetLogsDirectory();
                string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string logFilePath = Path.Combine(logsDir, $"audit_access_{dateStr}.log");

                var sb = new StringBuilder();
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine($"[AUDIT ACCESS EVENT]   : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
                sb.AppendLine($"[ACTOR ID / ROLE]      : {(actorId.HasValue ? actorId.Value.ToString() : "Anonymous")} ({actorRole})");
                sb.AppendLine($"[PATIENT ID]           : {patientId}");
                sb.AppendLine($"[ACTION]               : {action}");
                sb.AppendLine($"[CLIENT IP]            : {ipAddress ?? "unknown"}");
                if (!string.IsNullOrWhiteSpace(details))
                {
                    sb.AppendLine($"[DETAILS]              : {details}");
                }
                sb.AppendLine("--------------------------------------------------------------------------------");

                string entry = sb.ToString();
                lock (_lock)
                {
                    File.AppendAllText(logFilePath, entry, Encoding.UTF8);

                    // Also log to all_activities_YYYY-MM-DD.log
                    string combinedLog = Path.Combine(logsDir, $"all_activities_{dateStr}.log");
                    File.AppendAllText(combinedLog, entry, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AuditLogService Error]: {ex.Message}");
            }
        }
    }
}
