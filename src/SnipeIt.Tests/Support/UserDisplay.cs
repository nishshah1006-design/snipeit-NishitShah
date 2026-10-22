namespace SnipeIt.Tests.Support;

public static class UserDisplay
{
    /// <summary>
    /// The user dropdown shows "First Last (username)" or similar; pages that
    /// display the assignee show just the full name.
    /// </summary>
    public static string FullName(string dropdownText)
    {
        var text = dropdownText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? dropdownText;
        var bracket = text.IndexOf(" (", StringComparison.Ordinal);
        return (bracket > 0 ? text[..bracket] : text).Trim();
    }
}
