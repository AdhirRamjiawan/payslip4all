using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Infrastructure.Auth;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Web.Auth;
using Payslip4All.Infrastructure.Persistence.Repositories;
using Payslip4All.Infrastructure.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF community licence
QuestPDF.Settings.License = LicenseType.Community;

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Database provider switching
var provider = builder.Configuration["DatabaseProvider"] ?? "sqlite";
var connStr = provider == "mysql"
    ? builder.Configuration.GetConnectionString("MySqlConnection")
    : builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=payslip4all.db";

builder.Services.AddDbContext<PayslipDbContext>(options =>
{
    if (provider == "mysql")
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
    else
        options.UseSqlite(connStr);
});

// Cookie authentication
var expireDays = builder.Configuration.GetValue<int>("Auth:Cookie:ExpireDays", 30);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Portal/Auth/Login";
        options.LogoutPath = "/Portal/Auth/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(expireDays);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Auth state provider
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CookieAuthenticationStateProvider>());

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IPayslipRepository, PayslipRepository>();

// Infrastructure services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// Application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPayslipService, PayslipGenerationService>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PayslipDbContext>());

var app = builder.Build();

// Apply pending migrations on startup only when the DB is behind the codebase.
// If all migrations are "pending" but tables already exist (inconsistent state from
// a previous partial run), wipe and recreate so we don't crash on duplicate tables.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PayslipDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count > 0)
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        if (applied.Count == 0 && await db.Database.CanConnectAsync())
        {
            // No migration history but DB exists — stale/inconsistent state; start clean.
            await db.Database.EnsureDeletedAsync();
        }
        await db.Database.MigrateAsync();
    }
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// PDF download endpoint
app.MapGet("/payslips/{payslipId:guid}/download",
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CompanyOwner")]
    async (Guid payslipId, IPayslipService svc, HttpContext ctx) =>
    {
        var userIdStr = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
        var pdf = await svc.GetPdfAsync(payslipId, userId);
        if (pdf == null) return Results.NotFound();
        return Results.File(pdf, "application/pdf", $"payslip-{payslipId}.pdf");
    });

await app.RunAsync();

// Required by WebApplicationFactory<Program> in integration tests.
// Top-level statements generate a private Program class; this partial declaration
// makes it accessible to the test assembly.
public partial class Program { }
