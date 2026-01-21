using WileyWidget.McpServer.Tools;

var report = BatchValidateFormsTool.BatchValidateForms(
    formTypeNames: null,
    expectedTheme: "Office2019Colorful",
    failFast: false,
    outputFormat: "text");

Console.WriteLine(report);
return true;
