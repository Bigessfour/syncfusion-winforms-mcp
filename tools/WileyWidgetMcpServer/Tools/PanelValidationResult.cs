using System;

namespace WileyWidget.McpServer.Tools;

/// <summary>
/// Result of a single panel validation. Extends concepts from FormValidationResult
/// with panel-specific checks (MVVM, validation, control compliance).
/// </summary>
public class PanelValidationResult
{
    /// <summary>Fully qualified type name (e.g., WileyWidget.WinForms.Controls.SettingsPanel)</summary>
    public required string PanelTypeName { get; set; }

    /// <summary>Simple class name (e.g., SettingsPanel)</summary>
    public required string PanelName { get; set; }

    /// <summary>Expected theme name for validation</summary>
    public required string ExpectedTheme { get; set; }

    /// <summary>Overall pass/fail status</summary>
    public bool Passed { get; set; }

    /// <summary>Theme compliance check result</summary>
    public bool ThemeValid { get; set; }

    /// <summary>Syncfusion control usage and configuration compliance</summary>
    public bool ControlCompliance { get; set; }

    /// <summary>MVVM binding and ViewModel initialization check</summary>
    public bool MvvmValid { get; set; }

    /// <summary>Validation setup (ErrorProvider, binding) check</summary>
    public bool ValidationSetupValid { get; set; }

    /// <summary>ICompletablePanel.IsValid property (if panel implements ICompletablePanel)</summary>
    public bool IsValid { get; set; }

    /// <summary>Count of manual color violations (BackColor/ForeColor assignments)</summary>
    public int ViolationCount { get; set; }

    /// <summary>List of manual color violation details</summary>
    public string[] ManualColorViolations { get; set; } = Array.Empty<string>();

    /// <summary>Static source-level color violations (file:line:snippet)</summary>
    public string[] StaticColorViolations { get; set; } = Array.Empty<string>();

    /// <summary>Data binding issues (if any)</summary>
    public string[] DataBindingIssues { get; set; } = Array.Empty<string>();

    /// <summary>Validation errors from ICompletablePanel.ValidationErrors (if available)</summary>
    public string[] ValidationErrors { get; set; } = Array.Empty<string>();

    /// <summary>Any instantiation or loading errors</summary>
    public string? Error { get; set; }

    /// <summary>Time the validation was performed</summary>
    public DateTime ValidationTime { get; set; }

    /// <summary>Duration in milliseconds for this panel's validation</summary>
    public long DurationMs { get; set; }

    /// <summary>Time spent instantiating the panel (ms)</summary>
    public long InstantiationTimeMs { get; set; }

    /// <summary>Time spent loading theme (ms)</summary>
    public long ThemeLoadTimeMs { get; set; }

    /// <summary>Time spent in validation checks (ms)</summary>
    public long ValidationTimeMs { get; set; }
}
