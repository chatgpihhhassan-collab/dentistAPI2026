import os
import matplotlib.pyplot as plt
import matplotlib.patches as patches

def generate_diagrams(output_dir):
    os.makedirs(output_dir, exist_ok=True)
    
    # -------------------------------------------------------------------------
    # DIAGRAM 1: PPI SYSTEM DATA FLOW DIAGRAM
    # -------------------------------------------------------------------------
    fig, ax = plt.subplots(figsize=(16, 9), dpi=300)
    ax.set_facecolor('#F8FAFC')
    fig.patch.set_facecolor('#F8FAFC')
    ax.set_xlim(0, 16)
    ax.set_ylim(0, 9)
    ax.axis('off')

    # Title
    ax.text(8, 8.4, "PATIENT-PROVIDER INTERFACE (PPI) SYSTEM DATA FLOW DIAGRAM", 
            ha='center', va='center', fontsize=18, fontweight='bold', color='#0F172A', fontfamily='sans-serif')
    ax.text(8, 8.0, "Real-Time Operatory Consultation • Ambient Voice Scribing • 3D Odontogram Sync • Dual Geocoding", 
            ha='center', va='center', fontsize=11, color='#4A7CD2', fontfamily='sans-serif', fontweight='bold')

    # Nodes definition: (x, y, w, h, title, subtitle, color, text_color)
    nodes = [
        (0.8, 4.8, 3.0, 2.3, "1. INTAKE & REGISTRATION", 
         "• Patient Demographics\n• Dual Geocoding: OSM + Mapbox\n• Auto City & Postcode Sync\n• Dentition Arch Classification:\n  Adult (1-32) | Child (A-T)", "#2563EB", "#FFFFFF"),
        
        (4.4, 4.8, 3.2, 2.3, "2. AMBIENT VOICE CONSULT", 
         "• Operatory Mic Audio Capture\n• Web Speech API Recognition\n• Ingest to .NET 10 API\n• SHA-256 Audio Checksum\n• Raw & Cleaned Transcripts", "#0E8A80", "#FFFFFF"),
        
        (8.2, 4.8, 3.4, 2.3, "3. MULTI-TIER AI SCRIBE", 
         "• Google Vertex AI (Gemini 1.5)\n• Groq LPU (Llama-3-70b sub-sec)\n• Local LLM Offline Failover\n• 8-Section SOAP Synthesis\n• FDI / Universal Tooth Mapping", "#7C3AED", "#FFFFFF"),
        
        (12.2, 4.8, 3.0, 2.3, "4. 3D ODONTOGRAM SYNC", 
         "• Three.js + React Three Fiber\n• Dynamic Condition Shaders:\n  Caries (Red), Filling (Blue)\n  Crown (Gold), Extraction\n• 360° OrbitControls Raycast", "#059669", "#FFFFFF"),

        # Bottom Row
        (1.8, 1.2, 3.8, 2.3, "5. PATIENT PROTECTED INFO (PPI)", 
         "• TLS 1.3 Transport Security\n• AES-256 SQL Server TDE\n• SHA-256 Non-Repudiation Logs\n• PII Masking & De-identification\n• BCrypt Password Work Factor 11", "#1E293B", "#FFFFFF"),

        (6.2, 1.2, 4.0, 2.3, "6. CLINICAL SAFETY & WARNINGS", 
         "• Automated Contraindication Engine\n• Allergy vs Prescription Check\n• Critical, Warning & Info Logs\n• Mandatory Doctor Resolution\n• [dentist].[AIWarnings] Table", "#DC2626", "#FFFFFF"),

        (10.8, 1.2, 3.8, 2.3, "7. CONFIRMATION & EXPORT", 
         "• Web Speech Synthesis Voice:\n  'Doctor, chart synced successfully'\n• jsPDF / html2canvas Clinical PDF\n• Dapper SQL Server Persistence\n• Real-Time Appointment Booking", "#D97706", "#FFFFFF"),
    ]

    for (x, y, w, h, title, body, bg_c, txt_c) in nodes:
        # Box shadow
        rect_s = patches.FancyBboxPatch((x+0.05, y-0.05), w, h, boxstyle="round,pad=0.1,rounding_size=0.2",
                                        facecolor='#CBD5E1', edgecolor='none', alpha=0.5)
        ax.add_patch(rect_s)
        # Box
        rect = patches.FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.1,rounding_size=0.2",
                                      facecolor=bg_c, edgecolor='#FFFFFF', linewidth=2)
        ax.add_patch(rect)
        
        # Title
        ax.text(x + w/2, y + h - 0.35, title, ha='center', va='center', fontsize=11, fontweight='bold', color=txt_c)
        # Separator line
        ax.plot([x + 0.2, x + w - 0.2], [y + h - 0.65, y + h - 0.65], color='#FFFFFF', alpha=0.3, linewidth=1.5)
        # Body
        ax.text(x + 0.25, y + (h - 0.65)/2 - 0.1, body, ha='left', va='center', fontsize=9, color='#F1F5F9', linespacing=1.4)

    # Connecting Arrows across top row
    arrow_style = dict(arrowstyle="->", color="#4A7CD2", lw=3.5, mutation_scale=20)
    ax.annotate("", xy=(4.3, 5.95), xytext=(3.9, 5.95), arrowprops=arrow_style)
    ax.annotate("", xy=(8.1, 5.95), xytext=(7.7, 5.95), arrowprops=arrow_style)
    ax.annotate("", xy=(12.1, 5.95), xytext=(11.7, 5.95), arrowprops=arrow_style)

    # Downward / Loop Arrows
    arrow_down = dict(arrowstyle="->", color="#7C3AED", lw=3.0, mutation_scale=20)
    ax.annotate("", xy=(12.7, 3.6), xytext=(13.7, 4.7), arrowprops=arrow_down)
    ax.annotate("", xy=(8.2, 3.6), xytext=(9.9, 4.7), arrowprops=arrow_down)
    ax.annotate("", xy=(3.7, 3.6), xytext=(2.3, 4.7), arrowprops=arrow_down)

    ppi_flow_path = os.path.join(output_dir, "ppi_system_flow_diagram.png")
    plt.tight_layout()
    plt.savefig(ppi_flow_path, dpi=300, facecolor=fig.get_facecolor(), bbox_inches='tight')
    plt.close()
    print("Generated:", ppi_flow_path)

    # -------------------------------------------------------------------------
    # DIAGRAM 2: 17-TABLE ENTITY MODEL (ERD) DIAGRAM
    # -------------------------------------------------------------------------
    fig2, ax2 = plt.subplots(figsize=(16, 9), dpi=300)
    ax2.set_facecolor('#0F172A')
    fig2.patch.set_facecolor('#0F172A')
    ax2.set_xlim(0, 16)
    ax2.set_ylim(0, 9)
    ax2.axis('off')

    # Title
    ax2.text(8, 8.45, "DENTIA DATABASE ENTITY-RELATIONSHIP MODEL (17 TABLES)", 
             ha='center', va='center', fontsize=18, fontweight='bold', color='#FFFFFF', fontfamily='sans-serif')
    ax2.text(8, 8.1, "Microsoft SQL Server 2022 • dentist.* Schema • Dapper Micro-ORM High Performance Mapping", 
             ha='center', va='center', fontsize=11, color='#38BDF8', fontfamily='sans-serif', fontweight='bold')

    tables = [
        # Center: Patients
        (6.3, 4.5, 3.4, 3.1, "[dentist].[Patients] (Master)", 
         "PK: PatientID (INT)\n• FirstName, LastName\n• DOB, Gender, Phone\n• Address, City, Postcode\n• HealthcareRegion ('PK'/'NZ')\n• DentitionType (Adult/Ped)\n• GuardianName, Relationship\n• ProfileImageDataUrl", "#1E3A8A"),
        
        # Left Top: Doctors
        (0.6, 5.8, 2.5, 2.2, "[dentist].[Doctors]", 
         "PK: DoctorID (INT)\n• FullName, Email (UQ)\n• PasswordHash (BCrypt)\n• Specialization, Role\n• Region ('PK'/'NZ')", "#065F46"),

        # Left Mid: Appointments
        (0.6, 3.1, 2.5, 2.4, "[dentist].[Appointments]", 
         "PK: AppointmentID (INT)\nFK: DoctorID -> Doctors\n• FullName, Phone, Email\n• PreferredDate (DATETIME)\n• Status ('Pending'/'Confirmed')\n• Reason (Procedure)", "#065F46"),

        # Left Bot: ChatHistory & ClinicalLogs
        (0.6, 0.5, 2.5, 2.3, "[dentist].[ClinicalLogs]", 
         "PK: LogID (INT)\nFK: PatientID, DoctorID\n• Message (NVARCHAR MAX)\n• LogType ('Treatment'/'Scribe')\n• CreatedAt (DATETIME)", "#334155"),

        (3.4, 0.5, 2.5, 2.3, "[dentist].[ChatHistory]", 
         "PK: ChatID (INT)\nFK: PatientID -> Patients\n• Transcript (Query)\n• ParsedAction (AI Action)\n• Timestamp (DATETIME)", "#334155"),

        # Right Top: 3D Odontogram cluster
        (10.2, 5.8, 2.6, 2.2, "[dentist].[TeethState]", 
         "PK: TeethStateID (INT)\nFK: PatientID -> Patients\n• ToothNumber (1-32 / 11-48)\n• Condition, Surface\n• ColorHex (#EF4444)\n• Mobility, Furcation", "#0E7490"),

        (13.1, 5.8, 2.4, 2.2, "[dentist].[TreatmentHistory]", 
         "PK: HistoryID (INT)\nFK: PatientID -> Patients\n• ToothNumber (INT)\n• ProcedureName, Cost\n• PerformedBy, DateTreated\n• ClinicalNotes", "#0E7490"),

        (10.2, 3.4, 2.6, 2.1, "[dentist].[Radiographs]", 
         "PK: RadiographID (INT)\nFK: PatientID -> Patients\n• ToothNumber (Nullable)\n• ImageType (Bitewing/CBCT)\n• ImageUri, AIAnalysisSummary", "#0E7490"),

        # Voice & Scribe Cluster (Bottom Right)
        (6.3, 1.8, 3.4, 2.3, "[dentist].[DentalNoteSessions]", 
         "PK: SessionId (BIGINT)\nFK: PatientId, DoctorId\n• Status ('Active'/'Approved')\n• StartedAt, CompletedAt", "#581C87"),

        (6.3, 0.2, 1.6, 1.4, "[AudioRecordings]", 
         "PK: AudioId\nFK: SessionId\n• StorageUri\n• Sha256Hash", "#4C1D95"),

        (8.1, 0.2, 1.6, 1.4, "[Transcripts]", 
         "PK: TranscriptId\nFK: SessionId\n• RawTranscript\n• CleanedTranscript", "#4C1D95"),

        (10.2, 0.5, 2.6, 2.6, "[dentist].[DentalNotes]", 
         "PK: NoteId (BIGINT)\nFK: SessionId, PatientId\n• ChiefComplaint, HPI\n• DentalHistory, Objective\n• Assessment, Treatment\n• Prescriptions, Prognosis", "#581C87"),

        (13.1, 3.2, 2.4, 2.3, "[AI_Audit_&_Warnings]", 
         "• AIAuditLogs:\n  PK: AuditId, FK: NoteId\n  InputHash, OutputHash\n• AIWarnings:\n  PK: WarningId, FK: NoteId\n  Severity, AllergyAlert", "#991B1B"),

        (13.1, 0.5, 2.4, 2.4, "[Prescriptions_&_Plans]", 
         "• DentalNotePrescriptions:\n  PK: PrescriptionId, NoteId\n• DentalNoteTreatmentPlans:\n  PK: PlanId, NoteId\n• Prescriptions Master", "#15803D"),
    ]

    for (x, y, w, h, title, body, bg_c) in tables:
        rect = patches.FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.08,rounding_size=0.15",
                                      facecolor=bg_c, edgecolor='#38BDF8', linewidth=1.5)
        ax2.add_patch(rect)
        
        ax2.text(x + w/2, y + h - 0.25, title, ha='center', va='center', fontsize=9.5, fontweight='bold', color='#FFFFFF')
        ax2.plot([x + 0.1, x + w - 0.1], [y + h - 0.45, y + h - 0.45], color='#38BDF8', alpha=0.4, linewidth=1)
        ax2.text(x + 0.15, y + (h - 0.45)/2, body, ha='left', va='center', fontsize=7.5, color='#E2E8F0', linespacing=1.3)

    # Connection lines to Patients (Center node)
    line_kw = dict(color='#38BDF8', lw=2, linestyle='--', alpha=0.7)
    # Patients to Doctors
    ax2.plot([3.1, 6.3], [6.9, 6.0], **line_kw)
    # Patients to Appointments
    ax2.plot([3.1, 6.3], [4.3, 5.5], **line_kw)
    # Patients to TeethState
    ax2.plot([9.7, 10.2], [6.0, 6.9], **line_kw)
    # Patients to Radiographs
    ax2.plot([9.7, 10.2], [5.5, 4.45], **line_kw)
    # Patients to DentalNoteSessions
    ax2.plot([8.0, 8.0], [4.5, 4.1], **line_kw)
    # Patients to ClinicalLogs
    ax2.plot([3.1, 6.3], [1.65, 4.8], **line_kw)

    erd_path = os.path.join(output_dir, "erd_diagram.png")
    plt.tight_layout()
    plt.savefig(erd_path, dpi=300, facecolor=fig2.get_facecolor(), bbox_inches='tight')
    plt.close()
    print("Generated:", erd_path)

    return ppi_flow_path, erd_path

if __name__ == "__main__":
    generate_diagrams("f:\\DentistApp_Theme2\\scratch")
