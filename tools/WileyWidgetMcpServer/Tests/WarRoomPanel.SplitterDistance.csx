using System;
using System.Linq;
using System.Windows.Forms;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Themes;
using Syncfusion.Windows.Forms;
using WileyWidget.McpServer.Helpers;
using WileyWidget.WinForms.Controls;
using WileyWidget.WinForms.ViewModels;
using WileyWidget.WinForms.Utils;

// Regression: Ensure WarRoomPanel chart SplitContainer respects valid SplitterDistance bounds
SkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
SkinManager.ApplicationVisualTheme = "Office2019Colorful";

var form = new SfForm
{
    Text = "WarRoomPanel SplitterDistance Test",
    Width = 1200,
    Height = 800
};
SkinManager.SetVisualStyle(form, "Office2019Colorful");

var panel = new WarRoomPanel(new WarRoomViewModel())
{
    Dock = DockStyle.Fill
};

form.Controls.Add(panel);
form.Show();
Application.DoEvents();

var chartSplit = panel.Controls.Find("ChartSplitContainer", true).FirstOrDefault() as SplitContainer;
TestHelper.Assert(chartSplit != null, "ChartSplitContainer not found");

chartSplit!.CreateControl();
chartSplit.PerformLayout();
Application.DoEvents();

var boundsAvailable = SafeSplitterDistanceHelper.TryGetValidDistanceRange(chartSplit, out var minDistance, out var maxDistance);
TestHelper.Assert(boundsAvailable, "Splitter distance bounds unavailable after layout");

TestHelper.Assert(chartSplit.SplitterDistance >= minDistance, $"SplitterDistance {chartSplit.SplitterDistance} < min {minDistance}");
TestHelper.Assert(chartSplit.SplitterDistance <= maxDistance, $"SplitterDistance {chartSplit.SplitterDistance} > max {maxDistance}");

form.Dispose();

"PASS: WarRoomPanel SplitterDistance within bounds.";
