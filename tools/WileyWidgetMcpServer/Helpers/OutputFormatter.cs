using System.Text.Json;
using System.Text.Json.Serialization;

namespace WileyWidget.McpServer.Helpers;

/// <summary>
/// Shared output formatting utilities for consistent text and JSON responses across all tools.
/// Eliminates duplicated formatting logic in individual tools.
/// </summary>
public static class OutputFormatter
{
    /// <summary>
    /// JSON serializer options configured for consistent formatting across all tools.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Formats a successful response with optional JSON serialization.
    /// </summary>
    public static string FormatSuccess(string message, bool asJson = false)
    {
        if (asJson)
        {
            var response = new { success = true, message };
            return JsonSerializer.Serialize(response, JsonOptions);
        }

        return $"✅ {message}";
    }

    /// <summary>
    /// Formats an error response with optional JSON serialization.
    /// </summary>
    public static string FormatError(string errorMessage, bool asJson = false)
    {
        if (asJson)
        {
            var error = new { success = false, error = errorMessage };
            return JsonSerializer.Serialize(error, JsonOptions);
        }

        return $"❌ Error: {errorMessage}";
    }

    /// <summary>
    /// Formats a warning response with optional JSON serialization.
    /// </summary>
    public static string FormatWarning(string warningMessage, bool asJson = false)
    {
        if (asJson)
        {
            var warning = new { success = true, warning = warningMessage };
            return JsonSerializer.Serialize(warning, JsonOptions);
        }

        return $"⚠️ {warningMessage}";
    }

    /// <summary>
    /// Formats an object as JSON with consistent options.
    /// </summary>
    public static string ToJson<T>(T obj) => JsonSerializer.Serialize(obj, JsonOptions);

    /// <summary>
    /// Formats an object as JSON with a custom options override.
    /// </summary>
    public static string ToJson<T>(T obj, JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(obj, options ?? JsonOptions);

    /// <summary>
    /// Helper to determine if output should be JSON based on format string.
    /// </summary>
    public static bool ShouldFormatAsJson(string outputFormat) =>
        outputFormat?.ToLowerInvariant() == "json";

    /// <summary>
    /// Gets the appropriate serializer options for the requested format.
    /// </summary>
    public static JsonSerializerOptions GetSerializerOptions(string outputFormat = "text") =>
        ShouldFormatAsJson(outputFormat) ? JsonOptions : new JsonSerializerOptions();
}
