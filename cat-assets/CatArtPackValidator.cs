using MyCat.CatCore;

namespace MyCat.CatAssets;

public static class CatArtPackValidator
{
    private const int ExpectedCanvasSize = 512;
    private const int ExpectedFrameCount = 16;
    private static readonly RequiredClip[] RequiredClips =
    [
        new(CatActionId.IdleSit, null, true),
        new(CatActionId.RestSleep, null, true),
        new(CatActionId.WalkSlow, "left", true),
        new(CatActionId.WalkSlow, "right", true),
        new(CatActionId.WakeStretch, null, false),
        new(CatActionId.EdgeStop, null, false),
        new(CatActionId.PetReact, null, false),
        new(CatActionId.DragSettle, null, false),
        new(CatActionId.MouseNotice, null, false),
        new(CatActionId.WindowLinger, null, true),
        new(CatActionId.ObservationRest, null, false),
        new(CatActionId.ObservationActivity, null, false),
        new(CatActionId.ObservationAccompany, null, false)
    ];

    public static void Validate(string packRoot, CatArtPackManifest manifest)
    {
        if (manifest.Id != "my-cat" || manifest.Version != "0.3")
        {
            throw new InvalidDataException("The configured cat art pack must be 'my-cat' version '0.3'.");
        }

        if (manifest.CanvasWidth != ExpectedCanvasSize || manifest.CanvasHeight != ExpectedCanvasSize)
        {
            throw new InvalidDataException($"The cat art pack canvas must be {ExpectedCanvasSize} x {ExpectedCanvasSize}.");
        }

        if (manifest.FrameCount != ExpectedFrameCount)
        {
            throw new InvalidDataException($"The cat art pack must define {ExpectedFrameCount} frames per clip.");
        }

        var clipLookup = manifest.Clips.ToDictionary(ClipKey, StringComparer.Ordinal);
        if (clipLookup.Count != manifest.Clips.Count)
        {
            throw new InvalidDataException("The cat art pack manifest contains duplicate action clips.");
        }

        foreach (var required in RequiredClips)
        {
            var key = ClipKey(required.ActionId.Value, required.Direction);
            if (!clipLookup.TryGetValue(key, out var clip))
            {
                throw new InvalidDataException($"The cat art pack is missing the '{key}' clip.");
            }

            ValidateClip(packRoot, clip, required);
        }
    }

    internal static string ClipKey(CatArtPackClipManifest clip) => ClipKey(clip.ActionId, clip.Direction);

    internal static string ClipKey(string actionId, string? direction)
    {
        return string.IsNullOrWhiteSpace(direction)
            ? actionId
            : $"{actionId}:{direction.Trim().ToLowerInvariant()}";
    }

    private static void ValidateClip(string packRoot, CatArtPackClipManifest clip, RequiredClip required)
    {
        if (clip.Loop != required.Loop)
        {
            throw new InvalidDataException($"The '{ClipKey(clip)}' clip has the wrong loop setting.");
        }

        if (clip.FrameDurationMilliseconds <= 0)
        {
            throw new InvalidDataException($"The '{ClipKey(clip)}' clip must define a positive frame duration.");
        }

        if (clip.FrameDurationsMilliseconds is not null)
        {
            if (clip.FrameDurationsMilliseconds.Count != ExpectedFrameCount)
            {
                throw new InvalidDataException(
                    $"The '{ClipKey(clip)}' clip must define exactly {ExpectedFrameCount} per-frame durations.");
            }

            if (clip.FrameDurationsMilliseconds.Any(duration => duration <= 0))
            {
                throw new InvalidDataException($"The '{ClipKey(clip)}' clip has a non-positive per-frame duration.");
            }
        }

        var clipRoot = ResolveClipRoot(packRoot, clip.Directory);
        if (!Directory.Exists(clipRoot))
        {
            throw new DirectoryNotFoundException($"The cat art clip directory '{clipRoot}' is missing.");
        }

        var expectedFiles = Enumerable.Range(0, ExpectedFrameCount)
            .Select(index => Path.Combine(clipRoot, $"frame_{index:000}.png"))
            .ToArray();
        var missingFile = expectedFiles.FirstOrDefault(path => !File.Exists(path));
        if (missingFile is not null)
        {
            throw new FileNotFoundException($"The cat art clip '{ClipKey(clip)}' is missing a frame.", missingFile);
        }

        foreach (var frameFile in expectedFiles)
        {
            ValidatePngFrame(frameFile, clip);
        }

        var frameFiles = Directory.GetFiles(clipRoot, "frame_*.png", SearchOption.TopDirectoryOnly);
        if (frameFiles.Length != ExpectedFrameCount)
        {
            throw new InvalidDataException($"The cat art clip '{ClipKey(clip)}' must contain exactly {ExpectedFrameCount} PNG frames.");
        }
    }

    internal static string ResolveClipRoot(string packRoot, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidDataException("A cat art clip directory cannot be empty.");
        }

        var normalizedPackRoot = Path.GetFullPath(packRoot);
        var clipRoot = Path.GetFullPath(Path.Combine(normalizedPackRoot, directory));
        var relative = Path.GetRelativePath(normalizedPackRoot, clipRoot);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidDataException($"The cat art clip directory '{directory}' escapes the art pack.");
        }

        return clipRoot;
    }

    private static void ValidatePngFrame(string frameFile, CatArtPackClipManifest clip)
    {
        using var stream = File.OpenRead(frameFile);
        using var reader = new BinaryReader(stream);
        var signature = reader.ReadBytes(8);
        var pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (!signature.SequenceEqual(pngSignature))
        {
            throw new InvalidDataException($"The frame '{frameFile}' for '{ClipKey(clip)}' is not a PNG.");
        }

        var chunkLength = ReadBigEndianInt32(reader);
        var chunkType = new string(reader.ReadChars(4));
        if (chunkLength != 13 || chunkType != "IHDR")
        {
            throw new InvalidDataException($"The frame '{frameFile}' for '{ClipKey(clip)}' has an invalid PNG header.");
        }

        var width = ReadBigEndianInt32(reader);
        var height = ReadBigEndianInt32(reader);
        _ = reader.ReadByte(); // bit depth
        var colorType = reader.ReadByte();
        if (width != ExpectedCanvasSize || height != ExpectedCanvasSize || colorType != 6)
        {
            throw new InvalidDataException(
                $"The frame '{frameFile}' for '{ClipKey(clip)}' must be a {ExpectedCanvasSize} x {ExpectedCanvasSize} RGBA PNG.");
        }
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(sizeof(int));
        if (bytes.Length != sizeof(int))
        {
            throw new InvalidDataException("A cat art PNG ended before its header was complete.");
        }

        return (bytes[0] << 24)
            | (bytes[1] << 16)
            | (bytes[2] << 8)
            | bytes[3];
    }

    private sealed record RequiredClip(CatActionId ActionId, string? Direction, bool Loop);
}
