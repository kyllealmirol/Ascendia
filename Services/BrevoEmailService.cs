using System.Net.Http.Json;
using Ascendia.Models;
using Microsoft.Extensions.Options;

namespace Ascendia.Services;

public sealed class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Ascendia Registrar";
}

public interface IEmailService
{
    bool IsConfigured { get; }

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

    Task SendTestEmailAsync(
        string recipient,
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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ApiKey)
        && !string.IsNullOrWhiteSpace(_settings.SenderEmail);

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
            $"Hello {student.FullName ?? student.StudentName},",
            "",
            "We have received your enrollment application.",
            $"Course / Department: {GetCourseName(student)}",
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
            $"Course / Department: {GetCourseName(student)}",
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
            $"Course / Department: {GetCourseName(student)}",
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

    public Task SendTestEmailAsync(
        string recipient,
        CancellationToken cancellationToken)
    {
        return SendEmailAsync(
            recipient,
            "Ascendia email delivery test",
            "This is a test email from the Ascendia Registrar Portal. The Brevo email configuration is working.",
            null,
            cancellationToken);
    }

    private async Task SendEmailAsync(
        Student student,
        string subject,
        string text,
        CancellationToken cancellationToken)
    {
        await SendEmailAsync(student.Email, subject, text, student.StudentNumber, cancellationToken);
    }

    private async Task SendEmailAsync(
        string? recipient,
        string subject,
        string text,
        string? studentNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            _logger.LogWarning(
                "Brevo email is not configured; skipping {EmailSubject} email to {Recipient}. Set Brevo:ApiKey and Brevo:SenderEmail.",
                subject,
                recipient);
            return;
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning(
                "Cannot send {EmailSubject} email for student {StudentNumber}: recipient address is empty.",
                subject,
                studentNumber);
            return;
        }

        _logger.LogInformation(
            "Sending {EmailSubject} email via Brevo to {Recipient} for student {StudentNumber}.",
            subject,
            recipient,
            studentNumber);

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(new BrevoEmailRequest(
            new BrevoSender(_settings.SenderName, _settings.SenderEmail),
            [new BrevoRecipient(recipient)],
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
                "Brevo request failed for {EmailSubject} email to {Recipient} for student {StudentNumber}.",
                subject,
                recipient,
                studentNumber);
            throw;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrEmpty(_settings.ApiKey))
                {
                    responseBody = responseBody.Replace(
                        _settings.ApiKey,
                        "[REDACTED]",
                        StringComparison.Ordinal);
                }

                if (responseBody.Length > 2000)
                {
                    responseBody = responseBody[..2000];
                }

                var exception = new HttpRequestException(
                    $"Brevo API returned {(int)response.StatusCode} ({response.StatusCode}). Response: {responseBody}",
                    inner: null,
                    response.StatusCode);
                _logger.LogError(
                    exception,
                    "Brevo rejected {EmailSubject} email to {Recipient} for student {StudentNumber}. Status: {StatusCode}.",
                    subject,
                    recipient,
                    studentNumber,
                    (int)response.StatusCode);
                throw exception;
            }

            _logger.LogInformation(
                "Brevo accepted {EmailSubject} email to {Recipient} for student {StudentNumber}. Status: {StatusCode}.",
                subject,
                recipient,
                studentNumber,
                (int)response.StatusCode);
        }
    }

    private static string GetCourseName(Student student) =>
        string.IsNullOrWhiteSpace(student.Course)
            ? student.CourseCode ?? "Not specified"
            : student.Course;

    private sealed record BrevoEmailRequest(
        BrevoSender Sender,
        IReadOnlyList<BrevoRecipient> To,
        string Subject,
        string TextContent);

    private sealed record BrevoSender(string Name, string Email);

    private sealed record BrevoRecipient(string Email);
}
