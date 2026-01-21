using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Thread-safe cache for discovering and retrieving panel (UserControl) types from the WileyWidget.WinForms.Controls namespace.
/// Mirrors FormTypeCache pattern for consistency across validation tools.
/// </summary>
public static class PanelTypeCache
{
    private static readonly Dictionary<string, Type?> _typeCache = new();
    private static List<Type>? _allPanelTypes;
    private static readonly ReaderWriterLockSlim _cacheLock = new();

    /// <summary>
    /// Get a specific panel type by fully qualified name. Cached for performance.
    /// </summary>
    public static Type? GetPanelType(string panelTypeName)
    {
        ArgumentNullException.ThrowIfNull(panelTypeName);

        _cacheLock.EnterReadLock();
        try
        {
            if (_typeCache.ContainsKey(panelTypeName))
            {
                return _typeCache[panelTypeName];
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        var type = Type.GetType(panelTypeName);

        _cacheLock.EnterWriteLock();
        try
        {
            _typeCache[panelTypeName] = type;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        return type;
    }

    /// <summary>
    /// Discover all UserControl panel types in the WileyWidget.WinForms.Controls namespace.
    /// Filters abstract, test, and mock types. Cached for performance (2-3x faster on repeat calls).
    /// </summary>
    public static List<Type> GetAllPanelTypes()
    {
        _cacheLock.EnterReadLock();
        try
        {
            if (_allPanelTypes != null)
            {
                return new List<Type>(_allPanelTypes);
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        // Get assembly containing UserControls (via a known control)
        var assembly = typeof(UserControl).Assembly;

        // Search for entry assembly to get WileyWidget types
        try
        {
            assembly = System.Reflection.Assembly.Load("WileyWidget.WinForms");
        }
        catch
        {
            // Fallback to entry assembly
            assembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(UserControl).Assembly;
        }

        var panels = assembly.GetTypes()
            .Where(t => typeof(UserControl).IsAssignableFrom(t) &&
                        !t.IsAbstract &&
                        t.Namespace?.StartsWith("WileyWidget.WinForms.Controls", StringComparison.Ordinal) == true &&
                        !t.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) &&
                        !t.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase) &&
                        t != typeof(UserControl))
            .OrderBy(t => t.Name)
            .ToList();

        _cacheLock.EnterWriteLock();
        try
        {
            _allPanelTypes = new List<Type>(panels);
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        return new List<Type>(panels);
    }

    /// <summary>
    /// Clear the type cache. Useful for testing or after assembly updates.
    /// </summary>
    public static void ClearCache()
    {
        _cacheLock.EnterWriteLock();
        try
        {
            _typeCache.Clear();
            _allPanelTypes = null;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }
}
