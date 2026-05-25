using MyCat.CatCore;

namespace MyCat.CatAssets;

public sealed record CatAnimationClip(
    CatActionId ActionId,
    bool Loop,
    IReadOnlyList<CatFrame> Frames);

