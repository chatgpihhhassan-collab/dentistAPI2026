using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    /// <summary>
    /// Background queue processor for WhatsApp outbound messages.
    /// Strictly limits throughput to ~5 messages/minute per session with 2-6s random jitter,
    /// performs periodic gateway health checks, auto-spawns gateway in Dev, and handles fallback.
    /// </summary>
    public class WhatsAppBackgroundQueueService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WhatsAppBackgroundQueueService> _logger;
        private readonly IHostEnvironment _env;
        private DateTime _lastHealthCheck = DateTime.MinValue;

        public WhatsAppBackgroundQueueService(
            IServiceProvider serviceProvider,
            ILogger<WhatsAppBackgroundQueueService> logger,
            IHostEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _env = env;
        }

        private void EnsureDevGatewayRunning()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var res = client.GetAsync("http://localhost:2785/api/sessions/dentia-clinic-bot/status").GetAwaiter().GetResult();
                if (res.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ OpenWA Gateway is already responsive on http://localhost:2785");
                    return;
                }
            }
            catch
            {
                // Gateway not reachable on 2785, proceed to auto-spawn
            }

            try
            {
                var searchDirs = new[]
                {
                    Directory.GetCurrentDirectory(),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
                    @"f:\DentistApp_Theme2"
                };

                string? runnerScript = null;
                string? workingDir = null;

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
                    var candidate = Path.Combine(dir, "openwa_gateway_runner.mjs");
                    if (File.Exists(candidate))
                    {
                        runnerScript = candidate;
                        workingDir = dir;
                        break;
                    }
                }

                if (runnerScript != null)
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = $"\"{runnerScript}\"",
                        WorkingDirectory = workingDir,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    _logger.LogInformation("🚀 Development Mode: Auto-spawned OpenWA Gateway daemon at {Script}", runnerScript);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not auto-spawn OpenWA dev runner: {Message}", ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🦷 Dentia WhatsApp Rate-Limited Background Queue Service started.");

            // In local development, ensure OpenWA is running automatically so developer doesn't need to start it manually
            if (_env.IsDevelopment())
            {
                EnsureDevGatewayRunning();
            }

            // Wait 5 seconds after startup before first processing run
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                        // 1. Periodic Health Check every 60 seconds
                        if ((DateTime.UtcNow - _lastHealthCheck).TotalSeconds >= 60)
                        {
                            _lastHealthCheck = DateTime.UtcNow;
                            try
                            {
                                var status = await whatsAppService.GetSessionStatusAsync();
                                _logger.LogDebug("WhatsApp session health check: {Status} (Connected: {IsConnected})", 
                                    status.Status, status.IsConnected);
                            }
                            catch (Exception healthEx)
                            {
                                _logger.LogWarning("WhatsApp gateway health check failed: {Message}", healthEx.Message);
                            }
                        }

                        // 2. Process batch of pending messages (handles max 5/min, 2-6s jitter, retries, fallback)
                        int processed = await whatsAppService.ProcessPendingQueueBatchAsync();

                        if (processed > 0)
                        {
                            _logger.LogInformation("WhatsApp Background Queue: Dispatched {Count} message(s) in batch.", processed);
                            // Enforce minute pacing after a batch
                            await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken);
                        }
                        else
                        {
                            // Idle polling interval
                            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in WhatsAppBackgroundQueueService execution loop.");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            }

            _logger.LogInformation("Dentia WhatsApp Background Queue Service stopped gracefully.");
        }
    }
}
