using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages.Admin;

public sealed class FacultyManagementModel : PageModel
{
    private readonly AppDbContext _context;

    public FacultyManagementModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Instructor InstructorForm { get; set; } = new();

    public Instructor? EditingInstructor { get; private set; }
    public List<Instructor> Instructors { get; private set; } = [];

    public async Task OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id.HasValue)
        {
            EditingInstructor = await _context.Instructors.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken);
            if (EditingInstructor is not null)
            {
                InstructorForm = EditingInstructor;
            }
        }

        await LoadInstructorsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        NormalizeForm();
        if (!IsAllowedStatus(InstructorForm.Status))
        {
            ModelState.AddModelError(nameof(InstructorForm.Status), "Status must be Active or Inactive.");
        }

        if (!ModelState.IsValid)
        {
            await LoadInstructorsAsync(cancellationToken);
            return Page();
        }

        _context.Instructors.Add(InstructorForm);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Naidagdag ang instructor.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, CancellationToken cancellationToken)
    {
        NormalizeForm();
        if (!IsAllowedStatus(InstructorForm.Status))
        {
            ModelState.AddModelError(nameof(InstructorForm.Status), "Status must be Active or Inactive.");
        }

        var instructor = await _context.Instructors
            .Include(item => item.SubjectSections)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (instructor is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            EditingInstructor = instructor;
            await LoadInstructorsAsync(cancellationToken);
            return Page();
        }

        instructor.FullName = InstructorForm.FullName;
        instructor.Department = InstructorForm.Department;
        instructor.Email = InstructorForm.Email;
        instructor.Status = InstructorForm.Status;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Na-update ang instructor.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var instructor = await _context.Instructors.FindAsync([id], cancellationToken);
        if (instructor is null)
        {
            return NotFound();
        }

        _context.Instructors.Remove(instructor);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Na-delete ang instructor. Nanatili ang mga subject section at inalis lamang ang assignment.";
        return RedirectToPage();
    }

    private void NormalizeForm()
    {
        InstructorForm.FullName = InstructorForm.FullName?.Trim() ?? string.Empty;
        InstructorForm.Department = InstructorForm.Department?.Trim() ?? string.Empty;
        InstructorForm.Email = InstructorForm.Email?.Trim() ?? string.Empty;
        InstructorForm.Status = InstructorForm.Status?.Trim() ?? string.Empty;
    }

    private static bool IsAllowedStatus(string? status) =>
        status is "Active" or "Inactive";

    private async Task LoadInstructorsAsync(CancellationToken cancellationToken)
    {
        Instructors = await _context.Instructors.AsNoTracking()
            .Include(item => item.SubjectSections)
            .OrderBy(item => item.FullName)
            .ToListAsync(cancellationToken);
    }
}
