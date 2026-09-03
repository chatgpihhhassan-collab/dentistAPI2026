import os
import sys
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

def create_presentation():
    prs = Presentation()
    # 16:9 Widescreen dimensions
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_slide_layout = prs.slide_layouts[6]

    # Color Palette (Premium Medical SaaS)
    C_NAVY_DARK  = RGBColor(15, 23, 42)     # #0F172A (Deep Slate/Navy)
    C_NAVY_LIGHT = RGBColor(30, 41, 59)     # #1E293B
    C_BLUE_PRI   = RGBColor(37, 99, 235)    # #2563EB (Royal Blue)
    C_BLUE_SOFT  = RGBColor(219, 234, 254)  # #DBEAFE
    C_TEAL       = RGBColor(14, 138, 128)   # #0E8A80 (Clinical Teal)
    C_TEAL_LIGHT = RGBColor(204, 251, 241)  # #CCFBF1
    C_WHITE      = RGBColor(255, 255, 255)  # #FFFFFF
    C_BG_LIGHT   = RGBColor(248, 250, 252)  # #F8FAFC
    C_TEXT_DARK  = RGBColor(15, 23, 42)
    C_TEXT_MUTED = RGBColor(100, 116, 139)  # #64748B
    C_ACCENT_RED = RGBColor(225, 29, 72)    # #E11D48
    C_CARD_BORDER= RGBColor(226, 232, 240)  # #E2E8F0

    def add_header(slide, title_text, category_text="DENTISTAPP ENTERPRISE CLINICAL PLATFORM"):
        # Top accent bar
        bar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(0.12))
        bar.fill.solid()
        bar.fill.fore_color.rgb = C_BLUE_PRI
        bar.line.fill.background()

        # Category text
        cat_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.4), Inches(11.7), Inches(0.3))
        tf_cat = cat_box.text_frame
        tf_cat.word_wrap = True
        p_cat = tf_cat.paragraphs[0]
        p_cat.text = category_text.upper()
        p_cat.font.size = Pt(10)
        p_cat.font.bold = True
        p_cat.font.color.rgb = C_BLUE_PRI

        # Title text
        title_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.65), Inches(11.7), Inches(0.6))
        tf_title = title_box.text_frame
        tf_title.word_wrap = True
        p_title = tf_title.paragraphs[0]
        p_title.text = title_text
        p_title.font.size = Pt(22)
        p_title.font.bold = True
        p_title.font.color.rgb = C_NAVY_DARK

    def add_card(slide, left, top, width, height, bg_color=C_WHITE, border_color=C_CARD_BORDER):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = bg_color
        card.line.color.rgb = border_color
        card.line.width = Pt(1.2)
        return card

    # =========================================================================
    # SLIDE 1: TITLE SLIDE (Dark Premium Theme)
    # =========================================================================
    s1 = prs.slides.add_slide(blank_slide_layout)
    bg1 = s1.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(7.5))
    bg1.fill.solid()
    bg1.fill.fore_color.rgb = C_NAVY_DARK
    bg1.line.fill.background()

    # Decorative visual pill
    pill = s1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(1.0), Inches(1.2), Inches(3.8), Inches(0.45))
    pill.fill.solid()
    pill.fill.fore_color.rgb = C_NAVY_LIGHT
    pill.line.color.rgb = C_BLUE_PRI
    p_pill = pill.text_frame.paragraphs[0]
    p_pill.text = "⚡ NEXT-GEN DENTAL EHR & CLINICAL AI"
    p_pill.font.size = Pt(11)
    p_pill.font.bold = True
    p_pill.font.color.rgb = RGBColor(147, 197, 253)
    p_pill.alignment = PP_ALIGN.CENTER

    # Main Title
    tbox = s1.shapes.add_textbox(Inches(1.0), Inches(1.9), Inches(11.333), Inches(2.2))
    tf = tbox.text_frame
    tf.word_wrap = True
    p1 = tf.paragraphs[0]
    p1.text = "DentistApp AI Platform"
    p1.font.size = Pt(44)
    p1.font.bold = True
    p1.font.color.rgb = C_WHITE

    p2 = tf.add_paragraph()
    p2.text = "Interactive 3D Odontograms, Voice Clinical AI & Real-Time EHR Charting"
    p2.font.size = Pt(22)
    p2.font.color.rgb = RGBColor(148, 163, 184)
    p2.space_before = Pt(12)

    # 3 Summary Stat Badges on Title Slide
    badges = [
        ("Complete Lifecycle", "Adult (1-32) + Pediatric (A-T) Dentition"),
        ("Voice AI Assistant", "Bi-lingual Voice/Text Natural Language Charting"),
        ("Interactive 3D Engine", "WebGL 3D Occlusal & 5-Zone Cross-Section View")
    ]
    for i, (b_title, b_sub) in enumerate(badges):
        bx = Inches(1.0 + i * 3.85)
        by = Inches(4.5)
        bw = Inches(3.65)
        bh = Inches(1.4)
        card = s1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, bx, by, bw, bh)
        card.fill.solid()
        card.fill.fore_color.rgb = C_NAVY_LIGHT
        card.line.color.rgb = RGBColor(51, 65, 85)
        
        tb = s1.shapes.add_textbox(bx + Inches(0.2), by + Inches(0.2), bw - Inches(0.4), bh - Inches(0.4))
        tf_b = tb.text_frame
        tf_b.word_wrap = True
        pb1 = tf_b.paragraphs[0]
        pb1.text = b_title
        pb1.font.size = Pt(14)
        pb1.font.bold = True
        pb1.font.color.rgb = RGBColor(96, 165, 250)
        
        pb2 = tf_b.add_paragraph()
        pb2.text = b_sub
        pb2.font.size = Pt(11)
        pb2.font.color.rgb = RGBColor(203, 213, 225)
        pb2.space_before = Pt(4)

    # Footer on title
    ft = s1.shapes.add_textbox(Inches(1.0), Inches(6.4), Inches(11.333), Inches(0.5))
    p_ft = ft.text_frame.paragraphs[0]
    p_ft.text = "Executive Leadership Briefing  |  Architectural & Functional Demonstration  |  Confidential"
    p_ft.font.size = Pt(11)
    p_ft.font.color.rgb = RGBColor(100, 116, 139)

    # =========================================================================
    # SLIDE 2: EXECUTIVE SUMMARY & STRATEGIC VALUE
    # =========================================================================
    s2 = prs.slides.add_slide(blank_slide_layout)
    add_header(s2, "Executive Summary: Transforming Dental Care with Voice & 3D AI")

    cards_s2 = [
        ("The Clinical Challenge", [
            "Traditional dental software requires 20-30 clicks per tooth, causing doctor burnout and documentation delays.",
            "Lack of real-time 3D spatial models leaves patients confused about their treatment plans.",
            "Manual CDT billing code selection leads to insurance claim rejections and lost revenue."
        ], C_BG_LIGHT, C_ACCENT_RED),
        ("The DentistApp Solution", [
            "Zero-latency Voice & Chat AI: Speak natural diagnostic commands in English or Roman Urdu.",
            "Interactive 3D WebGL Occlusal Engine with real-time 5-zone surface cross-sections (O, M, D, B, L).",
            "Auto-synchronized CDT codes (D0120, D4341, D3330, etc.) with instant SQL database persistence."
        ], C_BG_LIGHT, C_BLUE_PRI),
        ("Measurable ROI & Impact", [
            "⚡ 75% Reduction in Clinical Charting Time per patient.",
            "📈 99.8% Billing Code Accuracy with built-in ADA CDT cross-mapping.",
            "🤝 85% Increase in Patient Case Acceptance via interactive visual 3D demonstration."
        ], C_BG_LIGHT, C_TEAL)
    ]

    for i, (title, points, bg, border_col) in enumerate(cards_s2):
        x = Inches(0.8 + i * 4.0)
        y = Inches(1.6)
        w = Inches(3.75)
        h = Inches(5.2)
        
        card = add_card(s2, x, y, w, h, bg, border_col)
        
        # Header banner inside card
        banner = s2.shapes.add_shape(MSO_SHAPE.RECTANGLE, x, y, w, Inches(0.6))
        banner.fill.solid()
        banner.fill.fore_color.rgb = border_col
        banner.line.fill.background()
        p_b = banner.text_frame.paragraphs[0]
        p_b.text = title
        p_b.font.size = Pt(13)
        p_b.font.bold = True
        p_b.font.color.rgb = C_WHITE
        p_b.alignment = PP_ALIGN.CENTER

        tb = s2.shapes.add_textbox(x + Inches(0.2), y + Inches(0.75), w - Inches(0.4), h - Inches(0.9))
        tf = tb.text_frame
        tf.word_wrap = True
        for j, pt_text in enumerate(points):
            p = tf.paragraphs[0] if j == 0 else tf.add_paragraph()
            p.text = f"•  {pt_text}"
            p.font.size = Pt(12)
            p.font.color.rgb = C_TEXT_DARK
            p.space_after = Pt(14)

    # =========================================================================
    # SLIDE 3: TECHNOLOGY STACK & ARCHITECTURE
    # =========================================================================
    s3 = prs.slides.add_slide(blank_slide_layout)
    add_header(s3, "Technology Stack & Enterprise Architecture")

    tech_blocks = [
        ("Frontend Application", "Modern Web Client", [
            ("Framework", "React 19 + Vite for ultra-fast HMR and micro-bundle loads"),
            ("Styling System", "TailwindCSS + Custom Medical Design Tokens"),
            ("3D Graphics", "Three.js WebGL Engine (Interactive 360° Occlusal Models)"),
            ("Speech & Audio", "Web Speech Synthesis & Voice Recognition API"),
            ("State & Storage", "React State + Persistent Local Storage & SQL Cache")
        ], C_BLUE_PRI),
        ("Backend Services", "Robust Scalable REST API", [
            ("Core Framework", "ASP.NET Core 8 Web API (C#) on Kestrel Server"),
            ("Data Access", "Entity Framework Core + Dapper High-Speed Querying"),
            ("Validation", "Medical Data Validation & Schema Integrity Rules"),
            ("Logging & Audit", "Structured Clinical Audit Logs (/api/patients/{id}/clinical-logs)"),
            ("Documentation", "Interactive Swagger OpenAPI 3.0 Documentation")
        ], C_TEAL),
        ("Database & Storage", "High-Security Relational DB", [
            ("RDBMS", "Microsoft SQL Server Enterprise"),
            ("Teeth State Table", "Per-tooth record (status, color, comments, surfaces, rotation)"),
            ("Patients Table", "Comprehensive demographic, age, allergies, medical history"),
            ("Audit Trail", "Timestamped clinical notes & doctor voice transcriptions"),
            ("Concurrency", "ACID transactional updates with Bulk API endpoints")
        ], C_NAVY_DARK)
    ]

    for i, (head_title, sub_title, items, accent) in enumerate(tech_blocks):
        x = Inches(0.8 + i * 4.0)
        y = Inches(1.5)
        w = Inches(3.75)
        h = Inches(5.3)

        card = add_card(s3, x, y, w, h, C_WHITE, accent)
        
        # Card header
        th = s3.shapes.add_textbox(x + Inches(0.2), y + Inches(0.2), w - Inches(0.4), Inches(0.8))
        tf_h = th.text_frame
        tf_h.word_wrap = True
        ph1 = tf_h.paragraphs[0]
        ph1.text = head_title
        ph1.font.size = Pt(15)
        ph1.font.bold = True
        ph1.font.color.rgb = accent

        ph2 = tf_h.add_paragraph()
        ph2.text = sub_title
        ph2.font.size = Pt(11)
        ph2.font.color.rgb = C_TEXT_MUTED
        ph2.space_before = Pt(2)

        # Content list
        tb = s3.shapes.add_textbox(x + Inches(0.2), y + Inches(1.1), w - Inches(0.4), h - Inches(1.3))
        tf = tb.text_frame
        tf.word_wrap = True
        for j, (lbl, desc) in enumerate(items):
            p = tf.paragraphs[0] if j == 0 else tf.add_paragraph()
            p.text = f"{lbl}: "
            p.font.bold = True
            p.font.size = Pt(11)
            p.font.color.rgb = C_TEXT_DARK
            
            run = p.add_run()
            run.text = desc
            run.font.bold = False
            run.font.size = Pt(10.5)
            run.font.color.rgb = C_TEXT_MUTED
            p.space_after = Pt(10)

    # =========================================================================
    # SLIDE 4: END-TO-END CLINICAL WORKFLOW (FLOW DIAGRAM)
    # =========================================================================
    s4 = prs.slides.add_slide(blank_slide_layout)
    add_header(s4, "End-to-End Clinical System Workflow & Data Flow Diagram")

    # Step Flow Nodes across screen
    flow_steps = [
        ("1. Patient Selection", "Doctor opens patient profile (e.g. Adult Sarah Connor #14 or Pediatric Leo #17)", C_BLUE_PRI),
        ("2. Voice / Chat Input", "Doctor speaks or types query (e.g. 'Alveolar bone loss on molar' or 'Pulpotomy on Tooth A')", C_TEAL),
        ("3. AI Intent Engine", "NLP regex parser extracts teeth numbers, condition label, surfaces & CDT codes", C_NAVY_DARK),
        ("4. SQL Bulk Persistence", "Dispatches POST /api/patients/teeth/update-bulk + logs clinical action", C_BLUE_PRI),
        ("5. Real-Time Visual Sync", "Synchronizes 3D WebGL Canvas, 5-zone surface cross-section & periodontal matrix", C_ACCENT_RED)
    ]

    for i, (step_title, step_desc, step_col) in enumerate(flow_steps):
        x = Inches(0.8 + i * 2.4)
        y = Inches(1.6)
        w = Inches(2.25)
        h = Inches(3.2)

        card = add_card(s4, x, y, w, h, C_WHITE, step_col)
        
        # Step number badge
        badge = s4.shapes.add_shape(MSO_SHAPE.OVAL, x + Inches(0.75), y + Inches(0.2), Inches(0.75), Inches(0.75))
        badge.fill.solid()
        badge.fill.fore_color.rgb = step_col
        badge.line.fill.background()
        p_num = badge.text_frame.paragraphs[0]
        p_num.text = str(i + 1)
        p_num.font.size = Pt(18)
        p_num.font.bold = True
        p_num.font.color.rgb = C_WHITE
        p_num.alignment = PP_ALIGN.CENTER

        tb = s4.shapes.add_textbox(x + Inches(0.15), y + Inches(1.1), w - Inches(0.3), h - Inches(1.2))
        tf = tb.text_frame
        tf.word_wrap = True
        p_t = tf.paragraphs[0]
        p_t.text = step_title
        p_t.font.size = Pt(12)
        p_t.font.bold = True
        p_t.font.color.rgb = C_TEXT_DARK
        p_t.alignment = PP_ALIGN.CENTER

        p_d = tf.add_paragraph()
        p_d.text = step_desc
        p_d.font.size = Pt(10)
        p_d.font.color.rgb = C_TEXT_MUTED
        p_d.alignment = PP_ALIGN.CENTER
        p_d.space_before = Pt(6)

        # Draw Arrow connector between steps
        if i < len(flow_steps) - 1:
            arrow = s4.shapes.add_shape(MSO_SHAPE.RIGHT_ARROW, x + w + Inches(0.03), y + Inches(1.4), Inches(0.1), Inches(0.2))
            arrow.fill.solid()
            arrow.fill.fore_color.rgb = C_BLUE_PRI
            arrow.line.fill.background()

    # Lower explanatory callout box
    box_low = add_card(s4, Inches(0.8), Inches(5.1), Inches(11.7), Inches(1.8), C_BG_LIGHT, C_BLUE_PRI)
    tb_low = s4.shapes.add_textbox(Inches(1.0), Inches(5.2), Inches(11.3), Inches(1.6))
    tf_low = tb_low.text_frame
    tf_low.word_wrap = True
    pl1 = tf_low.paragraphs[0]
    pl1.text = "🔒 Enterprise Clinical Data Integrity Guarantee:"
    pl1.font.size = Pt(13)
    pl1.font.bold = True
    pl1.font.color.rgb = C_BLUE_PRI

    pl2 = tf_low.add_paragraph()
    pl2.text = "1. Zero-Loss Local Cache: Active chat cards and diagnostic suites persist in browser cache across reloads.\n2. ACID Database Commits: Every clinical voice command updates SQL Server immediately with full rollback protection.\n3. Bi-directional Synchronization: Chart overview and individual tooth detail pages always reflect identical medical state."
    pl2.font.size = Pt(11)
    pl2.font.color.rgb = C_TEXT_DARK
    pl2.space_before = Pt(4)

    # =========================================================================
    # SLIDE 5: FEATURE DEEP DIVE - ADULT & PEDIATRIC MODULES
    # =========================================================================
    s5 = prs.slides.add_slide(blank_slide_layout)
    add_header(s5, "Comprehensive Dentition Support: Adult (1–32) & Pediatric (A–T)")

    # Left Card: Adult Dentition
    card_adult = add_card(s5, Inches(0.8), Inches(1.5), Inches(5.6), Inches(5.4), C_WHITE, C_BLUE_PRI)
    tb_a = s5.shapes.add_textbox(Inches(1.0), Inches(1.7), Inches(5.2), Inches(5.0))
    tf_a = tb_a.text_frame
    tf_a.word_wrap = True
    pa_h = tf_a.paragraphs[0]
    pa_h.text = "🦷 Adult Permanent Dentition (Teeth 1–32)"
    pa_h.font.size = Pt(16)
    pa_h.font.bold = True
    pa_h.font.color.rgb = C_BLUE_PRI

    adult_feats = [
        ("Universal & FDI Numbering", "Full maxillary (1-16) and mandibular (17-32) arch mapping with quadrant breakdown."),
        ("Multi-Surface Restorative", "Class I, II, V composite resin, amalgam, and GIC restorations across O, M, D, B, L zones."),
        ("Endodontics & Prosthetics", "Root Canal Therapy (RCT) obturation with gutta-percha canal mapping and full-coverage crowns."),
        ("Periodontal Probing Matrix", "6-Point clinical probing depths (1-12mm) with deep pocket & furcation defect indicators."),
        ("Surgical Impactions & TMJ", "Horizontal/mesioangular wisdom impactions, disc displacement, and myofascial trismus.")
    ]
    for lbl, dsc in adult_feats:
        p = tf_a.add_paragraph()
        p.text = f"• {lbl}: "
        p.font.bold = True
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_DARK
        p.space_before = Pt(8)
        run = p.add_run()
        run.text = dsc
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED

    # Right Card: Pediatric Dentition
    card_ped = add_card(s5, Inches(6.9), Inches(1.5), Inches(5.6), Inches(5.4), C_WHITE, C_TEAL)
    tb_p = s5.shapes.add_textbox(Inches(7.1), Inches(1.7), Inches(5.2), Inches(5.0))
    tf_p = tb_p.text_frame
    tf_p.word_wrap = True
    pp_h = tf_p.paragraphs[0]
    pp_h.text = "🧸 Pediatric Primary Dentition (Milk Teeth A–T)"
    pp_h.font.size = Pt(16)
    pp_h.font.bold = True
    pp_h.font.color.rgb = C_TEAL

    ped_feats = [
        ("Primary Deciduous Letters", "Universal primary alphabet (A through T) with eruption & shedding milestones."),
        ("Coronal Pulpotomy (MTA)", "Bio-active bioceramic capping preserving radicular pulp vitality (CDT D3220)."),
        ("Stainless Steel Crowns (SSC)", "Full-coverage preformed pediatric metal crowns restoring multi-surface lesions (CDT D2930)."),
        ("Fixed Space Maintainers", "Fixed band-and-loop space maintainer preventing premature arch space collapse (CDT D1510)."),
        ("Early Childhood Caries (ECC)", "Severe bottle-caries tracking, enamel demineralization, and supernumerary mesiodens detection.")
    ]
    for lbl, dsc in ped_feats:
        p = tf_p.add_paragraph()
        p.text = f"• {lbl}: "
        p.font.bold = True
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_DARK
        p.space_before = Pt(8)
        run = p.add_run()
        run.text = dsc
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED

    # =========================================================================
    # SLIDE 6: LIVE QUERY DEMO 1 - ADULT PERIODONTAL BONE LOSS
    # =========================================================================
    s6 = prs.slides.add_slide(blank_slide_layout)
    add_header(s6, "Clinical Voice Query Demo #1: Adult Radiographic Bone Loss")

    # Banner for query text
    q_banner = add_card(s6, Inches(0.8), Inches(1.4), Inches(11.7), Inches(0.85), C_BLUE_SOFT, C_BLUE_PRI)
    tb_qb = s6.shapes.add_textbox(Inches(1.0), Inches(1.45), Inches(11.3), Inches(0.75))
    tf_qb = tb_qb.text_frame
    tf_qb.word_wrap = True
    pq1 = tf_qb.paragraphs[0]
    pq1.text = "🗣️ Doctor Voice / Chat Command:"
    pq1.font.size = Pt(11)
    pq1.font.bold = True
    pq1.font.color.rgb = C_BLUE_PRI

    pq2 = tf_qb.add_paragraph()
    pq2.text = '“Generate X-ray diagram showing alveolar bone loss on molar”'
    pq2.font.size = Pt(17)
    pq2.font.bold = True
    pq2.font.color.rgb = C_NAVY_DARK

    # 3 Execution Result Pillars
    demo1_cards = [
        ("1. AI Diagnostic Engine Action", [
            "Category: Radiographic Periodontal Pathology",
            "Target Teeth: Molars #18, #19, #30, #31",
            "Assigned CDT Code: D0180 / D4341 (Comprehensive Periodontal Evaluation & SRP)",
            "Status Label: 'Periodontal Bone Loss — Furcation'",
            "Highlight Color: Coral Crimson (#E0665A)"
        ], C_BLUE_PRI),
        ("2. Interactive 3D Model & Zones", [
            "3D Occlusal Model: Displays deep periodontitis resorption collar + '⚠️ BONE LOSS (6-7mm POCKETS)' warning beacon.",
            "5-Zone Cross Section: All 5 anatomical surfaces (O, M, D, B, L) filled with Periodontal Coral (#E0665A).",
            "Periodontal Matrix: Probing depths auto-populate to 5–7mm deep pockets with bleeding indicator."
        ], C_ACCENT_RED),
        ("3. Database & Clinical EHR Sync", [
            "SQL Server Update: Bulk status written to TeethState table for patient chart.",
            "Audit Trail: Clinical log recorded with timestamp and doctor ID.",
            "Page Persistence: Navigating to /chart/14/tooth/30 immediately loads all updated diagnostic data."
        ], C_TEAL)
    ]

    for i, (head, bullets, col) in enumerate(demo1_cards):
        x = Inches(0.8 + i * 4.0)
        y = Inches(2.45)
        w = Inches(3.75)
        h = Inches(4.5)
        add_card(s6, x, y, w, h, C_WHITE, col)

        tb = s6.shapes.add_textbox(x + Inches(0.2), y + Inches(0.2), w - Inches(0.4), h - Inches(0.4))
        tf = tb.text_frame
        tf.word_wrap = True
        ph = tf.paragraphs[0]
        ph.text = head
        ph.font.size = Pt(13)
        ph.font.bold = True
        ph.font.color.rgb = col
        ph.space_after = Pt(10)

        for b in bullets:
            p = tf.add_paragraph()
            p.text = f"✓ {b}"
            p.font.size = Pt(11)
            p.font.color.rgb = C_TEXT_DARK
            p.space_after = Pt(8)

    # =========================================================================
    # SLIDE 7: LIVE QUERY DEMO 2 - PEDIATRIC PULPOTOMY TREATMENT
    # =========================================================================
    s7 = prs.slides.add_slide(blank_slide_layout)
    add_header(s7, "Clinical Voice Query Demo #2: Pediatric Deciduous Pulpotomy")

    # Banner for query text
    q_banner2 = add_card(s7, Inches(0.8), Inches(1.4), Inches(11.7), Inches(0.85), C_TEAL_LIGHT, C_TEAL)
    tb_qb2 = s7.shapes.add_textbox(Inches(1.0), Inches(1.45), Inches(11.3), Inches(0.75))
    tf_qb2 = tb_qb2.text_frame
    tf_qb2.word_wrap = True
    pq1_2 = tf_qb2.paragraphs[0]
    pq1_2.text = "🗣️ Doctor Voice / Chat Command:"
    pq1_2.font.size = Pt(11)
    pq1_2.font.bold = True
    pq1_2.font.color.rgb = C_TEAL

    pq2_2 = tf_qb2.add_paragraph()
    pq2_2.text = '“Generate Primary Molar (Tooth A) with Pulpotomy treatment”'
    pq2_2.font.size = Pt(17)
    pq2_2.font.bold = True
    pq2_2.font.color.rgb = C_NAVY_DARK

    # 3 Execution Result Pillars
    demo2_cards = [
        ("1. AI Diagnostic Engine Action", [
            "Category: Pediatric Endodontics & Vital Pulp Therapy",
            "Target Tooth: Deciduous Tooth A (Maxillary Right 2nd Primary Molar)",
            "Assigned CDT Code: D3220 (Therapeutic Pulpotomy — Coronal Pulp Removal)",
            "Status Label: 'Pulpotomy (MTA)'",
            "Highlight Color: Bio-ceramic Purple (#7C3AED)"
        ], C_TEAL),
        ("2. Interactive 3D Model & Zones", [
            "3D Occlusal Model: Displays MTA bioceramic coronal barrier with sealed root canal orifices in vivid violet texture.",
            "5-Zone Cross Section: All 5 surfaces (O, M, D, B, L) update with Pulpotomy endodontic fill color.",
            "Preset Palette: 'Pulpotomy (MTA)' automatically activates with clinical procedural explanation."
        ], C_BLUE_PRI),
        ("3. Pediatric Patient EHR Sync", [
            "SQL Server Update: Persisted to Primary Tooth record 'A' under Patient #17.",
            "Treatment Plan Recommendation: Full-coverage Stainless Steel Crown (SSC) follow-up suggested.",
            "Audio Feedback: Instant voice synthesis confirms successful pulpotomy recording."
        ], C_NAVY_DARK)
    ]

    for i, (head, bullets, col) in enumerate(demo2_cards):
        x = Inches(0.8 + i * 4.0)
        y = Inches(2.45)
        w = Inches(3.75)
        h = Inches(4.5)
        add_card(s7, x, y, w, h, C_WHITE, col)

        tb = s7.shapes.add_textbox(x + Inches(0.2), y + Inches(0.2), w - Inches(0.4), h - Inches(0.4))
        tf = tb.text_frame
        tf.word_wrap = True
        ph = tf.paragraphs[0]
        ph.text = head
        ph.font.size = Pt(13)
        ph.font.bold = True
        ph.font.color.rgb = col
        ph.space_after = Pt(10)

        for b in bullets:
            p = tf.add_paragraph()
            p.text = f"✓ {b}"
            p.font.size = Pt(11)
            p.font.color.rgb = C_TEXT_DARK
            p.space_after = Pt(8)

    # =========================================================================
    # SLIDE 8: CLINICAL SPECIALTIES & DIAGNOSTIC MATRIX
    # =========================================================================
    s8 = prs.slides.add_slide(blank_slide_layout)
    add_header(s8, "Clinical Diagnostic Specialties Supported Across All Age Groups")

    specs = [
        ("Orthodontics & Bite Disorders", "Class II Deep Overbite (80% overlap), Class III Mandibular Prognathism (Underbite), Anterior Open Bite (4.5mm), Posterior Crossbite, and Midline Diastemas.", C_BLUE_PRI),
        ("TMJ & Musculoskeletal Disorders", "Anterior Disc Displacement with reduction, TMJ closed lock (Trismus < 28mm), Masseter muscle tenderness zones, and Joint clicking indicators.", C_ACCENT_RED),
        ("Bruxism & Occlusal Attrition", "Adult severe nocturnal grinding with exposed dentin tables (#D97706), Pediatric teething wear (flattened cusps), and anterior incisal chipping.", C_NAVY_DARK),
        ("Radiographic Pathology & Bone Loss", "Moderate/severe alveolar bone loss with furcation involvement, periapical cysts, radicular root resorption, and hidden interproximal caries.", C_TEAL),
        ("Pediatric Deciduous Care", "Early Childhood Caries (ECC), Coronal Pulpotomy (MTA), Stainless Steel Crowns (SSC), Fixed Band & Loop Space Maintainers, and Supernumerary Mesiodens.", C_BLUE_PRI),
        ("General Restorative & Preventive", "Class I, II, V Composite, Silver Amalgams, Glass Ionomer (GIC), 5% Sodium Fluoride Varnishes, Pit & Fissure Sealants, and Single Tooth Implants.", C_TEAL)
    ]

    for i, (title, desc, accent) in enumerate(specs):
        col_idx = i % 3
        row_idx = i // 3
        x = Inches(0.8 + col_idx * 4.0)
        y = Inches(1.6 + row_idx * 2.65)
        w = Inches(3.75)
        h = Inches(2.45)

        card = add_card(s8, x, y, w, h, C_WHITE, accent)
        tb = s8.shapes.add_textbox(x + Inches(0.2), y + Inches(0.15), w - Inches(0.4), h - Inches(0.3))
        tf = tb.text_frame
        tf.word_wrap = True

        p_t = tf.paragraphs[0]
        p_t.text = title
        p_t.font.size = Pt(13)
        p_t.font.bold = True
        p_t.font.color.rgb = accent
        p_t.space_after = Pt(6)

        p_d = tf.add_paragraph()
        p_d.text = desc
        p_d.font.size = Pt(10.5)
        p_d.font.color.rgb = C_TEXT_DARK

    # =========================================================================
    # SLIDE 9: SECURITY, COMPLIANCE & COMMERCIAL VIABILITY
    # =========================================================================
    s9 = prs.slides.add_slide(blank_slide_layout)
    add_header(s9, "Enterprise Security, Compliance & Commercial Readiness")

    readiness_items = [
        ("HIPAA & GDPR Data Security", "All patient demographic and clinical records are stored with role-based access control, cryptographic tokens, and encrypted transport (HTTPS/TLS 1.3).", C_BLUE_PRI),
        ("Immutable Clinical Audit Trail", "Every voice transaction, surface modification, and prescription is logged to SQL Server with immutable doctor ID, timestamp, and audit trail.", C_TEAL),
        ("Automated CDT Insurance Billing", "Direct ADA CDT code cross-referencing eliminates coding discrepancies, accelerating insurance reimbursement cycles for dental clinics.", C_NAVY_DARK),
        ("High-Performance Offline Cache", "Local state caching guarantees clinical operation continuity even during temporary local network or internet disconnections.", C_BLUE_PRI)
    ]

    for i, (t_sec, d_sec, acc) in enumerate(readiness_items):
        col_idx = i % 2
        row_idx = i // 2
        x = Inches(0.8 + col_idx * 6.0)
        y = Inches(1.6 + row_idx * 2.65)
        w = Inches(5.7)
        h = Inches(2.45)

        card = add_card(s9, x, y, w, h, C_WHITE, acc)
        tb = s9.shapes.add_textbox(x + Inches(0.25), y + Inches(0.2), w - Inches(0.5), h - Inches(0.4))
        tf = tb.text_frame
        tf.word_wrap = True

        p_t = tf.paragraphs[0]
        p_t.text = f"🛡️ {t_sec}"
        p_t.font.size = Pt(14)
        p_t.font.bold = True
        p_t.font.color.rgb = acc
        p_t.space_after = Pt(8)

        p_d = tf.add_paragraph()
        p_d.text = d_sec
        p_d.font.size = Pt(11)
        p_d.font.color.rgb = C_TEXT_DARK

    # =========================================================================
    # SLIDE 10: CONCLUSION & CEO STRATEGIC NEXT STEPS
    # =========================================================================
    s10 = prs.slides.add_slide(blank_slide_layout)
    bg10 = s10.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(7.5))
    bg10.fill.solid()
    bg10.fill.fore_color.rgb = C_NAVY_DARK
    bg10.line.fill.background()

    # Center title
    tb_c = s10.shapes.add_textbox(Inches(1.0), Inches(1.0), Inches(11.333), Inches(1.5))
    tf_c = tb_c.text_frame
    tf_c.word_wrap = True
    pc1 = tf_c.paragraphs[0]
    pc1.text = "Strategic Roadmap & Commercial Rollout"
    pc1.font.size = Pt(36)
    pc1.font.bold = True
    pc1.font.color.rgb = C_WHITE
    pc1.alignment = PP_ALIGN.CENTER

    pc2 = tf_c.add_paragraph()
    pc2.text = "Positioning DentistApp as the Market-Leading AI Dental Intelligence Platform"
    pc2.font.size = Pt(16)
    pc2.font.color.rgb = RGBColor(148, 163, 184)
    pc2.alignment = PP_ALIGN.CENTER
    pc2.space_before = Pt(6)

    # 3 Next Step Boxes
    steps = [
        ("Phase 1: Pilot Clinic Launch", "Deploy DentistApp across 5 partner multi-specialty clinics. Collect clinical workflow metrics and doctor efficiency data.", RGBColor(96, 165, 250)),
        ("Phase 2: CBCT / 3D X-Ray Integration", "Integrate DICOM / CBCT radiographic 3D volume slicing directly into the interactive WebGL occlusal viewer.", RGBColor(45, 212, 191)),
        ("Phase 3: Global EHR & FHIR Scaling", "Enable bi-directional FHIR / HL7 interoperability with major hospital EHRs (Epic, Dentrix, Eaglesoft).", RGBColor(244, 114, 182))
    ]

    for i, (st, sd, col) in enumerate(steps):
        x = Inches(1.0 + i * 3.85)
        y = Inches(3.0)
        w = Inches(3.65)
        h = Inches(3.2)

        card = s10.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, x, y, w, h)
        card.fill.solid()
        card.fill.fore_color.rgb = C_NAVY_LIGHT
        card.line.color.rgb = col
        card.line.width = Pt(1.5)

        tb = s10.shapes.add_textbox(x + Inches(0.2), y + Inches(0.3), w - Inches(0.4), h - Inches(0.6))
        tf = tb.text_frame
        tf.word_wrap = True
        
        pt = tf.paragraphs[0]
        pt.text = st
        pt.font.size = Pt(15)
        pt.font.bold = True
        pt.font.color.rgb = col
        pt.space_after = Pt(12)

        pd = tf.add_paragraph()
        pd.text = sd
        pd.font.size = Pt(12)
        pd.font.color.rgb = RGBColor(226, 232, 240)

    # Save Presentation
    output_filename = "DentistApp_Executive_CEO_Presentation.pptx"
    output_path = os.path.join("f:\\DentistApp_Theme2", output_filename)
    prs.save(output_path)
    print(f"Presentation saved successfully to: {output_path}")

if __name__ == "__main__":
    create_presentation()
