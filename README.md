# My Cat

`My Cat` is a Windows desktop cat prototype. The first milestone keeps the
scope deliberately small: one transparent desktop cat that can sit, sleep,
walk slowly, react to a click, and exit from the system tray.

## Project layout

```text
cat-core        Behavior state and action selection
cat-assets      Replaceable placeholder action clips and frame metadata
windows-shell   WPF transparent desktop shell and tray integration
tests           Lightweight behavior checks with no test package dependency
```

## Current prototype scope

Included in M0-M1:

- Transparent borderless WPF window sized around one desktop cat.
- Click response that interrupts automatic behavior.
- Automatic sitting, sleeping, and slow walking loops.
- System tray exit action.
- Placeholder cat frames wired through stable action IDs:
  `idle_sit`, `rest_sleep`, `walk_slow`, and `pet_react`.

Deferred until later milestones:

- Cat menu and "tell it" recording flow.
- Event persistence, time buckets, habit weights, and learning feedback.
- Quiet mode, multi-cat support, and formal animation assets.

## Local development

The prototype targets .NET 9 for Windows WPF. Install a .NET 9 SDK before
building. A Windows Desktop runtime alone is not enough for development.

Build and run the shell:

```powershell
dotnet build .\MyCat.sln
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj
```

Run the core behavior checks:

```powershell
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj
```

## Manual M0-M1 check

1. Start the shell and confirm only the cat-shaped window content is visible.
2. Click the cat and confirm the click reaction takes priority.
3. Wait for the cat to rotate through sitting, sleeping, and slow walking.
4. Confirm walking stays inside the main screen work area.
5. Open the tray menu and choose `Exit`.
6. Leave a run open for 30 minutes before calling M0 stable.

