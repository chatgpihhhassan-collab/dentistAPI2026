# 🚀 Step 1 Implementation — Backend Data Models, Repositories & Patient Auth Controller

> **Status**: Completed ✅  
> **Date**: September 2026  
> **Focus**: Patient authentication models, repository layer, token generation, and `PatientAuthController`.

---

## 1. Summary of Changes

In this step, we built the foundation for the Patient Portal backend in ASP.NET Core:

1. **Updated C# Models (`DentistAPI/Models/DentalModels.cs`)**:
   - Upgraded `Patient` class with:
     - `ReferenceNumber` (string) — Unique identifier (e.g. `DEN-2026-00035`).
     - `PasswordHash` (string) — BCrypt hashed password.
     - `IsPortalActive` (bool) — Controls portal access status.
     - `LastLoginAt` (DateTime?) — Audit timestamp of last portal login.
     - `MustChangePassword` (bool) — Flag for forced password resets.
   - Upgraded `Appointment` class with direct `PatientID` (int?).
   - Added `Invoice` and `InvoiceItem` classes for dental billing and procedure tracking.
   - Added `Payment` class for both Online Card and Cash at Clinic transactions.
   - Added Authentication DTOs: `PatientLoginRequest`, `PatientRegisterRequest`, `PatientActivateRequest`, `PatientAuthResponse`, `OnlinePaymentRequest`, `CashVoucherRequest`, `ConfirmCashRequest`.

2. **Upgraded Token Service (`DentistAPI/Services/TokenService.cs`)**:
   - Added `PatientTokenPayload` with fields: `PatientId`, `ReferenceNumber`, `FullName`, `Role = "Patient"`, `ExpiresAt`.
   - Added `GeneratePatientToken(patientId, referenceNumber, firstName, lastName)` with a 14-day sliding expiration.
   - Added `ValidatePatientToken(token, out PatientTokenPayload)` ensuring strict separation from doctor tokens.

3. **Exempted Patient Routes in Middleware (`DentistAPI/Services/DoctorAuthMiddleware.cs`)**:
   - Added public route exemptions for `/api/patient-auth`, `/api/patient-portal`, and `/api/billing` so requests to the patient portal are not blocked by the doctor-only session guard.

4. **Added Repository Layer Queries (`DentistAPI/Repositories/DentalRepository.cs`)**:
   - `GetPatientForAuthAsync(identifier)`: Resolves patient by Reference Number, Email, or Phone.
   - `GetPatientByReferenceNumberAsync(referenceNumber)`: Resolves patient profile by reference code.
   - `RegisterPatientSelfAsync(request, passwordHash, referenceNumber)`: Creates new patient profile and returns generated `PatientID`.
   - `ActivatePatientAccountAsync(referenceNumber, dob, passwordHash)`: Verifies Date of Birth against the reference number and sets the initial password.
   - `UpdatePatientLastLoginAsync(patientId)`: Updates audit login timestamp.
   - `LogPatientPortalActivityAsync(patientId, action, ip, userAgent, details)`: Writes audit events to `[dentist].[PatientPortalActivityLogs]`.

5. **Created Controller (`DentistAPI/Controllers/PatientAuthController.cs`)**:
   - `POST /api/patient-auth/login`: Authenticates with Reference # or Email + Password.
   - `POST /api/patient-auth/register`: Direct patient self-registration assigning `DEN-2026-XXXXX`.
   - `POST /api/patient-auth/activate`: First-time walk-in slip activation verifying DOB.
   - `GET /api/patient-auth/me`: Validates session token and returns active profile.

---

## 2. API Contracts & Verification

### `POST /api/patient-auth/login`
```json
// Request Body
{
  "identifier": "DEN-2026-00035",
  "password": "Dentia2026!"
}

// 200 OK Response
{
  "token": "eyJhbGciOi...[JWT Signature]",
  "patientID": 35,
  "referenceNumber": "DEN-2026-00035",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@dentiaclinic.com",
  "phone": "+64 21 000 1234",
  "doctorID": 1,
  "currentTreatmentPlan": "Clear Aligners",
  "treatmentStage": "Stage 3 of 12",
  "dentitionType": "Adult"
}
```

### `POST /api/patient-auth/activate`
```json
// Request Body
{
  "referenceNumber": "DEN-2026-00035",
  "dob": "1992-05-14",
  "newPassword": "MySecurePassword2026!"
}
```

---

## 3. Build & Compilation Verification
- Command: `dotnet build DentistAPI/DentistAPI.csproj`
- Result: **0 Errors**, 78 Warnings (nullable framework annotations).
- Status: Ready for **Step 2** (Patient Portal & Billing Controllers).
