using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using TodoApp.E2E.Utils;

namespace TodoApp.E2E.TestCases
{
    [TestFixture]
    [Category("Login")]
    public class LoginWithValidCredentialsTests : IDisposable
    {
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private const string TestUsername = "wagura465@gmail.com";
        private const string TestPassword = "Qwerty123!";
        private readonly IPlaywright _playwright;
        private readonly string _baseUrl;

        public LoginWithValidCredentialsTests()
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
        public async Task Should_LoginSuccessfully_WithValidCredentials()
        {
            string testName = nameof(Should_LoginSuccessfully_WithValidCredentials);
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
                
                // Initialize test run and take a screenshot before login
                E2ETestReportManager.InitializeTestRun(testName);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-before-login");
                
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
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-after-login");
                
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
                await E2ETestReportManager.SaveTestReportAsync(testName, true, "Login successful");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-login-failed");
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('.validation-summary-errors')?.innerText || ''");
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = ex.Message;
                }
                
                TestContext.Out.WriteLine($"Login test failed: {errorMessage}");
                await E2ETestReportManager.SaveTestReportAsync(testName, false, $"Login failed: {errorMessage}");
                throw;
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

        private async Task SaveTestReportAsync(string testName, bool testResult, string? errorMessage = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testName))
                {
                    testName = TestContext.CurrentContext?.Test?.Name ?? "UnknownTest";
                }

                var report = new System.Text.StringBuilder();
                report.AppendLine("# Test Execution Report");
                report.AppendLine($"**Test Name:** {testName}");
                report.AppendLine($"**Execution Time:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"**Status:** {(testResult ? "✅ PASSED" : "❌ FAILED")}");
                
                if (_browser?.BrowserType != null)
                {
                    report.AppendLine($"**Browser:** {_browser.BrowserType.Name}");
                }
                
                if (_page?.ViewportSize != null)
                {
                    report.AppendLine($"**Viewport:** {_page.ViewportSize.Width}x{_page.ViewportSize.Height}");
                }
                
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    report.AppendLine("\n## Error Details");
                    report.AppendLine($"```\n{errorMessage}\n```");
                }
                
                // Save the report to a file
                var reportPath = Path.Combine(E2ETestReportManager.BaseReportsPath, $"{testName}_{DateTime.Now:yyyyMMddHHmmss}.md");
                var directoryPath = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                await File.WriteAllTextAsync(reportPath, report.ToString());
                TestContext.Out.WriteLine($"Test report saved to: {reportPath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Error saving test report: {ex.Message}");
            }
        }
    }
}
