using System;
using System.Text.RegularExpressions;

namespace DentistAPI.Services
{
    /// <summary>
    /// Pakistani (+92) phone normalizer, validator, and OpenWA chatId builder.
    /// Converts raw user inputs (e.g. "0300-1234567", "+92 300 1234567") into E.164 and OpenWA @c.us chat IDs.
    /// </summary>
    public static class WhatsAppPhoneNormalizer
    {
        public static (bool IsValid, string NormalizedDigits, string ChatId, string FormattedDisplay, string? Error) NormalizePakistani(string? rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone))
            {
                return (false, string.Empty, string.Empty, string.Empty, "Phone number is required.");
            }

            // 1. Remove all spaces, dashes, parentheses, plus signs, dots, slashes
            string cleaned = Regex.Replace(rawPhone.Trim(), @"[^\d]", "");

            if (string.IsNullOrEmpty(cleaned))
            {
                return (false, string.Empty, string.Empty, rawPhone, "No valid digits found in phone number.");
            }

            // 2. Handle leading 0092: 00923001234567 -> 923001234567
            if (cleaned.StartsWith("0092") && cleaned.Length == 14)
            {
                cleaned = cleaned.Substring(2);
            }
            // 3. Handle leading 0: 03001234567 -> 923001234567
            else if (cleaned.StartsWith("0") && cleaned.Length == 11)
            {
                cleaned = "92" + cleaned.Substring(1);
            }
            // 4. Handle 10-digit format without leading 0: 3001234567 -> 923001234567
            else if (cleaned.StartsWith("3") && cleaned.Length == 10)
            {
                cleaned = "92" + cleaned;
            }

            // 5. Pakistani mobile validation: Must start with 923 and be exactly 12 digits
            bool isValid = cleaned.StartsWith("923") && cleaned.Length == 12;
            if (!isValid)
            {
                return (false, cleaned, string.Empty, rawPhone, 
                    "Invalid Pakistani mobile number. Must be 11 digits starting with 03 (e.g. 03001234567) or +923001234567.");
            }

            string chatId = $"{cleaned}@c.us";
            string formattedDisplay = $"+{cleaned.Substring(0, 2)} {cleaned.Substring(2, 3)} {cleaned.Substring(5)}";

            return (true, cleaned, chatId, formattedDisplay, null);
        }

        /// <summary>
        /// Builds a direct WhatsApp web/app link (wa.me) for clicking from the UI.
        /// </summary>
        public static string BuildDirectChatUrl(string rawPhone, string? prefilledMessage = null)
        {
            var norm = NormalizePakistani(rawPhone);
            if (!norm.IsValid) return string.Empty;

            string url = $"https://wa.me/{norm.NormalizedDigits}";
            if (!string.IsNullOrWhiteSpace(prefilledMessage))
            {
                url += $"?text={Uri.EscapeDataString(prefilledMessage)}";
            }
            return url;
        }
    }
}
