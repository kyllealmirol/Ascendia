using System.Net.Http.Json;
using Ascendia.Models;
using Microsoft.Extensions.Options;

namespace Ascendia.Services;

public sealed class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Ascendia Registrar";
}

public interface IEmailService
{
    Task SendEnrollmentConfirmationAsync(
        Student student,
        CancellationToken cancellationToken);

    Task SendRejectionNotificationAsync(
        Student student,
        string? rejectionReason,
        CancellationToken cancellationToken);

    Task SendApprovalNotificationAsync(
        Student student,
        CancellationToken cancellationToken);
}

public sealed class BrevoEmailService : IEmailService
{
    private static readonly string[] CollegeRequirements =
    [
        "Form 138 (Report Card)",
        "Form 137",
        "Certificate of Good Moral",
        "PSA Authenticated Birth Certificate",
        "Passport Size ID Picture (White Background, Formal Attire) - 2pcs.",
        "Barangay Clearance"
    ];

    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient httpClient,
        IOptions<BrevoSettings> options,
        ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public Task SendEnrollmentConfirmationAsync(
        Student student,
        CancellationToken cancellationToken)
    {
        var requirements = string.Join(
            Environment.NewLine,
            CollegeRequirements.Select(requirement => $"- {requirement}"));
        var body = string.Join(
            Environment.NewLine,
            $"Hello {student.FullName},",
            "",
            "We have received your enrollment application.",
            $"Course / Department: {student.Course}",
            $"Student Number: {student.StudentNumber}",
            "",
            "Please prepare or submit these College Requirements to the Registrar:",
            requirements,
            "",
            "Please contact the Registrar if you have questions about your requirements.");

        return SendEmailAsync(
            student,
            "Enrollment application confirmation",
            body,
            cancellationToken);
    }

    public Task SendApprovalNotificationAsync(
        Student student,
        CancellationToken cancellationToken)
    {
        var body = string.Join(
            Environment.NewLine,
            $"Hello {student.FullName ?? student.StudentName},",
            "",
            "Your enrollment application has been approved.",
            $"Course / Department: {student.Course}",
            $"Student Number: {student.StudentNumber}",
            $"Section: {student.Section}",
            "",
            "Please contact the Registrar if you have questions.");

        return SendEmailAsync(
            student,
            "Enrollment application approved",
            body,
            cancellationToken);
    }

    public Task SendRejectionNotificationAsync(
        Student student,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        var body = string.Join(
            Environment.NewLine,
            $"Hello {student.FullName ?? student.StudentName},",
            "",
            "We regret to inform you that your enrollment application has been rejected.",
            $"Course / Department: {student.Course}",
            $"Student Number: {student.StudentNumber}",
            "",
            "Reason / Remarks:",
            string.IsNullOrWhiteSpace(rejectionReason)
                ? "Please contact the Registrar for more information."
                : rejectionReason.Trim());

        return SendEmailAsync(
            student,
            "Enrollment application status",
            body,
            cancellationToken);
    }

    private async Task SendEmailAsync(
        Student student,
        string subject,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            var exception = new InvalidOperationException(
                "Brevo API key is missing. Configure Brevo:ApiKey.");
            _logger.LogError(
                exception,
                "Cannot send {EmailSubject} email for student {StudentNumber}: Brevo API key is not configured.",
                subject,
                student.StudentNumber);
            throw exception;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            var exception = new InvalidOperationException(
                "Brevo sender address is missing. Configure Brevo:FromEmail with a verified sender address.");
            _logger.LogError(
                exception,
                "Cannot send {EmailSubject} email for student {StudentNumber}: Brevo sender address is not configured.",
                subject,
                student.StudentNumber);
            throw exception;
        }

        if (string.IsNullOrWhiteSpace(student.Email))
        {
            var exception = new InvalidOperationException(
                "A student email address is required to send this notification.");
            _logger.LogError(
                exception,
                "Cannot send {EmailSubject} email for student {StudentNumber}: recipient address is empty.",
                subject,
                student.StudentNumber);
            throw exception;
        }

        _logger.LogInformation(
            "Sending {EmailSubject} email via Brevo for student {StudentNumber} to {Recipient}.",
            subject,
            student.StudentNumber,
            student.Email);

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new BrevoEmailRequest(
            new BrevoSender(_settings.FromName, _settings.FromEmail),
            [new BrevoRecipient(student.Email)],
            subject,
            text));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Brevo request failed for {EmailSubject} email for student {StudentNumber} to {Recipient}.",
                subject,
                student.StudentNumber,
                student.Email);
            throw;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var exception = new HttpRequestException(
                    $"Brevo API returned {(int)response.StatusCode} ({response.StatusCode}).",
                    inner: null,
                    response.StatusCode);
                _logger.LogError(
                    exception,
                    "Brevo rejected {EmailSubject} email for student {StudentNumber} to {Recipient}. Status: {StatusCode}. Response: {ResponseBody}",
                    subject,
                    student.StudentNumber,
                    student.Email,
                    (int)response.StatusCode,
                    responseBody);
                throw exception;
            }

            _logger.LogInformation(
                "Brevo accepted {EmailSubject} email for student {StudentNumber} to {Recipient}. Status: {StatusCode}.",
                subject,
                student.StudentNumber,
                student.Email,
                (int)response.StatusCode);
        }
    }

    private sealed record BrevoEmailRequest(
        BrevoSender Sender,
        IReadOnlyList<BrevoRecipient> To,
        string Subject,
        string TextContent);

    private sealed record BrevoSender(string Name, string Email);

    private sealed record BrevoRecipient(string Email);
}
