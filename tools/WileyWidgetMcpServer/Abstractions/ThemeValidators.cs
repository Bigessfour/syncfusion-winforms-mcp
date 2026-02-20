using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WileyWidget.McpServer.Helpers;

namespace WileyWidget.McpServer.Abstractions;

/// <summary>
/// Syncfusion implementation that delegates to SyncfusionTestHelper
/// </summary>
public class SyncfusionThemeValidator : IThemeValidator
{
    public string FrameworkName => "Syncfusion";

    public bool CanHandle(Control control)
    {
        return control.GetType().Assembly.GetName().Name?.Contains("Syncfusion") == true;
    }

    public bool Validate(Control control, string expectedTheme)
    {
        var violations = GetViolations(control, expectedTheme);
        return !violations.Any();
    }

    public IEnumerable<ThemeViolation> GetViolations(Control control, string expectedTheme)
    {
        var violations = new List<ThemeViolation>();

        // Check if control is a Syncfusion control and validate its theme
        if (CanHandle(control))
        {
            var themeProperty = control.GetType().GetProperty("ThemeName");
            if (themeProperty != null)
            {
                var currentTheme = themeProperty.GetValue(control) as string;
                if (currentTheme != expectedTheme)
                {
                    violations.Add(new ThemeViolation
                    {
                        ViolationType = "ThemeMismatch",
                        ControlPath = GetControlPath(control),
                        ControlType = control.GetType().Name,
                        Description = $"Control has theme '{currentTheme}' but expected '{expectedTheme}'",
                        SuggestedFix = $"Set {control.GetType().Name}.ThemeName = \"{expectedTheme}\"",
                        Severity = "Error"
                    });
                }
            }
        }

        // Recursively check child controls
        foreach (Control child in control.Controls)
        {
            violations.AddRange(GetViolations(child, expectedTheme));
        }

        return violations;
    }

    private string GetControlPath(Control control)
    {
        var path = control.Name ?? control.GetType().Name;
        var parent = control.Parent;
        while (parent != null)
        {
            path = (parent.Name ?? parent.GetType().Name) + "." + path;
            parent = parent.Parent;
        }
        return path;
    }
}

public class VanillaWinFormsValidator : IThemeValidator
{
    public string FrameworkName => "WinForms";

    public bool CanHandle(Control control) => true; // Fallback

    public bool Validate(Control control, string expectedTheme) => true;

    public IEnumerable<ThemeViolation> GetViolations(Control control, string expectedTheme)
    {
        return Enumerable.Empty<ThemeViolation>();
    }
}

// Stubs for future expansion
public class DevExpressThemeValidator : VanillaWinFormsValidator { }
public class TelerikThemeValidator : VanillaWinFormsValidator { }
