using System;
using System.Windows.Forms;
using System.Reflection;
using WileyWidget.McpServer.Helpers;
using WileyWidget.McpServer.Tools;

namespace PanelValidationTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("PANEL BATCH VALIDATION - LIVE TEST RUN");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            // Discover panels first
            Console.WriteLine("📍 Discovering panels...");
            var panels = PanelTypeCache.GetAllPanelTypes();
            Console.WriteLine($"✓ Found {panels.Count} panels in WileyWidget.WinForms.Controls");

            if (panels.Count > 0)
            {
                Console.WriteLine("\n📋 Panels discovered:");
                foreach (var panel in panels.Take(5))
                {
                    Console.WriteLine($"  - {panel.Name}");
                }
                if (panels.Count > 5)
                {
                    Console.WriteLine($"  ... and {panels.Count - 5} more");
                }
            }

            Console.WriteLine();
            Console.WriteLine("🚀 Running batch validation (text format)...");
            Console.WriteLine();

            // Run validation
            var result = BatchValidatePanelsTool.BatchValidatePanels(
                panelTypeNames: null,  // All panels
                expectedTheme: "Office2019Colorful",
                failFast: false,
                outputFormat: "text"
            );

            Console.WriteLine(result);

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("✅ Validation complete!");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during validation: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
