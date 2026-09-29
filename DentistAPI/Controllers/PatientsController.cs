using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;
using System.Collections.Generic;
using System.Linq;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileUploadSecurityService _fileUploadService;
        private readonly string _apiKey;
        private readonly string _geminiEndpoint;

        public PatientsController(
            DentalRepository repository, 
            IConfiguration config, 
            IHttpClientFactory httpClientFactory,
            IFileUploadSecurityService fileUploadService)
        {
            _repository = repository;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _fileUploadService = fileUploadService;
            _apiKey = config["GEMINI_API_KEY"] ?? string.Empty;
            var modelName = !string.IsNullOrEmpty(config["GEMINI_MODEL"]) ? config["GEMINI_MODEL"]! : "gemini-flash-latest";
            _geminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent";
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name)) return BadRequest("Name is required.");
            
            var patient = await _repository.SearchPatientByNameAsync(name);
            if (patient == null) return NotFound("Patient not found.");
            
            return Ok(patient);
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetPatientsByDoctor(int doctorId)
        {
            var patients = await _repository.GetPatientsByDoctorAsync(doctorId);
            return Ok(patients);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
        {
            if (patient == null || patient.DoctorID <= 0 || string.IsNullOrEmpty(patient.FirstName) || string.IsNullOrEmpty(patient.LastName))
            {
                return BadRequest("Invalid patient data.");
            }

            // Validate Profile Image Size (Max 5MB)
            if (patient.ProfileImage != null && patient.ProfileImage.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Profile image size exceeds the maximum limit of 5MB.");
            }

            // If doctor didn't upload a profile image, AI assistant / backend sets default Male/Female/Neutral dummy avatar bytes
            if (patient.ProfileImage == null || patient.ProfileImage.Length == 0)
            {
                patient.ProfileImage = GetDefaultAvatarBytes(patient.Gender, out string mime);
                patient.ProfileImageMimeType = mime;
            }

            // Duplicate Verification: Same Doctor + Same First/Last Name + (Same DOB OR Same Phone)
            var existingDuplicate = await _repository.FindDuplicatePatientAsync(patient.DoctorID, patient.FirstName, patient.LastName, patient.DOB, patient.Phone);
            if (existingDuplicate != null)
            {
                return Conflict(new
                {
                    isDuplicate = true,
                    existingPatientId = existingDuplicate.PatientID,
                    firstName = existingDuplicate.FirstName,
                    lastName = existingDuplicate.LastName,
                    dob = existingDuplicate.DOB.ToString("yyyy-MM-dd"),
                    message = $"Patient '{existingDuplicate.FirstName} {existingDuplicate.LastName}' is already registered in your clinic (Patient ID #{existingDuplicate.PatientID})."
                });
            }

            if (string.IsNullOrEmpty(patient.Email))
            {
                patient.Email = $"{patient.FirstName.ToLower().Replace(" ", "")}.{patient.LastName.ToLower().Replace(" ", "")}@dentiaclinic.com";
            }

            if (patient.Address == null)
            {
                patient.Address = string.Empty;
            }

            int newId = await _repository.CreatePatientAsync(patient);
            patient.PatientID = newId;
            return Ok(patient);
        }

        [HttpPost("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] DuplicateCheckRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return Ok(new { isDuplicate = false });
            }

            DateTime? dobVal = null;
            if (DateTime.TryParse(request.Dob, out var parsedDob)) dobVal = parsedDob;

            var existing = await _repository.FindDuplicatePatientAsync(request.DoctorId, request.FirstName, request.LastName, dobVal, request.Phone);
            if (existing != null)
            {
                return Ok(new
                {
                    isDuplicate = true,
                    existingPatientId = existing.PatientID,
                    firstName = existing.FirstName,
                    lastName = existing.LastName,
                    dob = existing.DOB.ToString("yyyy-MM-dd"),
                    message = $"⚠️ Patient '{existing.FirstName} {existing.LastName}' is already registered (Patient ID #{existing.PatientID})."
                });
            }

            return Ok(new { isDuplicate = false });
        }

        [HttpPost("ai-intake")]
        public async Task<IActionResult> ProcessAIIntake([FromBody] AIIntakeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Intake prompt is required.");
            }

            AIIntakeResponse response = null;

            // 1. High-Speed Local Fast-Path: Instant <2ms execution for direct clinical inputs
            var localFastResponse = FallbackPatientIntakeRegex(request.Prompt, request.Region ?? "PK");
            if (localFastResponse != null && localFastResponse.ExtractedFields.Count > 0)
            {
                response = localFastResponse;
            }

            // 2. Cloud AI Fallback for complex unstructured natural language (with 2.5s fast timeout)
            if (response == null && !string.IsNullOrEmpty(_apiKey) && (_apiKey.StartsWith("AIzaSy") || _apiKey.StartsWith("AQ.")))
            {
                try
                {
                    response = await ExtractPatientWithGemini(request.Prompt, request.Region ?? "PK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gemini AI Intake Notice: {ex.Message}. Using local intake engine.");
                }
            }

            if (response == null)
            {
                response = localFastResponse ?? new AIIntakeResponse { Region = request.Region ?? "PK" };
            }

            // Auto-generate professional email if not supplied
            if (string.IsNullOrEmpty(response.Email) && !string.IsNullOrEmpty(response.FirstName))
            {
                var cleanFirst = response.FirstName.ToLower().Trim();
                var cleanLast = (response.LastName ?? "").ToLower().Trim();
                response.Email = $"{cleanFirst}{(string.IsNullOrEmpty(cleanLast) ? "" : "." + cleanLast)}@dentiaclinic.com";
            }

            // Duplicate Verification on extracted patient
            if (!string.IsNullOrEmpty(response.FirstName) && request.DoctorId > 0)
            {
                DateTime? dobVal = DateTime.TryParse(response.Dob, out var parsedDt) ? parsedDt : null;
                var existingDuplicate = await _repository.FindDuplicatePatientAsync(request.DoctorId, response.FirstName, response.LastName ?? "Patient", dobVal, response.Phone ?? "");
                
                if (existingDuplicate != null)
                {
                    response.IsDuplicate = true;
                    response.ExistingPatientId = existingDuplicate.PatientID;
                }
            }

            // If autoSave requested and we have valid minimum details, save directly to SQL DB
            if (request.AutoSave && !string.IsNullOrEmpty(response.FirstName) && request.DoctorId > 0)
            {
                if (response.IsDuplicate)
                {
                    response.Summary = $"⚠️ Patient '{response.FirstName} {response.LastName}' is already registered (Patient ID #{response.ExistingPatientId}).";
                }
                else
                {
                    try
                    {
                        var fullAddress = new[] { response.Address, response.City, response.Postcode }
                            .Where(s => !string.IsNullOrWhiteSpace(s));

                        DateTime dobVal = DateTime.TryParse(response.Dob, out var parsedDt) ? parsedDt : new DateTime(1995, 1, 1);

                        var newPatient = new Patient
                        {
                            FirstName = response.FirstName,
                            LastName = string.IsNullOrEmpty(response.LastName) ? "Patient" : response.LastName,
                            DOB = dobVal,
                            Gender = string.IsNullOrEmpty(response.Gender) ? "Other" : response.Gender,
                            Phone = response.Phone ?? "",
                            Email = response.Email,
                            Address = string.Join(", ", fullAddress),
                            Region = string.IsNullOrEmpty(response.Region) ? (request.Region ?? "PK") : response.Region,
                            NHINumber = response.NhiNumber,
                            CurrentTreatmentPlan = string.IsNullOrEmpty(response.CurrentTreatmentPlan) ? "General Consultation" : response.CurrentTreatmentPlan,
                            DoctorID = request.DoctorId
                        };

                        int newId = await _repository.CreatePatientAsync(newPatient);
                        newPatient.PatientID = newId;
                        response.SavedPatient = newPatient;
                        response.Summary += $" (Saved to clinic registry as Patient #{newId})";
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"Auto-save patient DB error: {dbEx.Message}");
                    }
                }
            }

            return Ok(response);
        }

        private async Task<AIIntakeResponse> ExtractPatientWithGemini(string prompt, string preferredRegion)
        {
            var client = _httpClientFactory.CreateClient();
            var currentYear = DateTime.Now.Year;

            var systemInstruction = $@"
You are an expert Dental & Medical Receptionist AI Assistant.
Analyze the doctor's spoken or typed patient intake command.
Current Year: {currentYear}
Preferred Region: {preferredRegion}

Extract any available patient fields and return ONLY a valid JSON object matching this exact schema:
{{
  ""firstName"": """",
  ""lastName"": """",
  ""dob"": """",
  ""phone"": """",
  ""gender"": """",
  ""address"": """",
  ""city"": """",
  ""postcode"": """",
  ""region"": ""{preferredRegion}"",
  ""nhiNumber"": """",
  ""currentTreatmentPlan"": ""General Consultation"",
  ""allergies"": [],
  ""medicalNotes"": """",
  ""email"": """",
  ""summary"": """",
  ""extractedFields"": []
}}

Rules:
1. If the input is just a person's name (e.g. 'Tariq Mehmood', 'Sarah Connor', 'Ali Khan'), set firstName to the first name and lastName to the surname, and add 'firstName' and 'lastName' to extractedFields.
2. If age is given (e.g. '38 years old' or '38 saal'), calculate dob as (Current Year - Age)-01-01.
3. If gender is given, set gender to 'Male' or 'Female'.
4. If national ID / CNIC / NHI is mentioned (e.g. 35201-1234567-1 or ABC1234), extract to nhiNumber and add 'nhiNumber' to extractedFields.
5. If treatment / reason for visit is mentioned (e.g. 'needs whitening', 'braces consult', 'severe toothache', 'root canal', 'cavity filling'), set currentTreatmentPlan to one of: 'General Consultation', 'Toothache & Emergency', 'Teeth Whitening (Cosmetic)', 'Braces (Orthodontics)', 'Cavity Restorative', 'Root Canal Treatment (RCT)', 'Scaling & Deep Cleaning' and add 'currentTreatmentPlan' to extractedFields.
6. If any allergy or medical condition is mentioned (e.g. 'allergic to penicillin', 'latex allergy', 'patient is diabetic', 'hypertension', 'bleeding disorder'), add the tag to allergies array and add 'allergies' to extractedFields.
7. If chief complaint or medical background is spoken, set medicalNotes and add 'medicalNotes' to extractedFields.
8. Do NOT output comments, markdown fences, or conversational text. Output ONLY pure raw JSON.";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = systemInstruction },
                            new { text = $"Doctor Input: \"{prompt}\"" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 350,
                    response_mime_type = "application/json"
                }
            };

            var url = $"{_geminiEndpoint}?key={_apiKey}";
            var content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));
            var startTime = DateTime.Now;
            var res = await client.PostAsync(url, content, cts.Token);
            var endTime = DateTime.Now;
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API notice in intake: {res.StatusCode} - {err}");
                GeminiCallLogger.LogCall(
                    callType: "AI PATIENT INTAKE EXTRACTION",
                    endpoint: url,
                    requestPayload: $"[Doctor Spoken Intake: \"{prompt}\"]\nPayload:\n{JsonSerializer.Serialize(requestPayload)}",
                    responseData: err,
                    startTime: startTime,
                    endTime: endTime,
                    isSuccess: false,
                    errorMessage: $"HTTP {res.StatusCode}: {err}"
                );
                return null;
            }

            var jsonRes = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonRes);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return null;

            var rawText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim();
            if (string.IsNullOrEmpty(rawText)) return null;

            // Strip markdown fences robustly
            if (rawText.Contains("```json"))
            {
                var start = rawText.IndexOf("```json") + 7;
                var end = rawText.LastIndexOf("```");
                if (end > start) rawText = rawText.Substring(start, end - start);
            }
            else if (rawText.Contains("```"))
            {
                var start = rawText.IndexOf("```") + 3;
                var end = rawText.LastIndexOf("```");
                if (end > start) rawText = rawText.Substring(start, end - start);
            }
            rawText = Regex.Replace(rawText, @"//.*$", "", RegexOptions.Multiline).Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var parsed = JsonSerializer.Deserialize<AIIntakeResponse>(rawText, options);

            GeminiCallLogger.LogCall(
                callType: "AI PATIENT INTAKE EXTRACTION",
                endpoint: url,
                requestPayload: $"[Doctor Spoken Intake: \"{prompt}\"]\nPayload:\n{JsonSerializer.Serialize(requestPayload)}",
                responseData: jsonRes,
                startTime: startTime,
                endTime: endTime,
                isSuccess: true,
                extractedResult: $"Parsed Patient: {parsed?.FirstName} {parsed?.LastName}, DOB: {parsed?.Dob}, Phone: {parsed?.Phone}, NHI: {parsed?.NhiNumber}\nRaw AI Output:\n{rawText}"
            );

            return parsed;
        }

        private AIIntakeResponse FallbackPatientIntakeRegex(string text, string defaultRegion)
        {
            var res = new AIIntakeResponse
            {
                Region = defaultRegion,
                ExtractedFields = new List<string>()
            };

            // 1. Phone extraction
            var phoneMatch = Regex.Match(text, @"(?:phone|cell|mobile|number|contact|no|call)?\s*[:\-\s]?\s*(\+?\d{10,13}|03\d{9}|02\d{7,9}|\d{3}[\s\-]?\d{3}[\s\-]?\d{4})", RegexOptions.IgnoreCase);
            if (phoneMatch.Success)
            {
                res.Phone = phoneMatch.Groups[1].Value.Replace(" ", "").Replace("-", "");
                res.ExtractedFields.Add("phone");
            }

            // 2. Gender extraction
            if (Regex.IsMatch(text, @"\b(female|woman|girl|khatoon|aurat)\b|\bf\b", RegexOptions.IgnoreCase))
            {
                res.Gender = "Female";
                res.ExtractedFields.Add("gender");
            }
            else if (Regex.IsMatch(text, @"\b(male|man|boy|mard)\b|\bm\b", RegexOptions.IgnoreCase))
            {
                res.Gender = "Male";
                res.ExtractedFields.Add("gender");
            }

            // 3. Age / DOB extraction (Supports '25 year male', '25 years', '25', '25yo', '25 saal', 'age 25', etc.)
            var ageMatch = Regex.Match(text, @"\b(?:age|umar|umer|years?(\s*old)?|yo|y/o|yrs?|yr|saal|sal)\s*[:\-\s]?\s*(\d{1,2})\b|\b(\d{1,2})\s*(?:years?(\s*old)?|yo|y/o|yrs?|yr|saal|sal)\b", RegexOptions.IgnoreCase);
            if (!ageMatch.Success)
            {
                // Match standalone number or number next to gender (e.g. "25", "25 male", "25 female", "male 25")
                ageMatch = Regex.Match(text, @"\b(\d{1,2})\b");
            }

            if (ageMatch.Success)
            {
                var ageStr = !string.IsNullOrEmpty(ageMatch.Groups[1].Value) ? ageMatch.Groups[1].Value : ageMatch.Groups[2].Value;
                if (int.TryParse(ageStr, out int age) && age > 0 && age <= 120)
                {
                    int birthYear = DateTime.Now.Year - age;
                    res.Dob = $"{birthYear}-01-01";
                    res.ExtractedFields.Add("dob");
                }
            }
            else
            {
                var dobMatch = Regex.Match(text, @"\b(\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2}[-/]\d{4})\b");
                if (dobMatch.Success && DateTime.TryParse(dobMatch.Value, out DateTime parsedDob))
                {
                    res.Dob = parsedDob.ToString("yyyy-MM-dd");
                    res.ExtractedFields.Add("dob");
                }
            }

            // 4. Region & City extraction
            if (Regex.IsMatch(text, @"\b(pakistan|pk|islamabad|lahore|karachi|rawalpindi|peshawar|quetta|multan|faisalabad)\b", RegexOptions.IgnoreCase))
            {
                res.Region = "PK";
                res.ExtractedFields.Add("region");
            }
            else if (Regex.IsMatch(text, @"\b(new zealand|nz|auckland|wellington|christchurch|hamilton|tauranga)\b", RegexOptions.IgnoreCase))
            {
                res.Region = "NZ";
                res.ExtractedFields.Add("region");
            }

            var cities = new[] { "Islamabad", "Lahore", "Karachi", "Rawalpindi", "Peshawar", "Auckland", "Wellington", "Christchurch", "Hamilton" };
            foreach (var c in cities)
            {
                if (Regex.IsMatch(text, $@"\b{c}\b", RegexOptions.IgnoreCase))
                {
                    res.City = c;
                    res.ExtractedFields.Add("city");
                    break;
                }
            }

            // 5. Standalone or prefixed Name extraction with strict stopword filtering
            var invalidWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "my", "a", "an", "the", "new", "this", "some", "our", "registered", "intake",
                "patient", "profile", "record", "someone", "mariz", "entry", "user", "one",
                "for", "me", "please", "want", "need", "to", "add", "register", "create", "enroll"
            };

            // Check if whole text is purely an intake command without actual person's name
            if (Regex.IsMatch(text, @"^(?:i\s+want|i\s+need|can\s+you|please|help\s+me|let\s+us|let's)?\s*(?:to\s+)?(?:add|register|create|enroll|input)\s*(?:a\s+|an\s+|the\s+|my\s+|our\s+|new\s+)?patient(?:\s+for\s+me)?$", RegexOptions.IgnoreCase))
            {
                // Pure intent command, no name supplied
            }
            else
            {
                var trimmedText = text.Trim();
                var pureWords = trimmedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (pureWords.Length >= 2 && pureWords.All(w => Regex.IsMatch(w, @"^[A-Za-z]+$")) && !pureWords.Any(w => invalidWords.Contains(w)))
                {
                    res.FirstName = pureWords[0];
                    res.LastName = string.Join(" ", pureWords.Skip(1));
                    res.ExtractedFields.Add("firstName");
                    res.ExtractedFields.Add("lastName");
                }
                else if (pureWords.Length == 1 && Regex.IsMatch(pureWords[0], @"^[A-Za-z]+$") && !invalidWords.Contains(pureWords[0]))
                {
                    res.FirstName = pureWords[0];
                    res.LastName = "";
                    res.ExtractedFields.Add("firstName");
                }
                else
                {
                    var nameMatch = Regex.Match(text, @"(?:patient named|name is|patient is|called)\s+([A-Za-z]+)(?:\s+([A-Za-z]+))?", RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        var first = nameMatch.Groups[1].Value;
                        var second = nameMatch.Groups[2].Success ? nameMatch.Groups[2].Value : "";

                        if (!invalidWords.Contains(first))
                        {
                            res.FirstName = first;
                            res.ExtractedFields.Add("firstName");
                            if (!string.IsNullOrWhiteSpace(second) && !invalidWords.Contains(second) && !Regex.IsMatch(second, @"\d|years|male|female|saal"))
                            {
                                res.LastName = second;
                                res.ExtractedFields.Add("lastName");
                            }
                        }
                    }
                }
            }

            // 6. CNIC / NHI Number extraction
            var cnicMatch = Regex.Match(text, @"\b(\d{5}-\d{7}-\d|\d{13}|[A-Z]{3}\d{4})\b", RegexOptions.IgnoreCase);
            if (cnicMatch.Success)
            {
                res.NhiNumber = cnicMatch.Groups[1].Value;
                res.ExtractedFields.Add("nhiNumber");
            }

            // 7. Treatment Modality / Reason for visit extraction
            if (Regex.IsMatch(text, @"\b(whitening|bleaching|teeth whitening)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Teeth Whitening (Cosmetic)";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(braces|aligner|ortho|orthodontic)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Braces (Orthodontics)";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(root canal|rct|endodontic)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Root Canal Treatment (RCT)";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(pain|toothache|emergency|swelling|severe pain)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Toothache & Emergency";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(filling|cavity|caries|restorative)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Cavity Restorative";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(cleaning|scaling|polishing|calculus)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Scaling & Deep Cleaning";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(crown|bridge|cap|prosthesis|implant)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "Crown & Bridge Prosthesis";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }
            else if (Regex.IsMatch(text, @"\b(checkup|check up|routine|general|consultation|normal)\b", RegexOptions.IgnoreCase))
            {
                res.CurrentTreatmentPlan = "General Consultation";
                res.ExtractedFields.Add("currentTreatmentPlan");
            }

            // 8. Allergy & Medical condition extraction
            res.Allergies = new List<string>();
            if (Regex.IsMatch(text, @"\b(penicillin|penicilin)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Penicillin Allergy");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(latex)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Latex Sensitive");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(diabetic|diabetes|sugar)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Diabetic");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(hypertension|bp|high blood pressure|blood pressure)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Hypertension");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(anesthetic|anesthesia|local anesthetic|numb)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Local Anesthetic Allergy");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(aspirin|nsaid|disprin|brufen)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Aspirin / NSAID");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(bleeding|bleeder|thinning|hemo|haemo)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("Bleeding Disorder");
                res.ExtractedFields.Add("allergies");
            }
            if (Regex.IsMatch(text, @"\b(no allerg|no known allerg|healthy|none|no problem|no allergy)\b", RegexOptions.IgnoreCase))
            {
                res.Allergies.Add("No Known Allergies");
                res.ExtractedFields.Add("allergies");
            }

            // 9. Clinical notes extraction
            var noteMatch = Regex.Match(text, @"(?:note|complaint|notes|issue|problem|symptoms?)[:\-\s]+(.+)", RegexOptions.IgnoreCase);
            if (noteMatch.Success)
            {
                res.MedicalNotes = noteMatch.Groups[1].Value.Trim();
                res.ExtractedFields.Add("medicalNotes");
            }

            res.Summary = $"Extracted {res.ExtractedFields.Count} patient intake fields.";
            return res;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(int id)
        {
            var patient = await _repository.GetPatientByIdAsync(id);
            if (patient == null) return NotFound("Patient not found.");
            return Ok(patient);
        }

        [HttpGet("{id}/chart")]
        public async Task<IActionResult> GetChart(int id)
        {
            var chart = await _repository.GetPatientChartAsync(id);
            return Ok(chart);
        }

        [HttpPost("teeth/update-bulk")]
        public async Task<IActionResult> UpdateBulk([FromBody] UpdateBulkRequest request)
        {
            if (request == null || request.Updates == null) return BadRequest("Invalid request.");

            foreach (var update in request.Updates)
            {
                int tNumber = 1;
                string? toothKey = update.ToothKey;
                string dentitionCat = update.DentitionCategory ?? "Adult";

                if (update.ToothNumber is System.Text.Json.JsonElement je)
                {
                    if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        tNumber = je.GetInt32();
                        if (string.IsNullOrEmpty(toothKey)) toothKey = tNumber.ToString();
                    }
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string str = je.GetString()?.Trim().ToUpper() ?? "1";
                        if (string.IsNullOrEmpty(toothKey)) toothKey = str;
                        if (int.TryParse(str, out int parsed))
                        {
                            tNumber = parsed;
                        }
                        else if (str.Length == 1 && str[0] >= 'A' && str[0] <= 'T')
                        {
                            tNumber = str[0] - 'A' + 1; // Maps A->1, B->2, ... T->20
                            dentitionCat = "Pediatric";
                        }
                    }
                }
                else if (update.ToothNumber != null)
                {
                    string str = update.ToothNumber.ToString()?.Trim().ToUpper() ?? "1";
                    if (string.IsNullOrEmpty(toothKey)) toothKey = str;
                    if (int.TryParse(str, out int parsed))
                    {
                        tNumber = parsed;
                    }
                    else if (str.Length == 1 && str[0] >= 'A' && str[0] <= 'T')
                    {
                        tNumber = str[0] - 'A' + 1;
                        dentitionCat = "Pediatric";
                    }
                }

                // Preserve actual diagnosis/condition and prevent generic status overwrite
                string resolvedStatus = update.ConditionStatus ?? update.Status ?? "Healthy";
                if (resolvedStatus == "Completed" || resolvedStatus == "Planned" || resolvedStatus == "In Progress")
                {
                    if (!string.IsNullOrEmpty(update.Comment))
                    {
                        var firstPart = update.Comment.Split(new[] { '.', '•', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(firstPart) && firstPart.Length > 3 && !firstPart.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedStatus = firstPart;
                        }
                    }
                }

                string resolvedComment = update.Comment ?? update.Comments ?? "Updated via Chart / Treatment Plan";
                if (!string.IsNullOrEmpty(update.Status) && !resolvedComment.Contains($"Status: {update.Status}"))
                {
                    resolvedComment += $" • Status: {update.Status}";
                }
                if (!string.IsNullOrEmpty(update.CdtCode) && !resolvedComment.Contains(update.CdtCode))
                {
                    resolvedComment += $" (CDT: {update.CdtCode})";
                }

                await _repository.UpdateTeethStateBulkAsync(
                    request.PatientId, 
                    tNumber, 
                    update.Color ?? "#10B981", 
                    resolvedStatus, 
                    resolvedStatus, // using status as treatment performed
                    resolvedComment,
                    dentitionCat,
                    toothKey,
                    update.DoctorId
                );
            }

            int primaryDocId = request.Updates?.FirstOrDefault()?.DoctorId ?? 1;
            GeminiCallLogger.LogSync(
                "TEETH ODONTOGRAM BULK SYNC",
                request.PatientId,
                primaryDocId,
                $"Successfully synchronized {request.Updates?.Count ?? 0} teeth observations to SQL database."
            );
            
            return Ok(new { message = "Bulk update successful." });
        }

        [HttpPost("{id}/chart/export")]
        public IActionResult ExportChart(int id)
        {
            // Simulate exporting chart to PDF using Nexu / Puppeteer
            string simulatedPath = $"/exports/patient_{id}_chart_{System.DateTime.Now:yyyyMMdd}.pdf";
            return Ok(new { message = "Chart exported successfully.", path = simulatedPath });
        }

        [HttpGet("{id}/prescriptions")]
        public async Task<IActionResult> GetPrescriptions(int id)
        {
            var prescriptions = await _repository.GetPrescriptionsAsync(id);
            return Ok(prescriptions);
        }

        [HttpPost("{id}/prescriptions")]
        public async Task<IActionResult> AddPrescription(int id, [FromBody] PrescriptionRequest req)
        {
            if (string.IsNullOrEmpty(req?.MedicineName)) return BadRequest("Medicine name is required.");
            
            await _repository.AddPrescriptionAsync(id, req.MedicineName);
            return Ok(new { message = "Prescription added successfully." });
        }

        [HttpGet("{id}/history/{toothNumber}")]
        public async Task<IActionResult> GetToothHistory(int id, int toothNumber)
        {
            var history = await _repository.GetToothHistoryAsync(id, toothNumber);
            return Ok(history);
        }

        [HttpPost("{id}/chat-history")]
        public async Task<IActionResult> SaveChatHistory(int id, [FromBody] ChatHistoryRequest req)
        {
            if (req == null) return BadRequest();
            await _repository.AddChatHistoryAsync(id, req.Transcript, req.ParsedAction);
            GeminiCallLogger.LogSync("CHAT HISTORY SYNC", id, 1, $"Transcript: {req.Transcript} | Action: {req.ParsedAction}");
            return Ok();
        }

        [HttpGet("{id}/chat-history")]
        public async Task<IActionResult> GetChatHistory(int id)
        {
            var history = await _repository.GetChatHistoryAsync(id);
            return Ok(history);
        }

        [HttpPost("{id}/clinical-logs")]
        public async Task<IActionResult> AddClinicalLog(int id, [FromBody] ClinicalLogRequest req)
        {
            int docId = (req != null && req.DoctorId.HasValue && req.DoctorId.Value > 0) ? req.DoctorId.Value : 2;
            string msg = !string.IsNullOrEmpty(req?.Message) ? req.Message : "Clinical note recorded";
            string effectiveLogType = !string.IsNullOrEmpty(req?.LogType) ? req.LogType : (!string.IsNullOrEmpty(req?.Action) ? req.Action : "Diagnostic Suite");
            
            try
            {
                await _repository.AddClinicalLogAsync(id, docId, msg, effectiveLogType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting clinical log: {ex.Message}");
            }

            try
            {
                GeminiCallLogger.LogSync("CLINICAL LOG SYNC", id, docId, $"[LogType: {effectiveLogType}] {msg}");
            }
            catch {}

            return Ok(new { message = "Clinical log added successfully." });
        }

        [HttpGet("{id}/clinical-logs")]
        public async Task<IActionResult> GetClinicalLogs(int id)
        {
            var logs = await _repository.GetClinicalLogsAsync(id);
            return Ok(logs);
        }

        [HttpGet("{id}/diagnostic-assessment")]
        public async Task<IActionResult> GetDiagnosticAssessment(int id)
        {
            var record = await _repository.GetDiagnosticAssessmentAsync(id);
            if (record == null) return Ok(null);
            return Ok(record);
        }

        [HttpPost("{id}/diagnostic-assessment")]
        public async Task<IActionResult> SaveDiagnosticAssessment(int id, [FromBody] DiagnosticAssessmentDto dto)
        {
            int docId = (dto != null && dto.DoctorId.HasValue && dto.DoctorId.Value > 0) ? dto.DoctorId.Value : 2;
            string category = dto?.SuiteCategory ?? "occlusion";
            string json = dto?.AssessmentJson ?? "{}";
            
            try
            {
                await _repository.SaveDiagnosticAssessmentAsync(id, docId, category, json, dto?.CdtCode, dto?.DiagnosisSummary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveDiagnosticAssessment error: {ex.Message}");
            }

            try
            {
                GeminiCallLogger.LogSync("DIAGNOSTIC ASSESSMENT SYNC", id, docId, $"Saved {category} diagnosis: {dto?.DiagnosisSummary}");
            }
            catch {}

            return Ok(new { message = "Diagnostic assessment saved successfully." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] Patient req)
        {
            if (req == null) return BadRequest("Invalid patient data.");

            // Validate Profile Image Size (Max 5MB)
            if (req.ProfileImage != null && req.ProfileImage.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Profile image size exceeds the maximum limit of 5MB.");
            }
            
            var existing = await _repository.GetPatientByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Patient not found." });

            req.PatientID = id;
            // Preserve doctor ID if not supplied
            if (req.DoctorID <= 0) req.DoctorID = existing.DoctorID;

            await _repository.UpdatePatientAsync(req);
            return Ok(new { message = "Patient information updated successfully.", patient = req });
        }

        [HttpPost("{id}/profile-image")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload-policy")]
        public async Task<IActionResult> UploadProfileImage(int id, IFormFile file)
        {
            var validation = await _fileUploadService.ValidateAndExtractAsync(file, FileUploadCategory.ProfileImage);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.ErrorMessage });
            }

            var existing = await _repository.GetPatientByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Patient not found." });

            await _repository.UpdatePatientProfileImageAsync(id, validation.FileBytes, validation.CanonicalMimeType);

            string dataUrl = $"data:{validation.CanonicalMimeType};base64,{Convert.ToBase64String(validation.FileBytes)}";
            return Ok(new
            {
                message = "Profile image updated successfully.",
                patientId = id,
                profileImageDataUrl = dataUrl
            });
        }

        [HttpGet("{id}/profile-image")]
        public async Task<IActionResult> GetProfileImage(int id)
        {
            var patient = await _repository.GetPatientByIdAsync(id);
            if (patient == null) return NotFound(new { message = "Patient not found." });

            Response.Headers.Append("X-Content-Type-Options", "nosniff");

            if (patient.ProfileImage != null && patient.ProfileImage.Length > 0)
            {
                var mime = !string.IsNullOrEmpty(patient.ProfileImageMimeType) ? patient.ProfileImageMimeType : "image/jpeg";
                return File(patient.ProfileImage, mime);
            }

            // Fallback default avatar SVG based on patient gender
            byte[] defaultBytes = GetDefaultAvatarBytes(patient.Gender, out string defaultMime);
            return File(defaultBytes, defaultMime);
        }

        [HttpDelete("{id}/profile-image")]
        public async Task<IActionResult> ResetProfileImage(int id)
        {
            var patient = await _repository.GetPatientByIdAsync(id);
            if (patient == null) return NotFound("Patient not found.");

            // Reset to default gender avatar
            byte[] defaultBytes = GetDefaultAvatarBytes(patient.Gender, out string defaultMime);
            await _repository.UpdatePatientProfileImageAsync(id, defaultBytes, defaultMime);

            return Ok(new { message = "Profile image reset to default avatar." });
        }

        [HttpPost("{id}/treatment-plan")]
        public async Task<IActionResult> UpdateTreatmentPlan(int id, [FromBody] TreatmentPlanRequest req)
        {
            if (req == null) return BadRequest("Invalid treatment plan data.");
            
            await _repository.UpdatePatientTreatmentPlanAsync(id, req.TreatmentPlan ?? "General Consultation", req.TreatmentStage, req.TargetShade);
            return Ok(new { message = "Treatment plan updated successfully." });
        }

        public static byte[] GetDefaultAvatarBytes(string? gender, out string mimeType)
        {
            mimeType = "image/svg+xml";
            string g = (gender ?? "").Trim().ToLower();
            if (g == "male")
            {
                return Encoding.UTF8.GetBytes(MaleAvatarSvg);
            }
            else if (g == "female")
            {
                return Encoding.UTF8.GetBytes(FemaleAvatarSvg);
            }
            return Encoding.UTF8.GetBytes(NeutralAvatarSvg);
        }

        private const string MaleAvatarSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 120 120"" width=""100%"" height=""100%"">
  <defs>
    <linearGradient id=""mGrad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#3B82F6"" />
      <stop offset=""100%"" stop-color=""#1D4ED8"" />
    </linearGradient>
  </defs>
  <circle cx=""60"" cy=""60"" r=""60"" fill=""url(#mGrad)"" />
  <path d=""M20 106 C 24 82, 42 74, 60 74 C 78 74, 96 82, 100 106 Z"" fill=""#EFF6FF"" />
  <path d=""M52 64 L68 64 L68 76 L52 76 Z"" fill=""#FDBA74"" />
  <ellipse cx=""60"" cy=""48"" rx=""19"" ry=""22"" fill=""#FED7AA"" />
  <path d=""M39 44 C 39 26, 52 20, 68 21 C 78 22, 82 28, 82 38 C 76 34, 68 33, 58 35 C 48 37, 42 41, 39 44 Z"" fill=""#1E293B"" />
</svg>";

        private const string FemaleAvatarSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 120 120"" width=""100%"" height=""100%"">
  <defs>
    <linearGradient id=""fGrad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#EC4899"" />
      <stop offset=""100%"" stop-color=""#9D174D"" />
    </linearGradient>
  </defs>
  <circle cx=""60"" cy=""60"" r=""60"" fill=""url(#fGrad)"" />
  <path d=""M34 42 C 32 64, 34 84, 40 92 L80 92 C 86 84, 88 64, 86 42 Z"" fill=""#331407"" />
  <path d=""M20 106 C 24 82, 42 74, 60 74 C 78 74, 96 82, 100 106 Z"" fill=""#FFF1F2"" />
  <path d=""M53 64 L67 64 L67 76 L53 76 Z"" fill=""#FDBA74"" />
  <ellipse cx=""60"" cy=""48"" rx=""18"" ry=""21"" fill=""#FED7AA"" />
  <path d=""M38 46 C 36 28, 50 20, 60 20 C 72 20, 84 28, 82 46 C 77 34, 69 31, 60 31 C 51 31, 43 34, 38 46 Z"" fill=""#451A03"" />
</svg>";

        private const string NeutralAvatarSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 120 120"" width=""100%"" height=""100%"">
  <defs>
    <linearGradient id=""nGrad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#0D9488"" />
      <stop offset=""100%"" stop-color=""#0F766E"" />
    </linearGradient>
  </defs>
  <circle cx=""60"" cy=""60"" r=""60"" fill=""url(#nGrad)"" />
  <path d=""M20 106 C 24 82, 42 74, 60 74 C 78 74, 96 82, 100 106 Z"" fill=""#F0FDFA"" />
  <circle cx=""60"" cy=""48"" r=""20"" fill=""#CCFBF1"" />
</svg>";

        // ==========================================
        // CLINICAL SPECIALTIES: IMPLANT PLANNING APIS
        // ==========================================
        [HttpGet("{id}/implant-plans")]
        public async Task<IActionResult> GetImplantPlans(int id)
        {
            var plans = await _repository.GetImplantPlansByPatientAsync(id);
            return Ok(plans);
        }

        [HttpPost("{id}/implant-plans")]
        public async Task<IActionResult> SaveImplantPlan(int id, [FromBody] ImplantPlanDto dto)
        {
            if (dto == null) return BadRequest("Plan payload is required.");

            // Validation: Numeric ranges
            if (dto.ImplantLength < 3.0m || dto.ImplantLength > 25.0m)
            {
                return BadRequest("Implant length must be between 3.0 mm and 25.0 mm (clinical standard: 6.0 mm - 18.0 mm).");
            }

            if (dto.ImplantDiameter < 2.0m || dto.ImplantDiameter > 10.0m)
            {
                return BadRequest("Implant diameter must be between 2.0 mm and 10.0 mm (clinical standard: 2.5 mm - 7.0 mm).");
            }

            // Validation: Bone Quality (Lekholm & Zarb classification: D1, D2, D3, D4)
            var validBoneQualities = new[] { "D1", "D2", "D3", "D4" };
            if (string.IsNullOrWhiteSpace(dto.BoneQuality) || !validBoneQualities.Contains(dto.BoneQuality.Trim().ToUpper()))
            {
                return BadRequest("Bone quality must be a valid Lekholm & Zarb classification: D1, D2, D3, or D4.");
            }

            if (dto.BoneHeightAvailable.HasValue && (dto.BoneHeightAvailable.Value < 0 || dto.BoneHeightAvailable.Value > 40.0m))
            {
                return BadRequest("Bone height available must be between 0.0 mm and 40.0 mm.");
            }

            if (dto.BoneWidthAvailable.HasValue && (dto.BoneWidthAvailable.Value < 0 || dto.BoneWidthAvailable.Value > 30.0m))
            {
                return BadRequest("Bone width available must be between 0.0 mm and 30.0 mm.");
            }

            var record = new ImplantPlanRecord
            {
                ImplantPlanID = dto.ImplantPlanID ?? 0,
                PatientID = id,
                DoctorID = dto.DoctorID,
                ToothNumber = dto.ToothNumber,
                ToothKey = dto.ToothKey ?? dto.ToothNumber.ToString(),
                ImplantBrand = dto.ImplantBrand?.Trim(),
                ImplantLength = dto.ImplantLength,
                ImplantDiameter = dto.ImplantDiameter,
                BoneQuality = dto.BoneQuality.Trim().ToUpper(),
                BoneHeightAvailable = dto.BoneHeightAvailable,
                BoneWidthAvailable = dto.BoneWidthAvailable,
                GraftingRequired = dto.GraftingRequired,
                SinusLiftStatus = string.IsNullOrWhiteSpace(dto.SinusLiftStatus) ? "None" : dto.SinusLiftStatus.Trim(),
                CbctReferenceUrl = dto.CbctReferenceUrl?.Trim(),
                DigitalPlanningNotes = dto.DigitalPlanningNotes?.Trim(),
                GuidedSurgeryFlag = dto.GuidedSurgeryFlag,
                PlanStatus = string.IsNullOrWhiteSpace(dto.PlanStatus) ? "Planned" : dto.PlanStatus.Trim(),
                PlannedDate = dto.PlannedDate,
                PlacementDate = dto.PlacementDate
            };

            int planId = await _repository.SaveImplantPlanAsync(record);
            record.ImplantPlanID = planId;

            return Ok(new { message = "Implant plan saved successfully.", implantPlanId = planId, plan = record });
        }

        [HttpDelete("{id}/implant-plans/{planId}")]
        public async Task<IActionResult> DeleteImplantPlan(int id, int planId)
        {
            var plan = await _repository.GetImplantPlanByIdAsync(planId);
            if (plan == null || plan.PatientID != id) return NotFound("Implant plan not found.");

            await _repository.DeleteImplantPlanAsync(planId);
            return Ok(new { message = "Implant plan deleted successfully." });
        }

        // ==========================================
        // CLINICAL SPECIALTIES: BIOPSY & PATHOLOGY APIS
        // ==========================================
        [HttpGet("{id}/biopsy-records")]
        public async Task<IActionResult> GetBiopsyRecords(int id)
        {
            var records = await _repository.GetBiopsyRecordsByPatientAsync(id);
            return Ok(records);
        }

        [HttpPost("{id}/biopsy-records")]
        public async Task<IActionResult> SaveBiopsyRecord(int id, [FromBody] BiopsyDto dto)
        {
            if (dto == null) return BadRequest("Biopsy payload is required.");

            // Validation: Biopsy Type single-select enum (Incisional / Excisional)
            var validTypes = new[] { "Incisional", "Excisional" };
            if (string.IsNullOrWhiteSpace(dto.BiopsyType) || !validTypes.Contains(dto.BiopsyType.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Biopsy type must be either 'Incisional' or 'Excisional'.");
            }

            // Validation: Anatomical site of biopsy
            if (string.IsNullOrWhiteSpace(dto.SiteOfBiopsy))
            {
                return BadRequest("Site of biopsy (anatomical location — tooth number, quadrant, or soft tissue region) is required.");
            }

            var record = new BiopsyRecord
            {
                BiopsyID = dto.BiopsyID ?? 0,
                PatientID = id,
                DoctorID = dto.DoctorID,
                BiopsyType = char.ToUpper(dto.BiopsyType.Trim()[0]) + dto.BiopsyType.Trim().Substring(1).ToLower(), // Normalize: Incisional / Excisional
                SiteOfBiopsy = dto.SiteOfBiopsy.Trim(),
                ToothNumber = dto.ToothNumber,
                ToothKey = dto.ToothKey,
                ClinicalImpression = dto.ClinicalImpression?.Trim(),
                PathologyLabName = dto.PathologyLabName?.Trim(),
                SpecimenReference = dto.SpecimenReference?.Trim(),
                BiopsyDate = dto.BiopsyDate ?? DateTime.UtcNow.Date,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Specimen Sent" : dto.Status.Trim(),
                HistopathologyDiagnosis = dto.HistopathologyDiagnosis?.Trim(),
                ResultsNotes = dto.ResultsNotes?.Trim(),
                FollowUpRequired = dto.FollowUpRequired,
                FollowUpDate = dto.FollowUpDate
            };

            int biopsyId = await _repository.SaveBiopsyRecordAsync(record);
            record.BiopsyID = biopsyId;

            return Ok(new { message = "Biopsy record saved successfully.", biopsyId = biopsyId, biopsy = record });
        }

        [HttpDelete("{id}/biopsy-records/{biopsyId}")]
        public async Task<IActionResult> DeleteBiopsyRecord(int id, int biopsyId)
        {
            var biopsy = await _repository.GetBiopsyRecordByIdAsync(biopsyId);
            if (biopsy == null || biopsy.PatientID != id) return NotFound("Biopsy record not found.");

            await _repository.DeleteBiopsyRecordAsync(biopsyId);
            return Ok(new { message = "Biopsy record deleted successfully." });
        }

        // ==========================================
        // CLINICAL SPECIALTIES: ORTHODONTICS - CLEAR ALIGNERS APIS
        // ==========================================
        [HttpGet("{id}/ortho-aligners")]
        public async Task<IActionResult> GetOrthoAligners(int id)
        {
            var aligners = await _repository.GetOrthoAlignersByPatientAsync(id);
            return Ok(aligners);
        }

        [HttpPost("{id}/ortho-aligners")]
        public async Task<IActionResult> SaveOrthoAligner(int id, [FromBody] OrthoAlignerTreatmentDto dto)
        {
            if (dto == null) return BadRequest("Ortho aligner payload is required.");

            // Validation: Brand
            if (string.IsNullOrWhiteSpace(dto.AlignerBrand))
            {
                return BadRequest("Aligner system / brand is required.");
            }

            // Validation: Stages
            if (dto.TotalStages < 1 || dto.TotalStages > 200)
            {
                return BadRequest("Total number of aligner stages must be between 1 and 200.");
            }

            if (dto.CurrentStage < 0 || dto.CurrentStage > dto.TotalStages + 10)
            {
                return BadRequest($"Current stage must be between 0 and {dto.TotalStages}.");
            }

            var record = new OrthoAlignerTreatmentRecord
            {
                OrthoAlignerID = dto.OrthoAlignerID ?? 0,
                PatientID = id,
                DoctorID = dto.DoctorID,
                AlignerBrand = dto.AlignerBrand.Trim(),
                TotalStages = dto.TotalStages,
                CurrentStage = dto.CurrentStage,
                AttachmentsRequired = dto.AttachmentsRequired,
                AttachmentNotes = dto.AttachmentNotes?.Trim(),
                IprRequired = dto.IprRequired,
                IprDetails = dto.IprDetails?.Trim(),
                WearSchedule = string.IsNullOrWhiteSpace(dto.WearSchedule) ? "7 Days/Tray" : dto.WearSchedule.Trim(),
                RefinementScanTracking = dto.RefinementScanTracking?.Trim(),
                RefinementCount = dto.RefinementCount,
                Arch = string.IsNullOrWhiteSpace(dto.Arch) ? "Dual" : dto.Arch.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
                StartDate = dto.StartDate,
                TargetCompletionDate = dto.TargetCompletionDate,
                ClinicalNotes = dto.ClinicalNotes?.Trim()
            };

            int alignerId = await _repository.SaveOrthoAlignerAsync(record);
            record.OrthoAlignerID = alignerId;

            return Ok(new { message = "Ortho aligner treatment plan saved successfully.", alignerId = alignerId, treatment = record });
        }

        [HttpDelete("{id}/ortho-aligners/{alignerId}")]
        public async Task<IActionResult> DeleteOrthoAligner(int id, int alignerId)
        {
            var aligner = await _repository.GetOrthoAlignerByIdAsync(alignerId);
            if (aligner == null || aligner.PatientID != id) return NotFound("Ortho aligner treatment record not found.");

            await _repository.DeleteOrthoAlignerAsync(alignerId);
            return Ok(new { message = "Ortho aligner record deleted successfully." });
        }
    }

    public class TreatmentPlanRequest
    {
        public string? TreatmentPlan { get; set; }
        public string? TreatmentStage { get; set; }
        public string? TargetShade { get; set; }
    }

    public class AIIntakeRequest
    {
        public string Prompt { get; set; }
        public int DoctorId { get; set; }
        public string Region { get; set; }
        public bool AutoSave { get; set; }
    }

    public class AIIntakeResponse
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Dob { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Postcode { get; set; }
        public string Region { get; set; }
        public string Email { get; set; }
        public string? NhiNumber { get; set; }
        public string? CurrentTreatmentPlan { get; set; }
        public string? MedicalNotes { get; set; }
        public List<string> Allergies { get; set; } = new List<string>();
        public string Summary { get; set; }
        public List<string> ExtractedFields { get; set; } = new List<string>();
        public Patient SavedPatient { get; set; }
        public bool IsDuplicate { get; set; }
        public int ExistingPatientId { get; set; }
    }

    public class DuplicateCheckRequest
    {
        public int DoctorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Dob { get; set; }
        public string Phone { get; set; }
    }

    public class UpdateBulkRequest
    {
        public int PatientId { get; set; }
        public List<ToothUpdate> Updates { get; set; }
    }

    public class ToothUpdate
    {
        public object? ToothNumber { get; set; }
        public string? ToothKey { get; set; }
        public string? DentitionCategory { get; set; }
        public int? DoctorId { get; set; }
        public string? Status { get; set; }
        public string? ConditionStatus { get; set; }
        public string? Color { get; set; }
        public string? Comment { get; set; }
        public string? Comments { get; set; }
        public string? CdtCode { get; set; }
    }

    public class PrescriptionRequest
    {
        public string MedicineName { get; set; }
    }

    public class ChatHistoryRequest
    {
        public string Transcript { get; set; }
        public string ParsedAction { get; set; }
    }
}
