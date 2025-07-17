using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace TodoApp.E2E
{
    [TestFixture]
    public class Tests : PageTest
    {
        private string _testResultsPath;
        private StringBuilder _testReport;
        private string _screenshotPath;

        [SetUp]
        public async Task Setup()
        {
            // Set up test results directory in a visible location
            _testResultsPath = Path.Combine(Directory.GetCurrentDirectory(), "test-results");
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(_testResultsPath);
            
            // Initialize test report
            _testReport = new StringBuilder();
            var testName = TestContext.CurrentContext.Test.Name;
            _screenshotPath = Path.Combine(_testResultsPath, $"{testName}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            
            _testReport.AppendLine($"# Test: {testName}");
            _testReport.AppendLine($"## Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _testReport.AppendLine("---");
            
            Console.WriteLine($"Screenshots will be saved to: {_screenshotPath}");
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                // Save the test report
                var reportPath = Path.Combine(_testResultsPath, "test-report.md");
                _testReport.AppendLine("\n---");
                _testReport.AppendLine($"## End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _testReport.AppendLine($"## Status: {(TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Passed ? "✅ PASSED" : "❌ FAILED")}");
                
                // Append to existing report or create new
                if (File.Exists(reportPath))
                {
                    File.AppendAllText(reportPath, "\n\n" + _testReport.ToString());
                }
                else
                {
                    File.WriteAllText(reportPath, _testReport.ToString());
                }
                
                TestContext.AddTestAttachment(reportPath, "Test Execution Report");
                
                if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    TestContext.WriteLine($"Test failed: {TestContext.CurrentContext.Result.Message}");
                    TestContext.WriteLine(TestContext.CurrentContext.Result.StackTrace);
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error during test teardown: {ex}");
            }
        }

        private void AddToReport(string message, bool isHeader = false)
        {
            var formattedMessage = isHeader 
                ? $"\n## {message}\n" 
                : $"- {DateTime.Now:HH:mm:ss.fff} - {message}";
            
            _testReport.AppendLine(formattedMessage);
            TestContext.WriteLine(message);
        }

        [Test]
        public async Task CanAccessPlaywrightWebsite()
        {
            AddToReport("Starting test: CanAccessPlaywrightWebsite");
            
            try
            {
                // Navigate to the website
                await Page.GotoAsync("https://playwright.dev/dotnet");
                AddToReport("Navigated to Playwright for .NET homepage");
                
                // Verify page title
                var title = await Page.TitleAsync();
                AddToReport($"Page title: {title}");
                Assert.That(title, Does.Contain("Playwright"), "Page title should contain 'Playwright'");
                
                // Take a screenshot
                await Page.ScreenshotAsync(new() { Path = _screenshotPath });
                AddToReport($"Screenshot saved to: {_screenshotPath}");
                
                // Add screenshot to report
                _testReport.AppendLine($"![Screenshot]({Path.GetFileName(_screenshotPath)})");
                _testReport.AppendLine("*Figure: Screenshot of the test run*");
                
                AddToReport("Test completed successfully");
            }
            catch (Exception ex)
            {
                AddToReport($"Test failed: {ex.Message}");
                AddToReport($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
