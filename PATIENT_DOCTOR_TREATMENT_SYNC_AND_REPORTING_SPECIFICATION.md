# Comprehensive Specification: Patient Portal Treatment Selection, Doctor Treatment Plan Management, Bi-Directional Notes Sync & Unified Patient Reporting

> **Document Version**: 2.0.0  
> **Status**: Approved Architecture & Implementation Specification  
> **Target Systems**: `Dentistfrontend` (Vercel) & `DentistAPI` / `DentistAPIClone` (.NET 8 + SQL Server)  
> **Author**: Antigravity Engineering & Clinical Architecture Team  

---

## 1. Executive Summary & Core Requirements

This specification provides the comprehensive, end-to-end blueprint for the clinical, operational, and financial workflow between **Patients** (via the Patient Portal) and **Attending Dental Surgeons** (via the Doctor Section).

```
+---------------------------------------------------------------------------------------------------------+
|                                    END-TO-END CLINICAL & FINANCIAL LOOP                                  |
+---------------------------------------------------------------------------------------------------------+
|                                                                                                         |
|   [PATIENT PORTAL]                                                    [DOCTOR CLINICAL WORKSPACE]       |
|   1. Browses Doctor's Procedure Catalog                               1. Receives Scheduled Booking     |
|   2. Books with Treatments OR Books Standalone (No Treatment Req.)    2. Sees Patient-Demanded Items    |
|   3. Adds "Patient Consultation Notes / Special Requests"             3. Reviews Symptoms & Notes       |
|                                       │                                               │                 |
|                                       ▼                                               ▼                 |
|   [BI-DIRECTIONAL SYNC]  ◄──────────────────────────────────────────────────────►  [DOCTOR EDITING]     |
|   - Real-time Note Synchronization                                        - Adds/Removes Procedures     |
|   - Real-time Fee & Invoice Recalculation                                 - Modifies Clinical Notes     |
|   - Immediate Visibility of Doctor Feedback                               - Syncs to Invoice & Ledger   |
|                                       │                                               │                 |
|                                       ▼                                               ▼                 |
|   [PATIENT TRANSPARENCY]                                              [COMPREHENSIVE REPORTING]         |
|   - View Updated Treatment Plan & Codes                               - 1-Click Patient Dossier Report  |
|   - View Accurate Itemized Invoice & Receipts                         - 32-Tooth Chart + Invoices + Rx  |
|   - Download PDF Receipts & Treatment Summaries                       - Chairside Print / PDF Export    |
|                                                                                                         |
+---------------------------------------------------------------------------------------------------------+
```

### Key Business & Technical Goals Addressed:
1. **Optional Treatment Selection at Booking**:
   - Patients can book an appointment **WITH** specific treatments selected from the doctor's fee catalog, **OR** book **WITHOUT** any treatment plan (pure consultation / triage booking). Treatment selection is never mandatory.
2. **Transparent Doctor Fee Schedule for Patients**:
   - Patients have a dedicated, intuitive UI to explore their assigned doctor’s full treatment and pricing catalog with category filters, estimated duration, and regional currency (`PKR` vs `NZD`).
3. **Doctor Visibility of Patient Demands**:
   - When a patient books with requested treatments, the doctor sees an immediate, clear breakdown of what procedures the patient demanded, along with their preliminary cost.
4. **Doctor Treatment Plan Modification & Chairside Editing**:
   - Doctors can modify the treatment plan at any time: add clinical procedures (e.g. from the chairside odontogram or fee schedule), remove invalid items, adjust fees, apply clinical discounts, and update notes.
5. **Bi-Directional Synchronization of "Patient Consultation Notes / Special Requests"**:
   - The "Patient Consultation Notes / Special Requests" section is synchronized between Patient and Doctor.
   - When the doctor updates comments, the patient immediately sees them in their portal.
   - When the patient updates their notes, the doctor immediately sees the update.
   - Eliminates previous bugs where notes duplicated, got mangled with procedure names, or failed to persist in the database.
6. **Per-Patient Financial & Treatment Reporting**:
   - Both Doctor and Patient have full transparency over treatment plans, itemized line items, invoice totals, paid amounts, balance due, and payment receipts.
   - Doctor can generate a comprehensive, single-click **Per-Patient Clinical & Invoice Report (Print & PDF)** combining 32-tooth odontogram findings, active treatment plans, invoices, and clinical notes.

---

## 2. Root Cause Analysis: Previous "Patient Consultation Notes" Persistence Issue

### 2.1 The Problem
In the previous implementation, when patients or doctors attempted to edit the "Patient Consultation Notes / Special Requests", the changes either failed to persist cleanly, or resulted in corrupted string duplication (e.g., `- Note: sensitivity - Note: please be gentle`).

### 2.2 Root Cause 1: Backend String Suffix Trapping (`DentalRepository.cs`)
In `UpdateAppointmentTreatmentPlanAsync`:
```csharp
// OLD FLAWED CODE:
string docPrefix = "";
if (!string.IsNullOrEmpty(appt.Reason) && appt.Reason.Contains("(") && appt.Reason.Contains(")"))
{
    int start = appt.Reason.LastIndexOf("(");
    docPrefix = " " + appt.Reason.Substring(start); // BUG: Captures from '(' to the END of string, including previous '- Note: ...'!
}
string newReason = $"{string.Join(", ", procNames)}{docPrefix}{(string.IsNullOrWhiteSpace(notes) ? "" : $" - Note: {notes.Trim()}")}";
```
* **Why it failed**:
  1. `appt.Reason.Substring(start)` grabbed `(Dr. Sarah J. Lee) - Note: old note`.
  2. The new note was appended to the old note: `(Dr. Sarah J. Lee) - Note: old note - Note: new note`.
  3. If the user cleared the note, `docPrefix` still contained the old note, making deletion impossible!

### 2.3 Root Cause 2: Frontend State Pollution (`PatientAppointments.jsx`)
In `handleOpenEditPlan`:
```javascript
// OLD FLAWED CODE:
setEditingNotes(appt.reason || '');
```
* **Why it failed**:
  `appt.reason` is a composite string: `Routine Checkup [Code: 011] (Dr. Sarah J. Lee) - Note: My tooth hurts`.
  Dumping this whole string into `editingNotes` caused the modal textarea to show the procedure codes and doctor name inside the note box! When the patient edited the text, the entire procedure title was submitted as the note.

### 2.4 The Architectural Solution
1. **Isolated Regex Extraction**:
   - Extract the doctor's attribution cleanly: `Regex.Match(reason, @"\((Dr\.?[^)]+)\)")`.
   - Strip any existing `- Note:` or `- Notes:` patterns before appending the updated note.
2. **Clean Frontend Extraction**:
   - Parse `appt.reason` into two isolated fields on the frontend:
     - `cleanProcedures`: e.g., `Routine Checkup [Code: 011]`
     - `cleanNotes`: e.g., `My tooth hurts`
3. **Database Schema Enhancement**:
   - Support a dedicated `PatientNotes` / `SpecialRequests` column or structured DTO storage, with backwards-compatible parsing of legacy composite `Reason` strings.

---

## 3. Database Schema & Data Linkage Architecture

```
                                  +---------------------------------------+
                                  |         [dentist].[Patients]          |
                                  |---------------------------------------|
                                  | PatientID (PK)                        |
                                  | FullName, Email, Phone                |
                                  | DoctorID (FK -> Doctors)              |
                                  | CurrentTreatmentPlan (NVARCHAR)       |
                                  +---------------------------------------+
                                                     |
                         +---------------------------+---------------------------+
                         | 1:N                                                   | 1:N
                         v                                                       v
+-----------------------------------------------+       +-----------------------------------------------+
|            [dentist].[Appointments]           |       |              [dentist].[Invoices]             |
|-----------------------------------------------|       |-----------------------------------------------|
| AppointmentID (PK)                            | 1:1   | InvoiceID (PK)                                |
| PatientID (FK)                                |◄─────►| AppointmentID (FK)                            |
| DoctorID (FK)                                 |       | PatientID (FK), DoctorID (FK)                 |
| PreferredDate (DATETIME2)                     |       | InvoiceNumber (e.g. INV-2026-00123)          |
| Status ('Confirmed', 'Completed', 'Cancelled')|       | SubTotal, TotalAmount, PaidAmount             |
| Reason (NVARCHAR: Procedures + Attrib)        |       | Status ('Paid', 'Pending Cash Settlement')    |
| Notes (NVARCHAR: Sync Patient/Doctor Notes)   |       | Currency ('PKR' | 'NZD')                      |
| CreatedAt, UpdatedAt                          |       | Notes (Payment Terms & Clinical Memo)         |
+-----------------------------------------------+       +-----------------------------------------------+
                         |                                                       |
                         |                                                       | 1:N
                         |                                                       v
                         |                              +-----------------------------------------------+
                         |                              |            [dentist].[InvoiceItems]           |
                         |                              |-----------------------------------------------|
                         |                              | InvoiceItemID (PK)                            |
                         |                              | InvoiceID (FK)                                |
                         |                              | ProcedureCode (e.g. '011', 'D0120')           |
                         |                              | Description (Procedure Name)                  |
                         |                              | Quantity (INT), UnitPrice (DECIMAL)           |
                         |                              | TotalPrice (DECIMAL)                          |
                         |                              +-----------------------------------------------+
                         v
+-----------------------------------------------+
|         [dentist].[Payments] (Receipts)       |
|-----------------------------------------------|
| PaymentID (PK)                                |
| InvoiceID (FK), PatientID (FK)                |
| PaymentReceiptNo (REC-2026-XXXXX / CSH-XXXXX) |
| Amount, PaymentMethod ('Online_Card'|'Cash')  |
| PaymentStatus ('Success'|'Pending_Cash_Verif')|
| CashVoucherCode (for front desk settlement)   |
| PaymentDate, Notes                            |
+-----------------------------------------------+
```

### Table Structure Definitions & Migration Script:

```sql
-- Ensure Dedicated Notes column exists on [dentist].[Appointments]
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('[dentist].[Appointments]') 
    AND name = 'Notes'
)
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD Notes NVARCHAR(MAX) NULL;
END
GO

-- Ensure DoctorID and Notes exist on [dentist].[Invoices]
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('[dentist].[Invoices]') 
    AND name = 'DoctorID'
)
BEGIN
    ALTER TABLE [dentist].[Invoices]
    ADD DoctorID INT NULL;
END
GO
```

---

## 4. Patient Portal Workflow: Booking, Optional Treatments & Notes

### 4.1 Step-by-Step Patient Booking Experience (`/portal/book`)

```
Step 1: Select Attending Doctor & Date/Time
  │   ├── Choose Dr. Sarah J. Lee (NZD) or Dr. Tariq Mahmood (PKR)
  │   └── Pick available 30-min clinical slot
  ▼
Step 2: Choose Treatment Option (STRICTLY OPTIONAL)
  │   ├── Option A [General Consultation Only (No Treatment Plan)]
  │   │     └── Standard consultation fee applied ($85 NZD / Rs 2,500 PKR)
  │   │     └── Procedures list remains empty []
  │   │
  │   └── Option B [Select Specific Procedures from Doctor's Fee Catalog]
  │         ├── Real-time search by procedure name or CDT code
  │         ├── Category filters (Preventive, Restorative, Endodontics, Implants, etc.)
  │         ├── Multi-selection with instant fee recalculation
  │         └── Dynamic procedure tags displayed
  ▼
Step 3: Patient Consultation Notes / Special Requests (OPTIONAL)
  │   ├── Freeform clinical notes textarea:
  │   │     "Patient has severe sensitivity on lower left molar when drinking cold water."
  │   │     "Please prepare numbing gel prior to examination."
  ▼
Step 4: Payment Preference & Instant Confirmation
  │   ├── Online Card: Instant credit/debit card capture -> Invoice marked 'Paid' -> Receipt 'REC-2026-XXXXX'
  │   └── Cash at Clinic: Booking confirmed -> Cash Voucher 'CSH-2026-XXXXX' generated for front desk
  ▼
Step 5: View Booking in Appointments Hub (`/portal/appointments`)
      ├── Clear treatment item chips with procedure codes and individual prices
      ├── Prominent "Patient Consultation Notes / Special Requests" callout card
      └── Ability to Edit Treatment Plan & Notes prior to visit
```

### 4.2 Patient View of Doctor's Fee Schedule & Treatment Plans
Patients can access their assigned doctor's complete procedure catalog anytime via:
1. **Dedicated Catalog Modal / Drawer** on `/portal/book`.
2. **"Explore Doctor Services" Link** on `/portal/dashboard`.
3. **Real-time Fee Schedule API**: `GET /api/treatment-pricing/doctor/{doctorId}`.

#### Features of the Fee Schedule Explorer:
- Grouped by the 15 standard dental categories (Examination, Restorative, Endodontics, Crowns, Orthodontics, etc.).
- Clearly lists standard duration (e.g. `30 mins`, `60 mins`).
- Displays currency-aware pricing (`PKR` for Dr. Tariq Mahmood, `NZD` for Dr. Sarah J. Lee).
- Quick **"Add to Consultation"** action button.

---

## 5. Doctor Workspace Workflow: Demand Review, Editing & Bi-Directional Sync

### 5.1 Where and How the Doctor Views Patient Demands
When the doctor opens:
1. **Appointments Management** (`/appointments`):
   - Each appointment card displays a **"Patient-Demanded Procedures"** badge.
   - Shows whether the patient booked a **General Consultation (No Treatments)** or **Requested Specific Procedures** (`3 Treatments Requested • Total $485 NZD`).
   - Shows the patient's **"Consultation Notes / Special Requests"** prominently highlighted with an amber callout box.
2. **Chairside Odontogram & Chart** (`/chart/:patientId`):
   - The top header displays the active appointment banner with patient notes.
   - The new **"💳 Treatment & Invoices"** tab lists all consultation items alongside odontogram teeth treatments.

### 5.2 Doctor Treatment Plan Modification Workflow

```
[Doctor clicks "Edit Treatment Plan & Notes" on Appointment]
                          │
                          ▼
+-----------------------------------------------------------------------+
| DOCTOR CHAIRSIDE TREATMENT PLAN EDITOR MODAL                          |
+-----------------------------------------------------------------------+
| Patient: Johnathan Davis (DEN-2026-0042) · Dr. Sarah J. Lee           |
|                                                                       |
| 1. PATIENT DEMANDED & SCHEDULED TREATMENTS:                           |
|    [011] Comprehensive Oral Examination         $85.00 NZD  [Remove]  |
|    [531] Composite Filling - Tooth #19          $150.00 NZD [Remove]  |
|    [NEW] + Add Chairside Procedure from Fee Schedule:                 |
|          [Select: D3330 - Molar Root Canal Therapy ($350) ▾] [+ Add]  |
|                                                                       |
| 2. ADJUST RECALCULATED TOTAL & DISCOUNTS:                             |
|    Recalculated Total: $585.00 NZD (Automatically updates invoice)    |
|                                                                       |
| 3. PATIENT CONSULTATION NOTES / SPECIAL REQUESTS (SYNCED):            |
|    ┌─────────────────────────────────────────────────────────────┐    |
|    │ Patient Note: Patient has cold sensitivity on lower left.   │    |
|    │ Doctor Update: Verified deep caries on #19. RCT recommended.│    |
|    └─────────────────────────────────────────────────────────────┘    |
|    [✓ Sync note to Patient Portal]                                    |
|                                                                       |
|            [Cancel]              [💾 Save & Synchronize Plan]         |
+-----------------------------------------------------------------------+
                          │
                          ▼
[PUT /api/appointments/{id}/treatment-plan]
  ├── Updates [dentist].[Appointments].Reason & Notes
  ├── Recalculates [dentist].[Invoices].SubTotal & TotalAmount
  ├── Replaces [dentist].[InvoiceItems] with new procedure list
  └── Emits activity audit log
```

### 5.3 Bi-Directional Note Synchronization Mechanism

To ensure notes are never lost or corrupted:

```
+---------------------------------------------------------------------------------------+
|                              BI-DIRECTIONAL NOTE SYNC PIPELINE                        |
+---------------------------------------------------------------------------------------+

  PATIENT PORTAL                                                  DOCTOR WORKSPACE
  Updates Notes in Modal                                          Updates Notes in Modal
  PUT /api/patient-portal/                                        PUT /api/appointments/
  appointments/{id}/treatment-plan                                {id}/treatment-plan
              │                                                               │
              └───────────────────────────────┬───────────────────────────────┘
                                              ▼
                             +---------------------------------+
                             |     DentalRepository.cs         |
                             | UpdateAppointmentTreatmentPlan  |
                             +---------------------------------+
                                              │
              ┌───────────────────────────────┴───────────────────────────────┐
              ▼                                                               ▼
  [CLEAN REASON CONSTRUCTION]                                     [ISOLATED NOTES STORAGE]
  - Takes procedure list                                          - Takes incoming `notes` string
  - Formats: "Proc A [Code], Proc B [Code]"                       - Saves directly to:
  - Appends Doctor attribution: "(Dr. Sarah J. Lee)"                UPDATE [dentist].[Appointments]
  - STOPS string pollution by omitting old notes from Reason!       SET Notes = @Notes, Reason = @CleanReason
              │                                                               │
              └───────────────────────────────┬───────────────────────────────┘
                                              ▼
                             +---------------------------------+
                             |    Clean Synchronization        |
                             +---------------------------------+
                                       │             │
                    ┌──────────────────┘             └──────────────────┐
                    ▼                                                   ▼
         PATIENT PORTAL SEES:                                DOCTOR SECTION SEES:
         📝 "Verified deep caries on #19.                   📝 "Verified deep caries on #19.
             RCT recommended."                                   RCT recommended."
```

---

## 6. Comprehensive Per-Patient Report: Architecture & PDF Generation

### 6.1 Report Structure & Content Breakdown
The **Comprehensive Patient Clinical & Financial Dossier (PDF & Print)** generated from the Doctor Section unifies the following sections onto an official clinical letterhead:

```
+---------------------------------------------------------------------------------------+
|  DENTIA CLINICAL DENTAL MEDICINE                                OFFICIAL PATIENT DOSSIER|
|  104 Symonds St, Auckland CBD · Tel: +64 9 300 1234 · info@dentia.clinic             |
+---------------------------------------------------------------------------------------+
| Patient: Johnathan Davis         Ref: DEN-2026-0042        DOB: 14-Aug-1988 (Age: 38)|
| Attending Doctor: Dr. Sarah J. Lee (NZDA Reg #88412)       Generated: 15-Sep-2026     |
+---------------------------------------------------------------------------------------+

1. 32-TOOTH ODONTOGRAM ANATOMICAL SUMMARY
   - Total Sound Teeth: 28/32 | Restored Teeth: 3 | Active Pathologies: 1
   - Tooth #14: Porcelain Crown [D2740] (Treated)
   - Tooth #19: Deep Occlusal Caries -> Molar Root Canal [D3330] (Active Plan)
   - Tooth #30: Composite Restoration [D2391] (Treated)

2. CONSULTATION & SPECIAL REQUESTS HISTORY
   - Date: Sep 15, 2026 · 10:30 AM · Operatory 1
   - Patient Note: "Patient reports acute sensitivity to cold liquids on lower left."
   - Attending Doctor Note: "Clinical examination confirmed caries on #19. Treatment plan updated."

3. ITEMIZED TREATMENT PROCEDURES & CDT CODES
   +------+----------------------------------------+--------+-----+------------+------------+
   | Code | Procedure Description                  | Tooth  | Qty | Unit Price | Total Fee  |
   +------+----------------------------------------+--------+-----+------------+------------+
   | 011  | Comprehensive Oral Examination         | All    | 1   | $85.00     | $85.00 NZD |
   | D3330| Molar Endodontic Therapy (Root Canal)  | #19    | 1   | $350.00    | $350.00 NZD|
   | D2391| Resin-Based Composite - One Surface    | #30    | 1   | $150.00    | $150.00 NZD|
   +------+----------------------------------------+--------+-----+------------+------------+
   |                                                    SUBTOTAL:          $585.00 NZD|
   |                                                    CLINICAL DISCOUNT:   $0.00 NZD|
   |                                                    TOTAL AMOUNT:      $585.00 NZD|
   +----------------------------------------------------------------------------------+

4. BILLING & FINANCIAL LEDGER SUMMARY
   - Invoice Reference: INV-2026-00042
   - Invoice Status: Partially Paid ($200.00 Paid via Online_Card · Balance: $385.00 Due)
   - Payment Receipts:
     * REC-2026-00042-01: $200.00 NZD (Paid 15-Sep-2026 via Visa ending in 4242)
     * Outstanding Balance: $385.00 NZD (Payable upon completion of RCT)

5. ATTENDING DOCTOR CLINICAL CERTIFICATION & SIGNATURE
   Doctor Signature: ___________________________      Date: 15-Sep-2026
+---------------------------------------------------------------------------------------+
```

### 6.2 Print & PDF Technical Implementation
- Implemented using the high-fidelity `@media print` CSS utility pattern in `printReportUtils.js`.
- Features:
  - Vector clinic logo and clean typography.
  - Page-break optimization (`page-break-inside: avoid;`).
  - Native browser print dialog (`window.print()`) that seamlessly produces vector PDFs with zero external library overhead.
  - High-resolution color badges indicating tooth conditions and payment settlement status.

---

## 7. Complete API Contracts & Schemas

### 7.1 Booking Appointment (Optional Treatment Plan)
* **Endpoint**: `POST /api/patient-portal/appointments`
* **Authorization**: Bearer Token (`Patient`)

#### Request Payload A: With Selected Treatments
```json
{
  "preferredDate": "2026-09-18T10:30:00Z",
  "reason": "Comprehensive Oral Examination & Consultation [Code: 011] (Dr. Sarah J. Lee)",
  "doctorID": 1,
  "paymentMethod": "Cash",
  "consultationFee": 85.00,
  "currency": "NZD",
  "cardLast4": null,
  "cardHolderName": null,
  "notes": "Patient experiencing intermittent pain when chewing on the left side.",
  "procedures": [
    {
      "procedureCode": "011",
      "procedureName": "Comprehensive Oral Examination & Consultation",
      "fee": 85.00,
      "category": "Examination & Diagnosis",
      "quantity": 1
    },
    {
      "procedureCode": "531",
      "procedureName": "Composite Filling & Tooth Restoration",
      "fee": 150.00,
      "category": "Fillings & Restorative Treatment",
      "quantity": 1
    }
  ]
}
```

#### Request Payload B: Without Treatments (Standalone Consultation)
```json
{
  "preferredDate": "2026-09-20T14:00:00Z",
  "reason": "General Dental Consultation (Dr. Tariq Mahmood)",
  "doctorID": 2,
  "paymentMethod": "Cash",
  "consultationFee": 2500.00,
  "currency": "PKR",
  "cardLast4": null,
  "cardHolderName": null,
  "notes": "Routine general checkup. No specific procedure chosen in advance.",
  "procedures": []
}
```

#### Response (200 OK):
```json
{
  "appointmentId": 142,
  "invoiceId": 98,
  "invoiceNumber": "INV-2026-00142",
  "paymentMethod": "Cash",
  "receiptOrVoucherNumber": "CSH-2026-00142",
  "invoiceStatus": "Pending Cash Settlement",
  "consultationFee": 85.00,
  "currency": "NZD",
  "notes": "Patient experiencing intermittent pain when chewing on the left side.",
  "message": "Appointment #142 confirmed! Cash voucher CSH-2026-00142 generated for invoice INV-2026-00142."
}
```

---

### 7.2 Updating Treatment Plan & Notes (Used by both Doctor & Patient)
* **Endpoint (Patient)**: `PUT /api/patient-portal/appointments/{id}/treatment-plan`
* **Endpoint (Doctor)**: `PUT /api/appointments/{id}/treatment-plan`
* **Authorization**: Bearer Token (`Patient` or `Doctor`)

#### Request Payload:
```json
{
  "procedures": [
    {
      "procedureCode": "011",
      "procedureName": "Comprehensive Oral Examination & Consultation",
      "fee": 85.00,
      "quantity": 1
    },
    {
      "procedureCode": "D3330",
      "procedureName": "Molar Endodontic Therapy (Root Canal)",
      "fee": 350.00,
      "quantity": 1
    }
  ],
  "notes": "Patient has severe cold sensitivity on #19. Doctor reviewed findings and scheduled root canal therapy."
}
```

#### Response (200 OK):
```json
{
  "message": "Treatment plan and invoice updated successfully.",
  "appointment": {
    "appointmentID": 142,
    "patientID": 26,
    "fullName": "Johnathan Davis",
    "preferredDate": "2026-09-18T10:30:00Z",
    "status": "Confirmed",
    "reason": "Comprehensive Oral Examination & Consultation [011], Molar Endodontic Therapy (Root Canal) [D3330] (Dr. Sarah J. Lee)",
    "notes": "Patient has severe cold sensitivity on #19. Doctor reviewed findings and scheduled root canal therapy.",
    "doctorID": 1,
    "totalAmount": 435.00,
    "currency": "NZD",
    "invoiceNumber": "INV-2026-00142",
    "invoiceStatus": "Pending Cash Settlement",
    "items": [
      {
        "procedureCode": "011",
        "description": "Comprehensive Oral Examination & Consultation",
        "quantity": 1,
        "unitPrice": 85.00,
        "totalPrice": 85.00
      },
      {
        "procedureCode": "D3330",
        "description": "Molar Endodontic Therapy (Root Canal)",
        "quantity": 1,
        "unitPrice": 350.00,
        "totalPrice": 350.00
      }
    ]
  }
}
```

---

### 7.3 Fetching Doctor Treatment Catalog & Fee Schedule
* **Endpoint**: `GET /api/treatment-pricing/doctor/{doctorId}`
* **Authorization**: Public / Authenticated

#### Response (200 OK):
```json
{
  "doctorId": 1,
  "doctorName": "Dr. Sarah J. Lee",
  "currency": "NZD",
  "categories": [
    "Examination & Diagnosis",
    "Preventive Dentistry",
    "Fillings & Restorative Treatment",
    "Crowns & Bridges",
    "Root Canal Treatment",
    "Extractions & Oral Surgery",
    "Dental Implants"
  ],
  "procedures": [
    {
      "procedureCode": "011",
      "procedureName": "Comprehensive Oral Examination & Consultation",
      "category": "Examination & Diagnosis",
      "estimatedDuration": "45 mins",
      "standardFee": 85.00,
      "description": "Comprehensive dental examination, ultrasonic scaling, plaque removal & polish."
    },
    {
      "procedureCode": "D3330",
      "procedureName": "Molar Endodontic Therapy (Root Canal)",
      "category": "Root Canal Treatment",
      "estimatedDuration": "60 mins",
      "standardFee": 350.00,
      "description": "Complete extirpation, pulp canal disinfection, and sterile root canal filling."
    }
  ]
}
```

---

## 8. Step-by-Step Implementation Roadmap

| Phase | Component | Key Actions & Changes | Files Involved |
| :--- | :--- | :--- | :--- |
| **Phase 1** | **Backend Repository & Models** | 1. Add `Notes` to `Appointment` model.<br>2. Update `BookPatientAppointmentWithPaymentAsync` to store clean `Notes`.<br>3. Fix `UpdateAppointmentTreatmentPlanAsync` regex to prevent string pollution.<br>4. Update `GetPatientAppointmentsAsync` and doctor appointments query to select `a.Notes`. | `DentistAPI/Models/DentalModels.cs`<br>`DentistAPI/Repositories/DentalRepository.cs`<br>`DentistAPIClone/.../DentalRepository.cs` |
| **Phase 2** | **Backend Controllers** | 1. Support `notes` in `UpdateTreatmentPlanRequest`.<br>2. Expose doctor treatment plan update endpoint `PUT /api/appointments/{id}/treatment-plan`.<br>3. Expose patient treatment plan update endpoint `PUT /api/patient-portal/appointments/{id}/treatment-plan`. | `DentistAPI/Controllers/AppointmentsController.cs`<br>`DentistAPI/Controllers/PatientPortalController.cs` |
| **Phase 3** | **Patient Portal Booking UI** | 1. Allow booking **WITHOUT** selecting procedures.<br>2. Add clean "Browse Doctor Treatment Catalog" modal.<br>3. Keep Notes separate from procedure names. | `Dentistfrontend/src/modules/patientPortal/pages/PatientBookAppointment.jsx` |
| **Phase 4** | **Patient Portal Appointments UI** | 1. Cleanly parse `appt.reason` into procedure badges and doctor name.<br>2. Render dedicated **"Patient Consultation Notes / Special Requests"** card.<br>3. In edit modal, pre-fill ONLY the clean notes string into the textarea.<br>4. Re-fetch and update local state upon saving. | `Dentistfrontend/src/modules/patientPortal/pages/PatientAppointments.jsx` |
| **Phase 5** | **Doctor Section Appointments UI** | 1. Show patient-demanded treatments badge and notes on appointment cards.<br>2. Provide chairside **"Edit Treatment Plan & Notes"** modal for doctors.<br>3. Sync notes and recalculated totals to invoice ledger. | `Dentistfrontend/src/pages/AppointmentsList.jsx`<br>`Dentistfrontend/src/components/AppointmentModal.jsx` |
| **Phase 6** | **Unified Patient Report Generator** | 1. Add **"💳 Treatment & Invoices"** tab in `ChartPage.jsx`.<br>2. Add print and PDF export utility for full patient clinical & invoice dossier.<br>3. Include 32-tooth odontogram findings, active treatment plans, invoices, and notes. | `Dentistfrontend/src/pages/ChartPage.jsx`<br>`Dentistfrontend/src/utils/printReportUtils.js` |

---

## 9. Verification & Quality Assurance Criteria

1. **Standalone Booking Verification**:
   - Verify a patient can book an appointment with 0 selected procedures and only custom notes.
   - Verify invoice creates a standard consultation line item ($85 NZD / Rs 2,500 PKR).
2. **Procedure-Rich Booking Verification**:
   - Verify a patient selecting 2 procedures (e.g. Exam + Filling) generates an invoice with 2 corresponding `InvoiceItems` matching the doctor's exact pricing.
3. **Doctor Edit & Recalculation Verification**:
   - Verify the doctor can add a 3rd procedure (e.g. Root Canal) and change notes.
   - Verify the invoice subtotal, balance due, and appointment reason update in SQL Server.
4. **Bi-Directional Notes Synchronization Verification**:
   - Edit notes from Patient Portal -> Refresh Doctor Section -> Verify exact note text appears.
   - Edit notes from Doctor Section -> Refresh Patient Portal -> Verify exact note text appears without string concatenation or duplicate `- Note:` prefixes.
5. **PDF & Print Verification**:
   - Trigger print/PDF from the Doctor Section.
   - Verify patient demographics, odontogram findings, itemized treatments, and invoices render cleanly without pagination breakage.

---
*End of Specification Document.*
