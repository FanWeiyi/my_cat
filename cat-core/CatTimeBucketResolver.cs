namespace MyCat.CatCore;

public static class CatTimeBucketResolver
{
    public static CatTimeBucket Resolve(DateTimeOffset occurredAt)
    {
        return occurredAt.Hour switch
        {
            >= 5 and < 12 => CatTimeBucket.Morning,
            >= 12 and < 18 => CatTimeBucket.Afternoon,
            >= 18 and < 23 => CatTimeBucket.Evening,
            _ => CatTimeBucket.Night
        };
    }
}

