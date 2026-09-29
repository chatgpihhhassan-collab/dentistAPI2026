# 🩺 Doctor Profile, Organization Experience & Superadmin Management Specification

## 📌 Executive Summary
This document specifies the end-to-end architecture, database schema, security model, and user workflows for **Doctor Profile & Career History Discovery** within the **DENTIA Patient Portal** and the **Superadmin Management Workspace**.

Patients can now research and review detailed doctor biographies, years of clinical experience, previous and current hospital/clinic affiliations ("what his work in what organization"), educational degrees, and certifications before choosing their attending dental surgeon for appointment bookings.

Only authenticated **Superadmins** have administrative privileges to add new doctors, modify clinical bios, adjust consultation fees, edit career timelines, and toggle active practitioner status.

---

## 🗄️ 1. Relational Database Schema (`[dentist].[Doctors]`)

The core `[dentist].[Doctors]` table in SQL Server 2022 has been enhanced with dedicated clinical career and profile fields:

| Column Name | Data Type | Nullable | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `DoctorID` | `INT` (PK, IDENTITY) | NO | - | Unique Doctor Identifier |
| `Username` | `NVARCHAR(50)` | NO | - | Clinician login username |
| `PasswordHash` | `NVARCHAR(255)` | NO | - | BCrypt hashed password |
| `FirstName` | `NVARCHAR(50)` | NO | - | Clinician first name |
| `LastName` | `NVARCHAR(50)` | NO | - | Clinician last name |
| `Region` | `NVARCHAR(10)` | YES | `'NZ'` | Practice region (`NZ`, `PK`, `US`, `UK`) |
| `IsSuperAdmin` | `BIT` | YES | `0` | Superadmin privilege flag |
| `Specialization` | `NVARCHAR(150)` | YES | `'General Dental Surgeon'` | Primary clinical discipline (e.g. *Orthodontist & Clear Aligners*) |
| `Title` | `NVARCHAR(100)` | YES | `'BDS, RDS'` | Professional degrees/titles (e.g. *BDS, MDS, FICOI*) |
| `YearsOfExperience`| `INT` | YES | `5` | Total years of active dental practice |
| `Biography` | `NVARCHAR(MAX)` | YES | `NULL` | Detailed clinical history and professional narrative |
| `OrganizationWorkHistory` | `NVARCHAR(MAX)` | YES | `NULL` | Structured JSON containing past & current organizations, roles, periods |
| `Education` | `NVARCHAR(MAX)` | YES | `NULL` | Degrees, universities attended, graduation years |
| `Certifications` | `NVARCHAR(MAX)` | YES | `NULL` | Specialty fellowships, board certifications, vendor credentials |
| `ConsultationFee`| `DECIMAL(18,2)` | YES | `100.00` | Standard consultation rate in local currency |
| `ProfileImageUrl`| `NVARCHAR(500)` | YES | `NULL` | Secure avatar/headshot image URL |
| `Languages` | `NVARCHAR(200)` | YES | `'English, Urdu'` | Languages spoken fluently |
| `Rating` | `DECIMAL(3,2)` | YES | `4.90` | Aggregate patient satisfaction rating (1.00 - 5.00) |
| `ReviewCount` | `INT` | YES | `25` | Total verified patient reviews |
| `IsActive` | `BIT` | YES | `1` | Active clinician directory flag |

### Example `OrganizationWorkHistory` JSON Schema:
```json
[
  {
    "organization": "Shifa International Hospital",
    "role": "Head of Oral & Maxillofacial Surgery",
    "period": "2018 - Present",
    "description": "Supervising surgical theater for advanced ridge augmentation, zygomatic implants, and trauma reconstructions."
  },
  {
    "organization": "Auckland Regional Hospital Dental Wing",
    "role": "Senior Clinical Registrar",
    "period": "2014 - 2018",
    "description": "Specialized in surgical extractions, bone grafting, and biopsy pathology assessment."
  },
  {
    "organization": "Mayo Hospital / King Edward Medical University",
    "role": "Resident Surgeon",
    "period": "2010 - 2014",
    "description": "Completed 4-year intensive clinical residency in maxillofacial trauma and oral pathology."
  }
]
```

---

## ⚙️ 2. RESTful Backend Endpoints

### A. Patient Portal Endpoints (`PatientPortalController.cs`)
- **`GET /api/patient-portal/doctors`**
  - **Access:** Public or Authenticated Patient
  - **Description:** Returns all active clinic doctors with specialization, years of experience, rating, fee, avatar, and organization preview.
- **`GET /api/patient-portal/doctors/{id}`**
  - **Access:** Public or Authenticated Patient
  - **Description:** Returns comprehensive single doctor profile including full career biography, organization timeline, education, and certifications.

### B. Superadmin Management Endpoints (`AuthController.cs`)
- **`GET /api/auth/doctors`**
  - **Access:** Superadmin sees all doctors (Active + Inactive); regular users see Active doctors only.
- **`POST /api/auth/doctors`**
  - **Access:** **Superadmin ONLY** (Enforces `IsCallerSuperAdmin()` token validation).
  - **Description:** Registers a new doctor with login credentials, clinical specialization, experience, and organization history.
- **`PUT /api/auth/doctors/{id}`**
  - **Access:** **Superadmin ONLY** (HTTP 403 Forbidden for non-superadmins).
  - **Description:** Modifies existing doctor profile fields (specialization, bio, years of experience, organizations list, consultation fee, image URL).
- **`PUT /api/auth/doctors/{id}/status`**
  - **Access:** **Superadmin ONLY**.
  - **Description:** Toggles active practitioner status (`IsActive = true/false`).
- **`PUT /api/auth/doctors/{id}/password`**
  - **Access:** Superadmin or Attending Doctor.
  - **Description:** Updates doctor login password with BCrypt hash.

---

## 📱 3. Patient Portal Experience (`/portal/doctors`)

1. **Specialist Discovery Grid**:
   - Patients can browse all doctors with photo headshots, verified experience badges, and clinical specialties.
   - Filter chips for quick categorization:
     - `All Specialists`
     - `Orthodontics & Clear Aligners`
     - `Implantology & Oral Surgery`
     - `Cosmetic & Restorative`
     - `Endodontics (Root Canal)`
     - `Pediatric Dentistry`
   - Real-time search by doctor name, clinical specialty, or organization name.

2. **Detailed Doctor Career & History Modal**:
   - Clicking **"Read Full Profile & History"** opens an interactive profile dialog featuring:
     - **Professional Biography**: Philosophy of care, years in practice, and background.
     - **Interactive Career Timeline**: Visual sequence of past and current hospital/clinic affiliations with dates and key accomplishments.
     - **Academic Qualifications**: Degrees earned, medical universities, and fellowship certifications.
     - **Consultation Rate & Languages**: Clear pricing transparency and communication accessibility.

3. **Direct 1-Click Booking Integration**:
   - Clicking **"Select & Book Visit with Dr. [Name]"** navigates seamlessly to:
     ```text
     /portal/book?doctor={doctorId}
     ```
   - `PatientBookAppointment.jsx` automatically locks in the selected doctor, pulls their personalized procedure pricing schedules, and presents their available consultation calendar!

---

## 🛡️ 4. Superadmin Management Workspace (`/admin/doctors`)

1. **Route Protection**:
   - Protected in `App.jsx` via `<AdminRoute>` which validates `doctor.isSuperAdmin === true`. Non-superadmins attempting access are instantly redirected to `/directory` or `/`.
2. **Key Capabilities for Superadmin**:
   - **Overview KPI Cards**: Total Registered Doctors, Active Practitioners, Combined Clinical Experience, and Hospital Affiliations.
   - **Dynamic Organization History Builder**:
     - Superadmin can add multiple past or current organizations using an interactive form (Organization Name, Role/Designation, Period, and Clinical Responsibilities).
   - **Profile Photo Customizer**: Paste image URLs with instant live preview or choose from pre-curated specialist avatars.
   - **Quick Actions**: 1-click password reset, status toggle (Active/Suspended), and profile updates.

---

## 📁 5. Related Files & Artifacts

- **Database Migration:** [`DOCTOR_PROFILES_AND_PATIENT_PORTAL_EXPANSION.sql`](file:///f:/DentistApp_Theme2/DOCTOR_PROFILES_AND_PATIENT_PORTAL_EXPANSION.sql)
- **Backend Model:** [`AuthModels.cs`](file:///f:/DentistApp_Theme2/DentistAPI/Models/AuthModels.cs)
- **Backend Repositories:** [`DentalRepository.cs`](file:///f:/DentistApp_Theme2/DentistAPI/Repositories/DentalRepository.cs)
- **Backend Controllers:** [`AuthController.cs`](file:///f:/DentistApp_Theme2/DentistAPI/Controllers/AuthController.cs), [`PatientPortalController.cs`](file:///f:/DentistApp_Theme2/DentistAPI/Controllers/PatientPortalController.cs)
- **Frontend Patient View:** `src/modules/patientPortal/pages/PatientDoctors.jsx`
- **Frontend Admin View:** `src/pages/DoctorManagement.jsx`
- **Frontend Layout:** `src/modules/patientPortal/layouts/PatientPortalLayout.jsx`
- **Frontend Booking Flow:** `src/modules/patientPortal/pages/PatientBookAppointment.jsx`
