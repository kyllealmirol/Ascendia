using Ascendia.Data;
using Ascendia.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ascendia.Pages.Admin;

public sealed class NoticesModel : PageModel
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> Categories =
        ["Notice", "Enrollment Schedule", "Alert"];

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NoticesModel> _logger;

    public NoticesModel(
        AppDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<NoticesModel> logger)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    [BindProperty]
    public SchoolNotice NoticeForm { get; set; } = new();

    [BindProperty]
    public IFormFile? NoticeImage { get; set; }

    public List<SchoolNotice> Notices { get; private set; } = [];
    public SchoolNotice? EditingNotice { get; private set; }
    public IReadOnlyCollection<string> CategoryOptions => Categories;

    public async Task OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id.HasValue)
        {
            EditingNotice = await _context.Notices.AsNoTracking()
                .SingleOrDefaultAsync(notice => notice.Id == id, cancellationToken);
            if (EditingNotice is not null)
            {
                NoticeForm = EditingNotice;
            }
        }

        await LoadNoticesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        ValidateForm(hasExistingImage: false);
        var imageValidation = await ValidateImageAsync(NoticeImage, cancellationToken);
        if (imageValidation is not null)
        {
            ModelState.AddModelError(nameof(NoticeImage), imageValidation);
        }

        if (!ModelState.IsValid)
        {
            await LoadNoticesAsync(cancellationToken);
            return Page();
        }

        NoticeForm.Title = NoticeForm.Title.Trim();
        NoticeForm.Message = CleanMessage(NoticeForm.Message);
        NoticeForm.DatePostedUtc = DateTime.UtcNow;
        if (NoticeImage is not null)
        {
            NoticeForm.ImagePath = await SaveImageAsync(NoticeImage, cancellationToken);
        }

        _context.Notices.Add(NoticeForm);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Nailathala ang notice.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        bool removeImage,
        CancellationToken cancellationToken)
    {
        var notice = await _context.Notices
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (notice is null)
        {
            return NotFound();
        }

        ValidateForm(hasExistingImage: notice.ImagePath is not null && !removeImage);
        var imageValidation = await ValidateImageAsync(NoticeImage, cancellationToken);
        if (imageValidation is not null)
        {
            ModelState.AddModelError(nameof(NoticeImage), imageValidation);
        }

        if (!ModelState.IsValid)
        {
            EditingNotice = notice;
            await LoadNoticesAsync(cancellationToken);
            return Page();
        }

        var oldImagePath = notice.ImagePath;
        var newImagePath = NoticeImage is not null
            ? await SaveImageAsync(NoticeImage, cancellationToken)
            : null;

        notice.Title = NoticeForm.Title.Trim();
        notice.Message = CleanMessage(NoticeForm.Message);
        notice.Category = NoticeForm.Category;
        notice.ExpiryDateUtc = NoticeForm.ExpiryDateUtc?.Date;
        notice.IsPinned = NoticeForm.IsPinned;
        notice.IsActive = NoticeForm.IsActive;
        if (newImagePath is not null)
        {
            notice.ImagePath = newImagePath;
        }
        else if (removeImage)
        {
            notice.ImagePath = null;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (newImagePath is not null)
            {
                DeleteStoredImage(newImagePath);
            }

            throw;
        }

        if (newImagePath is not null || removeImage)
        {
            DeleteStoredImage(oldImagePath);
        }

        TempData["SuccessMessage"] = "Na-update ang notice.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var notice = await _context.Notices.FindAsync([id], cancellationToken);
        if (notice is null)
        {
            return NotFound();
        }

        var imagePath = notice.ImagePath;
        _context.Notices.Remove(notice);
        await _context.SaveChangesAsync(cancellationToken);
        DeleteStoredImage(imagePath);
        TempData["SuccessMessage"] = "Na-delete ang notice.";
        return RedirectToPage();
    }

    private void ValidateForm(bool hasExistingImage)
    {
        NoticeForm.Title = NoticeForm.Title?.Trim() ?? string.Empty;
        NoticeForm.Category = NoticeForm.Category?.Trim() ?? string.Empty;
        if (!Categories.Contains(NoticeForm.Category))
        {
            ModelState.AddModelError(nameof(NoticeForm.Category), "Pumili ng valid na category.");
        }

        if (string.IsNullOrWhiteSpace(NoticeForm.Message)
            && NoticeImage is null
            && !hasExistingImage)
        {
            ModelState.AddModelError(string.Empty, "Kailangan ng message o announcement image.");
        }
    }

    private static async Task<string?> ValidateImageAsync(
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        if (image is null)
        {
            return null;
        }

        if (image.Length == 0 || image.Length > MaxImageBytes)
        {
            return "Dapat ay may laman ang image at hindi lalampas sa 5 MB.";
        }

        var extension = Path.GetExtension(image.FileName);
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
            .Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return "JPG, PNG, o WEBP lamang ang puwedeng i-upload.";
        }

        var signature = new byte[12];
        await using var stream = image.OpenReadStream();
        var read = await stream.ReadAsync(signature, cancellationToken);
        if (!HasImageSignature(signature, read, extension.ToLowerInvariant()))
        {
            return "Hindi tugma ang file content sa JPG, PNG, o WEBP extension.";
        }

        return null;
    }

    private async Task<string> SaveImageAsync(IFormFile image, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var signature = new byte[12];
        await using (var stream = image.OpenReadStream())
        {
            var read = await stream.ReadAsync(signature, cancellationToken);
            if (!HasImageSignature(signature, read, extension))
            {
                throw new InvalidDataException("Uploaded image content does not match its extension.");
            }
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = GetNoticeUploadsDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await image.CopyToAsync(output, cancellationToken);
        return $"/notice-images/{fileName}";
    }

    private string GetNoticeUploadsDirectory()
    {
        var configuredPath = _configuration["NoticeUploads:Path"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_environment.ContentRootPath, configuredPath);
        }

        return _environment.IsDevelopment()
            ? Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "notices")
            : "/data/uploads/notices";
    }

    private static bool HasImageSignature(byte[] bytes, int count, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => count >= 3
                && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => count >= 8
                && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => count >= 12
                && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private void DeleteStoredImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var fileName = Path.GetFileName(imagePath);
        if (!string.Equals(imagePath, $"/notice-images/{fileName}", StringComparison.Ordinal))
        {
            _logger.LogWarning("Skipped deletion of unexpected notice image path {ImagePath}.", imagePath);
            return;
        }

        var path = Path.Combine(GetNoticeUploadsDirectory(), fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    private async Task LoadNoticesAsync(CancellationToken cancellationToken)
    {
        Notices = await _context.Notices.AsNoTracking()
            .OrderByDescending(notice => notice.IsPinned)
            .ThenByDescending(notice => notice.DatePostedUtc)
            .ToListAsync(cancellationToken);
    }

    private static string? CleanMessage(string? message) =>
        string.IsNullOrWhiteSpace(message) ? null : message.Trim();
}
