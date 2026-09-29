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
builder.Services.AddSwaggerGen();

// Register Repository and Services
builder.Services.AddHttpClient("", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<DentalRepository>();
builder.Services.AddScoped<IAIDentalNotesRepository, AIDentalNotesRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

// Configure CORS for frontend (supporting SignalR negotiate and credentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
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
