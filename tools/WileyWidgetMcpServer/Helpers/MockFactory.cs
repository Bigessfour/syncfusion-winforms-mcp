using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;
using System;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Factory for creating mock objects needed for testing.
/// </summary>
public static class MockFactory
{
    static MockFactory()
    {
        // Register Syncfusion license from environment variable to prevent license dialogs
        try
        {
            var licenseKey = System.Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
            if (!string.IsNullOrEmpty(licenseKey))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
        }
        catch
        {
            // Suppress license registration errors - trial/development mode
        }
    }
    private static IServiceProvider CreateRealServiceProvider()
    {
        var services = DependencyInjection.CreateServiceCollection(includeDefaults: true);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        });
    }

    /// <summary>
    /// Creates a mock MainForm using the real DI container configured for tests.
    /// This avoids null stubs and exercises production registrations.
    /// </summary>
    public static MainForm CreateMockMainForm()
    {
        var sp = CreateTestServiceProvider();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<MainForm>>();
        var reportViewerOptions = sp.GetRequiredService<ReportViewerLaunchOptions>();
        var themeService = sp.GetRequiredService<IThemeService>();
        var windowStateService = sp.GetRequiredService<IWindowStateService>();
        var fileImportService = sp.GetRequiredService<IFileImportService>();

        // Ensure the argument order matches the MainForm constructor
        return new MainForm(sp, config, logger, reportViewerOptions, themeService, windowStateService, fileImportService);
    }

    /// <summary>
    /// Returns a test IServiceProvider backed by the real service registrations.
    /// </summary>
    public static IServiceProvider CreateTestServiceProvider()
    {
        return CreateRealServiceProvider();
    }

    /// <summary>
    /// Creates a mock IServiceScopeFactory for testing.
    /// </summary>
    public static IServiceScopeFactory CreateMockServiceScopeFactory()
    {
        return Mock.Of<IServiceScopeFactory>();
    }

    /// <summary>
    /// Creates a mock ILogger for the specified type.
    /// </summary>
    public static object CreateMockLogger(Type loggerType)
    {
        if (loggerType.IsGenericType && loggerType.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            var genericArg = loggerType.GetGenericArguments()[0];
            var method = typeof(MockFactory).GetMethod("CreateMockLoggerGeneric", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return method!.MakeGenericMethod(genericArg).Invoke(null, null)!;
        }
        return Mock.Of<ILogger>();
    }

    private static ILogger<T> CreateMockLoggerGeneric<T>()
    {
        return Mock.Of<ILogger<T>>();
    }

    /// <summary>
    /// Creates a mock instance of the specified interface type.
    /// </summary>
    public static object CreateMockInterface(Type interfaceType)
    {
        var ofMethod = typeof(Moq.Mock).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Of" && m.IsGenericMethod && m.GetParameters().Length == 0);

        if (ofMethod != null)
        {
            var genericMethod = ofMethod.MakeGenericMethod(interfaceType);
            return genericMethod.Invoke(null, null)!;
        }

        throw new InvalidOperationException($"Cannot create mock for type {interfaceType}");
    }
}
