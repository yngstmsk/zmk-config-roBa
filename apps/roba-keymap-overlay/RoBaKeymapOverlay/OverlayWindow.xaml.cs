using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using RoBaKeymapOverlay.Models;
using RoBaKeymapOverlay.Services;

namespace RoBaKeymapOverlay;

public partial class OverlayWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly KeymapCanvasRenderer _renderer;
    private readonly TrayService _trayService;
    private AppSettings _settings;
    private bool _isLocked = true;
    private bool _isLoaded;

    public OverlayWindow()
    {
        InitializeComponent();

        _settings = _settingsStore.Load();
        _renderer = new KeymapCanvasRenderer(KeymapCanvas);

        _trayService = new TrayService();
        _trayService.EditModeRequested += (_, _) => EnterEditMode();
        _trayService.LockRequested += (_, _) => EnterLockedMode();
        _trayService.OpacityIncreaseRequested += (_, _) => AdjustOpacity(0.1);
        _trayService.OpacityDecreaseRequested += (_, _) => AdjustOpacity(-0.1);
        _trayService.ExitRequested += (_, _) => Close();

        Loaded += OnLoaded;
        Closing += OnClosing;
        SizeChanged += (_, _) =>
        {
            RenderKeymap();
            PersistWindowState();
        };
        LocationChanged += (_, _) => PersistWindowState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();

        var layout = LayoutLoader.LoadLayer0();
        _renderer.SetLayout(layout);
        RenderKeymap();

        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();

        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);

        Win32WindowHelper.RegisterGlobalHotkey(
            this,
            Win32WindowHelper.HotkeyToggleLock,
            Win32WindowHelper.ModControl | Win32WindowHelper.ModAlt,
            Win32WindowHelper.VkL,
            out _);

        if (_settings.IsLocked)
        {
            EnterLockedMode();
            _trayService.ShowLockedHint();
        }
        else
        {
            EnterEditMode();
        }

        _isLoaded = true;
        Dispatcher.BeginInvoke(RenderKeymap, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplySettings()
    {
        Left = _settings.Window.Left;
        Top = _settings.Window.Top;
        Width = _settings.Window.Width;
        Height = _settings.Window.Height;
        Opacity = ClampOpacity(_settings.Opacity);
        OpacitySlider.Value = Opacity * 100;
        OpacityLabel.Text = $"{OpacitySlider.Value:0}%";

        SettingsStore.ClampToWorkingArea(this);
    }

    private void RenderKeymap()
    {
        if (!_isLoaded && !IsLoaded)
        {
            return;
        }

        var editBarHeight = _isLocked ? 0 : EditBar.ActualHeight + TitleBar.ActualHeight;
        var availableHeight = Math.Max(0, KeymapCanvas.ActualHeight);
        if (availableHeight <= 0)
        {
            availableHeight = Math.Max(0, ActualHeight - editBarHeight - 8);
        }

        var availableWidth = Math.Max(0, KeymapCanvas.ActualWidth);
        if (availableWidth <= 0)
        {
            availableWidth = Math.Max(0, ActualWidth - 8);
        }

        _renderer.Render(new System.Windows.Size(availableWidth, availableHeight));
    }

    private void EnterEditMode()
    {
        _isLocked = false;
        TitleBar.Visibility = Visibility.Visible;
        EditBar.Visibility = Visibility.Visible;
        LockedHint.Visibility = Visibility.Collapsed;
        ShowInTaskbar = true;
        ResizeMode = ResizeMode.CanResize;
        DragArea.Cursor = System.Windows.Input.Cursors.SizeAll;
        _trayService.SetLockedState(true);
        Win32WindowHelper.ApplyOverlayStyles(this, clickThrough: false);
        RenderKeymap();
        PersistState();
    }

    private void EnterLockedMode()
    {
        _isLocked = true;
        TitleBar.Visibility = Visibility.Collapsed;
        EditBar.Visibility = Visibility.Collapsed;
        LockedHint.Visibility = Visibility.Visible;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        DragArea.Cursor = System.Windows.Input.Cursors.Arrow;
        _trayService.SetLockedState(false);
        Win32WindowHelper.ApplyOverlayStyles(this, clickThrough: true);
        RenderKeymap();
        PersistState();
    }

    private void AdjustOpacity(double delta)
    {
        Opacity = ClampOpacity(Opacity + delta);
        OpacitySlider.Value = Opacity * 100;
        PersistState();
    }

    private static double ClampOpacity(double value) => Math.Clamp(value, 0.0, 1.0);

    private void OpacitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded)
        {
            return;
        }

        Opacity = ClampOpacity(OpacitySlider.Value / 100.0);
        OpacityLabel.Text = $"{OpacitySlider.Value:0}%";
        PersistState();
    }

    private void LockButton_OnClick(object sender, RoutedEventArgs e) => EnterLockedMode();

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => StartDragMove();

    private void DragArea_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => StartDragMove();

    private void EditBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Slider or System.Windows.Controls.Button)
        {
            return;
        }

        StartDragMove();
    }

    private void StartDragMove()
    {
        if (_isLocked)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // Ignore drag errors during resize.
        }
    }

    private void PersistWindowState()
    {
        if (!_isLoaded)
        {
            return;
        }

        _settings.Window.Left = Left;
        _settings.Window.Top = Top;
        _settings.Window.Width = Width;
        _settings.Window.Height = Height;
        _settingsStore.SaveDebounced(_settings);
    }

    private void PersistState()
    {
        _settings.Opacity = Opacity;
        _settings.IsLocked = _isLocked;
        PersistWindowState();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32WindowHelper.WmHotkey && wParam.ToInt32() == Win32WindowHelper.HotkeyToggleLock)
        {
            if (_isLocked)
            {
                EnterEditMode();
            }
            else
            {
                EnterLockedMode();
            }

            handled = true;
            return IntPtr.Zero;
        }

        if (msg == Win32WindowHelper.WmNcHitTest && !_isLocked)
        {
            var screenX = (short)(lParam.ToInt32() & 0xFFFF);
            var screenY = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
            var hit = Win32WindowHelper.HitTestResize(this, screenX, screenY);

            if (hit != Win32WindowHelper.HtClient)
            {
                handled = true;
                return (IntPtr)hit;
            }

            // Title bar region: treat as caption for drag
            var point = PointFromScreen(new System.Windows.Point(screenX, screenY));
            if (point.Y <= TitleBar.ActualHeight + 2)
            {
                handled = true;
                return (IntPtr)Win32WindowHelper.HtCaption;
            }
        }

        return IntPtr.Zero;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        Win32WindowHelper.UnregisterGlobalHotkey(this, Win32WindowHelper.HotkeyToggleLock);
        _settings.Opacity = Opacity;
        _settings.IsLocked = _isLocked;
        _settings.Window.Left = Left;
        _settings.Window.Top = Top;
        _settings.Window.Width = Width;
        _settings.Window.Height = Height;
        _settingsStore.SaveImmediate(_settings);
        _trayService.Dispose();
    }
}
