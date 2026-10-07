using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

namespace Ascendia.Pages.Admin
{
    public class CoursesManagementModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CoursesManagementModel> _logger;

        public CoursesManagementModel(
            AppDbContext context,
            ILogger<CoursesManagementModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public AcademicCourse NewCourse { get; set; } = new();

        [BindProperty]
        public AcademicCourse EditCourse { get; set; } = new();

        public List<AcademicCourse> CoursesList { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadCoursesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            NewCourse.CourseCode = NewCourse.CourseCode?.Trim() ?? string.Empty;
            NewCourse.CourseName = NewCourse.CourseName?.Trim() ?? string.Empty;
            NewCourse.Description = NewCourse.Description?.Trim() ?? string.Empty;
            ValidateCourse(NewCourse, "NewCourse");

            if (!ModelState.IsValid)
            {
                await LoadCoursesAsync();
                return Page();
            }

            if (await CourseCodeExistsAsync(NewCourse.CourseCode))
            {
                ModelState.AddModelError("NewCourse.CourseCode", "Mayroon nang kursong gumagamit ng code na ito.");
                await LoadCoursesAsync();
                return Page();
            }

            NewCourse.CourseCode = NewCourse.CourseCode.ToUpperInvariant();
            _context.Courses.Add(NewCourse);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                if (!await IsDuplicateCourseCodeAsync(NewCourse.CourseCode))
                {
                    _logger.LogError(exception, "Could not save course {CourseCode}.", NewCourse.CourseCode);
                    throw;
                }

                _context.Entry(NewCourse).State = EntityState.Detached;
                ModelState.AddModelError("NewCourse.CourseCode", "Mayroon nang kursong gumagamit ng code na ito.");
                await LoadCoursesAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Matagumpay na naidagdag ang bagong kurso at deskripsyon nito!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id)
        {
            EditCourse.CourseCode = EditCourse.CourseCode?.Trim() ?? string.Empty;
            EditCourse.CourseName = EditCourse.CourseName?.Trim() ?? string.Empty;
            EditCourse.Description = EditCourse.Description?.Trim() ?? string.Empty;
            ValidateCourse(EditCourse, "EditCourse");

            if (!ModelState.IsValid)
            {
                await LoadCoursesAsync();
                return Page();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course is null)
            {
                return NotFound();
            }

            var newCode = EditCourse.CourseCode.Trim().ToUpperInvariant();
            var duplicateCodeExists = await CourseCodeExistsAsync(newCode, id);
            if (duplicateCodeExists)
            {
                ModelState.AddModelError("EditCourse.CourseCode", "Mayroon nang kursong gumagamit ng code na ito.");
                await LoadCoursesAsync();
                return Page();
            }

            course.CourseCode = newCode;
            course.CourseName = EditCourse.CourseName.Trim();
            course.Description = EditCourse.Description.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                if (!await IsDuplicateCourseCodeAsync(newCode, id))
                {
                    _logger.LogError(exception, "Could not update course {CourseId}.", id);
                    throw;
                }

                ModelState.AddModelError("EditCourse.CourseCode", "Mayroon nang kursong gumagamit ng code na ito.");
                await LoadCoursesAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Na-update ang kurso. Awtomatikong makikita ang bagong detalye sa enrollment at student records.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var normalizedCourseCode = course.CourseCode.ToUpper();
            var normalizedCourseName = course.CourseName.ToUpper();
            var hasStudents = await _context.Students.AnyAsync(student =>
                student.CourseId == id
                || (student.CourseId == null
                    && student.CourseCode != null
                    && student.CourseCode.ToUpper() == normalizedCourseCode)
                || (student.CourseId == null
                    && student.Course != null
                    && (student.Course.ToUpper() == normalizedCourseCode
                        || student.Course.ToUpper() == normalizedCourseName)));
            if (hasStudents)
            {
                TempData["ErrorMessage"] = "Hindi matatanggal ang kursong ito dahil may mga student record na naka-link dito. I-edit ang kurso kung kailangang baguhin ang code, pangalan, o deskripsyon.";
                return RedirectToPage();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Naalis na ang kurso sa sistema.";
            return RedirectToPage();
        }

        private async Task LoadCoursesAsync()
        {
            CoursesList = await _context.Courses
                .AsNoTracking()
                .OrderBy(course => course.CourseCode)
                .ToListAsync();
        }

        private async Task<bool> CourseCodeExistsAsync(string code, int? exceptId = null)
        {
            var normalizedCode = code.ToUpperInvariant();
            return await _context.Courses.AnyAsync(course =>
                course.CourseCode.ToUpper() == normalizedCode
                && (!exceptId.HasValue || course.Id != exceptId.Value));
        }

        private async Task<bool> IsDuplicateCourseCodeAsync(string code, int? exceptId = null)
        {
            return await CourseCodeExistsAsync(code, exceptId);
        }

        private void ValidateCourse(AcademicCourse course, string prefix)
        {
            if (course.CourseCode.Length is < 2 or > 20)
            {
                ModelState.AddModelError($"{prefix}.CourseCode", "Dapat ay 2 hanggang 20 character ang course code.");
            }

            if (course.CourseName.Length is < 2 or > 150)
            {
                ModelState.AddModelError($"{prefix}.CourseName", "Dapat ay 2 hanggang 150 character ang course name.");
            }

            if (course.Description.Length > 2000)
            {
                ModelState.AddModelError($"{prefix}.Description", "Hindi maaaring lumampas sa 2,000 character ang description.");
            }
        }
    }
}
