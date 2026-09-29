using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace DentistAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(int doctorId, string username, bool isSuperAdmin);
        bool ValidateToken(string? token, out DoctorTokenPayload? payload);
        string GeneratePatientToken(int patientId, string referenceNumber, string firstName, string lastName);
        bool ValidatePatientToken(string? token, out PatientTokenPayload? payload);
    }

    public class DoctorTokenPayload
    {
        public int DoctorId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public long ExpiresAt { get; set; }
        public string Role { get; set; } = "Doctor";
    }

    public class PatientTokenPayload
    {
        public int PatientId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Patient";
        public long ExpiresAt { get; set; }
    }

    public class TokenService : ITokenService
    {
        private readonly byte[] _secretKey;

        public TokenService(IConfiguration configuration)
        {
            var keyString = configuration["Jwt:Key"] ?? "Dentia_Dental_Workspace_Super_Secure_Secret_Key_2026_NZ!";
            _secretKey = Encoding.UTF8.GetBytes(keyString);
        }

        public string GenerateToken(int doctorId, string username, bool isSuperAdmin)
        {
            var payload = new DoctorTokenPayload
            {
                DoctorId = doctorId,
                Username = username,
                IsSuperAdmin = isSuperAdmin,
                Role = "Doctor",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
            };

            var json = JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            using var hmac = new HMACSHA256(_secretKey);
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            var signatureBase64 = Convert.ToBase64String(signatureBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            return $"{payloadBase64}.{signatureBase64}";
        }

        public bool ValidateToken(string? token, out DoctorTokenPayload? payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Allow transition token for dev if needed
            if (token == "fake-jwt-token")
            {
                payload = new DoctorTokenPayload { DoctorId = 1, Username = "doctor", IsSuperAdmin = false, ExpiresAt = long.MaxValue };
                return true;
            }

            var parts = token.Split('.');
            if (parts.Length != 2) return false;

            var payloadBase64 = parts[0];
            var signatureBase64 = parts[1];

            using var hmac = new HMACSHA256(_secretKey);
            var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            var expectedSigBase64 = Convert.ToBase64String(expectedSignature)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            if (signatureBase64 != expectedSigBase64) return false;

            try
            {
                var mod = payloadBase64.Length % 4;
                if (mod > 0) payloadBase64 += new string('=', 4 - mod);
                payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));
                payload = JsonSerializer.Deserialize<DoctorTokenPayload>(json);

                if (payload == null) return false;
                if (payload.Role != "Doctor") return false;
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAt) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GeneratePatientToken(int patientId, string referenceNumber, string firstName, string lastName)
        {
            var payload = new PatientTokenPayload
            {
                PatientId = patientId,
                ReferenceNumber = referenceNumber,
                FullName = $"{firstName} {lastName}".Trim(),
                Role = "Patient",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(14).ToUnixTimeSeconds()
            };

            var json = JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            using var hmac = new HMACSHA256(_secretKey);
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            var signatureBase64 = Convert.ToBase64String(signatureBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            return $"{payloadBase64}.{signatureBase64}";
        }

        public bool ValidatePatientToken(string? token, out PatientTokenPayload? payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(token)) return false;

            var parts = token.Split('.');
            if (parts.Length != 2) return false;

            var payloadBase64 = parts[0];
            var signatureBase64 = parts[1];

            using var hmac = new HMACSHA256(_secretKey);
            var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            var expectedSigBase64 = Convert.ToBase64String(expectedSignature)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            if (signatureBase64 != expectedSigBase64) return false;

            try
            {
                var mod = payloadBase64.Length % 4;
                if (mod > 0) payloadBase64 += new string('=', 4 - mod);
                payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));
                payload = JsonSerializer.Deserialize<PatientTokenPayload>(json);

                if (payload == null) return false;
                if (payload.Role != "Patient") return false;
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAt) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
