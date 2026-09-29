# Complete Clinical & Technical Manual: Patient Portal Treatment Selection, Doctor Plan Management, Bi-Directional Notes Sync & Per-Patient Reporting

> **Document Title**: Patient-Doctor Treatment Lifecycle, Financial Ledger & Real-Time Sync Specification  
> **File Path**: `PATIENT_PORTAL_AND_DOCTOR_TREATMENT_SYNC_MANUAL.md`  
> **Target Audience**: Dental Surgeons, Clinic Administrative Staff, and Software Engineering Team  
> **Systems Covered**: `Dentistfrontend` (Vercel React SPA) & `DentistAPI` (.NET 8 + SQL Server)  

---

## 1. Executive Overview & Lifecycle Flow

This manual provides an exhaustive, step-by-step breakdown of how dental treatments, pricing, clinical notes, and financial reports flow between the **Patient Portal** and the **Doctor Workspace**.

```
+───────────────────────────────────────────────────────────────────────────────────────────────────+
|                                    COMPLETE CLINICAL & FINANCIAL LOOP                             |
+───────────────────────────────────────────────────────────────────────────────────────────────────+

  [PATIENT PORTAL]                                                      [DOCTOR WORKSPACE]
  ┌──────────────────────────────────────────────┐                      ┌──────────────────────────────────────────────┐
  │ 1. Browse Doctor Catalog & Fee Schedule      │                      │ 1. Appointments Board (/appointments)        │
  │    - 15 Dental Categories with Live Prices   │                      │    - "Patient-Demanded Treatments" Badge     │
  │    - PKR (Dr. Tariq/Jhangir) vs NZD (Dr. Lee)│                      │    - Amber Callout: Patient Notes            │
  │                                              │                      │                                              │
  │ 2. Book Appointment (/portal/book)           │                      │ 2. Chairside Odontogram (/chart/:patientId)  │
  │    - Mode A: Standalone Consultation         │                      │    - 32-Tooth Anatomical Chart Status        │
  │      (NO treatment plan required)            │                      │    - "💳 Treatment & Invoices" Tab           │
  │    - Mode B: Select Specific Procedures      │                      │                                              │
  │    - Enters "Consultation Notes / Requests"  │                      │ 3. Doctor Modifies Treatment Plan            │
  │                                              │                      │    - Adds Chairside Procedures from Catalog  │
  │ 3. View Invoices & Receipts (/portal/billing)│                      │    - Removes / Adjusts Prices & Discounts    │
  │    - Itemized breakdown & payment history    │                      │    - Updates Doctor Clinical Comments        │
  └──────────────────────┬───────────────────────┘                      └──────────────────────┬───────────────────────┘
                         │                                                                     │
                         │                   BI-DIRECTIONAL SYNCHRONIZATION                    │
                         │◄───────────────────────────────────────────────────────────────────►│
                         │  • Patient Notes & Doctor Comments sync via [Appointments].Notes    │
                         │  • Invoices & Line Items recalculate in real time                   │
                         │  • 1-Click Comprehensive Patient Dossier Report (PDF & Print)       │
                         ▼                                                                     ▼
```

---

## 2. Patient Portal: Treatment Selection, Pricing & Optional Booking

### 2.1 How Patients View Their Assigned Doctor's Fee Schedule Easily on the Frontend
Patients do not need to guess treatment prices or wait for their clinical visit. They can explore the doctor's full fee schedule anytime:

1. **Entry Point 1 — Dedicated Catalog Explorer in Booking Flow (`/portal/book`)**:
   - In Step 2 of the booking stepper, a prominent **"📖 Browse Full Doctor Fee Schedule"** button is available.
   - Clicking it opens an interactive modal listing all procedures for the selected doctor.
2. **Entry Point 2 — Quick Services Link on Patient Dashboard (`/portal/dashboard`)**:
   - Patients can click **"Explore Doctor Services & Fees"** to view standard procedures.
3. **Data Retrieval Mechanism**:
   - When the patient selects a doctor (e.g. `DoctorID: 2` Dr. Jhangir Ahmed or `DoctorID: 1` Dr. Sarah J. Lee), the frontend queries:
     ```http
     GET /api/treatment-pricing/doctor/{doctorId}
     ```
   - The response delivers:
     - `doctorId`: ID of the specialist.
     - `currency`: Automatically regionalized (`PKR` for Dr. Jhangir Ahmed / Dr. Tariq Mahmood; `NZD` for Dr. Sarah J. Lee).
     - `categories`: 15 standard dental categories (Examination, Restorative, Endodontics, Implants, etc.).
     - `procedures`: Array containing `procedureCode`, `procedureName`, `category`, `estimatedDuration` (e.g. `30 mins`, `60 mins`), and `standardFee`.

---

### 2.2 Optional Treatment Plan: Standalone Consultation vs. Multi-Procedure Booking
**Treatment selection is never mandatory for patients.** In Step 2 of `/portal/book`, patients are given two distinct options:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: CHOOSE CONSULTATION & TREATMENT PREFERENCE                                              │
├─────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                 │
│  ○ OPTION A: General Consultation Only (No Specific Treatment Plan Selected)                    │
│    "I want to see the doctor for an initial clinical examination, checkup, or diagnosis first." │
│    • Treatment list remains empty: []                                                           │
│    • Standard consultation fee applied ($85.00 NZD / Rs 2,500 PKR)                              │
│    • Booking reason: "General Dental Consultation (Dr. Sarah J. Lee)"                           │
│                                                                                                 │
│  ◉ OPTION B: Select Specific Procedures from Doctor's Fee Schedule                              │
│    "I know what treatment I need (e.g., Scaling & Polish, Cavity Filling, Tooth Extraction)."   │
│    • Filter by category: [All] [Preventive] [Restorative] [Root Canal] [Extractions]            │
│    • Search bar: "Type procedure name or code (e.g. 531, D3330)..."                            │
│    • Multi-selection checkboxes with real-time fee accumulator                                  │
│                                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────────────────────┘
```

#### What Happens When Patient Selects Treatments (Pricing Occurrence in Appointment):
1. **Dynamic Price Accumulator**:
   As the patient selects procedures, the total consultation fee recalculates instantly:
   $$\text{Total Fee} = \sum (\text{Procedure Fee} \times \text{Quantity})$$
2. **Payload Sent to API (`POST /api/patient-portal/appointments`)**:
   ```json
   {
     "preferredDate": "2026-09-18T10:30:00Z",
     "reason": "Comprehensive Oral Examination [011], Composite Filling [531] (Dr. Sarah J. Lee)",
     "notes": "Severe cold sensitivity on lower left molar.",
     "doctorID": 1,
     "paymentMethod": "Cash",
     "consultationFee": 235.00,
     "currency": "NZD",
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
3. **Database Occurrence**:
   When the backend repository receives this payload:
   - **`[dentist].[Appointments]`**: Creates appointment row with `Reason`, `DoctorID`, and `Notes = "Severe cold sensitivity on lower left molar."`.
   - **`[dentist].[Invoices]`**: Generates invoice `INV-2026-XXXXX` with `SubTotal = 235.00`, `TotalAmount = 235.00`, `BalanceAmount = 235.00` (or `0.00` if paid online via card), and `Currency = "NZD"`.
   - **`[dentist].[InvoiceItems]`**: Creates 2 itemized records:
     - Item 1: `Code: 011`, `Description: Comprehensive Oral Examination`, `UnitPrice: 85.00`
     - Item 2: `Code: 531`, `Description: Composite Filling & Tooth Restoration`, `UnitPrice: 150.00`
   - **`[dentist].[Payments]`**: Generates cash voucher `CSH-2026-XXXXX` or payment receipt `REC-2026-XXXXX`.

---

## 3. Doctor Section: Demand Review, Plan Modification & Chairside Editing

### 3.1 How the Doctor Sees What Treatments the Patient Demanded
When the doctor logs into the clinical workspace:

#### A. In Appointments Management (`/appointments`):
- **Patient-Demanded Treatments Badge**:
  Each appointment card displays whether the patient booked a **General Consultation** or requested specific procedures:
  - Example: `[ 🦷 2 Procedures Demanded • $235.00 NZD ]`
- **Patient Special Requests / Consultation Notes Callout**:
  A prominent gold/amber card displays the patient's exact notes:
  - Example: `📝 Patient Notes / Special Requests: "Severe cold sensitivity on lower left molar."`

#### B. In Chairside Odontogram (`/chart/:patientId`):
- Header banner displays active booking details, attending doctor, and patient notes.
- The new **"💳 Treatment & Invoices"** tab displays all scheduled items alongside historical treatments.

---

### 3.2 How the Doctor Edits the Treatment Plan and Adds Treatments
Doctors frequently need to modify the treatment plan after conducting their physical chairside examination (e.g., patient booked a routine cleaning, but doctor discovered deep occlusal caries requiring a root canal).

```
[Doctor clicks "✏️ Edit Treatment Plan & Notes" on Appointment or Chart]
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│ DOCTOR CHAIRSIDE TREATMENT PLAN EDITOR MODAL                                                │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│ Patient: Johnathan Davis (DEN-2026-0042) · Attending Doctor: Dr. Sarah J. Lee              │
│                                                                                             │
│ 1. CURRENT SCHEDULED PROCEDURES:                                                            │
│    [011] Comprehensive Oral Examination             $85.00 NZD   [✕ Remove]                │
│    [531] Composite Filling - Tooth #19              $150.00 NZD  [✕ Remove]                │
│                                                                                             │
│ 2. + ADD CHAIRSIDE PROCEDURE FROM DOCTOR CATALOG:                                           │
│    Category: [Root Canal Treatment ▾]                                                       │
│    Procedure: [D3330 - Molar Endodontic Therapy ($350.00 NZD) ▾]  [+ Add to Plan]            │
│                                                                                             │
│ 3. RECALCULATED FINANCIAL SUMMARY:                                                          │
│    New Total Amount: $585.00 NZD (Invoice INV-2026-00142 updated automatically)             │
│                                                                                             │
│ 4. SYNCHRONIZED CONSULTATION NOTES & SPECIAL REQUESTS:                                      │
│    ┌───────────────────────────────────────────────────────────────────────────────────┐    │
│    │ Patient Note: Severe cold sensitivity on lower left molar.                        │    │
│    │ Doctor Update: Verified deep caries on #19 with pulpal involvement. RCT initiated. │    │
│    └───────────────────────────────────────────────────────────────────────────────────┘    │
│    [✓ Sync updated comments to Patient Portal]                                              │
│                                                                                             │
│             [Cancel]                        [💾 Save & Synchronize Treatment Plan]          │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

#### Technical Action on Save (`PUT /api/appointments/{id}/treatment-plan`):
1. **Updates `[dentist].[Appointments]`**:
   - `Reason` updated to: `"Comprehensive Oral Examination [011], Composite Filling [531], Molar Endodontic Therapy [D3330] (Dr. Sarah J. Lee)"`.
   - `Notes` updated to: `"Patient Note: Severe cold sensitivity on lower left molar. Doctor Update: Verified deep caries on #19 with pulpal involvement. RCT initiated."`.
2. **Synchronizes `[dentist].[Invoices]` & `[InvoiceItems]`**:
   - Deletes old `InvoiceItems` for this appointment.
   - Inserts 3 new `InvoiceItems` matching the updated procedures.
   - Updates `Invoices.SubTotal` and `Invoices.TotalAmount` to `$585.00`.
   - Recalculates `Invoices.BalanceAmount` based on previously paid deposits.

---

## 4. Bi-Directional Synchronization of "Patient Consultation Notes / Special Requests"

### 4.1 The Mechanism
Previous versions suffered from string concatenation bugs where notes would duplicate (e.g. `- Note: test - Note: test`) because notes were stored as a composite suffix inside the `Reason` string.

**The Architectural Solution:**
- **Isolated Storage**: Notes are saved directly into the dedicated `Notes NVARCHAR(MAX)` column on `[dentist].[Appointments]`.
- **Clean Suffix Prevention**: `DentalRepository.cs` cleanly strips any legacy `- Note:` or composite strings before updating.
- **Immediate Push**:
  - When the doctor types comments in `/appointments` or `/chart/:patientId`, `Notes` is updated.
  - When the patient logs into `/portal/appointments`, the query loads `a.Notes`. The patient immediately sees the doctor's notes and clinical advice.
  - When the patient edits their special requests before the visit, the doctor immediately sees the new text on their screen.

---

## 5. Per-Patient Treatment Plans, Invoices & Full Dossier Report

### 5.1 How the Doctor Views the Per-Patient Treatment & Invoice Report
The doctor accesses the unified report inside `ChartPage.jsx` (`/chart/:patientId`) via the 4th tab:

```
[ 🩺 Dental Chart ]   [ 🧠 AI Notes ]   [ 🖼️ Imaging & X-Rays ]   [ 💳 Treatment & Invoices ]
```

Clicking **"💳 Treatment & Invoices"** loads the full dossier containing:

#### 1. Executive KPI Summary Banner:
- **Total Invoiced**: e.g., `$1,450.00 NZD` (or `Rs 125,000 PKR`).
- **Amount Settled / Paid**: e.g., `$1,150.00 NZD` (green badge).
- **Outstanding Balance Due**: e.g., `$300.00 NZD` (highlighted in rose/amber).
- **Active Treatments**: e.g., `4 Teeth Treated • 1 Treatment Planned`.
- **Quick Action**: `🖨️ Print / Download Treatment & Invoice Statement (PDF)`.

#### 2. Tab 1: Tooth-by-Tooth Treatment Matrix:
Maps every tooth from `[dentist].[TeethState]` to its clinical and financial status:
| Tooth Identifier | Diagnosis / Pathology | Procedure Performed | CDT Code | Clinical Status | Linked Invoice # | Fee |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **#14 — Maxillary R 1st Molar** | Irreversible Pulpitis | Root Canal Therapy | `D3330` | `Treated` (Emerald) | `INV-2026-0042` | $350.00 |
| **#19 — Mandibular L 1st Molar**| Deep Occlusal Caries | Composite Restoration | `D2391` | `Planned` (Amber) | `Unbilled` | $150.00 |
| **#8 — Maxillary R Central Inc** | Defective Crown Margin | Ceramic Crown | `D2740` | `In Progress` (Blue)| `INV-2026-0051` | $650.00 |

#### 3. Tab 2: Itemized Invoices & Payments Ledger:
- Expandable cards for all invoices issued to this patient.
- Shows line items, CDT codes, quantities, and unit prices.
- Shows payment receipts (`REC-2026-XXXXX` or cash counter settlements).
- **Chairside Payment Settlement Modal**: Doctor or front desk clicks **"Record Payment"** to settle cash, POS card, or bank transfer directly at chairside.

---

### 5.2 How the Doctor Generates the Full Patient Report (PDF & Print)
Clicking **"Print / Download Treatment & Invoice Statement"** invokes `handlePrintCompletePatientReport`:
1. Generates an official clinic letterhead statement with Dentia branding and address.
2. Injects patient demographics (Name, Ref #, DOB, Gender, Phone, Email).
3. Injects the 32-tooth anatomical chart summary (sound teeth, treated teeth, planned interventions).
4. Injects consultation history and synchronized doctor comments.
5. Injects itemized CDT procedure fees and financial ledger (subtotal, paid amount, balance due).
6. Injects attending doctor certification and signature block.
7. Opens the browser's native print preview dialog, enabling 1-click **Save as PDF** or physical printing.

---

### 5.3 How the Patient Views Their Treatment & Invoice Details
In the Patient Portal:
1. **`/portal/appointments`**:
   - Lists all scheduled procedures with individual prices and codes.
   - Displays the synchronized doctor comments card.
2. **`/portal/billing`**:
   - Lists all issued invoices with status (`Paid`, `Pending Cash Settlement`, `Unpaid`).
   - Line items table showing exact procedures billed.
   - Payment history showing receipt numbers and dates.
   - Option to pay outstanding balances online via card or generate cash vouchers for reception.

---

## 6. Database Schema & API Contracts Summary

### 6.1 Database Schema References

```sql
-- Appointments Table
[dentist].[Appointments] (
    AppointmentID   INT IDENTITY(1,1) PRIMARY KEY,
    PatientID       INT NOT NULL,
    DoctorID        INT NULL,
    PreferredDate   DATETIME2 NOT NULL,
    Status          NVARCHAR(50) DEFAULT 'Confirmed',
    Reason          NVARCHAR(500) NULL,             -- Procedure names & doctor attribution
    Notes           NVARCHAR(MAX) NULL,             -- Bi-directionally synced patient/doctor notes
    CreatedAt       DATETIME DEFAULT GETDATE()
);

-- Invoices Table
[dentist].[Invoices] (
    InvoiceID       BIGINT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber   NVARCHAR(50) NOT NULL UNIQUE,   -- INV-2026-XXXXX
    PatientID       INT NOT NULL,
    DoctorID        INT NULL,
    AppointmentID   INT NULL,
    IssueDate       DATE NOT NULL,
    DueDate         DATE NOT NULL,
    SubTotal        DECIMAL(18,2) NOT NULL,
    TotalAmount     DECIMAL(18,2) NOT NULL,
    PaidAmount      DECIMAL(18,2) DEFAULT 0.00,
    BalanceAmount   DECIMAL(18,2) DEFAULT 0.00,
    Status          NVARCHAR(50) DEFAULT 'Unpaid',  -- 'Paid', 'Pending Cash Settlement', 'Unpaid'
    Currency        NVARCHAR(10) DEFAULT 'NZD',     -- 'PKR' or 'NZD'
    Notes           NVARCHAR(500) NULL
);

-- Invoice Line Items Table
[dentist].[InvoiceItems] (
    InvoiceItemID   BIGINT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID       BIGINT NOT NULL FOREIGN KEY REFERENCES [dentist].[Invoices](InvoiceID),
    ProcedureCode   NVARCHAR(50) NULL,              -- e.g. '011', '531', 'D3330'
    Description     NVARCHAR(255) NOT NULL,
    Quantity        INT DEFAULT 1,
    UnitPrice       DECIMAL(18,2) NOT NULL,
    TotalPrice      AS (Quantity * UnitPrice)
);

-- Payments / Receipts Table
[dentist].[Payments] (
    PaymentID       BIGINT IDENTITY(1,1) PRIMARY KEY,
    PaymentReceiptNo NVARCHAR(50) NOT NULL,         -- REC-2026-XXXXX
    InvoiceID       BIGINT NOT NULL,
    PatientID       INT NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    PaymentMethod   NVARCHAR(50) NOT NULL,          -- 'Online_Card', 'Cash_Counter', 'POS_Card'
    PaymentStatus   NVARCHAR(50) DEFAULT 'Success',
    PaymentDate     DATETIME2 DEFAULT SYSUTCDATETIME()
);
```

### 6.2 Key API Endpoints Reference

| Method | Endpoint | Authorized Roles | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/treatment-pricing/doctor/{doctorId}` | Public / Patient | Returns doctor's 15-category fee schedule and currency. |
| `POST`| `/api/patient-portal/appointments` | Patient | Books appointment with optional procedures and clean notes. |
| `GET` | `/api/patient-portal/appointments` | Patient | Returns patient's bookings with itemized procedures and synced notes. |
| `PUT` | `/api/patient-portal/appointments/{id}/treatment-plan` | Patient | Allows patient to update scheduled procedures and notes prior to visit. |
| `GET` | `/api/appointments?doctorId={id}` | Doctor | Returns doctor's appointments with procedure badges and patient notes. |
| `PUT` | `/api/appointments/{id}/treatment-plan` | Doctor | Allows doctor to modify treatments, prices, and comments chairside. |
| `GET` | `/api/billing/patient/{patientId}/treatment-report` | Doctor / Admin | Delivers full clinical & financial dossier (teeth + invoices + payments). |
| `POST`| `/api/billing/invoices/{id}/record-payment` | Doctor / Reception | Records chairside settlement (cash, POS card) and marks invoice Paid. |

---

## 7. Step-by-Step Implementation Verification Checklist

- [x] **Database Schema**: `Notes NVARCHAR(MAX)` verified and added to `[dentist].[Appointments]`.
- [x] **Fee Schedule Explorer**: Patient can query and view doctor pricing grouped by 15 dental categories.
- [x] **Optional Treatment Booking**: Patient can book standalone consultation ($85 NZD / Rs 2,500 PKR) without choosing procedures, OR select specific treatments.
- [x] **Doctor Demand Visibility**: Doctor sees demanded procedures badge, total cost, and patient notes on appointment cards.
- [x] **Doctor Treatment Plan Modification**: Doctor can add/remove procedures, recalculate invoice totals, and update clinical comments.
- [x] **Bi-Directional Notes Sync**: Notes persist in `[Appointments].Notes` and sync seamlessly between Patient Portal and Doctor Section without string duplication.
- [x] **Per-Patient Treatment & Invoice Report**: Doctor can open `[ 💳 Treatment & Invoices ]` in `ChartPage.jsx` to view tooth-by-tooth matrix, invoices ledger, record chairside payments, and print official PDF dossiers.
- [x] **Patient Transparency**: Patient views itemized invoices, receipts, and doctor comments in `/portal/billing` and `/portal/appointments`.

---
*End of Manual Document.*
