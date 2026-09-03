# 🦷 Comprehensive Clinical Dental Specialties Architecture & SQL Database Guide (Categories 8–14)

---

## 📑 Table of Contents
1. [Executive Summary & Scope](#1-executive-summary--scope)
2. [Clinical Taxonomy: Detailed Breakdown of Categories 8–14](#2-clinical-taxonomy-detailed-breakdown-of-categories-814)
3. [SQL Server Database Schema & Migration Scripts](#3-sql-server-database-schema--migration-scripts)
4. [C# ASP.NET Core Backend API Integration](#4-c-aspnet-core-backend-api-integration)
5. [Three.js 3D Rendering & Geometry Requirements](#5-threejs-3d-rendering--geometry-requirements)
6. [Doctor Workflow, UI/UX Layout & Zero-Regression Safety](#6-doctor-workflow-uiux-layout--zero-regression-safety)

---

## 1. Executive Summary & Scope

Modern dental practice management requires charting both **tooth-specific conditions** (e.g., caries, restorations, impactions) and **arch-level / craniofacial conditions** (e.g., TMJ disorders, malocclusions, nocturnal bruxism, and deciduous pediatric teeth).

This technical document provides the complete, production-ready specification to incorporate **Categories 8 through 14** into the existing **Dentia Dental EHR platform** without disrupting any existing permanent tooth charting, 3D jaw visualizers, or database relationships.

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                  DENTIA CLINICAL EXPANSION MATRIX                                │
├───────────────────────────────┬──────────────────────────────────┬───────────────────────────────┤
│ 👶 Category 8: Pediatric      │ 📐 Category 9: Occlusion / Bite │ 💆 Category 10: TMJ Disorders │
│ • Primary Teeth A–T (20-Tooth)│ • Class I, II, III Malocclusion  │ • Joint Clicking & Crepitus   │
│ • Pulpotomy (Formocresol/MTA) │ • Deep Overbite & Open Bite      │ • Subluxation & Trismus       │
│ • Stainless Steel Crown (SSC) │ • Crossbite (Ant. & Post.)       │ • Muscle Palpation Pain (MPDS)│
├───────────────────────────────┼──────────────────────────────────┼───────────────────────────────┤
│ ⚡ Category 11: Bruxism       │ 🦷 Category 12: Impactions       │ ❄️ Category 13: Sensitivity   │
│ • Severe Nocturnal Grinding   │ • Horizontal & Angular Wisdom    │ • Non-Carious Cervical Lesion │
│ • Attrition & Abfraction      │ • Supernumerary (Mesiodens)      │ • Exposed Dentinal Tubules    │
│ • Nightguard Occlusal Splint  │ • Pell & Gregory Class I–III     │ • Desensitizer GLUMA Protocol │
├───────────────────────────────┴──────────────────────────────────┴───────────────────────────────┤
│ 🩻 Category 14: Radiographic-Only & Subclinical Findings                                          │
│ • Furcation Involvement (Class I–IV) • Vertical & Horizontal Bone Loss • Cysts & Condensing Osteitis │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Clinical Taxonomy: Detailed Breakdown of Categories 8–14

### Category 8: Pediatric Dentistry (Primary / Deciduous Dentition A–T)
* **Standard Pediatric Notation:** Universal Primary System (Letters **A through T**, 20 teeth total):
  * **Maxillary Primary Arch (A–J):** A (2nd Molar Right) to J (2nd Molar Left).
  * **Mandibular Primary Arch (K–T):** K (2nd Molar Left) to T (2nd Molar Right).
* **Key Pediatric Procedures:**
  * **Pulpotomy (PULP):** Coronal pulp removal with MTA/Formocresol dressing.
  * **Stainless Steel Crown (SSC):** Full coronal coverage metallic preformed cap.
  * **Space Maintainers:** Band & Loop, Lingual Holding Arch, Distal Shoe for premature primary molar loss.
  * **Strip Crowns:** Composite resin pediatric anterior esthetic crowns.

### Category 9: Occlusion & Bite Discrepancies
* **Arch-Level Diagnoses:**
  * **Angle's Classifications:** Class I (Normal/Crowding), Class II Div 1/Div 2 (Retrognathic), Class III (Prognathic/Underbite).
  * **Overbite (Vertical Overlap):** Normal (2–3mm), Deep Bite (>50% coverage), Impinging Palatal Bite.
  * **Overjet (Horizontal Spread):** Normal (2mm), Excessive Overjet (>5mm).
  * **Crossbite:** Anterior Crossbite (Reverse overjet) or Posterior Crossbite (Unilateral/Bilateral buccal lingual mismatch).
  * **Open Bite:** Anterior Open Bite (Thumb sucking / tongue thrust habit), Posterior Open Bite.

### Category 10: TMJ / Temporomandibular Joint & Craniofacial Disorders
* **Craniofacial Diagnostics:**
  * **Joint Sounds:** Reciprocal Clicking, Crepitus (Bone grinding / osteoarthritis), Pop on opening/closing.
  * **Mandibular Dynamics:** Deviation to left/right, Deflection, Maximum Interincisal Opening (MIO < 35mm = Trismus).
  * **Myofascial Pain Dysfunction (MPDS):** Masseter, Temporalis, Lateral Pterygoid muscle palpation tenderness.
  * **Joint Pathology:** Disc Displacement with Reduction (DDwR), Disc Displacement without Reduction (DDwoR), Subluxation.

### Category 11: Bruxism & Parafunctional Habits
* **Pathological Signs:**
  * **Severe Attrition:** Wear facets flattening cusps and incisal edges across full dental arches.
  * **Abfraction:** V-shaped micro-fractures at the cervical enamel margin caused by biomechanical flexure.
  * **Musculoskeletal:** Masseter muscle hypertrophy, scalloped tongue borders (linea alba).
  * **Prescriptions:** Hard Acrylic Occlusal Guard, Dual-Laminate Nightguard, Botox masseter trigger-point injections.

### Category 12: Impacted & Supernumerary Teeth
* **Wisdom Tooth Classification (Third Molars #1, #16, #17, #32):**
  * **Winter’s Angulation:** Mesioangular, Horizontal, Vertical, Distoangular, Inverted, Transverse.
  * **Pell & Gregory Level:** Class I, II, III (Ramus relationship) & Position A, B, C (Occlusal depth in bone).
* **Supernumerary Teeth:**
  * **Mesiodens:** Conical extra tooth between central incisors (#8 & #9).
  * **Paramolar / Distomolar:** Extra molar in the buccal or distal tuberosity region.

### Category 13: Dentin Hypersensitivity (Non-Carious Cervical Lesions)
* **Clinical Presentation:**
  * Acute, sharp, short pain triggered by cold air, acidic beverages, cold water, or tactile probing without macroscopic cavitated caries.
* **Underlying Etiology:** Exposed dentinal tubules due to gingival recession, toothbrush abrasion, or erosion.
* **Treatments:** Fluoride varnish 5% NaF, GLUMA Desensitizer, Calcium sodium phosphosilicate (NovaMin), Glass Ionomer cervical sealing.

### Category 14: Radiographic-Only & Subclinical Findings
* **Periodontal Bone Morphology:**
  * Horizontal Bone Loss (Mild <15%, Moderate 15–33%, Severe >33%).
  * Vertical / Angular Infrabony Defects (1-wall, 2-wall, 3-wall defects).
  * Furcation Involvement (Glickman Class I incipient, Class II cul-de-sac, Class III through-and-through, Class IV clinically visible).
* **Periapical & Bone Pathologies:**
  * Periapical Radiolucency (Granuloma / Radicular Cyst).
  * Condensing Osteitis (Focal periapical sclerosis around chronically inflamed vital pulps).
  * Idiopathic Osteosclerosis (Dense bone island, non-pathologic).

---

## 3. SQL Server Database Schema & Migration Scripts

The database schema is upgraded using **backwards-compatible additive migrations**. Existing `[dentist].[TeethState]` records are preserved, and clean dedicated tables are created for pediatric, occlusion, TMJ, and radiographic findings.

```sql
USE [DentistDB];
GO

-- ==============================================================================
-- 1. EXTEND TEETH STATE TABLE FOR PEDIATRIC & HYPERSENSITIVITY SUPPORT
-- ==============================================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[TeethState]') AND name = 'DentitionType')
BEGIN
    ALTER TABLE [dentist].[TeethState]
    ADD [DentitionType] VARCHAR(20) NOT NULL DEFAULT 'Permanent', -- 'Permanent' (1-32) or 'Pediatric' (A-T)
        [PrimaryLetter] CHAR(1) NULL,                             -- 'A' to 'T' for Pediatric teeth
        [SensitivityLevel] VARCHAR(20) NULL,                      -- 'None', 'Mild', 'Moderate', 'Severe'
        [ImpactionClassification] VARCHAR(50) NULL,               -- 'Mesioangular Class II Position B', etc.
        [SSCPlaced] BIT NOT NULL DEFAULT 0,                       -- Stainless Steel Crown placed
        [PulpotomyDate] DATETIME2 NULL;
END
GO

-- ==============================================================================
-- 2. CREATE PATIENT OCCLUSION, TMJ & BRUXISM TABLE (ARCH-LEVEL DIAGNOSES)
-- ==============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientOcclusionTMJ' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[PatientOcclusionTMJ] (
        [OcclusionID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        -- Category 9: Occlusion / Bite
        [AngleClassification] VARCHAR(30) NOT NULL DEFAULT 'Class I Normal', -- 'Class I', 'Class II Div 1', 'Class II Div 2', 'Class III'
        [OverbiteType] VARCHAR(30) NOT NULL DEFAULT 'Normal (2-3mm)',       -- 'Normal', 'Deep Bite (>50%)', 'Open Bite', 'Edge-to-Edge'
        [OverjetMm] DECIMAL(4,1) NOT NULL DEFAULT 2.0,
        [CrossbiteLocation] VARCHAR(100) NULL,                              -- 'None', 'Anterior #7-#10', 'Right Posterior #2-#4'
        -- Category 10: TMJ / Jaw Joint
        [RightTMJSound] VARCHAR(40) NOT NULL DEFAULT 'None',                -- 'None', 'Clicking', 'Crepitus', 'Popping'
        [LeftTMJSound] VARCHAR(40) NOT NULL DEFAULT 'None',                 -- 'None', 'Clicking', 'Crepitus', 'Popping'
        [MaximumOpeningMm] INT NOT NULL DEFAULT 45,                         -- <35mm indicates Trismus
        [MandibularDeviation] VARCHAR(30) NOT NULL DEFAULT 'None',          -- 'None', 'Deviation Right', 'Deviation Left', 'Deflection'
        [MuscleTenderness] VARCHAR(150) NULL,                               -- 'Bilateral Masseter & Right Temporalis'
        -- Category 11: Bruxism
        [BruxismSeverity] VARCHAR(30) NOT NULL DEFAULT 'None',              -- 'None', 'Mild Attrition', 'Moderate Grinding', 'Severe Nocturnal'
        [NightguardPrescribed] BIT NOT NULL DEFAULT 0,
        [NightguardType] VARCHAR(50) NULL,                                  -- 'Hard Acrylic Occlusal Splint', 'Dual Laminate'
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_OcclusionTMJ_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_PatientOcclusionTMJ_Patient] ON [dentist].[PatientOcclusionTMJ] ([PatientID]);
END
GO

-- ==============================================================================
-- 3. CREATE TOOTH RADIOGRAPHIC & SUBCLINICAL FINDINGS TABLE (CATEGORY 14)
-- ==============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ToothRadiographicFindings' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ToothRadiographicFindings] (
        [FindingID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [ToothNumber] INT NOT NULL,                                         -- 1-32 or 101-120 for Pediatric
        [BoneLossType] VARCHAR(30) NOT NULL DEFAULT 'None',                 -- 'None', 'Horizontal Mild (<15%)', 'Horizontal Moderate (15-33%)', 'Horizontal Severe (>33%)', 'Vertical Infrabony'
        [FurcationGrade] VARCHAR(20) NOT NULL DEFAULT 'Grade 0 (None)',     -- 'Grade 0', 'Class I (Incipient)', 'Class II (Cul-de-sac)', 'Class III (Through-and-through)', 'Class IV'
        [PeriapicalCondition] VARCHAR(50) NOT NULL DEFAULT 'Normal PDL',    -- 'Normal PDL', 'Widened PDL Space', 'Periapical Radiolucency (Granuloma/Cyst)', 'Condensing Osteitis'
        [CalculusSubgingival] BIT NOT NULL DEFAULT 0,
        [RootResorption] VARCHAR(30) NOT NULL DEFAULT 'None',               -- 'None', 'Internal Resorption', 'External Cervical', 'Apical Blunting'
        [RadiographDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Notes] NVARCHAR(500) NULL,
        CONSTRAINT [FK_Radiographic_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Radiographic_Patient_Tooth] ON [dentist].[ToothRadiographicFindings] ([PatientID], [ToothNumber]);
END
GO
```

---

## 4. C# ASP.NET Core Backend API Integration

### DTOs (`DentistAPI/DTOs/ClinicalExpansionDTOs.cs`)
```csharp
namespace DentistAPI.DTOs
{
    public class OcclusionTMJDto
    {
        public int PatientId { get; set; }
        public string AngleClassification { get; set; } = "Class I Normal";
        public string OverbiteType { get; set; } = "Normal (2-3mm)";
        public decimal OverjetMm { get; set; } = 2.0m;
        public string? CrossbiteLocation { get; set; }
        public string RightTMJSound { get; set; } = "None";
        public string LeftTMJSound { get; set; } = "None";
        public int MaximumOpeningMm { get; set; } = 45;
        public string MandibularDeviation { get; set; } = "None";
        public string? MuscleTenderness { get; set; }
        public string BruxismSeverity { get; set; } = "None";
        public bool NightguardPrescribed { get; set; } = false;
        public string? NightguardType { get; set; }
    }

    public class RadiographicFindingDto
    {
        public int PatientId { get; set; }
        public int ToothNumber { get; set; }
        public string BoneLossType { get; set; } = "None";
        public string FurcationGrade { get; set; } = "Grade 0 (None)";
        public string PeriapicalCondition { get; set; } = "Normal PDL";
        public bool CalculusSubgingival { get; set; }
        public string RootResorption { get; set; } = "None";
        public string? Notes { get; set; }
    }
}
```

### Controller Endpoints (`DentistAPI/Controllers/PatientsController.cs`)
```csharp
[HttpGet("{id}/occlusion-tmj")]
public async Task<IActionResult> GetOcclusionTMJ(int id)
{
    using var conn = _db.CreateConnection();
    const string sql = "SELECT * FROM [dentist].[PatientOcclusionTMJ] WHERE PatientID = @id";
    var result = await conn.QuerySingleOrDefaultAsync(sql, new { id });
    return Ok(result ?? new { PatientID = id, AngleClassification = "Class I Normal", RightTMJSound = "None", LeftTMJSound = "None", BruxismSeverity = "None" });
}

[HttpPost("occlusion-tmj/update")]
public async Task<IActionResult> UpdateOcclusionTMJ([FromBody] OcclusionTMJDto dto)
{
    using var conn = _db.CreateConnection();
    const string sql = @"
        IF EXISTS (SELECT 1 FROM [dentist].[PatientOcclusionTMJ] WHERE PatientID = @PatientId)
        BEGIN
            UPDATE [dentist].[PatientOcclusionTMJ]
            SET AngleClassification = @AngleClassification, OverbiteType = @OverbiteType, OverjetMm = @OverjetMm,
                CrossbiteLocation = @CrossbiteLocation, RightTMJSound = @RightTMJSound, LeftTMJSound = @LeftTMJSound,
                MaximumOpeningMm = @MaximumOpeningMm, MandibularDeviation = @MandibularDeviation, MuscleTenderness = @MuscleTenderness,
                BruxismSeverity = @BruxismSeverity, NightguardPrescribed = @NightguardPrescribed, NightguardType = @NightguardType,
                UpdatedAt = SYSUTCDATETIME()
            WHERE PatientID = @PatientId;
        END
        ELSE
        BEGIN
            INSERT INTO [dentist].[PatientOcclusionTMJ] 
            (PatientID, AngleClassification, OverbiteType, OverjetMm, CrossbiteLocation, RightTMJSound, LeftTMJSound, MaximumOpeningMm, MandibularDeviation, MuscleTenderness, BruxismSeverity, NightguardPrescribed, NightguardType)
            VALUES (@PatientId, @AngleClassification, @OverbiteType, @OverjetMm, @CrossbiteLocation, @RightTMJSound, @LeftTMJSound, @MaximumOpeningMm, @MandibularDeviation, @MuscleTenderness, @BruxismSeverity, @NightguardPrescribed, @NightguardType);
        END";
    await conn.ExecuteAsync(sql, dto);
    return Ok(new { success = true, message = "Occlusion, TMJ & Bruxism findings saved successfully." });
}
```

---

## 5. Three.js 3D Rendering & Geometry Requirements

To realistically visualize Categories 8–14 in the **3D Interactive Dental Jaw Arch (`ThreeDentalJawArch.jsx`)** and **Single Tooth Dossier (`ToothDetailPage.jsx`)**, the following Three.js procedural shaders and overlays are implemented:

```
                                  THREE.JS 3D CLINICAL OVERLAY PIPELINE
                                  
  ┌─────────────────────────┐      ┌─────────────────────────┐      ┌─────────────────────────┐
  │   1. PEDIATRIC MESH     │      │   2. IMPACTION PITCH    │      │   3. BRUXISM WEAR       │
  │ Deciduous A–T 20-tooth  │      │ Rotated 90° horizontally│      │ Flattened crown height  │
  │ geometry with Stainless │ ───> │ inside alveolar bone    │ ───> │ with dark amber dentin  │
  │ Steel Crown metal mat   │      │ with surgical guide box │      │ exposure facets         │
  └─────────────────────────┘      └─────────────────────────┘      └─────────────────────────┘
               │                                │                                │
               ▼                                ▼                                ▼
  ┌─────────────────────────┐      ┌─────────────────────────┐      ┌─────────────────────────┐
  │   4. RADIOGRAPHIC HALO  │      │   5. SENSITIVITY GLOW   │      │   6. BILATERAL TMJ DOTS │
  │ Translucent amber/purple│      │ Cyan / icy blue warning │      │ Left & Right condylar   │
  │ sphere at root apex for │      │ ring around cervical    │      │ joint spheres with click│
  │ cysts & bone resorption │      │ margin without cavity   │      │ pulse animation         │
  └─────────────────────────┘      └─────────────────────────┘      └─────────────────────────┘
```

### Procedural 3D Canvas Functions:
1. **Pediatric Stainless Steel Crown (SSC):**
   * High-specular chrome metallic reflection with preformed cusp bevels on primary molars (A, B, I, J, K, L, S, T).
2. **Impacted Tooth Spatial Tilting:**
   * Uses `mesh.rotation.z` and `mesh.rotation.x` to physically tilt impacted third molars (e.g. 90° for Horizontal Impaction, 45° for Mesioangular).
3. **Severe Bruxism Incisal Facet:**
   * Shaves 15% off the incisal edge and renders a central amber-brown dentin island surrounded by sharp enamel rims.
4. **Radiographic Apical Lesion Sphere:**
   * Emits a pulsing semi-transparent radial glow (`#EF4444` for active abscess, `#4C1D95` for condensing osteitis) at the root apex.
5. **Dentin Hypersensitivity Cervical Crest:**
   * Paints a thin icy-blue demineralization crest (`#38BDF8`) along the buccal cervical line with no cavitated black core.

---

## 6. Doctor Workflow, UI/UX Layout & Zero-Regression Safety

### Doctor Workflow in UI:
1. **Adult / Pediatric Arch Switcher:**
   * In `ChartPage.jsx`, a sleek toggle allows switching between **Permanent (1–32)** and **Pediatric (A–T)** odontogram views in one click.
2. **Specialized Diagnostics Drawer:**
   * A dedicated **"Craniofacial, Occlusion & TMJ"** panel in `ChartPage.jsx` allows doctors to quickly check off TMJ clicking, bruxism wear, and deep bite without cluttering individual tooth boxes.
3. **Tooth Detail Page Dossier (`ToothDetailPage.jsx`):**
   * Displays full endodontic canal anatomy, nerve supply, eruption age, and antagonistic occlusion.
   * Includes one-click access to **Radiographic Findings** and **Hypersensitivity Varnish protocols**.

### Zero-Regression Safety Assurances:
* ✅ All existing 32 permanent teeth database entries and saved patient chart states remain **100% unaffected**.
* ✅ The 5-Surface Cross-Section diagram (`O, M, D, B, L`) continues to work with live 2-way database synchronization.
* ✅ Voice dictation commands (`"Tooth 14 MOD caries"`, `"Tooth 30 occlusal amalgam"`) continue functioning seamlessly.
* ✅ The UI retains responsive zero-scroll layouts and clean typography across all viewport sizes.

---
*Created and Verified for Dentia Dental EHR Suite.*
