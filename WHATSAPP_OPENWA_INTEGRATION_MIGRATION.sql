-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: WHATSAPP OPENWA INTEGRATION MIGRATION
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Author          : Dentia Architecture & Engineering Team
-- Date            : October 2026
-- Description     :
--   Idempotent migration script for WhatsApp OpenWA Integration:
--   1. Adds WhatsApp Opt-In fields to [dentist].[Patients]
--   2. Creates [dentist].[WhatsAppMessages] audit log and queue table
--   3. Creates [dentist].[WhatsAppSessions] gateway state and QR table
--   4. Creates filtered indexes for high-speed rate-limited background queue polling
-- =====================================================================================

USE [DentistAPI];
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
    PRINT '>> [INFO] Schema [dentist] already exists.';
END
GO

-- -------------------------------------------------------------------------------------
-- 1. ADD WHATSAPP OPT-IN TO [dentist].[Patients]
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 1/3] Checking [dentist].[Patients] for WhatsApp Opt-In columns...';

-- 1.1 WhatsAppOptIn (BIT)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'WhatsAppOptIn'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [WhatsAppOptIn] BIT NOT NULL 
        CONSTRAINT DF_Patients_WhatsAppOptIn DEFAULT 1; -- Defaults to 1 (active) for existing patient convenience, can be toggled off
    PRINT '>> [SUCCESS] Column [WhatsAppOptIn] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [WhatsAppOptIn] already exists in [dentist].[Patients].';
END
GO

-- 1.2 WhatsAppOptInAt (DATETIME2)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dentist].[Patients]') 
      AND name = N'WhatsAppOptInAt'
)
BEGIN
    ALTER TABLE [dentist].[Patients]
    ADD [WhatsAppOptInAt] DATETIME2 NULL 
        CONSTRAINT DF_Patients_WhatsAppOptInAt DEFAULT SYSUTCDATETIME();
    PRINT '>> [SUCCESS] Column [WhatsAppOptInAt] added to [dentist].[Patients].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Column [WhatsAppOptInAt] already exists in [dentist].[Patients].';
END
GO

-- 1.3 WhatsAppLastNotifiedAt (DATETIME2)
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


-- -------------------------------------------------------------------------------------
-- 2. CREATE [dentist].[WhatsAppMessages] (QUEUE & AUDIT LOG TABLE)
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 2/3] Checking [dentist].[WhatsAppMessages] table...';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'WhatsAppMessages' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[WhatsAppMessages] (
        [MessageID]          INT IDENTITY(1,1) NOT NULL,
        [PatientID]          INT NULL,
        [AppointmentID]      INT NULL,
        [DoctorID]           INT NULL,
        [RecipientPhone]     NVARCHAR(30) NOT NULL,           -- e.g., "923001234567"
        [ChatID]             NVARCHAR(60) NOT NULL,           -- e.g., "923001234567@c.us"
        [MessageBody]        NVARCHAR(MAX) NOT NULL,          -- Formatted text message
        [MessageType]        NVARCHAR(50) NOT NULL            -- 'AppointmentBooking', 'AppointmentReminder', 'TreatmentPlanSummary', 'OTP', 'Custom'
                             CONSTRAINT DF_WhatsAppMessages_MessageType DEFAULT 'AppointmentBooking',
        [Status]             NVARCHAR(30) NOT NULL            -- 'Pending', 'Queued', 'Sending', 'Sent', 'Delivered', 'Read', 'Failed', 'Skipped_NoOptIn'
                             CONSTRAINT DF_WhatsAppMessages_Status DEFAULT 'Pending',
        [OpenWAMessageID]    NVARCHAR(120) NULL,              -- Gateway external message ID for status webhooks
        [SessionName]        NVARCHAR(100) NOT NULL           -- Gateway session ID (e.g. 'dentia-clinic-bot')
                             CONSTRAINT DF_WhatsAppMessages_SessionName DEFAULT 'dentia-clinic-bot',
        [RetryCount]         INT NOT NULL 
                             CONSTRAINT DF_WhatsAppMessages_RetryCount DEFAULT 0,
        [MaxRetries]         INT NOT NULL 
                             CONSTRAINT DF_WhatsAppMessages_MaxRetries DEFAULT 3,
        [ErrorMessage]       NVARCHAR(MAX) NULL,              -- Failure details or gateway error
        [ScheduledFor]       DATETIME2 NOT NULL               -- For rate-limiting delay (2-6s random jitter)
                             CONSTRAINT DF_WhatsAppMessages_ScheduledFor DEFAULT SYSUTCDATETIME(),
        [SentAt]             DATETIME2 NULL,
        [DeliveredAt]        DATETIME2 NULL,
        [ReadAt]             DATETIME2 NULL,
        [CreatedAt]          DATETIME2 NOT NULL 
                             CONSTRAINT DF_WhatsAppMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]          DATETIME2 NOT NULL 
                             CONSTRAINT DF_WhatsAppMessages_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_WhatsAppMessages PRIMARY KEY CLUSTERED ([MessageID] ASC),
        CONSTRAINT FK_WhatsAppMessages_Patients FOREIGN KEY ([PatientID]) 
            REFERENCES [dentist].[Patients] ([PatientID]) ON DELETE SET NULL,
        CONSTRAINT FK_WhatsAppMessages_Appointments FOREIGN KEY ([AppointmentID]) 
            REFERENCES [dentist].[Appointments] ([AppointmentID]) ON DELETE SET NULL
    );

    PRINT '>> [SUCCESS] Table [dentist].[WhatsAppMessages] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[WhatsAppMessages] already exists.';
END
GO

-- Indexes for WhatsApp Messages
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WhatsAppMessages_Queue' AND object_id = OBJECT_ID(N'[dentist].[WhatsAppMessages]'))
BEGIN
    -- Filtered index: only indexes actionable messages awaiting delivery
    CREATE NONCLUSTERED INDEX [IX_WhatsAppMessages_Queue] 
    ON [dentist].[WhatsAppMessages] ([Status], [ScheduledFor], [RetryCount])
    INCLUDE ([MessageID], [RecipientPhone], [ChatID], [SessionName])
    WHERE [Status] IN ('Pending', 'Queued', 'Retrying');
    
    PRINT '>> [SUCCESS] Index [IX_WhatsAppMessages_Queue] created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WhatsAppMessages_Patient' AND object_id = OBJECT_ID(N'[dentist].[WhatsAppMessages]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_WhatsAppMessages_Patient] 
    ON [dentist].[WhatsAppMessages] ([PatientID], [CreatedAt] DESC);
    
    PRINT '>> [SUCCESS] Index [IX_WhatsAppMessages_Patient] created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WhatsAppMessages_OpenWAMsgID' AND object_id = OBJECT_ID(N'[dentist].[WhatsAppMessages]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_WhatsAppMessages_OpenWAMsgID] 
    ON [dentist].[WhatsAppMessages] ([OpenWAMessageID])
    WHERE [OpenWAMessageID] IS NOT NULL;
    
    PRINT '>> [SUCCESS] Index [IX_WhatsAppMessages_OpenWAMsgID] created.';
END
GO


-- -------------------------------------------------------------------------------------
-- 3. CREATE [dentist].[WhatsAppSessions] (GATEWAY & QR STATE TABLE)
-- -------------------------------------------------------------------------------------
PRINT '>> [STEP 3/3] Checking [dentist].[WhatsAppSessions] table...';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'WhatsAppSessions' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[WhatsAppSessions] (
        [SessionRecordID]     INT IDENTITY(1,1) NOT NULL,
        [SessionName]         NVARCHAR(100) NOT NULL UNIQUE,   -- 'dentia-clinic-bot'
        [Status]              NVARCHAR(50) NOT NULL            -- 'DISCONNECTED', 'SCAN_QR_CODE', 'STARTING', 'WORKING', 'FAILED'
                              CONSTRAINT DF_WhatsAppSessions_Status DEFAULT 'DISCONNECTED',
        [QRCodeData]          NVARCHAR(MAX) NULL,              -- Raw QR or Base64 Image
        [ConnectedPhoneNumber] NVARCHAR(50) NULL,             -- e.g. "+923001234567"
        [PushName]            NVARCHAR(100) NULL,              -- Connected WhatsApp Profile Name
        [IsHealthy]           BIT NOT NULL 
                              CONSTRAINT DF_WhatsAppSessions_IsHealthy DEFAULT 0,
        [LastPingAt]          DATETIME2 NULL,
        [LastStatusChangeAt]  DATETIME2 NOT NULL 
                              CONSTRAINT DF_WhatsAppSessions_LastStatusChange DEFAULT SYSUTCDATETIME(),
        [CreatedAt]           DATETIME2 NOT NULL 
                              CONSTRAINT DF_WhatsAppSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]           DATETIME2 NOT NULL 
                              CONSTRAINT DF_WhatsAppSessions_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_WhatsAppSessions PRIMARY KEY CLUSTERED ([SessionRecordID] ASC)
    );

    PRINT '>> [SUCCESS] Table [dentist].[WhatsAppSessions] created.';

    -- Seed Default Master Session Row
    INSERT INTO [dentist].[WhatsAppSessions] ([SessionName], [Status], [IsHealthy])
    VALUES ('dentia-clinic-bot', 'DISCONNECTED', 0);
    PRINT '>> [SUCCESS] Seeded default session record [dentia-clinic-bot].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[WhatsAppSessions] already exists.';
END
GO


-- -------------------------------------------------------------------------------------
-- 4. VERIFICATION QUERY
-- -------------------------------------------------------------------------------------
PRINT '=====================================================================================';
PRINT '>> [VERIFICATION] Verifying WhatsApp OpenWA Schema Integration:';
PRINT '=====================================================================================';

SELECT 
    'Patients Column Added' AS [VerificationCheck],
    c.name AS [ItemName],
    t.name AS [DataType]
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dentist].[Patients]')
  AND c.name IN ('WhatsAppOptIn', 'WhatsAppOptInAt', 'WhatsAppLastNotifiedAt')

UNION ALL

SELECT 
    'Table Created' AS [VerificationCheck],
    t.name AS [ItemName],
    'TABLE' AS [DataType]
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dentist' AND t.name IN ('WhatsAppMessages', 'WhatsAppSessions');

PRINT '>> [COMPLETE] WhatsApp OpenWA Migration Ready.';
GO
