using System.ComponentModel.DataAnnotations;

namespace Ascendia.Models;

public class AcademicRequest
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StudentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CurrentCourse { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string RequestType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ReasonCategory { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AdditionalDetails { get; set; }

    [Required]
    [MaxLength(16)]
    public string Status { get; set; } = "Pending";

    public DateTime DateFiledUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
