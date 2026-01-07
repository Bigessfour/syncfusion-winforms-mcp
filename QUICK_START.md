# Quick Start – WileyWidget MCP Server

Get started validating your WinForms forms in **2 minutes**.

---

## 1. Install & Setup

### Prerequisites

- .NET 10.0+ installed
- Visual Studio 2022 or VS Code (with C# extension)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/wiley-widget-mcp
cd wiley-widget-mcp

# Restore packages
dotnet restore

# Build
dotnet build WileyWidget.sln
```

---

## 2. Run Your First Validation

### Option A: Validate a Single Form for Theme Compliance

```bash
# Test that MainForm uses SfSkinManager correctly
dotnet run --project tools/WileyWidgetMcpServer -- \
  --validate-form MainForm \
  --json-output
```

**Expected Output:**

```json
{
  "formName": "MainForm",
  "passed": true,
  "issues": [],
  "themeName": "Office2019Colorful",
  "validationTime": "45.23ms"
}
```

### Option B: Run an Inline Test

Create a test file `test.csx`:

```csharp
var form = new MainForm();

// Test that form loads
Console.WriteLine($"✓ Form created: {form.Text}");

// Test that grid exists
var grid = form.Controls.OfType<SfDataGrid>().FirstOrDefault();
if (grid == null) throw new Exception("Grid not found!");

Console.WriteLine($"✓ Grid found with {grid.Columns.Count} columns");

true  // Return true = test passed
```

Run it:

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --run-test-file test.csx
```

### Option C: Batch Validate All Forms

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --validate-all-forms src/WileyWidget.WinForms/Forms/ \
  --json-output
```

---

## 3. Integration Points

### GitHub Actions (CI/CD)

Add this to `.github/workflows/validate-forms.yml`:

```yaml
name: Validate Forms

on: [push, pull_request]

jobs:
  validate:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: dotnet/setup-dotnet@v3
        with:
          dotnet-version: "10.0.x"
      - run: dotnet build
      - run: |
          dotnet run --project tools/WileyWidgetMcpServer -- \
            --validate-all-forms src/ \
            --json-output
```

### Copilot Integration

Ask GitHub Copilot:

```
@copilot Validate MainForm for theme compliance using @wiley-widget
```

Copilot will automatically invoke the validation tools.

---

## 4. Common Scenarios

### Scenario 1: Check if a Form Has Correct Theme

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --validate-form WileyWidget.WinForms.Forms.AccountsForm
```

### Scenario 2: Inspect Grid Columns

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --inspect-grid WileyWidget.WinForms.Forms.AccountsForm
```

### Scenario 3: Find Manual Color Assignments

Create `find-colors.csx`:

```csharp
var form = new AccountsForm();

var violations = new List<string>();

// Check form itself
foreach (var property in form.GetType().GetProperties())
{
    if ((property.Name == "BackColor" || property.Name == "ForeColor") && property.CanRead)
    {
        var value = property.GetValue(form);
        violations.Add($"Form.{property.Name} = {value}");
    }
}

// Check all child controls
foreach (var control in GetAllControls(form))
{
    if (control.BackColor != SystemColors.Control)
        violations.Add($"{control.Name}.BackColor manually set");
    if (control.ForeColor != SystemColors.ControlText)
        violations.Add($"{control.Name}.ForeColor manually set");
}

if (violations.Count > 0)
{
    Console.WriteLine($"❌ Found {violations.Count} color violations:");
    foreach (var v in violations) Console.WriteLine($"  - {v}");
    false
}
else
{
    Console.WriteLine("✅ No manual color assignments found");
    true
}

IEnumerable<Control> GetAllControls(Control parent)
{
    foreach (Control control in parent.Controls)
    {
        yield return control;
        foreach (var child in GetAllControls(control))
            yield return child;
    }
}
```

Run it:

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --run-test-file find-colors.csx
```

---

## 5. Troubleshooting

### Problem: "Form not found"

**Solution:** Ensure your form is fully qualified:

```bash
# Wrong
--validate-form AccountsForm

# Right
--validate-form WileyWidget.WinForms.Forms.AccountsForm
```

### Problem: "Assembly not found"

**Solution:** Build the solution first:

```bash
dotnet build WileyWidget.sln
```

### Problem: "Test timed out"

**Solution:** Increase timeout (default 30s):

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --run-test-file test.csx \
  --timeout 60
```

### Problem: "I need more debugging info"

**Solution:** Enable console capture:

```bash
dotnet run --project tools/WileyWidgetMcpServer -- \
  --run-test-file test.csx \
  --capture-console
```

---

## 6. Next Steps

- **Read** [TOOL_USAGE.md](docs/TOOL_USAGE.md) for detailed documentation
- **Explore** [EXAMPLES.md](docs/EXAMPLES.md) for real-world patterns
- **Reference** [API_REFERENCE.md](docs/API_REFERENCE.md) for all parameters
- **Contribute** – See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines

---

## 7. Support

- **Questions?** Open a [Discussion](https://github.com/yourusername/wiley-widget-mcp/discussions)
- **Found a bug?** [Report an Issue](https://github.com/yourusername/wiley-widget-mcp/issues)
- **Want to contribute?** See [CONTRIBUTING.md](CONTRIBUTING.md)

---

**Happy validating! 🚀**
