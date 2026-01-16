using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WileyWidget.McpServer.Tools;
using Xunit;

namespace WileyWidget.McpServer.Tests;

public class WarRoomPanelSplitterDistanceTests
{
    [Fact]
    public async Task WarRoomPanel_SplitterDistance_WithinBounds()
    {
        var csxPath = Path.Combine(AppContext.BaseDirectory, "WarRoomPanel.SplitterDistance.csx");
        Assert.True(File.Exists(csxPath), $"Missing csx test file: {csxPath}");

        var json = await EvalCSharpTool.EvalCSharp(
            csx: string.Empty,
            csxFile: csxPath,
            timeoutSeconds: 30,
            jsonOutput: true);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        if (!success)
        {
            var error = root.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : "Unknown error";
            var output = root.TryGetProperty("output", out var outputProp) ? outputProp.GetString() : string.Empty;
            Assert.True(success, $"EvalCSharp failed: {error}{Environment.NewLine}{output}");
        }

        if (root.TryGetProperty("output", out var outputText))
        {
            var output = outputText.GetString() ?? string.Empty;
            Assert.DoesNotContain("FAIL", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
