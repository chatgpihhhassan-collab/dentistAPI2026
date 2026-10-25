-- ==============================================================================================
-- DENTIA CLINICAL DATABASE MIGRATION: TREATMENT SESSION TRACKING & ACTION AUDIT LOGS
-- Date: 2026-10-25
-- Description:
-- 1. Creates [dentist].[TreatmentSessions] table for recording chairside session duration and details.
-- 2. Creates [dentist].[TreatmentSessionLogs] table for granular clinical action logs per session.
-- 3. Adds high-performance indexes for instantaneous retrieval of latest session on Chart Page.
-- 4. Ensures idempotent execution and compatibility with existing Appointments and TreatmentItems.
-- ==============================================================================================

SET NOCOUNT ON;
PRINT '==================================================================';
PRINT 'BEGIN DENTIA TREATMENT SESSION TRACKING & AUDIT LOGS MIGRATION';
PRINT '==================================================================';

-- ----------------------------------------------------------------------------------------------
-- 1. CREATE TABLE: [dentist].[TreatmentSessions]
-- ----------------------------------------------------------------------------------------------
PRINT '1. Creating or validating [dentist].[TreatmentSessions] table...';

IF OBJECT_ID('dentist.TreatmentSessions', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[TreatmentSessions]
    (
        [SessionID]          BIGINT IDENTITY(1,1) NOT NULL,
        [PatientID]          INT NOT NULL,
        [DoctorID]           INT NOT NULL,
        [AppointmentID]      INT NULL,
        [StartedAt]          DATETIME2 NOT NULL CONSTRAINT [DF_TreatmentSessions_StartedAt] DEFAULT SYSUTCDATETIME(),
        [EndedAt]            DATETIME2 NULL,
        [DurationSeconds]    INT NOT NULL CONSTRAINT [DF_TreatmentSessions_DurationSeconds] DEFAULT 0,
        [FormattedDuration]  NVARCHAR(50) NOT NULL CONSTRAINT [DF_TreatmentSessions_FormattedDuration] DEFAULT '00:00',
        [Status]             NVARCHAR(30) NOT NULL CONSTRAINT [DF_TreatmentSessions_Status] DEFAULT 'active', -- 'active', 'completed', 'cancelled'
        [ActionCount]        INT NOT NULL CONSTRAINT [DF_TreatmentSessions_ActionCount] DEFAULT 0,
        [SummaryNotes]       NVARCHAR(MAX) NULL,
        [ActionLogsJson]     NVARCHAR(MAX) NULL, -- JSON array of all chairside clinical actions
        [CreatedAt]          DATETIME2 NOT NULL CONSTRAINT [DF_TreatmentSessions_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]          DATETIME2 NOT NULL CONSTRAINT [DF_TreatmentSessions_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT [PK_TreatmentSessions] PRIMARY KEY CLUSTERED ([SessionID] ASC)
    );
    PRINT '   + Table [dentist].[TreatmentSessions] created successfully.';
END
ELSE
BEGIN
    PRINT '   = Table [dentist].[TreatmentSessions] already exists.';
    
    -- Ensure all columns exist (defensive migration)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dentist.TreatmentSessions') AND name = 'ActionLogsJson')
    BEGIN
        ALTER TABLE [dentist].[TreatmentSessions] ADD [ActionLogsJson] NVARCHAR(MAX) NULL;
        PRINT '   + Column [ActionLogsJson] added to [dentist].[TreatmentSessions].';
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dentist.TreatmentSessions') AND name = 'ActionCount')
    BEGIN
        ALTER TABLE [dentist].[TreatmentSessions] ADD [ActionCount] INT NOT NULL CONSTRAINT [DF_TreatmentSessions_ActionCount_New] DEFAULT 0;
        PRINT '   + Column [ActionCount] added to [dentist].[TreatmentSessions].';
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dentist.TreatmentSessions') AND name = 'FormattedDuration')
    BEGIN
        ALTER TABLE [dentist].[TreatmentSessions] ADD [FormattedDuration] NVARCHAR(50) NOT NULL CONSTRAINT [DF_TreatmentSessions_FormattedDuration_New] DEFAULT '00:00';
        PRINT '   + Column [FormattedDuration] added to [dentist].[TreatmentSessions].';
    END;
END;

-- ----------------------------------------------------------------------------------------------
-- 2. CREATE TABLE: [dentist].[TreatmentSessionLogs] (Normalized Action Audit Log)
-- ----------------------------------------------------------------------------------------------
PRINT '2. Creating or validating [dentist].[TreatmentSessionLogs] table...';

IF OBJECT_ID('dentist.TreatmentSessionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE [dentist].[TreatmentSessionLogs]
    (
        [LogID]           BIGINT IDENTITY(1,1) NOT NULL,
        [SessionID]       BIGINT NOT NULL,
        [PatientID]       INT NOT NULL,
        [DoctorID]        INT NOT NULL,
        [ActionType]      NVARCHAR(60) NOT NULL, -- 'session_started', 'consultation_billed', 'tooth_diagnosed', 'procedure_added', 'radiograph_acquired', 'session_completed'
        [ElapsedSeconds]  INT NOT NULL CONSTRAINT [DF_TreatmentSessionLogs_ElapsedSeconds] DEFAULT 0,
        [Description]     NVARCHAR(500) NOT NULL,
        [MetadataJson]    NVARCHAR(MAX) NULL,
        [Timestamp]       DATETIME2 NOT NULL CONSTRAINT [DF_TreatmentSessionLogs_Timestamp] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_TreatmentSessionLogs] PRIMARY KEY CLUSTERED ([LogID] ASC)
    );
    PRINT '   + Table [dentist].[TreatmentSessionLogs] created successfully.';
END
ELSE
BEGIN
    PRINT '   = Table [dentist].[TreatmentSessionLogs] already exists.';
END;

-- ----------------------------------------------------------------------------------------------
-- 3. INDEXES: High-Performance Latest Session & Audit Queries
-- ----------------------------------------------------------------------------------------------
PRINT '3. Creating optimized performance indexes...';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TreatmentSessions_Patient_Started' 
      AND object_id = OBJECT_ID('dentist.TreatmentSessions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TreatmentSessions_Patient_Started]
    ON [dentist].[TreatmentSessions] ([PatientID] ASC, [StartedAt] DESC)
    INCLUDE ([SessionID], [DoctorID], [DurationSeconds], [FormattedDuration], [Status], [ActionCount], [EndedAt]);
    PRINT '   + Index [IX_TreatmentSessions_Patient_Started] created successfully.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TreatmentSessions_Doctor' 
      AND object_id = OBJECT_ID('dentist.TreatmentSessions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TreatmentSessions_Doctor]
    ON [dentist].[TreatmentSessions] ([DoctorID] ASC, [StartedAt] DESC);
    PRINT '   + Index [IX_TreatmentSessions_Doctor] created successfully.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TreatmentSessionLogs_Session' 
      AND object_id = OBJECT_ID('dentist.TreatmentSessionLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TreatmentSessionLogs_Session]
    ON [dentist].[TreatmentSessionLogs] ([SessionID] ASC, [Timestamp] ASC);
    PRINT '   + Index [IX_TreatmentSessionLogs_Session] created successfully.';
END;

-- ----------------------------------------------------------------------------------------------
-- 4. SEED / ENSURE DIAGNOSTIC FEE SCHEDULES AND CATALOG ENTRIES
-- ----------------------------------------------------------------------------------------------
PRINT '4. Ensuring diagnostic catalog procedures exist...';

IF OBJECT_ID('dentist.treatment_catalog', 'U') IS NOT NULL
BEGIN
    -- 1. Consultation D0150
    IF NOT EXISTS (SELECT 1 FROM [dentist].[treatment_catalog] WHERE procedure_code = 'D0150')
    BEGIN
        INSERT INTO [dentist].[treatment_catalog] (procedure_code, name, category, default_price, default_visits, is_active)
        VALUES ('D0150', 'Comprehensive Oral Evaluation / Consultation', 'consultation', 5000, 1, 1);
        PRINT '   + Inserted D0150 (Consultation) into [dentist].[treatment_catalog].';
    END;

    -- 2. Digital RVG X-Ray D0220
    IF NOT EXISTS (SELECT 1 FROM [dentist].[treatment_catalog] WHERE procedure_code = 'D0220')
    BEGIN
        INSERT INTO [dentist].[treatment_catalog] (procedure_code, name, category, default_price, default_visits, is_active)
        VALUES ('D0220', 'Digital Intraoral RVG Radiograph', 'xray', 2500, 1, 1);
        PRINT '   + Inserted D0220 (Digital RVG X-Ray) into [dentist].[treatment_catalog].';
    END;

    -- 3. Panoramic OPG X-Ray D0330
    IF NOT EXISTS (SELECT 1 FROM [dentist].[treatment_catalog] WHERE procedure_code = 'D0330')
    BEGIN
        INSERT INTO [dentist].[treatment_catalog] (procedure_code, name, category, default_price, default_visits, is_active)
        VALUES ('D0330', 'Full Mouth Panoramic OPG X-Ray', 'xray', 8500, 1, 1);
        PRINT '   + Inserted D0330 (Panoramic OPG X-Ray) into [dentist].[treatment_catalog].';
    END;
END;

PRINT '==================================================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY';
PRINT '==================================================================';
