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
    /// <summary>
    /// Type of violation (e.g., "ManualBackColor", "IncompatibleTheme", "MissingTheme")
    /// </summary>
    public string ViolationType { get; set; }

    /// <summary>
    /// Full path to the violating control (e.g., "MainForm > dockingManager1 > panel1 > button1")
    /// </summary>
    public string ControlPath { get; set; }

    /// <summary>
    /// Control type (e.g., "Panel", "SfDataGrid", "Button")
    /// </summary>
    public string ControlType { get; set; }

    /// <summary>
    /// Detailed description of the violation.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Suggested fix (e.g., "Remove BackColor assignment; let theme cascade from parent")
    /// </summary>
    public string SuggestedFix { get; set; }

    /// <summary>
    /// Severity: "Error", "Warning", "Info"
    /// </summary>
    public string Severity { get; set; } = "Warning";
}

/// <summary>
/// Implementations:
/// - SyncfusionThemeValidator (existing behavior, now implements IThemeValidator)
/// - DevExpressThemeValidator (future implementation)
/// - TelerikThemeValidator (future implementation)
/// - VanillaWinFormsValidator (generic - checks for manual colors, TabIndex order, etc.)
/// </summary>
public class ThemeValidatorFactory
{
    /// <summary>
    /// Auto-detects which validator to use based on assemblies loaded in the form.
    /// Falls back to VanillaWinFormsValidator if no specific framework detected.
    /// </summary>
    public static IThemeValidator Create(Control control, string frameworkHint = null)
    {
        if (!string.IsNullOrEmpty(frameworkHint))
        {
            return frameworkHint.ToLower() switch
            {
                "syncfusion" => new SyncfusionThemeValidator(),
                "devexpress" => new DevExpressThemeValidator(),
                "telerik" => new TelerikThemeValidator(),
                _ => new VanillaWinFormsValidator()
            };
        }

        // Auto-detect
        var assembly = control.GetType().Assembly;
        if (assembly.GetName().Name.Contains("Syncfusion"))
            return new SyncfusionThemeValidator();

        if (assembly.GetName().Name.Contains("DevExpress"))
            return new DevExpressThemeValidator();

        if (assembly.GetName().Name.Contains("Telerik"))
            return new TelerikThemeValidator();

        // Default: vanilla WinForms
        return new VanillaWinFormsValidator();
    }
}
