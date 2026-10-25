# 📋 Dentia Treatment Session Tracking, Auto-Billing & Real-Time Audit Logs Specification

**Document Version:** 1.0.0  
**Target Date:** October 25, 2026  
**System:** Dentia Clinical Practice Management Platform  
**Target Environments:** Frontend (`React 19 / Vite`), Backend (`ASP.NET Core 9.0 / Dapper`), Database (`Microsoft SQL Server 2022`)

---

## 1. Executive Summary & Root Cause Analysis

### 1.1 Problem Statement
When a doctor started a chairside treatment session on the Odontogram (`ChartPage.jsx`) and subsequently ended it for a new patient (e.g., *Sadaf Imran*, ID `#1055`), the following issues occurred:
1. **Empty Treatment Plan:** No initial entry was added to the Treatment Plan for the new patient (specifically, the initial Consultation Fee was missing).
2. **Missing Consultation & X-Ray Fee Visibility:** In `TreatmentPlanTab.jsx`, consultation and X-ray prices were not shown separately; category filter tabs showed only `ALL 0` because categories were dynamically extracted only from existing items rather than showing fixed standard tabs (`ALL`, `CONSULTATION`, `X-RAY`, `BRACES`, `WHITENING`, `CAVITY`).
3. **Lost Session Time & History:** When the session timer ended, the elapsed time disappeared from the UI, and no historical record or audit log remained of what clinical actions the doctor performed during the session.
4. **Performance Bottleneck:** The chart page re-rendered heavily on each 1-second timer tick, slowing down chart interactions.

### 1.2 Root Cause Analysis
1. **Silent 401 Unauthorized on Auto-Billing:** `ChartPage.jsx` and `TreatmentPlanTab.jsx` were calling `POST /api/patients/{id}/auto-diagnostic-fee` using native `fetch()` without an `Authorization: Bearer <token>` header instead of the centralized `apiFetch` utility. The backend rejected unauthenticated requests with `HTTP 401 Unauthorized`, which was swallowed by a silent `catch` block.
2. **Payload Property Mismatch:** The backend returns `{ added: true, fee: 5000, ... }`, whereas frontend code checked `if (data.billed)`, resulting in false negative checks even if a 200 response had arrived.
3. **Absence of Dedicated `TreatmentSessions` Database Table:** Session durations were previously only partially stored in an appointment record notes field. If a patient was accessed without an appointment ID, the session vanished. No action logs were tracked.
4. **Un-isolated Timer State:** Timer state (`treatmentSeconds`) was stored at the root of `ChartPage.jsx` (an 800+ KB, 13,000+ line component), causing unnecessary re-renders of the 32 SVG tooth structures every second.

---

## 2. Architecture & Design

```
+----------------------------------------------------------------------------------------------------+
|                                    DENTIA CHAIRSIDE SESSION WORKFLOW                              |
+----------------------------------------------------------------------------------------------------+
                                                  │
                 Doctor Opens Patient Chart       │  [Parallel Promise.all: Patient, Chart, Policy, Latest Session]
                                                  ▼
                         +──────────────────────────────────────────────────+
                         |  Check & Auto-Ensure Consultation Fee           |
                         |  (Doctor's Rate from DoctorFeeSchedules D0150)   |
                         +──────────────────────────────────────────────────+
                                                  │
                     Doctor clicks "Start Treatment"│  (Session begins)
                                                  ▼
                         +──────────────────────────────────────────────────+
                         |  [dentist].[TreatmentSessions] (Status: active)  |
                         |  Isolated Micro-Timer: ⏱️ Active: 04:32 (Pulsing)  |
                         +──────────────────────────────────────────────────+
                                                  │
             Doctor performs chairside actions    │  (Action Logger buffers in real-time)
             ├─ Tooth #19 Diagnosed: Occlusal Caries
             ├─ Nano-Pix RVG X-Ray Acquired on #19
             ├─ Added RCT & Crown to Treatment Plan
             ├─ Dictated Voice Clinical Observation Note
                                                  │
                     Doctor clicks "Record & End"  │  (Session completed)
                                                  ▼
                         +──────────────────────────────────────────────────+
                         |  Update Session: Duration (e.g. 18m 42s)         |
                         |  Persist ActionLogsJson with timestamps          |
                         +──────────────────────────────────────────────────+
                                                  │
                     Chart Page Header reflects    │  (Always visible across page reloads)
                                                  ▼
                         +──────────────────────────────────────────────────+
                         |  ⏱️ Last Session: 18m 42s (Oct 25, 10:45 PM)     |
                         |  Clicking badge opens Session Audit Log Drawer   |
                         +──────────────────────────────────────────────────+
```

---

## 3. Database Schema

### 3.1 Table: `[dentist].[TreatmentSessions]`
Tracks every chairside clinical encounter, its start/end times, calculated duration, doctor, appointment association, and complete action log.

| Column | Data Type | Constraints | Description |
|---|---|---|---|
| `SessionID` | `BIGINT` | `IDENTITY(1,1) PRIMARY KEY` | Unique Session Identifier |
| `PatientID` | `INT` | `NOT NULL, FK -> Patients` | Patient undergoing treatment |
| `DoctorID` | `INT` | `NOT NULL, FK -> Doctors` | Attending Dentist |
| `AppointmentID` | `INT` | `NULL, FK -> Appointments` | Linked appointment (if any) |
| `StartedAt` | `DATETIME2` | `NOT NULL DEFAULT SYSUTCDATETIME()` | Session initiation timestamp |
| `EndedAt` | `DATETIME2` | `NULL` | Session termination timestamp |
| `DurationSeconds` | `INT` | `NOT NULL DEFAULT 0` | Total session elapsed seconds |
| `FormattedDuration`| `NVARCHAR(50)` | `NOT NULL DEFAULT '00:00'` | Human-readable duration (e.g., `18m 42s`) |
| `Status` | `NVARCHAR(30)` | `NOT NULL DEFAULT 'active'` | `'active'`, `'completed'`, `'cancelled'` |
| `ActionCount` | `INT` | `NOT NULL DEFAULT 0` | Count of clinical actions performed |
| `SummaryNotes` | `NVARCHAR(MAX)`| `NULL` | Final doctor clinical recap |
| `ActionLogsJson` | `NVARCHAR(MAX)`| `NULL` | Complete JSON array of audit action logs |
| `CreatedAt` | `DATETIME2` | `NOT NULL DEFAULT SYSUTCDATETIME()` | Record creation timestamp |
| `UpdatedAt` | `DATETIME2` | `NOT NULL DEFAULT SYSUTCDATETIME()` | Last update timestamp |

### 3.2 Action Log JSON Specification (`ActionLogsJson`)
Each action entry stored in `ActionLogsJson` complies with the following schema:
```json
[
  {
    "id": "act_1729876201_1",
    "timestamp": "2026-10-25T22:30:00.000Z",
    "elapsedSeconds": 0,
    "actionType": "session_started",
    "title": "Treatment Session Started",
    "description": "Chairside clinical session initiated by Dr. Ahmed.",
    "metadata": { "doctorId": 1, "patientId": 1055 }
  },
  {
    "id": "act_1729876205_2",
    "timestamp": "2026-10-25T22:30:05.000Z",
    "elapsedSeconds": 5,
    "actionType": "consultation_billed",
    "title": "Consultation Fee Auto-Added",
    "description": "Added 'Dental Consultation & Diagnosis (D0150)' to Treatment Plan (Rs 5,000).",
    "metadata": { "procedureCode": "D0150", "fee": 5000, "source": "auto_consultation" }
  },
  {
    "id": "act_1729876320_3",
    "timestamp": "2026-10-25T22:32:00.000Z",
    "elapsedSeconds": 120,
    "actionType": "tooth_diagnosed",
    "title": "Tooth #19 Diagnosed",
    "description": "Diagnosed Tooth #19: Deep Occlusal Caries (Restorative).",
    "metadata": { "toothNumber": 19, "condition": "Caries", "surface": "O" }
  },
  {
    "id": "act_1729876530_4",
    "timestamp": "2026-10-25T22:35:30.000Z",
    "elapsedSeconds": 330,
    "actionType": "radiograph_acquired",
    "title": "RVG Radiograph Captured",
    "description": "Acquired Digital RVG X-Ray on Tooth #19 using Eighteeth Nano-Pix.",
    "metadata": { "modality": "RVG", "toothNumber": 19, "device": "Eighteeth Nano-Pix" }
  },
  {
    "id": "act_1729877322_5",
    "timestamp": "2026-10-25T22:48:42.000Z",
    "elapsedSeconds": 1122,
    "actionType": "session_completed",
    "title": "Session Completed & Recorded",
    "description": "Clinical encounter concluded. Duration: 18m 42s. Total actions logged: 5.",
    "metadata": { "durationSeconds": 1122, "formattedDuration": "18m 42s" }
  }
]
```

---

## 4. API Endpoints Specification

### 4.1 Start Session
- **Endpoint:** `POST /api/patients/{id}/treatment-sessions/start`
- **Headers:** `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Request Body:**
  ```json
  {
    "doctorId": 1,
    "appointmentId": 120,
    "notes": "Chairside treatment session started on Odontogram"
  }
  ```
- **Behavior:**
  1. Checks if patient already has an active session; resumes it or creates a new one.
  2. Ensures consultation fee exists in `[dentist].[TreatmentItems]`; if absent, auto-inserts it using the doctor's fee schedule.
  3. Appends initial `session_started` and `consultation_billed` action logs.
- **Response:** `200 OK`
  ```json
  {
    "sessionId": 45,
    "patientId": 1055,
    "doctorId": 1,
    "startedAt": "2026-10-25T22:30:00Z",
    "status": "active",
    "consultationFeeAdded": true,
    "consultationFee": 5000
  }
  ```

### 4.2 End Session
- **Endpoint:** `POST /api/patients/{id}/treatment-sessions/{sessionId}/end`
- **Headers:** `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Request Body:**
  ```json
  {
    "durationSeconds": 1122,
    "formattedDuration": "18m 42s",
    "summaryNotes": "Procedure concluded without complications.",
    "actions": [ /* array of action objects */ ]
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "success": true,
    "sessionId": 45,
    "formattedDuration": "18m 42s",
    "actionCount": 5,
    "message": "Treatment session completed and audit logs persisted."
  }
  ```

### 4.3 Get Latest Session
- **Endpoint:** `GET /api/patients/{id}/treatment-sessions/latest`
- **Headers:** `Authorization: Bearer <token>`
- **Response:** `200 OK`
  ```json
  {
    "hasSession": true,
    "sessionId": 45,
    "patientId": 1055,
    "doctorId": 1,
    "doctorName": "Dr. Ahmed",
    "startedAt": "2026-10-25T22:30:00Z",
    "endedAt": "2026-10-25T22:48:42Z",
    "durationSeconds": 1122,
    "formattedDuration": "18m 42s",
    "status": "completed",
    "actionCount": 5,
    "actionLogs": [ /* array of action objects */ ]
  }
  ```

### 4.4 Get All Sessions History
- **Endpoint:** `GET /api/patients/{id}/treatment-sessions`
- **Headers:** `Authorization: Bearer <token>`
- **Response:** `200 OK` returns list of all sessions sorted descending by `StartedAt`.

---

## 5. Frontend UI/UX & Performance Optimizations

### 5.1 Isolated Timer Micro-Component (`ChairsideTimerBadge`)
- **Problem:** Ticking `treatmentSeconds` state inside `ChartPage.jsx` triggered re-renders of the entire 13,000-line DOM tree, including all 32 odontogram tooth SVGs and surface polygon masks every 1000ms.
- **Solution:** Encapsulate timer logic into `<ChairsideTimerBadge startTime={...} onEnd={...} />`. The micro-component manages its own local 1-second interval, causing zero re-renders to the parent page.

### 5.2 Header Display States
1. **Idle State (No session running, but previous session exists):**
   - Renders: `⏱️ Last Session: 18m 42s` (Slate/Indigo badge) + `[Start Treatment]` (Emerald button).
   - Clicking `Last Session` opens the Session Audit Log Drawer.
2. **Active State (Session running):**
   - Renders: Pulsing green dot + `⏱️ Active Session: 04:35` (Font-mono, high contrast) + `[Record & End]` button.
   - Clinical actions automatically append to the active session buffer.

### 5.3 Treatment Plan Category Separation
- **Standard Tabs Rendered Unconditionally:**
  `ALL`, `CONSULTATION`, `X-RAY`, `BRACES`, `WHITENING`, `CAVITY`
- **Badge Counters:** Each category displays the exact procedure count (e.g. `CONSULTATION 1`, `X-RAY 0`, `BRACES 0`).
- **Quick Add Modality Bar:** Prominently displays doctor's configured consultation rate and X-ray rate with 1-click addition.

---

## 6. Verification & Quality Assurance Checklist
- [x] Verify API endpoint `/api/patients/{id}/auto-diagnostic-fee` uses authenticated `apiFetch`.
- [x] Verify response property check supports `data.added || data.Added || data.billed`.
- [x] Verify SQL migration creates `[dentist].[TreatmentSessions]` with proper indexes.
- [x] Verify starting a session creates an active session and auto-adds consultation fee.
- [x] Verify ending a session records total duration and detailed action logs in database.
- [x] Verify `ChartPage` displays `Last Session: XXm YYs` immediately upon page reload.
- [x] Verify clicking session duration pill opens the comprehensive Audit Log modal.
- [x] Verify Odontogram rendering is decoupled from 1-second timer ticks for maximum speed.
