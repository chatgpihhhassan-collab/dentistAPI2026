using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DentistAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    public class TreatmentPolicyService : ITreatmentPolicyService
    {
        private readonly string _connectionString;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<TreatmentPolicyService> _logger;

        public TreatmentPolicyService(
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ILogger<TreatmentPolicyService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Server=DBSERVER3;Database=DentistAPI;User Id=specnz;Password=nzspe@1;TrustServerCertificate=True;";
            _auditLogService = auditLogService;
            _logger = logger;
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        // =========================================================================
        // RULE 1: DOCTOR LOCK
        // Once a treatment is started (IN_PROGRESS), its doctor cannot be changed
        // until it is COMPLETED. Applies to first-time and returning patients.
        // =========================================================================
        public async Task<PolicyResult> CanChangeDoctorAsync(int patientId, int? toothNumber = null)
        {
            if (patientId <= 0) return PolicyResult.Allow();

            using var conn = CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT TOP 1 
                    ti.TreatmentItemID,
                    ti.PatientID,
                    ti.DoctorID,
                    ti.ToothNumber,
                    ti.ToothKey,
                    ti.ProcedureName,
                    ti.Status,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Dr. Assigned') AS DoctorName
                FROM [dentist].[TreatmentItems] ti WITH (NOLOCK)
                LEFT JOIN [dentist].[Doctors] doc WITH (NOLOCK) ON ti.DoctorID = doc.DoctorID
                WHERE ti.PatientID = @PatientId
                  AND ti.Status = 'in_progress'
                ORDER BY ti.StartedAt DESC, ti.UpdatedAt DESC";

            var active = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { PatientId = patientId });

            if (active != null)
            {
                int toothNo = (int)active.ToothNumber;
                string toothLabel = toothNo > 0 ? toothNo.ToString() : (active.ToothKey ?? "ALL");
                string treatment = (string)active.ProcedureName ?? "Procedure";
                int docId = (int)active.DoctorID;
                string docName = (string)active.DoctorName;

                string msg = $"Treatment is already in progress on Tooth #{toothLabel} ({treatment}). The doctor cannot be changed until this treatment is completed.";
                string msgUrdu = $"دانت نمبر {toothLabel} پر ({treatment}) کا علاج جاری ہے۔ جب تک یہ علاج مکمل نہیں ہو جاتا، ڈاکٹر تبدیل نہیں کیا جا سکتا۔";

                return PolicyResult.Block(
                    TreatmentPolicyErrorCodes.DoctorLocked,
                    msg,
                    msgUrdu,
                    toothNumber: toothNo,
                    toothKey: active.ToothKey,
                    treatmentName: treatment,
                    activeDoctorId: docId,
                    activeDoctorName: docName,
                    activeTreatmentItemId: (long)active.TreatmentItemID,
                    reasonCode: "IN_PROGRESS_DOCTOR_LOCKED"
                );
            }

            return PolicyResult.Allow();
        }

        // =========================================================================
        // RULE 2 & D1: ONE ACTIVE TREATMENT AT A TIME (PATIENT-LEVEL)
        // A new treatment can start only when the patient has no IN_PROGRESS treatment.
        // =========================================================================
        public async Task<PolicyResult> CanStartTreatmentAsync(int patientId, long treatmentItemId)
        {
            if (patientId <= 0) return PolicyResult.Allow();

            using var conn = CreateConnection();
            await conn.OpenAsync();

            // Row-lock or check for any active in_progress treatment for this patient
            string sql = @"
                SELECT TOP 1 
                    ti.TreatmentItemID,
                    ti.PatientID,
                    ti.DoctorID,
                    ti.ToothNumber,
                    ti.ToothKey,
                    ti.ProcedureName,
                    ti.Status,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Dr. Assigned') AS DoctorName
                FROM [dentist].[TreatmentItems] ti WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN [dentist].[Doctors] doc WITH (NOLOCK) ON ti.DoctorID = doc.DoctorID
                WHERE ti.PatientID = @PatientId
                  AND ti.Status = 'in_progress'
                  AND ti.TreatmentItemID <> @ItemId
                ORDER BY ti.StartedAt DESC, ti.UpdatedAt DESC";

            var active = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { PatientId = patientId, ItemId = treatmentItemId });

            if (active != null)
            {
                int toothNo = (int)active.ToothNumber;
                string toothLabel = toothNo > 0 ? toothNo.ToString() : (active.ToothKey ?? "General");
                string treatment = (string)active.ProcedureName ?? "Procedure";

                string msg = $"Treatment on Tooth #{toothLabel} ({treatment}) is still in progress. Please complete it before starting a new treatment.";
                string msgUrdu = $"دانت نمبر {toothLabel} پر ({treatment}) کا علاج ابھی جاری ہے۔ نیا علاج شروع کرنے سے پہلے براہ کرم اسے مکمل کریں۔";

                return PolicyResult.Block(
                    TreatmentPolicyErrorCodes.ActiveTreatmentExists,
                    msg,
                    msgUrdu,
                    toothNumber: toothNo,
                    toothKey: active.ToothKey,
                    treatmentName: treatment,
                    activeDoctorId: (int)active.DoctorID,
                    activeDoctorName: (string)active.DoctorName,
                    activeTreatmentItemId: (long)active.TreatmentItemID,
                    reasonCode: "PATIENT_LEVEL_ACTIVE_EXISTS"
                );
            }

            return PolicyResult.Allow();
        }

        // =========================================================================
        // RULE 4 & D2: STARTED + ANY PAYMENT = CHART LOCKED FOR THAT TOOTH
        // If treatment is IN_PROGRESS and net paid amount > 0, doctor cannot update
        // the dental chart for that tooth.
        // Rule 5: When treatment is COMPLETED, tooth is unlocked.
        // =========================================================================
        public async Task<PolicyResult> CanEditChartAsync(int patientId, int toothNumber, string? toothKey = null)
        {
            if (patientId <= 0 || toothNumber <= 0) return PolicyResult.Allow();

            using var conn = CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    ti.TreatmentItemID,
                    ti.PatientID,
                    ti.DoctorID,
                    ti.ToothNumber,
                    ti.ToothKey,
                    ti.ProcedureName,
                    ti.Status,
                    COALESCE(SUM(pa.amount), 0) AS NetPaidAmount,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Dr. Assigned') AS DoctorName
                FROM [dentist].[TreatmentItems] ti WITH (NOLOCK)
                LEFT JOIN [dentist].[payment_allocations] pa WITH (NOLOCK) ON ti.TreatmentItemID = pa.treatment_item_id
                LEFT JOIN [dentist].[Payments] p WITH (NOLOCK) ON pa.payment_id = p.PaymentID AND p.Status = 'active'
                LEFT JOIN [dentist].[Doctors] doc WITH (NOLOCK) ON ti.DoctorID = doc.DoctorID
                WHERE ti.PatientID = @PatientId
                  AND (ti.ToothNumber = @ToothNumber OR (@ToothKey IS NOT NULL AND ti.ToothKey = @ToothKey))
                  AND ti.Status = 'in_progress'
                GROUP BY 
                    ti.TreatmentItemID, ti.PatientID, ti.DoctorID, ti.ToothNumber, ti.ToothKey, 
                    ti.ProcedureName, ti.Status, doc.FirstName, doc.LastName";

            var item = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { PatientId = patientId, ToothNumber = toothNumber, ToothKey = toothKey });

            if (item != null)
            {
                int netPaid = (int)item.NetPaidAmount;
                // Rule 4: If in_progress AND net paid > 0, lock the chart!
                if (netPaid > 0)
                {
                    string toothLabel = toothNumber.ToString();
                    if (!string.IsNullOrWhiteSpace(toothKey) && toothKey != toothLabel)
                    {
                        toothLabel = toothKey;
                    }

                    string treatment = (string)item.ProcedureName ?? "Procedure";
                    string msg = $"Treatment has started and payment has been received for Tooth #{toothLabel}. The chart cannot be updated until this treatment is completed.";
                    string msgUrdu = $"دانت نمبر {toothLabel} کا علاج شروع ہو چکا ہے اور ادائیگی موصول ہو چکی ہے۔ جب تک یہ علاج مکمل نہیں ہو جاتا، چارٹ تبدیل نہیں کیا جا سکتا۔";

                    return PolicyResult.Block(
                        TreatmentPolicyErrorCodes.ChartLocked,
                        msg,
                        msgUrdu,
                        toothNumber: toothNumber,
                        toothKey: item.ToothKey,
                        treatmentName: treatment,
                        activeDoctorId: (int)item.DoctorID,
                        activeDoctorName: (string)item.DoctorName,
                        netPaidAmount: netPaid,
                        activeTreatmentItemId: (long)item.TreatmentItemID,
                        reasonCode: "IN_PROGRESS_WITH_PAYMENT"
                    );
                }
            }

            return PolicyResult.Allow();
        }

        // =========================================================================
        // RULE 3: NOT STARTED = FREELY EDITABLE
        // While a treatment is PLANNED (not started, no payment), doctor can edit it.
        // Completed items cannot be edited (remain as permanent clinical history).
        // =========================================================================
        public async Task<PolicyResult> CanEditTreatmentAsync(long treatmentItemId)
        {
            if (treatmentItemId <= 0) return PolicyResult.Allow();

            using var conn = CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    ti.TreatmentItemID,
                    ti.PatientID,
                    ti.ToothNumber,
                    ti.ToothKey,
                    ti.ProcedureName,
                    ti.Status,
                    COALESCE(SUM(pa.amount), 0) AS NetPaidAmount
                FROM [dentist].[TreatmentItems] ti WITH (NOLOCK)
                LEFT JOIN [dentist].[payment_allocations] pa WITH (NOLOCK) ON ti.TreatmentItemID = pa.treatment_item_id
                LEFT JOIN [dentist].[Payments] p WITH (NOLOCK) ON pa.payment_id = p.PaymentID AND p.Status = 'active'
                WHERE ti.TreatmentItemID = @ItemId
                GROUP BY ti.TreatmentItemID, ti.PatientID, ti.ToothNumber, ti.ToothKey, ti.ProcedureName, ti.Status";

            var item = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { ItemId = treatmentItemId });
            if (item == null) return PolicyResult.Allow();

            string status = (string)item.Status;
            int netPaid = (int)item.NetPaidAmount;

            if (status == "completed")
            {
                return PolicyResult.Block(
                    TreatmentPolicyErrorCodes.TreatmentLocked,
                    "Completed treatments are locked as permanent clinical history and cannot be edited.",
                    "مکمل شدہ علاج مستقل ریکارڈ ہے اور اس میں تبدیلی نہیں کی جا سکتی۔",
                    toothNumber: (int)item.ToothNumber,
                    toothKey: item.ToothKey,
                    treatmentName: (string)item.ProcedureName,
                    activeTreatmentItemId: treatmentItemId,
                    reasonCode: "COMPLETED_HISTORY_LOCKED"
                );
            }

            if (status == "in_progress" && netPaid > 0)
            {
                return PolicyResult.Block(
                    TreatmentPolicyErrorCodes.TreatmentLocked,
                    $"Treatment on Tooth #{item.ToothNumber} is in progress with payments received and cannot be modified directly.",
                    $"دانت نمبر {item.ToothNumber} پر علاج جاری ہے اور ادائیگی ہو چکی ہے، لہٰذا اس میں ترمیم نہیں ہو سکتی۔",
                    toothNumber: (int)item.ToothNumber,
                    toothKey: item.ToothKey,
                    treatmentName: (string)item.ProcedureName,
                    netPaidAmount: netPaid,
                    activeTreatmentItemId: treatmentItemId,
                    reasonCode: "IN_PROGRESS_PAID_LOCKED"
                );
            }

            return PolicyResult.Allow();
        }

        // =========================================================================
        // COMPREHENSIVE POLICY SNAPSHOT FOR UI CONSUMPTION
        // =========================================================================
        public async Task<PatientTreatmentPolicyDto> GetPatientPolicyAsync(int patientId)
        {
            var dto = new PatientTreatmentPolicyDto { PatientId = patientId };
            if (patientId <= 0) return dto;

            using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Fetch active in_progress treatment (Rule 1 & Rule 2)
            string activeSql = @"
                SELECT TOP 1 
                    ti.TreatmentItemID,
                    ti.ToothNumber,
                    ti.ToothKey,
                    ti.ProcedureName,
                    ti.DoctorID,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Dr. Assigned') AS DoctorName,
                    COALESCE(SUM(pa.amount), 0) AS NetPaidAmount
                FROM [dentist].[TreatmentItems] ti WITH (NOLOCK)
                LEFT JOIN [dentist].[payment_allocations] pa WITH (NOLOCK) ON ti.TreatmentItemID = pa.treatment_item_id
                LEFT JOIN [dentist].[Payments] p WITH (NOLOCK) ON pa.payment_id = p.PaymentID AND p.Status = 'active'
                LEFT JOIN [dentist].[Doctors] doc WITH (NOLOCK) ON ti.DoctorID = doc.DoctorID
                WHERE ti.PatientID = @PatientId
                  AND ti.Status = 'in_progress'
                GROUP BY ti.TreatmentItemID, ti.ToothNumber, ti.ToothKey, ti.ProcedureName, ti.DoctorID, doc.FirstName, doc.LastName
                ORDER BY ti.TreatmentItemID DESC";

            var active = await conn.QueryFirstOrDefaultAsync<dynamic>(activeSql, new { PatientId = patientId });

            if (active != null)
            {
                dto.HasActiveTreatment = true;
                dto.ActiveTreatmentItemId = (long)active.TreatmentItemID;
                dto.ActiveToothNumber = (int)active.ToothNumber;
                dto.ActiveToothKey = active.ToothKey;
                dto.ActiveTreatmentName = (string)active.ProcedureName;
                dto.ActiveDoctorId = (int)active.DoctorID;
                dto.ActiveDoctorName = (string)active.DoctorName;
                dto.ActiveNetPaid = (int)active.NetPaidAmount;

                // Doctor lock enforced
                dto.CanChangeDoctor = false;
                string toothLabel = dto.ActiveToothNumber > 0 ? dto.ActiveToothNumber.ToString() : (dto.ActiveToothKey ?? "General");
                dto.DoctorLockReason = $"Treatment is already in progress on Tooth #{toothLabel} ({dto.ActiveTreatmentName}). The doctor cannot be changed until this treatment is completed.";
                dto.DoctorLockReasonUrdu = $"دانت نمبر {toothLabel} پر ({dto.ActiveTreatmentName}) کا علاج جاری ہے۔ جب تک یہ علاج مکمل نہیں ہو جاتا، ڈاکٹر تبدیل نہیں کیا جا سکتا۔";

                // New treatment blocked
                dto.CanStartNewTreatment = false;
                dto.StartLockReason = $"Treatment on Tooth #{toothLabel} ({dto.ActiveTreatmentName}) is still in progress. Please complete it before starting a new treatment.";
                dto.StartLockReasonUrdu = $"دانت نمبر {toothLabel} پر ({dto.ActiveTreatmentName}) کا علاج ابھی جاری ہے۔ نیا علاج شروع کرنے سے پہلے براہ کرم اسے مکمل کریں۔";

                // If net paid > 0, lock this tooth's chart
                if (dto.ActiveNetPaid > 0 && dto.ActiveToothNumber.HasValue && dto.ActiveToothNumber.Value > 0)
                {
                    dto.LockedToothNumbers.Add(dto.ActiveToothNumber.Value);
                    if (!string.IsNullOrWhiteSpace(dto.ActiveToothKey))
                    {
                        dto.LockedToothKeys.Add(dto.ActiveToothKey);
                    }
                }
            }

            // 2. Compute total outstanding balance across all treatments (Decision D3)
            string balSql = @"
                SELECT 
                    COALESCE(SUM(ti.FinalPrice), 0) - COALESCE(SUM(alloc.AllocatedSum), 0) AS OutstandingBalance
                FROM [dentist].[TreatmentItems] ti WITH (NOLOCK)
                LEFT JOIN (
                    SELECT pa.treatment_item_id, SUM(pa.amount) AS AllocatedSum
                    FROM [dentist].[payment_allocations] pa WITH (NOLOCK)
                    JOIN [dentist].[Payments] p WITH (NOLOCK) ON pa.payment_id = p.PaymentID AND p.Status = 'active'
                    GROUP BY pa.treatment_item_id
                ) alloc ON ti.TreatmentItemID = alloc.treatment_item_id
                WHERE ti.PatientID = @PatientId
                  AND ti.Status IN ('proposed', 'accepted', 'in_progress', 'completed')";

            int outstanding = await conn.QueryFirstOrDefaultAsync<int>(balSql, new { PatientId = patientId });
            dto.TotalOutstandingBalance = Math.Max(0, outstanding);

            return dto;
        }

        // =========================================================================
        // AUDIT LOGGING FOR BLOCKED ATTEMPTS
        // =========================================================================
        public async Task LogBlockedAttemptAsync(
            int? actorId, 
            string actorRole, 
            int patientId, 
            string action, 
            string errorCode, 
            string message, 
            string? ipAddress = null)
        {
            try
            {
                // Physical file log
                _auditLogService.LogAccess(actorId, actorRole, patientId, $"BLOCKED_{action}", $"Code: {errorCode} | {message}", ipAddress);

                // Database AuditLog table
                using var conn = CreateConnection();
                await conn.OpenAsync();

                string sql = @"
                    INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, OldValue, NewValue, UserID, UserRole, IPAddress, CreatedAt)
                    VALUES ('TreatmentPolicy', @PatientId, @Action, @ErrorCode, @Message, @UserId, @UserRole, @IPAddress, SYSUTCDATETIME())";

                await conn.ExecuteAsync(sql, new
                {
                    PatientId = patientId,
                    Action = $"BLOCKED_{action}",
                    ErrorCode = errorCode,
                    Message = message,
                    UserId = actorId ?? 1,
                    UserRole = actorRole ?? "Doctor",
                    IPAddress = ipAddress
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist blocked attempt audit log for patient {PatientId}", patientId);
            }
        }
    }
}
