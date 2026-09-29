-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM - CLINICAL DETAIL FIELDS EXPANSION MIGRATION
-- Database Engine: Microsoft SQL Server 2019 / 2022 / Azure SQL
-- Schema: dentist
-- Author: Antigravity Assistant & Dentia Clinical Architecture Team
-- Date: September 2026
-- Description:
--   This idempotent migration script provisions tables and fields for:
--   1. Implant Planning:
--      - Implant Length (numeric, mm)
--      - Implant Diameter (numeric, mm)
--      - Bone Quality (categorical: D1, D2, D3, D4 Lekholm & Zarb classification)
--      - Bone Quantity (height/width available, grafting required Y/N, sinus lift status)
--      - 3D Planning (CBCT reference link, digital planning notes, guided surgery flag)
--   2. Biopsy Records:
--      - Biopsy Type (single-select: Incisional / Excisional)
--      - Site of Biopsy (anatomical location - tooth #, quadrant, soft tissue region)
--      - Clinical impression, pathology lab, specimen ref, histopathology results
--   3. Orthodontics — Clear Aligners:
--      - Aligner system/brand (Invisalign, ClearCorrect, Spark, etc.)
--      - Number of aligner stages/trays (Total & Current stage)
--      - Attachments required (Y/N + notes)
--      - IPR (Interproximal Reduction) required (Y/N + details)
--      - Wear schedule & refinement scan tracking
-- =====================================================================================

USE [DentistAPI];
GO

-- 1. Ensure dentist schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> Schema [dentist] verified/created.';
END
GO

-- =====================================================================================
-- 2. CREATE [dentist].[ImplantPlans] TABLE
-- =====================================================================================
PRINT '>> Step 2: Creating [dentist].[ImplantPlans] Table...';

IF OBJECT_ID('dentist.ImplantPlans', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[ImplantPlans] (
        [ImplantPlanID]         INT IDENTITY(1,1) NOT NULL,
        [PatientID]             INT NOT NULL,
        [DoctorID]              INT NULL,
        [ToothNumber]           INT NOT NULL,
        [ToothKey]              NVARCHAR(10) NULL,
        [ImplantBrand]          NVARCHAR(100) NULL,
        [ImplantLength]         DECIMAL(4,1) NOT NULL, -- e.g. 6.0 to 18.0 mm
        [ImplantDiameter]       DECIMAL(4,1) NOT NULL, -- e.g. 2.5 to 7.0 mm
        [BoneQuality]           NVARCHAR(10) NOT NULL, -- 'D1', 'D2', 'D3', 'D4' (Lekholm & Zarb)
        [BoneHeightAvailable]   DECIMAL(4,1) NULL,     -- Height in mm
        [BoneWidthAvailable]    DECIMAL(4,1) NULL,     -- Width in mm
        [GraftingRequired]      BIT NOT NULL CONSTRAINT DF_ImplantPlans_Grafting DEFAULT (0),
        [SinusLiftStatus]       NVARCHAR(50) NOT NULL CONSTRAINT DF_ImplantPlans_SinusLift DEFAULT ('None'),
                                -- 'None', 'Required', 'Completed', 'Crestal_Planned', 'Lateral_Window_Planned'
        [CbctReferenceUrl]      NVARCHAR(1000) NULL,
        [DigitalPlanningNotes]  NVARCHAR(MAX) NULL,
        [GuidedSurgeryFlag]     BIT NOT NULL CONSTRAINT DF_ImplantPlans_GuidedSurgery DEFAULT (0),
        [PlanStatus]            NVARCHAR(30) NOT NULL CONSTRAINT DF_ImplantPlans_PlanStatus DEFAULT ('Planned'),
                                -- 'Planned', 'Surgically Placed', 'Restored', 'Completed', 'Cancelled'
        [PlannedDate]           DATETIME2 NULL,
        [PlacementDate]         DATETIME2 NULL,
        [CreatedAt]             DATETIME2 NOT NULL CONSTRAINT DF_ImplantPlans_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]             DATETIME2 NOT NULL CONSTRAINT DF_ImplantPlans_UpdatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_ImplantPlans] PRIMARY KEY CLUSTERED ([ImplantPlanID] ASC),
        CONSTRAINT [FK_ImplantPlans_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]),
        CONSTRAINT [CK_ImplantPlans_BoneQuality] CHECK ([BoneQuality] IN ('D1', 'D2', 'D3', 'D4')),
        CONSTRAINT [CK_ImplantPlans_Length] CHECK ([ImplantLength] >= 3.0 AND [ImplantLength] <= 25.0),
        CONSTRAINT [CK_ImplantPlans_Diameter] CHECK ([ImplantDiameter] >= 2.0 AND [ImplantDiameter] <= 10.0)
    );

    CREATE NONCLUSTERED INDEX [IX_ImplantPlans_PatientID] ON [dentist].[ImplantPlans]([PatientID]);
    CREATE NONCLUSTERED INDEX [IX_ImplantPlans_ToothNumber] ON [dentist].[ImplantPlans]([ToothNumber]);
    PRINT '   + Table [dentist].[ImplantPlans] created successfully.';
END
ELSE
BEGIN
    PRINT '   - Table [dentist].[ImplantPlans] already exists.';
END
GO

-- =====================================================================================
-- 3. CREATE [dentist].[BiopsyRecords] TABLE
-- =====================================================================================
PRINT '>> Step 3: Creating [dentist].[BiopsyRecords] Table...';

IF OBJECT_ID('dentist.BiopsyRecords', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[BiopsyRecords] (
        [BiopsyID]                  INT IDENTITY(1,1) NOT NULL,
        [PatientID]                 INT NOT NULL,
        [DoctorID]                  INT NULL,
        [BiopsyType]                NVARCHAR(20) NOT NULL, -- 'Incisional' or 'Excisional'
        [SiteOfBiopsy]              NVARCHAR(255) NOT NULL, -- Anatomical location: tooth #, quadrant, soft tissue
        [ToothNumber]               INT NULL,
        [ToothKey]                  NVARCHAR(10) NULL,
        [ClinicalImpression]        NVARCHAR(MAX) NULL,     -- Provisional diagnosis / lesion details
        [PathologyLabName]          NVARCHAR(200) NULL,
        [SpecimenReference]         NVARCHAR(100) NULL,
        [BiopsyDate]                DATE NOT NULL CONSTRAINT DF_BiopsyRecords_BiopsyDate DEFAULT (CAST(GETDATE() AS DATE)),
        [Status]                    NVARCHAR(50) NOT NULL CONSTRAINT DF_BiopsyRecords_Status DEFAULT ('Specimen Sent'),
                                    -- 'Specimen Sent', 'Processing', 'Report Received', 'Benign', 'Premalignant', 'Malignant'
        [HistopathologyDiagnosis]   NVARCHAR(MAX) NULL,
        [ResultsNotes]              NVARCHAR(MAX) NULL,
        [FollowUpRequired]          BIT NOT NULL CONSTRAINT DF_BiopsyRecords_FollowUp DEFAULT (1),
        [FollowUpDate]              DATE NULL,
        [CreatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_BiopsyRecords_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_BiopsyRecords_UpdatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_BiopsyRecords] PRIMARY KEY CLUSTERED ([BiopsyID] ASC),
        CONSTRAINT [FK_BiopsyRecords_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]),
        CONSTRAINT [CK_BiopsyRecords_Type] CHECK ([BiopsyType] IN ('Incisional', 'Excisional'))
    );

    CREATE NONCLUSTERED INDEX [IX_BiopsyRecords_PatientID] ON [dentist].[BiopsyRecords]([PatientID]);
    CREATE NONCLUSTERED INDEX [IX_BiopsyRecords_Date] ON [dentist].[BiopsyRecords]([BiopsyDate]);
    PRINT '   + Table [dentist].[BiopsyRecords] created successfully.';
END
ELSE
BEGIN
    PRINT '   - Table [dentist].[BiopsyRecords] already exists.';
END
GO

-- =====================================================================================
-- 4. CREATE [dentist].[OrthoAlignerTreatments] TABLE
-- =====================================================================================
PRINT '>> Step 4: Creating [dentist].[OrthoAlignerTreatments] Table...';

IF OBJECT_ID('dentist.OrthoAlignerTreatments', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[OrthoAlignerTreatments] (
        [OrthoAlignerID]            INT IDENTITY(1,1) NOT NULL,
        [PatientID]                 INT NOT NULL,
        [DoctorID]                  INT NULL,
        [AlignerBrand]              NVARCHAR(100) NOT NULL, -- 'Invisalign', 'ClearCorrect', 'Spark', 'AngelAlign', 'In-House 3D Printed', 'Other'
        [TotalStages]               INT NOT NULL CONSTRAINT DF_OrthoAligners_TotalStages DEFAULT (1),
        [CurrentStage]              INT NOT NULL CONSTRAINT DF_OrthoAligners_CurrentStage DEFAULT (1),
        [AttachmentsRequired]       BIT NOT NULL CONSTRAINT DF_OrthoAligners_Attachments DEFAULT (0),
        [AttachmentNotes]           NVARCHAR(MAX) NULL,     -- Teeth and placement details
        [IprRequired]               BIT NOT NULL CONSTRAINT DF_OrthoAligners_Ipr DEFAULT (0),
        [IprDetails]                NVARCHAR(MAX) NULL,     -- Specific contact areas and reduction in mm
        [WearSchedule]              NVARCHAR(100) NOT NULL CONSTRAINT DF_OrthoAligners_WearSchedule DEFAULT ('7 Days/Tray'),
                                    -- '7 Days/Tray', '10 Days/Tray', '14 Days/Tray', '20-22 Hours/Day'
        [RefinementScanTracking]    NVARCHAR(MAX) NULL,     -- Tracking notes & dates of refinement scans
        [RefinementCount]           INT NOT NULL CONSTRAINT DF_OrthoAligners_RefinementCount DEFAULT (0),
        [Arch]                      NVARCHAR(20) NOT NULL CONSTRAINT DF_OrthoAligners_Arch DEFAULT ('Dual'),
                                    -- 'Upper', 'Lower', 'Dual'
        [Status]                    NVARCHAR(50) NOT NULL CONSTRAINT DF_OrthoAligners_Status DEFAULT ('Active'),
                                    -- 'Planned', 'Active', 'Refinement Needed', 'Retention', 'Completed', 'Discontinued'
        [StartDate]                 DATE NULL,
        [TargetCompletionDate]      DATE NULL,
        [ClinicalNotes]             NVARCHAR(MAX) NULL,
        [CreatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_OrthoAligners_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_OrthoAligners_UpdatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_OrthoAlignerTreatments] PRIMARY KEY CLUSTERED ([OrthoAlignerID] ASC),
        CONSTRAINT [FK_OrthoAlignerTreatments_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]),
        CONSTRAINT [CK_OrthoAligners_Stages] CHECK ([TotalStages] >= 1 AND [TotalStages] <= 200 AND [CurrentStage] >= 0)
    );

    CREATE NONCLUSTERED INDEX [IX_OrthoAligners_PatientID] ON [dentist].[OrthoAlignerTreatments]([PatientID]);
    CREATE NONCLUSTERED INDEX [IX_OrthoAligners_Status] ON [dentist].[OrthoAlignerTreatments]([Status]);
    PRINT '   + Table [dentist].[OrthoAlignerTreatments] created successfully.';
END
ELSE
BEGIN
    PRINT '   - Table [dentist].[OrthoAlignerTreatments] already exists.';
END
GO

PRINT '>> [Dentia] Clinical Specialties Expansion Migration Completed Successfully!';
GO
