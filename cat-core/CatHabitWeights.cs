namespace MyCat.CatCore;

public sealed record CatHabitWeights(
    double RestWeight,
    double ActivityWeight,
    double AccompanyWeight)
{
    public static readonly CatHabitWeights Default = new(0.35, 0.25, 0.4);
}

