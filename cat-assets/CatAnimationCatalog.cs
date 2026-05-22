using MyCat.CatCore;

namespace MyCat.CatAssets;

public sealed class CatAnimationCatalog
{
    private readonly IReadOnlyDictionary<CatActionId, CatAnimationClip> _clips =
        new Dictionary<CatActionId, CatAnimationClip>
        {
            [CatActionId.IdleSit] = Clip(
                CatActionId.IdleSit,
                true,
                ("sit_open", 720),
                ("sit_blink", 180),
                ("sit_open", 600)),
            [CatActionId.RestSleep] = Clip(
                CatActionId.RestSleep,
                true,
                ("sleep_low", 900),
                ("sleep_breathe", 900)),
            [CatActionId.WalkSlow] = Clip(
                CatActionId.WalkSlow,
                true,
                ("walk_left", 240),
                ("walk_right", 240)),
            [CatActionId.WakeStretch] = Clip(
                CatActionId.WakeStretch,
                false,
                ("wake_low", 240),
                ("wake_stretch", 560),
                ("wake_settle", 420)),
            [CatActionId.EdgeStop] = Clip(
                CatActionId.EdgeStop,
                false,
                ("edge_brake", 220),
                ("edge_watch", 860)),
            [CatActionId.PetReact] = Clip(
                CatActionId.PetReact,
                false,
                ("pet_squish", 220),
                ("pet_lift", 360),
                ("pet_squish", 220))
        };

    public CatAnimationClip Get(CatActionId actionId)
    {
        return _clips.TryGetValue(actionId, out var clip)
            ? clip
            : throw new KeyNotFoundException($"Unknown cat action '{actionId}'.");
    }

    private static CatAnimationClip Clip(
        CatActionId actionId,
        bool loop,
        params (string Key, int Milliseconds)[] frames)
    {
        return new CatAnimationClip(
            actionId,
            loop,
            frames.Select(frame => new CatFrame(frame.Key, TimeSpan.FromMilliseconds(frame.Milliseconds))).ToArray());
    }
}
