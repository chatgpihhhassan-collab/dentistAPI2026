using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Ensure every request has a tracking correlation ID
            string correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            // Expose correlation ID on response headers so clients and frontend logs can reference it
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            // 1. Log the full details securely on the server with Correlation ID
            _logger.LogError(exception, 
                "[ERROR INCIDENT {CorrelationId}] Unhandled exception at {Path} ({Method}): {Message}",
                correlationId, context.Request.Path, context.Request.Method, exception.Message);

            // 2. Prevent information leakage to the client
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("[ERROR INCIDENT {CorrelationId}] Response already started. Cannot write error response.", correlationId);
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var sanitizedErrorResponse = new
            {
                statusCode = StatusCodes.Status500InternalServerError,
                title = "Internal Server Error",
                message = "An unexpected error occurred while processing your request. Please reference this incident ID when contacting support.",
                correlationId = correlationId,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(sanitizedErrorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
