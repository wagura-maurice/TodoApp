using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace TodoApp.E2ETests;

public class ScreenshotTests : BaseTest, IAsyncLifetime
{
    private readonly string _screenshotsDir;

    public ScreenshotTests()
    {
        _screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        if (!Directory.Exists(_screenshotsDir))
        {
            Directory.CreateDirectory(_screenshotsDir);
        }
    }

    [Fact]
    public async Task TakeLoginPageScreenshot()
    {
        try
        {
            if (Page == null) throw new InvalidOperationException("Page is not initialized");
            
            // Navigate to the login page
            await Page.GotoAsync($"{BaseUrl}/Identity/Account/Login");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Take a full page screenshot
            await TakeScreenshotAsync("login-page");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TakeLoginPageScreenshot: {ex}");
            if (Page != null)
            {
                await TakeScreenshotAsync("login-page-error");
            }
            throw;
        }
    }

    public new async Task InitializeAsync() => await base.InitializeAsync();
    public new async Task DisposeAsync() => await base.DisposeAsync();
}
