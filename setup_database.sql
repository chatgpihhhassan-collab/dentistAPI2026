-- ============================================================================
-- LUMINA DENTAL STUDIO - FULL DATABASE SCHEMA SETUP FOR PRODUCTION / CLOUD
-- Target Database: Microsoft SQL Server (2019 / 2022 / Azure SQL)
-- ============================================================================

-- 1. Ensure Schema Exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
END
GO

-- 2. Doctors Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Doctors' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Doctors] (
        [DoctorID] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(100) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [Region] NVARCHAR(20) NOT NULL CONSTRAINT DF_Doctors_Region DEFAULT 'NZ',
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Doctors_CreatedAt DEFAULT GETDATE(),
        [IsSuperAdmin] BIT NOT NULL CONSTRAINT DF_Doctors_IsSuperAdmin DEFAULT 0
    );
END
GO

-- 3. Patients Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Patients' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Patients] (
        [PatientID] INT IDENTITY(1,1) PRIMARY KEY,
        [DoctorID] INT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [DOB] DATE NULL,
        [Gender] NVARCHAR(20) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Email] NVARCHAR(150) NULL,
        [Address] NVARCHAR(255) NULL,
        [MedicalAlerts] NVARCHAR(MAX) NULL,
        [Allergies] NVARCHAR(MAX) NULL,
        [CurrentTreatmentPlan] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT GETDATE(),
        [ProfileImage] VARBINARY(MAX) NULL,
        [ProfileImageMimeType] VARCHAR(50) NULL,
        [DentitionType] NVARCHAR(20) NOT NULL CONSTRAINT DF_Patients_DentitionType DEFAULT 'Adult',
        CONSTRAINT FK_Patients_Doctors FOREIGN KEY ([DoctorID]) REFERENCES [dentist].[Doctors]([DoctorID]) ON DELETE SET NULL
    );
END
GO

-- 4. TeethState Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TeethState' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[TeethState] (
        [TeethStateID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [ToothNumber] INT NOT NULL,
        [ToothKey] NVARCHAR(10) NULL,
        [DentitionCategory] NVARCHAR(20) NOT NULL CONSTRAINT DF_TeethState_DentitionCategory DEFAULT 'Adult',
        [ConditionStatus] NVARCHAR(500) NOT NULL,
        [ConditionColor] NVARCHAR(50) NOT NULL,
        [Surfaces] NVARCHAR(50) NULL,
        [Comments] NVARCHAR(MAX) NULL,
        [LastUpdated] DATETIME2 NOT NULL CONSTRAINT DF_TeethState_LastUpdated DEFAULT GETDATE(),
        [DoctorID] INT NULL,
        CONSTRAINT FK_TeethState_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
    CREATE INDEX IX_TeethState_PatientID ON [dentist].[TeethState]([PatientID]);
END
GO

-- 5. TreatmentHistory Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TreatmentHistory' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[TreatmentHistory] (
        [HistoryID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [ToothNumber] INT NOT NULL,
        [ToothKey] NVARCHAR(10) NULL,
        [DentitionCategory] NVARCHAR(20) NOT NULL CONSTRAINT DF_TreatmentHistory_DentitionCategory DEFAULT 'Adult',
        [TreatmentPerformed] NVARCHAR(500) NOT NULL,
        [Surfaces] NVARCHAR(50) NULL,
        [Comments] NVARCHAR(MAX) NULL,
        [ActionDate] DATETIME2 NOT NULL CONSTRAINT DF_TreatmentHistory_ActionDate DEFAULT GETDATE(),
        [DoctorID] INT NULL,
        CONSTRAINT FK_TreatmentHistory_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
    CREATE INDEX IX_TreatmentHistory_PatientID ON [dentist].[TreatmentHistory]([PatientID]);
END
GO

-- 6. Prescriptions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Prescriptions' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Prescriptions] (
        [PrescriptionID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [MedicineName] NVARCHAR(200) NOT NULL,
        [Dosage] NVARCHAR(100) NULL,
        [Frequency] NVARCHAR(100) NULL,
        [Duration] NVARCHAR(100) NULL,
        [Instructions] NVARCHAR(MAX) NULL,
        [PrescribedDate] DATETIME2 NOT NULL CONSTRAINT DF_Prescriptions_PrescribedDate DEFAULT GETDATE(),
        [DoctorID] INT NULL,
        CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
END
GO

-- 7. Appointments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Appointments] (
        [AppointmentID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NULL,
        [FullName] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(50) NULL,
        [Email] NVARCHAR(150) NULL,
        [ProcedureName] NVARCHAR(200) NULL,
        [PreferredDate] DATETIME2 NOT NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT 'Scheduled',
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Appointments_CreatedAt DEFAULT GETDATE(),
        [DoctorID] INT NULL
    );
END
GO

-- 8. Clinical Logs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClinicalLogs' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ClinicalLogs] (
        [LogID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [LogType] NVARCHAR(50) NOT NULL,
        [Details] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_ClinicalLogs_CreatedAt DEFAULT GETDATE(),
        [DoctorID] INT NULL,
        CONSTRAINT FK_ClinicalLogs_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
END
GO

-- 9. Chat History Table (AI Assistant interaction)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatHistory' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ChatHistory] (
        [ChatID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [Transcript] NVARCHAR(MAX) NOT NULL,
        [ParsedAction] NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME2 NOT NULL CONSTRAINT DF_ChatHistory_Timestamp DEFAULT GETDATE(),
        [DoctorID] INT NULL,
        CONSTRAINT FK_ChatHistory_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
END
GO

-- 10. Radiographs / Imaging Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Radiographs' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Radiographs] (
        [RadiographID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [DoctorID] INT NULL,
        [ImageName] NVARCHAR(255) NOT NULL,
        [ImageData] VARBINARY(MAX) NULL,
        [MimeType] VARCHAR(50) NOT NULL CONSTRAINT DF_Radiographs_Mime DEFAULT 'image/png',
        [AnalysisSummary] NVARCHAR(MAX) NULL,
        [UploadedAt] DATETIME2 NOT NULL CONSTRAINT DF_Radiographs_UploadedAt DEFAULT GETDATE(),
        CONSTRAINT FK_Radiographs_Patients FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );
END
GO

-- 11. AI Dental Note Sessions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DentalNoteSessions' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DentalNoteSessions] (
        [SessionId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [PatientId] BIGINT NOT NULL,
        [DentistId] BIGINT NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [StartedAt] DATETIME2 NOT NULL CONSTRAINT DF_DentalNoteSessions_StartedAt DEFAULT GETDATE(),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_DentalNoteSessions_CreatedAt DEFAULT GETDATE()
    );
END
GO

-- 12. Audio Recordings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AudioRecordings' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[AudioRecordings] (
        [AudioId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [SessionId] BIGINT NOT NULL,
        [StorageUri] NVARCHAR(1000) NOT NULL,
        [MimeType] NVARCHAR(100) NOT NULL,
        [DurationSeconds] INT NOT NULL,
        [Sha256Hash] NVARCHAR(256) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_AudioRecordings_CreatedAt DEFAULT GETDATE()
    );
END
GO

-- 13. Transcripts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transcripts' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Transcripts] (
        [TranscriptId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [SessionId] BIGINT NOT NULL,
        [RawText] NVARCHAR(MAX) NOT NULL,
        [CleanedText] NVARCHAR(MAX) NULL,
        [Language] NVARCHAR(50) NOT NULL CONSTRAINT DF_Transcripts_Language DEFAULT 'en',
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Transcripts_CreatedAt DEFAULT GETDATE()
    );
END
GO

-- 14. Dental Notes (SOAP Notes) Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DentalNotes' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DentalNotes] (
        [NoteId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [SessionId] BIGINT NOT NULL,
        [AudioId] BIGINT NULL,
        [PatientId] BIGINT NOT NULL,
        [DentistId] BIGINT NOT NULL,
        [Summary] NVARCHAR(MAX) NULL,
        [ChiefComplaint] NVARCHAR(MAX) NULL,
        [History] NVARCHAR(MAX) NULL,
        [Examination] NVARCHAR(MAX) NULL,
        [Assessment] NVARCHAR(MAX) NULL,
        [TreatmentPerformed] NVARCHAR(MAX) NULL,
        [PostOpAdvice] NVARCHAR(MAX) NULL,
        [FollowUp] NVARCHAR(MAX) NULL,
        [DentitionCategory] NVARCHAR(20) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT DF_DentalNotes_Status DEFAULT 'Completed',
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_DentalNotes_IsDeleted DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_DentalNotes_CreatedAt DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT DF_DentalNotes_UpdatedAt DEFAULT GETDATE()
    );
END
GO

-- 15. Dental Note Prescriptions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DentalNotePrescriptions' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DentalNotePrescriptions] (
        [PrescriptionId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [NoteId] BIGINT NOT NULL,
        [MedicationName] NVARCHAR(255) NOT NULL,
        [Strength] NVARCHAR(100) NULL,
        [Dose] NVARCHAR(100) NULL,
        [Route] NVARCHAR(100) NULL,
        [Frequency] NVARCHAR(100) NULL,
        [Duration] NVARCHAR(100) NULL,
        [Instructions] NVARCHAR(MAX) NULL,
        [Confidence] FLOAT NOT NULL CONSTRAINT DF_DNP_Confidence DEFAULT 1.0,
        CONSTRAINT FK_DNP_DentalNotes FOREIGN KEY ([NoteId]) REFERENCES [dentist].[DentalNotes]([NoteId]) ON DELETE CASCADE
    );
END
GO

-- 16. Dental Note Treatment Plans Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DentalNoteTreatmentPlans' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DentalNoteTreatmentPlans] (
        [PlanId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [NoteId] BIGINT NOT NULL,
        [SequenceNo] INT NOT NULL,
        [ProcedureName] NVARCHAR(255) NOT NULL,
        [ToothOrSite] NVARCHAR(100) NULL,
        [Timing] NVARCHAR(100) NULL,
        [Notes] NVARCHAR(MAX) NULL,
        CONSTRAINT FK_DNTP_DentalNotes FOREIGN KEY ([NoteId]) REFERENCES [dentist].[DentalNotes]([NoteId]) ON DELETE CASCADE
    );
END
GO

-- 17. AI Warnings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIWarnings' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[AIWarnings] (
        [WarningId] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [NoteId] BIGINT NOT NULL,
        [FieldName] NVARCHAR(100) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Severity] NVARCHAR(50) NOT NULL CONSTRAINT DF_AIWarnings_Severity DEFAULT 'Warning',
        [Resolved] BIT NOT NULL CONSTRAINT DF_AIWarnings_Resolved DEFAULT 0,
        CONSTRAINT FK_AIWarnings_DentalNotes FOREIGN KEY ([NoteId]) REFERENCES [dentist].[DentalNotes]([NoteId]) ON DELETE CASCADE
    );
END
GO

-- ============================================================================
-- SEED INITIAL CLINICIAN (ahmedjh) IF NOT EXISTS
-- Password is set to a secure bcrypt hash for demo/login
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dentist].[Doctors] WHERE [Username] = 'ahmedjh')
BEGIN
    INSERT INTO [dentist].[Doctors] ([Username], [PasswordHash], [FirstName], [LastName], [Region], [IsSuperAdmin])
    VALUES ('ahmedjh', '$2a$11$eE6mXkO2PskdDk6hYn1sC.Vf2Kx6T7oF4YdYfGzQfJ5h9u1p7a3aO', 'Jhangir', 'Ahmed', 'PK', 1);
END
GO

PRINT 'Lumina Dental Studio - All 16 tables and schema successfully created!';
