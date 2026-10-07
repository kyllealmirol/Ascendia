using System.ComponentModel.DataAnnotations;

namespace Ascendia.Data;

public class RegistrarAccount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string NormalizedUsername { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
