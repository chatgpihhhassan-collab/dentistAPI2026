# 🦷 Teens & Young Adults (12–25 Yrs): Orthodontic, Impaction & TMJ Diagnostic Specification

This document provides complete documentation and SQL Server database schema scripts for the 12 specialized diagnostic visualizers for Teens and Young Adults.

---

## 1. 12 Specialized Clinical Visualizers Matrix

| Category | Diagram / Visualizer | View | Clinical Significance & Key Anatomy | CDT Code |
|---|---|---|---|---|
| **Occlusion** | 1. Overbite (Deep Bite) | Side Profile (Sagittal) | Maxillary incisors overlap mandibular incisors by >50%. Palatal gingival impingement. | D8080 |
| **Occlusion** | 2. Underbite (Class III) | Side Profile (Sagittal) | Mandibular prognathism, negative overjet, anterior crossbite. | D8080 |
| **Occlusion** | 3. Crossbite | Frontal Arch (Coronal) | Maxillary teeth occluding inside mandibular arch (posterior/anterior). | D8080 |
| **Occlusion** | 4. Anterior Open Bite | Front / Sagittal | Vertical gap between incisal edges with full molar contact. | D8080 |
| **Occlusion** | 5. Uneven Molar Wear | Top-Down Occlusal | Occlusal attrition facets, yellow dentin exposure from bruxism / malocclusion. | D9944 |
| **Impactions** | 6. Mesioangular Wisdom Molar | Alveolar Bone Cross-Section | 3rd Molar tilted 45° mesially into 2nd molar cervical root. | D7230 |
| **Impactions** | 7. Horizontally Impacted Molar | Radiographic X-Ray Style | 3rd Molar completely horizontal (90°) locked within mandibular ramus. | D7240 |
| **Impactions** | 8. Palatally Impacted Canine | Maxillary Bone Cross-Section | Unerupted maxillary canine trapped within palatal cortical bone. | D7280 |
| **Impactions** | 9. Partially Erupted Premolar | Gingival Soft-Tissue | Incomplete eruption through alveolar crest with inflamed pericoronal flap. | D7220 |
| **TMJ** | 10. Normal TMJ Articulation | Sagittal Joint Cross-Section | Condylar head correctly seated in glenoid fossa with biconcave articular disc. | D0140 |
| **TMJ** | 11. TMJ Joint Clicking | Sagittal Joint + Sound Waves | Anterior disc displacement with reduction upon opening (reciprocal click). | D7880 |
| **TMJ** | 12. TMJ Closed Lock | Functional Sagittal Joint | Non-reducing anterior disc displacement limiting interincisal opening (<30mm). | D7880 |

---

## 2. SQL Server Database Migration Script (`dentist.*`)

```sql
USE [dentist_db];
GO

-- 1. Orthodontic & Bite Malocclusion Assessment Table
CREATE TABLE [dentist].[PatientOrthodonticOcclusion] (
    [OcclusionId] INT IDENTITY(1,1) PRIMARY KEY,
    [PatientID] INT NOT NULL FOREIGN KEY REFERENCES [dentist].[Patients]([PatientID]),
    [DoctorID] INT NULL FOREIGN KEY REFERENCES [dentist].[Doctors]([DoctorID]),
    [MolarClassification] VARCHAR(50) DEFAULT 'Class I Normal',
    [OverbiteType] VARCHAR(50) DEFAULT 'Normal (1-2mm)',
    [OverbitePercentage] INT DEFAULT 20,
    [OverjetType] VARCHAR(50) DEFAULT 'Normal (2mm)',
    [OverjetMm] DECIMAL(4,1) DEFAULT 2.0,
    [CrossbiteLocation] VARCHAR(100) DEFAULT 'None',
    [OpenBiteMm] DECIMAL(4,1) DEFAULT 0.0,
    [WearFacetSeverity] VARCHAR(50) DEFAULT 'None',
    [ActiveAppliance] VARCHAR(100) DEFAULT 'None',
    [Notes] NVARCHAR(MAX) NULL,
    [EvaluatedAt] DATETIME2 DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2 DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_PatientOrthodonticOcclusion_PatientID ON [dentist].[PatientOrthodonticOcclusion]([PatientID]);
GO

-- 2. Tooth Impaction Radiographic Details Table
CREATE TABLE [dentist].[ToothImpactionDetails] (
    [ImpactionId] INT IDENTITY(1,1) PRIMARY KEY,
    [PatientID] INT NOT NULL FOREIGN KEY REFERENCES [dentist].[Patients]([PatientID]),
    [ToothNumber] VARCHAR(10) NOT NULL,
    [ImpactionType] VARCHAR(100) NOT NULL,
    [PellGregoryClass] VARCHAR(50) DEFAULT 'Class II Position B',
    [AngulationDegrees] INT DEFAULT 45,
    [BoneCoverage] VARCHAR(50) DEFAULT 'Partial Bony',
    [ProximityToIAN] BIT DEFAULT 0,
    [PericoronitisPresent] BIT DEFAULT 0,
    [RecommendedSurgicalAction] VARCHAR(100) DEFAULT 'Surgical Extraction (Odontectomy)',
    [EvaluatedAt] DATETIME2 DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_ToothImpactionDetails_PatientID ON [dentist].[ToothImpactionDetails]([PatientID]);
GO

-- 3. TMJ Joint Articulation & Clicking Assessment Table
CREATE TABLE [dentist].[PatientTMJAssessments] (
    [TMJAssessmentId] INT IDENTITY(1,1) PRIMARY KEY,
    [PatientID] INT NOT NULL FOREIGN KEY REFERENCES [dentist].[Patients]([PatientID]),
    [DoctorID] INT NULL FOREIGN KEY REFERENCES [dentist].[Doctors]([DoctorID]),
    [RightJointStatus] VARCHAR(100) DEFAULT 'Normal Physiological Seating',
    [LeftJointStatus] VARCHAR(100) DEFAULT 'Normal Physiological Seating',
    [ClickingOccursOn] VARCHAR(50) DEFAULT 'None',
    [MaxInterincisalOpeningMm] DECIMAL(4,1) DEFAULT 44.0,
    [MandibularDeviation] VARCHAR(50) DEFAULT 'None',
    [PalpationTenderness] NVARCHAR(200) DEFAULT 'None',
    [ParafunctionalHabits] VARCHAR(100) DEFAULT 'None',
    [ClinicalDiagnosis] NVARCHAR(500) NULL,
    [RecommendedTherapy] NVARCHAR(500) DEFAULT 'Custom Night Guard & Occlusal Splint',
    [EvaluatedAt] DATETIME2 DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_PatientTMJAssessments_PatientID ON [dentist].[PatientTMJAssessments]([PatientID]);
GO
```
