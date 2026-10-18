using System.ComponentModel.DataAnnotations;
using Ascendia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ascendia.Pages.Admin;

[Authorize(Policy = "RegistrarOnly")]
public sealed class TestEmailModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TestEmailModel> _logger;

    public TestEmailModel(
        IEmailService emailService,
        ILogger<TestEmailModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    [Required]
    [EmailAddress]
    public string? Recipient { get; set; }

    public bool EmailServiceConfigured => _emailService.IsConfigured;
    public bool EmailSent { get; private set; }
    public string? ResultMessage { get; private set; }

    public void OnGet(string? to)
    {
        Recipient = to;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!_emailService.IsConfigured)
        {
            ResultMessage = "Email was not sent. Configure Brevo__ApiKey and Brevo__SenderEmail.";
            _logger.LogWarning(
                "Registrar email test requested for {Recipient}, but Brevo is not configured.",
                Recipient);
            return Page();
        }

        _logger.LogInformation("Registrar requested an email delivery test to {Recipient}.", Recipient);
        try
        {
            await _emailService.SendTestEmailAsync(Recipient!, cancellationToken);
            EmailSent = true;
            ResultMessage = $"Brevo accepted the test email for {Recipient}. Check the recipient inbox and spam folder.";
            _logger.LogInformation("Email delivery test was accepted for {Recipient}.", Recipient);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email delivery test failed for {Recipient}.", Recipient);
            ResultMessage = $"Email test failed ({exception.GetType().Name}). Review the Railway application logs for details.";
        }

        return Page();
    }
}
