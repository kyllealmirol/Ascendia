using System.Net;
using System.Net.Mail;
using System.Text;
using Ascendia.Models;
using Microsoft.Extensions.Options;

namespace Ascendia.Services;

public sealed class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public interface IEnrollmentEmailSender
{
    Task SendEnrollmentConfirmationAsync(
        Student student,
        CancellationToken cancellationToken);

    Task SendRejectionNotificationAsync(
        Student student,
        string? rejectionReason,
        CancellationToken cancellationToken);
}

public sealed class EnrollmentEmailSender : IEnrollmentEmailSender
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

    private readonly SmtpSettings _settings;

    public EnrollmentEmailSender(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEnrollmentConfirmationAsync(
        Student student,
        CancellationToken cancellationToken)
    {
        ValidateSmtpSettings();

        if (string.IsNullOrWhiteSpace(student.Email))
        {
            throw new InvalidOperationException(
                "A student email address is required to send the enrollment confirmation.");
        }

        var requirements = string.Join(
            Environment.NewLine,
            CollegeRequirements.Select(requirement => $"- {requirement}"));
        var body = new StringBuilder()
            .AppendLine($"Hello {student.FullName},")
            .AppendLine()
            .AppendLine("We have received your enrollment application.")
            .AppendLine($"Course / Department: {student.Course}")
            .AppendLine($"Student Number: {student.StudentNumber}")
            .AppendLine()
            .AppendLine("Please prepare or submit these College Requirements to the Registrar:")
            .AppendLine(requirements)
            .AppendLine()
            .Append("Please contact the Registrar if you have questions about your requirements.")
            .ToString();

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.Email, "Ascendia Student Enrollment"),
            Subject = "Enrollment application confirmation",
            Body = body,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(student.Email));

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.Email, _settings.Password)
        };

        await SendEmailAsync(client, message, cancellationToken);
    }

    public async Task SendRejectionNotificationAsync(
        Student student,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        ValidateSmtpSettings();

        if (string.IsNullOrWhiteSpace(student.Email))
        {
            throw new InvalidOperationException(
                "A student email address is required to send the rejection notification.");
        }

        var body = new StringBuilder()
            .AppendLine($"Hello {student.FullName ?? student.StudentName},")
            .AppendLine()
            .AppendLine("We regret to inform you that your enrollment application has been rejected.")
            .AppendLine($"Course / Department: {student.Course}")
            .AppendLine($"Student Number: {student.StudentNumber}")
            .AppendLine()
            .AppendLine("Reason / Remarks:")
            .AppendLine(string.IsNullOrWhiteSpace(rejectionReason)
                ? "Please contact the Registrar for more information."
                : rejectionReason.Trim())
            .ToString();

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.Email, "Ascendia Student Enrollment"),
            Subject = "Enrollment application status",
            Body = body,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(student.Email));

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.Email, _settings.Password)
        };

        await SendEmailAsync(client, message, cancellationToken);
    }

    private static async Task SendEmailAsync(
        SmtpClient client,
        MailMessage message,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"[SMTP] Sending email. Host: {client.Host}, Port: {client.Port}, SSL: {client.EnableSsl}, To: {string.Join(", ", message.To.Select(address => address.Address))}, Subject: {message.Subject}");

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            Console.WriteLine(
                $"[SMTP] Email sent successfully. Host: {client.Host}, Port: {client.Port}, To: {string.Join(", ", message.To.Select(address => address.Address))}, Subject: {message.Subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[SMTP FULL ERROR]: {ex.GetType().FullName} - {ex.Message} | Inner: {ex.InnerException?.Message}");
            Console.WriteLine($"[SMTP FULL EXCEPTION DETAILS]: {ex}");
            throw;
        }
    }

    private void ValidateSmtpSettings()
    {
        if (_settings.Port is < 1 or > 65535)
        {
            throw new SmtpException(
                $"Invalid SMTP port {_settings.Port}. Configure SmtpSettings:Port with a value between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Email)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new SmtpException(
                "SMTP sender credentials are missing. Configure SmtpSettings:Email and SmtpSettings:Password, or set EMAIL_USER and EMAIL_PASS environment variables.");
        }
    }
}
