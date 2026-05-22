using System.Text.Json;

namespace MyCat.CatCore;

public sealed class JsonCatInteractionMetricsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _path;

    public JsonCatInteractionMetricsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public CatInteractionMetrics Read()
    {
        if (!File.Exists(_path))
        {
            return CatInteractionMetrics.Empty;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<CatInteractionMetrics>(json, SerializerOptions)
                ?? CatInteractionMetrics.Empty;
        }
        catch (JsonException)
        {
            return CatInteractionMetrics.Empty;
        }
    }

    public void Write(CatInteractionMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(metrics, SerializerOptions));
    }
}
