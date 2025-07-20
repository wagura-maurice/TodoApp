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
    public class LoginWithInvalidCredentialsTests : IDisposable
    {
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private const string TestUsername = "nonexistent@example.com";
        private const string TestPassword = "WrongPassword123!";
        private readonly IPlaywright _playwright;
        private readonly string _baseUrl;

        public LoginWithInvalidCredentialsTests()
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
        public async Task Should_ShowError_WithInvalidCredentials()
        {
            string testName = nameof(Should_ShowError_WithInvalidCredentials);
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

                // Initialize test run and take a screenshot before login attempt
                E2ETestReportManager.InitializeTestRun(testName);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-before-login-attempt");
                
                // Fill in login form with invalid credentials
                TestContext.Out.WriteLine("Filling login form with invalid credentials...");
                await _page.FillAsync("input[name='Input.Email']", TestUsername);
                await _page.FillAsync("input[name='Input.Password']", TestPassword);
                
                // Click login button
                TestContext.Out.WriteLine("Submitting login form...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for error message to appear
                var errorMessageSelector = ".validation-summary-errors, .text-danger, .field-validation-error";
                await _page.WaitForSelectorAsync(errorMessageSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
                
                // Take a screenshot after login attempt
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-after-login-attempt");
                
                // Verify error message is displayed
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('.validation-summary-errors')?.innerText || " +
                    "document.querySelector('.text-danger')?.innerText || " +
                    "document.querySelector('.field-validation-error')?.innerText || ''");
                
                TestContext.Out.WriteLine($"Error message displayed: {errorMessage}");
                
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "No error message was displayed for invalid login attempt");
                Assert.That(errorMessage, Does.Contain("Invalid").Or.Contains("incorrect").IgnoreCase, 
                    "Error message does not indicate invalid login");
                
                // Verify we're still on the login page
                Assert.That(_page.Url.ToLower().Contains("login"), Is.True, 
                    "Expected to remain on login page after invalid login attempt");
                
                TestContext.Out.WriteLine("Invalid login test passed successfully!");
                // Test passed
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-error-message-visible");
                var pageContent = await _page.ContentAsync();
                TestContext.Out.WriteLine($"Page content: {pageContent}");
                
                TestContext.Out.WriteLine($"Invalid login test failed: {ex.Message}");
                throw;
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
