using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [EnableCors("AllowAll")]
    public class RadiographsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly GeminiDentalNotesService _geminiService;
        private readonly IFileUploadSecurityService _fileUploadService;
        private readonly ILogger<RadiographsController> _logger;

        public RadiographsController(
            DentalRepository repository, 
            GeminiDentalNotesService geminiService,
            IFileUploadSecurityService fileUploadService,
            ILogger<RadiographsController> logger)
        {
            _repository = repository;
            _geminiService = geminiService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        [HttpPost("patients/{patientId:int}/radiographs")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload-policy")]
        public async Task<IActionResult> UploadRadiograph(int patientId, IFormFile file, [FromQuery] int? doctorId)
        {
            // Security verification: Magic Bytes, Size Limit, and Extension Whitelist
            var validation = await _fileUploadService.ValidateAndExtractAsync(file, FileUploadCategory.Radiograph);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.ErrorMessage });
            }

            try
            {
                byte[] imageBytes = validation.FileBytes;
                string mimeType = validation.CanonicalMimeType;
                string safeFilename = validation.SafeFileName;

                // Call Gemini Vision to analyze image
                string analysisSummary = "Automated visual analysis is currently unavailable. Radiograph saved successfully.";
                try
                {
                    analysisSummary = await _geminiService.AnalyzeRadiographAsync(imageBytes, mimeType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini Radiograph AI analysis failed non-fatally for patient {PatientId}", patientId);
                }

                // Create Radiograph record with sanitized properties
                var radiograph = new Radiograph
                {
                    PatientID = patientId,
                    DoctorID = doctorId,
                    ImageName = safeFilename,
                    MimeType = mimeType,
                    ImageData = imageBytes,
                    AnalysisSummary = analysisSummary
                };

                // Save to Database
                int radiographId = await _repository.AddRadiographAsync(radiograph);
                radiograph.RadiographID = radiographId;

                // Return without raw bytes to avoid sending heavy payloads back to client
                return Ok(new
                {
                    RadiographID = radiographId,
                    PatientID = patientId,
                    DoctorID = doctorId,
                    ImageName = safeFilename,
                    MimeType = mimeType,
                    UploadedAt = DateTime.UtcNow,
                    AnalysisSummary = analysisSummary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload radiograph for PatientId: {PatientId}", patientId);
                return StatusCode(500, new { message = "An error occurred while saving the radiograph. Please try again." });
            }
        }

        [HttpGet("patients/{patientId:int}/radiographs")]
        public async Task<IActionResult> GetRadiographs(int patientId)
        {
            try
            {
                var list = await _repository.GetRadiographsByPatientAsync(patientId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching radiographs for PatientId: {PatientId}", patientId);
                return StatusCode(500, new { message = "An error occurred while retrieving radiographs." });
            }
        }

        [HttpGet("radiographs/{id:int}/image")]
        public async Task<IActionResult> GetRadiographImage(int id)
        {
            try
            {
                var radiograph = await _repository.GetRadiographByIdAsync(id);
                if (radiograph == null || radiograph.ImageData == null || radiograph.ImageData.Length == 0)
                {
                    return NotFound(new { message = "Radiograph image not found." });
                }

                // Prevent MIME sniffing
                Response.Headers.Append("X-Content-Type-Options", "nosniff");
                return File(radiograph.ImageData, radiograph.MimeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading radiograph image with ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while loading the image." });
            }
        }

        [HttpPut("radiographs/{id:int}/analysis")]
        public async Task<IActionResult> UpdateAnalysisSummary(int id, [FromBody] UpdateAnalysisRequest req)
        {
            try
            {
                var radiograph = await _repository.GetRadiographByIdAsync(id);
                if (radiograph == null)
                {
                    return NotFound(new { message = "Radiograph not found." });
                }

                await _repository.UpdateRadiographAnalysisAsync(id, req.AnalysisSummary);
                return Ok(new { message = "Analysis summary updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis summary for Radiograph {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the analysis." });
            }
        }

        [HttpPost("radiographs/{id:int}/reanalyze")]
        public async Task<IActionResult> ReanalyzeRadiograph(int id)
        {
            try
            {
                var radiograph = await _repository.GetRadiographByIdAsync(id);
                if (radiograph == null || radiograph.ImageData == null || radiograph.ImageData.Length == 0)
                {
                    return NotFound(new { message = "Radiograph image not found." });
                }

                string newAnalysis = await _geminiService.AnalyzeRadiographAsync(radiograph.ImageData, radiograph.MimeType);
                await _repository.UpdateRadiographAnalysisAsync(id, newAnalysis);

                return Ok(new { RadiographID = id, AnalysisSummary = newAnalysis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reanalyzing radiograph ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred during radiograph reanalysis." });
            }
        }

        [HttpDelete("radiographs/{id:int}")]
        public async Task<IActionResult> DeleteRadiograph(int id)
        {
            try
            {
                var radiograph = await _repository.GetRadiographByIdAsync(id);
                if (radiograph == null)
                {
                    return NotFound(new { message = "Radiograph not found." });
                }

                bool deleted = await _repository.DeleteRadiographAsync(id);
                if (deleted)
                {
                    return Ok(new { message = "Radiograph deleted successfully.", radiographId = id });
                }

                return StatusCode(500, new { message = "Failed to delete radiograph from database." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting radiograph with ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the radiograph." });
            }
        }

        [HttpDelete("patients/{patientId:int}/radiographs/{id:int}")]
        public async Task<IActionResult> DeletePatientRadiograph(int patientId, int id)
        {
            return await DeleteRadiograph(id);
        }

        [HttpPost("radiographs/{id:int}/delete")]
        public async Task<IActionResult> PostDeleteRadiograph(int id)
        {
            return await DeleteRadiograph(id);
        }

        [HttpPost("patients/{patientId:int}/radiographs/{id:int}/delete")]
        public async Task<IActionResult> PostDeletePatientRadiograph(int patientId, int id)
        {
            return await DeleteRadiograph(id);
        }
    }

    public class UpdateAnalysisRequest
    {
        public string AnalysisSummary { get; set; } = string.Empty;
    }
}
