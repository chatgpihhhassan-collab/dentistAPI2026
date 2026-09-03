# 📸 Patient Profile Image Upload & SQL Byte Storage Specification

This document details the architectural design, SQL schema modifications, backend API endpoints, and the specific frontend forms/components where the **Patient Profile Image** upload and display features are applied.

---

## 🗄️ 1. SQL Database Schema Changes

### Target Table: `[dentist].[Patients]`

| Column Name | Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| `ProfileImage` | `VARBINARY(MAX)` | **YES (Optional)** | `NULL` | Raw binary byte storage of patient photo (max 5MB) |
| `ProfileImageMimeType` | `VARCHAR(50)` | **YES (Optional)** | `NULL` | MIME type format (e.g. `image/jpeg`, `image/png`, `image/webp`) |

### SQL Migration Script
```sql
-- ==========================================================
-- Migration Script: Add ProfileImage & ProfileImageMimeType
-- Schema: dentist | Table: Patients
-- ==========================================================
USE [DentistDB];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = 'ProfileImage'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [ProfileImage] VARBINARY(MAX) NULL;
    PRINT 'Added column [ProfileImage] to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT 'Column [ProfileImage] already exists.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = 'ProfileImageMimeType'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [ProfileImageMimeType] VARCHAR(50) NULL;
    PRINT 'Added column [ProfileImageMimeType] to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT 'Column [ProfileImageMimeType] already exists.';
END
GO
```

---

## 🖥️ 2. Forms and Components Where Changes Are Applied

| # | Form / Component | File Path | Scope of Changes |
|---|---|---|---|
| **1** | **New Patient Registration Form** | `src/pages/NewPatientPage.jsx` | • **Interactive Photo Upload Box**: Click-to-browse or drag-and-drop avatar upload.<br>• **5MB Client-Side Validation**: Instant feedback/toast warning if selected image exceeds 5MB.<br>• **Live Dynamic Gender Fallback Preview**: If no image is uploaded, dynamically shows Male, Female, or Neutral dummy vector avatar as the doctor selects or AI extracts the gender.<br>• **Remove & Replace Actions**: Easy one-click reset to default avatar.<br>• **AI Assistant Chat Sync**: AI assistant confirms avatar status during conversational intake. |
| **2** | **Patient Directory & Edit Modal** | `src/pages/PatientDirectory.jsx` | • **Patient List Cards**: Renders patient's custom photo (or gender dummy avatar) next to patient name and ID.<br>• **Selected Patient Consultation Header**: Renders high-resolution profile photo in active consultation card.<br>• **Quick View Drawer**: Displays patient avatar in slide-out details.<br>• **Edit Patient Modal**: Enables doctor to update or replace existing profile image (with 5MB max check). |
| **3** | **Dental Chart & Odontogram Header** | `src/pages/ChartPage.jsx` | • Patient profile banner at top of chart displays uploaded photo or fallback gender avatar. |
| **4** | **Appointments List & Drawer** | `src/pages/AppointmentsList.jsx` | • Patient list items & schedule detail drawer display patient profile avatars. |
| **5** | **Book Appointment Modal** | `src/pages/BookAppointment.jsx` | • Patient search list items display patient profile avatars. |
| **6** | **Landing Dashboard** | `src/pages/LandingDashboard.jsx` | • Recent patient activity displays patient profile avatars. |
| **7** | **Avatar Utilities & Default SVGs** | `src/utils/avatarUtils.js` | • Utility functions for binary/base64 conversions, fallback logic, and premium vector SVGs for Male, Female, and Neutral defaults. |

---

## ⚙️ 3. Backend Architecture & Business Rules

1. **Size Limit Constraint**:
   - Strictly validates `imageBytes.Length <= 5 * 1024 * 1024` (5MB). Returns HTTP `400 Bad Request` if exceeded.
2. **Binary Byte Storage in SQL Server**:
   - Saved directly as `byte[]` into the `ProfileImage` (`VARBINARY(MAX)`) column with its MIME type in `ProfileImageMimeType`.
3. **Default Gender Fallback by AI Assistant / Backend**:
   - If the doctor or AI intake registers a patient without a custom image:
     - Gender `Male` ➔ Assigned default Male vector avatar bytes.
     - Gender `Female` ➔ Assigned default Female vector avatar bytes.
     - Gender `Other` or Unspecified ➔ Assigned default Neutral vector avatar bytes.
4. **Endpoints**:
   - `POST /api/patients` (supports inline `profileImageBase64` / byte array)
   - `POST /api/patients/{id}/profile-image` (supports `multipart/form-data` file upload up to 5MB)
   - `GET /api/patients/{id}/profile-image` (serves the binary image file directly with proper Content-Type)
