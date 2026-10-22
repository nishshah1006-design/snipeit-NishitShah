using SnipeIt.Tests.Models;

namespace SnipeIt.Tests.Support;

/// <summary>
/// What a scenario has done so far, shared between its steps. Reqnroll creates
/// one instance per scenario and injects it into each step definition class.
/// </summary>
public sealed class AssetScenarioState
{
    private AssetData? _asset;
    private string? _selectedUser;
    private ILocator? _listRow;

    public AssetData Asset
    {
        get => _asset ?? throw MissingStep("an asset has been created");
        set => _asset = value;
    }

    /// <summary>The assignee as shown in the user dropdown, e.g. "First Last (username) - #123".</summary>
    public string SelectedUser
    {
        get => _selectedUser ?? throw MissingStep("the asset has been checked out to a user");
        set => _selectedUser = value;
    }

    /// <summary>The assignee's full name, as pages other than the dropdown display it.</summary>
    public string SelectedUserName => UserDisplay.FullName(SelectedUser);

    public ILocator ListRow
    {
        get => _listRow ?? throw MissingStep("the asset has been found in the assets list");
        set => _listRow = value;
    }

    private static InvalidOperationException MissingStep(string precondition) =>
        new($"This step expects that {precondition} earlier in the scenario.");
}
