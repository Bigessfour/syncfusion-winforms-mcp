using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using WileyWidget.McpServer.Helpers;
using Syncfusion.WinForms.DataGrid;
using System.IO;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// MCP tool for detecting potential NullReferenceException risks in forms.
/// Scans for uninitialized Syncfusion controls, null DataSources, and missing dependencies.
/// 
/// IMPROVEMENTS FROM PRIORITY A REVIEW:
/// - Configurable timeout per form (prevents hangs)
/// - Configurable theme loading (optional, not required for pass)
/// - Granular error/warning distinction (errors = failures, warnings = non-critical)
/// - Detailed error staging (shows exactly which stage failed)
/// - Try-catch wrapping around specific control access
/// </summary>
[McpServerToolType]
public static class DetectNullRisksTool
{
    [McpServerTool]
    [Description("Scans WinForms forms for potential NullReferenceException risks (e.g., null SfDataGrid.DataSource). Returns structured report of risks.")]
    public static string DetectNullRisks(
        [Description("Optional: Array of fully qualified form type names to scan. If empty, scans all forms.")]
        string[]? formTypeNames = null,
        [Description("Output format: 'text' or 'json' (default: 'text')")]
        string outputFormat = "text",
        [Description("Exclude test forms and test assemblies (default: true)")]
        bool excludeTests = true,
        [Description("Theme name to use during initialization. Use null to skip theme loading. (default: 'Office2019Colorful')")]
        string? themeName = "Office2019Colorful",
        [Description("Timeout in seconds per form scan (0 = no timeout, default: 30)")]
        int timeoutSeconds = 30)
    {
        // Initialize MCP server to suppress Syncfusion license dialogs
        McpServerInitializer.EnsureInitialized();
        
        if (outputFormat is null)
        {
            throw new ArgumentNullException(nameof(outputFormat));
        }

        var startTime = DateTime.UtcNow;
        var results = new List<NullRiskResult>();

        try
        {
            IEnumerable<string> formsToScan;

            if (formTypeNames == null || formTypeNames.Length == 0)
            {
                var formTypes = FormTypeCache.GetAllFormTypes();
                var filteredTypes = excludeTests
                    ? formTypes.Where(IsProductionFormType)
                    : formTypes;
                // Skip MainForm - it has complex async initialization and background threads that hang in headless mode
                formsToScan = filteredTypes
                    .Where(t => !t.Name.Equals("MainForm", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.FullName!)
                    .OrderBy(n => n);
            }
            else
            {
                formsToScan = excludeTests
                    ? formTypeNames.Where(n => !LooksLikeTestTypeName(n))
                    : formTypeNames;
            }

            var formsList = formsToScan.ToList();
            var totalForms = formsList.Count;

            foreach (var formTypeName in formsList)
            {
                var formResult = new NullRiskResult
                {
                    FormTypeName = formTypeName,
                    FormName = formTypeName.Split('.').Last(),
                    ScanTime = DateTime.UtcNow
                };

                try
                {
                    // Run scan with timeout if configured
                    if (timeoutSeconds > 0)
                    {
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                        using (cts)
                        {
                            try
                            {
                                var scanTask = ScanFormAsync(formResult, themeName);
                                var completed = scanTask.Wait(TimeSpan.FromSeconds(timeoutSeconds));
                                if (!completed)
                                {
                                    formResult.ErrorStage = "Timeout";
                                    formResult.Error = $"[Timeout] Form scan exceeded {timeoutSeconds} seconds";
                                    formResult.ErrorDetails = $"Form instantiation and risk check did not complete within {timeoutSeconds} seconds. This may indicate a hang or infinite loop.";
                                }
                            }
                            catch (AggregateException aex) when (aex.InnerException is OperationCanceledException)
                            {
                                formResult.ErrorStage = "Timeout";
                                formResult.Error = $"[Timeout] Form scan exceeded {timeoutSeconds} seconds";
                                formResult.ErrorDetails = $"Form instantiation was cancelled after {timeoutSeconds} seconds.";
                            }
                            catch (AggregateException aex)
                            {
                                // Unwrap AggregateException to show actual error
                                var innerEx = aex.InnerException ?? aex;
                                SetError(formResult, "ScanAsync", innerEx);
                            }
                        }
                    }
                    else
                    {
                        // Run without timeout
                        var scanTask = ScanFormAsync(formResult, themeName);
                        try
                        {
                            scanTask.Wait();
                        }
                        catch (AggregateException aex)
                        {
                            var innerEx = aex.InnerException ?? aex;
                            SetError(formResult, "ScanAsync", innerEx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetError(formResult, "Scan", ex);
                }

                results.Add(formResult);
            }

            var duration = DateTime.UtcNow - startTime;

            return outputFormat.ToLowerInvariant() switch
            {
                "json" => GenerateJsonReport(results, duration, totalForms),
                _ => GenerateTextReport(results, duration, totalForms)
            };
        }
        catch (Exception ex)
        {
            return $"❌ Null risk scan error: {ex.Message}";
        }
    }

    /// <summary>
    /// Scans a single form for null risks. Organized in clear stages for debugging.
    /// Runs instantiation on STA thread to prevent Syncfusion license hangs.
    /// </summary>
    private static async Task ScanFormAsync(NullRiskResult formResult, string? themeName)
    {
        ArgumentNullException.ThrowIfNull(formResult);

        // Use STA thread to prevent Syncfusion license validation hangs
        try
        {
            FormInstantiationHelper.ExecuteOnStaThread(() =>
            {
                ScanFormOnStaThread(formResult, themeName);
                return true;
            }, timeoutSeconds: 15); // 15s timeout for form instantiation
        }
        catch (TimeoutException tex)
        {
            formResult.ErrorStage = "InstantiateForm";
            formResult.Error = $"[Timeout] Form instantiation exceeded 15 seconds (likely Syncfusion license hang)";
            formResult.ErrorDetails = tex.Message;
        }
        catch (Exception ex)
        {
            SetError(formResult, "ScanFormAsync", ex);
        }

        await Task.CompletedTask;
        return;
    }

    /// <summary>
    /// Inner scan logic that runs on STA thread.
    /// </summary>
    private static void ScanFormOnStaThread(NullRiskResult formResult, string? themeName)
    {
        ArgumentNullException.ThrowIfNull(formResult);

        Form? form = null;
        Form? mockMainForm = null;

        try
        {
            // Stage 1: Create mock main form
            try
            {
                mockMainForm = MockFactory.CreateMockMainForm();
            }
            catch (Exception ex)
            {
                SetError(formResult, "CreateMockMainForm", ex);
                return;
            }

            // Stage 2: Resolve form type
            var formType = FormTypeCache.GetFormType(formResult.FormTypeName);
            if (formType == null)
            {
                formResult.Error = $"Form type not found: {formResult.FormTypeName}";
                formResult.ErrorStage = "ResolveFormType";
                return;
            }

            // Stage 3: Instantiate form
            try
            {
                form = FormInstantiationHelper.InstantiateForm(formType, mockMainForm);
            }
            catch (Exception ex)
            {
                SetError(formResult, "InstantiateForm", ex);
                return;
            }

            // Stage 4: Load form (trigger component initialization)
            // This is optional - theme loading is non-critical
            if (!string.IsNullOrEmpty(themeName))
            {
                try
                {
                    var loaded = FormInstantiationHelper.LoadFormWithTheme(form, themeName);
                    if (!loaded)
                    {
                        // Non-fatal: theme load failed but form is instantiated
                        formResult.WarningStage = "LoadFormWithTheme";
                        formResult.Warning = $"Theme '{themeName}' load returned false (form may still be functional)";
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal: theme load threw but form is instantiated
                    formResult.WarningStage = "LoadFormWithTheme";
                    formResult.Warning = $"Theme load failed: {ex.GetType().Name}: {ex.Message}";
                }
            }

            // Stage 5: Run granular null risk checks
            var risks = new List<string>();

            // 5a. Check SfDataGrid DataSource
            try
            {
                var syncfusionControls = SyncfusionTestHelper.GetAllSyncfusionControls(form);
                var grids = syncfusionControls.OfType<SfDataGrid>().ToList();

                foreach (var grid in grids)
                {
                    try
                    {
                        if (grid.DataSource == null)
                        {
                            risks.Add($"SfDataGrid '{grid.Name}' has null DataSource");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Control access failed - record as risk but don't crash
                        risks.Add($"SfDataGrid '{grid.Name}' check failed: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                formResult.WarningStage = "GetSyncfusionControls";
                formResult.Warning = $"Could not enumerate Syncfusion controls: {ex.GetType().Name}";
            }

            // 5b. Check DockingManager
            try
            {
                var syncfusionControls = SyncfusionTestHelper.GetAllSyncfusionControls(form);
                var dockingManagers = syncfusionControls.Where(c => c.GetType().Name == "DockingManager").ToList();

                foreach (var dm in dockingManagers)
                {
                    try
                    {
                        var hostControlProp = dm.GetType().GetProperty("HostControl");
                        if (hostControlProp != null)
                        {
                            var hostControl = hostControlProp.GetValue(dm);
                            if (hostControl == null)
                            {
                                risks.Add($"DockingManager '{dm.Name}' has null HostControl");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Reflection failed - record as risk
                        risks.Add($"DockingManager '{dm.Name}' check failed: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                formResult.WarningStage = "DockingManagerCheck";
                formResult.Warning = $"Could not check DockingManager: {ex.GetType().Name}";
            }

            // Stage 6: Finalize results
            formResult.Risks = risks.ToArray();
            formResult.RiskCount = risks.Count;
            formResult.Passed = risks.Count == 0 && string.IsNullOrEmpty(formResult.Error);
        }
        finally
        {
            // Always dispose to prevent resource leaks
            FormInstantiationHelper.SafeDispose(form, mockMainForm);
        }
    }

    private static void SetError(NullRiskResult result, string stage, Exception ex)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(ex);

        result.ErrorStage = stage;
        result.Error = $"[{stage}] {ex.GetType().Name}: {ex.Message}";
        result.ErrorDetails = ex.ToString();
        result.ErrorStackTrace = ex.StackTrace;
    }

    private static bool IsProductionFormType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var typeName = type.FullName ?? type.Name;
        if (LooksLikeTestTypeName(typeName))
        {
            return false;
        }

        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        if (assemblyName.Contains("Tests", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var assemblyLocation = type.Assembly.Location ?? string.Empty;
        if (LooksLikeTestPath(assemblyLocation))
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeTestTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        return typeName.Contains(".Tests", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains(".Test", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("TestHarness", StringComparison.OrdinalIgnoreCase)
               || typeName.Contains("Mock", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeTestPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalized.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains($"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateTextReport(List<NullRiskResult> results, TimeSpan duration, int totalForms)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine("             NULL SAFETY SCAN REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Total Forms:    {totalForms}");
        sb.AppendLine($"Scanned:        {results.Count}");
        sb.AppendLine($"Clean:          {results.Count(r => r.Passed && string.IsNullOrEmpty(r.Error) && string.IsNullOrEmpty(r.Warning))} ✅");
        sb.AppendLine($"Warned:         {results.Count(r => !string.IsNullOrEmpty(r.Warning) && string.IsNullOrEmpty(r.Error))} ⚡");
        sb.AppendLine($"Failed:         {results.Count(r => !r.Passed || !string.IsNullOrEmpty(r.Error))} ❌");
        sb.AppendLine($"Duration:       {duration.TotalSeconds:F2}s");
        sb.AppendLine();

        var failedForms = results.Where(r => !string.IsNullOrEmpty(r.Error)).ToList();
        var warnedForms = results.Where(r => !string.IsNullOrEmpty(r.Warning) && string.IsNullOrEmpty(r.Error)).ToList();
        var riskyForms = results.Where(r => !r.Passed && string.IsNullOrEmpty(r.Error)).ToList();

        if (failedForms.Any() || warnedForms.Any() || riskyForms.Any())
        {
            if (failedForms.Any())
            {
                sb.AppendLine("❌ FAILURES DETECTED:");
                sb.AppendLine();

                foreach (var result in failedForms)
                {
                    sb.AppendLine($"  • {result.FormName}");
                    sb.AppendLine($"    Stage: {result.ErrorStage}");
                    sb.AppendLine($"    Error: {result.Error}");
                    if (!string.IsNullOrWhiteSpace(result.ErrorDetails))
                    {
                        var lines = result.ErrorDetails.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Take(15).ToList();
                        sb.AppendLine("    ↳ Error Details (first 15 lines):");
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                sb.AppendLine($"      {line}");
                            }
                        }
                    }
                    sb.AppendLine();
                }
            }

            if (warnedForms.Any())
            {
                sb.AppendLine("⚡ WARNINGS DETECTED (non-critical):");
                sb.AppendLine();

                foreach (var result in warnedForms)
                {
                    sb.AppendLine($"  • {result.FormName}");
                    sb.AppendLine($"    Warning: {result.Warning}");
                    if (!string.IsNullOrEmpty(result.WarningStage))
                    {
                        sb.AppendLine($"    Stage: {result.WarningStage}");
                    }
                    sb.AppendLine();
                }
            }

            if (riskyForms.Any())
            {
                sb.AppendLine("⚠️  POTENTIAL CONTROL RISKS:");
                sb.AppendLine();

                foreach (var result in riskyForms)
                {
                    sb.AppendLine($"  • {result.FormName}");
                    foreach (var risk in result.Risks)
                    {
                        sb.AppendLine($"    ⚠️  {risk}");
                    }
                    sb.AppendLine();
                }
            }
        }
        else
        {
            sb.AppendLine("✅ No null risks, warnings, or failures detected across all forms.");
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════");
        return sb.ToString();
    }

    private static string GenerateJsonReport(List<NullRiskResult> results, TimeSpan duration, int totalForms)
    {
        var report = new
        {
            reportType = "NullRiskScanReport",
            timestamp = DateTime.UtcNow,
            summary = new
            {
                totalForms,
                scanned = results.Count,
                passed = results.Count(r => r.Passed && string.IsNullOrEmpty(r.Error) && string.IsNullOrEmpty(r.Warning)),
                warned = results.Count(r => !string.IsNullOrEmpty(r.Warning) && string.IsNullOrEmpty(r.Error)),
                failed = results.Count(r => !r.Passed || !string.IsNullOrEmpty(r.Error)),
                durationSeconds = duration.TotalSeconds
            },
            results = results.Select(r => new
            {
                formName = r.FormName,
                formTypeName = r.FormTypeName,
                passed = r.Passed,
                riskCount = r.RiskCount,
                risks = r.Risks,
                error = r.Error,
                errorStage = r.ErrorStage,
                errorDetails = r.ErrorDetails,
                errorStackTrace = r.ErrorStackTrace,
                warning = r.Warning,
                warningStage = r.WarningStage,
                scanTime = r.ScanTime
            })
        };

        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class NullRiskResult
{
    public required string FormTypeName { get; set; }
    public required string FormName { get; set; }
    public bool Passed { get; set; }
    public int RiskCount { get; set; }
    public string[] Risks { get; set; } = Array.Empty<string>();
    public string? Error { get; set; }
    public string? ErrorStage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? Warning { get; set; }
    public string? WarningStage { get; set; }
    public DateTime ScanTime { get; set; }
}
