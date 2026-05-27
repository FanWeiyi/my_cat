using System.IO;

namespace MyCat.WindowsShell;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        TryEnsureLocalDirectories();
        AppLogger.Log("Startup", ProductInfo.Version);

        var app = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose
        };

        app.DispatcherUnhandledException += (_, e) =>
        {
            AppLogger.LogException("DispatcherUnhandledException", e.Exception, crash: true);
            ShowCrashMessage();
            e.Handled = true;
            app.Shutdown(1);
        };
        app.Exit += (_, e) => AppLogger.Log("Exit", $"ExitCode={e.ApplicationExitCode}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                AppLogger.LogException("UnhandledException", exception, crash: true);
            }
            else
            {
                AppLogger.Log("UnhandledException", e.ExceptionObject?.ToString());
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLogger.LogException("UnobservedTaskException", e.Exception, crash: true);
            e.SetObserved();
        };

        try
        {
            app.Run(new DesktopCatWindow());
        }
        catch (Exception ex)
        {
            AppLogger.LogException("StartupFailed", ex, crash: true);
            ShowCrashMessage();
        }
    }

    private static void ShowCrashMessage()
    {
        System.Windows.MessageBox.Show(
            "My Cat 遇到了一点问题。如果日志目录可写，错误信息已经保存到本地日志目录。",
            ProductInfo.Name,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }

    private static void TryEnsureLocalDirectories()
    {
        try
        {
            AppPaths.EnsureDataDirectory();
            AppPaths.EnsureLogDirectory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Restricted folders should not prevent the companion from starting.
        }
    }
}
