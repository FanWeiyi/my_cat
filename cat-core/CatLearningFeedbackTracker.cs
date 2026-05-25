namespace MyCat.CatCore;

public sealed class CatLearningFeedbackTracker
{
    public const int Threshold = 3;
    private readonly HashSet<string> _seenKeys;

    public CatLearningFeedbackTracker(IEnumerable<string>? seenKeys = null)
    {
        _seenKeys = seenKeys?.ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> SeenKeys => _seenKeys;

    public CatLearningFeedback? TryCreate(CatHabitProfile profile, CatObservationEvent latest)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(latest);

        if (profile.Count(latest.TimeBucket, latest.EventType) < Threshold)
        {
            return null;
        }

        var key = Key(latest.TimeBucket, latest.EventType);
        if (!_seenKeys.Add(key))
        {
            return null;
        }

        return new CatLearningFeedback(latest.TimeBucket, latest.EventType, Text(latest.EventType));
    }

    public static string Key(CatTimeBucket bucket, CatEventType eventType)
    {
        return $"{bucket}:{eventType}";
    }

    private static string Text(CatEventType eventType)
    {
        return eventType switch
        {
            CatEventType.Rest => "它记住这段时间更适合休息",
            CatEventType.Activity => "它记住这段时间更适合玩",
            CatEventType.Accompany => "它记住这段时间多陪陪你",
            _ => "它记住了一点节奏"
        };
    }
}
