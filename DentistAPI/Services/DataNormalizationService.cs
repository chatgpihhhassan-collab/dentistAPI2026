using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DentistAPI.Services
{
    public class DataNormalizationService : IDataNormalizationService
    {
        private static readonly Regex HtmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex MultiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex CnicRegex = new Regex(@"^[0-9]{5}-[0-9]{7}-[0-9]{1}$", RegexOptions.Compiled);

        private static readonly HashSet<int> ValidAdultFdiTeeth = new HashSet<int>
        {
            11, 12, 13, 14, 15, 16, 17, 18,
            21, 22, 23, 24, 25, 26, 27, 28,
            31, 32, 33, 34, 35, 36, 37, 38,
            41, 42, 43, 44, 45, 46, 47, 48
        };

        private static readonly HashSet<int> ValidPediatricFdiTeeth = new HashSet<int>
        {
            51, 52, 53, 54, 55,
            61, 62, 63, 64, 65,
            71, 72, 73, 74, 75,
            81, 82, 83, 84, 85
        };

        /// <summary>
        /// Normalizes Pakistani phone numbers to E.164 (+923XXXXXXXXX).
        /// Accepts: 03001234567, 923001234567, +923001234567, 3001234567.
        /// </summary>
        public (bool IsValid, string Normalized, string? ErrorMessage) NormalizePakistaniPhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return (false, string.Empty, "Phone number is required.");
            }

            string cleaned = raw.Trim();
            bool hasLeadingPlus = cleaned.StartsWith("+");
            string digits = Regex.Replace(cleaned, @"\D", "");

            string e164;

            if (digits.StartsWith("923") && digits.Length == 12)
            {
                e164 = $"+{digits}";
            }
            else if (digits.StartsWith("03") && digits.Length == 11)
            {
                e164 = $"+92{digits.Substring(1)}";
            }
            else if (digits.StartsWith("3") && digits.Length == 10)
            {
                e164 = $"+92{digits}";
            }
            else if (hasLeadingPlus && digits.StartsWith("923") && digits.Length == 12)
            {
                e164 = $"+{digits}";
            }
            else
            {
                return (false, string.Empty, "Invalid Pakistani mobile number. Must be 11 digits (e.g. 03001234567 or +923001234567).");
            }

            return (true, e164, null);
        }

        /// <summary>
        /// Normalizes and validates Pakistani CNIC (13 digits: XXXXX-XXXXXXX-X).
        /// </summary>
        public (bool IsValid, string Normalized, string? ErrorMessage) NormalizeCnic(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return (true, string.Empty, null); // Optional field
            }

            string digits = Regex.Replace(raw.Trim(), @"\D", "");
            if (digits.Length != 13)
            {
                return (false, string.Empty, "Invalid CNIC. Must contain exactly 13 digits (XXXXX-XXXXXXX-X).");
            }

            string formatted = $"{digits.Substring(0, 5)}-{digits.Substring(5, 7)}-{digits.Substring(12, 1)}";
            if (!CnicRegex.IsMatch(formatted))
            {
                return (false, string.Empty, "Invalid CNIC format.");
            }

            return (true, formatted, null);
        }

        /// <summary>
        /// Sanitizes text input: strips HTML tags, removes javascript pseudo-protocols, collapses whitespace, and limits length.
        /// </summary>
        public string SanitizeText(string? input, int maxLength = 2000)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string str = HtmlTagRegex.Replace(input, string.Empty);
            str = Regex.Replace(str, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
            str = MultiSpaceRegex.Replace(str, " ").Trim();

            if (str.Length > maxLength)
            {
                str = str.Substring(0, maxLength);
            }

            return str;
        }

        /// <summary>
        /// Normalizes email addresses (lowercased, trimmed).
        /// </summary>
        public string NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            return email.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Enforces password complexity:
        /// - Minimum 8 characters
        /// - Maximum 72 characters (BCrypt limit)
        /// - At least 1 lowercase letter
        /// - At least 1 uppercase letter
        /// - At least 1 digit
        /// - At least 1 special character
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidatePasswordComplexity(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Password is required.");
            }

            if (password.Length < 8)
            {
                return (false, "Password must be at least 8 characters long.");
            }

            if (password.Length > 72)
            {
                return (false, "Password cannot exceed 72 characters.");
            }

            if (!password.Any(char.IsLower))
            {
                return (false, "Password must contain at least one lowercase letter.");
            }

            if (!password.Any(char.IsUpper))
            {
                return (false, "Password must contain at least one uppercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                return (false, "Password must contain at least one number.");
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return (false, "Password must contain at least one special character.");
            }

            return (true, null);
        }

        /// <summary>
        /// Validates tooth number against FDI World Dental Federation notation (11-48, 51-85).
        /// </summary>
        public bool IsValidFdiToothNumber(int toothNumber)
        {
            return ValidAdultFdiTeeth.Contains(toothNumber) || ValidPediatricFdiTeeth.Contains(toothNumber);
        }

        /// <summary>
        /// Validates card number using Luhn algorithm (Mod 10).
        /// </summary>
        public bool ValidateLuhnCardNumber(string? rawCardNumber)
        {
            if (string.IsNullOrWhiteSpace(rawCardNumber)) return false;

            string digits = Regex.Replace(rawCardNumber, @"\D", "");
            if (digits.Length < 13 || digits.Length > 19) return false;

            int sum = 0;
            bool shouldDouble = false;

            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int digit = digits[i] - '0';

                if (shouldDouble)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                shouldDouble = !shouldDouble;
            }

            return (sum % 10 == 0);
        }

        /// <summary>
        /// Normalizes dentition type: maps aliases like 'Permanent' to 'Adult', 'Child' to 'Pediatric'.
        /// Valid canonical types: 'Adult', 'Pediatric', 'Mixed'.
        /// </summary>
        public string NormalizeDentitionType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Adult";
            string trimmed = raw.Trim().ToLowerInvariant();

            return trimmed switch
            {
                "permanent" or "adult" or "adults" => "Adult",
                "pediatric" or "child" or "primary" or "deciduous" => "Pediatric",
                "mixed" or "transitional" => "Mixed",
                _ => "Adult"
            };
        }

        /// <summary>
        /// Robustly extracts patient age from unstructured intake text while preventing false positives
        /// from tooth numbers (e.g. "Tooth 14 broken"), addresses ("House 23, Street 5"), dates, or telephone numbers.
        /// </summary>
        public int? ExtractAge(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // 1. Explicit age keywords (e.g. "age 25", "25 years old", "25yo", "25 saal", "umar 25", "25 yrs")
            var explicitMatch = Regex.Match(text, @"\b(?:age|umar|umer|years?(\s*old)?|yo|y/o|yrs?|yr|saal|sal)\s*[:\-\s]?\s*(\d{1,3})\b|\b(\d{1,3})\s*(?:years?(\s*old)?|yo|y/o|yrs?|yr|saal|sal)\b", RegexOptions.IgnoreCase);
            if (explicitMatch.Success)
            {
                string val = !string.IsNullOrEmpty(explicitMatch.Groups[1].Value) ? explicitMatch.Groups[1].Value : explicitMatch.Groups[2].Value;
                if (int.TryParse(val, out int explicitAge) && explicitAge > 0 && explicitAge <= 120)
                {
                    return explicitAge;
                }
            }

            // 2. Number adjacent to gender marker (e.g. "25 male", "female 30", "35yo male", "28 M", "M 28")
            var genderAdjacentMatch = Regex.Match(text, @"\b(\d{1,3})\s*[:\-\s]?\s*(?:male|female|man|woman|boy|girl|mard|aurat)\b|\b(?:male|female|man|woman|boy|girl|mard|aurat)\s*[:\-\s]?\s*(\d{1,3})\b", RegexOptions.IgnoreCase);
            if (genderAdjacentMatch.Success)
            {
                string val = !string.IsNullOrEmpty(genderAdjacentMatch.Groups[1].Value) ? genderAdjacentMatch.Groups[1].Value : genderAdjacentMatch.Groups[2].Value;
                if (int.TryParse(val, out int genderAge) && genderAge > 0 && genderAge <= 120)
                {
                    return genderAge;
                }
            }

            // 3. Fallback: Standalone number ONLY IF NOT qualified by tooth, address, phone, or date context
            var numberMatches = Regex.Matches(text, @"\b(\d{1,3})\b");
            foreach (Match m in numberMatches)
            {
                if (int.TryParse(m.Value, out int candidate) && candidate > 0 && candidate <= 120)
                {
                    // Context window around match (-30 chars to +30 chars)
                    int start = Math.Max(0, m.Index - 30);
                    int length = Math.Min(text.Length - start, m.Length + 60);
                    string surrounding = text.Substring(start, length).ToLowerInvariant();

                    // Check if preceded or followed by tooth / clinical keywords
                    bool isToothContext = Regex.IsMatch(surrounding, @"(?:tooth|teeth|#|dant|molar|premolar|incisor|canine|extraction|decay|filling|rct|cavity|crown)\s*#?\s*" + m.Value + @"\b|\b" + m.Value + @"\s*(?:tooth|teeth|molar|premolar|incisor|canine)", RegexOptions.IgnoreCase);
                    if (isToothContext) continue;

                    // Check if preceded or followed by address / street keywords
                    bool isAddressContext = Regex.IsMatch(surrounding, @"(?:house|h#|h\s*no|street|st#?|st\b|road|rd\b|sector|sec\b|block|blk\b|flat|apt|apartment|plot|unit|lane|floor|suite|g-\d|f-\d|i-\d|h-\d)\s*#?\s*" + m.Value + @"\b|\b" + m.Value + @"\s*(?:street|st|road|rd|sector|block|floor|flat|apt)", RegexOptions.IgnoreCase);
                    if (isAddressContext) continue;

                    // Check if it's part of a date or time or ordinal (e.g. "14th", "10:30")
                    bool isTimeOrDateContext = Regex.IsMatch(surrounding, @"(?:\d{1,2}:\d{2}|\d{1,2}(?:st|nd|rd|th)|\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\s*" + m.Value + @"\b|\b" + m.Value + @"\s*(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*)", RegexOptions.IgnoreCase);
                    if (isTimeOrDateContext) continue;

                    // Plausible standalone age: either the entire text is the number, or format like "Name, 25"
                    if (text.Trim() == m.Value || Regex.IsMatch(text.Trim(), @"^[a-zA-Z\s.'-]+,\s*" + m.Value + @"\s*$"))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
