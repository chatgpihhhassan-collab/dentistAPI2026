using System;
using System.Collections.Generic;

namespace DentistAPI.Services
{
    public interface IDataNormalizationService
    {
        (bool IsValid, string Normalized, string? ErrorMessage) NormalizePakistaniPhone(string? raw);
        (bool IsValid, string Normalized, string? ErrorMessage) NormalizeCnic(string? raw);
        string SanitizeText(string? input, int maxLength = 2000);
        string NormalizeEmail(string? email);
        (bool IsValid, string? ErrorMessage) ValidatePasswordComplexity(string? password);
        bool IsValidFdiToothNumber(int toothNumber);
        bool ValidateLuhnCardNumber(string? rawCardNumber);
        string NormalizeDentitionType(string? raw);
        int? ExtractAge(string? text);
    }
}
