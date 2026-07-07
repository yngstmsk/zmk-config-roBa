using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RoBaKeymapOverlay.Services;

public static class Win32WindowHelper
{
    private const int GwlExStyle = -20;
    private const int WsExLayered = 0x00080000;
    private const int WsExTransparent = 0x00000020;
    private const int WsExTopmost = 0x00000008;
    private const int WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static void ApplyOverlayStyles(Window window, bool clickThrough)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLong(hwnd, GwlExStyle);
        exStyle |= WsExLayered | WsExTopmost | WsExToolWindow;

        if (clickThrough)
        {
            exStyle |= WsExTransparent;
        }
        else
        {
            exStyle &= ~WsExTransparent;
        }

        SetWindowLong(hwnd, GwlExStyle, exStyle);
    }

    public static bool RegisterGlobalHotkey(Window window, int id, uint modifiers, uint key, out string? error)
    {
        var helper = new WindowInteropHelper(window);
        if (!RegisterHotKey(helper.Handle, id, modifiers, key))
        {
            error = $"RegisterHotKey failed (id={id}).";
            return false;
        }

        error = null;
        return true;
    }

    public static void UnregisterGlobalHotkey(Window window, int id)
    {
        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            UnregisterHotKey(helper.Handle, id);
        }
    }

    private static int GetWindowLong(IntPtr hwnd, int index)
    {
        if (IntPtr.Size == 8)
        {
            return (int)GetWindowLongPtr64(hwnd, index);
        }

        return GetWindowLong32(hwnd, index);
    }

    private static void SetWindowLong(IntPtr hwnd, int index, int value)
    {
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(hwnd, index, new IntPtr(value));
            return;
        }

        SetWindowLong32(hwnd, index, value);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const int WmHotkey = 0x0312;
    public const int WmNcHitTest = 0x0084;
    public const int HotkeyToggleLock = 9001;
    public const uint VkL = 0x4C;

    public const int HtClient = 1;
    public const int HtCaption = 2;
    public const int HtLeft = 10;
    public const int HtRight = 11;
    public const int HtTop = 12;
    public const int HtTopLeft = 13;
    public const int HtTopRight = 14;
    public const int HtBottom = 15;
    public const int HtBottomLeft = 16;
    public const int HtBottomRight = 17;

    public static int HitTestResize(Window window, int screenX, int screenY, int borderThickness = 10)
    {
        var windowPoint = window.PointFromScreen(new System.Windows.Point(screenX, screenY));
        var relativeX = windowPoint.X;
        var relativeY = windowPoint.Y;
        var width = window.ActualWidth;
        var height = window.ActualHeight;

        var onLeft = relativeX >= 0 && relativeX < borderThickness;
        var onRight = relativeX <= width && relativeX > width - borderThickness;
        var onTop = relativeY >= 0 && relativeY < borderThickness;
        var onBottom = relativeY <= height && relativeY > height - borderThickness;

        if (onTop && onLeft)
        {
            return HtTopLeft;
        }

        if (onTop && onRight)
        {
            return HtTopRight;
        }

        if (onBottom && onLeft)
        {
            return HtBottomLeft;
        }

        if (onBottom && onRight)
        {
            return HtBottomRight;
        }

        if (onLeft)
        {
            return HtLeft;
        }

        if (onRight)
        {
            return HtRight;
        }

        if (onTop)
        {
            return HtTop;
        }

        if (onBottom)
        {
            return HtBottom;
        }

        return HtClient;
    }
}
