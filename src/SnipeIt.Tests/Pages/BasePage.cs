namespace SnipeIt.Tests.Pages;

public abstract class BasePage
{
    protected BasePage(IPage page) => Page = page;

    protected IPage Page { get; }

    protected ILocator SuccessAlert => Page.Locator(".alert-success").First;

    protected static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    protected static IPageAssertions Expect(IPage page) => Assertions.Expect(page);
}
