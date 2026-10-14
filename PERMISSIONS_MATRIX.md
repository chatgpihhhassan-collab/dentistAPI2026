# Dentia Practice Management Platform — Role-Based Access Control (RBAC) Permissions Matrix

> **Security Architecture Standard:** Server-side Deny-by-Default (OWASP A01:2021, OWASP API1:2023).  
> UI visibility rules are purely cosmetic; every endpoint enforces identity, role authorization, and tenant ownership on the server.

---

## 1. System Roles Definition

| Role Identifier | Description | Identity Verification | Session Lifetime |
| :--- | :--- | :--- | :--- |
| **SuperAdmin** | Clinic owner, principal medical director, or practice systems administrator with unrestricted multi-clinic oversight. | Cryptographically signed HMAC-SHA256 Doctor Token (`IsSuperAdmin = true`) | 120 minutes with activity sliding window |
| **Doctor / Clinician** | Licensed dentist, dental specialist, or hygienist performing oral examinations, charting, radiographs, and procedures. | Cryptographically signed HMAC-SHA256 Doctor Token (`Role = "Doctor"`) | 120 minutes with activity sliding window |
| **Receptionist / Staff** | Front-desk staff managing clinic scheduling, patient registration, and cashier payments. | Future role extension; currently operates under clinic doctor token or portal coordinator credentials | 60 minutes |
| **Patient** | Individual receiving dental care; accesses personal appointments, health map, treatment estimates, and receipts. | Cryptographically signed HMAC-SHA256 Patient Token (`Role = "Patient"`, scoped to `PatientId`) | 240 minutes |
| **Public / Unauthenticated** | Any client connection without an authorization token. | None; allowed only on whitelist of non-sensitive public endpoints | Zero session |

---

## 2. API Endpoints Authorization Matrix

### Legend:
- ✅ **ALLOWED**: Full execution permitted.
- 🔒 **SCOPED**: Execution permitted **ONLY** for own record (tenant/patient ID match); cross-record queries receive `403 Forbidden`.
- ❌ **DENIED**: Blocked at middleware with `401 Unauthorized` or `403 Forbidden`.

---

### 2.1 Authentication & Profile Management (`/api/auth`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/auth/register` | `POST` | ✅ | ❌ | ❌ | ✅ | Public registration for clinics; validates password complexity via Zod/Server |
| `/api/auth/login` | `POST` | ✅ | ❌ | ❌ | ❌ | Validates credentials against BCrypt hash; generates short-lived Doctor Token |
| `/api/auth/logout` | `POST` | ❌ | ❌ | ✅ | ✅ | `TokenService.RevokeToken()` blacklists bearer token immediately |
| `/api/auth/doctors` | `GET` | ✅ | ✅ | ✅ | ✅ | Public directory returns active clinicians; SuperAdmin sees inactive clinicians |
| `/api/auth/doctors` | `POST` | ❌ | ❌ | ❌ | ✅ | `IsCallerSuperAdmin()` guard blocks non-superadmins |
| `/api/auth/doctors/{id}` | `GET` | ✅ | ✅ | ✅ | ✅ | Public clinician professional bio lookup |
| `/api/auth/doctors/{id}` | `PUT` | ❌ | ❌ | ❌ | ✅ | SuperAdmin only profile management |
| `/api/auth/doctors/{id}/status` | `PUT` | ❌ | ❌ | ❌ | ✅ | SuperAdmin only status toggle (Active/Inactive) |
| `/api/auth/doctors/{id}/password` | `PUT` | ❌ | ❌ | 🔒 | ✅ | Self (`DoctorId == id`) or SuperAdmin only; BCrypt hashed |
| `/api/auth/presence-stats` | `GET` | ✅ | ✅ | ✅ | ✅ | Aggregated anonymous presence counters |

---

### 2.2 Patient Management & Clinical Records (`/api/patients`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/patients` | `GET` | ❌ | ❌ | ✅ | ✅ | Doctor retrieves clinic roster; sanitized SQL parameterized query |
| `/api/patients` | `POST` | ❌ | ❌ | ✅ | ✅ | Clinician registers patient; validates CNIC, phone, DOB |
| `/api/patients/{id}` | `GET` | ❌ | 🔒 | ✅ | ✅ | `TryAuthorizePatientAccess()`: Patient can only view own ID; Audited in access log |
| `/api/patients/{id}` | `PUT` | ❌ | ❌ | ✅ | ✅ | Clinician updates patient demographics & medical history |
| `/api/patients/{id}/chart` | `GET` | ❌ | 🔒 | ✅ | ✅ | BOLA protected: Patient restricted to own chart; audited with `VIEW_PATIENT_CHART` |
| `/api/patients/{id}/chart/export` | `POST` | ❌ | 🔒 | ✅ | ✅ | PDF export authorization check; audited with `EXPORT_PATIENT_CHART` |
| `/api/patients/teeth/update-bulk`| `POST` | ❌ | ❌ | ✅ | ✅ | Clinician saves odontogram condition markers |
| `/api/patients/{id}/prescriptions`| `GET` | ❌ | 🔒 | ✅ | ✅ | BOLA protected; audited with `VIEW_PRESCRIPTIONS` |
| `/api/patients/{id}/prescriptions`| `POST`| ❌ | ❌ | ✅ | ✅ | Clinician records medication prescription |
| `/api/patients/{id}/clinical-logs`| `GET` | ❌ | 🔒 | ✅ | ✅ | BOLA protected; audited with `VIEW_CLINICAL_LOGS` |
| `/api/patients/{id}/clinical-logs`| `POST`| ❌ | ❌ | ✅ | ✅ | Clinician writes timestamped procedure progress note |
| `/api/patients/{id}/diagnostic-assessment`| `GET`| ❌ | 🔒 | ✅ | ✅ | BOLA protected; audited with `VIEW_DIAGNOSTIC_ASSESSMENT` |
| `/api/patients/{id}/diagnostic-assessment`| `POST`| ❌ | ❌ | ✅ | ✅ | Clinician stores orthodontic/TMJ diagnostic assessment |

---

### 2.3 Diagnostic Imaging & SOREDEX DIGORA RVG (`/api/radiographs`, `/api/imaging`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/patients/{id}/radiographs` | `GET` | ❌ | 🔒 | ✅ | ✅ | BOLA protected; lists radiograph metadata without large base64 payload |
| `/api/patients/{id}/radiographs` | `POST` | ❌ | ❌ | ✅ | ✅ | Multi-part upload; magic-byte verification (PNG/JPEG/DICOM), malware check |
| `/api/radiographs/{id}/image` | `GET` | ❌ | 🔒 | ✅ | ✅ | Streaming byte response; requires Bearer token or scoped ephemeral query ticket |
| `/api/hardware-imaging/status` | `GET` | ❌ | ❌ | ✅ | ✅ | Clinician local USB/Ethernet RVG sensor health check |
| `/api/hardware-imaging/capture` | `POST` | ❌ | ❌ | ✅ | ✅ | Hardware capture trigger; images never persist beyond session without auth |

---

### 2.4 Billing, Invoicing & Payments (`/api/billing`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/billing/invoices` | `GET` | ❌ | 🔒 | ✅ | ✅ | Patient sees only own invoices; Clinician sees practice financial ledger |
| `/api/billing/create-invoice` | `POST` | ❌ | ❌ | ✅ | ✅ | Restricted to Clinicians; validates line-item procedure fees |
| `/api/billing/invoices/{id}/record-payment` | `POST` | ❌ | ❌ | ✅ | ✅ | Cashier/Doctor payment entry; prevents overpayments exceeding balance |
| `/api/billing/pay-online` | `POST` | ❌ | 🔒 | ❌ | ❌ | Patient portal payment checkout; verifies Luhn and currency |
| `/api/billing/generate-cash-voucher` | `POST` | ❌ | 🔒 | ✅ | ✅ | Generates barcode voucher for Kuickpay/1Link bank deposit |
| `/api/billing/confirm-cash` | `POST` | ❌ | ❌ | ✅ | ✅ | Clinic accountant confirmation; non-clinicians receive `401 Unauthorized` |

---

### 2.5 AI Clinical Services (`/api/ai`, `/api/ai-dental-notes`, `/api/chatbot`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/ai/clinical-note` | `POST` | ❌ | ❌ | ✅ | ✅ | `ai-policy` rate limit (10 req/min); Delimiter-isolated transcript; PII de-identified |
| `/api/ai/radiograph-findings` | `POST` | ❌ | ❌ | ✅ | ✅ | AI vision assistant; provider API key held strictly on backend |
| `/api/ai-dental-notes/generate` | `POST` | ❌ | ❌ | ✅ | ✅ | Asynchronous note generation with concurrency lock (409 on duplicate collision) |
| `/api/chatbot/query` | `POST` | ❌ | ❌ | ✅ | ✅ | `ai-policy` rate limiting; prompt injection filtering; read-only dictionary lookup |

---

### 2.6 Diagnostic Logs & System Administration (`/api/logs`)

| Endpoint | Method | Public | Patient | Doctor | SuperAdmin | Server-Side Enforcement Mechanism |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `/api/logs/all` | `GET` | ❌ | ❌ | ❌ | ✅ | Strictly restricted to verified SuperAdmin; all PII/PHI masked |
| `/api/logs/database` | `GET` | ❌ | ❌ | ❌ | ✅ | SuperAdmin only; raw passwords & tokens redacted |
| `/api/logs/api` | `GET` | ❌ | ❌ | ❌ | ✅ | SuperAdmin only; response token bodies redacted |
| `/api/logs/view` | `GET` | ❌ | ❌ | ❌ | ✅ | SuperAdmin only; path traversal protected (`Path.GetFileName`) |

---

## 3. Defense-in-Depth Summary

1. **Deny-by-Default:** Any route starting with `/api/` not explicitly included in the public exception list is halted by `DoctorAuthMiddleware` before reaching the controller.
2. **Object Level Enforcement:** Controller methods resolving a patient record invoke `TryAuthorizePatientAccess(patientId)`, terminating unauthorized cross-patient attempts with HTTP 403.
3. **Audit Immutability:** Every access to clinical records is appended to `logs/audit_access_YYYY-MM-DD.log` with caller ID, role, target patient, action, and IP address.
