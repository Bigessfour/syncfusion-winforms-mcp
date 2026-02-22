#r "C:/Program Files/dotnet/shared/Microsoft.WindowsDesktop.App/10.0.3/System.Windows.Forms.dll"
#r "C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.3/System.Drawing.dll"
#r "C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.3/System.Drawing.Primitives.dll"
#r "C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/10.0.3/Microsoft.Extensions.Configuration.dll"
#r "C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/10.0.3/Microsoft.Extensions.Configuration.Abstractions.dll"
#r "C:/Program Files/dotnet/shared/Microsoft.AspNetCore.App/10.0.3/Microsoft.Extensions.Logging.Abstractions.dll"
#r "C:/Users/biges/Desktop/Wiley_Widget/tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/bin/Debug/net10.0-windows10.0.26100.0/Moq.dll"
#r "C:/Users/biges/Desktop/Wiley_Widget/tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/bin/Debug/net10.0-windows10.0.26100.0/Castle.Core.dll"
#r "C:/Users/biges/Desktop/Wiley_Widget/tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/bin/Debug/net10.0-windows10.0.26100.0/WileyWidget.WinForms.dll"
#r "C:/Users/biges/Desktop/Wiley_Widget/tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/bin/Debug/net10.0-windows10.0.26100.0/WileyWidget.Services.Abstractions.dll"
#r "C:/Users/biges/Desktop/Wiley_Widget/tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/bin/Debug/net10.0-windows10.0.26100.0/WileyWidget.Abstractions.dll"

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WileyWidget.Services.Abstractions;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

IEnumerable<Control> Descendants(Control root)
{
    foreach (Control child in root.Controls)
    {
        yield return child;
        foreach (var grandChild in Descendants(child))
        {
            yield return grandChild;
        }
    }
}

int GetClickHandlerCount(ToolStripItem item)
{
    var eventsProp = typeof(Component).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
    var eventClickField = typeof(ToolStripItem).GetField("EventClick", BindingFlags.Static | BindingFlags.NonPublic);

    var eventList = eventsProp?.GetValue(item) as EventHandlerList;
    var key = eventClickField?.GetValue(null);
    if (eventList == null || key == null)
    {
        return 0;
    }

    var handler = eventList[key] as Delegate;
    return handler?.GetInvocationList().Length ?? 0;
}

bool IsNavigationText(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return false;
    }

    var normalized = text.Trim().ToLowerInvariant();
    return normalized.Contains("enterprise")
        || normalized.Contains("vital")
        || normalized.Contains("account")
        || normalized.Contains("payment")
        || normalized.Contains("settings")
        || normalized.Contains("budget")
        || normalized.Contains("invoice")
        || normalized.Contains("report");
}

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["UI:IsUiTestHarness"] = "true",
        ["UI:UseSyncfusionDocking"] = "true",
        ["UI:ShowRibbon"] = "true",
        ["UI:ShowStatusBar"] = "true",
        ["UI:AutoShowDashboard"] = "false",
        ["UI:AutoShowPanels"] = "false"
    })
    .Build();

var provider = new Mock<IServiceProvider>().Object;
var logger = NullLogger<MainForm>.Instance;

var themeMock = new Mock<IThemeService>();
themeMock.Setup(x => x.CurrentTheme).Returns("Office2019Colorful");
themeMock.Setup(x => x.IsDark).Returns(false);
var themeService = themeMock.Object;

var windowStateMock = new Mock<IWindowStateService>();
windowStateMock.Setup(x => x.LoadMru()).Returns(new List<string>());
var windowStateService = windowStateMock.Object;

var fileImportService = new Mock<IFileImportService>().Object;

var syncfusionDockingErrors = new List<Exception>();
FirstChanceExceptionEventHandler firstChance = (_, args) =>
{
    if (args.Exception is ArgumentOutOfRangeException &&
        args.Exception.StackTrace != null &&
        args.Exception.StackTrace.Contains("Syncfusion", StringComparison.OrdinalIgnoreCase))
    {
        syncfusionDockingErrors.Add(args.Exception);
    }
};

AppDomain.CurrentDomain.FirstChanceException += firstChance;

try
{
    using var form = new MainForm(provider, configuration, logger, ReportViewerLaunchOptions.Disabled, themeService, windowStateService, fileImportService);
    form.Size = new System.Drawing.Size(1400, 900);
    _ = form.Handle;

    form.Show();
    Application.DoEvents();

    var ribbon = Descendants(form).FirstOrDefault(control => control.GetType().Name.Contains("RibbonControlAdv", StringComparison.OrdinalIgnoreCase));
    Assert(ribbon != null, "RibbonControlAdv was not found in MainForm control tree.");
    Assert(ribbon!.Visible, "RibbonControlAdv is not visible.");
    Assert(ribbon.Parent != null, "RibbonControlAdv has no parent; expected to be mounted in MainForm.");

    var toolStrips = Descendants(form).OfType<ToolStrip>().ToList();
    var navButtons = toolStrips
        .SelectMany(ts => ts.Items.Cast<ToolStripItem>())
        .Where(item => item is ToolStripButton && item.Enabled && IsNavigationText(item.Text))
        .Distinct()
        .Take(6)
        .ToList();

    Assert(navButtons.Count > 0, "No enabled ribbon navigation buttons were discovered.");

    var wiredButtonCount = navButtons.Count(item => GetClickHandlerCount(item) > 0);
    Assert(wiredButtonCount > 0, "Navigation buttons were discovered but none have click handlers attached.");

    var clicked = 0;
    foreach (var button in navButtons)
    {
        if (button is ToolStripButton navButton)
        {
            navButton.PerformClick();
            Application.DoEvents();
            Application.DoEvents();
            clicked++;
        }
    }

    Assert(clicked > 0, "No navigation buttons were successfully clicked.");
    Assert(syncfusionDockingErrors.Count == 0,
        $"Navigation triggered {syncfusionDockingErrors.Count} Syncfusion docking ArgumentOutOfRangeException(s). First: {syncfusionDockingErrors[0].Message}");

    var hasVisibleContentRegion = Descendants(form)
        .Any(control =>
            control.Visible
            && control != ribbon
            && control.Parent != ribbon
            && control.Width > 0
            && control.Height > 0
            && (control is Panel || control is UserControl || control is SplitContainer));

    Assert(hasVisibleContentRegion,
        "MainForm does not appear to have an active visible content region after ribbon navigation clicks.");

    Console.WriteLine($"PASS: RibbonControlAdv discovered, {wiredButtonCount} nav button(s) wired, {clicked} click(s) executed, and no Syncfusion docking out-of-range exceptions.");
}
finally
{
    AppDomain.CurrentDomain.FirstChanceException -= firstChance;
}
