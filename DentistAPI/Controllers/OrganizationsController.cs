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
    public class OrganizationsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITokenService _tokenService;

        public OrganizationsController(DentalRepository repository, ITokenService tokenService)
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

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var orgs = await _repository.GetAllOrganizationsAsync();
            return Ok(orgs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrganization(int id)
        {
            var org = await _repository.GetOrganizationByIdAsync(id);
            if (org == null) return NotFound(new { message = $"Organization with ID {id} not found." });
            return Ok(org);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Superadmin can register organizations." });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organization name is required." });
            }

            int id = await _repository.CreateOrganizationAsync(request);

            if (request.InitialDoctorIDs != null && request.InitialDoctorIDs.Count > 0)
            {
                foreach (var docId in request.InitialDoctorIDs)
                {
                    try
                    {
                        await _repository.AssignDoctorToOrganizationAsync(
                            id,
                            docId,
                            "Attending Specialist",
                            request.InitialDepartment ?? "Department of Oral Surgery & Dentistry",
                            "Mon - Fri",
                            true
                        );
                    }
                    catch { /* Continue linking other doctors */ }
                }
            }

            var created = await _repository.GetOrganizationByIdAsync(id);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Superadmin can edit organizations." });
            }

            var existing = await _repository.GetOrganizationByIdAsync(id);
            if (existing == null) return NotFound(new { message = $"Organization with ID {id} not found." });

            await _repository.UpdateOrganizationAsync(id, request);
            return Ok(new { message = "Organization details updated successfully." });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] ToggleOrganizationStatusRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Superadmin can change organization status." });
            }

            await _repository.ToggleOrganizationStatusAsync(id, request.IsActive);
            return Ok(new { message = $"Organization status set to {(request.IsActive ? "Active" : "Inactive")}." });
        }

        [HttpPost("{id}/assign-doctor")]
        public async Task<IActionResult> AssignDoctor(int id, [FromBody] AssignDoctorToOrgRequest request)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Superadmin can assign doctors to organizations." });
            }

            var org = await _repository.GetOrganizationByIdAsync(id);
            if (org == null) return NotFound(new { message = "Organization not found." });

            var doctor = await _repository.GetDoctorByIdAsync(request.DoctorID);
            if (doctor == null) return NotFound(new { message = "Doctor not found." });

            await _repository.AssignDoctorToOrganizationAsync(
                id, 
                request.DoctorID, 
                request.RoleInOrg ?? "Attending Specialist", 
                request.Department ?? "Department of Oral Surgery & Dentistry", 
                request.ConsultationDays ?? "Mon - Fri", 
                request.IsPrimary
            );

            return Ok(new { message = $"Dr. {doctor.FirstName} {doctor.LastName} assigned to {org.Name} successfully." });
        }

        [HttpDelete("{id}/remove-doctor/{doctorId}")]
        public async Task<IActionResult> RemoveDoctor(int id, int doctorId)
        {
            if (!IsCallerSuperAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied. Only Superadmin can remove doctors from organizations." });
            }

            await _repository.RemoveDoctorFromOrganizationAsync(id, doctorId);
            return Ok(new { message = "Doctor removed from organization successfully." });
        }
    }
}
