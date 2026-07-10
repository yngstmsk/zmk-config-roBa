using System.Drawing;
using System.Windows.Forms;

namespace RoBaKeymapOverlay.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _editModeItem;
    private readonly ToolStripMenuItem _opacityUpItem;
    private readonly ToolStripMenuItem _opacityDownItem;
    private readonly ToolStripMenuItem _syncStatusItem;
    private readonly ToolStripMenuItem _exitItem;

    public event EventHandler? EditModeRequested;
    public event EventHandler? LockRequested;
    public event EventHandler? OpacityIncreaseRequested;
    public event EventHandler? OpacityDecreaseRequested;
    public event EventHandler? ExitRequested;

    public TrayService()
    {
        _editModeItem = new ToolStripMenuItem("編集モード", null, (_, _) => EditModeRequested?.Invoke(this, EventArgs.Empty));
        var lockItem = new ToolStripMenuItem("ロック", null, (_, _) => LockRequested?.Invoke(this, EventArgs.Empty));
        _opacityUpItem = new ToolStripMenuItem("透明度 +10%", null, (_, _) => OpacityIncreaseRequested?.Invoke(this, EventArgs.Empty));
        _opacityDownItem = new ToolStripMenuItem("透明度 -10%", null, (_, _) => OpacityDecreaseRequested?.Invoke(this, EventArgs.Empty));
        _syncStatusItem = new ToolStripMenuItem("レイヤー同期: 起動中…") { Enabled = false };
        _exitItem = new ToolStripMenuItem("終了", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_editModeItem);
        _menu.Items.Add(lockItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_opacityUpItem);
        _menu.Items.Add(_opacityDownItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_syncStatusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "roBa Keymap Overlay",
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => EditModeRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowLockedHint()
    {
        _notifyIcon.ShowBalloonTip(
            5000,
            "roBa Keymap Overlay",
            "ロック中です。編集するにはトレイを右クリック→「編集モード」、または Ctrl+Alt+L を押してください。",
            ToolTipIcon.Info);
    }

    public void SetLockedState(bool isLocked)
    {
        _editModeItem.Enabled = isLocked;
    }

    public void SetLayerText(string layerText)
    {
        _notifyIcon.Text = $"roBa Overlay — {layerText}";
    }

    public void SetSyncStatus(string status)
    {
        _syncStatusItem.Text = status;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
