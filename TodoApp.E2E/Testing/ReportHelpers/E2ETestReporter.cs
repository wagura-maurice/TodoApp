using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TodoApp.E2E.Testing.ReportHelpers.Internal
{
    internal static class E2ETestReportManager
    {
        public static readonly string BaseReportsPath = Path.Combine(Directory.GetCurrentDirectory(), "TestReports");
        private static string _currentTestRunPath = string.Empty;
        private static string _currentTestName = string.Empty;
        private static int _testCaseNumber = 1;

        public static void InitializeTestRun(string testName)
        {
            _currentTestName = testName;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentTestRunPath = Path.Combine(BaseReportsPath, $"TestRun_{timestamp}_TC{_testCaseNumber:000}_{SanitizeFileName(testName)}");
            
            // Ensure the test run directory exists
            Directory.CreateDirectory(_currentTestRunPath);
            
            _testCaseNumber++;
        }
        
        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Replace(" ", "_");
        }
        
        private static string GetScreenshotsPath() => Path.Combine(_currentTestRunPath, "screenshots");
        private static string GetMetadataPath() => Path.Combine(_currentTestRunPath, "metadata");
        private static string GetMarkdownPath() => _currentTestRunPath;

        public static async Task<string> CaptureScreenshotAsync(IPage page, string stepName)
        {
            try
            {
                var testName = TestContext.CurrentContext?.Test?.Name ?? "UnknownTest";
                if (string.IsNullOrEmpty(_currentTestRunPath))
                {
                    InitializeTestRun(testName);
                }
                
                var screenshotsPath = GetScreenshotsPath();
                Directory.CreateDirectory(screenshotsPath);
                
                var safeStepName = string.Join("", stepName.Split(Path.GetInvalidFileNameChars()));
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var fileName = $"screenshot_{safeStepName}_{timestamp}.png".Replace(" ", "_");
                var filePath = Path.Combine(screenshotsPath, fileName);
                
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await page.ScreenshotAsync(new PageScreenshotOptions 
                { 
                    Path = filePath,
                    FullPage = true
                });
                
                // Also save the page HTML for debugging
                var htmlFilePath = Path.ChangeExtension(filePath, ".html");
                var pageContent = await page.ContentAsync();
                await File.WriteAllTextAsync(htmlFilePath, pageContent);
                
                TestContext.AddTestAttachment(filePath, $"{testName} - {stepName}");
                TestContext.Out.WriteLine($"Screenshot saved: {filePath}");
                TestContext.Out.WriteLine($"Page HTML saved: {htmlFilePath}");
                
                return filePath;
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to capture screenshot: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task SaveTestMetadataAsync(object metadata)
        {
            try
            {
                var metadataPath = GetMetadataPath();
                Directory.CreateDirectory(metadataPath);
                
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var fileName = $"test_metadata_{timestamp}.json";
                var filePath = Path.Combine(metadataPath, fileName);
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(metadata, options);
                await File.WriteAllTextAsync(filePath, json);
                
                TestContext.Out.WriteLine($"Metadata saved: {filePath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Failed to save metadata: {ex.Message}");
            }
        }

        public static async Task<string> SaveMarkdownReportAsync(string markdownContent, string? testName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentTestRunPath))
                {
                    testName ??= TestContext.CurrentContext?.Test?.Name ?? "UnknownTest";
                    if (string.IsNullOrWhiteSpace(testName))
                    {
                        testName = "UnknownTest";
                    }
                    InitializeTestRun(testName);
                }
                
                if (string.IsNullOrWhiteSpace(markdownContent))
                {
                    TestContext.Out.WriteLine("Warning: Empty markdown content provided for report");
                    markdownContent = "# Empty Test Report\nNo test results were recorded.";
                }
                
                var markdownPath = GetMarkdownPath();
                var fileName = $"test_report_{DateTime.Now:yyyyMMddHHmmss}.md";
                var filePath = Path.Combine(markdownPath, fileName);
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllTextAsync(filePath, markdownContent);
                TestContext.Out.WriteLine($"Markdown report saved: {filePath}");
                
                // Add the report as a test attachment
                TestContext.AddTestAttachment(filePath, "Test Execution Report");
                
                // Also output the report location to the test output
                TestContext.Out.WriteLine($"\n=== Test Report Location ===");
                TestContext.Out.WriteLine($"Report: {filePath}");
                TestContext.Out.WriteLine($"Screenshots: {Path.Combine(_currentTestRunPath, "screenshots")}");
                TestContext.Out.WriteLine($"Metadata: {Path.Combine(_currentTestRunPath, "metadata")}");
                TestContext.Out.WriteLine($"===========================\n");
                
                return filePath;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to save markdown report: {ex.Message}";
                TestContext.Out.WriteLine(errorMessage);
                TestContext.Error.WriteLine(errorMessage);
                return string.Empty;
            }
        }
    }
}
