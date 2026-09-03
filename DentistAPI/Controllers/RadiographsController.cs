using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;
using System.Collections.Generic;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class RadiographsController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly GeminiDentalNotesService _geminiService;

        public RadiographsController(DentalRepository repository, GeminiDentalNotesService geminiService)
        {
            _repository = repository;
            _geminiService = geminiService;
        }

        [HttpPost("patients/{patientId:int}/radiographs")]
        public async Task<IActionResult> UploadRadiograph(int patientId, [FromForm] IFormFile file, [FromQuery] int? doctorId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            try
            {
                // Read file into byte array
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                byte[] imageBytes = ms.ToArray();

                string mimeType = file.ContentType;
                string filename = file.FileName;

                // Call Gemini Vision to analyze image
                string analysisSummary = "Analysis failed or skipped.";
                try
                {
                    analysisSummary = await _geminiService.AnalyzeRadiographAsync(imageBytes, mimeType);
                }
                catch (Exception ex)
                {
                    analysisSummary = $"AI Radiograph Analysis Error: {ex.Message}";
                }

                // Create Radiograph record
                var radiograph = new Radiograph
                {
                    PatientID = patientId,
                    DoctorID = doctorId,
                    ImageName = filename,
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
                    ImageName = filename,
                    MimeType = mimeType,
                    UploadedAt = DateTime.UtcNow,
                    AnalysisSummary = analysisSummary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                    return NotFound("Radiograph image not found.");
                }

                return File(radiograph.ImageData, radiograph.MimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                    return NotFound("Radiograph not found.");
                }

                await _repository.UpdateRadiographAnalysisAsync(id, req.AnalysisSummary);
                return Ok(new { Message = "Analysis summary updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                    return NotFound("Radiograph image not found.");
                }

                string newAnalysis = await _geminiService.AnalyzeRadiographAsync(radiograph.ImageData, radiograph.MimeType);
                await _repository.UpdateRadiographAnalysisAsync(id, newAnalysis);

                return Ok(new { RadiographID = id, AnalysisSummary = newAnalysis });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class UpdateAnalysisRequest
    {
        public string AnalysisSummary { get; set; } = string.Empty;
    }
}
