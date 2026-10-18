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
        public SubjectSection NewSubject { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCourseCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedYearLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedSemester { get; set; }

        public List<SelectListItem> CourseOptions { get; set; } = new();
        public List<SelectListItem> YearLevelOptions { get; set; } = new();
        public List<SelectListItem> SemesterOptions { get; set; } = new();
        public List<SelectListItem> ActiveInstructorOptions { get; set; } = new();

        public List<SubjectSection> SubjectsList { get; set; } = [];

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

            if (NewSubject.InstructorId.HasValue
                && !await _context.Instructors.AnyAsync(
                    instructor => instructor.Id == NewSubject.InstructorId.Value
                        && instructor.Status == "Active",
                    cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(NewSubject.InstructorId),
                    "Active instructors lamang ang maaaring italaga.");
            }

            NewSubject.CourseCode = NewSubject.CourseCode?.Trim() ?? string.Empty;
            NewSubject.SubjectCode = NewSubject.SubjectCode?.Trim() ?? string.Empty;
            NewSubject.SubjectName = NewSubject.SubjectName?.Trim() ?? string.Empty;
            NewSubject.SectionName = NewSubject.SectionName?.Trim() ?? string.Empty;
            var duplicate = await _context.SubjectSections.AnyAsync(section =>
                section.CourseCode == NewSubject.CourseCode
                && section.SubjectCode == NewSubject.SubjectCode
                && section.SectionName == NewSubject.SectionName
                && section.YearLevel == NewSubject.YearLevel
                && section.Semester == NewSubject.Semester,
                cancellationToken);
            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "May existing subject record na para sa course, section, year, at semester na ito.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync(cancellationToken);
                return Page();
            }

            _context.SubjectSections.Add(NewSubject);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] = "Matagumpay na naidagdag ang bagong subject at section!";
            return RedirectToPage(FilterRouteValues());
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            var itemToRemove = await _context.SubjectSections.FindAsync([id], cancellationToken);
            if (itemToRemove is not null)
            {
                _context.SubjectSections.Remove(itemToRemove);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = "Matagumpay na natanggal ang record.";
            }

            NormalizeFilters();
            return RedirectToPage(FilterRouteValues());
        }

        public async Task<IActionResult> OnPostAssignInstructorAsync(
            int id,
            int? instructorId,
            CancellationToken cancellationToken)
        {
            var subject = await _context.SubjectSections.FindAsync([id], cancellationToken);
            if (subject is null)
            {
                return NotFound();
            }

            if (instructorId.HasValue
                && !await _context.Instructors.AnyAsync(
                    instructor => instructor.Id == instructorId.Value
                        && instructor.Status == "Active",
                    cancellationToken))
            {
                TempData["ErrorMessage"] = "Active instructors lamang ang maaaring italaga.";
                return RedirectToPage(FilterRouteValues());
            }

            subject.InstructorId = instructorId;
            await _context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Na-update ang instructor assignment.";
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
            ActiveInstructorOptions = await _context.Instructors
                .AsNoTracking()
                .Where(instructor => instructor.Status == "Active")
                .OrderBy(instructor => instructor.FullName)
                .Select(instructor => new SelectListItem(
                    instructor.FullName + " — " + instructor.Department,
                    instructor.Id.ToString()))
                .ToListAsync(cancellationToken);

            var subjects = _context.SubjectSections.AsNoTracking()
                .Include(subject => subject.Instructor)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(SelectedCourseCode))
            {
                subjects = subjects.Where(subject => subject.CourseCode == SelectedCourseCode);
            }

            if (!string.IsNullOrWhiteSpace(SelectedYearLevel))
            {
                subjects = subjects.Where(subject => subject.YearLevel == SelectedYearLevel);
            }

            if (!string.IsNullOrWhiteSpace(SelectedSemester))
            {
                subjects = subjects.Where(subject => subject.Semester == SelectedSemester);
            }

            SubjectsList = await subjects.OrderBy(subject => subject.CourseCode)
                .ThenBy(subject => subject.SubjectCode)
                .ToListAsync(cancellationToken);

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
