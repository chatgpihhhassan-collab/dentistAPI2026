# 🏥 DENTIA Patient Portal — Comprehensive Architecture, UI/UX Specification & Database Blueprint

> **Executive Overview**  
> This specification details the end-to-end architecture, role separation, UI/UX design system, database schema additions, and API blueprints to introduce a modern, high-trust **Patient Portal** for the **Dentia Dental Clinic** ecosystem.  
> The system enables patients to self-register or log in via a clinic-issued **Patient Reference Number (e.g. `DEN-2026-0035`)**, manage appointments, review clinical reports & radiographs, and settle treatment balances through **both Online Payment (Card / Digital Gateway)** and **Cash at Clinic (with front-desk verification vouchers)**.

---

## 📑 Table of Contents
1. [Architectural Separation: Clinician Workspace vs. Patient Portal](#1-architectural-separation-clinician-workspace-vs-patient-portal)
2. [Patient Onboarding & Login Mechanics](#2-patient-onboarding--login-mechanics)
3. [Patient Portal Core Functional Modules](#3-patient-portal-core-functional-modules)
   - [3.1 Patient Command Dashboard](#31-patient-command-dashboard)
   - [3.2 Smart Appointment Booking & Calendar](#32-smart-appointment-booking--calendar)
   - [3.3 Clinical Reports, Prescriptions & Diagnostics](#33-clinical-reports-prescriptions--diagnostics)
   - [3.4 Billing, Invoices & Dual Payment Gateway (Online + Cash)](#34-billing-invoices--dual-payment-gateway-online--cash)
4. [UI/UX Design System & Aesthetic Principles](#4-uiux-design-system--aesthetic-principles)
5. [Database Delta & Schema Impact Analysis](#5-database-delta--schema-impact-analysis)
6. [Full-Stack Implementation Blueprint (Backend & Frontend)](#6-full-stack-implementation-blueprint-backend--frontend)
7. [Security, HIPAA/GDPR Compliance & Token Management](#7-security-hipaagdpr-compliance--token-management)

---

## 1. Architectural Separation: Clinician Workspace vs. Patient Portal

To prevent clinical cross-contamination, privilege escalation, and UI friction, the system is architected as **two logically distinct surfaces sharing a unified, secure database and API core**.

```
                           ┌─────────────────────────────────────────────────┐
                           │               Dentia Web Platform               │
                           └───────────────────────┬─────────────────────────┘
                                                   │
                ┌──────────────────────────────────┴──────────────────────────────────┐
                ▼                                                                     ▼
┌──────────────────────────────────────┐                             ┌──────────────────────────────────────┐
│       Clinician Workspace            │                             │       Patient Self-Service Portal    │
│       (Doctor / Admin Section)       │                             │       (Patient Experience Section)   │
├──────────────────────────────────────┤                             ├──────────────────────────────────────┤
│ • URL Base: `/dashboard`, `/chart`   │                             │ • URL Base: `/portal/*`              │
│ • Audience: Dentists, Hygienists, Admin│                           │ • Audience: Registered Dental Patients│
│ • State Key: `localStorage['doctor']`│                             │ • State Key: `localStorage['patient']│
│ • Auth Guard: `<DoctorProtectedRoute>`│                            │ • Auth Guard: `<PatientProtectedRoute>│
│ • Persona: High Information Density  │                             │ • Persona: High Empathy, Accessible  │
│ • Features: 3D Odontograms, Voice    │                             │ • Features: Appointments, Reports,   │
│   Ambient Scribe, Multi-Surface Tooth│                             │   Prescriptions, Online Card Pay,    │
│   Pathology, Clinical SOAP Editing   │                             │   Cash Payment Vouchers              │
└──────────────────┬───────────────────┘                             └──────────────────┬───────────────────┘
                   │                                                                    │
                   │  JWT with Claim:                                                   │  JWT with Claim:
                   │  `Role: "Doctor"|"SuperAdmin"`                                     │  `Role: "Patient"`
                   │  `DoctorID: 1`                                                     │  `PatientID: 35`
                   │                                                                    │  `RefNo: "DEN-2026-00035"`
                   └──────────────────────────────────┬─────────────────────────────────┘
                                                      ▼
                                       ┌─────────────────────────────┐
                                       │   ASP.NET Core Web API      │
                                       │   (DentistAPI Controller)   │
                                       ├─────────────────────────────┤
                                       │ • Scoped Authorize Filters  │
                                       │ • Row-Level Data Isolation  │
                                       │ • Automatic Scribe Linkage  │
                                       │ • Realtime Notification Hub │
                                       └──────────────┬──────────────┘
                                                      ▼
                                       ┌─────────────────────────────┐
                                       │ SQL Server (dentist Schema) │
                                       │   17 Existing + 4 New Tables│
                                       └─────────────────────────────┘
```

### Key Differences Matrix

| Dimension | Doctor Workspace (`/dashboard`, `/chart/*`) | Patient Portal (`/portal/*`) |
| :--- | :--- | :--- |
| **Primary Goal** | Clinical diagnosis, procedure charting, ambient voice note generation | Transparency, appointment self-scheduling, report viewing, invoice settlement |
| **Authentication Credential** | Username + Clinician Password | **Patient Reference Number** (or Email/Phone) + Portal Password / DOB |
| **Token Claims** | `sub: doctorId`, `role: "Doctor"`, `region: "NZ"`, `isSuperAdmin: bit` | `sub: patientId`, `ref: "DEN-2026-00035"`, `role: "Patient"`, `doctorId: 2` |
| **Session Lifetime** | 45 minutes idle timeout (HIPAA strict auto-logout) | 7 days sliding refresh token with biometric/PIN re-auth option |
| **Visual Tone** | Midnight Slate `#10244B`, Surgical Royal Blue `#4A7CD2`, dense telemetry | Warm Cloud `#F4F6FA`, Soft Indigo `#EAF0FC`, Mint `#10B981`, friendly cards |
| **Data Scope** | All patients assigned to this doctor / all clinic patients | **Strictly Patient's Own Records** (queries always scoped by authenticated `PatientID`) |

---

## 2. Patient Onboarding & Login Mechanics

The portal provides a flexible, dual-entry access system that seamlessly accommodates both newly registering patients and patients already registered at the clinic front desk.

```
                               ┌───────────────────────────────────┐
                               │     Patient Visits Portal Entry   │
                               │          `/portal/login`          │
                               └─────────────────┬─────────────────┘
                                                 │
                     ┌───────────────────────────┴───────────────────────────┐
                     ▼                                                       ▼
       ┌───────────────────────────┐                           ┌───────────────────────────┐
       │   Path A: Returning User  │                           │  Path B: First-Time User  │
       │     Has Password Set      │                           │  Needs Account Setup      │
       └─────────────┬─────────────┘                           └─────────────┬─────────────┘
                     │                                                       │
        ┌────────────┴────────────┐                             ┌────────────┴────────────┐
        ▼                         ▼                             ▼                         ▼
┌──────────────┐          ┌──────────────┐              ┌──────────────┐          ┌──────────────┐
│ Reference #  │          │ Email/Phone  │              │ Walk-In Slip │          │ Direct Self- │
│  + Password  │          │  + Password  │              │ Reference #  │          │ Registration │
│              │          │              │              │  Activation  │          │ (New Patient)│
└───────┬──────┘          └───────┬──────┘              └───────┬──────┘          └───────┬──────┘
        │                         │                             │                         │
        └────────────┬────────────┘                             ▼                         ▼
                     │                                   Enter Ref # + DOB         Fill Intake Form:
                     ▼                                   to claim profile.         Name, Phone, DOB.
            Validate BCrypt Hash                         Sets Portal Password.     System generates
                     │                                          │                  Reference # instantly!
                     ▼                                          │                         │
            Generate Patient JWT ◄──────────────────────────────┴─────────────────────────┘
                     │
                     ▼
          Redirect to `/portal/dashboard`
```

### Path 1: Instant Login via Patient Reference Number (MRN)
- When a patient visits the clinic or books their first consultation, the clinic system assigns them a unique, branded reference code:
  $$\mathbf{DEN\text{-}YYYY\text{-}XXXXX} \quad \text{e.g., } \mathbf{DEN\text{-}2026\text{-}00035}$$
- This reference number is clearly printed on their **Appointment Receipt Card**, printed on clinical fee estimates, and sent via SMS/Email.
- **Login Input**: Reference Number + Password (or DOB for first-time activation).

### Path 2: First-Time Portal Activation (Clinic Walk-In Transition)
1. Patient clicks **"Have a Clinic Reference Number? Activate Account"**.
2. Inputs:
   - `Reference Number`: `DEN-2026-00035`
   - `Date of Birth`: `YYYY-MM-DD` (anti-fraud verification)
   - `Registered Phone` (last 4 digits match)
3. System verifies against `[dentist].[Patients]`.
4. Patient chooses a strong password (minimum 8 characters, 1 number, 1 uppercase).
5. Account is instantly active.

### Path 3: Direct Online Self-Registration
1. Prospective patient lands on `/portal/register`.
2. Fills in basic demographics: First Name, Last Name, DOB, Phone, Email, Gender, and Preferred Doctor / Location.
3. System creates profile in `[dentist].[Patients]`, generates reference code `DEN-2026-XXXXX`, hashes password with BCrypt, and logs the user straight into their portal with a welcome onboarding modal.

---

## 3. Patient Portal Core Functional Modules

### 3.1 Patient Command Dashboard
The dashboard serves as the central health cockpit for the patient. It avoids complex medical jargon and displays essential information through clear visual cards:

```
+-------------------------------------------------------------------------------------------------------+
|  🦷 DENTIA PATIENT PORTAL         [Appointments]  [My Reports]  [Invoices & Pay]     (👤 John Doe v)  |
+-------------------------------------------------------------------------------------------------------+
|                                                                                                       |
|  👋 Welcome back, John!                                            Patient Ref: DEN-2026-00035        |
|  Your dental health is in great standing. Next cleaning is recommended in October 2026.               |
|                                                                                                       |
|  +-----------------------------------+  +----------------------------------+  +---------------------+ |
|  | 🗓️ NEXT APPOINTMENT               |  | 💳 OUTSTANDING BALANCE           |  | 📋 ACTIVE PLAN      | |
|  | Thu, Sept 18, 2026 • 10:30 AM     |  | $145.00 NZD                      |  | Clear Aligners      | |
|  | Dr. Sarah Jenkins (Operatory 2)   |  | 1 Unpaid Invoice (INV-2026-0089) |  | Stage 3 of 12       | |
|  | [ Reschedule ]  [ Add to Cal ]    |  | [ Pay Online ]  [ Cash Voucher ] |  | [ View Roadmap ]    | |
|  +-----------------------------------+  +----------------------------------+  +---------------------+ |
|                                                                                                       |
|  +---------------------------------------------------------------------+  +-------------------------+ |
|  | 🦷 DENTAL HEALTH OVERVIEW                                            |  | 💊 ACTIVE PRESCRIPTIONS | |
|  | Visual Tooth Status: 28 Healthy | 2 Restored | 2 Under Observation   |  | • Amoxicillin 500mg     | |
|  |                                                                     |  |   1 tab TDS (3 days left)| |
|  |  (1)(2)(3)(4)[5](6)(7)(8) | (9)(10)(11)[12](13)(14)(15)(16)        |  | • Chlorhexidine Rinse   | |
|  |  (32)(31)(30)[29](28)(27)  | (22)(21)(20)[19](18)(17)               |  |   Twice daily after food| |
|  |  [ View Interactive Tooth Map ]                                      |  | [ Request Refill ]      | |
|  +---------------------------------------------------------------------+  +-------------------------+ |
|                                                                                                       |
|  +-------------------------------------------------------------------------------------------------+  |
|  | 📁 RECENT CLINICAL VISITS & REPORTS                                                               |  |
|  | • Aug 29, 2026 — Composite Restoration Tooth 14 (Dr. Sarah Jenkins)        [ 📄 View Report PDF ] |  |
|  | • Aug 15, 2026 — Routine Exam & Bitewing Radiographs                        [ 🖼️ View X-Rays ]    |  |
|  | • Jul 02, 2026 — Orthodontic Progress Check & Wire Change                   [ 📄 View Summary ]   |  |
|  +-------------------------------------------------------------------------------------------------+  |
+-------------------------------------------------------------------------------------------------------+
```

---

### 3.2 Smart Appointment Booking & Calendar
Patients can book, view, reschedule, or cancel consultations without needing to phone the reception desk.

- **Direct Synchronization**: Books straight into `[dentist].[Appointments]` with foreign key `PatientID`.
- **Slot Conflict Prevention**: Verifies doctor schedule and chair operatory availability before confirmation.
- **Appointment Features**:
  1. **Select Service / Purpose**:
     - *Routine Dental Checkup & Hygiene Cleaning* (45 mins)
     - *Tooth Pain / Emergency Consultation* (30 mins)
     - *Orthodontic Aligners / Braces Adjustment* (30 mins)
     - *Cosmetic Teeth Whitening Consultation* (45 mins)
     - *Restorative Filling / Crown Follow-up* (60 mins)
  2. **Select Attending Dentist**: Choose preferred dentist or first available slot.
  3. **Interactive Time-Slot Grid**: Morning (9:00 AM – 1:00 PM) and Afternoon (2:00 PM – 5:30 PM).
  4. **Calendar Sync**: One-click `.ics` export for Apple Calendar, Google Calendar, and Outlook.
  5. **Instant Confirmation SMS / Email**: Triggered via `IEmailService`.

---

### 3.3 Clinical Reports, Prescriptions & Diagnostics
Patients gain full transparency over their healthcare journey with clear, patient-friendly medical documentation:

1. **AI Consultation Summaries**:
   - Patient-tailored rendering of `[dentist].[DentalNotes]` omitting complex clinical acronyms while presenting:
     - *Reason for Visit*
     - *Procedures Performed Today*
     - *Post-Operative Instructions & Care Guidelines* (e.g. "Do not chew on right side for 2 hours; avoid hot liquids").
     - *Next Recall Recommendation*.
2. **Medications & Prescriptions**:
   - Rendered from `[dentist].[DentalNotePrescriptions]` and `[dentist].[Prescriptions]`.
   - Shows Drug Name, Dosage, Administration Route, Frequency, and remaining duration with badge indicator (`Active` vs `Completed`).
3. **High-Resolution Radiographs (X-Rays & Scans)**:
   - Fetched from `[dentist].[Radiographs]` with client-side canvas zoom, pan, and contrast adjustment.
   - Summarized by AI vision findings in layman-friendly language.
4. **Interactive Dental Odontogram Viewer**:
   - Simplified 32-tooth map displaying the patient's oral anatomy with intuitive color coding:
     - 🟢 **Healthy & Sound** (`#10B981`)
     - 🔵 **Successfully Restored / Crowned** (`#3B82F6`)
     - 🟡 **Under Observation** (`#F59E0B`)
     - 🟣 **Orthodontic Appliance Attached** (`#8B5CF6`)

---

### 3.4 Billing, Invoices & Dual Payment Gateway (Online + Cash)

A major friction point in dental clinics is billing opacity. The portal provides an itemized billing ledger supporting both **Digital Online Payments** and **In-Clinic Cash Payments**.

```
                               ┌───────────────────────────────────┐
                               │       Patient Selects Invoice     │
                               │        Total Due: $240.00         │
                               └─────────────────┬─────────────────┘
                                                 │
                     ┌───────────────────────────┴───────────────────────────┐
                     ▼                                                       ▼
       ┌───────────────────────────┐                           ┌───────────────────────────┐
       │   Method A: Pay Online    │                           │    Method B: Pay with Cash│
       │   (Instant Digital Pay)   │                           │     (In-Clinic Front Desk)│
       └─────────────┬─────────────┘                           └─────────────┬─────────────┘
                     │                                                       │
        ┌────────────┴────────────┐                                          │
        ▼                         ▼                                          ▼
┌──────────────┐          ┌──────────────┐                     ┌───────────────────────────┐
│ Credit/Debit │          │ Apple Pay /  │                     │ System generates          │
│ Card (Stripe)│          │ Google Pay   │                     │ "Cash Payment Voucher"    │
└───────┬──────┘          └───────┬──────┘                     │ with unique barcode & QR. │
        │                         │                            └─────────────┬─────────────┘
        └────────────┬────────────┘                                          │
                     │                                                       ▼
                     ▼                                         Invoice marked:
             Gateway Processes                                 `Status = 'Pending_Cash'`
                     │                                                       │
                     ▼                                                       ▼
            Payment Succeeded:                                 Patient presents voucher
            • Record in `Payments`                             to Clinic Receptionist.
            • Update `Invoices` -> `Paid`                                    │
            • Generate PDF Receipt                                           ▼
            • Instant confirmation                               Receptionist confirms cash in
                                                               Clinician Desk -> Marks `Paid`.
```

#### Detailed Workflow: Online Payment
1. Patient views invoice `INV-2026-00104` with breakdown:
   - *ADA 012 - Comprehensive Oral Exam*: $65.00
   - *ADA 111 - Full Mouth Prophylaxis*: $120.00
   - *ADA 022 - Periapical X-Ray (Tooth 19)*: $35.00
   - *GST / Tax (15%)*: $33.00
   - **Total Due**: **$253.00 NZD**
2. Patient clicks **"Pay Now Online"**.
3. Secure modal appears (powered by Stripe Elements / Hosted Checkout).
4. Patient enters Card details or uses 1-Click Apple Pay / Google Pay.
5. On webhook callback / API response:
   - `[dentist].[Payments]` records transaction ID `ch_3Nx...`, Gateway `Stripe`, Status `Success`.
   - `[dentist].[Invoices]` updates `PaidAmount = 253.00`, `BalanceAmount = 0.00`, `Status = 'Paid'`.
   - Official Tax Receipt is instantly generated for download.

#### Detailed Workflow: Cash Payment at Clinic
1. Patient clicks **"Pay with Cash at Clinic"**.
2. System produces a printable/mobile-scannable **Cash Payment Voucher**:
   - *Voucher Code*: `CSH-2026-00412`
   - *Patient*: John Doe (`DEN-2026-00035`)
   - *Invoice*: `INV-2026-00104`
   - *Exact Cash Due*: **$253.00 NZD**
   - *QR Code & Barcode* linking to the invoice.
3. System creates a pending cash payment record in `[dentist].[Payments]` with `PaymentStatus = 'Pending_Cash_Verification'`.
4. Invoice status transitions to `'Pending Cash Settlement'`.
5. Patient arrives at clinic reception, hands over cash, and the receptionist scans the QR code or types `CSH-2026-00412` in the clinic workspace to mark **"Cash Received & Confirmed"**.
6. System logs `ReceivedByDoctorID`, changes status to `'Success'`, and both patient and clinic receive an authorized Cash Receipt stamp.

---

## 4. UI/UX Design System & Aesthetic Principles

The Patient Portal uses an **empathetic, approachable, modern healthcare design system** aligned with Dentia's core color palette:

### Design Tokens

```css
:root {
  /* Brand Core */
  --portal-bg-canvas: #F8FAFC;         /* Clean, airy medical canvas */
  --portal-card-bg: #FFFFFF;           /* Crisp elevated surfaces */
  --portal-primary: #4A7CD2;           /* Trustworthy medical royal blue */
  --portal-primary-hover: #3665B7;     /* High-contrast hover */
  --portal-dark: #10244B;              /* Deep navy authority */
  --portal-light-accent: #EAF0FC;      /* Soft breathable chip background */
  
  /* Status Semantics */
  --portal-success: #10B981;           /* Paid / Completed / Healthy */
  --portal-success-light: #ECFDF5;
  --portal-warning: #F59E0B;           /* Pending / Under Observation */
  --portal-warning-light: #FFFBEB;
  --portal-danger: #EF4444;            /* Overdue / Urgent */
  --portal-danger-light: #FEF2F2;
  
  /* Typography */
  --portal-font-headings: 'Urbanist', -apple-system, sans-serif;
  --portal-font-body: 'Inter', -apple-system, sans-serif;
  
  /* Elevation Shadows */
  --portal-shadow-sm: 0 1px 2px 0 rgba(16, 36, 75, 0.05);
  --portal-shadow-md: 0 4px 6px -1px rgba(16, 36, 75, 0.08), 0 2px 4px -2px rgba(16, 36, 75, 0.05);
  --portal-shadow-lg: 0 10px 15px -3px rgba(16, 36, 75, 0.08), 0 4px 6px -4px rgba(16, 36, 75, 0.03);
}
```

### UI/UX Rules of Engagement
1. **Never Show Raw Clinical Abbreviations Without Explanations**: E.g. instead of showing *"RCT on #19 with GP obturation"*, show *"Root canal treatment completed on Lower Left Molar (Tooth 19)"*.
2. **Mobile First Responsive Hierarchy**: 70%+ of patients check dental reports and pay invoices from smartphones. All modals, payment drawers, and appointment pickers must have full touch targets (minimum 48px height) and swipe gestures.
3. **High-Contrast Financial Transparency**: Invoices display exact itemized lines so patients understand every dollar charged, boosting payment compliance.
4. **Skeleton Loaders & Zero Cumulative Layout Shift (CLS)**: As implemented in `FullPageSkeletonLoader.jsx`, all asynchronous data uses pulse animations.

---

## 5. Database Delta & Schema Impact Analysis

### Audit of Existing Schema (`dentist.*`)
The database currently comprises **17 verified tables** (`Patients`, `Doctors`, `Appointments`, `DentalNotes`, `Prescriptions`, `Radiographs`, `TeethState`, etc.).

### Required Enhancements to Existing Tables

#### 1. `[dentist].[Patients]`
Add portal access credentials and unique reference numbering:
- `ReferenceNumber NVARCHAR(30) NOT NULL UNIQUE` (e.g. `DEN-2026-00035`)
- `PasswordHash NVARCHAR(255) NULL` (BCrypt hash for portal password)
- `IsPortalActive BIT NOT NULL DEFAULT 1` (Allows disabling portal access if needed)
- `LastLoginAt DATETIME2 NULL` (Portal security audit)
- `MustChangePassword BIT NOT NULL DEFAULT 0` (Forces password change on initial activation)

#### 2. `[dentist].[Appointments]`
Ensure direct linkage to patient accounts:
- `PatientID INT NULL CONSTRAINT FK_Appointments_Patients FOREIGN KEY REFERENCES [dentist].[Patients](PatientID)`
- Index `IX_Appointments_PatientID` for high-performance patient calendar queries.

---

### New Tables to Create

#### 1. `[dentist].[Invoices]`
Stores financial bills generated by clinicians or treatment plans.

| Column Name | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `InvoiceID` | `BIGINT` | `PK IDENTITY(1,1)` | Unique internal invoice identifier |
| `InvoiceNumber` | `NVARCHAR(50)` | `NOT NULL UNIQUE` | Branded invoice number e.g. `INV-2026-00104` |
| `PatientID` | `INT` | `FK -> Patients(PatientID)` | Billing target patient |
| `DoctorID` | `INT` | `FK -> Doctors(DoctorID)` | Primary treating clinician |
| `AppointmentID` | `INT` | `FK -> Appointments` | Associated consultation (optional) |
| `IssueDate` | `DATE` | `NOT NULL DEFAULT GETDATE()` | Date invoice was issued |
| `DueDate` | `DATE` | `NOT NULL` | Payment due date |
| `SubTotal` | `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Pre-tax procedure sum |
| `TaxAmount` | `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Applicable sales tax / GST |
| `DiscountAmount`| `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Clinic courtesies / insurance copay |
| `TotalAmount` | `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Final payable balance |
| `PaidAmount` | `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Cumulative settled payments |
| `BalanceAmount` | `COMPUTED` | `AS (TotalAmount - PaidAmount)`| Real-time unpaid balance |
| `Status` | `NVARCHAR(30)` | `DEFAULT 'Unpaid'` | `'Unpaid'`, `'Partially Paid'`, `'Paid'`, `'Pending Cash Settlement'`, `'Cancelled'` |
| `Notes` | `NVARCHAR(MAX)`| `NULL` | Public payment terms / clinical note |
| `CreatedAt` | `DATETIME2` | `DEFAULT SYSUTCDATETIME()` | Invoice creation timestamp |

#### 2. `[dentist].[InvoiceItems]`
Itemized clinical procedures and materials per invoice.

| Column Name | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `InvoiceItemID` | `BIGINT` | `PK IDENTITY(1,1)` | Unique item identifier |
| `InvoiceID` | `BIGINT` | `FK -> Invoices CASCADE` | Parent invoice link |
| `ProcedureCode` | `NVARCHAR(50)` | `NULL` | Dental code (e.g. `ADA-012`, `NZ-111`) |
| `Description` | `NVARCHAR(500)` | `NOT NULL` | Procedure name (e.g. "Tooth Whitening Session 1") |
| `ToothNumber` | `INT` | `NULL` | Anatomical tooth (1-32) if applicable |
| `Quantity` | `INT` | `NOT NULL DEFAULT 1` | Units rendered |
| `UnitPrice` | `DECIMAL(18,2)` | `NOT NULL DEFAULT 0.00` | Fee per unit |
| `TotalPrice` | `COMPUTED` | `AS (Quantity * UnitPrice)` | Computed subtotal line item |

#### 3. `[dentist].[Payments]`
Records all financial transactions (Card, Online Gateway, and Front-Desk Cash).

| Column Name | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `PaymentID` | `BIGINT` | `PK IDENTITY(1,1)` | Unique payment identifier |
| `PaymentReceiptNo`| `NVARCHAR(50)`| `NOT NULL UNIQUE` | Branded receipt e.g. `REC-2026-00891` |
| `InvoiceID` | `BIGINT` | `FK -> Invoices` | Settled invoice reference |
| `PatientID` | `INT` | `FK -> Patients` | Paying patient reference |
| `Amount` | `DECIMAL(18,2)` | `NOT NULL` | Payment amount tendered |
| `PaymentMethod` | `NVARCHAR(50)` | `NOT NULL` | `'Online_Card'`, `'Cash'`, `'Bank_Transfer'`, `'Insurance'` |
| `PaymentStatus` | `NVARCHAR(30)` | `DEFAULT 'Success'` | `'Success'`, `'Pending_Cash_Verification'`, `'Failed'`, `'Refunded'` |
| `TransactionReference`|`NVARCHAR(150)`| `NULL` | Gateway Charge ID or Bank Ref |
| `PaymentGateway` | `NVARCHAR(50)` | `NULL` | `'Stripe'`, `'Square'`, `'ClinicCashDesk'` |
| `ReceivedByDoctorID`|`INT` | `FK -> Doctors` | Staff member who received cash |
| `PaymentDate` | `DATETIME2` | `DEFAULT SYSUTCDATETIME()` | Transaction timestamp |
| `Notes` | `NVARCHAR(MAX)`| `NULL` | Cash drawer voucher code or notes |

#### 4. `[dentist].[PatientPortalActivityLogs]`
Audits all portal logins, appointment bookings, report views, and downloads for security compliance.

| Column Name | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `LogID` | `BIGINT` | `PK IDENTITY(1,1)` | Unique audit identifier |
| `PatientID` | `INT` | `FK -> Patients` | Target patient |
| `Action` | `NVARCHAR(100)` | `NOT NULL` | `LOGIN`, `BOOK_APPOINTMENT`, `VIEW_REPORT`, `PAY_ONLINE`, `GENERATE_CASH_VOUCHER` |
| `IpAddress` | `VARCHAR(50)` | `NULL` | Remote client IP |
| `UserAgent` | `NVARCHAR(500)` | `NULL` | Browser / Device user agent |
| `Timestamp` | `DATETIME2` | `DEFAULT SYSUTCDATETIME()` | Event timestamp |

---

## 6. Full-Stack Implementation Blueprint (Backend & Frontend)

### 6.1 Backend API Endpoints (ASP.NET Core)

#### `PatientAuthController` (`/api/patient-auth`)
- `POST /api/patient-auth/login` — Authenticate via Reference Number or Email + Password.
- `POST /api/patient-auth/register` — Self-register new patient, auto-generate Reference #, return JWT.
- `POST /api/patient-auth/activate` — First-time activation: verify Reference # + DOB, set password.
- `POST /api/patient-auth/forgot-password` — Send reset OTP to verified phone or email.
- `GET  /api/patient-auth/me` — Return authenticated patient profile and preference state.

#### `PatientPortalController` (`/api/patient-portal`)
- `GET  /api/patient-portal/dashboard` — Aggregated summary (next appointment, unpaid balance, recent visits, active prescriptions).
- `GET  /api/patient-portal/appointments` — Fetch patient's upcoming and historical appointments.
- `POST /api/patient-portal/appointments` — Book new appointment slot.
- `PUT  /api/patient-portal/appointments/{id}/cancel` — Cancel an upcoming booking.
- `GET  /api/patient-portal/reports` — Clinical consultation notes, post-op guidance, doctor summaries.
- `GET  /api/patient-portal/prescriptions` — Active medications, dosages, refill history.
- `GET  /api/patient-portal/radiographs` — X-rays with AI vision summaries and high-res imaging links.
- `GET  /api/patient-portal/odontogram` — 32-tooth state summary for visual health diagram.

#### `BillingController` (`/api/billing`)
- `GET  /api/billing/invoices` — List all invoices for authenticated patient.
- `GET  /api/billing/invoices/{id}` — Itemized invoice details with procedures and payment history.
- `POST /api/billing/pay-online` — Process digital payment (Stripe token / intent verification).
- `POST /api/billing/generate-cash-voucher` — Generate in-clinic cash payment slip with barcode/QR.
- `POST /api/billing/confirm-cash-payment` — *(Doctor/Staff only)* Confirm reception of physical cash.
- `GET  /api/billing/receipts/{receiptNo}/pdf` — Generate downloadable official PDF receipt.

---

### 6.2 Frontend Route Separation (`Dentistfrontend/src/App.jsx`)

```jsx
// Patient Portal Route Layout
import PatientLayout from './modules/patientPortal/layouts/PatientLayout';
import PatientLogin from './modules/patientPortal/pages/PatientLogin';
import PatientRegister from './modules/patientPortal/pages/PatientRegister';
import PatientDashboard from './modules/patientPortal/pages/PatientDashboard';
import PatientAppointments from './modules/patientPortal/pages/PatientAppointments';
import PatientReports from './modules/patientPortal/pages/PatientReports';
import PatientBilling from './modules/patientPortal/pages/PatientBilling';

// Patient Protected Route Guard
const PatientProtectedRoute = ({ children }) => {
  const patient = JSON.parse(localStorage.getItem('patient'));
  const location = useLocation();

  if (!patient || !patient.token) {
    return <Navigate to="/portal/login" state={{ from: location }} replace />;
  }
  return children;
};

// Route Configuration in App.jsx
<Routes>
  {/* ---------------------------------------------------- */}
  {/* 1. PUBLIC LANDING & DOCTOR WORKSPACE (EXISTING)      */}
  {/* ---------------------------------------------------- */}
  <Route path="/" element={<BlockSuperAdmin><LandingDashboard /></BlockSuperAdmin>} />
  <Route path="/login" element={<BlockSuperAdmin><Auth /></BlockSuperAdmin>} />
  <Route path="/directory" element={<ProtectedRoute><PatientDirectory /></ProtectedRoute>} />
  <Route path="/chart/:patientId" element={<ProtectedRoute><ChartPage /></ProtectedRoute>} />
  <Route path="/ai-notes" element={<ProtectedRoute><AIDentalNotesPage /></ProtectedRoute>} />

  {/* ---------------------------------------------------- */}
  {/* 2. DEDICATED PATIENT PORTAL ROUTES                  */}
  {/* ---------------------------------------------------- */}
  {/* Public Patient Auth */}
  <Route path="/portal/login" element={<PatientLogin />} />
  <Route path="/portal/register" element={<PatientRegister />} />
  <Route path="/portal/activate" element={<PatientActivateAccount />} />

  {/* Authenticated Patient Workspace */}
  <Route path="/portal" element={
    <PatientProtectedRoute>
      <PatientLayout />
    </PatientProtectedRoute>
  }>
    <Route index element={<Navigate to="/portal/dashboard" replace />} />
    <Route path="dashboard" element={<PatientDashboard />} />
    <Route path="appointments" element={<PatientAppointments />} />
    <Route path="reports" element={<PatientReports />} />
    <Route path="prescriptions" element={<PatientPrescriptions />} />
    <Route path="radiographs" element={<PatientRadiographs />} />
    <Route path="billing" element={<PatientBilling />} />
  </Route>
</Routes>
```

---

## 7. Security, HIPAA/GDPR Compliance & Token Management

1. **Storage Isolation**:
   - Doctor credentials stored under `localStorage.getItem('doctor')`.
   - Patient credentials stored under `localStorage.getItem('patient')`.
   - Ensures a clinician and a patient can even use the same browser without cross-session pollution.
2. **Strict Backend Scoping**:
   - The backend claims middleware extracts `PatientID` from the validated JWT.
   - Any endpoint under `/api/patient-portal/*` automatically injects `WHERE PatientID = @ClaimsPatientID` into SQL statements. A patient cannot view another patient's data even if they guess or modify IDs in API requests.
3. **Password Hashing**:
   - All portal passwords hashed using `BCrypt.Net` with work factor 11.
4. **Audit Trail**:
   - Every invoice payment and appointment change creates a tamper-proof entry in `[dentist].[ClinicalLogs]` and `[dentist].[PatientPortalActivityLogs]`.

---

## 8. Summary of Migration Deliverables
- **Architecture & UI/UX Specification**: Complete in this document.
- **SQL Migration Script**: Created in `PATIENT_PORTAL_AND_BILLING_MIGRATION.sql` with idempotent schema additions, indexes, stored procedures, and sample seed data.
