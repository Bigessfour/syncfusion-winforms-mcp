using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Threading;
using Syncfusion.Windows.Forms;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// Enhanced MCP tool for dynamic evaluation of C# code snippets with stateful REPL, STA thread marshalling, 
/// screenshot capture, and structured output.
/// Provides rapid prototyping, debugging, and UI validation capabilities without recompilation.
/// </summary>
[McpServerToolType]
public static class EvalCSharpTool
{
    // Stateful REPL sessions: sessionId -> (ScriptState, Globals)
    private static readonly Dictionary<string, (ScriptState<object>?, EvalGlobals)> _sessions = new();
    private static readonly object _sessionLock = new();

    /// <summary>
    /// Shared globals available across all eval invocations and sessions.
    /// </summary>
    public class EvalGlobals
    {
        public SkinManager SkinManager { get; set; } = SkinManager.Instance;
        public TestHelper TestHelper { get; set; } = new();
        public Dictionary<string, object> Vars { get; set; } = new();

        public void Set(string name, object? value)
        {
            if (value == null)
                Vars.Remove(name);
            else
                Vars[name] = value;
        }

        public object? Get(string name) => Vars.TryGetValue(name, out var val) ? val : null;
    }

    /// <summary>
    /// Helper methods for testing and inspection.
    /// </summary>
    public class TestHelper
    {
        public void Log(string message) => Console.WriteLine($"[LOG] {message}");
        public void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public string InspectControl(Control? control)
        {
            if (control == null) return "Control is null";
            var sb = new StringBuilder();
            sb.AppendLine($"Control: {control.GetType().Name}");
            sb.AppendLine($"  Text: {control.Text}");
            sb.AppendLine($"  Visible: {control.Visible}");
            sb.AppendLine($"  Enabled: {control.Enabled}");
            sb.AppendLine($"  Size: {control.Size}");
            sb.AppendLine($"  Location: {control.Location}");

            // Syncfusion-specific properties
            if (control is Syncfusion.WinForms.Controls.SfForm form)
            {
                sb.AppendLine($"  ThemeName: {form.ThemeName ?? "(default)"}");
            }
            else if (control.GetType().GetProperty("ThemeName") is { } themeProp)
            {
                try
                {
                    var themeName = themeProp.GetValue(control);
                    sb.AppendLine($"  ThemeName: {themeName ?? "(default)"}");
                }
                catch { }
            }

            return sb.ToString();
        }

        public string InspectDataGrid(Syncfusion.WinForms.DataGrid.SfDataGrid? grid)
        {
            if (grid == null) return "DataGrid is null";
            var sb = new StringBuilder();
            sb.AppendLine($"SfDataGrid: {grid.Name ?? "(unnamed)"}");
            sb.AppendLine($"  Columns: {grid.Columns.Count}");
            sb.AppendLine($"  Rows: {grid.RowCount}");
            sb.AppendLine($"  DataSource: {grid.DataSource?.GetType().Name ?? "(null)"}");
            sb.AppendLine($"  ThemeName: {grid.ThemeName ?? "(default)"}");
            if (grid.Columns.Count > 0)
            {
                sb.AppendLine("  Columns:");
                foreach (var col in grid.Columns.Take(5))
                {
                    sb.AppendLine($"    - {col.MappingName} ({col.GetType().Name})");
                }
                if (grid.Columns.Count > 5)
                    sb.AppendLine($"    ... and {grid.Columns.Count - 5} more");
            }
            return sb.ToString();
        }
    }

    [McpServerTool]
    [Description("Evaluates C# code dynamically without compilation with stateful REPL, STA thread support, screenshot capture, and structured output. Perfect for rapid UI/control validation, exploratory testing, theme checks, and mock-driven debugging of WileyWidget forms and Syncfusion controls.")]
    public static async Task<string> EvalCSharp(
        [Description("C# code to execute. Can instantiate forms, inspect Syncfusion controls, verify properties, and run assertions.")]
        string csx,
        [Description("Optional: Full path to a .csx file to execute instead of inline code")]
        string? csxFile = null,
        [Description("Maximum execution time in seconds (default: 30)")]
        int timeoutSeconds = 30,
        [Description("Optional: Session ID for stateful REPL. Maintains globals and script state across calls.")]
        string? sessionId = null,
        [Description("Whether to run script on STA thread for WinForms/Syncfusion code (default: true)")]
        bool runOnStaThread = true,
        [Description("Whether to capture and return screenshot if result is a Form/Control (default: false)")]
        bool captureScreenshot = false,
        [Description("Return structured JSON output instead of plain text (default: false)")]
        bool jsonOutput = false,
        [Description("Script template to prepend: 'form', 'datagrid', 'theme-test', or null for none")]
        string? template = null)
    {
        var startTime = DateTime.UtcNow;
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        string? screenshotPath = null;
        Exception? runtimeError = null;
        object? returnValue = null;
        StringBuilder output = new();

        try
        {
            string codeToExecute;

            // Load from file or use inline code
            if (!string.IsNullOrEmpty(csxFile))
            {
                if (!File.Exists(csxFile))
                {
                    return FormatResponse(jsonOutput, success: false, error: $"File not found: {csxFile}",
                        durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
                }
                codeToExecute = await File.ReadAllTextAsync(csxFile, cancellationTokenSource.Token);
            }
            else if (!string.IsNullOrEmpty(csx))
            {
                codeToExecute = csx;
            }
            else
            {
                return FormatResponse(jsonOutput, success: false, error: "Either 'csx' or 'csxFile' parameter must be provided.",
                    durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
            }

            // Apply template if provided
            if (!string.IsNullOrEmpty(template))
            {
                codeToExecute = ApplyTemplate(template) + "\n" + codeToExecute;
            }

            // Get or create session globals
            EvalGlobals? globals = null;
            ScriptState<object>? scriptState = null;

            if (sessionId != null)
            {
                lock (_sessionLock)
                {
                    if (_sessions.TryGetValue(sessionId, out var session))
                    {
                        scriptState = session.Item1;
                        globals = session.Item2;
                    }
                    else
                    {
                        globals = new EvalGlobals();
                    }
                }
            }
            else
            {
                globals = new EvalGlobals();
            }

            // Execute on STA thread if needed
            if (runOnStaThread)
            {
                string? threadError = null;
                object? threadResult = null;

                var staThread = new Thread(() =>
                {
                    try
                    {
                        (threadResult, scriptState) = ExecuteScript(codeToExecute, globals, scriptState, output, cancellationTokenSource.Token);
                        returnValue = threadResult;
                    }
                    catch (Exception ex)
                    {
                        runtimeError = ex;
                        threadError = ex.Message;
                    }
                });

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();

                if (!staThread.Join(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    try
                    {
                        cancellationTokenSource.Cancel();
                        staThread.Interrupt();
                    }
                    catch { }
                    return FormatResponse(jsonOutput, success: false, error: $"Execution timeout: exceeded {timeoutSeconds} seconds",
                        durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
                }

                if (threadError != null)
                    runtimeError = new InvalidOperationException(threadError);
            }
            else
            {
                (returnValue, scriptState) = ExecuteScript(codeToExecute, globals, scriptState, output, cancellationTokenSource.Token);
            }

            // Store session if requested
            if (sessionId != null)
            {
                lock (_sessionLock)
                {
                    _sessions[sessionId] = (scriptState, globals);
                }
            }

            // Capture screenshot if requested
            if (captureScreenshot && returnValue is Control control)
            {
                screenshotPath = CaptureScreenshot(control);
            }

            var duration = DateTime.UtcNow - startTime;

            // Check if we're running low on time
            string? timeoutWarning = null;
            if (duration.TotalSeconds > timeoutSeconds * 0.8)
            {
                timeoutWarning = $"⚠️ Running close to timeout ({duration.TotalSeconds:F2}s / {timeoutSeconds}s)";
            }

            return FormatResponse(jsonOutput, success: true, output: output.ToString(), 
                returnValue: returnValue, screenshotPath: screenshotPath,
                durationMs: duration.TotalMilliseconds, timeoutWarning: timeoutWarning);
        }
        catch (CompilationErrorException compilationEx)
        {
            var duration = DateTime.UtcNow - startTime;
            var errors = string.Join("\n", compilationEx.Diagnostics
                .Select(d => $"  Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}"));
            return FormatResponse(jsonOutput, success: false, error: $"Compilation error:\n{errors}",
                durationMs: duration.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            return FormatResponse(jsonOutput, success: false, error: $"Execution timeout: exceeded {timeoutSeconds} seconds",
                durationMs: (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex) when (runtimeError == null)
        {
            var duration = DateTime.UtcNow - startTime;
            var errorMsg = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            return FormatResponse(jsonOutput, success: false, error: errorMsg,
                durationMs: duration.TotalMilliseconds);
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Execute the script with the given globals and script state.
    /// </summary>
    private static (object?, ScriptState<object>?) ExecuteScript(
        string codeToExecute,
        EvalGlobals globals,
        ScriptState<object>? previousState,
        StringBuilder output,
        CancellationToken cancellationToken)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            var stringWriter = new StringWriter(output);
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);

            // Configure script options with resilient reference loading
            var scriptOptions = ScriptOptions.Default;
            
            // Add references with error handling for missing assemblies
            var referencesToAdd = new[] 
            { 
                typeof(System.Windows.Forms.Form).Assembly,
                typeof(Syncfusion.WinForms.Controls.SfForm).Assembly,
                typeof(Syncfusion.WinForms.DataGrid.SfDataGrid).Assembly,
                typeof(Syncfusion.WinForms.Themes.Office2019Theme).Assembly,
                typeof(Syncfusion.Windows.Forms.SkinManager).Assembly,
                typeof(Syncfusion.WinForms.ListView.SfListView).Assembly,
                typeof(WileyWidget.WinForms.Forms.MainForm).Assembly,
                typeof(WileyWidget.McpServer.Helpers.SyncfusionTestHelper).Assembly,
                typeof(Moq.Mock).Assembly,
                // Add System assemblies explicitly
                typeof(object).Assembly, // mscorlib/System.Private.CoreLib
                typeof(System.Linq.Enumerable).Assembly, // System.Linq
            };
            
            foreach (var asm in referencesToAdd.Distinct())
            {
                try
                {
                    scriptOptions = scriptOptions.WithReferences(asm);
                }
                catch { /* Silently skip unavailable assemblies */ }
            }
            
            scriptOptions = scriptOptions
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Windows.Forms",
                    "System.Drawing",
                    "System.Threading",
                    "System.Threading.Tasks",
                    "Syncfusion.WinForms.Controls",
                    "Syncfusion.WinForms.DataGrid",
                    "Syncfusion.WinForms.Themes",
                    "Syncfusion.WinForms.Tools",
                    "Syncfusion.WinForms.ListView",
                    "Syncfusion.Windows.Forms",
                    "WileyWidget.WinForms.Forms",
                    "WileyWidget.WinForms.Themes",
                    "WileyWidget.WinForms.ViewModels",
                    "WileyWidget.Services",
                    "WileyWidget.McpServer.Helpers",
                    "Moq");

            // Execute and return result
            if (previousState != null)
            {
                var continuation = previousState.ContinueWithAsync(codeToExecute, scriptOptions, cancellationToken: cancellationToken);
                continuation.Wait(cancellationToken);
                return (continuation.Result.ReturnValue, continuation.Result);
            }
            else
            {
                var task = CSharpScript.RunAsync(codeToExecute, scriptOptions, globals: globals, cancellationToken: cancellationToken);
                task.Wait(cancellationToken);
                return (task.Result.ReturnValue, task.Result);
            }
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    /// Capture a screenshot of a Control or Form.
    /// </summary>
    private static string? CaptureScreenshot(Control control)
    {
        try
        {
            var bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

            // Save to temp file
            var tempFile = Path.Combine(Path.GetTempPath(), $"evalcsharp-screenshot-{Guid.NewGuid():N}.png");
            bitmap.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();

            return tempFile;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Apply a template to the code.
    /// </summary>
    private static string ApplyTemplate(string template) => template switch
    {
        "form" => @"
var form = new SfForm { Text = ""Test Form"", Width = 400, Height = 300 };
SkinManager.SetVisualStyle(form, ""Office2019Colorful"");
",
        "datagrid" => @"
var grid = new SfDataGrid { Name = ""TestGrid"", Height = 300, Width = 400 };
var dataSource = new List<object> 
{ 
    new { Id = 1, Name = ""Item 1"" },
    new { Id = 2, Name = ""Item 2"" }
};
grid.DataSource = dataSource;
SkinManager.SetVisualStyle(grid, ""Office2019Colorful"");
",
        "theme-test" => @"
SkinManager.LoadAssembly(typeof(Syncfusion.WinForms.Themes.Office2019Theme).Assembly);
SkinManager.ApplicationVisualTheme = ""Office2019Colorful"";
var testForm = new SfForm { Text = ""Theme Test"", Width = 400, Height = 300 };
SkinManager.SetVisualStyle(testForm, ""Office2019Colorful"");
",
        _ => ""
    };

    /// <summary>
    /// Format the response as JSON or plain text.
    /// </summary>
    private static string FormatResponse(
        bool jsonOutput,
        bool success,
        string? error = null,
        string? output = null,
        object? returnValue = null,
        string? screenshotPath = null,
        double durationMs = 0,
        string? timeoutWarning = null)
    {
        if (!jsonOutput)
        {
            var sb = new StringBuilder();

            if (success)
            {
                sb.AppendLine("✅ Execution Successful");
            }
            else
            {
                sb.AppendLine("❌ Execution Failed");
            }

            sb.AppendLine($"Duration: {durationMs:F2}ms");

            if (!string.IsNullOrEmpty(timeoutWarning))
            {
                sb.AppendLine();
                sb.AppendLine(timeoutWarning);
            }

            if (!string.IsNullOrEmpty(error))
            {
                sb.AppendLine();
                sb.AppendLine("Error:");
                sb.AppendLine(error);
            }

            if (!string.IsNullOrEmpty(output))
            {
                sb.AppendLine();
                sb.AppendLine("Output:");
                sb.AppendLine(output);
            }

            if (returnValue != null)
            {
                sb.AppendLine();
                sb.AppendLine("Return Value:");
                if (returnValue is Control control)
                {
                    sb.AppendLine($"  Type: {control.GetType().Name}");
                    sb.AppendLine($"  Text: {control.Text}");
                    sb.AppendLine($"  Size: {control.Size}");
                    sb.AppendLine($"  Visible: {control.Visible}");
                }
                else
                {
                    sb.AppendLine($"  Type: {returnValue.GetType().Name}");
                    sb.AppendLine($"  Value: {returnValue}");
                }
            }

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                sb.AppendLine();
                sb.AppendLine($"Screenshot: {screenshotPath}");
            }

            return sb.ToString();
        }
        else
        {
            var response = new
            {
                success,
                durationMs = Math.Round(durationMs, 2),
                timeoutWarning,
                error,
                output = string.IsNullOrEmpty(output) ? null : output,
                returnValue = returnValue == null ? null : new
                {
                    type = returnValue.GetType().Name,
                    value = returnValue.ToString()
                },
                screenshotPath
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
