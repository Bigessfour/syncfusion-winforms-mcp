using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Themes;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using WileyWidget.Abstractions;
using WileyWidget.Services.Abstractions;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;
using WileyWidget.WinForms.Services.Abstractions;

sealed class NoopWindowStateService : IWindowStateService
{
    public void RestoreWindowState(Form form) { }
    public void SaveWindowState(Form form) { }
    public List<string> LoadMru() => new();
    public void SaveMru(List<string> mruList) { }
    public void AddToMru(string filePath) { }
    public void ClearMru() { }
}

sealed class NoopFileImportService : IFileImportService
{
    public Task<Result<T>> ImportDataAsync<T>(string filePath, CancellationToken ct = default) where T : class
        => Task.FromResult(Result<T>.Failure("Not implemented"));

    public Task<Result> ValidateImportFileAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult(Result.Failure("Not implemented"));
}

sealed class SimpleThemeService : IThemeService
{
    public event EventHandler<string>? ThemeChanged;

    public string CurrentTheme { get; private set; } = "Office2019Colorful";

    public void ApplyTheme(string themeName)
    {
        CurrentTheme = themeName;
        SkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
        SkinManager.ApplicationVisualTheme = themeName;
        ThemeChanged?.Invoke(this, themeName);
    }
}

SkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
SkinManager.ApplicationVisualTheme = "Office2019Colorful";

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["UI:IsUiTestHarness"] = "true",
        ["UI:UseSyncfusionDocking"] = "true",
        ["UI:ShowRibbon"] = "true",
        ["UI:ShowStatusBar"] = "true"
    })
    .Build();

services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddDebug());
services.AddSingleton<IThemeService>(new SimpleThemeService());
services.AddSingleton<IWindowStateService>(new NoopWindowStateService());
services.AddSingleton<IFileImportService>(new NoopFileImportService());
services.AddSingleton(ReportViewerLaunchOptions.Disabled);

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<MainForm>>();
var themeService = provider.GetRequiredService<IThemeService>();
var windowStateService = provider.GetRequiredService<IWindowStateService>();
var fileImportService = provider.GetRequiredService<IFileImportService>();

using var form = new MainForm(provider, configuration, logger, ReportViewerLaunchOptions.Disabled, themeService, windowStateService, fileImportService);
var _ = form.Handle;

var (dockingManager, _, _, centralDocumentPanel, _, _, _) = DockingHostFactory.CreateDockingHost(form, provider, null, logger);

TestHelper.Assert(dockingManager.DockToFill, "DockToFill must be enabled to allow DockingStyle.Fill on the host control.");
TestHelper.Assert(dockingManager.HostControl != null, "HostControl must be set before docking.");

var dockStyle = dockingManager.GetDockStyle(centralDocumentPanel);
TestHelper.Assert(dockStyle == DockingStyle.Tabbed, $"Central panel should be DockingStyle.Tabbed. Actual: {dockStyle}");

"PASS: DockingManager DockToFill enabled and central panel docks Tabbed.";
