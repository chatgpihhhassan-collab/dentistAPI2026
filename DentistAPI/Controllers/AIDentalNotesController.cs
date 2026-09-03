using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentistAPI.Models;
using DentistAPI.Repositories;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/ai-dental-notes")]
    public class AIDentalNotesController : ControllerBase
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, byte> _processingAudios = new();
        private readonly IAIDentalNotesRepository _repository;
        private readonly ISpeechToTextService _sttService;
        private readonly IGeminiDentalNotesService _geminiService;
        private readonly DentalRepository _dentalRepository;
        private readonly ILogger<AIDentalNotesController> _logger;

        public AIDentalNotesController(
            IAIDentalNotesRepository repository,
            ISpeechToTextService sttService,
            IGeminiDentalNotesService geminiService,
            DentalRepository dentalRepository,
            ILogger<AIDentalNotesController> logger)
        {
            _repository = repository;
            _sttService = sttService;
            _geminiService = geminiService;
            _dentalRepository = dentalRepository;
            _logger = logger;
        }

        [HttpPost("recordings")]
        public async Task<IActionResult> ProcessRecording(
            [FromForm] IFormFile? audio, 
            [FromForm] long patientId, 
            [FromForm] long dentistId,
            [FromForm] int durationSeconds = 0)
        {
            _logger.LogInformation($"[CONTROLLER LOG] ProcessRecording started for PatientId: {patientId}, DentistId: {dentistId}, durationSeconds parameter: {durationSeconds}");
            try
            {
                var session = new DentalNoteSession { PatientId = patientId, DentistId = dentistId, StartedAt = DateTime.UtcNow };
                var sessionId = await _repository.CreateSessionAsync(session);
                _logger.LogInformation($"[CONTROLLER LOG] Session created with Id: {sessionId}");

                string transcriptText = "";
                string mimeType = "audio/webm";
                string sha256Hash = "";

                if (audio == null || audio.Length == 0)
                {
                    transcriptText = Request.Form["transcript"].ToString();
                    if (string.IsNullOrEmpty(transcriptText))
                    {
                        _logger.LogWarning("[CONTROLLER LOG] BadRequest: Both audio file and text transcript form fields are null or empty.");
                        return BadRequest("No audio or transcript provided.");
                    }
                    _logger.LogInformation($"[CONTROLLER LOG] No audio file uploaded. Using text transcript from form payload: '{transcriptText}'");
                    sha256Hash = "transcribed-locally";
                }
                else
                {
                    _logger.LogInformation($"[CONTROLLER LOG] Audio file received. Length: {audio.Length} bytes, Mime Type: {audio.ContentType}");
                    using var ms = new MemoryStream();
                    await audio.CopyToAsync(ms);
                    var audioBytes = ms.ToArray();
                    sha256Hash = Convert.ToHexString(SHA256.HashData(audioBytes)).ToLower();
                    mimeType = audio.ContentType ?? "audio/webm";

                    _logger.LogInformation($"[CONTROLLER LOG] Computed audio file SHA256 Hash: {sha256Hash}. Invoking speech-to-text service...");
                    transcriptText = await _sttService.TranscribeAudioAsync(audioBytes, mimeType);
                    _logger.LogInformation($"[CONTROLLER LOG] Speech-to-text transcription completed. Result text: '{transcriptText}'");
                }

                if (string.IsNullOrEmpty(transcriptText))
                {
                    _logger.LogWarning("[CONTROLLER LOG] Transcription text is empty. Falling back to default message.");
                    transcriptText = "[No speech detected]";
                }

                _logger.LogInformation($"[CONTROLLER LOG] Saving transcription to database for SessionId: {sessionId}...");
                await _repository.CreateTranscriptAsync(sessionId, transcriptText);

                _logger.LogInformation($"[CONTROLLER LOG] Saving audio recording record to database. SessionId: {sessionId}, MimeType: {mimeType}, DurationSeconds: {durationSeconds}, Hash: {sha256Hash}");
                var audioId = await _repository.SaveAudioRecordingAsync(
                    sessionId: sessionId,
                    mimeType: mimeType,
                    durationSeconds: durationSeconds,
                    sha256Hash: sha256Hash);

                _logger.LogInformation($"[CONTROLLER LOG] ProcessRecording finished successfully. SessionId: {sessionId}, AudioId: {audioId}");
                return Ok(new
                {
                    SessionId = sessionId,
                    AudioId = audioId,
                    Transcript = transcriptText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ProcessRecording.");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("process-lazy")]
        public async Task<IActionResult> ProcessLazy([FromBody] LazyProcessRequest request)
        {
            _logger.LogInformation($"ProcessLazy called for PatientId: {request.PatientId}");
            try
            {
                var latestAudio = await _repository.GetLatestAudioRecordingByPatientIdAsync(request.PatientId);
                if (latestAudio == null)
                {
                    return NotFound("No audio recording found for this patient.");
                }

                var existingNote = await _repository.GetDentalNoteByAudioIdAsync(latestAudio.AudioId);
                if (existingNote != null)
                {
                    _logger.LogInformation($"DentalNote already exists for AudioId: {latestAudio.AudioId}. Returning existing NoteId: {existingNote.NoteId}");
                    return Ok(new
                    {
                        Status = existingNote.Status,
                        MissingFields = new string[0],
                        DoctorPrompt = "",
                        NoteId = existingNote.NoteId,
                        SessionId = existingNote.SessionId,
                        AudioId = latestAudio.AudioId,
                        Note = existingNote
                    });
                }

                // Concurrency lock on AudioId to prevent duplicate compilation
                if (!_processingAudios.TryAdd(latestAudio.AudioId, 0))
                {
                    // Another request is actively compiling this audio, wait up to 15s for it to finish and return
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(500);
                        var checkNote = await _repository.GetDentalNoteByAudioIdAsync(latestAudio.AudioId);
                        if (checkNote != null)
                        {
                            return Ok(new
                            {
                                Status = checkNote.Status,
                                MissingFields = new string[0],
                                DoctorPrompt = "",
                                NoteId = checkNote.NoteId,
                                SessionId = checkNote.SessionId,
                                AudioId = latestAudio.AudioId,
                                Note = checkNote
                            });
                        }
                    }
                }

                try
                {
                    // Re-check after acquiring lock
                    var doubleCheck = await _repository.GetDentalNoteByAudioIdAsync(latestAudio.AudioId);
                    if (doubleCheck != null)
                    {
                        return Ok(new
                        {
                            Status = doubleCheck.Status,
                            MissingFields = new string[0],
                            DoctorPrompt = "",
                            NoteId = doubleCheck.NoteId,
                            SessionId = doubleCheck.SessionId,
                            AudioId = latestAudio.AudioId,
                            Note = doubleCheck
                        });
                    }

                    var transcriptText = await _repository.GetTranscriptTextBySessionIdAsync(latestAudio.SessionId);
                    if (string.IsNullOrEmpty(transcriptText))
                    {
                        return BadRequest("No transcript text found for the latest recording.");
                    }

                    var doctor = await _dentalRepository.GetDoctorByIdAsync((int)request.DentistId);
                    string doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Doctor";

                    var patient = await _dentalRepository.GetPatientByIdAsync((int)request.PatientId);
                    string patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "the patient";

                    _logger.LogInformation($"Compiling clinical note with Gemini for AudioId: {latestAudio.AudioId}...");
                    var result = await _geminiService.GenerateClinicalNoteAsync(transcriptText, request.PatientId, request.DentistId, doctorName, patientName, doctor?.Region ?? "NZ");
                    if (result == null || result.DraftNote == null)
                    {
                        return StatusCode(500, "Gemini failed to generate clinical notes.");
                    }

                    result.DraftNote.SessionId = latestAudio.SessionId;
                    result.DraftNote.AudioId = latestAudio.AudioId;

                    var noteId = await _repository.SaveDentalNoteAsync(result.DraftNote);
                    _logger.LogInformation($"Lazy processed successfully. NoteId: {noteId}, AudioId: {latestAudio.AudioId}");

                    // Auto-apply tooth status updates from Voice command
                    if (result.TeethUpdates != null && result.TeethUpdates.Count > 0)
                    {
                        _logger.LogInformation($"[CONTROLLER LOG] Found {result.TeethUpdates.Count} teeth updates from speech. Applying to patient chart...");
                        foreach (var update in result.TeethUpdates)
                        {
                            string color = GetColorForStatus(update.Status);
                            _logger.LogInformation($"[CONTROLLER LOG] Voice Command: Tooth {update.ToothNumber} status set to {update.Status} (Color: {color})");
                            await _dentalRepository.UpdateTeethStateBulkAsync(
                                (int)request.PatientId,
                                update.ToothNumber,
                                color,
                                update.Status,
                                update.Status,
                                "Auto-updated from Doctor speech command"
                            );
                        }
                    }

                    // Auto-sync prescriptions to patient record in database
                    if (result.DraftNote?.Prescriptions != null && result.DraftNote.Prescriptions.Count > 0)
                    {
                        _logger.LogInformation($"[CONTROLLER LOG] Syncing {result.DraftNote.Prescriptions.Count} voice-prescribed medications to Patient #{request.PatientId} records...");
                        foreach (var rx in result.DraftNote.Prescriptions)
                        {
                            string medSummary = $"{rx.MedicationName} {rx.Strength}".Trim();
                            if (!string.IsNullOrEmpty(rx.Frequency)) medSummary += $" - {rx.Frequency}";
                            if (!string.IsNullOrEmpty(rx.Duration)) medSummary += $" ({rx.Duration})";
                            await _dentalRepository.AddPrescriptionAsync((int)request.PatientId, medSummary);
                            await _dentalRepository.AddClinicalLogAsync((int)request.PatientId, (int)request.DentistId, $"Prescribed: {medSummary}", "Prescription");
                        }
                    }

                    // Auto-record Note creation in ClinicalLogs
                    await _dentalRepository.AddClinicalLogAsync(
                        (int)request.PatientId,
                        (int)request.DentistId,
                        $"AI Clinical Note #{noteId} recorded: {result.DraftNote?.Summary ?? "Consultation recorded"}",
                        "Voice Scribe"
                    );

                    // Auto-schedule follow-up appointment if dictated by doctor
                    var (parsedApptDate, apptStatus, apptReason) = ParseFollowUpVoiceSchedule(transcriptText, result.DraftNote?.FollowUp ?? "", result.DraftNote?.Assessment ?? "");

                    if (parsedApptDate.HasValue && patient != null)
                    {
                        DateTime apptDate = parsedApptDate.Value;
                        var existingAppt = await _dentalRepository.GetAppointmentsByDateQueryAsync(apptDate.Date);
                        bool alreadyBooked = existingAppt.Any(a => (a.FullName ?? "").Equals($"{patient.FirstName} {patient.LastName}", StringComparison.OrdinalIgnoreCase) && Math.Abs((a.PreferredDate - apptDate).TotalHours) < 4);

                        if (!alreadyBooked)
                        {
                            var newAppt = new Appointment
                            {
                                FullName = $"{patient.FirstName} {patient.LastName}",
                                Phone = patient.Phone ?? "N/A",
                                Email = patient.Email ?? "patient@dentalstudio.com",
                                PreferredDate = apptDate,
                                CreatedAt = DateTime.UtcNow,
                                Status = apptStatus,
                                Reason = apptReason,
                                DoctorID = (int)request.DentistId
                            };

                            await _dentalRepository.CreateAppointmentDirectAsync(newAppt);
                            await _dentalRepository.AddClinicalLogAsync(
                                (int)request.PatientId,
                                (int)request.DentistId,
                                $"Follow-up appointment scheduled for {apptDate:MMM dd, yyyy hh:mm tt} ({apptReason})",
                                "Appointment"
                            );
                            _logger.LogInformation($"[CONTROLLER LOG] Voice Schedule: Created appointment for {newAppt.FullName} on {apptDate:yyyy-MM-dd HH:mm} (Status: {apptStatus}, Reason: {apptReason})");
                        }
                    }

                    return Ok(new
                    {
                        Status = result.Status,
                        MissingFields = result.MissingFields,
                        DoctorPrompt = result.DoctorPrompt,
                        NoteId = noteId,
                        SessionId = latestAudio.SessionId,
                        AudioId = latestAudio.AudioId,
                        Transcript = transcriptText,
                        Note = result.DraftNote
                    });
                }
                finally
                {
                    _processingAudios.TryRemove(latestAudio.AudioId, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProcessLazy: {ex.Message}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private static (DateTime? ApptDate, string Status, string Reason) ParseFollowUpVoiceSchedule(string transcript, string followUpNote, string assessment)
        {
            string cleanTranscript = transcript.ToLower();
            string cleanFollowUp = followUpNote.ToLower();

            // Ignore negative/missing indicators
            if (cleanFollowUp.Contains("missing") || cleanFollowUp.Contains("nahi") || cleanFollowUp == "n/a" || cleanFollowUp == "none")
            {
                cleanFollowUp = "";
            }

            string combined = (cleanTranscript + " " + cleanFollowUp).ToLower();

            // Require explicit return/schedule intent with a timing marker
            bool hasReturnKeyword = combined.Contains("bulaya") || combined.Contains("wapas") || 
                                   combined.Contains("appointment") || combined.Contains("schedule") || 
                                   combined.Contains("call par") || combined.Contains("phone par") ||
                                   combined.Contains("review") || combined.Contains("followup") || combined.Contains("follow-up");

            bool hasTimingMarker = combined.Contains("haf") || combined.Contains("week") || 
                                  combined.Contains("din") || combined.Contains("day") ||
                                  combined.Contains("kal") || combined.Contains("parso") || 
                                  combined.Contains("tarikh") || combined.Contains("tareekh") || 
                                  combined.Contains("tomorrow") || System.Text.RegularExpressions.Regex.IsMatch(combined, @"\b\d{1,2}\s*(?:ko|th|st|nd|rd|baje)\b");

            if (!hasReturnKeyword || !hasTimingMarker) return (null, "Confirmed", "");

            DateTime baseDate = DateTime.Today;
            int daysToAdd = 7; // default 1 week
            int targetHour = 10; // default 10:00 AM
            int targetMinute = 0;
            string status = "Confirmed";

            // 1. Time of day parsing
            if (combined.Contains("sham") || combined.Contains("evening") || combined.Contains("raat"))
            {
                targetHour = 17; // 5:00 PM
            }
            else if (combined.Contains("dopahar") || combined.Contains("afternoon") || combined.Contains("tisray peher"))
            {
                targetHour = 14; // 2:00 PM
            }
            else if (combined.Contains("subah") || combined.Contains("morning") || combined.Contains("sawere"))
            {
                targetHour = 10; // 10:00 AM
            }

            // Check specific hour e.g. "5 baje", "11:00 am", "4 pm"
            var timeMatch = System.Text.RegularExpressions.Regex.Match(combined, @"\b(\d{1,2})(?::(\d{2}))?\s*(baje|am|pm)?\b");
            if (timeMatch.Success)
            {
                if (int.TryParse(timeMatch.Groups[1].Value, out int h) && h >= 1 && h <= 24)
                {
                    string modifier = timeMatch.Groups[3].Value.ToLower();
                    if (modifier == "pm" && h < 12) h += 12;
                    else if (modifier == "am" && h == 12) h = 0;
                    else if (modifier == "baje" && (combined.Contains("sham") || combined.Contains("dopahar") || combined.Contains("raat")) && h <= 7) h += 12;
                    
                    targetHour = h;
                    if (timeMatch.Groups[2].Success && int.TryParse(timeMatch.Groups[2].Value, out int m))
                    {
                        targetMinute = m;
                    }
                }
            }

            // 2. Relative Days Parsing
            if (combined.Contains("kal call") || combined.Contains("phone par") || combined.Contains("call par"))
            {
                daysToAdd = 1;
                status = "Pending"; // Call follow-up reminder
            }
            else if (combined.Contains("kal") || combined.Contains("tomorrow"))
            {
                daysToAdd = 1;
            }
            else if (combined.Contains("parson") || combined.Contains("parso") || combined.Contains("day after tomorrow"))
            {
                daysToAdd = 2;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(combined, @"\b(do|2)\s*din\s*baad\b") || combined.Contains("in 2 days"))
            {
                daysToAdd = 2;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(combined, @"\b(teen|3)\s*din\s*baad\b") || combined.Contains("in 3 days"))
            {
                daysToAdd = 3;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(combined, @"\b(char|chaar|4)\s*din\s*baad\b") || combined.Contains("in 4 days"))
            {
                daysToAdd = 4;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(combined, @"\b(panch|paanch|5)\s*din\s*baad\b") || combined.Contains("in 5 days"))
            {
                daysToAdd = 5;
            }
            else if (combined.Contains("do hafte") || combined.Contains("2 week") || combined.Contains("14 din"))
            {
                daysToAdd = 14;
            }
            else if (combined.Contains("aglay hafte") || combined.Contains("next week") || combined.Contains("ek hafte") || combined.Contains("1 week") || combined.Contains("7 din"))
            {
                daysToAdd = 7;
            }
            else
            {
                // Check specific calendar day (e.g. "28 ko", "28 tarikh", "28 tareekh", "28th", "15 aug")
                var dayMatch = System.Text.RegularExpressions.Regex.Match(combined, @"\b(\d{1,2})\s*(?:ko|tarikh|tareekh|th|st|nd|rd)?\b");
                if (dayMatch.Success && int.TryParse(dayMatch.Groups[1].Value, out int explicitDay) && explicitDay >= 1 && explicitDay <= 31)
                {
                    try
                    {
                        var candidate = new DateTime(baseDate.Year, baseDate.Month, explicitDay, targetHour, targetMinute, 0);
                        if (candidate < DateTime.Now) candidate = candidate.AddMonths(1);
                        return (candidate, status, BuildReason(assessment, followUpNote, combined));
                    }
                    catch {}
                }
            }

            DateTime finalDate = baseDate.AddDays(daysToAdd).AddHours(targetHour).AddMinutes(targetMinute);
            return (finalDate, status, BuildReason(assessment, followUpNote, combined));
        }

        private static string BuildReason(string assessment, string followUpNote, string combined)
        {
            if (combined.Contains("call par") || combined.Contains("phone"))
            {
                return "Call Follow-up: Schedule Root Canal Treatment Date";
            }
            if (combined.Contains("root canal") || combined.Contains("rct"))
            {
                return "Follow-up: Root Canal Treatment (RCT)";
            }
            if (!string.IsNullOrWhiteSpace(followUpNote))
            {
                return $"Follow-up: {followUpNote}";
            }
            if (!string.IsNullOrWhiteSpace(assessment))
            {
                return $"Follow-up: {assessment}";
            }
            return "Follow-up Dental Consultation";
        }

        [HttpGet("{noteId:long}")]
        public async Task<IActionResult> GetNote(long noteId)
        {
            _logger.LogInformation($"GetNote called for NoteId: {noteId}");
            try
            {
                var note = await _repository.GetDentalNoteByIdAsync(noteId);
                if (note == null) return NotFound($"Dental note not found for NoteId: {noteId}");
                return Ok(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetNote for NoteId: {noteId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("patient/{patientId:long}")]
        public async Task<IActionResult> GetNotesByPatient(int patientId, [FromQuery] int? dentistId, [FromQuery] bool includeDeleted = false)
        {
            _logger.LogInformation($"GetNotesByPatient called for PatientId: {patientId}, DentistId filter: {dentistId}, IncludeDeleted: {includeDeleted}");
            try
            {
                var notes = await _repository.GetDentalNotesByPatientIdAsync(patientId, dentistId, includeDeleted);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetNotesByPatient for PatientId: {patientId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{noteId:long}")]
        public async Task<IActionResult> DeleteNote(long noteId)
        {
            _logger.LogInformation($"DeleteNote called for NoteId: {noteId}");
            try
            {
                bool success = await _repository.SoftDeleteDentalNoteAsync(noteId, true);
                if (!success) return NotFound($"Dental note not found for NoteId: {noteId}");
                return Ok(new { message = "Dental note marked as deleted successfully.", NoteId = noteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteNote for NoteId: {noteId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPatch("{noteId:long}/restore")]
        public async Task<IActionResult> RestoreNote(long noteId)
        {
            _logger.LogInformation($"RestoreNote called for NoteId: {noteId}");
            try
            {
                bool success = await _repository.SoftDeleteDentalNoteAsync(noteId, false);
                if (!success) return NotFound($"Dental note not found for NoteId: {noteId}");
                return Ok(new { message = "Dental note restored successfully.", NoteId = noteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in RestoreNote for NoteId: {noteId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{noteId:long}")]
        public async Task<IActionResult> UpdateNote(long noteId, [FromBody] DentalNote updatedNote)
        {
            _logger.LogInformation($"UpdateNote called for NoteId: {noteId}");
            try
            {
                var existingNote = await _repository.GetDentalNoteByIdAsync(noteId);
                if (existingNote == null) return NotFound($"Dental note not found for NoteId: {noteId}");

                updatedNote.NoteId = noteId; // Ensure correct ID
                await _repository.UpdateDentalNoteAsync(updatedNote);
                return Ok(new { message = "Note updated successfully.", Note = updatedNote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateNote for NoteId: {noteId}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private string GetColorForStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Green";
            var s = status.ToUpper();
            if (s.Contains("HEALTHY")) return "Green";
            if (s.Contains("DECAY") || s.Contains("DAMAGE") || s.Contains("CARIES")) return "Red";
            if (s.Contains("ROOT CANAL") || s.Contains("ROOTCANAL") || s.Contains("PULPITIS")) return "Yellow";
            if (s.Contains("CLEAN")) return "Blue";
            if (s.Contains("TREAT") || s.Contains("FILL") || s.Contains("CROWN")) return "Purple";
            if (s.Contains("MISSING") || s.Contains("EXTRACT")) return "Grey";
            return "Green";
        }

        [HttpPost("{noteId:long}/approve")]
        public IActionResult ApproveNote(long noteId)
        {
            _logger.LogInformation($"ApproveNote called for NoteId: {noteId}");
            return Ok(new { message = "Note approved successfully." });
        }
    }

    public class LazyProcessRequest
    {
        public long PatientId { get; set; }
        public long DentistId { get; set; }
    }
}
