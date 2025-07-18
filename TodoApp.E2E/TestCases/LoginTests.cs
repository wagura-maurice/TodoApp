using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Linq;

namespace TodoApp.E2E.TestCases
{
    [TestFixture]
    [Category("Authentication")]
    public class LoginTests : IDisposable
    {
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private const string TestUsername = "wagura465@gmail.com";
        private const string TestPassword = "Qwerty123!";
        private readonly IPlaywright _playwright;

        public LoginTests()
        {
            _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
            _browser = _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500,
            }).GetAwaiter().GetResult();

            _context = _browser.NewContextAsync().GetAwaiter().GetResult();
            _page = _context.NewPageAsync().GetAwaiter().GetResult();
        }

        [SetUp]
        public async Task Setup()
        {
            // Navigate to the login page before each test
            await _page.GotoAsync("http://localhost:5001/Identity/Account/Login");
            
            // Log all browser console messages
            _page.Console += (_, msg) => TestContext.Out.WriteLine($"CONSOLE: {msg.Type}: {msg.Text}");

            // Log all page errors
            _page.PageError += (_, msg) => TestContext.Out.WriteLine($"PAGE ERROR: {msg}");

            // Log all requests and responses
            _page.Request += (_, request) => TestContext.Out.WriteLine($"REQUEST: {request.Method} {request.Url}");
            _page.Response += (_, response) =>
            {
                if (response != null)
                {
                    TestContext.Out.WriteLine($"RESPONSE: {response.Status} {response.Url}");
                }
            };
        }

        [TearDown]
        public async Task TearDown()
        {
            // Clean up after each test
            try
            {
                // Take a screenshot before closing
                if (_page != null)
                {
                    var screenshotBytes = await _page.ScreenshotAsync();
                    var screenshotPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"test-{TestContext.CurrentContext.Test.Name}.png");
                    await File.WriteAllBytesAsync(screenshotPath, screenshotBytes);
                    TestContext.AddTestAttachment(screenshotPath);
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to take screenshot: {ex.Message}");
            }
        }

        [Test]
        public async Task CanLoginWithValidCredentials()
        {
            // Navigate to the login page
            await _page.GotoAsync("http://localhost:5001/Identity/Account/Login");

            // Wait for the login form to be visible
            await _page.WaitForSelectorAsync("#account", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
            
            // Log the login form HTML for debugging
            var loginFormHtml = await _page.InnerHTMLAsync("#account");
            TestContext.Out.WriteLine($"Login form HTML: {loginFormHtml}");

            // Fill in the login form using JavaScript to ensure the values are set
            await _page.EvaluateAsync($@"
                document.querySelector('input[name=\'Input.Email\']').value = '{TestUsername.Replace("'", "\\'")}';
                document.querySelector('input[name=\'Input.Password\']').value = '{TestPassword.Replace("'", "\\'")}';
            ");

            // Submit the form using JavaScript
            await _page.EvaluateAsync("document.querySelector('form#account').submit()");

            // Wait for navigation to complete after form submission
            try
            {
                // Wait for either the Todo page to load or the login form to be hidden
                await Task.WhenAny(
                    _page.WaitForURLAsync(url => url.Contains("/Todo") || url.EndsWith("/"), new PageWaitForURLOptions { Timeout = 5000 }),
                    _page.WaitForSelectorAsync("#account", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 })
                );
            }
            catch (TimeoutException)
            {
                // If we timed out, check if we're on the login page with an error
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('.validation-summary-errors')?.innerText || ''");
                
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    throw new Exception($"Login failed with error: {errorMessage.Trim()}");
                }
                
                // Check if we're still on the login page
                if (_page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Login failed: Still on login page after submission");
                }
                
                // If we're not on the login page but not on the Todo page, log the current URL
                if (!_page.Url.Contains("/Todo") && !_page.Url.EndsWith("/"))
                {
                    throw new Exception($"Unexpected page after login: {_page.Url}");
                }
            }
            
            // Verify we're not on the login page anymore
            if (_page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Login failed: Still on login page after submission");
            }
            
            // Verify we were redirected to the Todo page
            if (!_page.Url.Contains("/Todo") && !_page.Url.EndsWith("/"))
            {
                throw new Exception($"Expected to be redirected to Todo page after login, but was: {_page.Url}");
            }
            
            // Verify the login form is not visible
            var loginFormVisible = await _page.IsVisibleAsync("#account");
            if (loginFormVisible)
            {
                throw new Exception("Login form is still visible after login attempt");
            }
            
            TestContext.Out.WriteLine($"Login successful! Redirected to: {_page.Url}");
        }

        [Test]
        public async Task CanLogoutAfterLogin()
        {
            // First, log in
            await CanLoginWithValidCredentials();
            
            // Verify we're logged in by checking for the user email in the navbar
            var userEmailInNavbar = await _page.QuerySelectorAsync("a[title='Manage']");
            Assert.That(userEmailInNavbar, Is.Not.Null, "User email should be visible in navbar after login");
            
            // Click the user email to open the dropdown
            await userEmailInNavbar!.ClickAsync();
            
            // Find and click the logout button in the dropdown
            var logoutForm = await _page.QuerySelectorAsync("form[action*='Logout']");
            Assert.That(logoutForm, Is.Not.Null, "Logout form should be present in the dropdown");
            
            // Submit the logout form
            await logoutForm!.EvaluateAsync("form => form.submit()");
            
            // Wait for navigation to complete
            await _page.WaitForURLAsync(url => url.EndsWith("/") || url.Contains("Identity/Account/Login"));
            
            // Verify we're on the home page or login page after logout
            Assert.That(_page.Url, Does.Match("(?:/$|Identity/Account/Login)"), 
                $"Should be redirected to home page or login page after logout, but was: {_page.Url}");
            
            // Verify the login link is visible in the navbar
            var loginLink = await _page.QuerySelectorAsync("a[href*='Account/Login']");
            Assert.That(loginLink, Is.Not.Null, "Login link should be visible after logout");
            
            // Verify the user email is no longer in the navbar
            userEmailInNavbar = await _page.QuerySelectorAsync("a[title='Manage']");
            Assert.That(userEmailInNavbar, Is.Null, "User email should not be visible in navbar after logout");
            
            TestContext.Out.WriteLine("Logout successful!");
        }

        public void Dispose()
        {
            // Clean up resources
            if (_page != null)
            {
                _page.CloseAsync().GetAwaiter().GetResult();
            }
            if (_context != null)
            {
                _context.CloseAsync().GetAwaiter().GetResult();
            }
            if (_browser != null)
            {
                _browser.CloseAsync().GetAwaiter().GetResult();
            }
            _playwright?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
