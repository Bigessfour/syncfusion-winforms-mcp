using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WileyWidget.McpServer.Helpers;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// Detects manual color assignments (BackColor/ForeColor) that bypass theme systems.
/// Works with ANY WinForms app — Syncfusion, DevExpress, Telerik, vanilla.
///
/// Rationale: Manual colors are the #1 source of theme inconsistency. This tool finds them instantly.
/// </summary>
public static class DetectManualColorsTool
{
    private static readonly Color[] SemanticColors = new[]
    {
        Color.Red,      // Error
        Color.Green,    // Success
        Color.Orange,   // Warning
        Color.Yellow,   // Alert
        Color.Blue,     // Info
        Color.Gray,     // Disabled
        Color.White,    // Default
        Color.Black     // Default
    };

    public class ManualColorViolation
    {
        public required string ControlPath { get; set; }
        public required string ControlType { get; set; }
        public required string PropertyName { get; set; } // BackColor or ForeColor
        public required string ColorValue { get; set; }   // "Color [Red]"
        public bool IsSemanticColor { get; set; }
        public required string Severity { get; set; }
    }

    /// <summary>
    /// Scans a form tree for manual color assignments.
    /// Ignores semantic colors (red for errors, green for success, etc.)
    /// </summary>
    public static string Scan(string formTypeName, string outputFormat = "text")
    {
        try
        {
            var type = Type.GetType(formTypeName);
            if (type == null)
                return OutputFormatter.FormatError($"Form type '{formTypeName}' not found");

            if (!typeof(Form).IsAssignableFrom(type))
                return OutputFormatter.FormatError($"'{formTypeName}' is not a Form");

            var mockMainForm = MockFactory.CreateMockMainForm(enableMdi: true);
            var form = FormInstantiationHelper.InstantiateForm(type, mockMainForm);

            try
            {
                var violations = FindManualColors(form).ToList();

                if (outputFormat == "json")
                    return FormatJson(formTypeName, violations);

                return FormatText(formTypeName, violations);
            }
            finally
            {
                FormInstantiationHelper.SafeDispose(form, mockMainForm);
            }
        }
        catch (Exception ex)
        {
            return OutputFormatter.FormatError($"Scan failed: {ex.Message}");
        }
    }

    private static IEnumerable<ManualColorViolation> FindManualColors(Control root)
    {
        var violations = new List<ManualColorViolation>();
        var visited = new HashSet<Control>();
        var path = new Stack<Control>();

        ScanControlTree(root, root, path, visited, violations);

        return violations;
    }

    private static void ScanControlTree(
        Control root,
        Control current,
        Stack<Control> path,
        HashSet<Control> visited,
        List<ManualColorViolation> violations)
    {
        if (visited.Contains(current))
            return;

        visited.Add(current);
        path.Push(current);

        // Check BackColor
        CheckColor(root, current, "BackColor", current.BackColor, path, violations);

        // Check ForeColor
        CheckColor(root, current, "ForeColor", current.ForeColor, path, violations);

        // Recurse into children
        foreach (Control child in current.Controls)
        {
            ScanControlTree(root, child, path, visited, violations);
        }

        path.Pop();
    }

    private static void CheckColor(
        Control root,
        Control control,
        string propertyName,
        Color color,
        Stack<Control> path,
        List<ManualColorViolation> violations)
    {
        // Skip default/inherited colors
        if (color == SystemColors.Control || color == SystemColors.ControlText)
            return;

        bool isSemanticColor = SemanticColors.Contains(color);
        string severity = isSemanticColor ? "Info" : "Warning";

        // Skip if inherited from parent (same as parent's property)
        if (control.Parent != null)
        {
            var parentColor = propertyName == "BackColor" ? control.Parent.BackColor : control.Parent.ForeColor;
            if (color == parentColor)
                return;
        }

        violations.Add(new ManualColorViolation
        {
            ControlPath = string.Join(" > ", path.Reverse().Select(c => c.Name ?? c.GetType().Name)),
            ControlType = control.GetType().Name,
            PropertyName = propertyName,
            ColorValue = $"Color [{color.Name}]",
            IsSemanticColor = isSemanticColor,
            Severity = severity
        });
    }

    private static string FormatText(string formTypeName, List<ManualColorViolation> violations)
    {
        if (!violations.Any())
            return $"✅ {formTypeName}: No manual colors detected";

        var errors = violations.Where(v => v.Severity == "Warning").ToList();
        var infos = violations.Where(v => v.Severity == "Info").ToList();

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Manual Color Detection: {formTypeName}");
        output.AppendLine("═══════════════════════════════════════════════");
        output.AppendLine($"Total violations: {violations.Count}");
        output.AppendLine($"⚠️  Manual colors: {errors.Count}");
        output.AppendLine($"ℹ️  Semantic colors: {infos.Count}");
        output.AppendLine();

        if (errors.Any())
        {
            output.AppendLine("⚠️  MANUAL COLORS (FIX THESE):");
            output.AppendLine("─────────────────────────────────────────────");
            foreach (var v in errors)
            {
                output.AppendLine($"  • {v.ControlPath}");
                output.AppendLine($"    Property: {v.PropertyName} = {v.ColorValue}");
                output.AppendLine($"    Type: {v.ControlType}");
                output.AppendLine();
            }
        }

        if (infos.Any())
        {
            output.AppendLine("ℹ️  SEMANTIC COLORS (OK):");
            output.AppendLine("─────────────────────────────────────────────");
            foreach (var v in infos.Take(5))
            {
                output.AppendLine($"  ✓ {v.ControlPath}: {v.PropertyName} = {v.ColorValue}");
            }

            if (infos.Count > 5)
                output.AppendLine($"  ... and {infos.Count - 5} more semantic colors");
        }

        return output.ToString();
    }

    private static string FormatJson(string formTypeName, List<ManualColorViolation> violations)
    {
        var json = new System.Text.StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"form\": \"{formTypeName}\",");
        json.AppendLine($"  \"total_violations\": {violations.Count},");
        json.AppendLine($"  \"manual_colors\": {violations.Count(v => v.Severity == "Warning")},");
        json.AppendLine($"  \"semantic_colors\": {violations.Count(v => v.Severity == "Info")},");
        json.AppendLine("  \"violations\": [");

        var items = violations.Select((v, i) =>
            $"    {{\n" +
            $"      \"control_path\": \"{v.ControlPath}\",\n" +
            $"      \"control_type\": \"{v.ControlType}\",\n" +
            $"      \"property\": \"{v.PropertyName}\",\n" +
            $"      \"color\": \"{v.ColorValue}\",\n" +
            $"      \"is_semantic\": {v.IsSemanticColor.ToString().ToLower()},\n" +
            $"      \"severity\": \"{v.Severity}\"\n" +
            $"    }}" + (i < violations.Count - 1 ? "," : ""));

        json.AppendLine(string.Join("\n", items));
        json.AppendLine("  ]");
        json.AppendLine("}");

        return json.ToString();
    }
}
