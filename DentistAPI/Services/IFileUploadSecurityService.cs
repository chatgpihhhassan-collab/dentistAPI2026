using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DentistAPI.Services
{
    public enum FileUploadCategory
    {
        ProfileImage,
        Radiograph,
        AudioRecording
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string SafeFileName { get; set; } = string.Empty;
        public string CanonicalMimeType { get; set; } = string.Empty;
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    }

    public interface IFileUploadSecurityService
    {
        Task<FileValidationResult> ValidateAndExtractAsync(IFormFile? file, FileUploadCategory category);
        FileValidationResult ValidateBytes(byte[]? bytes, string clientFileName, string clientContentType, FileUploadCategory category);
    }
}
