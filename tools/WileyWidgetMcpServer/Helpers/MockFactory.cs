using Microsoft.Extensions.Logging;
using Moq;
using WileyWidget.WinForms.Forms;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Factory for creating mock objects needed for testing.
/// </summary>
public static class MockFactory
{
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
    /// Creates a mock MainForm for isolated form testing using the parameterless constructor.
    /// The MainForm will have a TestServiceProvider injected so GetRequiredService returns mocks.
    /// </summary>
    public static MainForm CreateMockMainForm()
    {
        IServiceProvider sp = new TestServiceProvider();
        Microsoft.Extensions.Configuration.IConfiguration config = Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>();
        ILogger<MainForm> logger = Mock.Of<ILogger<MainForm>>();
        WileyWidget.WinForms.Configuration.ReportViewerLaunchOptions reportViewerOptions = Mock.Of<WileyWidget.WinForms.Configuration.ReportViewerLaunchOptions>();
        WileyWidget.WinForms.Services.IThemeService themeService = Mock.Of<WileyWidget.WinForms.Services.IThemeService>();

        MainForm mainForm = new MainForm(sp, config, logger, reportViewerOptions, themeService);

        return mainForm;
    }

    /// <summary>
    /// Returns a test IServiceProvider that will supply Mock.Of<T>() instances for requested services.
    /// </summary>
    public static IServiceProvider CreateTestServiceProvider()
    {
        return new TestServiceProvider();
    }
}
