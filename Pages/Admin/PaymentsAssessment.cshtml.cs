using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

namespace Ascendia.Pages.Admin
{
    public class PaymentsAssessmentModel : PageModel
    {
        private static readonly string[] AllowedPaymentStatuses =
        {
            "Unpaid",
            "Partially Paid",
            "Fully Paid"
        };

        private readonly AppDbContext _dbContext;
        private readonly ILogger<PaymentsAssessmentModel> _logger;

        public PaymentsAssessmentModel(
            AppDbContext dbContext,
            ILogger<PaymentsAssessmentModel> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public List<PaymentRecordViewModel> PaymentsList { get; set; } = new();

        public async Task OnGetAsync()
        {
            PaymentsList = await _dbContext.Students
                .AsNoTracking()
                .Include(student => student.AcademicCourse)
                .Where(student => student.Status == "Enrolled")
                .OrderBy(student => student.StudentNumber)
                .Select(student => new PaymentRecordViewModel
                {
                    StudentNumber = student.StudentNumber ?? string.Empty,
                    StudentName = student.FullName ?? student.StudentName ?? string.Empty,
                    CourseCode = student.AcademicCourse != null
                        ? student.AcademicCourse.CourseCode
                        : student.CourseCode ?? student.Course ?? string.Empty,
                    RemainingBalance = student.RemainingBalance,
                    HasPaymentReceipt = student.PaymentReceiptPath != null
                        && student.PaymentReceiptPath != string.Empty,
                    Status = student.PaymentStatus ?? "Not assessed"
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostUpdatePaymentAsync(
            string? studentNumber,
            decimal newBalance,
            string? newStatus)
        {
            studentNumber = studentNumber?.Trim();
            if (string.IsNullOrWhiteSpace(studentNumber)
                || !ModelState.IsValid
                || newBalance < 0
                || string.IsNullOrWhiteSpace(newStatus)
                || !AllowedPaymentStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Maglagay ng valid na balance at payment status.";
                return RedirectToPage();
            }

            if (newStatus == "Fully Paid" && newBalance != 0)
            {
                TempData["ErrorMessage"] = "Dapat zero ang natitirang balance para maitakda sa Fully Paid.";
                return RedirectToPage();
            }

            if (newStatus != "Fully Paid" && newBalance == 0)
            {
                TempData["ErrorMessage"] = "Piliin ang Fully Paid kapag zero ang natitirang balance.";
                return RedirectToPage();
            }

            var student = await _dbContext.Students
                .SingleOrDefaultAsync(candidate =>
                    candidate.StudentNumber == studentNumber && candidate.Status == "Enrolled");
            if (student is null)
            {
                return NotFound();
            }

            student.RemainingBalance = newBalance;
            student.PaymentStatus = newStatus;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to update payment assessment for student {StudentNumber}.",
                    studentNumber);
                TempData["ErrorMessage"] = "Hindi na-save ang assessment. Subukan muli.";
                return RedirectToPage();
            }

            TempData["SuccessMessage"] =
                $"Na-update ang payment assessment para kay {student.FullName ?? student.StudentName}.";
            return RedirectToPage();
        }
    }

    public class PaymentRecordViewModel
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public decimal? RemainingBalance { get; set; }
        public bool HasPaymentReceipt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}