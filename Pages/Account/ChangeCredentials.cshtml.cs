using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Services;

namespace Ascendia.Pages.Account;

[Authorize(Policy = "RegistrarOnly")]
public class ChangeCredentialsModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ChangeCredentialsModel> _logger;
    private readonly PasswordHasher<RegistrarAccount> _passwordHasher = new();

    public ChangeCredentialsModel(
        AppDbContext dbContext,
        ILogger<ChangeCredentialsModel> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [BindProperty]
    public CredentialsInput Input { get; set; } = new();

    public string CurrentUsername { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await GetCurrentAccountAsync();
        if (account is null)
        {
            return Challenge();
        }

        CurrentUsername = account.Username;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await GetCurrentAccountAsync();
        if (account is null)
        {
            return Challenge();
        }

        var newUsername = Input.NewUsername?.Trim();
        var newPassword = Input.NewPassword;
        var changeUsername = !string.IsNullOrWhiteSpace(newUsername);
        var changePassword = !string.IsNullOrEmpty(newPassword);
        if (!changeUsername && !changePassword)
        {
            ModelState.AddModelError(string.Empty, "Enter a new username, a new password, or both.");
        }

        if (changePassword)
        {
            if (!RegistrarPasswordPolicy.IsStrong(newPassword))
            {
                ModelState.AddModelError(
                    "Input.NewPassword",
                    $"Use at least {RegistrarPasswordPolicy.MinimumLength} characters, including uppercase and lowercase letters, a number, and a symbol.");
            }
        }
        else if (!string.IsNullOrEmpty(Input.ConfirmPassword))
        {
            ModelState.AddModelError("Input.ConfirmPassword", "Enter a new password before confirming it.");
        }

        if (!ModelState.IsValid)
        {
            CurrentUsername = account.Username;
            return Page();
        }

        if (_passwordHasher.VerifyHashedPassword(account, account.PasswordHash, Input.CurrentPassword)
            == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("Input.CurrentPassword", "The current password is incorrect.");
            CurrentUsername = account.Username;
            return Page();
        }

        var currentUsername = account.Username;
        if (!string.IsNullOrWhiteSpace(newUsername))
        {
            var normalizedUsername = newUsername.ToUpperInvariant();
            if (!string.Equals(normalizedUsername, account.NormalizedUsername, StringComparison.Ordinal))
            {
                var usernameExists = await _dbContext.RegistrarAccounts.AnyAsync(candidate =>
                    candidate.Id != account.Id && candidate.NormalizedUsername == normalizedUsername);
                if (usernameExists)
                {
                    ModelState.AddModelError("Input.NewUsername", "That username is already in use.");
                    CurrentUsername = currentUsername;
                    return Page();
                }

                account.Username = newUsername;
                account.NormalizedUsername = normalizedUsername;
            }
        }

        if (!string.IsNullOrEmpty(newPassword))
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, newPassword);
        }

        account.UpdatedAtUtc = DateTime.UtcNow;
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update credentials for registrar account {AccountId}.", account.Id);
            ModelState.AddModelError(string.Empty, "The credentials could not be saved. Please try again.");
            CurrentUsername = currentUsername;
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, "Registrar")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        TempData["StatusMessage"] = "Account credentials updated.";
        return RedirectToPage();
    }

    private async Task<RegistrarAccount?> GetCurrentAccountAsync()
    {
        var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(accountIdValue, out var accountId)
            ? await _dbContext.RegistrarAccounts.SingleOrDefaultAsync(account => account.Id == accountId)
            : null;
    }

    public class CredentialsInput
    {
        [Required]
        [DataType(DataType.Password)]
        [StringLength(256)]
        public string CurrentPassword { get; set; } = string.Empty;

        [StringLength(32, MinimumLength = 3)]
        [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Use only letters, numbers, dots, underscores, or hyphens.")]
        public string? NewUsername { get; set; }

        [DataType(DataType.Password)]
        [StringLength(256)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
