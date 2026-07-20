namespace CompanyPlannerCsvPerser.BrowserAutomation;

public interface IBrowserAutomationService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<string> LoadLocalMhtmlAsync(string mhtmlPath, CancellationToken cancellationToken = default);
    Task NavigateToAsync(string url, CancellationToken cancellationToken = default);
    Task WaitForSelectorAsync(string selector, CancellationToken cancellationToken = default);
    Task<string> GetPageContentAsync(CancellationToken cancellationToken = default);
    Task<string> GetCurrentUrlAsync(CancellationToken cancellationToken = default);
    Task<bool> TryClickAsync(string selector, CancellationToken cancellationToken = default);
    Task<bool> TryClickByTextAsync(string selector, string text, CancellationToken cancellationToken = default);
    Task<bool> TryClickExactTextAsync(string selector, string text, CancellationToken cancellationToken = default);
    Task<bool> TryClickLastAsync(string selector, CancellationToken cancellationToken = default);
    Task WaitForManualLoginAsync(string prompt, CancellationToken cancellationToken = default);
    Task DisposeAsync();
}
