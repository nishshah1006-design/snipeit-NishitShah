using System.Globalization;

namespace SnipeIt.Tests.Pages;

public sealed class AssetDetailsPage : BasePage
{
    public AssetDetailsPage(IPage page) : base(page) { }

    private ILocator DetailsPane => Page.Locator("#details").First;
    private ILocator HistoryTab => Page.Locator("a[href='#history']").First;
    private ILocator HistoryPane => Page.Locator("#history").First;
    private ILocator HistoryRows => HistoryPane.Locator("table tbody tr");

    // The first "well" in the details pane shows the status label, the "Deployed"
    // meta status and, when checked out, a link to the assignee.
    private ILocator StatusWell => DetailsPane.Locator(".well").First;
    private ILocator Assignee => StatusWell.Locator("a[href*='/users/']");

    private ILocator PurchaseCost => Page.Locator("span.js-copy-purchase_cost");
    private ILocator PurchaseDate =>
        Page.Locator("li.list-group-item").Filter(new() { Has = Page.Locator("[data-title='Purchase Date']") });
    private ILocator WarrantyExpires =>
        Page.Locator(".progress-group").Filter(new() { HasText = "Warranty Expires" }).Locator(".progress-number");

    // Snipe-IT wraps each copyable value in a span with a "js-copy-<field>" class.
    private const string CopyableValue = "span[class*='js-copy-']";

    /// <summary>Value cell of a row in the details pane's definition list, matched by its exact label.</summary>
    private ILocator DetailValue(string label) =>
        DetailsPane.Locator("dl.table-display dt")
            .Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(label)}\s*$") })
            .Locator("xpath=following-sibling::dd[1]")
            .Locator(CopyableValue);

    /// <summary>Value of an item in the side info panel, e.g. "serial-number", "model" or "notes".</summary>
    private ILocator InfoPanelValue(string itemId) =>
        Page.Locator($"li.list-group-item#{itemId}").Locator(CopyableValue);

    public Task ExpectAssetTagAsync(string assetTag) => Expect(DetailValue("Asset Tag")).ToHaveTextAsync(assetTag);

    public Task ExpectAssetNameAsync(string name) => Expect(DetailValue("Asset Name")).ToHaveTextAsync(name);

    public Task ExpectSerialAsync(string serial) => Expect(InfoPanelValue("serial-number")).ToHaveTextAsync(serial);

    public Task ExpectModelAsync(string modelName) => Expect(InfoPanelValue("model")).ToHaveTextAsync(modelName);

    public Task ExpectNotesAsync(string notes) => Expect(InfoPanelValue("notes")).ToHaveTextAsync(notes);

    public Task ExpectOrderNumberAsync(string orderNumber) =>
        Expect(InfoPanelValue("order-number")).ToHaveTextAsync(orderNumber);

    public Task ExpectPurchaseDateAsync(DateOnly purchased) =>
        Expect(PurchaseDate).ToContainTextAsync(DisplayDate(purchased));

    public Task ExpectWarrantyExpiresAsync(DateOnly expires) =>
        Expect(WarrantyExpires).ToContainTextAsync(DisplayDate(expires));

    /// <summary>
    /// Checks the cost's digits, allowing any separators: the demo's number format
    /// (e.g. 1,234.50 or 1.234,50) is a site setting that changes between resets.
    /// </summary>
    public Task ExpectPurchaseCostAsync(decimal cost)
    {
        var digits = cost.ToString("0.00", CultureInfo.InvariantCulture).Replace(".", string.Empty);
        var pattern = string.Join(@"[.,\s']?", digits.ToCharArray());
        return Expect(PurchaseCost).ToHaveTextAsync(new Regex($"^{pattern}$"));
    }

    /// <summary>Asserts the asset shows the given status, is deployed, and is assigned to the given user.</summary>
    public async Task ExpectCheckedOutAsync(string statusName, string userName)
    {
        await Expect(StatusWell).ToContainTextAsync(statusName);
        await Expect(StatusWell.Locator(".label")).ToHaveTextAsync("Deployed");
        await Expect(Assignee).ToHaveTextAsync(userName);
    }

    public async Task OpenHistoryTabAsync()
    {
        await HistoryTab.ClickAsync();
        await Expect(HistoryPane).ToBeVisibleAsync();
        await Expect(HistoryRows.First).ToBeVisibleAsync();
    }

    public Task ExpectCreatedEntryAsync() => Expect(HistoryRowsFor("create")).Not.ToHaveCountAsync(0);

    public async Task ExpectSingleCheckoutToAsync(string userName)
    {
        var checkoutRows = HistoryRowsFor("checkout");
        await Expect(checkoutRows).ToHaveCountAsync(1);
        await Expect(checkoutRows.First).ToContainTextAsync(userName);
    }

    /// <summary>Returns history rows whose text matches the action (e.g. "checkout").</summary>
    private ILocator HistoryRowsFor(string action) =>
        HistoryRows.Filter(new() { HasTextRegex = new Regex(action, RegexOptions.IgnoreCase) });

    // Snipe-IT's default display format, e.g. "Thu Jan 15, 2026".
    private static string DisplayDate(DateOnly date) =>
        date.ToString("ddd MMM dd, yyyy", CultureInfo.GetCultureInfo("en-US"));
}
