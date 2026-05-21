namespace MyCat.CatCore;

public sealed record CatActionTransition(
    CatState State,
    CatActionId ActionId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndsAt);

