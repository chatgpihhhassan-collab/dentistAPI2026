using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    /// <summary>
    /// Standardized JSON Error Envelope for all Dentia API validation failures.
    /// Format: { "success": false, "error": { "code": "VALIDATION_ERROR", "message": "...", "fields": { ... } } }
    /// </summary>
    public class ValidationErrorEnvelope
    {
        public bool Success { get; set; } = false;
        public ValidationErrorDetail Error { get; set; } = new ValidationErrorDetail();

        public ValidationErrorEnvelope()
        {
        }

        public ValidationErrorEnvelope(string message, Dictionary<string, string>? fields = null, string code = "VALIDATION_ERROR")
        {
            Success = false;
            Error = new ValidationErrorDetail
            {
                Code = code,
                Message = message,
                Fields = fields ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        public static ValidationErrorEnvelope Create(string message, string fieldName, string fieldError)
        {
            var envelope = new ValidationErrorEnvelope(message);
            envelope.Error.Fields[fieldName] = fieldError;
            return envelope;
        }
    }

    public class ValidationErrorDetail
    {
        public string Code { get; set; } = "VALIDATION_ERROR";
        public string Message { get; set; } = "Validation failed for one or more fields.";
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
