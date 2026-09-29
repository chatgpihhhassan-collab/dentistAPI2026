-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: FULL 15-CATEGORY MASTER DENTAL PROCEDURE CATALOG
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Total Procedures: 141 across 15 standard clinical categories
-- =====================================================================================
USE [DentistAPI];
GO

-- Create Master Procedure Catalog Table
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'MasterProcedureCatalog' AND s.name = 'dentist')
BEGIN
    CREATE TABLE [dentist].[MasterProcedureCatalog] (
        [CatalogID]         INT IDENTITY(1,1) PRIMARY KEY,
        [Category]          NVARCHAR(100) NOT NULL,
        [ProcedureName]     NVARCHAR(255) NOT NULL,
        [ProcedureCode]     NVARCHAR(50) NOT NULL UNIQUE,
        [EstimatedDuration] NVARCHAR(50) NOT NULL DEFAULT '45 mins',
        [DefaultFeeNZD]     DECIMAL(18,2) NOT NULL,
        [DefaultFeePKR]     DECIMAL(18,2) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT '>> Created [dentist].[MasterProcedureCatalog] table.';
END
GO

-- Populate/Update MasterProcedureCatalog
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0150')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Dental Consultation', 'D0150', '30 mins', 60.00, 5000.00, 'Initial comprehensive clinical consultation, patient history intake, and diagnostic assessment.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Dental Consultation', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 5000.00, [Description] = 'Initial comprehensive clinical consultation, patient history intake, and diagnostic assessment.'
    WHERE [ProcedureCode] = 'D0150';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0120')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Comprehensive Oral Examination', 'D0120', '45 mins', 85.00, 6500.00, 'Thorough dental charting, soft tissue inspection, periodontal screening, and occlusion review.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Comprehensive Oral Examination', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 85.00, [DefaultFeePKR] = 6500.00, [Description] = 'Thorough dental charting, soft tissue inspection, periodontal screening, and occlusion review.'
    WHERE [ProcedureCode] = 'D0120';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0120B')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Routine Dental Check-up', 'D0120B', '30 mins', 50.00, 4000.00, 'Periodic recall exam, enamel integrity check, and hygiene recall status evaluation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Routine Dental Check-up', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 50.00, [DefaultFeePKR] = 4000.00, [Description] = 'Periodic recall exam, enamel integrity check, and hygiene recall status evaluation.'
    WHERE [ProcedureCode] = 'D0120B';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0140')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Emergency Examination', 'D0140', '30 mins', 95.00, 7500.00, 'Focused problem-oriented diagnostic triage for acute pain, trauma, or odontogenic infection.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Emergency Examination', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 7500.00, [Description] = 'Focused problem-oriented diagnostic triage for acute pain, trauma, or odontogenic infection.'
    WHERE [ProcedureCode] = 'D0140';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0431')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Oral Cancer Screening', 'D0431', '20 mins', 55.00, 4500.00, 'Fluorescence-enhanced mucosal examination and lymph node palpation for neoplastic changes.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Oral Cancer Screening', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 55.00, [DefaultFeePKR] = 4500.00, [Description] = 'Fluorescence-enhanced mucosal examination and lymph node palpation for neoplastic changes.'
    WHERE [ProcedureCode] = 'D0431';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0220')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Dental X-ray', 'D0220', '15 mins', 35.00, 2500.00, 'High-resolution digital periapical radiographic exposure of single tooth and root apex.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Dental X-ray', [EstimatedDuration] = '15 mins', [DefaultFeeNZD] = 35.00, [DefaultFeePKR] = 2500.00, [Description] = 'High-resolution digital periapical radiographic exposure of single tooth and root apex.'
    WHERE [ProcedureCode] = 'D0220';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0330')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Full Mouth X-ray (OPG)', 'D0330', '20 mins', 110.00, 8500.00, 'Orthopantomogram panoramic radiograph capturing full maxillary and mandibular dentition.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Full Mouth X-ray (OPG)', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 110.00, [DefaultFeePKR] = 8500.00, [Description] = 'Orthopantomogram panoramic radiograph capturing full maxillary and mandibular dentition.'
    WHERE [ProcedureCode] = 'D0330';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0272')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Intraoral X-ray', 'D0272', '15 mins', 30.00, 2200.00, 'Bitewing intraoral radiograph for detecting interproximal caries and alveolar crest levels.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Intraoral X-ray', [EstimatedDuration] = '15 mins', [DefaultFeeNZD] = 30.00, [DefaultFeePKR] = 2200.00, [Description] = 'Bitewing intraoral radiograph for detecting interproximal caries and alveolar crest levels.'
    WHERE [ProcedureCode] = 'D0272';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0364')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'CBCT Scan', 'D0364', '30 mins', 220.00, 18000.00, 'Cone-Beam Computed Tomography 3D volumetric imaging for implant planning and impactions.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'CBCT Scan', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 18000.00, [Description] = 'Cone-Beam Computed Tomography 3D volumetric imaging for implant planning and impactions.'
    WHERE [ProcedureCode] = 'D0364';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0160')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Treatment Planning', 'D0160', '30 mins', 75.00, 6000.00, 'Multi-disciplinary phased dental treatment plan generation and patient consultation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Treatment Planning', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 75.00, [DefaultFeePKR] = 6000.00, [Description] = 'Multi-disciplinary phased dental treatment plan generation and patient consultation.'
    WHERE [ProcedureCode] = 'D0160';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0170')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Examination & Diagnosis', 'Second Opinion', 'D0170', '30 mins', 70.00, 5500.00, 'Independent clinical evaluation of prior diagnoses, radiographs, and proposed care plans.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Examination & Diagnosis', [ProcedureName] = 'Second Opinion', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 70.00, [DefaultFeePKR] = 5500.00, [Description] = 'Independent clinical evaluation of prior diagnoses, radiographs, and proposed care plans.'
    WHERE [ProcedureCode] = 'D0170';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1110')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Dental Cleaning / Scaling', 'D1110', '45 mins', 85.00, 6500.00, 'Ultrasonic supragingival calculus and plaque debridement across all teeth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Dental Cleaning / Scaling', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 85.00, [DefaultFeePKR] = 6500.00, [Description] = 'Ultrasonic supragingival calculus and plaque debridement across all teeth.'
    WHERE [ProcedureCode] = 'D1110';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1110B')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Polishing', 'D1110B', '20 mins', 40.00, 3000.00, 'Prophylaxis paste cup polishing to eliminate superficial extrinsic stain and biofilm.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Polishing', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 40.00, [DefaultFeePKR] = 3000.00, [Description] = 'Prophylaxis paste cup polishing to eliminate superficial extrinsic stain and biofilm.'
    WHERE [ProcedureCode] = 'D1110B';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1206')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Fluoride Treatment', 'D1206', '20 mins', 35.00, 2800.00, 'Topical high-concentration neutral sodium fluoride varnish for remineralization.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Fluoride Treatment', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 35.00, [DefaultFeePKR] = 2800.00, [Description] = 'Topical high-concentration neutral sodium fluoride varnish for remineralization.'
    WHERE [ProcedureCode] = 'D1206';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1351')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Dental Sealants', 'D1351', '30 mins', 45.00, 3500.00, 'Resin pit-and-fissure sealant application on vulnerable posterior molar grooves.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Dental Sealants', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 45.00, [DefaultFeePKR] = 3500.00, [Description] = 'Resin pit-and-fissure sealant application on vulnerable posterior molar grooves.'
    WHERE [ProcedureCode] = 'D1351';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1330')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Oral Hygiene Instructions', 'D1330', '20 mins', 30.00, 2000.00, 'Chairside demonstration of modified Bass brushing, interdental brushes, and flossing.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Oral Hygiene Instructions', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 30.00, [DefaultFeePKR] = 2000.00, [Description] = 'Chairside demonstration of modified Bass brushing, interdental brushes, and flossing.'
    WHERE [ProcedureCode] = 'D1330';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9910')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Desensitization Treatment', 'D9910', '20 mins', 45.00, 3500.00, 'Application of topical potassium nitrate / gluma desensitizing agent to root exposures.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Desensitization Treatment', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 45.00, [DefaultFeePKR] = 3500.00, [Description] = 'Application of topical potassium nitrate / gluma desensitizing agent to root exposures.'
    WHERE [ProcedureCode] = 'D9910';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9944')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Preventive Dentistry', 'Night Guard / Mouth Guard', 'D9944', '45 mins', 250.00, 19500.00, 'Custom-molded dual-laminate occlusal guard for nocturnal bruxism and TMJ protection.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Preventive Dentistry', [ProcedureName] = 'Night Guard / Mouth Guard', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 250.00, [DefaultFeePKR] = 19500.00, [Description] = 'Custom-molded dual-laminate occlusal guard for nocturnal bruxism and TMJ protection.'
    WHERE [ProcedureCode] = 'D9944';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2391')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Composite Filling', 'D2391', '45 mins', 150.00, 10500.00, 'Direct resin composite aesthetic restoration (1 surface) under rubber dam isolation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Composite Filling', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 150.00, [DefaultFeePKR] = 10500.00, [Description] = 'Direct resin composite aesthetic restoration (1 surface) under rubber dam isolation.'
    WHERE [ProcedureCode] = 'D2391';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2140')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Amalgam Filling', 'D2140', '45 mins', 110.00, 7500.00, 'High-copper dental amalgam restoration for durable posterior load bearing.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Amalgam Filling', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 110.00, [DefaultFeePKR] = 7500.00, [Description] = 'High-copper dental amalgam restoration for durable posterior load bearing.'
    WHERE [ProcedureCode] = 'D2140';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2330')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Glass Ionomer Filling', 'D2330', '30 mins', 120.00, 8500.00, 'Bioactive fluoride-releasing glass ionomer restoration for cervical lesions and subgingival margins.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Glass Ionomer Filling', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 120.00, [DefaultFeePKR] = 8500.00, [Description] = 'Bioactive fluoride-releasing glass ionomer restoration for cervical lesions and subgingival margins.'
    WHERE [ProcedureCode] = 'D2330';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2940')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Temporary Filling', 'D2940', '20 mins', 60.00, 4500.00, 'Zinc oxide eugenol / Cavit provisional sedative dressing for symptom relief.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Temporary Filling', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 4500.00, [Description] = 'Zinc oxide eugenol / Cavit provisional sedative dressing for symptom relief.'
    WHERE [ProcedureCode] = 'D2940';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2392')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Permanent Filling', 'D2392', '60 mins', 185.00, 13000.00, 'Multi-surface composite restoration with sectional matrix and tight contact points.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Permanent Filling', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 185.00, [DefaultFeePKR] = 13000.00, [Description] = 'Multi-surface composite restoration with sectional matrix and tight contact points.'
    WHERE [ProcedureCode] = 'D2392';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2390')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Tooth Bonding', 'D2390', '45 mins', 165.00, 11500.00, 'Direct composite resin bonding to close diastemas or restore chipped incisal edges.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Tooth Bonding', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 165.00, [DefaultFeePKR] = 11500.00, [Description] = 'Direct composite resin bonding to close diastemas or restore chipped incisal edges.'
    WHERE [ProcedureCode] = 'D2390';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2610')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Inlay', 'D2610', '60 mins', 380.00, 26000.00, 'Indirect porcelain or composite restoration fabricated within the cuspal bounds.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Inlay', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 380.00, [DefaultFeePKR] = 26000.00, [Description] = 'Indirect porcelain or composite restoration fabricated within the cuspal bounds.'
    WHERE [ProcedureCode] = 'D2610';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2620')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Fillings & Restorative Treatment', 'Onlay', 'D2620', '60 mins', 420.00, 29000.00, 'Indirect cuspal coverage restoration preserving remaining natural tooth structure.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Fillings & Restorative Treatment', [ProcedureName] = 'Onlay', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 420.00, [DefaultFeePKR] = 29000.00, [Description] = 'Indirect cuspal coverage restoration preserving remaining natural tooth structure.'
    WHERE [ProcedureCode] = 'D2620';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2750')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Dental Crown', 'D2750', '60 mins', 620.00, 40000.00, 'Full-coverage dental crown to restore strength, aesthetics, and masticatory function.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Dental Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 620.00, [DefaultFeePKR] = 40000.00, [Description] = 'Full-coverage dental crown to restore strength, aesthetics, and masticatory function.'
    WHERE [ProcedureCode] = 'D2750';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2740P')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Porcelain Crown', 'D2740P', '60 mins', 650.00, 43000.00, 'All-ceramic feldspathic / lithium disilicate (E.max) aesthetic dental crown.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Porcelain Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 650.00, [DefaultFeePKR] = 43000.00, [Description] = 'All-ceramic feldspathic / lithium disilicate (E.max) aesthetic dental crown.'
    WHERE [ProcedureCode] = 'D2740P';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2740Z')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Zirconia Crown', 'D2740Z', '60 mins', 680.00, 46000.00, 'Monolithic high-strength translucent zirconia crown milled with CAD/CAM precision.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Zirconia Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 680.00, [DefaultFeePKR] = 46000.00, [Description] = 'Monolithic high-strength translucent zirconia crown milled with CAD/CAM precision.'
    WHERE [ProcedureCode] = 'D2740Z';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2751')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'PFM Crown', 'D2751', '60 mins', 550.00, 36000.00, 'Porcelain-fused-to-metal crown combining sub-structure rigidity with enamel shade match.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'PFM Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 550.00, [DefaultFeePKR] = 36000.00, [Description] = 'Porcelain-fused-to-metal crown combining sub-structure rigidity with enamel shade match.'
    WHERE [ProcedureCode] = 'D2751';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2790')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Metal Crown', 'D2790', '45 mins', 450.00, 30000.00, 'Full cast precious / base metal alloy crown for minimal tooth reduction requirements.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Metal Crown', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 450.00, [DefaultFeePKR] = 30000.00, [Description] = 'Full cast precious / base metal alloy crown for minimal tooth reduction requirements.'
    WHERE [ProcedureCode] = 'D2790';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2970')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Temporary Crown', 'D2970', '30 mins', 90.00, 6500.00, 'Chairside provisional acrylic/bis-acryl crown to protect prepared pulp and occlusion.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Temporary Crown', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6500.00, [Description] = 'Chairside provisional acrylic/bis-acryl crown to protect prepared pulp and occlusion.'
    WHERE [ProcedureCode] = 'D2970';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6240')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Dental Bridge', 'D6240', '90 mins', 1250.00, 88000.00, 'Multi-unit fixed dental prosthesis anchoring pontics to adjacent natural abutments.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Dental Bridge', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 1250.00, [DefaultFeePKR] = 88000.00, [Description] = 'Multi-unit fixed dental prosthesis anchoring pontics to adjacent natural abutments.'
    WHERE [ProcedureCode] = 'D6240';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6240T')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Temporary Bridge', 'D6240T', '45 mins', 190.00, 13000.00, 'Provisional multi-unit bridge maintaining space and soft tissue contour during lab fabrication.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Temporary Bridge', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 190.00, [DefaultFeePKR] = 13000.00, [Description] = 'Provisional multi-unit bridge maintaining space and soft tissue contour during lab fabrication.'
    WHERE [ProcedureCode] = 'D6240T';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2920')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Crown Re-cementation', 'D2920', '30 mins', 75.00, 5000.00, 'Ultrasonic debridement of crown intaglio and adhesive resin cement re-attachment.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Crown Re-cementation', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 75.00, [DefaultFeePKR] = 5000.00, [Description] = 'Ultrasonic debridement of crown intaglio and adhesive resin cement re-attachment.'
    WHERE [ProcedureCode] = 'D2920';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6930')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Crowns & Bridges', 'Bridge Re-cementation', 'D6930', '30 mins', 95.00, 6500.00, 'Cleaning and re-cementation of dislodged multi-unit fixed bridge prosthesis.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Crowns & Bridges', [ProcedureName] = 'Bridge Re-cementation', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 6500.00, [Description] = 'Cleaning and re-cementation of dislodged multi-unit fixed bridge prosthesis.'
    WHERE [ProcedureCode] = 'D6930';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3330')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Root Canal Treatment (RCT)', 'D3330', '90 mins', 450.00, 30000.00, 'Rotary endodontic extirpation, sodium hypochlorite disinfection, and gutta-percha seal.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Root Canal Treatment (RCT)', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 450.00, [DefaultFeePKR] = 30000.00, [Description] = 'Rotary endodontic extirpation, sodium hypochlorite disinfection, and gutta-percha seal.'
    WHERE [ProcedureCode] = 'D3330';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3346')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Root Canal Re-treatment', 'D3346', '90 mins', 560.00, 38000.00, 'Removal of existing obturation materials, apex re-instrumentation, and bioceramic seal.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Root Canal Re-treatment', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 560.00, [DefaultFeePKR] = 38000.00, [Description] = 'Removal of existing obturation materials, apex re-instrumentation, and bioceramic seal.'
    WHERE [ProcedureCode] = 'D3346';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3220')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Pulpotomy', 'D3220', '45 mins', 140.00, 9500.00, 'Coronal pulp amputation and MTA / formocresol medicament application to preserve radicular vitality.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Pulpotomy', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 140.00, [DefaultFeePKR] = 9500.00, [Description] = 'Coronal pulp amputation and MTA / formocresol medicament application to preserve radicular vitality.'
    WHERE [ProcedureCode] = 'D3220';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3221')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Pulpectomy', 'D3221', '45 mins', 160.00, 11000.00, 'Complete removal of coronal and radicular pulp tissue as initial emergency triage.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Pulpectomy', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 160.00, [DefaultFeePKR] = 11000.00, [Description] = 'Complete removal of coronal and radicular pulp tissue as initial emergency triage.'
    WHERE [ProcedureCode] = 'D3221';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3331')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Temporary Root Canal Dressing', 'D3331', '30 mins', 85.00, 6000.00, 'Intracanal calcium hydroxide antimicrobial paste placement between endodontic visits.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Temporary Root Canal Dressing', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 85.00, [DefaultFeePKR] = 6000.00, [Description] = 'Intracanal calcium hydroxide antimicrobial paste placement between endodontic visits.'
    WHERE [ProcedureCode] = 'D3331';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2952')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Post & Core', 'D2952', '45 mins', 180.00, 12500.00, 'Custom cast or prefabricated metal post cemented into canal with composite core buildup.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Post & Core', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 180.00, [DefaultFeePKR] = 12500.00, [Description] = 'Custom cast or prefabricated metal post cemented into canal with composite core buildup.'
    WHERE [ProcedureCode] = 'D2952';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2954')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Fiber Post', 'D2954', '45 mins', 160.00, 11500.00, 'Glass-fiber reinforced resin post bonded into canal space to absorb masticatory stress.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Fiber Post', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 160.00, [DefaultFeePKR] = 11500.00, [Description] = 'Glass-fiber reinforced resin post bonded into canal space to absorb masticatory stress.'
    WHERE [ProcedureCode] = 'D2954';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2740RC')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Root Canal Treatment', 'Root Canal Crown', 'D2740RC', '60 mins', 630.00, 42000.00, 'Full cuspal protection crown engineered specifically for endodontically treated teeth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Root Canal Treatment', [ProcedureName] = 'Root Canal Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 630.00, [DefaultFeePKR] = 42000.00, [Description] = 'Full cuspal protection crown engineered specifically for endodontically treated teeth.'
    WHERE [ProcedureCode] = 'D2740RC';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7140')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Simple Tooth Extraction', 'D7140', '30 mins', 130.00, 8500.00, 'Atraumatic forceps luxation and extraction of non-restorable erupted tooth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Simple Tooth Extraction', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 130.00, [DefaultFeePKR] = 8500.00, [Description] = 'Atraumatic forceps luxation and extraction of non-restorable erupted tooth.'
    WHERE [ProcedureCode] = 'D7140';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7210')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Surgical Tooth Extraction', 'D7210', '60 mins', 260.00, 18500.00, 'Full-thickness flap reflection, bone guttering, tooth sectioning, and resorbable sutures.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Surgical Tooth Extraction', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 260.00, [DefaultFeePKR] = 18500.00, [Description] = 'Full-thickness flap reflection, bone guttering, tooth sectioning, and resorbable sutures.'
    WHERE [ProcedureCode] = 'D7210';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7220')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Wisdom Tooth Extraction', 'D7220', '45 mins', 280.00, 20000.00, 'Surgical removal of fully erupted or soft-tissue impacted third molar.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Wisdom Tooth Extraction', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 280.00, [DefaultFeePKR] = 20000.00, [Description] = 'Surgical removal of fully erupted or soft-tissue impacted third molar.'
    WHERE [ProcedureCode] = 'D7220';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7240')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Impacted Wisdom Tooth Removal', 'D7240', '60 mins', 360.00, 26000.00, 'Complete bony impaction removal with lingual/buccal flap elevation and ostectomy.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Impacted Wisdom Tooth Removal', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 360.00, [DefaultFeePKR] = 26000.00, [Description] = 'Complete bony impaction removal with lingual/buccal flap elevation and ostectomy.'
    WHERE [ProcedureCode] = 'D7240';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7250')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Root Removal', 'D7250', '30 mins', 150.00, 10500.00, 'Elevator delivery of retained fractured apical root tip.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Root Removal', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 150.00, [DefaultFeePKR] = 10500.00, [Description] = 'Elevator delivery of retained fractured apical root tip.'
    WHERE [ProcedureCode] = 'D7250';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7250S')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Surgical Root Removal', 'D7250S', '45 mins', 220.00, 15500.00, 'Surgical alveolar troughing and apical delivery of deeply embedded residual root.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Surgical Root Removal', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 15500.00, [Description] = 'Surgical alveolar troughing and apical delivery of deeply embedded residual root.'
    WHERE [ProcedureCode] = 'D7250S';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7510')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Incision & Drainage', 'D7510', '30 mins', 140.00, 9500.00, 'Surgical decompression of intraoral vestibular abscess with Penrose drain placement.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Incision & Drainage', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 140.00, [DefaultFeePKR] = 9500.00, [Description] = 'Surgical decompression of intraoral vestibular abscess with Penrose drain placement.'
    WHERE [ProcedureCode] = 'D7510';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7511')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Abscess Treatment', 'D7511', '30 mins', 130.00, 9000.00, 'Antimicrobial irrigation, curettage, and systemic antibiotic regimen for acute dentoalveolar infection.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Abscess Treatment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 130.00, [DefaultFeePKR] = 9000.00, [Description] = 'Antimicrobial irrigation, curettage, and systemic antibiotic regimen for acute dentoalveolar infection.'
    WHERE [ProcedureCode] = 'D7511';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7310')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Alveoloplasty', 'D7310', '45 mins', 240.00, 16500.00, 'Surgical recontouring of irregular alveolar bone ridges prior to denture fabrication.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Alveoloplasty', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 240.00, [DefaultFeePKR] = 16500.00, [Description] = 'Surgical recontouring of irregular alveolar bone ridges prior to denture fabrication.'
    WHERE [ProcedureCode] = 'D7310';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7960')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Frenectomy', 'D7960', '45 mins', 280.00, 19500.00, 'Surgical excision or laser ablation of restrictive labial/lingual frenum (tongue-tie release).');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Frenectomy', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 280.00, [DefaultFeePKR] = 19500.00, [Description] = 'Surgical excision or laser ablation of restrictive labial/lingual frenum (tongue-tie release).'
    WHERE [ProcedureCode] = 'D7960';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7286')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Extractions & Oral Surgery', 'Biopsy', 'D7286', '30 mins', 220.00, 15000.00, 'Incisional or punch biopsy of abnormal mucosal tissue for histopathological diagnosis.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Extractions & Oral Surgery', [ProcedureName] = 'Biopsy', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 15000.00, [Description] = 'Incisional or punch biopsy of abnormal mucosal tissue for histopathological diagnosis.'
    WHERE [ProcedureCode] = 'D7286';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4346')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Scaling', 'D4346', '45 mins', 90.00, 6500.00, 'Full-mouth debridement in the presence of generalized moderate-to-severe gingival inflammation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Scaling', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6500.00, [Description] = 'Full-mouth debridement in the presence of generalized moderate-to-severe gingival inflammation.'
    WHERE [ProcedureCode] = 'D4346';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4341')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Root Planing', 'D4341', '60 mins', 160.00, 11000.00, 'Subgingival instrumentation to smooth cementum and remove endotoxin-laden root surfaces.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Root Planing', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 160.00, [DefaultFeePKR] = 11000.00, [Description] = 'Subgingival instrumentation to smooth cementum and remove endotoxin-laden root surfaces.'
    WHERE [ProcedureCode] = 'D4341';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4910')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Periodontal Cleaning', 'D4910', '45 mins', 110.00, 7800.00, 'Ongoing maintenance therapy for patients previously treated for periodontal disease.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Periodontal Cleaning', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 110.00, [DefaultFeePKR] = 7800.00, [Description] = 'Ongoing maintenance therapy for patients previously treated for periodontal disease.'
    WHERE [ProcedureCode] = 'D4910';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0180')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Periodontal Examination', 'D0180', '30 mins', 65.00, 4800.00, '6-point periodontal pocket depth probing, bleeding indices, mobility, and furcation charting.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Periodontal Examination', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 65.00, [DefaultFeePKR] = 4800.00, [Description] = '6-point periodontal pocket depth probing, bleeding indices, mobility, and furcation charting.'
    WHERE [ProcedureCode] = 'D0180';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4210')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Gum Disease Treatment', 'D4210', '60 mins', 220.00, 15500.00, 'Targeted local delivery of antimicrobial microcapsules (Arestin/Periochip) into deep pockets.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Gum Disease Treatment', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 15500.00, [Description] = 'Targeted local delivery of antimicrobial microcapsules (Arestin/Periochip) into deep pockets.'
    WHERE [ProcedureCode] = 'D4210';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4240')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Gum Surgery', 'D4240', '60 mins', 380.00, 27000.00, 'Access flap periodontal surgery for debridement of deep osseous defects.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Gum Surgery', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 380.00, [DefaultFeePKR] = 27000.00, [Description] = 'Access flap periodontal surgery for debridement of deep osseous defects.'
    WHERE [ProcedureCode] = 'D4240';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4211')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Gingivectomy', 'D4211', '45 mins', 250.00, 17500.00, 'Surgical or laser excision of fibrotic, hyperplastic gingival tissue.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Gingivectomy', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 250.00, [DefaultFeePKR] = 17500.00, [Description] = 'Surgical or laser excision of fibrotic, hyperplastic gingival tissue.'
    WHERE [ProcedureCode] = 'D4211';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4212')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Gingivoplasty', 'D4212', '45 mins', 240.00, 16800.00, 'Surgical re-sculpting of gingival margins to create physiological contours and self-cleansing zones.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Gingivoplasty', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 240.00, [DefaultFeePKR] = 16800.00, [Description] = 'Surgical re-sculpting of gingival margins to create physiological contours and self-cleansing zones.'
    WHERE [ProcedureCode] = 'D4212';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4241')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Periodontal Flap Surgery', 'D4241', '60 mins', 410.00, 29000.00, 'Full-thickness mucoperiosteal flap reflection to treat complex infra-bony defects.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Periodontal Flap Surgery', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 410.00, [DefaultFeePKR] = 29000.00, [Description] = 'Full-thickness mucoperiosteal flap reflection to treat complex infra-bony defects.'
    WHERE [ProcedureCode] = 'D4241';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4273')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Gum / Periodontal Treatment', 'Gum Grafting', 'D4273', '90 mins', 560.00, 39000.00, 'Subepithelial connective tissue graft for root coverage and gingival recession repair.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Gum / Periodontal Treatment', [ProcedureName] = 'Gum Grafting', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 560.00, [DefaultFeePKR] = 39000.00, [Description] = 'Subepithelial connective tissue graft for root coverage and gingival recession repair.'
    WHERE [ProcedureCode] = 'D4273';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5110')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Complete Denture', 'D5110', '60 mins', 950.00, 68000.00, 'Full upper or lower acrylic resin denture replacing all natural teeth in the dental arch.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Complete Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 950.00, [DefaultFeePKR] = 68000.00, [Description] = 'Full upper or lower acrylic resin denture replacing all natural teeth in the dental arch.'
    WHERE [ProcedureCode] = 'D5110';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5213')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Partial Denture', 'D5213', '60 mins', 780.00, 52000.00, 'Removable cast partial denture replacing multiple missing teeth with retentive clasps.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Partial Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 780.00, [DefaultFeePKR] = 52000.00, [Description] = 'Removable cast partial denture replacing multiple missing teeth with retentive clasps.'
    WHERE [ProcedureCode] = 'D5213';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5211')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Acrylic Denture', 'D5211', '60 mins', 650.00, 45000.00, 'Economical removable tissue-borne acrylic transitional partial denture.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Acrylic Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 650.00, [DefaultFeePKR] = 45000.00, [Description] = 'Economical removable tissue-borne acrylic transitional partial denture.'
    WHERE [ProcedureCode] = 'D5211';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5225')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Flexible Denture', 'D5225', '60 mins', 870.00, 60000.00, 'Thermoplastic Valplast monomer-free flexible denture with tooth-colored clasps.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Flexible Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 870.00, [DefaultFeePKR] = 60000.00, [Description] = 'Thermoplastic Valplast monomer-free flexible denture with tooth-colored clasps.'
    WHERE [ProcedureCode] = 'D5225';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5214')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Metal Framework Denture', 'D5214', '60 mins', 1080.00, 75000.00, 'Cobalt-chromium precision-cast framework with prosthetic teeth and acrylic base.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Metal Framework Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 1080.00, [DefaultFeePKR] = 75000.00, [Description] = 'Cobalt-chromium precision-cast framework with prosthetic teeth and acrylic base.'
    WHERE [ProcedureCode] = 'D5214';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5130')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Immediate Denture', 'D5130', '60 mins', 820.00, 58000.00, 'Prosthesis fabricated prior to extractions and inserted immediately post-surgery.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Immediate Denture', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 820.00, [DefaultFeePKR] = 58000.00, [Description] = 'Prosthesis fabricated prior to extractions and inserted immediately post-surgery.'
    WHERE [ProcedureCode] = 'D5130';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5611')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Denture Repair', 'D5611', '45 mins', 140.00, 9800.00, 'Rapid cold-cure acrylic rejoining of fractured denture base or replacing dislodged tooth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Denture Repair', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 140.00, [DefaultFeePKR] = 9800.00, [Description] = 'Rapid cold-cure acrylic rejoining of fractured denture base or replacing dislodged tooth.'
    WHERE [ProcedureCode] = 'D5611';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5730')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Denture Relining', 'D5730', '45 mins', 220.00, 15500.00, 'Hard or soft chairside reline of tissue intaglio to restore snug retention.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Denture Relining', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 15500.00, [Description] = 'Hard or soft chairside reline of tissue intaglio to restore snug retention.'
    WHERE [ProcedureCode] = 'D5730';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5410')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dentures', 'Denture Adjustment', 'D5410', '30 mins', 60.00, 4200.00, 'Relief of sore spots, pressure ulcer areas, and selective grinding of denture occlusion.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dentures', [ProcedureName] = 'Denture Adjustment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 4200.00, [Description] = 'Relief of sore spots, pressure ulcer areas, and selective grinding of denture occlusion.'
    WHERE [ProcedureCode] = 'D5410';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6010C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant Consultation', 'D6010C', '45 mins', 95.00, 7200.00, '3D radiological evaluation, bone height measurement, and prosthetic feasibility review.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant Consultation', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 7200.00, [Description] = '3D radiological evaluation, bone height measurement, and prosthetic feasibility review.'
    WHERE [ProcedureCode] = 'D6010C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6010')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant Placement', 'D6010', '90 mins', 1650.00, 125000.00, 'Surgical osteotomy and precision insertion of titanium / zirconia endosteal dental implant.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant Placement', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 1650.00, [DefaultFeePKR] = 125000.00, [Description] = 'Surgical osteotomy and precision insertion of titanium / zirconia endosteal dental implant.'
    WHERE [ProcedureCode] = 'D6010';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7953')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Bone Grafting', 'D7953', '60 mins', 680.00, 48000.00, 'Particulate xenograft / allograft bone placement with bioabsorbable collagen membrane.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Bone Grafting', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 680.00, [DefaultFeePKR] = 48000.00, [Description] = 'Particulate xenograft / allograft bone placement with bioabsorbable collagen membrane.'
    WHERE [ProcedureCode] = 'D7953';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7951')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Sinus Lift', 'D7951', '90 mins', 980.00, 72000.00, 'Lateral window or crestal osteotome elevation of Schneiderian membrane with subantral grafting.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Sinus Lift', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 980.00, [DefaultFeePKR] = 72000.00, [Description] = 'Lateral window or crestal osteotome elevation of Schneiderian membrane with subantral grafting.'
    WHERE [ProcedureCode] = 'D7951';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6056')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant Abutment', 'D6056', '45 mins', 450.00, 32000.00, 'Custom CAD/CAM titanium or zirconia abutment connecting implant fixture to crown.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant Abutment', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 450.00, [DefaultFeePKR] = 32000.00, [Description] = 'Custom CAD/CAM titanium or zirconia abutment connecting implant fixture to crown.'
    WHERE [ProcedureCode] = 'D6056';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6058')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant Crown', 'D6058', '60 mins', 980.00, 70000.00, 'Screw-retained or cement-retained monolithic zirconia implant-supported crown.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant Crown', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 980.00, [DefaultFeePKR] = 70000.00, [Description] = 'Screw-retained or cement-retained monolithic zirconia implant-supported crown.'
    WHERE [ProcedureCode] = 'D6058';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6068')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant-Supported Bridge', 'D6068', '90 mins', 2250.00, 165000.00, 'Multi-unit fixed prosthesis anchored onto two or more osseointegrated dental implants.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant-Supported Bridge', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 2250.00, [DefaultFeePKR] = 165000.00, [Description] = 'Multi-unit fixed prosthesis anchored onto two or more osseointegrated dental implants.'
    WHERE [ProcedureCode] = 'D6068';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6110')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant-Supported Denture', 'D6110', '90 mins', 2900.00, 210000.00, 'All-on-4 / All-on-6 hybrid overdenture attached with Locator or bar attachments.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant-Supported Denture', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 2900.00, [DefaultFeePKR] = 210000.00, [Description] = 'All-on-4 / All-on-6 hybrid overdenture attached with Locator or bar attachments.'
    WHERE [ProcedureCode] = 'D6110';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D6100')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Dental Implants', 'Implant Removal', 'D6100', '60 mins', 390.00, 28000.00, 'Trephine osteotomy or reverse torque removal of failed or fractured implant fixture.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Dental Implants', [ProcedureName] = 'Implant Removal', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 390.00, [DefaultFeePKR] = 28000.00, [Description] = 'Trephine osteotomy or reverse torque removal of failed or fractured implant fixture.'
    WHERE [ProcedureCode] = 'D6100';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9972')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Teeth Whitening', 'D9972', '45 mins', 250.00, 17000.00, 'In-office high-grade hydrogen peroxide photo-activated laser teeth bleaching.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Teeth Whitening', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 250.00, [DefaultFeePKR] = 17000.00, [Description] = 'In-office high-grade hydrogen peroxide photo-activated laser teeth bleaching.'
    WHERE [ProcedureCode] = 'D9972';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2960')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Dental Veneers', 'D2960', '60 mins', 550.00, 39000.00, 'Custom porcelain or composite shell bonded to facial surface for instant smile enhancement.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Dental Veneers', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 550.00, [DefaultFeePKR] = 39000.00, [Description] = 'Custom porcelain or composite shell bonded to facial surface for instant smile enhancement.'
    WHERE [ProcedureCode] = 'D2960';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2961')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Composite Veneers', 'D2961', '60 mins', 320.00, 23000.00, 'Direct layer-by-layer nanofill composite contouring, polishing, and shade blending.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Composite Veneers', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 320.00, [DefaultFeePKR] = 23000.00, [Description] = 'Direct layer-by-layer nanofill composite contouring, polishing, and shade blending.'
    WHERE [ProcedureCode] = 'D2961';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2962')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Porcelain Veneers', 'D2962', '60 mins', 690.00, 49000.00, 'Ultra-thin feldspathic / lithium disilicate veneers crafted by master dental ceramist.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Porcelain Veneers', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 690.00, [DefaultFeePKR] = 49000.00, [Description] = 'Ultra-thin feldspathic / lithium disilicate veneers crafted by master dental ceramist.'
    WHERE [ProcedureCode] = 'D2962';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9975')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Smile Design', 'D9975', '60 mins', 420.00, 29000.00, 'Digital 3D aesthetic simulation, facial proportion analysis, and diagnostic wax-up try-in.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Smile Design', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 420.00, [DefaultFeePKR] = 29000.00, [Description] = 'Digital 3D aesthetic simulation, facial proportion analysis, and diagnostic wax-up try-in.'
    WHERE [ProcedureCode] = 'D9975';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2390C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Tooth Bonding', 'D2390C', '45 mins', 165.00, 11800.00, 'Adhesive cosmetic repair of chipped enamel, irregular edges, and interdental gaps.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Tooth Bonding', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 165.00, [DefaultFeePKR] = 11800.00, [Description] = 'Adhesive cosmetic repair of chipped enamel, irregular edges, and interdental gaps.'
    WHERE [ProcedureCode] = 'D2390C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9971')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Tooth Contouring', 'D9971', '30 mins', 90.00, 6800.00, 'Enameloplasty to soften sharp incisal corners and smooth minor tooth asymmetries.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Tooth Contouring', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6800.00, [Description] = 'Enameloplasty to soften sharp incisal corners and smooth minor tooth asymmetries.'
    WHERE [ProcedureCode] = 'D9971';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4212C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Cosmetic Dentistry', 'Gum Contouring', 'D4212C', '45 mins', 250.00, 17500.00, 'Diode laser aesthetic gingival re-sculpting to resolve gummy smile asymmetries.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Cosmetic Dentistry', [ProcedureName] = 'Gum Contouring', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 250.00, [DefaultFeePKR] = 17500.00, [Description] = 'Diode laser aesthetic gingival re-sculpting to resolve gummy smile asymmetries.'
    WHERE [ProcedureCode] = 'D4212C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Orthodontic Consultation', 'D8080C', '45 mins', 90.00, 6800.00, 'Cephalometric, panoramic, intraoral scan review, and orthodontic malocclusion staging.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Orthodontic Consultation', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6800.00, [Description] = 'Cephalometric, panoramic, intraoral scan review, and orthodontic malocclusion staging.'
    WHERE [ProcedureCode] = 'D8080C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080M')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Metal Braces', 'D8080M', '60 mins', 2200.00, 155000.00, 'High-grade stainless steel bracket system with nickel-titanium archwires.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Metal Braces', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 2200.00, [DefaultFeePKR] = 155000.00, [Description] = 'High-grade stainless steel bracket system with nickel-titanium archwires.'
    WHERE [ProcedureCode] = 'D8080M';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080CE')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Ceramic Braces', 'D8080CE', '60 mins', 2800.00, 195000.00, 'Tooth-colored aesthetic monocrystalline ceramic brackets that blend with enamel.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Ceramic Braces', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 2800.00, [DefaultFeePKR] = 195000.00, [Description] = 'Tooth-colored aesthetic monocrystalline ceramic brackets that blend with enamel.'
    WHERE [ProcedureCode] = 'D8080CE';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080SL')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Self-Ligating Braces', 'D8080SL', '60 mins', 3100.00, 215000.00, 'Low-friction self-ligating Damon-style bracket system for accelerated tooth movement.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Self-Ligating Braces', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 3100.00, [DefaultFeePKR] = 215000.00, [Description] = 'Low-friction self-ligating Damon-style bracket system for accelerated tooth movement.'
    WHERE [ProcedureCode] = 'D8080SL';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080L')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Lingual Braces', 'D8080L', '90 mins', 4300.00, 300000.00, 'Custom 100% invisible brackets bonded strictly to the lingual/palatal tooth surfaces.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Lingual Braces', [EstimatedDuration] = '90 mins', [DefaultFeeNZD] = 4300.00, [DefaultFeePKR] = 300000.00, [Description] = 'Custom 100% invisible brackets bonded strictly to the lingual/palatal tooth surfaces.'
    WHERE [ProcedureCode] = 'D8080L';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8080A')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Clear Aligners', 'D8080A', '45 mins', 3500.00, 260000.00, 'Series of transparent custom thermoformed aligner trays (Invisalign / ClearCorrect style).');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Clear Aligners', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 3500.00, [DefaultFeePKR] = 260000.00, [Description] = 'Series of transparent custom thermoformed aligner trays (Invisalign / ClearCorrect style).'
    WHERE [ProcedureCode] = 'D8080A';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8680')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Retainers', 'D8680', '30 mins', 220.00, 15500.00, 'Post-treatment retention appliance maintaining tooth alignment and stability.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Retainers', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 220.00, [DefaultFeePKR] = 15500.00, [Description] = 'Post-treatment retention appliance maintaining tooth alignment and stability.'
    WHERE [ProcedureCode] = 'D8680';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8680F')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Fixed Retainer', 'D8680F', '45 mins', 280.00, 19500.00, 'Braided multi-strand stainless steel wire bonded lingually from canine to canine.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Fixed Retainer', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 280.00, [DefaultFeePKR] = 19500.00, [Description] = 'Braided multi-strand stainless steel wire bonded lingually from canine to canine.'
    WHERE [ProcedureCode] = 'D8680F';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8680R')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Removable Retainer', 'D8680R', '30 mins', 190.00, 13500.00, 'Essix / Hawley transparent removable post-orthodontic retention tray.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Removable Retainer', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 190.00, [DefaultFeePKR] = 13500.00, [Description] = 'Essix / Hawley transparent removable post-orthodontic retention tray.'
    WHERE [ProcedureCode] = 'D8680R';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8670')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Orthodontic Adjustment', 'D8670', '30 mins', 80.00, 5800.00, 'Periodic wire change, elastic ligatures replacement, and archwire activation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Orthodontic Adjustment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 80.00, [DefaultFeePKR] = 5800.00, [Description] = 'Periodic wire change, elastic ligatures replacement, and archwire activation.'
    WHERE [ProcedureCode] = 'D8670';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8690')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Bracket Replacement', 'D8690', '30 mins', 60.00, 4200.00, 'Acid-etch and re-bonding of dislodged orthodontic bracket.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Bracket Replacement', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 4200.00, [Description] = 'Acid-etch and re-bonding of dislodged orthodontic bracket.'
    WHERE [ProcedureCode] = 'D8690';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8691')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Orthodontics', 'Wire Replacement', 'D8691', '30 mins', 55.00, 3900.00, 'Replacement and cinch-back of bent, fractured, or upgraded archwire.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Orthodontics', [ProcedureName] = 'Wire Replacement', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 55.00, [DefaultFeePKR] = 3900.00, [Description] = 'Replacement and cinch-back of bent, fractured, or upgraded archwire.'
    WHERE [ProcedureCode] = 'D8691';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0145')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Child Dental Examination', 'D0145', '30 mins', 40.00, 3000.00, 'Gentle pediatric oral health assessment, caries risk profiling, and eruption monitoring.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Child Dental Examination', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 40.00, [DefaultFeePKR] = 3000.00, [Description] = 'Gentle pediatric oral health assessment, caries risk profiling, and eruption monitoring.'
    WHERE [ProcedureCode] = 'D0145';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1208')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Fluoride Application', 'D1208', '20 mins', 30.00, 2300.00, 'Child-friendly flavored topical neutral sodium fluoride varnish treatment.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Fluoride Application', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 30.00, [DefaultFeePKR] = 2300.00, [Description] = 'Child-friendly flavored topical neutral sodium fluoride varnish treatment.'
    WHERE [ProcedureCode] = 'D1208';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1351P')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Fissure Sealant', 'D1351P', '30 mins', 40.00, 3000.00, 'Preventative resin coating applied to newly erupted primary and permanent molars.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Fissure Sealant', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 40.00, [DefaultFeePKR] = 3000.00, [Description] = 'Preventative resin coating applied to newly erupted primary and permanent molars.'
    WHERE [ProcedureCode] = 'D1351P';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2391P')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Pediatric Filling', 'D2391P', '30 mins', 95.00, 6800.00, 'Gentle composite / compomer tooth restoration tailored for primary dentition.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Pediatric Filling', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 6800.00, [Description] = 'Gentle composite / compomer tooth restoration tailored for primary dentition.'
    WHERE [ProcedureCode] = 'D2391P';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2930')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Pediatric Crown', 'D2930', '45 mins', 180.00, 12500.00, 'Preformed stainless steel / zirconia crown for severely decayed deciduous molars.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Pediatric Crown', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 180.00, [DefaultFeePKR] = 12500.00, [Description] = 'Preformed stainless steel / zirconia crown for severely decayed deciduous molars.'
    WHERE [ProcedureCode] = 'D2930';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3220P')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Pulpotomy', 'D3220P', '30 mins', 120.00, 8800.00, 'Coronal pulp therapy for primary teeth to avoid premature tooth loss.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Pulpotomy', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 120.00, [DefaultFeePKR] = 8800.00, [Description] = 'Coronal pulp therapy for primary teeth to avoid premature tooth loss.'
    WHERE [ProcedureCode] = 'D3220P';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D3221P')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Pulpectomy', 'D3221P', '45 mins', 140.00, 9800.00, 'Root canal therapy for primary tooth using resorbable paste obturation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Pulpectomy', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 140.00, [DefaultFeePKR] = 9800.00, [Description] = 'Root canal therapy for primary tooth using resorbable paste obturation.'
    WHERE [ProcedureCode] = 'D3221P';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D1510')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Space Maintainer', 'D1510', '45 mins', 190.00, 13500.00, 'Fixed band-and-loop appliance preserving dental arch space following early tooth loss.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Space Maintainer', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 190.00, [DefaultFeePKR] = 13500.00, [Description] = 'Fixed band-and-loop appliance preserving dental arch space following early tooth loss.'
    WHERE [ProcedureCode] = 'D1510';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7111')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Pediatric Dentistry', 'Milk Tooth Extraction', 'D7111', '20 mins', 70.00, 5000.00, 'Atraumatic extraction of over-retained, abscessed, or fractured deciduous milk tooth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Pediatric Dentistry', [ProcedureName] = 'Milk Tooth Extraction', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 70.00, [DefaultFeePKR] = 5000.00, [Description] = 'Atraumatic extraction of over-retained, abscessed, or fractured deciduous milk tooth.'
    WHERE [ProcedureCode] = 'D7111';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0140E')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Emergency Consultation', 'D0140E', '30 mins', 95.00, 7200.00, 'Rapid assessment and pain diagnosis for same-day acute dental complaints.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Emergency Consultation', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 7200.00, [Description] = 'Rapid assessment and pain diagnosis for same-day acute dental complaints.'
    WHERE [ProcedureCode] = 'D0140E';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9110')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Severe Toothache Treatment', 'D9110', '45 mins', 140.00, 10500.00, 'Palliative treatment of dental pain, pulp debridement, or caries excavation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Severe Toothache Treatment', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 140.00, [DefaultFeePKR] = 10500.00, [Description] = 'Palliative treatment of dental pain, pulp debridement, or caries excavation.'
    WHERE [ProcedureCode] = 'D9110';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9999')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Dental Trauma Treatment', 'D9999', '60 mins', 240.00, 17500.00, 'Emergency stabilization of alveolar fracture, crown-root fracture, or luxation.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Dental Trauma Treatment', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 240.00, [DefaultFeePKR] = 17500.00, [Description] = 'Emergency stabilization of alveolar fracture, crown-root fracture, or luxation.'
    WHERE [ProcedureCode] = 'D9999';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2999')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Broken Tooth Treatment', 'D2999', '45 mins', 160.00, 12000.00, 'Emergency composite smoothing, bonding, and protective dressing on cracked tooth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Broken Tooth Treatment', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 160.00, [DefaultFeePKR] = 12000.00, [Description] = 'Emergency composite smoothing, bonding, and protective dressing on cracked tooth.'
    WHERE [ProcedureCode] = 'D2999';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7270')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Knocked-Out Tooth Management', 'D7270', '60 mins', 290.00, 21000.00, 'Emergency reimplantation and flexible wire splinting of avulsed permanent incisor.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Knocked-Out Tooth Management', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 290.00, [DefaultFeePKR] = 21000.00, [Description] = 'Emergency reimplantation and flexible wire splinting of avulsed permanent incisor.'
    WHERE [ProcedureCode] = 'D7270';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7510E')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Dental Abscess Treatment', 'D7510E', '30 mins', 130.00, 9500.00, 'Urgent drainage of fluctuant dentoalveolar abscess and antibiotic administration.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Dental Abscess Treatment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 130.00, [DefaultFeePKR] = 9500.00, [Description] = 'Urgent drainage of fluctuant dentoalveolar abscess and antibiotic administration.'
    WHERE [ProcedureCode] = 'D7510E';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9999S')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Swelling/Infection Treatment', 'D9999S', '30 mins', 125.00, 9000.00, 'Facial cellulitis triage, systemic antibiotic infusion / prescription, and referral protocol.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Swelling/Infection Treatment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 125.00, [DefaultFeePKR] = 9000.00, [Description] = 'Facial cellulitis triage, systemic antibiotic infusion / prescription, and referral protocol.'
    WHERE [ProcedureCode] = 'D9999S';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2940E')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Temporary Filling', 'D2940E', '20 mins', 60.00, 4600.00, 'Quick sedative provisional dressing for emergency pulp protection.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Temporary Filling', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 4600.00, [Description] = 'Quick sedative provisional dressing for emergency pulp protection.'
    WHERE [ProcedureCode] = 'D2940E';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2970E')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Emergency Dental Treatment', 'Temporary Crown', 'D2970E', '30 mins', 90.00, 6800.00, 'Emergency fabrication and cementation of protective provisional crown.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Emergency Dental Treatment', [ProcedureName] = 'Temporary Crown', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6800.00, [Description] = 'Emergency fabrication and cementation of protective provisional crown.'
    WHERE [ProcedureCode] = 'D2970E';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0470C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Crown Impression', 'D0470C', '30 mins', 70.00, 5200.00, 'Precision polyether / vinyl polysiloxane impression or 3D digital intraoral scan.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Crown Impression', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 70.00, [DefaultFeePKR] = 5200.00, [Description] = 'Precision polyether / vinyl polysiloxane impression or 3D digital intraoral scan.'
    WHERE [ProcedureCode] = 'D0470C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0470B')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Bridge Impression', 'D0470B', '30 mins', 85.00, 6200.00, 'Multi-abutment impression capturing margins, tissue cuff, and opposing arch.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Bridge Impression', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 85.00, [DefaultFeePKR] = 6200.00, [Description] = 'Multi-abutment impression capturing margins, tissue cuff, and opposing arch.'
    WHERE [ProcedureCode] = 'D0470B';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0470D')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Denture Impression', 'D0470D', '30 mins', 80.00, 5800.00, 'Primary alginate and secondary border-molded custom tray final impression.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Denture Impression', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 80.00, [DefaultFeePKR] = 5800.00, [Description] = 'Primary alginate and secondary border-molded custom tray final impression.'
    WHERE [ProcedureCode] = 'D0470D';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0471')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Bite Registration', 'D0471', '20 mins', 50.00, 3600.00, 'Elastomeric registration of centric relation and intercuspal dental occlusion.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Bite Registration', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 50.00, [DefaultFeePKR] = 3600.00, [Description] = 'Elastomeric registration of centric relation and intercuspal dental occlusion.'
    WHERE [ProcedureCode] = 'D0471';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2990C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Crown Try-in', 'D2990C', '30 mins', 60.00, 4200.00, 'Clinical verification of crown proximal contact, margin fit, shade match, and occlusion.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Crown Try-in', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 60.00, [DefaultFeePKR] = 4200.00, [Description] = 'Clinical verification of crown proximal contact, margin fit, shade match, and occlusion.'
    WHERE [ProcedureCode] = 'D2990C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2990B')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Bridge Try-in', 'D2990B', '30 mins', 75.00, 5200.00, 'Verification of framework passivity, pontic tissue contact, and aesthetics.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Bridge Try-in', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 75.00, [DefaultFeePKR] = 5200.00, [Description] = 'Verification of framework passivity, pontic tissue contact, and aesthetics.'
    WHERE [ProcedureCode] = 'D2990B';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2990D')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Denture Try-in', 'D2990D', '30 mins', 70.00, 5000.00, 'Wax try-in to confirm tooth arrangement, smile line, phonetics, and vertical dimension.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Denture Try-in', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 70.00, [DefaultFeePKR] = 5000.00, [Description] = 'Wax try-in to confirm tooth arrangement, smile line, phonetics, and vertical dimension.'
    WHERE [ProcedureCode] = 'D2990D';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2991C')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Crown Delivery', 'D2991C', '30 mins', 80.00, 5800.00, 'Permanent resin cementation, excess cleanup, and final post-insertion occlusal check.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Crown Delivery', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 80.00, [DefaultFeePKR] = 5800.00, [Description] = 'Permanent resin cementation, excess cleanup, and final post-insertion occlusal check.'
    WHERE [ProcedureCode] = 'D2991C';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D2991D')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Denture Delivery', 'D2991D', '30 mins', 90.00, 6200.00, 'Insertion of completed denture prosthesis, pressure paste check, and patient care guidance.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Denture Delivery', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 90.00, [DefaultFeePKR] = 6200.00, [Description] = 'Insertion of completed denture prosthesis, pressure paste check, and patient care guidance.'
    WHERE [ProcedureCode] = 'D2991D';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D5421')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Prosthetic / Laboratory Procedures', 'Prosthesis Adjustment', 'D5421', '30 mins', 55.00, 4000.00, 'Fine adjustment of prosthetic clasps, borders, and occlusal contact areas.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Prosthetic / Laboratory Procedures', [ProcedureName] = 'Prosthesis Adjustment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 55.00, [DefaultFeePKR] = 4000.00, [Description] = 'Fine adjustment of prosthetic clasps, borders, and occlusal contact areas.'
    WHERE [ProcedureCode] = 'D5421';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9210')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Local Anesthesia', 'D9210', '15 mins', 35.00, 2600.00, 'Administration of articaine / lidocaine infiltration or inferior alveolar nerve block.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Local Anesthesia', [EstimatedDuration] = '15 mins', [DefaultFeeNZD] = 35.00, [DefaultFeePKR] = 2600.00, [Description] = 'Administration of articaine / lidocaine infiltration or inferior alveolar nerve block.'
    WHERE [ProcedureCode] = 'D9210';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9222')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Sedation', 'D9222', '60 mins', 290.00, 21000.00, 'Conscious intravenous sedation or nitrous oxide inhalation for dental anxiety.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Sedation', [EstimatedDuration] = '60 mins', [DefaultFeeNZD] = 290.00, [DefaultFeePKR] = 21000.00, [Description] = 'Conscious intravenous sedation or nitrous oxide inhalation for dental anxiety.'
    WHERE [ProcedureCode] = 'D9222';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D4322')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Tooth Splinting', 'D4322', '45 mins', 180.00, 13000.00, 'Direct composite and wire splinting to stabilize hypermobile periodontally compromised teeth.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Tooth Splinting', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 180.00, [DefaultFeePKR] = 13000.00, [Description] = 'Direct composite and wire splinting to stabilize hypermobile periodontally compromised teeth.'
    WHERE [ProcedureCode] = 'D4322';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9951')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Occlusal Adjustment', 'D9951', '30 mins', 85.00, 6200.00, 'Selective coronoplasty to eliminate premature contacts and canine guidance interferences.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Occlusal Adjustment', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 85.00, [DefaultFeePKR] = 6200.00, [Description] = 'Selective coronoplasty to eliminate premature contacts and canine guidance interferences.'
    WHERE [ProcedureCode] = 'D9951';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D0160T')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'TMJ Examination', 'D0160T', '45 mins', 95.00, 7200.00, 'Evaluation of temporomandibular joint clicking, crepitus, deviation, and masticatory muscle tenderness.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'TMJ Examination', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 95.00, [DefaultFeePKR] = 7200.00, [Description] = 'Evaluation of temporomandibular joint clicking, crepitus, deviation, and masticatory muscle tenderness.'
    WHERE [ProcedureCode] = 'D0160T';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D7880')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'TMJ Treatment', 'D7880', '45 mins', 240.00, 17500.00, 'Occlusal splint stabilization therapy and jaw mobilization exercises.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'TMJ Treatment', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 240.00, [DefaultFeePKR] = 17500.00, [Description] = 'Occlusal splint stabilization therapy and jaw mobilization exercises.'
    WHERE [ProcedureCode] = 'D7880';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9945')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Bruxism Treatment', 'D9945', '45 mins', 230.00, 16000.00, 'Therapeutic injection of botulinum toxin into masseter muscles or splint management.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Bruxism Treatment', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 230.00, [DefaultFeePKR] = 16000.00, [Description] = 'Therapeutic injection of botulinum toxin into masseter muscles or splint management.'
    WHERE [ProcedureCode] = 'D9945';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9941')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Mouth Guard', 'D9941', '30 mins', 150.00, 11000.00, 'Custom sports impact protection mouth guard for athletic activities.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Mouth Guard', [EstimatedDuration] = '30 mins', [DefaultFeeNZD] = 150.00, [DefaultFeePKR] = 11000.00, [Description] = 'Custom sports impact protection mouth guard for athletic activities.'
    WHERE [ProcedureCode] = 'D9941';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D8210')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Dental Appliance', 'D8210', '45 mins', 260.00, 18500.00, 'Custom anti-snoring / mandibular advancement appliance for mild-to-moderate sleep apnea.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Dental Appliance', [EstimatedDuration] = '45 mins', [DefaultFeeNZD] = 260.00, [DefaultFeePKR] = 18500.00, [Description] = 'Custom anti-snoring / mandibular advancement appliance for mild-to-moderate sleep apnea.'
    WHERE [ProcedureCode] = 'D8210';
END
IF NOT EXISTS (SELECT 1 FROM [dentist].[MasterProcedureCatalog] WHERE [ProcedureCode] = 'D9430')
BEGIN
    INSERT INTO [dentist].[MasterProcedureCatalog] ([Category], [ProcedureName], [ProcedureCode], [EstimatedDuration], [DefaultFeeNZD], [DefaultFeePKR], [Description])
    VALUES ('Other Dental Services', 'Post-operative Follow-up', 'D9430', '20 mins', 35.00, 2600.00, 'Suture removal, socket healing inspection, and post-surgical recovery monitoring.');
END
ELSE
BEGIN
    UPDATE [dentist].[MasterProcedureCatalog]
    SET [Category] = 'Other Dental Services', [ProcedureName] = 'Post-operative Follow-up', [EstimatedDuration] = '20 mins', [DefaultFeeNZD] = 35.00, [DefaultFeePKR] = 2600.00, [Description] = 'Suture removal, socket healing inspection, and post-surgical recovery monitoring.'
    WHERE [ProcedureCode] = 'D9430';
END
GO
PRINT '>> [MasterProcedureCatalog] populated with 137 procedures.';
GO

-- Seed DoctorFeeSchedules for Doctors (1, 2, 3, 4)
DECLARE @DocID INT, @Region NVARCHAR(10), @Currency NVARCHAR(10);

DECLARE doc_cursor CURSOR FOR 
    SELECT DoctorID, ISNULL(Region, 'NZ') FROM [dentist].[Doctors];

OPEN doc_cursor;
FETCH NEXT FROM doc_cursor INTO @DocID, @Region;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Currency = CASE WHEN @Region = 'PK' THEN 'PKR' ELSE 'NZD' END;

    -- Merge all 137 procedures into DoctorFeeSchedules
    MERGE INTO [dentist].[DoctorFeeSchedules] AS target
    USING (
        SELECT 
            @DocID AS DoctorID,
            @Currency AS Currency,
            ProcedureCode,
            ProcedureName,
            Category,
            EstimatedDuration,
            CASE WHEN @Currency = 'PKR' THEN DefaultFeePKR ELSE DefaultFeeNZD END AS StandardFee,
            Description
        FROM [dentist].[MasterProcedureCatalog]
    ) AS source
    ON (target.DoctorID = source.DoctorID AND target.ProcedureCode = source.ProcedureCode)
    WHEN MATCHED THEN
        UPDATE SET 
            target.ProcedureName = source.ProcedureName,
            target.Category = source.Category,
            target.EstimatedDuration = source.EstimatedDuration,
            target.Description = source.Description,
            target.UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description, IsActive, CreatedAt, UpdatedAt)
        VALUES (source.DoctorID, source.Currency, source.ProcedureCode, source.ProcedureName, source.Category, source.EstimatedDuration, source.StandardFee, source.Description, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT '>> Seeded/Synchronized 137 procedures for Doctor ' + CAST(@DocID AS NVARCHAR(10)) + ' (' + @Currency + ')';

    FETCH NEXT FROM doc_cursor INTO @DocID, @Region;
END

CLOSE doc_cursor;
DEALLOCATE doc_cursor;
GO

PRINT '>> [SUCCESS] All 15 categories and 137 procedures successfully seeded and synchronized for all doctors!';
GO
