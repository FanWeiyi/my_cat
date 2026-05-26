namespace MyCat.CatCore;

public sealed record CatBehaviorSettings
{
    public const int CurrentVersion = 1;

    public static readonly CatBehaviorSettings Empty = new();

    public int Version { get; init; } = CurrentVersion;

    public Dictionary<CatTimeBucket, CatHabitWeights> ManualWeights { get; init; } = new();

    public CatHabitWeights ResolveWeights(CatTimeBucket bucket, CatHabitProfile learnedProfile)
    {
        ArgumentNullException.ThrowIfNull(learnedProfile);
        return TryGetManual(bucket, out var manual)
            ? manual
            : learnedProfile.For(bucket);
    }

    public bool TryGetManual(CatTimeBucket bucket, out CatHabitWeights weights)
    {
        if (ManualWeights.TryGetValue(bucket, out var manual))
        {
            weights = manual.Normalized();
            return true;
        }

        weights = CatHabitWeights.Default;
        return false;
    }

    public CatBehaviorSettings WithManual(CatTimeBucket bucket, CatHabitWeights weights)
    {
        var next = new Dictionary<CatTimeBucket, CatHabitWeights>(ManualWeights)
        {
            [bucket] = weights.Normalized()
        };

        return this with
        {
            Version = CurrentVersion,
            ManualWeights = next
        };
    }

    public CatBehaviorSettings WithoutManual(CatTimeBucket bucket)
    {
        var next = new Dictionary<CatTimeBucket, CatHabitWeights>(ManualWeights);
        next.Remove(bucket);
        return this with
        {
            Version = CurrentVersion,
            ManualWeights = next
        };
    }
}
