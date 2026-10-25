using System.Threading.Tasks;
using DentistAPI.Models;

namespace DentistAPI.Services
{
    public interface ITreatmentPolicyService
    {
        /// <summary>
        /// Rule 1 — Doctor lock: Once a treatment is started (IN_PROGRESS), its doctor cannot be changed until it is COMPLETED.
        /// </summary>
        Task<PolicyResult> CanChangeDoctorAsync(int patientId, int? toothNumber = null);

        /// <summary>
        /// Rule 2 & D1 — Single active treatment at a time: A new treatment can start only when the patient has no IN_PROGRESS treatment.
        /// </summary>
        Task<PolicyResult> CanStartTreatmentAsync(int patientId, long treatmentItemId);

        /// <summary>
        /// Rule 4 & D2 — Started + any payment: If treatment is IN_PROGRESS and net paid > 0, dental chart for that tooth is locked.
        /// </summary>
        Task<PolicyResult> CanEditChartAsync(int patientId, int toothNumber, string? toothKey = null);

        /// <summary>
        /// Rule 3 — Not started = freely editable. Treatment details and plan stay in sync.
        /// </summary>
        Task<PolicyResult> CanEditTreatmentAsync(long treatmentItemId);

        /// <summary>
        /// Comprehensive policy snapshot for patient consumed by frontend to prevent duplicate checks.
        /// </summary>
        Task<PatientTreatmentPolicyDto> GetPatientPolicyAsync(int patientId);

        /// <summary>
        /// Logs blocked action attempt to audit system.
        /// </summary>
        Task LogBlockedAttemptAsync(int? actorId, string actorRole, int patientId, string action, string errorCode, string message, string? ipAddress = null);
    }
}
