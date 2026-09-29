using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    public class Doctor
    {
        public int DoctorID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Region { get; set; } = "NZ";
        public DateTime CreatedAt { get; set; }
        public bool IsSuperAdmin { get; set; }

        // Expanded Profile & Professional History Fields
        public string? Specialization { get; set; } = "General Dental Surgeon";
        public string? Title { get; set; } = "BDS, RDS";
        public int YearsOfExperience { get; set; } = 5;
        public string? Biography { get; set; }
        public string? OrganizationWorkHistory { get; set; } // JSON or structured text of hospital & organization affiliations
        public string? Education { get; set; }
        public string? Certifications { get; set; }
        public decimal ConsultationFee { get; set; } = 100.00m;
        public string? ProfileImageUrl { get; set; }
        public string? Languages { get; set; } = "English, Urdu";
        public decimal Rating { get; set; } = 4.90m;
        public int ReviewCount { get; set; } = 25;
        public bool IsActive { get; set; } = true;

        // Organization / Hospital Affiliation
        public int? OrganizationID { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationLogoUrl { get; set; }
        public string? OrganizationCity { get; set; }
        public string? HospitalDepartment { get; set; } = "Department of Oral Surgery & Dentistry";
    }

    public class Organization
    {
        public int OrganizationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Type { get; set; } = "Hospital"; // Hospital, Dental Clinic, Surgical Institute, Academic Center
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "NZ";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Accreditation { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Joined statistics
        public int DoctorCount { get; set; }
        public List<Doctor>? Doctors { get; set; }
    }

    public class DoctorOrganization
    {
        public int DoctorOrganizationID { get; set; }
        public int DoctorID { get; set; }
        public int OrganizationID { get; set; }
        public string? RoleInOrg { get; set; } = "Attending Specialist";
        public string? Department { get; set; }
        public string? ConsultationDays { get; set; }
        public bool IsPrimary { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Joined details
        public string? DoctorName { get; set; }
        public string? OrganizationName { get; set; }
    }

    public class CreateOrganizationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Type { get; set; } = "Hospital";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "NZ";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Accreditation { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int>? InitialDoctorIDs { get; set; }
        public string? InitialDepartment { get; set; } = "Department of Oral Surgery & Dentistry";
    }

    public class UpdateOrganizationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Type { get; set; } = "Hospital";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = "NZ";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Accreditation { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AssignDoctorToOrgRequest
    {
        public int DoctorID { get; set; }
        public string? RoleInOrg { get; set; } = "Attending Specialist";
        public string? Department { get; set; } = "Department of Oral Surgery & Dentistry";
        public string? ConsultationDays { get; set; } = "Mon - Fri";
        public bool IsPrimary { get; set; } = true;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Region { get; set; } = "NZ";
        public string? Specialization { get; set; }
        public string? Title { get; set; }
        public int YearsOfExperience { get; set; } = 5;
        public string? Biography { get; set; }
        public string? OrganizationWorkHistory { get; set; }
        public string? Education { get; set; }
        public string? Certifications { get; set; }
        public decimal ConsultationFee { get; set; } = 100.00m;
        public string? ProfileImageUrl { get; set; }
        public string? Languages { get; set; } = "English, Urdu";
        public int? OrganizationID { get; set; }
        public string? HospitalDepartment { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public int DoctorID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Region { get; set; } = "NZ";
        public bool IsSuperAdmin { get; set; }
        public string? Specialization { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int? OrganizationID { get; set; }
        public string? OrganizationName { get; set; }
    }

    public class UpdateDoctorRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Region { get; set; } = "NZ";
        public string? Specialization { get; set; }
        public string? Title { get; set; }
        public int YearsOfExperience { get; set; } = 5;
        public string? Biography { get; set; }
        public string? OrganizationWorkHistory { get; set; }
        public string? Education { get; set; }
        public string? Certifications { get; set; }
        public decimal ConsultationFee { get; set; } = 100.00m;
        public string? ProfileImageUrl { get; set; }
        public string? Languages { get; set; } = "English, Urdu";
        public decimal Rating { get; set; } = 4.90m;
        public int ReviewCount { get; set; } = 25;
        public bool IsActive { get; set; } = true;
        public int? OrganizationID { get; set; }
        public string? HospitalDepartment { get; set; }
    }

    public class CreateDoctorProfileRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Region { get; set; } = "NZ";
        public string? Specialization { get; set; } = "General Dental Surgeon";
        public string? Title { get; set; } = "BDS, RDS";
        public int YearsOfExperience { get; set; } = 5;
        public string? Biography { get; set; }
        public string? OrganizationWorkHistory { get; set; }
        public string? Education { get; set; }
        public string? Certifications { get; set; }
        public decimal ConsultationFee { get; set; } = 100.00m;
        public string? ProfileImageUrl { get; set; }
        public string? Languages { get; set; } = "English, Urdu";
        public decimal Rating { get; set; } = 4.90m;
        public int ReviewCount { get; set; } = 10;
        public bool IsActive { get; set; } = true;
        public int? OrganizationID { get; set; }
        public string? HospitalDepartment { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string Password { get; set; } = string.Empty;
    }

    public class ToggleDoctorStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class ToggleOrganizationStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
