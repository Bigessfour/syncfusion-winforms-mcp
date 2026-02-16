using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using WileyWidget.McpServer.Tools;

namespace WileyWidget.McpServer;

/// <summary>
/// MCP Server for WileyWidget UI Testing and Validation.
/// Exposes tools for headless form validation, Syncfusion control inspection, and theme compliance checks.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Allow a quick CLI helper to run the license check without starting the MCP server
            if (args != null && args.Length > 0)
            {
                if (args[0] == "--run-license-check")
                {
                    var fmt = args.Length > 1 ? args[1] : "json";
                    var output = WileyWidget.McpServer.Tools.ValidateSyncfusionLicenseTool.ValidateSyncfusionLicense(fmt);
                    Console.WriteLine(output);
                    return 0;
                }

                if (args[0] == "--run-script" && args.Length > 1)
                {
                    var scriptPath = args[1];
                    Console.WriteLine($"Running script: {scriptPath}...");
                    var result = await WileyWidget.McpServer.Tools.RunHeadlessFormTestTool.RunHeadlessFormTest(
                        scriptPath: scriptPath,
                        timeoutSeconds: 120,
                        runOnStaThread: true);
                    Console.WriteLine(result);
                    if (result.Contains("❌ Test FAILED", StringComparison.Ordinal))
                    {
                        return 1;
                    }

                    return 0;
                }
            }

            // Create empty application builder (no console output noise for STDIO transport)
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            // Load configuration from appsettings.json (enables environment-specific logging)
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
            builder.Configuration.AddEnvironmentVariables();

            // IMPORTANT (STDIO transport): never write non-protocol text to STDOUT.
            // Route all console logs to STDERR to avoid corrupting the MCP JSON-RPC stream.
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            // Add MCP server with STDIO transport and tools from assembly
            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            // Build and run the application
            var app = builder.Build();
            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            // Log to stderr (safe for STDIO transport)
            Console.Error.WriteLine($"MCP Server error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
