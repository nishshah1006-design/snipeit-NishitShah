using Microsoft.Extensions.Configuration;

namespace SnipeIt.Tests.Config;

/// <summary>
/// Strongly typed settings. Values come from appsettings.json and can be
/// overridden with environment variables, e.g. SnipeIt__Password=secret.
/// </summary>
public sealed class TestSettings
{
    public string BaseUrl { get; init; } = "https://demo.snipeitapp.com";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Browser { get; init; } = "chromium";
    public bool Headless { get; init; } = true;
    public int DefaultTimeoutMs { get; init; } = 30_000;
    public int ExpectTimeoutMs { get; init; } = 15_000;

    private static readonly Lazy<TestSettings> Instance = new(Load);

    public static TestSettings Current => Instance.Value;

    private static TestSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration.GetSection("SnipeIt").Get<TestSettings>()
            ?? throw new InvalidOperationException("Missing 'SnipeIt' configuration section.");

        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException("SnipeIt:Username and SnipeIt:Password must be configured.");
        }

        return settings;
    }
}
