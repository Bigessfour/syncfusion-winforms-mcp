@keepers - please run MCP CI on this branch (Windows runner). This PR adds a headless EvalCSharp xUnit test verifying `ProactiveInsightsPanel` layout invariants and includes notes that the test requires a Windows host.

Files added:
- `tools/WileyWidgetMcpServer/Tests/Tools/ProactiveInsightsPanelLayoutTests.cs` (xUnit wrapper)
- `tools/WileyWidgetMcpServer/Tests/Tools/PR-ADD-PROACTIVE-INSIGHTS-LAYOUT-TEST.md` (PR notes)

Why: Ensures layout changes are validated in the canonical MCP test layer before merging UI changes into Wiley-Widget main branch.

Thanks!