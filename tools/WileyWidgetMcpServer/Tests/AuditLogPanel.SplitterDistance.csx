using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Themes;
using WileyWidget.McpServer.Helpers;
using WileyWidget.WinForms.Configuration;
using WileyWidget.WinForms.Controls;
using WileyWidget.WinForms.Utils;

// Regression: Ensure AuditLogPanel SplitContainer respects valid SplitterDistance bounds
SkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
SkinManager.ApplicationVisualTheme = "Office2019Colorful";

var services = DependencyInjection.CreateServiceCollection(includeDefaults: true);
services.AddLogging();

using var provider = services.BuildServiceProvider();

var form = new SfForm
{
    Text = "AuditLogPanel SplitterDistance Test",
    Width = 1200,
    Height = 800
};
SkinManager.SetVisualStyle(form, "Office2019Colorful");

var panel = ActivatorUtilities.CreateInstance<AuditLogPanel>(provider);
panel.Dock = DockStyle.Fill;

form.Controls.Add(panel);
form.Show();
Application.DoEvents();

var split = panel.Controls.OfType<SplitContainer>().FirstOrDefault();
TestHelper.Assert(split != null, "AuditLogPanel SplitContainer not found");

split!.CreateControl();
split.PerformLayout();
Application.DoEvents();

var boundsAvailable = SafeSplitterDistanceHelper.TryGetValidDistanceRange(split, out var minDistance, out var maxDistance);
TestHelper.Assert(boundsAvailable, "Splitter distance bounds unavailable after layout");

TestHelper.Assert(split.SplitterDistance >= minDistance, $"SplitterDistance {split.SplitterDistance} < min {minDistance}");
TestHelper.Assert(split.SplitterDistance <= maxDistance, $"SplitterDistance {split.SplitterDistance} > max {maxDistance}");

form.Dispose();

"PASS: AuditLogPanel SplitterDistance within bounds.";
