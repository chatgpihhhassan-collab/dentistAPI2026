import os
import sys
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

def build_master_presentation():
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
    C_RED_ACCENT  = RGBColor(220, 38, 38)

    scratch_dir = "f:\\DentistApp_Theme2\\scratch"
    ppi_img = os.path.join(scratch_dir, "ppi_system_flow_diagram.png")
    erd_img = os.path.join(scratch_dir, "erd_diagram.png")

    def set_slide_background(slide, color=C_BG_PAGE):
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(7.5))
        bg.fill.solid()
        bg.fill.fore_color.rgb = color
        bg.line.fill.background()
        return bg

    def add_header(slide, title_text, category="DENTIA WORKSPACE • FULL ARCHITECTURE SPECIFICATION"):
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
    # SLIDE 1: COVER SLIDE
    # =========================================================================
    s1 = prs.slides.add_slide(blank_layout)
    set_slide_background(s1, C_NAVY_DARK)

    top_bar = s1.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(0.18))
    top_bar.fill.solid()
    top_bar.fill.fore_color.rgb = C_BLUE_ACCENT
    top_bar.line.fill.background()

    c1 = add_card(s1, Inches(1.0), Inches(1.0), Inches(11.333), Inches(5.5), C_NAVY_CARD, C_BLUE_PRI)
    tb1 = s1.shapes.add_textbox(Inches(1.5), Inches(1.4), Inches(10.333), Inches(4.7))
    tf1 = tb1.text_frame
    tf1.word_wrap = True

    p0 = tf1.paragraphs[0]
    p0.text = "ENTERPRISE DENTAL WORKSPACE • FULL TECHNICAL SPECIFICATION"
    p0.font.size = Pt(11)
    p0.font.bold = True
    p0.font.color.rgb = C_EMERALD
    p0.space_after = Pt(8)

    p1 = tf1.add_paragraph()
    p1.text = "Dentia: Tools, Skills, PPI Architecture & Entity Model"
    p1.font.size = Pt(28)
    p1.font.bold = True
    p1.font.color.rgb = C_WHITE
    p1.space_after = Pt(10)

    p2 = tf1.add_paragraph()
    p2.text = "A Complete Reference Guide on Frontend & Backend Tools, Clinical AI Skills, the Patient-Provider Interface (PPI), Patient Protected Information (PII/Security), and the 17-Table SQL Server Database Model."
    p2.font.size = Pt(12)
    p2.font.color.rgb = C_BLUE_LIGHT
    p2.space_after = Pt(20)

    p3 = tf1.add_paragraph()
    p3.text = "• WHAT TOOLS: React 19, Vite 8, Three.js 3D, .NET 10 Web API, Dapper 2.1, SQL Server 2022, Vertex AI, Groq LPU"
    p3.font.size = Pt(10.5)
    p3.font.color.rgb = RGBColor(226, 232, 240)

    p4 = tf1.add_paragraph()
    p4.text = "• WHAT SKILLS: AI Scribe SOAP Extraction, Real-Time Audio Dictation, 3D Odontogram Condition Shaders, Dual Geocoding"
    p4.font.size = Pt(10.5)
    p4.font.color.rgb = RGBColor(226, 232, 240)

    p5 = tf1.add_paragraph()
    p5.text = "• HOW PPI WORKS: Operatory Consultation Workflow, Ambient Voice Ingestion, and HIPAA/GDPR Protected Information"
    p5.font.size = Pt(10.5)
    p5.font.color.rgb = RGBColor(226, 232, 240)

    p6 = tf1.add_paragraph()
    p6.text = "• ENTITY MODEL: 17 Normalized SQL Tables with Primary Keys, Foreign Keys, and Architectural Cluster Mappings"
    p6.font.size = Pt(10.5)
    p6.font.color.rgb = RGBColor(226, 232, 240)
    p6.space_after = Pt(20)

    p7 = tf1.add_paragraph()
    p7.text = "Official Engineering & Clinical Review Document • Dentia Health Systems • September 2026"
    p7.font.size = Pt(10)
    p7.font.bold = True
    p7.font.color.rgb = C_TEXT_MUTED

    # =========================================================================
    # SLIDE 2: COMPLETE INVENTORY OF TOOLS (FRONTEND & BACKEND)
    # =========================================================================
    s2 = prs.slides.add_slide(blank_layout)
    set_slide_background(s2)
    add_header(s2, "Complete Tools Inventory: Frontend & Backend Technology Stack", "TOOLS & LIBRARIES MATRIX")

    tool_cols = [
        ("Frontend Tools & Frameworks", [
            ("React 19.2", "Core reactive UI runtime & concurrent mode"),
            ("Vite 8.2", "Ultra-fast bundler with Rolldown optimizations"),
            ("Tailwind CSS v4.3", "Modern utility styling with native CSS @theme"),
            ("Three.js & R3F", "WebGL 3D engine (@react-three/fiber & drei)"),
            ("Web Speech API", "Native browser SpeechRecognition & Synthesis"),
            ("OpenStreetMap", "Nominatim REST API for hyper-local sectors"),
            ("Mapbox REST API", "Geocoding v5 places with fallback token"),
            ("jsPDF & html2canvas", "High-res client-side SOAP report PDF export"),
            ("Recharts 3.10", "Interactive clinical stats & revenue charts"),
            ("Lucide-React", "Standardized clinical, tooth & UI icons"),
            ("Oxlint", "Rust-based ultra-fast linter for code health")
        ], C_BLUE_PRI),

        ("Backend API Tools & Infrastructure", [
            (".NET 10.0 Web API", "High-throughput modern C# REST application"),
            ("Dapper 2.1", "Micro-ORM for sub-millisecond SQL queries"),
            ("SqlClient 7.0", "Direct, optimized SQL Server database connection"),
            ("BCrypt.Net-Next", "Salted password hashing (Work Factor 11)"),
            ("OpenAPI / Swagger", "Interactive API documentation & schema explorer"),
            ("Google Cloud Vertex AI", "Google.Cloud.AIPlatform.V1 (Gemini 1.5 Scribe)"),
            ("Groq LPU SDK", "Sub-second Llama-3-70b / Mixtral inference"),
            ("On-Demand Local LLM", "Air-gapped Ollama failover for hospital LANs"),
            ("Microsoft SQL Server", "2022 Enterprise with dentist.* schema"),
            ("Multipart Streaming", "Acoustic audio file chunk ingestion pipeline"),
            ("Crypto Providers", "System.Security.Cryptography SHA256 hashes")
        ], C_TEAL_DARK)
    ]

    for i, (col_title, items, border_c) in enumerate(tool_cols):
        left = Inches(0.8 + i * 5.95)
        c = add_card(s2, left, Inches(1.35), Inches(5.78), Inches(5.6), C_WHITE, border_c)

        hdr = s2.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(5.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = border_c
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = col_title
        p_h.font.size = Pt(11.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s2.shapes.add_textbox(left + Inches(0.2), Inches(1.95), Inches(5.38), Inches(4.9))
        tf = tb.text_frame
        tf.word_wrap = True
        for name, desc in items:
            p = tf.add_paragraph() if tf.paragraphs[0].text else tf.paragraphs[0]
            p.text = f"✔ {name}: "
            p.font.size = Pt(9.5)
            p.font.bold = True
            p.font.color.rgb = C_NAVY_DARK
            run = p.add_run()
            run.text = desc
            run.font.bold = False
            run.font.color.rgb = C_TEXT_MUTED
            p.space_after = Pt(5.5)

    # =========================================================================
    # SLIDE 3: COMPLETE CLINICAL & AI SKILLS CATALOG
    # =========================================================================
    s3 = prs.slides.add_slide(blank_layout)
    set_slide_background(s3)
    add_header(s3, "Clinical AI & System Skills Catalog: Built-in Capabilities", "SYSTEM SKILLS INVENTORY")

    skills = [
        ("Skill 1: Clinical SOAP Extraction", 
         "Converts unstructured operatory speech into 8-section dental SOAP notes (Chief Complaint, HPI, Dental History, Objective Exams, Assessment, Treatment, Prescriptions, Prognosis).", C_BLUE_PRI),
        ("Skill 2: Real-Time Audio Dictation & Voice", 
         "Hands-free ambient consultation listening with live transcript generation, plus audible clinician confirmation via aiVoice.speakDoctorSuccess and error guards.", C_TEAL_DARK),
        ("Skill 3: 3D Odontogram Condition Mapping", 
         "Translates clinical findings (Caries, Composite, Amalgam, Crown, Root Canal, Extraction, Implant) into dynamic WebGL Three.js shaders on 32 individual tooth meshes.", C_PURPLE),
        ("Skill 4: Dual Geocoding Autocomplete", 
         "Auto-resolves CDA sectors (F-7, G-9, Gulberg) and New Zealand suburbs; auto-populates Address, City, and Postcode with green '✓ Synced' animated badges.", C_EMERALD),
        ("Skill 5: Dentition Arch Adaptation", 
         "Dynamically switches dental chart based on patient age: Adult (1-32 Universal / 11-48 FDI), Pediatric (A-T Primary), or Mixed Dentition (6-12 Years).", C_BLUE_ACCENT),
        ("Skill 6: Contraindication & Allergy Safety", 
         "Cross-references prescribed drugs against patient allergy history. Generates automatic severity alerts in [AIWarnings] requiring mandatory doctor sign-off.", C_RED_ACCENT),
        ("Skill 7: Cryptographic Non-Repudiation Audit", 
         "Generates SHA-256 integrity hashes for all prompt inputs and LLM outputs in [AIAuditLogs] to ensure legal HIPAA non-repudiation and prevent tampering.", C_NAVY_DARK),
        ("Skill 8: Multi-Tier Offline Failover", 
         "Resilient 3-tier routing: Primary Google Vertex AI Gemini 1.5 Flash -> Secondary Groq LPU Llama-3-70B -> Tertiary On-Demand Local LLM for 100% offline hospital LANs.", C_TEAL_DARK),
        ("Skill 9: Automated Signed PDF Reporting", 
         "Compiles patient history, dental jaw diagrams, condition summaries, and clinic branding into a printable/downloadable multi-page PDF via jsPDF & html2canvas.", C_PURPLE)
    ]

    for i, (title, desc, color) in enumerate(skills):
        row = i // 3
        col = i % 3
        left = Inches(0.8 + col * 3.98)
        top = Inches(1.35 + row * 1.88)

        c = add_card(s3, left, top, Inches(3.78), Inches(1.75), C_WHITE, color)

        hdr = s3.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, top, Inches(3.78), Inches(0.38))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(9.5)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s3.shapes.add_textbox(left + Inches(0.15), top + Inches(0.42), Inches(3.48), Inches(1.25))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = desc
        p.font.size = Pt(8.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.25

    # =========================================================================
    # SLIDE 4: VISUAL PPI SYSTEM DATA FLOW DIAGRAM (EMBEDDED IMAGE)
    # =========================================================================
    s4 = prs.slides.add_slide(blank_layout)
    set_slide_background(s4)
    add_header(s4, "System Data Flow Diagram: Patient-Provider Interface (PPI)", "VISUAL ARCHITECTURE DIAGRAM")

    if os.path.exists(ppi_img):
        s4.shapes.add_picture(ppi_img, Inches(0.8), Inches(1.35), Inches(11.733), Inches(5.6))
    else:
        # Fallback card
        c = add_card(s4, Inches(0.8), Inches(1.35), Inches(11.733), Inches(5.6), C_WHITE, C_BLUE_PRI)

    # =========================================================================
    # SLIDE 5: HOW PPI WORKS - PATIENT-PROVIDER INTERFACE DETAILS
    # =========================================================================
    s5 = prs.slides.add_slide(blank_layout)
    set_slide_background(s5)
    add_header(s5, "How PPI Works: Live Operatory Workflow & Speech Interaction", "PATIENT-PROVIDER INTERFACE (PPI)")

    ppi_stages = [
        ("Phase 1: Zero-Friction Patient Intake",
         "• Clinician opens NewPatientPage with smart country toggle (PK / NZ).\n• Dual-engine geocoding auto-completes street address, district, and postcode.\n• Age auto-calculates from Date of Birth, adapting dentition arch to Adult (1-32), Pediatric (A-T), or Mixed.",
         C_BLUE_PRI),
        ("Phase 2: Ambient Voice Scribing in Operatory",
         "• Operatory microphone continuously streams doctor-patient consultation.\n• Audio ingested as chunked streams to .NET 10 API; assigned SHA-256 deduplication checksum.\n• Web Speech API and backend SpeechToTextService produce raw and cleaned transcripts.",
         C_TEAL_DARK),
        ("Phase 3: Multi-Tier AI SOAP Note Synthesis",
         "• Transcripts routed to Gemini 1.5 Flash or Groq LPU with specialized dental prompts.\n• Automatically extracts 8-section clinical SOAP record including diagnosis and treatment plan.\n• Prescription medications and dosages mapped into [DentalNotePrescriptions].",
         C_PURPLE),
        ("Phase 4: 3D Odontogram Synchronization",
         "• Extracted findings (e.g. Tooth #14 MOD Caries, Tooth #30 Root Canal) immediately update [TeethState].\n• Three.js WebGL canvas re-renders individual tooth materials with color-coded shaders.\n• AI voice assistant speaks aloud: 'Doctor, treatment notes and chart synced successfully.'",
         C_EMERALD)
    ]

    for i, (stitle, sbody, scolor) in enumerate(ppi_stages):
        top = Inches(1.35 + i * 1.4)
        c = add_card(s5, Inches(0.8), top, Inches(11.733), Inches(1.25), C_WHITE, scolor)

        pill = s5.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(1.0), top + Inches(0.18), Inches(3.4), Inches(0.32))
        pill.fill.solid()
        pill.fill.fore_color.rgb = scolor
        pill.line.fill.background()
        tf_p = pill.text_frame
        p_p = tf_p.paragraphs[0]
        p_p.text = stitle.split(":")[0]
        p_p.font.size = Pt(10)
        p_p.font.bold = True
        p_p.font.color.rgb = C_WHITE
        p_p.alignment = PP_ALIGN.CENTER

        sub = s5.shapes.add_textbox(Inches(4.6), top + Inches(0.15), Inches(7.7), Inches(0.35))
        tf_s = sub.text_frame
        p_s = tf_s.paragraphs[0]
        p_s.text = stitle.split(":")[1].strip()
        p_s.font.size = Pt(11.5)
        p_s.font.bold = True
        p_s.font.color.rgb = C_NAVY_DARK

        tb = s5.shapes.add_textbox(Inches(1.0), top + Inches(0.55), Inches(11.3), Inches(0.65))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = sbody
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_TEXT_MUTED
        p.line_spacing = 1.3

    # =========================================================================
    # SLIDE 6: HOW PPI WORKS - PATIENT PROTECTED INFORMATION (SECURITY)
    # =========================================================================
    s6 = prs.slides.add_slide(blank_layout)
    set_slide_background(s6)
    add_header(s6, "How PPI Works: Patient Protected Information (PII/HIPAA/GDPR)", "SECURITY & COMPLIANCE")

    pii_pillars = [
        ("1. In-Transit & At-Rest Encryption", 
         "• Strict TLS 1.3 transport security across all frontend-to-backend REST API endpoints.\n• AES-256 Transparent Data Encryption (TDE) on SQL Server physical data pages.\n• Digital X-ray blobs and voice recordings stored with signed, expiring URI tokens.\n• Doctor passwords salted and hashed using BCrypt.Net-Next (Work Factor 11).", C_BLUE_PRI),
        
        ("2. Cryptographic Non-Repudiation (SHA-256)", 
         "• Every AI interaction computes and logs InputHash and OutputHash in [AIAuditLogs].\n• Proves that clinical records generated by AI were not tampered with after the consultation.\n• Full audit trail maps every change to DoctorID, NoteId, ModelName, and Timestamp.", C_TEAL_DARK),

        ("3. Automated Contraindication Safety Interceptor", 
         "• Real-time rule engine cross-references prescribed medications with patient allergy history.\n• Discrepancies (e.g. Amoxicillin prescribed to Penicillin-allergic patient) trigger [AIWarnings].\n• Clinical system blocks finalized sign-off until doctor reviews and marks flag as resolved.", C_RED_ACCENT),

        ("4. PII Masking & Air-Gapped Hospital LAN Failover", 
         "• Automated de-identification scrubs patient names, phones, and addresses before cloud routing.\n• On-Demand Local LLM Service runs Ollama locally on hospital hardware for 100% offline security.\n• Zero patient protected health data is ever retained for public AI model training.", C_PURPLE)
    ]

    for i, (title, body, color) in enumerate(pii_pillars):
        row = i // 2
        col = i % 2
        left = Inches(0.8 + col * 5.95)
        top = Inches(1.35 + row * 2.85)

        c = add_card(s6, left, top, Inches(5.78), Inches(2.68), C_WHITE, color)

        hdr = s6.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, top, Inches(5.78), Inches(0.48))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(11)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE

        tb = s6.shapes.add_textbox(left + Inches(0.2), top + Inches(0.55), Inches(5.38), Inches(2.0))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = body
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 7: VISUAL ENTITY MODEL DIAGRAM (EMBEDDED IMAGE)
    # =========================================================================
    s7 = prs.slides.add_slide(blank_layout)
    set_slide_background(s7)
    add_header(s7, "Database Entity-Relationship Model: 17 Normalized SQL Tables", "VISUAL ENTITY MODEL (ERD)")

    if os.path.exists(erd_img):
        s7.shapes.add_picture(erd_img, Inches(0.8), Inches(1.35), Inches(11.733), Inches(5.6))
    else:
        c = add_card(s7, Inches(0.8), Inches(1.35), Inches(11.733), Inches(5.6), C_WHITE, C_PURPLE)

    # =========================================================================
    # SLIDE 8: ENTITY MODEL BREAKDOWN - CLUSTERS 1 & 2
    # =========================================================================
    s8 = prs.slides.add_slide(blank_layout)
    set_slide_background(s8)
    add_header(s8, "Entity Model Breakdown: Core Patients & 3D Odontogram", "DATABASE ARCHITECTURE (PART 1)")

    c1_tables = [
        ("[dentist].[Patients] (Master Demographic)", 
         "• PatientID (INT, PK, IDENTITY)\n• FirstName, LastName (NVARCHAR 50, NOT NULL)\n• DOB (DATE, NOT NULL) -> Auto calculates age & dentition\n• Gender, Phone, Address, City, Postcode\n• HealthcareRegion: 'PK' (Pakistan) or 'NZ' (New Zealand)\n• DentitionType: 'Permanent', 'Pediatric', or 'Mixed'\n• GuardianName, GuardianRelationship (For pediatric)\n• ProfileImageDataUrl (Base64 avatar or default)", C_BLUE_PRI),

        ("[dentist].[Doctors] & [Appointments]", 
         "• Doctors (Clinician Master):\n  - DoctorID (INT, PK, IDENTITY)\n  - FullName, Email (UNIQUE), PasswordHash (BCrypt)\n  - Specialization, Role, Region\n\n• Appointments (Clinic Scheduling):\n  - AppointmentID (INT, PK, IDENTITY)\n  - DoctorID (INT, FK -> Doctors.DoctorID)\n  - FullName, Phone, PreferredDate, Status, Reason", C_TEAL_DARK),

        ("[dentist].[TeethState] & [Radiographs]", 
         "• TeethState (3D Odontogram Anatomy):\n  - TeethStateID (INT, PK, IDENTITY)\n  - PatientID (INT, FK -> Patients.PatientID)\n  - ToothNumber (1-32 Universal / 11-48 FDI)\n  - Condition, Surface, ColorHex (#EF4444, #3B82F6)\n\n• Radiographs (Digital Imaging Assets):\n  - RadiographID (INT, PK), ToothNumber\n  - ImageType ('Bitewing'/'CBCT'), ImageUri, AIAnalysisSummary", C_PURPLE)
    ]

    for i, (title, content, color) in enumerate(c1_tables):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s8, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s8.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(10)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s8.shapes.add_textbox(left + Inches(0.2), Inches(2.0), Inches(3.38), Inches(4.8))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.3

    # =========================================================================
    # SLIDE 9: ENTITY MODEL BREAKDOWN - CLUSTERS 3 & 4
    # =========================================================================
    s9 = prs.slides.add_slide(blank_layout)
    set_slide_background(s9)
    add_header(s9, "Entity Model Breakdown: Ambient Voice, AI Scribing & Plans", "DATABASE ARCHITECTURE (PART 2)")

    c2_tables = [
        ("[Sessions], [Audio] & [Transcripts]", 
         "• DentalNoteSessions (Lifecycle Container):\n  - SessionId (BIGINT, PK, IDENTITY)\n  - PatientId (FK), DoctorId (FK), Status, StartedAt\n\n• AudioRecordings (Raw Audio Ingest):\n  - AudioId (BIGINT, PK), StorageUri, Sha256Hash\n\n• Transcripts (Speech-to-Text):\n  - TranscriptId (BIGINT, PK), RawText, CleanedText", C_BLUE_PRI),

        ("[DentalNotes] & [AI Governance]", 
         "• DentalNotes (8-Section SOAP Record):\n  - NoteId (BIGINT, PK, IDENTITY)\n  - ChiefComplaint, HPI, DentalHistory, ObjectiveFindings,\n    Assessment, TreatmentRendered, TreatmentPlan, Prognosis\n\n• AIAuditLogs (Non-Repudiation Hashes):\n  - AuditId (BIGINT, PK), InputHash, OutputHash, ModelName\n\n• AIWarnings (Safety Contraindications):\n  - WarningId (BIGINT, PK), FieldName, Severity, Resolved", C_PURPLE),

        ("[Prescriptions] & [TreatmentPlans]", 
         "• DentalNotePrescriptions (Medications):\n  - PrescriptionId (BIGINT, PK), NoteId (FK)\n  - MedicationName, Strength, Dose, Frequency, Instructions\n\n• DentalNoteTreatmentPlans (Multi-Stage):\n  - PlanId (BIGINT, PK), NoteId (FK), Stage, Procedure, EstCost\n\n• Prescriptions (Pharmacy Master Chart):\n  - PrescriptionID (INT, PK), PatientID, DoctorID", C_EMERALD)
    ]

    for i, (title, content, color) in enumerate(c2_tables):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s9, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s9.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.55))
        hdr.fill.solid()
        hdr.fill.fore_color.rgb = color
        hdr.line.fill.background()
        tf_h = hdr.text_frame
        p_h = tf_h.paragraphs[0]
        p_h.text = title
        p_h.font.size = Pt(10)
        p_h.font.bold = True
        p_h.font.color.rgb = C_WHITE
        p_h.alignment = PP_ALIGN.CENTER

        tb = s9.shapes.add_textbox(left + Inches(0.2), Inches(2.0), Inches(3.38), Inches(4.8))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.3

    # =========================================================================
    # SLIDE 10: 3D DENTAL ODONTOGRAM ENGINE TOOLING
    # =========================================================================
    s10 = prs.slides.add_slide(blank_layout)
    set_slide_background(s10)
    add_header(s10, "3D Dental Odontogram: Three.js, R3F & Dynamic Shader Engine", "3D GRAPHICS ARCHITECTURE")

    tech_3d = [
        ("WebGL Scene & Camera Architecture", 
         "• Managed via @react-three/fiber v9.7 declarative React canvas.\n• @react-three/drei OrbitControls enables smooth 360° rotation, pitch, and zoom.\n• Auto-focus raycasting zooms smoothly to inspected tooth when selected in UI.", C_BLUE_PRI),
        ("Universal (1-32) & FDI (11-48) Adapters", 
         "• Supports both Universal Numbering System (USA standard) and FDI Two-Digit (International standard).\n• Automatic arch morphing: Adult (32 teeth), Pediatric primary (20 teeth A-T), or Mixed dentition.", C_TEAL_DARK),
        ("Dynamic Condition Shaders & Texturing", 
         "• Caries / Cavity: High-visibility pulsing crimson shader (#EF4444).\n• Composite / Amalgam: Medical cyan (#06B6D4) and metallic silver (#64748B).\n• Crown / Endodontic: Clinical royal blue (#3B82F6) and restorative gold (#F59E0B).\n• Missing / Extraction / Implant: Alpha transparency (#94A3B8) with titanium screw anchor mesh.", C_PURPLE)
    ]

    for i, (title, content, color) in enumerate(tech_3d):
        top = Inches(1.35 + i * 1.85)
        c = add_card(s10, Inches(0.8), top, Inches(11.733), Inches(1.65), C_WHITE, color)

        hdr = s10.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), top, Inches(3.6), Inches(1.65))
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

        tb = s10.shapes.add_textbox(Inches(4.6), top + Inches(0.12), Inches(7.7), Inches(1.4))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 11: DUAL GEOCODING ENGINE TOOLING & INTAKE
    # =========================================================================
    s11 = prs.slides.add_slide(blank_layout)
    set_slide_background(s11)
    add_header(s11, "Dual Geocoding Intake: OpenStreetMap Nominatim + Mapbox", "LOCATION INTELLIGENCE")

    geo_cards = [
        ("The Clinical Challenge", 
         "• Standard Mapbox API failed on Pakistani CDA administrative sectors (F-6, F-7, G-9, DHA, Bahria Town) without explicit city name.\n• External shadow DOM web components blocked manual typing of rural/custom clinic addresses.\n• Missing production environment variables caused silent search failures.", C_NAVY_DARK),

        ("Dual Geocoding Architecture", 
         "• Engine 1: OpenStreetMap Nominatim with English localization (accept-language=en) handles hyper-local sectors, suburbs, and postcodes.\n• Engine 2: Mapbox Geocoding REST API with encoded fallback token resolves highways, avenues, and global cities.\n• Real-time deduplication merges results seamlessly.", C_BLUE_PRI),

        ("Clinician UI/UX Innovations", 
         "• Inline Non-Clipping Drawer: Expands right beneath the search input without covering City/Postcode fields or getting clipped by parent containers.\n• 1-Click Select & Fill: Populates Street, City, and Postcode simultaneously with green '✓ Synced' animated badges.\n• Quick City Chips: 1-click pills (+ Islamabad, + Lahore, + Auckland, + Wellington).", C_EMERALD)
    ]

    for i, (title, content, color) in enumerate(geo_cards):
        left = Inches(0.8 + i * 3.98)
        c = add_card(s11, left, Inches(1.35), Inches(3.78), Inches(5.6), C_WHITE, color)

        hdr = s11.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, Inches(1.35), Inches(3.78), Inches(0.65))
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

        tb = s11.shapes.add_textbox(left + Inches(0.2), Inches(2.1), Inches(3.38), Inches(4.6))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 12: MULTI-TIER AI SCRIBING & FAILOVER
    # =========================================================================
    s12 = prs.slides.add_slide(blank_layout)
    set_slide_background(s12)
    add_header(s12, "Multi-Tier AI Scribing: Cloud Vertex AI, Groq LPU & Offline LLM", "AI ORCHESTRATION")

    ai_cards = [
        ("Tier 1: Google Cloud Vertex AI (Gemini 1.5 Flash)", 
         "• Primary clinical SOAP scribing engine via Google.Cloud.AIPlatform.V1.\n• Deep medical domain knowledge for anatomical tooth extraction, FDI mapping, and dosage verification.\n• Latency: ~1.2s - 2.0s | High semantic precision on complex multi-procedure consults.", C_BLUE_PRI),

        ("Tier 2: Groq LPU Ultra-Fast Inference (Llama-3-70b)", 
         "• Sub-second conversational sync via GroqAIService.cs.\n• Powers the live Intake AI Assistant and real-time operatory dictation.\n• Latency: ~300ms - 600ms | Immediate doctor voice feedback without lagging.", C_PURPLE),

        ("Tier 3: On-Demand Local LLM Failover (Ollama Air-Gap)", 
         "• Offline resilience via OnDemandLocalLLMService.cs.\n• Automatically activates if internet connectivity drops or in strict air-gapped hospital LANs.\n• 100% HIPAA/GDPR local containment: Zero audio or text packets leave the premises.", C_TEAL_DARK)
    ]

    for i, (title, content, color) in enumerate(ai_cards):
        top = Inches(1.35 + i * 1.85)
        c = add_card(s12, Inches(0.8), top, Inches(11.733), Inches(1.65), C_WHITE, color)

        hdr = s12.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), top, Inches(3.6), Inches(1.65))
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

        tb = s12.shapes.add_textbox(Inches(4.6), top + Inches(0.12), Inches(7.7), Inches(1.4))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = content
        p.font.size = Pt(10)
        p.font.color.rgb = C_NAVY_DARK
        p.line_spacing = 1.35

    # =========================================================================
    # SLIDE 13: HOW TOOLS AND SKILLS WORK TOGETHER AT RUNTIME
    # =========================================================================
    s13 = prs.slides.add_slide(blank_layout)
    set_slide_background(s13)
    add_header(s13, "Runtime Integration: How Tools and Skills Connect End-to-End", "SYSTEM INTEGRATION SEQUENCE")

    runtime_steps = [
        ("Step 1: Intake & Geocoding", "React 19 + Dual Geocoding", 
         "Doctor types address -> OpenStreetMap & Mapbox query in parallel -> Results populate Street, City, Postcode -> Dentition arch adapts to age.", C_BLUE_PRI),
        
        ("Step 2: Voice Dictation Ingest", "Web Speech + .NET 10 API", 
         "Microphone streams audio -> Web Speech transcribes in real-time -> .NET API calculates SHA-256 hash -> [AudioRecordings] & [Transcripts] saved.", C_TEAL_DARK),

        ("Step 3: AI Scribing & SOAP Compilation", "Vertex AI / Groq LPU", 
         "Cleaned transcript sent to Gemini 1.5 Flash / Groq -> 8-section SOAP compiled -> Drugs parsed to [DentalNotePrescriptions] -> Hashes logged to [AIAuditLogs].", C_PURPLE),

        ("Step 4: Safety Check & 3D Sync", "Three.js + [AIWarnings]", 
         "Allergies cross-referenced in [AIWarnings] -> Tooth conditions mapped to Three.js WebGL canvas -> Tooth colors update -> Spoken voice confirms success.", C_EMERALD)
    ]

    for i, (title, tech, desc, color) in enumerate(runtime_steps):
        top = Inches(1.35 + i * 1.4)
        c = add_card(s13, Inches(0.8), top, Inches(11.733), Inches(1.25), C_WHITE, color)

        pill = s13.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(1.0), top + Inches(0.18), Inches(3.2), Inches(0.32))
        pill.fill.solid()
        pill.fill.fore_color.rgb = color
        pill.line.fill.background()
        tf_p = pill.text_frame
        p_p = tf_p.paragraphs[0]
        p_p.text = title
        p_p.font.size = Pt(10)
        p_p.font.bold = True
        p_p.font.color.rgb = C_WHITE
        p_p.alignment = PP_ALIGN.CENTER

        sub = s13.shapes.add_textbox(Inches(4.4), top + Inches(0.15), Inches(7.9), Inches(0.35))
        tf_s = sub.text_frame
        p_s = tf_s.paragraphs[0]
        p_s.text = f"Tools Active: {tech}"
        p_s.font.size = Pt(11)
        p_s.font.bold = True
        p_s.font.color.rgb = C_BLUE_PRI

        tb = s13.shapes.add_textbox(Inches(1.0), top + Inches(0.55), Inches(11.3), Inches(0.65))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = desc
        p.font.size = Pt(9.5)
        p.font.color.rgb = C_TEXT_MUTED
        p.line_spacing = 1.3

    # =========================================================================
    # SLIDE 14: EXECUTIVE SUMMARY & CONCLUSION
    # =========================================================================
    s14 = prs.slides.add_slide(blank_layout)
    set_slide_background(s14, C_NAVY_DARK)

    c_end = add_card(s14, Inches(1.0), Inches(1.0), Inches(11.333), Inches(5.5), C_NAVY_CARD, C_BLUE_PRI)
    tb_e = s14.shapes.add_textbox(Inches(1.5), Inches(1.4), Inches(10.333), Inches(4.7))
    tf_e = tb_e.text_frame
    tf_e.word_wrap = True

    pe0 = tf_e.paragraphs[0]
    pe0.text = "EXECUTIVE SUMMARY & SYSTEM READINESS"
    pe0.font.size = Pt(11)
    pe0.font.bold = True
    pe0.font.color.rgb = C_EMERALD
    pe0.space_after = Pt(10)

    pe1 = tf_e.add_paragraph()
    pe1.text = "Dentia: Production-Ready Dental AI Platform"
    pe1.font.size = Pt(26)
    pe1.font.bold = True
    pe1.font.color.rgb = C_WHITE
    pe1.space_after = Pt(14)

    concl_bullets = [
        "✔ 22 Enterprise Tools: Full integration between React 19, Three.js, .NET 10 Web API, Dapper, SQL Server 2022, Vertex AI, and Groq LPU.",
        "✔ 9 Specialized Clinical Skills: Ambient SOAP scribing, 3D anatomical odontogram, voice dictation, allergy warnings, and audit hashing.",
        "✔ Robust PPI Architecture: Fully visual diagram mapping consultation audio to 3D chart sync and HIPAA/GDPR protected data governance.",
        "✔ 17 Normalized SQL Tables: High-performance ERD covering patient demographics, dental anatomy, voice recordings, and treatment planning.",
        "✔ Verified on Live Production: Live on Vercel production frontend and high-throughput .NET 10 Web API."
    ]

    for b in concl_bullets:
        p = tf_e.add_paragraph()
        p.text = b
        p.font.size = Pt(11)
        p.font.color.rgb = C_BLUE_LIGHT
        p.space_after = Pt(8)

    # Output Path
    out_master_path = "f:\\DentistApp_Theme2\\Dentia_Master_Architecture_Tools_Skills_PPI_and_ERD.pptx"
    prs.save(out_master_path)
    print("Master presentation successfully created at:", out_master_path)
    return out_master_path

if __name__ == "__main__":
    build_master_presentation()
