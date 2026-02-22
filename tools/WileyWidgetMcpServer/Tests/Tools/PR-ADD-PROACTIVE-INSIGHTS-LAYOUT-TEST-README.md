This PR adds a focused EvalCSharp test for `ProactiveInsightsPanel` layout invariants.

- Adds xUnit wrapper test `ProactiveInsightsPanelLayoutTests.cs` (calls `EvalCSharpTool.EvalCSharp` and asserts the returned JSON indicates success).
- Adds documentation and PR notes reminding that WinForms/Syncfusion tests require a Windows host (do not run in Docker Linux containers).

Please run CI on Windows runners to validate (the repo already contains Windows-hosted CI jobs for MCP tests).