# STEP 3: FRONTEND AUTHENTICATION, ROUTING ISOLATION & PORTAL ACCESS

## Overview
This step implements the architectural isolation between the **Dentist / Clinic Workspace** and the **Patient Portal** within the React SPA (`Dentistfrontend`). It creates dedicated authentication views, protected route guards, and a patient-centric layout with custom navigation.

---

## 1. Architecture: Doctor vs. Patient Separation

| Feature Dimension | Doctor / Staff Workspace | Patient Portal |
| :--- | :--- | :--- |
| **Base Route** | `/`, `/patients/*`, `/chart/*`, `/notes/*` | `/portal/*`, `/portal-login`, `/portal-register`, `/portal-activate` |
| **Storage Key** | `localStorage.getItem('doctor')` | `localStorage.getItem('patient')` |
| **User Role Claim** | `"Doctor"`, `"Receptionist"`, `"Admin"` | `"Patient"` |
| **Session Guard** | `ProtectedRoute.jsx` | `PatientProtectedRoute.jsx` |
| **Shell / Layout** | `Navbar.jsx`, `SidePanel.jsx` (Deep slate & navy) | `PatientPortalLayout.jsx` (Teal, slate, clean header, mobile tab bar) |
| **Timeout Policy** | 15-minute clinical idle countdown | 30-minute patient session timeout |
| **Cross-Contamination** | Prevented: Portal users cannot access clinical charts | Prevented: Clinic doctors cannot access patient personal invoices |

---

## 2. Implemented Components & Files

### A. Route Guard: `src/modules/patientPortal/components/PatientProtectedRoute.jsx`
- **Validation**: Checks `localStorage.getItem('patient')` for an active JWT session token and `role === 'Patient'`.
- **Expiration Check**: Inspects `expiresAt` timestamp against current epoch time.
- **Redirection**: Unauthorized or expired attempts are redirected to `/portal-login?expired=true`.

### B. Shell Layout: `src/modules/patientPortal/layouts/PatientPortalLayout.jsx`
- **Header**: Displays Dentia Patient Portal branding, patient avatar, Reference Number badge (`DEN-2026-XXXXX`), quick phone contact, notifications popover, and profile dropdown.
- **Desktop Navigation**: Horizontal pill tabs (`Dashboard`, `Appointments`, `Clinical Reports & X-Rays`, `Billing & Payments`).
- **Mobile Navigation**: Bottom docked floating glass bar with quick access to all 4 tabs and active indicators.
- **User Logout**: Clears `patient` key from localStorage and redirects cleanly to `/portal-login`.

### C. Patient Login: `src/modules/patientPortal/pages/PatientLogin.jsx`
- **Dual Identification Tabs**:
  1. **Reference Number Tab**: Allows login using clinic-assigned ID (e.g. `DEN-2026-00001`).
  2. **Email Tab**: Allows login using registered email address (e.g. `olivia.chen@example.com`).
- **Demo Quick-Fill Feature**: Pre-populates verified credentials for instant developer and reviewer testing.
- **Password Visibility Toggle**: Eye icon for secure, error-free typing.
- **Instant Redirection**: Navigates to `/portal/dashboard` upon successful JWT retrieval.

### D. Patient Self-Registration: `src/modules/patientPortal/pages/PatientRegister.jsx`
- **Form Fields**: First Name, Last Name, Email, Phone Number, Date of Birth, Gender, Password, Confirm Password.
- **Immediate ID Assignment**: Calls `POST /api/patient-auth/register` to issue an automatic `DEN-2026-XXXXX` Reference Number.
- **Success Modal**: Shows patient their new Reference Number and guides them directly into their portal dashboard.

### E. Account Activation: `src/modules/patientPortal/pages/PatientActivate.jsx`
- **Purpose**: For existing clinic patients who receive a Reference Number on their appointment card or SMS but have not yet configured a portal password.
- **Flow**: Enters Reference Number + Date of Birth + New Password -> Calls `POST /api/patient-auth/activate` -> Activates portal access immediately.

### F. Application Router Integration: `src/App.jsx`
- Configured React Router v6 with lazy-loaded portal views:
  - `/portal-login` -> `PatientLogin`
  - `/portal-register` -> `PatientRegister`
  - `/portal-activate` -> `PatientActivate`
  - `/portal` -> `PatientProtectedRoute` wrapping `PatientPortalLayout` with nested routes:
    - `/portal/dashboard` -> `PatientDashboard`
    - `/portal/appointments` -> `PatientAppointments`
    - `/portal/reports` -> `PatientReports`
    - `/portal/billing` -> `PatientBilling`

---

## 3. Verification & Build Integrity
- **Frontend Compilation**: `vite build` completed successfully.
- **Modules Transformed**: 2,130 modules bundled with 0 syntax errors.
- **Chunks Generated**:
  - `PatientLogin-*.js` (10.34 kB)
  - `PatientRegister-*.js` (7.64 kB)
  - `PatientActivate-*.js` (6.53 kB)
  - `PatientPortalLayout-*.js` (7.50 kB)
