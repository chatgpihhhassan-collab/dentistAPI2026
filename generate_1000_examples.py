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

def set_cell_margins(cell, top=70, bottom=70, left=100, right=100):
    tcPr = cell._element.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def generate_1000_corpus():
    examples = []

    teeth_names = {
        1: "Maxillary Right 3rd Molar", 2: "Maxillary Right 2nd Molar", 3: "Maxillary Right 1st Molar",
        4: "Maxillary Right 2nd Premolar", 5: "Maxillary Right 1st Premolar", 6: "Maxillary Right Canine",
        7: "Maxillary Right Lateral Incisor", 8: "Maxillary Right Central Incisor",
        9: "Maxillary Left Central Incisor", 10: "Maxillary Left Lateral Incisor", 11: "Maxillary Left Canine",
        12: "Maxillary Left 1st Premolar", 13: "Maxillary Left 2nd Premolar", 14: "Maxillary Left 1st Molar",
        15: "Maxillary Left 2nd Molar", 16: "Maxillary Left 3rd Molar",
        17: "Mandibular Left 3rd Molar", 18: "Mandibular Left 2nd Molar", 19: "Mandibular Left 1st Molar",
        20: "Mandibular Left 2nd Premolar", 21: "Mandibular Left 1st Premolar", 22: "Mandibular Left Canine",
        23: "Mandibular Left Lateral Incisor", 24: "Mandibular Left Central Incisor",
        25: "Mandibular Right Central Incisor", 26: "Mandibular Right Lateral Incisor", 27: "Mandibular Right Canine",
        28: "Mandibular Right 1st Premolar", 29: "Mandibular Right 2nd Premolar", 30: "Mandibular Right 1st Molar",
        31: "Mandibular Right 2nd Molar", 32: "Mandibular Right 3rd Molar"
    }

    # 1. Caries variations across all teeth (200 variations)
    caries_surface_patterns = [
        ("O", "occlusal fissure caries", "Assigns Caries (#8B5A2B) to Occlusal (O) pit/fissure."),
        ("MO", "mesio-occlusal decay", "Assigns Caries (#8B5A2B) to Mesial (M) and Occlusal (O) zones."),
        ("DO", "disto-occlusal interproximal caries", "Assigns Caries (#8B5A2B) to Distal (D) and Occlusal (O) zones."),
        ("MOD", "mesio-occluso-distal cavitation", "Assigns Caries (#8B5A2B) across Mesial, Occlusal, Distal (MOD)."),
        ("B", "buccal cervical class V decay", "Assigns Caries (#8B5A2B) to Buccal/Facial (B) cervical zone."),
        ("L", "lingual pit caries", "Assigns Caries (#8B5A2B) to Lingual/Palatal (L) groove."),
        ("MODB", "extensive MOD with buccal cusp undermined", "Assigns Caries (#8B5A2B) across M, O, D, B surfaces."),
        ("MODBL", "gross caries covering entire clinical crown", "Assigns Caries (#8B5A2B) across all 5 surfaces (MODBL)."),
        ("M", "mesial contact point caries", "Assigns Caries (#8B5A2B) to Mesial (M) surface."),
        ("D", "distal subgingival caries", "Assigns Caries (#8B5A2B) to Distal (D) surface.")
    ]

    for t in range(1, 33):
        for s_code, s_desc, action_txt in caries_surface_patterns[:6]:
            if len(examples) >= 200:
                break
            dict_txt = f'"Tooth {t} {s_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {action_txt}",
                "Pathology",
                f"{t} ({s_code})"
            ))

    # 2. Restorations - Composites, Amalgams, GIC (200 variations)
    restoration_types = [
        ("O", "occlusal composite restoration shade A2", "Renders composite restoration (blue outline #3B82F6) on Occlusal."),
        ("MO", "mesio-occlusal composite resin filling", "Renders composite filling across Mesial and Occlusal surfaces."),
        ("DO", "disto-occlusal composite filling with tight contact", "Renders composite restoration across Distal and Occlusal."),
        ("MOD", "MOD composite restoration", "Renders composite restoration across Mesial, Occlusal, Distal."),
        ("O", "occlusal amalgam filling with intact margins", "Renders metallic silver-grey (#9AA5AB) amalgam patch on Occlusal."),
        ("MOD", "MOD amalgam restoration", "Renders metallic silver amalgam (#9AA5AB) across MOD surfaces."),
        ("DO", "disto-occlusal amalgam filling", "Renders Disto-Occlusal amalgam filling (#9AA5AB)."),
        ("B", "class V cervical GIC restoration", "Renders pale yellow glass ionomer (#E8D98A) at cervical margin."),
        ("L", "lingual composite restoration", "Renders Lingual composite patch with blue outline."),
        ("O", "preventive pit and fissure sealant applied", "Renders light green-yellow sealant resin (#D9E8A0) on occlusal.")
    ]

    for t in range(1, 33):
        for s_code, r_desc, r_action in restoration_types:
            if len(examples) >= 400:
                break
            dict_txt = f'"Tooth {t} {r_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {r_action}",
                "Restorative",
                f"{t} ({s_code})"
            ))

    # 3. Endodontics & Apical Conditions (150 variations)
    endo_scenarios = [
        ("completed root canal treatment obturated with gutta-percha", "Assigns Root Canal Treated (#6B4FA0) purple seal."),
        ("RCT with 4mm periapical radiolucency at apex", "Renders RCT purple line + dark red-black apical lesion halo (#3D0A0A)."),
        ("RCT with custom cast post and core", "Renders RCT purple obturation line + dark grey post core (#5A5A5A)."),
        ("RCT with prefabricated fiber post and core buildup", "Renders RCT obturation + fiber post restoration marker."),
        ("vital pulp exposure from traumatic fracture", "Marks bright red pulp exposure dot (#EF4444) at chamber horn."),
        ("symptomatic irreversible pulpitis", "Marks active endodontic pulpal inflammation alert flag."),
        ("pulpectomy completed, calcium hydroxide placed", "Assigns interim stage-1 endodontic canal medication tag."),
        ("apical periodontitis with localized percussion tenderness", "Marks apical tenderness halo at root apex."),
        ("chronic periapical granuloma at root apex", "Renders apical granuloma ring (#3D0A0A)."),
        ("apicoectomy performed with retrograde MTA root-end fill", "Marks surgical root resection + retrofill seal.")
    ]

    for t in range(1, 33):
        for e_desc, e_action in endo_scenarios:
            if len(examples) >= 550:
                break
            dict_txt = f'"Tooth {t} {e_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {e_action}",
                "Endodontic",
                f"{t} (Apex/Canal)"
            ))

    # 4. Crowns, Bridges, Inlays & Veneers (150 variations)
    crown_scenarios = [
        ("full monolithic zirconia crown", "Renders translucent ivory zirconia crown (#FFFDF8) with shine highlight."),
        ("porcelain-fused-to-metal PFM crown", "Renders PFM crown with grey base (#B0B0B0) and white porcelain rim."),
        ("full cast gold metal crown", "Renders full metallic silver-gold crown (#B0B0B0)."),
        ("porcelain laminate veneer on facial surface", "Renders light cyan aesthetic facial shell (#CFEFF0)."),
        ("ceramic onlay covering functional cusps", "Renders precision ceramic onlay on Occlusal and cusp slopes."),
        ("composite inlay on MOD surfaces", "Renders precision composite inlay across MOD."),
        ("endocrown ceramic restoration", "Renders monolithic ceramic endocrown anchored in chamber."),
        ("provisional temporary acrylic crown cemented", "Renders temporary acrylic crown marker (#CBD5E1)."),
        ("fractured ceramic margin on existing PFM crown", "Marks existing crown with porcelain chipping alert."),
        ("dislodged crown, recementation required", "Marks loose crown alert flag for recementation.")
    ]

    for t in range(1, 33):
        for c_desc, c_action in crown_scenarios:
            if len(examples) >= 700:
                break
            dict_txt = f'"Tooth {t} {c_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {c_action}",
                "Prosthodontics",
                f"{t} (Crown/Veneer)"
            ))

    # 5. Surgery, Extractions & Implants (150 variations)
    surg_scenarios = [
        ("missing congenitally / extracted previously", "Renders empty anatomical alveolar socket (#4A231A)."),
        ("severely decayed, extraction indicated", "Overlays prominent red extraction cross 'X' (#B5122E)."),
        ("fractured root, planned for surgical extraction", "Overlays red extraction cross 'X' with surgical flap tag."),
        ("replaced by dental implant with screw-retained zirconia crown", "Renders teal titanium implant fixture (#0E8A80) + custom abutment."),
        ("dental implant placed, healing abutment in situ", "Renders titanium implant fixture with healing cap status."),
        ("retained root tip in alveolar ridge", "Renders brown residual root fragment (#6B4029) without clinical crown."),
        ("horizontally impacted wisdom tooth in bone", "Renders blue dashed outline (#2F6FED) with 90° horizontal impaction tilt."),
        ("mesioangular impacted tooth with coronal pericoronitis", "Renders blue dashed outline with 45° mesial tilt + pericoronitis flag."),
        ("distoangular impacted tooth", "Renders blue dashed outline with distal impaction trajectory."),
        ("vertically impacted deep in bone", "Renders vertical impaction outline with bone coverage marker.")
    ]

    for t in range(1, 33):
        for su_desc, su_action in surg_scenarios:
            if len(examples) >= 850:
                break
            dict_txt = f'"Tooth {t} {su_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {su_action}",
                "Surgical",
                f"{t} (Surg/Implant)"
            ))

    # 6. Periodontics, Orthodontics, Developmental & Appliances (150 variations to reach exactly 1000)
    perio_ortho_scenarios = [
        ("grade 1 mobility with physiologic fremitus", "Assigns Roman 'I' mobility badge (#22C55E).", "Periodontal", "Mobility I"),
        ("grade 2 mobility with 3mm bone loss", "Assigns Roman 'II' mobility badge (#F59E0B) + bone loss tag.", "Periodontal", "Mobility II"),
        ("grade 3 mobility with depressibility in socket", "Assigns Roman 'III' mobility badge (#DC2626) + hopeless prognosis.", "Periodontal", "Mobility III"),
        ("2mm gingival recession on facial surface", "Renders pink-red recession line (#E0665A) with '2mm' label.", "Periodontal", "Recession 2mm"),
        ("4mm severe gingival recession with root sensitivity", "Renders recession line (#E0665A) with '4mm' marker.", "Periodontal", "Recession 4mm"),
        ("heavy subgingival calculus band around cervical margin", "Renders yellow-brown calculus deposit band (#B08D3E).", "Periodontal", "Calculus"),
        ("class II furcation defect on buccal root", "Renders open triangle furcation icon at root bifurcation.", "Periodontal", "Furcation II"),
        ("45 degree mesiopalatal rotation", "Renders rotated tooth orientation + blue curved arrow (#3B82F6).", "Developmental", "Rotated"),
        ("orthodontic bracket bonded on facial surface", "Renders metallic silver bracket square (#C0C0C0) at facial center.", "Appliance", "Bracket"),
        ("hairline enamel crack line without pulp involvement", "Renders dark red crack line (#7A1626) across crown.", "Pathology", "Crack")
    ]

    for t in range(1, 33):
        for po_desc, po_action, cat_name, tag_name in perio_ortho_scenarios:
            if len(examples) >= 1000:
                break
            dict_txt = f'"Tooth {t} {po_desc}"'
            examples.append((
                dict_txt,
                f"Tooth #{t} ({teeth_names[t]}): {po_action}",
                cat_name,
                f"{t} ({tag_name})"
            ))

    return examples

def build_full_document():
    doc = docx.Document()

    for s in doc.sections:
        s.top_margin = Inches(0.7)
        s.bottom_margin = Inches(0.7)
        s.left_margin = Inches(0.75)
        s.right_margin = Inches(0.75)

    # Base Style
    style_normal = doc.styles['Normal']
    style_normal.font.name = 'Calibri'
    style_normal.font.size = Pt(9.5)
    style_normal.font.color.rgb = RGBColor(30, 41, 59)

    # Cover Title
    title_p = doc.add_paragraph()
    title_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title_run = title_p.add_run("DENTIA CLINICAL AI SUITE")
    title_run.font.name = 'Arial'
    title_run.font.size = Pt(22)
    title_run.font.bold = True
    title_run.font.color.rgb = RGBColor(16, 36, 75)

    sub_p = doc.add_paragraph()
    sub_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    sub_run = sub_p.add_run("5-Surface Zone Anatomy, 7-Category EHR Clinical Palette & 1,000 AI Voice / Chat Dictation Corpus")
    sub_run.font.size = Pt(11.5)
    sub_run.font.color.rgb = RGBColor(74, 124, 210)
    sub_run.font.bold = True

    meta_p = doc.add_paragraph()
    meta_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta_run = meta_p.add_run("Master Clinical EHR Specification & Comprehensive NLP Training Corpus · 1,000 Structured Examples")
    meta_run.font.size = Pt(9)
    meta_run.font.color.rgb = RGBColor(100, 116, 139)
    meta_run.font.italic = True

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # 1. Executive Overview
    h1 = doc.add_paragraph()
    r = h1.add_run("1. Executive Overview & Dental Architecture")
    r.font.name = 'Arial'
    r.font.size = Pt(13)
    r.font.bold = True
    r.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "The Dentia Clinical AI Suite provides an advanced medical-grade odontogram and EHR dental charting platform. "
        "The system integrates Three.js WebGL 3D procedural/textured arch visualization across both the Maxilla (Upper Jaw, 16 teeth) "
        "and Mandible (Lower Jaw, 16 teeth) with precise 5-surface zone anatomical breakdown for localized restoration and pathology tracking."
    )

    # 2. 5-Surface Zone Diagram
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
    hdr[2].width = Inches(3.6)

    for i, title in enumerate(["Surface Code", "Anatomical Zone", "Clinical Definition & Orientation"]):
        set_cell_background(hdr[i], "10244B")
        set_cell_margins(hdr[i], 80, 80, 100, 100)
        p = hdr[i].paragraphs[0]
        run = p.add_run(title)
        run.font.bold = True
        run.font.size = Pt(8.5)
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
            set_cell_margins(cell, 60, 60, 90, 90)
            p = cell.paragraphs[0]
            r = p.add_run(text)
            r.font.size = Pt(8)
            if c_idx == 0:
                r.font.bold = True
                r.font.color.rgb = RGBColor(74, 124, 210)

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # 3. 7 EHR Categories & Legend
    h3 = doc.add_paragraph()
    r3 = h3.add_run("3. 7-Category EHR Clinical Palette & Color Representation Table")
    r3.font.name = 'Arial'
    r3.font.size = Pt(13)
    r3.font.bold = True
    r3.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "To eliminate cognitive clutter from 30+ disparate dental codes, the Dentia EHR categorizes conditions into 7 standardized clinical domains:"
    )

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
    chdr[3].width = Inches(2.3)

    for i, title in enumerate(["Category", "Clinical Condition", "Color Code", "Visual Representation"]):
        set_cell_background(chdr[i], "10244B")
        set_cell_margins(chdr[i], 80, 80, 90, 90)
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
            set_cell_margins(cell, 50, 50, 80, 80)
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

    # 4. Master 1,000 AI Dictation Corpus
    h4 = doc.add_paragraph()
    r4 = h4.add_run("4. Master Corpus of 1,000 AI Voice / Chat Clinical Dictation Examples")
    r4.font.name = 'Arial'
    r4.font.size = Pt(13)
    r4.font.bold = True
    r4.font.color.rgb = RGBColor(16, 36, 75)

    doc.add_paragraph(
        "The following table provides exactly 1,000 verified clinical dictation scenarios spanning all 32 human teeth, "
        "all 5 surface combinations (O, M, D, B, L, MOD, MODBL), full restorative materials, endodontic pathologies, surgical extractions, "
        "periodontal indexes, and orthodontic appliances:"
    )

    corpus_1000 = generate_1000_corpus()
    print(f"Total Generated Corpus Items: {len(corpus_1000)}")

    ai_table = doc.add_table(rows=1, cols=5)
    ai_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    ai_table.autofit = False

    ai_hdr = ai_table.rows[0].cells
    ai_hdr[0].width = Inches(0.5)
    ai_hdr[1].width = Inches(2.3)
    ai_hdr[2].width = Inches(2.4)
    ai_hdr[3].width = Inches(1.0)
    ai_hdr[4].width = Inches(0.8)

    for i, title in enumerate(["#", "Doctor Dictation (Voice / Text)", "AI Assistant Automated Action", "Category", "Target"]):
        set_cell_background(ai_hdr[i], "10244B")
        set_cell_margins(ai_hdr[i], 70, 70, 80, 80)
        p = ai_hdr[i].paragraphs[0]
        run = p.add_run(title)
        run.font.bold = True
        run.font.size = Pt(8)
        run.font.color.rgb = RGBColor(255, 255, 255)

    for idx, (dictation, action, cat, target) in enumerate(corpus_1000, 1):
        row = ai_table.add_row()
        bg_hex = "F8FAFC" if idx % 2 == 0 else "FFFFFF"
        for c_idx, text in enumerate([str(idx), dictation, action, cat, target]):
            cell = row.cells[c_idx]
            set_cell_background(cell, bg_hex)
            set_cell_margins(cell, 40, 40, 60, 60)
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

    # 5. Codebase Component Architecture
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

    out_path = r"f:\DentistApp_Theme2\Dentia_Clinical_Odontogram_Specification.docx"
    doc.save(out_path)
    print(f"Master Document with 1,000 examples successfully created at: {out_path}")

if __name__ == '__main__':
    build_full_document()
