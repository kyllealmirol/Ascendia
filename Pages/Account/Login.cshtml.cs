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

namespace Ascendia.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<RegistrarAccount> _passwordHasher = new();

    public LoginModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var normalizedUsername = Input.Username.Trim().ToUpperInvariant();
        var account = await _dbContext.RegistrarAccounts
            .SingleOrDefaultAsync(candidate => candidate.NormalizedUsername == normalizedUsername);

        if (account is null || account.LockoutEndUtc > DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            Input.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            account.AccessFailedCount++;
            if (account.AccessFailedCount >= MaximumFailedAttempts)
            {
                account.AccessFailedCount = 0;
                account.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
            }

            await _dbContext.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, Input.Password);
        }

        account.AccessFailedCount = 0;
        account.LockoutEndUtc = null;
        await _dbContext.SaveChangesAsync();

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

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/AdminDashboard");
    }

    public class LoginInput
    {
        [Required]
        [StringLength(32)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
