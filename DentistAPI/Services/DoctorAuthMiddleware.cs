using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace DentistAPI.Services
{
    public class DoctorAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public DoctorAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Only enforce authentication on protected /api/ endpoints
            if (path.StartsWith("/api/"))
            {
                // Public and Patient API exceptions that don't require clinician authentication
                bool isPublic = path.StartsWith("/api/auth/login") ||
                                path.StartsWith("/api/auth/register") ||
                                ((path == "/api/auth/doctors" || path == "/api/auth/doctors/") && (context.Request.Method == "GET" || context.Request.Method == "OPTIONS")) ||
                                path.StartsWith("/api/auth/doctors") ||
                                path.StartsWith("/api/logs") ||
                                path.StartsWith("/api/patient-auth") ||
                                path.StartsWith("/api/patient-portal") ||
                                ((path.StartsWith("/api/billing/invoices") && !path.EndsWith("/record-payment") && (context.Request.Method == "GET" || context.Request.Method == "OPTIONS")) || path == "/api/billing/pay-online" || path == "/api/billing/generate-cash-voucher" || path == "/api/billing/payments") ||
                                path.StartsWith("/api/billing") ||
                                path.StartsWith("/api/whatsapp") || // Whitelist all WhatsApp status, qr, start, test-send, logs, retries, and webhook routes
                                (path == "/api/appointments" && context.Request.Method == "POST") || // Public patient appointment booking
                                (path == "/api/appointments/book-voice" && context.Request.Method == "POST") || // Voice appointment booking
                                (path.StartsWith("/api/appointments/available-slots") && (context.Request.Method == "GET" || context.Request.Method == "OPTIONS")) ||
                                (path.StartsWith("/api/radiographs/") && path.EndsWith("/image") && (context.Request.Method == "GET" || context.Request.Method == "HEAD")) ||
                                (path.StartsWith("/api/organizations") && (context.Request.Method == "GET" || context.Request.Method == "OPTIONS")) ||
                                (path.StartsWith("/api/treatment-pricing") && (context.Request.Method == "GET" || context.Request.Method == "OPTIONS"));

                if (!isPublic)
                {
                    // Check Authorization header (Bearer <token>), X-Doctor-Token, or query token
                    string? authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    string? token = null;

                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring(7).Trim();
                    }
                    else if (context.Request.Headers.TryGetValue("X-Doctor-Token", out var headerToken))
                    {
                        token = headerToken.FirstOrDefault();
                    }
                    else if (path.StartsWith("/api/radiographs/") && path.EndsWith("/image") && context.Request.Method == "GET")
                    {
                        // SEC-VULN-07: Restrict URL query tokens strictly to radiograph <img> streaming
                        if (context.Request.Query.TryGetValue("token", out var queryToken))
                        {
                            token = queryToken.FirstOrDefault();
                        }
                        else if (context.Request.Query.TryGetValue("access_token", out var queryAccessToken))
                        {
                            token = queryAccessToken.FirstOrDefault();
                        }
                    }

                    DoctorTokenPayload? payload = null;
                    PatientTokenPayload? patientPayload = null;

                    bool isValidDoctor = !string.IsNullOrEmpty(token) && tokenService.ValidateToken(token, out payload);
                    bool isValidPatient = !string.IsNullOrEmpty(token) && tokenService.ValidatePatientToken(token, out patientPayload);

                    if (!isValidDoctor && !isValidPatient)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            status = 401,
                            message = "Unauthorized: Access to clinical and patient data requires an active login session. Please log in."
                        });
                        return;
                    }

                    // Attach authenticated identity to context
                    if (isValidDoctor && payload != null)
                    {
                        context.Items["DoctorId"] = payload.DoctorId;
                        context.Items["DoctorUsername"] = payload.Username;
                        context.Items["IsSuperAdmin"] = payload.IsSuperAdmin;
                        context.Items["UserRole"] = payload.Role ?? "Doctor";
                    }
                    else if (isValidPatient && patientPayload != null)
                    {
                        context.Items["PatientId"] = patientPayload.PatientId;
                        context.Items["UserRole"] = "Patient";
                    }
                }
            }

            await _next(context);
        }
    }
}
