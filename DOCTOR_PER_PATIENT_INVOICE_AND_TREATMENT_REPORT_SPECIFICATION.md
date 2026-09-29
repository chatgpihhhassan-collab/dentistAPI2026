# Dentia Doctor Section: Per-Patient Invoice & Chart Treatment Report Specification

## 1. Executive Summary & Objective

This document defines the architecture, data flow, and user experience for accessing and generating **Per-Patient Invoice Reports with Chart & Treatment Details** directly within the **Doctor Section** of Dentia.

### The Problem
- Previously, detailed invoices and billing ledgers were primarily exposed to patients in the **Patient Portal** (`/portal/billing`).
- In the **Doctor Section** (`/chart/:patientId`, `/directory`, `/appointments`), doctors can view the 32-tooth odontogram chart, dictate AI notes, and view X-rays, but lacked a unified **Clinical Treatment & Financial Ledger** showing:
  1. What specific treatments have been performed or planned on which teeth (e.g. Tooth #14 Root Canal, Tooth #19 Composite Restoration, Tooth #8 Crown).
  2. What invoices were issued for those treatments for that specific patient.
  3. The itemized financial breakdown: Total Invoiced, Amount Settled, Balance Due, Currency (PKR / NZD), and Payment Methods.
  4. The ability for the doctor or receptionist to print an official **Patient Treatment & Tax Invoice Statement (PDF)** and record chairside cash/card settlements.

---

## 2. Architecture & Data Model Integration

The Per-Patient Treatment & Invoice Report unifies three core data sources into a single chairside view:

```
+-----------------------------------------------------------------------------------+
|                           PATIENT CLINICAL & FINANCIAL DOSSIER                    |
+-----------------------------------------------------------------------------------+
                                          |
      +-----------------------------------+-----------------------------------+
      |                                   |                                   |
      v                                   v                                   v
[1. ODONTOGRAM CHART]          [2. CLINICAL APPOINTMENTS]           [3. INVOICES & BILLING]
- 32 Teeth Status              - Reason / Consult Type              - Invoice # (INV-2026-XXXX)
- Pathologies (Caries, RCT)    - Treatment Items & Notes            - Line Items & CDT Codes
- Completed Treatments         - Attending Doctor (ID 1, 2, etc.)   - Unit Price, Tax, Discounts
- Planned Interventions        - Date & Timestamp                   - Paid Amount & Balance Due
- Surface / Quadrant           - Operatory Room                     - Payment Method (Cash, Card)
```

### Data Linkage Table

| Dimension | Database Source | Description in Report |
| :--- | :--- | :--- |
| **Patient Demographics** | `[dentist].[Patients]` | Name, Age, Gender, Ref # (`DEN-2026-XXXXX`), Phone, Current Treatment Plan |
| **Tooth Chart Treatments** | `[dentist].[PatientTeeth]` | Tooth #, Condition Status (`Treated`, `Caries`, `Root Canal`), Condition Color, Clinical Notes |
| **Consultation Treatments** | `[dentist].[Appointments]` | Consultation Reason, Items, Attending Doctor, Visit Date |
| **Invoices & Line Items** | `[dentist].[Invoices]` & `[InvoiceItems]` | Invoice Number, Issue Date, Due Date, Total Amount, Balance Amount, Status (`Paid`, `Pending`) |
| **Payment Receipts** | `[dentist].[Payments]` | Payment Date, Method (`Cash_Counter`, `Online_Card`), Transaction Ref, Receipt # |

---

## 3. User Experience & Doctor Workflow

### A. Primary Entry Point: Inside `ChartPage.jsx` (`/chart/:patientId`)

In the master clinical workspace header, directly alongside the existing clinical tabs:

```
[ 🩺 Dental Chart ]   [ 🧠 AI Notes ]   [ 🖼️ Imaging & X-Rays ]   [ 💳 Treatment & Invoices (NEW) ]
```

Clicking **"💳 Treatment & Invoices"** opens the dedicated full-width Clinical Financial Workspace with three intuitive sections:

#### 1. Financial Summary KPI Banner
- **Total Invoiced**: e.g., `$1,450.00 NZD` (or `Rs 95,000 PKR`)
- **Total Paid / Settled**: e.g., `$1,150.00 NZD` (with green status pill)
- **Outstanding Balance**: e.g., `$300.00 NZD` (highlighted in amber/rose if balance > 0)
- **Active Treatments**: e.g., `4 Teeth Treated • 1 Treatment Planned`
- **Actions**:
  - `🖨️ Print / Download Treatment & Invoice Statement (PDF)`
  - `+ Create New Invoice from Chart`

#### 2. Tab 1: Tooth-by-Tooth Chart & Treatment Breakdown
A clinical matrix mapping every affected tooth to its treatment and financial status:
- **Tooth Identifier**: Badge with tooth number and anatomical name (e.g. `Tooth #14 — Maxillary Right 1st Molar`).
- **Pathology / Diagnosis**: `Deep Occlusal Caries`, `Irreversible Pulpitis`, `Defective Margin`.
- **Procedure Performed**: `Root Canal Therapy [D3330]`, `Resin Composite Restoration [D2391]`, `Porcelain/Ceramic Crown [D2740]`.
- **Clinical Status**:
  - `Treated / Completed` (Emerald badge)
  - `Treatment Planned` (Amber badge)
  - `In Progress` (Blue badge)
- **Billed Status**: Linked Invoice Number (e.g. `INV-2026-0034`) or `Unbilled / Pending Invoice`.
- **Fee / Cost**: Standard fee with currency.

#### 3. Tab 2: Itemized Invoices & Receipts Ledger
An expandable ledger of all invoices issued for this patient:
- **Invoice Card Header**:
  - Invoice Number (e.g., `INV-2026-0042`) • Date: `Sep 15, 2026` • Attending Doctor: `Dr. Sarah J. Lee`
  - Status Pill: `Paid in Full` (green), `Pending Payment` (amber), `Overdue` (red)
- **Line Items Table**:
  - `#`, `Treatment / Procedure Description`, `CDT Code`, `Tooth #`, `Qty`, `Unit Price`, `Total`
- **Ledger Summary**:
  - Subtotal, Clinic Discounts, Total Invoiced, Amount Settled, Net Balance Due.
- **Payment History**:
  - Date, Payment Method (`Cash at Counter`, `POS Card Terminal`, `Online Patient Portal`), Receipt #.
- **Doctor Actions**:
  - **"Record Cash / Card Payment"**: Allows doctor/receptionist to record payment received at the clinic desk and instantly settle the invoice.
  - **"Download Invoice PDF"**: Generates official single invoice receipt.

---

### B. Secondary Entry Point: In Patient Directory (`/directory`)

On each patient row or card in the directory, an action button:
- `💳 Invoices & Treatment Report` $\rightarrow$ opens the quick modal view or navigates directly to `/chart/:patientId?tab=billing`.

---

## 4. Proposed Technical Implementation

### A. Backend API Changes (`DentistAPI` & `DentistAPIClone`)

#### 1. Endpoint: `GET /api/billing/patient/{patientId}/treatment-report`
- **Authorization**: Doctor or Admin token.
- **Controller**: `BillingController.cs` (or `PatientsController.cs`).
- **Response Payload**:
```json
{
  "patient": {
    "patientId": 36,
    "referenceNumber": "DEN-2026-66596",
    "fullName": "Ali Bajwa",
    "phone": "+92 300 1234567",
    "email": "ali.bajwa@example.com",
    "currentTreatmentPlan": "General Consultation",
    "currency": "PKR"
  },
  "summary": {
    "totalInvoiced": 85000.00,
    "totalPaid": 60000.00,
    "balanceDue": 25000.00,
    "invoiceCount": 2,
    "unpaidCount": 1,
    "currency": "PKR"
  },
  "chartTreatments": [
    {
      "toothNumber": 14,
      "conditionStatus": "Root Canal Completed",
      "conditionColor": "#7C3AED",
      "cdtCode": "D3330",
      "treatmentName": "Root Canal Therapy - Molar",
      "status": "Completed",
      "fee": 35000.00,
      "invoiceNumber": "INV-2026-0042",
      "date": "2026-09-15"
    },
    {
      "toothNumber": 19,
      "conditionStatus": "Caries / Restorative",
      "conditionColor": "#EF4444",
      "cdtCode": "D2391",
      "treatmentName": "Resin Composite - 1 Surface Posterior",
      "status": "Planned",
      "fee": 12000.00,
      "invoiceNumber": null,
      "date": "2026-09-15"
    }
  ],
  "invoices": [
    {
      "invoiceId": 42,
      "invoiceNumber": "INV-2026-0042",
      "issueDate": "2026-09-15T10:00:00Z",
      "dueDate": "2026-09-22T10:00:00Z",
      "doctorName": "Dr. Jhangir Ahmed",
      "totalAmount": 35000.00,
      "balanceAmount": 0.00,
      "status": "Paid",
      "currency": "PKR",
      "items": [
        {
          "description": "Root Canal Therapy - Tooth #14",
          "procedureCode": "D3330",
          "quantity": 1,
          "unitPrice": 35000.00,
          "totalPrice": 35000.00
        }
      ],
      "payments": [
        {
          "receiptNumber": "REC-2026-0081",
          "amount": 35000.00,
          "paymentMethod": "Cash_Counter",
          "paymentDate": "2026-09-15T10:45:00Z"
        }
      ]
    }
  ]
}
```

#### 2. Endpoint: `POST /api/billing/invoices/{id}/record-payment`
- Allows recording clinic payments (Cash, Card, Cheque) to settle an invoice.
- Updates invoice `BalanceAmount` and sets `Status = 'Paid'` when balance reaches 0.

---

### B. Frontend Components (`Dentistfrontend`)

1. **`PatientTreatmentInvoiceTab.jsx`**:
   - High-fidelity component embedded directly in `ChartPage.jsx` when `activeTab === 'billing'`.
   - Displays Demographics, KPI Cards, Tooth Treatment Matrix, and Invoices Ledger.
2. **`PatientTreatmentInvoiceModal.jsx`**:
   - Modal version accessible from `PatientDirectory.jsx` or `AppointmentsList.jsx` for quick access without switching away.
3. **`PatientTreatmentReportPdf.js`**:
   - Uses `jspdf` and `jspdf-autotable` (already installed in the frontend) to generate an official clinic statement including clinic logo, patient information, odontogram findings, treatment history, and invoice ledger.

---

## 5. Verification & Testing Plan

1. **API Validation**:
   - Test `GET /api/billing/patient/{id}/treatment-report` for patients with and without existing invoices.
   - Verify multi-currency detection (`PKR` for Dr. Jhangir / Pakistan region, `NZD` for New Zealand clinics).
2. **UI Verification**:
   - Navigate to `/chart/:patientId` $\rightarrow$ click **"Treatment & Invoices"** tab.
   - Verify tooth treatment badges match the active odontogram.
   - Test "Record Payment" flow and verify balance updates in real-time.
   - Test "Download PDF Statement" and verify layout and formatting.
3. **Build & Deployment**:
   - Run `npm run build` in `Dentistfrontend`.
   - Sync `dist/*` to `DentistAPIClone\API_dentist\wwwroot`.
   - Push to GitHub (`main`) and Azure DevOps (`Local`).
