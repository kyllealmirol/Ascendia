using System.ComponentModel.DataAnnotations;

namespace Ascendia.Models;

public sealed class ShiftingCreditingApplication
{
    public int Id { get; set; }

    public int? StudentId { get; set; }
    public Student? Student { get; set; }

    [Required, MaxLength(32)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string StudentName { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string ApplicationType { get; set; } = "Course Shifting";

    [MaxLength(100)]
    public string? CurrentCourse { get; set; }

    [MaxLength(20)]
    public string? DesiredCourseCode { get; set; }

    [MaxLength(200)]
    public string? PreviousSchool { get; set; }

    public string? CreditedSubjects { get; set; }

    public string? Remarks { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
