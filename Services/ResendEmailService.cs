using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ascendia.Models;
using Microsoft.Extensions.Options;

namespace Ascendia.Services;

public sealed class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
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
}

public sealed class ResendEmailService : IEmailService
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
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        HttpClient httpClient,
        IOptions<ResendSettings> options,
        ILogger<ResendEmailService> logger)
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
                "Resend API key is missing. Configure Resend:ApiKey.");
            _logger.LogError(
                exception,
                "Cannot send {EmailSubject} email for student {StudentNumber}: Resend API key is not configured.",
                subject,
                student.StudentNumber);
            throw exception;
        }

        if (string.IsNullOrWhiteSpace(_settings.From))
        {
            var exception = new InvalidOperationException(
                "Resend sender address is missing. Configure Resend:From with a verified sender address.");
            _logger.LogError(
                exception,
                "Cannot send {EmailSubject} email for student {StudentNumber}: Resend sender address is not configured.",
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
            "Sending {EmailSubject} email via Resend for student {StudentNumber} to {Recipient}.",
            subject,
            student.StudentNumber,
            student.Email);

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = JsonContent.Create(new ResendEmailRequest(
            _settings.From,
            student.Email,
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
                "Resend request failed for {EmailSubject} email for student {StudentNumber} to {Recipient}.",
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
                    $"Resend API returned {(int)response.StatusCode} ({response.StatusCode}).",
                    inner: null,
                    response.StatusCode);
                _logger.LogError(
                    exception,
                    "Resend rejected {EmailSubject} email for student {StudentNumber} to {Recipient}. Status: {StatusCode}. Response: {ResponseBody}",
                    subject,
                    student.StudentNumber,
                    student.Email,
                    (int)response.StatusCode,
                    responseBody);
                throw exception;
            }

            _logger.LogInformation(
                "Resend accepted {EmailSubject} email for student {StudentNumber} to {Recipient}. Status: {StatusCode}.",
                subject,
                student.StudentNumber,
                student.Email,
                (int)response.StatusCode);
        }
    }

    private sealed record ResendEmailRequest(
        string From,
        string To,
        string Subject,
        string Text);
}
