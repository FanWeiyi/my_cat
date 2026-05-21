using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyCat.CatCore;

public sealed class JsonCatEventStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _path;

    public JsonCatEventStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public IReadOnlyList<CatObservationEvent> ReadAll()
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<CatObservationEvent>();
        }

        var json = File.ReadAllText(_path);
        var events = JsonSerializer.Deserialize<List<CatObservationEvent>>(json, SerializerOptions);
        return events is null
            ? Array.Empty<CatObservationEvent>()
            : events;
    }

    public void Append(CatObservationEvent catEvent)
    {
        ArgumentNullException.ThrowIfNull(catEvent);

        var events = ReadAll().ToList();
        events.Add(catEvent);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(events, SerializerOptions));
    }
}
