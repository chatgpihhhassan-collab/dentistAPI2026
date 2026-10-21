using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DentistAPI.Repositories;
using DentistAPI.Services;
using DentistAPI.Models;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(
            DentalRepository repository,
            IAuditLogService auditLogService,
            ILogger<AuditLogsController> logger)
        {
            _repository = repository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RecordAuditLog([FromBody] AuditLogRecordRequest req)
        {
            int userId = req?.DoctorId ?? req?.DoctorID ?? req?.UserId ?? 1;
            string userRole = !string.IsNullOrWhiteSpace(req?.UserRole) ? req.UserRole : "Doctor";
            string action = !string.IsNullOrWhiteSpace(req?.Action) ? req.Action : "CHART_AUDIT_UPDATE";
            string entity = !string.IsNullOrWhiteSpace(req?.Entity) ? req.Entity : "PatientChart";
            long entityId = req?.DoctorId ?? 1;
            string details = !string.IsNullOrWhiteSpace(req?.Details) ? req.Details : (!string.IsNullOrWhiteSpace(req?.Message) ? req.Message : "Chart audit event");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                _auditLogService.LogAccess(userId, userRole, (int)entityId, action, details, ip);
                await _repository.InsertAuditLogAsync(entity, entityId, action, null, details, userId, userRole, ip);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-fatal error logging audit event in AuditLogsController");
            }

            return Ok(new { success = true, message = "Audit log recorded successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] long? entityId, [FromQuery] string? entity, [FromQuery] int limit = 50)
        {
            var logs = await _repository.GetAuditLogsAsync(entityId, entity, limit);
            return Ok(logs);
        }
    }
}
