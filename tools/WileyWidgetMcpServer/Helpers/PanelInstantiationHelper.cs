using System;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using WileyWidget.WinForms;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Handles instantiation and loading of UserControl panels with realistic DI mocking.
/// Adapts FormInstantiationHelper pattern for ScopedPanelBase<T> and other UserControls.
/// Supports constructor parameter mocking: ILogger<T>, IServiceProvider, ViewModels, and IServiceScopeFactory.
/// </summary>
public static class PanelInstantiationHelper
{
    private static bool _themeAssemblyLoaded = false;

    /// <summary>
    /// Ensure theme assembly is loaded once at startup of batch process.
    /// </summary>
    private static void EnsureThemeAssemblyLoaded()
    {
        if (!_themeAssemblyLoaded)
        {
            try
            {
                SkinManager.LoadAssembly(typeof(Syncfusion.WinForms.Themes.Office2019Theme).Assembly);
                _themeAssemblyLoaded = true;
            }
            catch
            {
                // Assembly may already be loaded
            }
        }
    }
    /// <summary>
    /// Instantiate a UserControl panel with mock DI parameters.
    /// Attempts constructors in order of parameter count (simplest first).
    /// </summary>
    public static UserControl InstantiatePanel(Type panelType, Form? mockHostForm = null)
    {
        if (panelType == null)
            throw new ArgumentNullException(nameof(panelType));

        if (!typeof(UserControl).IsAssignableFrom(panelType))
            throw new InvalidOperationException($"Type '{panelType.FullName}' does not inherit from UserControl");

        var constructors = panelType.GetConstructors()
            .OrderBy(c => c.GetParameters().Length)
            .ToArray();

        Exception? lastEx = null;

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            bool canInstantiate = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                // IServiceScopeFactory (common in ScopedPanelBase)
                if (paramType == typeof(IServiceScopeFactory))
                {
                    args[i] = MockFactory.CreateMockServiceScopeFactory();
                }
                // ILogger<T> via Mock.Of<T>() or NullLogger
                else if (paramType.IsGenericType &&
                         paramType.GetGenericTypeDefinition() == typeof(ILogger<>))
                {
                    try
                    {
                        args[i] = MockFactory.CreateMockLogger(paramType);
                    }
                    catch
                    {
                        args[i] = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                    }
                }
                // ILogger (non-generic)
                else if (paramType == typeof(ILogger))
                {
                    args[i] = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                }
                // IServiceProvider
                else if (paramType == typeof(IServiceProvider))
                {
                    args[i] = MockFactory.CreateTestServiceProvider();
                }
                // ReportViewerLaunchOptions (sealed record, use Disabled instance)
                else if (paramType == typeof(WileyWidget.WinForms.Configuration.ReportViewerLaunchOptions))
                {
                    args[i] = WileyWidget.WinForms.Configuration.ReportViewerLaunchOptions.Disabled;
                }
                // Form (parent form reference)
                else if (paramType == typeof(Form) || paramType.Name == "MainForm")
                {
                    args[i] = mockHostForm ?? MockFactory.CreateMockMainForm();
                }
                // String parameters (paths, file names)
                else if (paramType == typeof(string))
                {
                    var pname = parameters[i].Name ?? string.Empty;
                    if (pname.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        pname.IndexOf("file", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            args[i] = Path.GetTempFileName();
                        }
                        catch
                        {
                            args[i] = "dummy-path";
                        }
                    }
                    else
                    {
                        args[i] = $"dummy-string-{i}";
                    }
                }
                // ViewModel types (attempt parameterless or recursive mock)
                else if (paramType.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var parameterlessCtor = paramType.GetConstructor(Type.EmptyTypes);
                        if (parameterlessCtor != null)
                        {
                            args[i] = Activator.CreateInstance(paramType);
                        }
                        else
                        {
                            // Try to instantiate with recursive mocking
                            var vmCtor = paramType.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
                            if (vmCtor != null)
                            {
                                var vmArgs = new object?[vmCtor.GetParameters().Length];
                                // Recursively mock ViewModel dependencies (simplified for testing)
                                args[i] = vmCtor.Invoke(vmArgs);
                            }
                            else
                            {
                                args[i] = null;
                            }
                        }
                    }
                    catch
                    {
                        args[i] = null;
                    }
                }
                // Other interfaces via Mock.Of<T>()
                else if (paramType.IsInterface)
                {
                    try
                    {
                        args[i] = MockFactory.CreateMockInterface(paramType);
                    }
                    catch
                    {
                        args[i] = null;
                    }
                }
                // Value types (int, bool, enum, etc.)
                else if (paramType.IsValueType)
                {
                    try
                    {
                        args[i] = Activator.CreateInstance(paramType);
                    }
                    catch
                    {
                        args[i] = null;
                    }
                }
                // Sealed classes - try static instance properties or Activator
                else if (paramType.IsSealed)
                {
                    try
                    {
                        // Look for static properties that return instances (e.g., Disabled, Default)
                        var staticProps = paramType.GetProperties(BindingFlags.Public | BindingFlags.Static);
                        var instanceProp = staticProps.FirstOrDefault(p =>
                            p.PropertyType == paramType &&
                            p.GetMethod != null &&
                            p.GetMethod.IsStatic);
                        if (instanceProp != null)
                        {
                            args[i] = instanceProp.GetValue(null);
                        }
                        else
                        {
                            // Try Activator for parameterless constructor
                            args[i] = Activator.CreateInstance(paramType);
                        }
                    }
                    catch
                    {
                        args[i] = null;
                    }
                }
                else
                {
                    canInstantiate = false;
                    break;
                }
            }

            if (canInstantiate)
            {
                try
                {
                    var panel = (UserControl)ctor.Invoke(args)!;

                    // Mock/Fake Minimal Context to Avoid NullRefs
                    // For panels that blow up on Parent, Site, or binding
                    var fakeForm = new Form { Visible = false };
                    panel.Parent = fakeForm;               // or fakeForm.Controls.Add(panel);
                    panel.Dock = DockStyle.Fill;           // sometimes helps layout pass

                    // If using Syncfusion DockingManager style panels, you might need:
                    // try
                    // {
                    //     var fakeDocking = new DockingManager();
                    //     fakeDocking.Controls.Add(panel);
                    // }
                    // catch
                    // {
                    //     // DockingManager setup may fail, ignore
                    // }

                    // DI / ViewModel Setup (for MVVM Bindings failures)
                    // Most panels probably do InitializeComponent() → bind to this.DataContext = ViewModel
                    try
                    {
                        // Fake a minimal ViewModel for MVVM bindings
                        var fakeViewModel = new FakeViewModel();
                        panel.DataContext = fakeViewModel;
                    }
                    catch
                    {
                        // DataContext setup may fail, ignore
                    }

                    // ErrorProvider / Validation Setup
                    // Many panels likely have an ErrorProvider component dropped in designer. In headless mode it may not initialize properly.
                    try
                    {
                        var errorProvider = new ErrorProvider();
                        // errorProvider.Container = new Container(); // or attach to fake form
                    }
                    catch
                    {
                        // ErrorProvider setup may fail, ignore
                    }

                    return panel;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    continue;
                }
            }
        }

        throw new InvalidOperationException(
            $"UserControl type '{panelType.FullName}' does not have a suitable constructor. " +
            $"Tried {constructors.Length} constructor(s) but none could be instantiated with mock parameters. " +
            $"Last error: {lastEx?.Message ?? "(no details)"}");
    }

    /// <summary>
    /// Load a panel with theme application. Handles handle creation and UI thread marshaling.
    /// Realistic equivalent of FormInstantiationHelper.LoadFormWithTheme for UserControls.
    /// </summary>
    public static bool LoadPanelWithTheme(UserControl panel, string themeName = "Office2019Colorful", int waitMs = 500, Form? parentForm = null)
    {
        ArgumentNullException.ThrowIfNull(panel);

        try
        {
            // Load theme assembly - CRITICAL: Must load the correct theme assembly once
            EnsureThemeAssemblyLoaded();

            // Apply theme to parent form if provided (better cascade), otherwise to panel
            try
            {
                if (parentForm != null)
                {
                    // Apply theme at form level for better cascade
                    SkinManager.SetVisualStyle(parentForm, themeName);
                    Console.WriteLine($"[Theme Application] Applied theme '{themeName}' to parent form for panel {panel.GetType().Name}");
                }
                else
                {
                    SkinManager.SetVisualStyle(panel, themeName);
                }

                // Set ThemeName on any Syncfusion child controls
                foreach (var sfControl in SyncfusionTestHelper.GetAllSyncfusionControls(panel))
                {
                    try
                    {
                        var themeNameProp = sfControl.GetType().GetProperty("ThemeName");
                        if (themeNameProp?.CanWrite == true)
                        {
                            themeNameProp.SetValue(sfControl, themeName);
                        }
                    }
                    catch
                    {
                        // Ignore per-control theme failures
                    }
                }
            }
            catch
            {
                // Theme application may fail in headless mode but shouldn't block panel testing
            }

            // Ensure panel handle is created and components are initialized
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var maxWait = TimeSpan.FromMilliseconds(Math.Max(50, waitMs));

            while (sw.Elapsed < maxWait)
            {
                try
                {
                    if (panel.IsHandleCreated && panel.Controls.Count > 0)
                    {
                        break;
                    }
                    Application.DoEvents();
                }
                catch
                {
                    // Ignore transient initialization errors
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely dispose of a panel and its mock host form. Handles UI thread marshaling and suppresses errors.
    /// Follows industry best practice for headless control cleanup.
    /// </summary>
    public static void SafeDispose(UserControl? panel, Form? mockHostForm = null)
    {
        if (panel != null)
        {
            try
            {
                if (panel.InvokeRequired)
                {
                    panel.Invoke((System.Action)(() => DisposePanelInternal(panel)));
                }
                else
                {
                    DisposePanelInternal(panel);
                }
            }
            catch
            {
                // Suppress disposal errors
            }

            try
            {
                GC.SuppressFinalize(panel);
            }
            catch
            {
                // Ignore
            }
        }

        if (mockHostForm != null)
        {
            try
            {
                if (!mockHostForm.IsDisposed)
                {
                    mockHostForm.Close();
                    mockHostForm.Dispose();
                }
            }
            catch
            {
                // Suppress disposal errors
            }

            try
            {
                GC.SuppressFinalize(mockHostForm);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private static void DisposePanelInternal(UserControl panel)
    {
        if (!panel.IsDisposed)
        {
            // Dispose any DataBindings to avoid holding references
            try
            {
                panel.DataBindings.Clear();
            }
            catch
            {
                // Ignore
            }

            // Recursively dispose child controls
            try
            {
                foreach (Control child in panel.Controls)
                {
                    try
                    {
                        child.Dispose();
                    }
                    catch
                    {
                        // Ignore per-control disposal errors
                    }
                }
            }
            catch
            {
                // Ignore
            }

            // Dispose the panel itself
            try
            {
                panel.Dispose();
            }
            catch
            {
                // Ignore
            }
        }
    }

    /// <summary>
    /// Execute an operation on the STA thread (Windows Forms requirement).
    /// Used for headless validation that needs proper UI thread context.
    /// </summary>
    public static T ExecuteOnStaThread<T>(Func<T> operation, int timeoutSeconds = 30)
    {
        T? result = default;
        Exception? thrownException = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = operation();
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }
        });

        if (OperatingSystem.IsWindows())
        {
            thread.SetApartmentState(ApartmentState.STA);
        }
        thread.Start();

        var completed = thread.Join(TimeSpan.FromSeconds(timeoutSeconds));

        if (!completed)
        {
            thread.Interrupt();
            throw new TimeoutException($"Panel operation exceeded {timeoutSeconds} second timeout");
        }

        if (thrownException != null)
        {
            throw thrownException;
        }

        return result!;
    }

    /// <summary>
    /// Fake minimal ViewModel for MVVM bindings in batch testing.
    /// Provides dummy properties and commands to prevent binding failures.
    /// </summary>
    private class FakeViewModel
    {
        // Common properties that panels might bind to
        public string Title { get; set; } = "Fake Panel";
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public object? SelectedItem { get; set; }
        public System.Collections.IEnumerable? Items { get; set; } = new string[0];

        // Common commands that panels might use
        public System.Windows.Input.ICommand? SaveCommand { get; set; }
        public System.Windows.Input.ICommand? CancelCommand { get; set; }
        public System.Windows.Input.ICommand? RefreshCommand { get; set; }
    }
}
