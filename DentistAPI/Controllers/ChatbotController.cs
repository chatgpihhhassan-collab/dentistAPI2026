using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Repositories;
using DentistAPI.Models;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly string ApiKey;
        private readonly string GeminiEndpoint;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly DentalRepository _repository;

        public ChatbotController(Microsoft.Extensions.Configuration.IConfiguration config, DentalRepository repository)
        {
            _config = config;
            _repository = repository;
            ApiKey = config["GEMINI_API_KEY"] ?? "";
            var modelName = config["GEMINI_MODEL"] ?? "gemini-3.6-flash";
            GeminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent";
        }

        // Load NZULM Dictionary into memory
        private static System.Collections.Generic.HashSet<string> _medicineDictionary = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        
        static ChatbotController()
        {
            try {
                var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "NZULM_Dictionary.csv");
                if (System.IO.File.Exists(path)) {
                    var lines = System.IO.File.ReadAllLines(path);
                    foreach(var line in lines) {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("MedicineName")) continue;
                        var parts = line.Split(',');
                        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) {
                            _medicineDictionary.Add(parts[0].Trim());
                        }
                    }
                }
            } catch {
                // Failsafe if file is locked or missing
            }
        }

        [HttpPost("parse")]
        public async Task<IActionResult> ParseVoiceCommand([FromBody] VoiceCommandRequest request)
        {
            if (string.IsNullOrEmpty(request.Text)) return BadRequest("Text is required.");

            string rawText = request.Text.Trim();
            string lower = rawText.ToLower();

            // 1. Instant Greeting Response
            if (lower == "hi" || lower == "hello" || lower == "hey" || lower == "greetings" || lower == "hi there" || lower == "hello there")
            {
                return Ok(new {
                    ai_response = "Hello Doctor! I am your Dental Clinical AI Assistant. You can ask me for any patient's dossier, check today's appointments, query root canal or caries stats, or dictate tooth updates.",
                    tooth_updates = new object[] {},
                    prescriptions = new object[] {},
                    history_inquiries = new object[] {}
                });
            }

            // =========================================================================
            // PILLAR 3: CLINIC-WIDE CLINICAL & ODONTOGRAM STATS
            // =========================================================================
            if (System.Text.RegularExpressions.Regex.IsMatch(lower, @"(how many|how much|count of|number of|percentage of).*(patient|people|teeth|tooth|root canal|rct|damage|decay|caries|brace|ortho|whiten|miss)"))
            {
                var stats = await _repository.GetClinicStatsSummaryAsync(request.DoctorId);

                if (lower.Contains("root canal") || lower.Contains("rct"))
                {
                    return Ok(new {
                        ai_response = $"Doctor, currently in your practice, {stats.RootCanalPatients} patient(s) have active Root Canal indications or completed endodontic treatment out of {stats.TotalPatients} total registered patients.",
                        card_type = "clinic_stats",
                        stats_data = stats
                    });
                }
                if (lower.Contains("damage") || lower.Contains("decay") || lower.Contains("caries") || lower.Contains("cavity"))
                {
                    return Ok(new {
                        ai_response = $"Doctor, our clinic records show that {stats.DamagedTeethPatients} patient(s) currently have active Damaged or Carious teeth requiring restorative intervention.",
                        card_type = "clinic_stats",
                        stats_data = stats
                    });
                }
                if (lower.Contains("brace") || lower.Contains("ortho"))
                {
                    return Ok(new {
                        ai_response = $"Doctor, you have {stats.BracesPatients} patient(s) actively undergoing Orthodontic alignment and braces treatment.",
                        card_type = "clinic_stats",
                        stats_data = stats
                    });
                }
                if (lower.Contains("whiten"))
                {
                    return Ok(new {
                        ai_response = $"Doctor, there are {stats.WhiteningPatients} patient(s) currently enrolled in cosmetic teeth whitening treatment plans.",
                        card_type = "clinic_stats",
                        stats_data = stats
                    });
                }
                if (lower.Contains("miss") || lower.Contains("extract"))
                {
                    return Ok(new {
                        ai_response = $"Doctor, across your clinic registry, {stats.MissingTeethPatients} patient(s) have documented missing or extracted teeth in their odontogram charts.",
                        card_type = "clinic_stats",
                        stats_data = stats
                    });
                }

                // General clinic summary
                return Ok(new {
                    ai_response = $"Doctor, here is your clinical registry overview:\n• Total Registered Patients: {stats.TotalPatients}\n• Root Canal Cases: {stats.RootCanalPatients}\n• Active Decay/Caries: {stats.DamagedTeethPatients}\n• Active Orthodontics: {stats.BracesPatients}\n• Appointments Today: {stats.TodayAppointmentsCount}",
                    card_type = "clinic_stats",
                    stats_data = stats
                });
            }

            // =========================================================================
            // PILLAR 5: PATIENT DIRECTORY & FILTERED LISTINGS
            // =========================================================================
            if (System.Text.RegularExpressions.Regex.IsMatch(lower, @"^(list|show|give me|all).*(patients|patient directory|all patient)") && !lower.Contains("detail of") && !lower.Contains("record for"))
            {
                string filter = "all";
                if (lower.Contains("female")) filter = "female";
                else if (lower.Contains("male")) filter = "male";
                else if (lower.Contains("brace") || lower.Contains("ortho")) filter = "braces";
                else if (lower.Contains("whiten")) filter = "whitening";
                else if (lower.Contains("active")) filter = "active_plans";

                var patients = (await _repository.GetFilteredPatientsListAsync(filter)).ToList();
                return Ok(new {
                    ai_response = $"Doctor, I found {patients.Count} registered patient(s) in your clinic matching the '{filter}' filter. You can click any patient to open their complete chart.",
                    card_type = "patient_list",
                    patients = patients
                });
            }

            // =========================================================================
            // PILLAR 1 & 2: SPECIFIC PATIENT DOSSIER & CLINICAL DETAIL QUERIES
            // (Checks for specific patient mentions first before general clinic lookups)
            // =========================================================================
            var dossier = await _repository.GetFullPatientDossierAsync(lower, request.DoctorId);
            if (dossier == null && request.PatientId.HasValue && request.PatientId.Value > 0)
            {
                dossier = await _repository.GetFullPatientDossierAsync(request.PatientId.Value.ToString(), request.DoctorId);
            }

            if (dossier != null && dossier.Patient != null)
            {
                var p = dossier.Patient;
                int patientAge = Math.Max(0, DateTime.Now.Year - p.DOB.Year);
                bool isPediatric = patientAge < 6;
                int totalDentition = isPediatric ? 20 : 32;

                var teethList = dossier.Teeth.ToList();
                int healthyCount = teethList.Count(t => (t.ConditionStatus ?? "").ToLower().Contains("healthy") || t.ConditionColor == "#10B981");
                if (healthyCount == 0 && teethList.Count == 0) {
                    healthyCount = totalDentition;
                }
                var damagedTeeth = teethList.Where(t => (t.ConditionStatus ?? "").ToLower().Contains("decay") || (t.ConditionStatus ?? "").ToLower().Contains("damage") || (t.ConditionStatus ?? "").ToLower().Contains("caries") || (t.ConditionStatus ?? "").ToLower().Contains("cavity") || (t.ConditionStatus ?? "").ToLower().Contains("ecc") || t.ConditionColor == "#EF4444").Select(t => $"#{t.ToothNumber}").ToList();
                var rctTeeth = teethList.Where(t => (t.ConditionStatus ?? "").ToLower().Contains("root canal") || (t.ConditionStatus ?? "").ToLower().Contains("rct") || (t.ConditionStatus ?? "").ToLower().Contains("endo") || (t.ConditionStatus ?? "").ToLower().Contains("pulpotomy") || (t.ConditionStatus ?? "").ToLower().Contains("mta") || t.ConditionColor == "#F59E0B" || t.ConditionColor == "#7C3AED").Select(t => $"#{t.ToothNumber}").ToList();
                var missingTeeth = teethList.Where(t => (t.ConditionStatus ?? "").ToLower().Contains("miss") || (t.ConditionStatus ?? "").ToLower().Contains("extract") || (t.ConditionStatus ?? "").ToLower().Contains("exfoliat")).Select(t => $"#{t.ToothNumber}").ToList();
                var filledTeeth = teethList.Where(t => (t.ConditionStatus ?? "").ToLower().Contains("treat") || (t.ConditionStatus ?? "").ToLower().Contains("fill") || (t.ConditionStatus ?? "").ToLower().Contains("crown") || (t.ConditionStatus ?? "").ToLower().Contains("bridge") || (t.ConditionStatus ?? "").ToLower().Contains("ssc") || (t.ConditionStatus ?? "").ToLower().Contains("space")).Select(t => $"#{t.ToothNumber}").ToList();

                var nextAppt = dossier.Appointments.FirstOrDefault(a => a.PreferredDate >= DateTime.Now);
                var latestRx = dossier.Prescriptions.Select(r => r.MedicineName).Take(3).ToList();

                // Specific question formulations
                string specificAnswer = "";

                // 1. Shade / Tooth Color
                if (lower.Contains("shade") || lower.Contains("color") || lower.Contains("target shade"))
                {
                    specificAnswer = $"Doctor, the selected target shade for {p.FirstName} {p.LastName} is '{p.TargetShade ?? "A1"}' (Active Plan: {p.CurrentTreatmentPlan ?? "Routine Consultation"}).";
                }
                // 2. Braces / Alignment Stage
                else if (lower.Contains("stage") || (lower.Contains("brace") && !lower.Contains("how many")) || (lower.Contains("ortho") && !lower.Contains("how many")))
                {
                    specificAnswer = $"Doctor, {p.FirstName} {p.LastName} is currently on '{p.TreatmentStage ?? "Stage 1 (Initial Alignment)"}' for their {p.CurrentTreatmentPlan ?? "Orthodontic"} treatment.";
                }
                // 3. Root Canal Inquiry
                else if (lower.Contains("root canal") || lower.Contains("rct"))
                {
                    if (lower.Contains("when") || lower.Contains("date") || lower.Contains("did we do") || lower.Contains("was done"))
                    {
                        var rctLog = dossier.ClinicalLogs?.FirstOrDefault(l => (l.Message ?? "").ToLower().Contains("root canal") || (l.Message ?? "").ToLower().Contains("rct"));
                        string dateStr = rctLog != null 
                            ? rctLog.CreatedAt.ToString("dd MMM yyyy") 
                            : (nextAppt != null ? nextAppt.PreferredDate.ToString("dd MMM yyyy") : "the recent clinic session");

                        if (rctTeeth.Count > 0)
                        {
                            specificAnswer = $"Doctor, {p.FirstName} {p.LastName}'s Root Canal procedure for Tooth {string.Join(", ", rctTeeth)} was recorded around {dateStr} under their active treatment plan '{p.CurrentTreatmentPlan ?? "Endodontics"}'.";
                        }
                        else
                        {
                            specificAnswer = $"Doctor, {p.FirstName} {p.LastName} does not have any recorded history of Root Canal procedures.";
                        }
                    }
                    else if (rctTeeth.Count > 0)
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has Root Canal indicated or recorded on Tooth {string.Join(", ", rctTeeth)}. Their active treatment plan is '{p.CurrentTreatmentPlan ?? "Routine Dental Care"}' ({p.TreatmentStage ?? "Stage 1"}).";
                    }
                    else
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} does NOT currently require a Root Canal based on their {(isPediatric ? "20-tooth pediatric primary" : "32-tooth")} chart. (Active Plan: {p.CurrentTreatmentPlan ?? "Routine Consultation"}).";
                    }
                }
                // 4. Caries / Cavities / Damage
                else if (lower.Contains("damage") || lower.Contains("decay") || lower.Contains("caries") || lower.Contains("cavity") || lower.Contains("cavities"))
                {
                    if (damagedTeeth.Count > 0)
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has active caries/decay recorded on Tooth {string.Join(", ", damagedTeeth)}.";
                    }
                    else
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has no active caries or cavities recorded on their dental chart.";
                    }
                }
                // 5. Fillings / Restorations
                else if (lower.Contains("filling") || lower.Contains("fill") || lower.Contains("restor") || lower.Contains("crown"))
                {
                    if (filledTeeth.Count > 0)
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has completed restorations/fillings on Tooth {string.Join(", ", filledTeeth)}.";
                    }
                    else
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} currently has no recorded fillings or crown restorations.";
                    }
                }
                // 6. Missing Teeth
                else if (lower.Contains("miss") || lower.Contains("extract"))
                {
                    if (missingTeeth.Count > 0)
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has missing/extracted Tooth {string.Join(", ", missingTeeth)}.";
                    }
                    else
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} has no missing teeth charted (all {totalDentition} teeth present).";
                    }
                }
                // 7. Next Appointment for Patient
                else if (lower.Contains("appointment") || lower.Contains("visit") || lower.Contains("when"))
                {
                    if (nextAppt != null)
                    {
                        specificAnswer = $"Doctor, the next scheduled appointment for {p.FirstName} {p.LastName} is on {nextAppt.PreferredDate:dd MMM yyyy 'at' hh:mm tt} ({nextAppt.Status ?? "Confirmed"}).";
                    }
                    else
                    {
                        specificAnswer = $"Doctor, {p.FirstName} {p.LastName} does not have any upcoming appointments scheduled at this moment.";
                    }
                }
                // 8. Medical & Treatment History
                else if (lower.Contains("medical history") || lower.Contains("treatment history") || lower.Contains("clinical history") || lower.Contains("history"))
                {
                    string rctSummary = rctTeeth.Count > 0 ? $"Root Canal procedures on Tooth {string.Join(", ", rctTeeth)}" : "no endodontic therapy";
                    string cariesSummary = damagedTeeth.Count > 0 ? $"active caries monitoring on Tooth {string.Join(", ", damagedTeeth)}" : "no active caries";
                    string rxSummary = latestRx.Count > 0 ? $"Prescribed medications include: {string.Join(", ", latestRx)}." : "No active prescriptions on file.";
                    
                    specificAnswer = $"Doctor, {p.FirstName} {p.LastName}'s medical & clinical history includes an active plan for '{(!string.IsNullOrWhiteSpace(p.CurrentTreatmentPlan) ? p.CurrentTreatmentPlan : "Routine Dental Care")}' ({(!string.IsNullOrWhiteSpace(p.TreatmentStage) ? p.TreatmentStage : "Initial Phase")}), with {rctSummary} and {cariesSummary}. {rxSummary}";
                }
                // 9. Who is / Bio Profile
                else if (lower.Contains("who is") || lower.Contains("tell me about") || lower.Contains("profile"))
                {
                    specificAnswer = $"Doctor, {p.FirstName} {p.LastName} is a {patientAge}-year-old {p.Gender ?? "patient"} (Patient ID #{p.PatientID}) currently enrolled in '{(!string.IsNullOrWhiteSpace(p.CurrentTreatmentPlan) ? p.CurrentTreatmentPlan : "Routine Dental Care")}'. Target shade is '{(!string.IsNullOrWhiteSpace(p.TargetShade) ? p.TargetShade : "A1 Natural")}' with {healthyCount}/{totalDentition} healthy {(isPediatric ? "primary " : "")}teeth charted.";
                }
                // 10. Demographics / Age / Phone / Email
                else if (lower.Contains("age") || lower.Contains("old"))
                {
                    specificAnswer = $"Doctor, {p.FirstName} {p.LastName} is {patientAge} years old (DOB: {p.DOB:dd MMM yyyy}, Gender: {p.Gender ?? "N/A"}).";
                }
                else if (lower.Contains("phone") || lower.Contains("contact") || lower.Contains("number"))
                {
                    specificAnswer = $"Doctor, the contact number for {p.FirstName} {p.LastName} is {p.Phone ?? "Not recorded"} (Email: {p.Email ?? "N/A"}).";
                }

                string planStr = !string.IsNullOrWhiteSpace(p.CurrentTreatmentPlan) ? p.CurrentTreatmentPlan : "Routine Dental Care";
                string stageStr = !string.IsNullOrWhiteSpace(p.TreatmentStage) ? $" ({p.TreatmentStage})" : " (Active)";
                string shadeStr = !string.IsNullOrWhiteSpace(p.TargetShade) ? p.TargetShade : "A1 Natural";

                string spokenSummary = !string.IsNullOrEmpty(specificAnswer)
                    ? specificAnswer
                    : $"Doctor, here is the clinical dossier for {p.FirstName} {p.LastName} (Patient ID #{p.PatientID}):\n• Demographics: {patientAge} Years, {p.Gender ?? "N/A"}\n• Active Treatment: {planStr}{stageStr}\n• Selected Shade: {shadeStr}\n• Odontogram: {healthyCount}/{totalDentition} Healthy, {damagedTeeth.Count} Decayed {(damagedTeeth.Count > 0 ? $"({string.Join(", ", damagedTeeth)})" : "")}, {filledTeeth.Count} Filled/Restored, {rctTeeth.Count} Root Canal Cases\n• Next Scheduled Visit: {(nextAppt != null ? nextAppt.PreferredDate.ToString("dd MMM yyyy 'at' hh:mm tt") : "None scheduled")}";

                return Ok(new {
                    ai_response = spokenSummary,
                    card_type = "patient_dossier",
                    patient_dossier = new {
                        patientId = p.PatientID,
                        fullName = $"{p.FirstName} {p.LastName}",
                        age = patientAge,
                        gender = p.Gender ?? "N/A",
                        phone = p.Phone ?? "N/A",
                        email = p.Email ?? "N/A",
                        address = p.Address ?? "N/A",
                        currentTreatmentPlan = !string.IsNullOrWhiteSpace(p.CurrentTreatmentPlan) ? p.CurrentTreatmentPlan : "Routine Dental Care",
                        treatmentStage = !string.IsNullOrWhiteSpace(p.TreatmentStage) ? p.TreatmentStage : "Active Phase",
                        targetShade = !string.IsNullOrWhiteSpace(p.TargetShade) ? p.TargetShade : "A1 Natural",
                        totalTeethCount = totalDentition,
                        healthyTeethCount = Math.Max(0, totalDentition - (damagedTeeth.Count + rctTeeth.Count + missingTeeth.Count)),
                        damagedTeethCount = damagedTeeth.Count,
                        damagedTeeth = damagedTeeth,
                        rctTeeth = rctTeeth,
                        filledTeeth = filledTeeth,
                        missingTeeth = missingTeeth,
                        nextAppointment = nextAppt != null ? nextAppt.PreferredDate.ToString("dd MMM yyyy, hh:mm tt") : "None Scheduled",
                        prescriptions = latestRx,
                        chartUrl = $"/chart/{p.PatientID}"
                    }
                });
            }

            // If query explicitly asked about a patient or patient profile but patient not found
            var explicitPatientKeywords = new[] {
                "detail of", "record for", "history of", "profile of", "dossier for", "tell me about",
                "pull up", "bring up", "everything on", "everything about", "who is", "find patient",
                "shade was selected for", "shade for", "stage of braces for", "stage for", "cavities for",
                "caries for", "root canal for", "fillings for", "has", "does"
            };

            if (explicitPatientKeywords.Any(k => lower.Contains(k)) && !lower.Contains("appointment") && !lower.Contains("how many") && !lower.Contains("how much"))
            {
                return Ok(new {
                    ai_response = "Doctor, I could not find a registered patient matching that name in your clinic database. You can search the full registry or check upcoming schedules below:",
                    card_type = "not_found_suggestions",
                    suggestions = new[] {
                        "List all my patients",
                        "How many appointments today?",
                        "How many patients have root canal?",
                        "Can you pull up everything on Sana?"
                    }
                });
            }

            // =========================================================================
            // PILLAR 4: GENERAL APPOINTMENTS & CLINIC SCHEDULE INQUIRIES
            // =========================================================================
            if (lower.Contains("appointment") || lower.Contains("schedule") || lower.Contains("booked") || lower.Contains("booking"))
            {
                bool todayOnly = lower.Contains("today") || lower.Contains("aaj");
                string? dateText = null;

                // Extract specific dates like "13 aug", "28 august", "13-08", etc.
                var dateMatch = System.Text.RegularExpressions.Regex.Match(lower, @"\b(\d{1,2}(?:st|nd|rd|th)?\s+(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*|\d{4}-\d{2}-\d{2})\b");
                if (dateMatch.Success)
                {
                    dateText = dateMatch.Groups[1].Value;
                }

                var appts = (await _repository.GetAppointmentsByDateQueryAsync(null, todayOnly, dateText)).ToList();

                string responseMsg;
                if (appts.Count == 0)
                {
                    string target = todayOnly ? "for today" : (dateText != null ? $"for {dateText}" : "in the upcoming schedule");
                    responseMsg = $"Doctor, you have no appointments scheduled {target}.";
                }
                else
                {
                    string target = todayOnly ? "for today" : (dateText != null ? $"on {dateText}" : "in your upcoming schedule");
                    var summaryList = appts.Select(a => $"{a.FullName ?? "Patient"} at {a.PreferredDate:hh:mm tt} ({a.Status ?? "Confirmed"})");
                    responseMsg = $"Doctor, you have {appts.Count} appointment(s) scheduled {target}:\n• {string.Join("\n• ", summaryList)}";
                }

                return Ok(new {
                    ai_response = responseMsg,
                    card_type = "appointment_list",
                    appointments = appts
                });
            }

            // =========================================================================
            // STANDARD CLINICAL COMMANDS & GEMINI LLM FALLTHROUGH
            // =========================================================================
            if (string.IsNullOrEmpty(ApiKey) || (!ApiKey.StartsWith("AIzaSy") && !ApiKey.StartsWith("AQ.")))
            {
                return FallbackRegexParser(request.Text);
            }

            string userRegion = string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region;
            var promptTemplate = await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Prompts", "SystemPrompt.txt"));
            
            string regionalData = "No specific regional data found.";
            try {
                var drugsJsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "drugs.json");
                if (System.IO.File.Exists(drugsJsonPath)) {
                    var jsonContent = await System.IO.File.ReadAllTextAsync(drugsJsonPath);
                    using var drugsDoc = JsonDocument.Parse(jsonContent);
                    var drugIndex = drugsDoc.RootElement.GetProperty("drug_index");
                    
                    var sb = new StringBuilder();
                    foreach (var prop in drugIndex.EnumerateObject()) {
                        string drugName = prop.Name;
                        var drugData = prop.Value;
                        string generic = drugData.GetProperty("generic").GetString();
                        string sdcep = drugData.GetProperty("sdcep_guidelines").GetString();
                        
                        if (drugData.TryGetProperty("regions", out JsonElement regions) && regions.TryGetProperty(userRegion, out JsonElement regionData)) {
                            sb.AppendLine($"- {drugName} (Generic: {generic})");
                            sb.AppendLine($"  SDCEP Guidelines: {sdcep}");
                            if (regionData.TryGetProperty("standard_brand_names", out JsonElement brands)) {
                                sb.AppendLine($"  Local Brands ({userRegion}): " + string.Join(", ", brands.EnumerateArray().Select(b => b.GetString())));
                            }
                            if (regionData.TryGetProperty("legal_limits", out JsonElement limits)) {
                                sb.AppendLine($"  Legal Limits: {limits.GetString()}");
                            }
                            if (regionData.TryGetProperty("notes", out JsonElement notes)) {
                                sb.AppendLine($"  Notes: {notes.GetString()}");
                            }
                        }
                    }
                    if (sb.Length > 0) {
                        regionalData = sb.ToString();
                    }
                }
            } catch (System.Exception ex) {
                System.Console.WriteLine("Error reading drugs.json: " + ex.Message);
            }

            var prompt = promptTemplate.Replace("{USER_INPUT}", request.Text);
            prompt += $"\n\n### CURRENT REGULATORY REGION: {userRegion}\n";
            if (userRegion == "NZ") {
                prompt += "- Reference the New Zealand Formulary (NZF), NZ Heart Foundation infective endocarditis prophylaxis, and legal limits for prescription duration.\n";
            } else if (userRegion == "PK") {
                prompt += "- Reference DRAP approved brand names and standard local PMDC clinical compliance.\n";
            }
            prompt += "\n### LOCAL DRUG DIRECTORY:\n" + regionalData;
            
            if (!string.IsNullOrEmpty(request.TeethContext)) {
                prompt += "\n\n### PATIENT CURRENT TEETH CHART:\n" + request.TeethContext;
            }
            
            prompt += "\n\nINPUT:\n'" + request.Text + "'";
            
            using var client = new HttpClient();
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, GeminiEndpoint);
            requestMsg.Headers.Add("x-goog-api-key", ApiKey);
            requestMsg.Content = content;
            var response = await client.SendAsync(requestMsg);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return FallbackRegexParser(request.Text);
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            
            try 
            {
                var partsArray = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
                string textResult = "";
                foreach (var part in partsArray.EnumerateArray())
                {
                    if (!part.TryGetProperty("thought", out _))
                    {
                        textResult = part.GetProperty("text").GetString();
                        break;
                    }
                }
                textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
                var parsedJson = JsonSerializer.Deserialize<object>(textResult);
                return Ok(parsedJson);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Gemini parsing failed: {ex.Message}");
                return FallbackRegexParser(request.Text);
            }
        }

        private IActionResult FallbackRegexParser(string text)
        {
            text = text.ToLower();
            var prescriptionsList = new System.Collections.Generic.List<string>();
            var words = text.Split(new[] { ' ', ',', '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words) {
                if (_medicineDictionary.Contains(word)) {
                    string properName = char.ToUpper(word[0]) + word.Substring(1).ToLower();
                    if (!prescriptionsList.Contains(properName)) {
                        prescriptionsList.Add(properName);
                    }
                }
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, @"(?:tooth|teeth|#|number|daant)\s*(?:number|no\.?)?\s*(\d+)");
            int toothNumber = 0;
            if (match.Success) {
                int.TryParse(match.Groups[1].Value, out toothNumber);
            }

            string status = "Healthy";
            string color = "Green";
            string hexCode = "#10B981";

            if (text.Contains("damage") || text.Contains("decay") || text.Contains("caries") || text.Contains("cavity") || text.Contains("kera")) {
                status = "Damaged/Decay";
                color = "Red";
                hexCode = "#EF4444";
            } else if (text.Contains("root canal") || text.Contains("rct")) {
                status = "Root Canal Needed";
                color = "Orange";
                hexCode = "#F59E0B";
            } else if (text.Contains("clean") || text.Contains("scaling")) {
                status = "Cleaning Needed";
                color = "Blue";
                hexCode = "#3B82F6";
            } else if (text.Contains("crown") || text.Contains("bridge") || text.Contains("fill")) {
                status = "Already Treated";
                color = "Purple";
                hexCode = "#8B5CF6";
            } else if (text.Contains("miss") || text.Contains("extract")) {
                status = "Missing";
                color = "Grey";
                hexCode = "#94A3B8";
            }

            var toothUpdates = new System.Collections.Generic.List<object>();
            if (toothNumber >= 1 && toothNumber <= 32) {
                toothUpdates.Add(new {
                    tooth_number = toothNumber,
                    color = color,
                    status = status,
                    hex_code = hexCode,
                    condition = text
                });
            }

            return Ok(new {
                tooth_updates = toothUpdates.ToArray(),
                prescriptions = prescriptionsList.ToArray(),
                history_inquiries = new int[0],
                ai_response = toothUpdates.Count > 0 
                    ? $"Marked Tooth #{toothNumber} as {status}." 
                    : (prescriptionsList.Count > 0 ? $"Prescribed {string.Join(", ", prescriptionsList)}." : "Processed your clinical command.")
            });
        }
    }

    public class VoiceCommandRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? TeethContext { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
    }
}
