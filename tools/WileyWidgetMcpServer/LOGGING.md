# MCP Server Logging Configuration

## Overview

The WileyWidget MCP Server logs to **stderr** (not stdout) to avoid corrupting the JSON-RPC protocol stream. VS Code displays stderr output as warnings, but this is normal behavior.

## Log Levels

### Production (Default)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```
- **Effect**: Only warnings and errors appear in VS Code MCP logs
- **Use**: Normal operation, reduces noise

### Development
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```
- **Effect**: All info/warning/error messages appear
- **Use**: Debugging, troubleshooting

### Debug (Manual Override)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "ModelContextProtocol": "Debug"
    }
  }
}
```
- **Effect**: Verbose logging including debug messages
- **Use**: Deep troubleshooting

## How to Change Log Level

### Method 1: Edit appsettings.json
1. Open `tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer/appsettings.json`
2. Change `"Default": "Warning"` to desired level
3. Restart MCP server (Ctrl+Shift+P → "MCP: Restart Server")

### Method 2: Environment Variable
```powershell
$env:Logging__LogLevel__Default = "Information"
# Restart VS Code or MCP server
```

### Method 3: Use Development Config
Set environment to Development:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Restart VS Code
```

## Available Log Levels

| Level | Description |
|-------|-------------|
| `Trace` | Most verbose, includes all execution paths |
| `Debug` | Detailed debugging information |
| `Information` | General informational messages (default for Development) |
| `Warning` | Warnings and potential issues (default for Production) |
| `Error` | Errors and exceptions only |
| `Critical` | Critical failures only |
| `None` | No logging |

## Filtering Specific Categories

You can filter logging by namespace:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Hosting.Lifetime": "Error",
      "ModelContextProtocol.Server": "Information",
      "WileyWidget.McpServer": "Debug"
    }
  }
}
```

## Troubleshooting

### "Too many warnings in VS Code"
- **Cause**: Logs are routed to stderr (required for MCP protocol)
- **Solution**: Lower log level to `Warning` or `Error` in appsettings.json

### "Not seeing expected log messages"
- **Cause**: Log level too high
- **Solution**: Set to `Information` or `Debug` in appsettings.Development.json

### "Changes not taking effect"
- **Cause**: Config file not copied to output directory
- **Solution**: Rebuild the MCP server project

## Rebuild After Config Changes

```powershell
cd tests/syncfusion-winforms-mcp/tools/WileyWidgetMcpServer
dotnet build
# Or use the rebuild script
.\rebuild-mcp-server.ps1
```

## References

- [.NET Logging Overview](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [MCP Protocol Specification](https://spec.modelcontextprotocol.io/)
- [VS Code MCP Extension](https://marketplace.visualstudio.com/items?itemName=modelcontextprotocol.mcp)
