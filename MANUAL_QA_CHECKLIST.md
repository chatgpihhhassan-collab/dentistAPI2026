# 📋 Dentia Manual QA Checklist — Page-by-Page Verification Matrix

**Scope:** Complete functional, visual, accessibility, and edge-case manual verification guide for every page across the Clinician Workspace and the Patient Self-Service Health Portal.

---

## Part 1: Clinician & Practice Management Pages

### 1. `/login` — Clinician & Staff Authentication
* [ ] **Happy Path:** Valid username + password logs in successfully and redirects to `/dashboard` or `/directory`.
* [ ] **Field Validation:** Empty username/password highlights inputs in red with "Field required" message.
* [ ] **Security Masking:** Password field masks characters; eye toggle reveals/hides securely.
* [ ] **Error Handling:** Invalid credentials display "Invalid username or password" (does not disclose whether username or password was incorrect).
* [ ] **Rate Limit Feedback:** Submitting 5 failed attempts rapidly displays `429 Too Many Requests` countdown timer.
* [ ] **Session Telemetry:** Real-time online clinician presence counter increments accurately.

### 2. `/dashboard` — Landing Operational Console
* [ ] **Metrics Cards:** Total patients, today's appointments, completed treatments, and revenue render with live data (no mock placeholders).
* [ ] **Action Shortcuts:** "New Patient", "Book Appointment", "Open Odontogram" buttons navigate to respective routes.
* [ ] **Recent Activity Stream:** Chronological feed of recent patient registrations and chart edits.
* [ ] **Responsive Layout:** Cards collapse gracefully into 1 column on mobile and 3 columns on 1920x1080 display.

### 3. `/directory` — Patient Master Directory
* [ ] **Live Search:** Typing patient name, phone, or CNIC filters list instantly with debounced search.
* [ ] **Pagination:** Next/Previous buttons advance correctly; page numbers update; jumping to page 1 works.
* [ ] **Demographic Badges:** Displays Pakistani phone in `0300-1234567` format, CNIC in `XXXXX-XXXXXXX-X`, and adult/pediatric badge.
* [ ] **Action Links:** Clicking a patient card navigates directly to `/chart/:patientId`.
* [ ] **Empty State:** Searching for a non-existent name displays empathetic "No patients found" empty state with "Register New Patient" button.

### 4. `/new-patient` — Patient Intake & Clinical Registration
* [ ] **Required Demographics:** Enforces First Name, Last Name, Phone, DOB, and Gender.
* [ ] **Pakistani Mobile Validation:** Rejects landlines (`042...`) and non-Pakistani numbers; accepts `03001234567` and normalizes to `+923001234567`.
* [ ] **CNIC Formatting:** Auto-inserts hyphens after 5th and 12th digits.
* [ ] **AI Voice Intake:** Microphone button records spoken intake in English or Roman Urdu, extracts demographics, and populates form fields.
* [ ] **Duplicate Prevention Alert:** Submitting an existing patient displays duplicate warning modal with link to existing record.
* [ ] **Medical Alerts Checkbox:** Checking allergies or systemic condition flags patient card with red alert badge.

### 5. `/chart/:patientId` — Interactive 3D Odontogram & Clinical Workspace
* [ ] **3D Jaw Canvas Rendering:** Maxilla (Upper) and Mandible (Lower) 3D arches render cleanly with 1024x1024 anatomical alignment.
* [ ] **Split Operatory Mode:** Opening Chatbot sidebar resizes 3D jaw canvas symmetrically without teeth detaching from gum sockets.
* [ ] **Tooth Selection:** Clicking any of the 32 adult / 20 primary teeth opens tooth surface inspector.
* [ ] **Condition Assignment:** Assigning Caries, Composite Filling, Gold Crown, or Extraction updates tooth color immediately.
* [ ] **Restorative Missing Tooth Restriction:** Applying restorative filling to a missing tooth displays warning prompt requiring prior implant/bridge fixture.
* [ ] **Specialty Dossiers:** Toggling Ortho & TMJ, Implant Planner, or Biopsy requisition opens respective clinical drawer.
* [ ] **Chatbot Assistant:** Clinical voice dictation (e.g. "Tooth 16 occlusal decay") parses and updates odontogram in real time.

### 6. `/chart/:patientId/tooth/:toothNumber` — Tooth Surface Detail Inspector
* [ ] **5-Surface Map:** Mesial, Distal, Occlusal/Incisal, Buccal/Labial, Lingual surfaces selectable independently.
* [ ] **Anatomical Correctness:** Anteriors (Teeth 11-13, 21-23, 31-33, 41-43) show Incisal edge; Posteriors show Occlusal surface.
* [ ] **CDT Procedure Lookup:** Dropdown shows ADA CDT codes (e.g., D2391, D3330) with standardized fee schedules.
* [ ] **Audit History Log:** Preserves chronological log of past treatments with date and dentist name.

### 7. `/chart/:patientId/ai-studio` & `/ai-studio` — AI Diagnostic Radiology Studio
* [ ] **Radiograph Gallery:** Loads bitewing, periapical, and panoramic X-rays linked to patient.
* [ ] **Image Controls:** Invert colors, High Contrast, Bone Density Enhancement, Zoom (100% to 400%), and Pan function smoothly.
* [ ] **Hardware Ingestion:** Soredex Digora Optime LAN Bridge and Eighteeth NanoPix USB Sensor status badges show live connection state.
* [ ] **AI Findings Overlay:** Gemini AI pathology heatmap highlights detected enamel radiolucencies, bone loss, or periapical lesions.
* [ ] **Measurement Tool:** Calibrated millimeter ruler measures root length and alveolar bone defect accurately.

### 8. `/appointments` — Practice Calendar & Master Schedule
* [ ] **View Modes:** Toggle Day, Week, and Month views seamlessly.
* [ ] **Dentist Filtering:** Filtering by specific doctor shows only their assigned appointments.
* [ ] **Status Color Codes:** Pending (Yellow), Confirmed (Blue), In-Progress (Purple), Completed (Green), Cancelled (Red).
* [ ] **Status Mutation:** Clicking an appointment allows updating status; Completed cannot revert to Pending.
* [ ] **Collision Warning:** Booking an overlapping time slot for the same doctor triggers conflict warning.

### 9. `/book` — Standalone Appointment Booking
* [ ] **Working Hours Picker:** Restricts selectable time slots to 08:00 AM – 08:00 PM.
* [ ] **Past Date Restriction:** Days prior to today are disabled in calendar date picker.
* [ ] **Patient Autocomplete:** Typing existing patient name auto-fills phone number and email.
* [ ] **Confirmation SMS/Email:** Checkbox allows sending appointment confirmation notification.

### 10. `/history/:patientId` — Immutable Clinical Audit Trail
* [ ] **Chronological Timeline:** Lists every diagnosis, odontogram condition change, and prescription issued.
* [ ] **Audit Integrity:** Entries are read-only; editing or deleting historical entries is prohibited.
* [ ] **Doctor Attribution:** Every log entry attributes the specific treating dentist and timestamp.

### 11. `/ai-notes` & `/ai-notes/detail/:noteId` — AI Dental Clinical Scribe
* [ ] **Microphone Recording:** Live audio wave visualizer animates while speaking; timer counts duration.
* [ ] **Multi-lingual Dictation:** Accepts natural English, Roman Urdu, and technical dental jargon.
* [ ] **8-Point SOAP Generation:** Compiles Subjective, Objective, Assessment, Plan, CDT codes, and prescriptions.
* [ ] **Approval Workflow:** Doctor can edit generated text and click "Approve & Sync to Chart".

### 12. `/treatment-pricing` & `/doctor/pricing` — Doctor Fee Schedules
* [ ] **15 Clinical Categories:** Diagnostic, Preventive, Restorative, Endodontics, Periodontics, Prosthodontics, Implants, Orthodontics, Oral Surgery, etc.
* [ ] **Currency Switching:** Toggle between PKR, NZD, USD, GBP, and EUR updates currency symbols without float distortion.
* [ ] **Reset to Master:** "Reset to Master" restores clinic standard fee schedules.

### 13. `/admin/doctors` — SuperAdmin Doctor Management
* [ ] **SuperAdmin Guard:** Regular doctors attempting to access this route are redirected to `/dashboard`.
* [ ] **Doctor Provisioning:** Register new dentist with specialization, license #, and affiliated clinics.
* [ ] **Active Toggle:** Toggling doctor inactive disables their login and removes them from public booking directory.
* [ ] **Password Reset:** SuperAdmin can update any doctor's password adhering to password complexity rules.

### 14. `/admin/organizations` — Hospital & Clinic Management
* [ ] **Clinic Profiles:** Create and update hospital/clinic name, address, accreditation, and hero banner.
* [ ] **Doctor Affiliation:** Assign and remove doctors from clinics.

### 15. `/setup-guide` — Hardware Setup & Device Calibration
* [ ] **Step-by-step Driver Guide:** Instructions for Eighteeth NanoPix and Digora Optime setup.
* [ ] **Port Testing:** "Test Connection" button executes ping test against local bridge port.

---

## Part 2: Patient Self-Service Health Portal (`/portal`)

### 16. `/portal/login` — Patient Authentication
* [ ] **Identifier Input:** Accepts either `DEN-YYYY-XXXXX` Reference Number or registered Email.
* [ ] **Unactivated Record Alert:** Attempting to log into an unactivated account prompts patient with "Activate Account" button.
* [ ] **Remember Reference #:** Checkbox remembers Reference # in local device storage.

### 17. `/portal/register` — Patient Self-Registration
* [ ] **Form Validation:** First Name, Last Name, Phone, and Password required.
* [ ] **Password Strength Meter:** Enforces 8+ chars, upper, lower, digit, and symbol.
* [ ] **Instant Reference Generation:** On success, generates unique `DEN-YYYY-XXXXX` reference and redirects to dashboard.

### 18. `/portal/activate` — Account Claim & Password Activation
* [ ] **Two-Factor Verification:** Requires clinic-issued Reference Number + Date of Birth.
* [ ] **DOB Mismatch Defense:** Entering wrong DOB displays "Date of birth does not match clinic records".
* [ ] **Set New Password:** Prompts patient to set their secure portal password.

### 19. `/portal/dashboard` — Patient Health Overview
* [ ] **Welcome Banner:** Greets patient by first name; displays Reference Number and assigned clinic dentist.
* [ ] **Quick Action Cards:** "Book Appointment", "View Odontogram", "Pay Invoices", "Download Reports".
* [ ] **Outstanding Balance Pill:** Highlights unpaid balance in amber/red with "Pay Now" shortcut.

### 20. `/portal/odontogram` — Interactive Patient Tooth Map
* [ ] **Simplified Color Legend:** Healthy (Green), Needs Treatment (Red), In Progress (Purple), Treated (Blue).
* [ ] **Patient-Friendly Explanations:** Clicking a tooth displays layperson terminology (e.g., "Mild Cavity on Chewing Surface" instead of "Class I Dentine Caries").
* [ ] **Mobile Touch Support:** Pinch-to-zoom and touch selection function smoothly on smartphones.

### 21. `/portal/appointments` & `/portal/book` — Self-Service Booking
* [ ] **Doctor Selection:** Dropdown lists available active clinic dentists.
* [ ] **Available Slots:** Displays only open 30-minute intervals during working hours.
* [ ] **Cancellation:** Upcoming appointment shows "Cancel Appointment" button with confirmation prompt.

### 22. `/portal/billing` — Invoices, Online Card Payment & Cash Vouchers
* [ ] **Invoice Table:** Lists date, invoice number, items, total, paid, and balance.
* [ ] **Simulated Card Checkout:** Pay Online button opens Stripe-styled modal; verifies Luhn card number; generates instant receipt.
* [ ] **Cash Desk Voucher:** "Generate Cash Voucher" button creates `CSH-YYYY-XXXXX` voucher code with QR code for front-desk presentation.
* [ ] **PDF Download:** "Download Invoice PDF" generates branded PDF with clinic header and portal access slip.

### 23. `/portal/reports` — Clinical Summaries & Dental X-Rays
* [ ] **Diagnostic Assessments:** View doctor clinical summaries and post-op instructions.
* [ ] **X-Ray Gallery:** High-resolution view of patient radiographs with pan and zoom.
* [ ] **Audio Post-Op:** Play doctor voice note explaining home-care instructions.

### 24. `/portal/doctors` — Clinic Practitioner Directory
* [ ] **Doctor Cards:** Displays qualifications, years of experience, dental specialties, and profile photo.
* [ ] **"Book with Dr." Button:** Prefills selected doctor in the booking modal.
