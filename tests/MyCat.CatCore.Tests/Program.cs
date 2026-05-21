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
        PetReactionDuration = TimeSpan.FromSeconds(1)
    };

    public void Run()
    {
        StartsSitting();
        PettingInterruptsWalk();
        WalkSettlesBackToIdle();
        RestResumesAfterPetting();
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
        var controller = new CatBehaviorController(_fastOptions, () => 0.95);
        controller.Start(_start);
        var walk = controller.Advance(_start + TimeSpan.FromSeconds(1));
        var pet = controller.Pet(_start + TimeSpan.FromSeconds(1.1));

        Expect(walk?.State == CatState.Walk, "The sample should choose walking.");
        Expect(pet.State == CatState.PetReact, "A click should enter the pet reaction state.");
        Expect(pet.ActionId == CatActionId.PetReact, "A click should select the pet reaction clip.");
    }

    private void WalkSettlesBackToIdle()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.95);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(4));

        Expect(transition?.State == CatState.Idle, "A walk should end in a stable idle state.");
    }

    private void RestResumesAfterPetting()
    {
        var controller = new CatBehaviorController(_fastOptions, () => 0.5);
        controller.Start(_start);
        controller.Advance(_start + TimeSpan.FromSeconds(1));
        controller.Pet(_start + TimeSpan.FromSeconds(1.1));
        var transition = controller.Advance(_start + TimeSpan.FromSeconds(2.1));

        Expect(transition?.State == CatState.Rest, "Petting a resting cat should resume rest.");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
