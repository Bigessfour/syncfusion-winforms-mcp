// TestPanelInstantiation.csx - Test script for BatchValidatePanelsTool
// Runs validation on a single panel (SettingsPanel) to test the instantiation fix

var result = WileyWidget.McpServer.Tools.BatchValidatePanelsTool.BatchValidatePanels(
    panelTypeNames: new[] { "WileyWidget.WinForms.Controls.SettingsPanel" },
    expectedTheme: "Office2019Colorful",
    failFast: true,
    outputFormat: "text"
);

Console.WriteLine(result);
