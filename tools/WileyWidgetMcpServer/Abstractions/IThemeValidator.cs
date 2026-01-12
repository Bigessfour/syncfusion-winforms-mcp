using System.Windows.Forms;
using System.Collections.Generic;
using System;

namespace WileyWidget.McpServer.Abstractions;

/// <summary>
/// Framework-agnostic abstraction for theme/style validation.
/// Allows ValidateFormTheme to work with Syncfusion, DevExpress, Telerik, vanilla WinForms, etc.
/// </summary>
public interface IThemeValidator
{
    /// <summary>
    /// Validates a form/control against expected theme or styling rules.
    /// </summary>
    /// <param name="control">Root control to validate</param>
    /// <param name="expectedTheme">Expected theme name (e.g., "Office2019Colorful", null for vanilla WinForms)</param>
    /// <returns>True if compliant, false if violations found</returns>
    bool Validate(Control control, string expectedTheme);

    /// <summary>
    /// Returns all violations found in the control tree.
    /// </summary>
    /// <param name="control">Root control to scan</param>
    /// <param name="expectedTheme">Expected theme (optional)</param>
    /// <returns>List of violation descriptions with control paths</returns>
    IEnumerable<ThemeViolation> GetViolations(Control control, string expectedTheme);

    /// <summary>
    /// Gets the name of the framework this validator handles.
    /// </summary>
    string FrameworkName { get; }

    /// <summary>
    /// Detects which framework is in use for this control tree (if applicable).
    /// </summary>
    bool CanHandle(Control control);
}

/// <summary>
/// Represents a single theme/style violation.
/// </summary>
public class ThemeViolation
{
    public string? ViolationType { get; set; }
    public string? ControlPath { get; set; }
    public string? ControlType { get; set; }
    public string? Description { get; set; }
    public string? SuggestedFix { get; set; }
    public string Severity { get; set; } = "Warning";
}

/// <summary>
/// Factory for creating theme validators based on framework detection.
/// Validator implementations are in ThemeValidators.cs
/// </summary>
public class ThemeValidatorFactory
    {
        /// <summary>
        /// Auto-detects which validator to use based on assemblies loaded in the form.
        /// Falls back to VanillaWinFormsValidator if no specific framework detected.
        /// </summary>
        public static IThemeValidator Create(Control control, string? frameworkHint = null)
        {
            if (!string.IsNullOrEmpty(frameworkHint))
            {
                return frameworkHint.ToLower(System.Globalization.CultureInfo.InvariantCulture) switch
                {
                    "syncfusion" => new SyncfusionThemeValidator(),
                    "devexpress" => new DevExpressThemeValidator(),
                    "telerik" => new TelerikThemeValidator(),
                    _ => new VanillaWinFormsValidator()
                };
            }

            // Auto-detect
            var assembly = control.GetType().Assembly;
            var name = assembly.GetName().Name;

            if (name != null)
            {
                if (name.Contains("Syncfusion", StringComparison.OrdinalIgnoreCase))
                    return new SyncfusionThemeValidator();

                if (name.Contains("DevExpress", StringComparison.OrdinalIgnoreCase))
                    return new DevExpressThemeValidator();

                if (name.Contains("Telerik", StringComparison.OrdinalIgnoreCase))
                    return new TelerikThemeValidator();
            }

            // Default: vanilla WinForms
            return new VanillaWinFormsValidator();
        }
    }
