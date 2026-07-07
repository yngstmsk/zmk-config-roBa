using System.IO;
using System.Reflection;
using System.Text.Json;
using RoBaKeymapOverlay.Models;

namespace RoBaKeymapOverlay.Services;

public static class LayoutLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static KeymapLayout LoadLayer0()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "RoBaKeymapOverlay.layout.layer0.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var layout = JsonSerializer.Deserialize<KeymapLayout>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize layer0.json");

        return layout;
    }
}
