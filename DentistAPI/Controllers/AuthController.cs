using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Repositories;
using DentistAPI.Models;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DentalRepository _repository;

        public AuthController(DentalRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingDoctor = await _repository.GetDoctorByUsernameAsync(request.Username);
            if (existingDoctor != null) return BadRequest(new { message = $"The username '{request.Username}' is already registered. Please choose a different username or sign in." });

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            int id = await _repository.RegisterDoctorAsync(request.Username, hashedPassword, request.FirstName, request.LastName, string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region);
            return Ok(new AuthResponse { DoctorID = id, FirstName = request.FirstName, LastName = request.LastName, Region = string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region, Token = "fake-jwt-token" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var doctor = await _repository.GetDoctorByUsernameAsync(request.Username);
            if (doctor == null || !BCrypt.Net.BCrypt.Verify(request.Password, doctor.PasswordHash.Trim()))
            {
                return Unauthorized(new { message = "Invalid username or password. Please check your credentials and try again." });
            }

            return Ok(new AuthResponse { DoctorID = doctor.DoctorID, FirstName = doctor.FirstName, LastName = doctor.LastName, Region = doctor.Region, Token = "fake-jwt-token", IsSuperAdmin = doctor.IsSuperAdmin });
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _repository.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var doctor = await _repository.GetDoctorByIdAsync(id);
            if (doctor == null) return NotFound($"Doctor not found with ID {id}");
            return Ok(doctor);
        }

        [HttpPut("doctors/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorRequest request)
        {
            await _repository.UpdateDoctorAsync(id, request.FirstName, request.LastName, request.Region);
            return Ok();
        }

        [HttpPut("doctors/{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _repository.UpdateDoctorPasswordAsync(id, hashedPassword);
            return Ok();
        }
    }
}
