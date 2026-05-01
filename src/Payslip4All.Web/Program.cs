using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Infrastructure.Auth;
using Payslip4All.Infrastructure.Configuration;
using Payslip4All.Infrastructure.HostedPayments;
using Payslip4All.Infrastructure.Persistence;
using Payslip4All.Infrastructure.Persistence.DynamoDB;
using Payslip4All.Web.Auth;
using Payslip4All.Infrastructure.Persistence.Repositories;
using Payslip4All.Infrastructure.Services;
using Payslip4All.Infrastructure.Time;
using Payslip4All.Infrastructure.HostedServices;
using Payslip4All.Web.Extensions;
using Payslip4All.Web.Endpoints;
using QuestPDF.Infrastructure;
using Serilog;
using System.Text.Json;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

// Bootstrap Serilog early so startup errors are captured
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));

var builder = WebApplication.CreateBuilder(args);
InsertAwsSecretsConfigurationSource(builder.Configuration);
// Reverse proxy mode is driven by REVERSE_PROXY_ENABLED, REVERSE_PROXY_PUBLIC_HOST,
// and REVERSE_PROXY_UPSTREAM_BASE_URL through Payslip4AllCustomConfigurationKeys.
var reverseProxyOptions = ReverseProxyModeOptions.FromConfiguration(builder.Configuration);

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext());

if (reverseProxyOptions.Enabled)
{
    reverseProxyOptions.ValidateForStartup();

    builder.Services
        .AddReverseProxy()
        .LoadFromMemory(
            routes: [CreatePublicEdgeRoute()],
            clusters: [CreatePublicEdgeCluster(reverseProxyOptions)])
        .AddTransforms(transformBuilderContext => transformBuilderContext.AddOriginalHost(true));

    var proxyApp = builder.Build();

    if (!proxyApp.Environment.IsDevelopment())
    {
        proxyApp.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Gateway error.\n");
            });
        });
        proxyApp.UseHsts();
    }

    proxyApp.UseSerilogRequestLogging();
    proxyApp.Use(async (context, next) =>
    {
        if (!string.Equals(context.Request.Host.Host, reverseProxyOptions.PublicHost, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status421MisdirectedRequest;
            return;
        }

        if (reverseProxyOptions.HttpsListenerConfigured && !context.Request.IsHttps)
        {
            context.Response.Redirect(
                $"https://{reverseProxyOptions.PublicHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
                permanent: true);
            return;
        }

        await next();
    });
    proxyApp.UseStatusCodePages(async context =>
    {
        var response = context.HttpContext.Response;
        if (response.StatusCode is StatusCodes.Status502BadGateway
            or StatusCodes.Status503ServiceUnavailable
            or StatusCodes.Status504GatewayTimeout)
        {
            response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            response.ContentType = "text/plain";
            await response.WriteAsync("Service temporarily unavailable.");
        }
    });
    proxyApp.MapReverseProxy();

    await proxyApp.RunAsync();
    return;
}

// Configure forwarded headers for reverse-proxy deployments (YARP, Apache, Azure, etc.).
// Must be registered before the middleware pipeline is built.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    // Trust any proxy IP — restrict to specific KnownProxies entries in production.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// QuestPDF community licence
QuestPDF.Settings.License = LicenseType.Community;

static void InsertAwsSecretsConfigurationSource(ConfigurationManager configuration)
{
    var secretValues = LoadAwsSecretsConfigurationValues(configuration);
    if (secretValues.Count == 0)
        return;

    var originalSources = configuration.Sources.ToList();
    configuration.Sources.Clear();

    var inserted = false;
    foreach (var source in originalSources)
    {
        if (!inserted
            && source is EnvironmentVariablesConfigurationSource environmentSource
            && string.IsNullOrEmpty(environmentSource.Prefix))
        {
            ((IConfigurationBuilder)configuration).AddInMemoryCollection(secretValues);
            inserted = true;
        }

        ((IConfigurationBuilder)configuration).Add(source);
    }

    if (!inserted)
        ((IConfigurationBuilder)configuration).AddInMemoryCollection(secretValues);
}

static IReadOnlyDictionary<string, string?> LoadAwsSecretsConfigurationValues(IConfiguration configuration)
{
    var path = AwsSecretsConfigurationDefaults.ResolveSecretsFilePath(configuration);
    if (!File.Exists(path))
        return new Dictionary<string, string?>();

    using var document = JsonDocument.Parse(File.ReadAllText(path));
    if (document.RootElement.ValueKind != JsonValueKind.Object)
        throw new InvalidOperationException("The AWS app-config secret artifact must contain a JSON object.");

    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var property in document.RootElement.EnumerateObject())
    {
        values[property.Name] = property.Value.ValueKind switch
        {
            JsonValueKind.String => property.Value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.GetRawText(),
            JsonValueKind.Null => null,
            _ => throw new InvalidOperationException(
                $"The AWS app-config secret artifact value for '{property.Name}' must be a scalar JSON value."),
        };
    }

    return values;
}

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Database provider switching
// Read PERSISTENCE_PROVIDER from configuration (supports env var, appsettings, test overrides).
// Defaults to "sqlite".
var provider = (builder.Configuration[Payslip4AllCustomConfigurationKeys.PersistenceProvider]?.Trim().ToLowerInvariant())
               ?? "sqlite";
var dynamoDbOptions = DynamoDbConfigurationOptions.FromConfiguration(builder.Configuration);

// Validate provider value
if (provider is not ("sqlite" or "mysql" or "dynamodb"))
    throw new InvalidOperationException(
        $"Unknown persistence provider '{provider}'. Valid values are: sqlite, mysql, dynamodb.");

if (provider == "dynamodb")
    dynamoDbOptions.ValidateForStartup();

if (provider == "dynamodb")
{
    // Register DynamoDB client (singleton), repositories, unit of work, and table provisioner
    builder.Services.AddDynamoDbPersistence(dynamoDbOptions);
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
    builder.Services.AddScoped<IWalletRepository, WalletRepository>();
    builder.Services.AddScoped<IWalletActivityRepository, WalletActivityRepository>();
    builder.Services.AddScoped<IPayslipPricingRepository, PayslipPricingRepository>();
    builder.Services.AddScoped<IWalletTopUpAttemptRepository, WalletTopUpAttemptRepository>();
    builder.Services.AddScoped<IPaymentReturnEvidenceRepository, PaymentReturnEvidenceRepository>();
    builder.Services.AddScoped<IOutcomeNormalizationDecisionRepository, OutcomeNormalizationDecisionRepository>();
    builder.Services.AddScoped<IUnmatchedPaymentReturnRecordRepository, UnmatchedPaymentReturnRecordRepository>();
}

// Infrastructure services
builder.Services.AddHttpClient(nameof(PayFastHostedPaymentProvider));
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// Application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPayslipService, PayslipGenerationService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPayslipPricingService, PayslipPricingService>();
builder.Services.AddScoped<IWalletTopUpOutcomeNormalizer, WalletTopUpOutcomeNormalizer>();
builder.Services.AddScoped<IWalletTopUpAbandonmentService, WalletTopUpAbandonmentService>();
builder.Services.AddScoped<IWalletTopUpService, WalletTopUpService>();

// Hosted payment providers
var fakePaymentOptions = new FakeHostedPaymentOptions();
builder.Configuration.GetSection(FakeHostedPaymentOptions.SectionKey).Bind(fakePaymentOptions);
builder.Services.AddSingleton(fakePaymentOptions);
var payFastOptions = new PayFastHostedPaymentOptions();
builder.Configuration.GetSection(PayFastHostedPaymentOptions.SectionKey).Bind(payFastOptions);
builder.Services.AddSingleton(payFastOptions);
builder.Services.AddSingleton<PayFastSignatureVerifier>();
builder.Services.AddSingleton<IHostedPaymentProvider, PayFastHostedPaymentProvider>();
builder.Services.AddSingleton<IHostedPaymentProvider, FakeHostedPaymentProvider>();
builder.Services.AddSingleton<HostedPaymentProviderFactory>();
builder.Services.AddSingleton<IHostedPaymentProviderFactory>(sp => sp.GetRequiredService<HostedPaymentProviderFactory>());
builder.Services.AddHostedService<WalletTopUpReconciliationHostedService>();

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
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseGlobalExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet(HealthEndpoint.Path, HealthEndpoint.Handle);
app.MapPost("/api/payments/payfast/notify", PayFastNotifyEndpoint.HandleAsync);
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

static string NormalizeProxyDestination(string upstreamBaseUrl)
{
    var normalized = upstreamBaseUrl.Trim();
    return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : $"{normalized}/";
}

static RouteConfig CreatePublicEdgeRoute()
{
    return new RouteConfig
    {
        RouteId = "public-edge",
        ClusterId = "app-backend",
        Match = new RouteMatch
        {
            Path = "/{**catch-all}"
        }
    };
}

static ClusterConfig CreatePublicEdgeCluster(ReverseProxyModeOptions reverseProxyOptions)
{
    return new ClusterConfig
    {
        ClusterId = "app-backend",
        HttpRequest = new ForwarderRequestConfig
        {
            ActivityTimeout = TimeSpan.FromSeconds(reverseProxyOptions.ActivityTimeoutSeconds)
        },
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = new()
            {
                Address = NormalizeProxyDestination(reverseProxyOptions.UpstreamBaseUrl)
            }
        }
    };
}

// Required by WebApplicationFactory<Program> in integration tests.
// Top-level statements generate a private Program class; this partial declaration
// makes it accessible to the test assembly.
public partial class Program { }
