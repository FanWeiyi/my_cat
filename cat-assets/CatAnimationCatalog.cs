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
                ("sit_open", 620),
                ("sit_blink", 180),
                ("sit_tail", 420),
                ("sit_open", 560)),
            [CatActionId.RestSleep] = Clip(
                CatActionId.RestSleep,
                true,
                ("sleep_low", 760),
                ("sleep_breathe", 760),
                ("sleep_dream", 520)),
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
                ("pet_lift", 300),
                ("pet_nuzzle", 420),
                ("pet_squish", 240)),
            [CatActionId.DragSettle] = Clip(
                CatActionId.DragSettle,
                false,
                ("settle_drop", 240),
                ("settle_blink", 460),
                ("sit_open", 360)),
            [CatActionId.MouseNotice] = Clip(
                CatActionId.MouseNotice,
                false,
                ("notice_glance", 320),
                ("notice_focus", 520),
                ("sit_blink", 240)),
            [CatActionId.WindowLinger] = Clip(
                CatActionId.WindowLinger,
                true,
                ("window_perch", 680),
                ("window_watch", 900)),
            [CatActionId.ObservationRest] = Clip(
                CatActionId.ObservationRest,
                false,
                ("note_yawn", 520),
                ("sleep_low", 560)),
            [CatActionId.ObservationActivity] = Clip(
                CatActionId.ObservationActivity,
                false,
                ("note_perk", 420),
                ("walk_left", 280),
                ("note_perk", 380)),
            [CatActionId.ObservationAccompany] = Clip(
                CatActionId.ObservationAccompany,
                false,
                ("note_nuzzle", 460),
                ("pet_nuzzle", 520))
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
