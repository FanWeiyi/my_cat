using MyCat.CatAssets;
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
        DragLiftDuration = TimeSpan.FromSeconds(1),
        DragHoldDuration = TimeSpan.FromSeconds(1),
        DragDropDuration = TimeSpan.FromSeconds(1),
        MouseNoticeDuration = TimeSpan.FromSeconds(1),
        MouseTrackDuration = TimeSpan.FromSeconds(1),
        WindowLingerDuration = TimeSpan.FromSeconds(1),
        WindowStartleDuration = TimeSpan.FromSeconds(1),
        WindowAvoidDuration = TimeSpan.FromSeconds(1),
        TaskbarVisitDuration = TimeSpan.FromSeconds(1),
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
        DragLiftHoldAndDropReturnToIdle();
        MouseNoticeWaitsForIdleAndQuietMode();
        ClickMouseTrackReturnsToIdle();
        WindowLingerCanFinishAWalk();
        WindowAvoidReturnsToIdle();
        TaskbarVisitRespectsQuietMode();
        ObservationReactionFollowsTheRecordedEvent();
        ObservationCapturesTimeBucket();
        EventStoreSurvivesReload();
        InvalidEventJsonDoesNotCrash();
        RestObservationsBiasRestWeight();
        LearningFeedbackOnlyAppearsOnce();
        InteractionMetricsSurviveReload();
        InvalidMetricsJsonDoesNotCrash();
        BehaviorSettingsUseLearnedWeightsByDefault();
        BehaviorSettingsOverrideOnlyOneBucket();
        BehaviorSettingsNormalizeManualWeights();
        BehaviorSettingsStoreSurvivesReload();
        InvalidBehaviorSettingsJsonDoesNotCrash();
        ManualBehaviorSettingsAffectAutomaticChoices();
        QuietModeSuppressesManualActivity();
        QuietModeSuppressesWalking();
        DefaultBehaviorDurationsKeepIdleAndRestCalm();
        PersonalizedArtPackLoadsEveryAction();
        PersonalizedArtPackUsesLongHoldFrames();
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

    private void DragLiftHoldAndDropReturnToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var lift = controller.DragLifted(_start + TimeSpan.FromSeconds(0.1));
        var hold = controller.Advance(_start + TimeSpan.FromSeconds(1.1));
        var drop = controller.DragDropped(_start + TimeSpan.FromSeconds(1.2));
        var next = controller.Advance(_start + TimeSpan.FromSeconds(2.2));

        Expect(lift.ActionId == CatActionId.DragLift, "A dragged cat should first use the lift clip.");
        Expect(hold?.ActionId == CatActionId.DragHold, "After lift, dragging should use the held clip.");
        Expect(drop.ActionId == CatActionId.DragDrop, "A released cat should use the drop clip.");
        Expect(next?.State == CatState.Idle, "A drag drop should settle into idle.");
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

    private void ClickMouseTrackReturnsToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var track = controller.TrackMouse(_start + TimeSpan.FromSeconds(0.1), CatActionId.MouseTrackUp);
        var retarget = controller.RetargetMouse(CatActionId.MouseTrackRight);
        var next = controller.Advance(_start + TimeSpan.FromSeconds(1.1));

        Expect(track.ActionId == CatActionId.MouseTrackUp, "A clicked cat should briefly track the mouse direction.");
        Expect(retarget.ActionId == CatActionId.MouseTrackRight, "Mouse tracking should support live direction changes.");
        Expect(next?.State == CatState.Idle, "Mouse tracking should end in idle.");
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

    private void WindowAvoidReturnsToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var startle = controller.StartleFromWindow(_start + TimeSpan.FromSeconds(0.1));
        var avoid = controller.Advance(_start + TimeSpan.FromSeconds(1.1));
        var next = controller.Advance(_start + TimeSpan.FromSeconds(2.1));

        Expect(startle?.ActionId == CatActionId.WindowStartle, "A moving nearby window should startle the cat.");
        Expect(avoid?.ActionId == CatActionId.WindowAvoid, "After the startle, the cat should avoid the window.");
        Expect(next?.State == CatState.Idle, "Window avoidance should end in idle.");
    }

    private void TaskbarVisitRespectsQuietMode()
    {
        var controller = new CatBehaviorController(_fastOptions);
        controller.Start(_start);
        var visit = controller.VisitTaskbar(_start + TimeSpan.FromSeconds(0.1), lie: false);
        controller.Advance(_start + TimeSpan.FromSeconds(1.1));
        controller.SetQuietMode(true, _start + TimeSpan.FromSeconds(1.2));
        var quietVisit = controller.VisitTaskbar(_start + TimeSpan.FromSeconds(1.3), lie: true);

        Expect(visit?.ActionId == CatActionId.TaskbarSit, "A taskbar visit should use a taskbar clip.");
        Expect(quietVisit is null, "Quiet mode should suppress proactive taskbar visits.");
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
                .CountWindowLinger()
                .CountMouseTrack()
                .CountDragLift()
                .CountDragDrop()
                .CountWindowAvoid()
                .CountTaskbarVisit()
                .CountWindowPeek());

            var metrics = new JsonCatInteractionMetricsStore(path).Read();
            Expect(metrics.ClickCount == 1, "Metrics should keep click counts.");
            Expect(metrics.WindowLingerCount == 1, "Metrics should keep window linger counts.");
            Expect(metrics.TaskbarVisitCount == 1, "Metrics should keep taskbar visit counts.");
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

    private static void BehaviorSettingsUseLearnedWeightsByDefault()
    {
        var profile = CatHabitProfile.FromEvents(Enumerable.Range(0, 4)
            .Select(index => CatObservationEvent.Create(
                CatEventType.Rest,
                new DateTimeOffset(2026, 5, 21, 14, index, 0, TimeSpan.FromHours(8)),
                CatEventSource.DesktopCatMenu)));
        var resolved = CatBehaviorSettings.Empty.ResolveWeights(CatTimeBucket.Afternoon, profile);

        Expect(
            resolved.RestWeight == profile.For(CatTimeBucket.Afternoon).RestWeight,
            "Behavior settings should use learned weights when there is no manual override.");
    }

    private static void BehaviorSettingsOverrideOnlyOneBucket()
    {
        var profile = CatHabitProfile.FromEvents(Enumerable.Range(0, 4)
            .Select(index => CatObservationEvent.Create(
                CatEventType.Activity,
                new DateTimeOffset(2026, 5, 21, 20, index, 0, TimeSpan.FromHours(8)),
                CatEventSource.DesktopCatMenu)));
        var settings = CatBehaviorSettings.Empty.WithManual(
            CatTimeBucket.Afternoon,
            new CatHabitWeights(0.8, 0.1, 0.1));

        var afternoon = settings.ResolveWeights(CatTimeBucket.Afternoon, profile);
        var evening = settings.ResolveWeights(CatTimeBucket.Evening, profile);

        Expect(afternoon.RestWeight > 0.79, "A manual bucket should use the saved manual rest weight.");
        Expect(
            evening.ActivityWeight == profile.For(CatTimeBucket.Evening).ActivityWeight,
            "Manual settings should not affect other time buckets.");
    }

    private static void BehaviorSettingsNormalizeManualWeights()
    {
        var settings = CatBehaviorSettings.Empty.WithManual(
            CatTimeBucket.Morning,
            new CatHabitWeights(2, 1, 1));
        var manual = settings.ManualWeights[CatTimeBucket.Morning];
        var total = manual.RestWeight + manual.ActivityWeight + manual.AccompanyWeight;

        Expect(Math.Abs(total - 1) < 0.0001, "Manual behavior weights should be normalized before saving.");
        Expect(Math.Abs(manual.RestWeight - 0.5) < 0.0001, "Normalized rest weight should preserve the input ratio.");
    }

    private static void BehaviorSettingsStoreSurvivesReload()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"behavior-settings-{Guid.NewGuid():N}.json");

        try
        {
            var settings = CatBehaviorSettings.Empty.WithManual(
                CatTimeBucket.Night,
                new CatHabitWeights(0.1, 0.7, 0.2));
            var store = new JsonCatBehaviorSettingsStore(path);
            store.Write(settings);

            var reloaded = new JsonCatBehaviorSettingsStore(path).Read();
            var hasManual = reloaded.TryGetManual(CatTimeBucket.Night, out var weights);

            Expect(hasManual, "Saved behavior settings should keep a manual bucket override.");
            Expect(weights.ActivityWeight > 0.69, "Saved behavior settings should keep manual activity weight.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void InvalidBehaviorSettingsJsonDoesNotCrash()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"behavior-settings-invalid-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, "{ nope");
            var settings = new JsonCatBehaviorSettingsStore(path).Read();
            Expect(settings.ManualWeights.Count == 0, "Invalid behavior settings JSON should fall back to empty settings.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private void ManualBehaviorSettingsAffectAutomaticChoices()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.2)
        {
            BehaviorSettings = CatBehaviorSettings.Empty.WithManual(
                CatTimeBucket.Afternoon,
                new CatHabitWeights(0.05, 0.9, 0.05))
        };

        controller.Start(_start);
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(1));

        Expect(transition?.State == CatState.Walk, "Manual activity settings should affect automatic choices.");
    }

    private void QuietModeSuppressesManualActivity()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.95)
        {
            BehaviorSettings = CatBehaviorSettings.Empty.WithManual(
                CatTimeBucket.Afternoon,
                new CatHabitWeights(0.01, 0.98, 0.01))
        };

        controller.Start(_start);
        controller.SetQuietMode(true, _start);
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(1));

        Expect(transition?.State != CatState.Walk, "Quiet mode should still suppress manually boosted activity.");
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

    private static void DefaultBehaviorDurationsKeepIdleAndRestCalm()
    {
        var options = new CatBehaviorOptions();

        Expect(options.IdleDuration == TimeSpan.FromSeconds(45), "The default idle state should stay calm without feeling frozen.");
        Expect(options.RestDuration == TimeSpan.FromMinutes(3), "The default rest state should stay longer than active states.");
    }

    private static void PersonalizedArtPackLoadsEveryAction()
    {
        var catalog = new CatAnimationCatalog();
        var actionIds = new[]
        {
            CatActionId.IdleSit,
            CatActionId.RestSleep,
            CatActionId.WakeStretch,
            CatActionId.EdgeStop,
            CatActionId.PetReact,
            CatActionId.DragSettle,
            CatActionId.DragLift,
            CatActionId.DragHold,
            CatActionId.DragDrop,
            CatActionId.MouseNotice,
            CatActionId.MouseTrack,
            CatActionId.MouseTrackLeft,
            CatActionId.MouseTrackRight,
            CatActionId.MouseTrackUp,
            CatActionId.MouseTrackDown,
            CatActionId.WindowLinger,
            CatActionId.WindowStartle,
            CatActionId.WindowAvoid,
            CatActionId.TaskbarSit,
            CatActionId.TaskbarLie,
            CatActionId.ObservationRest,
            CatActionId.ObservationActivity,
            CatActionId.ObservationAccompany
        };

        foreach (var actionId in actionIds)
        {
            Expect(catalog.Get(actionId).Frames.Count == 16, $"The '{actionId}' clip should load 16 art frames.");
        }

        var leftWalk = catalog.Get(CatActionId.WalkSlow, facingLeft: true);
        var rightWalk = catalog.Get(CatActionId.WalkSlow, facingLeft: false);
        Expect(leftWalk.Frames.Count == 16, "The left walk clip should load 16 art frames.");
        Expect(rightWalk.Frames.Count == 16, "The right walk clip should load 16 art frames.");
        Expect(leftWalk.Frames[0].Key.Contains("walk_slow_left"), "Left walks should use the left art sequence.");
        Expect(rightWalk.Frames[0].Key.Contains("walk_slow_right"), "Right walks should use the right art sequence.");
    }

    private static void PersonalizedArtPackUsesLongHoldFrames()
    {
        var catalog = new CatAnimationCatalog();
        var idle = catalog.Get(CatActionId.IdleSit);
        var sleep = catalog.Get(CatActionId.RestSleep);

        Expect(idle.Frames[0].Duration == TimeSpan.FromSeconds(12), "Idle should move often enough to feel present.");
        Expect(
            idle.Frames.Skip(5).Take(6).All(frame => frame.Duration <= TimeSpan.FromMilliseconds(150)),
            "Idle blink frames 5-10 should play as one continuous short motion.");
        Expect(idle.Frames[8].Duration < TimeSpan.FromMilliseconds(200), "Idle blink should not pause on frame 8.");
        Expect(sleep.Frames[0].Duration == TimeSpan.FromSeconds(30), "Sleep should breathe occasionally without frequent motion.");
        Expect(sleep.Frames[1].Duration == TimeSpan.FromMilliseconds(1000), "Sleep breath frames should be slow and brief.");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
