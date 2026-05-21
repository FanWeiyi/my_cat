namespace MyCat.CatCore;

public readonly record struct CatActionId(string Value)
{
    public static readonly CatActionId IdleSit = new("idle_sit");
    public static readonly CatActionId RestSleep = new("rest_sleep");
    public static readonly CatActionId WalkSlow = new("walk_slow");
    public static readonly CatActionId PetReact = new("pet_react");

    public override string ToString() => Value;
}

