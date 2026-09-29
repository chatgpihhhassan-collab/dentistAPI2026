using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DentistAPI.Services
{
    public class ApiRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly object _lock = new();

        public ApiRequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value ?? string.Empty;

            // Only log /api/ endpoints to avoid cluttering with static files or swagger assets
            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Exclude logs polling endpoints from being logged recursively
            if (path.StartsWith("/api/Logs", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            DateTime startTime = DateTime.UtcNow;
            string method = context.Request.Method;
            string queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value! : string.Empty;
            string fullUrl = $"{method} {path}{queryString}";
            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // 1. Read Request Body
            string requestBody = string.Empty;
            context.Request.EnableBuffering();
            if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
            {
                using var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // Rewind for model binding
            }

            // Mask password if present in login/register requests
            string sanitizedRequestBody = SanitizeBody(requestBody);

            // 2. Intercept Response Body
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            Exception? caughtException = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                throw;
            }
            finally
            {
                DateTime endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                int statusCode = context.Response.StatusCode;

                // Read response body from memory stream
                responseBodyStream.Position = 0;
                string responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Position = 0;

                // Copy back to original stream so the client receives it
                await responseBodyStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                string sanitizedResponseBody = SanitizeBody(responseBody);
                if (sanitizedResponseBody.Length > 2000)
                {
                    sanitizedResponseBody = sanitizedResponseBody.Substring(0, 2000) + " ... [TRUNCATED FOR LOGS]";
                }

                // Format Log Entry
                LogApiCall(
                    fullUrl: fullUrl,
                    method: method,
                    path: path,
                    clientIp: clientIp,
                    statusCode: statusCode,
                    requestPayload: sanitizedRequestBody,
                    responsePayload: sanitizedResponseBody,
                    startTime: startTime,
                    endTime: endTime,
                    duration: duration,
                    exception: caughtException
                );
            }
        }

        private static void LogApiCall(
            string fullUrl,
            string method,
            string path,
            string clientIp,
            int statusCode,
            string requestPayload,
            string responsePayload,
            DateTime startTime,
            DateTime endTime,
            TimeSpan duration,
            Exception? exception)
        {
            try
            {
                string logsDir = DbCallLogger.GetLogsDirectory();
                string dateStr = startTime.ToString("yyyy-MM-dd");

                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"[TYPE]                  : API HTTP ENDPOINT CALL");
                sb.AppendLine($"[STATUS]                : {(statusCode >= 200 && statusCode < 400 ? "SUCCESS" : "ERROR / FAILED")}");
                sb.AppendLine($"[HTTP METHOD & URL]     : {fullUrl}");
                sb.AppendLine($"[PATH]                  : {path}");
                sb.AppendLine($"[CLIENT IP]             : {clientIp}");
                sb.AppendLine($"[HTTP STATUS CODE]      : {statusCode}");
                sb.AppendLine($"[REQUEST START TIME]    : {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[RESPONSE SENT TIME]    : {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"[TOTAL DURATION]        : {duration.TotalMilliseconds:F1} ms ({duration.TotalSeconds:F2}s)");

                if (exception != null)
                {
                    sb.AppendLine($"[EXCEPTION]             : {exception.Message}");
                }

                if (!string.IsNullOrWhiteSpace(requestPayload))
                {
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("[REQUEST BODY / PAYLOAD]:");
                    sb.AppendLine(requestPayload.Trim());
                }

                if (!string.IsNullOrWhiteSpace(responsePayload))
                {
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("[RESPONSE BODY / DATA]:");
                    sb.AppendLine(responsePayload.Trim());
                }
                sb.AppendLine("================================================================================\n");

                string logEntry = sb.ToString();

                // Live Console output
                Console.WriteLine(logEntry);

                lock (_lock)
                {
                    string apiLogFile = Path.Combine(logsDir, $"api_calls_{dateStr}.log");
                    File.AppendAllText(apiLogFile, logEntry, Encoding.UTF8);

                    // Combined daily log
                    string combinedLog = Path.Combine(logsDir, $"all_activities_{dateStr}.log");
                    File.AppendAllText(combinedLog, logEntry, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApiRequestLoggingMiddleware Error]: {ex.Message}");
            }
        }

        private static string SanitizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "(empty)";
            // Hide passwords in logs for security
            return System.Text.RegularExpressions.Regex.Replace(
                body,
                @"(""password""\s*:\s*"")[^""]+("")",
                "$1******$2",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
