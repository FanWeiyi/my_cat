namespace MyCat.CatCore;

public sealed record CatHabitWeights(
    double RestWeight,
    double ActivityWeight,
    double AccompanyWeight)
{
    public static readonly CatHabitWeights Default = new(0.35, 0.25, 0.4);

    public static CatHabitWeights Normalize(double restWeight, double activityWeight, double accompanyWeight)
    {
        var rest = Math.Max(0, restWeight);
        var activity = Math.Max(0, activityWeight);
        var accompany = Math.Max(0, accompanyWeight);
        var total = rest + activity + accompany;
        return total <= 0
            ? Default
            : new CatHabitWeights(rest / total, activity / total, accompany / total);
    }

    public CatHabitWeights Normalized() => Normalize(RestWeight, ActivityWeight, AccompanyWeight);
}
