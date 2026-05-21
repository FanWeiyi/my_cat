using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace MyCat.WindowsShell;

internal sealed class TrayIconHost : IDisposable
{
    private readonly Forms.ContextMenuStrip _menu = new();
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconHost(Action exit)
    {
        _menu.Items.Add("Exit", null, (_, _) => exit());
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

