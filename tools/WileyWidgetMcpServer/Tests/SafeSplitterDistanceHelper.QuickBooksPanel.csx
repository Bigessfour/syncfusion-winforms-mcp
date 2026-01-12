using System;
using System.Windows.Forms;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Themes;
using Syncfusion.Windows.Forms;
using WileyWidget.McpServer.Helpers;
using WileyWidget.WinForms.Utils;

// Regression: Ensure SafeSplitterDistanceHelper handles QuickBooksPanel-style layout safely at narrow widths
SkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
SkinManager.ApplicationVisualTheme = "Office2019Colorful";

var form = new SfForm
{
    Text = "Safe Splitter Regression",
    Width = 900,
    Height = 600
};
SkinManager.SetVisualStyle(form, "Office2019Colorful");

var split = new SplitContainer
{
    Orientation = Orientation.Vertical,
    Dock = DockStyle.Fill,
    FixedPanel = FixedPanel.Panel1,
    Panel1MinSize = 250,
    Panel2MinSize = 250
};

SafeSplitterDistanceHelper.SetSplitterDistanceDeferred(split, 400);
SafeSplitterDistanceHelper.SetupProportionalResizing(split, 0.5);

form.Controls.Add(split);
form.Show();
Application.DoEvents();

// Initial apply should succeed when width is sufficient
var initialApplied = SafeSplitterDistanceHelper.TrySetSplitterDistance(split, 400);
TestHelper.Assert(initialApplied, "Initial splitter distance should apply when space is available");

var requiredWidth = split.Panel1MinSize + split.Panel2MinSize + split.SplitterWidth;
form.Width = requiredWidth - 20; // force below combined min sizes
form.PerformLayout();
Application.DoEvents();

// When width is too small, helper should decline without throwing
var declined = SafeSplitterDistanceHelper.TrySetSplitterDistance(split, 400);
TestHelper.Assert(!declined, "TrySetSplitterDistance should return false when space is insufficient");

var boundsAvailable = SafeSplitterDistanceHelper.TryGetBounds(split, out var minDistance, out var maxDistance);
TestHelper.Assert(!boundsAvailable, "Bounds should be unavailable when width is below required total");

// Restore width and ensure helper can reapply safely
form.Width = requiredWidth + 60;
form.PerformLayout();
Application.DoEvents();

var reapplied = SafeSplitterDistanceHelper.TrySetSplitterDistance(split, 420);
TestHelper.Assert(reapplied, "Splitter distance should reapply after width is restored");

var maxAllowed = form.ClientSize.Width - split.Panel2MinSize - split.SplitterWidth;
TestHelper.Assert(split.SplitterDistance >= split.Panel1MinSize, "SplitterDistance respects Panel1MinSize");
TestHelper.Assert(split.SplitterDistance <= maxAllowed, "SplitterDistance respects maximum bound");

form.Dispose();

"PASS: SafeSplitterDistanceHelper handles narrow widths without exceptions.";
