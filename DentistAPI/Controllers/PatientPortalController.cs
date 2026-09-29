using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DentistAPI.Repositories;
using DentistAPI.Models;
using DentistAPI.Services;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/patient-portal")]
    public class PatientPortalController : ControllerBase
    {
        private readonly DentalRepository _repository;
        private readonly ITokenService _tokenService;

        public PatientPortalController(DentalRepository repository, ITokenService tokenService)
        {
            _repository = repository;
            _tokenService = tokenService;
        }

        private int? GetAuthenticatedPatientId()
        {
            string? authHeader = Request.Headers["Authorization"].ToString();
            string? token = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7).Trim();
            }
            else if (Request.Headers.TryGetValue("X-Patient-Token", out var customToken))
            {
                token = customToken.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(token) || !_tokenService.ValidatePatientToken(token, out var payload) || payload == null)
            {
                return null;
            }

            return payload.PatientId;
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _repository.GetAllDoctorsAsync();
            var list = doctors.Select(d => new
            {
                doctorID = d.DoctorID,
                id = d.DoctorID,
                username = d.Username,
                firstName = d.FirstName,
                lastName = d.LastName,
                fullName = $"Dr. {d.FirstName} {d.LastName}".Trim(),
                region = d.Region,
                title = !string.IsNullOrEmpty(d.Title) ? d.Title : (d.Region == "PK" ? "Consultant Dental Surgeon" : "Dental Surgeon & Specialist"),
                specialization = !string.IsNullOrEmpty(d.Specialization) ? d.Specialization : "General & Restorative Dentistry",
                yearsOfExperience = d.YearsOfExperience > 0 ? d.YearsOfExperience : 8,
                exp = $"{d.YearsOfExperience} yrs exp",
                biography = d.Biography,
                organizationWorkHistory = d.OrganizationWorkHistory,
                education = d.Education,
                certifications = d.Certifications,
                consultationFee = d.ConsultationFee > 0 ? d.ConsultationFee : (d.Region == "PK" ? 2500m : 150m),
                avatar = !string.IsNullOrEmpty(d.ProfileImageUrl) ? d.ProfileImageUrl : 
                    (d.DoctorID == 2
                        ? "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&q=80&w=400"
                        : (d.DoctorID == 4
                            ? "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&q=80&w=400"
                            : "https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&q=80&w=400")),
                languages = !string.IsNullOrEmpty(d.Languages) ? d.Languages : "English, Urdu",
                rating = d.Rating > 0 ? d.Rating : 4.92m,
                reviewCount = d.ReviewCount > 0 ? d.ReviewCount : 38,
                organizationID = d.OrganizationID,
                organizationName = !string.IsNullOrEmpty(d.OrganizationName) ? d.OrganizationName : (d.Region == "PK" ? "Shifa International Hospitals Ltd" : "Dentia Auckland Regional Dental Hospital"),
                organizationLogoUrl = d.OrganizationLogoUrl,
                organizationCity = d.OrganizationCity,
                hospitalDepartment = !string.IsNullOrEmpty(d.HospitalDepartment) ? d.HospitalDepartment : "Department of Oral Surgery & Dentistry"
            });
            return Ok(list);
        }

        [HttpGet("organizations")]
        public async Task<IActionResult> GetOrganizations()
        {
            var orgs = await _repository.GetAllOrganizationsAsync();
            return Ok(orgs);
        }

        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctorDetail(int id)
        {
            var d = await _repository.GetDoctorByIdAsync(id);
            if (d == null || d.IsSuperAdmin || !d.IsActive) return NotFound(new { message = "Doctor profile not found." });

            var detail = new
            {
                doctorID = d.DoctorID,
                id = d.DoctorID,
                username = d.Username,
                firstName = d.FirstName,
                lastName = d.LastName,
                fullName = $"Dr. {d.FirstName} {d.LastName}".Trim(),
                region = d.Region,
                title = !string.IsNullOrEmpty(d.Title) ? d.Title : (d.Region == "PK" ? "Consultant Dental Surgeon" : "Dental Surgeon & Specialist"),
                specialization = !string.IsNullOrEmpty(d.Specialization) ? d.Specialization : "General & Restorative Dentistry",
                yearsOfExperience = d.YearsOfExperience > 0 ? d.YearsOfExperience : 8,
                exp = $"{d.YearsOfExperience} yrs exp",
                biography = d.Biography,
                organizationWorkHistory = d.OrganizationWorkHistory,
                education = d.Education,
                certifications = d.Certifications,
                consultationFee = d.ConsultationFee > 0 ? d.ConsultationFee : (d.Region == "PK" ? 2500m : 150m),
                avatar = !string.IsNullOrEmpty(d.ProfileImageUrl) ? d.ProfileImageUrl : 
                    (d.DoctorID == 2
                        ? "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&q=80&w=400"
                        : (d.DoctorID == 4
                            ? "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&q=80&w=400"
                            : "https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&q=80&w=400")),
                languages = !string.IsNullOrEmpty(d.Languages) ? d.Languages : "English, Urdu",
                rating = d.Rating > 0 ? d.Rating : 4.92m,
                reviewCount = d.ReviewCount > 0 ? d.ReviewCount : 38,
                organizationID = d.OrganizationID,
                organizationName = !string.IsNullOrEmpty(d.OrganizationName) ? d.OrganizationName : (d.Region == "PK" ? "Shifa International Hospitals Ltd" : "Dentia Auckland Regional Dental Hospital"),
                organizationLogoUrl = d.OrganizationLogoUrl,
                organizationCity = d.OrganizationCity,
                hospitalDepartment = !string.IsNullOrEmpty(d.HospitalDepartment) ? d.HospitalDepartment : "Department of Oral Surgery & Dentistry"
            };
            return Ok(detail);
        }

        [HttpGet("doctors/{id}/services")]
        [HttpGet("doctors/{id}/procedures")]
        public async Task<IActionResult> GetDoctorServices(int id)
        {
            var doctor = await _repository.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = $"Doctor with ID {id} not found." });
            }

            var procedures = (await _repository.GetDoctorFeeScheduleAsync(id)).ToList();
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

        [HttpGet("services")]
        [HttpGet("procedures")]
        public async Task<IActionResult> GetAllServices([FromQuery] int? doctorId = null)
        {
            int targetDoctorId = doctorId.GetValueOrDefault(2);
            var doctor = await _repository.GetDoctorByIdAsync(targetDoctorId);
            var procedures = (await _repository.GetDoctorFeeScheduleAsync(targetDoctorId)).ToList();
            string currency = procedures.FirstOrDefault()?.Currency ?? (doctor?.Region == "PK" ? "PKR" : "NZD");

            return Ok(new
            {
                doctorId = targetDoctorId,
                doctorName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}".Trim() : "Clinical Faculty",
                region = doctor?.Region ?? "NZ",
                currency = currency,
                totalProcedures = procedures.Count,
                procedures = procedures
            });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var patient = await _repository.GetPatientByIdAsync(patientId.Value);
            if (patient == null) return NotFound(new { message = "Patient profile not found." });

            // 1. Appointments
            var appointments = (await _repository.GetPatientAppointmentsAsync(patientId.Value)).ToList();
            var nextAppointment = appointments
                .Where(a => a.PreferredDate >= DateTime.UtcNow && a.Status != "Cancelled")
                .OrderBy(a => a.PreferredDate)
                .FirstOrDefault();

            // 2. Invoices & Outstanding Balance
            var invoices = (await _repository.GetPatientInvoicesAsync(patientId.Value)).ToList();
            decimal totalBalance = invoices.Where(i => i.Status != "Paid" && i.Status != "Cancelled").Sum(i => i.BalanceAmount);
            int unpaidCount = invoices.Count(i => i.Status != "Paid" && i.Status != "Cancelled");

            // 3. Prescriptions
            var directRx = await _repository.GetPatientPrescriptionsListAsync(patientId.Value);
            var noteRx = await _repository.GetPatientNotePrescriptionsAsync(patientId.Value);
            int activeRxCount = directRx.Count() + noteRx.Count();

            // 4. Clinical notes / reports
            var notes = (await _repository.GetPatientDentalNotesSummaryAsync(patientId.Value)).Take(3).ToList();

            // 5. Odontogram statistics & diagnostic assessment
            var teeth = (await _repository.GetPatientTeethStatesAsync(patientId.Value)).ToList();
            var assessment = await _repository.GetDiagnosticAssessmentAsync(patientId.Value);

            var activePathologyTeeth = teeth.Where(t =>
            {
                var s = (t.ConditionStatus ?? "").ToLower();
                return !string.IsNullOrWhiteSpace(s) && !s.Contains("healthy") && !s.Contains("sound");
            }).ToList();

            int treatedCount = activePathologyTeeth.Count(t =>
            {
                var s = (t.ConditionStatus ?? "").ToLower();
                return s.Contains("treated") || s.Contains("restor") || s.Contains("fill") || s.Contains("crown") || s.Contains("veneer");
            });
            int attentionCount = activePathologyTeeth.Count - treatedCount;
            int healthyCount = Math.Max(0, 32 - (treatedCount + attentionCount));

            string activeCurrency = invoices.FirstOrDefault()?.Currency ?? (patient.DoctorID == 2 ? "PKR" : "NZD");

            return Ok(new
            {
                patient = new
                {
                    patientId = patient.PatientID,
                    referenceNumber = patient.ReferenceNumber,
                    firstName = patient.FirstName,
                    lastName = patient.LastName,
                    gender = !string.IsNullOrEmpty(patient.Gender) ? patient.Gender : "Unspecified",
                    dob = patient.DOB,
                    phone = patient.Phone,
                    email = patient.Email,
                    currentTreatmentPlan = patient.CurrentTreatmentPlan ?? "General Dentistry",
                    treatmentStage = patient.TreatmentStage ?? "Routine Maintenance",
                    dentitionType = patient.DentitionType ?? "Adult",
                    profileImageDataUrl = patient.ProfileImageDataUrl
                },
                nextAppointment = nextAppointment != null ? new
                {
                    appointmentId = nextAppointment.AppointmentID,
                    date = nextAppointment.PreferredDate,
                    status = nextAppointment.Status,
                    reason = nextAppointment.Reason
                } : null,
                billing = new
                {
                    totalBalance = totalBalance,
                    unpaidInvoiceCount = unpaidCount,
                    currency = activeCurrency
                },
                healthSummary = new
                {
                    healthyTeeth = healthyCount,
                    treatedTeeth = treatedCount,
                    needsAttention = attentionCount,
                    activePrescriptionsCount = activeRxCount
                },
                diagnosticAssessment = assessment,
                recentNotes = notes
            });
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var appointments = await _repository.GetPatientAppointmentsAsync(patientId.Value);
            return Ok(appointments);
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> BookAppointment([FromBody] PatientBookAppointmentRequest request)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var patient = await _repository.GetPatientByIdAsync(patientId.Value);
            if (patient == null) return NotFound(new { message = "Patient not found." });

            if (request.PreferredDate < DateTime.UtcNow.AddMinutes(-10))
            {
                return BadRequest(new { message = "Appointment date cannot be in the past." });
            }

            var (apptId, invoiceId, invNo, receiptOrVoucher, invStatus) = await _repository.BookPatientAppointmentWithPaymentAsync(
                patientId.Value,
                $"{patient.FirstName} {patient.LastName}",
                patient.Phone,
                patient.Email,
                request.PreferredDate,
                request.Reason ?? "Patient Portal Online Booking",
                request.DoctorID ?? patient.DoctorID,
                request.PaymentMethod ?? "Cash",
                request.ConsultationFee > 0 ? request.ConsultationFee : 85.00m,
                request.CardLast4,
                request.CardHolderName,
                request.Currency,
                request.Procedures,
                request.Notes
            );

            await _repository.LogPatientPortalActivityAsync(
                patientId.Value,
                "BOOK_APPOINTMENT",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                $"Booked appointment #{apptId} for {request.PreferredDate:g}. Invoice: {invNo}, Method: {request.PaymentMethod}, Status: {invStatus}"
            );

            return Ok(new
            {
                appointmentId = apptId,
                invoiceId = invoiceId,
                invoiceNumber = invNo,
                paymentMethod = request.PaymentMethod,
                receiptOrVoucherNumber = receiptOrVoucher,
                invoiceStatus = invStatus,
                consultationFee = request.ConsultationFee > 0 ? request.ConsultationFee : 85.00m,
                message = request.PaymentMethod == "Online_Card"
                    ? $"Appointment #{apptId} confirmed! Payment receipt #{receiptOrVoucher} generated and invoice {invNo} marked as Paid."
                    : $"Appointment #{apptId} confirmed! Cash voucher #{receiptOrVoucher} generated for invoice {invNo}. Please present at clinic reception."
            });
        }

        [HttpPut("appointments/{id}/treatment-plan")]
        public async Task<IActionResult> UpdateTreatmentPlan(int id, [FromBody] UpdateTreatmentPlanRequest request)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var (success, message, updatedAppt) = await _repository.UpdateAppointmentTreatmentPlanAsync(
                id, 
                patientId.Value, 
                request?.Procedures, 
                request?.Notes
            );

            if (!success)
            {
                return BadRequest(new { message });
            }

            await _repository.LogPatientPortalActivityAsync(
                patientId.Value,
                "UPDATE_TREATMENT_PLAN",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                $"Updated treatment plan for appointment #{id}: {request.Procedures.Count} procedures, Total: {updatedAppt?.TotalAmount} {updatedAppt?.Currency}"
            );

            return Ok(new
            {
                message = "Treatment plan updated successfully.",
                appointment = updatedAppt
            });
        }

        [HttpPut("appointments/{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            bool cancelled = await _repository.CancelPatientAppointmentAsync(id, patientId.Value);
            if (!cancelled)
            {
                return NotFound(new { message = "Appointment not found or you do not have permission to cancel it." });
            }

            await _repository.LogPatientPortalActivityAsync(
                patientId.Value,
                "CANCEL_APPOINTMENT",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                $"Cancelled appointment #{id}"
            );

            return Ok(new { message = "Appointment has been cancelled successfully." });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var notes = (await _repository.GetPatientDentalNotesSummaryAsync(patientId.Value)).ToList();
            var assessment = await _repository.GetDiagnosticAssessmentAsync(patientId.Value);
            var teeth = (await _repository.GetPatientTeethStatesAsync(patientId.Value)).ToList();

            var diagnosedTeeth = teeth.Where(t =>
            {
                var s = (t.ConditionStatus ?? "").ToLower();
                return !string.IsNullOrWhiteSpace(s) && !s.Contains("healthy") && !s.Contains("sound");
            }).ToList();

            if (assessment != null || diagnosedTeeth.Count > 0)
            {
                string docName = "Dr. Jhangir Ahmed";
                if (assessment?.DoctorID.HasValue == true)
                {
                    var doc = await _repository.GetDoctorByIdAsync(assessment.DoctorID.Value);
                    if (doc != null) docName = $"Dr. {doc.FirstName} {doc.LastName}".Trim();
                }

                string teethSummary = diagnosedTeeth.Count > 0 
                    ? string.Join("; ", diagnosedTeeth.Select(t => $"Tooth #{t.ToothNumber} ({t.ConditionStatus})"))
                    : "Comprehensive Full-Mouth Oral Examination completed";

                var examReport = new
                {
                    noteId = 999000 + (assessment?.AssessmentID ?? 1),
                    patientId = patientId.Value,
                    dentistId = assessment?.DoctorID ?? 2,
                    doctorName = docName,
                    summary = assessment?.DiagnosisSummary ?? $"Chairside Clinical Examination & Odontogram ({diagnosedTeeth.Count} Diagnosed Teeth)",
                    chiefComplaint = $"Comprehensive Dental & Anatomical Assessment ({assessment?.SuiteCategory ?? "Clinical Diagnostics"})",
                    treatmentPerformed = $"Digital 32-Tooth Odontogram Examination & Treatment Planning. Active findings: {teethSummary}",
                    postOpAdvice = "Follow prescribed oral hygiene protocol, monitor impacted areas and attend planned orthodontic/restorative consult.",
                    followUp = "Review in 2 weeks for Phase 1 clinical follow-up.",
                    createdAt = assessment?.UpdatedAt ?? assessment?.CreatedAt ?? (diagnosedTeeth.FirstOrDefault()?.LastUpdated ?? DateTime.UtcNow)
                };

                var combined = new List<dynamic> { examReport };
                combined.AddRange(notes);
                return Ok(combined);
            }

            return Ok(notes);
        }

        [HttpGet("diagnostic-assessment")]
        public async Task<IActionResult> GetDiagnosticAssessment()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var record = await _repository.GetDiagnosticAssessmentAsync(patientId.Value);
            return Ok(record);
        }

        [HttpGet("prescriptions")]
        public async Task<IActionResult> GetPrescriptions()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var directRx = await _repository.GetPatientPrescriptionsListAsync(patientId.Value);
            var noteRx = await _repository.GetPatientNotePrescriptionsAsync(patientId.Value);

            return Ok(new
            {
                directPrescriptions = directRx,
                consultationPrescriptions = noteRx
            });
        }

        [HttpGet("radiographs")]
        public async Task<IActionResult> GetRadiographs()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var xrays = await _repository.GetPatientRadiographsAsync(patientId.Value);
            var list = xrays.Select(x => new
            {
                x.RadiographID,
                x.PatientID,
                x.ImageName,
                x.MimeType,
                x.UploadedAt,
                x.AnalysisSummary,
                imageDataUrl = x.ImageData != null && x.ImageData.Length > 0
                    ? $"data:{x.MimeType};base64,{Convert.ToBase64String(x.ImageData)}"
                    : null
            });

            return Ok(list);
        }

        [HttpGet("odontogram")]
        public async Task<IActionResult> GetOdontogram()
        {
            var patientId = GetAuthenticatedPatientId();
            if (!patientId.HasValue) return Unauthorized(new { message = "Authentication required." });

            var teeth = await _repository.GetPatientTeethStatesAsync(patientId.Value);
            return Ok(teeth);
        }
    }
}
