using System.Windows.Forms;
using System.IO;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Themes;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Helper utilities for validating Syncfusion WinForms controls in tests.
/// </summary>
public static class SyncfusionTestHelper
{
    /// <summary>
    /// Validates that a control uses the expected theme via SkinManager.
    /// Checks Syncfusion controls for proper theme application.
    /// </summary>
    public static bool ValidateTheme(Control control, string expectedTheme)
    {
        try
        {
            if (control == null || control.IsDisposed)
                return false;

            // Check if any Syncfusion controls have the expected theme
            var syncfusionControls = GetAllSyncfusionControls(control);

            // If no Syncfusion controls, form passes by default
            if (syncfusionControls.Count == 0)
                return true;

            // Check if controls have proper theme name or rely on default
            // Note: ThemeName may be empty if using SkinManager cascade
            foreach (var ctrl in syncfusionControls)
            {
                // Try to get ThemeName property via reflection (available on most Syncfusion controls)
                var themeNameProp = ctrl.GetType().GetProperty("ThemeName");
                if (themeNameProp != null)
                {
                    var themeName = themeNameProp.GetValue(ctrl) as string;
                    // Empty theme name means using parent/SkinManager cascade (valid)
                    // Otherwise, must match expected theme
                    if (!string.IsNullOrEmpty(themeName) && themeName != expectedTheme)
                    {
                        Console.WriteLine($"[Theme Validation] {control.GetType().Name}: Control {ctrl.GetType().Name} has theme '{themeName}' but expected '{expectedTheme}'");
                        return false;
                    }
                }
            }

            Console.WriteLine($"[Theme Validation] {control.GetType().Name}: PASS - {syncfusionControls.Count} Syncfusion controls validated");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Theme Validation CRASH] {control?.GetType().Name ?? "null"}: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Gets all Syncfusion controls recursively from a control tree.
    /// </summary>
    public static List<Control> GetAllSyncfusionControls(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        var syncfusionControls = new List<Control>();

        // Check if this control is a Syncfusion control
        if (control.GetType().Namespace?.StartsWith("Syncfusion", StringComparison.Ordinal) == true)
        {
            syncfusionControls.Add(control);
        }

        // Recursively check children
        foreach (Control child in control.Controls)
        {
            syncfusionControls.AddRange(GetAllSyncfusionControls(child));
        }

        return syncfusionControls;
    }

    /// <summary>
    /// Validates that no controls have manual BackColor/ForeColor assignments.
    /// Allows semantic status colors (Red/Green/Orange) as exceptions.
    /// </summary>
    public static List<string> ValidateNoManualColors(Control control, string path = "")
    {
        try
        {
            ArgumentNullException.ThrowIfNull(control);
            var violations = new List<string>();
            var currentPath = string.IsNullOrEmpty(path) ? control.Name ?? control.GetType().Name : $"{path}.{control.Name ?? control.GetType().Name}";

            // Allowed colors (semantic status indicators)
            var allowedSemanticColors = new[]
            {
                Color.Red,
                Color.Green,
                Color.Orange,
                Color.Yellow,
                Color.LimeGreen,
                Color.DarkRed,
                Color.DarkGreen,
                Color.OrangeRed
            };

            // Allowed system/default colors (including common WinForms defaults)
            var allowedSystemColors = new[]
            {
                SystemColors.Control,
                SystemColors.Window,
                SystemColors.ControlLight,
                SystemColors.ControlDark,
                SystemColors.ControlText,
                Color.Transparent,
                Color.White,
                Color.Empty
            };

            // Check BackColor
            if (!allowedSystemColors.Contains(control.BackColor) &&
                !allowedSemanticColors.Contains(control.BackColor) &&
                control.BackColor.A > 0) // Ignore fully transparent
            {
                // Special case: Syncfusion controls may have themed colors
                var isSyncfusionControl = control.GetType().Namespace?.StartsWith("Syncfusion", StringComparison.Ordinal) == true;
                if (!isSyncfusionControl)
                {
                    violations.Add($"{currentPath}.BackColor = {ColorToString(control.BackColor)} (manual color - use SkinManager instead)");
                }
            }

            // Check ForeColor - also allow standard WinForms default text colors
            var allowedForeColors = allowedSystemColors.Concat(new[] { Color.Black }).ToArray();
            if (!allowedForeColors.Contains(control.ForeColor) &&
                !allowedSemanticColors.Contains(control.ForeColor) &&
                control.ForeColor.A > 0) // Ignore fully transparent
            {
                var isSyncfusionControl = control.GetType().Namespace?.StartsWith("Syncfusion", StringComparison.Ordinal) == true;
                if (!isSyncfusionControl)
                {
                    violations.Add($"{currentPath}.ForeColor = {ColorToString(control.ForeColor)} (manual color - use SkinManager instead)");
                }
            }

            // Recursively check children
            foreach (Control child in control.Controls)
            {
                violations.AddRange(ValidateNoManualColors(child, currentPath));
            }

            Console.WriteLine($"[Color Validation] {control.GetType().Name}: {violations.Count} violations found");
            return violations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Color Validation CRASH] {control?.GetType().Name ?? "null"}: {ex.Message}\n{ex.StackTrace}");
            return new List<string> { $"Color validation crashed: {ex.Message}" };
        }
    }

    /// <summary>
    /// Converts a color to a readable string format.
    /// </summary>
    private static string ColorToString(Color color)
    {
        if (color.IsNamedColor)
            return color.Name;
        return $"RGB({color.R}, {color.G}, {color.B})";
    }

    /// <summary>
    /// Finds the first SfDataGrid control on a form.
    /// </summary>
    public static SfDataGrid? FindSfDataGrid(Control control, string? gridName = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (control is SfDataGrid grid)
        {
            if (gridName == null || control.Name == gridName)
            {
                return grid;
            }
        }

        foreach (Control child in control.Controls)
        {
            var found = FindSfDataGrid(child, gridName);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// Validates that an SfDataGrid is properly configured.
    /// </summary>
    public static bool ValidateSfDataGrid(SfDataGrid grid)
    {
        // Basic validation: grid exists, has columns
        return grid != null && grid.Columns.Count > 0;
    }

    /// <summary>
    /// Static source scan for manual color assignments in the panel's class file(s).
    /// Returns lines in the form file.cs:line: snippet for any occurrences of BackColor/ForeColor/Color.FromArgb.
    /// </summary>
    public static List<string> ScanSourceForManualColors(string panelTypeName)
    {
        var violations = new List<string>();
        try
        {
            if (string.IsNullOrEmpty(panelTypeName)) return violations;

            var className = panelTypeName.Split('.').Last();

            // Heuristic: look under src/WileyWidget.WinForms/Controls for files containing the class declaration
            var workspaceRoot = Directory.GetCurrentDirectory();
            var controlsPath = Path.Combine(workspaceRoot, "src", "WileyWidget.WinForms", "Controls");

            // Fallback: if controlsPath doesn't exist, search the workspace for the class
            IEnumerable<string> candidateFiles;
            if (Directory.Exists(controlsPath))
            {
                candidateFiles = Directory.EnumerateFiles(controlsPath, "*.cs", SearchOption.AllDirectories);
            }
            else
            {
                candidateFiles = Directory.EnumerateFiles(workspaceRoot, "*.cs", SearchOption.AllDirectories);
            }

            foreach (var file in candidateFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    if (!content.Contains($"class {className}"))
                        continue;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (line.Contains(".BackColor") || line.Contains(".ForeColor") || line.Contains("Color.FromArgb"))
                        {
                            violations.Add($"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}");
                        }
                    }
                }
                catch
                {
                    // Ignore file access errors
                }
            }

            return violations;
        }
        catch (Exception ex)
        {
            return new List<string> { $"Static scan failed: {ex.Message}" };
        }
    }

    /// <summary>
    /// Tries to load a form by showing/hiding it to trigger component initialization.
    /// </summary>
    public static bool TryLoadForm(Form form, int waitMs = 500)
    {
        ArgumentNullException.ThrowIfNull(form);
        try
        {
            form.Show();

            // Poll for initialization with event pumping, up to waitMs
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var maxWait = TimeSpan.FromMilliseconds(Math.Max(50, waitMs));
            var initialized = false;
            while (sw.Elapsed < maxWait)
            {
                try
                {
                    Application.DoEvents();
                    if (form.IsHandleCreated)
                    {
                        var sfControls = GetAllSyncfusionControls(form);
                        if ((sfControls != null && sfControls.Count > 0) || (form.Controls != null && form.Controls.Count > 0))
                        {
                            initialized = true;
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore transient lookup errors
                }
                // No Thread.Sleep; rely on event pumping and stopwatch for polling
            }

            form.Hide();
            return initialized;
        }
        catch
        {
            return false;
        }
    }
}
