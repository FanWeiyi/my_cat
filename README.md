# My Cat

`My Cat` is a Windows desktop cat prototype. The current prototype keeps the
scope deliberately small: one transparent desktop cat that can sit, sleep,
walk slowly, react to a click, remember three real-cat observations, and exit
from the system tray.

## Project layout

```text
cat-core        Behavior state and action selection
cat-assets      Replaceable placeholder action clips and frame metadata
windows-shell   WPF transparent desktop shell and tray integration
tests           Lightweight behavior checks with no test package dependency
```

## Current prototype scope

Included in M0-M2:

- Transparent borderless WPF window sized around one desktop cat.
- Click response and a small cat menu that interrupts automatic behavior.
- Automatic sitting, sleeping, and slow walking loops.
- System tray observation shortcuts and exit action.
- Local JSON persistence for sleeping, playing, and accompanying observations.
- Placeholder cat frames wired through stable action IDs:
  `idle_sit`, `rest_sleep`, `walk_slow`, and `pet_react`.

Deferred until later milestones:

- Habit weights and learning feedback.
- Quiet mode, multi-cat support, and formal animation assets.

Recorded observations are saved at:

```text
%LOCALAPPDATA%\MyCat\events.json
```

## Local development

The prototype targets .NET 9 for Windows WPF. Install a .NET 9 SDK before
building. A Windows Desktop runtime alone is not enough for development.

Build and run the shell:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:APPDATA = "$PWD\.nuget-home"
dotnet build .\MyCat.sln --configfile .\NuGet.Config
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj --no-restore
```

Run the core behavior checks:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:APPDATA = "$PWD\.nuget-home"
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj --configfile .\NuGet.Config
```

## Manual M0-M1 check

1. Start the shell and confirm only the cat-shaped window content is visible.
2. Click the cat and confirm it reacts and opens its small menu.
3. Record sleeping, playing, or accompanying from the cat menu and see `已记下`.
4. Record the same three observations from the tray menu.
5. Restart the app and confirm `%LOCALAPPDATA%\MyCat\events.json` still contains the observations.
6. Wait for the cat to rotate through sitting, sleeping, and slow walking.
7. Confirm walking stays inside the main screen work area.
8. Open the tray menu and choose `退出`.
9. Leave a run open for 30 minutes before calling M0 stable.
