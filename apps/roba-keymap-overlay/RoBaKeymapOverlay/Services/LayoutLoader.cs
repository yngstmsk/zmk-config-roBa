using System.IO;
using System.Reflection;
using System.Text.Json;
using RoBaKeymapOverlay.Models;

namespace RoBaKeymapOverlay.Services;

public static class LayoutLoader
{
    private const int MaxLayerIndex = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static KeymapLayout? _baseLayout;
    private static Dictionary<int, string[]>? _layerLabels;

    public static KeymapLayout LoadLayer(int layerIndex)
    {
        var clamped = Math.Clamp(layerIndex, 0, MaxLayerIndex);
        var layout = CloneLayout(GetBaseLayout());

        if (clamped == 0)
        {
            layout.Name = "layer0";
            return layout;
        }

        var labels = GetLayerLabels();
        if (!labels.TryGetValue(clamped, out var layerLabels))
        {
            layout.Name = $"layer{clamped}";
            return layout;
        }

        ApplyLabels(layout, layerLabels);
        layout.Name = $"layer{clamped}";
        return layout;
    }

    public static KeymapLayout LoadLayer0() => LoadLayer(0);

    private static KeymapLayout GetBaseLayout()
    {
        if (_baseLayout is not null)
        {
            return _baseLayout;
        }

        const string resourceName = "RoBaKeymapOverlay.layout.layer0.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _baseLayout = JsonSerializer.Deserialize<KeymapLayout>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize layer0.json");

        return _baseLayout;
    }

    private static Dictionary<int, string[]> GetLayerLabels()
    {
        if (_layerLabels is not null)
        {
            return _layerLabels;
        }

        const string resourceName = "RoBaKeymapOverlay.layout.layer-labels.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var document = JsonSerializer.Deserialize<LayerLabelsDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize layer-labels.json");

        _layerLabels = document.Layers.ToDictionary(
            pair => int.Parse(pair.Key),
            pair => pair.Value);

        return _layerLabels;
    }

    private static KeymapLayout CloneLayout(KeymapLayout source)
    {
        return new KeymapLayout
        {
            Name = source.Name,
            UnitWidth = source.UnitWidth,
            UnitHeight = source.UnitHeight,
            Keys = source.Keys.Select(key => new KeyDefinition
            {
                Label = key.Label,
                X = key.X,
                Y = key.Y,
                W = key.W,
                H = key.H,
                R = key.R,
                Rx = key.Rx,
                Ry = key.Ry,
                Visible = key.Visible
            }).ToList()
        };
    }

    private static void ApplyLabels(KeymapLayout layout, IReadOnlyList<string> labels)
    {
        for (var i = 0; i < layout.Keys.Count && i < labels.Count; i++)
        {
            var label = labels[i];
            layout.Keys[i].Label = label;
            layout.Keys[i].Visible = !string.IsNullOrWhiteSpace(label);
        }
    }

    private sealed class LayerLabelsDocument
    {
        public Dictionary<string, string[]> Layers { get; set; } = new();
    }
}
