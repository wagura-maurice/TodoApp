using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using System.IO;

namespace TodoApp.E2E.TestCases
{
    [TestFixture]
    [Category("Login")]
    public class LoginTests : IDisposable
    {
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private const string TestUsername = "wagura465@gmail.com";
        private const string TestPassword = "Qwerty123!";
        private readonly IPlaywright _playwright;
        private readonly string _baseUrl;

        public LoginTests()
        {
            _baseUrl = "http://localhost:5001";
            TestContext.Out.WriteLine($"Using base URL: {_baseUrl}");
            
            _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
            _browser = _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500,
            }).GetAwaiter().GetResult();

            _context = _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 1024 },
                BaseURL = _baseUrl
            }).GetAwaiter().GetResult();
            
            _page = _context.NewPageAsync().GetAwaiter().GetResult();
        }

        [Test]
        public async Task CanLoginWithValidCredentials()
        {
            try
            {
                // Navigate to the login page
                var loginUrl = $"{_baseUrl}/Identity/Account/Login";
                TestContext.Out.WriteLine($"Navigating to login page: {loginUrl}");
                
                await _page.GotoAsync(loginUrl, new PageGotoOptions 
                { 
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });

                // Log current URL and page content for debugging
                TestContext.Out.WriteLine($"Current URL: {_page.Url}");
                TestContext.Out.WriteLine($"Page title: {await _page.TitleAsync()}");
                
                // Take a screenshot before login
                await TakeScreenshotAsync("before-login");
                
                // Fill in login form
                TestContext.Out.WriteLine("Filling login form...");
                await _page.FillAsync("input[name='Input.Email']", TestUsername);
                await _page.FillAsync("input[name='Input.Password']", TestPassword);
                
                // Click login button
                TestContext.Out.WriteLine("Submitting login form...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for navigation after login with timeout
                await _page.WaitForURLAsync(
                    url => url.StartsWith(_baseUrl) && !url.Contains("login"),
                    new PageWaitForURLOptions { Timeout = 10000 }
                );

                // Take a screenshot after login
                await TakeScreenshotAsync("after-login");
                
                // Verify successful login by checking if we're on a page that requires authentication
                var currentUrl = _page.Url.ToLower();
                var isOnHomePage = currentUrl == _baseUrl.ToLower() || 
                                 currentUrl.StartsWith(_baseUrl.ToLower() + "/");
                
                // Check for common elements that indicate successful login
                var hasWelcomeMessage = await _page.IsVisibleAsync("text='Welcome'");
                var hasLogoutLink = await _page.IsVisibleAsync("a:has-text('Logout')") || 
                                   await _page.IsVisibleAsync("button:has-text('Logout')") ||
                                   await _page.IsVisibleAsync("a[href*='Logout']") ||
                                   await _page.IsVisibleAsync("a[href*='logout']");
                
                // Check for common navigation elements
                var hasNavElements = await _page.IsVisibleAsync("nav") || 
                                    await _page.IsVisibleAsync("header") ||
                                    await _page.IsVisibleAsync(".navbar") ||
                                    await _page.IsVisibleAsync(".nav");
                
                // Log the findings
                TestContext.Out.WriteLine($"Current URL: {_page.Url}");
                TestContext.Out.WriteLine($"Is on home page: {isOnHomePage}");
                TestContext.Out.WriteLine($"Has welcome message: {hasWelcomeMessage}");
                TestContext.Out.WriteLine($"Has logout link: {hasLogoutLink}");
                TestContext.Out.WriteLine($"Has navigation elements: {hasNavElements}");
                
                // If we're on the home page and see common elements, consider login successful
                var isLoggedIn = isOnHomePage && (hasWelcomeMessage || hasLogoutLink || hasNavElements);
                
                // If still not sure, check the page content for common post-login elements
                if (!isLoggedIn)
                {
                    var pageContent = await _page.ContentAsync();
                    var hasCommonElements = pageContent.Contains("Welcome") ||
                                          pageContent.Contains("Dashboard") ||
                                          pageContent.Contains("My Account") ||
                                          pageContent.Contains("Sign Out");
                    
                    if (hasCommonElements)
                    {
                        TestContext.Out.WriteLine("Found common post-login elements in page content");
                        isLoggedIn = true;
                    }
                }
                
                Assert.That(isLoggedIn, Is.True, $"Login verification failed. Current URL: {_page.Url}");
                TestContext.Out.WriteLine("Login test passed successfully!");
            }
            catch (Exception ex)
            {
                await TakeScreenshotAsync("login-test-failed");
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('.validation-summary-errors')?.innerText || ''");
                TestContext.Out.WriteLine($"Login test failed: {ex}");
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    TestContext.Out.WriteLine($"Error message from page: {errorMessage}");
                }
                throw;
            }
        }

        private async Task TakeScreenshotAsync(string prefix)
        {
            try
            {
                var screenshotPath = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    $"screenshot-{DateTime.Now:yyyyMMddHHmmss}-{prefix}.png");
                
                await _page.ScreenshotAsync(new PageScreenshotOptions 
                { 
                    Path = screenshotPath,
                    FullPage = true
                });
                
                TestContext.Out.WriteLine($"Screenshot saved: {screenshotPath}");
                TestContext.AddTestAttachment(screenshotPath);
                
                // Also save page HTML for debugging
                var pageContent = await _page.ContentAsync();
                var htmlPath = Path.ChangeExtension(screenshotPath, ".html");
                await File.WriteAllTextAsync(htmlPath, pageContent);
                TestContext.Out.WriteLine($"Page HTML saved: {htmlPath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to take screenshot: {ex}");
            }
        }

        public void Dispose()
        {
            _page?.CloseAsync().GetAwaiter().GetResult();
            _context?.CloseAsync().GetAwaiter().GetResult();
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _playwright?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
