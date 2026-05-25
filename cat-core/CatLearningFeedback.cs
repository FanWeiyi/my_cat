namespace MyCat.CatCore;

public sealed record CatLearningFeedback(CatTimeBucket TimeBucket, CatEventType EventType, string Text);

