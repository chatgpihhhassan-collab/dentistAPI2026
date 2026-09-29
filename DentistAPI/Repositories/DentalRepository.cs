using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DentistAPI.Models;

namespace DentistAPI.Repositories
{
    public class DentalRepository
    {
        private readonly string _connectionString;

        public DentalRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=DentistDB;Trusted_Connection=True;MultipleActiveResultSets=true";
            InitializeSchema();
        }

        private void InitializeSchema()
        {
            try
            {
                using var connection = CreateConnection();
                // 1. Ensure dentist schema exists
                connection.Execute(@"
                    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
                    BEGIN
                        EXEC('CREATE SCHEMA dentist');
                    END");

                // 2. Add columns via dynamic SQL so batch compilation never fails
                connection.Execute(@"
                    IF COL_LENGTH('dentist.Patients', 'ProfileImage') IS NULL
                        EXEC('ALTER TABLE [dentist].[Patients] ADD [ProfileImage] VARBINARY(MAX) NULL');

                    IF COL_LENGTH('dentist.Patients', 'ProfileImageMimeType') IS NULL
                        EXEC('ALTER TABLE [dentist].[Patients] ADD [ProfileImageMimeType] VARCHAR(50) NULL');

                    IF COL_LENGTH('dentist.Patients', 'DentitionType') IS NULL
                        EXEC('ALTER TABLE [dentist].[Patients] ADD [DentitionType] NVARCHAR(20) NOT NULL CONSTRAINT DF_Patients_DentitionType DEFAULT ''Adult''');

                    IF COL_LENGTH('dentist.TeethState', 'Comments') IS NULL
                        EXEC('ALTER TABLE [dentist].[TeethState] ADD [Comments] NVARCHAR(MAX) NULL');

                    IF COL_LENGTH('dentist.TeethState', 'DentitionCategory') IS NULL
                        EXEC('ALTER TABLE [dentist].[TeethState] ADD [DentitionCategory] NVARCHAR(20) NOT NULL CONSTRAINT DF_TeethState_DentitionCategory DEFAULT ''Adult''');

                    IF COL_LENGTH('dentist.TeethState', 'ToothKey') IS NULL
                        EXEC('ALTER TABLE [dentist].[TeethState] ADD [ToothKey] NVARCHAR(10) NULL');

                    IF COL_LENGTH('dentist.TeethState', 'DoctorID') IS NULL
                        EXEC('ALTER TABLE [dentist].[TeethState] ADD [DoctorID] INT NULL');

                    IF COL_LENGTH('dentist.TreatmentHistory', 'DentitionCategory') IS NULL
                        EXEC('ALTER TABLE [dentist].[TreatmentHistory] ADD [DentitionCategory] NVARCHAR(20) NOT NULL CONSTRAINT DF_TreatmentHistory_DentitionCategory DEFAULT ''Adult''');

                    IF COL_LENGTH('dentist.TreatmentHistory', 'ToothKey') IS NULL
                        EXEC('ALTER TABLE [dentist].[TreatmentHistory] ADD [ToothKey] NVARCHAR(10) NULL');

                    IF COL_LENGTH('dentist.TreatmentHistory', 'DoctorID') IS NULL
                        EXEC('ALTER TABLE [dentist].[TreatmentHistory] ADD [DoctorID] INT NULL');

                    IF COL_LENGTH('dentist.DentalNotes', 'DentitionCategory') IS NULL
                        EXEC('ALTER TABLE [dentist].[DentalNotes] ADD [DentitionCategory] NVARCHAR(20) NULL');
                ");

                // 3. Enlarge column widths
                connection.Execute(@"
                    BEGIN TRY
                        EXEC('ALTER TABLE [dentist].[TeethState] ALTER COLUMN [ConditionStatus] NVARCHAR(500) NOT NULL');
                    END TRY BEGIN CATCH END CATCH;

                    BEGIN TRY
                        EXEC('ALTER TABLE [dentist].[TreatmentHistory] ALTER COLUMN [TreatmentPerformed] NVARCHAR(500) NOT NULL');
                    END TRY BEGIN CATCH END CATCH;
                ");

                // 4. Safe Data Backfill via dynamic SQL
                connection.Execute(@"
                    EXEC('
                        UPDATE [dentist].[Patients]
                        SET [DentitionType] = CASE 
                            WHEN DATEDIFF(YEAR, DOB, GETDATE()) < 13 THEN ''Pediatric''
                            ELSE ''Adult''
                        END
                        WHERE [DentitionType] IS NULL OR [DentitionType] = '''';

                        UPDATE [dentist].[Patients] SET [DentitionType] = ''Pediatric'' WHERE PatientID = 20;
                        UPDATE [dentist].[Patients] SET [DentitionType] = ''Adult'' WHERE PatientID IN (18, 19);

                        UPDATE [dentist].[TeethState] 
                        SET [DentitionCategory] = ''Pediatric'',
                            [ToothKey] = CASE ToothNumber
                                WHEN 1 THEN ''A'' WHEN 2 THEN ''B'' WHEN 3 THEN ''C'' WHEN 4 THEN ''D'' WHEN 5 THEN ''E''
                                WHEN 6 THEN ''F'' WHEN 7 THEN ''G'' WHEN 8 THEN ''H'' WHEN 9 THEN ''I'' WHEN 10 THEN ''J''
                                WHEN 11 THEN ''K'' WHEN 12 THEN ''L'' WHEN 13 THEN ''M'' WHEN 14 THEN ''N'' WHEN 15 THEN ''O''
                                WHEN 16 THEN ''P'' WHEN 17 THEN ''Q'' WHEN 18 THEN ''R'' WHEN 19 THEN ''S'' WHEN 20 THEN ''T''
                                ELSE CAST(ToothNumber AS NVARCHAR(10))
                            END
                        WHERE PatientID = 20 AND ([ToothKey] IS NULL OR [ToothKey] = '''' OR [DentitionCategory] <> ''Pediatric'');

                        UPDATE [dentist].[TeethState]
                        SET [DentitionCategory] = ''Adult'',
                            [ToothKey] = CAST(ToothNumber AS NVARCHAR(10))
                        WHERE PatientID <> 20 AND ([ToothKey] IS NULL OR [ToothKey] = '''');
                    ');
                ");

                // 5. Ensure Clinical Specialty Tables Exist
                connection.Execute(@"
                    IF OBJECT_ID('dentist.ImplantPlans', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dentist].[ImplantPlans] (
                            [ImplantPlanID]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [PatientID]             INT NOT NULL,
                            [DoctorID]              INT NULL,
                            [ToothNumber]           INT NOT NULL,
                            [ToothKey]              NVARCHAR(10) NULL,
                            [ImplantBrand]          NVARCHAR(100) NULL,
                            [ImplantLength]         DECIMAL(4,1) NOT NULL,
                            [ImplantDiameter]       DECIMAL(4,1) NOT NULL,
                            [BoneQuality]           NVARCHAR(10) NOT NULL,
                            [BoneHeightAvailable]   DECIMAL(4,1) NULL,
                            [BoneWidthAvailable]    DECIMAL(4,1) NULL,
                            [GraftingRequired]      BIT NOT NULL CONSTRAINT DF_ImplantPlans_Grafting DEFAULT (0),
                            [SinusLiftStatus]       NVARCHAR(50) NOT NULL CONSTRAINT DF_ImplantPlans_SinusLift DEFAULT ('None'),
                            [CbctReferenceUrl]      NVARCHAR(1000) NULL,
                            [DigitalPlanningNotes]  NVARCHAR(MAX) NULL,
                            [GuidedSurgeryFlag]     BIT NOT NULL CONSTRAINT DF_ImplantPlans_GuidedSurgery DEFAULT (0),
                            [PlanStatus]            NVARCHAR(30) NOT NULL CONSTRAINT DF_ImplantPlans_PlanStatus DEFAULT ('Planned'),
                            [PlannedDate]           DATETIME2 NULL,
                            [PlacementDate]         DATETIME2 NULL,
                            [CreatedAt]             DATETIME2 NOT NULL CONSTRAINT DF_ImplantPlans_CreatedAt DEFAULT (SYSUTCDATETIME()),
                            [UpdatedAt]             DATETIME2 NOT NULL CONSTRAINT DF_ImplantPlans_UpdatedAt DEFAULT (SYSUTCDATETIME())
                        );
                        CREATE NONCLUSTERED INDEX [IX_ImplantPlans_PatientID] ON [dentist].[ImplantPlans]([PatientID]);
                        CREATE NONCLUSTERED INDEX [IX_ImplantPlans_ToothNumber] ON [dentist].[ImplantPlans]([ToothNumber]);
                    END;

                    IF OBJECT_ID('dentist.BiopsyRecords', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dentist].[BiopsyRecords] (
                            [BiopsyID]                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [PatientID]                 INT NOT NULL,
                            [DoctorID]                  INT NULL,
                            [BiopsyType]                NVARCHAR(20) NOT NULL,
                            [SiteOfBiopsy]              NVARCHAR(255) NOT NULL,
                            [ToothNumber]               INT NULL,
                            [ToothKey]                  NVARCHAR(10) NULL,
                            [ClinicalImpression]        NVARCHAR(MAX) NULL,
                            [PathologyLabName]          NVARCHAR(200) NULL,
                            [SpecimenReference]         NVARCHAR(100) NULL,
                            [BiopsyDate]                DATE NOT NULL CONSTRAINT DF_BiopsyRecords_BiopsyDate DEFAULT (CAST(GETDATE() AS DATE)),
                            [Status]                    NVARCHAR(50) NOT NULL CONSTRAINT DF_BiopsyRecords_Status DEFAULT ('Specimen Sent'),
                            [HistopathologyDiagnosis]   NVARCHAR(MAX) NULL,
                            [ResultsNotes]              NVARCHAR(MAX) NULL,
                            [FollowUpRequired]          BIT NOT NULL CONSTRAINT DF_BiopsyRecords_FollowUp DEFAULT (1),
                            [FollowUpDate]              DATE NULL,
                            [CreatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_BiopsyRecords_CreatedAt DEFAULT (SYSUTCDATETIME()),
                            [UpdatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_BiopsyRecords_UpdatedAt DEFAULT (SYSUTCDATETIME())
                        );
                        CREATE NONCLUSTERED INDEX [IX_BiopsyRecords_PatientID] ON [dentist].[BiopsyRecords]([PatientID]);
                    END;

                    IF OBJECT_ID('dentist.OrthoAlignerTreatments', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dentist].[OrthoAlignerTreatments] (
                            [OrthoAlignerID]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [PatientID]                 INT NOT NULL,
                            [DoctorID]                  INT NULL,
                            [AlignerBrand]              NVARCHAR(100) NOT NULL,
                            [TotalStages]               INT NOT NULL CONSTRAINT DF_OrthoAligners_TotalStages DEFAULT (1),
                            [CurrentStage]              INT NOT NULL CONSTRAINT DF_OrthoAligners_CurrentStage DEFAULT (1),
                            [AttachmentsRequired]       BIT NOT NULL CONSTRAINT DF_OrthoAligners_Attachments DEFAULT (0),
                            [AttachmentNotes]           NVARCHAR(MAX) NULL,
                            [IprRequired]               BIT NOT NULL CONSTRAINT DF_OrthoAligners_Ipr DEFAULT (0),
                            [IprDetails]                NVARCHAR(MAX) NULL,
                            [WearSchedule]              NVARCHAR(100) NOT NULL CONSTRAINT DF_OrthoAligners_WearSchedule DEFAULT ('7 Days/Tray'),
                            [RefinementScanTracking]    NVARCHAR(MAX) NULL,
                            [RefinementCount]           INT NOT NULL CONSTRAINT DF_OrthoAligners_RefinementCount DEFAULT (0),
                            [Arch]                      NVARCHAR(20) NOT NULL CONSTRAINT DF_OrthoAligners_Arch DEFAULT ('Dual'),
                            [Status]                    NVARCHAR(50) NOT NULL CONSTRAINT DF_OrthoAligners_Status DEFAULT ('Active'),
                            [StartDate]                 DATE NULL,
                            [TargetCompletionDate]      DATE NULL,
                            [ClinicalNotes]             NVARCHAR(MAX) NULL,
                            [CreatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_OrthoAligners_CreatedAt DEFAULT (SYSUTCDATETIME()),
                            [UpdatedAt]                 DATETIME2 NOT NULL CONSTRAINT DF_OrthoAligners_UpdatedAt DEFAULT (SYSUTCDATETIME())
                        );
                        CREATE NONCLUSTERED INDEX [IX_OrthoAligners_PatientID] ON [dentist].[OrthoAlignerTreatments]([PatientID]);
                    END;
                ");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Database schema check: {ex.Message}");
            }
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<int> RegisterDoctorAsync(string username, string passwordHash, string firstName, string lastName, string region)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[Doctors] (Username, PasswordHash, FirstName, LastName, Region) 
                OUTPUT INSERTED.DoctorID 
                VALUES (@Username, @PasswordHash, @FirstName, @LastName, @Region)";
            return await connection.QuerySingleAsync<int>(query, new { Username = username, PasswordHash = passwordHash, FirstName = firstName, LastName = lastName, Region = region });
        }

        public async Task<Doctor> GetDoctorByUsernameAsync(string username)
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Doctor>("SELECT * FROM [dentist].[Doctors] WHERE Username = @Username", new { Username = username });
        }

        private static bool _doctorColumnsEnsured = false;
        private async Task EnsureDoctorColumnsExistAsync(IDbConnection connection)
        {
            if (_doctorColumnsEnsured) return;
            try
            {
                string ddl = @"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Specialization')
                        ALTER TABLE [dentist].[Doctors] ADD Specialization NVARCHAR(150) NULL DEFAULT 'General Dental Surgeon';
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Title')
                        ALTER TABLE [dentist].[Doctors] ADD Title NVARCHAR(100) NULL DEFAULT 'BDS, RDS';
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'YearsOfExperience')
                        ALTER TABLE [dentist].[Doctors] ADD YearsOfExperience INT NULL DEFAULT 5;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Biography')
                        ALTER TABLE [dentist].[Doctors] ADD Biography NVARCHAR(MAX) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'OrganizationWorkHistory')
                        ALTER TABLE [dentist].[Doctors] ADD OrganizationWorkHistory NVARCHAR(MAX) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Education')
                        ALTER TABLE [dentist].[Doctors] ADD Education NVARCHAR(MAX) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Certifications')
                        ALTER TABLE [dentist].[Doctors] ADD Certifications NVARCHAR(MAX) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ConsultationFee')
                        ALTER TABLE [dentist].[Doctors] ADD ConsultationFee DECIMAL(18,2) NULL DEFAULT 100.00;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ProfileImageUrl')
                        ALTER TABLE [dentist].[Doctors] ADD ProfileImageUrl NVARCHAR(500) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Languages')
                        ALTER TABLE [dentist].[Doctors] ADD Languages NVARCHAR(200) NULL DEFAULT 'English, Urdu';
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Rating')
                        ALTER TABLE [dentist].[Doctors] ADD Rating DECIMAL(3,2) NULL DEFAULT 4.90;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ReviewCount')
                        ALTER TABLE [dentist].[Doctors] ADD ReviewCount INT NULL DEFAULT 25;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'IsActive')
                        ALTER TABLE [dentist].[Doctors] ADD IsActive BIT NULL DEFAULT 1;

                    -- Organizations master table
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dentist].[Organizations]'))
                    BEGIN
                        CREATE TABLE [dentist].[Organizations] (
                            [OrganizationID] INT IDENTITY(1,1) PRIMARY KEY,
                            [Name] NVARCHAR(150) NOT NULL,
                            [Slug] NVARCHAR(100) NULL,
                            [Type] NVARCHAR(50) NOT NULL DEFAULT 'Hospital',
                            [Address] NVARCHAR(255) NULL,
                            [City] NVARCHAR(100) NULL,
                            [Country] NVARCHAR(50) NULL DEFAULT 'NZ',
                            [Phone] NVARCHAR(50) NULL,
                            [Email] NVARCHAR(100) NULL,
                            [Website] NVARCHAR(200) NULL,
                            [LogoUrl] NVARCHAR(500) NULL,
                            [HeroImageUrl] NVARCHAR(500) NULL,
                            [Description] NVARCHAR(MAX) NULL,
                            [Accreditation] NVARCHAR(150) NULL,
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    -- Link Doctors to Organizations
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'OrganizationID')
                    BEGIN
                        ALTER TABLE [dentist].[Doctors] ADD [OrganizationID] INT NULL;
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'HospitalDepartment')
                    BEGIN
                        ALTER TABLE [dentist].[Doctors] ADD [HospitalDepartment] NVARCHAR(100) NULL DEFAULT 'Department of Oral Surgery & Dentistry';
                    END

                    -- Junction table for multiple affiliations
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dentist].[DoctorOrganizations]'))
                    BEGIN
                        CREATE TABLE [dentist].[DoctorOrganizations] (
                            [DoctorOrganizationID] INT IDENTITY(1,1) PRIMARY KEY,
                            [DoctorID] INT NOT NULL,
                            [OrganizationID] INT NOT NULL,
                            [RoleInOrg] NVARCHAR(100) NULL DEFAULT 'Attending Specialist',
                            [Department] NVARCHAR(100) NULL,
                            [ConsultationDays] NVARCHAR(100) NULL,
                            [IsPrimary] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END
                ";
                await connection.ExecuteAsync(ddl);

                // Seed initial verified organizations if empty
                string seedSql = @"
                    IF NOT EXISTS (SELECT 1 FROM [dentist].[Organizations])
                    BEGIN
                        INSERT INTO [dentist].[Organizations] ([Name], [Slug], [Type], [Address], [City], [Country], [Phone], [Email], [Website], [LogoUrl], [Description], [Accreditation], [IsActive])
                        VALUES 
                        (N'Shifa International Hospitals Ltd', N'shifa-international', N'Hospital', N'Pitras Bukhari Rd, H-8/4', N'Islamabad', N'PK', N'+92 51 8463000', N'info@shifa.com.pk', N'https://shifa.com.pk', N'https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&q=80&w=300', N'JCI Accredited tertiary healthcare hospital featuring state-of-the-art maxillofacial surgery suites and emergency dental trauma units.', N'JCI Accredited', 1),
                        (N'Aga Khan University Hospital (AKUH)', N'akuh-karachi', N'Hospital', N'Stadium Rd', N'Karachi', N'PK', N'+92 21 111 911 911', N'contact@aku.edu', N'https://hospitals.aku.edu', N'https://images.unsplash.com/photo-1519494026892-80bbd2d6fd0d?auto=format&fit=crop&q=80&w=300', N'Premier academic medical center and quaternary referral hospital pioneering advanced orthodontic research and facial reconstructive surgery.', N'JCI & ISO 9001 Accredited', 1),
                        (N'Dentia Auckland Regional Dental Hospital', N'dentia-auckland', N'Dental Clinic', N'100 Queen Street', N'Auckland', N'NZ', N'+64 9 300 1234', N'auckland@dentia.co.nz', N'https://dentiaclinic.com', N'https://images.unsplash.com/photo-1629909613654-28e377c37b09?auto=format&fit=crop&q=80&w=300', N'Leading specialist dental facility with 12 operatory suites, computer-guided surgical implant technology, and digital smile design studios.', N'NZ Dental Council Accredited', 1),
                        (N'Starship Children’s Dental Specialist Hospital', N'starship-dental', N'Hospital', N'Park Road, Grafton', N'Auckland', N'NZ', N'+64 9 307 4949', N'starship@adhb.govt.nz', N'https://starship.org.nz', N'https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&q=80&w=300', N'Specialized pediatric dental health center providing sedation dentistry, interceptive orthodontics, and cleft palate care.', N'Royal Australasian College Accredited', 1);

                        -- Link existing doctors
                        UPDATE [dentist].[Doctors] SET OrganizationID = 1, HospitalDepartment = N'Division of Oral & Maxillofacial Surgery' WHERE FirstName LIKE '%Haider%' OR DoctorID = 2;
                        UPDATE [dentist].[Doctors] SET OrganizationID = 2, HospitalDepartment = N'Department of Orthodontics & Dentofacial Orthopedics' WHERE FirstName LIKE '%Sarah%' OR DoctorID = 4;
                        UPDATE [dentist].[Doctors] SET OrganizationID = 3, HospitalDepartment = N'Department of Restorative & Cosmetic Dentistry' WHERE OrganizationID IS NULL;
                    END
                ";
                await connection.ExecuteAsync(seedSql);

                _doctorColumnsEnsured = true;
            }
            catch
            {
                // Safe ignore if permission restricted or already handled
            }
        }

        public virtual async Task<Doctor> GetDoctorByIdAsync(int id)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            return await connection.QuerySingleOrDefaultAsync<Doctor>(@"
                SELECT d.DoctorID, d.Username, d.FirstName, d.LastName, d.Region, d.CreatedAt, d.IsSuperAdmin,
                       ISNULL(d.Specialization, 'General Dental Surgeon') AS Specialization,
                       ISNULL(d.Title, 'BDS, RDS') AS Title,
                       ISNULL(d.YearsOfExperience, 5) AS YearsOfExperience,
                       d.Biography,
                       d.OrganizationWorkHistory,
                       d.Education,
                       d.Certifications,
                       ISNULL(d.ConsultationFee, 100.00) AS ConsultationFee,
                       d.ProfileImageUrl,
                       ISNULL(d.Languages, 'English, Urdu') AS Languages,
                       ISNULL(d.Rating, 4.90) AS Rating,
                       ISNULL(d.ReviewCount, 25) AS ReviewCount,
                       ISNULL(d.IsActive, 1) AS IsActive,
                       d.OrganizationID,
                       ISNULL(d.HospitalDepartment, 'Department of Oral Surgery & Dentistry') AS HospitalDepartment,
                       o.Name AS OrganizationName,
                       o.LogoUrl AS OrganizationLogoUrl,
                       o.City AS OrganizationCity
                FROM [dentist].[Doctors] d
                LEFT JOIN [dentist].[Organizations] o ON o.OrganizationID = d.OrganizationID
                WHERE d.DoctorID = @Id", new { Id = id });
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            return await connection.QueryAsync<Doctor>(@"
                SELECT d.DoctorID, d.Username, d.FirstName, d.LastName, d.Region, d.CreatedAt, d.IsSuperAdmin,
                       ISNULL(d.Specialization, 'General Dental Surgeon') AS Specialization,
                       ISNULL(d.Title, 'BDS, RDS') AS Title,
                       ISNULL(d.YearsOfExperience, 5) AS YearsOfExperience,
                       d.Biography,
                       d.OrganizationWorkHistory,
                       d.Education,
                       d.Certifications,
                       ISNULL(d.ConsultationFee, 100.00) AS ConsultationFee,
                       d.ProfileImageUrl,
                       ISNULL(d.Languages, 'English, Urdu') AS Languages,
                       ISNULL(d.Rating, 4.90) AS Rating,
                       ISNULL(d.ReviewCount, 25) AS ReviewCount,
                       ISNULL(d.IsActive, 1) AS IsActive,
                       d.OrganizationID,
                       ISNULL(d.HospitalDepartment, 'Department of Oral Surgery & Dentistry') AS HospitalDepartment,
                       o.Name AS OrganizationName,
                       o.LogoUrl AS OrganizationLogoUrl,
                       o.City AS OrganizationCity
                FROM [dentist].[Doctors] d
                LEFT JOIN [dentist].[Organizations] o ON o.OrganizationID = d.OrganizationID
                WHERE d.IsSuperAdmin = 0 AND (d.IsActive IS NULL OR d.IsActive = 1)
                ORDER BY d.DoctorID ASC");
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsForAdminAsync()
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            return await connection.QueryAsync<Doctor>(@"
                SELECT d.DoctorID, d.Username, d.FirstName, d.LastName, d.Region, d.CreatedAt, d.IsSuperAdmin,
                       ISNULL(d.Specialization, 'General Dental Surgeon') AS Specialization,
                       ISNULL(d.Title, 'BDS, RDS') AS Title,
                       ISNULL(d.YearsOfExperience, 5) AS YearsOfExperience,
                       d.Biography,
                       d.OrganizationWorkHistory,
                       d.Education,
                       d.Certifications,
                       ISNULL(d.ConsultationFee, 100.00) AS ConsultationFee,
                       d.ProfileImageUrl,
                       ISNULL(d.Languages, 'English, Urdu') AS Languages,
                       ISNULL(d.Rating, 4.90) AS Rating,
                       ISNULL(d.ReviewCount, 25) AS ReviewCount,
                       ISNULL(d.IsActive, 1) AS IsActive,
                       d.OrganizationID,
                       ISNULL(d.HospitalDepartment, 'Department of Oral Surgery & Dentistry') AS HospitalDepartment,
                       o.Name AS OrganizationName,
                       o.LogoUrl AS OrganizationLogoUrl,
                       o.City AS OrganizationCity
                FROM [dentist].[Doctors] d
                LEFT JOIN [dentist].[Organizations] o ON o.OrganizationID = d.OrganizationID
                ORDER BY d.IsSuperAdmin DESC, d.DoctorID ASC");
        }

        public async Task<int> UpdateDoctorAsync(int id, string firstName, string lastName, string region)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET FirstName = @FirstName, LastName = @LastName, Region = @Region WHERE DoctorID = @Id", new { FirstName = firstName, LastName = lastName, Region = region, Id = id });
        }

        public async Task<int> UpdateDoctorFullProfileAsync(int id, UpdateDoctorRequest request)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                UPDATE [dentist].[Doctors] SET 
                    FirstName = @FirstName, 
                    LastName = @LastName, 
                    Region = @Region,
                    Specialization = @Specialization,
                    Title = @Title,
                    YearsOfExperience = @YearsOfExperience,
                    Biography = @Biography,
                    OrganizationWorkHistory = @OrganizationWorkHistory,
                    Education = @Education,
                    Certifications = @Certifications,
                    ConsultationFee = @ConsultationFee,
                    ProfileImageUrl = @ProfileImageUrl,
                    Languages = @Languages,
                    Rating = @Rating,
                    ReviewCount = @ReviewCount,
                    IsActive = @IsActive,
                    OrganizationID = @OrganizationID,
                    HospitalDepartment = @HospitalDepartment
                WHERE DoctorID = @Id";
            return await connection.ExecuteAsync(sql, new
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Region = request.Region,
                Specialization = request.Specialization,
                Title = request.Title,
                YearsOfExperience = request.YearsOfExperience,
                Biography = request.Biography,
                OrganizationWorkHistory = request.OrganizationWorkHistory,
                Education = request.Education,
                Certifications = request.Certifications,
                ConsultationFee = request.ConsultationFee,
                ProfileImageUrl = request.ProfileImageUrl,
                Languages = request.Languages,
                Rating = request.Rating,
                ReviewCount = request.ReviewCount,
                IsActive = request.IsActive,
                OrganizationID = request.OrganizationID,
                HospitalDepartment = request.HospitalDepartment ?? "Department of Oral Surgery & Dentistry"
            });
        }

        public async Task<int> CreateDoctorWithProfileAsync(CreateDoctorProfileRequest request, string passwordHash)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                INSERT INTO [dentist].[Doctors] (
                    Username, PasswordHash, FirstName, LastName, Region, CreatedAt, IsSuperAdmin,
                    Specialization, Title, YearsOfExperience, Biography, OrganizationWorkHistory,
                    Education, Certifications, ConsultationFee, ProfileImageUrl, Languages, Rating, ReviewCount, IsActive,
                    OrganizationID, HospitalDepartment
                ) 
                OUTPUT INSERTED.DoctorID 
                VALUES (
                    @Username, @PasswordHash, @FirstName, @LastName, @Region, GETDATE(), 0,
                    @Specialization, @Title, @YearsOfExperience, @Biography, @OrganizationWorkHistory,
                    @Education, @Certifications, @ConsultationFee, @ProfileImageUrl, @Languages, @Rating, @ReviewCount, @IsActive,
                    @OrganizationID, @HospitalDepartment
                )";
            int newDoctorId = await connection.QuerySingleAsync<int>(sql, new
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Region = string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region,
                Specialization = request.Specialization ?? "General Dental Surgeon",
                Title = request.Title ?? "BDS, RDS",
                YearsOfExperience = request.YearsOfExperience,
                Biography = request.Biography,
                OrganizationWorkHistory = request.OrganizationWorkHistory,
                Education = request.Education,
                Certifications = request.Certifications,
                ConsultationFee = request.ConsultationFee > 0 ? request.ConsultationFee : 100.00m,
                ProfileImageUrl = request.ProfileImageUrl,
                Languages = request.Languages ?? "English, Urdu",
                Rating = request.Rating > 0 ? request.Rating : 4.90m,
                ReviewCount = request.ReviewCount > 0 ? request.ReviewCount : 10,
                IsActive = request.IsActive,
                OrganizationID = request.OrganizationID,
                HospitalDepartment = request.HospitalDepartment ?? "Department of Oral Surgery & Dentistry"
            });

            if (request.OrganizationID.HasValue && request.OrganizationID.Value > 0)
            {
                try
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO [dentist].[DoctorOrganizations] (DoctorID, OrganizationID, RoleTitle, Department, IsPrimary, StartDate, IsActive, CreatedAt)
                        VALUES (@DoctorID, @OrganizationID, @RoleTitle, @Department, 1, CAST(GETDATE() AS DATE), 1, SYSUTCDATETIME())",
                        new
                        {
                            DoctorID = newDoctorId,
                            OrganizationID = request.OrganizationID.Value,
                            RoleTitle = request.Title ?? "Attending Dental Surgeon",
                            Department = request.HospitalDepartment ?? "Department of Oral Surgery & Dentistry"
                        });
                }
                catch { /* Ignore duplicate if any */ }
            }

            return newDoctorId;
        }

        public async Task<int> ToggleDoctorStatusAsync(int id, bool isActive)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            return await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET IsActive = @IsActive WHERE DoctorID = @Id", new { IsActive = isActive, Id = id });
        }

        // ==============================================================================
        // ORGANIZATIONS & HOSPITAL REPOSITORY METHODS
        // ==============================================================================
        public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                SELECT o.*, 
                       (SELECT COUNT(*) FROM [dentist].[Doctors] d WHERE d.OrganizationID = o.OrganizationID AND (d.IsActive IS NULL OR d.IsActive = 1)) AS DoctorCount
                FROM [dentist].[Organizations] o
                ORDER BY o.IsActive DESC, o.Name ASC";
            return await connection.QueryAsync<Organization>(sql);
        }

        public async Task<Organization?> GetOrganizationByIdAsync(int id)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                SELECT o.*, 
                       (SELECT COUNT(*) FROM [dentist].[Doctors] d WHERE d.OrganizationID = o.OrganizationID AND (d.IsActive IS NULL OR d.IsActive = 1)) AS DoctorCount
                FROM [dentist].[Organizations] o
                WHERE o.OrganizationID = @Id";
            var org = await connection.QuerySingleOrDefaultAsync<Organization>(sql, new { Id = id });
            if (org != null)
            {
                var docSql = @"
                    SELECT d.DoctorID, d.Username, d.FirstName, d.LastName, d.Region, d.Specialization, d.Title, d.YearsOfExperience, d.ProfileImageUrl, d.Rating, d.IsActive, d.HospitalDepartment
                    FROM [dentist].[Doctors] d
                    WHERE d.OrganizationID = @OrgId
                    ORDER BY d.FirstName ASC";
                var docs = await connection.QueryAsync<Doctor>(docSql, new { OrgId = id });
                org.Doctors = docs.ToList();
            }
            return org;
        }

        public async Task<int> CreateOrganizationAsync(CreateOrganizationRequest request)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                INSERT INTO [dentist].[Organizations] (
                    Name, Slug, Type, Address, City, Country, Phone, Email, Website, LogoUrl, HeroImageUrl, Description, Accreditation, IsActive, CreatedAt
                ) 
                OUTPUT INSERTED.OrganizationID 
                VALUES (
                    @Name, @Slug, @Type, @Address, @City, @Country, @Phone, @Email, @Website, @LogoUrl, @HeroImageUrl, @Description, @Accreditation, @IsActive, GETDATE()
                )";
            return await connection.QuerySingleAsync<int>(sql, request);
        }

        public async Task<int> UpdateOrganizationAsync(int id, UpdateOrganizationRequest request)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            string sql = @"
                UPDATE [dentist].[Organizations] SET
                    Name = @Name,
                    Slug = @Slug,
                    Type = @Type,
                    Address = @Address,
                    City = @City,
                    Country = @Country,
                    Phone = @Phone,
                    Email = @Email,
                    Website = @Website,
                    LogoUrl = @LogoUrl,
                    HeroImageUrl = @HeroImageUrl,
                    Description = @Description,
                    Accreditation = @Accreditation,
                    IsActive = @IsActive
                WHERE OrganizationID = @Id";
            return await connection.ExecuteAsync(sql, new
            {
                Id = id,
                request.Name,
                request.Slug,
                request.Type,
                request.Address,
                request.City,
                request.Country,
                request.Phone,
                request.Email,
                request.Website,
                request.LogoUrl,
                request.HeroImageUrl,
                request.Description,
                request.Accreditation,
                request.IsActive
            });
        }

        public async Task<int> ToggleOrganizationStatusAsync(int id, bool isActive)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            return await connection.ExecuteAsync("UPDATE [dentist].[Organizations] SET IsActive = @IsActive WHERE OrganizationID = @Id", new { IsActive = isActive, Id = id });
        }

        public async Task<int> AssignDoctorToOrganizationAsync(int orgId, int doctorId, string role, string dept, string days, bool isPrimary)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            if (isPrimary)
            {
                await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET OrganizationID = @OrgId, HospitalDepartment = @Dept WHERE DoctorID = @DoctorID", new { OrgId = orgId, Dept = dept, DoctorID = doctorId });
            }
            string junctionSql = @"
                IF EXISTS (SELECT 1 FROM [dentist].[DoctorOrganizations] WHERE DoctorID = @DoctorID AND OrganizationID = @OrgId)
                    UPDATE [dentist].[DoctorOrganizations] SET RoleInOrg = @Role, Department = @Dept, ConsultationDays = @Days, IsPrimary = @IsPrimary WHERE DoctorID = @DoctorID AND OrganizationID = @OrgId
                ELSE
                    INSERT INTO [dentist].[DoctorOrganizations] (DoctorID, OrganizationID, RoleInOrg, Department, ConsultationDays, IsPrimary, CreatedAt)
                    VALUES (@DoctorID, @OrgId, @Role, @Dept, @Days, @IsPrimary, GETDATE())";
            return await connection.ExecuteAsync(junctionSql, new { DoctorID = doctorId, OrgId = orgId, Role = role, Dept = dept, Days = days, IsPrimary = isPrimary });
        }

        public async Task<int> RemoveDoctorFromOrganizationAsync(int orgId, int doctorId)
        {
            using var connection = CreateConnection();
            await EnsureDoctorColumnsExistAsync(connection);
            await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET OrganizationID = NULL WHERE DoctorID = @DoctorID AND OrganizationID = @OrgId", new { DoctorID = doctorId, OrgId = orgId });
            return await connection.ExecuteAsync("DELETE FROM [dentist].[DoctorOrganizations] WHERE DoctorID = @DoctorID AND OrganizationID = @OrgId", new { DoctorID = doctorId, OrgId = orgId });
        }

        public async Task<int> UpdateDoctorPasswordAsync(int id, string passwordHash)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET PasswordHash = @PasswordHash WHERE DoctorID = @Id", new { PasswordHash = passwordHash, Id = id });
        }

        public async Task<IEnumerable<Patient>> GetPatientsByDoctorAsync(int doctorId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT DISTINCT p.* 
                FROM [dentist].[Patients] p
                LEFT JOIN [dentist].[Appointments] a ON a.PatientID = p.PatientID AND a.DoctorID = @DoctorID
                LEFT JOIN [dentist].[Invoices] i ON i.PatientID = p.PatientID AND i.DoctorID = @DoctorID
                WHERE p.DoctorID = @DoctorID 
                   OR a.AppointmentID IS NOT NULL 
                   OR i.InvoiceID IS NOT NULL
                ORDER BY p.PatientID DESC";
            return await connection.QueryAsync<Patient>(sql, new { DoctorID = doctorId });
        }

        public async Task<Patient> SearchPatientByNameAsync(string name)
        {
            using var connection = CreateConnection();
            string query = "SELECT * FROM [dentist].[Patients] WHERE FirstName LIKE @Name OR LastName LIKE @Name";
            return await connection.QueryFirstOrDefaultAsync<Patient>(query, new { Name = $"%{name}%" });
        }

        public virtual async Task<Patient> GetPatientByIdAsync(int id)
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Patient>("SELECT * FROM [dentist].[Patients] WHERE PatientID = @Id", new { Id = id });
        }

        public async Task<Patient> FindDuplicatePatientAsync(int doctorId, string firstName, string lastName, DateTime? dob, string phone)
        {
            using var connection = CreateConnection();
            string cleanFirst = (firstName ?? "").Trim().ToLower();
            string cleanLast = (lastName ?? "").Trim().ToLower();
            string cleanPhone = Regex.Replace(phone ?? "", @"[^\d]", "");

            string query = @"
                SELECT TOP 1 * FROM [dentist].[Patients] 
                WHERE DoctorID = @DoctorID 
                  AND LOWER(LTRIM(RTRIM(FirstName))) = @CleanFirst 
                  AND LOWER(LTRIM(RTRIM(LastName))) = @CleanLast
                  AND (
                      (@Dob IS NOT NULL AND CAST(DOB AS DATE) = CAST(@Dob AS DATE))
                      OR
                      (@CleanPhone <> '' AND LEN(@CleanPhone) >= 6 AND REPLACE(REPLACE(REPLACE(REPLACE(Phone, ' ', ''), '-', ''), '+', ''), '(', '') = @CleanPhone)
                  )";
                  
            return await connection.QueryFirstOrDefaultAsync<Patient>(query, new { 
                DoctorID = doctorId, 
                CleanFirst = cleanFirst, 
                CleanLast = cleanLast, 
                Dob = dob, 
                CleanPhone = cleanPhone 
            });
        }

        public async Task<int> CreatePatientAsync(Patient patient)
        {
            using var connection = CreateConnection();
            if (string.IsNullOrEmpty(patient.ReferenceNumber))
            {
                var year = DateTime.UtcNow.Year;
                var maxId = await connection.ExecuteScalarAsync<int>("SELECT ISNULL(MAX(PatientID), 0) + 1 FROM [dentist].[Patients]");
                patient.ReferenceNumber = $"DEN-{year}-{maxId:D5}";
            }

            string query = @"
                INSERT INTO [dentist].[Patients] (DoctorID, FirstName, LastName, DOB, Phone, NHINumber, Address, Email, Gender, Region, DentitionType, CurrentTreatmentPlan, TreatmentStage, TargetShade, ProfileImage, ProfileImageMimeType, ReferenceNumber, PasswordHash, IsPortalActive, CreatedAt)
                OUTPUT INSERTED.PatientID
                VALUES (@DoctorID, @FirstName, @LastName, @DOB, @Phone, @NHINumber, @Address, @Email, @Gender, @Region, COALESCE(@DentitionType, 'Adult'), @CurrentTreatmentPlan, @TreatmentStage, @TargetShade, @ProfileImage, @ProfileImageMimeType, @ReferenceNumber, @PasswordHash, COALESCE(@IsPortalActive, 1), GETDATE())";
            return await connection.QuerySingleAsync<int>(query, patient);
        }

        public async Task<int> UpdatePatientAsync(Patient patient)
        {
            using var connection = CreateConnection();
            string query = @"
                UPDATE [dentist].[Patients]
                SET FirstName = @FirstName,
                    LastName = @LastName,
                    DOB = @DOB,
                    Phone = @Phone,
                    NHINumber = @NHINumber,
                    Address = @Address,
                    Email = @Email,
                    Gender = @Gender,
                    Region = @Region,
                    DentitionType = COALESCE(@DentitionType, DentitionType),
                    CurrentTreatmentPlan = COALESCE(@CurrentTreatmentPlan, CurrentTreatmentPlan),
                    ProfileImage = COALESCE(@ProfileImage, ProfileImage),
                    ProfileImageMimeType = COALESCE(@ProfileImageMimeType, ProfileImageMimeType)
                WHERE PatientID = @PatientID";
            return await connection.ExecuteAsync(query, patient);
        }

        public async Task<int> UpdatePatientProfileImageAsync(int patientId, byte[] profileImage, string mimeType)
        {
            using var connection = CreateConnection();
            string query = @"
                UPDATE [dentist].[Patients]
                SET ProfileImage = @ProfileImage,
                    ProfileImageMimeType = @ProfileImageMimeType
                WHERE PatientID = @PatientID";
            return await connection.ExecuteAsync(query, new { PatientID = patientId, ProfileImage = profileImage, ProfileImageMimeType = mimeType });
        }

        public async Task<int> UpdatePatientTreatmentPlanAsync(int patientId, string treatmentPlan, string treatmentStage, string targetShade)
        {
            using var connection = CreateConnection();
            string query = @"
                UPDATE [dentist].[Patients] 
                SET CurrentTreatmentPlan = @TreatmentPlan,
                    TreatmentStage = @TreatmentStage,
                    TargetShade = @TargetShade
                WHERE PatientID = @PatientID";
            return await connection.ExecuteAsync(query, new { 
                PatientID = patientId, 
                TreatmentPlan = treatmentPlan, 
                TreatmentStage = treatmentStage, 
                TargetShade = targetShade 
            });
        }

        public async Task<IEnumerable<TeethState>> GetPatientChartAsync(int patientId)
        {
            using var connection = CreateConnection();
            string query = @"
                SELECT ts.TeethStateID, ts.PatientID, ts.DoctorID, ts.ToothNumber, ts.ToothKey, ts.DentitionCategory, ts.ConditionColor, ts.ConditionStatus, ts.LastUpdated,
                       COALESCE(
                           NULLIF(ts.Comments, ''), 
                           (SELECT TOP 1 th.Comments FROM [dentist].[TreatmentHistory] th 
                            WHERE th.PatientID = ts.PatientID AND ((th.ToothKey IS NOT NULL AND th.ToothKey = ts.ToothKey) OR th.ToothNumber = ts.ToothNumber)
                            ORDER BY th.ActionDate DESC, th.HistoryID DESC), 
                           ''
                       ) AS Comments
                FROM [dentist].[TeethState] ts 
                WHERE ts.PatientID = @PatientID
                ORDER BY ts.DentitionCategory, ts.ToothNumber";
            return await connection.QueryAsync<TeethState>(query, new { PatientID = patientId });
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsAsync(int patientId)
        {
            using var connection = CreateConnection();
            string query = "SELECT PrescriptionID, PatientID, MedicineName, PrescribedDate FROM [dentist].[Prescriptions] WHERE PatientID = @PatientID ORDER BY PrescribedDate DESC";
            return await connection.QueryAsync<Prescription>(query, new { PatientID = patientId });
        }

        public async Task UpdateTeethStateBulkAsync(int patientId, int toothNumber, string color, string status, string treatment, string comments, string dentitionCategory = "Adult", string? toothKey = null, int? doctorId = null)
        {
            using var connection = CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Fallback toothKey from toothNumber if not specified
                string effectiveToothKey = toothKey ?? (dentitionCategory == "Pediatric" && toothNumber >= 1 && toothNumber <= 20 
                    ? ((char)(64 + toothNumber)).ToString() 
                    : toothNumber.ToString());

                // Update or Insert TeethState partitioned by DentitionCategory & ToothKey
                string updateQuery = @"
                    IF EXISTS (SELECT 1 FROM [dentist].[TeethState] 
                               WHERE PatientID = @PatientID 
                                 AND ((@ToothKey IS NOT NULL AND ToothKey = @ToothKey) OR ToothNumber = @ToothNumber)
                                 AND DentitionCategory = @DentitionCategory)
                    BEGIN
                        UPDATE [dentist].[TeethState] 
                        SET ConditionColor = @Color, 
                            ConditionStatus = @Status, 
                            Comments = @Comments,
                            ToothKey = @ToothKey,
                            DoctorID = COALESCE(@DoctorID, DoctorID),
                            LastUpdated = GETDATE()
                        WHERE PatientID = @PatientID 
                          AND ((@ToothKey IS NOT NULL AND ToothKey = @ToothKey) OR ToothNumber = @ToothNumber)
                          AND DentitionCategory = @DentitionCategory
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [dentist].[TeethState] (PatientID, DoctorID, ToothNumber, ToothKey, DentitionCategory, ConditionColor, ConditionStatus, Comments)
                        VALUES (@PatientID, @DoctorID, @ToothNumber, @ToothKey, @DentitionCategory, @Color, @Status, @Comments)
                    END";

                await connection.ExecuteAsync(updateQuery, new { 
                    PatientID = patientId, 
                    DoctorID = doctorId, 
                    ToothNumber = toothNumber, 
                    ToothKey = effectiveToothKey, 
                    DentitionCategory = dentitionCategory, 
                    Color = color, 
                    Status = status, 
                    Comments = comments 
                }, transaction);

                // Insert Treatment History
                string historyQuery = @"
                    INSERT INTO [dentist].[TreatmentHistory] (PatientID, DoctorID, ToothNumber, ToothKey, DentitionCategory, TreatmentPerformed, Comments)
                    VALUES (@PatientID, @DoctorID, @ToothNumber, @ToothKey, @DentitionCategory, @Treatment, @Comments)";
                
                await connection.ExecuteAsync(historyQuery, new { 
                    PatientID = patientId, 
                    DoctorID = doctorId, 
                    ToothNumber = toothNumber, 
                    ToothKey = effectiveToothKey, 
                    DentitionCategory = dentitionCategory, 
                    Treatment = treatment, 
                    Comments = comments 
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<TreatmentHistory>> GetToothHistoryAsync(int patientId, int toothNumber)
        {
            using var connection = CreateConnection();
            string query = "SELECT * FROM [dentist].[TreatmentHistory] WHERE PatientID = @PatientID AND ToothNumber = @ToothNumber ORDER BY ActionDate DESC";
            return await connection.QueryAsync<TreatmentHistory>(query, new { PatientID = patientId, ToothNumber = toothNumber });
        }

        public async Task<int> AddPrescriptionAsync(int patientId, string medicineName)
        {
            using var connection = CreateConnection();
            if (patientId <= 0) return 0;

            // Prevent duplicates on the same day for the exact same medicine
            string checkQuery = "SELECT COUNT(1) FROM [dentist].[Prescriptions] WHERE PatientID = @PatientID AND MedicineName = @MedicineName AND CAST(PrescribedDate AS DATE) = CAST(GETDATE() AS DATE)";
            int exists = await connection.ExecuteScalarAsync<int>(checkQuery, new { PatientID = patientId, MedicineName = medicineName });
            if (exists > 0) return 0; // Silently ignore duplicate

            string query = "INSERT INTO [dentist].[Prescriptions] (PatientID, MedicineName) OUTPUT INSERTED.PrescriptionID VALUES (@PatientID, @MedicineName)";
            return await connection.QuerySingleAsync<int>(query, new { PatientID = patientId, MedicineName = medicineName });
        }

        public async Task<IEnumerable<ChatHistoryRecord>> GetChatHistoryAsync(int patientId)
        {
            using var connection = CreateConnection();
            if (patientId <= 0) return new List<ChatHistoryRecord>();

            string query = "SELECT ChatID, PatientID, Transcript, ParsedAction, Timestamp FROM [dentist].[ChatHistory] WHERE PatientID = @PatientID ORDER BY Timestamp DESC";
            return await connection.QueryAsync<ChatHistoryRecord>(query, new { PatientID = patientId });
        }

        public async Task AddChatHistoryAsync(int patientId, string transcript, string parsedAction)
        {
            using var connection = CreateConnection();
            if (patientId <= 0) return;

            string query = "INSERT INTO [dentist].[ChatHistory] (PatientID, Transcript, ParsedAction) VALUES (@PatientID, @Transcript, @ParsedAction)";
            await connection.ExecuteAsync(query, new { PatientID = patientId, Transcript = transcript, ParsedAction = parsedAction });
        }

        public async Task<int> AddClinicalLogAsync(int patientId, int doctorId, string message, string type)
        {
            using var connection = CreateConnection();
            if (patientId <= 0) return 0;

            string query = @"
                INSERT INTO [dentist].[ClinicalLogs] (PatientID, DoctorID, Message, LogType) 
                OUTPUT INSERTED.LogID 
                VALUES (@PatientID, @DoctorID, @Message, @LogType)";
            
            return await connection.QuerySingleAsync<int>(query, new { 
                PatientID = patientId, 
                DoctorID = doctorId > 0 ? doctorId : 1, 
                Message = message, 
                LogType = type 
            });
        }

        public async Task<IEnumerable<ClinicalLog>> GetClinicalLogsAsync(int patientId)
        {
            using var connection = CreateConnection();
            if (patientId <= 0) return new List<ClinicalLog>();

            string query = "SELECT TOP 20 * FROM [dentist].[ClinicalLogs] WHERE PatientID = @PatientID ORDER BY CreatedAt DESC";
            return await connection.QueryAsync<ClinicalLog>(query, new { PatientID = patientId });
        }

        public async Task<int> AddRadiographAsync(Radiograph radiograph)
        {
            using var connection = CreateConnection();
            string query = @"
                INSERT INTO [dentist].[Radiographs] (PatientID, DoctorID, ImageName, MimeType, ImageData, UploadedAt, AnalysisSummary)
                OUTPUT INSERTED.RadiographID
                VALUES (@PatientID, @DoctorID, @ImageName, @MimeType, @ImageData, GETDATE(), @AnalysisSummary)";
            return await connection.QuerySingleAsync<int>(query, radiograph);
        }

        public async Task<IEnumerable<Radiograph>> GetRadiographsByPatientAsync(int patientId)
        {
            using var connection = CreateConnection();
            // EXCLUDE ImageData bytes to keep the list response small
            string query = "SELECT RadiographID, PatientID, DoctorID, ImageName, MimeType, UploadedAt, AnalysisSummary FROM [dentist].[Radiographs] WHERE PatientID = @PatientID ORDER BY UploadedAt DESC";
            return await connection.QueryAsync<Radiograph>(query, new { PatientID = patientId });
        }

        public async Task<Radiograph> GetRadiographByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string query = "SELECT * FROM [dentist].[Radiographs] WHERE RadiographID = @Id";
            return await connection.QuerySingleOrDefaultAsync<Radiograph>(query, new { Id = id });
        }

        public async Task UpdateRadiographAnalysisAsync(int id, string analysisSummary)
        {
            using var connection = CreateConnection();
            string query = "UPDATE [dentist].[Radiographs] SET AnalysisSummary = @AnalysisSummary WHERE RadiographID = @Id";
            await connection.ExecuteAsync(query, new { Id = id, AnalysisSummary = analysisSummary });
        }

        public async Task<bool> DeleteRadiographAsync(int id)
        {
            using var connection = CreateConnection();
            string query = "DELETE FROM [dentist].[Radiographs] WHERE RadiographID = @Id";
            int affected = await connection.ExecuteAsync(query, new { Id = id });
            return affected > 0;
        }

        // ==========================================
        // SMART CLINICAL CHATBOT QUERY METHODS
        // ==========================================

        public class PatientDossier
        {
            public Patient? Patient { get; set; }
            public IEnumerable<TeethState> Teeth { get; set; } = new List<TeethState>();
            public IEnumerable<Appointment> Appointments { get; set; } = new List<Appointment>();
            public IEnumerable<Prescription> Prescriptions { get; set; } = new List<Prescription>();
            public IEnumerable<ClinicalLog> ClinicalLogs { get; set; } = new List<ClinicalLog>();
        }

        public async Task<PatientDossier?> GetFullPatientDossierAsync(string query, int? doctorId = null)
        {
            using var connection = CreateConnection();
            string clean = (query ?? "").Trim();
            if (string.IsNullOrEmpty(clean)) return null;

            // Search by ID or Name
            Patient? patient = null;

            // 1. Check if direct integer or explicit pattern like "patient 14", "id #7", "patient #14" is present
            if (int.TryParse(clean, out int pid))
            {
                string patientSql = "SELECT TOP 1 * FROM [dentist].[Patients] WHERE PatientID = @Pid";
                patient = await connection.QueryFirstOrDefaultAsync<Patient>(patientSql, new { Pid = pid });
            }
            else
            {
                // Explicitly require 'patient', 'id', 'record for patient', or 'dossier for patient' before numeric ID (never match 'tooth 2')
                var idMatch = System.Text.RegularExpressions.Regex.Match(clean, @"\b(?:patient\s*(?:id)?|dossier for patient|record for patient)\s*#?\s*(\d+)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int extractedId) && extractedId > 0)
                {
                    string patientSql = "SELECT TOP 1 * FROM [dentist].[Patients] WHERE PatientID = @Pid";
                    patient = await connection.QueryFirstOrDefaultAsync<Patient>(patientSql, new { Pid = extractedId });
                }
            }

            if (patient == null)
            {
                // Fetch all registered patients to perform precise whole-word and name-sequence matching
                var allPatients = (await connection.QueryAsync<Patient>("SELECT * FROM [dentist].[Patients]")).ToList();

                string normalizedQuery = " " + clean.ToLower() + " ";

                // 1. Highest Priority: Full Name match (FirstName + " " + LastName)
                patient = allPatients.FirstOrDefault(p => 
                    !string.IsNullOrWhiteSpace(p.FirstName) && 
                    !string.IsNullOrWhiteSpace(p.LastName) && 
                    normalizedQuery.Contains($" {p.FirstName.Trim().ToLower()} {p.LastName.Trim().ToLower()} ")
                );

                // 2. Second Priority: Both FirstName and LastName present anywhere in query
                if (patient == null)
                {
                    patient = allPatients.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.FirstName) && 
                        !string.IsNullOrWhiteSpace(p.LastName) && 
                        System.Text.RegularExpressions.Regex.IsMatch(normalizedQuery, $@"\b{System.Text.RegularExpressions.Regex.Escape(p.FirstName.Trim().ToLower())}\b") &&
                        System.Text.RegularExpressions.Regex.IsMatch(normalizedQuery, $@"\b{System.Text.RegularExpressions.Regex.Escape(p.LastName.Trim().ToLower())}\b")
                    );
                }

                // 3. Third Priority: Distinct First Name or Distinct Last Name (min 3 chars, excluding common stop words)
                if (patient == null)
                {
                    var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                        "has", "can", "show", "check", "tell", "give", "what", "when", "does",
                        "teeth", "tooth", "stage", "plan", "shade", "color", "fillings", "restorations",
                        "missing", "appointment", "visit", "record", "history", "profile", "dossier",
                        "any", "the", "for", "is", "are", "all", "with", "received", "selected", "patient"
                    };

                    patient = allPatients.FirstOrDefault(p => 
                        (!string.IsNullOrWhiteSpace(p.FirstName) && p.FirstName.Trim().Length >= 3 && !stopWords.Contains(p.FirstName.Trim()) && System.Text.RegularExpressions.Regex.IsMatch(normalizedQuery, $@"\b{System.Text.RegularExpressions.Regex.Escape(p.FirstName.Trim().ToLower())}\b")) ||
                        (!string.IsNullOrWhiteSpace(p.LastName) && p.LastName.Trim().Length >= 3 && !stopWords.Contains(p.LastName.Trim()) && System.Text.RegularExpressions.Regex.IsMatch(normalizedQuery, $@"\b{System.Text.RegularExpressions.Regex.Escape(p.LastName.Trim().ToLower())}\b"))
                    );
                }
            }

            if (patient == null) return null;

            // Fetch Teeth
            var teeth = await connection.QueryAsync<TeethState>("SELECT * FROM [dentist].[TeethState] WHERE PatientID = @Pid", new { Pid = patient.PatientID });
            
            // Fetch Appointments
            var appts = await connection.QueryAsync<Appointment>(
                "SELECT * FROM [dentist].[Appointments] WHERE FullName LIKE @FullName OR FullName LIKE @First ORDER BY PreferredDate DESC", 
                new { FullName = $"%{patient.FirstName}%{patient.LastName}%", First = $"%{patient.FirstName}%" }
            );

            // Fetch Prescriptions
            var rx = await connection.QueryAsync<Prescription>("SELECT * FROM [dentist].[Prescriptions] WHERE PatientID = @Pid ORDER BY PrescribedDate DESC", new { Pid = patient.PatientID });

            // Fetch Clinical Logs
            var logs = await connection.QueryAsync<ClinicalLog>("SELECT TOP 10 * FROM [dentist].[ClinicalLogs] WHERE PatientID = @Pid ORDER BY CreatedAt DESC", new { Pid = patient.PatientID });

            return new PatientDossier
            {
                Patient = patient,
                Teeth = teeth,
                Appointments = appts,
                Prescriptions = rx,
                ClinicalLogs = logs
            };
        }

        public class ClinicStats
        {
            public int TotalPatients { get; set; }
            public int RootCanalPatients { get; set; }
            public int DamagedTeethPatients { get; set; }
            public int BracesPatients { get; set; }
            public int WhiteningPatients { get; set; }
            public int MissingTeethPatients { get; set; }
            public int TodayAppointmentsCount { get; set; }
        }

        public async Task<ClinicStats> GetClinicStatsSummaryAsync(int? doctorId = null)
        {
            using var connection = CreateConnection();
            var stats = new ClinicStats();

            stats.TotalPatients = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [dentist].[Patients]");

            stats.RootCanalPatients = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT p.PatientID) 
                FROM [dentist].[Patients] p 
                LEFT JOIN [dentist].[TeethState] t ON p.PatientID = t.PatientID 
                WHERE t.ConditionStatus LIKE '%root canal%' 
                   OR t.ConditionStatus LIKE '%rct%' 
                   OR p.CurrentTreatmentPlan LIKE '%root canal%'");

            stats.DamagedTeethPatients = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT p.PatientID) 
                FROM [dentist].[Patients] p 
                LEFT JOIN [dentist].[TeethState] t ON p.PatientID = t.PatientID 
                WHERE t.ConditionStatus LIKE '%decay%' 
                   OR t.ConditionStatus LIKE '%caries%' 
                   OR t.ConditionStatus LIKE '%damage%' 
                   OR t.ConditionColor = '#EF4444' 
                   OR p.CurrentTreatmentPlan LIKE '%cavity%'");

            stats.BracesPatients = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM [dentist].[Patients] 
                WHERE CurrentTreatmentPlan LIKE '%brace%' OR CurrentTreatmentPlan LIKE '%ortho%'");

            stats.WhiteningPatients = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM [dentist].[Patients] 
                WHERE CurrentTreatmentPlan LIKE '%whiten%'");

            stats.MissingTeethPatients = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT PatientID) FROM [dentist].[TeethState] 
                WHERE ConditionStatus LIKE '%miss%' OR ConditionStatus LIKE '%extract%'");

            stats.TodayAppointmentsCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM [dentist].[Appointments] 
                WHERE CAST(PreferredDate AS DATE) = CAST(GETDATE() AS DATE)");

            return stats;
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsByDateQueryAsync(DateTime? specificDate, bool todayOnly = false, string? monthDayText = null)
        {
            using var connection = CreateConnection();

            if (todayOnly)
            {
                string todayQuery = "SELECT * FROM [dentist].[Appointments] WHERE CAST(PreferredDate AS DATE) = CAST(GETDATE() AS DATE) ORDER BY PreferredDate ASC";
                return await connection.QueryAsync<Appointment>(todayQuery);
            }

            if (specificDate.HasValue)
            {
                string dateQuery = "SELECT * FROM [dentist].[Appointments] WHERE CAST(PreferredDate AS DATE) = CAST(@TargetDate AS DATE) ORDER BY PreferredDate ASC";
                return await connection.QueryAsync<Appointment>(dateQuery, new { TargetDate = specificDate.Value });
            }

            if (!string.IsNullOrEmpty(monthDayText))
            {
                string textQuery = @"
                    SELECT * FROM [dentist].[Appointments] 
                    WHERE FORMAT(PreferredDate, 'dd MMM') LIKE @SearchText 
                       OR FORMAT(PreferredDate, 'd MMMM') LIKE @SearchText 
                       OR FORMAT(PreferredDate, 'yyyy-MM-dd') LIKE @SearchText
                    ORDER BY PreferredDate ASC";
                return await connection.QueryAsync<Appointment>(textQuery, new { SearchText = $"%{monthDayText}%" });
            }

            // Default to all upcoming
            string upcomingQuery = "SELECT * FROM [dentist].[Appointments] WHERE PreferredDate >= CAST(GETDATE() AS DATE) ORDER BY PreferredDate ASC";
            return await connection.QueryAsync<Appointment>(upcomingQuery);
        }

        public async Task<IEnumerable<Patient>> GetFilteredPatientsListAsync(string filterType)
        {
            using var connection = CreateConnection();
            string sql;

            switch (filterType.ToLower())
            {
                case "female":
                    sql = "SELECT * FROM [dentist].[Patients] WHERE Gender = 'Female' ORDER BY FirstName ASC";
                    break;
                case "male":
                    sql = "SELECT * FROM [dentist].[Patients] WHERE Gender = 'Male' ORDER BY FirstName ASC";
                    break;
                case "braces":
                case "orthodontics":
                    sql = "SELECT * FROM [dentist].[Patients] WHERE CurrentTreatmentPlan LIKE '%brace%' OR CurrentTreatmentPlan LIKE '%ortho%' ORDER BY FirstName ASC";
                    break;
                case "whitening":
                    sql = "SELECT * FROM [dentist].[Patients] WHERE CurrentTreatmentPlan LIKE '%whiten%' ORDER BY FirstName ASC";
                    break;
                case "active_plans":
                    sql = "SELECT * FROM [dentist].[Patients] WHERE CurrentTreatmentPlan IS NOT NULL AND LTRIM(RTRIM(CurrentTreatmentPlan)) <> '' ORDER BY FirstName ASC";
                    break;
                default:
                    sql = "SELECT TOP 20 * FROM [dentist].[Patients] ORDER BY PatientID DESC";
                    break;
            }

            return await connection.QueryAsync<Patient>(sql);
        }

        public async Task<int> CreateAppointmentDirectAsync(Appointment appointment)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[Appointments] (FullName, Phone, Email, PreferredDate, Status, Reason, DoctorID, CreatedAt)
                VALUES (@FullName, @Phone, @Email, @PreferredDate, @Status, @Reason, @DoctorID, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, appointment);
        }

        // =========================================================================
        // 🦷 PATIENT PORTAL & BILLING REPOSITORY METHODS
        // =========================================================================

        public async Task<Patient?> GetPatientForAuthAsync(string identifier)
        {
            using var connection = CreateConnection();
            string cleanId = identifier.Trim();
            string cleanPhone = Regex.Replace(cleanId, @"[^\d]", "");

            string sql = @"
                SELECT TOP 1 * FROM [dentist].[Patients]
                WHERE ([ReferenceNumber] = @CleanId
                   OR LOWER([Email]) = LOWER(@CleanId)
                   OR (@CleanPhone <> '' AND LEN(@CleanPhone) >= 7 AND REPLACE(REPLACE(REPLACE(Phone, ' ', ''), '-', ''), '+', '') = @CleanPhone))
                  AND [IsPortalActive] = 1";

            return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { CleanId = cleanId, CleanPhone = cleanPhone });
        }

        public async Task<Patient?> GetPatientByReferenceNumberAsync(string referenceNumber)
        {
            using var connection = CreateConnection();
            string sql = "SELECT TOP 1 * FROM [dentist].[Patients] WHERE [ReferenceNumber] = @RefNo";
            return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { RefNo = referenceNumber.Trim() });
        }

        public async Task<int> RegisterPatientSelfAsync(PatientRegisterRequest request, string passwordHash, string referenceNumber)
        {
            using var connection = CreateConnection();
            DateTime? dobVal = null;
            if (!string.IsNullOrEmpty(request.DOB) && DateTime.TryParse(request.DOB, out var d))
            {
                dobVal = d;
            }

            string email = request.Email;
            if (string.IsNullOrEmpty(email))
            {
                email = $"{request.FirstName.ToLower().Trim()}.{request.LastName.ToLower().Trim()}@dentiaclinic.com";
            }

            string sql = @"
                INSERT INTO [dentist].[Patients] (
                    DoctorID, FirstName, LastName, DOB, Phone, Email, Gender, Region,
                    DentitionType, CurrentTreatmentPlan, ReferenceNumber, PasswordHash, IsPortalActive, CreatedAt
                )
                OUTPUT INSERTED.PatientID
                VALUES (
                    @DoctorID, @FirstName, @LastName, @DOB, @Phone, @Email, @Gender, @Region,
                    'Adult', 'General Consultation', @ReferenceNumber, @PasswordHash, 1, GETDATE()
                )";

            return await connection.QuerySingleAsync<int>(sql, new
            {
                DoctorID = request.DoctorID > 0 ? request.DoctorID : 2,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                DOB = dobVal.HasValue ? (object)dobVal.Value : DBNull.Value,
                Phone = request.Phone?.Trim() ?? "",
                Email = email,
                Gender = request.Gender ?? "Other",
                Region = string.IsNullOrEmpty(request.Region) ? "NZ" : request.Region,
                ReferenceNumber = referenceNumber,
                PasswordHash = passwordHash
            });
        }

        public async Task<bool> ActivatePatientAccountAsync(string referenceNumber, DateTime dob, string passwordHash)
        {
            using var connection = CreateConnection();
            string sql = @"
                UPDATE [dentist].[Patients]
                SET PasswordHash = @PasswordHash,
                    IsPortalActive = 1,
                    MustChangePassword = 0
                WHERE ReferenceNumber = @RefNo 
                  AND CAST(DOB AS DATE) = CAST(@Dob AS DATE)";

            int rows = await connection.ExecuteAsync(sql, new { PasswordHash = passwordHash, RefNo = referenceNumber.Trim(), Dob = dob.Date });
            return rows > 0;
        }

        public async Task UpdatePatientLastLoginAsync(int patientId)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync("UPDATE [dentist].[Patients] SET LastLoginAt = SYSUTCDATETIME() WHERE PatientID = @PatientID", new { PatientID = patientId });
        }

        public async Task LogPatientPortalActivityAsync(int patientId, string action, string? ipAddress, string? userAgent, string? details)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[PatientPortalActivityLogs] (PatientID, Action, IpAddress, UserAgent, Details, Timestamp)
                VALUES (@PatientID, @Action, @IpAddress, @UserAgent, @Details, SYSUTCDATETIME())";
            await connection.ExecuteAsync(sql, new { PatientID = patientId, Action = action, IpAddress = ipAddress, UserAgent = userAgent, Details = details });
        }

        // --- INVOICES & BILLING ---

        public async Task<IEnumerable<Invoice>> GetPatientInvoicesAsync(int patientId)
        {
            using var connection = CreateConnection();

            // Self-heal any existing invoices where doctor is in PK (or DoctorID = 2) but Currency was saved as NZD or null
            try
            {
                await connection.ExecuteAsync(@"
                    UPDATE I
                    SET I.Currency = 'PKR'
                    FROM [dentist].[Invoices] I
                    INNER JOIN [dentist].[Doctors] D ON I.DoctorID = D.DoctorID
                    WHERE (D.Region = 'PK' OR D.DoctorID = 2) AND (I.Currency = 'NZD' OR I.Currency IS NULL OR I.Currency = '')
                ");
            }
            catch {}

            string sql = @"
                SELECT I.*, D.FirstName + ' ' + D.LastName AS DoctorName
                FROM [dentist].[Invoices] I
                LEFT JOIN [dentist].[Doctors] D ON I.DoctorID = D.DoctorID
                WHERE I.PatientID = @PatientID
                ORDER BY I.IssueDate DESC, I.InvoiceID DESC";

            var invoices = (await connection.QueryAsync<Invoice>(sql, new { PatientID = patientId })).ToList();
            if (invoices.Count > 0)
            {
                foreach (var inv in invoices)
                {
                    if (inv.DoctorID == 2 && (string.Equals(inv.Currency, "NZD", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(inv.Currency)))
                    {
                        inv.Currency = "PKR";
                    }
                }
                var invIds = invoices.Select(i => i.InvoiceID).ToList();
                var itemsSql = "SELECT * FROM [dentist].[InvoiceItems] WHERE InvoiceID IN @Ids ORDER BY InvoiceItemID ASC";
                var items = await connection.QueryAsync<InvoiceItem>(itemsSql, new { Ids = invIds });
                var grouped = items.GroupBy(it => it.InvoiceID).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var inv in invoices)
                {
                    if (grouped.TryGetValue(inv.InvoiceID, out var its))
                    {
                        inv.Items = its;
                    }
                }
            }
            return invoices;
        }

        public async Task SaveDiagnosticAssessmentAsync(int patientId, int doctorId, string suiteCategory, string assessmentJson, string? cdtCode, string? diagnosisSummary)
        {
            using var connection = CreateConnection();
            string sql = @"
                MERGE INTO [dentist].[DiagnosticAssessments] AS target
                USING (SELECT @PatientID AS PatientID) AS source
                ON (target.PatientID = source.PatientID AND target.SuiteCategory = @SuiteCategory)
                WHEN MATCHED THEN
                    UPDATE SET DoctorID = @DoctorID, AssessmentJson = @AssessmentJson, CdtCode = @CdtCode, DiagnosisSummary = @DiagnosisSummary, UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (PatientID, DoctorID, SuiteCategory, AssessmentJson, CdtCode, DiagnosisSummary, CreatedAt, UpdatedAt)
                    VALUES (@PatientID, @DoctorID, @SuiteCategory, @AssessmentJson, @CdtCode, @DiagnosisSummary, SYSUTCDATETIME(), SYSUTCDATETIME());";
            try
            {
                await connection.ExecuteAsync(sql, new { PatientID = patientId, DoctorID = doctorId, SuiteCategory = suiteCategory, AssessmentJson = assessmentJson, CdtCode = cdtCode, DiagnosisSummary = diagnosisSummary });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveDiagnosticAssessmentAsync error: {ex.Message}");
            }
        }

        public async Task<DiagnosticAssessmentRecord?> GetDiagnosticAssessmentAsync(int patientId)
        {
            using var connection = CreateConnection();
            string query = "SELECT TOP 1 * FROM [dentist].[DiagnosticAssessments] WHERE PatientID = @PatientID ORDER BY UpdatedAt DESC";
            return await connection.QueryFirstOrDefaultAsync<DiagnosticAssessmentRecord>(query, new { PatientID = patientId });
        }

        public async Task<Invoice?> GetInvoiceDetailsAsync(long invoiceId, int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT I.*, D.FirstName + ' ' + D.LastName AS DoctorName
                FROM [dentist].[Invoices] I
                LEFT JOIN [dentist].[Doctors] D ON I.DoctorID = D.DoctorID
                WHERE I.InvoiceID = @InvoiceID AND I.PatientID = @PatientID";

            var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(sql, new { InvoiceID = invoiceId, PatientID = patientId });
            if (invoice != null)
            {
                if (invoice.DoctorID == 2 && (string.Equals(invoice.Currency, "NZD", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(invoice.Currency)))
                {
                    invoice.Currency = "PKR";
                }
                var items = await connection.QueryAsync<InvoiceItem>("SELECT * FROM [dentist].[InvoiceItems] WHERE InvoiceID = @InvoiceID", new { InvoiceID = invoiceId });
                invoice.Items = items.ToList();
            }
            return invoice;
        }

        public async Task<(string ReceiptNo, string NewStatus)> ProcessPaymentStoredProcAsync(long invoiceId, int patientId, decimal amount, string paymentMethod, string? transactionRef, string? gateway, int? receivedByDoctorId, string? notes)
        {
            using var connection = CreateConnection();
            var p = new DynamicParameters();
            p.Add("@InvoiceID", invoiceId);
            p.Add("@PatientID", patientId);
            p.Add("@Amount", amount);
            p.Add("@PaymentMethod", paymentMethod);
            p.Add("@TransactionReference", transactionRef);
            p.Add("@PaymentGateway", gateway);
            p.Add("@ReceivedByDoctorID", receivedByDoctorId);
            p.Add("@Notes", notes);
            p.Add("@GeneratedReceiptNo", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
            p.Add("@NewInvoiceStatus", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("[dentist].[usp_ProcessInvoicePayment]", p, commandType: CommandType.StoredProcedure);

            string receipt = p.Get<string>("@GeneratedReceiptNo") ?? "";
            string status = p.Get<string>("@NewInvoiceStatus") ?? "Paid";
            return (receipt, status);
        }

        public async Task<dynamic?> ConfirmCashPaymentStoredProcAsync(string voucherCode, int staffDoctorId, decimal? confirmedAmount)
        {
            using var connection = CreateConnection();
            var p = new DynamicParameters();
            p.Add("@CashVoucherCode", voucherCode);
            p.Add("@StaffDoctorID", staffDoctorId);
            p.Add("@ConfirmedAmount", confirmedAmount);

            return await connection.QueryFirstOrDefaultAsync("[dentist].[usp_ConfirmCashPaymentAtFrontDesk]", p, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Payment>> GetPatientPaymentsAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[Payments] WHERE PatientID = @PatientID ORDER BY PaymentDate DESC";
            return await connection.QueryAsync<Payment>(sql, new { PatientID = patientId });
        }

        // --- APPOINTMENTS FOR PATIENT PORTAL ---

        public async Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT a.*, 
                       i.InvoiceNumber, 
                       i.TotalAmount, 
                       i.Currency, 
                       i.Status AS InvoiceStatus,
                       pay.PaymentMethod
                FROM [dentist].[Appointments] a
                LEFT JOIN [dentist].[Invoices] i ON i.AppointmentID = a.AppointmentID
                LEFT JOIN (
                    SELECT InvoiceID, MAX(PaymentMethod) as PaymentMethod 
                    FROM [dentist].[Payments] 
                    GROUP BY InvoiceID
                ) pay ON pay.InvoiceID = i.InvoiceID
                WHERE a.PatientID = @PatientID
                ORDER BY a.PreferredDate DESC";

            var appts = (await connection.QueryAsync<Appointment>(sql, new { PatientID = patientId })).ToList();
            if (!appts.Any()) return appts;

            try
            {
                var apptIds = appts.Select(a => a.AppointmentID).ToList();
                string itemsSql = @"
                    SELECT ii.InvoiceItemID, ii.InvoiceID, ii.ProcedureCode, ii.Description, ii.Quantity, ii.UnitPrice, ii.TotalPrice, i.AppointmentID
                    FROM [dentist].[InvoiceItems] ii
                    INNER JOIN [dentist].[Invoices] i ON i.InvoiceID = ii.InvoiceID
                    WHERE i.AppointmentID IN @ApptIds";

                var items = await connection.QueryAsync<dynamic>(itemsSql, new { ApptIds = apptIds });
                var itemsByAppt = items.GroupBy(it => (int)it.AppointmentID).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var appt in appts)
                {
                    if (itemsByAppt.TryGetValue(appt.AppointmentID, out var apptItems))
                    {
                        appt.Items = apptItems.Select(it => new InvoiceItem
                        {
                            InvoiceItemID = (long)it.InvoiceItemID,
                            InvoiceID = (long)it.InvoiceID,
                            ProcedureCode = (string?)it.ProcedureCode,
                            Description = (string)it.Description,
                            Quantity = (int)it.Quantity,
                            UnitPrice = (decimal)it.UnitPrice,
                            TotalPrice = (decimal)it.TotalPrice
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed loading appointment invoice items: {ex.Message}");
            }

            return appts;
        }

        public async Task<int> BookPatientAppointmentAsync(int patientId, string fullName, string? phone, string? email, DateTime preferredDate, string? reason, int? doctorId)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[Appointments] (PatientID, FullName, Phone, Email, PreferredDate, Status, Reason, DoctorID, CreatedAt)
                OUTPUT INSERTED.AppointmentID
                VALUES (@PatientID, @FullName, @Phone, @Email, @PreferredDate, 'Confirmed', @Reason, @DoctorID, GETDATE())";

            int apptId = await connection.QuerySingleAsync<int>(sql, new
            {
                PatientID = patientId,
                FullName = fullName,
                Phone = phone ?? "",
                Email = email,
                PreferredDate = preferredDate,
                Reason = reason ?? "Patient Portal Self-Booking",
                DoctorID = doctorId
            });

            if (doctorId.HasValue && doctorId.Value > 0)
            {
                await connection.ExecuteAsync("UPDATE [dentist].[Patients] SET DoctorID = @DoctorID WHERE PatientID = @PatientID", new { DoctorID = doctorId.Value, PatientID = patientId });
            }

            return apptId;
        }

        public async Task<(int AppointmentId, long InvoiceId, string InvoiceNumber, string ReceiptOrVoucherNo, string Status)> BookPatientAppointmentWithPaymentAsync(
            int patientId, 
            string fullName, 
            string? phone, 
            string? email, 
            DateTime preferredDate, 
            string? reason, 
            int? doctorId,
            string paymentMethod,
            decimal consultationFee,
            string? cardLast4,
            string? cardHolderName,
            string? currency = null,
            System.Collections.Generic.List<SelectedProcedureDto>? procedures = null,
            string? notes = null)
        {
            using var connection = CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                // Calculate aggregated fee if multiple procedures are provided
                decimal fee = consultationFee;
                if (procedures != null && procedures.Count > 0)
                {
                    decimal procsTotal = procedures.Sum(p => p.Fee * (p.Quantity > 0 ? p.Quantity : 1));
                    if (procsTotal > 0) fee = procsTotal;
                }
                if (fee <= 0) fee = 85.00m;

                // 1. Insert Appointment with dedicated Notes column
                string apptSql = @"
                    INSERT INTO [dentist].[Appointments] (PatientID, FullName, Phone, Email, PreferredDate, Status, Reason, DoctorID, Notes, CreatedAt)
                    OUTPUT INSERTED.AppointmentID
                    VALUES (@PatientID, @FullName, @Phone, @Email, @PreferredDate, 'Confirmed', @Reason, @DoctorID, @Notes, GETDATE())";

                int apptId = await connection.QuerySingleAsync<int>(apptSql, new
                {
                    PatientID = patientId,
                    FullName = fullName,
                    Phone = phone ?? "",
                    Email = email,
                    PreferredDate = preferredDate,
                    Reason = reason ?? "Dental Consultation",
                    DoctorID = doctorId,
                    Notes = string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes.Trim()
                }, tx);

                // Update patient DoctorID to the booked doctor
                if (doctorId.HasValue && doctorId.Value > 0)
                {
                    await connection.ExecuteAsync("UPDATE [dentist].[Patients] SET DoctorID = @DoctorID WHERE PatientID = @PatientID", new { DoctorID = doctorId.Value, PatientID = patientId }, tx);
                }

                // 2. Generate Invoice
                string invNo = $"INV-2026-{apptId:D5}";
                bool isCard = paymentMethod.Equals("Online_Card", StringComparison.OrdinalIgnoreCase) || paymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase);
                string invStatus = isCard ? "Paid" : "Pending Cash Settlement";
                decimal paidAmount = isCard ? fee : 0.00m;
                string invCurrency = !string.IsNullOrWhiteSpace(currency) ? currency.Trim().ToUpper() : null;
                if (string.IsNullOrWhiteSpace(invCurrency) && doctorId.HasValue && doctorId.Value > 0)
                {
                    var docRegion = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Region FROM [dentist].[Doctors] WHERE DoctorID = @DoctorID", 
                        new { DoctorID = doctorId.Value }, 
                        tx
                    );
                    if ((!string.IsNullOrEmpty(docRegion) && docRegion.Equals("PK", StringComparison.OrdinalIgnoreCase)) || doctorId.Value == 2)
                    {
                        invCurrency = "PKR";
                    }
                }
                invCurrency = invCurrency ?? "NZD";

                string invSql = @"
                    INSERT INTO [dentist].[Invoices] 
                    (InvoiceNumber, PatientID, DoctorID, AppointmentID, IssueDate, DueDate, SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAmount, Status, Currency, Notes, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.InvoiceID
                    VALUES 
                    (@InvoiceNumber, @PatientID, @DoctorID, @AppointmentID, CAST(GETDATE() AS DATE), CAST(@DueDate AS DATE), @SubTotal, 0.00, 0.00, @TotalAmount, @PaidAmount, @Status, @Currency, @Notes, SYSUTCDATETIME(), SYSUTCDATETIME())";

                long invoiceId = await connection.QuerySingleAsync<long>(invSql, new
                {
                    InvoiceNumber = invNo,
                    PatientID = patientId,
                    DoctorID = doctorId,
                    AppointmentID = apptId,
                    DueDate = preferredDate,
                    SubTotal = fee,
                    TotalAmount = fee,
                    PaidAmount = paidAmount,
                    Status = invStatus,
                    Currency = invCurrency,
                    Notes = $"Consultation appointment #{apptId} - Payment via {(isCard ? "Online Card" : "Cash at Clinic")}"
                }, tx);

                // 3. Insert Invoice Items (Individual records per procedure)
                if (procedures != null && procedures.Count > 0)
                {
                    string multiItemSql = @"
                        INSERT INTO [dentist].[InvoiceItems] (InvoiceID, ProcedureCode, Description, Quantity, UnitPrice)
                        VALUES (@InvoiceID, @ProcedureCode, @Description, @Quantity, @UnitPrice)";

                    foreach (var p in procedures)
                    {
                        await connection.ExecuteAsync(multiItemSql, new
                        {
                            InvoiceID = invoiceId,
                            ProcedureCode = p.ProcedureCode,
                            Description = p.ProcedureName,
                            Quantity = p.Quantity > 0 ? p.Quantity : 1,
                            UnitPrice = p.Fee
                        }, tx);
                    }
                }
                else
                {
                    string itemSql = @"
                        INSERT INTO [dentist].[InvoiceItems] (InvoiceID, Description, Quantity, UnitPrice)
                        VALUES (@InvoiceID, @Description, 1, @UnitPrice)";

                    await connection.ExecuteAsync(itemSql, new
                    {
                        InvoiceID = invoiceId,
                        Description = reason ?? "Dental Consultation & Examination",
                        UnitPrice = fee
                    }, tx);
                }

                // 4. Insert Payment / Voucher Record
                string receiptNo = $"REC-2026-{apptId:D5}";
                string voucherCode = $"CSH-2026-{apptId:D5}";
                string receiptOrVoucher = isCard ? receiptNo : voucherCode;
                string paymentStatus = isCard ? "Success" : "Pending_Cash_Verification";

                string paySql = @"
                    INSERT INTO [dentist].[Payments]
                    (PaymentReceiptNo, InvoiceID, PatientID, Amount, PaymentMethod, PaymentStatus, TransactionReference, PaymentGateway, CashVoucherCode, ReceivedByDoctorID, PaymentDate, Notes)
                    VALUES
                    (@PaymentReceiptNo, @InvoiceID, @PatientID, @Amount, @PaymentMethod, @PaymentStatus, @TxRef, @Gateway, @CashVoucherCode, @ReceivedByDoctorID, SYSUTCDATETIME(), @Notes)";

                await connection.ExecuteAsync(paySql, new
                {
                    PaymentReceiptNo = receiptOrVoucher,
                    InvoiceID = invoiceId,
                    PatientID = patientId,
                    Amount = fee,
                    PaymentMethod = isCard ? "Online_Card" : "Cash",
                    PaymentStatus = paymentStatus,
                    TxRef = isCard ? $"ch_card_{Guid.NewGuid():N}".Substring(0, 24) : null,
                    Gateway = isCard ? "OnlineGateway" : "ClinicCashDesk",
                    CashVoucherCode = isCard ? null : voucherCode,
                    ReceivedByDoctorID = doctorId,
                    Notes = isCard 
                        ? $"Online Card payment by {cardHolderName ?? fullName} (Card ending in {cardLast4 ?? "4242"})"
                        : "Cash voucher generated during online appointment booking. Settle at front desk."
                }, tx);

                tx.Commit();
                return (apptId, invoiceId, invNo, receiptOrVoucher, invStatus);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<(bool Success, string Message, Appointment? Appointment)> UpdateAppointmentTreatmentPlanAsync(
            int appointmentId, 
            int patientId, 
            System.Collections.Generic.List<SelectedProcedureDto>? procedures, 
            string? notes = null)
        {
            if (procedures == null || procedures.Count == 0)
            {
                procedures = new System.Collections.Generic.List<SelectedProcedureDto>
                {
                    new SelectedProcedureDto
                    {
                        ProcedureCode = "011",
                        ProcedureName = "General Dental Consultation",
                        Fee = 85.00m,
                        Quantity = 1
                    }
                };
            }

            using var connection = CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                // 1. Verify Appointment
                string apptSql = "SELECT * FROM [dentist].[Appointments] WHERE AppointmentID = @AppointmentID AND PatientID = @PatientID";
                var appt = await connection.QueryFirstOrDefaultAsync<Appointment>(apptSql, new { AppointmentID = appointmentId, PatientID = patientId }, tx);
                if (appt == null)
                {
                    return (false, "Appointment not found or unauthorized.", null);
                }

                if (string.Equals(appt.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(appt.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(appt.Status, "Done", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"Cannot modify treatments for an appointment that is already {appt.Status}.", null);
                }

                // 2. Fetch Invoice
                string invSql = "SELECT * FROM [dentist].[Invoices] WHERE AppointmentID = @AppointmentID";
                var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(invSql, new { AppointmentID = appointmentId }, tx);

                decimal newTotal = procedures.Sum(p => p.Fee * (p.Quantity > 0 ? p.Quantity : 1));

                // Extract doctor attribution safely (e.g. "(Dr. Sarah J. Lee)") without capturing notes
                string docAttribution = "";
                if (!string.IsNullOrEmpty(appt.Reason))
                {
                    var docMatch = System.Text.RegularExpressions.Regex.Match(
                        appt.Reason, 
                        @"\((Dr\.?[^)]+)\)", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    if (docMatch.Success)
                    {
                        docAttribution = $" ({docMatch.Groups[1].Value.Trim()})";
                    }
                }

                // Clean incoming notes: strip any accidental duplicate "- Note:" or procedure prefixes if user passed composite
                string cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim();
                if (!string.IsNullOrEmpty(cleanNotes))
                {
                    var noteMatch = System.Text.RegularExpressions.Regex.Match(
                        cleanNotes, 
                        @"-\s*Notes?:\s*(.*)$", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                    if (noteMatch.Success)
                    {
                        cleanNotes = noteMatch.Groups[1].Value.Trim();
                    }
                }

                var procNames = procedures.Select(p => !string.IsNullOrEmpty(p.ProcedureCode) ? $"{p.ProcedureName} [{p.ProcedureCode}]" : p.ProcedureName).ToList();

                string baseReason = procNames.Any() 
                    ? string.Join(", ", procNames) 
                    : "General Dental Consultation";

                string newReason = $"{baseReason}{docAttribution}{(string.IsNullOrWhiteSpace(cleanNotes) ? "" : $" - Note: {cleanNotes}")}";

                // Update Appointment Reason AND dedicated Notes column
                await connection.ExecuteAsync(
                    "UPDATE [dentist].[Appointments] SET Reason = @Reason, Notes = @Notes WHERE AppointmentID = @AppointmentID",
                    new { Reason = newReason, Notes = string.IsNullOrWhiteSpace(cleanNotes) ? (object)DBNull.Value : cleanNotes, AppointmentID = appointmentId },
                    tx
                );

                if (invoice != null)
                {
                    // Delete existing InvoiceItems
                    await connection.ExecuteAsync(
                        "DELETE FROM [dentist].[InvoiceItems] WHERE InvoiceID = @InvoiceID",
                        new { InvoiceID = invoice.InvoiceID },
                        tx
                    );

                    // Insert new InvoiceItems
                    string insertItemSql = @"
                        INSERT INTO [dentist].[InvoiceItems] (InvoiceID, ProcedureCode, Description, Quantity, UnitPrice)
                        VALUES (@InvoiceID, @ProcedureCode, @Description, @Quantity, @UnitPrice)";

                    foreach (var proc in procedures)
                    {
                        await connection.ExecuteAsync(insertItemSql, new
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProcedureCode = proc.ProcedureCode,
                            Description = proc.ProcedureName,
                            Quantity = proc.Quantity > 0 ? proc.Quantity : 1,
                            UnitPrice = proc.Fee
                        }, tx);
                    }

                    // Update Invoice Amounts
                    decimal paidAmount = invoice.Status == "Paid" ? newTotal : invoice.PaidAmount;
                    await connection.ExecuteAsync(@"
                        UPDATE [dentist].[Invoices]
                        SET SubTotal = @SubTotal,
                            TotalAmount = @TotalAmount,
                            PaidAmount = @PaidAmount,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE InvoiceID = @InvoiceID",
                        new
                        {
                            SubTotal = newTotal,
                            TotalAmount = newTotal,
                            PaidAmount = paidAmount,
                            InvoiceID = invoice.InvoiceID
                        }, tx);

                    // Update Payment record amount
                    await connection.ExecuteAsync(@"
                        UPDATE [dentist].[Payments]
                        SET Amount = @Amount,
                            Notes = ISNULL(Notes, '') + ' | Updated on ' + CONVERT(VARCHAR(20), GETDATE(), 120)
                        WHERE InvoiceID = @InvoiceID",
                        new { Amount = newTotal, InvoiceID = invoice.InvoiceID },
                        tx
                    );
                }

                tx.Commit();

                // Build return model
                appt.Reason = newReason;
                appt.Notes = cleanNotes;
                appt.TotalAmount = newTotal;
                appt.Currency = invoice?.Currency ?? (appt.DoctorID == 2 ? "PKR" : "NZD");
                appt.InvoiceNumber = invoice?.InvoiceNumber;
                appt.InvoiceStatus = invoice?.Status;
                appt.Items = procedures.Select(p => new InvoiceItem
                {
                    ProcedureCode = p.ProcedureCode,
                    Description = p.ProcedureName,
                    Quantity = p.Quantity > 0 ? p.Quantity : 1,
                    UnitPrice = p.Fee,
                    TotalPrice = p.Fee * (p.Quantity > 0 ? p.Quantity : 1)
                }).ToList();

                return (true, "Treatment plan updated successfully.", appt);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string Message, Appointment? Appointment)> UpdateAppointmentTreatmentPlanDoctorAsync(
            int appointmentId, 
            int? doctorId, 
            System.Collections.Generic.List<SelectedProcedureDto>? procedures, 
            string? notes = null)
        {
            if (procedures == null || procedures.Count == 0)
            {
                procedures = new System.Collections.Generic.List<SelectedProcedureDto>
                {
                    new SelectedProcedureDto
                    {
                        ProcedureCode = "011",
                        ProcedureName = "General Dental Consultation",
                        Fee = 85.00m,
                        Quantity = 1
                    }
                };
            }

            using var connection = CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                // 1. Verify Appointment
                string apptSql = "SELECT * FROM [dentist].[Appointments] WHERE AppointmentID = @AppointmentID";
                var appt = await connection.QueryFirstOrDefaultAsync<Appointment>(apptSql, new { AppointmentID = appointmentId }, tx);
                if (appt == null)
                {
                    return (false, "Appointment not found.", null);
                }

                // 2. Fetch Invoice
                string invSql = "SELECT * FROM [dentist].[Invoices] WHERE AppointmentID = @AppointmentID";
                var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(invSql, new { AppointmentID = appointmentId }, tx);

                decimal newTotal = procedures.Sum(p => p.Fee * (p.Quantity > 0 ? p.Quantity : 1));

                string docAttribution = "";
                if (!string.IsNullOrEmpty(appt.Reason))
                {
                    var docMatch = System.Text.RegularExpressions.Regex.Match(
                        appt.Reason, 
                        @"\((Dr\.?[^)]+)\)", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    if (docMatch.Success)
                    {
                        docAttribution = $" ({docMatch.Groups[1].Value.Trim()})";
                    }
                }

                string cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim();
                if (!string.IsNullOrEmpty(cleanNotes))
                {
                    var noteMatch = System.Text.RegularExpressions.Regex.Match(
                        cleanNotes, 
                        @"-\s*Notes?:\s*(.*)$", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                    if (noteMatch.Success)
                    {
                        cleanNotes = noteMatch.Groups[1].Value.Trim();
                    }
                }

                var procNames = procedures.Select(p => !string.IsNullOrEmpty(p.ProcedureCode) ? $"{p.ProcedureName} [{p.ProcedureCode}]" : p.ProcedureName).ToList();
                string baseReason = procNames.Any() ? string.Join(", ", procNames) : "General Dental Consultation";
                string newReason = $"{baseReason}{docAttribution}{(string.IsNullOrWhiteSpace(cleanNotes) ? "" : $" - Note: {cleanNotes}")}";

                // Update Appointment Reason and Notes
                await connection.ExecuteAsync(
                    "UPDATE [dentist].[Appointments] SET Reason = @Reason, Notes = @Notes WHERE AppointmentID = @AppointmentID",
                    new { Reason = newReason, Notes = string.IsNullOrWhiteSpace(cleanNotes) ? (object)DBNull.Value : cleanNotes, AppointmentID = appointmentId },
                    tx
                );

                if (invoice != null)
                {
                    await connection.ExecuteAsync(
                        "DELETE FROM [dentist].[InvoiceItems] WHERE InvoiceID = @InvoiceID",
                        new { InvoiceID = invoice.InvoiceID },
                        tx
                    );

                    string insertItemSql = @"
                        INSERT INTO [dentist].[InvoiceItems] (InvoiceID, ProcedureCode, Description, Quantity, UnitPrice)
                        VALUES (@InvoiceID, @ProcedureCode, @Description, @Quantity, @UnitPrice)";

                    foreach (var proc in procedures)
                    {
                        await connection.ExecuteAsync(insertItemSql, new
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProcedureCode = proc.ProcedureCode,
                            Description = proc.ProcedureName,
                            Quantity = proc.Quantity > 0 ? proc.Quantity : 1,
                            UnitPrice = proc.Fee
                        }, tx);
                    }

                    decimal paidAmount = invoice.Status == "Paid" ? newTotal : invoice.PaidAmount;
                    await connection.ExecuteAsync(@"
                        UPDATE [dentist].[Invoices]
                        SET SubTotal = @SubTotal,
                            TotalAmount = @TotalAmount,
                            PaidAmount = @PaidAmount,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE InvoiceID = @InvoiceID",
                        new
                        {
                            SubTotal = newTotal,
                            TotalAmount = newTotal,
                            PaidAmount = paidAmount,
                            InvoiceID = invoice.InvoiceID
                        }, tx);
                }

                tx.Commit();

                appt.Reason = newReason;
                appt.Notes = cleanNotes;
                appt.TotalAmount = newTotal;
                appt.Currency = invoice?.Currency ?? (appt.DoctorID == 2 ? "PKR" : "NZD");
                appt.InvoiceNumber = invoice?.InvoiceNumber;
                appt.InvoiceStatus = invoice?.Status;
                appt.Items = procedures.Select(p => new InvoiceItem
                {
                    ProcedureCode = p.ProcedureCode,
                    Description = p.ProcedureName,
                    Quantity = p.Quantity > 0 ? p.Quantity : 1,
                    UnitPrice = p.Fee,
                    TotalPrice = p.Fee * (p.Quantity > 0 ? p.Quantity : 1)
                }).ToList();

                return (true, "Doctor treatment plan updated successfully.", appt);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, ex.Message, null);
            }
        }

        // --- PER-PATIENT INVOICE & CHART TREATMENT REPORT METHODS ---

        public async Task<PatientTreatmentReportDto?> GetPatientTreatmentReportAsync(int patientId)
        {
            using var connection = CreateConnection();
            var patient = await connection.QueryFirstOrDefaultAsync<Patient>(
                "SELECT * FROM [dentist].[Patients] WHERE PatientID = @PatientID",
                new { PatientID = patientId }
            );
            if (patient == null) return null;

            string currency = (patient.DoctorID == 2 || string.Equals(patient.Region, "PK", StringComparison.OrdinalIgnoreCase)) ? "PKR" : "NZD";

            // Invoices with items and payments
            var invoices = (await GetPatientInvoicesAsync(patientId)).ToList();
            var allPayments = (await GetPatientPaymentsAsync(patientId)).ToList();
            var paymentsByInvoice = allPayments.GroupBy(p => p.InvoiceID).ToDictionary(g => g.Key, g => g.ToList());

            var invoiceDtos = invoices.Select(inv => new InvoiceReportDto
            {
                InvoiceId = inv.InvoiceID,
                InvoiceNumber = inv.InvoiceNumber,
                IssueDate = inv.IssueDate,
                DueDate = inv.DueDate,
                DoctorName = inv.DoctorName,
                TotalAmount = inv.TotalAmount,
                PaidAmount = inv.PaidAmount,
                BalanceAmount = inv.BalanceAmount,
                Status = inv.Status,
                Currency = !string.IsNullOrEmpty(inv.Currency) ? inv.Currency : currency,
                Notes = inv.Notes,
                Items = inv.Items ?? new System.Collections.Generic.List<InvoiceItem>(),
                Payments = paymentsByInvoice.TryGetValue(inv.InvoiceID, out var pays)
                    ? pays.Select(p => new PaymentReceiptDto
                    {
                        ReceiptNumber = p.PaymentReceiptNo,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        PaymentDate = p.PaymentDate,
                        TransactionReference = p.TransactionReference
                    }).ToList()
                    : new System.Collections.Generic.List<PaymentReceiptDto>()
            }).ToList();

            decimal totalInvoiced = invoiceDtos.Sum(i => i.TotalAmount);
            decimal totalPaid = invoiceDtos.Sum(i => i.PaidAmount);
            decimal balanceDue = invoiceDtos.Sum(i => i.BalanceAmount);

            // Teeth State Treatments from Odontogram
            var teeth = await connection.QueryAsync<TeethState>(
                "SELECT * FROM [dentist].[TeethState] WHERE PatientID = @PatientID ORDER BY ToothNumber ASC",
                new { PatientID = patientId }
            );

            var chartTreatments = new System.Collections.Generic.List<ChartTreatmentDto>();
            foreach (var t in teeth)
            {
                var s = (t.ConditionStatus ?? "").Trim();
                if (string.IsNullOrEmpty(s) || s.Equals("Healthy", StringComparison.OrdinalIgnoreCase) || s.Equals("Sound", StringComparison.OrdinalIgnoreCase))
                    continue;

                string treatmentStatus = "Planned";
                if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                    s.Contains("Treated", StringComparison.OrdinalIgnoreCase) || 
                    s.Contains("Placed", StringComparison.OrdinalIgnoreCase) || 
                    s.Contains("Filling", StringComparison.OrdinalIgnoreCase) || 
                    s.Contains("Crown", StringComparison.OrdinalIgnoreCase) || 
                    s.Contains("RCT", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(t.Comments) && t.Comments.Contains("Status: Completed", StringComparison.OrdinalIgnoreCase)))
                {
                    treatmentStatus = "Completed";
                }
                else if (s.Contains("In Progress", StringComparison.OrdinalIgnoreCase) ||
                         (!string.IsNullOrEmpty(t.Comments) && t.Comments.Contains("Status: In Progress", StringComparison.OrdinalIgnoreCase)))
                {
                    treatmentStatus = "In Progress";
                }

                // If ConditionStatus was saved as generic status (like "Completed"), resolve actual condition from Comments or CDT
                string displayCondition = s;
                if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("Planned", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("In Progress", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("Treated", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(t.Comments))
                    {
                        var firstPart = t.Comments.Split(new[] { '.', '•', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(firstPart) && firstPart.Length > 3 && !firstPart.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
                        {
                            displayCondition = firstPart;
                        }
                    }
                }

                string? cdt = null;
                if (!string.IsNullOrEmpty(t.Comments))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(t.Comments, @"(D\d{4})");
                    if (match.Success) cdt = match.Groups[1].Value;
                }

                string? invNo = null;
                decimal fee = 0;
                bool isEstimated = false;

                var matchedItem = invoiceDtos.SelectMany(i => i.Items.Select(it => new { Invoice = i, Item = it }))
                    .FirstOrDefault(x => 
                        x.Item.Description.Contains($"#{t.ToothNumber}") || 
                        x.Item.Description.Contains($"Tooth {t.ToothNumber}") ||
                        x.Item.Description.Contains($"Tooth #{t.ToothNumber}"));
                if (matchedItem != null)
                {
                    invNo = matchedItem.Invoice.InvoiceNumber;
                    fee = matchedItem.Item.TotalPrice;
                }
                else
                {
                    // Infer CDT code and standard catalog fee for unbilled care
                    var (inferredCdt, estimatedPrice) = InferCdtAndEstimatedFee(displayCondition, t.Comments, currency);
                    if (string.IsNullOrEmpty(cdt)) cdt = inferredCdt;
                    fee = estimatedPrice;
                    isEstimated = true;
                }

                chartTreatments.Add(new ChartTreatmentDto
                {
                    ToothNumber = t.ToothNumber,
                    ToothKey = t.ToothKey,
                    ConditionStatus = displayCondition,
                    ConditionColor = t.ConditionColor,
                    CdtCode = cdt,
                    TreatmentName = displayCondition,
                    Status = treatmentStatus,
                    Fee = fee,
                    IsEstimatedFee = isEstimated,
                    InvoiceNumber = invNo,
                    Date = t.LastUpdated,
                    Comments = t.Comments
                });
            }

            decimal unbilledCompletedTotal = chartTreatments
                .Where(c => c.Status == "Completed" && string.IsNullOrEmpty(c.InvoiceNumber))
                .Sum(c => c.Fee);
            int unbilledCompletedCount = chartTreatments
                .Count(c => c.Status == "Completed" && string.IsNullOrEmpty(c.InvoiceNumber));

            return new PatientTreatmentReportDto
            {
                Patient = new PatientReportDemographicsDto
                {
                    PatientId = patient.PatientID,
                    ReferenceNumber = patient.ReferenceNumber,
                    FullName = $"{patient.FirstName} {patient.LastName}".Trim(),
                    Phone = patient.Phone,
                    Email = patient.Email,
                    CurrentTreatmentPlan = patient.CurrentTreatmentPlan ?? "General Consultation",
                    Currency = currency,
                    Dob = patient.DOB,
                    Gender = patient.Gender
                },
                Summary = new FinancialSummaryDto
                {
                    TotalInvoiced = invoiceDtos.Sum(i => i.TotalAmount),
                    TotalPaid = invoiceDtos.Sum(i => i.PaidAmount),
                    BalanceDue = invoiceDtos.Sum(i => i.BalanceAmount),
                    InvoiceCount = invoiceDtos.Count,
                    UnbilledCompletedTotal = unbilledCompletedTotal,
                    UnbilledCompletedCount = unbilledCompletedCount,
                    Currency = currency
                },
                Invoices = invoiceDtos,
                ChartTreatments = chartTreatments
            };
        }

        public static (string cdt, decimal estimatedFee) InferCdtAndEstimatedFee(string condition, string? comments, string currency)
        {
            string text = $"{condition} {comments}".ToLowerInvariant();
            bool isPkr = string.Equals(currency, "PKR", StringComparison.OrdinalIgnoreCase);

            var match = System.Text.RegularExpressions.Regex.Match(text, @"\b(d\d{4})\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string code = match.Groups[1].Value.ToUpperInvariant();
                switch (code)
                {
                    // Implantology
                    case "D6010": return (code, isPkr ? 120000 : 1450);
                    case "D6058": return (code, isPkr ? 95000 : 1250);
                    // Endodontics
                    case "D3330": return (code, isPkr ? 50000 : 680);
                    case "D3320": return (code, isPkr ? 40000 : 550);
                    case "D3310": return (code, isPkr ? 35000 : 480);
                    case "D3220": return (code, isPkr ? 20000 : 250);
                    // Pediatric & Orthodontic
                    case "D2930": return (code, isPkr ? 18000 : 220);
                    case "D1510": return (code, isPkr ? 22000 : 260);
                    case "D8080": return (code, isPkr ? 35000 : 400);
                    case "D8660": return (code, isPkr ? 15000 : 180);
                    case "D8210": return (code, isPkr ? 25000 : 300);
                    // Oral Surgery & Impactions
                    case "D7240": return (code, isPkr ? 45000 : 550);
                    case "D7230": return (code, isPkr ? 35000 : 420);
                    case "D7220": return (code, isPkr ? 25000 : 300);
                    case "D7210": return (code, isPkr ? 18000 : 220);
                    case "D7140": return (code, isPkr ? 10000 : 120);
                    // Restorative
                    case "D2740": return (code, isPkr ? 75000 : 950);
                    case "D2160": return (code, isPkr ? 15000 : 195);
                    case "D2393": return (code, isPkr ? 17000 : 210);
                    case "D2392": return (code, isPkr ? 14000 : 175);
                    case "D2391": return (code, isPkr ? 12000 : 145);
                    case "D2140": return (code, isPkr ? 10000 : 120);
                    // Preventive & Diagnostic
                    case "D1110": return (code, isPkr ? 8000 : 95);
                    case "D0150": return (code, isPkr ? 8000 : 95);
                    case "D0120": return (code, isPkr ? 5000 : 65);
                    case "D0210": return (code, isPkr ? 12000 : 140);
                    case "D0220": return (code, isPkr ? 3000 : 45);
                    case "D0330": return (code, isPkr ? 8000 : 110);
                    default: return (code, isPkr ? 20000 : 250);
                }
            }

            if (text.Contains("implant") || text.Contains("screw-retained") || text.Contains("fixture"))
                return ("D6058", isPkr ? 95000 : 1250);
            if (text.Contains("rct") || text.Contains("root canal") || text.Contains("pulpitis") || text.Contains("endodontic"))
                return ("D3330", isPkr ? 50000 : 680);
            if (text.Contains("pulpotomy") || text.Contains("mta"))
                return ("D3220", isPkr ? 20000 : 250);
            if (text.Contains("ssc") || text.Contains("stainless steel crown") || text.Contains("steel crown"))
                return ("D2930", isPkr ? 18000 : 220);
            if (text.Contains("space maintainer") || text.Contains("band and loop") || text.Contains("band & loop"))
                return ("D1510", isPkr ? 22000 : 260);
            if (text.Contains("impacted") && (text.Contains("horizontal") || text.Contains("bony") || text.Contains("90")))
                return ("D7240", isPkr ? 45000 : 550);
            if (text.Contains("partially erupted") || text.Contains("operculectomy") || text.Contains("tissue impaction"))
                return ("D7220", isPkr ? 25000 : 300);
            if (text.Contains("ortho") || text.Contains("overbite") || text.Contains("malocclusion") || text.Contains("aligner") || text.Contains("braces"))
                return ("D8080", isPkr ? 35000 : 400);
            if (text.Contains("amalgam (mod)") || text.Contains("mod amalgam") || (text.Contains("amalgam") && text.Contains("mod")))
                return ("D2160", isPkr ? 15000 : 195);
            if (text.Contains("composite (o)") || text.Contains("occlusal composite") || text.Contains("filling"))
                return ("D2391", isPkr ? 12000 : 145);
            if (text.Contains("composite (mo)") || text.Contains("composite (do)"))
                return ("D2392", isPkr ? 14000 : 175);
            if (text.Contains("composite") || text.Contains("amalgam"))
                return ("D2140", isPkr ? 11000 : 135);
            if (text.Contains("caries") || text.Contains("decay") || text.Contains("ecc") || text.Contains("cavity"))
                return ("D2140", isPkr ? 9500 : 120);
            if (text.Contains("crown") || text.Contains("zirconia") || text.Contains("porcelain"))
                return ("D2740", isPkr ? 75000 : 950);
            if (text.Contains("surgical extraction") || text.Contains("sectioning"))
                return ("D7210", isPkr ? 18000 : 220);
            if (text.Contains("extraction") || text.Contains("extracted") || text.Contains("exfoliated") || text.Contains("missing"))
                return ("D7140", isPkr ? 10000 : 120);
            if (text.Contains("scaling") || text.Contains("calculus") || text.Contains("cleaning") || text.Contains("prophy"))
                return ("D1110", isPkr ? 8000 : 95);

            return ("D0150", isPkr ? 8000 : 95);
        }

        public async Task<InvoiceReportDto> CreateInvoiceFromTreatmentsAsync(CreateTreatmentInvoiceRequest request)
        {
            using var connection = CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();

            var year = DateTime.UtcNow.Year;
            var maxId = await connection.ExecuteScalarAsync<int>(
                "SELECT ISNULL(MAX(InvoiceID), 0) + 1 FROM [dentist].[Invoices]",
                transaction: tx
            );
            string invNo = $"INV-{year}-{maxId:D5}";

            decimal subTotal = request.Items.Sum(x => x.UnitPrice);
            string invCurrency = !string.IsNullOrEmpty(request.Currency) ? request.Currency : "NZD";
            int docId = request.DoctorId ?? 2;

            string invSql = @"
                INSERT INTO [dentist].[Invoices] 
                (InvoiceNumber, PatientID, DoctorID, IssueDate, DueDate, SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAmount, Status, Currency, Notes, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.InvoiceID
                VALUES 
                (@InvoiceNumber, @PatientID, @DoctorID, CAST(GETDATE() AS DATE), DATEADD(day, 14, CAST(GETDATE() AS DATE)), @SubTotal, 0.00, 0.00, @SubTotal, 0.00, 'Issued', @Currency, @Notes, SYSUTCDATETIME(), SYSUTCDATETIME())";

            long invoiceId = await connection.QuerySingleAsync<long>(invSql, new
            {
                InvoiceNumber = invNo,
                PatientID = request.PatientId,
                DoctorID = docId,
                SubTotal = subTotal,
                Currency = invCurrency,
                Notes = request.Notes ?? $"Itemized clinical invoice issued for completed tooth procedures."
            }, tx);

            string itemSql = @"
                INSERT INTO [dentist].[InvoiceItems] (InvoiceID, ProcedureCode, Description, Quantity, UnitPrice)
                VALUES (@InvoiceID, @ProcedureCode, @Description, 1, @UnitPrice)";

            var insertedItems = new System.Collections.Generic.List<InvoiceItem>();
            foreach (var item in request.Items)
            {
                await connection.ExecuteAsync(itemSql, new
                {
                    InvoiceID = invoiceId,
                    ProcedureCode = item.ProcedureCode ?? "D0150",
                    Description = !string.IsNullOrEmpty(item.Description) ? item.Description : $"Tooth #{item.ToothNumber} treatment",
                    UnitPrice = item.UnitPrice
                }, tx);

                insertedItems.Add(new InvoiceItem
                {
                    InvoiceID = invoiceId,
                    ProcedureCode = item.ProcedureCode ?? "D0150",
                    Description = item.Description ?? $"Tooth #{item.ToothNumber}",
                    Quantity = 1,
                    UnitPrice = item.UnitPrice
                });
            }

            tx.Commit();

            return new InvoiceReportDto
            {
                InvoiceId = invoiceId,
                InvoiceNumber = invNo,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                TotalAmount = subTotal,
                PaidAmount = 0.00m,
                BalanceAmount = subTotal,
                Status = "Issued",
                Currency = invCurrency,
                Notes = request.Notes,
                Items = insertedItems
            };
        }

        public async Task<(bool Success, string Message, string? ReceiptNumber, decimal NewBalance, string NewStatus)> RecordInvoicePaymentAsync(
            long invoiceId, 
            decimal amount, 
            string paymentMethod, 
            string? notes, 
            int? doctorId)
        {
            if (amount <= 0) return (false, "Payment amount must be greater than zero.", null, 0, "");

            using var connection = CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(
                    "SELECT * FROM [dentist].[Invoices] WHERE InvoiceID = @InvoiceID",
                    new { InvoiceID = invoiceId },
                    tx
                );
                if (invoice == null) return (false, "Invoice not found.", null, 0, "");

                decimal newPaid = invoice.PaidAmount + amount;
                if (newPaid > invoice.TotalAmount) newPaid = invoice.TotalAmount;
                decimal newBalance = Math.Max(0, invoice.TotalAmount - newPaid);
                string newStatus = newBalance <= 0 ? "Paid" : "Partially Paid";

                string receiptNo = $"REC-2026-{invoiceId:D4}-{DateTime.UtcNow:mmss}";

                string paySql = @"
                    INSERT INTO [dentist].[Payments] (InvoiceID, PatientID, Amount, PaymentMethod, PaymentReceiptNo, PaymentStatus, ReceivedByDoctorID, Notes, PaymentDate)
                    VALUES (@InvoiceID, @PatientID, @Amount, @PaymentMethod, @PaymentReceiptNo, 'Success', @DoctorID, @Notes, SYSUTCDATETIME())";

                await connection.ExecuteAsync(paySql, new
                {
                    InvoiceID = invoiceId,
                    PatientID = invoice.PatientID,
                    Amount = amount,
                    PaymentMethod = paymentMethod ?? "Cash_Counter",
                    PaymentReceiptNo = receiptNo,
                    DoctorID = doctorId,
                    Notes = notes ?? "Chairside clinical payment settlement"
                }, tx);

                await connection.ExecuteAsync(@"
                    UPDATE [dentist].[Invoices]
                    SET PaidAmount = @PaidAmount,
                        Status = @Status,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE InvoiceID = @InvoiceID",
                    new { PaidAmount = newPaid, Status = newStatus, InvoiceID = invoiceId },
                    tx
                );

                tx.Commit();
                return (true, "Payment recorded successfully.", receiptNo, newBalance, newStatus);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, ex.Message, null, 0, "");
            }
        }

        public async Task<bool> CancelPatientAppointmentAsync(int appointmentId, int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                UPDATE [dentist].[Appointments]
                SET Status = 'Cancelled'
                WHERE AppointmentID = @AppointmentID AND PatientID = @PatientID";
            int rows = await connection.ExecuteAsync(sql, new { AppointmentID = appointmentId, PatientID = patientId });
            return rows > 0;
        }

        // --- CLINICAL DATA & REPORTS FOR PATIENT PORTAL ---

        public async Task<IEnumerable<dynamic>> GetPatientDentalNotesSummaryAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT 
                    N.NoteId, N.PatientId, N.DentistId, N.Summary, N.ChiefComplaint,
                    N.TreatmentPerformed, N.PostOpAdvice, N.FollowUp, N.CreatedAt,
                    D.FirstName + ' ' + D.LastName AS DoctorName
                FROM [dentist].[DentalNotes] N
                LEFT JOIN [dentist].[Doctors] D ON N.DentistId = D.DoctorID
                WHERE N.PatientId = @PatientId AND ISNULL(N.IsDeleted, 0) = 0
                ORDER BY N.CreatedAt DESC";

            return await connection.QueryAsync(sql, new { PatientId = patientId });
        }

        public async Task<IEnumerable<Prescription>> GetPatientPrescriptionsListAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT * FROM [dentist].[Prescriptions]
                WHERE PatientID = @PatientID
                ORDER BY PrescribedDate DESC";
            return await connection.QueryAsync<Prescription>(sql, new { PatientID = patientId });
        }

        public async Task<IEnumerable<dynamic>> GetPatientNotePrescriptionsAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT P.*, N.CreatedAt
                FROM [dentist].[DentalNotePrescriptions] P
                INNER JOIN [dentist].[DentalNotes] N ON P.NoteId = N.NoteId
                WHERE N.PatientId = @PatientId AND ISNULL(N.IsDeleted, 0) = 0
                ORDER BY N.CreatedAt DESC";
            return await connection.QueryAsync(sql, new { PatientId = patientId });
        }

        public async Task<IEnumerable<Radiograph>> GetPatientRadiographsAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT RadiographID, PatientID, DoctorID, ImageName, MimeType, UploadedAt, AnalysisSummary, ImageData
                FROM [dentist].[Radiographs]
                WHERE PatientID = @PatientID
                ORDER BY UploadedAt DESC";
            return await connection.QueryAsync<Radiograph>(sql, new { PatientID = patientId });
        }

        public async Task<IEnumerable<TeethState>> GetPatientTeethStatesAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT * FROM [dentist].[TeethState]
                WHERE PatientID = @PatientID
                ORDER BY ToothNumber ASC";
            return await connection.QueryAsync<TeethState>(sql, new { PatientID = patientId });
        }

        public async Task<bool> ResetDoctorFeeScheduleToMasterAsync(int doctorId, string? targetCurrency = null)
        {
            using var connection = CreateConnection();
            string docSql = "SELECT Region FROM [dentist].[Doctors] WHERE DoctorID = @DoctorID";
            string? region = await connection.QueryFirstOrDefaultAsync<string>(docSql, new { DoctorID = doctorId });
            string currency = !string.IsNullOrWhiteSpace(targetCurrency) ? targetCurrency : (region == "PK" ? "PKR" : "NZD");

            string resetSql = @"
                DELETE FROM [dentist].[DoctorFeeSchedules] WHERE DoctorID = @DoctorID;

                INSERT INTO [dentist].[DoctorFeeSchedules]
                (DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description, IsActive, CreatedAt, UpdatedAt)
                SELECT 
                    @DoctorID,
                    @Currency,
                    ProcedureCode,
                    ProcedureName,
                    Category,
                    EstimatedDuration,
                    CASE WHEN @Currency = 'PKR' THEN DefaultFeePKR ELSE DefaultFeeNZD END,
                    Description,
                    1,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM [dentist].[MasterProcedureCatalog];";

            await connection.ExecuteAsync(resetSql, new { DoctorID = doctorId, Currency = currency });
            return true;
        }

        public async Task<IEnumerable<DoctorFeeScheduleItem>> GetDoctorFeeScheduleAsync(int doctorId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT FeeScheduleID, DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description, IsActive, UpdatedAt
                FROM [dentist].[DoctorFeeSchedules]
                WHERE DoctorID = @DoctorID AND IsActive = 1
                ORDER BY Category, ProcedureName";
            var list = (await connection.QueryAsync<DoctorFeeScheduleItem>(sql, new { DoctorID = doctorId })).ToList();

            if (list.Count == 0)
            {
                await ResetDoctorFeeScheduleToMasterAsync(doctorId);
                list = (await connection.QueryAsync<DoctorFeeScheduleItem>(sql, new { DoctorID = doctorId })).ToList();
            }

            return list;
        }

        public async Task<bool> UpdateDoctorFeeScheduleAsync(int doctorId, string currency, List<DoctorFeeScheduleItem> procedures)
        {
            using var connection = CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var proc in procedures)
                {
                    string upsertSql = @"
                        IF EXISTS (SELECT 1 FROM [dentist].[DoctorFeeSchedules] 
                                   WHERE DoctorID = @DoctorID 
                                     AND ((@FeeScheduleID > 0 AND FeeScheduleID = @FeeScheduleID) OR ProcedureCode = @ProcedureCode))
                        BEGIN
                            UPDATE [dentist].[DoctorFeeSchedules]
                            SET Currency = @Currency,
                                ProcedureName = @ProcedureName,
                                StandardFee = @StandardFee,
                                EstimatedDuration = @EstimatedDuration,
                                Description = @Description,
                                Category = @Category,
                                IsActive = 1,
                                UpdatedAt = GETDATE()
                            WHERE DoctorID = @DoctorID 
                              AND ((@FeeScheduleID > 0 AND FeeScheduleID = @FeeScheduleID) OR ProcedureCode = @ProcedureCode);
                        END
                        ELSE
                        BEGIN
                            INSERT INTO [dentist].[DoctorFeeSchedules]
                            (DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description, IsActive, UpdatedAt)
                            VALUES
                            (@DoctorID, @Currency, @ProcedureCode, @ProcedureName, @Category, @EstimatedDuration, @StandardFee, @Description, 1, GETDATE());
                        END";

                    await connection.ExecuteAsync(upsertSql, new
                    {
                        DoctorID = doctorId,
                        Currency = currency,
                        FeeScheduleID = proc.FeeScheduleID,
                        ProcedureCode = string.IsNullOrWhiteSpace(proc.ProcedureCode) ? $"CUSTOM-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}" : proc.ProcedureCode.Trim(),
                        ProcedureName = proc.ProcedureName?.Trim() ?? "Custom Procedure",
                        Category = string.IsNullOrWhiteSpace(proc.Category) ? "General" : proc.Category.Trim(),
                        EstimatedDuration = string.IsNullOrWhiteSpace(proc.EstimatedDuration) ? "45 mins" : proc.EstimatedDuration.Trim(),
                        StandardFee = proc.StandardFee,
                        Description = proc.Description
                    }, transaction);
                }

                string updateCurrSql = "UPDATE [dentist].[DoctorFeeSchedules] SET Currency = @Currency WHERE DoctorID = @DoctorID";
                await connection.ExecuteAsync(updateCurrSql, new { Currency = currency, DoctorID = doctorId }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================================================================
        // 📸 DENTIA IMAGING, GROQ VISION & VOICESTUDIO REPOSITORY METHODS
        // =========================================================================

        public async Task<int> InsertRadiographAsync(RadiographRecord radiograph)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[radiographs] 
                (patient_id, tooth_key, modality, source_device_type, source_device_brand, source_device_model, file_url, thumbnail_url, mime_type, file_size_bytes, captured_at, appointment_id, uploaded_by, analysis_status, analysis_error, created_at)
                VALUES 
                (@PatientId, @ToothKey, @Modality, @SourceDeviceType, @SourceDeviceBrand, @SourceDeviceModel, @FileUrl, @ThumbnailUrl, @MimeType, @FileSizeBytes, @CapturedAt, @AppointmentId, @UploadedBy, @AnalysisStatus, @AnalysisError, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, radiograph);
        }

        public async Task<RadiographRecord?> GetRadiographRecordByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[radiographs] WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<RadiographRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<RadiographRecord>> GetRadiographsPagedAsync(int patientId, string? modality, string? brand, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 30)
        {
            using var connection = CreateConnection();
            int offset = (page - 1) * pageSize;
            string sql = @"
                SELECT * FROM [dentist].[radiographs]
                WHERE patient_id = @PatientId
                  AND (@Modality IS NULL OR @Modality = 'all' OR modality = @Modality)
                  AND (@Brand IS NULL OR @Brand = 'all' OR source_device_brand = @Brand)
                  AND (@FromDate IS NULL OR captured_at >= @FromDate)
                  AND (@ToDate IS NULL OR captured_at <= @ToDate)
                ORDER BY captured_at DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            return await connection.QueryAsync<RadiographRecord>(sql, new 
            { 
                PatientId = patientId, 
                Modality = modality, 
                Brand = brand, 
                FromDate = fromDate, 
                ToDate = toDate, 
                Offset = offset, 
                PageSize = pageSize 
            });
        }

        public async Task<bool> UpdateRadiographAnalysisStatusAsync(int id, string status, string? error)
        {
            using var connection = CreateConnection();
            string sql = @"
                UPDATE [dentist].[radiographs]
                SET analysis_status = @Status,
                    analysis_error = @Error
                WHERE id = @Id;";

            int rows = await connection.ExecuteAsync(sql, new { Id = id, Status = status, Error = error });
            return rows > 0;
        }

        // --- AI Findings Methods ---

        public async Task<int> InsertAIFindingAsync(AIFindingRecord finding)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[ai_findings]
                (radiograph_id, patient_id, tooth_number, numbering_system, surfaces, finding_text, suggested_condition, suggested_cdt_code, confidence, status, reviewed_by, reviewed_at, created_at)
                VALUES
                (@RadiographId, @PatientId, @ToothNumber, @NumberingSystem, @Surfaces, @FindingText, @SuggestedCondition, @SuggestedCdtCode, @Confidence, @Status, @ReviewedBy, @ReviewedAt, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, finding);
        }

        public async Task<IEnumerable<AIFindingRecord>> GetAIFindingsByPatientAsync(int patientId, string? status = null)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT * FROM [dentist].[ai_findings]
                WHERE patient_id = @PatientId
                  AND (@Status IS NULL OR status = @Status)
                ORDER BY created_at DESC;";

            return await connection.QueryAsync<AIFindingRecord>(sql, new { PatientId = patientId, Status = status });
        }

        public async Task<AIFindingRecord?> GetAIFindingByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[ai_findings] WHERE id = @Id;";
            return await connection.QueryFirstOrDefaultAsync<AIFindingRecord>(sql, new { Id = id });
        }

        public async Task<bool> UpdateAIFindingStatusAsync(int id, string status, int? reviewedBy, int? teethStateId)
        {
            using var connection = CreateConnection();
            string sql = @"
                UPDATE [dentist].[ai_findings]
                SET status = @Status,
                    reviewed_by = @ReviewedBy,
                    reviewed_at = SYSUTCDATETIME(),
                    applied_teeth_state_id = @TeethStateId
                WHERE id = @Id;";

            int rows = await connection.ExecuteAsync(sql, new { Id = id, Status = status, ReviewedBy = reviewedBy, TeethStateId = teethStateId });
            return rows > 0;
        }

        public async Task<int> UpsertTeethStateAsync(TeethState state)
        {
            using var connection = CreateConnection();
            string sql = @"
                MERGE [dentist].[TeethState] AS target
                USING (SELECT @PatientID AS PatientID, @ToothNumber AS ToothNumber, @ToothKey AS ToothKey) AS source
                ON (target.PatientID = source.PatientID AND (target.ToothKey = source.ToothKey OR (target.ToothNumber = source.ToothNumber AND target.ToothNumber <> 0)))
                WHEN MATCHED THEN
                    UPDATE SET 
                        ConditionStatus = @ConditionStatus,
                        ConditionColor = @ConditionColor,
                        Comments = @Comments,
                        DoctorID = @DoctorID,
                        LastUpdated = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (PatientID, DoctorID, ToothNumber, ToothKey, DentitionCategory, ConditionColor, ConditionStatus, Comments, LastUpdated)
                    VALUES (@PatientID, @DoctorID, @ToothNumber, @ToothKey, @DentitionCategory, @ConditionColor, @ConditionStatus, @Comments, SYSUTCDATETIME());
                
                SELECT TeethStateID FROM [dentist].[TeethState] 
                WHERE PatientID = @PatientID AND (ToothKey = @ToothKey OR (ToothNumber = @ToothNumber AND ToothNumber <> 0));";

            return await connection.ExecuteScalarAsync<int>(sql, state);
        }

        // --- AI Notes Drafts Methods ---

        public async Task<int> InsertAINoteDraftAsync(AINoteDraftRecord draft)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[ai_notes_drafts]
                (radiograph_id, patient_id, doctor_id, source_type, source_device_label, soap_json, status, created_at)
                VALUES
                (@RadiographId, @PatientId, @DoctorId, @SourceType, @SourceDeviceLabel, @SoapJson, @Status, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, draft);
        }

        public async Task<AINoteDraftRecord?> GetAINoteDraftByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[ai_notes_drafts] WHERE id = @Id;";
            return await connection.QueryFirstOrDefaultAsync<AINoteDraftRecord>(sql, new { Id = id });
        }

        public async Task<bool> MarkAINoteDraftSignedAsync(int draftId, int signedBy, long officialNoteId)
        {
            using var connection = CreateConnection();
            string sql = @"
                UPDATE [dentist].[ai_notes_drafts]
                SET status = 'signed',
                    signed_by = @SignedBy,
                    signed_at = SYSUTCDATETIME(),
                    final_dental_note_id = @OfficialNoteId
                WHERE id = @DraftId;";

            int rows = await connection.ExecuteAsync(sql, new { DraftId = draftId, SignedBy = signedBy, OfficialNoteId = officialNoteId });
            return rows > 0;
        }

        public async Task<long> InsertOfficialClinicalNoteAsync(DentalNote note)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[DentalNotes]
                (PatientId, DentistId, Examination, TreatmentPerformed, Assessment, ChiefComplaint, History, PostOpAdvice, FollowUp, Status, ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt)
                VALUES
                (@PatientId, @DentistId, @Examination, @TreatmentPerformed, @Assessment, @ChiefComplaint, @History, @PostOpAdvice, @FollowUp, @Status, @ApprovedBy, @ApprovedAt, SYSUTCDATETIME(), SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            return await connection.ExecuteScalarAsync<long>(sql, note);
        }

        // --- VoiceStudio Doctor Voice Profiles & Audio Messages ---

        public async Task<DoctorVoiceProfile?> GetDoctorVoiceProfileAsync(int doctorId)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[DoctorVoiceProfiles] WHERE doctor_id = @DoctorId AND is_active = 1;";
            return await connection.QueryFirstOrDefaultAsync<DoctorVoiceProfile>(sql, new { DoctorId = doctorId });
        }

        public async Task<int> SaveDoctorVoiceProfileAsync(DoctorVoiceProfile profile)
        {
            using var connection = CreateConnection();
            string sql = @"
                MERGE [dentist].[DoctorVoiceProfiles] AS target
                USING (SELECT @DoctorId AS DoctorId) AS source
                ON (target.doctor_id = source.DoctorId)
                WHEN MATCHED THEN
                    UPDATE SET 
                        voice_model_id = @VoiceModelId,
                        voice_name = @VoiceName,
                        sample_audio_url = @SampleAudioUrl,
                        preferred_language = @PreferredLanguage,
                        is_active = @IsActive,
                        updated_at = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (doctor_id, voice_model_id, voice_name, sample_audio_url, preferred_language, is_active, created_at, updated_at)
                    VALUES (@DoctorId, @VoiceModelId, @VoiceName, @SampleAudioUrl, @PreferredLanguage, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME());
                
                SELECT id FROM [dentist].[DoctorVoiceProfiles] WHERE doctor_id = @DoctorId;";

            return await connection.ExecuteScalarAsync<int>(sql, profile);
        }

        public async Task<int> InsertPatientAudioMessageAsync(PatientAudioMessage msg)
        {
            using var connection = CreateConnection();
            string sql = @"
                INSERT INTO [dentist].[PatientAudioMessages]
                (patient_id, doctor_id, note_id, message_type, audio_file_url, duration_seconds, message_text, created_at)
                VALUES
                (@PatientId, @DoctorId, @NoteId, @MessageType, @AudioFileUrl, @DurationSeconds, @MessageText, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, msg);
        }

        public async Task<IEnumerable<PatientAudioMessage>> GetPatientAudioMessagesAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = @"
                SELECT * FROM [dentist].[PatientAudioMessages]
                WHERE patient_id = @PatientId
                ORDER BY created_at DESC;";

            return await connection.QueryAsync<PatientAudioMessage>(sql, new { PatientId = patientId });
        }

        // ==========================================
        // CLINICAL SPECIALTIES: IMPLANT PLANNING CRUD
        // ==========================================
        public async Task<IEnumerable<ImplantPlanRecord>> GetImplantPlansByPatientAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[ImplantPlans] WHERE PatientID = @PatientId ORDER BY CreatedAt DESC;";
            return await connection.QueryAsync<ImplantPlanRecord>(sql, new { PatientId = patientId });
        }

        public async Task<ImplantPlanRecord?> GetImplantPlanByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[ImplantPlans] WHERE ImplantPlanID = @Id;";
            return await connection.QueryFirstOrDefaultAsync<ImplantPlanRecord>(sql, new { Id = id });
        }

        public async Task<int> SaveImplantPlanAsync(ImplantPlanRecord plan)
        {
            using var connection = CreateConnection();
            if (plan.ImplantPlanID > 0)
            {
                string updateSql = @"
                    UPDATE [dentist].[ImplantPlans]
                    SET DoctorID = @DoctorID,
                        ToothNumber = @ToothNumber,
                        ToothKey = @ToothKey,
                        ImplantBrand = @ImplantBrand,
                        ImplantLength = @ImplantLength,
                        ImplantDiameter = @ImplantDiameter,
                        BoneQuality = @BoneQuality,
                        BoneHeightAvailable = @BoneHeightAvailable,
                        BoneWidthAvailable = @BoneWidthAvailable,
                        GraftingRequired = @GraftingRequired,
                        SinusLiftStatus = @SinusLiftStatus,
                        CbctReferenceUrl = @CbctReferenceUrl,
                        DigitalPlanningNotes = @DigitalPlanningNotes,
                        GuidedSurgeryFlag = @GuidedSurgeryFlag,
                        PlanStatus = @PlanStatus,
                        PlannedDate = @PlannedDate,
                        PlacementDate = @PlacementDate,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE ImplantPlanID = @ImplantPlanID;
                    SELECT @ImplantPlanID;";
                return await connection.ExecuteScalarAsync<int>(updateSql, plan);
            }
            else
            {
                string insertSql = @"
                    INSERT INTO [dentist].[ImplantPlans]
                    (PatientID, DoctorID, ToothNumber, ToothKey, ImplantBrand, ImplantLength, ImplantDiameter,
                     BoneQuality, BoneHeightAvailable, BoneWidthAvailable, GraftingRequired, SinusLiftStatus,
                     CbctReferenceUrl, DigitalPlanningNotes, GuidedSurgeryFlag, PlanStatus, PlannedDate, PlacementDate,
                     CreatedAt, UpdatedAt)
                    VALUES
                    (@PatientID, @DoctorID, @ToothNumber, @ToothKey, @ImplantBrand, @ImplantLength, @ImplantDiameter,
                     @BoneQuality, @BoneHeightAvailable, @BoneWidthAvailable, @GraftingRequired, @SinusLiftStatus,
                     @CbctReferenceUrl, @DigitalPlanningNotes, @GuidedSurgeryFlag, @PlanStatus, @PlannedDate, @PlacementDate,
                     SYSUTCDATETIME(), SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                return await connection.ExecuteScalarAsync<int>(insertSql, plan);
            }
        }

        public async Task<bool> DeleteImplantPlanAsync(int id)
        {
            using var connection = CreateConnection();
            int rows = await connection.ExecuteAsync("DELETE FROM [dentist].[ImplantPlans] WHERE ImplantPlanID = @Id;", new { Id = id });
            return rows > 0;
        }

        // ==========================================
        // CLINICAL SPECIALTIES: BIOPSY & PATHOLOGY CRUD
        // ==========================================
        public async Task<IEnumerable<BiopsyRecord>> GetBiopsyRecordsByPatientAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[BiopsyRecords] WHERE PatientID = @PatientId ORDER BY BiopsyDate DESC, CreatedAt DESC;";
            return await connection.QueryAsync<BiopsyRecord>(sql, new { PatientId = patientId });
        }

        public async Task<BiopsyRecord?> GetBiopsyRecordByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[BiopsyRecords] WHERE BiopsyID = @Id;";
            return await connection.QueryFirstOrDefaultAsync<BiopsyRecord>(sql, new { Id = id });
        }

        public async Task<int> SaveBiopsyRecordAsync(BiopsyRecord biopsy)
        {
            using var connection = CreateConnection();
            if (biopsy.BiopsyID > 0)
            {
                string updateSql = @"
                    UPDATE [dentist].[BiopsyRecords]
                    SET DoctorID = @DoctorID,
                        BiopsyType = @BiopsyType,
                        SiteOfBiopsy = @SiteOfBiopsy,
                        ToothNumber = @ToothNumber,
                        ToothKey = @ToothKey,
                        ClinicalImpression = @ClinicalImpression,
                        PathologyLabName = @PathologyLabName,
                        SpecimenReference = @SpecimenReference,
                        BiopsyDate = @BiopsyDate,
                        Status = @Status,
                        HistopathologyDiagnosis = @HistopathologyDiagnosis,
                        ResultsNotes = @ResultsNotes,
                        FollowUpRequired = @FollowUpRequired,
                        FollowUpDate = @FollowUpDate,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE BiopsyID = @BiopsyID;
                    SELECT @BiopsyID;";
                return await connection.ExecuteScalarAsync<int>(updateSql, biopsy);
            }
            else
            {
                string insertSql = @"
                    INSERT INTO [dentist].[BiopsyRecords]
                    (PatientID, DoctorID, BiopsyType, SiteOfBiopsy, ToothNumber, ToothKey, ClinicalImpression,
                     PathologyLabName, SpecimenReference, BiopsyDate, Status, HistopathologyDiagnosis, ResultsNotes,
                     FollowUpRequired, FollowUpDate, CreatedAt, UpdatedAt)
                    VALUES
                    (@PatientID, @DoctorID, @BiopsyType, @SiteOfBiopsy, @ToothNumber, @ToothKey, @ClinicalImpression,
                     @PathologyLabName, @SpecimenReference, @BiopsyDate, @Status, @HistopathologyDiagnosis, @ResultsNotes,
                     @FollowUpRequired, @FollowUpDate, SYSUTCDATETIME(), SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                return await connection.ExecuteScalarAsync<int>(insertSql, biopsy);
            }
        }

        public async Task<bool> DeleteBiopsyRecordAsync(int id)
        {
            using var connection = CreateConnection();
            int rows = await connection.ExecuteAsync("DELETE FROM [dentist].[BiopsyRecords] WHERE BiopsyID = @Id;", new { Id = id });
            return rows > 0;
        }

        // ==========================================
        // CLINICAL SPECIALTIES: ORTHODONTICS - CLEAR ALIGNERS CRUD
        // ==========================================
        public async Task<IEnumerable<OrthoAlignerTreatmentRecord>> GetOrthoAlignersByPatientAsync(int patientId)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[OrthoAlignerTreatments] WHERE PatientID = @PatientId ORDER BY CreatedAt DESC;";
            return await connection.QueryAsync<OrthoAlignerTreatmentRecord>(sql, new { PatientId = patientId });
        }

        public async Task<OrthoAlignerTreatmentRecord?> GetOrthoAlignerByIdAsync(int id)
        {
            using var connection = CreateConnection();
            string sql = "SELECT * FROM [dentist].[OrthoAlignerTreatments] WHERE OrthoAlignerID = @Id;";
            return await connection.QueryFirstOrDefaultAsync<OrthoAlignerTreatmentRecord>(sql, new { Id = id });
        }

        public async Task<int> SaveOrthoAlignerAsync(OrthoAlignerTreatmentRecord ortho)
        {
            using var connection = CreateConnection();
            if (ortho.OrthoAlignerID > 0)
            {
                string updateSql = @"
                    UPDATE [dentist].[OrthoAlignerTreatments]
                    SET DoctorID = @DoctorID,
                        AlignerBrand = @AlignerBrand,
                        TotalStages = @TotalStages,
                        CurrentStage = @CurrentStage,
                        AttachmentsRequired = @AttachmentsRequired,
                        AttachmentNotes = @AttachmentNotes,
                        IprRequired = @IprRequired,
                        IprDetails = @IprDetails,
                        WearSchedule = @WearSchedule,
                        RefinementScanTracking = @RefinementScanTracking,
                        RefinementCount = @RefinementCount,
                        Arch = @Arch,
                        Status = @Status,
                        StartDate = @StartDate,
                        TargetCompletionDate = @TargetCompletionDate,
                        ClinicalNotes = @ClinicalNotes,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE OrthoAlignerID = @OrthoAlignerID;
                    SELECT @OrthoAlignerID;";
                return await connection.ExecuteScalarAsync<int>(updateSql, ortho);
            }
            else
            {
                string insertSql = @"
                    INSERT INTO [dentist].[OrthoAlignerTreatments]
                    (PatientID, DoctorID, AlignerBrand, TotalStages, CurrentStage, AttachmentsRequired, AttachmentNotes,
                     IprRequired, IprDetails, WearSchedule, RefinementScanTracking, RefinementCount, Arch, Status,
                     StartDate, TargetCompletionDate, ClinicalNotes, CreatedAt, UpdatedAt)
                    VALUES
                    (@PatientID, @DoctorID, @AlignerBrand, @TotalStages, @CurrentStage, @AttachmentsRequired, @AttachmentNotes,
                     @IprRequired, @IprDetails, @WearSchedule, @RefinementScanTracking, @RefinementCount, @Arch, @Status,
                     @StartDate, @TargetCompletionDate, @ClinicalNotes, SYSUTCDATETIME(), SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                return await connection.ExecuteScalarAsync<int>(insertSql, ortho);
            }
        }

        public async Task<bool> DeleteOrthoAlignerAsync(int id)
        {
            using var connection = CreateConnection();
            int rows = await connection.ExecuteAsync("DELETE FROM [dentist].[OrthoAlignerTreatments] WHERE OrthoAlignerID = @Id;", new { Id = id });
            return rows > 0;
        }
    }
}
