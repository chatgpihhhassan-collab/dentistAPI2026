# 📱 Dentia WhatsApp Integration Specification (OpenWA API Gateway)

> **Document Type:** System Architecture & Integration Specification  
> **Author:** Dentia Engineering & Architecture Team  
> **Version:** 1.0.0 (Production Blueprint)  
> **Target Application:** Dentia Dental Practice Operating System  
> **Related Database Migration:** [`WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql`](file:///f:/DentistApp_Theme2/WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql)

---

## 1. Executive Summary & Stack Overview

This document specifies the end-to-end integration of self-hosted **OpenWA** ([github.com/rmyndharis/OpenWA](https://github.com/rmyndharis/OpenWA)) into the **Dentia** dental application. 

### Current Application Stack
* **Backend:** ASP.NET Core 8.0 Web API (C#) (`DentistAPIClone/API_dentist` / `DentistAPI`)
* **Frontend:** React 19 + Vite (Vanilla CSS & Tailwind) (`Dentistfrontend`)
* **Database:** Microsoft SQL Server 2019/2022 / Azure SQL (`[dentist]` schema)
* **Default Country & Phone Code:** Pakistan (`+92`, local format `03XXXXXXXXX` &bull; normalized E.164 `923XXXXXXXXX`)
* **Background Queue Engine:** ASP.NET Core `IHostedService` (`BackgroundService`) backed by SQL Queue table `[dentist].[WhatsAppMessages]` (Zero external Redis dependency required; built-in persistent retries, rate-limiting & jitter).

---

## 2. Planned File Structure

The following files are planned for implementation across the repository:

```
f:\DentistApp_Theme2\
│
├── WHATSAPP_OPENWA_INTEGRATION_SPECIFICATION.md  <-- THIS SPECIFICATION
├── WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql     <-- IDEMPOTENT SQL SERVER SCRIPT
├── docker-compose.openwa.yml                     <-- DOCKER COMPOSE SERVICE DEFINITION
│
├── DentistAPIClone\API_dentist\ (and DentistAPI\)
│   ├── Controllers\
│   │   ├── WhatsAppController.cs                 <-- Admin API (Session status, QR code, test send, logs)
│   │   └── WhatsAppWebhookController.cs          <-- OpenWA Webhook receiver (HMAC-SHA256 signature verification)
│   │
│   ├── Services\WhatsApp\
│   │   ├── IWhatsAppService.cs                   <-- WhatsApp Service Interface
│   │   ├── WhatsAppService.cs                    <-- OpenWA HTTP Client wrapper, message dispatcher & templates
│   │   ├── WhatsAppBackgroundQueueService.cs     <-- BackgroundService rate-limiter (5 msgs/min, 2-6s delay)
│   │   └── WhatsAppPhoneNormalizer.cs            <-- Pakistani (+92) phone validator & @c.us chatId builder
│   │
│   ├── Models\
│   │   ├── WhatsAppModels.cs                     <-- DTOs, Queue Models, Webhook payloads, Admin stats
│   │   └── DentalModels.cs                       <-- Patient model updated with WhatsAppOptIn properties
│   │
│   └── appsettings.json                          <-- OpenWA configuration keys
│
└── Dentistfrontend\src\
    ├── pages\
    │   ├── WhatsAppAdminPage.jsx                 <-- Admin dashboard (QR code scanner, status, live message logs)
    │   ├── PatientDirectory.jsx                  <-- UPDATED: WhatsApp Active/Inactive badge & direct chat link
    │   ├── NewPatientPage.jsx                    <-- UPDATED: WhatsApp Opt-In toggle on registration
    │   └── AppointmentsList.jsx                  <-- UPDATED: WhatsApp dispatch indicator on appointment records
    │
    ├── components\whatsapp\
    │   ├── WhatsAppStatusBadge.jsx               <-- Reusable live session status pill (Connected/Disconnected)
    │   ├── WhatsAppOptInToggle.jsx               <-- Reusable patient profile opt-in switch with disclaimer
    │   └── WhatsAppMessageHistoryDrawer.jsx      <-- Slide-over drawer showing patient's WhatsApp message history
    │
    └── services\
        └── whatsappService.js                    <-- Frontend API client for WhatsApp admin & log endpoints
```

---

## 3. Database Schema Changes ([`WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql`](file:///f:/DentistApp_Theme2/WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql))

### 3.1 Patient Opt-In Columns on `[dentist].[Patients]`
| Column Name | Data Type | Nullable | Default | Description |
| :--- | :--- | :---: | :---: | :--- |
| `WhatsAppOptIn` | `BIT` | NO | `1` | `1` = Patient opted-in to receive WhatsApp updates; `0` = Inactive/Opted-out |
| `WhatsAppOptInAt` | `DATETIME2` | YES | `SYSUTCDATETIME()` | Timestamp when patient consented |
| `WhatsAppLastNotifiedAt` | `DATETIME2` | YES | `NULL` | Timestamp of latest sent notification |

### 3.2 Audit Log & Queue Table: `[dentist].[WhatsAppMessages]`
Stores every queued, sent, failed, or incoming WhatsApp message with full audit trail:
```sql
CREATE TABLE [dentist].[WhatsAppMessages] (
    [MessageID]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientID]          INT NULL FOREIGN KEY REFERENCES [dentist].[Patients]([PatientID]),
    [AppointmentID]      INT NULL FOREIGN KEY REFERENCES [dentist].[Appointments]([AppointmentID]),
    [DoctorID]           INT NULL,
    [RecipientPhone]     NVARCHAR(30) NOT NULL,          -- e.g. "923001234567"
    [ChatID]             NVARCHAR(60) NOT NULL,          -- e.g. "923001234567@c.us"
    [MessageBody]        NVARCHAR(MAX) NOT NULL,
    [MessageType]        NVARCHAR(50) NOT NULL DEFAULT 'AppointmentBooking',
    [Status]             NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Queued', 'Sending', 'Sent', 'Delivered', 'Read', 'Failed', 'Skipped_NoOptIn'
    [OpenWAMessageID]    NVARCHAR(120) NULL,
    [SessionName]        NVARCHAR(100) NOT NULL DEFAULT 'dentia-clinic-bot',
    [RetryCount]         INT NOT NULL DEFAULT 0,
    [MaxRetries]         INT NOT NULL DEFAULT 3,
    [ErrorMessage]       NVARCHAR(MAX) NULL,
    [ScheduledFor]       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [SentAt]             DATETIME2 NULL,
    [DeliveredAt]        DATETIME2 NULL,
    [ReadAt]             DATETIME2 NULL,
    [CreatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

### 3.3 Gateway State Table: `[dentist].[WhatsAppSessions]`
Tracks gateway connection state, QR code base64 string, and battery telemetry:
```sql
CREATE TABLE [dentist].[WhatsAppSessions] (
    [SessionRecordID]      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [SessionName]          NVARCHAR(100) NOT NULL UNIQUE, -- 'dentia-clinic-bot'
    [Status]               NVARCHAR(50) NOT NULL DEFAULT 'DISCONNECTED', -- 'DISCONNECTED', 'SCAN_QR_CODE', 'WORKING', 'FAILED'
    [QRCodeData]           NVARCHAR(MAX) NULL,
    [ConnectedPhoneNumber] NVARCHAR(50) NULL,
    [PushName]             NVARCHAR(100) NULL,
    [IsHealthy]            BIT NOT NULL DEFAULT 0,
    [LastPingAt]           DATETIME2 NULL,
    [LastStatusChangeAt]   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedAt]            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

---

## 4. OpenWA Configuration & Docker Compose

### 4.1 Environment Variables
All configuration is read strictly from environment variables or `appsettings.json`. Secrets are never exposed to the frontend:

```json
{
  "OpenWA": {
    "BaseUrl": "http://localhost:2785",
    "ApiKey": "dentia_prod_secret_key_998877",
    "SessionId": "dentia-clinic-bot",
    "WebhookSecret": "dentia_webhook_hmac_secret_554433",
    "ClinicName": "Dentia Premier Dental Hospital",
    "ClinicAddress": "Suite 402, Medical City, Blue Area, Islamabad",
    "ClinicPhone": "+92 51 2233445",
    "RateLimitMsgsPerMinute": 5,
    "MinDelaySeconds": 2,
    "MaxDelaySeconds": 6
  }
}
```

### 4.2 Docker Compose (`docker-compose.openwa.yml`)
```yaml
version: '3.8'

services:
  openwa-gateway:
    image: rmyndharis/openwa:latest
    container_name: dentia-openwa-gateway
    restart: unless-stopped
    ports:
      - "127.0.0.1:2785:2785" # Bound strictly to localhost; exposed via Nginx/Caddy HTTPS
    environment:
      - PORT=2785
      - API_KEY=dentia_prod_secret_key_998877
      - WEBHOOK_URL=http://host.docker.internal:5000/api/whatsapp/webhook
      - WEBHOOK_SECRET=dentia_webhook_hmac_secret_554433
      - PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable
      - PUPPETEER_ARGS=--no-sandbox,--disable-setuid-sandbox,--disable-dev-shm-usage
    volumes:
      - openwa_sessions:/app/sessions
    logging:
      driver: "json-file"
      options:
        max-size: "20m"
        max-file: "5"

volumes:
  openwa_sessions:
    name: dentia_openwa_sessions
```

> **HTTPS & Reverse Proxy Note:** Never expose port 2785 directly to the public internet. Use an internal Docker network or reverse proxy (Nginx / Caddy / Cloudflare Tunnel) with SSL termination and IP whitelisting.

---

## 5. Core Backend Modules (.NET 8 C#)

### 5.1 Phone Normalization Helper (`WhatsAppPhoneNormalizer.cs`)
Handles all Pakistani phone formats and formats the required OpenWA `chatId`:
```csharp
public static class WhatsAppPhoneNormalizer
{
    public static (bool IsValid, string NormalizedDigits, string ChatId, string FormattedDisplay) NormalizePakistani(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return (false, "", "", "");

        // 1. Remove all spaces, dashes, parentheses, plus signs
        string cleaned = Regex.Replace(rawPhone, @"[^\d]", "");

        // 2. Handle leading zeros: 03001234567 -> 923001234567
        if (cleaned.StartsWith("0") && cleaned.Length == 11)
        {
            cleaned = "92" + cleaned.Substring(1);
        }
        // 3. Handle local without leading zero: 3001234567 -> 923001234567
        else if (cleaned.StartsWith("3") && cleaned.Length == 10)
        {
            cleaned = "92" + cleaned;
        }
        // 4. Handle 0092: 00923001234567 -> 923001234567
        else if (cleaned.StartsWith("0092") && cleaned.Length == 14)
        {
            cleaned = cleaned.Substring(2);
        }

        // 5. Pakistani mobile validation: Must start with 923 and be exactly 12 digits
        bool isValid = cleaned.StartsWith("923") && cleaned.Length == 12;
        if (!isValid)
            return (false, cleaned, "", rawPhone);

        string chatId = $"{cleaned}@c.us";
        string formatted = $"+{cleaned.Substring(0, 2)} {cleaned.Substring(2, 3)} {cleaned.Substring(5)}";

        return (true, cleaned, chatId, formatted);
    }
}
```

### 5.2 WhatsApp Service (`WhatsAppService.cs`)
Wraps OpenWA HTTP endpoints:
- `POST /api/sessions/{sessionId}/messages/send-text`
- `GET /api/sessions/{sessionId}/status`
- `GET /api/sessions/{sessionId}/qr`
- `POST /api/sessions/{sessionId}/start`

Includes appointment message dispatch logic:
```csharp
public async Task<int> QueueAppointmentConfirmationAsync(Appointment appointment, Patient patient, Doctor doctor)
{
    // Strict Opt-in Check (Principle: Never send to non-opted-in patients)
    if (patient == null || !patient.WhatsAppOptIn)
    {
        _logger.LogInformation("WhatsApp notification skipped for patient {PatientId}: WhatsAppOptIn is false.", patient?.PatientID);
        return 0;
    }

    var norm = WhatsAppPhoneNormalizer.NormalizePakistani(patient.Phone);
    if (!norm.IsValid)
    {
        _logger.LogWarning("WhatsApp notification skipped for patient {PatientId}: Invalid cell number '{Phone}'.", patient.PatientID, patient.Phone);
        return 0;
    }

    // Build professional appointment template
    string messageText = BuildAppointmentConfirmationTemplate(appointment, patient, doctor);

    // Insert into [dentist].[WhatsAppMessages] queue table with random 2-6s jitter
    var randomJitterSeconds = Random.Shared.Next(2, 7);
    var scheduledTime = DateTime.UtcNow.AddSeconds(randomJitterSeconds);

    return await InsertQueuedMessageAsync(new WhatsAppMessageEntity
    {
        PatientID = patient.PatientID,
        AppointmentID = appointment.AppointmentID,
        DoctorID = appointment.DoctorID,
        RecipientPhone = norm.NormalizedDigits,
        ChatID = norm.ChatId,
        MessageBody = messageText,
        MessageType = "AppointmentBooking",
        Status = "Pending",
        ScheduledFor = scheduledTime
    });
}
```

### 5.3 Background Queue Worker (`WhatsAppBackgroundQueueService.cs`)
An ASP.NET Core `BackgroundService` that runs continuously:
1. Polls `[dentist].[WhatsAppMessages]` where `Status IN ('Pending', 'Retrying') AND ScheduledFor <= SYSUTCDATETIME()`.
2. Enforces **strict rate-limiting**: Maximum **5 messages per minute** per session.
3. Inserts a **random jitter delay of 2 to 6 seconds** between successive sends to simulate natural human activity and prevent WhatsApp automated detection bans.
4. Executes HTTP POST to OpenWA `send-text` endpoint with header `X-API-Key`.
5. On success, updates `Status = 'Sent'`, records `SentAt = GETUTCDATETIME()`, and stores `OpenWAMessageID`.
6. On failure, calculates exponential backoff (`DateTime.UtcNow.AddMinutes(Math.Pow(2, RetryCount))`) and increments `RetryCount`. If `RetryCount >= MaxRetries`, sets `Status = 'Failed'`.
7. **Session Health Check & Automatic Fallback**: If OpenWA is disconnected, automatically sends an email via `_emailService.SendEmailAsync()` for critical booking notifications.

### 5.4 Webhook Handler (`WhatsAppWebhookController.cs`)
Endpoint: `POST /api/whatsapp/webhook`
- Validates incoming `X-Hub-Signature` or `X-OpenWA-Signature` using `HMAC-SHA256` with `OPENWA_WEBHOOK_SECRET`.
- Handles events:
  - `message.received`: If patient replies **"STOP"**, automatically sets `WhatsAppOptIn = 0` on `[dentist].[Patients]`.
  - `session.status`: Updates `[dentist].[WhatsAppSessions]` with `WORKING`, `SCAN_QR_CODE`, or `DISCONNECTED`.
  - `message.status`: Updates message records to `Delivered` or `Read`.

---

## 6. Patient Profile Opt-In & Appointment Workflow

### 6.1 Patient Registration & Edit Profile
- In **[`NewPatientPage.jsx`](file:///f:/DentistApp_Theme2/Dentistfrontend/src/pages/NewPatientPage.jsx)** and the Patient Edit Modal in **[`PatientDirectory.jsx`](file:///f:/DentistApp_Theme2/Dentistfrontend/src/pages/PatientDirectory.jsx)**, a dedicated WhatsApp consent switch is displayed:
  ```
  [X] Receive Appointment & Clinical Updates via WhatsApp (+92 300 1234567)
  ```
- Defaults to checked (`true`) upon entering a valid Pakistani cell phone.
- Consent timestamp `WhatsAppOptInAt` is recorded in the database.

### 6.2 Appointment Booking Trigger
Whenever an appointment is set for a specific patient:
1. **Trigger Points**:
   - Receptionist schedules via `AppointmentsList.jsx` or `PatientDirectory.jsx`.
   - Patient books via self-service portal (`PatientBookAppointment.jsx`).
   - Doctor schedules hands-free via Jarvis Voice Copilot (`POST /api/appointments/book-voice`).
2. **Evaluation Logic**:
   - Backend checks if `patient.WhatsAppOptIn == true`.
   - Normalizes cell number to Pakistani format (`923XXXXXXXXX`).
   - If active, automatically queues the customized confirmation message.
3. **Dispatch Template**:
   ```
   🦷 *DENTIA DENTAL HOSPITAL*
   *Appointment Confirmation*
   ━━━━━━━━━━━━━━━━━━━━━
   Dear *Mr. Ali Khan*,

   Your dental appointment has been scheduled successfully:

   📅 *Date:* Tuesday, October 21, 2026
   ⏰ *Time:* 03:30 PM
   👨‍⚕️ *Doctor:* Dr. Jhangir Ahmed (Oral Surgeon)
   📋 *Reason/Procedure:* Comprehensive Examination & Scaling
   📍 *Location:* Suite 402, Medical City, Blue Area, Islamabad

   *Patient Guidelines:*
   • Please arrive 10 minutes prior to your allocated slot.
   • Bring any recent dental radiographs or prescriptions.

   To reschedule or if you have queries, please call: +92 51 2233445.
   Reply *STOP* to opt-out of WhatsApp notifications.
   ━━━━━━━━━━━━━━━━━━━━━
   _Dentia Smart Healthcare System_
   ```

---

## 7. Frontend UI / UX Specification

### 7.1 Patient Directory (`PatientDirectory.jsx`)
In the Patient Directory table and detailed dossier:
- **WhatsApp Active / Inactive Badge Pill**:
  - If `patient.whatsAppOptIn && patient.phone`:
    - Renders a vibrant **Green WhatsApp Pill**:
      ```jsx
      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200 flex items-center gap-1">
        <MessageCircle className="w-3 h-3 text-emerald-600" />
        WhatsApp Active
      </span>
      ```
    - Clicking the pill opens a quick-chat link directly to `https://wa.me/923XXXXXXXXX` in a new tab.
  - If `!patient.whatsAppOptIn`:
    - Renders a muted **Gray Pill**:
      ```jsx
      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-100 text-slate-500 border border-slate-200 flex items-center gap-1">
        <MessageCircleOff className="w-3 h-3 text-slate-400" />
        WhatsApp Inactive
      </span>
      ```
    - Doctor can toggle opt-in state on/off with 1 click from the Patient Edit Modal.

### 7.2 WhatsApp Admin Page (`WhatsAppAdminPage.jsx`)
A dedicated admin view accessible under clinic settings:
1. **Gateway Status Card**:
   - Displays real-time session state: `WORKING` (Connected), `SCAN_QR_CODE` (Action Required), or `DISCONNECTED`.
   - Connected phone number, push name, and battery telemetry.
2. **Interactive QR Code Display**:
   - When a session requires linking, renders the QR code fetched from `GET /api/whatsapp/qr`.
   - Polls every 3 seconds until successfully linked, transitioning to a green verified badge.
3. **Queue & Message Audit Table**:
   - Live table of recent outgoing messages with filters (`All`, `Pending`, `Sent`, `Delivered`, `Failed`).
   - Columns: Message ID, Recipient, Message Type, Status badge, Timestamp, and Error details.
4. **Test Dispatch Tool**:
   - Allows clinic manager to send a test message to their own number to verify gateway connectivity.

---

## 8. Anti-Ban Safety Rules & Production Guidelines

OpenWA interacts with WhatsApp Web. To strictly prevent WhatsApp number bans:

1. **Use a Dedicated Clinic SIM**: Never link personal doctor numbers. Register a dedicated clinic reception number.
2. **Account Warm-Up Period**:
   - **Week 1**: Maximum 15–20 messages per day to known numbers.
   - **Week 2**: Up to 50 messages per day.
   - **Week 3+**: Regular clinic volume (100–200 messages/day).
3. **Strict Rate Limiting**:
   - Maximum **5 messages per minute**.
   - Enforce **2 to 6 seconds random jitter delay** between messages.
4. **100% Opt-In Only**:
   - Never send unsolicited bulk broadcast messages.
   - Always honor the `STOP` opt-out reply immediately.
5. **Session Data Persistence**:
   - Ensure the Docker volume `openwa_sessions:/app/sessions` is mounted to persistent host storage so browser tokens are never lost on container reboot.

---

## 9. Verification & Acceptance Checklist

- [ ] Execute `WHATSAPP_OPENWA_INTEGRATION_MIGRATION.sql` on SQL Server (`DentistAPI`).
- [ ] Confirm columns `WhatsAppOptIn`, `WhatsAppOptInAt`, and `WhatsAppLastNotifiedAt` exist in `[dentist].[Patients]`.
- [ ] Confirm tables `[dentist].[WhatsAppMessages]` and `[dentist].[WhatsAppSessions]` are created.
- [ ] Verify `docker-compose.openwa.yml` runs and reaches OpenWA Swagger at `http://localhost:2785/api/docs`.
- [ ] Verify patient directory displays **WhatsApp Active** / **WhatsApp Inactive** badges.
- [ ] Verify booking an appointment for an opted-in patient queues a confirmation WhatsApp message.
- [ ] Verify rate limiter delays sends with 2–6s random jitter.
- [ ] Verify disconnect auto-fallback to email.
