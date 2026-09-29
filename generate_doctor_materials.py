import os
import sys
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

from pptx import Presentation
from pptx.util import Inches as PptInches, Pt as PptPt
from pptx.dml.color import RGBColor as PptRGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

# ==============================================================================
# COLOR PALETTE (Exact Website Theme)
# ==============================================================================
# --color-primary-teal: #4A7CD2 (Primary Brand)
# --color-primary-hover: #3665B7 (Deep Brand)
# --color-dark-slate: #10244B (Dark Slate / Navy)
# --color-light-teal: #EAF0FC (Light Ice Blue)
# --color-warm-cream: #F4F6FA (Soft Background)
# --color-accent-gold: #EAA638 (Accent Amber Gold)
# --color-muted-text: #4A5568 (Muted Slate)

HEX_PRIMARY = "4A7CD2"
HEX_PRIMARY_DARK = "3665B7"
HEX_DARK_SLATE = "10244B"
HEX_LIGHT_TEAL = "EAF0FC"
HEX_WARM_CREAM = "F4F6FA"
HEX_ACCENT_GOLD = "EAA638"
HEX_MUTED_TEXT = "4A5568"
HEX_WHITE = "FFFFFF"

RGB_PRIMARY = RGBColor(74, 124, 210)
RGB_PRIMARY_DARK = RGBColor(54, 101, 183)
RGB_DARK_SLATE = RGBColor(16, 36, 75)
RGB_LIGHT_TEAL = RGBColor(234, 240, 252)
RGB_ACCENT_GOLD = RGBColor(234, 166, 56)
RGB_MUTED_TEXT = RGBColor(74, 85, 104)

PPT_PRIMARY = PptRGBColor(74, 124, 210)
PPT_PRIMARY_DARK = PptRGBColor(54, 101, 183)
PPT_DARK_SLATE = PptRGBColor(16, 36, 75)
PPT_LIGHT_TEAL = PptRGBColor(234, 240, 252)
PPT_WARM_CREAM = PptRGBColor(244, 246, 250)
PPT_ACCENT_GOLD = PptRGBColor(234, 166, 56)
PPT_MUTED_TEXT = PptRGBColor(74, 85, 104)
PPT_WHITE = PptRGBColor(255, 255, 255)
PPT_BORDER = PptRGBColor(212, 226, 249)

# ==============================================================================
# 1. GENERATE DOCX FILE (Target: 3 Concise, High-Impact Pages)
# ==============================================================================
def create_docx(filename="DENTIA_DOCTOR_EXECUTIVE_BROCHURE.docx"):
    doc = docx.Document()

    # Set precise page margins (0.65 in) so it comfortably spans exactly 3 pages
    for section in doc.sections:
        section.top_margin = Inches(0.65)
        section.bottom_margin = Inches(0.65)
        section.left_margin = Inches(0.75)
        section.right_margin = Inches(0.75)

    def set_cell_background(cell, fill_hex):
        tcPr = cell._tc.get_or_add_tcPr()
        shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
        tcPr.append(shd)

    def set_cell_margins(cell, top=140, bottom=140, left=180, right=180):
        tcPr = cell._tc.get_or_add_tcPr()
        tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:bottom w:w="{bottom}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>')
        tcPr.append(tcMar)

    def set_cell_border(cell, **kwargs):
        """kwargs: top, bottom, left, right (val, color, sz)"""
        tcPr = cell._tc.get_or_add_tcPr()
        borders = parse_xml(f'<w:tcBorders {nsdecls("w")}/>')
        for edge in ('top', 'left', 'bottom', 'right'):
            if edge in kwargs:
                edge_data = kwargs[edge]
                b_xml = parse_xml(f'<w:{edge} {nsdecls("w")} w:val="{edge_data.get("val","single")}" w:sz="{edge_data.get("sz","4")}" w:space="0" w:color="{edge_data.get("color","auto")}"/>')
                borders.append(b_xml)
            else:
                b_xml = parse_xml(f'<w:{edge} {nsdecls("w")} w:val="none"/>')
                borders.append(b_xml)
        tcPr.append(borders)

    # --- HEADER BANNER TABLE ---
    banner_table = doc.add_table(rows=1, cols=1)
    banner_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell_b = banner_table.rows[0].cells[0]
    cell_b.width = Inches(7.0)
    set_cell_background(cell_b, HEX_DARK_SLATE)
    set_cell_margins(cell_b, top=180, bottom=180, left=220, right=220)

    p_badge = cell_b.paragraphs[0]
    p_badge.alignment = WD_ALIGN_PARAGRAPH.LEFT
    r_badge = p_badge.add_run("CLINICIAN PLATFORM OVERVIEW  •  FOR DENTAL PRACTITIONERS & CLINIC DIRECTORS")
    r_badge.font.size = Pt(8.5)
    r_badge.font.bold = True
    r_badge.font.color.rgb = RGB_ACCENT_GOLD

    p_b_title = cell_b.add_paragraph()
    r_b_title = p_b_title.add_run("DENTIA™ CLINICAL WORKSPACE & JARVIS AI")
    r_b_title.font.size = Pt(20)
    r_b_title.font.bold = True
    r_b_title.font.color.rgb = RGBColor(255, 255, 255)

    p_b_sub = cell_b.add_paragraph()
    p_b_sub.paragraph_format.space_after = Pt(2)
    r_b_sub = p_b_sub.add_run("The World's First 100% Hands-Free, Voice-Driven Dental Practice OS")
    r_b_sub.font.size = Pt(11.5)
    r_b_sub.font.color.rgb = RGB_LIGHT_TEAL

    p_space = doc.add_paragraph()
    p_space.paragraph_format.space_before = Pt(6)
    p_space.paragraph_format.space_after = Pt(4)

    # --- EXECUTIVE PITCH FOR DOCTORS ---
    p_lead = doc.add_paragraph()
    p_lead.paragraph_format.space_after = Pt(6)
    p_lead.paragraph_format.line_spacing = 1.15
    r_lead = p_lead.add_run("Dentia is engineered specifically to eliminate the three greatest frustrations dentists face daily: ")
    r_lead.font.size = Pt(10)
    r_lead.font.color.rgb = RGB_MUTED_TEXT

    r_l1 = p_lead.add_run("(1) Breaking glove sterility ")
    r_l1.font.size = Pt(10)
    r_l1.bold = True
    r_l1.font.color.rgb = RGB_DARK_SLATE

    r_lead2 = p_lead.add_run("to touch contaminated mice and keyboards; ")
    r_lead2.font.size = Pt(10)
    r_lead2.font.color.rgb = RGB_MUTED_TEXT

    r_l2 = p_lead.add_run("(2) Spending 1.5 to 2 hours every evening ")
    r_l2.font.size = Pt(10)
    r_l2.bold = True
    r_l2.font.color.rgb = RGB_DARK_SLATE

    r_lead3 = p_lead.add_run("typing tedious clinical SOAP notes; and ")
    r_lead3.font.size = Pt(10)
    r_lead3.font.color.rgb = RGB_MUTED_TEXT

    r_l3 = p_lead.add_run("(3) Struggling with clunky legacy desktop software ")
    r_l3.font.size = Pt(10)
    r_l3.bold = True
    r_l3.font.color.rgb = RGB_DARK_SLATE

    r_lead4 = p_lead.add_run("for X-ray sensors and phosphor plate scanners. Dentia runs entirely in your modern browser with zero local software installs.")
    r_lead4.font.size = Pt(10)
    r_lead4.font.color.rgb = RGB_MUTED_TEXT

    # ==========================================================================
    # SECTION 1: WHAT ATTRACTS DOCTORS MOST (THE HERO FEATURES)
    # ==========================================================================
    p_s1 = doc.add_paragraph()
    p_s1.paragraph_format.space_before = Pt(8)
    p_s1.paragraph_format.space_after = Pt(4)
    r_s1 = p_s1.add_run("⭐ CORE CLINICAL ADVANTAGES THAT DOCTORS LOVE")
    r_s1.font.size = Pt(12)
    r_s1.bold = True
    r_s1.font.color.rgb = RGB_PRIMARY_DARK

    # Table of 4 Hero Features (2x2 Grid)
    grid_table = doc.add_table(rows=2, cols=2)
    grid_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    
    hero_features = [
        {
            "badge": "STERILITY & EFFICIENCY",
            "title": "1. Hands-Free 'Jarvis' Clinical Voice Copilot",
            "desc": "Speak naturally while keeping sterile gloves on! Jarvis understands English & Roman Urdu, updates 3D tooth shaders (#14 occlusal caries, #19 RCT), arms X-ray scanners, and answers clinical pharmacology questions in a calm, professional lady voice.",
            "impact": "Benefit: 100% sterile operatory, 0% cross-contamination."
        },
        {
            "badge": "TIME SAVER (2 HRS/DAY)",
            "title": "2. Ambient 8-Section AI Clinical SOAP Scribe",
            "desc": "Never type notes again. During consultation, Dentia listens to the doctor's exam and automatically compiles Subjective, Objective, Assessment, Treatment Plan, Rx dosages, and CDT billing codes ready for 1-click digital signature.",
            "impact": "Benefit: Saves 10-15 minutes per patient visit."
        },
        {
            "badge": "PATIENT RETENTION",
            "title": "3. Doctor-Cloned Voice Post-Op Audio",
            "desc": "After completing surgery, Dentia synthesizes personalized post-operative care advice spoken in the treating doctor's own natural cloned voice, delivered directly to the patient's phone portal. Reduces anxious follow-up calls by 70%.",
            "impact": "Benefit: Premium patient experience & zero after-hours calls."
        },
        {
            "badge": "HARDWARE INNOVATION",
            "title": "4. Zero-Install Hardware Hub (Digora & Sensors)",
            "desc": "No drivers or 10GB software to install! Directly connect Soredex DIGORA® Optime Ethernet scanners and NanoPix RVG sensors over clinic LAN. Laser-scanned phosphor plates auto-mount into the active patient's chart in under 6 seconds.",
            "impact": "Benefit: Works on any laptop, tablet, or operatory Mac/PC."
        }
    ]

    for idx, feat in enumerate(hero_features):
        row = idx // 2
        col = idx % 2
        cell = grid_table.rows[row].cells[col]
        cell.width = Inches(3.45)
        set_cell_background(cell, HEX_WARM_CREAM)
        set_cell_margins(cell, top=120, bottom=120, left=140, right=140)
        set_cell_border(cell, left={"val": "single", "sz": "18", "color": HEX_PRIMARY})

        p1 = cell.paragraphs[0]
        p1.paragraph_format.space_after = Pt(2)
        r_b = p1.add_run(feat["badge"])
        r_b.font.size = Pt(7.5)
        r_b.bold = True
        r_b.font.color.rgb = RGB_ACCENT_GOLD

        p2 = cell.add_paragraph()
        p2.paragraph_format.space_after = Pt(2)
        r_t = p2.add_run(feat["title"])
        r_t.font.size = Pt(10)
        r_t.bold = True
        r_t.font.color.rgb = RGB_DARK_SLATE

        p3 = cell.add_paragraph()
        p3.paragraph_format.space_after = Pt(3)
        p3.paragraph_format.line_spacing = 1.05
        r_d = p3.add_run(feat["desc"])
        r_d.font.size = Pt(8.5)
        r_d.font.color.rgb = RGB_MUTED_TEXT

        p4 = cell.add_paragraph()
        p4.paragraph_format.space_after = Pt(0)
        r_i = p4.add_run(feat["impact"])
        r_i.font.size = Pt(8.5)
        r_i.bold = True
        r_i.font.color.rgb = RGB_PRIMARY_DARK

    # Space before Page 2
    doc.add_page_break()

    # ==========================================================================
    # PAGE 2: CLINICAL WORKFLOW & PATIENT ENGAGEMENT
    # ==========================================================================
    p_p2_hdr = doc.add_paragraph()
    p_p2_hdr.paragraph_format.space_after = Pt(4)
    r_p2_h = p_p2_hdr.add_run("🦷 INTERACTIVE 3D ODONTOGRAM & CLINICAL WORKSPACE")
    r_p2_h.font.size = Pt(13)
    r_p2_h.bold = True
    r_p2_h.font.color.rgb = RGB_DARK_SLATE

    p_p2_sub = doc.add_paragraph()
    p_p2_sub.paragraph_format.space_after = Pt(8)
    r_p2_s = p_p2_sub.add_run("Visually engaging, clinically accurate 3D dental charting that increases patient treatment plan acceptance by over 40%.")
    r_p2_s.font.size = Pt(9.5)
    r_p2_s.font.color.rgb = RGB_MUTED_TEXT

    # 3-Column Odontogram Highlights
    t_odo = doc.add_table(rows=1, cols=3)
    t_odo.alignment = WD_TABLE_ALIGNMENT.CENTER
    odo_cols = [
        {
            "title": "3D Dual-Jaw Canvas",
            "bullets": [
                "Full Maxilla & Mandible 3D meshes",
                "32 Permanent + 20 Deciduous teeth",
                "FDI (11-48) & Universal (1-32) notation",
                "Live shader coloring for Caries, RCT, Crowns & Missing teeth"
            ]
        },
        {
            "title": "Microscopic Surface Canvas",
            "bullets": [
                "5-Surface per-tooth anatomy (MODBL)",
                "Root canal anatomy & canal count",
                "Mobility grade & periodontal probing",
                "Complete chronological history per tooth"
            ]
        },
        {
            "title": "AI Radiology Filmstrip",
            "bullets": [
                "Automated mounting for Bitewings & OPG",
                "AI Caries & Periapical Bone Loss scan",
                "Dashed amber AI markers with safety review",
                "Zero unreviewed AI auto-commits"
            ]
        }
    ]

    for c_idx, o_col in enumerate(odo_cols):
        cell = t_odo.rows[0].cells[c_idx]
        cell.width = Inches(2.3)
        set_cell_background(cell, HEX_LIGHT_TEAL)
        set_cell_margins(cell, top=100, bottom=100, left=120, right=120)
        set_cell_border(cell, top={"val": "single", "sz": "12", "color": HEX_PRIMARY})

        p = cell.paragraphs[0]
        p.paragraph_format.space_after = Pt(3)
        r = p.add_run(o_col["title"])
        r.font.size = Pt(9.5)
        r.bold = True
        r.font.color.rgb = RGB_PRIMARY_DARK

        for b in o_col["bullets"]:
            pb = cell.add_paragraph()
            pb.paragraph_format.space_after = Pt(2)
            pb.paragraph_format.line_spacing = 1.05
            rb = pb.add_run(f"• {b}")
            rb.font.size = Pt(8)
            rb.font.color.rgb = RGB_DARK_SLATE

    # Privacy & Sterility Box
    p_space2 = doc.add_paragraph()
    p_space2.paragraph_format.space_before = Pt(8)
    p_space2.paragraph_format.space_after = Pt(4)

    box_table = doc.add_table(rows=1, cols=1)
    box_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    c_box = box_table.rows[0].cells[0]
    c_box.width = Inches(7.0)
    set_cell_background(c_box, HEX_WARM_CREAM)
    set_cell_margins(c_box, top=120, bottom=120, left=160, right=160)
    set_cell_border(c_box, left={"val": "single", "sz": "24", "color": HEX_ACCENT_GOLD})

    p_bx_t = c_box.paragraphs[0]
    p_bx_t.paragraph_format.space_after = Pt(2)
    r_bx_t = p_bx_t.add_run("🔒 CLINICIAN PRIVACY FIRST: ZERO ACCIDENTAL RECORDING")
    r_bx_t.font.size = Pt(10)
    r_bx_t.bold = True
    r_bx_t.font.color.rgb = RGB_DARK_SLATE

    p_bx_d = c_box.add_paragraph()
    p_bx_d.paragraph_format.space_after = Pt(0)
    p_bx_d.paragraph_format.line_spacing = 1.1
    r_bx_d = p_bx_d.add_run(
        "Dentia Jarvis stays completely MUTED and DORMANT by default. The microphone hardware track is strictly closed so private patient discussions are never overheard. It activates ONLY when the doctor taps a foot-pedal switch, hits the Spacebar, or taps the on-screen toggle. An automatic 60-second inactivity timer ensures it disengages the moment consultation finishes."
    )
    r_bx_d.font.size = Pt(8.5)
    r_bx_d.font.color.rgb = RGB_MUTED_TEXT

    # Specialty Modules
    p_spec_h = doc.add_paragraph()
    p_spec_h.paragraph_format.space_before = Pt(10)
    p_spec_h.paragraph_format.space_after = Pt(4)
    r_sp = p_spec_h.add_run("📋 SPECIALTY CLINICAL SUITES BUILT FOR EXPERTS")
    r_sp.font.size = Pt(11)
    r_sp.bold = True
    r_sp.font.color.rgb = RGB_PRIMARY_DARK

    p_sp_body = doc.add_paragraph()
    p_sp_body.paragraph_format.space_after = Pt(4)
    p_sp_body.paragraph_format.line_spacing = 1.1
    p_sp_body.add_run(
        "• Orthodontics & Clear Aligners: Automated tray sequencing (12-40+ stages), attachment tracking, and calibrated IPR (0.1mm - 0.5mm).\n"
        "• TMJ & Occlusion Suite: Digital TMJ click tracking, trismus opening measurements, and stabilization splint protocols.\n"
        "• Implant Planning & 3D Guide: Lekholm & Zarb bone quality assessment, 2mm IAN nerve safety buffer, and sinus lift guidance.\n"
        "• Pathology & Oral Biopsy Requisitions: Formalin preservation checks, anatomical site indexing, and histological lab requisitions."
    )
    for r in p_sp_body.runs:
        r.font.size = Pt(8.5)
        r.font.color.rgb = RGB_DARK_SLATE

    doc.add_page_break()

    # ==========================================================================
    # PAGE 3: FULL PLATFORM FEATURE MATRIX & DOCTOR ROI
    # ==========================================================================
    p_p3_hdr = doc.add_paragraph()
    p_p3_hdr.paragraph_format.space_after = Pt(4)
    r_p3_h = p_p3_hdr.add_run("📊 COMPLETE PRACTICE FEATURE MATRIX & DOCTOR BENEFITS")
    r_p3_h.font.size = Pt(13)
    r_p3_h.bold = True
    r_p3_h.font.color.rgb = RGB_DARK_SLATE

    # Feature Matrix Table
    matrix_table = doc.add_table(rows=7, cols=3)
    matrix_table.alignment = WD_TABLE_ALIGNMENT.CENTER

    headers = ["PLATFORM MODULE", "CAPABILITIES & WORKFLOW", "DOCTOR BENEFIT & ROI"]
    for i, h in enumerate(headers):
        cell = matrix_table.rows[0].cells[i]
        set_cell_background(cell, HEX_DARK_SLATE)
        set_cell_margins(cell, top=100, bottom=100, left=120, right=120)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        r = p.add_run(h)
        r.font.size = Pt(8.5)
        r.bold = True
        r.font.color.rgb = RGBColor(255, 255, 255)

    matrix_rows = [
        ("Smart Patient Intake", "Photo ID, DOB age calculation, auto-dentition adaptation (Pediatric, Mixed, Adult), geocoding.", "Zero intake paperwork errors; instant profile sync."),
        ("Operatory Schedule", "Real-time doctor calendar, conflict detection, emergency triage queue, status lifecycle.", "Eliminates operatory double-booking & idle chair time."),
        ("Comprehensive Directory", "Unified search across all patient history, treatment stage, tooth conditions, and audit logs.", "Instant file access chairside in under 1 second."),
        ("Automated Billing & CDT", "ADA CDT D-codes mapping, procedure fee schedules, auto itemized invoice generation.", "Zero missed procedure billing codes; 15% revenue lift."),
        ("Patient Portal & Health Map", "Patients view treatment progress, invoices, dental health map, and doctor audio instructions.", "Cuts front-desk phone inquiries by over 60%."),
        ("Cloud DICOM Imaging", "Hardware Ethernet / USB bridge to Soredex Digora & NanoPix with AI caries detection.", "$0 desktop software license fees; works on any laptop.")
    ]

    for r_idx, (m_mod, m_cap, m_roi) in enumerate(matrix_rows):
        row = matrix_table.rows[r_idx + 1]
        bg = HEX_WARM_CREAM if r_idx % 2 == 0 else HEX_WHITE
        
        # Col 0
        c0 = row.cells[0]
        c0.width = Inches(1.8)
        set_cell_background(c0, bg)
        set_cell_margins(c0, top=80, bottom=80, left=100, right=100)
        p0 = c0.paragraphs[0]
        r0 = p0.add_run(m_mod)
        r0.font.size = Pt(8.5)
        r0.bold = True
        r0.font.color.rgb = RGB_PRIMARY_DARK

        # Col 1
        c1 = row.cells[1]
        c1.width = Inches(3.0)
        set_cell_background(c1, bg)
        set_cell_margins(c1, top=80, bottom=80, left=100, right=100)
        p1 = c1.paragraphs[0]
        p1.paragraph_format.line_spacing = 1.05
        r1 = p1.add_run(m_cap)
        r1.font.size = Pt(8)
        r1.font.color.rgb = RGB_DARK_SLATE

        # Col 2
        c2 = row.cells[2]
        c2.width = Inches(2.2)
        set_cell_background(c2, bg)
        set_cell_margins(c2, top=80, bottom=80, left=100, right=100)
        p2 = c2.paragraphs[0]
        p2.paragraph_format.line_spacing = 1.05
        r2 = p2.add_run(m_roi)
        r2.font.size = Pt(8)
        r2.bold = True
        r2.font.color.rgb = RGB_MUTED_TEXT

    # ROI Summary Numbers
    p_roi_h = doc.add_paragraph()
    p_roi_h.paragraph_format.space_before = Pt(10)
    p_roi_h.paragraph_format.space_after = Pt(4)
    r_rh = p_roi_h.add_run("📈 QUANTIFIED PRACTICE IMPACT (MEASURED IN DOCTOR WORKFLOWS)")
    r_rh.font.size = Pt(11)
    r_rh.bold = True
    r_rh.font.color.rgb = RGB_PRIMARY_DARK

    t_stat = doc.add_table(rows=1, cols=4)
    t_stat.alignment = WD_TABLE_ALIGNMENT.CENTER
    stats = [
        ("2.0 Hrs", "Saved Daily in Clinical Dictation"),
        ("100%", "Hands-Free Glove Sterility Maintained"),
        ("+40%", "Treatment Plan Patient Acceptance"),
        ("6 Sec", "Digora X-Ray Auto-Mount Speed")
    ]
    for s_idx, (s_val, s_lbl) in enumerate(stats):
        cell = t_stat.rows[0].cells[s_idx]
        cell.width = Inches(1.75)
        set_cell_background(cell, HEX_LIGHT_TEAL)
        set_cell_margins(cell, top=100, bottom=100, left=100, right=100)
        
        p_val = cell.paragraphs[0]
        p_val.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_val.paragraph_format.space_after = Pt(1)
        r_v = p_val.add_run(s_val)
        r_v.font.size = Pt(14)
        r_v.bold = True
        r_v.font.color.rgb = RGB_PRIMARY_DARK

        p_lbl = cell.add_paragraph()
        p_lbl.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_lbl.paragraph_format.space_after = Pt(0)
        r_l = p_lbl.add_run(s_lbl)
        r_l.font.size = Pt(7.5)
        r_l.font.color.rgb = RGB_DARK_SLATE

    # Footer Call to Action
    p_cta = doc.add_paragraph()
    p_cta.paragraph_format.space_before = Pt(12)
    p_cta.paragraph_format.space_after = Pt(0)
    p_cta.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_c1 = p_cta.add_run("Ready to transform your operatory? ")
    r_c1.font.size = Pt(9.5)
    r_c1.bold = True
    r_c1.font.color.rgb = RGB_DARK_SLATE

    r_c2 = p_cta.add_run("Experience the live interactive demo at ")
    r_c2.font.size = Pt(9.5)
    r_c2.font.color.rgb = RGB_MUTED_TEXT

    r_c3 = p_cta.add_run("https://dentistfrontend.vercel.app  •  Dentia Clinical Technologies")
    r_c3.font.size = Pt(9.5)
    r_c3.bold = True
    r_c3.font.color.rgb = RGB_PRIMARY_DARK

    doc.save(filename)
    print(f"DOCX created: {filename}")


# ==============================================================================
# 2. GENERATE PPTX FILE (Target: 4 High-Impact, Visual Widescreen Slides)
# ==============================================================================
def create_pptx(filename="DENTIA_DOCTOR_FEATURE_SHOWCASE.pptx"):
    prs = Presentation()
    prs.slide_width = PptInches(13.333)
    prs.slide_height = PptInches(7.5)
    blank_layout = prs.slide_layouts[6]

    def add_top_bar(slide, category_text, title_text):
        # Top gradient accent bar
        bar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, PptInches(0), PptInches(0), PptInches(13.333), PptInches(0.12))
        bar.fill.solid()
        bar.fill.fore_color.rgb = PPT_PRIMARY
        bar.line.fill.background()

        # Category Box
        cbox = slide.shapes.add_textbox(PptInches(0.8), PptInches(0.4), PptInches(11.7), PptInches(0.3))
        tf_c = cbox.text_frame
        p_c = tf_c.paragraphs[0]
        p_c.text = category_text.upper()
        p_c.font.size = PptPt(9.5)
        p_c.font.bold = True
        p_c.font.color.rgb = PPT_ACCENT_GOLD

        # Title Box
        tbox = slide.shapes.add_textbox(PptInches(0.8), PptInches(0.68), PptInches(11.7), PptInches(0.6))
        tf_t = tbox.text_frame
        p_t = tf_t.paragraphs[0]
        p_t.text = title_text
        p_t.font.size = PptPt(22)
        p_t.font.bold = True
        p_t.font.color.rgb = PPT_DARK_SLATE

    def add_card(slide, left, top, width, height, bg_color=PPT_WHITE, border_color=PPT_BORDER):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = bg_color
        card.line.color.rgb = border_color
        card.line.width = PptPt(1.2)
        return card

    # --------------------------------------------------------------------------
    # SLIDE 1: TITLE & EXECUTIVE HOOK (HERO SLIDE)
    # --------------------------------------------------------------------------
    s1 = prs.slides.add_slide(blank_layout)

    # Background fill
    bg1 = s1.shapes.add_shape(MSO_SHAPE.RECTANGLE, PptInches(0), PptInches(0), PptInches(13.333), PptInches(7.5))
    bg1.fill.solid()
    bg1.fill.fore_color.rgb = PPT_DARK_SLATE
    bg1.line.fill.background()

    # Brand Pill
    pill = s1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, PptInches(1.0), PptInches(1.2), PptInches(4.2), PptInches(0.45))
    pill.fill.solid()
    pill.fill.fore_color.rgb = PPT_PRIMARY_DARK
    pill.line.fill.background()
    p_pill = pill.text_frame.paragraphs[0]
    p_pill.text = "THE NEXT-GEN CLINICAL WORKSPACE"
    p_pill.font.size = PptPt(10)
    p_pill.font.bold = True
    p_pill.font.color.rgb = PPT_ACCENT_GOLD
    p_pill.alignment = PP_ALIGN.CENTER

    # Main Headline
    t_box1 = s1.shapes.add_textbox(PptInches(1.0), PptInches(1.8), PptInches(11.3), PptInches(1.8))
    tf1 = t_box1.text_frame
    tf1.word_wrap = True
    p1_h1 = tf1.paragraphs[0]
    p1_h1.text = "Dentia™ & Jarvis AI Clinical Copilot"
    p1_h1.font.size = PptPt(36)
    p1_h1.font.bold = True
    p1_h1.font.color.rgb = PPT_WHITE

    p1_sub = tf1.add_paragraph()
    p1_sub.text = "The World's First 100% Hands-Free Dental Practice Operating System with Voice Charting, Ambient SOAP Scribing & Hardware LAN Sync."
    p1_sub.font.size = PptPt(16)
    p1_sub.font.color.rgb = PPT_LIGHT_TEAL

    # 3 Stat Cards on Title Slide
    stats1 = [
        ("100% Sterile Operatory", "Never touch a dirty mouse or keyboard with surgical gloves on. Speak naturally to update charts."),
        ("2 Hours Saved Daily", "Ambient voice scribe drafts 8-section SOAP notes & CDT codes automatically during treatment."),
        ("Zero-Install Hardware", "Directly connects Soredex DIGORA® Optime Ethernet & RVG sensors in the web browser.")
    ]
    for idx, (head, desc) in enumerate(stats1):
        c_left = PptInches(1.0 + idx * 3.85)
        card = add_card(s1, c_left, PptInches(4.2), PptInches(3.6), PptInches(2.4), bg_color=PptRGBColor(26, 46, 88), border_color=PPT_PRIMARY)
        
        tb = s1.shapes.add_textbox(c_left + PptInches(0.2), PptInches(4.4), PptInches(3.2), PptInches(2.0))
        tf = tb.text_frame
        tf.word_wrap = True
        
        ph = tf.paragraphs[0]
        ph.text = head
        ph.font.size = PptPt(14)
        ph.font.bold = True
        ph.font.color.rgb = PPT_ACCENT_GOLD

        pd = tf.add_paragraph()
        pd.text = desc
        pd.font.size = PptPt(11)
        pd.font.color.rgb = PPT_LIGHT_TEAL

    # --------------------------------------------------------------------------
    # SLIDE 2: THE 4 FEATURES DOCTORS ARE ATTRACTED TO (THE HEROES)
    # --------------------------------------------------------------------------
    s2 = prs.slides.add_slide(blank_layout)
    add_top_bar(s2, "Doctor Attraction & Clinical Value", "The 4 Breakthrough Features Dentists Love Most")

    heroes = [
        {
            "tag": "⭐ TOP DOCTOR ATTRACTION",
            "title": "Hands-Free Voice Charting (Jarvis)",
            "voice": "Lady Voice Persona (Jenny / Zira / Nova)",
            "bullets": [
                "Voice-driven Odontogram: 'Tooth 14 occlusal caries', 'Tooth 19 RCT'",
                "Bilingual Understanding: English and Roman Urdu fluency",
                "Strict Privacy: 100% dormant; wakes only on sterile foot-pedal/spacebar",
                "Instant 3D dual-jaw mesh update with zero contamination"
            ]
        },
        {
            "tag": "⭐ TIME RECOVERY (2 HRS/DAY)",
            "title": "Ambient 8-Section AI SOAP Scribe",
            "voice": "Automated Consultation Transcription",
            "bullets": [
                "Listens to doctor dictation & categorizes into SOAP structure",
                "Maps Subjective, Objective, Assessment, Plan & Pharmacology",
                "Automates ADA CDT billing codes (D0120, D2391, D3330)",
                "One-click verbal sign-off: 'Jarvis, sign and finalize note'"
            ]
        },
        {
            "tag": "⭐ PATIENT RETENTION & TRUST",
            "title": "Doctor-Cloned Voice Post-Op Audio",
            "voice": "VoiceStudio Neural Voice Cloning",
            "bullets": [
                "Synthesizes care instructions in the treating doctor's actual voice",
                "Delivered straight to patient portal & mobile phone",
                "Reassures patients at home, eliminating post-treatment panic",
                "Slashes repetitive after-hours phone calls by over 70%"
            ]
        },
        {
            "tag": "⭐ ZERO IT HEADACHE",
            "title": "Zero-Install Hardware LAN Hub",
            "voice": "Soredex DIGORA® Optime + NanoPix RVG",
            "bullets": [
                "Connects directly over Ethernet TCP/IP & USB in browser",
                "Phosphor plates auto-mount into active chart in 5-7 seconds",
                "Integrated AI vision detects interproximal decay & bone loss",
                "Eliminates 10GB legacy desktop software & expensive dongles"
            ]
        }
    ]

    for idx, h in enumerate(heroes):
        row = idx // 2
        col = idx % 2
        c_left = PptInches(0.8 + col * 5.95)
        c_top = PptInches(1.5 + row * 2.8)
        
        # Card Background
        add_card(s2, c_left, c_top, PptInches(5.75), PptInches(2.6), bg_color=PPT_WARM_CREAM, border_color=PPT_PRIMARY)

        # Content Box
        tbox = s2.shapes.add_textbox(c_left + PptInches(0.25), c_top + PptInches(0.15), PptInches(5.25), PptInches(2.3))
        tf = tbox.text_frame
        tf.word_wrap = True

        ptag = tf.paragraphs[0]
        ptag.text = h["tag"]
        ptag.font.size = PptPt(9)
        ptag.font.bold = True
        ptag.font.color.rgb = PPT_ACCENT_GOLD

        ptit = tf.add_paragraph()
        ptit.text = h["title"]
        ptit.font.size = PptPt(13)
        ptit.font.bold = True
        ptit.font.color.rgb = PPT_DARK_SLATE

        for b in h["bullets"]:
            pb = tf.add_paragraph()
            pb.text = f"• {b}"
            pb.font.size = PptPt(9.5)
            pb.font.color.rgb = PPT_MUTED_TEXT

    # --------------------------------------------------------------------------
    # SLIDE 3: 3D ODONTOGRAM & CLINICAL SPECIALTY SUITES
    # --------------------------------------------------------------------------
    s3 = prs.slides.add_slide(blank_layout)
    add_top_bar(s3, "Visual Treatment Excellence", "Interactive 3D Odontogram & Multi-Specialty Suites")

    col_data3 = [
        {
            "title": "3D Dual-Jaw Anatomical Chart",
            "desc": "Empowers patient visual understanding and accelerates treatment plan consent by 40%.",
            "items": [
                "Maxilla (Upper) & Mandible (Lower) 3D arches",
                "Color shaders: Decay (Red), RCT (Violet), Crown (Amber), Implant (Emerald)",
                "Full adult 1-32 Universal and 11-48 FDI notation",
                "Pediatric deciduous arch (A-T) auto-adaptation"
            ]
        },
        {
            "title": "Microscopic Surface Canvas",
            "desc": "High-precision diagnostic mapping for endodontics and complex prosthodontics.",
            "items": [
                "Individual 5-surface mapping (Mesial, Distal, Occlusal, Buccal, Lingual)",
                "Detailed root canal morphology and canal counter",
                "Per-tooth chronological treatment history ledger",
                "Antagonist tracking and occlusion mapping"
            ]
        },
        {
            "title": "Advanced Specialty Suites",
            "desc": "Built-in clinical workflows for dental specialists and comprehensive clinics.",
            "items": [
                "Orthodontics: Clear aligner sequencing & IPR staging",
                "TMJ & Occlusion: Disc clicking & trismus analysis",
                "Implantology: Bone quality & 2mm IAN nerve buffer",
                "Radiology: Automated AI caries & bone resorption detection"
            ]
        }
    ]

    for idx, c in enumerate(col_data3):
        c_left = PptInches(0.8 + idx * 3.95)
        add_card(s3, c_left, PptInches(1.5), PptInches(3.8), PptInches(5.4), bg_color=PPT_WHITE, border_color=PPT_PRIMARY)

        tbox = s3.shapes.add_textbox(c_left + PptInches(0.25), PptInches(1.7), PptInches(3.3), PptInches(5.0))
        tf = tbox.text_frame
        tf.word_wrap = True

        p_t = tf.paragraphs[0]
        p_t.text = c["title"]
        p_t.font.size = PptPt(13)
        p_t.font.bold = True
        p_t.font.color.rgb = PPT_PRIMARY_DARK

        p_d = tf.add_paragraph()
        p_d.text = c["desc"]
        p_d.font.size = PptPt(10)
        p_d.font.italic = True
        p_d.font.color.rgb = PPT_MUTED_TEXT

        p_sp = tf.add_paragraph()
        p_sp.text = ""

        for item in c["items"]:
            pi = tf.add_paragraph()
            pi.text = f"✔ {item}"
            pi.font.size = PptPt(9.5)
            pi.font.color.rgb = PPT_DARK_SLATE

    # --------------------------------------------------------------------------
    # SLIDE 4: COMPLETE PLATFORM MATRIX & PRACTICE ROI
    # --------------------------------------------------------------------------
    s4 = prs.slides.add_slide(blank_layout)
    add_top_bar(s4, "Complete Practice Operating System", "Full Feature Matrix & Measurable Practice ROI")

    # Left Card: Complete Feature Matrix
    c_left = PptInches(0.8)
    add_card(s4, c_left, PptInches(1.5), PptInches(6.8), PptInches(5.4), bg_color=PPT_WARM_CREAM, border_color=PPT_BORDER)
    
    tb_m = s4.shapes.add_textbox(c_left + PptInches(0.25), PptInches(1.7), PptInches(6.3), PptInches(5.0))
    tf_m = tb_m.text_frame
    tf_m.word_wrap = True

    p_mh = tf_m.paragraphs[0]
    p_mh.text = "COMPLETE PRACTICE FEATURE SUITE"
    p_mh.font.size = PptPt(12)
    p_mh.font.bold = True
    p_mh.font.color.rgb = PPT_DARK_SLATE

    matrix_bullets = [
        ("Smart Patient Intake", "Auto-calculates age, adapts dentition arch, geocodes addresses, eliminates paperwork errors."),
        ("Operatory Schedule", "Doctor calendar, conflict prevention, real-time status flow (Pending, Confirmed, Completed)."),
        ("Patient Master Directory", "Forensic clinical audit logs, instant search across all teeth states and treatments."),
        ("Automated CDT Invoicing", "ADA procedure billing schedules, instant PDF reports, zero missed billing items."),
        ("Patient Portal & Health Map", "Patients view their teeth health map, pay invoices, and hear doctor voice instructions."),
        ("Cloud DICOM Security", "100% web-based, zero workstation drivers, multi-tier cloud and offline AI fallback.")
    ]
    for m_head, m_body in matrix_bullets:
        pm = tf_m.add_paragraph()
        pm.text = f"• {m_head}: {m_body}"
        pm.font.size = PptPt(9.5)
        pm.font.color.rgb = PPT_MUTED_TEXT

    # Right Card: Quantified ROI Impact
    c_right = PptInches(7.8)
    add_card(s4, c_right, PptInches(1.5), PptInches(4.7), PptInches(5.4), bg_color=PPT_DARK_SLATE, border_color=PPT_PRIMARY)

    tb_r = s4.shapes.add_textbox(c_right + PptInches(0.3), PptInches(1.8), PptInches(4.1), PptInches(4.8))
    tf_r = tb_r.text_frame
    tf_r.word_wrap = True

    pr_h = tf_r.paragraphs[0]
    pr_h.text = "PROVEN CLINICIAN ROI"
    pr_h.font.size = PptPt(13)
    pr_h.font.bold = True
    pr_h.font.color.rgb = PPT_ACCENT_GOLD

    roi_stats = [
        ("2.0 Hours / Day", "Saved in manual dictation and note typing."),
        ("100% Sterile", "Glove sterility maintained with zero keyboard contact."),
        ("+40% Acceptance", "Higher patient treatment plan consent via 3D visualization."),
        ("6 Seconds", "Average speed to scan and auto-mount Digora X-rays.")
    ]
    for val, lbl in roi_stats:
        pv = tf_r.add_paragraph()
        pv.text = val
        pv.font.size = PptPt(16)
        pv.font.bold = True
        pv.font.color.rgb = PPT_WHITE

        pl = tf_r.add_paragraph()
        pl.text = lbl
        pl.font.size = PptPt(10)
        pl.font.color.rgb = PPT_LIGHT_TEAL

    prs.save(filename)
    print(f"PPTX created: {filename}")


if __name__ == "__main__":
    create_docx()
    create_pptx()
