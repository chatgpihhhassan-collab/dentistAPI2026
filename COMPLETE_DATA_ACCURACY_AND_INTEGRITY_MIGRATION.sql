-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM - COMPLETE DATA ACCURACY & INTEGRITY MIGRATION
-- Database Engine: Microsoft SQL Server 2017 / 2019 / 2022 / Azure SQL
-- Schema: dentist
-- Author: Full-Stack Clinical Data Architecture Team
-- Date: October 2026
-- Description:
--   This comprehensive, idempotent migration script enforces database-level data accuracy,
--   normalization, and domain integrity across all core entities in Dentia:
--
--   PHASE 1: DIAGNOSTIC AUDIT (Identifies pre-existing dirty/malformed records)
--   PHASE 2: DATA SANITIZATION & NORMALIZATION (Cleanses existing data before adding constraints)
--     - Pakistani Phone normalization (+923XXXXXXXXX)
--     - Gender standardization ('male', 'female', 'other')
--     - Dentition category alignment ('Adult', 'Pediatric', 'Mixed')
--     - Orphaned foreign key remediation (Appointments -> Doctors)
--     - Invoice & Payment negative amount remediation
--   PHASE 3: DOMAIN CHECK CONSTRAINTS & DEFAULT INTEGRITY
--     - CK_Patients_Gender, CK_Patients_DentitionType, CK_Patients_DOB
--     - CK_Appointments_Status, CK_Invoices_Amounts, CK_Payments_Amount
--     - CK_InvoiceItems_QuantityUnitPrice
--   PHASE 4: COLLISION PREVENTION & FILTERED UNIQUE CONSTRAINTS
--     - Prevents doctor appointment double-booking at the exact same minute
--     - (Uses SQL Server compliant IN operator for filtered index predicate)
--   PHASE 5: FOREIGN KEY SAFEGUARDS & PERFORMANCE INDEXING
--     - FK_Appointments_Doctors, FK_TeethState_Patients, FK_TreatmentHistory_Patients
--     - Indexes on foreign keys for fast joins and collision checks
-- =====================================================================================

USE [DentistAPI];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '======================================================================';
PRINT '  STARTING DENTIA DATA ACCURACY & INTEGRITY MIGRATION';
PRINT '======================================================================';
GO

-- 0. Ensure schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> Schema [dentist] verified/created.';
END
GO

-- =====================================================================================
-- PHASE 1: DIAGNOSTIC AUDIT (Non-destructive inspection)
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 1: DIAGNOSTIC AUDIT (Inspecting database for anomalies)...';
PRINT '----------------------------------------------------------------------';

-- Audit 1: Patients with non-E.164 phone formats
SELECT 
    PatientID, 
    FirstName, 
    LastName, 
    Phone, 
    'Phone needs E.164 normalization (+923...)' AS AuditNote
FROM [dentist].[Patients]
WHERE Phone IS NOT NULL 
  AND Phone NOT LIKE '+923[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';

-- Audit 2: Patients with irregular Gender values
SELECT 
    PatientID, 
    FirstName, 
    LastName, 
    Gender, 
    'Non-standard gender value (will normalize to male/female/other)' AS AuditNote
FROM [dentist].[Patients]
WHERE Gender IS NULL 
   OR LOWER(LTRIM(RTRIM(Gender))) NOT IN ('male', 'female', 'other');

-- Audit 3: Patients with irregular DentitionType values
SELECT 
    PatientID, 
    FirstName, 
    LastName, 
    DentitionType, 
    'Non-standard dentition type (will normalize to Adult/Pediatric/Mixed)' AS AuditNote
FROM [dentist].[Patients]
WHERE DentitionType IS NULL 
   OR DentitionType NOT IN ('Adult', 'Pediatric', 'Mixed');

-- Audit 4: Appointments with invalid statuses
SELECT 
    AppointmentID, 
    FullName, 
    PreferredDate, 
    Status, 
    'Unrecognized appointment status (will normalize to Pending)' AS AuditNote
FROM [dentist].[Appointments]
WHERE Status IS NULL 
   OR Status NOT IN ('Pending', 'Confirmed', 'In-Progress', 'Completed', 'Cancelled', 'No-Show', 'Rejected');

-- Audit 5: Appointments with orphaned or invalid DoctorID
IF OBJECT_ID('dentist.Doctors', 'U') IS NOT NULL
BEGIN
    SELECT 
        A.AppointmentID, 
        A.FullName, 
        A.DoctorID, 
        'Orphaned DoctorID (does not exist in dentist.Doctors table)' AS AuditNote
    FROM [dentist].[Appointments] A
    LEFT JOIN [dentist].[Doctors] D ON A.DoctorID = D.DoctorID
    WHERE A.DoctorID IS NOT NULL AND (A.DoctorID <= 0 OR D.DoctorID IS NULL);
END

-- Audit 6: Invoices with negative amounts
IF OBJECT_ID('dentist.Invoices', 'U') IS NOT NULL
BEGIN
    SELECT 
        InvoiceID, 
        InvoiceNumber, 
        SubTotal, 
        TotalAmount, 
        PaidAmount, 
        'Negative billing values detected' AS AuditNote
    FROM [dentist].[Invoices]
    WHERE SubTotal < 0 OR TotalAmount < 0 OR PaidAmount < 0;
END
GO

-- =====================================================================================
-- PHASE 2: DATA SANITIZATION & NORMALIZATION (Data Cleaning)
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 2: DATA SANITIZATION & NORMALIZATION...';
PRINT '----------------------------------------------------------------------';

BEGIN TRANSACTION;

-- 2.1 Clean and normalize Phone numbers in [dentist].[Patients]
PRINT '   -> Normalizing [dentist].[Patients].[Phone] to Pakistani E.164...';

-- Remove spaces, dashes, parentheses
UPDATE [dentist].[Patients]
SET [Phone] = REPLACE(REPLACE(REPLACE(REPLACE([Phone], ' ', ''), '-', ''), '(', ''), ')', '')
WHERE [Phone] IS NOT NULL;

-- Convert 03XXXXXXXXX to +923XXXXXXXXX
UPDATE [dentist].[Patients]
SET [Phone] = '+92' + SUBSTRING([Phone], 2, LEN([Phone]) - 1)
WHERE [Phone] LIKE '03%' AND LEN([Phone]) = 11;

-- Convert 923XXXXXXXXX to +923XXXXXXXXX
UPDATE [dentist].[Patients]
SET [Phone] = '+' + [Phone]
WHERE [Phone] LIKE '923%' AND LEN([Phone]) = 12;

-- If phone is empty or whitespace, set to default clinic helpline format
UPDATE [dentist].[Patients]
SET [Phone] = '+923000000000'
WHERE [Phone] IS NULL OR LTRIM(RTRIM([Phone])) = '';

PRINT '      Done.';

-- 2.2 Clean and normalize Phone numbers in [dentist].[Appointments]
PRINT '   -> Normalizing [dentist].[Appointments].[Phone]...';

UPDATE [dentist].[Appointments]
SET [Phone] = REPLACE(REPLACE(REPLACE(REPLACE([Phone], ' ', ''), '-', ''), '(', ''), ')', '')
WHERE [Phone] IS NOT NULL;

UPDATE [dentist].[Appointments]
SET [Phone] = '+92' + SUBSTRING([Phone], 2, LEN([Phone]) - 1)
WHERE [Phone] LIKE '03%' AND LEN([Phone]) = 11;

UPDATE [dentist].[Appointments]
SET [Phone] = '+' + [Phone]
WHERE [Phone] LIKE '923%' AND LEN([Phone]) = 12;

PRINT '      Done.';

-- 2.3 Normalize Genders in [dentist].[Patients]
PRINT '   -> Normalizing [dentist].[Patients].[Gender]...';

UPDATE [dentist].[Patients]
SET [Gender] = LOWER(LTRIM(RTRIM([Gender])))
WHERE [Gender] IS NOT NULL;

UPDATE [dentist].[Patients]
SET [Gender] = 'male'
WHERE [Gender] IN ('m', 'man', 'boy');

UPDATE [dentist].[Patients]
SET [Gender] = 'female'
WHERE [Gender] IN ('f', 'woman', 'girl');

UPDATE [dentist].[Patients]
SET [Gender] = 'other'
WHERE [Gender] IS NULL 
   OR [Gender] NOT IN ('male', 'female', 'other');

PRINT '      Done.';

-- 2.4 Normalize DentitionType in [dentist].[Patients]
PRINT '   -> Normalizing [dentist].[Patients].[DentitionType]...';

UPDATE [dentist].[Patients]
SET [DentitionType] = 'Adult'
WHERE [DentitionType] IS NULL 
   OR LTRIM(RTRIM([DentitionType])) = ''
   OR [DentitionType] NOT IN ('Adult', 'Pediatric', 'Mixed');

-- Auto-align pediatric dentition if patient is under 13 years old
UPDATE [dentist].[Patients]
SET [DentitionType] = 'Pediatric'
WHERE DATEDIFF(YEAR, [DOB], GETDATE()) < 13
  AND [DentitionType] = 'Adult';

PRINT '      Done.';

-- 2.5 Normalize Appointment Statuses in [dentist].[Appointments]
PRINT '   -> Normalizing [dentist].[Appointments].[Status]...';

UPDATE [dentist].[Appointments]
SET [Status] = 'Pending'
WHERE [Status] IS NULL 
   OR LTRIM(RTRIM([Status])) = ''
   OR [Status] NOT IN ('Pending', 'Confirmed', 'In-Progress', 'Completed', 'Cancelled', 'No-Show', 'Rejected');

PRINT '      Done.';

-- 2.6 Remediate Orphaned DoctorIDs in [dentist].[Appointments]
PRINT '   -> Remediating orphaned DoctorIDs in [dentist].[Appointments]...';

IF OBJECT_ID('dentist.Doctors', 'U') IS NOT NULL
BEGIN
    UPDATE A
    SET A.DoctorID = NULL
    FROM [dentist].[Appointments] A
    LEFT JOIN [dentist].[Doctors] D ON A.DoctorID = D.DoctorID
    WHERE A.DoctorID IS NOT NULL AND (A.DoctorID <= 0 OR D.DoctorID IS NULL);
END

PRINT '      Done.';

-- 2.7 Sanitize Invoices & Payments amounts (if tables exist)
IF OBJECT_ID('dentist.Invoices', 'U') IS NOT NULL
BEGIN
    PRINT '   -> Sanitizing billing monetary values...';

    UPDATE [dentist].[Invoices]
    SET [SubTotal] = 0.00
    WHERE [SubTotal] < 0;

    UPDATE [dentist].[Invoices]
    SET [DiscountAmount] = 0.00
    WHERE [DiscountAmount] < 0;

    UPDATE [dentist].[Invoices]
    SET [TaxAmount] = 0.00
    WHERE [TaxAmount] < 0;

    UPDATE [dentist].[Invoices]
    SET [TotalAmount] = [SubTotal] - [DiscountAmount] + [TaxAmount]
    WHERE [TotalAmount] < 0;

    UPDATE [dentist].[Invoices]
    SET [PaidAmount] = 0.00
    WHERE [PaidAmount] < 0;

    PRINT '      Done.';
END

IF OBJECT_ID('dentist.Payments', 'U') IS NOT NULL
BEGIN
    UPDATE [dentist].[Payments]
    SET [Amount] = 1.00
    WHERE [Amount] <= 0;
END

COMMIT TRANSACTION;
PRINT '>> PHASE 2 COMPLETED SUCCESSFULLY.';
GO

-- =====================================================================================
-- PHASE 3: DOMAIN CHECK CONSTRAINTS & INTEGRITY RULES
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 3: DOMAIN CHECK CONSTRAINTS...';
PRINT '----------------------------------------------------------------------';

-- 3.1 Patient Gender Check Constraint
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Patients_Gender' AND parent_object_id = OBJECT_ID('dentist.Patients'))
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD CONSTRAINT [CK_Patients_Gender] 
    CHECK ([Gender] IN ('male', 'female', 'other'));
    PRINT '   + Added constraint [CK_Patients_Gender]';
END
ELSE
BEGIN
    PRINT '   - Constraint [CK_Patients_Gender] already exists.';
END
GO

-- 3.2 Patient DentitionType Check Constraint
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Patients_DentitionType' AND parent_object_id = OBJECT_ID('dentist.Patients'))
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD CONSTRAINT [CK_Patients_DentitionType] 
    CHECK ([DentitionType] IN ('Adult', 'Pediatric', 'Mixed'));
    PRINT '   + Added constraint [CK_Patients_DentitionType]';
END
ELSE
BEGIN
    PRINT '   - Constraint [CK_Patients_DentitionType] already exists.';
END
GO

-- 3.3 Patient DOB Realistic Bounds (DOB cannot be in future, and max 125 years old)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Patients_DOB' AND parent_object_id = OBJECT_ID('dentist.Patients'))
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD CONSTRAINT [CK_Patients_DOB] 
    CHECK ([DOB] <= SYSUTCDATETIME() AND [DOB] >= DATEADD(YEAR, -125, SYSUTCDATETIME()));
    PRINT '   + Added constraint [CK_Patients_DOB]';
END
ELSE
BEGIN
    PRINT '   - Constraint [CK_Patients_DOB] already exists.';
END
GO

-- 3.4 Appointment Status Whitelist Check Constraint
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Appointments_Status' AND parent_object_id = OBJECT_ID('dentist.Appointments'))
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD CONSTRAINT [CK_Appointments_Status] 
    CHECK ([Status] IN ('Pending', 'Confirmed', 'In-Progress', 'Completed', 'Cancelled', 'No-Show', 'Rejected'));
    PRINT '   + Added constraint [CK_Appointments_Status]';
END
ELSE
BEGIN
    PRINT '   - Constraint [CK_Appointments_Status] already exists.';
END
GO

-- 3.5 Billing Invoices Financial Values Integrity
IF OBJECT_ID('dentist.Invoices', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Invoices_Amounts' AND parent_object_id = OBJECT_ID('dentist.Invoices'))
    BEGIN
        ALTER TABLE [dentist].[Invoices]
        ADD CONSTRAINT [CK_Invoices_Amounts]
        CHECK ([SubTotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0 AND [PaidAmount] >= 0);
        PRINT '   + Added constraint [CK_Invoices_Amounts]';
    END
    ELSE
    BEGIN
        PRINT '   - Constraint [CK_Invoices_Amounts] already exists.';
    END
END
GO

-- 3.6 Payments Positive Amount Constraint
IF OBJECT_ID('dentist.Payments', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Payments_Amount' AND parent_object_id = OBJECT_ID('dentist.Payments'))
    BEGIN
        ALTER TABLE [dentist].[Payments]
        ADD CONSTRAINT [CK_Payments_Amount]
        CHECK ([Amount] > 0);
        PRINT '   + Added constraint [CK_Payments_Amount]';
    END
    ELSE
    BEGIN
        PRINT '   - Constraint [CK_Payments_Amount] already exists.';
    END
END
GO

-- 3.7 Invoice Items Quantity & UnitPrice Integrity
IF OBJECT_ID('dentist.InvoiceItems', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_InvoiceItems_QuantityUnitPrice' AND parent_object_id = OBJECT_ID('dentist.InvoiceItems'))
    BEGIN
        ALTER TABLE [dentist].[InvoiceItems]
        ADD CONSTRAINT [CK_InvoiceItems_QuantityUnitPrice]
        CHECK ([Quantity] >= 1 AND [UnitPrice] >= 0);
        PRINT '   + Added constraint [CK_InvoiceItems_QuantityUnitPrice]';
    END
    ELSE
    BEGIN
        PRINT '   - Constraint [CK_InvoiceItems_QuantityUnitPrice] already exists.';
    END
END
GO

-- =====================================================================================
-- PHASE 4: COLLISION PREVENTION & FILTERED UNIQUE CONSTRAINTS
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 4: COLLISION PREVENTION & FILTERED UNIQUE CONSTRAINTS...';
PRINT '----------------------------------------------------------------------';

-- 4.1 Filtered Unique Index on Appointments (Prevent Doctor Double Booking at the exact same minute)
-- Note: SQL Server filtered indexes require positive IN (...) predicate, NOT IN is prohibited.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Appointments_Doctor_PreferredDate' AND object_id = OBJECT_ID('dentist.Appointments'))
BEGIN
    -- First verify no duplicate active appointments exist before creating unique index
    ;WITH DuplicateCTE AS (
        SELECT 
            AppointmentID,
            DoctorID,
            PreferredDate,
            ROW_NUMBER() OVER (
                PARTITION BY DoctorID, PreferredDate 
                ORDER BY AppointmentID ASC
            ) AS RowNum
        FROM [dentist].[Appointments]
        WHERE DoctorID IS NOT NULL 
          AND PreferredDate IS NOT NULL 
          AND Status IN ('Pending', 'Confirmed', 'In-Progress')
    )
    -- Shift duplicate appointments forward by 30 mins to resolve collisions cleanly
    UPDATE A
    SET A.PreferredDate = DATEADD(MINUTE, 30 * (D.RowNum - 1), A.PreferredDate)
    FROM [dentist].[Appointments] A
    INNER JOIN DuplicateCTE D ON A.AppointmentID = D.AppointmentID
    WHERE D.RowNum > 1;

    CREATE UNIQUE NONCLUSTERED INDEX [UX_Appointments_Doctor_PreferredDate]
    ON [dentist].[Appointments]([DoctorID], [PreferredDate])
    WHERE [DoctorID] IS NOT NULL 
      AND [PreferredDate] IS NOT NULL 
      AND [Status] IN ('Pending', 'Confirmed', 'In-Progress');

    PRINT '   + Created Unique Filtered Index [UX_Appointments_Doctor_PreferredDate] (Double-booking prevention)';
END
ELSE
BEGIN
    PRINT '   - Index [UX_Appointments_Doctor_PreferredDate] already exists.';
END
GO

-- =====================================================================================
-- PHASE 5: FOREIGN KEY SAFEGUARDS & JOIN PERFORMANCE INDEXES
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 5: FOREIGN KEY SAFEGUARDS & PERFORMANCE INDEXING...';
PRINT '----------------------------------------------------------------------';

-- 5.1 FK from Appointments to Doctors
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Appointments_Doctors' AND parent_object_id = OBJECT_ID('dentist.Appointments'))
BEGIN
    IF OBJECT_ID('dentist.Doctors', 'U') IS NOT NULL
    BEGIN
        -- Remediate any remaining orphaned DoctorIDs before adding foreign key
        UPDATE A
        SET A.DoctorID = NULL
        FROM [dentist].[Appointments] A
        LEFT JOIN [dentist].[Doctors] D ON A.DoctorID = D.DoctorID
        WHERE A.DoctorID IS NOT NULL AND (A.DoctorID <= 0 OR D.DoctorID IS NULL);

        ALTER TABLE [dentist].[Appointments] WITH CHECK
        ADD CONSTRAINT [FK_Appointments_Doctors]
        FOREIGN KEY ([DoctorID]) REFERENCES [dentist].[Doctors]([DoctorID]);
        PRINT '   + Created Foreign Key [FK_Appointments_Doctors]';
    END
END
ELSE
BEGIN
    PRINT '   - Foreign Key [FK_Appointments_Doctors] already exists.';
END
GO

-- 5.2 FK from TeethState to Patients
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TeethState_Patients' AND parent_object_id = OBJECT_ID('dentist.TeethState'))
BEGIN
    -- Delete any orphaned teeth records pointing to non-existent patients
    DELETE TS
    FROM [dentist].[TeethState] TS
    LEFT JOIN [dentist].[Patients] P ON TS.PatientID = P.PatientID
    WHERE P.PatientID IS NULL;

    ALTER TABLE [dentist].[TeethState] WITH CHECK
    ADD CONSTRAINT [FK_TeethState_Patients]
    FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]);
    PRINT '   + Created Foreign Key [FK_TeethState_Patients]';
END
ELSE
BEGIN
    PRINT '   - Foreign Key [FK_TeethState_Patients] already exists.';
END
GO

-- 5.3 FK from TreatmentHistory to Patients
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TreatmentHistory_Patients' AND parent_object_id = OBJECT_ID('dentist.TreatmentHistory'))
BEGIN
    DELETE TH
    FROM [dentist].[TreatmentHistory] TH
    LEFT JOIN [dentist].[Patients] P ON TH.PatientID = P.PatientID
    WHERE P.PatientID IS NULL;

    ALTER TABLE [dentist].[TreatmentHistory] WITH CHECK
    ADD CONSTRAINT [FK_TreatmentHistory_Patients]
    FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]);
    PRINT '   + Created Foreign Key [FK_TreatmentHistory_Patients]';
END
ELSE
BEGIN
    PRINT '   - Foreign Key [FK_TreatmentHistory_Patients] already exists.';
END
GO

-- 5.4 Performance Indexing on Foreign Keys & Query Paths
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Appointments_Doctor_Date' AND object_id = OBJECT_ID('dentist.Appointments'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Appointments_Doctor_Date]
    ON [dentist].[Appointments]([DoctorID], [PreferredDate])
    INCLUDE ([FullName], [Phone], [Status]);
    PRINT '   + Created Performance Index [IX_Appointments_Doctor_Date]';
END
ELSE
BEGIN
    PRINT '   - Index [IX_Appointments_Doctor_Date] already exists.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TeethState_Patient_Tooth' AND object_id = OBJECT_ID('dentist.TeethState'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TeethState_Patient_Tooth]
    ON [dentist].[TeethState]([PatientID], [ToothNumber])
    INCLUDE ([ConditionStatus], [ConditionColor], [DentitionCategory]);
    PRINT '   + Created Performance Index [IX_TeethState_Patient_Tooth]';
END
ELSE
BEGIN
    PRINT '   - Index [IX_TeethState_Patient_Tooth] already exists.';
END
GO

PRINT '';
PRINT '======================================================================';
PRINT '  DENTIA DATA ACCURACY & INTEGRITY MIGRATION COMPLETED SUCCESSFULLY!  ';
PRINT '======================================================================';
GO
