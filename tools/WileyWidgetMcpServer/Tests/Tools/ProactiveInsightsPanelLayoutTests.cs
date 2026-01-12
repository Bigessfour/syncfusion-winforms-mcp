using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using WileyWidget.McpServer.Tools;

namespace WileyWidget.McpServer.Tests.Tools
{
    public class ProactiveInsightsPanelLayoutTests
    {
        [Fact]
        public async Task EvalCSharp_ProactiveInsightsPanelLayout_ShouldPass()
        {
            var csx = @"using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using WileyWidget.WinForms.Controls;

// Instantiate and layout the panel
var pPanel = new ProactiveInsightsPanel();
pPanel.Size = new Size(800,600);
pPanel.CreateControl();
pPanel.PerformLayout();

var top = pPanel.Controls.Find(\"ProactiveTopPanel\", true).FirstOrDefault() as Control;
TestHelper.Assert(top != null, \"ProactiveTopPanel not found\");
TestHelper.Assert(top.Height >= 60, $\"Top panel height too small: {top.Height}\");

var feed = pPanel.Controls.Find(\"InsightFeedPanel\", true).FirstOrDefault() as Control;
TestHelper.Assert(feed != null, \"InsightFeedPanel not found\");

TestHelper.Assert(feed.Bounds.Top >= top.Bounds.Bottom, \"InsightFeedPanel positioned above header\");

var grid = feed.Controls.Find(\"InsightsDataGrid\", true).FirstOrDefault() as Control;
TestHelper.Assert(grid != null, \"InsightsDataGrid not found\");
TestHelper.Assert(grid.Bounds.Height >= 100, $\"DataGrid height too small: {grid.Bounds.Height}\");

var overlay = feed.Controls.Find(\"InsightLoadingOverlay\", true).FirstOrDefault() as Control;
TestHelper.Assert(overlay != null, \"InsightLoadingOverlay not found\");

overlay.Visible = true;
feed.PerformLayout();

TestHelper.Assert(grid.Bounds.Equals(overlay.Bounds), \"Loading overlay does not match grid bounds\");

overlay.Visible = false;

TestHelper.Log(\"Layout tests passed: header min size, grid fills remaining area, overlay covers grid\");

return true;";

            var result = await EvalCSharpTool.EvalCSharp(csx, timeoutSeconds: 30, jsonOutput: true);
            var json = JsonDocument.Parse(result);
            var success = json.RootElement.GetProperty("success").GetBoolean();
            Assert.True(success, $"EvalCSharp failed: {json.RootElement.GetProperty("error").GetString()} \n {json.RootElement.GetProperty("output").GetString()}");
        }
    }
}
