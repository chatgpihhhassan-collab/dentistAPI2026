-- =====================================================================================
-- 🦷 DENTIA PLATFORM: CLINIC WEBSITE LANDING CONFIG & WHATSAPP DATA INTEGRITY MIGRATION
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database / LocalDB
-- Target Database : DentistDB / DentistAPI
-- Schema          : dentist
-- Author          : Dentia Senior Database & Architecture Engineering Team
-- Date            : October 2026
-- Description     :
--   Complete, idempotent migration script addressing all database-level requirements:
--   1. Schema verification ([dentist])
--   2. Creates [dentist].[ClinicLandingConfig] table for dynamic website CMS management
--   3. Seeds default clinic landing page configuration, sections, contact, and reviews
--   4. Validates and adds WhatsApp opt-in columns to [dentist].[Patients]
--   5. Updates existing patient records with WhatsAppOptIn = 1 so notifications are never dropped
--   6. Ensures [dentist].[Appointments] has [PatientID], [Reason], and WhatsApp tracking columns
--   7. Ensures [dentist].[WhatsAppMessages] queue and audit log table exists
--   8. Creates high-performance indexes for Today's Visits, Registrations, and Returning Patients
-- =====================================================================================

SET NOCOUNT ON;
GO

-- -------------------------------------------------------------------------------------
-- 0. SCHEMA VERIFICATION
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> [SUCCESS] Schema [dentist] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Schema [dentist] verified.';
END
GO

-- -------------------------------------------------------------------------------------
-- 1. CLINIC DYNAMIC WEBSITE LANDING CMS CONFIGURATION TABLE
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 1/5] Verifying [dentist].[ClinicLandingConfig] table...';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'ClinicLandingConfig' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[ClinicLandingConfig] (
        [ConfigID]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ClinicKey]           NVARCHAR(100) NOT NULL CONSTRAINT DF_ClinicLandingConfig_Key DEFAULT ('default'),
        [ClinicName]          NVARCHAR(200) NOT NULL CONSTRAINT DF_ClinicLandingConfig_Name DEFAULT ('Dentia Dental Clinic'),
        [Phone]               NVARCHAR(50)  NULL,
        [EmergencyPhone]      NVARCHAR(50)  NULL,
        [Email]               NVARCHAR(255) NULL,
        [Address]             NVARCHAR(500) NULL,
        [OperatingHours]      NVARCHAR(255) NULL,
        [GoogleMapsUrl]       NVARCHAR(1000) NULL,
        [GoogleReviewsRating] DECIMAL(2,1)  NULL CONSTRAINT DF_ClinicLandingConfig_Rating DEFAULT (4.9),
        [GoogleReviewsCount]  NVARCHAR(50)  NULL CONSTRAINT DF_ClinicLandingConfig_ReviewsCount DEFAULT ('23k+'),
        [ConfigJson]          NVARCHAR(MAX) NOT NULL, -- Full JSON structure for sections toggles, slides, reviews, and FAQs
        [UpdatedAt]           DATETIME2     NOT NULL CONSTRAINT DF_ClinicLandingConfig_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy]           NVARCHAR(100) NULL,
        CONSTRAINT UQ_ClinicLandingConfig_ClinicKey UNIQUE ([ClinicKey])
    );
    PRINT '>> [SUCCESS] Table [dentist].[ClinicLandingConfig] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[ClinicLandingConfig] already exists.';
END
GO

-- 1.2 Seed Default Landing Configuration if not present
IF NOT EXISTS (SELECT 1 FROM [dentist].[ClinicLandingConfig] WHERE [ClinicKey] = 'default')
BEGIN
    INSERT INTO [dentist].[ClinicLandingConfig] (
        [ClinicKey], 
        [ClinicName], 
        [Phone], 
        [EmergencyPhone], 
        [Email], 
        [Address], 
        [OperatingHours], 
        [GoogleMapsUrl], 
        [GoogleReviewsRating], 
        [GoogleReviewsCount], 
        [ConfigJson], 
        [UpdatedAt], 
        [UpdatedBy]
    )
    VALUES (
        'default',
        'Dentia Dental Clinic',
        '+1 123 456 789',
        '+1 800 555 3368',
        'contact@dentiaclinic.com',
        'Suite 400, Grand Central Medical Plaza, Downtown Dental Arts',
        'Mon - Sat: 08:00 - 20:00',
        'https://maps.google.com',
        4.9,
        '23k+',
        N'{
          "clinicName": "Dentia Dental Clinic",
          "phone": "+1 123 456 789",
          "emergencyPhone": "+1 800 555 3368",
          "email": "contact@dentiaclinic.com",
          "address": "Suite 400, Grand Central Medical Plaza, Downtown Dental Arts",
          "operatingHours": "Mon - Sat: 08:00 - 20:00",
          "googleMapsUrl": "https://maps.google.com",
          "googleReviewsRating": 4.9,
          "googleReviewsCount": "23k+",
          "sections": {
            "topEmergencyBar": true,
            "heroCarousel": true,
            "trustBadges": true,
            "clinicalSpecialties": true,
            "doctorExperience": true,
            "beforeAfterSmiles": true,
            "googleReviews": true,
            "pricingCalculator": true,
            "frequentlyAskedQuestions": true,
            "directBookingForm": true,
            "locationMap": true,
            "footer": true
          }
        }',
        SYSUTCDATETIME(),
        'System Seed'
    );
    PRINT '>> [SUCCESS] Initial default landing page configuration seeded.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Default landing page configuration already present.';
END
GO


-- -------------------------------------------------------------------------------------
-- 2. PATIENT WHATSAPP OPT-IN & REGISTRATION DATE INTEGRITY
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 2/5] Checking [dentist].[Patients] columns...';

-- 2.1 WhatsAppOptIn column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'WhatsAppOptIn'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [WhatsAppOptIn] BIT NOT NULL CONSTRAINT DF_Patients_WhatsAppOptIn DEFAULT (1);
    PRINT '>> [SUCCESS] Column [WhatsAppOptIn] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [WhatsAppOptIn] already exists in [dentist].[Patients].';
END
GO

-- 2.2 WhatsAppOptInAt column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'WhatsAppOptInAt'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [WhatsAppOptInAt] DATETIME2 NULL CONSTRAINT DF_Patients_WhatsAppOptInAt DEFAULT (SYSUTCDATETIME());
    PRINT '>> [SUCCESS] Column [WhatsAppOptInAt] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [WhatsAppOptInAt] already exists in [dentist].[Patients].';
END
GO

-- 2.3 WhatsAppLastNotifiedAt column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'WhatsAppLastNotifiedAt'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [WhatsAppLastNotifiedAt] DATETIME2 NULL;
    PRINT '>> [SUCCESS] Column [WhatsAppLastNotifiedAt] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [WhatsAppLastNotifiedAt] already exists in [dentist].[Patients].';
END
GO

-- 2.4 CreatedAt column (for Today Registered Patients metric)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'CreatedAt'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [CreatedAt] DATETIME NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT (GETDATE());
    PRINT '>> [SUCCESS] Column [CreatedAt] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [CreatedAt] already exists in [dentist].[Patients].';
END
GO

-- 2.5 CRITICAL DATA REMEDIATION:
-- Activate WhatsAppOptIn for all existing patients where it was 0 or NULL
-- This prevents the WhatsApp service from silently dropping appointment notifications!
UPDATE [dentist].[Patients]
SET [WhatsAppOptIn] = 1,
    [WhatsAppOptInAt] = ISNULL([WhatsAppOptInAt], SYSUTCDATETIME())
WHERE [WhatsAppOptIn] = 0 OR [WhatsAppOptIn] IS NULL;

PRINT '>> [SUCCESS] Remediation complete: All existing patients set to WhatsAppOptIn = 1.';
GO


-- -------------------------------------------------------------------------------------
-- 3. APPOINTMENTS LINKAGE & WHATSAPP DELIVERY COLUMNS
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 3/5] Checking [dentist].[Appointments] columns...';

-- 3.1 PatientID column in Appointments
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') 
      AND name = N'PatientID'
)
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [PatientID] INT NULL;
    PRINT '>> [SUCCESS] Column [PatientID] added to [dentist].[Appointments].';

    -- Add Foreign Key if possible
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys 
        WHERE name = 'FK_Appointments_Patients'
    )
    BEGIN
        ALTER TABLE [dentist].[Appointments]
        ADD CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientID) REFERENCES [dentist].[Patients](PatientID);
        PRINT '>> [SUCCESS] Foreign Key FK_Appointments_Patients created.';
    END
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [PatientID] already exists in [dentist].[Appointments].';
END
GO

-- 3.2 WhatsAppQueued column in Appointments
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') 
      AND name = N'WhatsAppQueued'
)
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [WhatsAppQueued] BIT NOT NULL CONSTRAINT DF_Appointments_WhatsAppQueued DEFAULT (0);
    PRINT '>> [SUCCESS] Column [WhatsAppQueued] added to [dentist].[Appointments].';
END
GO

-- 3.3 WhatsAppSentAt column in Appointments
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') 
      AND name = N'WhatsAppSentAt'
)
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [WhatsAppSentAt] DATETIME2 NULL;
    PRINT '>> [SUCCESS] Column [WhatsAppSentAt] added to [dentist].[Appointments].';
END
GO

-- 3.4 Reason column in Appointments
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Appointments]') 
      AND name = N'Reason'
)
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [Reason] NVARCHAR(MAX) NULL;
    PRINT '>> [SUCCESS] Column [Reason] added to [dentist].[Appointments].';
END
GO


-- -------------------------------------------------------------------------------------
-- 4. WHATSAPP QUEUE & AUDIT TABLE ([dentist].[WhatsAppMessages])
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 4/5] Checking [dentist].[WhatsAppMessages] table...';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'WhatsAppMessages' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[WhatsAppMessages] (
        [MessageId]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PatientId]          INT NULL,
        [AppointmentId]      INT NULL,
        [DoctorId]           INT NULL,
        [RecipientPhone]     NVARCHAR(50) NOT NULL,
        [MessageType]        NVARCHAR(50) NOT NULL, -- 'AppointmentConfirmation', 'Reminder', 'FollowUp', etc.
        [MessageBody]        NVARCHAR(MAX) NOT NULL,
        [Status]             NVARCHAR(30) NOT NULL CONSTRAINT DF_WhatsAppMessages_Status DEFAULT ('Queued'), -- 'Queued', 'Sent', 'Failed', 'Cancelled'
        [ScheduledSendAt]    DATETIME2 NOT NULL CONSTRAINT DF_WhatsAppMessages_Scheduled DEFAULT (SYSUTCDATETIME()),
        [SentAt]             DATETIME2 NULL,
        [DeliveryStatus]     NVARCHAR(30) NULL, -- 'Pending', 'Delivered', 'Read'
        [DeliveredAt]        DATETIME2 NULL,
        [RetryCount]         INT NOT NULL CONSTRAINT DF_WhatsAppMessages_Retries DEFAULT (0),
        [ErrorMessage]       NVARCHAR(1000) NULL,
        [CreatedAt]          DATETIME2 NOT NULL CONSTRAINT DF_WhatsAppMessages_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    PRINT '>> [SUCCESS] Table [dentist].[WhatsAppMessages] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[WhatsAppMessages] already exists.';
END
GO


-- -------------------------------------------------------------------------------------
-- 5. PERFORMANCE INDEXES FOR CLINICAL DASHBOARD METRICS
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 5/5] Creating optimized non-clustered indexes...';

-- 5.1 Index on Appointments(PreferredDate, Status) for "Today's Patient Visits"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Appointments_PreferredDate_Status' 
      AND object_id = OBJECT_ID(N'[dentist].[Appointments]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Appointments_PreferredDate_Status]
    ON [dentist].[Appointments] ([PreferredDate], [Status])
    INCLUDE ([AppointmentID], [PatientID], [DoctorID], [FullName], [Phone]);
    PRINT '>> [SUCCESS] Index [IX_Appointments_PreferredDate_Status] created.';
END
GO

-- 5.2 Index on Patients(CreatedAt) for "Today Registered Patients"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Patients_CreatedAt' 
      AND object_id = OBJECT_ID(N'[dentist].[Patients]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Patients_CreatedAt]
    ON [dentist].[Patients] ([CreatedAt])
    INCLUDE ([PatientID], [FirstName], [LastName], [Phone], [WhatsAppOptIn]);
    PRINT '>> [SUCCESS] Index [IX_Patients_CreatedAt] created.';
END
GO

-- 5.3 Index on Appointments(PatientID, PreferredDate) for "Revisited Patients"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Appointments_PatientID_PreferredDate' 
      AND object_id = OBJECT_ID(N'[dentist].[Appointments]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Appointments_PatientID_PreferredDate]
    ON [dentist].[Appointments] ([PatientID], [PreferredDate])
    WHERE [PatientID] IS NOT NULL;
    PRINT '>> [SUCCESS] Index [IX_Appointments_PatientID_PreferredDate] created.';
END
GO


-- -------------------------------------------------------------------------------------
-- VERIFICATION SUMMARY REPORT
-- -------------------------------------------------------------------------------------
PRINT '--------------------------------------------------------------------------------';
PRINT '🎉 DENTIA CLINICAL DATABASE MIGRATION APPLIED SUCCESSFULLY';
PRINT '--------------------------------------------------------------------------------';

SELECT 
    'Table' = s.name + '.' + t.name,
    'Total Rows' = p.rows
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
WHERE s.name = 'dentist' AND t.name IN ('ClinicLandingConfig', 'Patients', 'Appointments', 'WhatsAppMessages')
ORDER BY t.name;

SELECT 
    'ClinicKey' = ClinicKey,
    'ClinicName' = ClinicName,
    'Phone' = Phone,
    'Email' = Email,
    'OperatingHours' = OperatingHours,
    'GoogleReviews' = CAST(GoogleReviewsRating AS VARCHAR(10)) + ' (' + GoogleReviewsCount + ')',
    'UpdatedAt' = CONVERT(VARCHAR(25), UpdatedAt, 120)
FROM [dentist].[ClinicLandingConfig];
GO
