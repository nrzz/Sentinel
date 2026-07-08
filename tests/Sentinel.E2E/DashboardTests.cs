using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Sentinel.E2E;

[CollectionDefinition("BrowserE2E")]
public class BrowserE2ECollection : ICollectionFixture<BrowserE2EFixture>
{
}

[Collection("BrowserE2E")]
public class LoginPageTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public async Task InitializeAsync()
    {
        if (!BrowserE2EFixture.IsAvailable)
        {
            return;
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task LoginPage_DisplaysForm()
    {
        if (!BrowserE2EFixture.IsAvailable)
        {
            return;
        }

        await _page!.GotoAsync($"{BrowserE2EFixture.BaseUrl}/login");
        Assert.True(await _page.Locator("#email").IsVisibleAsync());
        Assert.True(await _page.Locator("#password").IsVisibleAsync());
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToOverview()
    {
        if (!BrowserE2EFixture.IsAvailable)
        {
            return;
        }

        await _page!.GotoAsync($"{BrowserE2EFixture.BaseUrl}/login");
        await _page.FillAsync("#email", "admin@sentinel.local");
        await _page.FillAsync("#password", "Admin123!");
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForURLAsync("**/");
        Assert.Matches(new Regex("/$|/overview$"), _page.Url);
    }
}

[Collection("BrowserE2E")]
public class LogsPageTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public async Task InitializeAsync()
    {
        if (!BrowserE2EFixture.IsAvailable)
        {
            return;
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task LogsPage_ShowsSearchControls()
    {
        if (!BrowserE2EFixture.IsAvailable)
        {
            return;
        }

        await _page!.GotoAsync($"{BrowserE2EFixture.BaseUrl}/login");
        await _page.FillAsync("#email", "admin@sentinel.local");
        await _page.FillAsync("#password", "Admin123!");
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForURLAsync("**/");

        await _page.GotoAsync($"{BrowserE2EFixture.BaseUrl}/logs");
        Assert.True(await _page.Locator("input[placeholder='Search logs...']").IsVisibleAsync());
    }
}
