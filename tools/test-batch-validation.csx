#!/usr/bin/env dotnet script

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// Load WinForms assembly to instantiate panels
var winformsPath = @"src\WileyWidget.WinForms\bin\Debug\net10.0-windows10.0.26100.0\WileyWidget.WinForms.dll";
#pragma warning disable IL2026
var winformsAsm = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), winformsPath));

Console.WriteLine("=== BATCH PANEL VALIDATION ===\n");
Console.WriteLine($"Loaded assembly: {winformsAsm.FullName}\n");

// Find all panel types
var panelTypes = winformsAsm.GetTypes()
    .Where(t => t.Name.EndsWith("Panel") && !t.IsAbstract)
    .OrderBy(t => t.Name)
    .ToList();
#pragma warning restore IL2026

Console.WriteLine($"Found {panelTypes.Count} panel types:\n");

foreach (var panelType in panelTypes)
{
    // Check if panel inherits from ScopedPanelBase
    var baseType = panelType.BaseType;
    var isScopedPanelBase = baseType?.Name.StartsWith("ScopedPanelBase") ?? false;

    // Check if panel implements ICompletablePanel
    #pragma warning disable IL2075
    var implementsCompletable = panelType.GetInterfaces().Any(i => i.Name == "ICompletablePanel");
    #pragma warning restore IL2075

    var status = isScopedPanelBase && implementsCompletable ? "✅ COMPLIANT" : "❌ NEEDS MIGRATION";

    Console.WriteLine($"{panelType.Name,-35} {status}");
    if (!isScopedPanelBase)
        Console.WriteLine($"  └─ Base: {baseType?.Name}");
}

Console.WriteLine("\n=== SUMMARY ===");
var compliant = panelTypes.Count(t =>
{
    var baseType = t.BaseType;
    return baseType?.Name.StartsWith("ScopedPanelBase") ?? false;
});

Console.WriteLine($"Compliant panels: {compliant}/{panelTypes.Count}");
Console.WriteLine($"Pending migration: {panelTypes.Count - compliant}");
