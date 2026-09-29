using System;
using System.IO;
using System.Text;

namespace DentistAPI.Services
{
    public static class DbCallLogger
    {
        private static readonly object _lock = new();

        public static string GetLogsDirectory()
        {
            // AppContext.BaseDirectory points to the actual deployment folder under IIS (e.g. E:\ValentiaTechnololgies\Dentist\)
            string baseDir = AppContext.BaseDirectory;
            string projectLogs = Path.Combine(baseDir, "Logs");

            if (!Directory.Exists(projectLogs))
            {
                try { Directory.CreateDirectory(projectLogs); } catch { }
            }

            // Fallback to CurrentDirectory if needed
            if (!Directory.Exists(projectLogs))
            {
                string currentDir = Directory.GetCurrentDirectory();
                projectLogs = Path.Combine(currentDir, "Logs");
                try { Directory.CreateDirectory(projectLogs); } catch { }
            }

            return projectLogs;
        }

        public static void LogDbCall(
            string databaseName,
            string serverName,
            string sqlQuery,
            string parameters,
            DateTime startTime,
            DateTime endTime,
            bool isSuccess,
            string? resultSummary = null,
            string? errorMessage = null)
        {
            try
            {
                var logsDir = GetLogsDirectory();
                var duration = endTime - startTime;
                string dateStr = startTime.ToString("yyyy-MM-dd");

                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"[TYPE]                  : DATABASE CALL");
                sb.AppendLine($"[STATUS]                : {(isSuccess ? "SUCCESS" : "FAILED / ERROR")}");
                sb.AppendLine($"[DATABASE NAME]         : {databaseName}");
                sb.AppendLine($"[DATABASE SERVER]       : {serverName}");
                sb.AppendLine($"[EXECUTION START TIME]  : {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[EXECUTION END TIME]    : {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[DURATION]              : {duration.TotalMilliseconds:F1} ms ({duration.TotalSeconds:F2}s)");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    sb.AppendLine($"[DB ERROR]              : {errorMessage}");
                }

                if (!string.IsNullOrEmpty(resultSummary))
                {
                    sb.AppendLine($"[DB RESPONSE / RESULT]  : {resultSummary}");
                }

                if (!string.IsNullOrWhiteSpace(parameters))
                {
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("[SQL PARAMETERS]:");
                    sb.AppendLine(parameters.Trim());
                }

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine("[SQL QUERY / COMMAND]:");
                sb.AppendLine(sqlQuery?.Trim() ?? "(empty query)");
                sb.AppendLine("================================================================================\n");

                string logEntry = sb.ToString();

                // Live Console output
                Console.WriteLine(logEntry);

                // Append to database_calls_YYYY-MM-DD.log
                lock (_lock)
                {
                    string logFilePath = Path.Combine(logsDir, $"database_calls_{dateStr}.log");
                    File.AppendAllText(logFilePath, logEntry, Encoding.UTF8);

                    // Also maintain a combined all_activities_YYYY-MM-DD.log
                    string combinedLog = Path.Combine(logsDir, $"all_activities_{dateStr}.log");
                    File.AppendAllText(combinedLog, logEntry, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DbCallLogger Error] Failed to write database log: {ex.Message}");
            }
        }
    }
}
