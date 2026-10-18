using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages;

public sealed class EnrollmentModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public EnrollmentModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<AcademicCourse> Courses { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Courses = await _dbContext.Courses
            .AsNoTracking()
            .OrderBy(course => course.CourseCode)
            .ToListAsync(cancellationToken);
    }
}
