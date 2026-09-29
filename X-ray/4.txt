-- ============================================================================
-- DENTIA IMAGING, GROQ VISION & VOICESTUDIO AI PIPELINE DATABASE MIGRATION (FIXED)
-- Database: Microsoft SQL Server | Schema: [dentist]
-- ============================================================================

USE [DentistAPI];
GO

-- Ensure [dentist] schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA [dentist]');
END
GO

-- ============================================================================
-- 1. [dentist].[radiographs] Table (Extend with new clinical columns)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'radiographs' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[radiographs] (
        [RadiographID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [patient_id] INT NOT NULL,
        [tooth_key] NVARCHAR(10) NULL,
        [modality] NVARCHAR(50) NOT NULL,
        [source_device_type] NVARCHAR(50) NOT NULL,
        [source_device_brand] NVARCHAR(100) NULL,
        [source_device_model] NVARCHAR(100) NULL,
        [file_url] NVARCHAR(500) NOT NULL,
        [thumbnail_url] NVARCHAR(500) NULL,
        [mime_type] NVARCHAR(100) NOT NULL DEFAULT 'image/jpeg',
        [file_size_bytes] BIGINT NULL,
        [captured_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [appointment_id] INT NULL,
        [uploaded_by] INT NULL,
        [analysis_status] NVARCHAR(30) NOT NULL DEFAULT 'pending',
        [analysis_error] NVARCHAR(MAX) NULL,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_radiographs_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_radiographs_patient_date] ON [dentist].[radiographs] ([patient_id], [captured_at] DESC);
    CREATE INDEX [IX_radiographs_modality] ON [dentist].[radiographs] ([modality]);
END
ELSE
BEGIN
    -- Ensure all new columns exist on existing table
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'tooth_key')
        ALTER TABLE [dentist].[radiographs] ADD [tooth_key] NVARCHAR(10) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'modality')
        ALTER TABLE [dentist].[radiographs] ADD [modality] NVARCHAR(50) NOT NULL DEFAULT 'periapical';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'source_device_type')
        ALTER TABLE [dentist].[radiographs] ADD [source_device_type] NVARCHAR(50) NOT NULL DEFAULT 'sensor';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'source_device_brand')
        ALTER TABLE [dentist].[radiographs] ADD [source_device_brand] NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'source_device_model')
        ALTER TABLE [dentist].[radiographs] ADD [source_device_model] NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'file_url')
        ALTER TABLE [dentist].[radiographs] ADD [file_url] NVARCHAR(500) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'thumbnail_url')
        ALTER TABLE [dentist].[radiographs] ADD [thumbnail_url] NVARCHAR(500) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'analysis_status')
        ALTER TABLE [dentist].[radiographs] ADD [analysis_status] NVARCHAR(30) NOT NULL DEFAULT 'completed';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'analysis_error')
        ALTER TABLE [dentist].[radiographs] ADD [analysis_error] NVARCHAR(MAX) NULL;

    -- Add created_at if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[radiographs]') AND name = 'created_at')
        ALTER TABLE [dentist].[radiographs] ADD [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME();
END
GO

-- ============================================================================
-- 2. [dentist].[ai_findings] Table (References RadiographID)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ai_findings' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ai_findings] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [radiograph_id] INT NOT NULL,
        [patient_id] INT NOT NULL,
        [tooth_number] NVARCHAR(10) NOT NULL,
        [numbering_system] NVARCHAR(20) NOT NULL DEFAULT 'Universal',
        [surfaces] NVARCHAR(MAX) NOT NULL DEFAULT '[]',
        [finding_text] NVARCHAR(500) NOT NULL,
        [suggested_condition] NVARCHAR(100) NOT NULL,
        [suggested_cdt_code] NVARCHAR(20) NULL,
        [confidence] FLOAT NOT NULL,
        [status] NVARCHAR(30) NOT NULL DEFAULT 'pending',
        [reviewed_by] INT NULL,
        [reviewed_at] DATETIME2(7) NULL,
        [applied_teeth_state_id] INT NULL,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        -- References [RadiographID] correctly:
        CONSTRAINT [FK_ai_findings_radiograph] FOREIGN KEY ([radiograph_id]) REFERENCES [dentist].[radiographs]([RadiographID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ai_findings_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID])
    );

    CREATE INDEX [IX_ai_findings_patient_status] ON [dentist].[ai_findings] ([patient_id], [status]);
    CREATE INDEX [IX_ai_findings_radiograph] ON [dentist].[ai_findings] ([radiograph_id]);
END
GO

-- ============================================================================
-- 3. [dentist].[ai_notes_drafts] Table (References RadiographID)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ai_notes_drafts' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ai_notes_drafts] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [radiograph_id] INT NULL,
        [patient_id] INT NOT NULL,
        [doctor_id] INT NULL,
        [source_type] NVARCHAR(50) NOT NULL DEFAULT 'radiograph_ai',
        [source_device_label] NVARCHAR(150) NULL,
        [soap_json] NVARCHAR(MAX) NOT NULL,
        [status] NVARCHAR(30) NOT NULL DEFAULT 'draft',
        [signed_by] INT NULL,
        [signed_at] DATETIME2(7) NULL,
        [final_dental_note_id] BIGINT NULL,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        -- References [RadiographID] correctly:
        CONSTRAINT [FK_ai_notes_drafts_radiograph] FOREIGN KEY ([radiograph_id]) REFERENCES [dentist].[radiographs]([RadiographID]) ON DELETE SET NULL,
        CONSTRAINT [FK_ai_notes_drafts_patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID])
    );

    CREATE INDEX [IX_ai_notes_drafts_patient_status] ON [dentist].[ai_notes_drafts] ([patient_id], [status]);
END
GO

-- ============================================================================
-- 4. [dentist].[DoctorVoiceProfiles] Table
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DoctorVoiceProfiles' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DoctorVoiceProfiles] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [doctor_id] INT NOT NULL,
        [voice_model_id] NVARCHAR(100) NOT NULL,
        [voice_name] NVARCHAR(150) NOT NULL,
        [sample_audio_url] NVARCHAR(500) NULL,
        [preferred_language] NVARCHAR(20) NOT NULL DEFAULT 'en',
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [updated_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_DoctorVoiceProfiles_Doctor] FOREIGN KEY ([doctor_id]) REFERENCES [dentist].[Doctors]([DoctorID]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [UX_DoctorVoiceProfiles_Doctor] ON [dentist].[DoctorVoiceProfiles] ([doctor_id]);
END
GO

-- ============================================================================
-- 5. [dentist].[PatientAudioMessages] Table
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientAudioMessages' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[PatientAudioMessages] (
        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [patient_id] INT NOT NULL,
        [doctor_id] INT NOT NULL,
        [note_id] BIGINT NULL,
        [message_type] NVARCHAR(50) NOT NULL DEFAULT 'post_op_instructions',
        [audio_file_url] NVARCHAR(500) NOT NULL,
        [duration_seconds] INT NULL,
        [message_text] NVARCHAR(MAX) NOT NULL,
        [listened_at] DATETIME2(7) NULL,
        [created_at] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_PatientAudioMessages_Patient] FOREIGN KEY ([patient_id]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PatientAudioMessages_Patient] ON [dentist].[PatientAudioMessages] ([patient_id], [created_at] DESC);
END
GO

PRINT '>>> DENTIA IMAGING, VISION AI & VOICESTUDIO SCHEMA MIGRATION COMPLETED SUCCESSFULLY <<<';
