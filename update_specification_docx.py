import docx
from docx.shared import Pt, RGBColor, Inches
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:bottom w:w="{bottom}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>')
    tcPr.append(tcMar)

def update_docx():
    docx_path = "Dentia_Clinical_Odontogram_Specification.docx"
    doc = docx.Document(docx_path)
    
    print(f"Loaded {docx_path} with {len(doc.paragraphs)} paragraphs and {len(doc.tables)} tables.")

    # Detailed Palette rows for Table 1 (All categories and conditions)
    palette_data = [
        # 1. Restorative
        ("🛠️ Restorative", "Normal / Healthy", "#10B981", "Plain cream/white ivory enamel, sound intact structure (CDT D0120)"),
        ("🛠️ Restorative", "Filling — Composite", "#2563EB", "Light-cured tooth-colored resin fill with royal blue boundary (CDT D2391/D2392)"),
        ("🛠️ Restorative", "Filling — Amalgam", "#64748B", "Solid metallic silver-slate fill on localized surface zone (CDT D2140/D2150)"),
        ("🛠️ Restorative", "Filling — GIC", "#F59E0B", "Pale warm amber glass ionomer cement fill with fluoride release (CDT D2330)"),
        ("🛠️ Restorative", "Inlay / Onlay — Ceramic", "#3B82F6", "Precision indirect ceramic inlay/onlay with translucent finish (CDT D2610)"),
        
        # 2. Pathology & Caries
        ("🔴 Pathology", "Caries — Occlusal (Class I)", "#EF4444", "Crimson/dark brown cavitation on occlusal pits and fissures (CDT D2140)"),
        ("🔴 Pathology", "Caries — Interproximal (Class II)", "#EF4444", "Mesial (MO) or Distal (DO/MOD) proximal enamel-dentin lesion (CDT D2150)"),
        ("🔴 Pathology", "Caries — Cervical (Class V)", "#EF4444", "Buccal/facial gingival margin demineralization lesion (CDT D2140)"),
        ("🔴 Pathology", "Demineralization / White Spot", "#FCA5A5", "Early sub-surface enamel demineralization without cavitation (CDT D1351)"),
        ("🔴 Pathology", "Periapical Abscess / Infection", "#EF4444", "Acute periapical radiolucent lesion with active exudate (CDT D0220)"),
        
        # 3. Endodontics
        ("🟣 Endodontics", "Root Canal Therapy (RCT)", "#7C3AED", "Debrided root canals with gutta-percha orange points and violet core (CDT D3330)"),
        ("🟣 Endodontics", "Pulpotomy — MTA Bioceramic", "#7C3AED", "Coronal pulp amputation with bioactive MTA bioceramic barrier (CDT D3220)"),
        ("🟣 Endodontics", "Apicoectomy / Retrofill", "#6D28D9", "Surgical root-end resection with MTA retrograde root filling (CDT D3410)"),
        ("🟣 Endodontics", "Internal / External Resorption", "#8B5CF6", "Radicular dentin resorption with bioluminescent violet halo (CDT D3450)"),

        # 4. Prosthodontics
        ("👑 Prosthodontics", "Crown — Full Ceramic / Zirconia", "#D97706", "Full-coronal monolithic aesthetic zirconia or E.max crown (CDT D2740)"),
        ("👑 Prosthodontics", "Crown — PFM (Porcelain-Metal)", "#B45309", "Porcelain-fused-to-metal crown with subtle metal collar (CDT D2750)"),
        ("👑 Prosthodontics", "Crown — Stainless Steel (SSC)", "#64748B", "Preformed full-coverage pediatric metallic crown (CDT D2930)"),
        ("👑 Prosthodontics", "Bridge Pontic / Retainer", "#D97706", "Fixed partial denture prosthetic replacement unit (CDT D6240)"),
        ("👑 Prosthodontics", "Veneer — Porcelain / Resin", "#3B82F6", "Labial aesthetic laminate shell bonded to anterior enamel (CDT D2962)"),

        # 5. Periodontics & Surgery
        ("🦷 Periodontics", "Periodontal Bone Loss (Furcation)", "#E0665A", "Moderate-severe alveolar crest resorption with 5-7mm pockets (CDT D4341)"),
        ("🦷 Periodontics", "Gingival Recession / Exposed Root", "#E0665A", "Apical migration of gingival margin exposing root cementum (CDT D9910)"),
        ("🦷 Periodontics", "Dental Implant — Titanium/Zirconia", "#0E8A80", "Osseointegrated endosseous implant fixture with abutment (CDT D6010)"),
        ("🦷 Periodontics", "Extracted / Missing Tooth", "#DC2626", "Clinically absent / extracted tooth with empty alveolar socket (CDT D7140)"),
        ("🦷 Periodontics", "Tooth Mobility (Class I-III)", "#E0665A", "Horizontal/axial tooth hypermobility due to periodontal ligament breakdown"),

        # 6. Orthodontics
        ("📐 Orthodontics", "Orthodontic Brackets & Archwire", "#0284C7", "Bonded metallic/ceramic bracket with horizontal tension wire (CDT D8080)"),
        ("📐 Orthodontics", "Class II Deep Overbite (80%)", "#0284C7", "Excessive vertical incisal overlap with palatal impingement (CDT D8080)"),
        ("📐 Orthodontics", "Class III Prognathic Underbite", "#0284C7", "Negative anterior overjet with mandibular prognathism (CDT D8080)"),
        ("📐 Orthodontics", "Crossbite (Anterior / Posterior)", "#0284C7", "Transverse lateral displacement of molar and premolar cusps (CDT D8210)"),
        ("📐 Orthodontics", "Anterior Open Bite (4.5mm)", "#0284C7", "Vertical interincisal gap with incomplete occlusion (CDT D8220)"),

        # 7. TMJ & Musculoskeletal
        ("🧠 TMJ Disorders", "TMJ Anterior Disc Displacement", "#E11D48", "Condylar disc displacement with reciprocal acoustic clicking (CDT D7880)"),
        ("🧠 TMJ Disorders", "TMJ Closed Lock / Trismus", "#E11D48", "Non-reducing disc displacement restricting mouth opening <28mm (CDT D7880)"),
        ("🧠 TMJ Disorders", "Masseter Muscle Hypertonicity", "#E11D48", "Myofascial trigger points with masticatory muscle tenderness (CDT D7880)"),

        # 8. Pediatric Dentistry (A-T)
        ("🧸 Pediatric (A-T)", "Primary Deciduous Sound Enamel", "#10B981", "Intact deciduous milk tooth with healthy translucent enamel (CDT D0120)"),
        ("🧸 Pediatric (A-T)", "Early Childhood Caries (ECC)", "#EF4444", "Rampant primary enamel demineralization / nursing bottle decay (CDT D2140)"),
        ("🧸 Pediatric (A-T)", "Primary Pulpotomy (MTA)", "#7C3AED", "Coronal pulpal debridement and bio-ceramic seal on primary molar (CDT D3220)"),
        ("🧸 Pediatric (A-T)", "Primary Stainless Steel Crown", "#64748B", "Full coronal restoration for multi-surface primary decay (CDT D2930)"),
        ("🧸 Pediatric (A-T)", "Space Maintainer (Band & Loop)", "#93C5FD", "Fixed orthodontic loop preserving arch space for permanent successor (CDT D1510)"),
        ("🧸 Pediatric (A-T)", "Primary Tooth Exfoliated / Absent", "#DC2626", "Naturally exfoliated or surgically removed primary tooth (CDT D7111)"),
        ("🧸 Pediatric (A-T)", "Fluoride Varnish / Sealant", "#06B6D4", "High-concentration 5% NaF varnish desensitizing glaze (CDT D1206)"),

        # 9. Bruxism & Attrition
        ("⚡ Bruxism & Wear", "Severe Occlusal Attrition", "#F59E0B", "Flattened molar table with amber exposed dentin pools (CDT D9944)"),
        ("⚡ Bruxism & Wear", "Canine Cusp Flattening", "#F59E0B", "Loss of pointed canine guidance due to lateral parafunctional grinding (CDT D9944)"),
        ("⚡ Bruxism & Wear", "Anterior Incisal Chipping", "#F59E0B", "Micro-fractures and jagged enamel edges from nocturnal clenching (CDT D9944)"),
        ("⚡ Bruxism & Wear", "Pediatric Teething Attrition", "#F59E0B", "Mild deciduous cusp wear in children during primary teething (CDT D9944)"),

        # 10. Hypersensitivity & Erosion
        ("❄️ Sensitivity", "Dentin Hypersensitivity (Exposed Root)", "#3B82F6", "Cervical CEJ exposure producing sharp thermal stimulation pain (CDT D9910)"),
        ("❄️ Sensitivity", "Enamel Acid Erosion (Chemical)", "#F59E0B", "Smooth cupped enamel erosion with thin glassy translucent edges (CDT D9910)"),
        ("❄️ Sensitivity", "Cracked Enamel / Craze Line", "#F59E0B", "Hairline vertical fracture line splitting marginal ridge (CDT D2740)"),

        # 11. Radiographic & Subclinical
        ("📡 Radiographic", "Alveolar Crest Bone Resorption", "#E0665A", "Radiographic horizontal/vertical bone loss spanning root furcation (CDT D4341)"),
        ("📡 Radiographic", "Periapical Cyst / Granuloma", "#8B5CF6", "Well-defined radiolucent cortical halo at root apex (CDT D0220)"),
        ("📡 Radiographic", "Hidden Interproximal Caries", "#EF4444", "Sub-enamel interproximal bite-wing decay hidden under contact (CDT D0272)"),

        # 12. Impactions & Supernumerary
        ("🦷 Impactions", "Mesioangular Wisdom Impaction", "#8B5CF6", "45° tilted third molar crown impinging on second molar root (CDT D7230)"),
        ("🦷 Impactions", "Horizontal Molar Impaction (90°)", "#8B5CF6", "Transverse 90° bone impaction adjacent to IAN nerve canal (CDT D7240)"),
        ("🦷 Impactions", "Palatally Trapped Canine", "#8B5CF6", "Maxillary canine unerupted and locked within palatal vault (CDT D7280)"),
        ("🦷 Impactions", "Supernumerary Tooth (Mesiodens)", "#8B5CF6", "Extra anatomical tooth positioned between central incisors (CDT D7280/D7140)")
    ]

    # Re-populate Table 1 with complete palette
    table1 = doc.tables[1]
    # Remove existing data rows (keep header)
    while len(table1.rows) > 1:
        tr = table1.rows[-1]._tr
        table1._tbl.remove(tr)

    for cat, cond, col, vis in palette_data:
        row = table1.add_row()
        cells = row.cells
        cells[0].text = cat
        cells[1].text = cond
        cells[2].text = col
        cells[3].text = vis

        # Format row styling
        for idx, cell in enumerate(cells):
            set_cell_margins(cell, top=120, bottom=120, left=140, right=140)
            p = cell.paragraphs[0]
            p.runs[0].font.size = Pt(9.5)
            p.runs[0].font.name = 'Calibri'
            if idx == 0:
                p.runs[0].font.bold = True
            elif idx == 2:
                # Color code column
                p.runs[0].font.bold = True
                clean_hex = col.replace('#', '')
                set_cell_background(cell, clean_hex)
                # If dark color, white text, else dark text
                if clean_hex in ['7C3AED', '6D28D9', '0E8A80', '0284C7', 'E11D48', 'DC2626', 'EF4444', '2563EB']:
                    p.runs[0].font.color.rgb = RGBColor(255, 255, 255)
                else:
                    p.runs[0].font.color.rgb = RGBColor(15, 23, 42)

    print(f"Updated Table 1 with {len(palette_data)} comprehensive clinical conditions.")

    # Update Table 3 (Categories Overview)
    master_categories = [
        ("1", "🛠️ Restorative & Operative", "Composite Resin, Silver Amalgam, Glass Ionomer (GIC), Inlays/Onlays, Fissure Sealants", "#2563EB (Blue)"),
        ("2", "🔴 Pathology & Caries", "Pit & Fissure Caries, Cavitation, Interproximal Decay, Class V Cervical Lesions, Demineralization", "#EF4444 (Red)"),
        ("3", "🟣 Endodontics & Vital Pulp", "Root Canal Therapy (RCT), Gutta-percha Canal Obturation, MTA Pulpotomy, Periapical Abscess", "#7C3AED (Purple)"),
        ("4", "👑 Prosthodontics & Crowns", "Monolithic Zirconia, PFM Crowns, Stainless Steel Crowns (SSC), Fixed Bridge Pontics, Veneers", "#D97706 (Gold)"),
        ("5", "🦷 Periodontics & Surgery", "Titanium Dental Implants, Surgical Extractions, Alveolar Bone Loss, Gingival Recession, Mobility", "#0E8A80 (Teal)"),
        ("6", "📐 Orthodontics & Occlusion", "Fixed Orthodontic Brackets, Deep Overbite (80%), Prognathic Underbite, Crossbite, Open Bite", "#0284C7 (Sky Blue)"),
        ("7", "🧠 TMJ & Musculoskeletal", "Anterior Disc Displacement, Joint Clicking, TMJ Closed Lock / Trismus (<28mm), Masseter Trigger Points", "#E11D48 (Rose)"),
        ("8", "🧸 Pediatric Dentistry (A–T)", "Primary Deciduous Teeth, Coronal Pulpotomy (MTA), Stainless Steel Crowns, Fixed Space Maintainers, ECC", "#7C3AED (Purple) / #64748B"),
        ("9", "⚡ Bruxism & Attrition", "Severe Occlusal Grinding & Exposed Dentin, Canine Cusp Flattening, Incisal Chipping, Teething Wear", "#F59E0B (Amber)"),
        ("10", "❄️ Hypersensitivity & Erosion", "Exposed Root CEJ Sensitivity, Chemical Acid Enamel Erosion, Cracked Enamel / Craze Micro-lines", "#3B82F6 (Blue) / #F59E0B"),
        ("11", "📡 Radiographic Findings", "Alveolar Crest Resorption, Furcation Involvement, Apical Cysts, Radicular Root Resorption, Hidden Caries", "#E0665A (Coral) / #8B5CF6"),
        ("12", "🦷 Impactions & Supernumerary", "Mesioangular 3rd Molars, Horizontally Impacted Molars, Palatally Trapped Canines, Supernumerary Mesiodens", "#8B5CF6 (Violet)")
    ]

    table3 = doc.tables[3]
    while len(table3.rows) > 1:
        tr = table3.rows[-1]._tr
        table3._tbl.remove(tr)

    for cat_num, cat_name, cat_desc, cat_col in master_categories:
        row = table3.add_row()
        cells = row.cells
        cells[0].text = cat_num
        cells[1].text = cat_name
        cells[2].text = cat_desc
        cells[3].text = cat_col

        for idx, cell in enumerate(cells):
            set_cell_margins(cell, top=100, bottom=100, left=120, right=120)
            p = cell.paragraphs[0]
            p.runs[0].font.size = Pt(9.5)
            p.runs[0].font.name = 'Calibri'
            if idx == 0 or idx == 1:
                p.runs[0].font.bold = True

    print(f"Updated Table 3 with {len(master_categories)} Master Clinical Categories.")

    # Save document in-place
    doc.save(docx_path)
    print(f"Successfully modified and saved {docx_path} in-place!")

if __name__ == "__main__":
    update_docx()
