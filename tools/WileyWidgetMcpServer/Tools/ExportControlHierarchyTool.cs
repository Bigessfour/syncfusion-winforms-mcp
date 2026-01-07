using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;

namespace WileyWidgetMcpServer.Tools;

/// <summary>
/// Exports the complete control hierarchy of a form as a structured tree (JSON or text).
/// Useful for: documentation, debugging layout issues, understanding form structure.
/// </summary>
public static class ExportControlHierarchyTool
{
    public class ControlNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Visible { get; set; }
        public bool Enabled { get; set; }
        public string Dock { get; set; }
        public int TabIndex { get; set; }
        public List<ControlNode> Children { get; set; } = new();
    }

    /// <summary>
    /// Exports a form's control hierarchy as JSON or text tree.
    /// </summary>
    public static string Export(string formTypeName, string outputFormat = "text", bool includeProperties = false)
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
                var root = BuildControlTree(form, includeProperties);

                return outputFormat switch
                {
                    "json" => FormatJson(root),
                    "tree" => FormatTree(root),
                    _ => FormatText(root)
                };
            }
            finally
            {
                FormInstantiationHelper.SafeDispose(form, mockMainForm);
            }
        }
        catch (Exception ex)
        {
            return OutputFormatter.FormatError($"Export failed: {ex.Message}");
        }
    }

    private static ControlNode BuildControlTree(Control control, bool includeProperties)
    {
        var node = new ControlNode
        {
            Name = control.Name,
            Type = control.GetType().Name,
            Text = control.Text,
            X = control.Location.X,
            Y = control.Location.Y,
            Width = control.Width,
            Height = control.Height,
            Visible = control.Visible,
            Enabled = control.Enabled,
            Dock = control.Dock.ToString(),
            TabIndex = control.TabIndex
        };

        foreach (Control child in control.Controls)
        {
            node.Children.Add(BuildControlTree(child, includeProperties));
        }

        return node;
    }

    private static string FormatJson(ControlNode root)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(root, options);
    }

    private static string FormatTree(ControlNode root)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine("Control Hierarchy");
        output.AppendLine("════════════════════════════════════════════════");
        PrintNode(root, "", true, output);
        return output.ToString();
    }

    private static void PrintNode(ControlNode node, string indent, bool isLast, System.Text.StringBuilder sb)
    {
        sb.AppendLine(
            $"{indent}{(isLast ? "└── " : "├── ")}{node.Name} [{node.Type}] " +
            $"(Visible: {node.Visible}, Enabled: {node.Enabled})"
        );

        if (!string.IsNullOrEmpty(node.Text))
            sb.AppendLine($"{indent}{(isLast ? "    " : "│   ")}Text: \"{node.Text}\"");

        if (node.Width > 0 || node.Height > 0)
            sb.AppendLine(
                $"{indent}{(isLast ? "    " : "│   ")}" +
                $"Size: {node.Width}x{node.Height} @ ({node.X}, {node.Y})"
            );

        var nextIndent = indent + (isLast ? "    " : "│   ");

        for (int i = 0; i < node.Children.Count; i++)
        {
            PrintNode(node.Children[i], nextIndent, i == node.Children.Count - 1, sb);
        }
    }

    private static string FormatText(ControlNode root)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine("Control Hierarchy Export");
        output.AppendLine("════════════════════════════════════════════════");
        output.AppendLine($"Root: {root.Name} [{root.Type}]");
        output.AppendLine($"Total Controls: {CountControls(root)}");
        output.AppendLine();

        var depth = 0;
        var stack = new Stack<(ControlNode, int)>();
        stack.Push((root, depth));

        while (stack.Count > 0)
        {
            var (node, d) = stack.Pop();
            var indent = new string(' ', d * 2);

            output.AppendLine($"{indent}• {node.Name}");
            output.AppendLine($"{indent}  Type: {node.Type}");
            if (!string.IsNullOrEmpty(node.Text))
                output.AppendLine($"{indent}  Text: \"{node.Text}\"");
            output.AppendLine($"{indent}  Visible: {node.Visible}, Enabled: {node.Enabled}");
            output.AppendLine(
                $"{indent}  Size: {node.Width}x{node.Height}, Position: ({node.X}, {node.Y})"
            );
            output.AppendLine($"{indent}  Dock: {node.Dock}, TabIndex: {node.TabIndex}");
            output.AppendLine();

            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push((node.Children[i], d + 1));
            }
        }

        return output.ToString();
    }

    private static int CountControls(ControlNode root)
    {
        return 1 + root.Children.Sum(CountControls);
    }
}
