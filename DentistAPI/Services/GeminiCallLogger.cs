using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DentistAPI.Services
{
    public static class GeminiCallLogger
    {
        private static readonly object _lock = new();

        private static string GetLogsDirectory()
        {
            // Priority 1: Current Working Directory (Project root if running via dotnet run)
            string currentDir = Directory.GetCurrentDirectory();
            string projectLogs = Path.Combine(currentDir, "Logs");

            if (!Directory.Exists(projectLogs))
            {
                try { Directory.CreateDirectory(projectLogs); } catch { }
            }
            return projectLogs;
        }

        public static void LogCall(
            string callType,
            string endpoint,
            string requestPayload,
            string responseData,
            DateTime startTime,
            DateTime endTime,
            bool isSuccess,
            string? extractedResult = null,
            string? errorMessage = null,
            string? additionalNotes = null)
        {
            string responsePayload = responseData;
            try
            {
                var logsDir = GetLogsDirectory();
                var duration = endTime - startTime;
                string dateStr = startTime.ToString("yyyy-MM-dd");
                
                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"[CALL TYPE]             : {callType}");
                sb.AppendLine($"[STATUS]                : {(isSuccess ? "SUCCESS" : "FAILED / ERROR")}");
                sb.AppendLine($"[CALL SENT TIME]        : {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[RESPONSE RECEIVED TIME]: {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[TOTAL DURATION]        : {duration.TotalMilliseconds:F0} ms ({duration.TotalSeconds:F2} seconds)");
                sb.AppendLine($"[TARGET ENDPOINT]       : {endpoint}");

                if (!string.IsNullOrEmpty(additionalNotes))
                {
                    sb.AppendLine($"[CONTEXT / METADATA]    : {additionalNotes}");
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    sb.AppendLine($"[ERROR MESSAGE]         : {errorMessage}");
                }

                if (!string.IsNullOrEmpty(extractedResult))
                {
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("[EXTRACTED OUTPUT / TRANSCRIPTION / RESULT]:");
                    sb.AppendLine(extractedResult);
                }

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine("[RAW REQUEST DATA SENT TO GEMINI]:");
                sb.AppendLine(requestPayload);

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine("[RAW RESPONSE RECEIVED FROM GEMINI]:");
                sb.AppendLine(responsePayload);
                sb.AppendLine("================================================================================\n");

                string logEntry = sb.ToString();

                // Stream to standard console output (visible in Vercel / Render / Cloud live logs dashboard)
                Console.WriteLine(logEntry);

                lock (_lock)
                {
                    // 1. General log file for all Gemini interactions
                    string mainLogFile = Path.Combine(logsDir, $"gemini_calls_{dateStr}.log");
                    File.AppendAllText(mainLogFile, logEntry, Encoding.UTF8);

                    // 2. Specialized log file specifically for Doctor Mic Transcriptions
                    if (callType.Contains("MIC", StringComparison.OrdinalIgnoreCase) || 
                        callType.Contains("SPEECH", StringComparison.OrdinalIgnoreCase) ||
                        callType.Contains("AUDIO", StringComparison.OrdinalIgnoreCase))
                    {
                        string micLogFile = Path.Combine(logsDir, $"gemini_mic_transcriptions_{dateStr}.log");
                        File.AppendAllText(micLogFile, logEntry, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeminiCallLogger ERROR] Could not write to log file: {ex.Message}");
            }
        }

        public static void LogSync(string syncType, int patientId, int doctorId, string details)
        {
            try
            {
                var logsDir = GetLogsDirectory();
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"[SYNC TYPE]      : {syncType}");
                sb.AppendLine($"[TIMESTAMP]      : {timeStr}");
                sb.AppendLine($"[PATIENT ID]     : {patientId}");
                sb.AppendLine($"[DOCTOR ID]      : {doctorId}");
                sb.AppendLine($"[DETAILS / DATA] : {details}");
                sb.AppendLine("================================================================================\n");

                string logEntry = sb.ToString();
                Console.WriteLine($"[SYNC AUDIT LOG] {syncType} | Patient #{patientId} | Doctor #{doctorId} | {details}");

                lock (_lock)
                {
                    string syncLogFile = Path.Combine(logsDir, $"sync_audit_{dateStr}.log");
                    File.AppendAllText(syncLogFile, logEntry, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncLogger ERROR] Could not write sync log: {ex.Message}");
            }
        }
    }
}
