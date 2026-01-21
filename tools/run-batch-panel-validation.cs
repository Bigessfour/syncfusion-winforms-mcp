#!/usr/bin/env dotnet-script
// Execute: dotnet script run-batch-panel-validation.cs

#r "nuget: Spectre.Console"
#r "nuget: System.Reflection"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Spectre.Console;

// Load the WinForms assembly to discover panels
var assemblyPath = @"src\WileyWidget.WinForms\bin\Debug\net10.0-windows10.0.26100.0\WileyWidget.WinForms.dll";

if (!File.Exists(assemblyPath))
{
    AnsiConsole.MarkupLine("[red]❌ Assembly not found: {0}[/]", assemblyPath);
    AnsiConsole.MarkupLine("[yellow]Build the project first: dotnet build[/]");
    return;
}

try
{
    var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
    AnsiConsole.MarkupLine("[green]✓ Loaded:[/] {0}", assemblyPath);

    // Discover all UserControl panels in WileyWidget.WinForms.Controls namespace
    var panels = assembly.GetTypes()
        .Where(t =>
            t.BaseType?.Name == "UserControl" &&
            t.Namespace != null &&
            t.Namespace.StartsWith("WileyWidget.WinForms.Controls") &&
            !t.IsAbstract &&
            !t.Name.Contains("Designer"))
        .OrderBy(t => t.Name)
        .ToList();

    AnsiConsole.MarkupLine("[cyan]═══════════════════════════════════════════════════════════════[/]");
    AnsiConsole.MarkupLine("[cyan]📋 PANEL DISCOVERY REPORT[/]");
    AnsiConsole.MarkupLine("[cyan]═══════════════════════════════════════════════════════════════[/]");
    AnsiConsole.MarkupLine("");

    AnsiConsole.MarkupLine("[yellow]Total Panels Found:[/] [bold]{0}[/]", panels.Count);
    AnsiConsole.MarkupLine("");

    if (panels.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]⚠️  No panels discovered. Check assembly loading.[/]");
        return;
    }

    // Create a table with panel information
    var table = new Table();
    table.AddColumn("[cyan]Panel Name[/]");
    table.AddColumn("[cyan]Namespace[/]");
    table.AddColumn("[cyan]Full Type Name[/]");

    foreach (var panel in panels)
    {
        table.AddRow(
            panel.Name,
            panel.Namespace ?? "N/A",
            panel.FullName ?? "N/A"
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("");
    AnsiConsole.MarkupLine("[cyan]═══════════════════════════════════════════════════════════════[/]");
    AnsiConsole.MarkupLine("");

    // Show what the BatchValidatePanelsTool would validate
    AnsiConsole.MarkupLine("[cyan]🔍 Validation Categories:[/]");
    AnsiConsole.MarkupLine("  [green]✓[/] Theme Compliance (SfSkinManager authority)");
    AnsiConsole.MarkupLine("  [green]✓[/] Control API Usage (Syncfusion v32.1.19)");
    AnsiConsole.MarkupLine("  [green]✓[/] MVVM Bindings");
    AnsiConsole.MarkupLine("  [green]✓[/] Validation Setup (ErrorProvider)");
    AnsiConsole.MarkupLine("  [green]✓[/] Manual Color Violations");
    AnsiConsole.MarkupLine("  [green]✓[/] Event Handling");
    AnsiConsole.MarkupLine("  [green]✓[/] Resource Cleanup");
    AnsiConsole.MarkupLine("");

    AnsiConsole.MarkupLine("[cyan]📌 Next Steps:[/]");
    AnsiConsole.MarkupLine("  1. Use MCP Inspector: [bold]Tools > MCP Inspector[/]");
    AnsiConsole.MarkupLine("  2. Select: [bold]BatchValidatePanels[/]");
    AnsiConsole.MarkupLine("  3. Set parameters:");
    AnsiConsole.MarkupLine("     [gray]- panelTypeNames: null (validates all)[/]");
    AnsiConsole.MarkupLine("     - expectedTheme: [bold]Office2019Colorful[/]");
    AnsiConsole.MarkupLine("     - failFast: [bold]false[/]");
    AnsiConsole.MarkupLine("     - outputFormat: [bold]html[/] (or text/json)");
    AnsiConsole.MarkupLine("  4. Click [bold]Execute[/]");
    AnsiConsole.MarkupLine("");

    AnsiConsole.MarkupLine("[cyan]═══════════════════════════════════════════════════════════════[/]");
    AnsiConsole.MarkupLine("");
    AnsiConsole.MarkupLine("[green]✅ Ready to run BatchValidatePanelsTool![/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[red]❌ Error:[/] {0}", ex.Message);
    AnsiConsole.MarkupLine("[red]{0}[/]", ex.StackTrace);
}
