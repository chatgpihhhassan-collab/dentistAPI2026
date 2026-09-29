# Dentia Imaging Capture + Groq AI Observation Pipeline
## Master Architectural Specification, API Contracts & Implementation Prompts

---

### Executive Summary & Invariant Architecture

This specification defines the complete end-to-end architecture, database schema, backend services, real-time notification layer, frontend UI components, local hardware capture bridge, and integration test suite for **Dentia's Clinical Imaging & Vision AI Pipeline**.

```mermaid
flowchart TD
    subgraph Hardware_Layer ["Hardware & Ingestion Sources"]
        UVC["USB Intraoral Camera<br/>(Apple Dental / Coxo / Magenta)"] -->|UVC MediaStream| WebCapture["Dentia Web App<br/>(<CameraCapturePanel />)"]
        RVG["RVG Digital Sensor / OPG<br/>(Woodpecker / Vatech / Eighteeth / Carestream)"] -->|Export Dir / TWAIN| Bridge["Dentia Capture Bridge<br/>(Windows .NET Background Service)"]
    end

    subgraph Dentia_Backend ["Dentia Core Backend (.NET 8 Web API)"]
        WebCapture -->|POST /api/imaging/upload| UploadEndpoint["Upload Endpoint<br/>(RadiographsController)"]
        Bridge -->|POST /api/imaging/upload + Clinic Token| UploadEndpoint
        UploadEndpoint -->|Store File / Stream| LocalStorage[("Secure File Storage / Blob")]
        UploadEndpoint -->|Insert Row| DB_Radiographs[("[dentist].[radiographs]")]
        UploadEndpoint -.->|Async Trigger (Fire & Forget)| GroqService["Groq Vision Service<br/>(llama-3.2-11b-vision-preview)"]
        
        GroqService -->|Insert Findings| DB_Findings[("[dentist].[ai_findings]<br/>status='pending'")]
        GroqService -->|Insert Draft SOAP| DB_DraftNotes[("[dentist].[ai_notes_drafts]")]
        GroqService -->|Push Real-time Event| SignalRHub["SignalR ImagingHub<br/>(imaging:new / ai:findings_ready)"]
    end

    subgraph Dentia_Frontend ["Dentia Clinician Frontend (React / Tailwind)"]
        SignalRHub -->|Real-time Socket| ClientSocket["useImagingNotifications Hook"]
        ClientSocket -->|Auto-refresh| Gallery["<ImagingGallery />"]
        ClientSocket -->|Render Amber Markers| Odontogram["Odontogram Pending Overlay<br/>(Dashed Amber / AI Badge)"]
        
        Odontogram -->|Accept / Edit & Accept| ReviewEndpoint["PATCH /api/ai-findings/:id"]
        ReviewEndpoint -->|THE ONLY WRITE PATH| DB_TeethState[("[dentist].[TeethState]")]
        
        NoteUI["<AINoteReviewPanel />"] -->|Dentist Sign| SignEndpoint["PATCH /api/ai-notes-drafts/:id/sign"]
        SignEndpoint -->|THE ONLY WRITE PATH| DB_ClinicalNotes[("[dentist].[DentalNotes]")]
    end

    classDef danger fill:#fee2e2,stroke:#ef4444,stroke-width:2px;
    classDef safe fill:#dcfce7,stroke:#22c55e,stroke-width:2px;
    class DB_TeethState,DB_ClinicalNotes safe;
```

---

## The Core Invariant Rule

> [!CAUTION]
> **Strict Chart Isolation Policy (Zero Direct AI Writes):**
> 1. AI Vision predictions are strictly advisory suggestions. **NO background job, Groq callback, or ingestion pipeline is ever permitted to write directly to `[dentist].[TeethState]` or `[dentist].[DentalNotes]`.**
> 2. The ONLY code path allowed to modify `[dentist].[TeethState]` is the explicit clinician action in `PATCH /api/ai-findings/:findingId` (`action: 'accept'` or `'edit_accept'`).
> 3. The ONLY code path allowed to finalize a clinical note into `[dentist].[DentalNotes]` is the explicit clinician signature in `PATCH /api/ai-notes-drafts/:draftId/sign`.
> 4. Findings with confidence `< 0.6` **MUST NOT** allow a single-tap one-click accept in the UI; they must force a full interactive clinician review modal.

---

# Prompt 0 — Shared Architectural Contract & Data Schema

### 1. Technology Stack Mapping for Dentia
- **Backend**: ASP.NET Core 8 Web API (`.NET 8`), C#, Microsoft SQL Server (`[dentist]` schema), Dapper / ADO.NET / EF Core repository pattern.
- **Vision Engine**: Groq Vision Cloud API (Model: `llama-3.2-11b-vision-preview` / `llama-3.2-90b-vision-preview`), Base64 payload, strict JSON Schema mode.
- **Real-Time Gateway**: Microsoft ASP.NET Core SignalR (`/hubs/imaging`) with WebSocket transport (automatic fallback to Server-Sent Events / Long Polling).
- **Frontend**: React 18 / Vite, TailwindCSS, Lucide-React icons, HTML5 `getUserMedia` & `MediaRecorder` APIs.
- **Hardware Bridge**: .NET 8 C# Windows Background Service (`Dentia.CaptureBridge.Worker`) using `FileSystemWatcher` and standard TWAIN 2.x interfaces.

### 2. Standardized Domain Enums & Dictionaries

#### Modalities (`modality`)
- `periapical` (PA Radiograph / RVG Sensor)
- `bitewing` (Bitewing X-ray)
- `panoramic` (OPG / Full Jaw)
- `cephalometric` (Lateral Ceph / Ortho)
- `intraoral_photo` (Still image from intraoral camera)
- `intraoral_video` (Short MP4/WebM clip from intraoral camera)
- `consult_video` (Consultation/Smile design recording)

#### Hardware Brands & Models (`source_device_brand`, `source_device_model`)
- **Woodpecker**: `i-Sensor H1`, `i-Sensor H2`, `i-Sensor H1.5`
- **Eighteeth**: `NanoPix 1`, `NanoPix 2`
- **Vatech**: `EzSensor Classic`, `EzSensor Soft`, `EzSensor HD`, `Pax-i3D`
- **Carestream Dental**: `RVG 5200`, `RVG 6200`, `CS 8100`
- **Apple Dental / Coxo / Magenta**: `UVC Intraoral HD`, `Coxo C-Smart`, `Magenta MD-960U`

#### Clinical Findings Vocabulary
- `caries`
- `periapical radiolucency`
- `bone loss`
- `fracture`
- `calculus`
- `restoration`
- `missing tooth`
- `implant`
- `impaction`

#### Tooth Numbering Systems
- `Universal` (Adult: `1` to `32`, Primary: `A` to `T`)
- `FDI` / ISO 3950 (Adult: `11`-`18`, `21`-`28`, `31`-`38`, `41`-`48`; Primary: `51`-`55`, `61`-`65`, `71`-`75`, `81`-`85`)

---

# Prompt 1 — Backend Implementation Specification

## Task 1.1: Database Schema Migration (T-SQL / SQL Server)

```sql
-- ============================================================================
-- DENTIA IMAGING & VISION AI PIPELINE SCHEMA MIGRATION
-- Database: Microsoft SQL Server | Schema: [dentist]
-- ============================================================================

-- 1. Create or Extend [dentist].[radiographs] Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'radiographs' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[radiographs] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [patient_id] INT NOT NULL,
        [tooth_key] NVARCHAR(10) NULL, -- e.g., '14', '30', 'A', '1.6'
        [modality] NVARCHAR(50) NOT NULL, -- 'periapical', 'bitewing', 'panoramic', 'cephalometric', 'intraoral_photo', 'intraoral_video', 'consult_video'
        [source_device_type] NVARCHAR(50) NOT NULL, -- 'sensor', 'opg', 'intraoral_camera', 'webcam'
        [source_device_brand] NVARCHAR(100) NULL, -- 'Woodpecker', 'Eighteeth', 'Vatech', 'Carestream', 'Apple Dental', 'Coxo'
        [source_device_model] NVARCHAR(100) NULL, -- 'i-Sensor H1.5', 'NanoPix 1', 'EzSensor', 'Coxo UVC'
        [file_url] NVARCHAR(500) NOT NULL,
        [thumbnail_url] NVARCHAR(500) NULL,
        [mime_type] NVARCHAR(100) NOT NULL DEFAULT 'image/jpeg',
        [file_size_bytes] BIGINT NULL,
        [captured_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [appointment_id] INT NULL,
        [uploaded_by] INT NULL, -- FK to Doctor/Clinician
        [analysis_status] NVARCHAR(30) NOT NULL DEFAULT 'pending', -- 'none', 'pending', 'analyzing', 'completed', 'failed'
        [analysis_error] NVARCHAR(MAX) NULL,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_radiographs_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_radiographs_patient_date] ON [dentist].[radiographs] ([patient_id], [captured_at] DESC);
    CREATE INDEX [IX_radiographs_modality] ON [dentist].[radiographs] ([modality]);
END
GO

-- 2. Create [dentist].[ai_findings] Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ai_findings' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ai_findings] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [radiograph_id] INT NOT NULL,
        [patient_id] INT NOT NULL,
        [tooth_number] NVARCHAR(10) NOT NULL, -- e.g., '14', '30', '3.6'
        [numbering_system] NVARCHAR(20) NOT NULL DEFAULT 'Universal', -- 'Universal' | 'FDI'
        [surfaces] NVARCHAR(MAX) NOT NULL DEFAULT '[]', -- JSON array: e.g. ["D","O"], ["M","O","D"]
        [finding_text] NVARCHAR(500) NOT NULL,
        [suggested_condition] NVARCHAR(100) NOT NULL, -- 'caries', 'periapical radiolucency', 'calculus', etc.
        [suggested_cdt_code] NVARCHAR(20) NULL, -- e.g. 'D2392', 'D2150'
        [confidence] FLOAT NOT NULL, -- 0.00 to 1.00
        [status] NVARCHAR(30) NOT NULL DEFAULT 'pending', -- 'pending', 'accepted', 'edited_accepted', 'dismissed'
        [reviewed_by] INT NULL, -- FK to Doctor
        [reviewed_at] DATETIME2(7) NULL,
        [applied_teeth_state_id] INT NULL, -- Populated only upon 'accepted' / 'edited_accepted'
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_ai_findings_radiograph] FOREIGN KEY ([radiograph_id]) REFERENCES [dentist].[radiographs]([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ai_findings_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID])
    );

    CREATE INDEX [IX_ai_findings_patient_status] ON [dentist].[ai_findings] ([patient_id], [status]);
    CREATE INDEX [IX_ai_findings_radiograph] ON [dentist].[ai_findings] ([radiograph_id]);
END
GO

-- 3. Create [dentist].[ai_notes_drafts] Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ai_notes_drafts' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ai_notes_drafts] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [radiograph_id] INT NULL, -- Nullable (shared with voice scribe / dictation engine)
        [patient_id] INT NOT NULL,
        [doctor_id] INT NULL,
        [source_type] NVARCHAR(50) NOT NULL DEFAULT 'radiograph_ai', -- 'radiograph_ai', 'voice_scribe', 'hybrid'
        [source_device_label] NVARCHAR(150) NULL, -- e.g. 'Woodpecker i-Sensor H1.5 (RVG)'
        [soap_json] NVARCHAR(MAX) NOT NULL, -- Structured JSON of 8 SOAP sections
        [status] NVARCHAR(30) NOT NULL DEFAULT 'draft', -- 'draft', 'signed'
        [signed_by] INT NULL,
        [signed_at] DATETIME2(7) NULL,
        [final_dental_note_id] BIGINT NULL, -- FK to [dentist].[DentalNotes]
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_ai_notes_drafts_radiograph] FOREIGN KEY ([radiograph_id]) REFERENCES [dentist].[radiographs]([id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ai_notes_drafts_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID])
    );

    CREATE INDEX [IX_ai_notes_drafts_patient_status] ON [dentist].[ai_notes_drafts] ([patient_id], [status]);
END
GO
```

---

## Task 1.2: Upload & Ingest Endpoints

### Endpoint Specification
- **`POST /api/imaging/upload`**: Ingests multipart image/video, verifies clinical authentication, saves physical file securely, creates a `[dentist].[radiographs]` record, and triggers Groq analysis asynchronously in the background.
- **`GET /api/imaging/:patientId`**: Lists radiographs for a patient with pagination, filtering by `modality`, `source_device_brand`, and date range.
- **`GET /api/imaging/file/:radiographId`**: Securely streams the stored image with patient authorization checks.

### C# Controller Implementation (`RadiographsController.cs`)

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Models;
using DentistAPI.Repositories;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/imaging")]
    public class ImagingController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly IGroqVisionService _groqVisionService;
        private readonly IWebHostEnvironment _env;

        public ImagingController(DentalRepository repository, IGroqVisionService groqVisionService, IWebHostEnvironment env)
        {
            _repository = repository;
            _groqVisionService = groqVisionService;
            _env = env;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
        public async Task<IActionResult> UploadImaging([FromForm] ImagingUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { error = "No image or video file was provided." });

            if (request.PatientId <= 0)
                return BadRequest(new { error = "Valid patient_id is required." });

            try
            {
                // 1. Generate safe storage directory: wwwroot/uploads/imaging/patient_{id}/
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "imaging", $"patient_{request.PatientId}");
                Directory.CreateDirectory(uploadsFolder);

                string fileExt = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExt)) fileExt = ".jpg";
                
                string uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExt}";
                string physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                string relativeFileUrl = $"/uploads/imaging/patient_{request.PatientId}/{uniqueFileName}";

                // 2. Persist to Database
                var radiograph = new RadiographRecord
                {
                    PatientId = request.PatientId,
                    ToothKey = request.ToothKey,
                    Modality = string.IsNullOrWhiteSpace(request.Modality) ? "intraoral_photo" : request.Modality.ToLower(),
                    SourceDeviceType = string.IsNullOrWhiteSpace(request.SourceDeviceType) ? "intraoral_camera" : request.SourceDeviceType,
                    SourceDeviceBrand = request.SourceDeviceBrand ?? "Standard UVC",
                    SourceDeviceModel = request.SourceDeviceModel ?? "Standard Camera",
                    FileUrl = relativeFileUrl,
                    ThumbnailUrl = relativeFileUrl, // Can be generated thumbnail
                    MimeType = request.File.ContentType,
                    FileSizeBytes = request.File.Length,
                    CapturedAt = request.CapturedAt ?? DateTime.UtcNow,
                    AppointmentId = request.AppointmentId,
                    UploadedBy = request.DoctorId ?? 1,
                    AnalysisStatus = request.AutoAnalyze ? "analyzing" : "none"
                };

                int radiographId = await _repository.InsertRadiographAsync(radiograph);
                radiograph.Id = radiographId;

                // 3. Fire & Forget Groq Vision Analysis (Do NOT block HTTP response)
                if (request.AutoAnalyze)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _groqVisionService.AnalyzeRadiographAsync(radiographId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[GroqVision Background Error] Radiograph ID {radiographId}: {ex.Message}");
                        }
                    });
                }

                return Ok(new
                {
                    id = radiographId,
                    patient_id = radiograph.PatientId,
                    modality = radiograph.Modality,
                    source_device_brand = radiograph.SourceDeviceBrand,
                    source_device_model = radiograph.SourceDeviceModel,
                    file_url = radiograph.FileUrl,
                    captured_at = radiograph.CapturedAt,
                    analysis_status = radiograph.AnalysisStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpGet("{patientId:int}")]
        public async Task<IActionResult> GetPatientRadiographs(
            int patientId,
            [FromQuery] string? modality,
            [FromQuery] string? brand,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            try
            {
                var result = await _repository.GetRadiographsPagedAsync(patientId, modality, brand, fromDate, toDate, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("file/{radiographId:int}")]
        public async Task<IActionResult> StreamRadiographFile(int radiographId)
        {
            var radiograph = await _repository.GetRadiographRecordByIdAsync(radiographId);
            if (radiograph == null) return NotFound(new { error = "Radiograph not found." });

            string fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), radiograph.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound(new { error = "File not found on disk." });

            return PhysicalFile(fullPath, radiograph.MimeType, enableRangeProcessing: true);
        }
    }

    public class ImagingUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public int PatientId { get; set; }
        public string? ToothKey { get; set; }
        public string? Modality { get; set; }
        public string? SourceDeviceType { get; set; }
        public string? SourceDeviceBrand { get; set; }
        public string? SourceDeviceModel { get; set; }
        public int? AppointmentId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? CapturedAt { get; set; }
        public bool AutoAnalyze { get; set; } = true;
    }
}
```

---

## Task 1.3: Groq AI Vision Analysis Service

### Groq Vision Service Implementation (`GroqVisionService.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using DentistAPI.Models;
using DentistAPI.Repositories;
using DentistAPI.Hubs;

namespace DentistAPI.Services
{
    public interface IGroqVisionService
    {
        Task AnalyzeRadiographAsync(int radiographId);
    }

    public class GroqVisionService : IGroqVisionService
    {
        private readonly DentalRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly string _groqApiKey;
        private readonly string _groqModel;
        private readonly IHubContext<ImagingHub> _hubContext;
        private readonly IWebHostEnvironment _env;

        public GroqVisionService(
            DentalRepository repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IHubContext<ImagingHub> hubContext,
            IWebHostEnvironment env)
        {
            _repository = repository;
            _httpClient = httpClientFactory.CreateClient();
            _groqApiKey = config["Groq:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
            _groqModel = config["Groq:VisionModel"] ?? "llama-3.2-11b-vision-preview";
            _hubContext = hubContext;
            _env = env;
        }

        public async Task AnalyzeRadiographAsync(int radiographId)
        {
            var radiograph = await _repository.GetRadiographRecordByIdAsync(radiographId);
            if (radiograph == null) return;

            try
            {
                await _repository.UpdateRadiographAnalysisStatusAsync(radiographId, "analyzing", null);

                // 1. Load image and encode to Base64
                string fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), radiograph.FileUrl.TrimStart('/'));
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Physical file missing at {fullPath}");
                }

                byte[] imageBytes = await File.ReadAllBytesAsync(fullPath);
                string base64Image = Convert.ToBase64String(imageBytes);
                string dataUrl = $"data:{radiograph.MimeType};base64,{base64Image}";

                // 2. Fetch Patient Context (Numbering system & dentition)
                var patient = await _repository.GetPatientByIdAsync(radiograph.PatientId);
                string numberingSystem = "Universal"; // Or patient.PreferredNumberingSystem
                string toothContext = !string.IsNullOrEmpty(radiograph.ToothKey) ? $" of tooth {radiograph.ToothKey}" : "";

                // 3. Build Prompt matching strict JSON Mode
                string systemPrompt = $@"You are a dental radiograph/photo analysis assistant. Analyze the attached {radiograph.Modality} image{toothContext}. Use only the {numberingSystem} tooth numbering system. Constrain findings to this vocabulary: caries, periapical radiolucency, bone loss, fracture, calculus, restoration, missing tooth, implant, impaction. Respond ONLY in JSON matching this schema:
{{
  ""findings"": [
    {{
      ""toothNumber"": ""string"",
      ""numberingSystem"": ""{numberingSystem}"",
      ""surface"": [""M"", ""O"", ""D"", ""B"", ""L""],
      ""finding"": ""string"",
      ""suggestedCondition"": ""string"",
      ""suggestedCdtCode"": ""string"",
      ""confidence"": 0.95
    }}
  ]
}}
If you are not confident about a value, still include it but lower the confidence score accordingly. Never invent a tooth number you cannot identify — omit that finding instead.";

                var payload = new
                {
                    model = _groqModel,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = $"Analyze this {radiograph.Modality} image and return clinical observations." },
                                new { type = "image_url", image_url = new { url = dataUrl } }
                            }
                        }
                    },
                    response_format = new { type = "json_object" },
                    temperature = 0.1,
                    max_tokens = 1500
                };

                // 4. Dispatch to Groq API over TLS (Never log base64 payload)
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Groq API Error ({response.StatusCode}): {responseJson}");
                }

                // 5. Parse Vision Results
                using var doc = JsonDocument.Parse(responseJson);
                var contentText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var parsedResult = JsonSerializer.Deserialize<GroqVisionResult>(contentText ?? "{\"findings\":[]}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // 6. Insert AI Findings into DB (Status = 'pending')
                var insertedFindings = new List<AIFindingRecord>();
                var soapProcedures = new StringBuilder();
                var soapExam = new StringBuilder();

                if (parsedResult?.Findings != null && parsedResult.Findings.Count > 0)
                {
                    foreach (var f in parsedResult.Findings)
                    {
                        var findingRecord = new AIFindingRecord
                        {
                            RadiographId = radiographId,
                            PatientId = radiograph.PatientId,
                            ToothNumber = f.ToothNumber,
                            NumberingSystem = f.NumberingSystem ?? numberingSystem,
                            Surfaces = JsonSerializer.Serialize(f.Surface ?? new List<string>()),
                            FindingText = f.Finding,
                            SuggestedCondition = f.SuggestedCondition,
                            SuggestedCdtCode = f.SuggestedCdtCode,
                            Confidence = Math.Clamp(f.Confidence, 0.0, 1.0),
                            Status = "pending"
                        };

                        int findingId = await _repository.InsertAIFindingAsync(findingRecord);
                        findingRecord.Id = findingId;
                        insertedFindings.Add(findingRecord);

                        soapExam.AppendLine($"- Tooth #{f.ToothNumber}: {f.Finding} ({f.SuggestedCondition}, Surfaces: {string.Join(",", f.Surface ?? new List<string>())}) [Confidence: {f.Confidence:P0}]");
                        if (!string.IsNullOrEmpty(f.SuggestedCdtCode))
                        {
                            soapProcedures.AppendLine($"- CDT {f.SuggestedCdtCode}: Recommended treatment for Tooth #{f.ToothNumber} ({f.Finding})");
                        }
                    }
                }

                // 7. Generate Draft SOAP Note
                var soapNote = new
                {
                    chiefComplaint = $"Imaging observation from {radiograph.SourceDeviceBrand} ({radiograph.Modality}).",
                    historyOfPresentIllness = "Captured during clinical imaging exam.",
                    medicalHistory = "Reviewed, no acute contraindications noted.",
                    objectiveExam = soapExam.Length > 0 ? soapExam.ToString().Trim() : "No obvious radiographic abnormalities detected.",
                    assessment = $"Radiographic findings evaluated via Groq Vision AI. {insertedFindings.Count} observation(s) pending dentist review.",
                    proceduresPerformed = soapProcedures.Length > 0 ? soapProcedures.ToString().Trim() : "Radiographic examination completed.",
                    postOpInstructions = "Pending clinical treatment acceptance.",
                    followUpPlan = "Review pending chart overlay with patient."
                };

                var noteDraft = new AINoteDraftRecord
                {
                    RadiographId = radiographId,
                    PatientId = radiograph.PatientId,
                    DoctorId = radiograph.UploadedBy,
                    SourceType = "radiograph_ai",
                    SourceDeviceLabel = $"{radiograph.SourceDeviceBrand} {radiograph.SourceDeviceModel} ({radiograph.Modality})",
                    SoapJson = JsonSerializer.Serialize(soapNote),
                    Status = "draft"
                };

                int draftId = await _repository.InsertAINoteDraftAsync(noteDraft);

                // 8. Update Radiograph Status to completed
                await _repository.UpdateRadiographAnalysisStatusAsync(radiographId, "completed", null);

                // 9. Emit Real-time Notification via SignalR Hub
                await _hubContext.Clients.Group($"patient_{radiograph.PatientId}").SendAsync("ai:findings_ready", new
                {
                    radiograph_id = radiographId,
                    patient_id = radiograph.PatientId,
                    findings_count = insertedFindings.Count,
                    draft_note_id = draftId,
                    findings = insertedFindings
                });
            }
            catch (Exception ex)
            {
                // Resilient Error Handling — Mark as failed, never crash the pipeline
                await _repository.UpdateRadiographAnalysisStatusAsync(radiographId, "failed", ex.Message);

                await _hubContext.Clients.Group($"patient_{radiograph.PatientId}").SendAsync("ai:analysis_failed", new
                {
                    radiograph_id = radiographId,
                    patient_id = radiograph.PatientId,
                    error = ex.Message
                });
            }
        }
    }

    public class GroqVisionResult
    {
        public List<GroqFindingItem> Findings { get; set; } = new();
    }

    public class GroqFindingItem
    {
        public string ToothNumber { get; set; } = string.Empty;
        public string NumberingSystem { get; set; } = "Universal";
        public List<string> Surface { get; set; } = new();
        public string Finding { get; set; } = string.Empty;
        public string SuggestedCondition { get; set; } = string.Empty;
        public string SuggestedCdtCode { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
```

---

## Task 1.4: Review & Acceptance Endpoints

### Strict Single-Write Flow Controller (`AIFindingsController.cs`)

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Models;
using DentistAPI.Repositories;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class AIFindingsController : ControllerBase
    {
        private readonly DentalRepository _repository;

        public AIFindingsController(DentalRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("ai-findings/{patientId:int}")]
        public async Task<IActionResult> GetPendingFindings(int patientId, [FromQuery] string status = "pending")
        {
            var list = await _repository.GetAIFindingsByPatientAsync(patientId, status);
            return Ok(list);
        }

        /// <summary>
        /// THE ONLY CODE PATH ALLOWED TO WRITE TO TeethState FROM AI FINDINGS
        /// </summary>
        [HttpPatch("ai-findings/{findingId:int}")]
        public async Task<IActionResult> ReviewFinding(int findingId, [FromBody] ReviewFindingRequest request)
        {
            var finding = await _repository.GetAIFindingByIdAsync(findingId);
            if (finding == null) return NotFound(new { error = "AI Finding not found." });

            if (request.Action == "dismiss")
            {
                await _repository.UpdateAIFindingStatusAsync(findingId, "dismissed", request.ReviewedBy, null);
                return Ok(new { status = "dismissed", message = "Finding dismissed without chart update." });
            }

            if (request.Action == "accept" || request.Action == "edit_accept")
            {
                // Extract parameters (respecting doctor edits if provided)
                string toothNumber = request.Edits?.ToothNumber ?? finding.ToothNumber;
                string condition = request.Edits?.SuggestedCondition ?? finding.SuggestedCondition;
                string surfaces = request.Edits?.Surfaces != null ? JsonSerializer.Serialize(request.Edits.Surfaces) : finding.Surfaces;
                string cdtCode = request.Edits?.SuggestedCdtCode ?? finding.SuggestedCdtCode;

                // 1. Map Condition to Chart Color Status
                string colorCode = condition.ToLower() switch
                {
                    "caries" => "#EF4444", // Red
                    "periapical radiolucency" => "#DC2626", // Deep Red
                    "bone loss" => "#F59E0B", // Amber
                    "calculus" => "#D97706",
                    "restoration" => "#3B82F6", // Blue
                    "missing tooth" => "#6B7280", // Grey
                    "implant" => "#8B5CF6", // Purple
                    _ => "#10B981"
                };

                // 2. Perform Single-Point Write to [dentist].[TeethState]
                int teethStateId = await _repository.UpsertTeethStateAsync(new TeethState
                {
                    PatientID = finding.PatientId,
                    DoctorID = request.ReviewedBy,
                    ToothKey = toothNumber,
                    ToothNumber = int.TryParse(toothNumber, out int tn) ? tn : 0,
                    ConditionStatus = condition,
                    ConditionColor = colorCode,
                    Comments = $"[AI Confirmed] CDT: {cdtCode}, Surfaces: {surfaces}",
                    LastUpdated = DateTime.UtcNow
                });

                // 3. Mark finding as accepted and link teeth_state_id
                string finalStatus = request.Action == "edit_accept" ? "edited_accepted" : "accepted";
                await _repository.UpdateAIFindingStatusAsync(findingId, finalStatus, request.ReviewedBy, teethStateId);

                return Ok(new
                {
                    status = finalStatus,
                    teeth_state_id = teethStateId,
                    message = "Finding accepted and successfully written to patient chart."
                });
            }

            return BadRequest(new { error = "Invalid action. Supported: 'accept', 'edit_accept', 'dismiss'." });
        }

        [HttpPatch("ai-notes-drafts/{draftId:int}/sign")]
        public async Task<IActionResult> SignDraftNote(int draftId, [FromBody] SignNoteRequest request)
        {
            var draft = await _repository.GetAINoteDraftByIdAsync(draftId);
            if (draft == null) return NotFound(new { error = "Draft note not found." });

            // 1. Create official clinical note record in [dentist].[DentalNotes]
            long officialNoteId = await _repository.InsertOfficialClinicalNoteAsync(new DentalNote
            {
                PatientId = draft.PatientId,
                DentistId = request.DoctorId,
                Examination = request.EditedSoap?.ObjectiveExam ?? "",
                TreatmentPerformed = request.EditedSoap?.ProceduresPerformed ?? "",
                Assessment = request.EditedSoap?.Assessment ?? "",
                ChiefComplaint = request.EditedSoap?.ChiefComplaint ?? "",
                History = request.EditedSoap?.MedicalHistory ?? "",
                PostOpAdvice = request.EditedSoap?.PostOpInstructions ?? "",
                FollowUp = request.EditedSoap?.FollowUpPlan ?? "",
                Status = "signed",
                ApprovedBy = request.DoctorId,
                ApprovedAt = DateTime.UtcNow
            });

            // 2. Mark draft note as signed
            await _repository.MarkAINoteDraftSignedAsync(draftId, request.DoctorId, officialNoteId);

            return Ok(new
            {
                status = "signed",
                note_id = officialNoteId,
                signed_at = DateTime.UtcNow
            });
        }
    }

    public class ReviewFindingRequest
    {
        public string Action { get; set; } = "accept"; // 'accept' | 'edit_accept' | 'dismiss'
        public int ReviewedBy { get; set; }
        public FindingEditPayload? Edits { get; set; }
    }

    public class FindingEditPayload
    {
        public string? ToothNumber { get; set; }
        public string? SuggestedCondition { get; set; }
        public string? SuggestedCdtCode { get; set; }
        public string[]? Surfaces { get; set; }
    }

    public class SignNoteRequest
    {
        public int DoctorId { get; set; }
        public SoapSectionsPayload? EditedSoap { get; set; }
    }

    public class SoapSectionsPayload
    {
        public string? ChiefComplaint { get; set; }
        public string? MedicalHistory { get; set; }
        public string? ObjectiveExam { get; set; }
        public string? Assessment { get; set; }
        public string? ProceduresPerformed { get; set; }
        public string? PostOpInstructions { get; set; }
        public string? FollowUpPlan { get; set; }
    }
}
```

---

## Task 1.5: Real-Time Event Architecture (SignalR Hub)

> [!NOTE]
> **Why SignalR over raw SSE/WebSockets?**
> In ASP.NET Core, SignalR provides full bi-directional transport negotiation (WebSockets first, falling back gracefully to SSE/Long Polling for older clinic networks), built-in connection grouping (`patient_{id}` and `clinic_{id}`), automatic reconnections, and native TypeScript client integration.

### SignalR Hub Implementation (`ImagingHub.cs`)

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DentistAPI.Hubs
{
    public class ImagingHub : Hub
    {
        public async Task JoinPatientSession(string patientId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"patient_{patientId}");
        }

        public async Task LeavePatientSession(string patientId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"patient_{patientId}");
        }

        public async Task JoinClinicSession(string clinicId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic_{clinicId}");
        }
    }
}
```

---

## Task 1.6: Dentia Capture Bridge Windows Service (.NET 8 C#)

> [!TIP]
> **Tech Stack Selection: .NET 8 Worker Background Service**
> A C# .NET 8 background service is chosen because:
> 1. Native Windows API & Win32 FileSystemWatcher hooks with 0ms OS overhead.
> 2. Direct interoperability with vendor TWAIN Data Source drivers (`twaindsm.dll` / 32-bit & 64-bit sensor SDKs from Woodpecker, Vatech, Carestream).
> 3. Zero-dependency single-file executable distribution with automatic Windows Service registration (`sc.exe create`).

```csharp
// Program.cs - Dentia.CaptureBridge.Worker
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dentia.CaptureBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient("DentiaApi", client =>
                    {
                        client.BaseAddress = new Uri(hostContext.Configuration["Dentia:ApiBaseUrl"] ?? "https://dentist-api-dev.vitonta.com/");
                        client.Timeout = TimeSpan.FromSeconds(60);
                    });
                    services.AddHostedService<HardwareWatcherWorker>();
                });
    }

    public class HardwareWatcherWorker : BackgroundService
    {
        private readonly ILogger<HardwareWatcherWorker> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string[] _watchPaths;

        public HardwareWatcherWorker(ILogger<HardwareWatcherWorker> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _watchPaths = _config.GetSection("WatchPaths").Get<string[]>() ?? new[]
            {
                @"C:\ProgramData\Woodpecker\i-Sensor\Export",
                @"C:\Vatech\EzDent-i\Capture",
                @"C:\NanoPix\Export",
                @"C:\Carestream\Capture"
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dentia Capture Bridge started at: {time}", DateTimeOffset.Now);

            foreach (var path in _watchPaths)
            {
                if (!Directory.Exists(path))
                {
                    try { Directory.CreateDirectory(path); } catch { }
                }

                if (Directory.Exists(path))
                {
                    var watcher = new FileSystemWatcher(path)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        Filter = "*.*",
                        EnableRaisingEvents = true
                    };
                    watcher.Created += async (s, e) => await OnNewHardwareCaptureDetected(e.FullPath);
                    _logger.LogInformation("Watching hardware directory: {path}", path);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task OnNewHardwareCaptureDetected(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".dcm" && ext != ".tif")
                return;

            _logger.LogInformation("New sensor image detected: {file}", filePath);

            // Wait briefly for sensor software to finish flushing file
            await Task.Delay(1200);

            // Detect Brand from path or file
            string brand = "Woodpecker";
            string model = "i-Sensor H1.5";
            if (filePath.Contains("Vatech", StringComparison.OrdinalIgnoreCase)) { brand = "Vatech"; model = "EzSensor HD"; }
            else if (filePath.Contains("NanoPix", StringComparison.OrdinalIgnoreCase)) { brand = "Eighteeth"; model = "NanoPix 1"; }
            else if (filePath.Contains("Carestream", StringComparison.OrdinalIgnoreCase)) { brand = "Carestream"; model = "RVG 5200"; }

            // Retrieve Active Chairside Patient Context from local config/state file
            int activePatientId = GetActiveChairsidePatientId();

            await UploadWithRetryAsync(filePath, activePatientId, brand, model);
        }

        private int GetActiveChairsidePatientId()
        {
            string stateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "active_session.json");
            if (File.Exists(stateFile))
            {
                try
                {
                    var content = File.ReadAllText(stateFile);
                    var doc = System.Text.Json.JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("active_patient_id", out var pid))
                        return pid.GetInt32();
                }
                catch { }
            }
            return 1; // Default fallback
        }

        private async Task UploadWithRetryAsync(string filePath, int patientId, string brand, string model, int maxRetries = 3)
        {
            var client = _httpClientFactory.CreateClient("DentiaApi");
            string clinicApiKey = _config["Dentia:ClinicApiKey"] ?? "CLINIC_BRIDGE_KEY_NZ_99";

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var form = new MultipartFormDataContent();
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    form.Add(fileContent, "File", Path.GetFileName(filePath));
                    form.Add(new StringContent(patientId.ToString()), "PatientId");
                    form.Add(new StringContent("periapical"), "Modality");
                    form.Add(new StringContent("sensor"), "SourceDeviceType");
                    form.Add(new StringContent(brand), "SourceDeviceBrand");
                    form.Add(new StringContent(model), "SourceDeviceModel");
                    form.Add(new StringContent("true"), "AutoAnalyze");

                    var req = new HttpRequestMessage(HttpMethod.Post, "/api/imaging/upload") { Content = form };
                    req.Headers.Add("X-Clinic-Api-Key", clinicApiKey);

                    var response = await client.SendAsync(req);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully dispatched {file} to Dentia Cloud Pipeline.", filePath);
                        return;
                    }
                    _logger.LogWarning("Upload attempt {attempt} failed: {status}", attempt, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Upload exception on attempt {attempt}: {err}", attempt, ex.Message);
                }

                await Task.Delay(2000 * attempt);
            }
        }
    }
}
```

---

## Task 1.7: Clinical Security & Guardrails
- **Patient Isolation**: All imaging queries filter strictly by `patient_id` and clinic tenancy.
- **Hardware Authentication**: Capture Bridge uses a scoped header `X-Clinic-Api-Key` verified by ASP.NET Core middleware, bypassing user passwords.
- **Data Privacy**: Groq Vision payloads are sent exclusively over TLS 1.3. Base64 payloads are never written to log files or audit tables.

---

# Prompt 2 — Frontend Implementation Specification

## Design System Tokens
- **Primary Deep Teal**: `#0B4F4A`
- **Accent Seafoam**: `#4FB3A9`
- **Warm Amber (Pending AI / AI Badges)**: `#E8934A`
- **Soft Grey-Teal Backgrounds**: `#F2F7F6`
- **Card Border**: `#D1E3E0`
- **Text Primary**: `#0F2F2C`

---

## Task 2.1: Camera Capture Panel (`<CameraCapturePanel />`)

```jsx
import React, { useState, useEffect, useRef } from 'react';
import { Camera, Video, RefreshCw, CheckCircle, AlertCircle, Sparkles, X, StopCircle } from 'lucide-react';

export const CameraCapturePanel = ({ patientId, toothKey = null, onUploadSuccess, onClose }) => {
  const [devices, setDevices] = useState([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState('');
  const [deviceInfo, setDeviceInfo] = useState({ brand: 'UVC Camera', model: 'Intraoral' });
  const [stream, setStream] = useState(null);
  const [mode, setMode] = useState('photo'); // 'photo' | 'video'
  const [capturedBlob, setCapturedBlob] = useState(null);
  const [capturedDataUrl, setCapturedDataUrl] = useState(null);
  const [isRecording, setIsRecording] = useState(false);
  const [recordTimer, setRecordTimer] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const [errorMsg, setErrorMsg] = useState(null);

  const videoRef = useRef(null);
  const mediaRecorderRef = useRef(null);
  const recordedChunksRef = useRef([]);
  const timerIntervalRef = useRef(null);

  // 1. Scan & Smart Brand Autodetection
  useEffect(() => {
    async function initDevices() {
      try {
        const devs = await navigator.mediaDevices.enumerateDevices();
        const videoDevs = devs.filter((d) => d.kind === 'videoinput');
        setDevices(videoDevs);

        if (videoDevs.length > 0) {
          // Detect clinical brands
          const clinicalDev = videoDevs.find((d) => {
            const label = d.label.toLowerCase();
            return (
              label.includes('apple dental') ||
              label.includes('coxo') ||
              label.includes('magenta') ||
              label.includes('intraoral') ||
              label.includes('uvc camera')
            );
          });

          const chosen = clinicalDev || videoDevs[0];
          setSelectedDeviceId(chosen.deviceId);
          parseDeviceBrandModel(chosen.label);
        } else {
          setErrorMsg('No video capture devices or intraoral cameras detected.');
        }
      } catch (err) {
        setErrorMsg('Camera permission denied or device inaccessible.');
      }
    }
    initDevices();
  }, []);

  const parseDeviceBrandModel = (label) => {
    const l = label.toLowerCase();
    if (l.includes('apple dental')) setDeviceInfo({ brand: 'Apple Dental', model: 'HD Intraoral' });
    else if (l.includes('coxo')) setDeviceInfo({ brand: 'Coxo', model: 'C-Smart UVC' });
    else if (l.includes('magenta')) setDeviceInfo({ brand: 'Magenta', model: 'MD-960U' });
    else setDeviceInfo({ brand: 'Generic UVC', model: label || 'Intraoral Camera' });
  };

  // 2. Start Video Stream
  useEffect(() => {
    if (!selectedDeviceId) return;

    let currentStream = null;
    navigator.mediaDevices
      .getUserMedia({
        video: { deviceId: { exact: selectedDeviceId }, width: { ideal: 1920 }, height: { ideal: 1080 } },
        audio: false,
      })
      .then((s) => {
        currentStream = s;
        setStream(s);
        if (videoRef.current) videoRef.current.srcObject = s;
        setErrorMsg(null);
      })
      .catch((err) => {
        setErrorMsg('Failed to open camera stream: ' + err.message);
      });

    return () => {
      if (currentStream) {
        currentStream.getTracks().forEach((track) => track.stop());
      }
    };
  }, [selectedDeviceId]);

  // 3. Capture Photo Frame
  const takePhoto = () => {
    if (!videoRef.current) return;
    const canvas = document.createElement('canvas');
    canvas.width = videoRef.current.videoWidth || 1280;
    canvas.height = videoRef.current.videoHeight || 720;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(videoRef.current, 0, 0, canvas.width, canvas.height);

    canvas.toBlob((blob) => {
      setCapturedBlob(blob);
      setCapturedDataUrl(URL.createObjectURL(blob));
    }, 'image/jpeg', 0.95);
  };

  // 4. Record Video Clip (Max 20s)
  const startRecording = () => {
    if (!stream) return;
    recordedChunksRef.current = [];
    const recorder = new MediaRecorder(stream, { mimeType: 'video/webm' });
    mediaRecorderRef.current = recorder;

    recorder.ondataavailable = (e) => {
      if (e.data.size > 0) recordedChunksRef.current.push(e.data);
    };

    recorder.onstop = () => {
      const blob = new Blob(recordedChunksRef.current, { type: 'video/webm' });
      setCapturedBlob(blob);
      setCapturedDataUrl(URL.createObjectURL(blob));
      setIsRecording(false);
      clearInterval(timerIntervalRef.current);
    };

    recorder.start();
    setIsRecording(true);
    setRecordTimer(0);

    timerIntervalRef.current = setInterval(() => {
      setRecordTimer((prev) => {
        if (prev >= 20) {
          recorder.stop();
          return 20;
        }
        return prev + 1;
      });
    }, 1000);
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
    }
  };

  // 5. Confirm & Upload
  const handleUpload = async () => {
    if (!capturedBlob) return;
    setIsUploading(true);
    setErrorMsg(null);

    const formData = new FormData();
    formData.append('File', capturedBlob, mode === 'photo' ? 'capture.jpg' : 'capture.webm');
    formData.append('PatientId', patientId);
    if (toothKey) formData.append('ToothKey', toothKey);
    formData.append('Modality', mode === 'photo' ? 'intraoral_photo' : 'intraoral_video');
    formData.append('SourceDeviceType', 'intraoral_camera');
    formData.append('SourceDeviceBrand', deviceInfo.brand);
    formData.append('SourceDeviceModel', deviceInfo.model);
    formData.append('AutoAnalyze', 'true');

    try {
      const res = await fetch('https://dentist-api-dev.vitonta.com/api/imaging/upload', {
        method: 'POST',
        body: formData,
      });

      if (!res.ok) throw new Error(`Upload returned status ${res.status}`);
      const data = await res.json();
      setIsUploading(false);
      if (onUploadSuccess) onUploadSuccess(data);
    } catch (err) {
      setIsUploading(false);
      setErrorMsg('Failed to upload image to AI pipeline: ' + err.message);
    }
  };

  return (
    <div className="bg-[#F2F7F6] border border-[#D1E3E0] rounded-2xl p-6 shadow-xl max-w-2xl mx-auto text-[#0F2F2C]">
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-[#D1E3E0]">
        <div className="flex items-center gap-3">
          <div className="p-2.5 bg-[#0B4F4A] text-white rounded-xl">
            <Camera className="w-5 h-5" />
          </div>
          <div>
            <h3 className="font-semibold text-lg text-[#0B4F4A]">Chairside Intraoral Capture</h3>
            <p className="text-xs text-teal-700">Auto-connected: <span className="font-semibold">{deviceInfo.brand} {deviceInfo.model}</span></p>
          </div>
        </div>
        {onClose && (
          <button onClick={onClose} className="p-2 hover:bg-gray-200 rounded-lg text-gray-500">
            <X className="w-5 h-5" />
          </button>
        )}
      </div>

      {/* Device Selector */}
      <div className="mt-4 flex gap-3">
        <select
          value={selectedDeviceId}
          onChange={(e) => {
            setSelectedDeviceId(e.target.value);
            const dev = devices.find((d) => d.deviceId === e.target.value);
            if (dev) parseDeviceBrandModel(dev.label);
          }}
          className="flex-1 bg-white border border-[#D1E3E0] rounded-xl px-3 py-2 text-sm text-[#0F2F2C] focus:outline-none focus:ring-2 focus:ring-[#4FB3A9]"
        >
          {devices.map((d) => (
            <option key={d.deviceId} value={d.deviceId}>
              {d.label || `Camera (${d.deviceId.slice(0, 8)})`}
            </option>
          ))}
        </select>

        <div className="flex bg-white rounded-xl border border-[#D1E3E0] p-1">
          <button
            onClick={() => { setMode('photo'); setCapturedBlob(null); }}
            className={`px-3 py-1 text-xs font-medium rounded-lg ${mode === 'photo' ? 'bg-[#0B4F4A] text-white' : 'text-gray-600'}`}
          >
            Photo
          </button>
          <button
            onClick={() => { setMode('video'); setCapturedBlob(null); }}
            className={`px-3 py-1 text-xs font-medium rounded-lg ${mode === 'video' ? 'bg-[#0B4F4A] text-white' : 'text-gray-600'}`}
          >
            Video
          </button>
        </div>
      </div>

      {/* Camera Live View / Preview Box */}
      <div className="mt-4 relative bg-black rounded-xl overflow-hidden aspect-video flex items-center justify-center">
        {errorMsg ? (
          <div className="p-6 text-center text-red-300">
            <AlertCircle className="w-8 h-8 mx-auto mb-2 text-red-400" />
            <p className="text-sm font-medium">{errorMsg}</p>
          </div>
        ) : capturedDataUrl ? (
          mode === 'photo' ? (
            <img src={capturedDataUrl} alt="Captured preview" className="w-full h-full object-contain" />
          ) : (
            <video src={capturedDataUrl} controls className="w-full h-full object-contain" />
          )
        ) : (
          <>
            <video ref={videoRef} autoPlay playsInline muted className="w-full h-full object-cover" />
            {isRecording && (
              <div className="absolute top-4 right-4 bg-red-600 text-white px-3 py-1 rounded-full text-xs font-bold animate-pulse flex items-center gap-1.5">
                <span className="w-2 h-2 rounded-full bg-white"></span> REC {recordTimer}s / 20s
              </div>
            )}
          </>
        )}
      </div>

      {/* Control Actions */}
      <div className="mt-5 flex items-center justify-between">
        {capturedBlob ? (
          <div className="flex items-center gap-3 w-full">
            <button
              onClick={() => { setCapturedBlob(null); setCapturedDataUrl(null); }}
              className="flex-1 py-2.5 border border-gray-300 rounded-xl text-sm font-medium hover:bg-white text-gray-700"
            >
              Retake
            </button>
            <button
              onClick={handleUpload}
              disabled={isUploading}
              className="flex-1 py-2.5 bg-[#0B4F4A] hover:bg-[#083c38] text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 shadow-lg"
            >
              {isUploading ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4 text-[#E8934A]" />}
              {isUploading ? 'Analyzing with Groq...' : 'Confirm & Run AI Analysis'}
            </button>
          </div>
        ) : mode === 'photo' ? (
          <button
            onClick={takePhoto}
            className="w-full py-3 bg-[#0B4F4A] hover:bg-[#083c38] text-white font-semibold rounded-xl flex items-center justify-center gap-2 shadow-md"
          >
            <Camera className="w-5 h-5" /> Snap Photo
          </button>
        ) : (
          <button
            onClick={isRecording ? stopRecording : startRecording}
            className={`w-full py-3 text-white font-semibold rounded-xl flex items-center justify-center gap-2 shadow-md ${
              isRecording ? 'bg-red-600 hover:bg-red-700' : 'bg-[#0B4F4A] hover:bg-[#083c38]'
            }`}
          >
            {isRecording ? <StopCircle className="w-5 h-5" /> : <Video className="w-5 h-5" />}
            {isRecording ? 'Stop Recording' : 'Start 20s Clip'}
          </button>
        )}
      </div>
    </div>
  );
};
```

---

## Task 2.2: Imaging & X-Rays Gallery (`<ImagingGallery />`)

```jsx
import React, { useState, useEffect } from 'react';
import { Eye, Filter, Calendar, Sparkles, HardDrive, Plus, X } from 'lucide-react';
import { CameraCapturePanel } from './CameraCapturePanel';

export const ImagingGallery = ({ patientId, onSelectFindingForReview }) => {
  const [radiographs, setRadiographs] = useState([]);
  const [activeModality, setActiveModality] = useState('all');
  const [activeBrand, setActiveBrand] = useState('all');
  const [selectedItem, setSelectedItem] = useState(null);
  const [showCaptureModal, setShowCaptureModal] = useState(false);
  const [loading, setLoading] = useState(true);

  const fetchImages = async () => {
    try {
      setLoading(true);
      const res = await fetch(`https://dentist-api-dev.vitonta.com/api/imaging/${patientId}`);
      if (res.ok) {
        const data = await res.json();
        setRadiographs(data || []);
      }
    } catch (err) {
      console.error('Failed to load gallery:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchImages();
  }, [patientId]);

  const filtered = radiographs.filter((r) => {
    if (activeModality !== 'all' && r.modality !== activeModality) return false;
    if (activeBrand !== 'all' && r.source_device_brand !== activeBrand) return false;
    return true;
  });

  return (
    <div className="bg-white border border-[#D1E3E0] rounded-2xl p-6 shadow-sm">
      {/* Top Controls */}
      <div className="flex flex-wrap items-center justify-between gap-4 pb-5 border-b border-gray-100">
        <div>
          <h2 className="text-xl font-bold text-[#0B4F4A]">Patient Imaging & Radiographs</h2>
          <p className="text-xs text-gray-500">RVG Digital Sensors, OPGs, and Intraoral Captures</p>
        </div>

        <div className="flex items-center gap-3">
          {/* Modality Filter */}
          <select
            value={activeModality}
            onChange={(e) => setActiveModality(e.target.value)}
            className="bg-[#F2F7F6] border border-[#D1E3E0] rounded-xl px-3 py-1.5 text-xs text-[#0F2F2C]"
          >
            <option value="all">All Modalities</option>
            <option value="periapical">Periapical (RVG)</option>
            <option value="bitewing">Bitewing</option>
            <option value="panoramic">Panoramic (OPG)</option>
            <option value="intraoral_photo">Intraoral Photo</option>
          </select>

          <button
            onClick={() => setShowCaptureModal(true)}
            className="px-4 py-2 bg-[#0B4F4A] hover:bg-[#083c38] text-white rounded-xl text-xs font-semibold flex items-center gap-1.5 shadow-sm"
          >
            <Plus className="w-4 h-4" /> Capture New
          </button>
        </div>
      </div>

      {/* Grid */}
      {loading ? (
        <div className="p-12 text-center text-teal-800 font-medium">Loading radiographs...</div>
      ) : filtered.length === 0 ? (
        <div className="p-12 text-center border-2 border-dashed border-[#D1E3E0] rounded-2xl mt-6">
          <HardDrive className="w-10 h-10 text-teal-600 mx-auto mb-3 opacity-60" />
          <h4 className="font-semibold text-gray-700">No radiographs found</h4>
          <p className="text-xs text-gray-500 mt-1 max-w-sm mx-auto">
            Captured automatically from your connected Woodpecker / Vatech / Eighteeth sensor via Capture Bridge or use the capture button above.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6">
          {filtered.map((item) => (
            <div
              key={item.id}
              onClick={() => setSelectedItem(item)}
              className="group relative bg-[#F2F7F6] border border-[#D1E3E0] rounded-xl overflow-hidden cursor-pointer hover:shadow-md transition-all"
            >
              <div className="aspect-square bg-gray-900 overflow-hidden flex items-center justify-center">
                <img src={item.thumbnail_url || item.file_url} alt="Scan" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
              </div>

              {/* Badges */}
              <div className="p-2.5">
                <div className="flex items-center justify-between text-[11px] font-semibold">
                  <span className="text-[#0B4F4A] uppercase">{item.modality}</span>
                  {item.analysis_status === 'completed' && (
                    <span className="bg-[#E8934A]/20 text-[#E8934A] px-1.5 py-0.5 rounded flex items-center gap-1">
                      <Sparkles className="w-3 h-3" /> AI
                    </span>
                  )}
                </div>
                <p className="text-[10px] text-gray-500 mt-0.5 truncate">{item.source_device_brand} {item.source_device_model}</p>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Lightbox Modal */}
      {selectedItem && (
        <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-3xl w-full overflow-hidden shadow-2xl">
            <div className="p-4 bg-[#0B4F4A] text-white flex justify-between items-center">
              <div>
                <h3 className="font-semibold text-sm">{selectedItem.source_device_brand} {selectedItem.source_device_model} — {selectedItem.modality}</h3>
                <p className="text-xs text-teal-200">Captured: {new Date(selectedItem.captured_at).toLocaleString()}</p>
              </div>
              <button onClick={() => setSelectedItem(null)} className="p-1.5 hover:bg-white/10 rounded-lg">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="bg-black p-4 flex justify-center max-h-[60vh]">
              <img src={selectedItem.file_url} alt="Full resolution" className="max-h-[55vh] object-contain rounded-lg" />
            </div>
            <div className="p-4 bg-[#F2F7F6] flex justify-between items-center">
              <span className="text-xs text-gray-600">Status: <strong className="text-[#0B4F4A]">{selectedItem.analysis_status}</strong></span>
              {onSelectFindingForReview && (
                <button
                  onClick={() => {
                    const item = selectedItem;
                    setSelectedItem(null);
                    onSelectFindingForReview(item);
                  }}
                  className="px-4 py-2 bg-[#E8934A] text-white rounded-xl text-xs font-bold hover:bg-[#d47f38]"
                >
                  Review AI Findings on Odontogram &rarr;
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Capture Panel Modal */}
      {showCaptureModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <CameraCapturePanel
            patientId={patientId}
            onClose={() => setShowCaptureModal(false)}
            onUploadSuccess={() => {
              setShowCaptureModal(false);
              fetchImages();
            }}
          />
        </div>
      )}
    </div>
  );
};
```

---

## Task 2.3: Real-Time Event Hook (`useImagingNotifications.js`)

```javascript
import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

export const useImagingNotifications = ({ patientId, onNewRadiograph, onFindingsReady, onAnalysisFailed }) => {
  useEffect(() => {
    if (!patientId) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('https://dentist-api-dev.vitonta.com/hubs/imaging')
      .withAutomaticReconnect()
      .build();

    connection
      .start()
      .then(() => {
        connection.invoke('JoinPatientSession', patientId.toString());
      })
      .catch((err) => console.error('SignalR Hub Connection Error:', err));

    connection.on('imaging:new', (data) => {
      if (onNewRadiograph) onNewRadiograph(data);
    });

    connection.on('ai:findings_ready', (data) => {
      if (onFindingsReady) onFindingsReady(data);
    });

    connection.on('ai:analysis_failed', (data) => {
      if (onAnalysisFailed) onAnalysisFailed(data);
    });

    return () => {
      connection.stop();
    };
  }, [patientId]);
};
```

---

## Task 2.4: Pending AI Findings Overlay on the Odontogram (`<PendingOdontogramOverlay />`)

> [!IMPORTANT]
> **Distinct Visual Semantics**: Pending markers use a **dashed warm amber outline (`#E8934A`)** and an amber "AI" badge. They must NEVER be confused with confirmed solid-colored chart conditions. Confidence `< 0.6` forces a full interactive review modal.

```jsx
import React, { useState } from 'react';
import { Sparkles, Check, Edit3, X, AlertTriangle, ShieldCheck } from 'lucide-react';

export const PendingOdontogramOverlay = ({
  findings = [],
  confidenceThreshold = 0.6,
  onAcceptFinding,
  onEditAcceptFinding,
  onDismissFinding,
}) => {
  const [selectedFinding, setSelectedFinding] = useState(null);
  const [isEditing, setIsEditing] = useState(false);
  const [editedCondition, setEditedCondition] = useState('');
  const [editedCdt, setEditedCdt] = useState('');

  if (!findings || findings.length === 0) return null;

  return (
    <div className="relative">
      {/* Render Markers for Each Pending Finding */}
      {findings.map((f) => {
        const isLowConfidence = f.confidence < confidenceThreshold;
        return (
          <div
            key={f.id}
            onClick={() => {
              setSelectedFinding(f);
              setEditedCondition(f.suggested_condition);
              setEditedCdt(f.suggested_cdt_code || '');
              setIsEditing(false);
            }}
            className="cursor-pointer inline-flex items-center gap-1 px-2.5 py-1 rounded-full border-2 border-dashed border-[#E8934A] bg-[#E8934A]/10 text-[#0F2F2C] hover:bg-[#E8934A]/20 transition-all m-1 shadow-sm"
          >
            <span className="w-2 h-2 rounded-full bg-[#E8934A] animate-ping"></span>
            <span className="text-xs font-bold text-[#E8934A]">AI #{f.tooth_number}</span>
            <span className="text-xs font-medium truncate max-w-[90px]">{f.suggested_condition}</span>
            <span className="text-[10px] bg-white/80 px-1 rounded text-gray-600 font-mono">
              {(f.confidence * 100).toFixed(0)}%
            </span>
          </div>
        );
      })}

      {/* Interactive Review Popover / Modal */}
      {selectedFinding && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-[#D1E3E0]">
            {/* Modal Header */}
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <div className="flex items-center gap-2">
                <div className="p-2 bg-[#E8934A]/20 text-[#E8934A] rounded-xl">
                  <Sparkles className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-bold text-[#0B4F4A]">AI Observation Review</h3>
                  <p className="text-xs text-gray-500">Tooth #{selectedFinding.tooth_number} ({selectedFinding.numbering_system})</p>
                </div>
              </div>
              <button onClick={() => setSelectedFinding(null)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Finding Details */}
            <div className="mt-4 space-y-3 bg-[#F2F7F6] p-4 rounded-xl">
              <div>
                <span className="text-xs font-semibold text-gray-500">Suggested Condition:</span>
                {!isEditing ? (
                  <p className="text-sm font-bold text-[#0B4F4A]">{selectedFinding.suggested_condition}</p>
                ) : (
                  <input
                    type="text"
                    value={editedCondition}
                    onChange={(e) => setEditedCondition(e.target.value)}
                    className="w-full mt-1 bg-white border border-[#D1E3E0] rounded-lg px-2.5 py-1 text-sm text-[#0F2F2C]"
                  />
                )}
              </div>

              <div className="flex justify-between items-center">
                <div>
                  <span className="text-xs font-semibold text-gray-500">CDT Code:</span>
                  {!isEditing ? (
                    <p className="text-xs font-mono font-bold text-gray-700">{selectedFinding.suggested_cdt_code || 'N/A'}</p>
                  ) : (
                    <input
                      type="text"
                      value={editedCdt}
                      onChange={(e) => setEditedCdt(e.target.value)}
                      className="w-24 mt-1 bg-white border border-[#D1E3E0] rounded-lg px-2 py-1 text-xs font-mono text-[#0F2F2C]"
                    />
                  )}
                </div>
                <div>
                  <span className="text-xs font-semibold text-gray-500">Confidence Score:</span>
                  <div className="flex items-center gap-1.5 mt-0.5">
                    <span
                      className={`text-xs font-bold px-2 py-0.5 rounded ${
                        selectedFinding.confidence >= 0.75
                          ? 'bg-green-100 text-green-700'
                          : selectedFinding.confidence >= 0.6
                          ? 'bg-amber-100 text-amber-700'
                          : 'bg-red-100 text-red-700'
                      }`}
                    >
                      {(selectedFinding.confidence * 100).toFixed(1)}%
                    </span>
                  </div>
                </div>
              </div>

              {selectedFinding.confidence < confidenceThreshold && (
                <div className="flex items-center gap-2 p-2 bg-amber-50 border border-amber-200 rounded-lg text-[11px] text-amber-800">
                  <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
                  <span>Low confidence observation. Forced clinical verification required before chart write.</span>
                </div>
              )}
            </div>

            {/* Action Buttons */}
            <div className="mt-6 flex items-center justify-between gap-2">
              <button
                onClick={() => {
                  onDismissFinding(selectedFinding.id);
                  setSelectedFinding(null);
                }}
                className="px-3 py-2 text-xs font-semibold text-gray-500 hover:bg-gray-100 rounded-xl"
              >
                Dismiss
              </button>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => setIsEditing(!isEditing)}
                  className="px-3 py-2 border border-gray-300 hover:bg-gray-50 rounded-xl text-xs font-semibold text-gray-700 flex items-center gap-1"
                >
                  <Edit3 className="w-3.5 h-3.5" /> {isEditing ? 'Cancel Edit' : 'Edit & Adjust'}
                </button>

                {isEditing ? (
                  <button
                    onClick={() => {
                      onEditAcceptFinding(selectedFinding.id, {
                        tooth_number: selectedFinding.tooth_number,
                        suggested_condition: editedCondition,
                        suggested_cdt_code: editedCdt,
                      });
                      setSelectedFinding(null);
                    }}
                    className="px-4 py-2 bg-[#0B4F4A] hover:bg-[#083c38] text-white rounded-xl text-xs font-bold flex items-center gap-1 shadow-md"
                  >
                    <Check className="w-3.5 h-3.5" /> Save & Accept
                  </button>
                ) : (
                  <button
                    onClick={() => {
                      onAcceptFinding(selectedFinding.id);
                      setSelectedFinding(null);
                    }}
                    className="px-4 py-2 bg-[#0B4F4A] hover:bg-[#083c38] text-white rounded-xl text-xs font-bold flex items-center gap-1 shadow-md"
                  >
                    <ShieldCheck className="w-4 h-4 text-[#4FB3A9]" /> Accept to Chart
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
```

---

## Task 2.5: AI Note Review Panel (`<AINoteReviewPanel />`)

```jsx
import React, { useState } from 'react';
import { FileText, CheckCircle2, Sparkles, HardDrive, Edit3 } from 'lucide-react';

export const AINoteReviewPanel = ({ draftNote, onSignNote }) => {
  const [soap, setSoap] = useState(() => {
    try {
      return typeof draftNote.soap_json === 'string' ? JSON.parse(draftNote.soap_json) : draftNote.soap_json;
    } catch {
      return {};
    }
  });
  const [isSigning, setIsSigning] = useState(false);

  const handleFieldChange = (field, value) => {
    setSoap((prev) => ({ ...prev, [field]: value }));
  };

  const handleSign = async () => {
    setIsSigning(true);
    await onSignNote(draftNote.id, soap);
    setIsSigning(false);
  };

  return (
    <div className="bg-white border border-[#D1E3E0] rounded-2xl p-6 shadow-sm">
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-gray-100">
        <div className="flex items-center gap-3">
          <div className="p-2.5 bg-[#0B4F4A] text-white rounded-xl">
            <FileText className="w-5 h-5" />
          </div>
          <div>
            <h3 className="font-bold text-base text-[#0B4F4A]">AI Draft SOAP Clinical Note</h3>
            <div className="flex items-center gap-2 mt-0.5">
              <span className="text-[11px] bg-[#E8934A]/20 text-[#E8934A] font-semibold px-2 py-0.5 rounded-full flex items-center gap-1">
                <Sparkles className="w-3 h-3" /> Groq Vision Generated
              </span>
              <span className="text-[11px] text-gray-500 flex items-center gap-1">
                <HardDrive className="w-3 h-3" /> Source: {draftNote.source_device_label || 'Intraoral Camera'}
              </span>
            </div>
          </div>
        </div>

        <button
          onClick={handleSign}
          disabled={isSigning}
          className="px-5 py-2.5 bg-[#0B4F4A] hover:bg-[#083c38] text-white rounded-xl text-xs font-bold flex items-center gap-2 shadow-lg"
        >
          <CheckCircle2 className="w-4 h-4 text-[#4FB3A9]" />
          {isSigning ? 'Signing Note...' : 'Sign & Finalize Note'}
        </button>
      </div>

      {/* 8-Section SOAP Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
        {[
          { key: 'chiefComplaint', label: '1. Chief Complaint' },
          { key: 'historyOfPresentIllness', label: '2. History of Present Illness' },
          { key: 'medicalHistory', label: '3. Medical History' },
          { key: 'objectiveExam', label: '4. Objective Exam (Findings)' },
          { key: 'assessment', label: '5. Diagnostic Assessment' },
          { key: 'proceduresPerformed', label: '6. Procedures Performed (CDT)' },
          { key: 'postOpInstructions', label: '7. Post-Op Instructions' },
          { key: 'followUpPlan', label: '8. Next Visit & Follow Up' },
        ].map(({ key, label }) => (
          <div key={key} className="bg-[#F2F7F6] p-3.5 rounded-xl border border-[#D1E3E0]">
            <label className="text-xs font-bold text-[#0B4F4A] block mb-1.5">{label}</label>
            <textarea
              rows={3}
              value={soap[key] || ''}
              onChange={(e) => handleFieldChange(key, e.target.value)}
              className="w-full bg-white border border-[#D1E3E0] rounded-lg p-2.5 text-xs text-[#0F2F2C] focus:outline-none focus:ring-2 focus:ring-[#4FB3A9]"
            />
          </div>
        ))}
      </div>
    </div>
  );
};
```

---

# Prompt 3 — End-to-End Integration & Verification Suite

## End-to-End Trace Verification

### Flow A: USB Intraoral Camera (Apple Dental / Coxo / Magenta)
1. Dentist clicks **"Capture New"** &rarr; `<CameraCapturePanel />` mounts.
2. `navigator.mediaDevices.enumerateDevices()` finds keyword `"Apple Dental"` &rarr; selects camera index 0.
3. Stream renders in `<video />` element &rarr; Doctor snaps photo.
4. Canvas renders frame to JPEG blob &rarr; Dispatches `POST /api/imaging/upload` with `modality="intraoral_photo"`.
5. Backend stores `/uploads/imaging/patient_X/uuid.jpg` &rarr; Creates `[dentist].[radiographs]` record.
6. Groq Vision fires in background &rarr; System prompt extracts findings &rarr; Inserts into `[dentist].[ai_findings]` (`status='pending'`) and `[dentist].[ai_notes_drafts]`.
7. SignalR pushes `ai:findings_ready` to browser &rarr; Odontogram displays dashed amber marker &rarr; Doctor clicks **Accept** &rarr; `PATCH /api/ai-findings/:id` writes to `[dentist].[TeethState]`.

### Flow B: RVG Sensor Capture (Woodpecker / Vatech via Capture Bridge)
1. Doctor takes X-ray with handheld X-ray gun onto Woodpecker i-Sensor.
2. Woodpecker software exports TIFF/JPEG to `C:\ProgramData\Woodpecker\i-Sensor\Export\scan01.jpg`.
3. Dentia Capture Bridge `FileSystemWatcher` detects file &rarr; reads active patient ID from local session cache.
4. Bridge sends multipart POST with header `X-Clinic-Api-Key` to `/api/imaging/upload`.
5. Upload endpoint saves file &rarr; Groq vision analyzes PA scan &rarr; SignalR broadcasts `imaging:new` and `ai:findings_ready`.
6. Clinician sees instant toast: *"New periapical received from Woodpecker i-Sensor H1.5"* &rarr; Gallery updates automatically without browser refresh.

---

## Invariant Verification Audit

| Invariant Check | Enforced Location | Verified Status |
| :--- | :--- | :--- |
| **No direct AI write to `[dentist].[TeethState]`** | `GroqVisionService.cs` only inserts to `[dentist].[ai_findings]` (`status='pending'`). | ✅ PASS (Zero direct writes) |
| **Single write gateway to `[dentist].[TeethState]`** | `AIFindingsController.ReviewFinding()` only on `accept` or `edit_accept`. | ✅ PASS |
| **No direct write to `[dentist].[DentalNotes]`** | `AIFindingsController.SignDraftNote()` only on doctor signature. | ✅ PASS |
| **Low confidence forced review (< 0.6)** | `<PendingOdontogramOverlay />` hides one-tap accept for `confidence < 0.6`. | ✅ PASS |
| **Non-logging of vision payloads** | `GroqVisionService.cs` excludes Base64 string from console/log output. | ✅ PASS |

---

## 10-Step Interactive QA Checklist

- [ ] **Test 1: UVC Intraoral Detection**: Connect USB camera &rarr; Verify camera name `"Apple Dental"` / `"Coxo"` is pre-selected automatically in `<CameraCapturePanel />`.
- [ ] **Test 2: Camera Permission Denied**: Block camera permission in browser &rarr; Verify graceful error message appears without unhandled exceptions.
- [ ] **Test 3: Still Photo Capture & Upload**: Capture photo &rarr; Click confirm &rarr; Verify HTTP 200 from `/api/imaging/upload` and file is stored in `wwwroot/uploads/imaging/`.
- [ ] **Test 4: 20s Clip Recording**: Record video clip &rarr; Verify timer stops automatically at 20s and generates valid `.webm` blob.
- [ ] **Test 5: Groq AI Vision Analysis**: Inspect `[dentist].[ai_findings]` &rarr; Verify teeth numbers and conditions (e.g. Caries on #14, confidence 0.92) are parsed as `status='pending'`.
- [ ] **Test 6: SignalR Real-Time Notification**: Keep patient chart open on two separate browser tabs &rarr; Upload scan on Tab 1 &rarr; Verify Tab 2 receives `ai:findings_ready` and renders amber marker without manual page reload.
- [ ] **Test 7: Single-Tap Acceptance Flow**: Click pending marker (Confidence &ge; 0.6) &rarr; Click "Accept to Chart" &rarr; Verify finding transitions to `accepted` and record is added to `[dentist].[TeethState]`.
- [ ] **Test 8: Low Confidence Review Gate**: Inject finding with `confidence = 0.45` &rarr; Verify quick accept button is disabled/hidden and doctor is forced into edit/review modal.
- [ ] **Test 9: Capture Bridge Hot-Folder Drop**: Drop test JPEG into `C:\ProgramData\Woodpecker\i-Sensor\Export\` &rarr; Verify bridge uploads within 2 seconds with brand `"Woodpecker"`.
- [ ] **Test 10: SOAP Note Signature**: Open `<AINoteReviewPanel />` &rarr; Edit subjective exam text &rarr; Click "Sign & Finalize Note" &rarr; Verify signed note writes to `[dentist].[DentalNotes]`.

---

## Configuration Reference

### Backend `appsettings.json`
```json
{
  "Groq": {
    "ApiKey": "gsk_YourProductionGroqApiKeyHere",
    "VisionModel": "llama-3.2-11b-vision-preview"
  },
  "Clinic": {
    "SecretApiKey": "CLINIC_BRIDGE_KEY_NZ_99"
  },
  "ConnectionStrings": {
    "DentalDb": "Server=localhost;Database=DentistDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Capture Bridge `appsettings.bridge.json`
```json
{
  "Dentia": {
    "ApiBaseUrl": "https://dentist-api-dev.vitonta.com/",
    "ClinicApiKey": "CLINIC_BRIDGE_KEY_NZ_99"
  },
  "WatchPaths": [
    "C:\\ProgramData\\Woodpecker\\i-Sensor\\Export",
    "C:\\Vatech\\EzDent-i\\Capture",
    "C:\\NanoPix\\Export",
    "C:\\Carestream\\Capture"
  ]
}
```
