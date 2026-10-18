using System.ComponentModel.DataAnnotations;

namespace Ascendia.Models;

public sealed class Instructor
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string Status { get; set; } = "Active";

    public ICollection<SubjectSection> SubjectSections { get; set; } = new List<SubjectSection>();
}
