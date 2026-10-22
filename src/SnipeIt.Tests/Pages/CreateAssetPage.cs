using System.Globalization;
using SnipeIt.Tests.Components;
using SnipeIt.Tests.Models;

namespace SnipeIt.Tests.Pages;

public sealed class CreateAssetPage : BasePage
{
    public CreateAssetPage(IPage page) : base(page) { }

    private ILocator AssetTag => Page.Locator("#asset_tag");
    private ILocator Serial => Page.Locator("input[name='serials[1]']");
    private ILocator Name => Page.Locator("input[name='name']");
    private ILocator Notes => Page.Locator("textarea[name='notes']");
    private ILocator WarrantyMonths => Page.Locator("#warranty_months");
    private ILocator OrderNumber => Page.Locator("#order_number");
    private ILocator PurchaseDate => Page.Locator("#purchase_date");
    private ILocator PurchaseCost => Page.Locator("#purchase_cost");
    private ILocator CheckoutSelector => Page.Locator("#assignto_selector");
    private ILocator CheckoutToUser => Page.Locator("#assignto_selector label").Filter(new() { HasText = "User" });
    private ILocator OptionalInfoToggle => Page.Locator("#optional_info");
    private ILocator OptionalDetailsPanel => Page.Locator("#optional_details");
    private ILocator OrderInfoToggle => Page.Locator("#order_info");
    private ILocator OrderDetailsPanel => Page.Locator("#order_details");
    private ILocator ErrorAlert => Page.Locator(".alert-danger").First;
    // Server-side validation messages are rendered as a sibling span of the invalid input.
    private ILocator AssetTagError => Page.Locator("#asset_tag ~ .alert-msg");
    private ILocator SaveButton => Page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).Last;

    private Select2Dropdown Model => new(Page, "model_select_id");
    private Select2Dropdown Status => new(Page, "status_select_id");
    private Select2Dropdown AssignedUser => new(Page, "assigned_user_select");

    public async Task GotoAsync()
    {
        await Page.GotoAsync("/hardware/create");
        await Expect(AssetTag).ToBeVisibleAsync();

        // The header's Livewire alert menu loads in the background after page load. If that
        // request lands mid-redirect after saving, it consumes the one-time "created" flash
        // message, so let it finish before the form can be submitted.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Fills in every field the asset data provides, checks it out to a random user and saves.
    /// Returns the display text of the user that was picked.
    /// </summary>
    public async Task<string> CreateCheckedOutToRandomUserAsync(AssetData asset)
    {
        await FillRequiredFieldsAsync(asset);

        // The "Checkout to" section only appears once a deployable status is chosen.
        await Expect(CheckoutSelector).ToBeVisibleAsync();
        await CheckoutToUser.ClickAsync();
        var user = await AssignedUser.SelectRandomAsync();

        await Notes.FillAsync(asset.Notes);
        await FillOptionalInformationAsync(asset);
        await FillOrderInformationAsync(asset);

        await SaveButton.ClickAsync();
        await Expect(SuccessAlert).ToBeVisibleAsync();

        return user;
    }

    /// <summary>Fills in only the required fields and saves, expecting the asset to be created.</summary>
    public async Task CreateAsync(AssetData asset)
    {
        await SubmitAsync(asset);
        await Expect(SuccessAlert).ToBeVisibleAsync();
    }

    /// <summary>Fills in only the required fields and saves, without assuming the save succeeds.</summary>
    public async Task SubmitAsync(AssetData asset)
    {
        await FillRequiredFieldsAsync(asset);
        await SaveButton.ClickAsync();
    }

    /// <summary>Asserts the form was sent back with a validation error on the Asset Tag field.</summary>
    public async Task ExpectAssetTagRejectedAsync(string message)
    {
        await Expect(ErrorAlert).ToContainTextAsync("Please check the form below for errors");
        await Expect(AssetTagError).ToHaveTextAsync(message);
        await Expect(SuccessAlert).Not.ToBeVisibleAsync();
    }

    private async Task FillRequiredFieldsAsync(AssetData asset)
    {
        await AssetTag.FillAsync(asset.AssetTag);
        await Serial.FillAsync(asset.Serial);
        await Model.SelectAsync(asset.ModelName, searchTerm: asset.ModelSearchTerm);
        await Status.SelectAsync(asset.StatusName);
    }

    // Name and warranty live in the "Optional Information" panel, collapsed by default.
    private async Task FillOptionalInformationAsync(AssetData asset)
    {
        await ExpandAsync(OptionalInfoToggle, OptionalDetailsPanel);
        await Name.FillAsync(asset.Name);

        if (asset.WarrantyMonths is { } months)
        {
            await WarrantyMonths.FillAsync(months.ToString(CultureInfo.InvariantCulture));
        }
    }

    // Order number, purchase date and cost live in the "Order Related Information" panel, collapsed by default.
    private async Task FillOrderInformationAsync(AssetData asset)
    {
        await ExpandAsync(OrderInfoToggle, OrderDetailsPanel);
        await OrderNumber.FillAsync(asset.OrderNumber);

        if (asset.PurchaseDate is { } purchased)
        {
            // Snipe-IT's bootstrap-datetimepicker (with useCurrent) replaces typed text with today's
            // date, so set the date through the widget's own API, as picking it from the calendar
            // would, then confirm the input holds it.
            var value = purchased.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            await PurchaseDate.EvaluateAsync(
                "(input, date) => $(input).closest('[data-provide=\"datetimepicker\"]').data('DateTimePicker').date(date)",
                value);
            await Expect(PurchaseDate).ToHaveValueAsync(value);
        }

        if (asset.PurchaseCost is { } cost)
        {
            await PurchaseCost.FillAsync(cost.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }

    private async Task ExpandAsync(ILocator toggle, ILocator panel)
    {
        if (!await panel.IsVisibleAsync())
        {
            await toggle.ClickAsync();
        }
        await Expect(panel).ToBeVisibleAsync();
    }
}
