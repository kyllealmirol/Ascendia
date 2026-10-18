using System.Threading.Channels;
using Ascendia.Models;

namespace Ascendia.Services;

public enum EnrollmentEmailType
{
    Confirmation,
    Rejection
}

public sealed record EnrollmentEmailJob(
    EnrollmentEmailType Type,
    Student Student,
    string? RejectionReason);

public sealed class EnrollmentEmailQueue
{
    private readonly Channel<EnrollmentEmailJob> _channel =
        Channel.CreateBounded<EnrollmentEmailJob>(
            new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    public bool TryQueueConfirmation(Student student)
    {
        return _channel.Writer.TryWrite(
            new EnrollmentEmailJob(
                EnrollmentEmailType.Confirmation,
                CreateStudentSnapshot(student),
                null));
    }

    public bool TryQueueRejection(Student student, string? rejectionReason)
    {
        return _channel.Writer.TryWrite(
            new EnrollmentEmailJob(
                EnrollmentEmailType.Rejection,
                CreateStudentSnapshot(student),
                rejectionReason));
    }

    public IAsyncEnumerable<EnrollmentEmailJob> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    private static Student CreateStudentSnapshot(Student student)
    {
        return new Student
        {
            StudentNumber = student.StudentNumber,
            FullName = student.FullName,
            StudentName = student.StudentName,
            Course = student.Course,
            Email = student.Email
        };
    }
}

public sealed class EnrollmentEmailWorker : BackgroundService
{
    private readonly EnrollmentEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentEmailWorker> _logger;

    public EnrollmentEmailWorker(
        EnrollmentEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentEmailWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSender = scope.ServiceProvider
                        .GetRequiredService<IEnrollmentEmailSender>();
                    using var sendTimeout = CancellationTokenSource
                        .CreateLinkedTokenSource(stoppingToken);
                    sendTimeout.CancelAfter(TimeSpan.FromSeconds(30));

                    if (job.Type == EnrollmentEmailType.Confirmation)
                    {
                        await emailSender.SendEnrollmentConfirmationAsync(
                            job.Student,
                            sendTimeout.Token);
                    }
                    else
                    {
                        await emailSender.SendRejectionNotificationAsync(
                            job.Student,
                            job.RejectionReason,
                            sendTimeout.Token);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to send background {EmailType} email for student {StudentNumber} to {Email}.",
                        job.Type,
                        job.Student.StudentNumber,
                        job.Student.Email);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
    }
}
