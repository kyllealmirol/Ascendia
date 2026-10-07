using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Services;

var builder = WebApplication.CreateBuilder(args);

// I-register ang SQLite Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=smartenroll.db"));
builder.Services.AddScoped<StudentNumberService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.Name = "Ascendia.Registrar";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RegistrarOnly", policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole("Registrar"));
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RegistrarOnly");
    options.Conventions.AuthorizeFolder("/Registrar", "RegistrarOnly");
    options.Conventions.AuthorizePage("/AdminDashboard", "RegistrarOnly");
    options.Conventions.AuthorizePage("/Account/ChangeCredentials", "RegistrarOnly");
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var hasRegistrarAccount = await dbContext.RegistrarAccounts.AnyAsync();
    var initialUsername = app.Configuration["Registrar:InitialUsername"];
    var initialPassword = app.Configuration["Registrar:InitialPassword"];

    if (string.IsNullOrWhiteSpace(initialUsername) != string.IsNullOrWhiteSpace(initialPassword))
    {
        throw new InvalidOperationException(
            "Set both Registrar:InitialUsername and Registrar:InitialPassword to provision the initial registrar account.");
    }

    if (!hasRegistrarAccount
        && string.IsNullOrWhiteSpace(initialUsername)
        && string.IsNullOrWhiteSpace(initialPassword)
        && app.Environment.IsDevelopment())
    {
        initialUsername = "registrar";
        initialPassword = "Password123!";
        app.Logger.LogWarning(
            "Using the development-only default registrar credentials. Change the password before deploying this application.");
    }

    if (!hasRegistrarAccount && string.IsNullOrWhiteSpace(initialUsername))
    {
        throw new InvalidOperationException(
            "No registrar account exists. Set Registrar:InitialUsername and Registrar:InitialPassword to create the initial account.");
    }

    if (!hasRegistrarAccount)
    {
        var username = initialUsername!.Trim();
        if (username.Length is < 3 or > 32
            || !System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9._-]+$")
            || !RegistrarPasswordPolicy.IsStrong(initialPassword))
        {
            throw new InvalidOperationException(
                "The initial registrar username must be 3-32 letters, digits, dots, underscores, or hyphens; the password must be at least 12 characters and include uppercase, lowercase, numeric, and special characters.");
        }

        var account = new RegistrarAccount
        {
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        account.PasswordHash = new PasswordHasher<RegistrarAccount>()
            .HashPassword(account, initialPassword!);

        dbContext.RegistrarAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Provisioned the initial registrar account.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads")
        || context.Request.Path.StartsWithSegments("/private-uploads"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();