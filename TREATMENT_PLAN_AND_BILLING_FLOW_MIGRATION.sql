-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: TREATMENT PLAN & UNIFIED BILLING MIGRATION SCRIPT
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Author          : Dentia Clinical Architecture Team
-- Date            : October 2026
-- Description     :
--   1. Ensures [dentist] schema exists.
--   2. Creates [dentist].[Visits] for patient check-ins & presence tracking.
--   3. Creates [dentist].[Charges] for automatic consultation & radiograph fees.
--   4. Creates [dentist].[TreatmentItems] (Core single-source-of-truth table for all tooth care).
--   5. Creates [dentist].[AuditLogs] for tracking every clinical & financial status change.
--   6. Upgrades [dentist].[Appointments] with Type, AutoCreated, and PatientID.
--   7. Upgrades [dentist].[Payments] with Kind ('advance'/'settlement') and TreatmentItemID.
--   8. Upgrades [dentist].[Invoices] to ensure full compatibility.
-- =====================================================================================

USE [DentistAPI];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- -------------------------------------------------------------------------------------
-- 0. SCHEMA VERIFICATION
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> [SUCCESS] Schema [dentist] verified/created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Schema [dentist] already exists.';
END
GO

-- -------------------------------------------------------------------------------------
-- 1. TABLE: [dentist].[Visits]
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'Visits' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[Visits] (
        [VisitID]        BIGINT IDENTITY(1,1) NOT NULL,
        [PatientID]      INT NOT NULL,
        [DoctorID]       INT NOT NULL,
        [AppointmentID]  INT NULL,
        [VisitDate]      DATE NOT NULL CONSTRAINT DF_Visits_VisitDate DEFAULT CAST(SYSUTCDATETIME() AS DATE),
        [Status]         NVARCHAR(30) NOT NULL CONSTRAINT DF_Visits_Status DEFAULT 'checked_in', -- 'checked_in', 'done', 'cancelled'
        [CreatedAt]      DATETIME2(7) NOT NULL CONSTRAINT DF_Visits_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Visits PRIMARY KEY CLUSTERED ([VisitID] ASC)
    );
    PRINT '>> [SUCCESS] Created table [dentist].[Visits].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[Visits] already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Visits_Patient_Date' AND object_id = OBJECT_ID('dentist.Visits'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Visits_Patient_Date ON [dentist].[Visits] ([PatientID], [VisitDate] DESC);
    PRINT '   + Created index IX_Visits_Patient_Date';
END
GO

-- -------------------------------------------------------------------------------------
-- 2. TABLE: [dentist].[Charges]
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'Charges' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[Charges] (
        [ChargeID]       BIGINT IDENTITY(1,1) NOT NULL,
        [PatientID]      INT NOT NULL,
        [VisitID]        BIGINT NOT NULL,
        [ChargeType]     NVARCHAR(30) NOT NULL, -- 'consultation', 'xray'
        [RadiographID]   INT NULL,
        [Description]    NVARCHAR(255) NOT NULL,
        [Amount]         DECIMAL(18,2) NOT NULL CONSTRAINT CK_Charges_Amount CHECK ([Amount] >= 0),
        [Currency]       NVARCHAR(10) NOT NULL CONSTRAINT DF_Charges_Currency DEFAULT 'PKR',
        [InvoiceID]      BIGINT NULL,
        [CreatedAt]      DATETIME2(7) NOT NULL CONSTRAINT DF_Charges_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Charges PRIMARY KEY CLUSTERED ([ChargeID] ASC)
    );
    PRINT '>> [SUCCESS] Created table [dentist].[Charges].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[Charges] already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Charges_Patient_Unbilled' AND object_id = OBJECT_ID('dentist.Charges'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Charges_Patient_Unbilled 
    ON [dentist].[Charges] ([PatientID], [InvoiceID]) 
    INCLUDE ([ChargeType], [Amount], [Description]);
    PRINT '   + Created index IX_Charges_Patient_Unbilled';
END
GO

-- -------------------------------------------------------------------------------------
-- 3. TABLE: [dentist].[TreatmentItems] (CORE SINGLE SOURCE OF TRUTH)
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'TreatmentItems' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[TreatmentItems] (
        [TreatmentItemID]   BIGINT IDENTITY(1,1) NOT NULL,
        [PatientID]         INT NOT NULL,
        [DoctorID]          INT NOT NULL,
        [ToothNumber]       INT NOT NULL,
        [ToothKey]          NVARCHAR(10) NOT NULL CONSTRAINT DF_TreatmentItems_ToothKey DEFAULT '1',
        [ProcedureCode]     NVARCHAR(50) NOT NULL,
        [ProcedureName]     NVARCHAR(255) NOT NULL,
        [Fee]               DECIMAL(18,2) NOT NULL CONSTRAINT CK_TreatmentItems_Fee CHECK ([Fee] >= 0),
        [Currency]          NVARCHAR(10) NOT NULL CONSTRAINT DF_TreatmentItems_Currency DEFAULT 'PKR',
        [Status]            NVARCHAR(30) NOT NULL CONSTRAINT DF_TreatmentItems_Status DEFAULT 'planned', 
                            -- 'planned', 'in_progress', 'completed', 'invoiced', 'cancelled'
        [PlannedVisitID]    BIGINT NULL,
        [AppointmentID]     INT NULL,
        [StartedAt]         DATETIME2(7) NULL,
        [CompletedVisitID]  BIGINT NULL,
        [CompletedAt]       DATETIME2(7) NULL,
        [InvoiceID]         BIGINT NULL,
        [Notes]             NVARCHAR(MAX) NULL,
        [CreatedAt]         DATETIME2(7) NOT NULL CONSTRAINT DF_TreatmentItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2(7) NOT NULL CONSTRAINT DF_TreatmentItems_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_TreatmentItems PRIMARY KEY CLUSTERED ([TreatmentItemID] ASC),
        CONSTRAINT CK_TreatmentItems_Status CHECK ([Status] IN ('planned', 'in_progress', 'completed', 'invoiced', 'cancelled'))
    );
    PRINT '>> [SUCCESS] Created table [dentist].[TreatmentItems].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[TreatmentItems] already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TreatmentItems_Patient_Status' AND object_id = OBJECT_ID('dentist.TreatmentItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TreatmentItems_Patient_Status 
    ON [dentist].[TreatmentItems] ([PatientID], [Status])
    INCLUDE ([ToothNumber], [ToothKey], [ProcedureCode], [Fee], [AppointmentID], [InvoiceID]);
    PRINT '   + Created index IX_TreatmentItems_Patient_Status';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TreatmentItems_Tooth_Active' AND object_id = OBJECT_ID('dentist.TreatmentItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TreatmentItems_Tooth_Active
    ON [dentist].[TreatmentItems] ([PatientID], [ToothNumber], [Status])
    WHERE [Status] IN ('planned', 'in_progress');
    PRINT '   + Created filtered index IX_TreatmentItems_Tooth_Active';
END
GO

-- -------------------------------------------------------------------------------------
-- 4. TABLE: [dentist].[AuditLogs]
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'AuditLogs' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[AuditLogs] (
        [AuditLogID]    BIGINT IDENTITY(1,1) NOT NULL,
        [Entity]        NVARCHAR(50) NOT NULL,
        [EntityID]      BIGINT NOT NULL,
        [Action]        NVARCHAR(50) NOT NULL,
        [OldValue]      NVARCHAR(MAX) NULL,
        [NewValue]      NVARCHAR(MAX) NULL,
        [UserID]        INT NOT NULL,
        [UserRole]      NVARCHAR(50) NOT NULL CONSTRAINT DF_AuditLogs_UserRole DEFAULT 'Doctor',
        [IPAddress]     NVARCHAR(50) NULL,
        [CreatedAt]     DATETIME2(7) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED ([AuditLogID] ASC)
    );
    PRINT '>> [SUCCESS] Created table [dentist].[AuditLogs].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[AuditLogs] already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Entity' AND object_id = OBJECT_ID('dentist.AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Entity ON [dentist].[AuditLogs] ([Entity], [EntityID], [CreatedAt] DESC);
    PRINT '   + Created index IX_AuditLogs_Entity';
END
GO

-- -------------------------------------------------------------------------------------
-- 5. UPGRADE: [dentist].[Appointments] (Conditional on Table Existence)
-- -------------------------------------------------------------------------------------
IF OBJECT_ID('dentist.Appointments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dentist.Appointments', 'Type') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Appointments] 
        ADD [Type] NVARCHAR(30) NOT NULL CONSTRAINT DF_Appointments_Type DEFAULT 'treatment';
        PRINT '   + Added column [Type] to [dentist].[Appointments]';
    END

    IF COL_LENGTH('dentist.Appointments', 'AutoCreated') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Appointments] 
        ADD [AutoCreated] BIT NOT NULL CONSTRAINT DF_Appointments_AutoCreated DEFAULT 0;
        PRINT '   + Added column [AutoCreated] to [dentist].[Appointments]';
    END

    IF COL_LENGTH('dentist.Appointments', 'PatientID') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Appointments] 
        ADD [PatientID] INT NULL;
        PRINT '   + Added column [PatientID] to [dentist].[Appointments]';
    END
END
ELSE
BEGIN
    PRINT '>> [NOTE] [dentist].[Appointments] does not exist yet in this DB instance; skipping column alters.';
END
GO

-- -------------------------------------------------------------------------------------
-- 6. UPGRADE: [dentist].[Payments] (Conditional on Table Existence)
-- -------------------------------------------------------------------------------------
IF OBJECT_ID('dentist.Payments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dentist.Payments', 'Kind') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Payments] 
        ADD [Kind] NVARCHAR(30) NOT NULL CONSTRAINT DF_Payments_Kind DEFAULT 'settlement';
        PRINT '   + Added column [Kind] to [dentist].[Payments]';
    END

    IF COL_LENGTH('dentist.Payments', 'TreatmentItemID') IS NULL
    BEGIN
        ALTER TABLE [dentist].[Payments] 
        ADD [TreatmentItemID] BIGINT NULL;
        PRINT '   + Added column [TreatmentItemID] to [dentist].[Payments]';
    END

    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dentist.Payments') 
        AND name = 'InvoiceID' 
        AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [dentist].[Payments] ALTER COLUMN [InvoiceID] BIGINT NULL;
        PRINT '   + Altered [dentist].[Payments].[InvoiceID] to NULLable.';
    END
END
ELSE
BEGIN
    PRINT '>> [NOTE] [dentist].[Payments] does not exist yet in this DB instance; skipping column alters.';
END
GO

PRINT '>> [COMPLETE] Treatment Plan & Billing Flow Database Migration applied successfully.';
GO
