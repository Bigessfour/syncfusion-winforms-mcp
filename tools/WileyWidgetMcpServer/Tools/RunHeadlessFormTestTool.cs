using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Syncfusion.Licensing;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// Production-ready MCP tool for running headless form tests via C# scripts.
/// Focuses on pure Roslyn scripting with reflection-based inspection — no visual rendering.
/// Safe for use in headless environments, CI/CD, and remote servers.
/// </summary>
[McpServerToolType]
public static class RunHeadlessFormTestTool
{
    /// <summary>
    /// Lightweight session state for storing test results across calls.
    /// </summary>
    private static readonly Dictionary<string, object?> _sessionState = new();
    private static readonly object _sessionLock = new();
    private static readonly object _licenseLock = new();
    private static bool _licenseInitialized;
    [McpServerTool]
    [Description("Executes a headless UI test for a WinForms form via .csx script or inline C# code. Safe for CI/CD and remote servers—no visual rendering, pure Roslyn scripting with reflection inspection.")]
    public static async Task<string> RunHeadlessFormTest(
        [Description("Optional: Path to .csx script file relative to workspace root (e.g., 'tests/WileyWidget.UITests/Scripts/AccountsFormTest.csx'")]
        string? scriptPath = null,
        [Description("Optional: Inline C# test code to execute. Used if scriptPath not provided.")]
        string? testCode = null,
        [Description("Optional: Specific form type to test (e.g., 'WileyWidget.WinForms.Forms.AccountsForm').")]
        string? formTypeName = null,
        [Description("Maximum execution time in seconds (default: 30)")]
        int timeoutSeconds = 30,
        [Description("Whether to capture Console.Out/Console.Error output (default: true)")]
        bool captureConsoleOutput = true,
        [Description("Whether to run on STA thread for Syncfusion/WinForms initialization (default: true)")]
        bool runOnStaThread = true,
        [Description("Return structured JSON output instead of plain text (default: false)")]
        bool jsonOutput = false,
        [Description("Optional: Session ID to store/retrieve test state across calls")]
        string? sessionId = null)
    {
        var startTime = DateTime.UtcNow;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var output = new StringBuilder();
        Exception? runtimeException = null;
        object? testResult = null;
        bool testPassed = false;

        try
        {
            EnsureSyncfusionLicenseRegistered();

            if (string.IsNullOrEmpty(scriptPath) && string.IsNullOrEmpty(testCode))
            {
                return FormatResponse(jsonOutput, success: false, error: "Either 'scriptPath' or 'testCode' must be provided.",
                    durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
            }

            string codeToExecute;
            string testDescription;

            // Load script or use inline code
            if (!string.IsNullOrEmpty(scriptPath))
            {
                var fullPath = ResolveScriptPath(scriptPath);
                if (!File.Exists(fullPath))
                {
                    return FormatResponse(jsonOutput, success: false, error: $"Script file not found: {scriptPath}",
                        durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
                }

                codeToExecute = await File.ReadAllTextAsync(fullPath, cancellationTokenSource.Token);
                testDescription = Path.GetFileName(scriptPath);
            }
            else
            {
                codeToExecute = testCode!;
                testDescription = formTypeName ?? "Inline Test";
            }

            // Execute test
            TestExecutionResult result;
            if (runOnStaThread)
            {
                result = await ExecuteOnStaThread(codeToExecute, testDescription, captureConsoleOutput, output, cancellationTokenSource.Token);
            }
            else
            {
                result = await ExecuteTest(codeToExecute, testDescription, captureConsoleOutput, output, cancellationTokenSource.Token);
            }

            testPassed = result.Passed;
            testResult = result.Result;
            runtimeException = result.Exception;

            // Store result in session if requested
            if (sessionId != null)
            {
                lock (_sessionLock)
                {
                    _sessionState[sessionId] = testResult;
                }
            }

            var duration = DateTime.UtcNow - startTime;

            if (runtimeException != null)
            {
                return FormatResponse(jsonOutput, success: false,
                    error: $"{runtimeException.GetType().Name}: {runtimeException.Message}\n\nStack Trace:\n{runtimeException.StackTrace}",
                    output: output.ToString(), durationMs: duration.TotalMilliseconds);
            }

            return FormatResponse(jsonOutput, success: testPassed,
                output: output.ToString(), returnValue: testResult, durationMs: duration.TotalMilliseconds,
                testDescription: testDescription);
        }
        catch (OperationCanceledException)
        {
            return FormatResponse(jsonOutput, success: false,
                error: $"Test timeout: exceeded {timeoutSeconds} seconds",
                durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            return FormatResponse(jsonOutput, success: false,
                error: $"Unexpected error: {ex.Message}\n\n{ex.StackTrace}",
                durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private static void EnsureSyncfusionLicenseRegistered()
    {
        lock (_licenseLock)
        {
            if (_licenseInitialized)
            {
                return;
            }

            var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY")
                ?? Environment.GetEnvironmentVariable("Syncfusion__LicenseKey")
                ?? Environment.GetEnvironmentVariable("Syncfusion:LicenseKey")
                ?? Environment.GetEnvironmentVariable("Syncfusion_LicenseKey");

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                _licenseInitialized = true;
                return;
            }

            try
            {
                SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
            catch
            {
            }

            _licenseInitialized = true;
        }
    }

    private class TestExecutionResult
    {
        public bool Passed { get; set; }
        public object? Result { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Execute test on default thread with console capture.
    /// </summary>
    private static async Task<TestExecutionResult> ExecuteTest(
        string codeToExecute,
        string testDescription,
        bool captureConsoleOutput,
        StringBuilder output,
        CancellationToken cancellationToken)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var executionResult = new TestExecutionResult();

        try
        {
            if (captureConsoleOutput)
            {
                var stringWriter = new StringWriter(output);
                Console.SetOut(stringWriter);
                Console.SetError(stringWriter);
            }

            // Configure script with all necessary references
            var scriptOptions = BuildScriptOptions();

            var result = await CSharpScript.EvaluateAsync(codeToExecute, scriptOptions, cancellationToken: cancellationToken);
            executionResult.Result = result;

            // Convention: return true = pass, false = fail, null/void = pass
            executionResult.Passed = result switch
            {
                bool b => b,
                _ => true
            };
        }
        catch (CompilationErrorException compilationEx)
        {
            executionResult.Exception = compilationEx;
            executionResult.Passed = false;
        }
        catch (Exception testEx)
        {
            executionResult.Exception = testEx;
            executionResult.Passed = false;
        }
        finally
        {
            if (captureConsoleOutput)
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        return executionResult;
    }

    /// <summary>
    /// Execute test on STA thread (for Syncfusion initialization edge cases).
    /// </summary>
    private static async Task<TestExecutionResult> ExecuteOnStaThread(
        string codeToExecute,
        string testDescription,
        bool captureConsoleOutput,
        StringBuilder output,
        CancellationToken cancellationToken)
    {
        var container = new TestExecutionResult();
        var originalOut = Console.Out;
        var originalError = Console.Error;

        // We run a managed thread that blocks, so we wrap it in Task.Run if we want to await it,
        // but the method is async so we can just return a Task.

        await Task.Run(() =>
        {
            using var completed = new ManualResetEventSlim(false);
            var staThread = new Thread(() =>
            {
               // ... logic ...
               try
               {
                   if (captureConsoleOutput)
                   {
                       var stringWriter = new StringWriter(output);
                       Console.SetOut(stringWriter);
                       Console.SetError(stringWriter);
                   }

                   var scriptOptions = BuildScriptOptions();
                   var task = CSharpScript.EvaluateAsync(codeToExecute, scriptOptions, cancellationToken: cancellationToken);
                   task.Wait(cancellationToken);

                   container.Result = task.Result;
                   container.Passed = task.Result switch { bool b => b, _ => true };
               }
               catch (Exception ex)
               {
                   container.Exception = ex is AggregateException ae ? ae.InnerException : ex;
                   container.Passed = false;
               }
               finally
               {
                   if (captureConsoleOutput)
                   {
                       Console.SetOut(originalOut);
                       Console.SetError(originalError);
                   }

                   completed.Set();
               }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();

            while (!completed.IsSet && !cancellationToken.IsCancellationRequested)
            {
                completed.Wait(TimeSpan.FromMilliseconds(200));
            }

            if (!completed.IsSet)
            {
                container.Exception = new TimeoutException("STA thread test execution timed out");
                container.Passed = false;

                if (captureConsoleOutput)
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                }
            }
        });

        return container;
    }

    /// <summary>
    /// Build script options with all necessary Syncfusion and WileyWidget references.
    /// </summary>
    private static ScriptOptions BuildScriptOptions()
    {
        var options = ScriptOptions.Default;
        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void TryAddAssemblyPath(ISet<string> paths, Assembly? assembly)
        {
            if (assembly is null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(assembly.Location) && File.Exists(assembly.Location))
                {
                    paths.Add(assembly.Location);
                }
            }
            catch
            {
            }
        }

        var requiredAssemblies = new[]
        {
            typeof(System.Windows.Forms.Form).Assembly,
            typeof(Syncfusion.WinForms.Controls.SfForm).Assembly,
            typeof(Syncfusion.WinForms.DataGrid.SfDataGrid).Assembly,
            typeof(Syncfusion.WinForms.Themes.Office2019Theme).Assembly,
            typeof(Syncfusion.Windows.Forms.SkinManager).Assembly,
            typeof(Syncfusion.Windows.Forms.Tools.RibbonControlAdv).Assembly,
            typeof(Syncfusion.WinForms.ListView.SfListView).Assembly,
            typeof(WileyWidget.WinForms.Forms.MainForm).Assembly,
            typeof(WileyWidget.McpServer.Helpers.SyncfusionTestHelper).Assembly,
            typeof(object).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
        };

        foreach (var assembly in requiredAssemblies)
        {
            TryAddAssemblyPath(referencePaths, assembly);
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryAddAssemblyPath(referencePaths, assembly);
        }

        var runtimeAssemblyNames = new[]
        {
            "System.Windows.Forms",
            "System.Drawing.Common",
            "Accessibility",
            "Microsoft.Win32.SystemEvents",
        };

        foreach (var assemblyName in runtimeAssemblyNames)
        {
            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                TryAddAssemblyPath(referencePaths, assembly);
            }
            catch
            {
            }
        }

        options = options.WithReferences(referencePaths);

        return options
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Windows.Forms",
                "System.Linq",
                "System.Reflection",
                "System.Text",
                "System.Threading.Tasks",
                "Syncfusion.WinForms.Controls",
                "Syncfusion.WinForms.DataGrid",
                "Syncfusion.WinForms.Themes",
                "Syncfusion.Windows.Forms",
                "Syncfusion.Windows.Forms.Tools",
                "Syncfusion.WinForms.ListView",
                "WileyWidget.WinForms.Forms",
                "WileyWidget.WinForms.ViewModels",
                "WileyWidget.Services",
                "WileyWidget.McpServer.Helpers");
    }

    /// <summary>
    /// Resolve script path with fallback to workspace root.
    /// </summary>
    private static string ResolveScriptPath(string scriptPath)
    {
        // Try current directory first
        if (File.Exists(scriptPath))
            return scriptPath;

        // Try relative to workspace root
        var workspaceRoot = FindWorkspaceRoot();
        if (workspaceRoot != null)
        {
            var fullPath = Path.Combine(workspaceRoot, scriptPath);
            if (File.Exists(fullPath))
                return fullPath;
        }

        // Return original path (caller will detect missing file)
        return scriptPath;
    }

    /// <summary>
    /// Find workspace root with multiple heuristics.
    /// </summary>
    private static string? FindWorkspaceRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            // Check for solution file
            if (File.Exists(Path.Combine(current, "WileyWidget.sln")))
                return current;

            // Check for typical markers
            if (Directory.Exists(Path.Combine(current, "src")) &&
                Directory.Exists(Path.Combine(current, "tests")) &&
                File.Exists(Path.Combine(current, "README.md")))
                return current;

            // Check for .git (monorepo marker)
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    /// <summary>
    /// Format response as plain text or JSON.
    /// </summary>
    private static string FormatResponse(
        bool jsonOutput,
        bool success,
        string? error = null,
        string? output = null,
        object? returnValue = null,
        double durationMs = 0,
        string? testDescription = null)
    {
        if (!jsonOutput)
        {
            var sb = new StringBuilder();

            if (success)
            {
                sb.AppendLine($"✅ Test PASSED{(testDescription != null ? $": {testDescription}" : "")}");
            }
            else
            {
                sb.AppendLine($"❌ Test FAILED{(testDescription != null ? $": {testDescription}" : "")}");
            }

            sb.AppendLine($"Duration: {durationMs:F2}ms");

            if (!string.IsNullOrEmpty(error))
            {
                sb.AppendLine();
                sb.AppendLine("Error:");
                sb.AppendLine(error);
            }

            if (!string.IsNullOrEmpty(output))
            {
                sb.AppendLine();
                sb.AppendLine("Console Output:");
                sb.AppendLine(output);
            }

            if (returnValue != null && returnValue is not bool)
            {
                sb.AppendLine();
                sb.AppendLine($"Return Value: {returnValue}");
            }

            return sb.ToString();
        }
        else
        {
            var response = new
            {
                success,
                testDescription,
                durationMs = Math.Round(durationMs, 2),
                error = string.IsNullOrEmpty(error) ? null : error,
                output = string.IsNullOrEmpty(output) ? null : output,
                returnValue = returnValue is bool ? null : returnValue?.ToString()
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
