using Microsoft.Playwright;

namespace CompanyPlannerCsvPerser.BrowserAutomation;

public static class PlaywrightBrowserInstaller
{
    public static async Task<IBrowser> LaunchChromiumAsync(
        IPlaywright playwright,
        BrowserTypeLaunchOptions options)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(options);
        }
        catch (PlaywrightException ex) when (IsMissingBrowserError(ex))
        {
            InstallChromium();
            return await playwright.Chromium.LaunchAsync(options);
        }
    }

    public static void InstallChromium()
    {
        Console.WriteLine();
        Console.WriteLine("Playwright Chromium is not installed for this app version.");
        Console.WriteLine("Downloading browsers (one-time setup, ~250 MB)...");
        Console.WriteLine();

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Playwright browser install failed with exit code {exitCode}. " +
                "Run './install-playwright-browsers.sh' from the repository root.");
        }

        Console.WriteLine();
        Console.WriteLine("Playwright Chromium installed successfully.");
        Console.WriteLine();
    }

    private static bool IsMissingBrowserError(PlaywrightException ex)
    {
        return ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("playwright.ps1 install", StringComparison.OrdinalIgnoreCase);
    }
}
