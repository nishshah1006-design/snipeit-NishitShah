namespace SnipeIt.Tests.Models;

/// <summary>The data used to create an asset, and later to verify it.</summary>
public sealed record AssetData(
    string AssetTag,
    string Name,
    string Serial,
    string OrderNumber,
    string ModelName,
    string StatusName,
    string Notes)
{
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchaseCost { get; init; }
    public int? WarrantyMonths { get; init; }

    /// <summary>Snipe-IT derives the warranty expiry from the purchase date plus the warranty length.</summary>
    public DateOnly? WarrantyExpires =>
        PurchaseDate is { } purchased && WarrantyMonths is { } months ? purchased.AddMonths(months) : null;

    /// <summary>
    /// Text typed into the model dropdown's search box. Punctuation is stripped
    /// (e.g. the inch mark in Macbook Pro 13") so the AJAX search stays forgiving.
    /// </summary>
    public string ModelSearchTerm => ToSearchTerm(ModelName);

    /// <summary>
    /// Builds an asset of the given model with unique identifiers so the test
    /// never collides with other people's data on the shared demo instance.
    /// </summary>
    public static AssetData CreateUnique(string modelName, string statusName)
    {
        var runId = $"{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}".ToUpperInvariant();

        return new AssetData(
            AssetTag: $"QA-{runId}",
            Name: $"{ToSearchTerm(modelName)} QA {runId}",
            Serial: $"SN-{runId}",
            OrderNumber: $"PO-{runId}",
            ModelName: modelName,
            StatusName: statusName,
            Notes: $"Created by Playwright automation run {runId}");
    }

    private static string ToSearchTerm(string modelName) =>
        Regex.Replace(modelName, @"[^\w\s]", string.Empty).Trim();
}
