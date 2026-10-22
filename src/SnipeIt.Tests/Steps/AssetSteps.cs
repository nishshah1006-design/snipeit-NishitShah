using System.Globalization;
using SnipeIt.Tests.Models;
using SnipeIt.Tests.Pages;
using SnipeIt.Tests.Support;

namespace SnipeIt.Tests.Steps;

[Binding]
public sealed class AssetSteps
{
    private readonly IPage _page;
    private readonly AssetScenarioState _state;
    private readonly IReqnrollOutputHelper _output;

    public AssetSteps(IPage page, AssetScenarioState state, IReqnrollOutputHelper output)
    {
        _page = page;
        _state = state;
        _output = output;
    }

    [When("I create an asset checked out to a random user with these details:")]
    public async Task WhenICreateAnAssetCheckedOutToARandomUser(Table details)
    {
        _state.Asset = ParseAsset(details);

        var createPage = new CreateAssetPage(_page);
        await createPage.GotoAsync();
        _state.SelectedUser = await createPage.CreateCheckedOutToRandomUserAsync(_state.Asset);
        _output.WriteLine($"Created {_state.Asset.AssetTag} checked out to '{_state.SelectedUser}'");
    }

    [Given("an asset already exists with these details:")]
    public async Task GivenAnAssetAlreadyExists(Table details)
    {
        _state.Asset = ParseAsset(details);

        var createPage = new CreateAssetPage(_page);
        await createPage.GotoAsync();
        await createPage.CreateAsync(_state.Asset);
        _output.WriteLine($"Created {_state.Asset.AssetTag}");
    }

    [When("I try to create another asset with the same asset tag")]
    public async Task WhenITryToCreateAnotherAssetWithTheSameAssetTag()
    {
        var existing = _state.Asset;
        var fresh = AssetData.CreateUnique(existing.ModelName, existing.StatusName);
        var duplicate = fresh with { AssetTag = existing.AssetTag };

        var createPage = new CreateAssetPage(_page);
        await createPage.GotoAsync();
        await createPage.SubmitAsync(duplicate);
    }

    [Then("the asset tag is rejected with the error {string}")]
    public Task ThenTheAssetTagIsRejected(string message) =>
        new CreateAssetPage(_page).ExpectAssetTagRejectedAsync(message);

    [Then("the asset appears in the assets list with its name, serial and assignee")]
    public async Task ThenTheAssetAppearsInTheAssetsList()
    {
        var listPage = new AssetListPage(_page);
        await listPage.GotoAsync();
        _state.ListRow = await listPage.FindAssetRowAsync(_state.Asset.AssetTag);
        await listPage.ExpectRowShowsAsync(_state.ListRow, _state.Asset.Name, _state.Asset.Serial, _state.SelectedUserName);
    }

    [When("I open the asset from the assets list")]
    public Task WhenIOpenTheAssetFromTheAssetsList() =>
        new AssetListPage(_page).OpenAssetAsync(_state.ListRow, _state.Asset.AssetTag);

    [Then("the asset page shows the details I entered")]
    public async Task ThenTheAssetPageShowsTheDetailsIEntered()
    {
        var asset = _state.Asset;
        var details = new AssetDetailsPage(_page);

        await details.ExpectAssetTagAsync(asset.AssetTag);
        await details.ExpectAssetNameAsync(asset.Name);
        await details.ExpectSerialAsync(asset.Serial);
        await details.ExpectModelAsync(asset.ModelName);
        await details.ExpectNotesAsync(asset.Notes);
        await details.ExpectOrderNumberAsync(asset.OrderNumber);

        if (asset.PurchaseDate is { } purchased)
        {
            await details.ExpectPurchaseDateAsync(purchased);
        }
        if (asset.PurchaseCost is { } cost)
        {
            await details.ExpectPurchaseCostAsync(cost);
        }
        if (asset.WarrantyExpires is { } expires)
        {
            await details.ExpectWarrantyExpiresAsync(expires);
        }
    }

    [Then("the asset is deployed to the selected user")]
    public Task ThenTheAssetIsDeployedToTheSelectedUser() =>
        new AssetDetailsPage(_page).ExpectCheckedOutAsync(_state.Asset.StatusName, _state.SelectedUserName);

    [When("I open the asset's history")]
    public Task WhenIOpenTheAssetsHistory() => new AssetDetailsPage(_page).OpenHistoryTabAsync();

    [Then("the history shows the asset was created")]
    public Task ThenTheHistoryShowsTheAssetWasCreated() => new AssetDetailsPage(_page).ExpectCreatedEntryAsync();

    [Then("the history shows exactly one checkout to the selected user")]
    public Task ThenTheHistoryShowsExactlyOneCheckout() =>
        new AssetDetailsPage(_page).ExpectSingleCheckoutToAsync(_state.SelectedUserName);

    /// <summary>
    /// Builds an asset from a two-column Field | Value table. Model and Status are required;
    /// the asset tag, name, serial, order number and notes are always generated so they are
    /// unique to this run.
    /// </summary>
    private static AssetData ParseAsset(Table details)
    {
        var fields = details.Rows.ToDictionary(row => row["Field"].Trim(), row => row["Value"].Trim(), StringComparer.OrdinalIgnoreCase);
        string? Optional(string field) => fields.Remove(field, out var value) ? value : null;
        string Required(string field) => Optional(field) ?? throw new ArgumentException($"The asset details table needs a '{field}' row.");

        var asset = AssetData.CreateUnique(Required("Model"), Required("Status")) with
        {
            PurchaseDate = Optional("Purchase Date") is { } date ? DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            PurchaseCost = Optional("Purchase Cost") is { } cost ? decimal.Parse(cost, CultureInfo.InvariantCulture) : null,
            WarrantyMonths = Optional("Warranty Months") is { } months ? int.Parse(months, CultureInfo.InvariantCulture) : null,
        };

        if (fields.Count > 0)
        {
            throw new ArgumentException($"Unknown asset field(s): {string.Join(", ", fields.Keys)}.");
        }

        return asset;
    }
}
