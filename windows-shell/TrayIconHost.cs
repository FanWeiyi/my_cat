using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace MyCat.WindowsShell;

internal sealed class TrayIconHost : IDisposable
{
    private readonly Forms.ContextMenuStrip _menu = new();
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconHost(Action<CatCore.CatEventType> record, Action exit)
    {
        var tellMenu = new Forms.ToolStripMenuItem("告诉它");
        tellMenu.DropDownItems.Add("我家猫在睡觉", null, (_, _) => record(CatCore.CatEventType.Rest));
        tellMenu.DropDownItems.Add("我家猫在玩", null, (_, _) => record(CatCore.CatEventType.Activity));
        tellMenu.DropDownItems.Add("我家猫在陪我", null, (_, _) => record(CatCore.CatEventType.Accompany));

        _menu.Items.Add(tellMenu);
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
}
