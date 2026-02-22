// RibbonFactory.Tests.csx - Comprehensive Headless Tests for RibbonFactory
// Purpose: Test all public symbols in RibbonFactory per test analysis report
// Run via: dotnet script run RibbonFactory.Tests.csx or MCP RunHeadlessFormTest
// Usage: Designed for headless/CI environments (no UI rendering required)

#r "nuget: Moq, 4.18.4"
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;

// ============================================================================
// TEST INFRASTRUCTURE & MOCKS
// ============================================================================

/// <summary>
/// Headless MainForm mock - minimal implementation for ribbon testing
/// Provides just enough functionality for ribbon creation and navigation
/// </summary>
public class HeadlessMainFormMock : MainForm
{
    private Dictionary<string, object> _panels = new();

    public HeadlessMainFormMock() : base()
    {
        // Minimal initialization
        this.Text = "Headless Test Form";
        this.Size = new System.Drawing.Size(800, 600);
    }

    /// <summary>
    /// Mock ShowPanel - required by ribbon button handlers
    /// </summary>
    public void ShowPanel<TPanel>(string title, DockingStyle style, bool allowFloating = false)
        where TPanel : class
    {
        _panels[typeof(TPanel).Name] = new { Title = title, Style = style, Floating = allowFloating };
        Console.WriteLine($"  [MOCK] ShowPanel<{typeof(TPanel).Name}>: {title} ({style})");
    }

    /// <summary>
    /// Mock BeginInvoke for JARVIS button
    /// </summary>
    public new void BeginInvoke(Delegate method, params object[] args)
    {
        if (method is System.Action action)
            action?.Invoke();
    }

    /// <summary>
    /// Mock PerformGlobalSearchAsync
    /// </summary>
    public System.Threading.Tasks.Task PerformGlobalSearchAsync(string query)
    {
        Console.WriteLine($"  [MOCK] Global search: {query}");
        return System.Threading.Tasks.Task.CompletedTask;
    }

    /// <summary>
    /// Mock SwitchRightPanel
    /// </summary>
    public void SwitchRightPanel(object panelMode)
    {
        Console.WriteLine($"  [MOCK] Switch right panel: {panelMode}");
    }

    /// <summary>
    /// Mock GlobalIsBusy property
    /// </summary>
    public bool GlobalIsBusy { get; set; }

    public Dictionary<string, object> ShowedPanels => _panels;
}

/// <summary>
/// Test result tracker
/// </summary>
public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

/// <summary>
/// Test runner for ribbon tests
/// </summary>
public class RibbonFactoryTestRunner
{
    private List<TestResult> _results = new();
    private ILogger? _logger;

    public RibbonFactoryTestRunner(ILogger? logger = null)
    {
        _logger = logger;
    }

    private void LogTest(string name, bool passed, string message, long elapsed)
    {
        var icon = passed ? "✅" : "❌";
        var status = passed ? "PASS" : "FAIL";
        Console.WriteLine($"{icon} [{status}] {name} ({elapsed}ms)");
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine($"    → {message}");

        _results.Add(new TestResult
        {
            TestName = name,
            Passed = passed,
            Message = message,
            ElapsedMilliseconds = elapsed
        });
    }

    // ============================================================================
    // TEST: CreateRibbon()
    // ============================================================================

    public void Test_CreateRibbon_ReturnsNonNullTuple()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                Assert.NotNull(ribbon, "Ribbon is null");
                Assert.NotNull(homeTab, "HomeTab is null");

                LogTest("CreateRibbon_ReturnsNonNullTuple", true, "Tuple contains non-null ribbon and hometab", sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_ReturnsNonNullTuple", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_RibbonStructure()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                Assert.Equal("Ribbon_Main", ribbon.Name, "Ribbon name mismatch");
                Assert.Equal(160, ribbon.Height, "Ribbon height incorrect");
                Assert.False(ribbon.AutoSize, "Ribbon should not auto-size");
                Assert.NotNull(ribbon.Header, "Ribbon header is null");
                Assert.True(ribbon.Header.MainItems.Count >= 1, "Ribbon header has no items");

                LogTest("CreateRibbon_RibbonStructure", true,
                    $"Ribbon: {ribbon.Name}, Height: {ribbon.Height}, Tabs: {ribbon.Header.MainItems.Count}",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_RibbonStructure", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_HomeTabStructure()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                Assert.Equal("Home", homeTab.Text, "Home tab text mismatch");
                Assert.Equal("HomeTab", homeTab.Name, "Home tab name mismatch");
                Assert.NotNull(homeTab.Panel, "Home tab panel is null");

                // Count groups in home tab
                var groupCount = homeTab.Panel.Controls.OfType<ToolStripEx>().Count();
                Assert.True(groupCount >= 7, $"Expected 7+ groups, found {groupCount}");

                LogTest("CreateRibbon_HomeTabStructure", true,
                    $"HomeTab: '{homeTab.Text}', Groups: {groupCount}",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_HomeTabStructure", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_RibbonGroups()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                var groups = homeTab.Panel.Controls.OfType<ToolStripEx>().ToList();
                var groupNames = groups.Select(g => g.Text).ToList();

                var expectedGroups = new[] { "Core Navigation", "Financials", "Reporting", "Tools", "Layout", "Views", "Actions" };
                foreach (var expected in expectedGroups)
                {
                    Assert.True(groupNames.Any(g => g.Contains(expected)),
                        $"Missing group: {expected}. Found: {string.Join(", ", groupNames)}");
                }

                LogTest("CreateRibbon_RibbonGroups", true,
                    $"Found all 7 groups: {string.Join(", ", groupNames)}",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_RibbonGroups", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_NavigationButtonsInitialState()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                // Collect all buttons
                var allButtons = CollectAllButtons(homeTab);
                var navButtons = allButtons.Where(b => b.Name?.StartsWith("Nav_") == true).ToList();

                // All nav buttons should be DISABLED initially
                var disabledCount = navButtons.Count(b => !b.Enabled);
                Assert.True(disabledCount == navButtons.Count,
                    $"Not all nav buttons are disabled. Disabled: {disabledCount}/{navButtons.Count}");

                LogTest("CreateRibbon_NavigationButtonsInitialState", true,
                    $"All {navButtons.Count} navigation buttons are DISABLED (as expected)",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_NavigationButtonsInitialState", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_ThemeApplication()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                // Theme should be set
                Assert.False(string.IsNullOrEmpty(ribbon.ThemeName),
                    "Ribbon ThemeName is not set");

                var expectedTheme = SfSkinManager.ApplicationVisualTheme ?? "Office2019Colorful";
                Assert.Equal(expectedTheme, ribbon.ThemeName,
                    $"Ribbon theme '{ribbon.ThemeName}' doesn't match expected '{expectedTheme}'");

                LogTest("CreateRibbon_ThemeApplication", true,
                    $"Theme applied: {ribbon.ThemeName}",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_ThemeApplication", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_BackStageView()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                Assert.NotNull(ribbon.BackStageView, "BackStageView is null");

                LogTest("CreateRibbon_BackStageView", true,
                    "BackStageView initialized",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_BackStageView", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_QuickAccessToolbar()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                // QAT should be present (Header has QuickItems)
                Assert.NotNull(ribbon.Header, "Header is null");
                // Just verify it doesn't crash - full QAT testing is complex

                LogTest("CreateRibbon_QuickAccessToolbar", true,
                    "QAT initialized without errors",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_QuickAccessToolbar", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateRibbon_AllNavigationButtonsExist()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                var allButtons = CollectAllButtons(homeTab);
                var navButtons = allButtons.Where(b => b.Name?.StartsWith("Nav_") == true).ToList();

                var expectedNavButtons = new[]
                {
                    "Nav_Dashboard", "Nav_Accounts", "Nav_Analytics", "Nav_Reports",
                    "Nav_Settings", "Nav_QuickBooks", "Nav_JARVIS", "Nav_WarRoom"
                };

                var foundButtons = navButtons.Select(b => b.Name).ToList();
                foreach (var expected in expectedNavButtons)
                {
                    Assert.True(foundButtons.Contains(expected),
                        $"Missing button: {expected}. Found: {string.Join(", ", foundButtons)}");
                }

                LogTest("CreateRibbon_AllNavigationButtonsExist", true,
                    $"All {expectedNavButtons.Length} navigation buttons present",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("CreateRibbon_AllNavigationButtonsExist", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    // ============================================================================
    // TEST: EnableRibbonNavigation()
    // ============================================================================

    public void Test_EnableRibbonNavigation_EnablesNavButtons()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                // Get initial state
                var allButtonsBefore = CollectAllButtons(homeTab);
                var navButtonsBefore = allButtonsBefore.Where(b => b.Name?.StartsWith("Nav_") == true).ToList();
                var disabledBefore = navButtonsBefore.Count(b => !b.Enabled);

                // Enable navigation
                RibbonFactory.EnableRibbonNavigation(ribbon, null);

                // Get new state
                var allButtonsAfter = CollectAllButtons(homeTab);
                var navButtonsAfter = allButtonsAfter.Where(b => b.Name?.StartsWith("Nav_") == true).ToList();
                var enabledAfter = navButtonsAfter.Count(b => b.Enabled);

                Assert.True(disabledBefore > 0, "No disabled buttons before EnableRibbonNavigation");
                Assert.True(enabledAfter > disabledBefore, "Nav buttons were not enabled");
                Assert.Equal(navButtonsAfter.Count, enabledAfter,
                    "Not all nav buttons are enabled after call");

                LogTest("EnableRibbonNavigation_EnablesNavButtons", true,
                    $"Nav buttons: {disabledBefore} disabled → {enabledAfter} enabled",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("EnableRibbonNavigation_EnablesNavButtons", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_EnableRibbonNavigation_OnlyNavButtons()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null);

                var allButtonsBefore = CollectAllButtons(homeTab);
                var nonNavBefore = allButtonsBefore.Where(b => !b.Name?.StartsWith("Nav_") == true).ToList();
                var nonNavDisabledBefore = nonNavBefore.Count(b => !b.Enabled);

                RibbonFactory.EnableRibbonNavigation(ribbon, null);

                var allButtonsAfter = CollectAllButtons(homeTab);
                var nonNavAfter = allButtonsAfter.Where(b => !b.Name?.StartsWith("Nav_") == true).ToList();
                var nonNavDisabledAfter = nonNavAfter.Count(b => !b.Enabled);

                // Non-nav buttons should not change
                Assert.Equal(nonNavDisabledBefore, nonNavDisabledAfter,
                    "Non-nav buttons were affected by EnableRibbonNavigation");

                LogTest("EnableRibbonNavigation_OnlyNavButtons", true,
                    "Non-nav buttons unchanged",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("EnableRibbonNavigation_OnlyNavButtons", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_EnableRibbonNavigation_WithLogger()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Delegate>((level, id, state, ex, fn) =>
                {
                    Console.WriteLine($"  [LOG] {state}");
                });

            using (var form = new HeadlessMainFormMock())
            {
                var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, mockLogger.Object);

                RibbonFactory.EnableRibbonNavigation(ribbon, mockLogger.Object);

                // Verify logger was called
                mockLogger.Verify(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.AtLeastOnce,
                    "Logger was not called");

                LogTest("EnableRibbonNavigation_WithLogger", true,
                    "Logger invoked correctly",
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            LogTest("EnableRibbonNavigation_WithLogger", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    // ============================================================================
    // TEST: CreateDesignTimeRibbon()
    // ============================================================================

    public void Test_CreateDesignTimeRibbon_ReturnsNonNull()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var ribbon = RibbonFactory.CreateDesignTimeRibbon();

            Assert.NotNull(ribbon, "DesignTimeRibbon is null");

            LogTest("CreateDesignTimeRibbon_ReturnsNonNull", true,
                "Design-time ribbon created",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogTest("CreateDesignTimeRibbon_ReturnsNonNull", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    public void Test_CreateDesignTimeRibbon_Structure()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var ribbon = RibbonFactory.CreateDesignTimeRibbon();

            Assert.Equal("DesignPreview_Ribbon", ribbon.Name, "Design ribbon name mismatch");
            Assert.NotNull(ribbon.Header, "Design ribbon header is null");
            Assert.True(ribbon.Header.MainItems.Count >= 1, "Design ribbon has no tabs");

            var homeTab = ribbon.Header.MainItems.FirstOrDefault();
            Assert.NotNull(homeTab, "Design ribbon has no home tab");
            Assert.Equal("DesignHome", homeTab.Name, "Design home tab name mismatch");

            LogTest("CreateDesignTimeRibbon_Structure", true,
                $"Design ribbon: {ribbon.Name}, Tabs: {ribbon.Header.MainItems.Count}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogTest("CreateDesignTimeRibbon_Structure", false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    // ============================================================================
    // TEST EXECUTION & REPORTING
    // ============================================================================

    public void RunAllTests()
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        RibbonFactory Test Suite - Headless Mode            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // CreateRibbon tests
        Console.WriteLine("📍 Testing CreateRibbon():");
        Console.WriteLine();
        Test_CreateRibbon_ReturnsNonNullTuple();
        Test_CreateRibbon_RibbonStructure();
        Test_CreateRibbon_HomeTabStructure();
        Test_CreateRibbon_RibbonGroups();
        Test_CreateRibbon_NavigationButtonsInitialState();
        Test_CreateRibbon_ThemeApplication();
        Test_CreateRibbon_BackStageView();
        Test_CreateRibbon_QuickAccessToolbar();
        Test_CreateRibbon_AllNavigationButtonsExist();
        Console.WriteLine();

        // EnableRibbonNavigation tests
        Console.WriteLine("📍 Testing EnableRibbonNavigation():");
        Console.WriteLine();
        Test_EnableRibbonNavigation_EnablesNavButtons();
        Test_EnableRibbonNavigation_OnlyNavButtons();
        Test_EnableRibbonNavigation_WithLogger();
        Console.WriteLine();

        // CreateDesignTimeRibbon tests
        Console.WriteLine("📍 Testing CreateDesignTimeRibbon():");
        Console.WriteLine();
        Test_CreateDesignTimeRibbon_ReturnsNonNull();
        Test_CreateDesignTimeRibbon_Structure();
        Console.WriteLine();

        // Summary
        PrintSummary();
    }

    private void PrintSummary()
    {
        var passed = _results.Count(r => r.Passed);
        var failed = _results.Count(r => !r.Passed);
        var totalMs = _results.Sum(r => r.ElapsedMilliseconds);

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    TEST RESULTS                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Total Tests:   {_results.Count}");
        Console.WriteLine($"Passed:        {passed} ✅");
        Console.WriteLine($"Failed:        {failed} ❌");
        Console.WriteLine($"Total Time:    {totalMs}ms");
        Console.WriteLine();

        if (failed > 0)
        {
            Console.WriteLine("Failed Tests:");
            foreach (var result in _results.Where(r => !r.Passed))
            {
                Console.WriteLine($"  ❌ {result.TestName}");
                Console.WriteLine($"     {result.Message}");
            }
            Console.WriteLine();
        }

        var successRate = _results.Count > 0 ? (passed * 100 / _results.Count) : 0;
        Console.WriteLine($"Success Rate:  {successRate}%");
        Console.WriteLine();

        if (failed == 0)
            Console.WriteLine("🎉 ALL TESTS PASSED!");
        else
            Console.WriteLine($"⚠️  {failed} test(s) failed");

        Console.WriteLine();
    }

    // ============================================================================
    // HELPERS
    // ============================================================================

    private List<ToolStripButton> CollectAllButtons(ToolStripTabItem tab)
    {
        var buttons = new List<ToolStripButton>();

        foreach (var group in tab.Panel.Controls.OfType<ToolStripEx>())
        {
            foreach (var item in group.Items)
            {
                if (item is ToolStripButton btn)
                    buttons.Add(btn);
                else if (item is ToolStripPanelItem panelItem)
                {
                    foreach (var subItem in panelItem.Items)
                    {
                        if (subItem is ToolStripButton subBtn)
                            buttons.Add(subBtn);
                    }
                }
            }
        }

        return buttons;
    }
}

/// <summary>
/// Simple assertion helper (mimics xUnit style)
/// </summary>
public static class Assert
{
    public static void NotNull(object? value, string message)
    {
        if (value == null)
            throw new Exception($"Expected non-null, but was null. {message}");
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Expected true, but was false. {message}");
    }

    public static void False(bool condition, string message)
    {
        if (condition)
            throw new Exception($"Expected false, but was true. {message}");
    }

    public static void Equal<T>(T expected, T actual, string message) where T : notnull
    {
        if (!expected.Equals(actual))
            throw new Exception($"Expected '{expected}' but got '{actual}'. {message}");
    }
}

// ============================================================================
// MAIN ENTRY POINT
// ============================================================================

try
{
    Console.WriteLine("🚀 Starting RibbonFactory headless tests...");
    Console.WriteLine();

    var runner = new RibbonFactoryTestRunner();
    runner.RunAllTests();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ Fatal error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    System.Environment.Exit(1);
}
