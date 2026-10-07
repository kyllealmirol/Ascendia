using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

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

        private readonly AppDbContext _context;

        public ReportsMasterlistModel(AppDbContext context)
        {
            _context = context;
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
                StudentId = student.StudentNumber?.Trim() ?? string.Empty,
                FullName = student.FullName?.Trim() ?? student.StudentName?.Trim() ?? string.Empty,
                CourseCode = courseCode,
                CourseProgram = courseProgram ?? courseCode,
                Status = student.Status?.Trim() ?? string.Empty
            };
        }
    }

    public class ReportItemViewModel
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseProgram { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
