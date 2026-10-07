using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;
using System.IO;

namespace Ascendia.Pages.Admin
{
    public class StudentApplicationsModel : PageModel
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentApplicationsModel(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IList<Student> StudentsList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            StudentsList = await _context.Students
                .Include(student => student.AcademicCourse)
                .Where(s => s.Status == "Pending" || string.IsNullOrEmpty(s.Status))
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostApproveAsync(string? studentNumber)
        {
            studentNumber = studentNumber?.Trim();
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                return BadRequest();
            }

            var student = await _context.Students
                .SingleOrDefaultAsync(candidate => candidate.StudentNumber == studentNumber);
            if (student == null)
            {
                return NotFound();
            }

            student.Status = "Enrolled";

            var coursePrefix = string.IsNullOrWhiteSpace(student.CourseCode)
                ? GetCoursePrefix(student.Course)
                : student.CourseCode.Trim();

            if (string.IsNullOrWhiteSpace(student.CourseCode))
            {
                student.CourseCode = coursePrefix;
            }

            if (string.IsNullOrEmpty(student.Section))
            {
                student.Section = $"{coursePrefix}-4A";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Matagumpay na na-approve ang aplikasyon ni {student.FullName} at itinalaga sa seksyong {student.Section}!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(string? studentNumber, string rejectionReason)
        {
            studentNumber = studentNumber?.Trim();
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                return BadRequest();
            }

            var student = await _context.Students
                .SingleOrDefaultAsync(candidate => candidate.StudentNumber == studentNumber);
            if (student == null)
            {
                return NotFound();
            }

            student.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Na-reject ang aplikasyon. Reason: {rejectionReason}";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDocumentAsync(
            string? studentNumber,
            string documentType,
            CancellationToken cancellationToken)
        {
            studentNumber = studentNumber?.Trim();
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                return BadRequest();
            }

            var student = await _context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.StudentNumber == studentNumber,
                    cancellationToken);
            if (student is null)
            {
                return NotFound();
            }

            var storedPath = documentType.ToLowerInvariant() switch
            {
                "psa" => student.PsaBirthCertificatePath,
                "form138" => student.Form138Path,
                "form137" => student.Form137Path,
                "goodmoral" => student.GoodMoralPath,
                "tor" => student.TorPath,
                "paymentreceipt" => student.PaymentReceiptPath,
                "profilepicture" => student.ProfilePicturePath,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return NotFound();
            }

            var normalizedStoredPath = storedPath.Replace('\\', '/');
            var fileName = Path.GetFileName(normalizedStoredPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName != normalizedStoredPath[(normalizedStoredPath.LastIndexOf('/') + 1)..])
            {
                return NotFound();
            }

            var fileRoot = normalizedStoredPath.StartsWith("/private-uploads/", StringComparison.Ordinal)
                ? Path.Combine(_environment.ContentRootPath, "App_Data", "uploads")
                : normalizedStoredPath.StartsWith("/uploads/", StringComparison.Ordinal)
                    ? Path.Combine(_environment.WebRootPath, "uploads")
                    : null;
            if (fileRoot is null)
            {
                return NotFound();
            }

            var fullPath = Path.GetFullPath(Path.Combine(fileRoot, fileName));
            var fullRoot = Path.GetFullPath(fileRoot) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fullPath);
            string contentType;
            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/pdf";
            }
            else if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/octet-stream";
                Response.Headers.ContentDisposition = "attachment";
            }
            else if (!ContentTypeProvider.TryGetContentType(fullPath, out var imageContentType))
            {
                contentType = "application/octet-stream";
            }
            else
            {
                contentType = imageContentType;
            }

            var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            return new FileStreamResult(stream, contentType)
            {
                EnableRangeProcessing = true
            };
        }

        private static string GetCoursePrefix(string? course)
        {
            if (string.IsNullOrWhiteSpace(course))
            {
                return "GEN";
            }

            var normalizedCourse = course.Trim().ToUpperInvariant();

            if (normalizedCourse.Contains("BSCPE") || normalizedCourse.Contains("COMPUTER ENGINEERING"))
            {
                return "BSCpE";
            }

            if (normalizedCourse.Contains("BSIT") || normalizedCourse.Contains("INFORMATION TECHNOLOGY"))
            {
                return "BSIT";
            }

            if (normalizedCourse.Contains("BSCS") || normalizedCourse.Contains("COMPUTER SCIENCE"))
            {
                return "BSCS";
            }

            var prefix = string.Concat(
                normalizedCourse
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(word => word is not "BACHELOR" and not "OF" and not "SCIENCE" and not "IN")
                    .Select(word => word[0]));
            return string.IsNullOrEmpty(prefix) ? "GEN" : prefix;
        }
    }
}