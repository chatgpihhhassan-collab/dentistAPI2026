# Dentia — Imaging + Groq AI Integration: Implementation Prompts

This is a set of ready-to-paste prompts for building the feature described in the
Imaging & AI Integration Report. Use them with an AI coding assistant (Claude Code,
Cursor, etc.) one at a time, in order. Each prompt is self-contained enough to hand
to an agent and let it work, but they build on each other — paste **Prompt 0** first
in every new session so the agent has shared context, then paste the backend or
frontend prompt for whatever you're building that session.

**How to use this doc**
1. Start a fresh session in your coding agent for the backend work. Paste **Prompt 0**, then **Prompt 1 (Backend)**.
2. Start a separate session for frontend work (or continue in the same repo). Paste **Prompt 0**, then **Prompt 2 (Frontend)**.
3. Work through the numbered sub-tasks inside each prompt one at a time rather than asking for everything at once — the agent will produce better code in smaller steps.
4. Use **Prompt 3 (Integration Test Pass)** once both sides are built, to wire them together and verify the end-to-end flow.

---

## Prompt 0 — Shared Project Context (paste first, every session)

```
You are working on Dentia, a dental EHR/practice-management web app. I'm adding a
new capability: capturing images/video from chairside hardware (X-ray sensors,
intraoral cameras, webcams), routing them into the existing "Imaging & X-Rays"
module, sending them to Groq's vision API for an AI clinical read, and turning
that read into a draft SOAP note plus draft odontogram chart changes.

Existing system context you should assume:
- Patients, Doctors, Appointments, TeethState, ClinicalLogs/DentalNotes,
  Radiographs, Prescriptions, and PeriodontalChart are existing entities/tables.
- The odontogram supports Universal (1-32, A-T) and FDI numbering, dual-jaw
  charting, and 5-surface (MODBL: Mesial, Occlusal/Incisal, Distal, Buccal/Facial,
  Lingual/Palatal) condition tagging.
- There is already an 8-section AI SOAP note format used by an "Ambient AI Voice
  Scribe" feature (Subjective, Objective Exam, Odontogram Sync, Assessment & Dx,
  Procedures Performed, Materials & Pharmacology, Post-Op Instructions, Follow-Up
  & Recall). New AI-generated notes from imaging should use this same format and
  land in the same notes table so voice-dictated and image-triggered notes are
  indistinguishable in storage.
- CORE PRINCIPLE — "Manual Save Safety": nothing an AI proposes is ever written
  to the permanent patient record automatically. AI output is always staged as a
  pending/draft item that a dentist must explicitly Accept (or Accept-with-edits)
  before it touches TeethState, ClinicalLogs, or any other persisted clinical
  table. Never design a code path that auto-commits an AI suggestion.

Two device categories, two different capture paths — keep this distinction
explicit in any code you write:
1. UVC devices (intraoral cameras, webcams) — plug-and-play, captured directly
   in the browser via getUserMedia()/MediaRecorder. No extra backend bridge
   needed for capture itself.
2. TWAIN/DICOM devices (X-ray sensors, panoramic/OPG machines) — NOT reachable
   from a sandboxed browser. These require a separate local "Capture Bridge"
   service running on the clinic PC that watches the vendor software's export
   folder/DICOM node and pushes new images to our backend over HTTPS.

Confirm you understand this context, then wait for the next prompt before writing code.
```

---

## Prompt 1 — Backend Implementation

```
Build the backend for Dentia's imaging capture + Groq AI observation pipeline.
Work through these sub-tasks in order, showing me the result after each one
before moving to the next.

### 1. Database schema
Add/extend tables (adapt syntax to our actual DB — ask me if you're unsure
which one we're using):

- `radiographs` (or extend existing [dentist].[Radiographs]):
  id, patient_id (FK), tooth_key (nullable), modality (enum: 'periapical',
  'bitewing', 'panoramic', 'cephalometric', 'intraoral_photo', 'intraoral_video',
  'consult_video'), source_device (enum: 'sensor', 'opg', 'intraoral_camera',
  'webcam'), file_url, thumbnail_url, captured_at, appointment_id (FK, nullable),
  uploaded_by (doctor_id FK), created_at.

- `ai_findings`: id, radiograph_id (FK), patient_id (FK), tooth_number,
  numbering_system ('Universal' | 'FDI'), surfaces (array/json, e.g. ["D","O"]),
  finding_text, suggested_condition, suggested_cdt_code, confidence (float 0-1),
  status (enum: 'pending', 'accepted', 'edited_accepted', 'dismissed'),
  reviewed_by (doctor_id FK, nullable), reviewed_at (nullable), created_at.

- `ai_notes_drafts`: id, radiograph_id (FK, nullable — also usable by the voice
  scribe so make this generic), patient_id (FK), soap_json (the 8-section
  content), status ('draft' | 'signed'), signed_by (nullable), signed_at
  (nullable), created_at.

### 2. Upload/ingest endpoints
- `POST /api/imaging/upload` — accepts multipart image/video + metadata
  (patient_id, modality, source_device, appointment_id, tooth_key optional).
  Saves the file to storage, inserts a `radiographs` row, and — if
  `auto_analyze=true` in the request — kicks off the Groq analysis job
  (async, don't block the upload response on it).
- `GET /api/imaging/:patientId` — list radiographs for a patient, paginated,
  filterable by modality and date range.
- `GET /api/imaging/file/:radiographId` — stream/serve the stored image
  (respect the auth/role checks already used elsewhere in the app for patient
  data access).

### 3. Groq AI analysis service
Write a service module `groqVisionService` with a function
`analyzeRadiograph(radiographId)` that:
- Loads the image and relevant patient context (active numbering system,
  tooth_key if the image was already tagged to a tooth).
- Calls Groq's chat completions endpoint (OpenAI-SDK-compatible) with the
  image base64-encoded, in JSON mode, using this system prompt template:

  """
  You are a dental radiograph/photo analysis assistant. Analyze the attached
  {{modality}} image{{#if tooth_key}} of tooth {{tooth_key}}{{/if}}. Use only
  the {{numbering_system}} tooth numbering system. Constrain findings to this
  vocabulary: caries, periapical radiolucency, bone loss, fracture, calculus,
  restoration, missing tooth, implant, impaction. Respond ONLY in JSON matching
  this schema: { "findings": [ { "toothNumber": string, "numberingSystem":
  string, "surface": string[], "finding": string, "suggestedCondition": string,
  "suggestedCdtCode": string, "confidence": number } ] }. If you are not
  confident about a value, still include it but lower the confidence score
  accordingly. Never invent a tooth number you cannot identify — omit that
  finding instead.
  """

- Parses the JSON response, inserts one `ai_findings` row per finding
  (status='pending').
- Generates a draft SOAP note (reuse/extend whatever note-generation logic
  already backs the voice scribe if it exists) and inserts an `ai_notes_drafts`
  row, populating at minimum the Objective Exam and Procedures Performed
  sections from the findings.
- Emits a WebSocket event (see task 5) so the open frontend session updates
  live.
- Wrap the whole thing in try/catch; on failure, mark the radiograph with an
  `analysis_status` of 'failed' rather than leaving it stuck — don't let a
  Groq error break the upload flow.

### 4. Review/accept endpoints
- `GET /api/ai-findings/:patientId?status=pending` — list pending findings for
  a patient (used to render the pending overlay on the odontogram).
- `PATCH /api/ai-findings/:findingId` — body: `{ action: 'accept' | 'edit_accept'
  | 'dismiss', edits?: {...}, reviewed_by }`. On 'accept' or 'edit_accept', this
  is the ONLY code path allowed to write to the TeethState table — apply the
  (possibly edited) tooth/surface/condition/CDT code there, then update the
  finding's status. On 'dismiss', just update status — no TeethState write.
- `PATCH /api/ai-notes-drafts/:draftId/sign` — marks a draft note as signed
  (only after the dentist has reviewed/edited it in the UI), writes it into
  the same clinical-notes table the voice scribe uses.

### 5. Real-time notification
Add a WebSocket (or SSE, your choice — tell me which and why) channel scoped
per clinic session so that when a Capture Bridge upload or a Groq analysis
completes, the open Dentia browser tab for that patient gets pushed an event
(`imaging:new`, `ai:findings_ready`) without polling.

### 6. Capture Bridge service (separate small app)
Scaffold a minimal local Windows service/CLI app (Node or .NET — pick one,
tell me why) that:
- Watches a configured folder for new image files (simulate the sensor/OPG
  vendor software dropping files there).
- On a new file, POSTs it to `/api/imaging/upload` with the currently-active
  patient/appointment context (read from a small local config file or a
  short-lived token the main app writes when a chart is opened).
- Retries on network failure, logs failures locally, never silently drops a
  captured X-ray.

### 7. Security
- All endpoints require the existing auth middleware; add a check that the
  requesting user has clinical access to that patient (reuse existing
  role-based checks — tell me what those look like in this codebase or ask
  me to point you to them).
- Images to Groq go over TLS; never log the base64 image payload.
- The Capture Bridge authenticates with a clinic-scoped API token, not a
  user password.

Show me the schema migration first, then the endpoints, then the Groq
service, then the Capture Bridge scaffold. Ask me before picking a tech stack
for the Capture Bridge if it isn't obvious from the existing repo.
```

---

## Prompt 2 — Frontend Implementation

```
Build the frontend for Dentia's imaging capture + AI observation review UI.
Work through these sub-tasks in order, showing me each component before
moving to the next. Match the existing Dentia visual style: deep teal
(#0B4F4A) primary, seafoam (#4FB3A9) accents, warm amber (#E8934A) for
highlights/pending states, soft grey-teal cards (#F2F7F6), rounded corners,
generous whitespace. Reuse existing design tokens/components if this repo
already has them instead of inventing new ones.

### 1. Camera Capture Panel (`<CameraCapturePanel />`)
For UVC devices only (intraoral cameras behave like webcams to the browser —
no separate code path needed for them vs. a laptop webcam).
- On mount, call `navigator.mediaDevices.enumerateDevices()` and list video
  input devices in a dropdown so the dentist can pick the intraoral camera
  if more than one camera is attached.
- Use `getUserMedia()` to show a live preview once a device is selected.
- Two capture modes:
  - **Capture Photo** — grab a still frame from the video element onto a
    canvas, convert to blob, show a confirm/retake screen.
  - **Record Clip** — use `MediaRecorder` to record up to a configurable max
    duration (default 20s), with a visible timer and a Stop button.
- On confirm, upload the blob to `POST /api/imaging/upload` (from the backend
  prompt) with `patient_id`, `modality` ('intraoral_photo' | 'intraoral_video'
  | 'consult_video' depending on context), `tooth_key` (if the panel was
  opened from a specific tooth in the odontogram), and `auto_analyze: true`.
- Show upload progress and a success state with a thumbnail once done.
- Handle and clearly display permission-denied and no-camera-found states —
  don't fail silently.

### 2. Imaging & X-Rays Gallery (`<ImagingGallery />`)
- Fetches `GET /api/imaging/:patientId`, renders a filterable grid
  (by modality, date) of thumbnails.
- Each thumbnail opens a lightbox/detail view showing the full image, its
  metadata (modality, captured date, tooth if tagged), and — if AI analysis
  ran — a summary of the findings with a link to jump to the pending-review
  UI (task 4).
- Include an empty state and a "Capture New" button that opens the
  CameraCapturePanel (task 1) or, for X-ray/OPG modalities, shows a short
  "captured automatically from your connected sensor" hint instead of a
  capture button, since those come from the Capture Bridge, not the browser.

### 3. Live capture notifications
- Subscribe to the WebSocket/SSE channel from the backend
  (`imaging:new`, `ai:findings_ready`) for the currently open patient.
- On `imaging:new` (e.g., a Capture Bridge just uploaded a new X-ray), show a
  toast/inline banner: "New [modality] received" and auto-refresh the gallery
  — this is how a chairside X-ray appears without the dentist doing anything
  in the browser.
- On `ai:findings_ready`, show a subtler "AI review ready" indicator near the
  odontogram (task 4 consumes this).

### 4. Pending AI Findings Overlay on the Odontogram
This is the most important UI piece — it must never look like a confirmed
chart state.
- Fetch `GET /api/ai-findings/:patientId?status=pending` when the chart loads
  and whenever `ai:findings_ready` fires.
- For each pending finding, render a distinct marker on the relevant tooth —
  dashed outline, amber color, a small "AI" badge — visually separate from
  the solid-color confirmed condition markers already on the odontogram.
  Do not reuse the confirmed-condition color styling for pending items.
- Tapping/hovering a pending marker opens a small card showing: the source
  thumbnail, the finding text, suggested condition + CDT code, confidence
  score (with a visual low/medium/high indicator), and three actions:
  **Accept**, **Edit & Accept**, **Dismiss**.
  - Accept → `PATCH /api/ai-findings/:id { action: 'accept' }`, marker
    converts to a normal confirmed-condition marker on success.
  - Edit & Accept → opens the existing tooth-detail surface/condition editor
    pre-filled with the AI's suggestion, dentist adjusts, confirms → PATCH
    with `action: 'edit_accept'` and the edited values.
  - Dismiss → PATCH with `action: 'dismiss'`, marker disappears, no chart
    write happens.
- Findings below a confidence threshold (pass this as a prop, default 0.6)
  should NOT show a one-tap Accept button — only "Review" which opens the
  full tooth detail view first, forcing the dentist to look closer before
  accepting anything low-confidence.

### 5. AI Note Review Panel
- When an `ai_notes_drafts` row exists for the patient's current visit, show
  it in the same panel/flow used for the existing voice-scribe SOAP notes
  (reuse that component if it exists — ask me to point you to it) — the
  dentist should not be able to tell, from the UI, whether a draft note came
  from voice dictation or an X-ray/photo, except for a small "Source: [image
  thumbnail]" chip that links back to the originating image.
- Edit-in-place for each of the 8 SOAP sections, then a Sign button that
  calls `PATCH /api/ai-notes-drafts/:id/sign`.

### 6. States & error handling
For every component above, explicitly design and show me: loading state,
empty state, error state (upload failed / Groq analysis failed / permission
denied), and the success state. Don't leave any of these as an afterthought —
ask me for the exact copy/wording if you're unsure what tone to use.

Show me component 1 fully working (including the confirm/retake flow) before
moving to component 2, and so on.
```

---

## Prompt 3 — Integration Test Pass (run after both sides exist)

```
Backend and frontend for the imaging + AI pipeline both exist now. Do an
integration pass:

1. Trace the full happy path end-to-end in code (don't just describe it —
   actually open each file in the chain) for: intraoral camera capture →
   upload → Groq analysis → pending marker appears on odontogram → dentist
   accepts → TeethState updated → marker becomes a confirmed condition.
   Flag any place the chain is broken or where an assumption doesn't match
   what the other side actually built.

2. Confirm — by reading the code, not by trusting the comments — that there
   is no code path anywhere that writes to TeethState or the clinical notes
   table without going through the accept/sign endpoints in Prompt 1, task 4.
   This is the one invariant that must never be violated.

3. Write a short manual test checklist I can run through in a browser with a
   real or mocked camera device, covering: successful capture, camera
   permission denied, upload failure/retry, Groq analysis failure (mock a
   500 from Groq), low-confidence finding review flow, edit-and-accept flow,
   dismiss flow, and the Capture Bridge's simulated-file-drop flow.

4. List anything you had to guess or assume because it wasn't specified, so
   I can confirm or correct it.
```

---

## Notes on adapting these prompts

- **Tech stack**: the prompts are intentionally stack-agnostic (they say
  "adapt to our actual DB/framework") so the agent should ask before assuming
  Postgres/Node/React if your repo uses something else — if it doesn't ask,
  tell it explicitly what the stack is before running Prompt 1 or 2.
- **Groq model name**: Groq rotates which vision model is default every so
  often. Before running Prompt 1, check `https://console.groq.com/docs/vision`
  for the current model slug and paste it into the prompt (or tell the agent
  to read your `.env`/config if you've already set one).
- **Capture Bridge distribution**: Prompt 1 task 6 scaffolds the bridge but
  doesn't cover packaging/installer or auto-update — treat that as a follow-up
  prompt once the core bridge logic works.
- **Run the prompts as separate sessions** for backend and frontend if your
  agent tends to lose focus in very long single sessions — Prompt 0 keeps
  both sessions aligned on the same data model and safety invariant.
