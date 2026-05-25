namespace MyCat.CatCore;

public sealed record CatObservationEvent(
    CatEventType EventType,
    DateTimeOffset OccurredAt,
    CatTimeBucket TimeBucket,
    string Source)
{
    public static CatObservationEvent Create(CatEventType eventType, DateTimeOffset occurredAt, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new CatObservationEvent(
            eventType,
            occurredAt,
            CatTimeBucketResolver.Resolve(occurredAt),
            source);
    }
}

