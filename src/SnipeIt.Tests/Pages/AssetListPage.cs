namespace SnipeIt.Tests.Pages;

public sealed class AssetListPage : BasePage
{
    public AssetListPage(IPage page) : base(page) { }

    private ILocator SearchBox => Page.Locator("input.search-input").First;
    private ILocator Rows => Page.Locator("table tbody tr");

    public async Task GotoAsync()
    {
        await Page.GotoAsync("/hardware");
        await Expect(SearchBox).ToBeVisibleAsync();
    }

    /// <summary>Searches the list by asset tag and returns the single matching row.</summary>
    public async Task<ILocator> FindAssetRowAsync(string assetTag)
    {
        await SearchBox.FillAsync(assetTag);
        await SearchBox.PressAsync("Enter");

        // Bootstrap Table can render cloned copies of rows for its fixed columns,
        // so take the first match rather than insisting on exactly one element.
        var row = Rows.Filter(new() { HasText = assetTag }).First;
        await Expect(row).ToBeVisibleAsync();
        return row;
    }

    public async Task ExpectRowShowsAsync(ILocator row, params string[] values)
    {
        foreach (var value in values)
        {
            await Expect(row).ToContainTextAsync(value);
        }
    }

    public async Task OpenAssetAsync(ILocator row, string assetTag)
    {
        await row.GetByRole(AriaRole.Link, new() { Name = assetTag, Exact = true }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/hardware/\d+$"));
    }
}
