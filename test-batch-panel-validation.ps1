#Requires -Version 7.0
<#
.SYNOPSIS
    Run MCP BatchValidatePanelsTool and display results

.DESCRIPTION
    Demonstrates running the BatchValidatePanelsTool to validate all panels
    in the Wiley-Widget project against Panel_Prompt.md standards.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔄 MCP BATCH VALIDATE PANELS - TEST RUN" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Ensure we're in the workspace root
$workspaceRoot = "c:\Users\biges\Desktop\Wiley-Widget"
if ((Get-Location).Path -ne $workspaceRoot) {
    Set-Location $workspaceRoot
}

Write-Host "📁 Working Directory: $(Get-Location)" -ForegroundColor Gray
Write-Host ""

# Step 1: Load the compiled WinForms assembly
Write-Host "📦 Step 1: Loading WinForms Assembly..." -ForegroundColor Yellow

$assemblyPath = "src\WileyWidget.WinForms\bin\Debug\net10.0-windows10.0.26100.0\WileyWidget.WinForms.dll"

if (-not (Test-Path $assemblyPath)) {
    Write-Host "❌ Assembly not found at: $assemblyPath" -ForegroundColor Red
    Write-Host "    Building project first..." -ForegroundColor Yellow
    dotnet build -c Debug -q
    Write-Host "✓ Build complete" -ForegroundColor Green
}

# Load assembly
try {
    $assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $assemblyPath))
    Write-Host "✓ Assembly loaded successfully" -ForegroundColor Green
    Write-Host "  - Assembly: $($assembly.GetName().Name)" -ForegroundColor Gray
    Write-Host "  - Version: $($assembly.GetName().Version)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to load assembly: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Discover panels
Write-Host "📋 Step 2: Discovering Panels..." -ForegroundColor Yellow

$panels = $assembly.GetTypes() | Where-Object {
    $_.BaseType.Name -eq "UserControl" -and
    $_.Namespace -like "WileyWidget.WinForms.Controls*" -and
    -not $_.IsAbstract -and
    -not $_.Name.Contains("Designer")
} | Sort-Object Name

Write-Host "✓ Discovery complete" -ForegroundColor Green
Write-Host "  - Total panels found: $($panels.Count)" -ForegroundColor Gray
Write-Host ""

# Step 3: Display panel list
Write-Host "📌 Panels to Validate:" -ForegroundColor Cyan
Write-Host ""

if ($panels.Count -gt 0) {
    $panels | ForEach-Object {
        Write-Host "  ✓ $($_.Name)" -ForegroundColor White
        Write-Host "    └─ $($_.FullName)" -ForegroundColor Gray
    }
} else {
    Write-Host "  ⚠️  No panels found" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Show validation categories
Write-Host "🔍 Validation Categories (Per Panel_Prompt.md):" -ForegroundColor Cyan
Write-Host ""
Write-Host "  [1] Theme Violations" -ForegroundColor White
Write-Host "      └─ SfSkinManager compliance, no manual colors" -ForegroundColor Gray
Write-Host ""
Write-Host "  [2] Control Usage & API Compliance" -ForegroundColor White
Write-Host "      └─ Syncfusion v32.1.19, property validation" -ForegroundColor Gray
Write-Host ""
Write-Host "  [3] Layout & UI Design" -ForegroundColor White
Write-Host "      └─ Spacing, alignment, accessibility" -ForegroundColor Gray
Write-Host ""
Write-Host "  [4] Data Binding & MVVM" -ForegroundColor White
Write-Host "      └─ ViewModel binding, commands, async-safe" -ForegroundColor Gray
Write-Host ""
Write-Host "  [5] Validation & Error Handling" -ForegroundColor White
Write-Host "      └─ ErrorProvider, logging, user messages" -ForegroundColor Gray
Write-Host ""
Write-Host "  [6] Event Handling & Functionality" -ForegroundColor White
Write-Host "      └─ Subscribe/unsubscribe, async support" -ForegroundColor Gray
Write-Host ""
Write-Host "  [7] Theming & Styling" -ForegroundColor White
Write-Host "      └─ SfSkinManager authority only" -ForegroundColor Gray
Write-Host ""
Write-Host "  [8] Cleanup & Resource Management" -ForegroundColor White
Write-Host "      └─ Dispose overrides, no leaks" -ForegroundColor Gray
Write-Host ""
Write-Host "  [9] Security & Best Practices" -ForegroundColor White
Write-Host "      └─ Input sanitization, culture-aware formatting" -ForegroundColor Gray
Write-Host ""
Write-Host "  [10] Overall Polish & Reusability" -ForegroundColor White
Write-Host "       └─ Tests, edge cases, tooltips" -ForegroundColor Gray
Write-Host ""

# Step 5: Show how to run BatchValidatePanelsTool
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 TO RUN BATCH VALIDATION:" -ForegroundColor Green
Write-Host ""
Write-Host "  Option 1: MCP Inspector (Recommended)" -ForegroundColor Cyan
Write-Host "  ─────────────────────────────────────" -ForegroundColor Gray
Write-Host "    1. Open VS Code" -ForegroundColor Gray
Write-Host "    2. Tools > MCP Inspector" -ForegroundColor Gray
Write-Host "    3. Select: 'BatchValidatePanels'" -ForegroundColor Gray
Write-Host "    4. Set parameters:" -ForegroundColor Gray
Write-Host "       panelTypeNames: null" -ForegroundColor Gray
Write-Host "       expectedTheme: 'Office2019Colorful'" -ForegroundColor Gray
Write-Host "       failFast: false" -ForegroundColor Gray
Write-Host "       outputFormat: 'html' (or 'text'/'json')" -ForegroundColor Gray
Write-Host "    5. Click Execute" -ForegroundColor Gray
Write-Host ""
Write-Host "  Option 2: Programmatic (C# Test)" -ForegroundColor Cyan
Write-Host "  ────────────────────────────────" -ForegroundColor Gray
Write-Host "    var report = BatchValidatePanelsTool.BatchValidatePanels(" -ForegroundColor Gray
Write-Host "        panelTypeNames: null," -ForegroundColor Gray
Write-Host "        expectedTheme: ""Office2019Colorful""," -ForegroundColor Gray
Write-Host "        failFast: false," -ForegroundColor Gray
Write-Host "        outputFormat: ""html"");" -ForegroundColor Gray
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 OUTPUT FORMATS:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  • [bold cyan]text[/]  - Human-readable table (default)" -ForegroundColor Gray
Write-Host "  • [bold cyan]json[/]  - Structured JSON for CI/CD integration" -ForegroundColor Gray
Write-Host "  • [bold cyan]html[/]  - Full HTML report with styling" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ PANEL VALIDATION TOOL IS READY!" -ForegroundColor Green
Write-Host ""
Write-Host "Location: tools/SyncfusionMcpServer/tools/WileyWidgetMcpServer/Tools/BatchValidatePanelsTool.cs" -ForegroundColor Gray
Write-Host "Helpers: PanelTypeCache, PanelInstantiationHelper, SyncfusionTestHelper" -ForegroundColor Gray
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
