using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Infrastructure.Auth;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Web.Auth;
using Payslip4All.Infrastructure.Persistence.Repositories;
using Payslip4All.Infrastructure.Services;
using Payslip4All.Web.Extensions;
using QuestPDF.Infrastructure;
using Serilog;

// Bootstrap Serilog early so startup errors are captured
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext());

// Configure forwarded headers for reverse-proxy deployments (nginx, Apache, Azure, etc.).
// Must be registered before the middleware pipeline is built.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust any proxy IP — restrict to specific KnownProxies entries in production.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// QuestPDF community licence
QuestPDF.Settings.License = LicenseType.Community;

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Database provider switching
// Read PERSISTENCE_PROVIDER from configuration (supports env var, appsettings, test overrides).
// Defaults to "sqlite".
var provider = (builder.Configuration["PERSISTENCE_PROVIDER"]?.Trim().ToLowerInvariant())
               ?? "sqlite";

// Validate provider value
if (provider is not ("sqlite" or "mysql" or "dynamodb"))
    throw new InvalidOperationException(
        $"Unknown persistence provider '{provider}'. Valid values are: sqlite, mysql, dynamodb.");

if (provider == "dynamodb")
{
    var dynamoRegion = Environment.GetEnvironmentVariable("DYNAMODB_REGION")?.Trim();
    if (string.IsNullOrWhiteSpace(dynamoRegion))
        throw new InvalidOperationException(
            "PERSISTENCE_PROVIDER is set to 'dynamodb' but the required environment variable DYNAMODB_REGION is not set.");
}

if (provider == "dynamodb")
{
    // Register DynamoDB client (singleton), repositories, unit of work, and table provisioner
    builder.Services.AddDynamoDbPersistence();
}
else
{
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
}

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

// Repositories (EF Core — only for SQLite/MySQL)
if (provider != "dynamodb")
{
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
    builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    builder.Services.AddScoped<ILoanRepository, LoanRepository>();
    builder.Services.AddScoped<IPayslipRepository, PayslipRepository>();
}

// Infrastructure services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// Application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPayslipService, PayslipGenerationService>();

// IUnitOfWork: registered by AddDynamoDbPersistence() for dynamodb; for sqlite/mysql, use PayslipDbContext
if (provider != "dynamodb")
{
    builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PayslipDbContext>());
}

var app = builder.Build();

// Apply pending migrations on startup only when the DB is behind the codebase.
// If all migrations are "pending" but tables already exist (inconsistent state from
// a previous partial run), wipe and recreate so we don't crash on duplicate tables.
// DynamoDB tables are provisioned by DynamoDbTableProvisioner (IHostedService).
if (provider != "dynamodb")
{
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
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
// Forwarded headers MUST be first so UseHttpsRedirection() sees the correct scheme
// when the app is behind a TLS-terminating reverse proxy (nginx, Apache, Azure, etc.).
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseGlobalExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
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
