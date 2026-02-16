using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WileyWidget.Services.Abstractions;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;
using Syncfusion.Windows.Forms.Tools;

var failures = new List<string>();
var trace = new List<string>();
var syncfusionOrNavExceptions = new List<Exception>();

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

void PumpMessages(int cycles)
{
    for (var i = 0; i < cycles; i++)
    {
        Application.DoEvents();
        System.Threading.Thread.Sleep(8);
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

var services = DependencyInjection.CreateServiceCollection(includeDefaults: true);
services.AddSingleton<IConfiguration>(configuration);
var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });

var logger = provider.GetService<ILogger<MainForm>>() ?? NullLogger<MainForm>.Instance;
var themeService = provider.GetService<IThemeService>() ?? Mock.Of<IThemeService>();
var windowStateService = provider.GetService<IWindowStateService>() ?? Mock.Of<IWindowStateService>();
var fileImportService = provider.GetService<IFileImportService>() ?? Mock.Of<IFileImportService>();

EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> firstChance = (_, args) =>
{
    var stack = args.Exception.StackTrace;
    if (stack != null &&
        (stack.Contains("Syncfusion", StringComparison.OrdinalIgnoreCase) ||
         stack.Contains("PanelNavigationService", StringComparison.OrdinalIgnoreCase) ||
         stack.Contains("MainForm.Navigation", StringComparison.OrdinalIgnoreCase)))
    {
        syncfusionOrNavExceptions.Add(args.Exception);
    }
};

AppDomain.CurrentDomain.FirstChanceException += firstChance;

var form = new MainForm(provider, configuration, logger, ReportViewerLaunchOptions.Disabled, themeService, windowStateService, fileImportService);
try
{
    form.Size = new System.Drawing.Size(1400, 900);
    _ = form.Handle;
    form.Show();
    PumpMessages(80);

    var ensurePanelNavigatorMethod = typeof(MainForm).GetMethod("EnsurePanelNavigatorInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
    ensurePanelNavigatorMethod?.Invoke(form, null);
    PumpMessages(20);

    var ribbon = GetPrivateField(form, "_ribbon") as Control;
    Require(ribbon != null, "MainForm private _ribbon is null after Show/OnLoad.");
    Require(ribbon != null && ribbon.Visible, "MainForm private _ribbon exists but is not visible.");

    var panelNavigator = form.PanelNavigator;
    Require(panelNavigator != null, "MainForm.PanelNavigator is null after initialization.");

    var ribbonToolStrips = Descendants(form)
        .OfType<ToolStrip>()
        .Where(ts => !string.Equals(ts.Name, "NavigationStrip", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var ribbonButtons = ribbonToolStrips
        .SelectMany(ts => FlattenToolStripItems(ts.Items.Cast<ToolStripItem>()))
        .OfType<ToolStripButton>()
        .Where(btn => btn.Enabled && !string.IsNullOrWhiteSpace(btn.Name) && btn.Name.StartsWith("Nav_", StringComparison.OrdinalIgnoreCase))
        .Distinct()
        .ToList();

    Require(ribbonButtons.Count > 0, "No enabled Nav_* buttons found in Ribbon toolstrips.");

    var buttonTargetContract = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Nav_Dashboard"] = "Dashboard",
        ["Nav_Accounts"] = "Municipal Accounts",
        ["Nav_Payments"] = "Payments",
        ["Nav_Budget"] = "Budget",
        ["Nav_Settings"] = "Settings"
    };

    var byName = ribbonButtons
        .GroupBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    foreach (var required in buttonTargetContract.Keys)
    {
        if (!byName.ContainsKey(required))
        {
            failures.Add($"Missing required ribbon button: {required}");
        }
    }

    var clickDurations = new List<long>();
    var successfulTransitions = 0;

    foreach (var kvp in buttonTargetContract)
    {
        if (!byName.TryGetValue(kvp.Key, out var button))
        {
            continue;
        }

        var activeBefore = panelNavigator?.GetActivePanelName();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        button.PerformClick();
        PumpMessages(24);
        sw.Stop();
        clickDurations.Add(sw.ElapsedMilliseconds);

        var activeAfter = panelNavigator?.GetActivePanelName();
        trace.Add($"CLICK {button.Name} ({button.Text}) => before:'{activeBefore ?? "<null>"}' after:'{activeAfter ?? "<null>"}' expected:'{kvp.Value}' ms:{sw.ElapsedMilliseconds}");

        if (string.Equals(activeAfter, kvp.Value, StringComparison.OrdinalIgnoreCase))
        {
            successfulTransitions++;
        }
        else
        {
            failures.Add($"{button.Name} did not activate expected target '{kvp.Value}'. Actual: '{activeAfter ?? "<null>"}'.");
        }
    }

    Require(successfulTransitions >= Math.Max(1, buttonTargetContract.Count - 1),
        $"Insufficient successful navigation transitions: {successfulTransitions}/{buttonTargetContract.Count}.");

    if (clickDurations.Count > 0)
    {
        var maxClick = clickDurations.Max();
        var avgClick = clickDurations.Average();
        if (maxClick > 900)
        {
            failures.Add($"Navigation click latency too high. Max={maxClick}ms Avg={avgClick:F1}ms.");
        }

        trace.Add($"LATENCY max={maxClick} avg={avgClick:F1}");
    }

    if (syncfusionOrNavExceptions.Count > 0)
    {
        var first = syncfusionOrNavExceptions[0];
        failures.Add($"Captured {syncfusionOrNavExceptions.Count} Syncfusion/navigation first-chance exception(s). First: {first.GetType().Name}: {first.Message}");
    }

    if (failures.Count > 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MainForm real navigation trace FAILED:");
        foreach (var failure in failures)
        {
            sb.AppendLine(" - " + failure);
        }

        sb.AppendLine("Trace:");
        foreach (var line in trace)
        {
            sb.AppendLine("   " + line);
        }

        throw new Exception(sb.ToString());
    }

    Console.WriteLine("PASS: MainForm real navigation trace completed.");
    foreach (var line in trace)
    {
        Console.WriteLine(line);
    }

    return true;
}
finally
{
    AppDomain.CurrentDomain.FirstChanceException -= firstChance;
    form.Dispose();
    if (provider is IDisposable disposableProvider)
    {
        disposableProvider.Dispose();
    }
}
