using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages.Admin
{
    public class SubjectsSectionsModel : PageModel
    {
        private const int SectionCapacity = 40;
        private static readonly string[] YearLevels =
        [
            "1st Year",
            "2nd Year",
            "3rd Year",
            "4th Year"
        ];
        private static readonly string[] Semesters = ["1st Sem", "2nd Sem"];

        private readonly AppDbContext _context;

        public SubjectsSectionsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SubjectSectionViewModel NewSubject { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCourseCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedYearLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedSemester { get; set; }

        public List<SelectListItem> CourseOptions { get; set; } = new();
        public List<SelectListItem> YearLevelOptions { get; set; } = new();
        public List<SelectListItem> SemesterOptions { get; set; } = new();

        public List<SubjectSectionViewModel> SubjectsList { get; set; } =
        [
            new() { Id = 1, SubjectCode = "CPE 411", SubjectName = "Engineering Management", SectionName = "4A", Units = 3, CourseCode = "BSCpE", YearLevel = "4th Year", Semester = "1st Sem" },
            new() { Id = 2, SubjectCode = "CPE 412", SubjectName = "Embedded Systems", SectionName = "4A", Units = 3, CourseCode = "BSCpE", YearLevel = "4th Year", Semester = "1st Sem" },
            new() { Id = 3, SubjectCode = "IT 301", SubjectName = "Information Assurance and Security", SectionName = "3B", Units = 3, CourseCode = "BSIT", YearLevel = "3rd Year", Semester = "1st Sem" }
        ];

        public List<StudentSectionViewModel> StudentSections { get; set; } = new();

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            NormalizeFilters();
            await LoadPageDataAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            NormalizeFilters();
            var courseExists = !string.IsNullOrWhiteSpace(NewSubject.CourseCode)
                && await _context.Courses.AnyAsync(
                    course => course.CourseCode == NewSubject.CourseCode,
                    cancellationToken);
            if (!courseExists
                || !YearLevels.Contains(NewSubject.YearLevel, StringComparer.OrdinalIgnoreCase)
                || !Semesters.Contains(NewSubject.Semester, StringComparer.OrdinalIgnoreCase)
                || NewSubject.Units is < 1 or > 10)
            {
                ModelState.AddModelError(string.Empty, "Maglagay ng valid na course, year, semester, at units.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync(cancellationToken);
                return Page();
            }

            NewSubject.Id = SubjectsList.Count > 0 ? SubjectsList.Max(subject => subject.Id) + 1 : 1;
            SubjectsList.Add(NewSubject);

            TempData["SuccessMessage"] = "Matagumpay na naidagdag ang bagong subject at section!";
            return RedirectToPage(FilterRouteValues());
        }

        public IActionResult OnPostDelete(int id)
        {
            var itemToRemove = SubjectsList.FirstOrDefault(subject => subject.Id == id);
            if (itemToRemove is not null)
            {
                SubjectsList.Remove(itemToRemove);
                TempData["SuccessMessage"] = "Matagumpay na natanggal ang record.";
            }

            NormalizeFilters();
            return RedirectToPage(FilterRouteValues());
        }

        public async Task<IActionResult> OnPostAutoSectionAsync(CancellationToken cancellationToken)
        {
            NormalizeFilters();
            if (string.IsNullOrWhiteSpace(SelectedCourseCode)
                || !YearLevels.Contains(SelectedYearLevel, StringComparer.OrdinalIgnoreCase)
                || !Semesters.Contains(SelectedSemester, StringComparer.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Pumili ng valid na course, year level, at semester bago mag-auto-section.";
                return RedirectToPage(FilterRouteValues());
            }

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.CourseCode.ToUpper() == SelectedCourseCode!.ToUpper(),
                    cancellationToken);
            if (course is null)
            {
                TempData["ErrorMessage"] = "Hindi makita ang napiling course.";
                return RedirectToPage(FilterRouteValues());
            }

            var students = await _context.Students
                .Include(student => student.AcademicCourse)
                .Where(student => student.Status != null && student.Status.Trim().ToUpper() == "ENROLLED")
                .ToListAsync(cancellationToken);

            var matchingStudents = students
                .Where(student =>
                    string.Equals(GetCourseCode(student), course.CourseCode, StringComparison.OrdinalIgnoreCase)
                    && MatchesYearAndSemester(student.YearAndSemester, SelectedYearLevel!, SelectedSemester!))
                .OrderBy(student => student.StudentNumber ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(student => student.Id)
                .ToList();

            if (matchingStudents.Count == 0)
            {
                TempData["ErrorMessage"] = "Walang Enrolled na estudyanteng tumutugma sa napiling filters.";
                return RedirectToPage(FilterRouteValues());
            }

            for (var index = 0; index < matchingStudents.Count; index++)
            {
                var sectionLetter = (char)('A' + (index / SectionCapacity % 26));
                var sectionGroup = index / (SectionCapacity * 26);
                var sectionSuffix = sectionGroup == 0
                    ? sectionLetter.ToString()
                    : $"{sectionLetter}{sectionGroup + 1}";
                matchingStudents[index].Section = $"{course.CourseCode}-{GetYearNumber(SelectedYearLevel!)}{sectionSuffix}";
            }

            await _context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] =
                $"Matagumpay na na-section ang {matchingStudents.Count} enrolled na estudyante. Hanggang {SectionCapacity} bawat section.";
            return RedirectToPage(FilterRouteValues());
        }

        private async Task LoadPageDataAsync(CancellationToken cancellationToken)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .OrderBy(course => course.CourseCode)
                .ToListAsync(cancellationToken);

            CourseOptions = courses
                .Select(course => new SelectListItem(course.CourseCode, course.CourseCode))
                .ToList();
            YearLevelOptions = YearLevels.Select(year => new SelectListItem(year, year)).ToList();
            SemesterOptions = Semesters.Select(semester => new SelectListItem(semester, semester)).ToList();

            SubjectsList = SubjectsList
                .Where(subject => MatchesFilters(subject.CourseCode, subject.YearLevel, subject.Semester))
                .ToList();

            var students = await _context.Students
                .AsNoTracking()
                .Include(student => student.AcademicCourse)
                .OrderBy(student => student.StudentNumber)
                .ThenBy(student => student.Id)
                .ToListAsync(cancellationToken);

            StudentSections = students
                .Where(student => MatchesFilters(
                    GetCourseCode(student),
                    GetYearLevel(student.YearAndSemester),
                    GetSemester(student.YearAndSemester)))
                .Select(student => new StudentSectionViewModel
                {
                    StudentNumber = student.StudentNumber ?? string.Empty,
                    FullName = student.FullName ?? student.StudentName ?? string.Empty,
                    CourseCode = GetCourseCode(student),
                    YearAndSemester = student.YearAndSemester ?? string.Empty,
                    Section = student.Section ?? string.Empty,
                    Status = student.Status ?? string.Empty,
                    IsSpecialStatus = IsSpecialStatus(student.Status, student.StudentType)
                })
                .ToList();
        }

        private bool MatchesFilters(string? courseCode, string? yearLevel, string? semester)
        {
            return (string.IsNullOrWhiteSpace(SelectedCourseCode)
                    || string.Equals(courseCode, SelectedCourseCode, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(SelectedYearLevel)
                    || string.Equals(yearLevel, SelectedYearLevel, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(SelectedSemester)
                    || string.Equals(semester, SelectedSemester, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesYearAndSemester(string? yearAndSemester, string yearLevel, string semester)
        {
            return string.Equals(GetYearLevel(yearAndSemester), yearLevel, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetSemester(yearAndSemester), semester, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCourseCode(Student student)
        {
            if (!string.IsNullOrWhiteSpace(student.AcademicCourse?.CourseCode))
            {
                return student.AcademicCourse.CourseCode.Trim();
            }

            return !string.IsNullOrWhiteSpace(student.CourseCode)
                ? student.CourseCode.Trim()
                : student.Course?.Trim() ?? string.Empty;
        }

        private static string? GetYearLevel(string? yearAndSemester)
        {
            return YearLevels.FirstOrDefault(year =>
                yearAndSemester?.StartsWith(year, StringComparison.OrdinalIgnoreCase) == true);
        }

        private static string? GetSemester(string? yearAndSemester)
        {
            return Semesters.FirstOrDefault(semester =>
                yearAndSemester?.EndsWith(semester, StringComparison.OrdinalIgnoreCase) == true);
        }

        private static int GetYearNumber(string yearLevel)
        {
            return Array.IndexOf(YearLevels, yearLevel) + 1;
        }

        private static bool IsSpecialStatus(string? status, string? studentType)
        {
            return string.Equals(status, "Dropped", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Stopped", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Transferee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Transferred", StringComparison.OrdinalIgnoreCase)
                || string.Equals(studentType, "Transferee", StringComparison.OrdinalIgnoreCase);
        }

        private void NormalizeFilters()
        {
            SelectedCourseCode = SelectedCourseCode?.Trim();
            SelectedYearLevel = YearLevels.FirstOrDefault(year =>
                string.Equals(year, SelectedYearLevel?.Trim(), StringComparison.OrdinalIgnoreCase));
            SelectedSemester = Semesters.FirstOrDefault(semester =>
                string.Equals(semester, SelectedSemester?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private object FilterRouteValues()
        {
            return new
            {
                SelectedCourseCode,
                SelectedYearLevel,
                SelectedSemester
            };
        }
    }

    public class SubjectSectionViewModel
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public int Units { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
    }

    public class StudentSectionViewModel
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string YearAndSemester { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsSpecialStatus { get; set; }
    }
}
