using System.Windows;

namespace MyCat.WindowsShell;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        app.Run(new DesktopCatWindow());
    }
}

