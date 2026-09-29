-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: NEW TABLES (IDEMPOTENT CREATION SCRIPT)
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Author          : Dentia Architecture Team
-- Date            : September 2026
-- Description     :
--   Checks if tables exist before creating them. Safe to execute repeatedly.
--   1. [dentist].[DoctorFeeSchedules]
--   2. [dentist].[Invoices]
--   3. [dentist].[InvoiceItems]
--   4. [dentist].[Payments]
-- =====================================================================================

USE [DentistAPI];
GO

-- 0. SCHEMA VERIFICATION
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> [SUCCESS] Schema [dentist] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Schema [dentist] already exists.';
END
GO


-- =====================================================================================
-- 1. DOCTOR FEE SCHEDULES TABLE
-- =====================================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'DoctorFeeSchedules' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[DoctorFeeSchedules] (
        [FeeScheduleID]     INT IDENTITY(1,1) NOT NULL,
        [DoctorID]          INT NOT NULL,
        [Currency]          NVARCHAR(10) NOT NULL CONSTRAINT DF_DoctorFeeSchedules_Currency DEFAULT 'NZD',
        [ProcedureCode]     NVARCHAR(50) NOT NULL,
        [ProcedureName]     NVARCHAR(255) NOT NULL,
        [Category]          NVARCHAR(100) NOT NULL,
        [EstimatedDuration] NVARCHAR(50) NOT NULL CONSTRAINT DF_DoctorFeeSchedules_Duration DEFAULT '45 mins',
        [StandardFee]       DECIMAL(18,2) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [IsActive]          BIT NOT NULL CONSTRAINT DF_DoctorFeeSchedules_IsActive DEFAULT 1,
        [CreatedAt]         DATETIME2 NOT NULL CONSTRAINT DF_DoctorFeeSchedules_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2 NOT NULL CONSTRAINT DF_DoctorFeeSchedules_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DoctorFeeSchedules PRIMARY KEY CLUSTERED ([FeeScheduleID] ASC),
        CONSTRAINT FK_DoctorFeeSchedules_Doctors FOREIGN KEY ([DoctorID]) 
            REFERENCES [dentist].[Doctors] ([DoctorID]) ON DELETE CASCADE
    );

    PRINT '>> [SUCCESS] Created table [dentist].[DoctorFeeSchedules].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[DoctorFeeSchedules] already exists.';
END
GO

-- Unique Index for (DoctorID, ProcedureCode)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_DoctorFeeSchedules_Doctor_Code' 
    AND object_id = OBJECT_ID('dentist.DoctorFeeSchedules')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_DoctorFeeSchedules_Doctor_Code
    ON [dentist].[DoctorFeeSchedules] ([DoctorID], [ProcedureCode]);

    PRINT '>> [SUCCESS] Created unique index [IX_DoctorFeeSchedules_Doctor_Code].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Unique index [IX_DoctorFeeSchedules_Doctor_Code] already exists.';
END
GO


-- =====================================================================================
-- 2. PATIENT MASTER INVOICES TABLE
-- =====================================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'Invoices' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[Invoices] (
        [InvoiceID]       BIGINT IDENTITY(1,1) NOT NULL,
        [InvoiceNumber]   NVARCHAR(50) NOT NULL,
        [PatientID]       INT NOT NULL,
        [DoctorID]        INT NULL,
        [AppointmentID]   INT NULL,
        [IssueDate]       DATE NOT NULL CONSTRAINT DF_Invoices_IssueDate DEFAULT CAST(GETDATE() AS DATE),
        [DueDate]         DATE NOT NULL CONSTRAINT DF_Invoices_DueDate DEFAULT DATEADD(DAY, 14, CAST(GETDATE() AS DATE)),
        [SubTotal]        DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [TaxAmount]       DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [DiscountAmount]  DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [TotalAmount]     DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [PaidAmount]      DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [BalanceAmount]   AS ([TotalAmount] - [PaidAmount]) PERSISTED,
        [Status]          NVARCHAR(30) NOT NULL DEFAULT 'Unpaid',
        [Currency]        NVARCHAR(10) NOT NULL DEFAULT 'NZD',
        [Notes]           NVARCHAR(MAX) NULL,
        [CreatedAt]       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED ([InvoiceID] ASC),
        CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE ([InvoiceNumber]),
        CONSTRAINT FK_Invoices_Patients FOREIGN KEY ([PatientID]) 
            REFERENCES [dentist].[Patients] ([PatientID]) ON DELETE CASCADE,
        CONSTRAINT FK_Invoices_Doctors FOREIGN KEY ([DoctorID]) 
            REFERENCES [dentist].[Doctors] ([DoctorID]) ON DELETE SET NULL
    );

    PRINT '>> [SUCCESS] Created table [dentist].[Invoices].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[Invoices] already exists.';
END
GO


-- =====================================================================================
-- 3. INVOICE LINE ITEMS TABLE
-- =====================================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'InvoiceItems' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[InvoiceItems] (
        [InvoiceItemID] BIGINT IDENTITY(1,1) NOT NULL,
        [InvoiceID]     BIGINT NOT NULL,
        [ProcedureCode] NVARCHAR(50) NULL,
        [Description]   NVARCHAR(255) NOT NULL,
        [ToothNumber]   INT NULL,
        [Quantity]      INT NOT NULL DEFAULT 1,
        [UnitPrice]     DECIMAL(18,2) NOT NULL,
        [TotalPrice]    AS ([Quantity] * [UnitPrice]) PERSISTED,
        [CreatedAt]     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_InvoiceItems PRIMARY KEY CLUSTERED ([InvoiceItemID] ASC),
        CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY ([InvoiceID]) 
            REFERENCES [dentist].[Invoices] ([InvoiceID]) ON DELETE CASCADE
    );

    PRINT '>> [SUCCESS] Created table [dentist].[InvoiceItems].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[InvoiceItems] already exists.';
END
GO


-- =====================================================================================
-- 4. PAYMENTS & CASH VOUCHERS TABLE
-- =====================================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'Payments' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[Payments] (
        [PaymentID]            BIGINT IDENTITY(1,1) NOT NULL,
        [PaymentReceiptNo]     NVARCHAR(50) NOT NULL,
        [InvoiceID]            BIGINT NOT NULL,
        [PatientID]            INT NOT NULL,
        [Amount]               DECIMAL(18,2) NOT NULL,
        [PaymentMethod]        NVARCHAR(30) NOT NULL,
        [PaymentStatus]        NVARCHAR(30) NOT NULL DEFAULT 'Completed',
        [TransactionReference] NVARCHAR(100) NULL,
        [PaymentGateway]       NVARCHAR(50) NULL,
        [CashVoucherCode]      NVARCHAR(50) NULL,
        [ReceivedByDoctorID]   INT NULL,
        [PaymentDate]          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Notes]                NVARCHAR(MAX) NULL,

        CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED ([PaymentID] ASC),
        CONSTRAINT UQ_Payments_ReceiptNo UNIQUE ([PaymentReceiptNo]),
        CONSTRAINT FK_Payments_Invoices FOREIGN KEY ([InvoiceID]) 
            REFERENCES [dentist].[Invoices] ([InvoiceID]) ON DELETE CASCADE,
        CONSTRAINT FK_Payments_Patients FOREIGN KEY ([PatientID]) 
            REFERENCES [dentist].[Patients] ([PatientID])
    );

    PRINT '>> [SUCCESS] Created table [dentist].[Payments].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[Payments] already exists.';
END
GO

PRINT '>> [COMPLETE] All tables and indexes verified successfully.';
GO
