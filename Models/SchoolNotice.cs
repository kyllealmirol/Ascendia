using System.ComponentModel.DataAnnotations;

namespace Ascendia.Models;

public sealed class SchoolNotice
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    [Required, MaxLength(32)]
    public string Category { get; set; } = "Notice";

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public DateTime DatePostedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDateUtc { get; set; }
    public bool IsPinned { get; set; }
    public bool IsActive { get; set; } = true;
}
