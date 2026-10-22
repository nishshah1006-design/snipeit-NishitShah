using Reqnroll.BoDi;
using SnipeIt.Tests.Config;

namespace SnipeIt.Tests.Support;

/// <summary>
/// Browser lifecycle for the scenarios: one browser per test run, and a fresh
/// browser context and page per scenario. The page is registered in Reqnroll's
/// container so step definitions receive it through their constructors.
/// A Playwright trace is saved only when a scenario fails, so passing runs stay fast and clean.
/// </summary>
[Binding]
public sealed class PlaywrightHooks
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    private readonly IObjectContainer _container;
    private readonly ScenarioContext _scenario;
    private IBrowserContext? _context;

    public PlaywrightHooks(IObjectContainer container, ScenarioContext scenario)
    {
        _container = container;
        _scenario = scenario;
    }

    private static TestSettings Settings => TestSettings.Current;

    [BeforeTestRun]
    public static async Task LaunchBrowserAsync()
    {
        Assertions.SetDefaultExpectTimeout(Settings.ExpectTimeoutMs);

        var browserName = Settings.Browser.ToLowerInvariant();
        InstallBrowser(browserName);

        _playwright = await Playwright.CreateAsync();
        var browserType = browserName switch
        {
            "chromium" => _playwright.Chromium,
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            var other => throw new InvalidOperationException($"Unsupported browser '{other}'. Use chromium, firefox or webkit."),
        };
        _browser = await browserType.LaunchAsync(new() { Headless = Settings.Headless });
    }

    /// <summary>
    /// Downloads the browser on first use, so a fresh clone only needs "dotnet test".
    /// It's a quick no-op when the browser is already installed.
    /// </summary>
    private static void InstallBrowser(string browserName)
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", browserName]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright failed to install '{browserName}' (exit code {exitCode}).");
        }
    }

    [AfterTestRun]
    public static async Task CloseBrowserAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();
    }

    [BeforeScenario]
    public async Task OpenPageAsync()
    {
        _context = await _browser!.NewContextAsync(new()
        {
            BaseURL = Settings.BaseUrl,
            ViewportSize = new() { Width = 1600, Height = 1000 },
            Locale = "en-AU",
        });

        await _context.Tracing.StartAsync(new()
        {
            Title = _scenario.ScenarioInfo.Title,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });

        var page = await _context.NewPageAsync();
        page.SetDefaultTimeout(Settings.DefaultTimeoutMs);
        page.SetDefaultNavigationTimeout(Settings.DefaultTimeoutMs);
        _container.RegisterInstanceAs(page);
    }

    [AfterScenario]
    public async Task ClosePageAsync()
    {
        if (_context is null)
        {
            return;
        }

        var tracePath = _scenario.TestError is not null
            ? Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces",
                $"{TestContext.CurrentContext.Test.Name}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip")
            : null;

        await _context.Tracing.StopAsync(new() { Path = tracePath });
        await _context.CloseAsync();

        if (tracePath is not null)
        {
            TestContext.AddTestAttachment(tracePath, "Playwright trace (open with: pwsh bin/Debug/net10.0/playwright.ps1 show-trace <file>)");
        }
    }
}
