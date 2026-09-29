-- ===================================================================================
-- SCRIPT: ORGANIZATIONS_AND_DOCTOR_AFFILIATION_EXPANSION.sql
-- PURPOSE: Production Schema DDL, Indices, Foreign Keys & Seed Data for Healthcare
--          Organizations (Hospitals / Medical Centers) and Doctor Affiliations.
-- PLATFORM: Microsoft SQL Server / Azure SQL / Docker MSSQL
-- CREATED: 2026-09-28
-- ===================================================================================

USE [DentistDB];
GO

-- 1. Ensure 'dentist' schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA [dentist]');
    PRINT '[SUCCESS] Created schema [dentist].';
END
GO

-- ===================================================================================
-- 2. CREATE TABLE: [dentist].[Organizations]
--    Represents Hospitals, Medical Centers, and Dental Health Networks.
-- ===================================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Organizations' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[Organizations] (
        [OrganizationID]      INT IDENTITY(1,1) PRIMARY KEY,
        [Name]                NVARCHAR(200) NOT NULL,
        [LegalName]           NVARCHAR(250) NULL,
        [RegistrationNumber]  NVARCHAR(100) NULL,
        [Type]                NVARCHAR(80)  DEFAULT 'Hospital', -- Hospital, Dental Clinic, Medical Center, Academic Dental Institute
        [Address]             NVARCHAR(300) NULL,
        [City]                NVARCHAR(100) NULL,
        [State]               NVARCHAR(100) NULL,
        [PostalCode]          NVARCHAR(50)  NULL,
        [Country]             NVARCHAR(100) DEFAULT 'Pakistan',
        [Phone]               NVARCHAR(50)  NULL,
        [Email]               NVARCHAR(120) NULL,
        [Website]             NVARCHAR(250) NULL,
        [LogoUrl]             NVARCHAR(500) NULL,
        [Accreditation]       NVARCHAR(200) NULL, -- JCI Accredited, ISO 9001, PMDC Certified, NZDA Approved
        [IsActive]            BIT           DEFAULT 1 NOT NULL,
        [CreatedAt]           DATETIME2     DEFAULT SYSUTCDATETIME() NOT NULL,
        [UpdatedAt]           DATETIME2     DEFAULT SYSUTCDATETIME() NOT NULL
    );
    PRINT '[SUCCESS] Created table [dentist].[Organizations].';
END
ELSE
BEGIN
    PRINT '[INFO] Table [dentist].[Organizations] already exists.';
END
GO

-- ===================================================================================
-- 3. ALTER TABLE: [dentist].[Doctors]
--    Add Direct Primary Organization & Department Columns
-- ===================================================================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Doctors' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[Doctors]') AND name = 'OrganizationID')
    BEGIN
        ALTER TABLE [dentist].[Doctors]
        ADD [OrganizationID] INT NULL;
        PRINT '[SUCCESS] Added column [OrganizationID] to [dentist].[Doctors].';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dentist].[Doctors]') AND name = 'HospitalDepartment')
    BEGIN
        ALTER TABLE [dentist].[Doctors]
        ADD [HospitalDepartment] NVARCHAR(150) NULL DEFAULT 'Department of Dental Surgery';
        PRINT '[SUCCESS] Added column [HospitalDepartment] to [dentist].[Doctors].';
    END

    -- Add Foreign Key constraint if not already present
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Doctors_Organizations')
    BEGIN
        ALTER TABLE [dentist].[Doctors]
        ADD CONSTRAINT [FK_Doctors_Organizations]
        FOREIGN KEY ([OrganizationID]) REFERENCES [dentist].[Organizations]([OrganizationID])
        ON DELETE SET NULL;
        PRINT '[SUCCESS] Added foreign key [FK_Doctors_Organizations].';
    END
END
GO

-- ===================================================================================
-- 4. CREATE JUNCTION TABLE: [dentist].[DoctorOrganizations]
--    Supports Doctors practicing at multiple hospitals, visiting consultancies,
--    or secondary clinical affiliations.
-- ===================================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DoctorOrganizations' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[DoctorOrganizations] (
        [DoctorOrgID]     INT IDENTITY(1,1) PRIMARY KEY,
        [DoctorID]        INT NOT NULL,
        [OrganizationID]  INT NOT NULL,
        [RoleTitle]       NVARCHAR(120) DEFAULT 'Attending Dental Surgeon',
        [Department]      NVARCHAR(150) DEFAULT 'Oral & Maxillofacial Dentistry',
        [IsPrimary]       BIT           DEFAULT 1 NOT NULL,
        [StartDate]       DATE          NULL,
        [EndDate]         DATE          NULL,
        [IsActive]        BIT           DEFAULT 1 NOT NULL,
        [CreatedAt]       DATETIME2     DEFAULT SYSUTCDATETIME() NOT NULL,
        CONSTRAINT [FK_DocOrg_Doctor] FOREIGN KEY ([DoctorID]) REFERENCES [dentist].[Doctors]([DoctorID]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocOrg_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dentist].[Organizations]([OrganizationID]) ON DELETE CASCADE
    );
    PRINT '[SUCCESS] Created table [dentist].[DoctorOrganizations].';
END
ELSE
BEGIN
    PRINT '[INFO] Table [dentist].[DoctorOrganizations] already exists.';
END
GO

-- ===================================================================================
-- 5. PERFORMANCE INDICES
-- ===================================================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Doctors_OrganizationID' AND object_id = OBJECT_ID('[dentist].[Doctors]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Doctors_OrganizationID] 
    ON [dentist].[Doctors]([OrganizationID])
    INCLUDE ([DoctorID], [FirstName], [LastName], [Specialization], [IsActive]);
    PRINT '[SUCCESS] Created index [IX_Doctors_OrganizationID].';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocOrg_DoctorID' AND object_id = OBJECT_ID('[dentist].[DoctorOrganizations]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocOrg_DoctorID] 
    ON [dentist].[DoctorOrganizations]([DoctorID], [IsActive]);
    PRINT '[SUCCESS] Created index [IX_DocOrg_DoctorID].';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocOrg_OrgID' AND object_id = OBJECT_ID('[dentist].[DoctorOrganizations]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocOrg_OrgID] 
    ON [dentist].[DoctorOrganizations]([OrganizationID], [IsActive]);
    PRINT '[SUCCESS] Created index [IX_DocOrg_OrgID].';
END
GO

-- ===================================================================================
-- 6. SEED PREMIER HEALTHCARE ORGANIZATIONS / HOSPITALS
-- ===================================================================================
SET IDENTITY_INSERT [dentist].[Organizations] ON;

MERGE [dentist].[Organizations] AS target
USING (VALUES 
    (1, N'Shifa International Hospitals Ltd', N'Shifa International Hospitals Ltd', N'HOSP-PK-001', N'Tertiary Care Hospital', N'Sector H-8/4, Pitras Bukhari Road', N'Islamabad', N'ICT', N'44000', N'Pakistan', N'+92 51 8463000', N'info@shifa.com.pk', N'https://www.shifa.com.pk', N'https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&q=80&w=200', N'JCI Accredited & ISO 9001', 1),
    (2, N'Aga Khan University Hospital', N'The Aga Khan University Hospital', N'HOSP-PK-002', N'Academic Medical Center & University Hospital', N'Stadium Road, P.O. Box 3500', N'Karachi', N'Sindh', N'74800', N'Pakistan', N'+92 21 111 911 911', N'akuh.information@aku.edu', N'https://hospitals.aku.edu', N'https://images.unsplash.com/photo-1519494026892-80bbd2d6fd0d?auto=format&fit=crop&q=80&w=200', N'JCI Accredited & CAP Certified', 1),
    (3, N'Dentia Auckland Regional Dental Hospital', N'Dentia Regional Health Network Ltd', N'HOSP-NZ-103', N'Dental Specialist Hospital', N'122 Queen Street, CBD', N'Auckland', N'Auckland', N'1010', N'New Zealand', N'+64 9 300 4567', N'auckland@dentiaclinic.com', N'https://dentiaauckland.co.nz', N'https://images.unsplash.com/photo-1629909613654-28e377c37b09?auto=format&fit=crop&q=80&w=200', N'NZ Dental Council Approved & Ministry of Health', 1),
    (4, N'Starship Children’s Dental Hospital', N'Auckland District Health Board', N'HOSP-NZ-104', N'Pediatric & Specialized Surgery Hospital', N'2 Park Road, Grafton', N'Auckland', N'Auckland', N'1023', N'New Zealand', N'+64 9 307 4949', N'pediatric@starshiphealth.org', N'https://starship.org.nz', N'https://images.unsplash.com/photo-1516549655169-df83a0774514?auto=format&fit=crop&q=80&w=200', N'Australasian Pediatric Dental Board', 1)
) AS source (
    [OrganizationID], [Name], [LegalName], [RegistrationNumber], [Type], [Address], [City], [State], [PostalCode], [Country], [Phone], [Email], [Website], [LogoUrl], [Accreditation], [IsActive]
)
ON (target.[OrganizationID] = source.[OrganizationID])
WHEN MATCHED THEN
    UPDATE SET 
        target.[Name]               = source.[Name],
        target.[LegalName]          = source.[LegalName],
        target.[RegistrationNumber] = source.[RegistrationNumber],
        target.[Type]               = source.[Type],
        target.[Address]            = source.[Address],
        target.[City]               = source.[City],
        target.[State]              = source.[State],
        target.[PostalCode]         = source.[PostalCode],
        target.[Country]            = source.[Country],
        target.[Phone]              = source.[Phone],
        target.[Email]              = source.[Email],
        target.[Website]            = source.[Website],
        target.[LogoUrl]            = source.[LogoUrl],
        target.[Accreditation]      = source.[Accreditation],
        target.[IsActive]           = source.[IsActive],
        target.[UpdatedAt]          = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT ([OrganizationID], [Name], [LegalName], [RegistrationNumber], [Type], [Address], [City], [State], [PostalCode], [Country], [Phone], [Email], [Website], [LogoUrl], [Accreditation], [IsActive])
    VALUES (source.[OrganizationID], source.[Name], source.[LegalName], source.[RegistrationNumber], source.[Type], source.[Address], source.[City], source.[State], source.[PostalCode], source.[Country], source.[Phone], source.[Email], source.[Website], source.[LogoUrl], source.[Accreditation], source.[IsActive]);

SET IDENTITY_INSERT [dentist].[Organizations] OFF;
PRINT '[SUCCESS] Seeded/Synchronized 4 Premier Healthcare Organizations.';
GO

-- ===================================================================================
-- 7. ASSIGN EXISTING DOCTORS TO ORGANIZATIONS (HOSPITALS)
-- ===================================================================================
-- Shifa International Hospital (Islamabad)
UPDATE [dentist].[Doctors]
SET [OrganizationID] = 1,
    [HospitalDepartment] = N'Department of Oral Implantology & Advanced Restorative Surgery'
WHERE [OrganizationID] IS NULL AND ([FirstName] LIKE '%Dentist%' OR [Specialization] LIKE '%Implant%');

-- Aga Khan University Hospital (Karachi)
UPDATE [dentist].[Doctors]
SET [OrganizationID] = 2,
    [HospitalDepartment] = N'Department of Orthodontics & Craniofacial Orthopedics'
WHERE [OrganizationID] IS NULL AND [Specialization] LIKE '%Ortho%';

-- Dentia Auckland Regional Dental Hospital
UPDATE [dentist].[Doctors]
SET [OrganizationID] = 3,
    [HospitalDepartment] = N'Department of Comprehensive Restorative & Esthetic Dentistry'
WHERE [OrganizationID] IS NULL;

-- Ensure Junction table has active records for primary doctors
INSERT INTO [dentist].[DoctorOrganizations] ([DoctorID], [OrganizationID], [RoleTitle], [Department], [IsPrimary], [StartDate], [IsActive])
SELECT 
    d.[DoctorID], 
    d.[OrganizationID], 
    ISNULL(d.[Title], N'Consultant Dental Surgeon'), 
    ISNULL(d.[HospitalDepartment], N'Department of Dental Surgery'), 
    1, 
    CAST('2022-01-01' AS DATE), 
    1
FROM [dentist].[Doctors] d
WHERE d.[OrganizationID] IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM [dentist].[DoctorOrganizations] do 
      WHERE do.[DoctorID] = d.[DoctorID] AND do.[OrganizationID] = d.[OrganizationID]
  );

PRINT '[SUCCESS] Assigned existing doctors to primary hospitals and populated junction table.';
GO

-- ===================================================================================
-- 8. VERIFICATION QUERIES
-- ===================================================================================
PRINT '--- VERIFICATION: ORGANIZATIONS DIRECTORY ---';
SELECT 
    [OrganizationID],
    [Name],
    [City],
    [Type],
    [Accreditation],
    [Phone],
    [IsActive]
FROM [dentist].[Organizations];

PRINT '--- VERIFICATION: DOCTORS WITH HOSPITAL AFFILIATIONS ---';
SELECT 
    d.[DoctorID],
    d.[FirstName] + ' ' + d.[LastName] AS [DoctorName],
    d.[Specialization],
    o.[Name] AS [HospitalName],
    d.[HospitalDepartment],
    o.[City] AS [HospitalCity],
    d.[IsActive]
FROM [dentist].[Doctors] d
LEFT JOIN [dentist].[Organizations] o ON d.[OrganizationID] = o.[OrganizationID];
GO
