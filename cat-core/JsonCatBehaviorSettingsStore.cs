using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyCat.CatCore;

public sealed class JsonCatBehaviorSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _path;

    public JsonCatBehaviorSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public CatBehaviorSettings Read()
    {
        if (!File.Exists(_path))
        {
            return CatBehaviorSettings.Empty;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<CatBehaviorSettings>(json, SerializerOptions)
                ?? CatBehaviorSettings.Empty;
            return Normalize(settings);
        }
        catch (JsonException)
        {
            return CatBehaviorSettings.Empty;
        }
    }

    public void Write(CatBehaviorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(Normalize(settings), SerializerOptions));
    }

    private static CatBehaviorSettings Normalize(CatBehaviorSettings settings)
    {
        var normalized = settings.ManualWeights.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Normalized());
        return settings with
        {
            Version = CatBehaviorSettings.CurrentVersion,
            ManualWeights = normalized
        };
    }
}
