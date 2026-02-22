### Test details

- Test: `ProactiveInsightsPanelLayoutTests.EvalCSharp_ProactiveInsightsPanelLayout_ShouldPass`
- Purpose: Ensure header min-height is respected, data grid fills remaining area, and loading overlay covers the grid when visible.
- Runs: xUnit in `tools/WileyWidgetMcpServer/Tests` using `EvalCSharpTool.EvalCSharp(...)`.

### Notes
- This test requires a Windows host because WinForms and Syncfusion require Windows GDI/Platform APIs. Do not expect it to run successfully in a Linux container.
- Please run the MCP server or CI on a Windows runner to validate.