using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace TodoApp.E2ETests;

public class BaseTest : IAsyncLifetime
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IPage? Page { get; private set; }
    protected const string BaseUrl = "https://localhost:5001";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 50,
        });
        
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });
        
        Page = await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
            await Browser.DisposeAsync();
        Playwright?.Dispose();
    }

    protected async Task TakeScreenshotAsync(string name)
    {
        if (Page == null) return;
        
        var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        if (!Directory.Exists(screenshotsDir))
        {
            Directory.CreateDirectory(screenshotsDir);
        }

        var fileName = $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        var filePath = Path.Combine(screenshotsDir, fileName);
        await Page.ScreenshotAsync(new PageScreenshotOptions 
        { 
            Path = filePath,
            FullPage = true
        });
        Console.WriteLine($"Screenshot saved to: {filePath}");
    }
}
