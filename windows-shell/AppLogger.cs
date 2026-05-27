using System.IO;

namespace MyCat.WindowsShell;

internal static class AppLogger
{
    private static readonly object Sync = new();

    public static void Log(string eventName, string? detail = null)
    {
        WriteLine("app", Format(eventName, detail));
    }

    public static void LogException(string eventName, Exception exception, bool crash = false)
    {
        var detail = string.Join(
            Environment.NewLine,
            exception.GetType().FullName,
            exception.Message,
            exception.StackTrace);

        WriteLine(crash ? "crash" : "app", Format(eventName, detail));
    }

    private static string Format(string eventName, string? detail)
    {
        var message = $"[{DateTimeOffset.Now:O}] {eventName}";
        return string.IsNullOrWhiteSpace(detail)
            ? message
            : $"{message}{Environment.NewLine}{detail}";
    }

    private static void WriteLine(string prefix, string message)
    {
        try
        {
            AppPaths.EnsureLogDirectory();
            var path = Path.Combine(AppPaths.LogDirectory, $"{prefix}-{DateTimeOffset.Now:yyyyMMdd}.log");
            lock (Sync)
            {
                File.AppendAllText(path, message + Environment.NewLine + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never become the reason the desktop companion exits.
        }
    }
}
