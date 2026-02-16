// RibbonFactory.Tests.patched.csx - Patched for local dotnet-script (no #r NuGet, no Moq)
// Purpose: Test key RibbonFactory features in a headless script

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using WileyWidget.WinForms.Forms;
using WileyWidget.WinForms.Services;

public class HeadlessMainFormMock : MainForm
{
    private Dictionary<string, object> _panels = new();
    public HeadlessMainFormMock() : base() { this.Text = "Headless Test Form"; }
    public void ShowPanel<TPanel>(string title, DockingStyle style, bool allowFloating = false) where TPanel : class { _panels[typeof(TPanel).Name] = new { Title = title, Style = style, Floating = allowFloating }; Console.WriteLine($"  [MOCK] ShowPanel<{typeof(TPanel).Name}>: {title} ({style})"); }
    public new void BeginInvoke(Delegate method, params object[] args) { if (method is System.Action action) action?.Invoke(); }
    public System.Threading.Tasks.Task PerformGlobalSearchAsync(string query) { Console.WriteLine($"  [MOCK] Global search: {query}"); return System.Threading.Tasks.Task.CompletedTask; }
    public void SwitchRightPanel(object panelMode) { Console.WriteLine($"  [MOCK] Switch right panel: {panelMode}"); }
    public bool GlobalIsBusy { get; set; }
    public Dictionary<string, object> ShowedPanels => _panels;
}
public class TestResult { public string TestName { get; set; } = string.Empty; public bool Passed { get; set; } public string Message { get; set; } = string.Empty; public Exception? Exception { get; set; } public long ElapsedMilliseconds { get; set; } }
public class SimpleLogger : ILogger { private readonly List<string> _messages = new List<string>(); public IReadOnlyList<string> Messages => _messages; public IDisposable BeginScope<TState>(TState state) => new NullDisposable(); public bool IsEnabled(LogLevel level) => true; public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { var text = formatter != null ? formatter(state, exception) : (state?.ToString() ?? string.Empty); _messages.Add(text); Console.WriteLine($"  [LOG] {text}"); } }
public class NullDisposable : IDisposable { public void Dispose() {} }
public static class Assert { public static void NotNull(object? value, string message) { if (value == null) throw new Exception($"Expected non-null, but was null. {message}"); } public static void True(bool condition, string message) { if (!condition) throw new Exception($"Expected true, but was false. {message}"); } public static void False(bool condition, string message) { if (condition) throw new Exception($"Expected false, but was true. {message}"); } public static void Equal<T>(T expected, T actual, string message) where T : notnull { if (!expected.Equals(actual)) throw new Exception($"Expected '{expected}' but got '{actual}'. {message}"); } }

public class RibbonFactoryTestRunner
{
    private List<TestResult> _results = new();
    private void LogTest(string name, bool passed, string message, long elapsed) { var icon = passed ? "✅" : "❌"; var status = passed ? "PASS" : "FAIL"; Console.WriteLine($"{icon} [{status}] {name} ({elapsed}ms)"); if (!string.IsNullOrEmpty(message)) Console.WriteLine($"    → {message}"); _results.Add(new TestResult { TestName = name, Passed = passed, Message = message, ElapsedMilliseconds = elapsed }); }
    private List<ToolStripButton> CollectAllButtons(ToolStripTabItem tab) { var buttons = new List<ToolStripButton>(); foreach (var group in tab.Panel.Controls.OfType<ToolStripEx>()) { foreach (var item in group.Items) { if (item is ToolStripButton btn) buttons.Add(btn); else if (item is ToolStripPanelItem panelItem) { foreach (var subItem in panelItem.Items) { if (subItem is ToolStripButton subBtn) buttons.Add(subBtn); } } } } return buttons; }

    public void RunAllTests()
    {
        try
        {
            Test_CreateRibbon_ReturnsNonNullTuple();
            Test_CreateRibbon_HomeTabStructure();
            Test_CreateRibbon_AllNavigationButtonsExist();
            Test_EnableRibbonNavigation_EnablesNavButtons();
            Test_EnableRibbonNavigation_WithLogger();
            Test_CreateDesignTimeRibbon_ReturnsNonNull();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Runner failure: {ex.Message}");
        }
        PrintSummary();
    }

    public void Test_CreateRibbon_ReturnsNonNullTuple() { var sw = Stopwatch.StartNew(); try { using(var form = new HeadlessMainFormMock()) { var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null); Assert.NotNull(ribbon, "Ribbon is null"); Assert.NotNull(homeTab, "HomeTab is null"); LogTest("CreateRibbon_ReturnsNonNullTuple", true, "Tuple contains non-null ribbon and hometab", sw.ElapsedMilliseconds); } } catch(Exception ex) { LogTest("CreateRibbon_ReturnsNonNullTuple", false, ex.Message, sw.ElapsedMilliseconds); } }
    public void Test_CreateRibbon_HomeTabStructure() { var sw = Stopwatch.StartNew(); try { using(var form = new HeadlessMainFormMock()) { var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null); Assert.Equal("Home", homeTab.Text, "Home tab text mismatch"); Assert.Equal("HomeTab", homeTab.Name, "Home tab name mismatch"); Assert.NotNull(homeTab.Panel, "Home tab panel is null"); var groupCount = homeTab.Panel.Controls.OfType<ToolStripEx>().Count(); Assert.True(groupCount >= 7, $"Expected 7+ groups, found {groupCount}"); LogTest("CreateRibbon_HomeTabStructure", true, $"HomeTab: '{homeTab.Text}', Groups: {groupCount}", sw.ElapsedMilliseconds); } } catch(Exception ex) { LogTest("CreateRibbon_HomeTabStructure", false, ex.Message, sw.ElapsedMilliseconds); } }
    public void Test_CreateRibbon_AllNavigationButtonsExist() { var sw = Stopwatch.StartNew(); try { using(var form = new HeadlessMainFormMock()) { var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null); var allButtons = CollectAllButtons(homeTab); var navButtons = allButtons.Where(b => b.Name?.StartsWith("Nav_") == true).ToList(); var expectedNavButtons = new[] { "Nav_Dashboard", "Nav_Accounts", "Nav_Analytics", "Nav_Reports", "Nav_Settings", "Nav_QuickBooks", "Nav_JARVIS", "Nav_WarRoom" }; var foundButtons = navButtons.Select(b => b.Name).ToList(); foreach (var expected in expectedNavButtons) { Assert.True(foundButtons.Contains(expected), $"Missing button: {expected}. Found: {string.Join(", ", foundButtons)}"); } LogTest("CreateRibbon_AllNavigationButtonsExist", true, $"All {expectedNavButtons.Length} navigation buttons present", sw.ElapsedMilliseconds); } } catch(Exception ex) { LogTest("CreateRibbon_AllNavigationButtonsExist", false, ex.Message, sw.ElapsedMilliseconds); } }
    public void Test_EnableRibbonNavigation_EnablesNavButtons() { var sw = Stopwatch.StartNew(); try { using(var form = new HeadlessMainFormMock()) { var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, null); var allButtonsBefore = CollectAllButtons(homeTab); var navButtonsBefore = allButtonsBefore.Where(b => b.Name?.StartsWith("Nav_") == true).ToList(); var disabledBefore = navButtonsBefore.Count(b => !b.Enabled); RibbonFactory.EnableRibbonNavigation(ribbon, null); var allButtonsAfter = CollectAllButtons(homeTab); var navButtonsAfter = allButtonsAfter.Where(b => b.Name?.StartsWith("Nav_") == true).ToList(); var enabledAfter = navButtonsAfter.Count(b => b.Enabled); Assert.True(disabledBefore > 0, "No disabled buttons before EnableRibbonNavigation"); Assert.True(enabledAfter > disabledBefore, "Nav buttons were not enabled"); Assert.Equal(navButtonsAfter.Count, enabledAfter, "Not all nav buttons are enabled after call"); LogTest("EnableRibbonNavigation_EnablesNavButtons", true, $"Nav buttons: {disabledBefore} disabled → {enabledAfter} enabled", sw.ElapsedMilliseconds); } } catch(Exception ex) { LogTest("EnableRibbonNavigation_EnablesNavButtons", false, ex.Message, sw.ElapsedMilliseconds); } }
    public void Test_EnableRibbonNavigation_WithLogger() { var sw = Stopwatch.StartNew(); try { var testLogger = new SimpleLogger(); using(var form = new HeadlessMainFormMock()) { var (ribbon, homeTab) = RibbonFactory.CreateRibbon(form, testLogger); RibbonFactory.EnableRibbonNavigation(ribbon, testLogger); if (!testLogger.Messages.Any()) throw new Exception("Logger was not called"); LogTest("EnableRibbonNavigation_WithLogger", true, "Logger invoked correctly", sw.ElapsedMilliseconds); } } catch(Exception ex) { LogTest("EnableRibbonNavigation_WithLogger", false, ex.Message, sw.ElapsedMilliseconds); } }
    public void Test_CreateDesignTimeRibbon_ReturnsNonNull() { var sw = Stopwatch.StartNew(); try { var ribbon = RibbonFactory.CreateDesignTimeRibbon(); Assert.NotNull(ribbon, "DesignTimeRibbon is null"); LogTest("CreateDesignTimeRibbon_ReturnsNonNull", true, "Design-time ribbon created", sw.ElapsedMilliseconds); } catch(Exception ex) { LogTest("CreateDesignTimeRibbon_ReturnsNonNull", false, ex.Message, sw.ElapsedMilliseconds); } }
    private void PrintSummary() { var passed = _results.Count(r => r.Passed); var failed = _results.Count(r => !r.Passed); var totalMs = _results.Sum(r => r.ElapsedMilliseconds); Console.WriteLine(); Console.WriteLine($"Total Tests:   {_results.Count}"); Console.WriteLine($"Passed:        {passed} ✅"); Console.WriteLine($"Failed:        {failed} ❌"); Console.WriteLine($"Total Time:    {totalMs}ms"); Console.WriteLine(); if (failed > 0) { Console.WriteLine("Failed Tests:"); foreach (var result in _results.Where(r => !r.Passed)) { Console.WriteLine($"  ❌ {result.TestName}"); Console.WriteLine($"     {result.Message}"); } Console.WriteLine(); } var successRate = _results.Count > 0 ? (passed * 100 / _results.Count) : 0; Console.WriteLine($"Success Rate:  {successRate}%"); if (failed == 0) Console.WriteLine("🎉 ALL TESTS PASSED!"); else Console.WriteLine($"⚠️  {failed} test(s) failed"); Console.WriteLine(); }
}

// Capture output and write to workspace file
var sb = new System.Text.StringBuilder();
using (var swWriter = new System.IO.StringWriter(sb))
{
    var originalOut = Console.Out;
    var originalErr = Console.Error;
    Console.SetOut(swWriter);
    Console.SetError(swWriter);
    try
    {
        var runner = new RibbonFactoryTestRunner();
        runner.RunAllTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Fatal error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
    finally
    {
        Console.Out.Flush();
        Console.SetOut(originalOut);
        Console.SetError(originalErr);
    }
}

try
{
    System.IO.File.WriteAllText("tests/RibbonFactory.Tests.patched.run.log", sb.ToString(), Encoding.UTF8);
    Console.WriteLine("WROTE_LOG: tests/RibbonFactory.Tests.patched.run.log");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to write log file: {ex.Message}");
}
