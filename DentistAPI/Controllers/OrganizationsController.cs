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

            if (request == null)
            {
                return BadRequest(new { message = "Request body cannot be empty." });
            }

            // 1. Name validation (VAL-003, VAL-006)
            request.Name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organization Name is required and cannot be empty or whitespace only." });
            }
            if (request.Name.Length > 150)
            {
                return BadRequest(new { message = "Organization Name cannot exceed 150 characters." });
            }

            // 2. Duplicate organization name check (ORG-001)
            var existingOrgs = (await _repository.GetAllOrganizationsAsync()).ToList();
            if (existingOrgs.Any(o => string.Equals(o.Name.Trim(), request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = $"An organization with the name '{request.Name}' already exists. Duplicate organization names are not allowed." });
            }

            // 3. Unique slug generation (ORG-001)
            string rawSlug = string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug;
            string baseSlug = System.Text.RegularExpressions.Regex.Replace(rawSlug.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "org";
            string uniqueSlug = baseSlug;
            int slugIndex = 1;
            while (existingOrgs.Any(o => string.Equals(o.Slug, uniqueSlug, StringComparison.OrdinalIgnoreCase)))
            {
                uniqueSlug = $"{baseSlug}-{slugIndex++}";
            }
            request.Slug = uniqueSlug;

            // 4. Field validations (VAL-003, VAL-007)
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                request.City = request.City.Trim();
                if (request.City.Length > 100) return BadRequest(new { message = "City cannot exceed 100 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.City, @"^[a-zA-Z\s.'-]{1,100}$"))
                {
                    return BadRequest(new { message = "City can only contain letters, spaces, hyphens, and apostrophes." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Accreditation))
            {
                request.Accreditation = request.Accreditation.Trim();
                if (request.Accreditation.Length > 150) return BadRequest(new { message = "Accreditation cannot exceed 150 characters." });
            }
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                request.Address = request.Address.Trim();
                if (request.Address.Length > 255) return BadRequest(new { message = "Address cannot exceed 255 characters." });
            }
            if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                request.LogoUrl = request.LogoUrl.Trim();
                if (request.LogoUrl.Length > 500) return BadRequest(new { message = "Logo URL cannot exceed 500 characters." });
                if (!Uri.TryCreate(request.LogoUrl, UriKind.Absolute, out var uriResult) ||
                    !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    return BadRequest(new { message = "Logo URL must be a valid HTTP or HTTPS web address." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                request.Phone = request.Phone.Trim();
                if (request.Phone.Length > 50) return BadRequest(new { message = "Phone number cannot exceed 50 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^(\+?[0-9\s\-().]{7,25})$"))
                {
                    return BadRequest(new { message = "Phone number must be a valid telephone format." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                request.Email = request.Email.Trim();
                if (request.Email.Length > 100) return BadRequest(new { message = "Email cannot exceed 100 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { message = "Please enter a valid email address." });
                }
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

            if (request == null)
            {
                return BadRequest(new { message = "Request body cannot be empty." });
            }

            var existing = await _repository.GetOrganizationByIdAsync(id);
            if (existing == null) return NotFound(new { message = $"Organization with ID {id} not found." });

            // 1. Name validation (VAL-003, VAL-006)
            request.Name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organization Name is required and cannot be empty or whitespace only." });
            }
            if (request.Name.Length > 150)
            {
                return BadRequest(new { message = "Organization Name cannot exceed 150 characters." });
            }

            // 2. Duplicate organization name check against other organizations (ORG-001)
            var allOrgs = (await _repository.GetAllOrganizationsAsync()).ToList();
            if (allOrgs.Any(o => o.OrganizationID != id && string.Equals(o.Name.Trim(), request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { message = $"Another organization with the name '{request.Name}' already exists." });
            }

            // 3. Regenerate slug on rename (ORG-001)
            string rawSlug = string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug;
            string baseSlug = System.Text.RegularExpressions.Regex.Replace(rawSlug.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "org";
            string uniqueSlug = baseSlug;
            int slugIndex = 1;
            while (allOrgs.Any(o => o.OrganizationID != id && string.Equals(o.Slug, uniqueSlug, StringComparison.OrdinalIgnoreCase)))
            {
                uniqueSlug = $"{baseSlug}-{slugIndex++}";
            }
            request.Slug = uniqueSlug;

            // 4. Field validations (VAL-003, VAL-007, ORG-002)
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                request.City = request.City.Trim();
                if (request.City.Length > 100) return BadRequest(new { message = "City cannot exceed 100 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.City, @"^[a-zA-Z\s.'-]{1,100}$"))
                {
                    return BadRequest(new { message = "City can only contain letters, spaces, hyphens, and apostrophes." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Accreditation))
            {
                request.Accreditation = request.Accreditation.Trim();
                if (request.Accreditation.Length > 150) return BadRequest(new { message = "Accreditation cannot exceed 150 characters." });
            }
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                request.Address = request.Address.Trim();
                if (request.Address.Length > 255) return BadRequest(new { message = "Address cannot exceed 255 characters." });
            }
            if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                request.LogoUrl = request.LogoUrl.Trim();
                if (request.LogoUrl.Length > 500) return BadRequest(new { message = "Logo URL cannot exceed 500 characters." });
                if (!Uri.TryCreate(request.LogoUrl, UriKind.Absolute, out var uriResult) ||
                    !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    return BadRequest(new { message = "Logo URL must be a valid HTTP or HTTPS web address." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                request.Phone = request.Phone.Trim();
                if (request.Phone.Length > 50) return BadRequest(new { message = "Phone number cannot exceed 50 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^(\+?[0-9\s\-().]{7,25})$"))
                {
                    return BadRequest(new { message = "Phone number must be a valid telephone format." });
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                request.Email = request.Email.Trim();
                if (request.Email.Length > 100) return BadRequest(new { message = "Email cannot exceed 100 characters." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { message = "Please enter a valid email address." });
                }
            }

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

            if (request == null || request.DoctorID <= 0)
            {
                return BadRequest(new { message = "Please select a valid clinician to assign." });
            }

            var org = await _repository.GetOrganizationByIdAsync(id);
            if (org == null) return NotFound(new { message = "Organization not found." });

            var doctor = await _repository.GetDoctorByIdAsync(request.DoctorID);
            if (doctor == null) return NotFound(new { message = "Doctor record not found." });

            // Enforce role check: Receptionists and SuperAdmin cannot be assigned as doctors (DATA-001)
            if (string.Equals(doctor.Role, "Receptionist", StringComparison.OrdinalIgnoreCase) || doctor.IsSuperAdmin)
            {
                return BadRequest(new { message = "Receptionists and SuperAdmin accounts cannot be assigned as hospital clinicians." });
            }

            // Enforce duplicate check: doctor already assigned (ORG-004)
            bool isAlreadyAssigned = await _repository.IsDoctorAssignedToOrganizationAsync(id, request.DoctorID);
            if (isAlreadyAssigned)
            {
                return BadRequest(new { message = $"Dr. {doctor.FirstName} {doctor.LastName} is already assigned to {org.Name}. Duplicate assignments are not permitted." });
            }

            // Input validations (ORG-004, VAL-003, VAL-006)
            request.RoleInOrg = request.RoleInOrg?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(request.RoleInOrg))
            {
                return BadRequest(new { message = "Clinical role is required." });
            }
            if (request.RoleInOrg.Length > 100)
            {
                return BadRequest(new { message = "Clinical role cannot exceed 100 characters." });
            }

            if (request.Department != null)
            {
                request.Department = request.Department.Trim();
                if (request.Department.Length > 100) return BadRequest(new { message = "Department cannot exceed 100 characters." });
            }

            request.ConsultationDays = request.ConsultationDays?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(request.ConsultationDays))
            {
                return BadRequest(new { message = "Consultation days are required." });
            }
            if (request.ConsultationDays.Length > 100)
            {
                return BadRequest(new { message = "Consultation days cannot exceed 100 characters." });
            }

            await _repository.AssignDoctorToOrganizationAsync(
                id, 
                request.DoctorID, 
                request.RoleInOrg, 
                request.Department ?? "Department of Oral Surgery & Dentistry", 
                request.ConsultationDays, 
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
