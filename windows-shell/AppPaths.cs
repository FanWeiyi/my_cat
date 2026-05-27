using System.IO;

namespace MyCat.WindowsShell;

internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyCat");

    public static string LogDirectory { get; } = Path.Combine(DataDirectory, "logs");

    public static string EventStorePath => Path.Combine(DataDirectory, "events.json");

    public static string LearningStateStorePath => Path.Combine(DataDirectory, "learning-state.json");

    public static string MetricsStorePath => Path.Combine(DataDirectory, "interaction-metrics.json");

    public static string BehaviorSettingsStorePath => Path.Combine(DataDirectory, "behavior-settings.json");

    public static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    public static void EnsureLogDirectory()
    {
        Directory.CreateDirectory(LogDirectory);
    }
}
