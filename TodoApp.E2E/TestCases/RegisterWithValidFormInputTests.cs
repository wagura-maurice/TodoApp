using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using TodoApp.E2E.Utils;

namespace TodoApp.E2E.TestCases
{
    [TestFixture]
    [Category("Registration")]
    public class RegisterWithValidFormInputTests : IDisposable
    {
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private readonly IPlaywright _playwright;
        private readonly string _baseUrl;

        public RegisterWithValidFormInputTests()
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
        public async Task Should_RegisterSuccessfully_WithValidFormInput()
        {
            try
            {
                // Initialize test run with the test name
                var testName = nameof(Should_RegisterSuccessfully_WithValidFormInput);
                E2ETestReportManager.InitializeTestRun(testName);
                
                // Generate unique test data
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var testEmail = $"testuser_{timestamp}@example.com";
                var testPassword = "P@ssw0rd123!";

                // Navigate to the registration page
                var registerUrl = $"{_baseUrl}/Identity/Account/Register";
                TestContext.Out.WriteLine($"Navigating to registration page: {registerUrl}");
                
                await _page.GotoAsync(registerUrl);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-registration-page-loaded");

                // Fill in registration form
                TestContext.Out.WriteLine("Filling registration form...");
                await _page.FillAsync("input[name='Input.Email']", testEmail);
                await _page.FillAsync("input[name='Input.Password']", testPassword);
                await _page.FillAsync("input[name='Input.ConfirmPassword']", testPassword);
                
                // Capture filled form
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-registration-form-filled");

                // Submit the form
                TestContext.Out.WriteLine("Submitting registration form...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for navigation and capture result
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-post-registration-page");

                // Verify successful registration
                var currentUrl = _page.Url.ToLower();
                var isOnLoginPage = currentUrl.Contains("/identity/account/login");
                var isOnHomePage = currentUrl.EndsWith("/") || currentUrl.Contains("/home/index");
                
                if (isOnLoginPage || isOnHomePage)
                {
                    TestContext.Out.WriteLine("Registration successful - Redirected to expected page");
                    await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-registration-successful");
                }
                else
                {
                    var errorMessage = await _page.EvaluateAsync<string>(
                        "document.querySelector('.validation-summary-errors')?.innerText || ''");
                    
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        TestContext.Out.WriteLine($"Registration failed with error: {errorMessage}");
                        await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-registration-failed");
                        Assert.Fail($"Registration failed: {errorMessage}");
                    }
                    else
                    {
                        await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-unexpected-page");
                        Assert.Fail("Unexpected page after registration attempt");
                    }
                }
                
                // Clean up (if needed) - Delete test user
                // This would typically be done in a teardown method or via API
                
                Assert.Pass("Registration test completed successfully");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-registration-test-failed");
                TestContext.Out.WriteLine($"Registration test failed: {ex}");
                throw;
            }
        }

        [Test]
        public async Task CanRegisterNewUser()
        {
            try
            {
                // Initialize test run with the test name
                E2ETestReportManager.InitializeTestRun(nameof(CanRegisterNewUser));
                
                // Generate unique test data
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var testEmail = $"testuser_{timestamp}@example.com";
                var testPassword = "P@ssw0rd123!";

                // Navigate to the registration page
                var registerUrl = $"{_baseUrl}/Identity/Account/Register";
                TestContext.Out.WriteLine($"Navigating to registration page: {registerUrl}");
                
                await _page.GotoAsync(registerUrl);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-registration-page-loaded");

                // Fill in registration form
                TestContext.Out.WriteLine("Filling registration form...");
                await _page.FillAsync("input[name='Input.Email']", testEmail);
                await _page.FillAsync("input[name='Input.Password']", testPassword);
                await _page.FillAsync("input[name='Input.ConfirmPassword']", testPassword);
                
                // Capture filled form
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-registration-form-filled");

                // Submit the form
                TestContext.Out.WriteLine("Submitting registration form...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for navigation and capture result
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-post-registration-page");

                // Verify successful registration
                var currentUrl = _page.Url.ToLower();
                var isOnLoginPage = currentUrl.Contains("/identity/account/login");
                var isOnHomePage = currentUrl.EndsWith("/") || currentUrl.Contains("/home/index");
                
                if (isOnLoginPage || isOnHomePage)
                {
                    TestContext.Out.WriteLine("Registration successful - Redirected to expected page");
                    await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-registration-successful");
                }
                else
                {
                    var errorMessage = await _page.EvaluateAsync<string>(
                        "document.querySelector('.validation-summary-errors')?.innerText || ''");
                    
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        TestContext.Out.WriteLine($"Registration failed with error: {errorMessage}");
                        await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-registration-failed");
                        Assert.Fail($"Registration failed: {errorMessage}");
                    }
                    else
                    {
                        await E2ETestReportManager.CaptureScreenshotAsync(_page, "04-unexpected-page");
                        Assert.Fail("Unexpected page after registration attempt");
                    }
                }
                
                // Clean up (if needed) - Delete test user
                // This would typically be done in a teardown method or via API
                
                Assert.Pass("Registration test completed successfully");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-registration-test-failed");
                TestContext.Out.WriteLine($"Registration test failed: {ex}");
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
    }
}
