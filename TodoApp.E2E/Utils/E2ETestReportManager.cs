using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TodoApp.E2E.Utils
{
    /// <summary>
    /// Provides functionality for managing E2E test reports and screenshots
    /// </summary>
    public static class E2ETestReportManager
    {
        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
        public static readonly string BaseReportsPath = Path.Combine(BaseDirectory, "TestReports");
        private static string _currentTestRunPath = string.Empty;
        private static string _currentTestName = string.Empty;
        private static int _testCaseNumber = 1;
        private static readonly List<string> _screenshots = new();

        /// <summary>
        /// Initializes a new test run
        /// </summary>
        /// <param name="testName">Name of the test</param>
        public static void InitializeTestRun(string testName)
        {
            if (string.IsNullOrEmpty(testName))
                throw new ArgumentException("Test name cannot be null or empty", nameof(testName));

            _currentTestName = testName;
            _testCaseNumber = 1;
            _screenshots.Clear();

            try
            {
                // Ensure base reports directory exists
                if (!Directory.Exists(BaseReportsPath))
                {
                    Directory.CreateDirectory(BaseReportsPath);
                }

                // Create a timestamped directory for this test run
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentTestRunPath = Path.Combine(BaseReportsPath, $"{testName}_{timestamp}");
                
                if (!Directory.Exists(_currentTestRunPath))
                {
                    Directory.CreateDirectory(_currentTestRunPath);
                }

                TestContext.Out.WriteLine($"Test report directory: {_currentTestRunPath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to initialize test run: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Captures a screenshot of the current page
        /// </summary>
        /// <param name="page">The Playwright page</param>
        /// <param name="stepName">Name of the test step</param>
        /// <returns>Path to the saved screenshot</returns>
        public static async Task<string> CaptureScreenshotAsync(IPage page, string stepName)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
                
            if (string.IsNullOrEmpty(stepName))
                throw new ArgumentException("Step name cannot be null or empty", nameof(stepName));

            try
            {
                if (string.IsNullOrEmpty(_currentTestRunPath))
                {
                    throw new InvalidOperationException("Test run not initialized. Call InitializeTestRun first.");
                }

                // Sanitize the step name for use in filenames
                var safeStepName = string.Join("", stepName.Split(Path.GetInvalidFileNameChars()));
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var screenshotPath = Path.Combine(_currentTestRunPath, $"screenshot_{_testCaseNumber:D3}_{safeStepName}_{timestamp}.png");
                
                await page.ScreenshotAsync(new PageScreenshotOptions 
                { 
                    Path = screenshotPath,
                    FullPage = true
                });
                
                _screenshots.Add(screenshotPath);
                TestContext.Out.WriteLine($"Screenshot saved: {screenshotPath}");
                _testCaseNumber++;
                
                return screenshotPath;
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to capture screenshot: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves a test report with the given details
        /// </summary>
        /// <param name="testName">Name of the test</param>
        /// <param name="passed">Whether the test passed</param>
        /// <param name="message">Optional message to include in the report</param>
        /// <returns>Path to the saved report</returns>
        public static async Task<string> SaveTestReportAsync(string testName, bool passed, string? message = null)
        {
            if (string.IsNullOrEmpty(testName))
                throw new ArgumentException("Test name cannot be null or empty", nameof(testName));
                
            try
            {
                if (string.IsNullOrEmpty(_currentTestRunPath))
                    throw new InvalidOperationException("Test run not initialized. Call InitializeTestRun first.");

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var reportPath = Path.Combine(_currentTestRunPath, $"test_report_{testName}_{timestamp}.txt");

                var reportContent = new StringBuilder();
                reportContent.AppendLine($"Test: {testName}");
                reportContent.AppendLine($"Status: {(passed ? "PASSED" : "FAILED")}");
                reportContent.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                reportContent.AppendLine($"Message: {message ?? "N/A"}");
                
                // Include list of screenshots if any were taken
                if (_screenshots.Count > 0)
                {
                    reportContent.AppendLine("\nScreenshots:");
                    foreach (var screenshot in _screenshots)
                    {
                        reportContent.AppendLine(screenshot);
                    }
                }

                await File.WriteAllTextAsync(reportPath, reportContent.ToString());
                TestContext.Out.WriteLine($"Test report saved: {reportPath}");
                
                return reportPath;
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to save test report: {ex.Message}");
                throw;
            }
        }
    }
}
