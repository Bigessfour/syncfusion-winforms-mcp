using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WileyWidget.McpServer.Helpers;
using WileyWidget.Services.Abstractions;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Controls.Panels;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;
using Syncfusion.Windows.Forms.Tools;

var failures = new List<string>();

void Require(bool condition, string message)
{
    if (!condition)
    {
        failures.Add(message);
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

IEnumerable<ToolStripItem> FlattenToolStripItems(IEnumerable<ToolStripItem> items)
{
    foreach (var item in items)
    {
        yield return item;

        if (item is ToolStripPanelItem panelItem)
        {
            foreach (var nested in FlattenToolStripItems(panelItem.Items.Cast<ToolStripItem>()))
            {
                yield return nested;
            }
        }

        if (item is ToolStripDropDownItem dropDown)
        {
            foreach (var nested in FlattenToolStripItems(dropDown.DropDownItems.Cast<ToolStripItem>()))
            {
                yield return nested;
            }
        }
    }
}

object? GetPrivateField(object instance, string fieldName)
{
    var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    return field?.GetValue(instance);
}

void SetPrivateField(object instance, string fieldName, object? value)
{
    var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    field?.SetValue(instance, value);
}

void PumpMessages(int cycles)
{
    for (var i = 0; i < cycles; i++)
    {
        Application.DoEvents();
        System.Threading.Thread.Sleep(6);
    }
}

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["UI:IsUiTestHarness"] = "true",
        ["UI:UseSyncfusionDocking"] = "true",
        ["UI:ShowRibbon"] = "true",
        ["UI:ShowStatusBar"] = "true",
        ["UI:AutoShowDashboard"] = "false",
        ["UI:AutoShowPanels"] = "false",
        ["UI:MinimalMode"] = "false",
        ["UI:DefaultTheme"] = "Office2019Colorful"
    })
    .Build();

var provider = WileyWidget.McpServer.Helpers.MockFactory.CreateTestServiceProvider();
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
var navigationTargets = new List<string>();
var dispatchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var clickDurationsMs = new List<long>();
string? activePanelName = null;

void RecordDispatch(string panelName)
{
    activePanelName = panelName;
    navigationTargets.Add(panelName);
    dispatchCounts[panelName] = dispatchCounts.TryGetValue(panelName, out var count) ? count + 1 : 1;
}

var panelNavigatorMock = new Mock<WileyWidget.WinForms.Services.IPanelNavigationService>();
panelNavigatorMock
    .Setup(x => x.GetActivePanelName())
    .Returns(() => activePanelName);

panelNavigatorMock
    .Setup(x => x.ShowForm<BudgetDashboardForm>(It.IsAny<string>(), It.IsAny<DockingStyle>(), It.IsAny<bool>()))
    .Callback<string, DockingStyle, bool>((panelName, _, _) => RecordDispatch(panelName));

panelNavigatorMock
    .Setup(x => x.ShowPanel<AccountsPanel>(It.IsAny<string>(), It.IsAny<DockingStyle>(), It.IsAny<bool>()))
    .Callback<string, DockingStyle, bool>((panelName, _, _) => RecordDispatch(panelName));

panelNavigatorMock
    .Setup(x => x.ShowPanel<PaymentsPanel>(It.IsAny<string>(), It.IsAny<DockingStyle>(), It.IsAny<bool>()))
    .Callback<string, DockingStyle, bool>((panelName, _, _) => RecordDispatch(panelName));

panelNavigatorMock
    .Setup(x => x.ShowPanel<BudgetPanel>(It.IsAny<string>(), It.IsAny<DockingStyle>(), It.IsAny<bool>()))
    .Callback<string, DockingStyle, bool>((panelName, _, _) => RecordDispatch(panelName));

panelNavigatorMock
    .Setup(x => x.ShowPanel<SettingsPanel>(It.IsAny<string>(), It.IsAny<DockingStyle>(), It.IsAny<bool>()))
    .Callback<string, DockingStyle, bool>((panelName, _, _) => RecordDispatch(panelName));

EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> firstChance = (_, args) =>
{
    if (args.Exception is ArgumentOutOfRangeException &&
        args.Exception.StackTrace != null &&
        args.Exception.StackTrace.Contains("Syncfusion", StringComparison.OrdinalIgnoreCase))
    {
        syncfusionDockingErrors.Add(args.Exception);
    }
};

AppDomain.CurrentDomain.FirstChanceException += firstChance;

var form = new MainForm(provider, configuration, logger, ReportViewerLaunchOptions.Disabled, themeService, windowStateService, fileImportService);
try
{
    SetPrivateField(form, "_panelNavigator", panelNavigatorMock.Object);

    form.Size = new System.Drawing.Size(1400, 900);
    _ = form.Handle;
    form.Show();
    PumpMessages(60);

    var ribbon = GetPrivateField(form, "_ribbon") as Control;
    Require(ribbon != null, "MainForm private _ribbon field is null after Show/OnLoad.");
    Require(ribbon != null && ribbon.Visible, "MainForm ribbon exists but is not visible.");

    var panelNavigator = GetPrivateField(form, "_panelNavigator");
    Require(panelNavigator != null, "MainForm private _panelNavigator is null after test injection.");

    var ribbonToolStrips = Descendants(form)
        .OfType<ToolStrip>()
        .Where(ts => !string.Equals(ts.Name, "NavigationStrip", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var allNavButtons = ribbonToolStrips
        .SelectMany(ts => FlattenToolStripItems(ts.Items.Cast<ToolStripItem>()))
        .OfType<ToolStripButton>()
        .Where(button => !string.IsNullOrWhiteSpace(button.Name) &&
                         button.Name.StartsWith("Nav_", StringComparison.OrdinalIgnoreCase))
        .Distinct()
        .ToList();

    var ribbonNavButtons = allNavButtons
        .Where(button => button.Enabled)
        .ToList();

    var discoveredNames = allNavButtons
        .Select(button => $"{button.Name}(Enabled={button.Enabled},Visible={button.Visible})")
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var stripSummaries = ribbonToolStrips
        .Select(ts => $"{ts.Name}(Items={ts.Items.Count},Visible={ts.Visible},Enabled={ts.Enabled})")
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var discoveredSet = allNavButtons
        .Select(button => button.Name)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var enabledSet = ribbonNavButtons
        .Select(button => button.Name)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var buttonTargetContract = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Nav_Dashboard"] = "Dashboard",
        ["Nav_Accounts"] = "Municipal Accounts",
        ["Nav_Payments"] = "Payments",
        ["Nav_Budget"] = "Budget",
        ["Nav_Settings"] = "Settings"
    };

    var discoveredByName = ribbonNavButtons
        .GroupBy(button => button.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    var disabledRequiredButtons = buttonTargetContract.Keys
        .Where(name => discoveredSet.Contains(name) && !enabledSet.Contains(name))
        .ToList();

    if (disabledRequiredButtons.Count > 0)
    {
        failures.Add("Required ribbon buttons are present but disabled: " + string.Join(", ", disabledRequiredButtons));
    }

    var missingRequiredButtons = buttonTargetContract.Keys
        .Where(key => !discoveredSet.Contains(key))
        .ToList();

    if (missingRequiredButtons.Count > 0)
    {
        failures.Add("Missing required ribbon buttons: " + string.Join(", ", missingRequiredButtons));
    }

    if (discoveredNames.Count == 0)
    {
        failures.Add("No Nav_* buttons found across ribbon toolstrips.");
    }

    if (stripSummaries.Count == 0)
    {
        failures.Add("No ToolStrip instances discovered for ribbon inspection.");
    }

    var contractButtonsEnabled = buttonTargetContract.Keys
        .Where(enabledSet.Contains)
        .ToList();

    var buttonsToClick = contractButtonsEnabled
        .Where(discoveredByName.ContainsKey)
        .Select(key => discoveredByName[key])
        .ToList();

    if (buttonsToClick.Count == 0)
    {
        buttonsToClick = ribbonNavButtons.Take(3).ToList();
        failures.Add("No enabled contract buttons available; using fallback Nav_* buttons only.");
    }
    Require(ribbonNavButtons.Count > 0, "No enabled ribbon Nav_* buttons were discovered on MainForm.");

    void ClickAndVerify(ToolStripButton button, string expectedTarget)
    {
        var beforeCount = navigationTargets.Count;
        var clickStopwatch = System.Diagnostics.Stopwatch.StartNew();
        button.PerformClick();
        PumpMessages(16);
        clickStopwatch.Stop();
        clickDurationsMs.Add(clickStopwatch.ElapsedMilliseconds);

        if (navigationTargets.Count <= beforeCount)
        {
            failures.Add($"Button {button.Name} did not dispatch navigation call.");
            return;
        }

        var actualTarget = navigationTargets[^1];
        if (!string.Equals(actualTarget, expectedTarget, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"Button {button.Name} dispatched '{actualTarget}' instead of expected '{expectedTarget}'.");
        }
    }

    var rounds = 3;
    for (var round = 1; round <= rounds; round++)
    {
        foreach (var button in buttonsToClick)
        {
            var expectedTarget = buttonTargetContract.TryGetValue(button.Name, out var target)
                ? target
                : button.Name;
            ClickAndVerify(button, expectedTarget);
        }
    }

    var stressSequence = new[] { "Nav_Accounts", "Nav_Payments", "Nav_Budget", "Nav_Accounts", "Nav_Settings", "Nav_Dashboard" };
    foreach (var name in stressSequence)
    {
        if (discoveredByName.TryGetValue(name, out var stressButton) && buttonTargetContract.TryGetValue(name, out var expectedTarget))
        {
            ClickAndVerify(stressButton, expectedTarget);
        }
    }

    Require(navigationTargets.Count > 0, "Ribbon Nav_* clicks did not dispatch any panel navigation calls.");

    foreach (var contract in buttonTargetContract)
    {
        if (!discoveredByName.ContainsKey(contract.Key))
        {
            continue;
        }

        var observedDispatchCount = dispatchCounts.TryGetValue(contract.Value, out var count) ? count : 0;
        Require(observedDispatchCount > 0, $"Expected target '{contract.Value}' was never dispatched by {contract.Key}.");
    }

    var firstDockingErrorMessage = syncfusionDockingErrors.Count > 0
        ? syncfusionDockingErrors[0].Message
        : "none";
    Require(syncfusionDockingErrors.Count == 0,
        $"Navigation triggered {syncfusionDockingErrors.Count} Syncfusion docking ArgumentOutOfRangeException(s). First: {firstDockingErrorMessage}");

    var hasVisibleContentRegion = Descendants(form)
        .Any(control =>
            control.Visible
            && control != ribbon
            && control.Width > 0
            && control.Height > 0
            && (control is Panel || control is UserControl || control is SplitContainer));

    Require(hasVisibleContentRegion,
        "MainForm did not expose a visible content region after ribbon navigation stress sequence.");

    var maxDurationMs = clickDurationsMs.Count > 0 ? clickDurationsMs.Max() : 0;
    var avgDurationMs = clickDurationsMs.Count > 0 ? clickDurationsMs.Average() : 0;

    if (maxDurationMs > 700)
    {
        failures.Add($"Navigation click latency too high. Max={maxDurationMs}ms Avg={avgDurationMs:F1}ms.");
    }

    if (failures.Count > 0)
    {
        var summary = new StringBuilder();
        summary.AppendLine("Ribbon navigation stress validation failed:");
        foreach (var failure in failures)
        {
            summary.AppendLine(" - " + failure);
        }

        summary.AppendLine($"Dispatches: {navigationTargets.Count}");
        summary.AppendLine($"MaxClickMs: {maxDurationMs}, AvgClickMs: {avgDurationMs:F1}");
        summary.AppendLine("ToolStrips: " + string.Join(" | ", stripSummaries));
        summary.AppendLine("DiscoveredButtons: " + string.Join(", ", discoveredNames));
        summary.AppendLine("ObservedTargets: " + string.Join(", ", navigationTargets.Take(20)));
        throw new Exception(summary.ToString());
    }

    Console.WriteLine($"PASS: realistic ribbon navigation stress run completed. Buttons={ribbonNavButtons.Count}, Dispatches={navigationTargets.Count}, MaxClickMs={maxDurationMs}, AvgClickMs={avgDurationMs:F1}.");
    return true;
}
finally
{
    AppDomain.CurrentDomain.FirstChanceException -= firstChance;
    form.Dispose();
}
