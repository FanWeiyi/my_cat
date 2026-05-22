using MyCat.CatCore;

var checks = new CatBehaviorChecks();
checks.Run();

internal sealed class CatBehaviorChecks
{
    private readonly DateTimeOffset _start = new(2026, 5, 21, 12, 0, 0, TimeSpan.Zero);
    private readonly CatBehaviorOptions _fastOptions = new()
    {
        IdleDuration = TimeSpan.FromSeconds(1),
        RestDuration = TimeSpan.FromSeconds(2),
        WalkDuration = TimeSpan.FromSeconds(3),
        WakeDuration = TimeSpan.FromSeconds(1),
        EdgePauseDuration = TimeSpan.FromSeconds(1),
        PetReactionDuration = TimeSpan.FromSeconds(1),
        DragSettleDuration = TimeSpan.FromSeconds(1),
        MouseNoticeDuration = TimeSpan.FromSeconds(1),
        WindowLingerDuration = TimeSpan.FromSeconds(1),
        ObservationReactionDuration = TimeSpan.FromSeconds(1)
    };

    public void Run()
    {
        StartsSitting();
        PettingInterruptsWalk();
        WalkSettlesBackToIdle();
        RestResumesAfterPetting();
        RestWakesBeforeWalking();
        WalkEdgeStopsBeforeIdle();
        DragSettleReturnsToIdle();
        MouseNoticeWaitsForIdleAndQuietMode();
        WindowLingerCanFinishAWalk();
        ObservationReactionFollowsTheRecordedEvent();
        ObservationCapturesTimeBucket();
        EventStoreSurvivesReload();
        InvalidEventJsonDoesNotCrash();
        RestObservationsBiasRestWeight();
        LearningFeedbackOnlyAppearsOnce();
        InteractionMetricsSurviveReload();
        InvalidMetricsJsonDoesNotCrash();
        QuietModeSuppressesWalking();
        Console.WriteLine("Cat core behavior checks passed.");
    }

    private void StartsSitting()
    {
        var controller = new CatBehaviorController(_fastOptions);
        var transition = controller.Start(_start);

        Expect(transition.State == CatState.Idle, "The cat should start idle.");
        Expect(transition.ActionId == CatActionId.IdleSit, "The idle clip should be the sitting clip.");
    }

    private void PettingInterruptsWalk()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.45);
        controller.Start(_start);
        var walk = controller.Advance(_start + TimeSpan.FromSeconds(1));
        var pet = controller.Pet(_start + TimeSpan.FromSeconds(1.1));

        Expect(walk?.State == CatState.Walk, "The sample should choose walking.");
        Expect(pet.State == CatState.PetReact, "A click should enter the pet reaction state.");
        Expect(pet.ActionId == CatActionId.PetReact, "A click should select the pet reaction clip.");
    }

    private void WalkSettlesBackToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.45);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(4));

        Expect(transition?.State == CatState.Idle, "A walk should end in a stable idle state.");
    }

    private void RestResumesAfterPetting()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.2);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        controller.Pet(_start + TimeSpan.FromSeconds(1.1));
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(2.1));

        Expect(transition?.State == CatState.Rest, "Petting a resting cat should resume rest.");
    }

    private void RestWakesBeforeWalking()
    {
        var samples = new Queue<double>([0.2, 0.45]);
        var controller = new CatBehaviorController(_fastOptions, () => samples.Dequeue());
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(3));

        Expect(transition?.State == CatState.Wake, "Leaving rest for activity should wake first.");
        Expect(transition?.ActionId == CatActionId.WakeStretch, "Wake should use the stretch clip.");
    }

    private void WalkEdgeStopsBeforeIdle()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.45);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        var transition = controller.ReachWalkEdge(_start + TimeSpan.FromSeconds(1.2));

        Expect(transition.State == CatState.EdgePause, "A walking cat should pause at an edge.");
        Expect(transition.ActionId == CatActionId.EdgeStop, "An edge pause should use the edge clip.");
    }

    private void DragSettleReturnsToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var settle = controller.DragSettled(_start + TimeSpan.FromSeconds(0.1));
        var next = controller.Advance(_start + TimeSpan.FromSeconds(1.1));

        Expect(settle.ActionId == CatActionId.DragSettle, "A placed cat should settle before idling.");
        Expect(next?.State == CatState.Idle, "A drag settle should end in idle.");
    }

    private void MouseNoticeWaitsForIdleAndQuietMode()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.45);
        controller.Start(_start);
        var notice = controller.NoticeMouse(_start + TimeSpan.FromSeconds(0.1));
        controller.Advance(_start + TimeSpan.FromSeconds(1.1));
        controller.Advance(_start + TimeSpan.FromSeconds(2.1));
        var walkingNotice = controller.NoticeMouse(_start + TimeSpan.FromSeconds(2.2));
        controller.SetQuietMode(true, _start + TimeSpan.FromSeconds(2.3));
        var quietNotice = controller.NoticeMouse(_start + TimeSpan.FromSeconds(2.4));
        var quietWindow = controller.LingerByWindow(_start + TimeSpan.FromSeconds(2.5));

        Expect(notice?.ActionId == CatActionId.MouseNotice, "An idle cat should notice a nearby mouse.");
        Expect(walkingNotice is null, "Mouse notice should not interrupt active walking.");
        Expect(quietNotice is null, "Quiet mode should suppress mouse notice.");
        Expect(quietWindow is null, "Quiet mode should suppress window lingering.");
    }

    private void ObservationReactionFollowsTheRecordedEvent()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var rest = controller.ReactToObservation(CatEventType.Rest, _start + TimeSpan.FromSeconds(0.1));
        var next = controller.Advance(_start + TimeSpan.FromSeconds(1.1));

        Expect(rest.ActionId == CatActionId.ObservationRest, "A rest note should use the rest response.");
        Expect(next?.State == CatState.Rest, "A rest note should settle into a rest state.");
    }

    private void WindowLingerCanFinishAWalk()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.45);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        var pause = controller.LingerByWindow(_start + TimeSpan.FromSeconds(1.1));

        Expect(pause?.ActionId == CatActionId.WindowLinger, "A walk can finish at a window stay.");
    }

    private static void ObservationCapturesTimeBucket()
    {
        var catEvent = CatObservationEvent.Create(
            CatEventType.Activity,
            new DateTimeOffset(2026, 5, 21, 19, 30, 0, TimeSpan.FromHours(8)),
            CatEventSource.TrayMenu);

        Expect(catEvent.TimeBucket == CatTimeBucket.Evening, "An evening observation should use the evening bucket.");
        Expect(catEvent.Source == CatEventSource.TrayMenu, "An observation should keep its entry source.");
    }

    private static void EventStoreSurvivesReload()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"events-{Guid.NewGuid():N}.json");

        try
        {
            var store = new JsonCatEventStore(path);
            store.Append(CatObservationEvent.Create(
                CatEventType.Rest,
                new DateTimeOffset(2026, 5, 21, 14, 0, 0, TimeSpan.FromHours(8)),
                CatEventSource.DesktopCatMenu));

            var reloaded = new JsonCatEventStore(path).ReadAll();
            Expect(reloaded.Count == 1, "A saved event should survive a store reload.");
            Expect(reloaded[0].TimeBucket == CatTimeBucket.Afternoon, "Reloaded event data should keep its time bucket.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void InvalidEventJsonDoesNotCrash()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"events-invalid-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, "{ nope");
            var events = new JsonCatEventStore(path).ReadAll();
            Expect(events.Count == 0, "Invalid event JSON should fall back to an empty history.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void InteractionMetricsSurviveReload()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"metrics-{Guid.NewGuid():N}.json");

        try
        {
            var store = new JsonCatInteractionMetricsStore(path);
            store.Write(CatInteractionMetrics.Empty
                .CountClick()
                .CountDrag()
                .CountTell()
                .CountQuietModeEnable()
                .CountMouseNotice()
                .CountWindowLinger());

            var metrics = new JsonCatInteractionMetricsStore(path).Read();
            Expect(metrics.ClickCount == 1, "Metrics should keep click counts.");
            Expect(metrics.WindowLingerCount == 1, "Metrics should keep window linger counts.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void InvalidMetricsJsonDoesNotCrash()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"metrics-invalid-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, "{ nope");
            var metrics = new JsonCatInteractionMetricsStore(path).Read();
            Expect(metrics == CatInteractionMetrics.Empty, "Invalid metrics JSON should fall back to empty counts.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void RestObservationsBiasRestWeight()
    {
        var events = Enumerable.Range(0, 4)
            .Select(index => CatObservationEvent.Create(
                CatEventType.Rest,
                new DateTimeOffset(2026, 5, 21, 14, index, 0, TimeSpan.FromHours(8)),
                CatEventSource.DesktopCatMenu));

        var profile = CatHabitProfile.FromEvents(events);
        Expect(
            profile.For(CatTimeBucket.Afternoon).RestWeight > CatHabitWeights.Default.RestWeight,
            "Repeated rest observations should increase rest weight in that bucket.");
    }

    private static void LearningFeedbackOnlyAppearsOnce()
    {
        var events = Enumerable.Range(0, 3)
            .Select(index => CatObservationEvent.Create(
                CatEventType.Activity,
                new DateTimeOffset(2026, 5, 21, 20, index, 0, TimeSpan.FromHours(8)),
                CatEventSource.TrayMenu))
            .ToArray();
        var profile = CatHabitProfile.FromEvents(events);
        var tracker = new CatLearningFeedbackTracker();

        var first = tracker.TryCreate(profile, events[^1]);
        var second = tracker.TryCreate(profile, events[^1]);

        Expect(first is not null, "The third matching observation should create learning feedback.");
        Expect(second is null, "A learning feedback key should not repeat.");
    }

    private void QuietModeSuppressesWalking()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.55)
        {
            HabitProfile = CatHabitProfile.FromEvents(Enumerable.Range(0, 8)
                .Select(index => CatObservationEvent.Create(
                    CatEventType.Activity,
                    new DateTimeOffset(2026, 5, 21, 20, index, 0, TimeSpan.FromHours(8)),
                    CatEventSource.TrayMenu)))
        };

        controller.Start(_start);
        controller.SetQuietMode(true, _start);
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(1));

        Expect(transition?.State != CatState.Walk, "Quiet mode should suppress proactive walking.");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
