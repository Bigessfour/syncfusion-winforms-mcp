namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Static initializer for MCP server that suppresses Syncfusion UI dialogs and license warnings.
/// Must be called before any form instantiation.
/// </summary>
internal static class McpServerInitializer
{
    static McpServerInitializer()
    {
        InitializeSyncfusionSilentMode();
    }

    public static void EnsureInitialized()
    {
        // Triggers static constructor
    }

    private static void InitializeSyncfusionSilentMode()
    {
        try
        {
            // Suppress Syncfusion license dialogs and warnings at process level
            System.Environment.SetEnvironmentVariable("SYNCFUSION_SILENT_LICENSE_VALIDATION", "true", System.EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("SYNCFUSION_SHOW_LICENSE_ERROR", "false", System.EnvironmentVariableTarget.Process);
            
            // Attempt to disable Syncfusion license provider UI through reflection
            try
            {
                var licenseProviderType = Type.GetType("Syncfusion.Licensing.SyncfusionLicenseProvider, Syncfusion.Licensing");
                if (licenseProviderType != null)
                {
                    // Try to set SuppressValidation flag if it exists
                    var suppressProp = licenseProviderType.GetProperty("SuppressValidation", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (suppressProp?.CanWrite == true)
                    {
                        suppressProp.SetValue(null, true);
                    }
                }
            }
            catch
            {
                // Syncfusion licensing assembly might not be loaded yet - that's OK
            }
        }
        catch
        {
            // Ignore initialization errors
        }
    }
}
