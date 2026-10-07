using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

namespace Ascendia.Pages.Admin;

public class AcademicRequestsModel : PageModel
{
    private static readonly IReadOnlyDictionary<string, string> ApprovedStudentStatuses =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Drop"] = "Dropped",
            ["Stop"] = "Stopped",
            ["LOA"] = "LOA",
            ["Transfer"] = "Transferred"
        };

    private readonly AppDbContext _dbContext;

    public AcademicRequestsModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IList<AcademicRequestViewModel> RequestsList { get; private set; } =
        new List<AcademicRequestViewModel>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var requests = await _dbContext.AcademicRequests
            .AsNoTracking()
            .OrderByDescending(request => request.DateFiledUtc)
            .Select(request => new AcademicRequestViewModel
            {
                Id = request.Id,
                StudentNumber = request.StudentNumber,
                StudentName = request.StudentName,
                CurrentCourse = request.CurrentCourse,
                RequestType = request.RequestType,
                Details = request.AdditionalDetails == null
                    ? request.ReasonCategory
                    : request.ReasonCategory + " — " + request.AdditionalDetails,
                DateFiledUtc = request.DateFiledUtc,
                Status = request.Status
            })
            .ToListAsync(cancellationToken);

        RequestsList = requests;
    }

    public Task<IActionResult> OnPostApproveAsync(int id, CancellationToken cancellationToken)
    {
        return UpdateRequestStatusAsync(id, "Approved", cancellationToken);
    }

    public Task<IActionResult> OnPostRejectAsync(int id, CancellationToken cancellationToken)
    {
        return UpdateRequestStatusAsync(id, "Rejected", cancellationToken);
    }

    private async Task<IActionResult> UpdateRequestStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        var request = await _dbContext.AcademicRequests
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.Status == "Pending",
                cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        if (status == "Approved")
        {
            var requestType = request.RequestType.Trim();
            if (!ApprovedStudentStatuses.TryGetValue(requestType, out var studentStatus))
            {
                TempData["ErrorMessage"] =
                    $"Hindi maaprubahan ang request #{request.Id}: hindi suportado ang request type na '{request.RequestType}'.";
                return RedirectToPage();
            }

            var student = await _dbContext.Students
                .SingleOrDefaultAsync(
                    candidate => candidate.StudentNumber == request.StudentNumber,
                    cancellationToken);
            if (student is null || student.Id != request.StudentId)
            {
                TempData["ErrorMessage"] =
                    $"Hindi maaprubahan ang request #{request.Id}: hindi tugma o hindi makita ang student record.";
                return RedirectToPage();
            }

            if (!string.Equals(student.Status, "Enrolled", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    $"Hindi maaprubahan ang request #{request.Id}: ang kasalukuyang status ng estudyante ay '{student.Status}'.";
                return RedirectToPage();
            }

            student.Status = studentStatus;
        }

        request.Status = status;
        request.ReviewedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = status == "Approved"
            ? $"Naaprubahan ang request #{request.Id}; na-update ang status ni {request.StudentName} sa {ApprovedStudentStatuses[request.RequestType.Trim()]}."
            : $"Academic request #{request.Id} has been {status.ToLowerInvariant()}.";
        return RedirectToPage();
    }
}

public class AcademicRequestViewModel
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CurrentCourse { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime DateFiledUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}
