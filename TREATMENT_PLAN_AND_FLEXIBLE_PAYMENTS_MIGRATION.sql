-- ==============================================================================================
-- DENTIA CLINICAL & FINANCIAL RE-ARCHITECTURE MIGRATION
-- Single Source of Truth: TreatmentItems, Payment Allocations, Flexible Payments, & Catalog
-- Database: DentistAPI | Schema: dentist
-- ==============================================================================================

USE [DentistAPI];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '==================================================================';
PRINT 'BEGIN DENTIA TREATMENT PLAN & FLEXIBLE PAYMENTS MIGRATION';
PRINT '==================================================================';

-- ----------------------------------------------------------------------------------------------
-- 1. EXTEND [dentist].[TreatmentItems] (Single Source of Truth)
-- ----------------------------------------------------------------------------------------------
PRINT '1. Extending [dentist].[TreatmentItems]...';

IF OBJECT_ID('dentist.TreatmentItems', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dentist.TreatmentItems', 'CatalogID') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [CatalogID] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'Category') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [Category] NVARCHAR(100) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'Surface') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [Surface] NVARCHAR(50) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'Source') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [Source] NVARCHAR(30) NOT NULL CONSTRAINT DF_TreatmentItems_Source DEFAULT 'system';

    IF COL_LENGTH('dentist.TreatmentItems', 'SourceRef') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [SourceRef] NVARCHAR(100) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'DedupeKey') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [DedupeKey] NVARCHAR(150) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'EstimatedPrice') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [EstimatedPrice] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'Discount') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [Discount] INT NOT NULL CONSTRAINT DF_TreatmentItems_Discount DEFAULT 0;

    IF COL_LENGTH('dentist.TreatmentItems', 'FinalPrice') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [FinalPrice] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'PriceNote') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [PriceNote] NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'PlannedVisits') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [PlannedVisits] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'CreatedBy') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [CreatedBy] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'AcceptedAt') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [AcceptedAt] DATETIME2(7) NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'CompletedBy') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [CompletedBy] INT NULL;

    IF COL_LENGTH('dentist.TreatmentItems', 'ClosedReason') IS NULL
        ALTER TABLE [dentist].[TreatmentItems] ADD [ClosedReason] NVARCHAR(MAX) NULL;

    -- Drop any existing status check constraint if present
    IF OBJECT_ID('dentist.CK_TreatmentItems_Status', 'C') IS NOT NULL
        ALTER TABLE [dentist].[TreatmentItems] DROP CONSTRAINT [CK_TreatmentItems_Status];

    DECLARE @ckStatusName NVARCHAR(200);
    SELECT TOP 1 @ckStatusName = cc.name
    FROM sys.check_constraints cc
    JOIN sys.tables t ON cc.parent_object_id = t.object_id
    WHERE t.name = 'TreatmentItems' AND cc.definition LIKE '%Status%';

    IF @ckStatusName IS NOT NULL
        EXEC('ALTER TABLE [dentist].[TreatmentItems] DROP CONSTRAINT [' + @ckStatusName + ']');

    -- 1A. CRITICAL: Clean up and backfill existing rows BEFORE applying the new CHECK constraint
    EXEC('
        -- Normalize status strings
        UPDATE [dentist].[TreatmentItems] 
        SET [Status] = LOWER(LTRIM(RTRIM(COALESCE([Status], ''proposed''))));

        -- Map legacy statuses to new 7-state lifecycle
        UPDATE [dentist].[TreatmentItems] 
        SET [Status] = ''proposed'' 
        WHERE [Status] IN (''planned'', ''plan'', ''pending'');

        UPDATE [dentist].[TreatmentItems] 
        SET [Status] = ''completed'' 
        WHERE [Status] IN (''invoiced'', ''done'', ''finished'', ''complete'');

        -- Safe fallback for any unmapped or invalid legacy value
        UPDATE [dentist].[TreatmentItems] 
        SET [Status] = ''proposed'' 
        WHERE [Status] NOT IN (''draft'', ''proposed'', ''accepted'', ''in_progress'', ''completed'', ''declined'', ''cancelled'')
           OR [Status] IS NULL;

        -- Backfill prices and dedupe keys
        UPDATE [dentist].[TreatmentItems] 
        SET [EstimatedPrice] = CAST([Fee] AS INT), 
            [FinalPrice] = CAST([Fee] AS INT) 
        WHERE [FinalPrice] IS NULL AND [Fee] IS NOT NULL;

        UPDATE [dentist].[TreatmentItems] 
        SET [DedupeKey] = CONCAT([PatientID], ''_'', COALESCE([ProcedureCode], ''PROC''), ''_'', COALESCE([ToothNumber], 0)) 
        WHERE [DedupeKey] IS NULL;
    ');

    -- 1B. Now safely apply the new CHECK constraint on clean data
    ALTER TABLE [dentist].[TreatmentItems] WITH CHECK ADD CONSTRAINT CK_TreatmentItems_Status 
    CHECK ([Status] IN ('draft', 'proposed', 'accepted', 'in_progress', 'completed', 'declined', 'cancelled'));

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TreatmentItems_Active_Dedupe' AND object_id = OBJECT_ID('dentist.TreatmentItems'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_TreatmentItems_Active_Dedupe 
        ON [dentist].[TreatmentItems] ([PatientID], [DedupeKey])
        WHERE [Status] IN ('draft', 'proposed', 'accepted', 'in_progress');
    END
    PRINT '   + [dentist].[TreatmentItems] configuration complete.';
END
GO

-- ----------------------------------------------------------------------------------------------
-- 2. CREATE TABLE: [dentist].[treatment_catalog] (Directory / Modalities Catalog)
-- ----------------------------------------------------------------------------------------------
PRINT '2. Ensuring [dentist].[treatment_catalog] exists...';

IF OBJECT_ID('dentist.treatment_catalog', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[treatment_catalog] (
        [id]            INT IDENTITY(1,1) NOT NULL,
        [name]          NVARCHAR(255) NOT NULL,
        [category]      NVARCHAR(100) NOT NULL,
        [procedure_code] NVARCHAR(50) NULL,
        [default_price] INT NOT NULL CONSTRAINT DF_treatment_catalog_price DEFAULT 0,
        [default_visits] INT NULL,
        [is_active]     BIT NOT NULL CONSTRAINT DF_treatment_catalog_active DEFAULT 1,
        [created_at]    DATETIME2(7) NOT NULL CONSTRAINT DF_treatment_catalog_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_treatment_catalog PRIMARY KEY CLUSTERED ([id] ASC)
    );

    INSERT INTO [dentist].[treatment_catalog] ([name], [category], [procedure_code], [default_price], [default_visits], [is_active])
    VALUES
    ('General Dental Consultation', 'consultation', 'D0150', 5000, 1, 1),
    ('Teeth Whitening (Cosmetic Laser)', 'whitening', 'D9972', 25000, 3, 1),
    ('Braces (Orthodontics Full Arch)', 'ortho', 'D8080', 120000, 12, 1),
    ('Clear Aligners Therapy', 'ortho', 'D8090', 180000, 8, 1),
    ('Composite Resin Restoration (Filling)', 'restorative', 'D2391', 10000, 1, 1),
    ('Glass Ionomer (GIC) Restoration', 'restorative', 'D2140', 8000, 1, 1),
    ('Root Canal Therapy (RCT - Anterior)', 'endo', 'D3310', 15000, 2, 1),
    ('Root Canal Therapy (RCT - Molar)', 'endo', 'D3330', 25000, 3, 1),
    ('Porcelain Fused to Metal Crown', 'restorative', 'D2750', 22000, 2, 1),
    ('Full Ceramic / Zirconia Crown', 'restorative', 'D2740', 35000, 2, 1),
    ('Dental Scaling & Air Polishing', 'hygiene', 'D1110', 8000, 1, 1),
    ('Simple Tooth Extraction', 'surgery', 'D7140', 10000, 1, 1),
    ('Surgical Extraction (Wisdom Impaction)', 'surgery', 'D7210', 20000, 1, 1),
    ('Dental Implant Titanium Fixture', 'implant', 'D6010', 95000, 3, 1),
    ('Biopsy / Histopathology Requisition', 'surgery', 'D7286', 15000, 1, 1);

    PRINT '   + Created and seeded [dentist].[treatment_catalog]';
END
ELSE
BEGIN
    PRINT '   + Table [dentist].[treatment_catalog] already exists.';
END
GO

-- ----------------------------------------------------------------------------------------------
-- 3. CREATE TABLE: [dentist].[treatment_item_appointments] (Multi-Visit Linkage)
-- ----------------------------------------------------------------------------------------------
PRINT '3. Ensuring [dentist].[treatment_item_appointments] exists...';

IF OBJECT_ID('dentist.treatment_item_appointments', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[treatment_item_appointments] (
        [id]                BIGINT IDENTITY(1,1) NOT NULL,
        [treatment_item_id] BIGINT NOT NULL,
        [appointment_id]    INT NULL,
        [visit_id]          BIGINT NULL,
        [role]              NVARCHAR(30) NOT NULL CONSTRAINT DF_item_appt_role DEFAULT 'planned',
        [notes]             NVARCHAR(MAX) NULL,
        [created_at]        DATETIME2(7) NOT NULL CONSTRAINT DF_item_appt_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_treatment_item_appointments PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT FK_item_appt_item FOREIGN KEY ([treatment_item_id]) REFERENCES [dentist].[TreatmentItems]([TreatmentItemID]) ON DELETE CASCADE,
        CONSTRAINT CK_item_appt_role CHECK ([role] IN ('planned', 'performed'))
    );
    CREATE NONCLUSTERED INDEX IX_item_appt_item ON [dentist].[treatment_item_appointments] ([treatment_item_id]);
    PRINT '   + Created table [dentist].[treatment_item_appointments]';
END
ELSE
BEGIN
    PRINT '   + Table [dentist].[treatment_item_appointments] already exists.';
END
GO

-- ----------------------------------------------------------------------------------------------
-- 4. EXTEND [dentist].[Payments] (Voiding, Auditing, & Visit Linking)
-- ----------------------------------------------------------------------------------------------
PRINT '4. Extending [dentist].[Payments]...';

IF OBJECT_ID('dentist.Payments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dentist.Payments', 'ReceivedAt') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Payments] ADD [ReceivedAt] DATETIME2(7) NULL;
        EXEC('UPDATE [dentist].[Payments] SET [ReceivedAt] = [PaymentDate] WHERE [ReceivedAt] IS NULL');
    END

    IF COL_LENGTH('dentist.Payments', 'ReceivedBy') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Payments] ADD [ReceivedBy] INT NULL;
        EXEC('UPDATE [dentist].[Payments] SET [ReceivedBy] = [ReceivedByDoctorID] WHERE [ReceivedBy] IS NULL');
    END

    IF COL_LENGTH('dentist.Payments', 'AppointmentID') IS NULL
        ALTER TABLE [dentist].[Payments] ADD [AppointmentID] INT NULL;

    IF COL_LENGTH('dentist.Payments', 'VisitID') IS NULL
        ALTER TABLE [dentist].[Payments] ADD [VisitID] BIGINT NULL;

    IF COL_LENGTH('dentist.Payments', 'Status') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Payments] ADD [Status] NVARCHAR(30) NULL;
        EXEC('UPDATE [dentist].[Payments] SET [Status] = COALESCE([PaymentStatus], ''active'')');
        ALTER TABLE [dentist].[Payments] ALTER COLUMN [Status] NVARCHAR(30) NOT NULL;
    END

    IF COL_LENGTH('dentist.Payments', 'VoidReason') IS NULL
        ALTER TABLE [dentist].[Payments] ADD [VoidReason] NVARCHAR(500) NULL;

    IF COL_LENGTH('dentist.Payments', 'VoidedBy') IS NULL
        ALTER TABLE [dentist].[Payments] ADD [VoidedBy] INT NULL;

    IF COL_LENGTH('dentist.Payments', 'VoidedAt') IS NULL
        ALTER TABLE [dentist].[Payments] ADD [VoidedAt] DATETIME2(7) NULL;

    PRINT '   + [dentist].[Payments] extended.';
END
GO

-- ----------------------------------------------------------------------------------------------
-- 5. CREATE TABLE: [dentist].[payment_allocations] (Which Treatment Got How Much)
-- ----------------------------------------------------------------------------------------------
PRINT '5. Ensuring [dentist].[payment_allocations] exists...';

IF OBJECT_ID('dentist.payment_allocations', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[payment_allocations] (
        [id]                BIGINT IDENTITY(1,1) NOT NULL,
        [payment_id]        BIGINT NOT NULL,
        [treatment_item_id] BIGINT NOT NULL,
        [amount]            INT NOT NULL CONSTRAINT CK_alloc_amount CHECK ([amount] > 0),
        [created_by]        INT NULL,
        [created_at]        DATETIME2(7) NOT NULL CONSTRAINT DF_alloc_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_payment_allocations PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT FK_alloc_payment FOREIGN KEY ([payment_id]) REFERENCES [dentist].[Payments]([PaymentID]) ON DELETE CASCADE,
        CONSTRAINT FK_alloc_item FOREIGN KEY ([treatment_item_id]) REFERENCES [dentist].[TreatmentItems]([TreatmentItemID])
    );
    CREATE NONCLUSTERED INDEX IX_alloc_payment ON [dentist].[payment_allocations] ([payment_id]);
    CREATE NONCLUSTERED INDEX IX_alloc_item ON [dentist].[payment_allocations] ([treatment_item_id]);
    PRINT '   + Created table [dentist].[payment_allocations]';
END
ELSE
BEGIN
    PRINT '   + Table [dentist].[payment_allocations] already exists.';
END
GO

PRINT '==================================================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY';
PRINT '==================================================================';
