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

        public virtual async Task<Doctor> GetDoctorByIdAsync(int id)
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Doctor>("SELECT DoctorID, Username, FirstName, LastName, Region, CreatedAt, IsSuperAdmin FROM [dentist].[Doctors] WHERE DoctorID = @Id", new { Id = id });
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Doctor>("SELECT DoctorID, Username, FirstName, LastName, Region, CreatedAt, IsSuperAdmin FROM [dentist].[Doctors] WHERE IsSuperAdmin = 0");
        }

        public async Task<int> UpdateDoctorAsync(int id, string firstName, string lastName, string region)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET FirstName = @FirstName, LastName = @LastName, Region = @Region WHERE DoctorID = @Id", new { FirstName = firstName, LastName = lastName, Region = region, Id = id });
        }

        public async Task<int> UpdateDoctorPasswordAsync(int id, string passwordHash)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync("UPDATE [dentist].[Doctors] SET PasswordHash = @PasswordHash WHERE DoctorID = @Id", new { PasswordHash = passwordHash, Id = id });
        }

        public async Task<IEnumerable<Patient>> GetPatientsByDoctorAsync(int doctorId)
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Patient>("SELECT * FROM [dentist].[Patients] WHERE DoctorID = @DoctorID ORDER BY PatientID DESC", new { DoctorID = doctorId });
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
            string query = @"
                INSERT INTO [dentist].[Patients] (DoctorID, FirstName, LastName, DOB, Phone, NHINumber, Address, Email, Gender, Region, DentitionType, CurrentTreatmentPlan, TreatmentStage, TargetShade, ProfileImage, ProfileImageMimeType, CreatedAt)
                OUTPUT INSERTED.PatientID
                VALUES (@DoctorID, @FirstName, @LastName, @DOB, @Phone, @NHINumber, @Address, @Email, @Gender, @Region, COALESCE(@DentitionType, 'Adult'), @CurrentTreatmentPlan, @TreatmentStage, @TargetShade, @ProfileImage, @ProfileImageMimeType, GETDATE())";
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
    }
}
