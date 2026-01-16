using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WileyWidget.McpServer.Helpers;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// Search a form for controls matching specific criteria.
/// Examples:
///   - Find all buttons with Text="Submit"
///   - Find all controls derived from TreeView
///   - Find controls with TabIndex > 10
/// Useful for: "Where did I set that custom font?" or "Which grids have AutoResizeColumns disabled?"
/// </summary>
public static class FindControlsByPropertyTool
{
    public class ControlMatch
    {
        public required string ControlPath { get; set; }
        public required string ControlName { get; set; }
        public required string ControlType { get; set; }
        public required string Text { get; set; }
        public Dictionary<string, object> MatchingProperties { get; set; } = new();
    }

    /// <summary>
    /// Searches for controls matching criteria.
    /// Criteria examples:
    ///   "Text=Submit" - controls with Text property containing "Submit"
    ///   "Type=Button" - all buttons
    ///   "Type=*Grid" - all controls with "Grid" in the type name (e.g., DataGridView, SfDataGrid)
    ///   "Font.Size>12" - controls with font size > 12
    /// </summary>
    public static string Search(
        string formTypeName,
        string searchCriteria,
        string outputFormat = "text")
    {
        try
        {
            var type = Type.GetType(formTypeName);
            if (type == null)
                return OutputFormatter.FormatError($"Form type '{formTypeName}' not found");

            if (!typeof(Form).IsAssignableFrom(type))
                return OutputFormatter.FormatError($"'{formTypeName}' is not a Form");

            var mockMainForm = MockFactory.CreateMockMainForm();
            var form = FormInstantiationHelper.InstantiateForm(type, mockMainForm);

            try
            {
                var criteria = ParseCriteria(searchCriteria);
                var matches = FindMatches(form, criteria).ToList();

                return outputFormat switch
                {
                    "json" => FormatJson(searchCriteria, matches),
                    _ => FormatText(searchCriteria, matches)
                };
            }
            finally
            {
                FormInstantiationHelper.SafeDispose(form, mockMainForm);
            }
        }
        catch (Exception ex)
        {
            return OutputFormatter.FormatError($"Search failed: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ParseCriteria(string criteria)
    {
        var parsed = new Dictionary<string, string>();

        var parts = criteria.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                parsed[kv[0].Trim()] = kv[1].Trim();
            }
        }

        return parsed;
    }

    private static IEnumerable<ControlMatch> FindMatches(Control root, Dictionary<string, string> criteria)
    {
        var matches = new List<ControlMatch>();
        var visited = new HashSet<Control>();

        ScanControlTree(root, root, visited, criteria, new Stack<Control>(), matches);

        return matches;
    }

    private static void ScanControlTree(
        Control root,
        Control current,
        HashSet<Control> visited,
        Dictionary<string, string> criteria,
        Stack<Control> path,
        List<ControlMatch> matches)
    {
        if (visited.Contains(current))
            return;

        visited.Add(current);
        path.Push(current);

        // Check if current control matches criteria
        if (MatchesCriteria(current, criteria))
        {
            var match = new ControlMatch
            {
                ControlPath = string.Join(" > ", path.Reverse().Select(c => c.Name ?? c.GetType().Name)),
                ControlName = current.Name,
                ControlType = current.GetType().Name,
                Text = current.Text
            };

            // Extract matching properties
            foreach (var (key, value) in criteria)
            {
                var propValue = GetPropertyValue(current, key);
                if (propValue != null)
                    match.MatchingProperties[key] = propValue;
            }

            matches.Add(match);
        }

        // Recurse
        foreach (Control child in current.Controls)
        {
            ScanControlTree(root, child, visited, criteria, path, matches);
        }

        path.Pop();
    }

    private static bool MatchesCriteria(Control control, Dictionary<string, string> criteria)
    {
        foreach (var (key, expected) in criteria)
        {
            if (key.Equals("Type", StringComparison.OrdinalIgnoreCase))
            {
                if (!MatchType(control, expected))
                    return false;
            }
            else if (key.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                if (!control.Text.Contains(expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                if (!control.Name.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                // Generic property matching
                var value = GetPropertyValue(control, key);
                if (value == null || !value.ToString()!.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    private static bool MatchType(Control control, string pattern)
    {
        var typeName = control.GetType().Name;

        // Exact match
        if (typeName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Wildcard matching (*Grid matches SfDataGrid, DataGridView, etc.)
        if (pattern.Contains("*"))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(typeName, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Base type matching (Button matches all button types)
        return control.GetType().BaseType?.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static object? GetPropertyValue(Control control, string propertyPath)
    {
        try
        {
            var parts = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            object? current = control;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                var prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatText(string criteria, List<ControlMatch> matches)
    {
        if (!matches.Any())
            return $"No controls found matching criteria: {criteria}";

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Search Results: {criteria}");
        output.AppendLine("════════════════════════════════════════════════");
        output.AppendLine($"Found: {matches.Count} control(s)");
        output.AppendLine();

        foreach (var match in matches)
        {
            output.AppendLine($"✓ {match.ControlPath}");
            output.AppendLine($"  Name: {match.ControlName}");
            output.AppendLine($"  Type: {match.ControlType}");
            if (!string.IsNullOrEmpty(match.Text))
                output.AppendLine($"  Text: \"{match.Text}\"");

            if (match.MatchingProperties.Any())
            {
                output.AppendLine("  Properties:");
                foreach (var (key, value) in match.MatchingProperties)
                {
                    output.AppendLine($"    {key} = {value}");
                }
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private static string FormatJson(string criteria, List<ControlMatch> matches)
    {
        var json = new System.Text.StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"criteria\": \"{criteria}\",");
        json.AppendLine($"  \"matches_found\": {matches.Count},");
        json.AppendLine("  \"results\": [");

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            json.AppendLine("    {");
            json.AppendLine($"      \"path\": \"{m.ControlPath}\",");
            json.AppendLine($"      \"name\": \"{m.ControlName}\",");
            json.AppendLine($"      \"type\": \"{m.ControlType}\",");
            json.AppendLine($"      \"text\": \"{m.Text}\",");
            json.AppendLine("      \"properties\": {");

            var props = m.MatchingProperties.Select((kv, j) =>
                $"        \"{kv.Key}\": \"{kv.Value}\"" + (j < m.MatchingProperties.Count - 1 ? "," : "")
            );
            json.AppendLine(string.Join("\n", props));

            json.AppendLine("      }");
            json.Append("    }");
            if (i < matches.Count - 1)
                json.AppendLine(",");
            else
                json.AppendLine();
        }

        json.AppendLine("  ]");
        json.AppendLine("}");

        return json.ToString();
    }
}
