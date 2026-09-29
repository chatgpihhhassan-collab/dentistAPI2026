using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private string GetLogsDirectory()
        {
            return DbCallLogger.GetLogsDirectory();
        }

        public class StructuredLogEntry
        {
            public string CallType { get; set; } = "UNKNOWN";
            public string Status { get; set; } = "SUCCESS";
            public string CallSentTime { get; set; } = "";
            public string ResponseReceivedTime { get; set; } = "";
            public string TotalDuration { get; set; } = "";
            public string TargetEndpoint { get; set; } = "";
            public string? Metadata { get; set; }
            public string? ErrorMessage { get; set; }
            public string? DatabaseName { get; set; }
            public string? DatabaseServer { get; set; }
            public object? ExtractedResult { get; set; }
            public object? RawRequestData { get; set; }
            public object? RawResponseReceived { get; set; }
            public string RawContent { get; set; } = "";
        }

        private static object ParseJsonOrString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string trimmed = input.Trim();

            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    return JsonSerializer.Deserialize<object>(trimmed)!;
                }
                catch { }
            }

            var jsonStart = trimmed.IndexOf('{');
            var jsonEnd = trimmed.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var prefix = trimmed.Substring(0, jsonStart).Trim();
                var jsonSub = trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1).Trim();
                try
                {
                    using var doc = JsonDocument.Parse(jsonSub);
                    var parsedObj = JsonSerializer.Deserialize<object>(jsonSub);
                    return new { info = prefix, data = parsedObj };
                }
                catch { }
            }

            return trimmed;
        }

        private static StructuredLogEntry ParseRawEntry(string entry)
        {
            var item = new StructuredLogEntry { RawContent = entry };
            var lines = entry.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var extractedSb = new StringBuilder();
            var requestSb = new StringBuilder();
            var responseSb = new StringBuilder();

            int section = 0; // 0: header, 1: extracted, 2: request, 3: response

            foreach (var line in lines)
            {
                if (line.Contains("[EXTRACTED OUTPUT / TRANSCRIPTION / RESULT]:")) { section = 1; continue; }
                else if (line.Contains("[RAW REQUEST DATA SENT TO GEMINI]:") || line.Contains("[SQL PARAMETERS]:") || line.Contains("[REQUEST BODY / PAYLOAD]:")) { section = 2; continue; }
                else if (line.Contains("[RAW RESPONSE RECEIVED FROM GEMINI]:") || line.Contains("[SQL QUERY / COMMAND]:") || line.Contains("[RESPONSE BODY / DATA]:")) { section = 3; continue; }
                else if (line.StartsWith("-----") || line.StartsWith("=====")) { continue; }

                if (section == 1) { extractedSb.AppendLine(line); }
                else if (section == 2) { requestSb.AppendLine(line); }
                else if (section == 3) { responseSb.AppendLine(line); }
                else
                {
                    var match = Regex.Match(line, @"^\[(?<key>[^\]]+)\]\s*:\s*(?<value>.*)$");
                    if (match.Success)
                    {
                        var key = match.Groups["key"].Value.Trim().ToUpperInvariant();
                        var val = match.Groups["value"].Value.Trim();

                        switch (key)
                        {
                            case "CALL TYPE":
                            case "TYPE":
                                item.CallType = val;
                                break;
                            case "STATUS":
                                item.Status = val;
                                break;
                            case "CALL SENT TIME":
                            case "EXECUTION START TIME":
                            case "REQUEST START TIME":
                                item.CallSentTime = val;
                                break;
                            case "RESPONSE RECEIVED TIME":
                            case "EXECUTION END TIME":
                            case "RESPONSE SENT TIME":
                                item.ResponseReceivedTime = val;
                                break;
                            case "TOTAL DURATION":
                            case "DURATION":
                                item.TotalDuration = val;
                                break;
                            case "TARGET ENDPOINT":
                            case "HTTP METHOD & URL":
                            case "PATH":
                                item.TargetEndpoint = val;
                                break;
                            case "DATABASE NAME":
                                item.DatabaseName = val;
                                break;
                            case "DATABASE SERVER":
                                item.DatabaseServer = val;
                                break;
                            case "CONTEXT / METADATA":
                            case "CLIENT IP":
                            case "HTTP STATUS CODE":
                                item.Metadata = string.IsNullOrEmpty(item.Metadata) ? $"{key}: {val}" : $"{item.Metadata} | {key}: {val}";
                                break;
                            case "DB ERROR":
                            case "ERROR MESSAGE":
                            case "EXCEPTION":
                                item.ErrorMessage = val;
                                break;
                            case "DB RESPONSE / RESULT":
                                item.ExtractedResult = val;
                                break;
                        }
                    }
                }
            }

            if (extractedSb.Length > 0 && item.ExtractedResult == null)
            {
                item.ExtractedResult = ParseJsonOrString(extractedSb.ToString().Trim());
            }

            if (requestSb.Length > 0)
            {
                item.RawRequestData = ParseJsonOrString(requestSb.ToString().Trim());
            }

            if (responseSb.Length > 0)
            {
                item.RawResponseReceived = ParseJsonOrString(responseSb.ToString().Trim());
            }

            return item;
        }

        private List<StructuredLogEntry> ReadLogEntries(string filename, int maxEntries = 100)
        {
            var logsDir = GetLogsDirectory();
            string logFile = Path.Combine(logsDir, filename);
            if (!System.IO.File.Exists(logFile)) return new List<StructuredLogEntry>();

            try
            {
                var content = System.IO.File.ReadAllText(logFile, Encoding.UTF8);
                return content.Split("================================================================================", StringSplitOptions.RemoveEmptyEntries)
                              .Select(e => e.Trim())
                              .Where(e => !string.IsNullOrWhiteSpace(e))
                              .Reverse()
                              .Take(maxEntries)
                              .Select(ParseRawEntry)
                              .ToList();
            }
            catch
            {
                return new List<StructuredLogEntry>();
            }
        }

        /// <summary>
        /// Get database query logs with DB name, queries, parameters, duration, and results.
        /// </summary>
        [HttpGet("database")]
        [Produces("application/json")]
        public IActionResult GetDatabaseLogs([FromQuery] string? date = null, [FromQuery] int maxEntries = 50)
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            var entries = ReadLogEntries($"database_calls_{targetDate}.log", maxEntries);

            return Ok(new
            {
                type = "DATABASE CALLS",
                date = targetDate,
                totalEntries = entries.Count,
                entries = entries
            });
        }

        /// <summary>
        /// Get all HTTP API endpoint requests and responses.
        /// </summary>
        [HttpGet("api")]
        [Produces("application/json")]
        public IActionResult GetApiEndpointLogs([FromQuery] string? date = null, [FromQuery] int maxEntries = 50)
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            var entries = ReadLogEntries($"api_calls_{targetDate}.log", maxEntries);

            return Ok(new
            {
                type = "API ENDPOINT REQUESTS",
                date = targetDate,
                totalEntries = entries.Count,
                entries = entries
            });
        }

        /// <summary>
        /// Get all Gemini AI calls and mic recordings.
        /// </summary>
        [HttpGet("gemini")]
        [Produces("application/json")]
        public IActionResult GetGeminiLogs([FromQuery] string? date = null, [FromQuery] int maxEntries = 50)
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            var entries = ReadLogEntries($"gemini_calls_{targetDate}.log", maxEntries);

            return Ok(new
            {
                type = "GEMINI AI CALLS",
                date = targetDate,
                totalEntries = entries.Count,
                entries = entries
            });
        }

        /// <summary>
        /// Get doctor mic voice dictation transcription logs.
        /// </summary>
        [HttpGet("gemini/mic")]
        [Produces("application/json")]
        public IActionResult GetMicTranscriptionLogs([FromQuery] string? date = null, [FromQuery] int maxEntries = 50)
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            var entries = ReadLogEntries($"gemini_mic_transcriptions_{targetDate}.log", maxEntries);

            return Ok(new
            {
                type = "MIC TRANSCRIPTIONS",
                date = targetDate,
                totalEntries = entries.Count,
                entries = entries
            });
        }

        /// <summary>
        /// Get combined logs of all activity (DB, API, Gemini).
        /// </summary>
        [HttpGet("all")]
        [Produces("application/json")]
        public IActionResult GetAllLogs([FromQuery] string? date = null, [FromQuery] int maxEntries = 100)
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            var entries = ReadLogEntries($"all_activities_{targetDate}.log", maxEntries);

            return Ok(new
            {
                type = "ALL ACTIVITIES",
                date = targetDate,
                totalEntries = entries.Count,
                entries = entries
            });
        }

        /// <summary>
        /// Interactive visual dashboard to inspect all API, DB, and Gemini logs with filter tabs.
        /// </summary>
        [HttpGet("view")]
        [Produces("text/html")]
        public IActionResult ViewLogsDashboard([FromQuery] string? date = null, [FromQuery] string filter = "all")
        {
            string targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date.Trim();
            string filename = filter.ToLowerInvariant() switch
            {
                "db" or "database" => $"database_calls_{targetDate}.log",
                "api" or "endpoints" => $"api_calls_{targetDate}.log",
                "gemini" => $"gemini_calls_{targetDate}.log",
                "mic" => $"gemini_mic_transcriptions_{targetDate}.log",
                _ => $"all_activities_{targetDate}.log"
            };

            var entries = ReadLogEntries(filename, 100);

            // If all_activities doesn't exist yet, fall back to checking gemini or db
            if (entries.Count == 0 && filter == "all")
            {
                entries = ReadLogEntries($"database_calls_{targetDate}.log", 50);
                entries.AddRange(ReadLogEntries($"api_calls_{targetDate}.log", 50));
                entries.AddRange(ReadLogEntries($"gemini_calls_{targetDate}.log", 50));
            }

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"<title>Live System Logs - {targetDate}</title>");
            html.AppendLine("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=Fira+Code:wght@400;500&display=swap' rel='stylesheet'>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: 'Inter', sans-serif; background: #0B0F19; color: #E2E8F0; margin: 0; padding: 24px; }");
            html.AppendLine(".container { max-width: 1200px; margin: 0 auto; }");
            html.AppendLine(".header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #1E293B; padding-bottom: 16px; margin-bottom: 20px; }");
            html.AppendLine("h1 { font-size: 24px; font-weight: 800; color: #F8FAFC; margin: 0; }");
            html.AppendLine(".tabs { display: flex; gap: 8px; margin-bottom: 24px; flex-wrap: wrap; }");
            html.AppendLine(".tab { background: #1E293B; color: #94A3B8; text-decoration: none; padding: 8px 16px; border-radius: 10px; font-size: 13px; font-weight: 600; border: 1px solid #334155; transition: all 0.2s; }");
            html.AppendLine(".tab:hover { background: #334155; color: #FFF; }");
            html.AppendLine(".tab.active { background: #2563EB; color: #FFF; border-color: #3B82F6; }");
            html.AppendLine(".badge { background: #3B82F6; color: white; padding: 4px 10px; border-radius: 9999px; font-size: 11px; font-weight: 700; }");
            html.AppendLine(".badge-success { background: #059669; }");
            html.AppendLine(".badge-fail { background: #DC2626; }");
            html.AppendLine(".badge-db { background: #D97706; }");
            html.AppendLine(".badge-api { background: #7C3AED; }");
            html.AppendLine(".badge-gemini { background: #2563EB; }");
            html.AppendLine(".card { background: #131B2E; border: 1px solid #1E293B; border-radius: 14px; margin-bottom: 18px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.5); }");
            html.AppendLine(".card-header { padding: 14px 18px; background: #17223B; border-bottom: 1px solid #1E293B; display: flex; justify-content: space-between; align-items: center; }");
            html.AppendLine(".card-title { font-weight: 700; font-size: 14px; color: #38BDF8; display: flex; align-items: center; gap: 10px; }");
            html.AppendLine(".card-body { padding: 18px; }");
            html.AppendLine(".meta-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 10px; margin-bottom: 14px; background: #0B0F19; padding: 12px 16px; border-radius: 10px; border: 1px solid #1E293B; font-size: 12px; }");
            html.AppendLine(".meta-label { color: #64748B; font-weight: 600; text-transform: uppercase; font-size: 10px; }");
            html.AppendLine(".meta-val { color: #F1F5F9; font-weight: 700; margin-top: 2px; }");
            html.AppendLine(".code-title { font-size: 11px; font-weight: 700; text-transform: uppercase; color: #94A3B8; margin-top: 14px; margin-bottom: 6px; }");
            html.AppendLine("pre { background: #06090E; border: 1px solid #1E293B; border-radius: 8px; padding: 12px; font-family: 'Fira Code', monospace; font-size: 12px; overflow-x: auto; white-space: pre-wrap; margin: 0; }");
            html.AppendLine("</style></head><body>");
            html.AppendLine("<div class='container'>");
            html.AppendLine("<div class='header'>");
            html.AppendLine($"<div><h1>📋 System Activity Logs</h1><p style='color:#94A3B8; font-size:13px; margin:4px 0 0 0;'>Date: <strong>{targetDate}</strong> | Showing: <strong>{entries.Count} entries</strong></p></div>");
            html.AppendLine($"<div><a class='tab' href='/api/logs/{filter}?date={targetDate}' target='_blank'>JSON API</a> <a class='tab' href='/swagger' target='_blank' style='margin-left:6px;'>Swagger UI</a></div>");
            html.AppendLine("</div>");

            // Filter Tabs
            string activeAll = filter == "all" ? "active" : "";
            string activeApi = filter == "api" ? "active" : "";
            string activeDb = filter == "db" ? "active" : "";
            string activeGemini = filter == "gemini" ? "active" : "";
            string activeMic = filter == "mic" ? "active" : "";

            html.AppendLine("<div class='tabs'>");
            html.AppendLine($"<a class='tab {activeAll}' href='/api/logs/view?date={targetDate}&filter=all'>📊 All Activities</a>");
            html.AppendLine($"<a class='tab {activeApi}' href='/api/logs/view?date={targetDate}&filter=api'>🌐 API Endpoints & URLs</a>");
            html.AppendLine($"<a class='tab {activeDb}' href='/api/logs/view?date={targetDate}&filter=db'>💾 Database Calls & DB Names</a>");
            html.AppendLine($"<a class='tab {activeGemini}' href='/api/logs/view?date={targetDate}&filter=gemini'>🤖 Gemini AI Calls</a>");
            html.AppendLine($"<a class='tab {activeMic}' href='/api/logs/view?date={targetDate}&filter=mic'>🎙️ Mic Transcriptions</a>");
            html.AppendLine("</div>");

            if (entries.Count == 0)
            {
                html.AppendLine($"<div class='card' style='padding:50px; text-align:center;'><h3 style='color:#94A3B8;'>No logs found for filter '{filter}' on {targetDate}.</h3><p style='font-size:13px; color:#64748B;'>Perform any API or Database action to see live records here.</p></div>");
            }
            else
            {
                foreach (var log in entries)
                {
                    bool isOk = log.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    string badgeStatus = isOk ? "badge-success" : "badge-fail";
                    string typeBadge = log.CallType.Contains("DATABASE") ? "badge-db" : (log.CallType.Contains("API") ? "badge-api" : "badge-gemini");

                    html.AppendLine("<div class='card'>");
                    html.AppendLine("<div class='card-header'>");
                    html.AppendLine($"<div class='card-title'><span class='badge {badgeStatus}'>{log.Status}</span> <span class='badge {typeBadge}'>{log.CallType}</span> <span>{log.TargetEndpoint}</span></div>");
                    html.AppendLine($"<div><span class='badge' style='background:#334155;'>⏱️ {log.TotalDuration}</span></div>");
                    html.AppendLine("</div>");
                    html.AppendLine("<div class='card-body'>");

                    html.AppendLine("<div class='meta-grid'>");
                    html.AppendLine($"<div><div class='meta-label'>Start Time</div><div class='meta-val'>{log.CallSentTime}</div></div>");
                    html.AppendLine($"<div><div class='meta-label'>End Time</div><div class='meta-val'>{log.ResponseReceivedTime}</div></div>");
                    if (!string.IsNullOrEmpty(log.DatabaseName))
                    {
                        html.AppendLine($"<div><div class='meta-label'>Database Name</div><div class='meta-val' style='color:#F59E0B;'>{log.DatabaseName}</div></div>");
                        html.AppendLine($"<div><div class='meta-label'>Server Host</div><div class='meta-val'>{log.DatabaseServer}</div></div>");
                    }
                    if (!string.IsNullOrEmpty(log.Metadata))
                    {
                        html.AppendLine($"<div><div class='meta-label'>Details</div><div class='meta-val'>{log.Metadata}</div></div>");
                    }
                    html.AppendLine("</div>");

                    if (!string.IsNullOrEmpty(log.ErrorMessage))
                    {
                        html.AppendLine("<div class='code-title' style='color:#EF4444;'>Error Details</div>");
                        html.AppendLine($"<pre style='color:#FCA5A5; border-color:#EF4444;'>{System.Net.WebUtility.HtmlEncode(log.ErrorMessage)}</pre>");
                    }

                    if (log.RawRequestData != null)
                    {
                        string reqTitle = log.CallType.Contains("DATABASE") ? "SQL Parameters" : "Request Payload / Query Data";
                        html.AppendLine($"<div class='code-title'>{reqTitle}</div>");
                        html.AppendLine($"<pre style='color:#FDE68A;'>{System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(log.RawRequestData, new JsonSerializerOptions { WriteIndented = true }))}</pre>");
                    }

                    if (log.RawResponseReceived != null)
                    {
                        string respTitle = log.CallType.Contains("DATABASE") ? "SQL Query / Command Executed" : "Response Data / Body";
                        html.AppendLine($"<div class='code-title'>{respTitle}</div>");
                        html.AppendLine($"<pre style='color:#BAE6FD;'>{System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(log.RawResponseReceived, new JsonSerializerOptions { WriteIndented = true }))}</pre>");
                    }

                    if (log.ExtractedResult != null)
                    {
                        html.AppendLine("<div class='code-title'>Result / Summary</div>");
                        html.AppendLine($"<pre style='color:#A7F3D0;'>{System.Net.WebUtility.HtmlEncode(JsonSerializer.Serialize(log.ExtractedResult, new JsonSerializerOptions { WriteIndented = true }))}</pre>");
                    }

                    html.AppendLine("</div></div>");
                }
            }

            html.AppendLine("</div></body></html>");
            return Content(html.ToString(), "text/html", Encoding.UTF8);
        }

        /// <summary>
        /// List all available log files stored on the server.
        /// </summary>
        [HttpGet("files")]
        [Produces("application/json")]
        public IActionResult ListLogFiles()
        {
            var logsDir = GetLogsDirectory();
            if (!Directory.Exists(logsDir))
            {
                return Ok(new { directory = logsDir, files = Array.Empty<object>() });
            }

            var files = Directory.GetFiles(logsDir, "*.log")
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.LastWriteTime)
                                 .Select(f => new
                                 {
                                     fileName = f.Name,
                                     sizeKb = Math.Round((double)f.Length / 1024, 2),
                                     lastModified = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                 })
                                 .ToList();

            return Ok(new
            {
                directory = logsDir,
                count = files.Count,
                files = files
            });
        }
    }
}
