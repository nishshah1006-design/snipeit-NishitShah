namespace SnipeIt.Tests.Components;

/// <summary>
/// Wraps a Select2 dropdown. Snipe-IT renders most selects (model, status,
/// user) with Select2, which hides the native &lt;select&gt; and builds its own
/// markup with predictable ids derived from the select id.
/// </summary>
public sealed class Select2Dropdown
{
    private readonly IPage _page;
    private readonly string _selectId;

    public Select2Dropdown(IPage page, string selectId)
    {
        _page = page;
        _selectId = selectId;
    }

    private ILocator Selection =>
        _page.Locator($"[aria-labelledby='select2-{_selectId}-container'], #select2-{_selectId}-container").First;

    private ILocator RenderedValue => _page.Locator($"#select2-{_selectId}-container");

    // Select2 marks both the inline field and the floating dropdown as "--open";
    // the dropdown is the only one that contains a .select2-dropdown panel.
    private ILocator OpenDropdown => _page.Locator(".select2-container--open .select2-dropdown");

    private ILocator SearchField => OpenDropdown.Locator(".select2-search__field");

    private ILocator ResultsList => _page.Locator($"#select2-{_selectId}-results");

    // Select2's AJAX "loading" placeholder is an icon with no text, so a text-based
    // HasNotText filter doesn't catch it; exclude it by its class instead.
    private ILocator Options =>
        _page.Locator($"#select2-{_selectId}-results li[role='option']:not(.loading-results)")
            .Filter(new() { HasNotText = "No results" });

    /// <summary>Opens the dropdown, optionally types a search term, and picks the first matching option.</summary>
    public async Task<string> SelectAsync(string optionText, string? searchTerm = null)
    {
        await OpenAsync();

        if (searchTerm is not null && await SearchField.IsVisibleAsync())
        {
            await SearchField.FillAsync(searchTerm);
        }

        var option = Options.Filter(new() { HasText = optionText }).First;
        await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var chosen = (await option.InnerTextAsync()).Trim();
        await option.ClickAsync();

        await Assertions.Expect(RenderedValue).ToContainTextAsync(optionText);
        return chosen;
    }

    /// <summary>Opens the dropdown and picks a random option from the first page of results.</summary>
    public async Task<string> SelectRandomAsync()
    {
        await OpenAsync();

        await Options.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var count = await Options.CountAsync();
        Assert.That(count, Is.GreaterThan(0), $"No options available in '{_selectId}'.");

        var option = Options.Nth(Random.Shared.Next(count));
        var chosen = (await option.InnerTextAsync()).Trim();
        Assert.That(chosen, Is.Not.Empty, $"Picked a blank/placeholder option in '{_selectId}'.");
        await option.ClickAsync();

        await Assertions.Expect(RenderedValue).Not.ToBeEmptyAsync();
        return chosen;
    }

    private async Task OpenAsync()
    {
        await Selection.ClickAsync();
        await Assertions.Expect(ResultsList).ToBeVisibleAsync();
    }
}
