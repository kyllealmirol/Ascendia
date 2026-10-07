using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

namespace Ascendia.Services;

public sealed class StudentNumberService
{
    private const int MaximumGenerationAttempts = 100;
    private const int MaximumSaveAttempts = 5;

    private readonly AppDbContext _dbContext;

    public StudentNumberService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaximumGenerationAttempts; attempt++)
        {
            var candidate = CreateCandidate();
            var alreadyExists = await _dbContext.Students
                .AsNoTracking()
                .AnyAsync(student => student.StudentNumber == candidate, cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not generate a unique student number after multiple attempts.");
    }

    public async Task<string> AddStudentWithUniqueNumberAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Students.Add(student);

        for (var attempt = 0; attempt < MaximumSaveAttempts; attempt++)
        {
            student.StudentNumber = await GenerateUniqueAsync(cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return student.StudentNumber;
            }
            catch (DbUpdateException)
            {
                var duplicateNumberExists = await _dbContext.Students
                    .AsNoTracking()
                    .AnyAsync(
                        existing => existing.StudentNumber == student.StudentNumber
                            && existing.Id != student.Id,
                        cancellationToken);

                if (!duplicateNumberExists)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException(
            "Could not save the student because a unique student number could not be assigned.");
    }

    public async Task<bool> AddStudentWithProvidedNumberAsync(
        Student student,
        string studentNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = studentNumber.Trim();
        var alreadyExists = await _dbContext.Students
            .AsNoTracking()
            .AnyAsync(existing => existing.StudentNumber == normalizedNumber, cancellationToken);

        if (alreadyExists)
        {
            return false;
        }

        student.StudentNumber = normalizedNumber;
        _dbContext.Students.Add(student);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            var duplicateNumberExists = await _dbContext.Students
                .AsNoTracking()
                .AnyAsync(
                    existing => existing.StudentNumber == normalizedNumber
                        && existing.Id != student.Id,
                    cancellationToken);

            if (!duplicateNumberExists)
            {
                throw;
            }

            _dbContext.Entry(student).State = EntityState.Detached;
            return false;
        }
    }

    private static string CreateCandidate()
    {
        var yearPrefix = DateTime.Now.Year % 100;
        var randomDigits = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return $"{yearPrefix:D2}0{randomDigits:D6}";
    }
}
