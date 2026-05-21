namespace MyCat.WindowsShell;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose
        };

        app.Run(new DesktopCatWindow());
    }
}
