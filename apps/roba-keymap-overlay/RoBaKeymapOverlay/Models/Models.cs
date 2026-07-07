namespace RoBaKeymapOverlay.Models;

public sealed class KeyDefinition
{
    public string Label { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 1;
    public double H { get; set; } = 1;
    public double? R { get; set; }
    public double? Rx { get; set; }
    public double? Ry { get; set; }
    public bool Visible { get; set; } = true;
}

public sealed class KeymapLayout
{
    public string Name { get; set; } = "layer0";
    public double UnitWidth { get; set; } = 52;
    public double UnitHeight { get; set; } = 52;
    public List<KeyDefinition> Keys { get; set; } = new();
}

public sealed class WindowSettings
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 320;
}

public sealed class AppSettings
{
    public WindowSettings Window { get; set; } = new();
    public double Opacity { get; set; } = 0.85;
    public bool IsLocked { get; set; } = false;
}

public sealed class LayoutBounds
{
    public double MinX { get; init; }
    public double MinY { get; init; }
    public double MaxX { get; init; }
    public double MaxY { get; init; }
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
}

public sealed class ScaleResult
{
    public double Scale { get; init; }
    public double OffsetX { get; init; }
    public double OffsetY { get; init; }
    public bool AllowClipping { get; init; }
}
