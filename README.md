# Syncfusion WinForms MCP Server

![.NET](https://img.shields.io/badge/.NET-10.0-blue) ![License](https://img.shields.io/badge/license-MIT-green) ![MCP](https://img.shields.io/badge/MCP-1.0-orange) ![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

Headless MCP server for Syncfusion WinForms UI validation & testing. Enforces theme compliance, detects manual colors, inspects grids/docking, batch-validates forms, exports hierarchies, and runs stateful C# eval—no rendering needed. Perfect for GitHub Copilot agents & CI/CD pipelines.

## ✨ Features

- **🎨 Theme Validation:** Enforce Office2019Colorful/Office2016 themes across all forms
- **🔍 Manual Color Detection:** Find hardcoded colors that bypass SkinManager
- **📊 Grid Inspection:** Analyze SfDataGrid columns, bindings, and data
- **🏗️ Control Hierarchy Export:** Generate JSON/text trees of form layouts
- **⚡ Headless Testing:** Instantiate forms without UI rendering
- **🧪 C# Eval:** Execute dynamic C# code with full WinForms context
- **📦 Batch Validation:** Validate dozens of forms in one operation
- **🔧 DI Testing:** Verify dependency injection configuration

## 🚀 Quick Start

### Prerequisites

- .NET 10.0 SDK
- Windows (WinForms requirement)
- Syncfusion WinForms NuGet packages

### Installation

1. Clone the repository:

```bash
git clone https://github.com/Bigessfour/syncfusion-winforms-mcp.git
cd syncfusion-winforms-mcp
```

2. Build the server:

```bash
dotnet build tools/WileyWidgetMcpServer/WileyWidgetMcpServer.csproj
```

3. Configure your MCP client (GitHub Copilot, Claude Desktop, etc.):

```json
{
  "mcpServers": {
    "syncfusion-winforms": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/path/to/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/WileyWidgetMcpServer.csproj"
      ]
    }
  }
}
```

## 📖 Documentation

- [Quick Start Guide](tools/WileyWidgetMcpServer/QUICK_START.md) - Get up and running in 5 minutes
- [API Reference](tools/WileyWidgetMcpServer/QUICK_REFERENCE.md) - Complete tool documentation
- [Examples](EXAMPLES.md) - Real-world usage scenarios
- [Architecture](ARCHITECTURE-MULTI-FRAMEWORK.md) - Multi-framework support design
- [Contributing](CONTRIBUTING.md) - How to contribute

## 🛠️ Tools Available

| Tool | Purpose |
|------|---------|
| `ValidateFormThemeTool` | Check if form uses specified Syncfusion theme |
| `BatchValidateFormsTool` | Validate theme compliance across multiple forms |
| `DetectManualColorsTool` | Find hardcoded Color.FromArgb() calls |
| `InspectSfDataGridTool` | Analyze SfDataGrid configuration |
| `InspectDockingManagerTool` | Examine DockingManager layout |
| `ExportControlHierarchyTool` | Generate control tree as JSON/text |
| `FindControlsByPropertyTool` | Search controls by property values |
| `RunHeadlessFormTestTool` | Execute form tests without rendering |
| `EvalCSharpTool` | Run dynamic C# code with form context |
| `RunDependencyInjectionTestsTool` | Validate DI container setup |
| `DetectNullRisksTool` | Find potential NullReferenceException risks |
| `ValidateSyncfusionLicenseTool` | Verify Syncfusion license configuration |

## 💡 Use Cases

### With GitHub Copilot

```
@workspace Use the syncfusion-winforms MCP server to validate all forms 
in the Forms/ directory use Office2019Colorful theme. Show me any violations.
```

### CI/CD Pipeline

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --tool BatchValidateFormsTool \
  --theme Office2019Colorful \
  --output json
```

### Theme Migration

```
@workspace Use DetectManualColorsTool to find all hardcoded colors in my forms. 
I'm migrating to SkinManager-based theming.
```

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- **Stephen McKitrick** - Creator & Maintainer
- **AI Collaborators** - Claude Haiku 4.5, Grok 4.1
- **Open Source Community** - Syncfusion, Microsoft, .NET Foundation, GitHub, MCP, VS Code

## 📬 Contact

- **GitHub Issues:** For bugs and feature requests
- **GitHub Discussions:** For questions and ideas
- **Email:** bigessfour@gmail.com (sensitive matters only)
