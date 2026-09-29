using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/patient-auth")]
    [EnableRateLimiting("auth-strict")]
    public class PatientAuthController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITokenService _tokenService;

        public PatientAuthController(DentalRepository repository, ITokenService tokenService)
        {
            _repository = repository;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] PatientLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Please provide your Patient Reference Number (or Email) and password." });
            }

            var patient = await _repository.GetPatientForAuthAsync(request.Identifier);
            if (patient == null)
            {
                return Unauthorized(new { message = "Invalid Patient Reference Number or password. Please verify your credentials." });
            }

            if (string.IsNullOrEmpty(patient.PasswordHash))
            {
                return BadRequest(new
                {
                    needsActivation = true,
                    referenceNumber = patient.ReferenceNumber,
                    message = "Your clinic reference number exists, but your portal account has not been activated yet. Please activate your account."
                });
            }

            bool passwordValid = false;
            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, patient.PasswordHash.Trim());
            }
            catch
            {
                // Fallback direct match for dev / demo
                passwordValid = (request.Password == patient.PasswordHash);
            }

            if (!passwordValid)
            {
                return Unauthorized(new { message = "Invalid credentials. Please verify your Reference Number or Password." });
            }

            await _repository.UpdatePatientLastLoginAsync(patient.PatientID);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            await _repository.LogPatientPortalActivityAsync(patient.PatientID, "LOGIN", ip, ua, "Successful patient portal authentication");

            string token = _tokenService.GeneratePatientToken(patient.PatientID, patient.ReferenceNumber ?? "", patient.FirstName, patient.LastName);

            return Ok(new PatientAuthResponse
            {
                Token = token,
                PatientID = patient.PatientID,
                ReferenceNumber = patient.ReferenceNumber ?? "",
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DoctorID = patient.DoctorID,
                CurrentTreatmentPlan = patient.CurrentTreatmentPlan,
                TreatmentStage = patient.TreatmentStage,
                DentitionType = patient.DentitionType,
                ProfileImageDataUrl = patient.ProfileImageDataUrl
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PatientRegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "First name, last name, and password are required." });
            }

            // Check duplicate by Name + Phone
            DateTime? dobVal = null;
            if (!string.IsNullOrEmpty(request.DOB) && DateTime.TryParse(request.DOB, out var d)) dobVal = d;

            var existing = await _repository.FindDuplicatePatientAsync(request.DoctorID > 0 ? request.DoctorID : 1, request.FirstName, request.LastName, dobVal, request.Phone ?? "");
            if (existing != null)
            {
                return Conflict(new
                {
                    message = $"An account for '{request.FirstName} {request.LastName}' is already registered with Reference #{existing.ReferenceNumber}. Please sign in or activate your account.",
                    referenceNumber = existing.ReferenceNumber
                });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var year = DateTime.UtcNow.Year;
            // Generate next reference number
            var nextId = (new Random().Next(10000, 99999));
            string refNo = $"DEN-{year}-{nextId:D5}";

            int newId = await _repository.RegisterPatientSelfAsync(request, hashedPassword, refNo);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            await _repository.LogPatientPortalActivityAsync(newId, "REGISTER", ip, ua, $"New patient self-registered with Ref #{refNo}");

            string token = _tokenService.GeneratePatientToken(newId, refNo, request.FirstName, request.LastName);

            return Ok(new PatientAuthResponse
            {
                Token = token,
                PatientID = newId,
                ReferenceNumber = refNo,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                DoctorID = request.DoctorID,
                CurrentTreatmentPlan = "General Consultation",
                TreatmentStage = "Initial Evaluation",
                DentitionType = "Adult"
            });
        }

        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] PatientActivateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ReferenceNumber) || string.IsNullOrWhiteSpace(request.DOB) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "Reference number, Date of Birth, and new password are required." });
            }

            if (!DateTime.TryParse(request.DOB, out var parsedDob))
            {
                return BadRequest(new { message = "Invalid date format for Date of Birth." });
            }

            var patient = await _repository.GetPatientByReferenceNumberAsync(request.ReferenceNumber);
            if (patient == null)
            {
                return NotFound(new { message = "No patient record found for the provided Reference Number." });
            }

            if (patient.DOB.Date != parsedDob.Date)
            {
                return BadRequest(new { message = "Verification failed: Date of Birth does not match the clinic records for this reference number." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            bool success = await _repository.ActivatePatientAccountAsync(request.ReferenceNumber, parsedDob, hashedPassword);

            if (!success)
            {
                return StatusCode(500, new { message = "Unable to activate account. Please contact clinic reception." });
            }

            await _repository.UpdatePatientLastLoginAsync(patient.PatientID);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            await _repository.LogPatientPortalActivityAsync(patient.PatientID, "ACTIVATE", ip, ua, "Patient activated portal account via Reference Number & DOB verification");

            string token = _tokenService.GeneratePatientToken(patient.PatientID, patient.ReferenceNumber ?? request.ReferenceNumber, patient.FirstName, patient.LastName);

            return Ok(new PatientAuthResponse
            {
                Token = token,
                PatientID = patient.PatientID,
                ReferenceNumber = patient.ReferenceNumber ?? request.ReferenceNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DoctorID = patient.DoctorID,
                CurrentTreatmentPlan = patient.CurrentTreatmentPlan,
                TreatmentStage = patient.TreatmentStage,
                DentitionType = patient.DentitionType
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentPatient()
        {
            string? authHeader = Request.Headers["Authorization"].ToString();
            string? token = null;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7).Trim();
            }

            if (string.IsNullOrEmpty(token) || !_tokenService.ValidatePatientToken(token, out var payload) || payload == null)
            {
                return Unauthorized(new { message = "Valid patient session token required." });
            }

            var patient = await _repository.GetPatientByIdAsync(payload.PatientId);
            if (patient == null)
            {
                return NotFound(new { message = "Patient profile not found." });
            }

            return Ok(new PatientAuthResponse
            {
                Token = token,
                PatientID = patient.PatientID,
                ReferenceNumber = patient.ReferenceNumber ?? payload.ReferenceNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DoctorID = patient.DoctorID,
                CurrentTreatmentPlan = patient.CurrentTreatmentPlan,
                TreatmentStage = patient.TreatmentStage,
                DentitionType = patient.DentitionType,
                ProfileImageDataUrl = patient.ProfileImageDataUrl
            });
        }
    }
}
