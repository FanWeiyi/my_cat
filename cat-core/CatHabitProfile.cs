namespace MyCat.CatCore;

public sealed class CatHabitProfile
{
    private readonly IReadOnlyDictionary<CatTimeBucket, CatHabitWeights> _weights;
    private readonly IReadOnlyDictionary<(CatTimeBucket Bucket, CatEventType EventType), int> _counts;

    private CatHabitProfile(
        IReadOnlyDictionary<CatTimeBucket, CatHabitWeights> weights,
        IReadOnlyDictionary<(CatTimeBucket Bucket, CatEventType EventType), int> counts)
    {
        _weights = weights;
        _counts = counts;
    }

    public static CatHabitProfile FromEvents(IEnumerable<CatObservationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var counts = events
            .GroupBy(catEvent => (catEvent.TimeBucket, catEvent.EventType))
            .ToDictionary(group => group.Key, group => group.Count());

        var weights = Enum.GetValues<CatTimeBucket>()
            .ToDictionary(bucket => bucket, bucket => BuildWeights(bucket, counts));

        return new CatHabitProfile(weights, counts);
    }

    public CatHabitWeights For(CatTimeBucket bucket)
    {
        return _weights.TryGetValue(bucket, out var weights)
            ? weights
            : CatHabitWeights.Default;
    }

    public int Count(CatTimeBucket bucket, CatEventType eventType)
    {
        return _counts.TryGetValue((bucket, eventType), out var count)
            ? count
            : 0;
    }

    private static CatHabitWeights BuildWeights(
        CatTimeBucket bucket,
        IReadOnlyDictionary<(CatTimeBucket Bucket, CatEventType EventType), int> counts)
    {
        var rest = CatHabitWeights.Default.RestWeight + Boost(bucket, CatEventType.Rest, counts);
        var activity = CatHabitWeights.Default.ActivityWeight + Boost(bucket, CatEventType.Activity, counts);
        var accompany = CatHabitWeights.Default.AccompanyWeight + Boost(bucket, CatEventType.Accompany, counts);
        var total = rest + activity + accompany;

        return new CatHabitWeights(rest / total, activity / total, accompany / total);
    }

    private static double Boost(
        CatTimeBucket bucket,
        CatEventType eventType,
        IReadOnlyDictionary<(CatTimeBucket Bucket, CatEventType EventType), int> counts)
    {
        return counts.TryGetValue((bucket, eventType), out var count)
            ? Math.Min(count, 8) * 0.09
            : 0;
    }
}

