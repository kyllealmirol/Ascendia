using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;
using System.IO;

namespace Ascendia.Pages.Admin
{
    public class ReportsMasterlistModel : PageModel
    {
        private static readonly HashSet<string> AllowedStudentStatuses =
        [
            "Pending",
            "Enrolled",
            "Dropped",
            "Stopped",
            "LOA",
            "Transferred",
            "Rejected"
        ];

        private const long MaxUploadBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedDocumentExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
            };
        private static readonly HashSet<string> AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
            };
        private static readonly RequirementDefinition[] RequirementDefinitions =
        [
            new("psa", "PSA Birth Certificate", student => student.PsaBirthCertificatePath,
                (student, path) => student.PsaBirthCertificatePath = path),
            new("form138", "Form 138", student => student.Form138Path,
                (student, path) => student.Form138Path = path),
            new("form137", "Form 137", student => student.Form137Path,
                (student, path) => student.Form137Path = path),
            new("goodmoral", "Good Moral", student => student.GoodMoralPath,
                (student, path) => student.GoodMoralPath = path),
            new("picture", "2x2 Picture", student => student.ProfilePicturePath,
                (student, path) => student.ProfilePicturePath = path, ImagesOnly: true),
            new("tor", "Transcript of Records (TOR)", student => student.TorPath,
                (student, path) => student.TorPath = path)
        ];

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReportsMasterlistModel(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<SelectListItem> CourseOptions { get; set; } = new();

        public List<ReportItemViewModel> Masterlist { get; set; } = new();

        public async Task OnGetAsync()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .OrderBy(course => course.CourseCode)
                .ToListAsync();

            var students = await _context.Students
                .AsNoTracking()
                .Include(student => student.AcademicCourse)
                .OrderByDescending(student => student.Id)
                .ToListAsync();

            var allStudents = students
                .Select(CreateReportItem)
                .ToList();

            var availableCourses = courses
                .Select(course => new
                {
                    Code = course.CourseCode.Trim(),
                    Name = course.CourseName.Trim()
                })
                .Concat(allStudents
                    .Where(student => !string.IsNullOrWhiteSpace(student.CourseCode))
                    .Where(student => !courses.Any(course =>
                        string.Equals(course.CourseCode, student.CourseCode, StringComparison.OrdinalIgnoreCase)))
                    .Select(student => new
                    {
                        Code = student.CourseCode,
                        Name = student.CourseProgram
                    }))
                .Where(course => !string.IsNullOrWhiteSpace(course.Code))
                .GroupBy(course => course.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(course => course.Code)
                .ToList();

            CourseOptions = availableCourses
                .Select(course => new SelectListItem(course.Code, course.Code))
                .ToList();

            var category = SelectedCategory?.Trim();
            if (string.IsNullOrEmpty(category))
            {
                Masterlist = allStudents;
            }
            else
            {
                var selectedCourseCode = availableCourses
                    .Select(course => course.Code)
                    .FirstOrDefault(code => string.Equals(code, category, StringComparison.OrdinalIgnoreCase));
                if (selectedCourseCode is null)
                {
                    Masterlist = new List<ReportItemViewModel>();
                }
                else
                {
                    var normalizedCategory = selectedCourseCode.ToUpperInvariant();
                    var filteredStudents = await _context.Students
                        .AsNoTracking()
                        .Include(student => student.AcademicCourse)
                        .Where(student =>
                            (student.AcademicCourse != null
                                && student.AcademicCourse.CourseCode != null
                                && student.AcademicCourse.CourseCode.Trim() != string.Empty
                                && student.AcademicCourse.CourseCode.Trim().ToUpper() == normalizedCategory)
                            || ((student.AcademicCourse == null
                                    || student.AcademicCourse.CourseCode == null
                                    || student.AcademicCourse.CourseCode.Trim() == string.Empty)
                                && student.CourseCode != null
                                && student.CourseCode.Trim() != string.Empty
                                && student.CourseCode.Trim().ToUpper() == normalizedCategory)
                            || ((student.AcademicCourse == null
                                    || student.AcademicCourse.CourseCode == null
                                    || student.AcademicCourse.CourseCode.Trim() == string.Empty)
                                && (student.CourseCode == null || student.CourseCode.Trim() == string.Empty)
                                && student.Course != null
                                && student.Course.Trim().ToUpper() == normalizedCategory))
                        .OrderByDescending(student => student.Id)
                        .ToListAsync();

                    Masterlist = filteredStudents
                        .Select(CreateReportItem)
                        .ToList();
                }
            }

            var searchTerm = SearchTerm?.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                SearchTerm = searchTerm;
                Masterlist = Masterlist
                    .Where(student =>
                        student.StudentId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                        || student.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(
            string? studentNumber,
            string? newStatus,
            string? selectedCategory,
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            studentNumber = studentNumber?.Trim();
            newStatus = newStatus?.Trim();
            selectedCategory = selectedCategory?.Trim();
            searchTerm = searchTerm?.Trim();

            if (string.IsNullOrWhiteSpace(studentNumber)
                || string.IsNullOrWhiteSpace(newStatus)
                || !AllowedStudentStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Maglagay ng valid na student number at status.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            var student = await _context.Students
                .SingleOrDefaultAsync(
                    candidate => candidate.StudentNumber == studentNumber,
                    cancellationToken);
            if (student is null)
            {
                TempData["ErrorMessage"] = "Hindi makita ang estudyante gamit ang student number.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            student.Status = newStatus;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"Na-update ang status ni {student.FullName ?? student.StudentName ?? studentNumber} sa {newStatus}.";
            return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
        }

        public async Task<IActionResult> OnPostUploadRequirementAsync(
            int studentId,
            string? requirementKey,
            IFormFile? requirementFile,
            string? selectedCategory,
            string? searchTerm,
            CancellationToken cancellationToken)
        {
            selectedCategory = selectedCategory?.Trim();
            searchTerm = searchTerm?.Trim();
            var definition = RequirementDefinitions.FirstOrDefault(requirement =>
                string.Equals(requirement.Key, requirementKey, StringComparison.OrdinalIgnoreCase));

            if (studentId <= 0 || definition is null)
            {
                TempData["ErrorMessage"] = "Hindi valid ang estudyante o requirement na pinili.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            if (requirementFile is null || requirementFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Pumili ng file na ia-upload.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            var extension = Path.GetExtension(requirementFile.FileName);
            var allowedExtensions = definition.ImagesOnly
                ? AllowedImageExtensions
                : AllowedDocumentExtensions;
            if (requirementFile.Length > MaxUploadBytes || !allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = definition.ImagesOnly
                    ? "Mag-upload ng image file na hanggang 10 MB para sa 2x2 Picture."
                    : "Mag-upload ng PDF o image file na hanggang 10 MB para sa requirement.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            var student = await _context.Students
                .SingleOrDefaultAsync(candidate => candidate.Id == studentId, cancellationToken);
            if (student is null)
            {
                TempData["ErrorMessage"] = "Hindi makita ang napiling estudyante.";
                return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
            }

            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var uploadDirectory = Path.Combine(webRootPath, "uploads", "requirements");
            Directory.CreateDirectory(uploadDirectory);

            var fileName = $"{definition.Key}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadDirectory, fileName);
            await using (var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await requirementFile.CopyToAsync(fileStream, cancellationToken);
            }

            definition.SetPath(student, $"/uploads/requirements/{fileName}");
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"{definition.Label} ni {student.FullName ?? student.StudentName ?? student.StudentNumber} ay matagumpay na na-upload.";
            return RedirectToPage(new { SelectedCategory = selectedCategory, SearchTerm = searchTerm });
        }

        public async Task<IActionResult> OnGetRequirementDocumentAsync(
            int studentId,
            string? requirementKey,
            CancellationToken cancellationToken)
        {
            var definition = RequirementDefinitions.FirstOrDefault(requirement =>
                string.Equals(requirement.Key, requirementKey, StringComparison.OrdinalIgnoreCase));
            if (studentId <= 0 || definition is null)
            {
                return BadRequest();
            }

            var student = await _context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == studentId, cancellationToken);
            if (student is null)
            {
                return NotFound();
            }

            var storedPath = definition.GetPath(student)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return NotFound();
            }

            var fileName = Path.GetFileName(storedPath);
            if (string.IsNullOrWhiteSpace(fileName)
                || fileName != storedPath[(storedPath.LastIndexOf('/') + 1)..])
            {
                return NotFound();
            }

            var contentRoot = storedPath.StartsWith("/uploads/requirements/", StringComparison.Ordinal)
                ? Path.Combine(GetWebRootPath(), "uploads", "requirements")
                : storedPath.StartsWith("/uploads/", StringComparison.Ordinal)
                    ? Path.Combine(GetWebRootPath(), "uploads")
                    : storedPath.StartsWith("/private-uploads/", StringComparison.Ordinal)
                        ? Path.Combine(_environment.ContentRootPath, "App_Data", "uploads")
                        : null;
            if (contentRoot is null)
            {
                return NotFound();
            }

            var fullPath = Path.GetFullPath(Path.Combine(contentRoot, fileName));
            var fullRoot = Path.GetFullPath(contentRoot) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fullPath);
            var contentType = extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Disposition"] = "inline";
            return new PhysicalFileResult(fullPath, contentType)
            {
                EnableRangeProcessing = true
            };
        }

        private static ReportItemViewModel CreateReportItem(Student student)
        {
            var courseCode = !string.IsNullOrWhiteSpace(student.AcademicCourse?.CourseCode)
                ? student.AcademicCourse.CourseCode.Trim()
                : !string.IsNullOrWhiteSpace(student.CourseCode)
                ? student.CourseCode.Trim()
                : student.Course?.Trim() ?? string.Empty;
            var courseProgram = student.AcademicCourse?.CourseName.Trim()
                ?? student.Course?.Trim()
                ?? string.Empty;

            return new ReportItemViewModel
            {
                DatabaseId = student.Id,
                StudentId = student.StudentNumber?.Trim() ?? string.Empty,
                FullName = student.FullName?.Trim() ?? student.StudentName?.Trim() ?? string.Empty,
                CourseCode = courseCode,
                CourseProgram = courseProgram ?? courseCode,
                Status = student.Status?.Trim() ?? string.Empty,
                Requirements = RequirementDefinitions
                    .Select(requirement => new RequirementBadgeViewModel
                    {
                        Key = requirement.Key,
                        Label = requirement.Label,
                        IsSubmitted = !string.IsNullOrWhiteSpace(requirement.GetPath(student))
                    })
                    .ToList()
            };
        }

        private sealed record RequirementDefinition(
            string Key,
            string Label,
            Func<Student, string?> GetPath,
            Action<Student, string?> SetPath,
            bool ImagesOnly = false);

        private string GetWebRootPath()
        {
            return string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
        }
    }

    public class ReportItemViewModel
    {
        public int DatabaseId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseProgram { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<RequirementBadgeViewModel> Requirements { get; set; } = new();
        public bool RequirementsComplete => Requirements.Count > 0 && Requirements.All(requirement => requirement.IsSubmitted);
    }

    public class RequirementBadgeViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsSubmitted { get; set; }
    }
}
