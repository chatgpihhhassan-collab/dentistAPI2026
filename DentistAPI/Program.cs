using System;
using System.Threading.RateLimiting;
using DentistAPI.Repositories;
using DentistAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// Register Repository and Services
builder.Services.AddHttpClient("", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<DentalRepository>();
builder.Services.AddScoped<IAIDentalNotesRepository, AIDentalNotesRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDataNormalizationService, DataNormalizationService>();
builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
builder.Services.AddScoped<TreatmentPlanService>();

// Register WhatsApp OpenWA Service & Background Queue Processor
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>(client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("OPENWA_BASE_URL") 
                  ?? builder.Configuration["OpenWA:BaseUrl"] 
                  ?? "http://localhost:2785";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddHostedService<WhatsAppBackgroundQueueService>();

var useMockSpeech = builder.Configuration.GetValue<bool>("UseMockSpeechToText", false);
if (useMockSpeech)
{
    builder.Services.AddScoped<ISpeechToTextService, MockSpeechToTextService>();
}
else
{
    builder.Services.AddScoped<ISpeechToTextService, GeminiSpeechToTextService>();
}

builder.Services.AddScoped<IGeminiDentalNotesService, GeminiDentalNotesService>();
builder.Services.AddScoped<GeminiDentalNotesService>();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IFileUploadSecurityService, FileUploadSecurityService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// Configure Rate Limiting Policies
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers.RetryAfter = "60";
        var rejectionEnvelope = new
        {
            statusCode = StatusCodes.Status429TooManyRequests,
            error = "Too Many Requests",
            message = "Rate limit exceeded. Please wait a moment before sending additional requests.",
            retryAfterSeconds = 60,
            timestamp = DateTime.UtcNow
        };
        await context.HttpContext.Response.WriteAsJsonAsync(rejectionEnvelope, cancellationToken);
    };

    // 1. Strict Auth Policy: 5 requests per 1 minute window per IP
    options.AddPolicy("auth-strict", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 3,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // 2. Upload Policy: 15 uploads per 1 minute window per IP
    options.AddPolicy("upload-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 15,
            TokensPerPeriod = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(20),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        });
    });

    // 3. AI Inference Policy: 25 requests per 1 minute window per IP
    options.AddPolicy("ai-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 25,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        });
    });

    // 4. General Global Policy: 200 requests per 1 minute window per IP
    options.AddPolicy("general-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
});

// Configure CORS with explicit origin validation (SEC-05)
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.EndsWith("vercel.app", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.EndsWith("vitonta.com", StringComparison.OrdinalIgnoreCase) ||
                        configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
});

// Subscribe Database Diagnostic Observer to log all SQL calls, db names, parameters, and timings
System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(new DatabaseDiagnosticObserver());

var app = builder.Build();

// 1. Centralized Global Exception Middleware (No information leakage, attaches Correlation ID)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Security Headers Middleware (SEC-06 & SEC-08)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob: https:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; font-src 'self' data:; connect-src 'self' https: http://localhost:* ws://localhost:*; frame-ancestors 'self';";
    await next();
});

// 2. Intercept and log all incoming API calls, URLs, and responses
app.UseMiddleware<ApiRequestLoggingMiddleware>();

// 3. Configure Swagger / OpenAPI documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dentist API v1");
    c.RoutePrefix = "swagger";
});

// 4. CORS
app.UseCors("AllowAll");

// 5. Rate Limiter Middleware
app.UseRateLimiter();

// 6. Doctor Token Authentication Guard: blocks unauthorized access to clinical & patient APIs
app.UseMiddleware<DoctorAuthMiddleware>();

// 7. Static files and client fallbacks
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers().RequireRateLimiting("general-policy");


app.MapFallbackToFile("index.html");

app.Run();
