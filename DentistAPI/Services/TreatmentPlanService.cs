using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using DentistAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentistAPI.Services
{
    public interface ITreatmentPlanService
    {
        Task<TreatmentItem> CreateTreatmentItemAsync(CreateTreatmentItemRequest request);
        Task<List<TreatmentItem>> GetTreatmentItemsAsync(int patientId, string view);
        Task<TreatmentItem?> GetTreatmentItemByIdAsync(long itemId);
        Task<bool> UpdateTreatmentItemStatusAsync(long itemId, UpdateTreatmentItemRequest request);
        Task<bool> LinkAppointmentAsync(long itemId, LinkAppointmentRequest request);
        Task<(bool Success, string Message, string ReceiptNumber)> RecordFlexiblePaymentAsync(RecordFlexiblePaymentRequest request);
        Task<(bool Success, string Message)> AllocateCreditAsync(AllocateCreditRequest request);
        Task<(bool Success, string Message)> VoidPaymentAsync(VoidPaymentRequest request);
        Task<List<PaymentDetailDto>> GetPatientPaymentsAsync(int patientId);
        Task<AccountSummaryDto> GetAccountSummaryAsync(int patientId);
        Task<List<ActivePlanChipDto>> GetActivePlansAsync(int patientId);
        Task<List<UnplannedFindingDto>> GetUnplannedFindingsAsync(int patientId);
        Task<int> SyncDirectoryModalitiesAsync(int patientId, int doctorId);
        Task<List<TreatmentCatalogItem>> GetCatalogAsync();
        Task<AutoDiagnosticFeeResult> EnsureDiagnosticFeeAsync(int patientId, AutoDiagnosticFeeRequest request);
    }

    public class TreatmentPlanService : ITreatmentPlanService
    {
        private readonly string _connectionString;
        private readonly ITreatmentPolicyService _policyService;
        private readonly ILogger<TreatmentPlanService> _logger;

        public TreatmentPlanService(
            IConfiguration configuration, 
            ITreatmentPolicyService policyService,
            ILogger<TreatmentPlanService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? "Server=DBSERVER3;Database=DentistAPI;User Id=specnz;Password=nzspe@1;TrustServerCertificate=True;";
            _policyService = policyService;
            _logger = logger;
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        // =========================================================================
        // 1. SINGLE ENTRY POINT: CREATE TREATMENT ITEM (Idempotent via DedupeKey)
        // =========================================================================
        public async Task<TreatmentItem> CreateTreatmentItemAsync(CreateTreatmentItemRequest request)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            int toothNo = request.ToothNo ?? 0;
            string codeOrName = !string.IsNullOrWhiteSpace(request.ProcedureCode) 
                ? request.ProcedureCode.Trim() 
                : (!string.IsNullOrWhiteSpace(request.Name) ? Regex.Replace(request.Name.ToUpper(), @"[^A-Z0-9]", "") : "PROC");

            // Dedupe key: patient + code/name + tooth (e.g. "13_D2140_14" or "13_WHITENING_0")
            string dedupeKey = $"{request.PatientId}_{codeOrName}_{toothNo}";

            // Check if an active item already exists with this dedupe key
            string checkSql = @"
                SELECT * FROM [dentist].[TreatmentItems]
                WHERE PatientID = @PatientId AND DedupeKey = @DedupeKey
                  AND Status IN ('draft', 'proposed', 'accepted', 'in_progress')";

            var existing = await conn.QueryFirstOrDefaultAsync<TreatmentItem>(checkSql, new { request.PatientId, DedupeKey = dedupeKey });
            if (existing != null)
            {
                _logger.LogInformation("Idempotent check hit: TreatmentItem already exists for DedupeKey: {Key}. Returning existing.", dedupeKey);
                return existing;
            }

            // Snapshot price from Catalog if not specified
            int finalPrice = request.FinalPrice ?? request.EstimatedPrice ?? 0;
            int estimatedPrice = request.EstimatedPrice ?? finalPrice;
            string category = request.Category ?? "general";
            int? plannedVisits = request.PlannedVisits;

            if (finalPrice <= 0)
            {
                var catalogItem = await conn.QueryFirstOrDefaultAsync<TreatmentCatalogItem>(@"
                    SELECT TOP 1 * FROM [dentist].[treatment_catalog]
                    WHERE (procedure_code = @Code AND @Code <> '') OR [name] LIKE @NameLike",
                    new { Code = request.ProcedureCode ?? "", NameLike = $"%{request.Name}%" });

                if (catalogItem != null && catalogItem.DefaultPrice > 0)
                {
                    finalPrice = catalogItem.DefaultPrice;
                    estimatedPrice = catalogItem.DefaultPrice;
                    category = catalogItem.Category;
                    plannedVisits ??= catalogItem.DefaultVisits;
                    request.CatalogId ??= catalogItem.ID;
                }
                else
                {
                    throw new InvalidOperationException("Procedure fee must be greater than zero. Procedures cannot be added with a zero or negative fee.");
                }
            }

            int discount = Math.Max(0, request.Discount);
            if (discount > 0)
            {
                finalPrice = Math.Max(0, finalPrice - discount);
            }

            if (finalPrice <= 0)
            {
                throw new InvalidOperationException("Final procedure fee after discount must be greater than zero.");
            }

            string status = !string.IsNullOrWhiteSpace(request.Status) ? request.Status : "proposed";
            if (string.Equals(request.Source, "voice", StringComparison.OrdinalIgnoreCase))
            {
                status = "draft"; // Voice entries always start as draft until confirmed
            }
            else if (string.Equals(status, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                var startPolicy = await _policyService.CanStartTreatmentAsync(request.PatientId, 0);
                if (!startPolicy.Allowed)
                {
                    await _policyService.LogBlockedAttemptAsync(request.CreatedBy, "Doctor", request.PatientId, "CREATE_ACTIVE_TREATMENT", startPolicy.ErrorCode ?? TreatmentPolicyErrorCodes.ActiveTreatmentExists, startPolicy.Message);
                    throw new TreatmentPolicyException(startPolicy);
                }
            }

            string toothKey = toothNo > 0 ? toothNo.ToString() : "ALL";

            string insertSql = @"
                INSERT INTO [dentist].[TreatmentItems] (
                    PatientID, DoctorID, ToothNumber, ToothKey, ProcedureCode, ProcedureName,
                    Fee, Currency, Status, CatalogID, Category, Surface, Source, SourceRef,
                    DedupeKey, EstimatedPrice, Discount, FinalPrice, PriceNote, PlannedVisits,
                    CreatedBy, CreatedAt, UpdatedAt
                )
                OUTPUT INSERTED.TreatmentItemID
                VALUES (
                    @PatientId, @DoctorId, @ToothNo, @ToothKey, @ProcedureCode, @Name,
                    @FinalPrice, 'PKR', @Status, @CatalogId, @Category, @Surface, @Source, @SourceRef,
                    @DedupeKey, @EstimatedPrice, @Discount, @FinalPrice, @PriceNote, @PlannedVisits,
                    @CreatedBy, SYSUTCDATETIME(), SYSUTCDATETIME()
                )";

            long newItemId = await conn.QuerySingleAsync<long>(insertSql, new
            {
                request.PatientId,
                DoctorId = request.DoctorId ?? 1,
                ToothNo = toothNo,
                ToothKey = toothKey,
                ProcedureCode = request.ProcedureCode ?? "D0150",
                request.Name,
                FinalPrice = finalPrice,
                Status = status,
                request.CatalogId,
                Category = category,
                request.Surface,
                Source = request.Source ?? "manual",
                request.SourceRef,
                DedupeKey = dedupeKey,
                EstimatedPrice = estimatedPrice,
                Discount = discount,
                request.PriceNote,
                PlannedVisits = plannedVisits,
                CreatedBy = request.CreatedBy ?? 1
            });

            // Log Audit
            await conn.ExecuteAsync(@"
                INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, NewValue, UserID, UserRole, CreatedAt)
                VALUES ('TreatmentItem', @Id, 'create', @Status, @UserId, 'System', SYSUTCDATETIME())",
                new { Id = newItemId, Status = status, UserId = request.CreatedBy ?? 1 });

            var item = await conn.QuerySingleAsync<TreatmentItem>(
                "SELECT * FROM [dentist].[TreatmentItems] WHERE TreatmentItemID = @Id", new { Id = newItemId });

            item.Paid = 0;
            item.Balance = item.FinalPrice ?? (int)item.Fee;
            item.PaymentStatus = "unpaid";
            return item;
        }

        // =========================================================================
        // 2. GET TREATMENT ITEMS (Plan view vs Invoices view)
        // =========================================================================
        public async Task<List<TreatmentItem>> GetTreatmentItemsAsync(int patientId, string view)
        {
            using var conn = CreateConnection();
            string sql;

            if (string.Equals(view, "invoices", StringComparison.OrdinalIgnoreCase))
            {
                // Invoices/billing tab shows all active and completed items (everything billable, excluding cancelled/declined)
                sql = @"
                    SELECT ti.*, appt.PreferredDate AS ScheduledDate
                    FROM [dentist].[TreatmentItems] ti
                    LEFT JOIN [dentist].[Appointments] appt ON ti.AppointmentID = appt.AppointmentID
                    WHERE ti.PatientID = @PatientId
                      AND ti.Status NOT IN ('cancelled', 'declined')
                    ORDER BY 
                        CASE ti.Status 
                            WHEN 'completed' THEN 1 
                            WHEN 'in_progress' THEN 2 
                            WHEN 'accepted' THEN 3 
                            WHEN 'proposed' THEN 4 
                            ELSE 5 
                        END, 
                        ti.CompletedAt DESC, ti.UpdatedAt DESC";
            }
            else
            {
                // Plan tab shows: draft, proposed, accepted, in_progress
                sql = @"
                    SELECT ti.*, appt.PreferredDate AS ScheduledDate
                    FROM [dentist].[TreatmentItems] ti
                    LEFT JOIN [dentist].[Appointments] appt ON ti.AppointmentID = appt.AppointmentID
                    WHERE ti.PatientID = @PatientId
                      AND ti.Status IN ('draft', 'proposed', 'accepted', 'in_progress')
                    ORDER BY 
                        CASE ti.Status 
                            WHEN 'in_progress' THEN 1 
                            WHEN 'accepted' THEN 2 
                            WHEN 'proposed' THEN 3 
                            WHEN 'draft' THEN 4 
                            ELSE 5 
                        END, ti.CreatedAt DESC";
            }

            var items = (await conn.QueryAsync<TreatmentItem>(sql, new { PatientId = patientId })).ToList();
            if (!items.Any()) return items;

            var itemIds = items.Select(i => i.TreatmentItemID).ToList();

            // Fetch allocations for all items
            string allocSql = @"
                SELECT 
                    pa.id AS AllocationID,
                    pa.payment_id AS PaymentID,
                    p.PaymentReceiptNo AS ReceiptNumber,
                    pa.amount AS Amount,
                    p.PaymentMethod,
                    p.PaymentDate,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Front Desk') AS ReceivedByName,
                    p.AppointmentID,
                    p.VisitID,
                    p.Notes,
                    pa.treatment_item_id AS TreatmentItemID
                FROM [dentist].[payment_allocations] pa
                JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                LEFT JOIN [dentist].[Doctors] doc ON p.ReceivedBy = doc.DoctorID
                WHERE p.Status = 'active' AND pa.treatment_item_id IN @ItemIds
                ORDER BY p.PaymentDate ASC";

            var allAllocations = (await conn.QueryAsync<dynamic>(allocSql, new { ItemIds = itemIds })).ToList();
            var allocationsByItem = allAllocations.GroupBy(a => (long)a.TreatmentItemID).ToDictionary(g => g.Key, g => g.ToList());

            // Fetch appointments linked to each item
            string apptSql = @"
                SELECT 
                    tia.id AS ID,
                    tia.treatment_item_id AS TreatmentItemID,
                    tia.appointment_id AS AppointmentID,
                    tia.visit_id AS VisitID,
                    tia.role AS Role,
                    tia.notes AS Notes,
                    appt.PreferredDate AS AppointmentDate,
                    COALESCE(doc.FirstName + ' ' + doc.LastName, 'Attending Dentist') AS DoctorName
                FROM [dentist].[treatment_item_appointments] tia
                LEFT JOIN [dentist].[Appointments] appt ON tia.appointment_id = appt.AppointmentID
                LEFT JOIN [dentist].[Doctors] doc ON appt.DoctorID = doc.DoctorID
                WHERE tia.treatment_item_id IN @ItemIds
                ORDER BY tia.created_at ASC";

            var allAppts = (await conn.QueryAsync<TreatmentItemAppointmentDto>(apptSql, new { ItemIds = itemIds })).ToList();
            var apptsByItem = allAppts.GroupBy(a => a.TreatmentItemID).ToDictionary(g => g.Key, g => g.ToList());

            // Compute financial aggregates dynamically
            foreach (var item in items)
            {
                int price = item.FinalPrice ?? (int)item.Fee;
                item.FinalPrice = price;

                if (allocationsByItem.TryGetValue(item.TreatmentItemID, out var allocs))
                {
                    item.Allocations = allocs.Select(a => new PaymentAllocationDetailDto
                    {
                        AllocationID = a.AllocationID,
                        PaymentID = a.PaymentID,
                        ReceiptNumber = a.ReceiptNumber ?? "",
                        Amount = a.Amount,
                        PaymentMethod = a.PaymentMethod ?? "cash",
                        PaymentDate = a.PaymentDate,
                        ReceivedByName = a.ReceivedByName,
                        AppointmentID = a.AppointmentID,
                        VisitID = a.VisitID,
                        Notes = a.Notes
                    }).ToList();

                    item.Paid = item.Allocations.Sum(a => a.Amount);
                }
                else
                {
                    item.Paid = 0;
                }

                item.Balance = Math.Max(0, price - item.Paid);

                if (item.Paid == 0) item.PaymentStatus = "unpaid";
                else if (item.Paid < price) item.PaymentStatus = "partial";
                else if (item.Paid == price) item.PaymentStatus = "paid";
                else item.PaymentStatus = "overpaid";

                if (apptsByItem.TryGetValue(item.TreatmentItemID, out var linkedAppts))
                {
                    item.Appointments = linkedAppts;
                }
            }

            return items;
        }

        public async Task<TreatmentItem?> GetTreatmentItemByIdAsync(long itemId)
        {
            using var conn = CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<TreatmentItem>(
                "SELECT * FROM [dentist].[TreatmentItems] WHERE TreatmentItemID = @Id", new { Id = itemId });
            if (item == null) return null;

            int paid = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(pa.amount), 0)
                FROM [dentist].[payment_allocations] pa
                JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                WHERE pa.treatment_item_id = @Id AND p.Status = 'active'", new { Id = itemId });

            int price = item.FinalPrice ?? (int)item.Fee;
            item.FinalPrice = price;
            item.Paid = paid;
            item.Balance = Math.Max(0, price - paid);
            item.PaymentStatus = paid == 0 ? "unpaid" : (paid < price ? "partial" : (paid == price ? "paid" : "overpaid"));
            return item;
        }

        // =========================================================================
        // 3. STATUS LIFECYCLE & ROLE ENFORCEMENT
        // =========================================================================
        public async Task<bool> UpdateTreatmentItemStatusAsync(long itemId, UpdateTreatmentItemRequest request)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var current = await conn.QueryFirstOrDefaultAsync<TreatmentItem>(
                "SELECT * FROM [dentist].[TreatmentItems] WHERE TreatmentItemID = @Id", new { Id = itemId });
            if (current == null) return false;

            string oldStatus = current.Status;
            string newStatus = request.Status?.ToLowerInvariant() ?? oldStatus;

            // Role enforcement: Receptionist cannot complete treatment!
            if (string.Equals(newStatus, "completed", StringComparison.OrdinalIgnoreCase) && 
                string.Equals(request.UserRole, "Receptionist", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Receptionists are not permitted to complete clinical treatments. Only Doctors can mark treatments as completed.");
            }

            // Price change permissions: doctor or admin only, with reason
            if (request.FinalPrice.HasValue && request.FinalPrice.Value != current.FinalPrice)
            {
                if (request.FinalPrice.Value <= 0)
                {
                    throw new InvalidOperationException("Procedure fee must be greater than zero. Zero or negative fee is not allowed.");
                }

                if (string.Equals(request.UserRole, "Receptionist", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Receptionists cannot modify treatment prices. Only Doctors or Admins can adjust fees.");
                }

                var editPolicy = await _policyService.CanEditTreatmentAsync(itemId);
                if (!editPolicy.Allowed)
                {
                    await _policyService.LogBlockedAttemptAsync(request.UserId, request.UserRole, current.PatientID, "EDIT_TREATMENT_PRICE", editPolicy.ErrorCode ?? TreatmentPolicyErrorCodes.TreatmentLocked, editPolicy.Message);
                    throw new TreatmentPolicyException(editPolicy);
                }
            }

            DateTime? startedAt = current.StartedAt;
            DateTime? acceptedAt = current.AcceptedAt;
            DateTime? completedAt = current.CompletedAt;
            int? completedBy = current.CompletedBy;

            if (newStatus == "accepted" && acceptedAt == null) acceptedAt = DateTime.UtcNow;
            if (newStatus == "in_progress")
            {
                if (startedAt == null) startedAt = DateTime.UtcNow;

                // Rule 2 & D1: Patient-level single active treatment policy
                var startPolicy = await _policyService.CanStartTreatmentAsync(current.PatientID, itemId);
                if (!startPolicy.Allowed)
                {
                    await _policyService.LogBlockedAttemptAsync(request.UserId, request.UserRole, current.PatientID, "START_TREATMENT", startPolicy.ErrorCode ?? TreatmentPolicyErrorCodes.ActiveTreatmentExists, startPolicy.Message);
                    throw new TreatmentPolicyException(startPolicy);
                }
            }

            if (newStatus == "completed")
            {
                completedAt = DateTime.UtcNow;
                completedBy = request.UserId;
            }

            int finalPrice = request.FinalPrice ?? current.FinalPrice ?? (int)current.Fee;

            string sql = @"
                UPDATE [dentist].[TreatmentItems]
                SET Status = @Status,
                    FinalPrice = @FinalPrice,
                    Fee = @FinalPrice,
                    PriceNote = COALESCE(@PriceNote, PriceNote),
                    ClosedReason = COALESCE(@ClosedReason, ClosedReason),
                    TransitionNotes = COALESCE(@TransitionNotes, TransitionNotes),
                    StartedAt = @StartedAt,
                    AcceptedAt = @AcceptedAt,
                    CompletedAt = @CompletedAt,
                    CompletedBy = @CompletedBy,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE TreatmentItemID = @Id";

            int rows = await conn.ExecuteAsync(sql, new
            {
                Id = itemId,
                Status = newStatus,
                FinalPrice = finalPrice,
                request.PriceNote,
                request.ClosedReason,
                request.TransitionNotes,
                StartedAt = startedAt,
                AcceptedAt = acceptedAt,
                CompletedAt = completedAt,
                CompletedBy = completedBy
            });

            // If procedure is cancelled or declined, automatically release its allocations back to unallocated patient credit pool
            if (string.Equals(newStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newStatus, "declined", StringComparison.OrdinalIgnoreCase))
            {
                await conn.ExecuteAsync(@"
                    DELETE FROM [dentist].[payment_allocations]
                    WHERE treatment_item_id = @Id", new { Id = itemId });
            }

            // Write Audit Log
            try
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, OldValue, NewValue, UserID, UserRole, CreatedAt)
                    VALUES ('TreatmentItem', @Id, 'status_transition', @OldStatus, @NewStatus, @UserId, @UserRole, SYSUTCDATETIME())",
                    new 
                    { 
                        Id = itemId, 
                        OldStatus = oldStatus, 
                        NewStatus = newStatus, 
                        UserId = request.UserId > 0 ? request.UserId : 1, 
                        UserRole = !string.IsNullOrWhiteSpace(request.UserRole) ? request.UserRole : "Doctor" 
                    });
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "AuditLog writing failed during status transition for TreatmentItem #{ItemId}", itemId);
            }

            return rows > 0;
        }

        // =========================================================================
        // 4. MULTI-VISIT LINKAGE: LINK APPOINTMENT TO TREATMENT
        // =========================================================================
        public async Task<bool> LinkAppointmentAsync(long itemId, LinkAppointmentRequest request)
        {
            using var conn = CreateConnection();
            string role = string.Equals(request.Role, "performed", StringComparison.OrdinalIgnoreCase) ? "performed" : "planned";

            string sql = @"
                INSERT INTO [dentist].[treatment_item_appointments] 
                (treatment_item_id, appointment_id, visit_id, role, notes, created_at)
                VALUES (@ItemId, @ApptId, @VisitId, @Role, @Notes, SYSUTCDATETIME())";

            int rows = await conn.ExecuteAsync(sql, new
            {
                ItemId = itemId,
                ApptId = request.AppointmentId,
                VisitId = request.VisitId,
                Role = role,
                request.Notes
            });

            // If performed, transition treatment item to in_progress if currently proposed or accepted
            if (role == "performed")
            {
                await conn.ExecuteAsync(@"
                    UPDATE [dentist].[TreatmentItems]
                    SET Status = 'in_progress', StartedAt = COALESCE(StartedAt, SYSUTCDATETIME()), UpdatedAt = SYSUTCDATETIME()
                    WHERE TreatmentItemID = @ItemId AND Status IN ('proposed', 'accepted')",
                    new { ItemId = itemId });
            }

            return rows > 0;
        }

        // =========================================================================
        // 5. FLEXIBLE PAYMENTS & ALLOCATIONS (Integer money, ACID Transactional)
        // =========================================================================
        public async Task<(bool Success, string Message, string ReceiptNumber)> RecordFlexiblePaymentAsync(RecordFlexiblePaymentRequest request)
        {
            if (request.Amount <= 0)
            {
                return (false, "Payment amount must be greater than zero.", "");
            }

            if (request.Amount > 20000000)
            {
                return (false, "Payment amount exceeds the maximum permissible transaction limit (Rs 20,000,000).", "");
            }

            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // Validate allocation amounts
                long totalAllocated = request.Allocations?.Sum(a => a.Amount) ?? 0;
                if (totalAllocated > request.Amount)
                {
                    tx.Rollback();
                    return (false, $"Sum of allocations (Rs {totalAllocated:N0}) cannot exceed payment amount (Rs {request.Amount:N0}).", "");
                }

                // Generate sequential unique receipt number
                int receiptCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dentist].[Payments] WITH (UPDLOCK, HOLDLOCK)", transaction: tx);
                string receiptNo = $"REC-{DateTime.UtcNow:yyyyMMdd}-{(receiptCount + 1):D5}";

                // Insert into Payments table
                string paymentSql = @"
                    INSERT INTO [dentist].[Payments] (
                        PaymentReceiptNo, PatientID, Amount, PaymentMethod, PaymentStatus, Status,
                        PaymentDate, ReceivedAt, ReceivedBy, ReceivedByDoctorID, AppointmentID, VisitID,
                        Notes, Kind
                    )
                    OUTPUT INSERTED.PaymentID
                    VALUES (
                        @ReceiptNo, @PatientId, @Amount, @Method, 'Completed', 'active',
                        SYSUTCDATETIME(), SYSUTCDATETIME(), @ReceivedBy, @ReceivedBy, @AppointmentId, @VisitId,
                        @Notes, 'unified'
                    )";

                long paymentId = await conn.QuerySingleAsync<long>(paymentSql, new
                {
                    ReceiptNo = receiptNo,
                    request.PatientId,
                    Amount = (decimal)request.Amount,
                    Method = request.Method ?? "cash",
                    request.ReceivedBy,
                    request.AppointmentId,
                    request.VisitId,
                    Notes = request.Notes ?? "Flexible payment settlement"
                }, tx);

                // Insert allocations
                if (request.Allocations != null && request.Allocations.Any())
                {
                    foreach (var alloc in request.Allocations.Where(a => a.Amount > 0))
                    {
                        // Check item balance
                        var item = await conn.QueryFirstOrDefaultAsync<TreatmentItem>(
                            "SELECT * FROM [dentist].[TreatmentItems] WHERE TreatmentItemID = @Id",
                            new { Id = alloc.TreatmentItemId }, tx);

                        if (item == null) continue;

                        int price = item.FinalPrice ?? (int)item.Fee;
                        int alreadyPaid = await conn.ExecuteScalarAsync<int>(@"
                            SELECT COALESCE(SUM(pa.amount), 0)
                            FROM [dentist].[payment_allocations] pa
                            JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                            WHERE pa.treatment_item_id = @Id AND p.Status = 'active'",
                            new { Id = alloc.TreatmentItemId }, tx);

                        int remainingBalance = Math.Max(0, price - alreadyPaid);

                        // FINANCIAL INTEGRITY RULE: Payment cannot exceed procedure balance due
                        if (alloc.Amount > remainingBalance)
                        {
                            tx.Rollback();
                            return (false, $"Amount (Rs {alloc.Amount:N0}) cannot be greater than balance due (Rs {remainingBalance:N0}) for {item.ProcedureName ?? item.Name}.", "");
                        }

                        int allocAmount = (int)alloc.Amount;

                        if (allocAmount > 0)
                        {
                            await conn.ExecuteAsync(@"
                                INSERT INTO [dentist].[payment_allocations]
                                (payment_id, treatment_item_id, amount, created_by, created_at)
                                VALUES (@PaymentId, @ItemId, @Amount, @CreatedBy, SYSUTCDATETIME())",
                                new { PaymentId = paymentId, ItemId = alloc.TreatmentItemId, Amount = allocAmount, CreatedBy = request.ReceivedBy }, tx);

                            // AUTO-ACCEPT: Allocating money to a proposed item auto-accepts it!
                            if (string.Equals(item.Status, "proposed", StringComparison.OrdinalIgnoreCase))
                            {
                                await conn.ExecuteAsync(@"
                                    UPDATE [dentist].[TreatmentItems]
                                    SET Status = 'accepted', AcceptedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
                                    WHERE TreatmentItemID = @ItemId",
                                    new { ItemId = alloc.TreatmentItemId }, tx);

                                await conn.ExecuteAsync(@"
                                    INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, OldValue, NewValue, UserID, UserRole, CreatedAt)
                                    VALUES ('TreatmentItem', @ItemId, 'auto_accept_on_payment', 'proposed', 'accepted', @UserId, 'Receptionist', SYSUTCDATETIME())",
                                    new { ItemId = alloc.TreatmentItemId, UserId = request.ReceivedBy }, tx);
                            }
                        }
                    }
                }

                // Audit log for payment
                await conn.ExecuteAsync(@"
                    INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, NewValue, UserID, UserRole, CreatedAt)
                    VALUES ('Payment', @PaymentId, 'payment_received', @AmountStr, @UserId, 'Receptionist', SYSUTCDATETIME())",
                    new { PaymentId = paymentId, AmountStr = $"Amount: {request.Amount}, Receipt: {receiptNo}", UserId = request.ReceivedBy }, tx);

                tx.Commit();
                return (true, $"Payment of Rs {request.Amount:N0} recorded successfully (Receipt #{receiptNo}).", receiptNo);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Error recording flexible payment for Patient #{PatientId}", request.PatientId);
                return (false, "An error occurred while saving the payment: " + ex.Message, "");
            }
        }

        // =========================================================================
        // 6. ALLOCATE ADVANCE CREDIT TO TREATMENTS
        // =========================================================================
        public async Task<(bool Success, string Message)> AllocateCreditAsync(AllocateCreditRequest request)
        {
            if (request.Allocations == null || !request.Allocations.Any())
            {
                return (false, "No allocations specified.");
            }

            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // Calculate total unallocated credit
                int totalPaid = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COALESCE(SUM(CAST(Amount AS INT)), 0)
                    FROM [dentist].[Payments]
                    WHERE PatientID = @PatientId AND Status = 'active'",
                    new { request.PatientId }, tx);

                int totalAllocated = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COALESCE(SUM(pa.amount), 0)
                    FROM [dentist].[payment_allocations] pa
                    JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                    WHERE p.PatientID = @PatientId AND p.Status = 'active'",
                    new { request.PatientId }, tx);

                int availableCredit = Math.Max(0, totalPaid - totalAllocated);
                int requestSum = (int)request.Allocations.Sum(a => a.Amount);

                if (requestSum > availableCredit)
                {
                    tx.Rollback();
                    return (false, $"Requested allocation (Rs {requestSum:N0}) exceeds available advance credit (Rs {availableCredit:N0}).");
                }

                // Find payment IDs with unallocated balance
                var payments = (await conn.QueryAsync<dynamic>(@"
                    SELECT 
                        p.PaymentID, 
                        CAST(p.Amount AS INT) AS TotalAmount,
                        COALESCE((SELECT SUM(amount) FROM [dentist].[payment_allocations] WHERE payment_id = p.PaymentID), 0) AS AllocatedAmount
                    FROM [dentist].[Payments] p
                    WHERE p.PatientID = @PatientId AND p.Status = 'active'
                    ORDER BY p.PaymentDate ASC",
                    new { request.PatientId }, tx)).ToList();

                foreach (var alloc in request.Allocations.Where(a => a.Amount > 0))
                {
                    int needed = (int)alloc.Amount;

                    foreach (var pay in payments)
                    {
                        if (needed <= 0) break;

                        int unallocOnPay = (int)pay.TotalAmount - (int)pay.AllocatedAmount;
                        if (unallocOnPay <= 0) continue;

                        int take = Math.Min(needed, unallocOnPay);
                        await conn.ExecuteAsync(@"
                            INSERT INTO [dentist].[payment_allocations]
                            (payment_id, treatment_item_id, amount, created_by, created_at)
                            VALUES (@PaymentId, @ItemId, @Amount, @CreatedBy, SYSUTCDATETIME())",
                            new { PaymentId = (long)pay.PaymentID, ItemId = alloc.TreatmentItemId, Amount = take, CreatedBy = request.UserId }, tx);

                        pay.AllocatedAmount = (int)pay.AllocatedAmount + take;
                        needed -= take;
                    }

                    // Auto-accept if proposed
                    await conn.ExecuteAsync(@"
                        UPDATE [dentist].[TreatmentItems]
                        SET Status = 'accepted', AcceptedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
                        WHERE TreatmentItemID = @ItemId AND Status = 'proposed'",
                        new { ItemId = alloc.TreatmentItemId }, tx);
                }

                tx.Commit();
                return (true, $"Allocated Rs {requestSum:N0} of credit successfully.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Error allocating credit for Patient #{PatientId}", request.PatientId);
                return (false, "Failed to allocate credit: " + ex.Message);
            }
        }

        // =========================================================================
        // 7. VOID PAYMENT (Clinicians & Admins, mandatory reason, reverses allocations)
        // =========================================================================
        public async Task<(bool Success, string Message)> VoidPaymentAsync(VoidPaymentRequest request)
        {
            if (!string.Equals(request.UserRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.UserRole, "Doctor", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.UserRole, "Dentist", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Permission denied: Only clinicians or administrators can void or refund payments.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return (false, "A valid reason is required to void a payment.");
            }

            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var payment = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM [dentist].[Payments] WHERE PaymentID = @Id", new { Id = request.PaymentId }, tx);

                if (payment == null)
                {
                    tx.Rollback();
                    return (false, "Payment record not found.");
                }

                if (string.Equals((string)payment.Status, "voided", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((string)payment.PaymentStatus, "Voided", StringComparison.OrdinalIgnoreCase))
                {
                    tx.Rollback();
                    return (false, "This payment is already voided.");
                }

                // Mark payment as voided
                await conn.ExecuteAsync(@"
                    UPDATE [dentist].[Payments]
                    SET Status = 'voided',
                        PaymentStatus = 'Voided',
                        VoidReason = @Reason,
                        VoidedBy = @UserId,
                        VoidedAt = SYSUTCDATETIME()
                    WHERE PaymentID = @Id",
                    new { Id = request.PaymentId, request.Reason, request.UserId }, tx);

                // Delete allocations for this payment (reverses allocation)
                await conn.ExecuteAsync(
                    "DELETE FROM [dentist].[payment_allocations] WHERE payment_id = @Id",
                    new { Id = request.PaymentId }, tx);

                // Audit log
                await conn.ExecuteAsync(@"
                    INSERT INTO [dentist].[AuditLogs] (Entity, EntityID, Action, NewValue, UserID, UserRole, CreatedAt)
                    VALUES ('Payment', @Id, 'void_payment', @Reason, @UserId, @Role, SYSUTCDATETIME())",
                    new { Id = request.PaymentId, request.Reason, request.UserId, Role = request.UserRole ?? "Doctor" }, tx);

                tx.Commit();
                return (true, $"Payment #{payment.PaymentReceiptNo} voided successfully. Allocations reversed.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _logger.LogError(ex, "Error voiding payment #{PaymentId}", request.PaymentId);
                return (false, "Failed to void payment: " + ex.Message);
            }
        }

        // =========================================================================
        // 7B. PATIENT PAYMENTS HISTORY LIST
        // =========================================================================
        public async Task<List<PaymentDetailDto>> GetPatientPaymentsAsync(int patientId)
        {
            using var conn = CreateConnection();
            string sql = @"
                SELECT 
                    p.PaymentID,
                    p.PaymentReceiptNo AS ReceiptNo,
                    p.PatientID,
                    CAST(p.Amount AS INT) AS Amount,
                    p.PaymentMethod AS Method,
                    p.PaymentDate,
                    COALESCE(p.Status, 'active') AS Status,
                    p.PaymentStatus,
                    p.VoidReason,
                    p.VoidedAt,
                    COALESCE(u.FirstName + ' ' + u.LastName, 'Front Desk') AS ReceivedByName,
                    COALESCE((SELECT SUM(pa.amount) FROM [dentist].[payment_allocations] pa WHERE pa.payment_id = p.PaymentID), 0) AS AllocatedAmount
                FROM [dentist].[Payments] p
                LEFT JOIN [dentist].[Doctors] u ON p.ReceivedBy = u.DoctorID
                WHERE p.PatientID = @PatientId
                ORDER BY p.PaymentDate DESC";

            var rows = (await conn.QueryAsync<dynamic>(sql, new { PatientId = patientId })).ToList();
            return rows.Select(r => new PaymentDetailDto
            {
                PaymentID = (long)r.PaymentID,
                ReceiptNo = (string)(r.ReceiptNo ?? ""),
                PatientID = (int)r.PatientID,
                Amount = (int)r.Amount,
                Method = (string)(r.Method ?? "cash"),
                PaymentDate = (DateTime)r.PaymentDate,
                Status = (string)(r.Status ?? "active"),
                PaymentStatus = (string)(r.PaymentStatus ?? ""),
                VoidReason = (string?)r.VoidReason,
                VoidedAt = (DateTime?)r.VoidedAt,
                ReceivedByName = (string)(r.ReceivedByName ?? "Front Desk"),
                AllocatedAmount = (int)r.AllocatedAmount,
                UnallocatedAmount = Math.Max(0, (int)r.Amount - (int)r.AllocatedAmount)
            }).ToList();
        }

        // =========================================================================
        // 8. PATIENT ACCOUNT FINANCIAL SUMMARY
        // =========================================================================
        public async Task<AccountSummaryDto> GetAccountSummaryAsync(int patientId)
        {
            using var conn = CreateConnection();

            // Completed work value
            int completedWorkValue = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(COALESCE(FinalPrice, CAST(Fee AS INT))), 0)
                FROM [dentist].[TreatmentItems]
                WHERE PatientID = @PatientId AND Status = 'completed'",
                new { PatientId = patientId });

            // Planned value (active chairside work not yet completed)
            int plannedValue = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(COALESCE(FinalPrice, CAST(Fee AS INT))), 0)
                FROM [dentist].[TreatmentItems]
                WHERE PatientID = @PatientId AND Status IN ('proposed', 'accepted', 'in_progress')",
                new { PatientId = patientId });

            // Total paid (active non-voided payments)
            int totalPaid = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(CAST(Amount AS INT)), 0)
                FROM [dentist].[Payments]
                WHERE PatientID = @PatientId 
                  AND LOWER(ISNULL(Status, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')
                  AND LOWER(ISNULL(PaymentStatus, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')",
                new { PatientId = patientId });

            // Allocations to completed items
            int allocatedToCompleted = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(pa.amount), 0)
                FROM [dentist].[payment_allocations] pa
                JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                JOIN [dentist].[TreatmentItems] ti ON pa.treatment_item_id = ti.TreatmentItemID
                WHERE p.PatientID = @PatientId 
                  AND LOWER(ISNULL(p.Status, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')
                  AND LOWER(ISNULL(p.PaymentStatus, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')
                  AND ti.Status = 'completed'",
                new { PatientId = patientId });

            // Allocations to active non-completed items (strictly excludes cancelled, declined, or deleted items)
            int allocatedToIncomplete = await conn.ExecuteScalarAsync<int>(@"
                SELECT COALESCE(SUM(pa.amount), 0)
                FROM [dentist].[payment_allocations] pa
                JOIN [dentist].[Payments] p ON pa.payment_id = p.PaymentID
                JOIN [dentist].[TreatmentItems] ti ON pa.treatment_item_id = ti.TreatmentItemID
                WHERE p.PatientID = @PatientId 
                  AND LOWER(ISNULL(p.Status, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')
                  AND LOWER(ISNULL(p.PaymentStatus, '')) NOT IN ('voided', 'deleted', 'cancelled', 'refunded')
                  AND ti.Status IN ('proposed', 'accepted', 'in_progress')",
                new { PatientId = patientId });

            int totalAllocated = allocatedToCompleted + allocatedToIncomplete;
            int unallocatedCredit = Math.Max(0, totalPaid - totalAllocated);

            int outstandingNow = Math.Max(0, completedWorkValue - allocatedToCompleted);
            int advanceHeld = allocatedToIncomplete + unallocatedCredit;

            return new AccountSummaryDto
            {
                CompletedWorkValue = completedWorkValue,
                TotalPaid = totalPaid,
                OutstandingNow = outstandingNow,
                AdvanceHeld = advanceHeld,
                PlannedValue = plannedValue,
                UnallocatedCredit = unallocatedCredit,
                Currency = "PKR"
            };
        }

        // =========================================================================
        // 9. ACTIVE PLAN HEADER CHIPS (Derived from accepted/in_progress items)
        // =========================================================================
        public async Task<List<ActivePlanChipDto>> GetActivePlansAsync(int patientId)
        {
            using var conn = CreateConnection();
            string sql = @"
                SELECT 
                    Category, 
                    ProcedureName AS Name, 
                    Status, 
                    PlannedVisits,
                    ToothNumber,
                    (SELECT COUNT(*) FROM [dentist].[treatment_item_appointments] WHERE treatment_item_id = ti.TreatmentItemID AND role = 'performed') AS PerformedVisits
                FROM [dentist].[TreatmentItems] ti
                WHERE PatientID = @PatientId AND Status IN ('accepted', 'in_progress')
                ORDER BY CreatedAt ASC";

            var items = (await conn.QueryAsync<dynamic>(sql, new { PatientId = patientId })).ToList();
            if (!items.Any()) return new List<ActivePlanChipDto>();

            var summary = await GetAccountSummaryAsync(patientId);

            var groups = items.GroupBy(i => (string)(i.Category ?? "general")).ToList();
            var chips = new List<ActivePlanChipDto>();

            foreach (var g in groups)
            {
                var first = g.First();
                string cat = g.Key;
                string label = char.ToUpper(cat[0]) + cat.Substring(1);

                if (cat.Equals("whitening", StringComparison.OrdinalIgnoreCase)) label = "Teeth Whitening";
                else if (cat.Equals("ortho", StringComparison.OrdinalIgnoreCase)) label = "Braces (Ortho)";
                else if (cat.Equals("endo", StringComparison.OrdinalIgnoreCase)) label = "Root Canal (RCT)";
                else if (cat.Equals("restorative", StringComparison.OrdinalIgnoreCase)) label = "Restorative Fillings";

                int planned = first.PlannedVisits ?? 0;
                int done = (int)first.PerformedVisits;
                string prog = planned > 0 ? $"Visit {done}/{planned}" : (g.Count() > 1 ? $"{g.Count()} procedures" : (first.ToothNumber > 0 ? $"Tooth #{first.ToothNumber}" : "Active"));

                chips.Add(new ActivePlanChipDto
                {
                    Category = cat,
                    Name = label,
                    ProgressText = prog,
                    Count = g.Count(),
                    BalanceDue = summary.OutstandingNow > 0 ? summary.OutstandingNow : null,
                    AdvanceCredit = summary.AdvanceHeld > 0 ? summary.AdvanceHeld : null
                });
            }

            return chips;
        }

        // =========================================================================
        // 10. UNPLANNED FINDINGS FROM TEETHSTATE (No open TreatmentItem yet)
        // =========================================================================
        public async Task<List<UnplannedFindingDto>> GetUnplannedFindingsAsync(int patientId)
        {
            using var conn = CreateConnection();

            // Find teeth in TeethState that are non-healthy and have NO open item in TreatmentItems
            string sql = @"
                SELECT ts.ToothNumber, ts.ToothKey, ts.ConditionStatus, ts.ConditionColor, ts.Comments
                FROM [dentist].[TeethState] ts
                WHERE ts.PatientID = @PatientId
                  AND ts.ConditionStatus IS NOT NULL
                  AND ts.ConditionStatus <> ''
                  AND LOWER(ts.ConditionStatus) NOT IN ('healthy', 'sound')
                  AND NOT EXISTS (
                      SELECT 1 FROM [dentist].[TreatmentItems] ti
                      WHERE ti.PatientID = ts.PatientID 
                        AND ti.ToothNumber = ts.ToothNumber
                        AND ti.Status IN ('draft', 'proposed', 'accepted', 'in_progress')
                  )";

            var rows = (await conn.QueryAsync<dynamic>(sql, new { PatientId = patientId })).ToList();
            var list = new List<UnplannedFindingDto>();

            foreach (var r in rows)
            {
                string status = (string)(r.ConditionStatus ?? "");
                string code = "D2391";
                string name = "Composite Resin Restoration";
                int price = 10000;

                if (status.Contains("decay", StringComparison.OrdinalIgnoreCase) || status.Contains("damaged", StringComparison.OrdinalIgnoreCase))
                {
                    code = "D2391";
                    name = "Composite Restoration (Filling)";
                    price = 10000;
                }
                else if (status.Contains("missing", StringComparison.OrdinalIgnoreCase) || status.Contains("extracted", StringComparison.OrdinalIgnoreCase))
                {
                    code = "D7140";
                    name = "Simple Tooth Extraction";
                    price = 10000;
                }
                else if (status.Contains("root canal", StringComparison.OrdinalIgnoreCase) || status.Contains("rct", StringComparison.OrdinalIgnoreCase))
                {
                    code = "D3330";
                    name = "Root Canal Therapy (RCT)";
                    price = 25000;
                }
                else if (status.Contains("crown", StringComparison.OrdinalIgnoreCase))
                {
                    code = "D2740";
                    name = "Full Ceramic Crown";
                    price = 35000;
                }

                list.Add(new UnplannedFindingDto
                {
                    ToothNumber = (int)r.ToothNumber,
                    ToothKey = (string)(r.ToothKey ?? r.ToothNumber.ToString()),
                    ConditionStatus = status,
                    ConditionColor = (string)(r.ConditionColor ?? "#EF4444"),
                    Comments = (string)r.Comments,
                    SuggestedProcedureCode = code,
                    SuggestedProcedureName = name,
                    EstimatedPrice = price
                });
            }

            return list;
        }

        // =========================================================================
        // 11. DIRECTORY MODALITIES AUTO-SYNC (Idempotent backfill from Patients table)
        // =========================================================================
        public async Task<int> SyncDirectoryModalitiesAsync(int patientId, int doctorId)
        {
            using var conn = CreateConnection();
            var patient = await conn.QueryFirstOrDefaultAsync<Patient>(
                "SELECT * FROM [dentist].[Patients] WHERE PatientID = @Id", new { Id = patientId });

            if (patient == null || string.IsNullOrWhiteSpace(patient.CurrentTreatmentPlan)) return 0;

            string plan = patient.CurrentTreatmentPlan.ToLowerInvariant();
            int syncedCount = 0;

            if (plan.Contains("whiten"))
            {
                await CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Name = "Teeth Whitening (Cosmetic Laser)",
                    Category = "whitening",
                    ProcedureCode = "D9972",
                    ToothNo = 0,
                    Source = "directory",
                    SourceRef = patient.TreatmentStage,
                    EstimatedPrice = 25000,
                    FinalPrice = 25000,
                    PlannedVisits = 3,
                    Status = "accepted"
                });
                syncedCount++;
            }

            if (plan.Contains("brace") || plan.Contains("ortho"))
            {
                await CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Name = "Braces (Orthodontics Full Arch)",
                    Category = "ortho",
                    ProcedureCode = "D8080",
                    ToothNo = 0,
                    Source = "directory",
                    SourceRef = patient.TreatmentStage,
                    EstimatedPrice = 120000,
                    FinalPrice = 120000,
                    PlannedVisits = 12,
                    Status = "accepted"
                });
                syncedCount++;
            }

            if (plan.Contains("cavity") || plan.Contains("restorative"))
            {
                int toothNo = 0;
                string stage = patient.TreatmentStage ?? "";
                var match = System.Text.RegularExpressions.Regex.Match(stage, @"#(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedTooth))
                {
                    toothNo = parsedTooth;
                }

                await CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Name = toothNo > 0 ? $"Composite Resin Restoration (Tooth #{toothNo})" : "Composite Resin Restoration",
                    Category = "restorative",
                    ProcedureCode = "D2391",
                    ToothNo = toothNo,
                    Source = "directory",
                    SourceRef = stage,
                    EstimatedPrice = 12000,
                    FinalPrice = 12000,
                    PlannedVisits = 1,
                    Status = "accepted"
                });
                syncedCount++;
            }

            return syncedCount;
        }

        // =========================================================================
        // 12. GET PROCEDURES CATALOG
        // =========================================================================
        public async Task<List<TreatmentCatalogItem>> GetCatalogAsync()
        {
            using var conn = CreateConnection();
            var items = await conn.QueryAsync<TreatmentCatalogItem>(
                "SELECT * FROM [dentist].[treatment_catalog] WHERE is_active = 1 ORDER BY category, default_price");
            return items.ToList();
        }

        // =========================================================================
        // 13. AUTO-DIAGNOSTIC FEES (CONSULTATION & DEVICE X-RAYS WITH DOCTOR RATES)
        // =========================================================================
        public async Task<AutoDiagnosticFeeResult> EnsureDiagnosticFeeAsync(int patientId, AutoDiagnosticFeeRequest request)
        {
            using var conn = CreateConnection();
            int docId = request.DoctorId ?? 1;

            if (string.Equals(request.FeeType, "consultation", StringComparison.OrdinalIgnoreCase))
            {
                // 1. Idempotency Check: Does patient already have Consultation in TreatmentItems?
                string checkSql = @"
                    SELECT TOP 1 TreatmentItemID FROM [dentist].[TreatmentItems]
                    WHERE PatientID = @PatientId 
                      AND (Category = 'consultation' OR ProcedureCode IN ('D0150', 'D0120'))";
                
                var existing = await conn.QueryFirstOrDefaultAsync<long?>(checkSql, new { PatientId = patientId });
                if (existing.HasValue && existing.Value > 0)
                {
                    return new AutoDiagnosticFeeResult 
                    { 
                        Added = false, 
                        ItemId = existing.Value,
                        Message = "Consultation fee already exists in treatment plan." 
                    };
                }

                // 2. Fetch DOCTOR'S custom configured Consultation Fee from DoctorFeeSchedules
                string feeSql = @"
                    SELECT TOP 1 StandardFee FROM [dentist].[DoctorFeeSchedules]
                    WHERE DoctorID = @DoctorId AND (ProcedureCode = 'D0150' OR Category = 'consultation') AND IsActive = 1";
                
                int? feeNullable = await conn.QueryFirstOrDefaultAsync<int?>(feeSql, new { DoctorId = docId });
                int fee = feeNullable.GetValueOrDefault(0);
                if (fee <= 0)
                {
                    // Fallback to clinic treatment_catalog
                    var catFee = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 default_price FROM [dentist].[treatment_catalog] WHERE procedure_code = 'D0150'");
                    fee = catFee.GetValueOrDefault(5000);
                }
                if (fee <= 0) fee = 5000;

                // 3. Create Treatment Item
                var item = await CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                {
                    PatientId = patientId,
                    DoctorId = docId,
                    Name = "Dental Consultation & Diagnosis",
                    Category = "consultation",
                    ProcedureCode = "D0150",
                    ToothNo = 0,
                    EstimatedPrice = fee,
                    FinalPrice = fee,
                    PlannedVisits = 1,
                    Status = "accepted",
                    Source = "auto_consultation"
                });

                return new AutoDiagnosticFeeResult 
                { 
                    Added = true, 
                    Fee = fee, 
                    Name = "Dental Consultation & Diagnosis", 
                    ProcedureCode = "D0150", 
                    ItemId = item.TreatmentItemID,
                    Message = $"Consultation fee (Rs {fee:N0}) added to treatment plan."
                };
            }
            else if (string.Equals(request.FeeType, "rvg_xray", StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(request.FeeType, "opg_xray", StringComparison.OrdinalIgnoreCase))
            {
                bool isOpg = string.Equals(request.FeeType, "opg_xray", StringComparison.OrdinalIgnoreCase);
                string code = isOpg ? "D0330" : "D0220";
                int toothNo = request.ToothNumber ?? 0;
                string defName = isOpg 
                    ? "Full Mouth Panoramic OPG X-Ray" 
                    : (toothNo > 0 ? $"Digital RVG Diagnostic X-Ray (Tooth #{toothNo})" : "Digital RVG Diagnostic X-Ray");

                // Fetch DOCTOR'S custom configured X-Ray Fee from DoctorFeeSchedules
                string feeSql = @"
                    SELECT TOP 1 StandardFee FROM [dentist].[DoctorFeeSchedules]
                    WHERE DoctorID = @DoctorId AND ProcedureCode = @Code AND IsActive = 1";
                
                int? feeNullable = await conn.QueryFirstOrDefaultAsync<int?>(feeSql, new { DoctorId = docId, Code = code });
                int fee = feeNullable.GetValueOrDefault(0);
                if (fee <= 0)
                {
                    var catFee = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 default_price FROM [dentist].[treatment_catalog] WHERE procedure_code = @Code", new { Code = code });
                    fee = catFee.GetValueOrDefault(isOpg ? 8500 : 2500);
                }
                if (fee <= 0) fee = isOpg ? 8500 : 2500;

                var item = await CreateTreatmentItemAsync(new CreateTreatmentItemRequest
                {
                    PatientId = patientId,
                    DoctorId = docId,
                    Name = defName,
                    Category = "xray",
                    ProcedureCode = code,
                    ToothNo = toothNo,
                    EstimatedPrice = fee,
                    FinalPrice = fee,
                    PlannedVisits = 1,
                    Status = "accepted",
                    Source = request.SourceRef ?? "auto_device_xray"
                });

                return new AutoDiagnosticFeeResult 
                { 
                    Added = true, 
                    Fee = fee, 
                    Name = defName, 
                    ProcedureCode = code, 
                    ItemId = item.TreatmentItemID,
                    Message = $"{defName} fee (Rs {fee:N0}) added to treatment plan."
                };
            }

            return new AutoDiagnosticFeeResult { Added = false, Message = $"Unrecognized fee type '{request.FeeType}'." };
        }
    }
}
