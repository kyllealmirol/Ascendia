using System.ComponentModel.DataAnnotations;

namespace Ascendia.Models;

public sealed class SubjectSection
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string SubjectCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string SubjectName { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string SectionName { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string YearLevel { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Semester { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Units { get; set; }

    [MaxLength(100)]
    public string? Schedule { get; set; }

    [MaxLength(100)]
    public string? Room { get; set; }

    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }
}
