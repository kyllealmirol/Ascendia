using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;
using System.ComponentModel.DataAnnotations;

namespace Ascendia.Pages
{
    public class DropStopTransferModel : PageModel
    {
        private static readonly IReadOnlyDictionary<string, string[]> AllowedReasons =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Transfer"] =
                [
                    "Academic / Career Alignment (Change of Interest)",
                    "Relocation / Change of Address",
                    "Financial Consideration",
                    "Personal / Family Reasons"
                ],
                ["Drop"] =
                [
                    "Prerequisite Conflict / Schedule Issue",
                    "Academic Overload / Time Management",
                    "Financial Constraints",
                    "Health / Medical Reasons"
                ],
                ["Stop"] =
                [
                    "Financial Difficulties (Need to work/save)",
                    "Medical / Health Issues (Extended recovery)",
                    "Family Responsibilities",
                    "Employment Opportunities"
                ],
                ["LOA"] =
                [
                    "Financial Difficulties (Need to work/save)",
                    "Medical / Health Issues (Extended recovery)",
                    "Family Responsibilities",
                    "Employment Opportunities"
                ]
            };

        private readonly AppDbContext _dbContext;

        public DropStopTransferModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public DropStopRequestModel TransferRequest { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (TransferRequest is null)
            {
                ModelState.AddModelError(string.Empty, "Hindi valid ang request form.");
                return Page();
            }

            var requestType = TransferRequest.RequestType?.Trim();
            if (requestType is null || !AllowedReasons.TryGetValue(requestType, out var allowedReasons))
            {
                ModelState.AddModelError("TransferRequest.RequestType", "Pumili ng valid na request type.");
            }
            else if (TransferRequest.ReasonCategory is null
                || !allowedReasons.Contains(TransferRequest.ReasonCategory, StringComparer.Ordinal))
            {
                ModelState.AddModelError("TransferRequest.ReasonCategory", "Pumili ng valid na dahilan para sa request type.");
            }

            TransferRequest.StudentNumber = TransferRequest.StudentNumber?.Trim();
            TransferRequest.StudentName = TransferRequest.StudentName?.Trim();
            TransferRequest.CurrentCourse = TransferRequest.CurrentCourse?.Trim();
            TransferRequest.AdditionalDetails = TransferRequest.AdditionalDetails?.Trim();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var student = await _dbContext.Students
                .Include(candidate => candidate.AcademicCourse)
                .SingleOrDefaultAsync(
                    candidate => candidate.StudentNumber == TransferRequest.StudentNumber
                        && candidate.Status == "Enrolled",
                    cancellationToken);

            if (student is null)
            {
                ModelState.AddModelError(
                    "TransferRequest.StudentNumber",
                    "Hindi makita ang enrolled student gamit ang student number na ito.");
                return Page();
            }

            var submittedName = TransferRequest.StudentName ?? string.Empty;
            var storedName = student.FullName ?? student.StudentName ?? string.Empty;
            if (!string.Equals(submittedName, storedName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "TransferRequest.StudentName",
                    "Hindi tugma ang pangalan sa student record.");
                return Page();
            }

            var academicRequest = new AcademicRequest
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber!,
                StudentName = storedName,
                CurrentCourse = student.AcademicCourse?.CourseName
                    ?? student.Course
                    ?? student.CourseCode
                    ?? string.Empty,
                RequestType = requestType!,
                ReasonCategory = TransferRequest.ReasonCategory!,
                AdditionalDetails = TransferRequest.AdditionalDetails,
                DateFiledUtc = DateTime.UtcNow
            };

            _dbContext.AcademicRequests.Add(academicRequest);
            await _dbContext.SaveChangesAsync(cancellationToken);

            SuccessMessage = $"Your {academicRequest.RequestType} request has been submitted to the Registrar.";
            return RedirectToPage();
        }
    }

    public class DropStopRequestModel
    {
        [Required, StringLength(32)]
        public string? StudentNumber { get; set; }
        [Required, StringLength(200)]
        public string? StudentName { get; set; }
        [Required, StringLength(100)]
        public string? CurrentCourse { get; set; }
        [Required, StringLength(16)]
        public string? RequestType { get; set; }
        [Required, StringLength(200)]
        public string? ReasonCategory { get; set; }
        [StringLength(2000)]
        public string? AdditionalDetails { get; set; }
    }
}