using CompanyPlannerCsvPerser.Configuration;
using CompanyPlannerCsvPerser.Mhtml;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace CompanyPlannerCsvPerser.BrowserAutomation;

public sealed class PlaywrightBrowserAutomationService : IBrowserAutomationService
{
    private readonly BrowserOptions _browserOptions;
    private readonly IMhtmlLoader _mhtmlLoader;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _disposed;

    public PlaywrightBrowserAutomationService(
        IOptions<BrowserOptions> browserOptions,
        IMhtmlLoader mhtmlLoader)
    {
        _browserOptions = browserOptions.Value;
        _mhtmlLoader = mhtmlLoader;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _playwright = await PlaywrightBrowserInstaller.CreatePlaywrightAsync();
        _browser = await PlaywrightBrowserInstaller.LaunchChromiumAsync(
            _playwright,
            new BrowserTypeLaunchOptions
            {
                Headless = _browserOptions.Headless,
                SlowMo = _browserOptions.SlowMo
            });

        var context = await _browser.NewContextAsync();
        _page = await context.NewPageAsync();
        _page.SetDefaultTimeout(_browserOptions.NavigationTimeoutMs);
    }

    public async Task<string> LoadLocalMhtmlAsync(string mhtmlPath, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        var document = _mhtmlLoader.Load(mhtmlPath);
        var htmlWithBase = $"<base href=\"{document.BaseUrl}\">{document.Html}";
        await _page!.SetContentAsync(htmlWithBase);

        return await _page.ContentAsync();
    }

    public async Task NavigateToAsync(string url, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        await _page!.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
    }

    public async Task WaitForSelectorAsync(string selector, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached
        });
    }

    public async Task<string> GetPageContentAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        return await _page!.ContentAsync();
    }

    public Task<string> GetCurrentUrlAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        return Task.FromResult(_page!.Url);
    }

    public async Task<bool> TryClickAsync(string selector, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        var locator = _page!.Locator(selector);
        if (await locator.CountAsync() == 0)
        {
            return false;
        }

        await locator.First.ClickAsync();
        return true;
    }

    public async Task<bool> TryClickByTextAsync(string selector, string text, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        var locator = _page!.Locator(selector).Filter(new LocatorFilterOptions
        {
            HasTextString = text
        });

        if (await locator.CountAsync() == 0)
        {
            return false;
        }

        await locator.First.ClickAsync();
        return true;
    }

    public async Task<bool> TryClickExactTextAsync(string selector, string text, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        var locator = _page!.Locator(selector).Filter(new LocatorFilterOptions
        {
            HasTextRegex = new System.Text.RegularExpressions.Regex(
                $"^{System.Text.RegularExpressions.Regex.Escape(text)}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)
        });

        if (await locator.CountAsync() == 0)
        {
            return false;
        }

        await locator.First.ClickAsync();
        return true;
    }

    public async Task<bool> TryClickLastAsync(string selector, CancellationToken cancellationToken = default)
    {
        EnsurePageReady();
        var locator = _page!.Locator(selector);
        var count = await locator.CountAsync();
        if (count == 0)
        {
            return false;
        }

        await locator.Nth(count - 1).ClickAsync();
        return true;
    }

    public Task WaitForManualLoginAsync(string prompt, CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine(prompt);
        Console.WriteLine();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.ReadLine();
        }, cancellationToken);
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_page is not null)
        {
            await _page.Context.CloseAsync();
            _page = null;
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }

    private void EnsurePageReady()
    {
        if (_page is null)
        {
            throw new InvalidOperationException("Browser has not been initialized. Call InitializeAsync first.");
        }
    }
}
