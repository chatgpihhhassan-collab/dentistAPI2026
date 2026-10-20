using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/clinic")]
    public class ClinicController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<ClinicController> _logger;

        public ClinicController(IConfiguration configuration, ILogger<ClinicController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _logger = logger;
        }

        /// <summary>
        /// Publicly accessible landing page configuration endpoint for website visitors and doctors
        /// </summary>
        [AllowAnonymous]
        [HttpGet("landing-config")]
        public async Task<IActionResult> GetLandingConfig([FromQuery] string clinicKey = "default")
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var tableExists = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM sys.tables t 
                    JOIN sys.schemas s ON t.schema_id = s.schema_id 
                    WHERE t.name = 'ClinicLandingConfig' AND s.name = 'dentist'");

                if (tableExists == 0)
                {
                    return Ok(null);
                }

                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT TOP 1 ConfigJson, ClinicName, Phone, EmergencyPhone, Email, Address, OperatingHours, GoogleMapsUrl, GoogleReviewsRating, GoogleReviewsCount, UpdatedAt 
                    FROM [dentist].[ClinicLandingConfig]
                    WHERE ClinicKey = @ClinicKey", new { ClinicKey = clinicKey });

                if (row == null || string.IsNullOrWhiteSpace((string)row.ConfigJson))
                {
                    return Ok(null);
                }

                string configJson = (string)row.ConfigJson;
                var parsed = JsonSerializer.Deserialize<object>(configJson);
                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve clinic landing config from SQL database. Falling back to client defaults.");
                return Ok(null);
            }
        }

        /// <summary>
        /// Save/update landing page configuration in SQL database (Doctors or Website Admins)
        /// </summary>
        [HttpPost("landing-config")]
        [HttpPut("landing-config")]
        public async Task<IActionResult> SaveLandingConfig([FromBody] JsonElement payload, [FromQuery] string clinicKey = "default")
        {
            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new { message = "Landing configuration payload is required." });
            }

            try
            {
                string rawJson = payload.GetRawText();
                string clinicName = payload.TryGetProperty("clinicName", out var cn) ? cn.GetString() ?? "Dentia Dental Clinic" : "Dentia Dental Clinic";
                string phone = payload.TryGetProperty("phone", out var p) ? p.GetString() ?? "" : "";
                string emergencyPhone = payload.TryGetProperty("emergencyPhone", out var ep) ? ep.GetString() ?? "" : "";
                string email = payload.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
                string address = payload.TryGetProperty("address", out var addr) ? addr.GetString() ?? "" : "";
                string operatingHours = payload.TryGetProperty("operatingHours", out var oh) ? oh.GetString() ?? "" : "";
                string googleMapsUrl = payload.TryGetProperty("googleMapsUrl", out var gmu) ? gmu.GetString() ?? "" : "";
                string reviewsCount = payload.TryGetProperty("googleReviewsCount", out var rc) ? rc.GetString() ?? "23k+" : "23k+";
                decimal rating = 4.9m;
                if (payload.TryGetProperty("googleReviewsRating", out var grr))
                {
                    if (grr.ValueKind == JsonValueKind.Number && grr.TryGetDecimal(out var dVal)) rating = dVal;
                    else if (grr.ValueKind == JsonValueKind.String && decimal.TryParse(grr.GetString(), out var sVal)) rating = sVal;
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
                    BEGIN
                        EXEC('CREATE SCHEMA dentist');
                    END;

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.tables t 
                        JOIN sys.schemas s ON t.schema_id = s.schema_id 
                        WHERE t.name = 'ClinicLandingConfig' AND s.name = 'dentist'
                    )
                    BEGIN
                        CREATE TABLE [dentist].[ClinicLandingConfig] (
                            [ConfigID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [ClinicKey] NVARCHAR(100) NOT NULL CONSTRAINT DF_ClinicLandingConfig_Key DEFAULT ('default'),
                            [ClinicName] NVARCHAR(200) NOT NULL,
                            [Phone] NVARCHAR(50) NULL,
                            [EmergencyPhone] NVARCHAR(50) NULL,
                            [Email] NVARCHAR(255) NULL,
                            [Address] NVARCHAR(500) NULL,
                            [OperatingHours] NVARCHAR(255) NULL,
                            [GoogleMapsUrl] NVARCHAR(1000) NULL,
                            [GoogleReviewsRating] DECIMAL(2,1) NULL,
                            [GoogleReviewsCount] NVARCHAR(50) NULL,
                            [ConfigJson] NVARCHAR(MAX) NOT NULL,
                            [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT DF_ClinicLandingConfig_UpdatedAt DEFAULT (SYSUTCDATETIME()),
                            [UpdatedBy] NVARCHAR(100) NULL,
                            CONSTRAINT UQ_ClinicLandingConfig_ClinicKey UNIQUE ([ClinicKey])
                        );
                    END;");

                await conn.ExecuteAsync(@"
                    IF EXISTS (SELECT 1 FROM [dentist].[ClinicLandingConfig] WHERE ClinicKey = @ClinicKey)
                    BEGIN
                        UPDATE [dentist].[ClinicLandingConfig]
                        SET ClinicName = @ClinicName,
                            Phone = @Phone,
                            EmergencyPhone = @EmergencyPhone,
                            Email = @Email,
                            Address = @Address,
                            OperatingHours = @OperatingHours,
                            GoogleMapsUrl = @GoogleMapsUrl,
                            GoogleReviewsRating = @GoogleReviewsRating,
                            GoogleReviewsCount = @GoogleReviewsCount,
                            ConfigJson = @ConfigJson,
                            UpdatedAt = SYSUTCDATETIME(),
                            UpdatedBy = @UpdatedBy
                        WHERE ClinicKey = @ClinicKey;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [dentist].[ClinicLandingConfig]
                            (ClinicKey, ClinicName, Phone, EmergencyPhone, Email, Address, OperatingHours, GoogleMapsUrl, GoogleReviewsRating, GoogleReviewsCount, ConfigJson, UpdatedAt, UpdatedBy)
                        VALUES
                            (@ClinicKey, @ClinicName, @Phone, @EmergencyPhone, @Email, @Address, @OperatingHours, @GoogleMapsUrl, @GoogleReviewsRating, @GoogleReviewsCount, @ConfigJson, SYSUTCDATETIME(), @UpdatedBy);
                    END;", new
                {
                    ClinicKey = clinicKey,
                    ClinicName = clinicName,
                    Phone = phone,
                    EmergencyPhone = emergencyPhone,
                    Email = email,
                    Address = address,
                    OperatingHours = operatingHours,
                    GoogleMapsUrl = googleMapsUrl,
                    GoogleReviewsRating = rating,
                    GoogleReviewsCount = reviewsCount,
                    ConfigJson = rawJson,
                    UpdatedBy = User?.Identity?.Name ?? "Doctor"
                });

                return Ok(new { success = true, message = "Clinic landing page configuration successfully saved to SQL database." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save landing configuration to SQL database.");
                return StatusCode(500, new { message = "Failed to save landing configuration to SQL database.", error = ex.Message });
            }
        }
    }
}
