using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using RoBaKeymapOverlay.Models;

namespace RoBaKeymapOverlay.Services;

public sealed class KeymapCanvasRenderer
{
    private readonly Canvas _canvas;
    private KeymapLayout? _layout;
    private LayoutBounds? _bounds;

    public KeymapCanvasRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void SetLayout(KeymapLayout layout)
    {
        _layout = layout;
        _bounds = LayoutScaler.ComputeBounds(layout);
    }

    public void Render(System.Windows.Size availableSize)
    {
        _canvas.Children.Clear();

        if (_layout is null || _bounds is null)
        {
            return;
        }

        var scaleResult = LayoutScaler.ComputeScale(_layout, _bounds, availableSize);

        var background = new Border
        {
            Width = availableSize.Width,
            Height = availableSize.Height,
            Background = new SolidColorBrush(Color.FromArgb(48, 20, 24, 32)),
            CornerRadius = new CornerRadius(6),
            IsHitTestVisible = false
        };
        _canvas.Children.Add(background);

        var keysCanvas = new Canvas
        {
            Width = availableSize.Width,
            Height = availableSize.Height,
            ClipToBounds = scaleResult.AllowClipping,
            IsHitTestVisible = false
        };

        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(new ScaleTransform(scaleResult.Scale, scaleResult.Scale));
        transformGroup.Children.Add(new TranslateTransform(scaleResult.OffsetX, scaleResult.OffsetY));
        keysCanvas.RenderTransform = transformGroup;

        foreach (var key in _layout.Keys.Where(k => k.Visible))
        {
            keysCanvas.Children.Add(CreateKeyElement(_layout, key));
        }

        _canvas.Children.Add(keysCanvas);
        _canvas.ClipToBounds = scaleResult.AllowClipping;
    }

    private static UIElement CreateKeyElement(KeymapLayout layout, KeyDefinition key)
    {
        var unitW = layout.UnitWidth;
        var unitH = layout.UnitHeight;
        var x = key.X * unitW;
        var y = key.Y * unitH;
        var w = key.W * unitW - 4;
        var h = key.H * unitH - 4;

        var border = new Border
        {
            Width = w,
            Height = h,
            Background = new SolidColorBrush(Color.FromArgb(200, 45, 52, 64)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(220, 120, 130, 150)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = key.Label,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = Math.Max(8, Math.Min(w, h) * 0.28),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };

        if (key.R is not null and not 0)
        {
            var pivotX = (key.Rx ?? key.X) * unitW - x;
            var pivotY = (key.Ry ?? key.Y) * unitH - y;
            border.RenderTransform = new RotateTransform(key.R.Value, pivotX, pivotY);
        }

        Canvas.SetLeft(border, x + 2);
        Canvas.SetTop(border, y + 2);

        return border;
    }
}
