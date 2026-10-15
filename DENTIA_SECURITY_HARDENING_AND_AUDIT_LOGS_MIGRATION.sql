-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: SECURITY HARDENING & AUDIT LOGGING MIGRATION
-- Database Engine : Microsoft SQL Server 2017 / 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Author          : Senior Application Security Engineer (OWASP Hardening)
-- Date            : October 2026
-- Standards       : OWASP Top 10, OWASP API Top 10, HIPAA §164.312, NZ HIPC 2020
-- =====================================================================================
-- DESCRIPTION:
--   This idempotent migration script implements all database-level security safeguards:
--
--   PHASE 1: IMMUTABLE SECURITY AUDIT LOGGING ([dentist].[SecurityAuditLogs])
--     - Tracks who viewed, modified, or exported patient clinical records & radiographs
--     - Tamper-resistant trigger TR_SecurityAuditLogs_PreventModification blocks UPDATE/DELETE
--
--   PHASE 2: DISTRIBUTED TOKEN REVOCATION DENYLIST ([dentist].[RevokedTokens])
--     - Persists invalidated JWT session tokens across API service restarts & multi-node clusters
--
--   PHASE 3: ACCOUNT LOCKOUT & BRUTE-FORCE DEFENSE COLUMNS
--     - Adds FailedLoginAttempts, LockoutEndUtc, and LastLoginUtc to Doctors & Patients
--
--   PHASE 4: MULTI-TENANT BOLA/IDOR SCOPING & SECURITY INDEXES
--     - High-performance composite indexes for instant tenant isolation & audit querying
--
--   PHASE 5: LEAST-PRIVILEGE SECURITY ROLE ([DentiaAppRole])
--     - Creates dedicated app role with strict SELECT/INSERT/UPDATE permissions
--     - Explicitly DENIES DELETE and ALTER on [dentist].[SecurityAuditLogs]
-- =====================================================================================

USE [DentistAPI];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '======================================================================';
PRINT '  STARTING DENTIA DATABASE SECURITY HARDENING MIGRATION';
PRINT '======================================================================';
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
-- PHASE 1: IMMUTABLE AUDIT LOGGING TABLE ([dentist].[SecurityAuditLogs])
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 1: IMMUTABLE SECURITY AUDIT LOGGING TABLE';
PRINT '----------------------------------------------------------------------';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'SecurityAuditLogs' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[SecurityAuditLogs] (
        [LogID]             BIGINT IDENTITY(1,1) NOT NULL,
        [TimestampUtc]      DATETIME2(3) NOT NULL CONSTRAINT DF_SecurityAuditLogs_Timestamp DEFAULT SYSUTCDATETIME(),
        [ActorID]           INT NULL,
        [ActorRole]         NVARCHAR(50) NOT NULL CONSTRAINT DF_SecurityAuditLogs_ActorRole DEFAULT 'Unknown',
        [ActorUsername]     NVARCHAR(100) NULL,
        [ActionType]        NVARCHAR(100) NOT NULL, -- 'LOGIN_SUCCESS', 'LOGIN_FAILED', 'PATIENT_READ', 'BOLA_DENIED', 'CHART_EXPORT', etc.
        [TargetPatientID]   INT NULL,
        [TargetEntity]      NVARCHAR(100) NULL,     -- 'Patients', 'DentalChartNotes', 'Invoices', 'Radiographs'
        [TargetEntityID]    NVARCHAR(100) NULL,     -- e.g. 'Patient #42', 'Invoice #1004'
        [ClientIP]          NVARCHAR(45) NOT NULL CONSTRAINT DF_SecurityAuditLogs_ClientIP DEFAULT '0.0.0.0',
        [UserAgent]         NVARCHAR(500) NULL,
        [CorrelationID]     NVARCHAR(64) NULL,
        [IsFailure]         BIT NOT NULL CONSTRAINT DF_SecurityAuditLogs_IsFailure DEFAULT 0,
        [Details]           NVARCHAR(MAX) NULL,     -- Sanitized JSON metadata (no passwords, no raw tokens)

        CONSTRAINT PK_SecurityAuditLogs PRIMARY KEY CLUSTERED ([LogID] ASC)
    );

    PRINT '>> [SUCCESS] Created table [dentist].[SecurityAuditLogs].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[SecurityAuditLogs] already exists.';
END
GO

-- 1.1 Foreign key to Patients (SET NULL on delete so audit history is never erased)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_SecurityAuditLogs_Patients' 
      AND parent_object_id = OBJECT_ID('dentist.SecurityAuditLogs')
)
BEGIN
    ALTER TABLE [dentist].[SecurityAuditLogs]
    ADD CONSTRAINT FK_SecurityAuditLogs_Patients FOREIGN KEY ([TargetPatientID])
    REFERENCES [dentist].[Patients] ([PatientID])
    ON DELETE SET NULL;

    PRINT '>> [SUCCESS] Foreign key [FK_SecurityAuditLogs_Patients] established (ON DELETE SET NULL).';
END
GO

-- 1.2 Tamper-Resistance: Block UPDATE and DELETE on SecurityAuditLogs
IF EXISTS (
    SELECT 1 FROM sys.triggers 
    WHERE name = 'TR_SecurityAuditLogs_PreventModification'
      AND parent_id = OBJECT_ID('dentist.SecurityAuditLogs')
)
BEGIN
    DROP TRIGGER [dentist].[TR_SecurityAuditLogs_PreventModification];
END
GO

CREATE TRIGGER [dentist].[TR_SecurityAuditLogs_PreventModification]
ON [dentist].[SecurityAuditLogs]
FOR UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR ('[SECURITY VIOLATION] Audit log records are strictly immutable. UPDATE and DELETE operations are forbidden.', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO
PRINT '>> [SUCCESS] Activated tamper-resistant immutability trigger [TR_SecurityAuditLogs_PreventModification].';
GO


-- =====================================================================================
-- PHASE 2: DISTRIBUTED TOKEN REVOCATION DENYLIST ([dentist].[RevokedTokens])
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 2: DISTRIBUTED TOKEN REVOCATION TABLE';
PRINT '----------------------------------------------------------------------';

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t 
    JOIN sys.schemas s ON t.schema_id = s.schema_id 
    WHERE t.name = 'RevokedTokens' AND s.name = 'dentist'
)
BEGIN
    CREATE TABLE [dentist].[RevokedTokens] (
        [TokenHash]     NVARCHAR(64) NOT NULL, -- SHA-256 hex digest of the revoked token
        [RevokedAtUtc]  DATETIME2(3) NOT NULL CONSTRAINT DF_RevokedTokens_RevokedAt DEFAULT SYSUTCDATETIME(),
        [ExpiresAtUtc]  DATETIME2(3) NOT NULL,
        [RevokedBy]     NVARCHAR(100) NOT NULL CONSTRAINT DF_RevokedTokens_RevokedBy DEFAULT 'User Logout',
        [Reason]        NVARCHAR(255) NULL,    -- 'Logout', 'Password Reset', 'Security Rotation', 'Admin Kick'

        CONSTRAINT PK_RevokedTokens PRIMARY KEY CLUSTERED ([TokenHash] ASC)
    );

    PRINT '>> [SUCCESS] Created table [dentist].[RevokedTokens].';
END
ELSE
BEGIN
    PRINT '>> [INFO] Table [dentist].[RevokedTokens] already exists.';
END
GO

-- Index for automated cleanup of expired revoked tokens
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_RevokedTokens_ExpiresAtUtc' 
      AND object_id = OBJECT_ID('dentist.RevokedTokens')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RevokedTokens_ExpiresAtUtc
    ON [dentist].[RevokedTokens] ([ExpiresAtUtc]);

    PRINT '>> [SUCCESS] Index [IX_RevokedTokens_ExpiresAtUtc] created.';
END
GO


-- =====================================================================================
-- PHASE 3: ACCOUNT LOCKOUT & BRUTE-FORCE DEFENSE COLUMNS
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 3: ACCOUNT LOCKOUT & CREDENTIAL DEFENSE COLUMNS';
PRINT '----------------------------------------------------------------------';

-- 3.1 Doctors table security columns
IF COL_LENGTH('dentist.Doctors', 'FailedLoginAttempts') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [FailedLoginAttempts] INT NOT NULL CONSTRAINT DF_Doctors_FailedLoginAttempts DEFAULT 0;
    PRINT '>> [SUCCESS] Added [FailedLoginAttempts] to [dentist].[Doctors].';
END

IF COL_LENGTH('dentist.Doctors', 'LockoutEndUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [LockoutEndUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LockoutEndUtc] to [dentist].[Doctors].';
END

IF COL_LENGTH('dentist.Doctors', 'LastLoginUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [LastLoginUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LastLoginUtc] to [dentist].[Doctors].';
END

IF COL_LENGTH('dentist.Doctors', 'LastFailedLoginUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [LastFailedLoginUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LastFailedLoginUtc] to [dentist].[Doctors].';
END

IF COL_LENGTH('dentist.Doctors', 'LastPasswordChangeUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [LastPasswordChangeUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LastPasswordChangeUtc] to [dentist].[Doctors].';
END

IF COL_LENGTH('dentist.Doctors', 'RequiresPasswordChange') IS NULL
BEGIN
    ALTER TABLE [dentist].[Doctors] 
    ADD [RequiresPasswordChange] BIT NOT NULL CONSTRAINT DF_Doctors_RequiresPasswordChange DEFAULT 0;
    PRINT '>> [SUCCESS] Added [RequiresPasswordChange] to [dentist].[Doctors].';
END
GO

-- 3.2 Patients table security columns (for Patient Portal auth)
IF COL_LENGTH('dentist.Patients', 'FailedLoginAttempts') IS NULL
BEGIN
    ALTER TABLE [dentist].[Patients] 
    ADD [FailedLoginAttempts] INT NOT NULL CONSTRAINT DF_Patients_FailedLoginAttempts DEFAULT 0;
    PRINT '>> [SUCCESS] Added [FailedLoginAttempts] to [dentist].[Patients].';
END

IF COL_LENGTH('dentist.Patients', 'LockoutEndUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Patients] 
    ADD [LockoutEndUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LockoutEndUtc] to [dentist].[Patients].';
END

IF COL_LENGTH('dentist.Patients', 'LastLoginUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Patients] 
    ADD [LastLoginUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LastLoginUtc] to [dentist].[Patients].';
END

IF COL_LENGTH('dentist.Patients', 'LastFailedLoginUtc') IS NULL
BEGIN
    ALTER TABLE [dentist].[Patients] 
    ADD [LastFailedLoginUtc] DATETIME2(3) NULL;
    PRINT '>> [SUCCESS] Added [LastFailedLoginUtc] to [dentist].[Patients].';
END
GO


-- =====================================================================================
-- PHASE 4: MULTI-TENANT BOLA/IDOR SCOPING & AUDIT PERFORMANCE INDEXES
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 4: MULTI-TENANT SCOPING & SECURITY AUDIT INDEXES';
PRINT '----------------------------------------------------------------------';

-- Index 1: Fast BOLA / Tenant Scoping on Patients
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Patients_SecurityScoping' 
      AND object_id = OBJECT_ID('dentist.Patients')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Patients_SecurityScoping
    ON [dentist].[Patients] ([PatientID], [DoctorID])
    INCLUDE ([FirstName], [LastName], [CNIC], [Phone], [IsActive]);

    PRINT '>> [SUCCESS] Created composite index [IX_Patients_SecurityScoping].';
END

-- Index 2: Audit log queries by Patient ID & Timestamp
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_SecurityAuditLogs_Patient_Time' 
      AND object_id = OBJECT_ID('dentist.SecurityAuditLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SecurityAuditLogs_Patient_Time
    ON [dentist].[SecurityAuditLogs] ([TargetPatientID], [TimestampUtc] DESC)
    INCLUDE ([ActionType], [ActorID], [ActorRole], [ClientIP]);

    PRINT '>> [SUCCESS] Created index [IX_SecurityAuditLogs_Patient_Time].';
END

-- Index 3: Audit log queries by Actor ID, Role & Timestamp
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_SecurityAuditLogs_Actor_Time' 
      AND object_id = OBJECT_ID('dentist.SecurityAuditLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SecurityAuditLogs_Actor_Time
    ON [dentist].[SecurityAuditLogs] ([ActorID], [ActorRole], [TimestampUtc] DESC)
    INCLUDE ([ActionType], [TargetPatientID], [IsFailure]);

    PRINT '>> [SUCCESS] Created index [IX_SecurityAuditLogs_Actor_Time].';
END

-- Index 4: Filtered index for actively locked-out Doctor accounts
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Doctors_ActiveLockout' 
      AND object_id = OBJECT_ID('dentist.Doctors')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Doctors_ActiveLockout
    ON [dentist].[Doctors] ([DoctorID], [LockoutEndUtc])
    WHERE [LockoutEndUtc] IS NOT NULL;

    PRINT '>> [SUCCESS] Created filtered index [IX_Doctors_ActiveLockout].';
END
GO


-- =====================================================================================
-- PHASE 5: LEAST-PRIVILEGE SECURITY APPLICATION ROLE ([DentiaAppRole])
-- =====================================================================================
PRINT '';
PRINT '----------------------------------------------------------------------';
PRINT '>> PHASE 5: LEAST-PRIVILEGE APPLICATION ROLE';
PRINT '----------------------------------------------------------------------';

-- Create dedicated application role if it does not already exist
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'DentiaAppRole' AND type = 'R')
BEGIN
    CREATE ROLE [DentiaAppRole];
    PRINT '>> [SUCCESS] Database role [DentiaAppRole] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Database role [DentiaAppRole] already exists.';
END
GO

-- Grant standard data manipulation permissions to schema dentist
GRANT SELECT, INSERT, UPDATE, EXECUTE ON SCHEMA::dentist TO [DentiaAppRole];

-- Explicitly DENY DELETE and ALTER on SecurityAuditLogs to guarantee immutability
DENY DELETE ON [dentist].[SecurityAuditLogs] TO [DentiaAppRole];
DENY ALTER ON [dentist].[SecurityAuditLogs] TO [DentiaAppRole];

PRINT '>> [SUCCESS] Permissions applied: SELECT, INSERT, UPDATE on schema [dentist]; DENY DELETE on [SecurityAuditLogs].';
GO

PRINT '';
PRINT '======================================================================';
PRINT '  DENTIA DATABASE SECURITY HARDENING MIGRATION COMPLETED SUCCESSFULLY ';
PRINT '======================================================================';
GO
