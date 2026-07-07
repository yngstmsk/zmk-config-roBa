using System.Windows;
using RoBaKeymapOverlay.Models;

namespace RoBaKeymapOverlay.Services;

public static class LayoutScaler
{
    private const double MinScaleThreshold = 0.3;
    private const double MaxAspectRatioDeviation = 2.5;

    public static LayoutBounds ComputeBounds(KeymapLayout layout)
    {
        var visibleKeys = layout.Keys.Where(k => k.Visible).ToList();
        if (visibleKeys.Count == 0)
        {
            return new LayoutBounds();
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var key in visibleKeys)
        {
            var corners = GetKeyCorners(layout, key);
            foreach (var (x, y) in corners)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        const double padding = 8;
        return new LayoutBounds
        {
            MinX = minX - padding,
            MinY = minY - padding,
            MaxX = maxX + padding,
            MaxY = maxY + padding
        };
    }

    public static ScaleResult ComputeScale(
        KeymapLayout layout,
        LayoutBounds bounds,
        System.Windows.Size availableSize)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || availableSize.Width <= 0 || availableSize.Height <= 0)
        {
            return new ScaleResult { Scale = 1, OffsetX = 0, OffsetY = 0 };
        }

        var scaleX = availableSize.Width / bounds.Width;
        var scaleY = availableSize.Height / bounds.Height;
        var scale = Math.Min(scaleX, scaleY);

        var layoutAspect = bounds.Width / bounds.Height;
        var windowAspect = availableSize.Width / availableSize.Height;
        var aspectDeviation = Math.Max(layoutAspect, windowAspect) / Math.Min(layoutAspect, windowAspect);

        var allowClipping = scale < MinScaleThreshold || aspectDeviation > MaxAspectRatioDeviation;

        var scaledWidth = bounds.Width * scale;
        var scaledHeight = bounds.Height * scale;
        var offsetX = (availableSize.Width - scaledWidth) / 2 - bounds.MinX * scale;
        var offsetY = (availableSize.Height - scaledHeight) / 2 - bounds.MinY * scale;

        return new ScaleResult
        {
            Scale = scale,
            OffsetX = offsetX,
            OffsetY = offsetY,
            AllowClipping = allowClipping
        };
    }

    private static IEnumerable<(double X, double Y)> GetKeyCorners(KeymapLayout layout, KeyDefinition key)
    {
        var unitW = layout.UnitWidth;
        var unitH = layout.UnitHeight;
        var x = key.X * unitW;
        var y = key.Y * unitH;
        var w = key.W * unitW;
        var h = key.H * unitH;

        var corners = new[]
        {
            (x, y),
            (x + w, y),
            (x + w, y + h),
            (x, y + h)
        };

        if (key.R is null or 0)
        {
            return corners;
        }

        var angle = key.R.Value * Math.PI / 180.0;
        var pivotX = (key.Rx ?? key.X) * unitW;
        var pivotY = (key.Ry ?? key.Y) * unitH;

        return corners.Select(corner => Rotate(corner.Item1, corner.Item2, pivotX, pivotY, angle));
    }

    private static (double X, double Y) Rotate(double x, double y, double pivotX, double pivotY, double angleRad)
    {
        var dx = x - pivotX;
        var dy = y - pivotY;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);
        return (pivotX + dx * cos - dy * sin, pivotY + dx * sin + dy * cos);
    }
}
