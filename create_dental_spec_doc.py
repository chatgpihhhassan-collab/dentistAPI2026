import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=140, right=140):
    tcPr = cell._element.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def create_document():
    doc = docx.Document()

    # Page Margins
    for s in doc.sections:
        s.top_margin = Inches(0.75)
        s.bottom_margin = Inches(0.75)
        s.left_margin = Inches(0.8)
        s.right_margin = Inches(0.8)

    # Base Style
    style_normal = doc.styles['Normal']
    style_normal.font.name = 'Calibri'
    style_normal.font.size = Pt(10)
    style_normal.font.color.rgb = RGBColor(30, 41, 59)

    # ==================== COVER / HEADER ====================
    title_p = doc.add_paragraph()
    title_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title_run = title_p.add_run("DENTIA CLINICAL AI SUITE")
    title_run.font.name = 'Arial'
    title_run.font.size = Pt(22)
    title_run.font.bold = True
    title_run.font.color.rgb = RGBColor(16, 36, 75)

    sub_p = doc.add_paragraph()
    sub_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    sub_run = sub_p.add_run("Comprehensive 5-Surface Zone Anatomy, 7-Category EHR Clinical Palette & 100 AI Voice/Chat Dictation Examples")
    sub_run.font.size = Pt(11.5)
    sub_run.font.color.rgb = RGBColor(74, 124, 210)
    sub_run.font.bold = True

    meta_p = doc.add_paragraph()
    meta_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta_run = meta_p.add_run("Clinical EHR & Interactive Odontogram Specification Document · Master Reference Edition")
    meta_run.font.size = Pt(9)
    meta_run.font.color.rgb = RGBColor(100, 116, 139)
    meta_run.font.italic = True

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # ==================== 1. EXECUTIVE OVERVIEW ====================
    h1 = doc.add_paragraph()
    r = h1.add_run("1. Executive Overview & System Architecture")
    r.font.name = 'Arial'
    r.font.size = Pt(13)
    r.font.bold = True
    r.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "The Dentia Clinical AI Suite provides an advanced medical-grade odontogram and EHR dental charting platform. "
        "The system combines Three.js WebGL 3D procedural/textured arch visualization across both the Maxilla (Upper Jaw, 16 teeth) "
        "and Mandible (Lower Jaw, 16 teeth) with precise 5-surface zone anatomical breakdown for localized restoration and pathology tracking."
    )

    # ==================== 2. 5-SURFACE ZONE ANATOMY ====================
    h2 = doc.add_paragraph()
    r2 = h2.add_run("2. 5-Surface Zone Diagram (Surface-Specific Dental Charting)")
    r2.font.name = 'Arial'
    r2.font.size = Pt(13)
    r2.font.bold = True
    r2.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "In modern dental practice, clinical conditions such as caries, fillings, inlays, and sealants are localized to specific surfaces "
        "rather than uniformly coloring the entire tooth crown. Every human tooth is divided into 5 standard anatomical surfaces:"
    )

    surf_table = doc.add_table(rows=1, cols=3)
    surf_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    surf_table.autofit = False

    hdr = surf_table.rows[0].cells
    hdr[0].width = Inches(1.2)
    hdr[1].width = Inches(2.2)
    hdr[2].width = Inches(3.4)

    for i, title in enumerate(["Surface Code", "Anatomical Zone", "Clinical Definition & Orientation"]):
        set_cell_background(hdr[i], "10244B")
        set_cell_margins(hdr[i], 100, 100, 120, 120)
        p = hdr[i].paragraphs[0]
        run = p.add_run(title)
        run.font.bold = True
        run.font.size = Pt(9)
        run.font.color.rgb = RGBColor(255, 255, 255)

    surfaces_data = [
        ("O / I", "Occlusal / Incisal", "Center masticatory chewing surface for molars/premolars, or cutting edge for anterior incisors."),
        ("M", "Mesial", "Anterior proximal surface oriented toward the dental arch midline."),
        ("D", "Distal", "Posterior proximal surface oriented away from the dental arch midline."),
        ("B / F", "Buccal / Facial (Labial)", "Outer tooth surface facing the cheeks (posterior) or lips (anterior incisors & canines)."),
        ("L / P", "Lingual / Palatal", "Inner tooth surface facing the tongue (mandible) or hard palate (maxilla).")
    ]

    for row_idx, (code, name, desc) in enumerate(surfaces_data):
        row = surf_table.add_row()
        bg_hex = "F8FAFC" if row_idx % 2 == 0 else "FFFFFF"
        for c_idx, text in enumerate([code, name, desc]):
            cell = row.cells[c_idx]
            set_cell_background(cell, bg_hex)
            set_cell_margins(cell, 70, 70, 100, 100)
            p = cell.paragraphs[0]
            r = p.add_run(text)
            r.font.size = Pt(8.5)
            if c_idx == 0:
                r.font.bold = True
                r.font.color.rgb = RGBColor(74, 124, 210)

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # ==================== 3. 7 GROUPED EHR CATEGORIES ====================
    h3 = doc.add_paragraph()
    r3 = h3.add_run("3. 7-Category EHR Clinical Palette & Representation Table")
    r3.font.name = 'Arial'
    r3.font.size = Pt(13)
    r3.font.bold = True
    r3.font.color.rgb = RGBColor(16, 36, 75)

    conditions_table_data = [
        ("🛠️ Restorative", "Normal / Healthy", "#F3ECE2", "Plain cream/white ivory enamel, no pathology"),
        ("🛠️ Restorative", "Filling — Amalgam", "#9AA5AB", "Solid metallic silver-grey patch on surface zone"),
        ("🛠️ Restorative", "Filling — Composite", "#3B82F6", "Tooth-colored fill with distinct blue outline"),
        ("🛠️ Restorative", "Filling — GIC", "#E8D98A", "Pale glass ionomer cement yellow tint"),
        ("🛠️ Restorative", "Crown — Metal", "#B0B0B0", "Full crown solid metallic-silver fill"),
        ("🛠️ Restorative", "Crown — PFM", "#B0B0B0 / #FFFDF8", "Porcelain-Fused-to-Metal (Grey base + white occlusal rim)"),
        ("🛠️ Restorative", "Crown — Ceramic / Zirconia", "#FFFDF8", "High-gloss translucent ivory white with shine highlight"),
        ("🛠️ Restorative", "Veneer", "#CFEFF0", "Light cyan thin aesthetic overlay on facial/labial surface"),
        ("🛠️ Restorative", "Bridge / Pontic", "#8A8A8A", "Connecting bar between abutments; dashed pontic outline"),
        ("🛠️ Restorative", "Sealant", "#D9E8A0", "Light green-yellow thin coating on occlusal pits & fissures"),
        ("🛠️ Restorative", "Post & Core", "#5A5A5A", "Dark grey vertical post inside root canal space"),

        ("⚡ Endodontic", "Root Canal Treated (RCT)", "#6B4FA0", "Purple vertical line from crown to apex + apical dot"),
        ("⚡ Endodontic", "Pulp Exposure", "#EF4444", "Bright red dot at active pulp horn exposure"),
        ("⚡ Endodontic", "Periapical Lesion / Abscess", "#3D0A0A", "Dark red-black radiolucent apical halo circle"),

        ("🩺 Surgical", "Missing Tooth", "#4A231A", "Empty anatomical alveolar socket depression"),
        ("🩺 Surgical", "Extraction Indicated", "#B5122E", "Red bold 'X' cross overlay over tooth crown"),
        ("🩺 Surgical", "Extracted (History)", "#C9C9C9", "Light grey dashed silhouette + soft past X record"),
        ("🩺 Surgical", "Dental Implant", "#0E8A80", "Teal titanium fixture screw threads + abutment crown"),
        ("🩺 Surgical", "Retained Root", "#6B4029", "Brown residual root apex in bone without clinical crown"),

        ("🔬 Pathology", "Caries (Decay)", "#8B5A2B → #1C110A", "Brown-to-black gradient patch on affected zone"),
        ("🔬 Pathology", "Fractured / Chipped", "#7A1626", "Dark red zigzag crack line across crown"),
        ("🔬 Pathology", "Attrition / Abrasion", "#C9A66B", "Tan/yellow flat worn facet patch on occlusal table"),
        ("🔬 Pathology", "Discoloration / Fluorosis", "#D8CBAE", "Mottled brown-white irregular patches on crown"),
        ("🔬 Pathology", "Root Resorption", "#EA8C1E", "Orange dashed notch / cervical root indent"),

        ("🩸 Periodontal", "Mobility Grade I/II/III", "#22C55E / #F59E0B / #DC2626", "Roman numeral badge (I, II, III) adjacent to tooth"),
        ("🩸 Periodontal", "Gingival Recession", "#E0665A", "Pink-red line showing apical margin level + mm label"),
        ("🩸 Periodontal", "Calculus / Plaque", "#B08D3E", "Yellow-brown supragingival deposit band at cervical line"),

        ("📐 Developmental", "Impacted / Unerupted", "#2F6FED", "Blue dashed outline with angled impaction trajectory"),
        ("📐 Developmental", "Supernumerary Tooth", "#D97706", "Orange dashed extra tooth silhouette + asterisk (*) mark"),
        ("📐 Developmental", "Malposition / Rotation", "#3B82F6", "Rotated tooth orientation + blue curved arrow icon"),
        ("📐 Developmental", "Diastema (Gap)", "#6B7280", "Grey double-ended arrow (↔) + interdental mm measurement"),

        ("🦷 Appliance", "Orthodontic Bracket", "#C0C0C0", "Metallic silver square bracket on facial surface center"),
        ("🦷 Appliance", "Space Maintainer", "#93C5FD", "Light blue band & loop appliance across interdental gap"),
        ("🦷 Appliance", "Denture Section", "#D8A9A0", "Grey-pink dashed arch bracket wrapping spanned ridge")
    ]

    cond_table = doc.add_table(rows=1, cols=4)
    cond_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    cond_table.autofit = False

    chdr = cond_table.rows[0].cells
    chdr[0].width = Inches(1.4)
    chdr[1].width = Inches(2.0)
    chdr[2].width = Inches(1.3)
    chdr[3].width = Inches(2.1)

    for i, title in enumerate(["Category", "Clinical Condition", "Color Code", "Visual Representation"]):
        set_cell_background(chdr[i], "10244B")
        set_cell_margins(chdr[i], 90, 90, 100, 100)
        p = chdr[i].paragraphs[0]
        run = p.add_run(title)
        run.font.bold = True
        run.font.size = Pt(8.5)
        run.font.color.rgb = RGBColor(255, 255, 255)

    for row_idx, (cat, cond, color_code, repr_desc) in enumerate(conditions_table_data):
        row = cond_table.add_row()
        bg_hex = "F8FAFC" if row_idx % 2 == 0 else "FFFFFF"
        for c_idx, text in enumerate([cat, cond, color_code, repr_desc]):
            cell = row.cells[c_idx]
            set_cell_background(cell, bg_hex)
            set_cell_margins(cell, 60, 60, 90, 90)
            p = cell.paragraphs[0]
            r = p.add_run(text)
            r.font.size = Pt(8)
            if c_idx == 0:
                r.font.bold = True
            elif c_idx == 1:
                r.font.bold = True
                r.font.color.rgb = RGBColor(16, 36, 75)
            elif c_idx == 2:
                r.font.bold = True
                r.font.color.rgb = RGBColor(74, 124, 210)

    doc.add_paragraph().paragraph_format.space_after = Pt(8)

    # ==================== 4. 100 AI ASSISTANT CHAT & VOICE EXAMPLES ====================
    h4 = doc.add_paragraph()
    r4 = h4.add_run("4. 100 AI Assistant Voice / Chat Clinical Dictation Examples")
    r4.font.name = 'Arial'
    r4.font.size = Pt(13)
    r4.font.bold = True
    r4.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "Below is a comprehensive corpus of 100 realistic clinical voice and chat dictation scenarios. "
        "The Dentia NLP engine translates natural dentist speech into standardized surface-level EHR charts and odontogram updates:"
    )

    # Generate 100 Real-world Clinical Examples
    raw_100_examples = [
        # 1-15: Single & Multi-surface Caries
        ("1", '"Tooth 14 MOD caries with deep dentinal involvement"', "Tooth #14: Assigns Caries (#8B5A2B) to Mesial (M), Occlusal (O), and Distal (D) zones.", "Restorative / Pathology", "14 (MOD)"),
        ("2", '"Tooth 3 occlusal pit caries ICDAS 3"', "Tooth #3: Assigns Caries (#8B5A2B) to Occlusal (O) pit.", "Pathology", "3 (O)"),
        ("3", '"Tooth 19 DO caries extending subgingivally"', "Tooth #19: Assigns Caries (#8B5A2B) to Distal (D) and Occlusal (O) zones.", "Pathology", "19 (DO)"),
        ("4", '"Tooth 30 MO interproximal decay"', "Tooth #30: Assigns Caries (#8B5A2B) to Mesial (M) and Occlusal (O) surfaces.", "Pathology", "30 (MO)"),
        ("5", '"Tooth 8 ML caries along lingual marginal ridge"', "Tooth #8: Assigns Caries (#8B5A2B) to Mesial (M) and Lingual (L) surfaces.", "Pathology", "8 (ML)"),
        ("6", '"Tooth 9 DL caries near incisal corner"', "Tooth #9: Assigns Caries (#8B5A2B) to Distal (D) and Lingual (L) zones.", "Pathology", "9 (DL)"),
        ("7", '"Tooth 2 buccal cervical caries at gum line"', "Tooth #2: Assigns Caries (#8B5A2B) to Buccal (B) cervical zone.", "Pathology", "2 (B)"),
        ("8", '"Tooth 18 lingual groove decay"', "Tooth #18: Assigns Caries (#8B5A2B) to Lingual (L) groove.", "Pathology", "18 (L)"),
        ("9", '"Tooth 31 MODBL extensive gross caries"', "Tooth #31: Assigns Caries (#8B5A2B) to all 5 surfaces (M, O, D, B, L).", "Pathology", "31 (MODBL)"),
        ("10", '"Tooth 12 DO caries recurrent under existing composite"', "Tooth #12: Assigns Recurrent Caries to Disto-Occlusal zones.", "Pathology", "12 (DO)"),
        ("11", '"Tooth 5 O caries, 12 O caries, and 28 O caries"', "Teeth #5, #12, #28: Assigns Occlusal Caries to all three premolars simultaneously.", "Pathology (Batch)", "5 (O), 12 (O), 28 (O)"),
        ("12", '"Tooth 20 MO decay and 29 DO decay"', "Teeth #20 (MO) & #29 (DO): Assigns proximal caries respectively.", "Pathology (Batch)", "20 (MO), 29 (DO)"),
        ("13", '"Tooth 6 facial root caries with gingival recession"', "Tooth #6: Assigns Facial Caries + marks 2mm gingival recession.", "Pathology & Perio", "6 (F)"),
        ("14", '"Tooth 21 cervical abrasion with secondary caries"', "Tooth #21: Assigns Buccal Caries with Attrition/Abrasion note.", "Pathology", "21 (B)"),
        ("15", '"Tooth 15 deep fissure caries, watchful monitoring"', "Tooth #15: Assigns Incipient Occlusal Caries with clinical alert tag.", "Pathology", "15 (O)"),

        # 16-30: Restorative Fillings (Composite, Amalgam, GIC)
        ("16", '"Tooth 30 occlusal amalgam filling in good condition"', "Tooth #30: Renders metallic silver-grey (#9AA5AB) amalgam patch on Occlusal.", "Restorative", "30 (O)"),
        ("17", '"Tooth 14 MOD amalgam restoration with intact margins"', "Tooth #14: Renders silver amalgam (#9AA5AB) across M, O, D zones.", "Restorative", "14 (MOD)"),
        ("18", '"Tooth 19 DO amalgam filling"', "Tooth #19: Renders Disto-Occlusal amalgam patch (#9AA5AB).", "Restorative", "19 (DO)"),
        ("19", '"Tooth 8 MIFD composite filling, shade A2"', "Tooth #8: Renders composite restoration (blue outline #3B82F6) on Mesial, Incisal, Facial.", "Restorative", "8 (MIF)"),
        ("20", '"Tooth 9 class IV composite restoration on distal angle"', "Tooth #9: Renders composite restoration on Disto-Incisal angle.", "Restorative", "9 (DI)"),
        ("21", '"Tooth 10 lingual pit composite resin"', "Tooth #10: Renders Lingual composite dot with blue perimeter.", "Restorative", "10 (L)"),
        ("22", '"Tooth 24 facial composite veneer-filling"', "Tooth #24: Renders Facial composite restoration (#3B82F6 outline).", "Restorative", "24 (F)"),
        ("23", '"Tooth 25 cervical class V GIC restoration"', "Tooth #25: Renders pale yellow GIC patch (#E8D98A) at cervical margin.", "Restorative", "25 (B)"),
        ("24", '"Tooth 4 MO composite filling"', "Tooth #4: Renders Mesio-Occlusal composite restoration (#3B82F6).", "Restorative", "4 (MO)"),
        ("25", '"Tooth 13 DO composite filling"', "Tooth #13: Renders Disto-Occlusal composite restoration.", "Restorative", "13 (DO)"),
        ("26", '"Tooth 28 MOD composite filling"', "Tooth #28: Renders Mesio-Occluso-Distal composite filling.", "Restorative", "28 (MOD)"),
        ("27", '"Tooth 29 O composite filling"', "Tooth #29: Renders Occlusal composite restoration.", "Restorative", "29 (O)"),
        ("28", '"Tooth 18 class V buccal GIC filling"', "Tooth #18: Renders Buccal GIC filling (#E8D98A).", "Restorative", "18 (B)"),
        ("29", '"Tooth 31 occlusal GIC base under composite"', "Tooth #31: Renders Occlusal composite with underlying GIC liner note.", "Restorative", "31 (O)"),
        ("30", '"Tooth 2 MOD amalgam replacing failed composite"', "Tooth #2: Updates #2 from composite to MOD amalgam (#9AA5AB).", "Restorative", "2 (MOD)"),

        # 31-45: Endodontics (RCT, Pulp, Apical Lesions, Posts)
        ("31", '"Tooth 19 completed root canal treatment"', "Tooth #19: Assigns Root Canal Treated (#6B4FA0) purple obturation seal.", "Endodontic", "19 (Root/Crown)"),
        ("32", '"Tooth 30 RCT with 5mm periapical abscess"', "Tooth #30: Renders RCT purple seal + dark red-black apical halo (#3D0A0A).", "Endodontic", "30 (Apical)"),
        ("33", '"Tooth 14 RCT with custom cast post and core"', "Tooth #14: Renders RCT purple line + dark grey post core (#5A5A5A).", "Endodontic", "14 (Canal)"),
        ("34", '"Tooth 8 trauma with direct pulp exposure"', "Tooth #8: Marks bright red pulp exposure dot (#EF4444) at central chamber.", "Endodontic", "8 (Pulp)"),
        ("35", '"Tooth 9 pulp necrosis with periapical lucency"', "Tooth #9: Marks non-vital necrosis tag + periapical radiolucency (#3D0A0A).", "Endodontic", "9 (Apex)"),
        ("36", '"Tooth 3 root canal treated in 2021, asymptomatic"', "Tooth #3: Assigns past completed RCT status (#6B4FA0).", "Endodontic", "3 (Root)"),
        ("37", '"Tooth 4 pulpectomy initiated, calcium hydroxide placed"', "Tooth #4: Assigns interim RCT stage 1 tag with medicament note.", "Endodontic", "4 (Canal)"),
        ("38", '"Tooth 20 RCT with prefabricated fiber post"', "Tooth #20: Renders RCT obturation line + fiber post marker.", "Endodontic", "20 (Post)"),
        ("39", '"Tooth 29 previously treated root canal with missed MB2 canal"', "Tooth #29: Marks RCT with retreat indicated warning flag.", "Endodontic", "29 (Canal)"),
        ("40", '"Tooth 31 periapical cyst at mesial root apex"', "Tooth #31: Renders apical cyst circle (#3D0A0A) at mesial root.", "Endodontic", "31 (Apex)"),
        ("41", '"Tooth 6 symptomatic apical periodontitis"', "Tooth #6: Marks apical tenderness & inflammation halo.", "Endodontic", "6 (Apex)"),
        ("42", '"Tooth 10 dens evaginatus with pulp involvement"', "Tooth #10: Marks developmental anomaly + pulp alert.", "Endodontic", "10 (Pulp)"),
        ("43", '"Tooth 11 vital pulp therapy performed with MTA"', "Tooth #11: Renders pulp cap marker with MTA liner.", "Endodontic", "11 (Pulp)"),
        ("44", '"Tooth 22 root canal completed, obturated with gutta percha"', "Tooth #22: Renders purple obturation line from orifice to apical constriction.", "Endodontic", "22 (Root)"),
        ("45", '"Tooth 15 apicoectomy with retrograde MTA seal"', "Tooth #15: Marks surgical root resection + retrofill seal.", "Endodontic / Surgical", "15 (Apex)"),

        # 46-60: Crowns, Veneers & Fixed Prosthodontics
        ("46", '"Tooth 19 full zirconia ceramic crown"', "Tooth #19: Renders full ivory-white translucent zirconia crown (#FFFDF8).", "Restorative / Prostho", "19 (Full Crown)"),
        ("47", '"Tooth 30 porcelain fused to metal PFM crown"', "Tooth #30: Renders PFM crown with grey base (#B0B0B0) and white porcelain rim.", "Restorative / Prostho", "30 (Full Crown)"),
        ("48", '"Tooth 18 full cast gold metal crown"', "Tooth #18: Renders full metallic silver-gold crown (#B0B0B0).", "Restorative / Prostho", "18 (Full Crown)"),
        ("49", '"Tooth 8 porcelain laminate veneer"', "Tooth #8: Renders light cyan aesthetic facial shell (#CFEFF0).", "Restorative / Prostho", "8 (Facial)"),
        ("50", '"Tooth 9 porcelain veneer with incisal wrap"', "Tooth #9: Renders light cyan facial veneer (#CFEFF0) with incisal overlap.", "Restorative / Prostho", "9 (Facial/I)"),
        ("51", '"Tooth 6 through 11 cosmetic zirconia veneers"', "Teeth #6, 7, 8, 9, 10, 11: Renders aesthetic veneer shells on all anterior teeth.", "Prostho (Batch)", "6–11 (Facial)"),
        ("52", '"3-unit bridge from tooth 3 to tooth 5 with tooth 4 pontic"', "Renders abutment crowns on #3 & #5 with dashed pontic (#8A8A8A) at #4.", "Prosthodontics", "3, 4, 5 (Bridge)"),
        ("53", '"4-unit bridge teeth 19 to 22 with 20 and 21 pontics"', "Renders fixed partial denture bridging #19 to #22.", "Prosthodontics", "19–22 (Bridge)"),
        ("54", '"Tooth 14 endocrown ceramic restoration"', "Tooth #14: Renders monolithic ceramic endocrown.", "Restorative / Prostho", "14 (Crown)"),
        ("55", '"Tooth 4 ceramic onlay covering buccal cusps"', "Tooth #4: Renders ceramic onlay on Occlusal and Buccal cusps.", "Restorative", "4 (OB)"),
        ("56", '"Tooth 5 composite inlay on MOD surfaces"', "Tooth #5: Renders precision composite inlay across MOD.", "Restorative", "5 (MOD)"),
        ("57", '"Tooth 2 temporary acrylic crown cemented"', "Tooth #2: Renders temporary provisional crown marker.", "Restorative / Prostho", "2 (Temp Cr.)"),
        ("58", '"Tooth 31 stainless steel crown SSC in place"', "Tooth #31: Renders pediatric/adult preformed metallic crown.", "Restorative", "31 (Crown)"),
        ("59", '"Maryland bridge replacing tooth 10, winged to 9 and 11"', "Renders resin-bonded pontic at #10 with lingual wings on #9 and #11.", "Prosthodontics", "9, 10, 11 (Bridge)"),
        ("60", '"Tooth 12 fractured porcelain on existing PFM crown"', "Tooth #12: Marks crown status with chipping fracture alert.", "Restorative / Pathology", "12 (Crown)"),

        # 61-75: Surgical Extractions, Implants, Missing Sockets
        ("61", '"Tooth 1 missing, tooth 16 missing, tooth 17 missing, tooth 32 missing"', "Teeth #1, 16, 17, 32: Marks all four third molars as congenital/extracted missing (#4A231A).", "Surgical (Wisdom)", "1, 16, 17, 32"),
        ("62", '"Tooth 3 extracted previously in 2018"', "Tooth #3: Renders light grey dashed silhouette with past extraction mark (#C9C9C9).", "Surgical", "3 (Socket)"),
        ("63", '"Tooth 14 gross caries, extraction indicated"', "Tooth #14: Overlays prominent red extraction cross 'X' (#B5122E).", "Surgical", "14 (Plan Ext.)"),
        ("64", '"Tooth 19 fractured root, planned for surgical extraction and bone graft"', "Tooth #19: Overlays red extraction cross 'X' with socket preservation tag.", "Surgical", "19 (Plan Ext.)"),
        ("65", '"Tooth 30 replaced by endosseous dental implant with screw retained crown"', "Tooth #30: Renders teal titanium implant fixture (#0E8A80) + custom abutment.", "Surgical / Implant", "30 (Implant)"),
        ("66", '"Tooth 19 dental implant placed, healing abutment attached"', "Tooth #19: Renders implant fixture with healing cap status.", "Surgical / Implant", "19 (Implant)"),
        ("67", '"Tooth 4 and 5 missing, planning two implants"', "Teeth #4 & #5: Marks missing sockets with planned implant surgical flags.", "Surgical", "4, 5 (Sockets)"),
        ("68", '"Tooth 31 retained root tip in alveolar ridge"', "Tooth #31: Renders brown root fragment (#6B4029) without coronal tooth structure.", "Surgical", "31 (Root)"),
        ("69", '"Tooth 15 retained palatal root after incomplete extraction"', "Tooth #15: Marks retained palatal root apex for surgical retrieval.", "Surgical", "15 (Root)"),
        ("70", '"Tooth 1 impacted horizontal third molar"', "Tooth #1: Renders blue dashed outline (#2F6FED) with 90° horizontal impaction tilt.", "Developmental / Surg", "1 (Impacted)"),
        ("71", '"Tooth 17 mesioangular impacted wisdom tooth"', "Tooth #17: Renders blue dashed outline with 45° mesial impaction angle.", "Developmental / Surg", "17 (Impacted)"),
        ("72", '"Tooth 32 distoangular impacted wisdom tooth"', "Tooth #32: Renders blue dashed outline with distal impaction tilt.", "Developmental / Surg", "32 (Impacted)"),
        ("73", '"Tooth 16 vertically impacted in maxillary tuberosity"', "Tooth #16: Marks high vertical impaction with sinus proximity alert.", "Developmental / Surg", "16 (Impacted)"),
        ("74", '"Tooth 6 impacted canine in palate with orthodontic traction plan"', "Tooth #6: Marks palatally impacted canine with bracket traction hook.", "Developmental / Ortho", "6 (Impacted)"),
        ("75", '"All-on-4 implant restoration on mandibular arch"', "Mandible: Renders 4 implants (Teeth #19, 22, 27, 30) supporting full arch hybrid.", "Surgical / Prostho", "19, 22, 27, 30"),

        # 76-88: Periodontics (Mobility, Recession, Furcation, Calculus)
        ("76", '"Tooth 24 grade 2 mobility and 3mm gingival recession"', "Tooth #24: Assigns Roman 'II' mobility badge (#F59E0B) + 3mm recession line (#E0665A).", "Periodontal", "24 (Perio)"),
        ("77", '"Tooth 25 grade 3 mobility, hopless prognosis"', "Tooth #25: Assigns Roman 'III' mobility badge (#DC2626) + extraction recommendation.", "Periodontal", "25 (Mobility III)"),
        ("78", '"Tooth 23 and 26 grade 1 mobility"', "Teeth #23 & 26: Assigns Roman 'I' mobility badges (#22C55E).", "Periodontal", "23, 26 (Mobility I)"),
        ("79", '"Tooth 6 through 11 generalized 2mm to 4mm gingival recession"', "Teeth #6–11: Renders apical gumline recession lines with millimeter markers.", "Periodontal (Batch)", "6–11 (Recession)"),
        ("80", '"Tooth 19 class II furcation involvement on buccal"', "Tooth #19: Renders open triangle furcation icon at buccal root bifurcate.", "Periodontal", "19 (Furcation)"),
        ("81", '"Tooth 30 class III through-and-through furcation"', "Tooth #30: Renders solid filled triangle furcation icon at root bifurcation.", "Periodontal", "30 (Furcation)"),
        ("82", '"Mandibular anteriors teeth 22 to 27 heavy subgingival calculus"', "Teeth #22–27: Renders yellow-brown calculus deposit band (#B08D3E) at lingual cervical margins.", "Periodontal (Batch)", "22–27 (Calculus)"),
        ("83", '"Tooth 3 deep 7mm periodontal pocket on mesial"', "Tooth #3: Marks red periodontal probing depth (7mm) at mesial sulcus.", "Periodontal", "3 (Pocket)"),
        ("84", '"Tooth 14 active bleeding on probing on distal"', "Tooth #14: Marks red BOP bleeding dot at distal sulcus.", "Periodontal", "14 (BOP)"),
        ("85", '"Tooth 28 cervical abfraction with gingival cleft"', "Tooth #28: Marks V-shaped cervical abfraction notch + Stillman's cleft.", "Pathology / Perio", "28 (Cervical)"),
        ("86", '"Tooth 12 localized aggressive periodontitis, 6mm bone loss"', "Tooth #12: Marks severe horizontal alveolar bone resorption.", "Periodontal", "12 (Bone Loss)"),
        ("87", '"Tooth 20 McCall festoon with thickened marginal gingiva"', "Tooth #20: Marks marginal gingival enlargement band.", "Periodontal", "20 (Gums)"),
        ("88", '"Generalized moderate plaque and gingivitis throughout arch"', "Dual Arches: Renders light yellow plaque biofilm band across all cervical margins.", "Periodontal (Global)", "All 32 Teeth"),

        # 89-100: Orthodontics, Developmental, Pediatric & Appliances
        ("89", '"Tooth 8 and 9 midline diastema 2.5mm"', "Midline (8-9): Renders grey double-ended arrow (↔) with '2.5mm' measurement.", "Developmental", "8–9 (Midline)"),
        ("90", '"Tooth 7 and 8 interdental gap 1.5mm"', "Teeth #7 & #8: Renders interdental diastema gap indicator.", "Developmental", "7–8 (Diastema)"),
        ("91", '"Tooth 10 congenitally missing (peg lateral microdontia on 7)"', "Tooth #10: Marks congenital absence; Tooth #7: Marks peg-shaped lateral incisor.", "Developmental", "7 (Peg), 10 (Agenesis)"),
        ("92", '"Tooth 9 mesiodens supernumerary tooth present between centrals"', "Central Midline: Renders orange dashed extra tooth icon (#D97706) with asterisk (*).", "Developmental", "Midline (Supernumerary)"),
        ("93", '"Tooth 12 45 degree mesiopalatal rotation"', "Tooth #12: Renders rotated tooth orientation + blue curved rotation arrow (#3B82F6).", "Developmental", "12 (Rotated)"),
        ("94", '"Tooth 22 labially displaced outside dental arch"', "Tooth #22: Renders malpositioned labial ectopia arrow.", "Developmental", "22 (Ectopic)"),
        ("95", '"Full upper and lower orthodontic brackets placed from second molar to second molar"', "Teeth #2–15 & #18–31: Renders metallic silver brackets (#C0C0C0) on facial surfaces.", "Appliance (Batch)", "2–15, 18–31 (Ortho)"),
        ("96", '"Band and loop space maintainer placed from tooth 30 to 28"', "Teeth #28–30: Renders light blue appliance loop (#93C5FD) spanning missing tooth #29.", "Appliance", "28, 29, 30 (Space Maint.)"),
        ("97", '"Maxillary removable partial denture replacing teeth 4, 5, 12, 13"', "Maxilla: Renders pink-grey dashed partial denture bracket (#D8A9A0) across bilateral gaps.", "Appliance", "4, 5, 12, 13 (RPD)"),
        ("98", '"Tooth 3, 14, 19, 30 pit and fissure sealants applied"', "Teeth #3, 14, 19, 30: Renders light green-yellow sealant resin (#D9E8A0) on occlusal grooves.", "Restorative / Prev", "3, 14, 19, 30 (O Sealant)"),
        ("99", '"Tooth 8 cracked tooth syndrome with pain on release"', "Tooth #8: Renders dark red hairline crack line (#7A1626) through crown.", "Pathology / Trauma", "8 (Crack)"),
        ("100", '"Tooth 9 complete dental avulsion with replantation and flexible splint"', "Tooth #9: Marks trauma avulsion status + 1-week periodontal stabilization splint.", "Trauma / Surgical", "9 (Splint)")
    ]

    ai_table = doc.add_table(rows=1, cols=5)
    ai_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    ai_table.autofit = False

    ai_hdr = ai_table.rows[0].cells
    ai_hdr[0].width = Inches(0.4)
    ai_hdr[1].width = Inches(2.2)
    ai_hdr[2].width = Inches(2.3)
    ai_hdr[3].width = Inches(1.1)
    ai_hdr[4].width = Inches(0.9)

    for i, title in enumerate(["#", "Doctor Dictation (Voice / Text)", "AI Assistant Automated Action", "Category", "Target Teeth"]):
        set_cell_background(ai_hdr[i], "10244B")
        set_cell_margins(ai_hdr[i], 90, 90, 90, 90)
        p = ai_hdr[i].paragraphs[0]
        run = p.add_run(title)
        run.font.bold = True
        run.font.size = Pt(8)
        run.font.color.rgb = RGBColor(255, 255, 255)

    for row_idx, (num, dictation, action, cat, target) in enumerate(raw_100_examples):
        row = ai_table.add_row()
        bg_hex = "F8FAFC" if row_idx % 2 == 0 else "FFFFFF"
        for c_idx, text in enumerate([num, dictation, action, cat, target]):
            cell = row.cells[c_idx]
            set_cell_background(cell, bg_hex)
            set_cell_margins(cell, 50, 50, 70, 70)
            p = cell.paragraphs[0]
            r = p.add_run(text)
            r.font.size = Pt(7.5)
            if c_idx == 0:
                r.font.bold = True
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            elif c_idx == 1:
                r.font.bold = True
                r.font.color.rgb = RGBColor(16, 36, 75)
            elif c_idx == 3:
                r.font.color.rgb = RGBColor(74, 124, 210)
                r.font.bold = True
            elif c_idx == 4:
                r.font.bold = True
                r.font.color.rgb = RGBColor(15, 23, 42)

    doc.add_paragraph().paragraph_format.space_after = Pt(8)

    # ==================== 5. CODEBASE COMPONENT ARCHITECTURE ====================
    h5 = doc.add_paragraph()
    r5 = h5.add_run("5. Codebase Component Architecture")
    r5.font.name = 'Arial'
    r5.font.size = Pt(13)
    r5.font.bold = True
    r5.font.color.rgb = RGBColor(16, 36, 75)

    comp_items = [
        ("ToothSurfaceDiagram.jsx", "Interactive 5-zone SVG geometric cross diagram. Supports dynamic orientation (Buccal/Lingual, Mesial/Distal) based on Maxilla vs Mandible quadrants. Live multi-zone state management (MOD, DO, MO, B, L)."),
        ("ClinicalConditionPalette.jsx", "Grouped 7-category EHR condition palette with color swatches, active condition selector, and expandable AI voice commands drawer."),
        ("ThreeDentalJawArch.jsx", "WebGL 3D dental arch engine with orthographic camera, directional studio lighting, raycasting hover/click, and dynamic clinical texture/material shaders seated inside millimeter-calibrated empty jaw sockets."),
        ("ChartPage.jsx", "Master clinical odontogram orchestrator combining 3D WebGL arches, 2D dual-jaw visualizer, live audio waveform equalizer, AI SOAP note generator, and interactive patient EHR records.")
    ]

    for filename, desc in comp_items:
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4)
        run_fn = p.add_run(f"• {filename}: ")
        run_fn.font.bold = True
        run_fn.font.color.rgb = RGBColor(74, 124, 210)
        run_desc = p.add_run(desc)
        run_desc.font.size = Pt(9)

    # Save Document
    out_path = r"f:\DentistApp_Theme2\Dentia_Clinical_Odontogram_Specification.docx"
    doc.save(out_path)
    print(f"Document successfully created at: {out_path}")

if __name__ == '__main__':
    create_document()
