# Examples

## Real-World Usage Scenarios

### Example 1: Validate Theme Compliance Across All Forms

```json
{
  "mcpServers": {
    "syncfusion-winforms": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/WileyWidgetMcpServer.csproj"]
    }
  }
}
```

**Prompt:**

```
Use the BatchValidateFormsTool to validate all forms in the WileyWidget.WinForms.Forms namespace
use Office2019Colorful theme and output as JSON.
```

### Example 2: Inspect DataGrid Configuration

```
Use InspectSfDataGridTool to inspect the AccountsForm's SfDataGrid configuration.
Show me columns, data binding, and sample rows.
```

### Example 3: Detect Manual Color Assignments

```
Use DetectManualColorsTool to find all manual color assignments in EnterpriseVitalSignsPanel.
I want to ensure we're using SkinManager everywhere.
```

### Example 4: Run Headless Form Test

```
Use RunHeadlessFormTestTool to test AccountsForm initialization without UI rendering.
I want to verify the form loads correctly and all controls are properly themed.
```

### Example 5: Dynamic C# Evaluation

```
Use EvalCSharpTool to instantiate AccountsForm and check if the grid DataSource is null.
```

### Example 6: Export Control Hierarchy

```
Use ExportControlHierarchyTool to export the complete control tree of MainForm as JSON.
I need to understand the nested structure for documentation.
```

### Example 7: Dependency Injection Tests

```
Use RunDependencyInjectionTestsTool to run all DI validation tests.
I want to ensure services are registered with correct lifetimes.
```

## GitHub Copilot Chat Integration

These tools work seamlessly with GitHub Copilot Chat:

```
@workspace Can you use the syncfusion-winforms MCP server to validate
that all my forms use Office2019Colorful theme? Show me any violations.
```

Copilot will automatically invoke the appropriate tools and provide actionable insights.
