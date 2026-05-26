using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace MyCat.WindowsShell;

internal sealed class TrayIconHost : IDisposable
{
    private readonly Forms.ContextMenuStrip _menu = new();
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _quietModeItem;
    private bool _syncingQuietMode;

    public TrayIconHost(Action<CatCore.CatEventType> record, Action<bool> setQuietMode, Action openBehaviorSettings, Action exit)
    {
        var tellMenu = new Forms.ToolStripMenuItem("告诉它");
        tellMenu.DropDownItems.Add("我家猫在睡觉", null, (_, _) => record(CatCore.CatEventType.Rest));
        tellMenu.DropDownItems.Add("我家猫在玩", null, (_, _) => record(CatCore.CatEventType.Activity));
        tellMenu.DropDownItems.Add("我家猫在陪我", null, (_, _) => record(CatCore.CatEventType.Accompany));

        _menu.Items.Add(tellMenu);
        _quietModeItem = new Forms.ToolStripMenuItem("安静模式")
        {
            CheckOnClick = true
        };
        _quietModeItem.CheckedChanged += (_, _) =>
        {
            if (!_syncingQuietMode)
            {
                setQuietMode(_quietModeItem.Checked);
            }
        };
        _menu.Items.Add(_quietModeItem);
        _menu.Items.Add("行为节奏设置", null, (_, _) => openBehaviorSettings());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("退出", null, (_, _) => exit());
        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = Drawing.SystemIcons.Application,
            Text = "My Cat",
            Visible = true
        };
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    public void SetQuietMode(bool enabled)
    {
        _syncingQuietMode = true;
        _quietModeItem.Checked = enabled;
        _syncingQuietMode = false;
    }
}
