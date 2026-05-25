using System.Text.Json;

namespace MyCat.CatAssets;

public sealed class CatArtPackManifest
{
    public string Id { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public int CanvasWidth { get; init; }

    public int CanvasHeight { get; init; }

    public int FrameCount { get; init; }

    public IReadOnlyList<CatArtPackClipManifest> Clips { get; init; } = [];

    public static CatArtPackManifest Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("The cat art pack manifest is missing.", manifestPath);
        }

        using var stream = File.OpenRead(manifestPath);
        return JsonSerializer.Deserialize<CatArtPackManifest>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException($"The cat art pack manifest '{manifestPath}' is empty.");
    }
}

public sealed class CatArtPackClipManifest
{
    public string ActionId { get; init; } = string.Empty;

    public string? Direction { get; init; }

    public string Directory { get; init; } = string.Empty;

    public bool Loop { get; init; }

    public int FrameDurationMilliseconds { get; init; }

    public IReadOnlyList<int>? FrameDurationsMilliseconds { get; init; }
}
