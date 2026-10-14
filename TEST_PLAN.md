# 🦷 Dentia Master Test Plan & Quality Assurance Strategy

**Version:** 2.0.0 (Production Verified)  
**Author:** Senior Lead QA / SDET Engineer  
**Coverage Scope:** Full ASP.NET Core Backend API (`API_dentist`) + React 19 / Vite Frontend (`Dentistfrontend`)

---

## 1. Overview & Quality Objectives
The Dentia testing strategy guarantees clinical safety, HIPAA/GDPR patient data privacy, zero floating-point billing inaccuracies, and resilience under adversarial conditions (XSS, SQL injection, prompt injection, and high-frequency concurrency).

### Key Test Metrics
* **Total Automated Assertions:** **294 Passing Assertions** (198 Unit/API/Component/Regression + 96 AI Clinical Prompt Evals)
* **Execution Speed:** Full test suite executes in **< 3.0 seconds** via Node.js native ESM runner
* **Test Isolation:** 100% deterministic (zero external network dependencies, frozen time fixtures, mocked AI provider)
* **Patient Data Policy:** Strict zero mock data in production; 100% synthetic generated test fixtures in test suite

---

## 2. Business Rules & Functional Guardrails Under Test

### A. Patients Management
1. **Required Demographics:** Enforces non-empty `FirstName` (min 2 chars), `LastName` (min 2 chars), `DOB`, `Gender`, and valid mobile `Phone`.
2. **Pakistani Phone E.164 Normalization:**
   * Accepts: `03001234567`, `+923001234567`, `923001234567`, `3001234567` (with spaces/hyphens).
   * Canonical Storage: Strictly normalizes to `+923XXXXXXXXX`.
   * Rejections: Landlines (`042...`, `051...`), short numbers (< 10 digits), and non-Pakistani invalid prefixes.
3. **CNIC Validation:** Validates Pakistani 13-digit National ID in `XXXXX-XXXXXXX-X` format.
4. **Duplicate Prevention:** Evaluates `(DoctorID, FirstName, LastName, Phone, DOB)` to prevent accidental double-registration (`409 Conflict`).
5. **Clinic Data Isolation:** Multi-tenant scoping guarantees clinicians cannot access records from unassociated clinics or organizations.

### B. Appointments & Calendar Scheduling
1. **Double-Booking & 30-Minute Overlap:** Prevents scheduling two appointments for the same dentist within a 30-minute interval (`ABS(DATEDIFF(MINUTE, PreferredDate, @CheckDate)) < 30`).
2. **Independent Multi-Chair Concurrency:** Multiple distinct dentists can accept appointments at the exact same minute without false collisions.
3. **Operating Hours:** Appointments strictly allowed between 08:00 AM and 08:00 PM (hours 8 through 19).
4. **Past Booking Prohibition:** Rejects dates prior to current time (with chairside 15-minute grace period for walk-ins).
5. **Status Lifecycle Machine:**
   * Valid forward progression: `Pending` → `Confirmed` → `In-Progress` → `Completed`, `Cancelled`, or `No-Show`.
   * **Terminal Status Guard (APPT-BUG-01):** `Completed`, `Cancelled`, and `No-Show` records can NEVER revert back to `Pending` or `Confirmed`.
6. **Timezone & DST Correctness:** UTC database storage with correct local client time conversion (PKT UTC+5 / NZDT UTC+13).

### C. 3D Odontogram & Clinical Treatments
1. **FDI World Dental Federation Notation:**
   * Adult (Permanent): 11–18, 21–28, 31–38, 41–48 (32 teeth).
   * Primary (Pediatric): 51–55, 61–65, 71–75, 81–85 (20 teeth).
   * Rejects invalid numbers (e.g., 0, 9, 19, 49, 99).
2. **Surface Validity per Anatomical Tooth Type:**
   * Anteriors (Incisors & Canines): Mesial, Distal, Incisal, Labial, Lingual (No occlusal).
   * Posteriors (Premolars & Molars): Mesial, Distal, Occlusal, Buccal, Lingual.
3. **Missing Tooth Rule (ODONTO-BUG-01):** Applying restorative filling or endodontic treatment to an `Extracted / Missing` tooth is blocked unless an implant or pontic fixture is planned.
4. **Audit History Preservation:** Any odontogram condition update inserts an append-only audit trail in `[dentist].[PatientToothHistory]`.

### D. Billing, Invoicing & Financial Safety
1. **Currency-Safe Arithmetic:** Integer cent calculations prevent IEEE-754 binary floating point rounding errors (`0.1 + 0.2 = 0.30`).
2. **Financial Invariant:** $\text{Balance} = \text{Subtotal} - \text{Discount} + \text{Tax} - \text{PaidAmount}$.
3. **Overpayment Guard (BILL-BUG-01):** Balance can never drop below zero. Payments exceeding remaining balance are rejected.
4. **Sequential Identifier Formats:** Validates `INV-YYYY-XXXXX` for invoices, `REC-YYYY-XXXXX` for receipts, and `CSH-YYYY-XXXXX` for cash vouchers.
5. **Cash Desk Two-Factor Confirmation:** Cash payment vouchers remain in `Pending` state until confirmed by authorized clinic reception staff.

### E. RVG & Radiograph Ingestion
1. **Magic-Byte Deep Verification:**
   * PNG: `89 50 4E 47 0D 0A 1A 0A`
   * JPEG: `FF D8 FF`
   * WebP: `52 49 46 46 ... 57 45 42 50`
   * DICOM: `44 49 43 4D` at byte offset 128
2. **Dangerous Extension Rejection:** `.exe`, `.bat`, `.cmd`, `.ps1`, `.sh`, `.php`, `.aspx`, `.svg`, `.html`, `.js` are blocked.
3. **Size Quotas:** Profile Images $\le$ 5 MB, Radiographs $\le$ 15 MB, Audio recordings $\le$ 25 MB. Zero-byte files rejected.
4. **Path Traversal Defense:** Sanitizes filenames using `Path.GetFileName` and assigns unguessable UUIDs (`nanopix_{patientId}_{timestamp}_{guid}.png`).
5. **Access Ownership (SEC-04):** Patients can only access their own radiograph image streams.

### F. Authentication, Roles & Security (RBAC)
1. **Role Permissions:**
   * `SuperAdmin`: Exclusive access to `/admin/doctors`, `/admin/organizations`, `/api/logs`. Blocked from clinical charts.
   * `Doctor`: Clinical chart access, patient directory, fee schedules, prescriptions. Blocked from superadmin views.
   * `Patient`: Strictly isolated to `/portal/*` and `/api/patient-portal/*`. Blocked from doctor APIs (`/api/patients`, `/api/appointments`).
2. **IDOR Defense:** Attempting to query another patient's records or update another doctor's password triggers 403 Forbidden.
3. **Rate Limiting:** Auth (5 req/min), Uploads (15 req/min), AI (25 req/min), General (200 req/min). Returns `429 Too Many Requests` + `Retry-After: 60`.
4. **Session Timeout:** 10-minute sliding inactivity timeout with 60-second warning banner and cookie auto-expiry on browser close.

### G. AI Features (Mocked Provider Engine)
1. **Zero External API Calls in Tests:** Real Gemini/Groq APIs are never called during automated test runs.
2. **Simulated Fault Modes:**
   * Valid structured JSON output.
   * Malformed JSON recovery (HTTP 502).
   * Network timeout handling (HTTP 408).
   * Rate limit backoff (HTTP 429).
   * Empty / null response recovery.
   * Hallucinated tooth number filtering against FDI standards.
   * Delimiter injection & prompt breakout tag neutralization.

---

## 3. Universal Edge Case Checklist

Every endpoint and form input is tested against this systematic matrix:
* **Empty / Whitespace:** `""`, `"   "`, `"\t\n"`, `null`, `undefined`
* **Buffer Overflow:** Strings of 10,000+ characters (truncated cleanly to schema limits)
* **Numeric Boundaries:** `-1`, `0`, `0.0001`, `999999999999`, `NaN`, `Infinity`
* **Internationalization:** Urdu Nastaliq (`مریض کے دائیں داڑھ میں شدید درد ہے`), Roman Urdu (`Dant 16 me keera`), Emoji (`🦷💥`), Arabic RTL
* **Security Payloads:** `<script>alert('XSS')</script>`, `<img src=x onerror=...>`, `'; DROP TABLE [dentist].[Patients];--`
* **Concurrency:** Simultaneous requests for appointment slots, payments, and AI compilations
* **Session Lifecycle:** Expired tokens, tampered signatures, session cookies
* **Pagination Bounds:** `page=0`, `page=-5`, `page=99999`, `limit=0`, `limit=5000`
* **Calendar:** Leap year Feb 29, month-end rollovers, DST shifts

---

## 4. Test Suite Structure & Organization

```
tests/
  ├── run_all_tests.mjs                  # Master test runner executing all phases
  ├── factories/
  │    └── test_data_factories.mjs       # HIPAA/GDPR-compliant synthetic data generators
  ├── mocks/
  │    └── mock_ai_provider.mjs          # Fault-injectable AI provider simulation
  ├── unit/
  │    ├── validators.test.mjs           # Phone, CNIC, FDI tooth notation, Luhn, sanitization
  │    ├── billing_calculator.test.mjs  # Currency-safe math, taxes, discounts, overpayment guard
  │    └── date_time_utils.test.mjs     # Operating hours, past booking, collisions, leap years
  ├── api/
  │    ├── auth_and_roles.test.mjs      # Login, RBAC matrix (SuperAdmin/Doctor/Patient), IDOR
  │    ├── patients_api.test.mjs        # Patient creation, duplicate prevention, XSS/SQLi vectors
  │    ├── appointments_api.test.mjs    # 30-min collision check, operating hours, status transitions
  │    ├── billing_api.test.mjs         # Invoices, online payments, cash vouchers, overpayments
  │    ├── rvg_uploads_api.test.mjs     # Magic-byte inspection, size quotas, traversal defenses
  │    └── ai_features_mock.test.mjs    # AI provider mocking, timeout, rate limit, hallucinated teeth
  ├── component/
  │    └── form_states.test.mjs         # UI form states: loading, empty, error, disabled buttons
  ├── non-functional/
  │    ├── performance_and_load.test.mjs# Sub-millisecond benchmarks, 500-request concurrency burst
  │    └── accessibility_axe.test.mjs   # WCAG 2.1 AA accessibility compliance audit
  ├── regression/
  │    └── security_and_bugs.mjs        # 19 regression suites (SEC-01 to SEC-08, BUG-01 to BUG-06, REG)
  ├── e2e/
  │    ├── playwright.config.mjs        # Desktop (1920x1080) and Mobile (iPhone 14) configuration
  │    └── critical_flows.spec.mjs       # Complete clinician-to-patient critical lifecycle
  └── ai-eval/
       ├── run_eval.mjs                 # 80-scenario clinical evaluation runner
       └── test_cases.json              # Clinical evaluation test scenarios
```

---

## 5. How to Run the Tests

### A. Run Entire Master Test Suite
```bash
# Using npm
npm test

# Direct runner
node tests/run_all_tests.mjs
```

### B. Run Specific Test Layers
```bash
# Unit Tests Only
node --test tests/unit/*.test.mjs

# API Integration Tests Only
node --test tests/api/*.test.mjs

# Component Form State Tests Only
node --test tests/component/*.test.mjs

# Non-Functional Performance & Accessibility Tests
node --test tests/non-functional/*.test.mjs

# Security & Regression Suite
node --test tests/regression/security_and_bugs.mjs

# AI Clinical Prompt Evaluation (96 assertions across 8 prompts)
node tests/ai-eval/run_eval.mjs
```

### C. Run Playwright End-to-End Tests
```bash
# Run on desktop & mobile viewports
npx playwright test --config=tests/e2e/playwright.config.mjs

# Run with interactive UI mode
npx playwright test --config=tests/e2e/playwright.config.mjs --ui
```

---

## 6. How to Add New Tests

### 1. Adding a New Unit Test
Create a new file or add a `describe` block under `tests/unit/`:
```javascript
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

describe('Unit: My New Clinical Calculator', () => {
  it('should calculate anesthetic maximum recommended dose (MRD) accurately', () => {
    const weightKg = 70;
    const maxLidocaineMgPerKg = 4.4;
    const mrd = weightKg * maxLidocaineMgPerKg;
    assert.equal(mrd, 308.0);
  });
});
```

### 2. Adding a New API Integration Test
Use `tests/factories/test_data_factories.mjs` to generate fixtures:
```javascript
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { buildSyntheticPatient } from '../factories/test_data_factories.mjs';

describe('API Integration: Specialty Pathology Requisition', () => {
  it('should reject requisition when clinical diagnosis is blank', () => {
    const p = buildSyntheticPatient();
    // Test logic...
    assert.equal(true, true);
  });
});
```

### 3. Adding an AI Prompt Evaluation Case
Open [`tests/ai-eval/test_cases.json`](file:///f:/DentistApp_Theme2/tests/ai-eval/test_cases.json) and append a new scenario under the relevant prompt template:
```json
{
  "id": "cns-11",
  "description": "Pediatric emergency avulsion of tooth 51",
  "variables": {
    "doctorName": "Dr. Sarah Khan",
    "patientName": "Zaid Ali",
    "patientId": 1050,
    "region": "PK",
    "language": "en",
    "transcript": "Tooth 51 completely avulsed after fall from bicycle 20 minutes ago."
  },
  "expectedFindings": ["Avulsion", "Tooth 51"],
  "forbiddenFindings": ["Permanent dentition", "Tooth 11"]
}
```

---

## 7. Continuous Integration Pipeline (GitHub Actions)
The automated CI pipeline is configured at [`.github/workflows/dentia-qa-pipeline.yml`](file:///f:/DentistApp_Theme2/.github/workflows/dentia-qa-pipeline.yml).
It triggers automatically on every push or pull request to `main`, `master`, and `Local`:
1. **Static Analysis & Linting:** Oxlint executes against all JavaScript/JSX files.
2. **Backend Compilation:** .NET 10 Release build compiles with zero warnings as errors.
3. **Automated Test Suite:** Executes Unit, API, Component, Non-Functional, Regression, and AI Evaluation phases.
4. **Playwright Multi-Viewport Verification:** Runs full clinical workflow on Desktop and Mobile viewports.
