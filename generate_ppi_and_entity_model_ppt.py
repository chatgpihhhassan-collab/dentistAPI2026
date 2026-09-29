import os
import sys
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

def build_presentation():
    prs = Presentation()
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6]

    # Theme Palette
    C_NAVY_DARK   = RGBColor(15, 23, 42)     # #0F172A
    C_NAVY_CARD   = RGBColor(24, 34, 53)     # #182235
    C_BLUE_PRI    = RGBColor(37, 99, 235)    # #2563EB
    C_BLUE_ACCENT = RGBColor(74, 124, 210)   # #4A7CD2
    C_BLUE_LIGHT  = RGBColor(234, 240, 252)  # #EAF0FC
    C_TEAL_DARK   = RGBColor(14, 138, 128)   # #0E8A80
    C_TEAL_LIGHT  = RGBColor(204, 251, 241)  # #CCFBF1
    C_EMERALD     = RGBColor(16, 185, 129)   # #10B981
    C_EMERALD_BG  = RGBColor(236, 253, 245)  # #ECFDF5
    C_WHITE       = RGBColor(255, 255, 255)
    C_BG_PAGE     = RGBColor(248, 250, 252)  # #F8FAFC
    C_TEXT_DARK   = RGBColor(15, 23, 42)
    C_TEXT_MUTED  = RGBColor(100, 116, 139)  # #64748B
    C_BORDER      = RGBColor(226, 232, 240)  # #E2E8F0
    C_PURPLE      = RGBColor(147, 51, 234)   # #9333EA
    C_PURPLE_LIGHT= RGBColor(243, 232, 255)

    def set_slide_background(slide, color=C_BG_PAGE):
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(7.5))
        bg.fill.solid()
        bg.fill.fore_color.rgb = color
        bg.line.fill.background()
        return bg

    def add_header(slide, title_text, category="DENTIA DENTAL WORKSPACE • ARCHITECTURE & SECURITY SPECIFICATION"):
        bar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(0.12))
        bar.fill.solid()
        bar.fill.fore_color.rgb = C_BLUE_PRI
        bar.line.fill.background()

        cat_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.35), Inches(11.7), Inches(0.28))
        tf_cat = cat_box.text_frame
        p_cat = tf_cat.paragraphs[0]
        p_cat.text = category.upper()
        p_cat.font.size = Pt(9.5)
        p_cat.font.bold = True
        p_cat.font.color.rgb = C_BLUE_PRI

        title_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.62), Inches(11.7), Inches(0.55))
        tf_title = title_box.text_frame
        p_title = tf_title.paragraphs[0]
        p_title.text = title_text
        p_title.font.size = Pt(21)
        p_title.font.bold = True
        p_title.font.color.rgb = C_NAVY_DARK

    def add_card(slide, left, top, width, height, bg_color=C_WHITE, border_color=C_BORDER):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = bg_color
        card.line.color.rgb = border_color
        card.line.width = Pt(1.1)
        return card

    # =========================================================================
    # SLIDE 1: TITLE SLIDE (Dark Premium)
    # =========================================================================
    s1 = prs.slides.add_slide(blank_layout)
    set_slide_background(s1, C_NAVY_DARK)

    # Gradient accent bar
    top_bar = s1.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(0.18))
    top_bar.fill.solid()
    top_bar.fill.fore_color.rgb = C_BLUE_ACCENT
    top_bar.line.fill.background()

    # Title Card
    c_title = add_card(s1, Inches(1.2), Inches(1.2), Inches(10.933), Inches(5.1), C_NAVY_CARD, C_BLUE_PRI)
    
    tbox = s1.shapes.add_textbox(Inches(1.6), Inches(1.6), Inches(10.1), Inches(4.3))
    tf = tbox.text_frame
    tf.word_wrap = True

    p0 = tf.paragraphs[0]
    p0.text = "CLINICAL ENTERPRISE ARCHITECTURE & SYSTEM BLUEPRINT"
    p0.font.size = Pt(11)
    p0.font.bold = True
    p0.font.color.rgb = C_EMERALD
    p0.space_after = Pt(12)

    p1 = tf.add_paragraph()
    p1.text = "Dentia Workspace: PPI Architecture & Entity Model"
    p1.font.size = Pt(28)
    p1.font.bold = True
    p1.font.color.rgb = C_WHITE
    p1.space_after = Pt(8)

    p2 = tf.add_paragraph()
    p2.text = "Full Stack Integration • Patient-Provider Interface (PPI) • Patient Protected Information (PII/Security) • 17-Table SQL Server Entity Relationship Model"
    p2.font.size = Pt(13)
    p2.font.color.rgb = C_BLUE_LIGHT
    p2.space_after = Pt(24)

    p3 = tf.add_paragraph()
    p3.text = "• Frontend: React 19 + Vite 8 + Three.js 3D Odontogram + Web Speech API + Dual Geocoding"
    p3.font.size = Pt(11)
    p3.font.color.rgb = RGBColor(203, 213, 225)

    p4 = tf.add_paragraph()
    p4.text = "• Backend API: .NET 10 Web API + Dapper Micro-ORM + SQL Server (dentist.* schema)"
    p4.font.size = Pt(11)
    p4.font.color.rgb = RGBColor(203, 213, 225)

    p5 = tf.add_paragraph()
    p5.text = "• AI Scribing: Google Vertex AI Gemini 1.5 + Groq LPU + Local Offline LLM Failover"
    p5.font.size = Pt(11)
    p5.font.color.rgb = RGBColor(203, 213, 225)
    p5.space_after = Pt(24)

    p6 = tf.add_paragraph()
    p6.text = "Confidential Clinical Presentation • Dentia Health Systems • September 2026"
    p6.font.size = Pt(10)
    p6.font.bold = True
    p6.font.color.rgb = C_TEXT_MUTED

    # =========================================================================
    # SLIDE 2: END-TO-END PLATFORM ARCHITECTURE
    # =========================================================================
    s2 = prs.slides.add_slide(blank_layout)
    set_slide_background(s2)
    add_header(s2, "End-to-End System Topology & Tech Stack Integration", "SYSTEM ARCHITECTURE")

    cols = [
        ("1. Presentation Layer (Vercel / SPA)", [
            ("Framework", "React 19, Vite 8, React Router v7"),
            ("Design System", "Tailwind CSS v4, Glassmorphism, Urbanist font"),
            ("3D Graphics", "Three.js, React Three Fiber, Drei 3D Jaw"),
            ("Voice Engine", "Web Speech API (SpeechRecognition + Synthesis)"),
            ("Geocoding", "Dual Nominatim (OSM) + Mapbox REST API"),
            ("Reporting", "jsPDF, html2canvas (Multi-page SOAP reports)"),
            ("Telemetry", "Live audio sync & AI voice assistant feedback")
        ], C_BLUE_PRI, C_BLUE_LIGHT),

        ("2. Application API Layer (.NET 10)", [
            ("Runtime", "ASP.NET Core .NET 10.0 Web API"),
            ("Micro-ORM", "Dapper 2.1 (Sub-millisecond data access)"),
            ("Data Access", "Microsoft.Data.SqlClient (Direct SQL Server)"),
            ("Security", "BCrypt.Net-Next (Salted Auth) + Role-Based JWT"),
            ("Controllers", "Patients, Appointments, Scribe, Chat, X-Ray"),
            ("Doc Engine", "OpenAPI, Swagger Interactive Sandbox"),
            ("Audio Ingest", "Multipart audio streaming + SHA256 hashes")
        ], C_TEAL_DARK, C_TEAL_LIGHT),

        ("3. Intelligence & Database Layer", [
            ("Cloud AI", "Google Vertex AI Gemini 1.5 Flash Scribe"),
            ("LPU Inference", "Groq LPU (Llama-3-70b / Mixtral sub-second)"),
            ("Offline Failover", "Local Ollama LLM endpoint (HIPAA air-gap)"),
            ("Database", "Microsoft SQL Server 2022 (dentist.* schema)"),
            ("Table Count", "17 High-Performance normalized tables"),
            ("Auditing", "SHA256 Prompt/Output hashes in AIAuditLogs"),
            ("Safety Layer", "AIWarnings automated contraindication alerts")
        ], C_PURPLE, C_PURPLE_LIGHT)
    ]

    for i, (col_title, items, border_c, fill_c) in enumerate(cols):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s2, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, border_c)
        
        # Header banner inside card
        banner = s2.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.65))
        banner.fill.solid()
        banner.fill.fore_color.rgb = border_c
        banner.line.fill.background()
        tf_b = banner.text_frame
        p_b = tf_b.paragraphs[0]
        p_b.text = col_title
        p_b.font.size = Pt(11.5)
        p_b.font.bold = True
        p_b.font.color.rgb = C_WHITE
        p_b.alignment = PP_ALIGN.CENTER

        tb = s2.shapes.add_textbox(left + Inches(0.2), Inches(2.1), Inches(3.38), Inches(4.7))
        tf = tb.text_frame
        tf.word_wrap = True
        for k, v in items:
            p = tf.add_paragraph() if tf.paragraphs[0].text else tf.paragraphs[0]
            p.text = f"• {k}: "
            p.font.size = Pt(10)
            p.font.bold = True
            p.font.color.rgb = C_NAVY_DARK
            run = p.add_run()
            run.text = v
            run.font.bold = False
            run.font.color.rgb = C_TEXT_MUTED
            p.space_after = Pt(10)

    # =========================================================================
    # SLIDE 3: PPI PART 1 - PATIENT-PROVIDER INTERFACE (WORKFLOW)
    # =========================================================================
    s3 = prs.slides.add_slide(blank_layout)
    set_slide_background(s3)
    add_header(s3, "How PPI Works: Patient-Provider Interface & Clinical Scribing", "CLINICAL INTERACTION PROTOCOL")

    steps = [
        ("Step 1: Patient Registration & Region Intake", 
         "• Clinician selects Healthcare Region: Pakistan (PK) or New Zealand (NZ).\n• Dual-engine address autocomplete (OpenStreetMap + Mapbox) auto-resolves street, CDA sectors (F-7, G-9), and postal codes.\n• Dentition Arch Classification adapts dynamically: Adult (1-32), Pediatric (A-T), or Mixed (6-12 Yrs).",
         C_BLUE_PRI),
        ("Step 2: Ambient Voice Consultation (PPI Speech Capture)", 
         "• In-operatory microphone streams clinician-patient consultation in real-time.\n• Audio files ingested and assigned SHA256 anti-duplicate checksums in [AudioRecordings].\n• Speech-to-text pipeline generates dual transcripts (Raw verbatim + Cleaned clinical) in [Transcripts].",
         C_TEAL_DARK),
        ("Step 3: Multi-Tier AI Synthesis & SOAP Generation", 
         "• Transcript routed through Gemini / Groq Scribe pipeline using clinical system prompts.\n• Automatically extracts 8-section SOAP record: Chief Complaint, HPI, Dental History, Objective Exams, Diagnosis, Treatment, Prescriptions, and Prognosis.",
         C_PURPLE),
        ("Step 4: Interactive Odontogram Sync & Clinician Confirmation", 
         "• Extracted tooth conditions (e.g. Tooth #19 Composite, Tooth #3 Extraction) instantly update the 3D Jaw Odontogram.\n• Clinician reviews contraindication alerts in [AIWarnings] (e.g., Penicillin allergy flag).\n• Spoken AI voice assistant announces: 'Doctor, treatment notes and chart synced successfully.'",
         C_EMERALD)
    ]

    for i, (title, body, color) in enumerate(steps):
        top = Inches(1.35 + i * 1.4)
        c = add_card(s3, Inches(0.8), top, Inches(11.733), Inches(1.25), C_WHITE, color)
        
        # Step Indicator Pill
        pill = s3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(1.0), top + Inches(0.2), Inches(3.2), Inches(0.35))
        pill.fill.solid()
        pill.fill.fore_color.rgb = color
        pill.line.fill.background()
        tf_p = pill.text_frame
        p_p = tf_p.paragraphs[0]
        p_p.text = title.split(":")[0]
        p_p.font.size = Pt(10)
        p_p.font.bold = True
        p_p.font.color.rgb = C_WHITE
        p_p.alignment = PP_ALIGN.CENTER

        # Subtitle
        sub_box = s3.shapes.add_textbox(Inches(4.35), top + Inches(0.18), Inches(8.0), Inches(0.35))
        tf_s = sub_box.text_frame
        p_s = tf_s.paragraphs[0]
        p_s.text = title.split(":")[1].strip()
        p_s.font.size = Pt(12)
        p_s.font.bold = True
        p_s.font.color.rgb = C_NAVY_DARK

        # Body
        b_box = s3.shapes.add_textbox(Inches(1.0), top + Inches(0.58), Inches(11.3), Inches(0.6))
        tf_b = b_box.text_frame
        tf_b.word_wrap = True
        p_b = tf_b.paragraphs[0]
        p_b.text = body
        p_b.font.size = Pt(9.5)
        p_b.font.color.rgb = C_TEXT_MUTED

    # =========================================================================
    # SLIDE 4: PPI PART 2 - PATIENT PROTECTED INFORMATION & HIPAA COMPLIANCE
    # =========================================================================
    s4 = prs.slides.add_slide(blank_layout)
    set_slide_background(s4)
    add_header(s4, "How PPI Works: Patient Protected Information (PII/HIPAA/GDPR)", "PRIVACY, COMPLIANCE & GOVERNANCE")

    pii_cards = [
        ("1. In-Transit & At-Rest Encryption", 
         "• TLS 1.3 Strict Transport Security on all API endpoints.\n• AES-256 transparent database encryption on SQL Server.\n• Audio recordings and X-ray blobs stored with encrypted URI tokens.\n• Sensitive credentials salted with BCrypt (Work Factor 11).",
         C_BLUE_PRI),
        ("2. SHA-256 Prompt & Response Hashing", 
         "• Every AI interaction records input and output SHA-256 hashes in [AIAuditLogs].\n• Ensures non-repudiation and detects data tampering or unauthorized modifications.\n• Version-controlled prompt catalog guarantees audit reproducibility.",
         C_TEAL_DARK),
        ("3. Clinical Safety & Contraindication Guard", 
         "• Cross-references patient allergy profile against prescribed medications.\n• Automated severity alerts (Warning, Critical, Info) logged in [AIWarnings].\n• Doctor must explicitly review and resolve critical flags before finalized sign-off.",
         C_PURPLE),
        ("4. De-Identification & Air-Gapped Fallback", 
         "• PII masking layer scrubs patient full names and phones before sending to cloud LLMs.\n• Complete local failover via OnDemandLocalLLMService allows 100% offline hospital execution.\n• Zero patient data is ever used for public AI model training.",
         C_EMERALD)
    ]

    for i, (title, body, color) in enumerate(pii_cards):
        row = i // 2
        col = i % 2
        left = Inches(0.8 + col * 5.95)
        top = Inches(1.4 + row * 2.85)
        
        c = add_card(s4, left, top, Inches(5.78), Inches(2.65), C_WHITE, color)
        
        # Header bar
        hbar = s4.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, top, Inches(5.78), Inches(0.48))
        hbar.fill.solid()
        hbar.fill.fore_color.rgb = color
        hbar.line.fill.background()
        tf_h = hbar.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(11)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE

        # Text
        tb = s4.shapes.add_textbox(left + Inches(0.2), top + Inches(0.55), Inches(5.38), Inches(2.0))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = body
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.3

    # =========================================================================
    # SLIDE 5: 17-TABLE ENTITY MODEL OVERVIEW (ERD MAP)
    # =========================================================================
    s5 = prs.slides.add_slide(blank_layout)
    set_slide_background(s5)
    add_header(s5, "Complete Database Architecture: 17 Normalized SQL Tables", "ENTITY MODEL (ERD)")

    erd_clusters = [
        ("Core Clinical & Patient Master", [
            ("Patients", "PK: PatientID | Name, DOB, Phone, Region, Dentition"),
            ("Doctors", "PK: DoctorID | Name, Email, Hash, Specialization"),
            ("Appointments", "PK: AppointmentID | FK: DoctorID | Date, Status, Reason"),
            ("ClinicalLogs", "PK: LogID | FK: PatientID, DoctorID | Action trail"),
            ("ChatHistory", "PK: ChatID | FK: PatientID | Dialogues & actions")
        ], C_BLUE_PRI),

        ("Dental Anatomy & Odontogram", [
            ("TeethState", "PK: TeethStateID | FK: PatientID | ToothNum, Condition, Surface, Hex"),
            ("TreatmentHistory", "PK: HistoryID | FK: PatientID | ToothNum, Procedure, Cost"),
            ("Radiographs", "PK: RadiographID | FK: PatientID | ImageUri, Type, AI Analysis")
        ], C_TEAL_DARK),

        ("Ambient Voice & AI Scribe Pipeline", [
            ("DentalNoteSessions", "PK: SessionId | FK: PatientId, DoctorId | Status, Timestamps"),
            ("AudioRecordings", "PK: AudioId | FK: SessionId | StorageUri, Sha256Hash"),
            ("Transcripts", "PK: TranscriptId | FK: SessionId | RawText, CleanedText"),
            ("DentalNotes", "PK: NoteId | FK: SessionId, PatientId | 8-Section SOAP"),
            ("AIAuditLogs", "PK: AuditId | FK: NoteId | ModelName, InHash, OutHash"),
            ("AIWarnings", "PK: WarningId | FK: NoteId | Severity, Message, Resolved")
        ], C_PURPLE),

        ("Prescriptions & Multi-Stage Plans", [
            ("DentalNotePrescriptions", "PK: PrescriptionId | FK: NoteId | Med, Dose, Frequency"),
            ("DentalNoteTreatmentPlans", "PK: PlanId | FK: NoteId | Stage, Procedure, EstCost"),
            ("Prescriptions", "PK: PrescriptionID | FK: PatientID, DoctorID | Medication master")
        ], C_EMERALD)
    ]

    for i, (c_title, tables, color) in enumerate(erd_clusters):
        left = Inches(0.8 + i * 2.98)
        c = add_card(s5, left, Inches(1.35), Inches(2.83), Inches(5.6), C_WHITE, color)

        # Header
        hdr = s5.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(2.83), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = c_title
        p_h.font.size = Pt(10)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s5.shapes.add_textbox(left + Inches(0.12), Inches(1.95), Inches(2.6), Inches(4.9))
        tf = tb.text_frame
        tf.word_wrap = True
        for tname, desc in tables:
            p = tf.add_paragraph() if tf.paragraphs[0].text else tf.paragraphs[0]
            p.text = f"■ [{tname}]"
            p.font.size = Pt(10)
            p.font.bold = True
            p.font.color.rgb = C_NAVY_DARK

            p_d = tf.add_paragraph()
            p_d.text = desc
            p_d.font.size = Pt(8.5)
            p_d.font.color.rgb = C_TEXT_MUTED
            p_d.space_after = Pt(8)

    # =========================================================================
    # SLIDE 6: ENTITY DEEP-DIVE: PATIENTS, DOCTORS & APPOINTMENTS
    # =========================================================================
    s6 = prs.slides.add_slide(blank_layout)
    set_slide_background(s6)
    add_header(s6, "Entity Deep-Dive: Patient Registry, Scheduling & Doctors", "ENTITY RELATIONSHIPS (PART 1)")

    e1_items = [
        ("[dentist].[Patients] (Master Demographic)", 
         "• PatientID (INT, PK, IDENTITY)\n• FirstName, LastName (NVARCHAR 50, NOT NULL)\n• DOB (DATE, NOT NULL) -> Auto calculates age & dentition\n• Gender, Phone, Address, City, Postcode\n• HealthcareRegion: 'PK' (Pakistan) or 'NZ' (New Zealand)\n• DentitionType: 'Permanent', 'Pediatric', or 'Mixed'\n• GuardianName, GuardianRelationship (For pediatric)\n• ProfileImageDataUrl (Base64 avatar or default)",
         C_BLUE_PRI),
        ("[dentist].[Doctors] (Clinician Credentials)", 
         "• DoctorID (INT, PK, IDENTITY)\n• FullName, Email (NVARCHAR 100, UNIQUE)\n• PasswordHash (BCrypt hashed string)\n• Specialization (General, Ortho, Pediatric, Endo)\n• Role ('Clinician', 'Admin', 'Chief_Surgeon')\n• Region ('PK', 'NZ')",
         C_TEAL_DARK),
        ("[dentist].[Appointments] (Clinic Scheduling)", 
         "• AppointmentID (INT, PK, IDENTITY)\n• DoctorID (INT, FK -> Doctors.DoctorID)\n• FullName, Phone, Email\n• PreferredDate (DATETIME, NOT NULL)\n• Status: 'Pending', 'Confirmed', 'Completed', 'Cancelled'\n• Reason (Procedure type / Chief complaint)\n• CreatedAt (DATETIME, DEFAULT GETDATE())",
         C_PURPLE)
    ]

    for i, (title, content, color) in enumerate(e1_items):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s6, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s6.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(10.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s6.shapes.add_textbox(left + Inches(0.2), Inches(2.0), Inches(3.38), Inches(4.8))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 7: ENTITY DEEP-DIVE: AMBIENT SCRIBE & AI AUDIT PIPELINE
    # =========================================================================
    s7 = prs.slides.add_slide(blank_layout)
    set_slide_background(s7)
    add_header(s7, "Entity Deep-Dive: Ambient Scribe Pipeline & AI Audit Hashes", "ENTITY RELATIONSHIPS (PART 2)")

    e2_items = [
        ("[DentalNoteSessions] & [AudioRecordings]", 
         "• SessionId (BIGINT, PK, IDENTITY)\n  - PatientId (FK -> Patients), DoctorId (FK -> Doctors)\n  - Status: 'Active', 'Processing', 'Approved'\n  - StartedAt, CompletedAt (DATETIME2)\n\n• AudioId (BIGINT, PK, IDENTITY)\n  - SessionId (FK -> DentalNoteSessions)\n  - StorageUri (Blob storage link)\n  - DurationSeconds, MimeType\n  - Sha256Hash: Cryptographic audio anti-tamper hash",
         C_BLUE_PRI),
        ("[Transcripts] & [DentalNotes] (SOAP)", 
         "• TranscriptId (BIGINT, PK, IDENTITY)\n  - SessionId (FK -> DentalNoteSessions)\n  - RawTranscript: Full verbatim speech\n  - CleanedTranscript: PII-masked medical speech\n\n• NoteId (BIGINT, PK, IDENTITY)\n  - 8 Structured SOAP Sections:\n    ChiefComplaint, HPI, DentalHistory, ObjectiveFindings,\n    Diagnosis, TreatmentPlan, Prescriptions, Prognosis",
         C_PURPLE),
        ("[AIAuditLogs] & [AIWarnings] (Governance)", 
         "• AuditId (BIGINT, PK, IDENTITY)\n  - NoteId (FK -> DentalNotes), UserId (FK)\n  - ModelName: 'gemini-1.5-flash' / 'llama-3-70b'\n  - PromptVersion, InputHash (SHA256), OutputHash (SHA256)\n  - CreatedAt (Immutable audit timestamp)\n\n• WarningId (BIGINT, PK, IDENTITY)\n  - Severity: 'Critical', 'Warning', 'Info'\n  - FieldName, Message, Resolved (BIT)",
         C_EMERALD)
    ]

    for i, (title, content, color) in enumerate(e2_items):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s7, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s7.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(10.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s7.shapes.add_textbox(left + Inches(0.2), Inches(2.0), Inches(3.38), Inches(4.8))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 8: ENTITY DEEP-DIVE: 3D ODONTOGRAM & TEETH STATE
    # =========================================================================
    s8 = prs.slides.add_slide(blank_layout)
    set_slide_background(s8)
    add_header(s8, "Entity Deep-Dive: 3D Teeth State, Treatments & Radiographs", "ENTITY RELATIONSHIPS (PART 3)")

    e3_items = [
        ("[dentist].[TeethState] (3D Odontogram)", 
         "• TeethStateID (INT, PK, IDENTITY)\n• PatientID (INT, FK -> Patients.PatientID)\n• ToothNumber (INT, 1-32 Universal or 11-48 FDI)\n• Condition: 'Healthy', 'Caries', 'Restoration', 'Crown', 'Endodontic', 'Extraction_Needed', 'Missing', 'Implant'\n• Surface: 'O', 'MOD', 'B', 'L', 'D', 'M' (Restoration surface)\n• ColorHex: Dynamic shader color (#EF4444, #3B82F6, etc.)\n• MobilityGrade: 0, 1, 2, 3\n• FurcationInvolvement: Class I, II, III",
         C_BLUE_PRI),
        ("[dentist].[TreatmentHistory] (Procedures)", 
         "• HistoryID (INT, PK, IDENTITY)\n• PatientID (INT, FK -> Patients.PatientID)\n• ToothNumber (INT, 1-32)\n• ProcedureName: Composite, Extraction, Crown, Root Canal\n• PerformedBy: Attending DoctorID / Clinician\n• DateTreated (DATETIME, NOT NULL)\n• Cost (DECIMAL 10,2)\n• ClinicalNotes (Post-procedure remarks)",
         C_TEAL_DARK),
        ("[dentist].[Radiographs] (Digital Imaging)", 
         "• RadiographID (INT, PK, IDENTITY)\n• PatientID (INT, FK -> Patients.PatientID)\n• ToothNumber (Nullable, if localized periapical)\n• ImageType: 'Bitewing', 'Periapical', 'Panoramic', 'CBCT'\n• ImageUri: High-resolution DICOM / JPEG storage link\n• TakenDate (DATETIME, NOT NULL)\n• AIAnalysisSummary: Computer vision bone loss & lesion notes",
         C_PURPLE)
    ]

    for i, (title, content, color) in enumerate(e3_items):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s8, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s8.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(10.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s8.shapes.add_textbox(left + Inches(0.2), Inches(2.0), Inches(3.38), Inches(4.8))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 9: 3D DENTAL ODONTOGRAM ENGINE
    # =========================================================================
    s9 = prs.slides.add_slide(blank_layout)
    set_slide_background(s9)
    add_header(s9, "Three.js & WebGL 3D Odontogram Engine Architecture", "3D VISUALIZATION ENGINE")

    features_3d = [
        ("Three.js & React Three Fiber Runtime", 
         "• Declarative scene graph managed via @react-three/fiber v9.7.\n• @react-three/drei v10.7 OrbitControls: Smooth 360° pan, zoom, and auto-centering on active tooth selection.\n• Dynamic three-point directional lighting + ambient occlusion for realistic enamel shading.",
         C_BLUE_PRI),
        ("FDI & Universal Dentition Arch Adapters", 
         "• Dual numbering support: Universal (1-32) and International FDI (11-48).\n• Automatic arch classification based on age: Adult (1-32), Pediatric Primary (A-T / 51-85), and Mixed (6-12 yrs).\n• Symmetrical maxillary and mandibular socket layout with real anatomical curvature.",
         C_TEAL_DARK),
        ("Dynamic Material Condition Shaders", 
         "• Healthy Enamel: Pearlescent natural ivory gradient (#F8FAFC).\n• Active Caries: High-visibility crimson warning (#EF4444) with pulsing indicator.\n• Composite / Amalgam: Medical cyan (#06B6D4) and silver-slate (#64748B).\n• Endodontic / Crown: Clinical royal blue (#3B82F6) and restorative gold (#F59E0B).\n• Extraction / Implant: Muted opacity (#94A3B8) with titanium screw mesh overlay.",
         C_PURPLE)
    ]

    for i, (title, content, color) in enumerate(features_3d):
        top = Inches(1.35 + i * 1.85)
        c = add_card(s9, Inches(0.8), top, Inches(11.733), Inches(1.65), C_WHITE, color)

        hdr = s9.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), top, Inches(3.6), Inches(1.65))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        tf_h.word_wrap = True
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(12)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s9.shapes.add_textbox(Inches(4.6), top + Inches(0.12), Inches(7.7), Inches(1.4))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 10: DUAL GEOCODING ENGINE
    # =========================================================================
    s10 = prs.slides.add_slide(blank_layout)
    set_slide_background(s10)
    add_header(s10, "Smart Location Intake: OpenStreetMap Nominatim + Mapbox", "PATIENT INTAKE GEOCODING")

    geo_cols = [
        ("The Challenge in Healthcare Address Search", 
         "• Standard Mapbox API fails on Pakistani CDA administrative sectors (F-6, F-7, G-9, I-8, DHA, Bahria Town) unless full city is typed.\n• External third-party Shadow DOM web components blocked manual typing of rural/custom clinic addresses.\n• Missing production environment variables caused silent search failures.",
         C_NAVY_DARK, C_BG_PAGE),
        ("Dual-Engine Geocoding Architecture", 
         "• Engine 1: OpenStreetMap Nominatim with English localization (accept-language=en) handles hyper-local sectors, suburbs, and postcodes.\n• Engine 2: Mapbox Geocoding REST API with encoded fallback token resolves highways, avenues, and global cities.\n• Real-time deduplication merges results seamlessly.",
         C_BLUE_PRI, C_BLUE_LIGHT),
        ("Clinician UI/UX Innovations", 
         "• Inline Non-Clipping Drawer: Never overlaps City/Postcode inputs and never gets cut off by parent containers.\n• 1-Click Select & Fill: Populates Street, City, and Postcode simultaneously with green '✓ Synced' animated badges.\n• Quick City Chips: 1-click pills (+ Islamabad, + Lahore, + Auckland, + Wellington).",
         C_EMERALD, C_EMERALD_BG)
    ]

    for i, (title, content, color, bg) in enumerate(geo_cols):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s10, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s10.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.65))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(11)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s10.shapes.add_textbox(left + Inches(0.2), Inches(2.1), Inches(3.38), Inches(4.6))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 11: MULTI-TIER AI SCRIBING & FAILOVER
    # =========================================================================
    s11 = prs.slides.add_slide(blank_layout)
    set_slide_background(s11)
    add_header(s11, "Multi-Tier AI Scribing: Cloud Vertex AI, Groq LPU & Offline LLM", "INTELLIGENT SCRIBING ENGINE")

    ai_tiers = [
        ("Tier 1: Google Cloud Vertex AI (Gemini 1.5 Flash)", 
         "• Primary clinical SOAP scribing engine via Google.Cloud.AIPlatform.V1.\n• Deep medical domain knowledge for anatomical tooth extraction, FDI mapping, and dosage verification.\n• Latency: ~1.2s - 2.0s | High semantic precision on complex multi-procedure consults.",
         C_BLUE_PRI),
        ("Tier 2: Groq LPU Ultra-Fast Inference (Llama-3-70b)", 
         "• Sub-second conversational sync via GroqAIService.cs.\n• Powers the live Intake AI Assistant and real-time operatory dictation.\n• Latency: ~300ms - 600ms | Immediate doctor voice feedback without lagging.",
         C_PURPLE),
        ("Tier 3: On-Demand Local LLM Failover (Ollama Air-Gap)", 
         "• Offline resilience via OnDemandLocalLLMService.cs.\n• Automatically activates if internet connectivity drops or in strict air-gapped hospital LANs.\n• 100% HIPAA/GDPR local containment: Zero audio or text packets leave the premises.",
         C_TEAL_DARK)
    ]

    for i, (title, content, color) in enumerate(ai_tiers):
        top = Inches(1.35 + i * 1.85)
        c = add_card(s11, Inches(0.8), top, Inches(11.733), Inches(1.65), C_WHITE, color)

        hdr = s11.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), top, Inches(3.6), Inches(1.65))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        tf_h.word_wrap = True
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(11.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s11.shapes.add_textbox(Inches(4.6), top + Inches(0.12), Inches(7.7), Inches(1.4))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 12: CONCLUSION & EXECUTIVE SUMMARY
    # =========================================================================
    s12 = prs.slides.add_slide(blank_layout)
    set_slide_background(s12, C_NAVY_DARK)

    # Title Card
    c_end = add_card(s12, Inches(1.2), Inches(1.2), Inches(10.933), Inches(5.1), C_NAVY_CARD, C_BLUE_PRI)
    
    tbox_e = s12.shapes.add_textbox(Inches(1.6), Inches(1.6), Inches(10.1), Inches(4.3))
    tf_e = tbox_e.text_frame
    tf_e.word_wrap = True

    p0 = tf_e.paragraphs[0]
    p0.text = "EXECUTIVE SUMMARY & SYSTEM READINESS"
    p0.font.size = Pt(11)
    p0.font.bold = True
    p0.font.color.rgb = C_EMERALD
    p0.space_after = Pt(10)

    p1 = tf_e.add_paragraph()
    p1.text = "Dentia: The Future of Digital Dentistry & Clinical AI"
    p1.font.size = Pt(26)
    p1.font.bold = True
    p1.font.color.rgb = C_WHITE
    p1.space_after = Pt(14)

    bullets = [
        "✓ Robust PPI Workflow: Seamlessly bridges Patient-Provider Interaction and Patient Protected Information.",
        "✓ 17-Table Enterprise Schema: High-performance SQL Server database with full audit hashes and safety triggers.",
        "✓ 3D Interactive Odontogram: Real-time Three.js WebGL dental arch synchronized directly with AI clinical notes.",
        "✓ Dual Geocoding & Zero-Barrier Intake: Flawless address resolution across Pakistan (PK) and New Zealand (NZ).",
        "✓ Multi-Tier AI Scribing: Cloud Vertex AI + Groq LPU + Air-Gapped Local LLM failover ensures 99.99% uptime.",
        "✓ Production Ready & Deployed: Verified on live Vercel production frontend and high-throughput .NET 10 Web API."
    ]

    for b in bullets:
        pb = tf_e.add_paragraph()
        pb.text = b
        pb.font.size = Pt(11)
        pb.font.color.rgb = C_BLUE_LIGHT
        pb.space_after = Pt(6)

    # Save
    out_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Dentia_Platform_Architecture_PPI_and_Entity_Model.pptx")
    prs.save(out_path)
    print(f"Presentation successfully saved to: {out_path}")
    return out_path

if __name__ == "__main__":
    build_presentation()
