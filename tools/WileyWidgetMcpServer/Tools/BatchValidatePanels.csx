// BatchValidatePanels.csx - Test script for BatchValidatePanelsTool
// Runs batch validation on all panels in WileyWidget.WinForms.Controls namespace

using System;
using System.Windows.Forms;
using WileyWidget.McpServer.Tools;

// Run batch validation on all panels
var result = BatchValidatePanelsTool.BatchValidatePanels(
    panelTypeNames: null,  // null means validate all panels
    expectedTheme: "Office2019Colorful",
    failFast: false,
    outputFormat: "text"
);

Console.WriteLine(result);
