using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages.Admin;

public sealed class ShiftingCreditingModel : PageModel
{
    private static readonly string[] ApplicationTypes =
        ["Course Shifting", "Credit Transfer", "Subject Evaluation"];
    private static readonly string[] Statuses =
        ["Pending", "For Evaluation", "Approved", "Rejected"];

    private readonly AppDbContext _context;
    private readonly ILogger<ShiftingCreditingModel> _logger;

    public ShiftingCreditingModel(
        AppDbContext context,
        ILogger<ShiftingCreditingModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public ShiftingCreditingApplication NewApplication { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public List<ShiftingCreditingApplication> Applications { get; private set; } = [];
    public List<SelectListItem> Students { get; private set; } = [];
    public List<SelectListItem> Courses { get; private set; } = [];
    public IReadOnlyList<string> ApplicationTypeOptions => ApplicationTypes;
    public IReadOnlyList<string> StatusOptions => Statuses;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        NewApplication.StudentNumber = NewApplication.StudentNumber.Trim();
        NewApplication.ApplicationType = NewApplication.ApplicationType.Trim();
        NewApplication.CreditedSubjects = CleanMultiline(NewApplication.CreditedSubjects);
        NewApplication.Remarks = CleanMultiline(NewApplication.Remarks);

        if (!ApplicationTypes.Contains(NewApplication.ApplicationType, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(NewApplication.ApplicationType), "Pumili ng valid na application type.");
        }

        var student = await _context.Students.SingleOrDefaultAsync(
            item => item.StudentNumber == NewApplication.StudentNumber,
            cancellationToken);
        if (student is null)
        {
            ModelState.AddModelError(nameof(NewApplication.StudentNumber), "Hindi makita ang estudyante.");
        }

        AcademicCourse? desiredCourse = null;
        if (string.Equals(NewApplication.ApplicationType, "Course Shifting", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(NewApplication.DesiredCourseCode))
        {
            var normalizedCode = NewApplication.DesiredCourseCode.Trim().ToUpperInvariant();
            desiredCourse = await _context.Courses.AsNoTracking().SingleOrDefaultAsync(
                course => course.CourseCode.ToUpper() == normalizedCode,
                cancellationToken);
            if (desiredCourse is null)
            {
                ModelState.AddModelError(nameof(NewApplication.DesiredCourseCode), "Pumili ng available na kurso.");
            }
        }

        if (string.Equals(NewApplication.ApplicationType, "Credit Transfer", StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(NewApplication.CreditedSubjects))
        {
            ModelState.AddModelError(nameof(NewApplication.CreditedSubjects), "Ilagay ang mga subject na ipapa-credit.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        NewApplication.StudentId = student!.Id;
        NewApplication.StudentName = student.FullName ?? student.StudentName ?? string.Empty;
        NewApplication.CurrentCourse = student.Course ?? student.CourseCode;
        NewApplication.DesiredCourseCode = desiredCourse?.CourseCode;
        NewApplication.Status = "Pending";
        NewApplication.SubmittedAtUtc = DateTime.UtcNow;
        _context.ShiftingCreditingApplications.Add(NewApplication);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Naidagdag ang evaluation application.";
        return RedirectToPage(new { TypeFilter, StatusFilter });
    }

    public async Task<IActionResult> OnPostEvaluateAsync(
        int id,
        string? status,
        string? remarks,
        CancellationToken cancellationToken)
    {
        status = status?.Trim();
        remarks = remarks?.Trim();
        if (status is null || !Statuses.Contains(status, StringComparer.Ordinal))
        {
            return BadRequest("Invalid application status.");
        }

        var application = await _context.ShiftingCreditingApplications
            .Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (status == "Approved"
            && application.ApplicationType == "Course Shifting"
            && application.Student is not null
            && !string.IsNullOrWhiteSpace(application.DesiredCourseCode))
        {
            var normalizedCode = application.DesiredCourseCode.ToUpperInvariant();
            var course = await _context.Courses.SingleOrDefaultAsync(
                item => item.CourseCode.ToUpper() == normalizedCode,
                cancellationToken);
            if (course is null)
            {
                ModelState.AddModelError(string.Empty, "Hindi na available ang kursong hinihingi.");
                await LoadPageAsync(cancellationToken);
                return Page();
            }

            application.Student.CourseId = course.Id;
            application.Student.CourseCode = course.CourseCode;
            application.Student.Course = course.CourseName;
        }

        application.Status = status;
        application.Remarks = remarks;
        application.ReviewedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Shifting/credit application {ApplicationId} updated to {Status} by registrar.",
            application.Id,
            application.Status);
        TempData["SuccessMessage"] = $"Na-update ang application sa {status}.";
        return RedirectToPage(new { TypeFilter, StatusFilter });
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        var query = _context.ShiftingCreditingApplications
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(TypeFilter)
            && ApplicationTypes.Contains(TypeFilter, StringComparer.Ordinal))
        {
            query = query.Where(item => item.ApplicationType == TypeFilter);
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter)
            && Statuses.Contains(StatusFilter, StringComparer.Ordinal))
        {
            query = query.Where(item => item.Status == StatusFilter);
        }

        Applications = await query
            .OrderByDescending(item => item.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        Students = await _context.Students
            .AsNoTracking()
            .OrderBy(item => item.StudentNumber)
            .Select(item => new SelectListItem(
                (item.StudentNumber ?? "No number") + " — " + (item.FullName ?? item.StudentName ?? "Student"),
                item.StudentNumber ?? string.Empty))
            .ToListAsync(cancellationToken);
        Courses = await _context.Courses.AsNoTracking()
            .OrderBy(item => item.CourseCode)
            .Select(item => new SelectListItem(item.CourseCode + " — " + item.CourseName, item.CourseCode))
            .ToListAsync(cancellationToken);
    }

    private static string? CleanMultiline(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
