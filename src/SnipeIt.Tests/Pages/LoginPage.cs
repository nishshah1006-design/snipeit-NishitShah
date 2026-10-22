namespace SnipeIt.Tests.Pages;

public sealed class LoginPage : BasePage
{
    public LoginPage(IPage page) : base(page) { }

    private ILocator Username => Page.Locator("#username");
    private ILocator Password => Page.Locator("#password-field, input[name='password']").First;
    private ILocator LoginButton => Page.Locator("button#submit");

    public async Task GotoAsync()
    {
        await Page.GotoAsync("/login");
        await Expect(Username).ToBeVisibleAsync();
    }

    public async Task LoginAsync(string username, string password)
    {
        await Username.FillAsync(username);
        await Password.FillAsync(password);
        await LoginButton.ClickAsync();

        // A successful login leaves the login page and renders the app shell (sidebar).
        await Expect(Page).Not.ToHaveURLAsync(new Regex("/login"));
        await Expect(Page.Locator(".main-sidebar")).ToBeVisibleAsync();
    }
}
