using MyCat.CatCore;

namespace MyCat.CatAssets;

public sealed class CatAnimationCatalog
{
    private readonly IReadOnlyDictionary<string, CatAnimationClip> _clips;

    public CatAnimationCatalog(string? packRoot = null)
    {
        PackRoot = Path.GetFullPath(packRoot ?? Path.Combine(AppContext.BaseDirectory, "cats", "my-cat"));
        Manifest = CatArtPackManifest.Load(Path.Combine(PackRoot, "manifest.json"));
        CatArtPackValidator.Validate(PackRoot, Manifest);
        _clips = Manifest.Clips.ToDictionary(CatArtPackValidator.ClipKey, CreateClip, StringComparer.Ordinal);
    }

    public string PackRoot { get; }

    public CatArtPackManifest Manifest { get; }

    public CatAnimationClip Get(CatActionId actionId, bool facingLeft = true)
    {
        var direction = actionId == CatActionId.WalkSlow
            ? facingLeft ? "left" : "right"
            : null;
        var key = CatArtPackValidator.ClipKey(actionId.Value, direction);

        return _clips.TryGetValue(key, out var clip)
            ? clip
            : throw new KeyNotFoundException($"Unknown cat action clip '{key}'.");
    }

    private CatAnimationClip CreateClip(CatArtPackClipManifest clip)
    {
        var clipRoot = CatArtPackValidator.ResolveClipRoot(PackRoot, clip.Directory);
        var frames = Enumerable.Range(0, Manifest.FrameCount)
            .Select(index =>
            {
                var durationMilliseconds = clip.FrameDurationsMilliseconds?[index]
                    ?? clip.FrameDurationMilliseconds;
                return new CatFrame(
                    Path.Combine(clipRoot, $"frame_{index:000}.png"),
                    TimeSpan.FromMilliseconds(durationMilliseconds));
            })
            .ToArray();

        return new CatAnimationClip(new CatActionId(clip.ActionId), clip.Loop, frames);
    }
}
