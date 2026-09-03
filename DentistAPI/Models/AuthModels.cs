using System;

namespace DentistAPI.Models
{
    public class Doctor
    {
        public int DoctorID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Region { get; set; } = "NZ";
        public DateTime CreatedAt { get; set; }
        public bool IsSuperAdmin { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Region { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public int DoctorID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Region { get; set; }
        public bool IsSuperAdmin { get; set; }
    }

    public class UpdateDoctorRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Region { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string Password { get; set; }
    }
}
