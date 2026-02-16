using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WileyWidget.Services.Abstractions;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Factory for creating mock objects needed for testing.
/// </summary>
public static class MockFactory
{
    public static IConfiguration CreateTestConfiguration()
    {
        // Headless / MCP validation configuration:
        // - Treat as UI test harness to skip expensive startup paths.
        // - Disable docking and auto-show behavior to avoid background threads that can destabilize stdio servers.
        var dict = new Dictionary<string, string?>
        {
            ["UI:IsUiTestHarness"] = "true",
            ["UI:UseSyncfusionDocking"] = "false",
            ["UI:AutoShowDashboard"] = "false",
            ["UI:AutoShowPanels"] = "false",
            ["UI:MinimalMode"] = "true",
            ["UI:DefaultTheme"] = "Office2019Colorful",
            ["Diagnostics:VerboseFirstChanceExceptions"] = "false"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    /// <summary>
    /// Lightweight IServiceProvider used for tests. Returns a Mock.Of<T>() for requested interfaces.
    /// </summary>
    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            try
            {
                if (serviceType == typeof(IServiceProvider)) return this;

                // Prefer Mock.Of<T>() to generate a lightweight mock instance
                var ofMethod = typeof(Moq.Mock).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Of" && m.IsGenericMethod && m.GetParameters().Length == 0);

                if (ofMethod != null)
                {
                    return ofMethod.MakeGenericMethod(serviceType).Invoke(null, null);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Creates a mock MainForm for isolated form testing.
    /// The MainForm will have all required dependencies mocked.
    /// </summary>
    public static MainForm CreateMockMainForm(bool enableMdi = false)
    {
        var sp = new TestServiceProvider();
        var config = CreateTestConfiguration();
        var logger = Mock.Of<ILogger<MainForm>>();
        var reportOptions = ReportViewerLaunchOptions.Disabled;
        var themeService = Mock.Of<IThemeService>();
        var windowStateService = Mock.Of<IWindowStateService>();
        var fileImportService = Mock.Of<IFileImportService>();

        var mainForm = new MainForm(sp, config, logger, reportOptions, themeService, windowStateService, fileImportService);

        return mainForm;
    }

    /// <summary>
    /// Returns a test IServiceProvider that will supply Mock.Of<T>() instances for requested services.
    /// </summary>
    public static IServiceProvider CreateTestServiceProvider() => new TestServiceProvider();
}
