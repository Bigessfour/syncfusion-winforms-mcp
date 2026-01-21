using Xunit;
using WileyWidget.McpServer.Tools;

namespace WileyWidget.McpServer.Tests;

/// <summary>
/// Integration test that runs the BatchValidatePanelsTool
/// against all panels in the WileyWidget.WinForms.Controls namespace.
/// </summary>
public class BatchValidatePanelsTests
{
    [Fact]
    public void BatchValidatePanels_ValidateAll_ReturnsReport()
    {
        // Act - validate all panels with text output
        var report = BatchValidatePanelsTool.BatchValidatePanels(
            panelTypeNames: null,
            expectedTheme: "Office2019Colorful",
            failFast: false,
            outputFormat: "text");

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);

        // Print report for review
        System.Console.WriteLine(report);
    }

    [Fact]
    public void BatchValidatePanels_ValidateAll_JsonOutput()
    {
        // Act - validate all panels with JSON output
        var report = BatchValidatePanelsTool.BatchValidatePanels(
            panelTypeNames: null,
            expectedTheme: "Office2019Colorful",
            failFast: false,
            outputFormat: "json");

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        Assert.Contains("\"", report); // JSON should have quotes

        // Print report for review
        System.Console.WriteLine(report);
    }

    [Fact]
    public void BatchValidatePanels_ValidateAll_HtmlOutput()
    {
        // Act - validate all panels with HTML output
        var report = BatchValidatePanelsTool.BatchValidatePanels(
            panelTypeNames: null,
            expectedTheme: "Office2019Colorful",
            failFast: false,
            outputFormat: "html");

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report);
        Assert.Contains("<", report); // HTML should have tags

        // Print report for review
        System.Console.WriteLine(report);
    }
}
