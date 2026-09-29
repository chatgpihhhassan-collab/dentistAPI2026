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
    [Route("api/[controller]")]
    [EnableRateLimiting("auth-strict")]
    public class AuthController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITokenService _tokenService;

        public AuthController(DentalRepository repository, ITokenService tokenService)
        {
            _repository = repository;
            _tokenService = tokenService;
        }

        private bool IsCallerSuperAdmin()
        {
            string? authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                string token = authHeader.Substring(7).Trim();
                if (_tokenService.ValidateToken(token, out var payload) && payload != null)
                {
                    return payload.IsSuperAdmin;
                }
            }
            return false;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingDoctor = await _repository.GetDoctorByUsernameAsync(request.Username);
            if (existingDoctor != null) return BadRequest(new { message = $"The username '{request.Username}' is already registered. Please choose a different username or sign in." });

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            int id = await _repository.RegisterDoctorAsync(request.Username, hashedPassword, request.FirstName, request.LastName, string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region);
            string token = _tokenService.GenerateToken(id, request.Username, false);
            return Ok(new AuthResponse { 
                DoctorID = id, 
                FirstName = request.FirstName, 
                LastName = request.LastName, 
                Region = string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region, 
                Token = token,
                Specialization = request.Specialization ?? "General Dental Surgeon",
                ProfileImageUrl = request.ProfileImageUrl
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var doctor = await _repository.GetDoctorByUsernameAsync(request.Username);
            if (doctor == null || !BCrypt.Net.BCrypt.Verify(request.Password, doctor.PasswordHash.Trim()))
            {
                return Unauthorized(new { message = "Invalid username or password. Please check your credentials and try again." });
            }

            string token = _tokenService.GenerateToken(doctor.DoctorID, doctor.Username, doctor.IsSuperAdmin);
            return Ok(new AuthResponse { 
                DoctorID = doctor.DoctorID, 
                FirstName = doctor.FirstName, 
                LastName = doctor.LastName, 
                Region = doctor.Region, 
                Token = token, 
                IsSuperAdmin = doctor.IsSuperAdmin,
                Specialization = doctor.Specialization,
                ProfileImageUrl = doctor.ProfileImageUrl
            });
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            // If caller is superadmin, return all doctors (including inactive), otherwise return active doctors
            bool isSuperAdmin = IsCallerSuperAdmin();
            var doctors = isSuperAdmin 
                ? await _repository.GetAllDoctorsForAdminAsync() 
                : await _repository.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var doctor = await _repository.GetDoctorByIdAsync(id);
            if (doctor == null) return NotFound($"Doctor not found with ID {id}");
            return Ok(doctor);
        }

        // Superadmin only endpoint to create a new doctor with complete clinical profile
        [HttpPost("doctors")]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorProfileRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Super Admin can register or manage doctor profiles." });
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var existingDoctor = await _repository.GetDoctorByUsernameAsync(request.Username);
            if (existingDoctor != null)
            {
                return BadRequest(new { message = $"Username '{request.Username}' is already taken." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            int newDoctorId = await _repository.CreateDoctorWithProfileAsync(request, hashedPassword);
            var createdDoc = await _repository.GetDoctorByIdAsync(newDoctorId);

            return Ok(createdDoc);
        }

        // Superadmin only endpoint to update complete doctor profile, experience, and organizations
        [HttpPut("doctors/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Super Admin can edit doctor profiles and experience." });
            }

            var existing = await _repository.GetDoctorByIdAsync(id);
            if (existing == null) return NotFound(new { message = $"Doctor with ID {id} not found." });

            await _repository.UpdateDoctorFullProfileAsync(id, request);
            return Ok(new { message = "Doctor profile and experience updated successfully." });
        }

        // Superadmin only endpoint to toggle doctor active/inactive status
        [HttpPut("doctors/{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] ToggleDoctorStatusRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Super Admin can modify doctor active status." });
            }

            await _repository.ToggleDoctorStatusAsync(id, request.IsActive);
            return Ok(new { message = $"Doctor status set to {(request.IsActive ? "Active" : "Inactive")}." });
        }

        // Superadmin or self can update password
        [HttpPut("doctors/{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Password cannot be empty." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _repository.UpdateDoctorPasswordAsync(id, hashedPassword);
            return Ok(new { message = "Password updated successfully." });
        }
    }
}
