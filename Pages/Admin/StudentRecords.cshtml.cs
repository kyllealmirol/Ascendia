using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages.Admin;

public sealed class StudentRecordsModel : PageModel
{
    private const int PageSize = 20;
    private readonly AppDbContext _context;

    public StudentRecordsModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CourseFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SectionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? YearFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? GraphYearAndSemester { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DetailsId { get; set; }

    public List<Student> Students { get; private set; } = [];
    public Student? Details { get; private set; }
    public List<SelectListItem> CourseOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];
    public List<SelectListItem> SectionOptions { get; private set; } = [];
    public List<SelectListItem> YearOptions { get; private set; } = [];
    public List<string> ChartLabels { get; private set; } = [];
    public List<int> ChartValues { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var courses = await _context.Courses.AsNoTracking().OrderBy(course => course.CourseCode)
            .ToListAsync(cancellationToken);
        CourseOptions = courses.Select(course =>
            new SelectListItem($"{course.CourseCode} — {course.CourseName}", course.CourseCode)).ToList();

        var filterData = await _context.Students.AsNoTracking()
            .Select(student => new { student.Status, student.Section, student.YearAndSemester })
            .ToListAsync(cancellationToken);
        StatusOptions = filterData.Select(item => item.Status).Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value)
            .Select(value => new SelectListItem(value!, value)).ToList();
        SectionOptions = filterData.Select(item => item.Section).Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value)
            .Select(value => new SelectListItem(value!, value)).ToList();
        YearOptions = filterData.Select(item => item.YearAndSemester).Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value)
            .Select(value => new SelectListItem(value!, value)).ToList();

        var students = _context.Students.AsNoTracking().Include(student => student.AcademicCourse).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            students = students.Where(student =>
                (student.FullName != null && student.FullName.Contains(search))
                || (student.StudentName != null && student.StudentName.Contains(search))
                || (student.StudentNumber != null && student.StudentNumber.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(CourseFilter))
        {
            students = students.Where(student =>
                student.CourseCode == CourseFilter || student.AcademicCourse!.CourseCode == CourseFilter);
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            students = students.Where(student => student.Status == StatusFilter);
        }

        if (!string.IsNullOrWhiteSpace(SectionFilter))
        {
            students = students.Where(student => student.Section == SectionFilter);
        }

        if (!string.IsNullOrWhiteSpace(YearFilter))
        {
            students = students.Where(student => student.YearAndSemester == YearFilter);
        }

        TotalCount = await students.CountAsync(cancellationToken);
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        Students = await students.OrderBy(student => student.StudentNumber)
            .ThenBy(student => student.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);

        var chartStudents = await _context.Students.AsNoTracking()
            .Where(student => student.Status != null
                && student.Status.Trim().ToUpper() == "ENROLLED")
            .Select(student => new { student.CourseCode, student.Course, student.YearAndSemester })
            .ToListAsync(cancellationToken);
        var chartData = chartStudents
            .Where(student => string.IsNullOrWhiteSpace(GraphYearAndSemester)
                || string.Equals(student.YearAndSemester, GraphYearAndSemester, StringComparison.OrdinalIgnoreCase))
            .GroupBy(student => string.IsNullOrWhiteSpace(student.CourseCode)
                ? student.Course ?? "Unspecified"
                : student.CourseCode)
            .OrderBy(group => group.Key)
            .ToList();
        ChartLabels = chartData.Select(group => group.Key).ToList();
        ChartValues = chartData.Select(group => group.Count()).ToList();

        if (DetailsId.HasValue)
        {
            Details = await _context.Students.AsNoTracking().Include(student => student.AcademicCourse)
                .SingleOrDefaultAsync(student => student.Id == DetailsId, cancellationToken);
        }
    }

    public object CurrentRouteValues(int page) => new
    {
        Search,
        CourseFilter,
        StatusFilter,
        SectionFilter,
        YearFilter,
        GraphYearAndSemester,
        PageNumber = page
    };
}
