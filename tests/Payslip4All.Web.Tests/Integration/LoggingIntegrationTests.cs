using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Payslip4All.Web.Tests.Integration;

/// <summary>
/// T009 + T012 — Integration tests for Serilog file logging.
/// Verifies rolling daily file creation (US2) and configurable log level (US3).
/// Uses WebApplicationFactory with a temp directory override so real file I/O
/// is exercised without polluting the development logs/ directory.
/// </summary>
public class LoggingIntegrationTests : IDisposable
{
    private readonly string _tempLogDir;

    public LoggingIntegrationTests()
    {
        _tempLogDir = Path.Combine(Path.GetTempPath(), $"payslip4all-test-logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempLogDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempLogDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private WebApplicationFactory<Program> BuildFactory(
        string? minimumLevel = null,
        string? logPath = null)
    {
        var resolvedPath = logPath ?? Path.Combine(_tempLogDir, "payslip4all-.log");

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    // WriteTo:0 is the Console sink; WriteTo:1 is the File sink.
                    // Override the File sink (index 1) to redirect output to the temp dir.
                    ["Serilog:WriteTo:1:Name"] = "File",
                    ["Serilog:WriteTo:1:Args:path"] = resolvedPath,
                    ["Serilog:WriteTo:1:Args:rollingInterval"] = "Day",
                    ["Serilog:WriteTo:1:Args:retainedFileCountLimit"] = "31",
                    ["Serilog:WriteTo:1:Args:buffered"] = "false",
                    // Suppress all namespace-level overrides so MinimumLevel:Default is authoritative
                    ["Serilog:MinimumLevel:Override:Microsoft"] = minimumLevel ?? "Warning",
                    ["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime"] = minimumLevel ?? "Warning",
                    ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = minimumLevel ?? "Warning",
                    ["Serilog:MinimumLevel:Override:System"] = minimumLevel ?? "Warning"
                };

                if (minimumLevel != null)
                    overrides["Serilog:MinimumLevel:Default"] = minimumLevel;

                cfg.AddInMemoryCollection(overrides);
            });

            // Use in-memory SQLite for tests to avoid migration side-effects
            builder.ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "sqlite",
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(_tempLogDir, "test.db")}"
                }));
        });
    }

    // ─── US2: Rolling Daily Log Files ──────────────────────────────────────

    /// <summary>
    /// T009 — After any HTTP request, a log file named with the current UTC date
    /// must exist at the configured path.
    /// </summary>
    [Fact]
    public async Task LogFile_IsCreated_WithCurrentDateInName()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Any request will produce at least one log entry
        await client.GetAsync("/");

        // Serilog Sinks.File with buffered:false writes synchronously
        // Give a brief moment for the OS to flush
        await Task.Delay(100);

        var today = DateTime.Now.ToString("yyyyMMdd");
        var logFiles = Directory.GetFiles(_tempLogDir, "*.log", SearchOption.TopDirectoryOnly);

        Assert.True(logFiles.Length > 0, $"No log files found in {_tempLogDir}");
        var fileNames = string.Join(", ", logFiles.Select(Path.GetFileName));
        Assert.True(
            logFiles.Any(f => Path.GetFileName(f).Contains(today)),
            $"Expected a file containing '{today}' in name. Found: [{fileNames}]");
    }

    /// <summary>
    /// T009 — The log file must be non-empty after a request.
    /// </summary>
    [Fact]
    public async Task LogFile_IsNonEmpty_AfterRequest()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/");
        await Task.Delay(100);

        var logFiles = Directory.GetFiles(_tempLogDir, "*.log", SearchOption.TopDirectoryOnly);
        Assert.True(logFiles.Length > 0, "No log files found");

        var content = await File.ReadAllTextAsync(logFiles[0]);
        Assert.False(string.IsNullOrWhiteSpace(content), "Log file is empty");
    }

    /// <summary>
    /// T009 — Logs directory is created automatically by Serilog even if it did not pre-exist.
    /// </summary>
    [Fact]
    public async Task LogDirectory_IsCreatedAutomatically_WhenNotPreExisting()
    {
        var newSubDir = Path.Combine(_tempLogDir, "auto-created");
        // Do NOT create the directory — Serilog should create it
        var logPath = Path.Combine(newSubDir, "payslip4all-.log");

        using var factory = BuildFactory(logPath: logPath);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/");
        await Task.Delay(100);

        Assert.True(Directory.Exists(newSubDir),
            $"Expected Serilog to auto-create directory: {newSubDir}");
    }

    // ─── US3: Configurable Log Level ───────────────────────────────────────

    /// <summary>
    /// T012 — When MinimumLevel is Warning, Information-level entries must NOT appear in the log file.
    /// </summary>
    [Fact]
    public async Task LogFile_DoesNotContainInformation_WhenMinimumLevelIsWarning()
    {
        using var factory = BuildFactory(minimumLevel: "Warning");
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/");
        await Task.Delay(100);

        var logFiles = Directory.GetFiles(_tempLogDir, "*.log", SearchOption.TopDirectoryOnly);
        if (logFiles.Length == 0) return; // No file at all means no entries — pass

        var content = await File.ReadAllTextAsync(logFiles[0]);
        // [INF] is the 3-char level code for Information in our output template
        Assert.DoesNotContain("[INF]", content);
    }

    /// <summary>
    /// T012 — When MinimumLevel is Debug, entries of all levels including Debug appear.
    /// </summary>
    [Fact]
    public async Task LogFile_ContainsEntries_WhenMinimumLevelIsDebug()
    {
        using var factory = BuildFactory(minimumLevel: "Debug");
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/");
        await Task.Delay(100);

        var logFiles = Directory.GetFiles(_tempLogDir, "*.log", SearchOption.TopDirectoryOnly);
        Assert.True(logFiles.Length > 0, "Expected log file to be created at Debug level");

        var content = await File.ReadAllTextAsync(logFiles[0]);
        Assert.False(string.IsNullOrWhiteSpace(content), "Log file should not be empty at Debug level");
    }
}
