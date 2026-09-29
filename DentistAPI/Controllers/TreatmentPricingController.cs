using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DentistAPI.Repositories;
using DentistAPI.Models;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/treatment-pricing")]
    public class TreatmentPricingController : ControllerBase
    {
        private readonly DentalRepository _repository;

        public TreatmentPricingController(DentalRepository repository)
        {
            _repository = repository;
        }

        [AllowAnonymous]
        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetDoctorFeeSchedule(int doctorId)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound(new { message = $"Doctor with ID {doctorId} not found." });
            }

            var procedures = (await _repository.GetDoctorFeeScheduleAsync(doctorId)).ToList();

            string currency = procedures.FirstOrDefault()?.Currency ?? (doctor.Region == "PK" ? "PKR" : "NZD");

            return Ok(new
            {
                doctorId = doctor.DoctorID,
                doctorName = $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
                region = doctor.Region,
                currency = currency,
                totalProcedures = procedures.Count,
                procedures = procedures
            });
        }

        [HttpPut("doctor/{doctorId}")]
        public async Task<IActionResult> UpdateDoctorFeeSchedule(int doctorId, [FromBody] UpdateDoctorFeeScheduleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Currency))
            {
                return BadRequest(new { message = "Currency and procedures list are required." });
            }

            var doctor = await _repository.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound(new { message = $"Doctor with ID {doctorId} not found." });
            }

            bool success = await _repository.UpdateDoctorFeeScheduleAsync(doctorId, request.Currency.Trim().ToUpper(), request.Procedures ?? new List<DoctorFeeScheduleItem>());

            var updated = await _repository.GetDoctorFeeScheduleAsync(doctorId);

            return Ok(new
            {
                message = "Treatment fee schedule and currency updated successfully.",
                doctorId = doctor.DoctorID,
                currency = request.Currency.Trim().ToUpper(),
                totalProcedures = updated.Count(),
                procedures = updated
            });
        }

        [HttpPost("doctor/{doctorId}/reset-master")]
        public async Task<IActionResult> ResetToMasterCatalog(int doctorId, [FromQuery] string? currency = null)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound(new { message = $"Doctor with ID {doctorId} not found." });
            }

            await _repository.ResetDoctorFeeScheduleToMasterAsync(doctorId, currency);
            var updated = await _repository.GetDoctorFeeScheduleAsync(doctorId);

            string activeCurr = updated.FirstOrDefault()?.Currency ?? (doctor.Region == "PK" ? "PKR" : "NZD");

            return Ok(new
            {
                message = "Fee schedule successfully reset to full 15-category master catalog.",
                doctorId = doctor.DoctorID,
                currency = activeCurr,
                totalProcedures = updated.Count(),
                procedures = updated
            });
        }

        [AllowAnonymous]
        [HttpGet("currencies")]
        public IActionResult GetSupportedCurrencies()
        {
            var currencies = new[]
            {
                new { code = "NZD", symbol = "$", label = "New Zealand Dollar (NZD $)" },
                new { code = "PKR", symbol = "Rs", label = "Pakistani Rupee (PKR Rs)" },
                new { code = "USD", symbol = "$", label = "US Dollar (USD $)" },
                new { code = "GBP", symbol = "£", label = "British Pound (GBP £)" },
                new { code = "EUR", symbol = "€", label = "Euro (EUR €)" },
                new { code = "AUD", symbol = "$", label = "Australian Dollar (AUD $)" }
            };

            return Ok(currencies);
        }
    }
}
