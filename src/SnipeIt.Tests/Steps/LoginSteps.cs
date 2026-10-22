using SnipeIt.Tests.Config;
using SnipeIt.Tests.Pages;

namespace SnipeIt.Tests.Steps;

[Binding]
public sealed class LoginSteps
{
    private readonly IPage _page;

    public LoginSteps(IPage page) => _page = page;

    [Given("I am logged in as an administrator")]
    public async Task GivenIAmLoggedInAsAnAdministrator()
    {
        var settings = TestSettings.Current;
        var login = new LoginPage(_page);
        await login.GotoAsync();
        await login.LoginAsync(settings.Username, settings.Password);
    }
}
