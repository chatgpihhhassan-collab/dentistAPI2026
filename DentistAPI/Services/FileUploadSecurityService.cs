using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    public class FileUploadSecurityService : IFileUploadSecurityService
    {
        private readonly ILogger<FileUploadSecurityService> _logger;

        // Size limits in bytes
        public const long MaxProfileImageSize = 5 * 1024 * 1024;    // 5 MB
        public const long MaxRadiographSize = 15 * 1024 * 1024;     // 15 MB
        public const long MaxAudioSize = 25 * 1024 * 1024;          // 25 MB

        // Allowed extensions (lowercased with dot)
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedRadiographExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".dcm", ".dicom" };
        private static readonly string[] AllowedAudioExtensions = { ".webm", ".wav", ".mp3", ".ogg", ".m4a" };

        // Prohibited extensions that must be rejected regardless of MIME type
        private static readonly string[] DangerousExtensions =
        {
            ".exe", ".dll", ".bat", ".cmd", ".sh", ".bash", ".ps1", ".vbs",
            ".php", ".php3", ".php4", ".phtml", ".aspx", ".asp", ".jsp",
            ".cgi", ".pl", ".py", ".rb", ".jar", ".war", ".svg", ".html",
            ".htm", ".xhtml", ".xml", ".js", ".mjs", ".msi", ".com", ".scr"
        };

        public FileUploadSecurityService(ILogger<FileUploadSecurityService> logger)
        {
            _logger = logger;
        }

        public async Task<FileValidationResult> ValidateAndExtractAsync(IFormFile? file, FileUploadCategory category)
        {
            if (file == null || file.Length == 0)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "No file was uploaded or the uploaded file is empty."
                };
            }

            // Check size quota
            long maxSize = category switch
            {
                FileUploadCategory.ProfileImage => MaxProfileImageSize,
                FileUploadCategory.Radiograph => MaxRadiographSize,
                FileUploadCategory.AudioRecording => MaxAudioSize,
                _ => MaxProfileImageSize
            };

            if (file.Length > maxSize)
            {
                long maxMb = maxSize / (1024 * 1024);
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size ({file.Length / (1024 * 1024.0):F1} MB) exceeds maximum allowed limit of {maxMb} MB."
                };
            }

            // Read stream into byte buffer
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            return ValidateBytes(fileBytes, file.FileName, file.ContentType, category);
        }

        public FileValidationResult ValidateBytes(byte[]? bytes, string clientFileName, string clientContentType, FileUploadCategory category)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File content is empty."
                };
            }

            // Sanitize raw filename to prevent Directory Traversal
            string strippedFileName = Path.GetFileName(clientFileName ?? "unnamed_file");
            string extension = Path.GetExtension(strippedFileName).ToLowerInvariant();

            // 1. Immediate rejection of dangerous extensions
            if (DangerousExtensions.Contains(extension))
            {
                _logger.LogWarning("[SECURITY] Blocked upload attempt with dangerous extension: {Extension}", extension);
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File type '{extension}' is strictly prohibited for security reasons."
                };
            }

            // 2. Validate category allowed extensions
            string[] allowedExtensions = category switch
            {
                FileUploadCategory.ProfileImage => AllowedImageExtensions,
                FileUploadCategory.Radiograph => AllowedRadiographExtensions,
                FileUploadCategory.AudioRecording => AllowedAudioExtensions,
                _ => AllowedImageExtensions
            };

            // Allow fallback extension detection if missing from filename for audio recordings (e.g. MediaRecorder webm blobs)
            if (string.IsNullOrEmpty(extension) && category == FileUploadCategory.AudioRecording)
            {
                extension = ".webm";
            }

            if (!allowedExtensions.Contains(extension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid file extension '{extension}'. Allowed extensions for this upload: {string.Join(", ", allowedExtensions)}"
                };
            }

            // 3. Deep Magic Bytes Signature Inspection
            var (isMagicValid, canonicalMime) = VerifyMagicBytes(bytes, extension, category);
            if (!isMagicValid)
            {
                _logger.LogWarning("[SECURITY] File header magic-byte mismatch for file '{FileName}' claimed as '{Extension}'", strippedFileName, extension);
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Security verification failed: File content does not match its claimed file signature/format."
                };
            }

            // 4. Generate cryptographically safe unique filename
            string safeBaseName = Regex.Replace(Path.GetFileNameWithoutExtension(strippedFileName), @"[^a-zA-Z0-9_\-]", "_");
            if (string.IsNullOrWhiteSpace(safeBaseName)) safeBaseName = "file";
            if (safeBaseName.Length > 30) safeBaseName = safeBaseName.Substring(0, 30);

            string safeFileName = $"{safeBaseName}_{Guid.NewGuid():N}{extension}";

            return new FileValidationResult
            {
                IsValid = true,
                SafeFileName = safeFileName,
                CanonicalMimeType = canonicalMime,
                FileBytes = bytes
            };
        }

        private static (bool IsValid, string CanonicalMime) VerifyMagicBytes(byte[] bytes, string extension, FileUploadCategory category)
        {
            if (bytes.Length < 4) return (false, string.Empty);

            // JPEG check: FF D8 FF
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return (extension == ".jpg" || extension == ".jpeg", "image/jpeg");
            }

            // PNG check: 89 50 4E 47 0D 0A 1A 0A
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            {
                return (extension == ".png", "image/png");
            }

            // WebP check: RIFF....WEBP (Bytes 0-3: 52 49 46 46, Bytes 8-11: 57 45 42 50)
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            {
                return (extension == ".webp", "image/webp");
            }

            // DICOM check: DICM header at byte offset 128 (standard DICOM preamble) or offset 0
            if (category == FileUploadCategory.Radiograph)
            {
                if (bytes.Length >= 132 &&
                    bytes[128] == 0x44 && bytes[129] == 0x49 && bytes[130] == 0x43 && bytes[131] == 0x4D)
                {
                    return (extension == ".dcm" || extension == ".dicom", "application/dicom");
                }
                if (bytes.Length >= 4 &&
                    bytes[0] == 0x44 && bytes[1] == 0x49 && bytes[2] == 0x43 && bytes[3] == 0x4D)
                {
                    return (extension == ".dcm" || extension == ".dicom", "application/dicom");
                }
            }

            // Audio: WebM / Matroska EBML: 1A 45 DF A3
            if (bytes.Length >= 4 &&
                bytes[0] == 0x1A && bytes[1] == 0x45 && bytes[2] == 0xDF && bytes[3] == 0xA3)
            {
                return (extension == ".webm", "audio/webm");
            }

            // Audio: WAV RIFF....WAVE (Bytes 0-3: 52 49 46 46, Bytes 8-11: 57 41 56 45)
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x41 && bytes[10] == 0x56 && bytes[11] == 0x45)
            {
                return (extension == ".wav", "audio/wav");
            }

            // Audio: MP3 ID3 header: 49 44 33
            if (bytes.Length >= 3 && bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)
            {
                return (extension == ".mp3", "audio/mpeg");
            }

            // Audio: MP3 frame sync (FF FB, FF F3, FF F2)
            if (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] == 0xFB || bytes[1] == 0xF3 || bytes[1] == 0xF2))
            {
                return (extension == ".mp3", "audio/mpeg");
            }

            // Audio: Ogg container: 4F 67 67 53 (OggS)
            if (bytes.Length >= 4 &&
                bytes[0] == 0x4F && bytes[1] == 0x67 && bytes[2] == 0x67 && bytes[3] == 0x53)
            {
                return (extension == ".ogg", "audio/ogg");
            }

            // Audio: M4A / MP4 container: ftyp at bytes 4..7
            if (bytes.Length >= 8 &&
                bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
            {
                return (extension == ".m4a" || extension == ".mp4", "audio/mp4");
            }

            return (false, string.Empty);
        }
    }
}
