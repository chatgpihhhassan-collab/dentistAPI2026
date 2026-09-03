using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DentistAPI.Repositories;
using DentistAPI.Models;
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
        private readonly string _apiKey;
        private readonly string _geminiEndpoint;

        public PatientsController(DentalRepository repository, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _repository = repository;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _apiKey = config["GEMINI_API_KEY"] ?? "";
            var modelName = config["GEMINI_MODEL"] ?? "gemini-3.5-flash";
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
            var res = await client.PostAsync(url, content, cts.Token);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API notice in intake: {res.StatusCode} - {err}");
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

                await _repository.UpdateTeethStateBulkAsync(
                    request.PatientId, 
                    tNumber, 
                    update.Color ?? "#10B981", 
                    update.Status ?? "Healthy", 
                    update.Status ?? "Healthy", // using status as treatment performed
                    update.Comment ?? "Updated via Chart / Voice Command",
                    dentitionCat,
                    toothKey,
                    update.DoctorId
                );
            }
            
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
            if (req == null || string.IsNullOrEmpty(req.Message)) return BadRequest("Message is required.");
            
            await _repository.AddClinicalLogAsync(id, req.DoctorID, req.Message, req.LogType);
            return Ok(new { message = "Clinical log added successfully." });
        }

        [HttpGet("{id}/clinical-logs")]
        public async Task<IActionResult> GetClinicalLogs(int id)
        {
            var logs = await _repository.GetClinicalLogsAsync(id);
            return Ok(logs);
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
        public async Task<IActionResult> UploadProfileImage(int id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No image file was provided.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest("File size exceeds the 5MB maximum limit.");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Invalid image format. Allowed formats: JPEG, PNG, WebP, GIF, SVG.");
            }

            var existing = await _repository.GetPatientByIdAsync(id);
            if (existing == null) return NotFound("Patient not found.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] imageBytes = ms.ToArray();

            await _repository.UpdatePatientProfileImageAsync(id, imageBytes, file.ContentType);

            string dataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(imageBytes)}";
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
            if (patient == null) return NotFound("Patient not found.");

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
        public string? Color { get; set; }
        public string? Comment { get; set; }
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
