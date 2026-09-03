# 📅 Notion Appointment Integration Guide for DENTIA

This document outlines how **Notion Appointment Integration & Notion-Style Scheduling** works, how it can be implemented in the **DENTIA Clinical Suite**, and how **Doctors** and **Patients** can view, schedule, and sync their appointments in real time.

---

## 📌 Executive Summary

### 1. What is "Notion Appointment"?
In modern clinical workflows, **Notion Appointment** represents a dual capability:
1. **Notion 2-Way Cloud Sync**: Real-time synchronization between DENTIA's SQL Server database (`[dentist].[Appointments]`) and a **Notion Database / Notion Calendar**, allowing receptionists, practice managers, and doctors to manage consultations in Notion while keeping the clinic app perfectly updated.
2. **Notion-Style Interactive UI in DENTIA**: A rich, modern scheduling interface inside the DENTIA web app featuring **Multi-View Modes** (Interactive Month/Week Calendar, Kanban Status Board, Timeline Schedule, and Filterable List).

---

## 👥 How Doctors and Patients View Their Appointments

```mermaid
flowchart TD
    subgraph Doctor_Experience["🩺 Doctor & Clinic Staff"]
        D1["DENTIA Directory / Schedule Dashboard"] --> D2["Notion-Style Multi-View (Calendar / Kanban)"]
        D1 --> D3["AI Voice Scribe Auto-Booking"]
        D4["Notion Workspace / Notion Calendar App"] --> D5["Real-Time 2-Way Sync"]
    end

    subgraph Core_Engine["⚙️ DENTIA Backend & Database"]
        DB["SQL Server: [dentist].[Appointments]"] <--> API["DentistAPI (.NET Core)"]
        API <--> N_SYNC["Notion Sync Worker / Webhooks"]
    end

    subgraph Patient_Experience["📱 Patient Experience"]
        P1["Online Booking Portal / Mobile Form"] --> API
        P2["Email & SMS Confirmation with Calendar (.ics)"] <-- API
        P3["Patient Portal / Self-Service View"] <-- API
    end

    D2 <--> DB
    D5 <--> N_SYNC
    N_SYNC <--> DB
```

### A. How Doctors See and Manage Appointments:
1. **DENTIA Web Application (`/directory` & `/appointments`)**:
   - **Kanban Board**: Drag-and-drop appointments between `Pending` ➔ `Confirmed` ➔ `In Consultation` ➔ `Completed` ➔ `Cancelled`.
   - **Daily Timeline**: View appointments segmented by Doctor, Operatory Chair, and Time Slot (9:00 AM - 6:00 PM).
   - **1-Click Odontogram Link**: Clicking any appointment instantly loads the patient's 32-tooth chart, X-rays, and AI clinical notes.
   - **AI Voice-Dictated Bookings**: When a doctor says *"Follow up in 2 weeks for RCT"*, the AI automatically inserts the record into both SQL Server and Notion.
2. **Notion Mobile & Desktop App**:
   - Doctors can view their daily appointments inside their Notion workspace or on **Notion Calendar** synced with Google Calendar / Outlook.
   - Changes made in Notion (e.g. rescheduling or adding a consultation note) reflect immediately in DENTIA.

### B. How Patients See Their Appointments:
1. **Self-Service Booking Widget (`/book-appointment`)**:
   - Patients choose their Doctor, Preferred Date/Time Slot, and Reason for visit (e.g. *Root Canal, Whitening, Orthodontics*).
2. **Instant Email & SMS Confirmations**:
   - Automated confirmation email containing appointment summary, clinic location, and an **Add to Google / Apple Calendar (`.ics`)** link.
3. **Patient Self-Service Status Page**:
   - Patients can view their upcoming booking status (`Confirmed` / `Pending`) and request a reschedule.

---

## 🏗️ Architecture & Data Mapping

### 1. Database Mapping (`[dentist].[Appointments]` ⟷ Notion Database)

| SQL Server Column (`[dentist].[Appointments]`) | Notion Database Property | Notion Property Type | Example Value |
| :--- | :--- | :--- | :--- |
| `AppointmentID` | `Appointment ID` | `Number` | `104` |
| `FullName` | `Patient Name` | `Title` | `"Arslan Khan"` |
| `Phone` | `Contact Phone` | `Phone Number` | `"+92 300 1234567"` |
| `Email` | `Email Address` | `Email` | `"arslan.khan@dentiaclinic.com"` |
| `PreferredDate` | `Appointment Time` | `Date` (with Time) | `2026-09-02T18:00:00Z` |
| `Status` | `Status` | `Select` / `Status` | `Confirmed`, `Pending`, `Completed` |
| `Reason` | `Clinical Reason / Modality` | `Rich Text` | `"Follow-up: Root Canal Treatment"` |
| `DoctorID` | `Attending Dentist` | `Select` | `"Dr. Jhangir Ahmed"` |
| `CreatedAt` | `Booked Date` | `Created Time` | `2026-08-26 15:30:00` |
| *New Column* `NotionPageId` | `Notion Page ID` | `Text` | `"a1b2c3d4-e5f6-7890..."` |

---

## 🚀 Step-by-Step Implementation Guide

### Step 1: Set Up Notion API Integration
1. Go to **[Notion Developers Portal](https://www.notion.so/my-integrations)** and click **"+ New integration"**.
2. Name it **"DENTIA Appointment Sync"** and copy the **Internal Integration Secret Token** (`secret_...`).
3. In your Notion workspace, create a new Database called **"DENTIA Appointments"** with the properties mapped above.
4. Click the `...` menu in Notion ➔ **Connections** ➔ Add **"DENTIA Appointment Sync"**.
5. Copy the **Database ID** from the Notion URL (`https://notion.so/workspace/{DATABASE_ID}?v=...`).

---

### Step 2: Configure `appsettings.json` in Backend
Add the Notion configuration section to [`f:\DentistApp_Theme2\DentistAPI\appsettings.json`](file:///f:/DentistApp_Theme2/DentistAPI/appsettings.json):

```json
{
  "NotionSettings": {
    "EnableNotionSync": true,
    "ApiToken": "secret_YOUR_NOTION_INTERNAL_INTEGRATION_TOKEN",
    "DatabaseId": "YOUR_NOTION_DATABASE_ID",
    "ApiVersion": "2022-06-28"
  }
}
```

---

### Step 3: Implement Backend `NotionAppointmentService.cs`

Create a dedicated service in `DentistAPI/Services/NotionAppointmentService.cs` to handle REST API calls to Notion:

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DentistAPI.Models;

namespace DentistAPI.Services
{
    public interface INotionAppointmentService
    {
        Task<string?> CreateNotionAppointmentAsync(Appointment appointment, string doctorName);
        Task<bool> UpdateNotionAppointmentStatusAsync(string notionPageId, string status, string? reason);
        Task SyncFromNotionAsync();
    }

    public class NotionAppointmentService : INotionAppointmentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiToken;
        private readonly string _databaseId;
        private readonly bool _enabled;
        private readonly ILogger<NotionAppointmentService> _logger;

        public NotionAppointmentService(HttpClient httpClient, IConfiguration config, ILogger<NotionAppointmentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _enabled = config.GetValue<bool>("NotionSettings:EnableNotionSync", false);
            _apiToken = config["NotionSettings:ApiToken"] ?? "";
            _databaseId = config["NotionSettings:DatabaseId"] ?? "";
        }

        public async Task<string?> CreateNotionAppointmentAsync(Appointment appointment, string doctorName)
        {
            if (!_enabled || string.IsNullOrEmpty(_apiToken) || string.IsNullOrEmpty(_databaseId))
                return null;

            try
            {
                var payload = new
                {
                    parent = new { database_id = _databaseId },
                    properties = new
                    {
                        Name = new { title = new[] { new { text = new { content = appointment.FullName } } } },
                        AppointmentID = new { number = appointment.AppointmentID },
                        Phone = new { phone_number = appointment.Phone ?? "" },
                        Email = new { email = appointment.Email ?? "" },
                        AppointmentTime = new { date = new { start = appointment.PreferredDate.ToString("yyyy-MM-ddTHH:mm:ssZ") } },
                        Status = new { select = new { name = appointment.Status ?? "Pending" } },
                        Reason = new { rich_text = new[] { new { text = new { content = appointment.Reason ?? "Clinical Consultation" } } } },
                        Dentist = new { select = new { name = doctorName } }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.notion.com/v1/pages");
                request.Headers.Add("Authorization", $"Bearer {_apiToken}");
                request.Headers.Add("Notion-Version", "2022-06-28");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var resJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resJson);
                    var pageId = doc.RootElement.GetProperty("id").GetString();
                    _logger.LogInformation($"[NOTION SYNC] Created Notion page {pageId} for Appointment #{appointment.AppointmentID}");
                    return pageId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NOTION SYNC ERROR] Failed to sync to Notion: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> UpdateNotionAppointmentStatusAsync(string notionPageId, string status, string? reason)
        {
            if (!_enabled || string.IsNullOrEmpty(notionPageId)) return false;

            try
            {
                var payload = new
                {
                    properties = new
                    {
                        Status = new { select = new { name = status } }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.notion.com/v1/pages/{notionPageId}");
                request.Headers.Add("Authorization", $"Bearer {_apiToken}");
                request.Headers.Add("Notion-Version", "2022-06-28");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NOTION UPDATE ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task SyncFromNotionAsync()
        {
            // Periodic polling or webhook handler to pull changes from Notion into SQL Server
        }
    }
}
```

---

### Step 4: Add Notion-Style Multi-View Appointment Board in React Frontend

Inside DENTIA's Frontend ([`src/pages/AppointmentsPage.jsx`](file:///f:/DentistApp_Theme2/src/pages/AppointmentsPage.jsx) / [`src/components/NotionAppointmentBoard.jsx`](file:///f:/DentistApp_Theme2/src/components/)):

1. **View Switcher Tabs**:
   - 📅 **Calendar View** (Month / Week grid with color-coded appointment pills).
   - 📋 **Kanban Board** (Columns: `Pending`, `Confirmed`, `In Progress`, `Completed`).
   - ⏳ **Timeline / Agenda View** (Hour-by-hour operatory schedule).
   - 📑 **Notion Table View** (Inline editable rows with filter, search, and sorting).

2. **Interactive Capabilities**:
   - Drag appointments between dates/times to reschedule.
   - Filter by Attending Doctor (`Dr. Jhangir Ahmed`, `Dr. Sarah`), Region (`PK`, `NZ`), or Treatment Plan (`Orthodontics`, `RCT`, `Whitening`).
   - **Export to PDF & Notion Workspace** button.

---

## 🌟 Key Benefits of Notion Integration for DENTIA

1. **Zero Data Silos**: Clinic receptionists using Notion and dentists using DENTIA's chart view stay synchronized in real time.
2. **Mobile Freedom**: Doctors can check their daily patient appointments on the Notion iOS/Android app on the go.
3. **No Double Bookings**: Active collision checks prevent overlapping appointments across both systems.
4. **Automated AI Bridge**: Ambient voice scribing automatically books and categorizes appointments into the Notion workspace.
