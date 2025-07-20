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
    public class RegisterWithInvalidFormInputTests : IDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly IPage _page;
        private readonly string _baseUrl;

        public RegisterWithInvalidFormInputTests()
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
        public async Task Should_ShowError_WithEmptyForm()
        {
            string testName = nameof(Should_ShowError_WithEmptyForm);
            try
            {
                // Initialize test run with the test name
                E2ETestReportManager.InitializeTestRun(testName);
                
                // Navigate to the registration page
                var registerUrl = $"{_baseUrl}/Identity/Account/Register";
                TestContext.Out.WriteLine($"Navigating to registration page: {registerUrl}");
                
                await _page.GotoAsync(registerUrl);
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-registration-page-loaded");

                // Submit the form without filling any fields
                TestContext.Out.WriteLine("Submitting empty registration form...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for client-side validation
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500); // Small delay to ensure validation messages are shown
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-form-submitted-with-errors");

                // Check for validation errors
                var emailError = await _page.IsVisibleAsync("#Input_Email-error");
                var passwordError = await _page.IsVisibleAsync("#Input_Password-error");
                var confirmPasswordError = await _page.IsVisibleAsync("#Input_ConfirmPassword-error");
                
                TestContext.Out.WriteLine($"Email error shown: {emailError}");
                TestContext.Out.WriteLine($"Password error shown: {passwordError}");
                TestContext.Out.WriteLine($"Confirm password error shown: {confirmPasswordError}");
                
                Assert.Multiple(() =>
                {
                    Assert.That(emailError, Is.True, "Email validation error should be shown");
                    Assert.That(passwordError, Is.True, "Password validation error should be shown");
                    Assert.That(confirmPasswordError, Is.True, "Confirm password validation error should be shown");
                });
                
                TestContext.Out.WriteLine("Empty form validation test passed successfully!");
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-validation-errors-shown");
                await E2ETestReportManager.SaveTestReportAsync(testName, true, "Empty form validation test passed successfully");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-validation-test-failed");
                await E2ETestReportManager.SaveTestReportAsync(testName, false, $"Validation test failed: {ex.Message}");
                TestContext.Out.WriteLine($"Validation test failed: {ex}");
                throw;
            }
        }

        [Test]
        public async Task Should_ShowError_WithInvalidEmail()
        {
            string testName = nameof(Should_ShowError_WithInvalidEmail);
            try
            {
                // Initialize test run with the test name
                E2ETestReportManager.InitializeTestRun(testName);
                
                // Navigate to the registration page
                var registerUrl = $"{_baseUrl}/Identity/Account/Register";
                TestContext.Out.WriteLine($"Navigating to registration page: {registerUrl}");
                
                await _page.GotoAsync(registerUrl);
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-registration-page-loaded");

                // Fill in registration form with invalid email
                TestContext.Out.WriteLine("Filling registration form with invalid email...");
                await _page.FillAsync("input[name='Input.Email']", "invalid-email");
                await _page.FillAsync("input[name='Input.Password']", "ValidPass123!");
                await _page.FillAsync("input[name='Input.ConfirmPassword']", "ValidPass123!");
                
                await Task.Delay(300); // Small delay to ensure fields are filled
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-form-filled-with-invalid-email");
                
                // Submit the form
                TestContext.Out.WriteLine("Submitting form with invalid email...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for validation
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500); // Small delay to ensure validation is complete
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-form-submitted-with-invalid-email");

                // Check for email validation error
                var emailError = await _page.IsVisibleAsync("#Input_Email-error");
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('#Input_Email-error')?.innerText || ''");
                
                TestContext.Out.WriteLine($"Email error shown: {emailError}");
                TestContext.Out.WriteLine($"Error message: {errorMessage}");
                
                Assert.That(emailError, Is.True, "Email validation error should be shown for invalid email");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Error message should not be empty");
                
                TestContext.Out.WriteLine($"Invalid email test passed successfully!");
                await E2ETestReportManager.SaveTestReportAsync(testName, true, "Invalid email test passed successfully");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-invalid-email-test-failed");
                await E2ETestReportManager.SaveTestReportAsync(testName, false, $"Invalid email test failed: {ex.Message}");
                TestContext.Out.WriteLine($"Invalid email test failed: {ex}");
                throw;
            }
        }

        [Test]
        public async Task Should_ShowError_WhenPasswordsDontMatch()
        {
            string testName = nameof(Should_ShowError_WhenPasswordsDontMatch);
            try
            {
                // Initialize test run with the test name
                E2ETestReportManager.InitializeTestRun(testName);
                
                // Navigate to the registration page
                var registerUrl = $"{_baseUrl}/Identity/Account/Register";
                TestContext.Out.WriteLine($"Navigating to registration page: {registerUrl}");
                
                await _page.GotoAsync(registerUrl);
                await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "01-registration-page-loaded");

                // Fill in registration form with mismatched passwords
                TestContext.Out.WriteLine("Filling registration form with mismatched passwords...");
                await _page.FillAsync("input[name='Input.Email']", "test@example.com");
                await _page.FillAsync("input[name='Input.Password']", "Password123!");
                await _page.FillAsync("input[name='Input.ConfirmPassword']", "DifferentPassword123!");
                
                await Task.Delay(300); // Small delay to ensure fields are filled
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "02-form-filled-with-mismatched-passwords");
                
                // Submit the form
                TestContext.Out.WriteLine("Submitting form with mismatched passwords...");
                await _page.ClickAsync("button[type='submit']");
                
                // Wait for validation
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500); // Small delay to ensure validation is complete
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "03-form-submitted-with-mismatched-passwords");

                // Check for password mismatch error
                var confirmPasswordError = await _page.IsVisibleAsync("#Input_ConfirmPassword-error");
                var errorMessage = await _page.EvaluateAsync<string>(
                    "document.querySelector('#Input_ConfirmPassword-error')?.innerText || ''");
                
                TestContext.Out.WriteLine($"Password mismatch error shown: {confirmPasswordError}");
                TestContext.Out.WriteLine($"Error message: {errorMessage}");
                
                Assert.That(confirmPasswordError, Is.True, "Password mismatch error should be shown");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Error message should not be empty");
                
                TestContext.Out.WriteLine($"Password mismatch test passed successfully!");
                await E2ETestReportManager.SaveTestReportAsync(testName, true, "Password mismatch test passed successfully");
            }
            catch (Exception ex)
            {
                await E2ETestReportManager.CaptureScreenshotAsync(_page, "error-password-mismatch-test-failed");
                await E2ETestReportManager.SaveTestReportAsync(testName, false, $"Password mismatch test failed: {ex.Message}");
                TestContext.Out.WriteLine($"Password mismatch test failed: {ex}");
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
