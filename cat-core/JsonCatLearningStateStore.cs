using System.Text.Json;

namespace MyCat.CatCore;

public sealed class JsonCatLearningStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _path;

    public JsonCatLearningStateStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public CatLearningState Read()
    {
        if (!File.Exists(_path))
        {
            return CatLearningState.Empty;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<CatLearningState>(json, SerializerOptions)
                ?? CatLearningState.Empty;
        }
        catch (JsonException)
        {
            return CatLearningState.Empty;
        }
    }

    public void Write(CatLearningState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(state, SerializerOptions));
    }
}
