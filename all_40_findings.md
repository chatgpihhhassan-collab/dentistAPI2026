# APT-001 · 'Save Changes' does nothing when setting an appointment to 'Rejected /

APT-001 · 'Save Changes' does nothing when setting an appointment to 'Rejected /
Cancelled' (no confirmation, no feedback)
Severity
Critical (Jira: Highest)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in on the Doctor / Staff tab and click 'Continue anyway' if prompted
2. Open Appointments, then 'Data Ledger', and search 'Waqar'
3. Open Waqar Ahmed's Wed Oct 21 2026 10:00 appointment
4. Click 'Change Status'
5. Select 'Rejected / Cancelled' in 'Select New Status'
6. Click 'Save Changes' (I clicked it 3 times and waited 3 seconds)
Expected
A confirmation prompt appears, the status is saved, the dialog closes, a success message shows,
and the list updates to Cancelled.
Actual
The dialog stays open with no confirmation prompt, error, spinner or success message. I could not
confirm that the status changed, so cancelling appears to be blocked. Re-tested manually on 20
Oct 2026 (desktop). Set the 22 Oct 11:00 appointment to 'Rejected / Cancelled' and clicked Save
Changes: PUT /api/appointments/46 was sent (5.8 s), the dialog stayed open with no message, and
after reload the status was still 'Confirmed'.
Suggested fix
Check the status-update request and its error handling. Show a confirmation prompt first. After
saving, close the dialog, show a toast, and refresh the list. Show an error if the request fails, and
disable the button while it is pending.



==================== PAGE 12 ====================

================================================================================

# AI-001 · Clinical-looking AI/demo values shown as real patient data

AI-001 · Clinical-looking AI/demo values shown as real patient data
Severity
High (Jira: High)
Area / type
AI content · Content
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open the dashboard and select the new patient Waqar Ahmed
Expected
Only measured or recorded clinical data, clearly labelled
Actual
The dashboard shows 'AI SCANNER LIVE · Enamel Density 99.4% · Zero Pathology' with no scan
taken; the new patient shows 'HEALTH 100%' and is tagged 'Braces, Whitening, Cavity' although he
has no clinical record.
Suggested fix
Remove demo values, or label them clearly as samples; never show diagnostic numbers without a
source.



==================== PAGE 13 ====================

================================================================================

# APT-002 · Booking shows a failure (or no confirmation) although the appointment is

APT-002 · Booking shows a failure (or no confirmation) although the appointment is
saved; save takes ~14 s
Severity
High (Jira: High)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in via the Doctor / Staff tab (staff_user / staff_password) and wait for the dashboard.
2. Click the Appointments Hub icon in the top nav.
3. Click 'New Appointment'.
4. Select Attending Specialist 'Dr. jhangir ahmed (General Dentistry)'.
5. Pick patient Waqar Ahmed (#1045, +923455097066) from the list.
6. Set Preferred Date & Time to 10/21/2026 10:00 AM and Reason for Visit 'Consultation'.
7. Click 'Confirm Appointment' (retry several times).
Expected
The appointment is created, a confirmation shows patient, dentist, service, date and time, and it
appears in the calendar.
Actual
The automated run saw 'Failed to schedule appointment.' on every attempt, yet Waqar Ahmed now
has appointments on 20 Oct 11:30 and 21 Oct 10:00 from those attempts. Re-tested manually on
20 Oct 2026 (desktop). Booking 22 Oct 11:00: POST /api/appointments took 14.4 s, the dialog
closed with no success message, and the calendar did not show the booking until the page was
reloaded. Staff will re-book and create duplicates.
Suggested fix
Show the server's real result (success toast with date/time, or the actual error), refresh the calendar
after saving, disable the button while saving, and investigate why the API takes ~14 s.



==================== PAGE 14 ====================

================================================================================

# APT-003 · No way to reschedule an existing appointment (only 'Change Status' is offered)

APT-003 · No way to reschedule an existing appointment (only 'Change Status' is offered)
Severity
High (Jira: High)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in on the Doctor / Staff tab (Open Patient Charts) and click 'Continue anyway' if
prompted
2. Open Appointments
3. Open the 'Data Ledger' tab and search 'Waqar'
4. Open the Wed Oct 21 2026 10:00 appointment for Waqar Ahmed from the row actions
5. In the Appointment Summary modal, look for a way to change the date or time
Expected
A Reschedule or Edit action lets staff pick a new day and time. The change is confirmed and shown
in the list, calendar and patient record.
Actual
The modal only offers 'Change Status', which opens 'Update Appointment Status' with status,
clinical note and synced notes. There is no date or time control.
Suggested fix
Add a Reschedule action to the row menu and the summary modal. It should use a date and time
picker that shows only free slots, and show a success message.



==================== PAGE 15 ====================

================================================================================

# WEB-001 · Public home page shows fictional doctors and unsupported claims

WEB-001 · Public home page shows fictional doctors and unsupported claims
Severity
High (Jira: High)
Area / type
Home page · Content
Page
https://dentistfrontend.vercel.app/
Steps
1. Open https://dentistfrontend.vercel.app/
2. Scroll through the Doctors, stats and testimonial sections
Expected
Only the clinic's real doctors, numbers and reviews
Actual
Doctors 'Dr. Sarah Bennett', 'Dr. Maya Lin', 'Dr. Michael Reyes', 'Dr. James Carter' with US
credentials (DDS, FACP, AACD) — none of them are in the clinic's doctor list. Claims '23k+ Google
Reviews', '12,000+ Smiles', '15+ Years', '100% Transparent Diagnostics' and a generic testimonial
look like AI/template text. On a healthcare site this is misleading.
Suggested fix
Load doctors, stats and reviews from clinic data, and hide a section when there is no real data.



==================== PAGE 16 ====================

================================================================================

# NAV-001 · Dashboard (/directory) loading overlay stuck at 20% after login; needs

NAV-001 · Dashboard (/directory) loading overlay stuck at 20% after login; needs
'Continue anyway'
Severity
High (Jira: High)
Area / type
Navigation · Bug
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Open https://dentistfrontend.vercel.app/login, Doctor / Staff tab.
2. Enter the staff username and password and click 'Open Patient Charts'.
3. Wait 30+ seconds on /directory.
Expected
The dashboard finishes loading and the overlay disappears on its own.
Actual
A loading overlay stays at 20% for 30+ seconds. The page is only usable after clicking 'Continue
anyway'. The overlay appeared again after navigating from /directory to other pages and after a
second login.
Suggested fix
Add a timeout and error handling to the loading routine, and show real progress. Dismiss the
overlay when the data call fails or completes, and don't block navigation behind it.



==================== PAGE 17 ====================

================================================================================

# REG-001 · Create Patient Profile hangs on 'Saving Profile...' with no message, although

REG-001 · Create Patient Profile hangs on 'Saving Profile...' with no message, although
the patient is created
Severity
High (Jira: High)
Area / type
Registration · Bug
Page
https://dentistfrontend.vercel.app/new-patient
Steps
1. Log in at https://dentistfrontend.vercel.app/login with the staff credentials.
2. Dismiss the 'Continue anyway' prompt if shown.
3. Open Patient Directory and click 'Add Patient'.
4. Enter first name Waqar and last name Ahmed.
5. Enter date of birth 1990-05-15 and select gender Male.
6. Expand 'Extended Clinical & Contact Info' and enter phone 923455097066.
7. Click 'Create Patient Profile' and wait 10+ seconds.
Expected
The patient is saved and a clear success message appears, or a clear error message appears if
saving fails. The button returns to normal.
Actual
The button stays disabled on 'Saving Profile...' with a spinner for 10+ seconds. There is no success
or error message. It is unknown whether the patient was created. Re-tested manually on 20 Oct
2026 (desktop). The patient was in fact saved (Waqar Ahmed, #1045), so the problem is the
missing feedback; staff will retry and create duplicate patients.
Suggested fix
Check the create-patient API call and its response handling. Add a request timeout and an error
state that re-enables the button. Show a success toast and redirect to the patient directory or
profile on success.



==================== PAGE 18 ====================

================================================================================

# REG-002 · Validation errors appear only in the AI chat panel, not next to the invalid fields

REG-002 · Validation errors appear only in the AI chat panel, not next to the invalid fields
Severity
High (Jira: High)
Area / type
Registration · UX
Page
https://dentistfrontend.vercel.app/new-patient
Steps
1. Log in as staff.
2. Go to Patient Directory and click 'Add Patient'.
3. Click 'Create Patient Profile' with the form empty.
4. Look at the form fields and at the Intake AI Assistant panel.
5. Repeat with a future date of birth, or with phone '92345abc066'.
Expected
Each required or invalid field shows a clear message next to it, with the field highlighted.
Actual
The errors are listed in the chat panel on the right. Only Gender has an inline red error. The date of
birth and phone errors were shown only in the chat panel.
Suggested fix
Add inline error text and aria-invalid/aria-describedby to every field (first name, last name, DOB,
gender, phone). Scroll to and focus the first invalid field. Keep the chat message as a secondary
channel.



==================== PAGE 19 ====================

================================================================================

# REG-003 · Saved patient data differs from what was entered (date of birth, email,

REG-003 · Saved patient data differs from what was entered (date of birth, email,
address)
Severity
High (Jira: High)
Area / type
Registration · Bug
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Register a patient with DOB 1990-05-15, email waqar.ahmed.qa@example.com, address
'QA test address, Karachi'
3. Open Patient Directory > Waqar Ahmed > Edit Patient Information
Expected
The record shows exactly what was entered (DOB 1990-05-15, age 36)
Actual
Saved DOB is 1979-03-11 (age shown 47), email is waqar.ahmed@dentiaclinic.com (never entered),
address is empty.
Suggested fix
Save the submitted values as-is; never derive or invent fields. Add a test that compares the saved
record with the submitted form.

================================================================================

# REG-004 · App invents patient email addresses (firstname.lastname@dentiaclinic.com)

REG-004 · App invents patient email addresses (firstname.lastname@dentiaclinic.com)
Severity
High (Jira: High)
Area / type
Registration · Bug
Page
https://dentistfrontend.vercel.app/new-patient
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Add Patient: there is no email field
3. Register a patient, then open the patient record
Expected
Email is collected from the patient or left empty
Actual
Every patient gets an address on the clinic's own domain (e.g. saif.haider@dentiaclinic.com).
Reminders or invoices could be sent to a real mailbox that belongs to someone else.
Suggested fix
Add an optional email field and store nothing when it is empty.



==================== PAGE 20 ====================

================================================================================

# SEC-001 · Login page shows the real staff username as an example (e.g. ahmedjh)

SEC-001 · Login page shows the real staff username as an example (e.g. ahmedjh)
Severity
High (Jira: High)
Area / type
Security · Bug
Page
https://dentistfrontend.vercel.app/login
Steps
1. Open https://dentistfrontend.vercel.app/login (not logged in)
2. Look at the Clinician Username label and placeholder
Expected
No real account names on a public page
Actual
The label and placeholder read 'e.g. ahmedjh', a real staff login. Combined with a weak password,
the account is easy to guess.
Suggested fix
Use a neutral placeholder ('Your username'), and enforce a password policy for staff accounts.



==================== PAGE 21 ====================



==================== PAGE 22 ====================

================================================================================

# APT-004 · Specialist dropdown has duplicate, placeholder and malformed doctor names

APT-004 · Specialist dropdown has duplicate, placeholder and malformed doctor names
Severity
Medium (Jira: Medium)
Area / type
Appointments · Content
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in via the Doctor / Staff tab.
2. Open Appointments Hub, then 'New Appointment'.
3. Open the 'Attending Specialist' dropdown.
Expected
A list of unique, real dentists with distinct specialties.
Actual
'Dr. jhangir ahmed' appears 3 times. There are also 'Dr. Dr Abdullah', 'Dr. Dr Raima' (double 'Dr.'
prefix) and 'Dr. Test Doctor'. All 14 entries are labelled 'General Dentistry'. The same list appears in
the calendar filter.
Suggested fix
Deduplicate doctor records and remove test data from staging. Strip the stored 'Dr' prefix or do
not prepend 'Dr.' when the name already has it. Assign real specialties.



==================== PAGE 23 ====================

================================================================================

# APT-005 · Unlabelled red 'Failed 🔴' badge next to 'Confirmed' (it is the WhatsApp

APT-005 · Unlabelled red 'Failed 🔴' badge next to 'Confirmed' (it is the WhatsApp
delivery status)
Severity
Medium (Jira: Medium)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in via the Doctor / Staff tab.
2. Open Appointments Hub.
3. Select Tue Oct 20 in the calendar and look at the Waqar Ahmed 11:30 AM Consultation
card.
Expected
A single clear status that is consistent with the status filter options (Confirmed, Pending,
Completed, Cancelled).
Actual
The card shows both 'Confirmed' and a 'Failed 🔴' badge. 'Failed' is not a filter option. Re-tested
manually on 20 Oct 2026 (desktop). Other cards show 'WhatsApp Inactive ⚪' in the same place,
so the badge is the WhatsApp notification status, but nothing on the card says so.
Suggested fix
Label it (e.g. 'WhatsApp: failed to send') with a tooltip and a Retry action.



==================== PAGE 24 ====================

================================================================================

# APT-006 · New appointment defaults to UTC time and allows past dates

APT-006 · New appointment defaults to UTC time and allows past dates
Severity
Medium (Jira: Medium)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Appointments Hub > New Appointment at about 14:13 local time (PKT)
3. Look at 'Preferred Date & Time'
Expected
Defaults to the next free slot in clinic time (PKT); past times cannot be chosen
Actual
Defaults to 2026-10-20 09:13, i.e. the current UTC time (5 hours behind PKT); the field has no
minimum, so past dates can be picked.
Suggested fix
Build the default from the clinic's timezone, set a min on the input, and validate on the server.

================================================================================

# APT-007 · Counters and calendar show 0 / empty while data is still loading

APT-007 · Counters and calendar show 0 / empty while data is still loading
Severity
Medium (Jira: Medium)
Area / type
Appointments · Bug
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open or reload Appointments Hub
3. Watch the stat cards and calendar in the first seconds
Expected
A loading state until data has arrived
Actual
'0 TOTAL BOOKED' and an empty month are shown first, then real numbers (27) appear; the
dashboard likewise showed '0 patients scheduled today' and later '2'. Staff can mistake this for an
empty schedule.
Suggested fix
Show skeletons or spinners on counters and calendar until the API responds.



==================== PAGE 25 ====================

================================================================================

# CFG-001 · Placeholder phone number and email shown in header and footer on every

CFG-001 · Placeholder phone number and email shown in header and footer on every
page
Severity
Medium (Jira: Medium)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/
Steps
1. Log in as staff
2. Open the Home page
3. Look at the top bar and the footer
4. Open /about, /treatment and /doctor/pricing to compare
Expected
Clinic phone and email come from clinic settings and are real values.
Actual
Phone '+1 123 456 789' and email 'contact@dentiaclinic.com' are shown identically on all pages.
The phone is an obvious dummy sequence. No settings page exists to edit them.
Suggested fix
Add clinic contact fields to a settings table or page. Load them in the header and footer from one
shared source, and replace the dummy values.



==================== PAGE 26 ====================

================================================================================

# CFG-002 · Opening hours and address are hardcoded and generic

CFG-002 · Opening hours and address are hardcoded and generic
Severity
Medium (Jira: Medium)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/treatment
Steps
1. Log in as staff
2. Open /treatment and scroll to the footer
3. Check the 'Practice Location' and hours text
Expected
The address and opening hours come from settings.
Actual
The footer shows 'Suite 400, Grand Central Medical Plaza, Downtown Dental Arts' and 'Mon - Sat:
08:00 - 20:00'. Both look like template text. They are the same on every page and no setting exists
for them.
Suggested fix
Move address and hours into clinic settings and render them from there. Replace the sample
address.



==================== PAGE 27 ====================

================================================================================

# CFG-003 · Hardcoded Google rating and patient/experience stats

CFG-003 · Hardcoded Google rating and patient/experience stats
Severity
Medium (Jira: Medium)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/about
Steps
1. Log in as staff
2. Open /about
3. Read the top bar and the philosophy section
Expected
Ratings and statistics come from real data or settings.
Actual
'★ 5.0 (23k Google Reviews)' appears in the header on every page. '10,000+ Happy Patients' and
'15+ Years Experience' are static. A perfect 5.0 from 23k reviews looks like sample data, and the
10,000 patients and 23k reviews also do not match a new system.
Suggested fix
Pull the review score from a configurable integration or settings. Compute the patient count from
the database, or remove it.



==================== PAGE 28 ====================

================================================================================

# CFG-004 · Default currency shows NZD ($) and no fee data loads on pricing page

CFG-004 · Default currency shows NZD ($) and no fee data loads on pricing page
Severity
Medium (Jira: Medium)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/doctor/pricing
Steps
1. Log in as staff
2. Open 'Doctor Fee Schedules & Treatment Pricing' (/doctor/pricing)
3. Look at 'Active Currency' and the page banner
Expected
The default currency matches the clinic's setting (the page does have a currency selector). The fee
list loads.
Actual
Currency defaults to 'NZD ($) — New Zealand Dollar' and the options include PKR, USD and GBP.
The clinic phone is +1 and the doctor's name suggests a different region, so the default looks
arbitrary. A 'Network connection error.' banner appears and the fee list shows '0' procedures. The
hardcoded count '141' and '15 specialties' appear in many labels.
Suggested fix
Store the default currency in clinic settings and load it on page load. Fix the fee API error. Derive
procedure and specialty counts from data.



==================== PAGE 29 ====================

================================================================================

# CFG-005 · Website Studio can only show/hide home page sections, not edit their text

CFG-005 · Website Studio can only show/hide home page sections, not edit their text
Severity
Medium (Jira: Medium)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/clinic/website-studio
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Website & Landing Page Studio
3. Try to change the stats, doctors or contact details
Expected
Clinic staff can edit section text, contact details and stats
Actual
Sections can only be toggled on or off; stats (e.g. '12k+ Smiles, 15+ Years'), doctors and contact
details are fixed in the code.
Suggested fix
Make section content editable in the studio (or drive it from clinic settings).



==================== PAGE 30 ====================

================================================================================

# NAV-002 · Current page is not highlighted in the main menu

NAV-002 · Current page is not highlighted in the main menu
Severity
Medium (Jira: Medium)
Area / type
Navigation · Accessibility
Page
https://dentistfrontend.vercel.app/privacy
Steps
1. Log in.
2. Click 'Continue anyway' if the overlay shows.
3. Open /appointments, /doctor/pricing, /treatment and /privacy in turn.
4. Inspect the header icon menu on each page.
Expected
The menu item for the current page has a distinct active style and aria-current="page".
Actual
All 9 menu icons have identical inactive classes (text-slate-600 hover:...) on every page checked, so
the user can't tell which section they are in.
Suggested fix
Compare the router pathname to each link's href and apply an active class plus aria-
current="page".



==================== PAGE 31 ====================

================================================================================

# NAV-003 · Browser tab title is identical on every page

NAV-003 · Browser tab title is identical on every page
Severity
Medium (Jira: Medium)
Area / type
Navigation · Accessibility
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in.
2. Open /directory, /appointments, /doctor/pricing, /treatment, /terms and /privacy in turn.
3. Read the browser tab title on each.
Expected
Each page has its own title, for example 'Appointments – Dentia'.
Actual
The title is always 'Dentia — AI Dental Practice Management & Direct Web RVG Sensor Software'.
This hurts screen-reader users, history and bookmarks.
Suggested fix
Set a page-specific document.title (for example with per-route metadata or a Helmet/useEffect
hook).



==================== PAGE 32 ====================

================================================================================

# NAV-004 · Social and map links point to bare domains (placeholders)

NAV-004 · Social and map links point to bare domains (placeholders)
Severity
Medium (Jira: Medium)
Area / type
Navigation · Content
Page
https://dentistfrontend.vercel.app/book
Steps
1. Log in.
2. Open /book and scroll to the footer.
3. Read the hrefs of the Instagram, Facebook and LinkedIn icons and of 'Get Driving
Directions' without clicking them.
Expected
Links go to the clinic's own profile pages and to a maps location for the address shown.
Actual
hrefs are https://instagram.com/, https://facebook.com/, https://linkedin.com/ and
https://maps.google.com/, with no page or location. All open in a new tab (target=_blank). The
social icons use only aria-labels.
Suggested fix
Use the real profile URLs and a maps URL for the clinic address. Add rel="noopener noreferrer".
Hide the icons until real URLs exist.

================================================================================

# PAT-001 · Patient names are not validated or normalised

PAT-001 · Patient names are not validated or normalised
Severity
Medium (Jira: Medium)
Area / type
Patient list · Bug
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Patient Directory > All Patients and page through the list
Expected
Names are stored in a consistent format and junk input is rejected
Actual
25 of 40 names are all lowercase ('saif haider'), one is mixed ('salman ALI'), 'xx yy' was accepted,
and the name field accepts HTML tags, emoji and digits.
Suggested fix
Trim and title-case names on save, reject digits/markup, and require at least 2 letters per name.



==================== PAGE 33 ====================

================================================================================

# PAT-002 · Patient list cannot be sorted (no A–Z, age or date options)

PAT-002 · Patient list cannot be sorted (no A–Z, age or date options)
Severity
Medium (Jira: Medium)
Area / type
Patient list · UX
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Patient Directory > All Patients
Expected
Sort by name (A–Z / Z–A), patient ID and registration date
Actual
The list is fixed in descending patient-ID order with no sort control; 40 patients are split into 8
pages of 5, so finding a name means paging or searching.
Suggested fix
Add a sort menu (default A–Z by last name) and a larger page size (20–50).



==================== PAGE 34 ====================

================================================================================

# PAT-003 · Bad and test data visible in the patient list (Age 2025, duplicates, test records)

PAT-003 · Bad and test data visible in the patient list (Age 2025, duplicates, test records)
Severity
Medium (Jira: Medium)
Area / type
Patient list · Bug
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Patient Directory > All Patients and page through the list
Expected
Ages are computed from a valid DOB; duplicates are flagged; no test data
Actual
'Test Patient' (#37) shows 'Age: 2025' (missing DOB treated as year 0); 'Tariq Mehmood' exists twice
(#13, #2); test records such as 'Test Patient', 'Sarah Connor' and 'xx yy'; patient IDs jump from #46
to #1044.
Suggested fix
Show 'DOB not set' when the DOB is missing, add duplicate detection (name + phone), and clean
seed data.

================================================================================

# PERF-001 · App keeps polling localhost:5050 / 5066 (hardware bridge) on every computer

PERF-001 · App keeps polling localhost:5050 / 5066 (hardware bridge) on every computer
Severity
Medium (Jira: Medium)
Area / type
Performance · Bug
Page
https://dentistfrontend.vercel.app/directory
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open browser dev tools > Network
3. Stay on the dashboard for a few minutes
Expected
Hardware status is checked only when a device is configured, with back-off
Actual
Requests to http://localhost:5050 and :5066 (/nanopix/status, /api/logs) repeat constantly and fail
— 289 requests within a few minutes. Newer browsers may also prompt users about local network
access.
Suggested fix
Only poll when the chairside hardware is enabled, and back off after failures.



==================== PAGE 35 ====================

================================================================================

# REG-005 · Phone field is hidden in a collapsed section, marked optional, and has no

REG-005 · Phone field is hidden in a collapsed section, marked optional, and has no
country-code format hint
Severity
Medium (Jira: Medium)
Area / type
Registration · UX
Page
https://dentistfrontend.vercel.app/new-patient
Steps
1. Log in as staff.
2. Go to Patient Directory and click 'Add Patient'.
3. Look for the phone field on the form.
4. Expand 'Extended Clinical & Contact Info' and read the phone label and placeholder.
Expected
Phone, the primary contact and duplicate-check key, is visible in the main section with a format
hint such as '92 3XX XXXXXXX'. Alternatively it is clearly required.
Actual
Phone is inside the collapsed section labelled '100% Optional' and is labelled '(optional)'. The
placeholder is 'e.g. 0321 4455667 (Optional)', a local format that contradicts the 92 country-code
format. There is no format hint.
Suggested fix
Move phone to the main section. Add helper text and an input mask for the 92 country-code
format, and make the placeholder consistent with the format actually stored. Decide whether
phone is required.



==================== PAGE 36 ====================

================================================================================

# REG-006 · No length limits on patient fields; a long name breaks the form layout

REG-006 · No length limits on patient fields; a long name breaks the form layout
Severity
Medium (Jira: Medium)
Area / type
Registration · Bug
Page
https://dentistfrontend.vercel.app/new-patient
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open Add Patient
3. Type a 284-character first name
Expected
Field stops at a sensible limit (e.g. 50 characters) with a counter or message
Actual
All 284 characters are accepted (no maxlength on any field); the live preview heading grows to
1,740 px and pushes the form sideways, cutting off its labels.
Suggested fix
Add maxlength and server-side limits to every field; wrap or truncate long text in the preview.



==================== PAGE 37 ====================

================================================================================

# APT-008 · Ambiguous 'Cancel' button in the Update Appointment Status dialog

APT-008 · Ambiguous 'Cancel' button in the Update Appointment Status dialog
Severity
Low (Jira: Low)
Area / type
Appointments · UX
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in on the Doctor / Staff tab and open Appointments > Data Ledger
2. Search 'Waqar' and open an appointment
3. Click 'Change Status'
4. Look at the buttons and the status options ('Rejected / Cancelled')
5. Click 'Cancel'
Expected
Button labels make clear whether they cancel the dialog or the appointment, for example 'Close' or
'Discard changes' for the dialog, and 'Cancel appointment' for the appointment.
Actual
'Cancel' only closes the dialog without changing anything. The appointment is cancelled by
choosing 'Rejected / Cancelled' from the dropdown, so the wording is easy to misread.
Suggested fix
Rename the dialog button to 'Close' or 'Discard'. Add a dedicated 'Cancel appointment' action with
a confirmation prompt that names the patient and slot, and label the status 'Cancelled'.



==================== PAGE 38 ====================

================================================================================

# APT-009 · Booking form labels point to the wrong fields

APT-009 · Booking form labels point to the wrong fields
Severity
Low (Jira: Low)
Area / type
Appointments · Accessibility
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff at https://dentistfrontend.vercel.app/login
2. Open New Appointment
3. Inspect the inputs with a screen reader or browser dev tools
Expected
Each label is linked to its own input
Actual
The email input is announced as 'Phone Number *' and the date/time input as 'Email Address
(Optional)'.
Suggested fix
Give every input an id and point each label's htmlFor at it.

================================================================================

# CFG-006 · Doctor name shown inconsistently ('Dr. ahmed', 'jhangir ahmed', 'Dr. jhangir

CFG-006 · Doctor name shown inconsistently ('Dr. ahmed', 'jhangir ahmed', 'Dr. jhangir
ahmed')
Severity
Low (Jira: Low)
Area / type
Configurable text · Content
Page
https://dentistfrontend.vercel.app/doctor/pricing
Steps
1. Log in as staff
2. Look at the header: it shows 'Dr. ahmed'
3. Open /doctor/pricing: intro text says 'Dr. jhangir ahmed'
4. Open /about: avatar name is 'jhangir ahmed'
Expected
One consistent, properly capitalised dentist name from the user profile.
Actual
Three different forms of the name are shown, with lowercase surnames. The /about team section
lists no dentist names at all, only generic text.
Suggested fix
Build the display name from one profile field (title, first name, last name), capitalise it, and use it
everywhere. Load team members from the database.



==================== PAGE 39 ====================

================================================================================

# CFG-007 · Clinic name varies: 'Dentia', 'Dentia Clinic', 'Dentia Clinical Systems'

CFG-007 · Clinic name varies: 'Dentia', 'Dentia Clinic', 'Dentia Clinical Systems'
Severity
Low (Jira: Low)
Area / type
Configurable text · Content
Page
https://dentistfrontend.vercel.app/login
Steps
1. Open /login and read the footer: '© 2026 Dentia Clinical Systems'
2. Log in and open any page; the footer reads 'Copyright © 2026 Dentia Clinic'
3. Note the logo text 'dentia' and the page title 'Dentia — AI Dental Practice Management…'
Expected
A single clinic name from settings used consistently.
Actual
Login footer says 'Dentia Clinical Systems'. The app footer says 'Dentia Clinic'. The title and logo
say 'Dentia'. The copyright year 2026 matches today's date but is probably also hardcoded.
Suggested fix
Define the clinic name and legal name in settings. Generate the footer text and the year
dynamically (new Date().getFullYear()).



==================== PAGE 40 ====================

================================================================================

# CFG-008 · Login page contains sample demo data and hardcoded telemetry numbers

CFG-008 · Login page contains sample demo data and hardcoded telemetry numbers
Severity
Low (Jira: Low)
Area / type
Configurable text · Content
Page
https://dentistfrontend.vercel.app/login
Steps
1. Open /login (Doctor / Staff tab)
2. Look at the demo screenshot and the stats panel
Expected
No fake patient or doctor names or old dates on a production login page. Statistics reflect real
data.
Actual
The demo screenshot shows 'Dr. Sarah J. Lee', 'OCT 27, 2023' and 'M. CHEN'. Telemetry shows '14
active / 28,490+ historical' and 'v2.6 Secure Protocol'. The username hint 'e.g. staff_user' is also
static text.
Suggested fix
Replace the sample screenshot text with neutral artwork. Remove or fetch the telemetry numbers.
Read the version from the build config.



==================== PAGE 41 ====================

================================================================================

# CFG-009 · Treatment and specialty lists hardcoded with no prices; footer specialty links all

CFG-009 · Treatment and specialty lists hardcoded with no prices; footer specialty links all
go to /treatment
Severity
Low (Jira: Low)
Area / type
Configurable text · Config
Page
https://dentistfrontend.vercel.app/treatment
Steps
1. Log in as staff
2. Open /treatment and read the service cards
3. Check the footer 'Dental Specialties' links
Expected
The service list comes from the fee schedule or treatment catalogue, and each link goes to its own
treatment.
Actual
Service names are fixed text. The footer lists five specialties (General & Preventive Care, Cosmetic
Dentistry & Veneers, Pediatric Dental Health, Dental Implants & Restorations, Laser Teeth
Whitening). These differ from the 15 specialties on the pricing page, and all five links go to
/treatment. No prices are shown.
Suggested fix
Generate the service lists from the treatment catalogue used on the pricing page, and link each
footer item to its own treatment.



==================== PAGE 42 ====================

================================================================================

# NAV-005 · Header menu is icon-only with no visible labels and several destinations are

NAV-005 · Header menu is icon-only with no visible labels and several destinations are
mislabelled or duplicated
Severity
Low (Jira: Low)
Area / type
Navigation · UX
Page
https://dentistfrontend.vercel.app/book
Steps
1. Log in.
2. Open /book.
3. Look at the header menu.
Expected
Menu items show text labels or accessible names, and items go to distinct destinations.
Actual
The 9 icons have only title tooltips and no visible text. 'Our Doctors', 'About Us' and 'Patient
Reviews' are in-page anchors on the home page (/#team, /#about, /#reviews). The footer 'About
Us' goes to /about, which is a different page. All 5 footer 'Dental Specialties' links go to the same
/treatment page. Link text is empty (title only).
Suggested fix
Add visible labels or aria-labels. Link specialties to their own sections or anchors. Make 'About Us'
consistent between the header and the footer.



==================== PAGE 43 ====================

================================================================================

# NAV-006 · Deliberate logout and unauthenticated access both show 'Session Expired' on

NAV-006 · Deliberate logout and unauthenticated access both show 'Session Expired' on
the login page
Severity
Low (Jira: Low)
Area / type
Navigation · UX
Page
https://dentistfrontend.vercel.app/login?expired=true
Steps
1. Log in.
2. Click the Log Out button in the header.
3. Open https://dentistfrontend.vercel.app/directory directly.
Expected
A user who has logged out sees a neutral login page. Only an actual session timeout shows
'Session Expired'.
Actual
The URL redirects to /login?expired=true and shows a 'Session Expired' banner, which is
misleading after a normal logout.
Suggested fix
Use a plain /login redirect (or ?next=) for logged-out users and set expired=true only when a
session actually times out.



==================== PAGE 44 ====================

================================================================================

# UX-001 · Several interactive controls are very small click targets

UX-001 · Several interactive controls are very small click targets
Severity
Low (Jira: Low)
Area / type
UX · Accessibility
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff
2. Open Appointments Hub (/appointments)
3. Inspect the calendar header controls and the calendar slots
Expected
Click targets of at least 24px, ideally 32 to 44px, with labelled controls.
Actual
The page scan found small controls: 'Today' 56x20, 'Previous Month' and 'Next Month' 24x24, and
'Slot' 120x16 items. It also found 3 unlabeled interactive elements.
Suggested fix
Increase the target size and padding of these controls and add accessible labels to the unlabeled
elements.



==================== PAGE 45 ====================

================================================================================

# UX-002 · Duplicate and confusing specialist chips; doctors with '0 Appts' and the same

UX-002 · Duplicate and confusing specialist chips; doctors with '0 Appts' and the same
name
Severity
Low (Jira: Low)
Area / type
UX · Content
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff
2. Open Appointments Hub (/appointments)
3. Look at the 'Specialists' chip row above the calendar
Expected
A distinct list of doctors, with names properly capitalised and no duplicates.
Actual
'Dr. jhangir ahmed' appears several times (one with 24 Appts, others with 0). The names are lower-
case ('Dr. jhangir ahmed'), and a 'Dr. Test' chip is shown. The row is cut off at the right edge and
must be scrolled horizontally.
Suggested fix
De-duplicate doctors, normalise name casing, hide test accounts in production-like data, and make
the horizontal scroll visible or wrap the chips.



==================== PAGE 46 ====================

================================================================================

# UX-003 · Floating 'JARVIS COPILOT' widget covers page content and the 'Hardware:

UX-003 · Floating 'JARVIS COPILOT' widget covers page content and the 'Hardware:
Offline' status is unexplained
Severity
Low (Jira: Low)
Area / type
UX · UX
Page
https://dentistfrontend.vercel.app/appointments
Steps
1. Log in as staff
2. Open Appointments Hub (/appointments)
3. Look at the bottom-right corner while viewing the Day Focus & Agenda panel
Expected
Floating widgets should not hide action buttons or content. Status chips such as 'Hardware: Offline'
should say what is offline and what to do.
Actual
The fixed Jarvis widget overlaps the right-hand agenda panel, where the appointment cards'
buttons sit. A 'Hardware: Offline' chip sits in the header with no explanation.
Suggested fix
Make the widget collapsible or smaller, move it so it does not cover the agenda, and add help text
to the hardware status.
Scope and limitations
Findings were produced by an AI agent testing through the browser only (no access to source code), and should be reviewed
before being filed. Items marked Config are inferred from what the pages show: a value may already come from a setting that
has not been changed.
Not covered:
Registration: The following were not completed because the step budget ran out and the final save was stuck. Email
validation could not be tested because the form has no email field. Phone '123' outcome was not verified. Name of
spaces and name '12345' were not tested. Whether any record was created by the invalid submissions is unknown. I did
not confirm that Waqar Ahmed exists in the patient list, so I did not check the stored phone or DOB format. The
duplicate-phone check and the double-click submit check were not run, though the button does disable while saving.



==================== PAGE 47 ====================
Appointments: Past-date blocking, off-hours or Sunday booking, confirmation content check, double-booking
prevention, and the list sort order were not tested. The first two were not reached because the step budget ran out. The
last three were blocked because no booking could be created. Date and time format consistency across pages and
timezone labels could not be fully checked; the only time seen was '11:30 AM' on the calendar card.
Appointments: Not covered because of the step budget and the blocked actions: confirming that a cancellation saved;
whether cancelled appointments are hidden, labelled or filterable; calendar and patient record views of changes; whether
the original or cancelled slot can be rebooked; the undo option; and the full set of row action menus. Waqar Ahmed
already had two appointments, so I created no patient or appointment. I may have left the Oct 21 10:00 appointment
unchanged. Its status after my three Save clicks was not verified by reloading.
Navigation: Back button after a search was not tested, and only /doctor/pricing was explicitly reloaded, so reload and
direct-open coverage is partial. The tel:/mailto: check found no such links. The evidence_step values are approximate,
because my notes did not record exact step numbers for all checks. The WhatsApp menu link path contained a segment
that my tooling masked, so I could not read that URL and did not assess it.
Configurable text: The WhatsApp gateway page content and the directory and appointment pages were opened only
briefly, so their texts and time slots were not recorded in detail. I did not open the appointment booking form or patient
forms, since this check was read-only. I did not check external links. I read their addresses only. The step budget ran out
before a second pass. Some evidence_step values are null because I could not match them to an exact step number.
UX: Dashboard/home, Patient Directory content, Calendar as a separate page, Settings and Profile were not reviewed.
The agent terminated after 5 consecutive output-format failures at step 16 of 60. Kanban, Daily Timeline, Data Ledger
and Patient Lookup views were not opened. Long-page, empty-state, sorting/pagination and date-format consistency
checks were not completed. Destructive-action confirmation was not tested. The Patient Directory was not scrolled.
Desktop smoke test (manual): Phone-size layout (paused at your request; early notes: no mobile menu, page 46 px
wider than the screen, most tap targets under 32 px). WhatsApp gateway page, invoices and odontogram were not
tested.

================================================================================

