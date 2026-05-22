# My Cat Development Progress Report

Date: 2026-05-22  
Workspace: `D:\work\code\my_cat`  
Remote target: `FanWeiyi/my_cat`

## Current status

The prototype has reached the planned M0-M4 playtest scope locally.

It is a Windows WPF desktop companion with one transparent placeholder cat,
six wired actions, local observation recording, lightweight habit biasing,
quiet mode, playtest documentation, and a repeatable packaging flow.

Current practical state:

- Local prototype runs on Windows.
- Release build and core behavior checks pass.
- A framework-dependent playtest ZIP can be generated locally.
- Source has not yet been pushed to GitHub from this workspace because the
  original `.git` metadata is not writable in the current session and GitHub
  push authentication returned `401`.

## Delivered milestones

| Milestone | Status | Notes |
|---|---|---|
| M0 desktop feasibility | Done locally | Transparent window, click target, tray exit, desktop placement. |
| M1 companion baseline | Done locally | Sit, sleep, slow walk, click reaction, automatic state changes. |
| M2 teach loop | Done locally | Cat menu, tray recording, three observation types, JSON persistence, `已记下` feedback. |
| M3 light learning | Done locally | Time buckets, weights, behavior biasing, one-time learning feedback, quiet mode. |
| M4 playtest prep | Done locally | Six action IDs wired, playtest docs, feedback form, release checklist, packaging script. |

## What exists now

### Desktop shell

- Transparent borderless always-on-top WPF window sized around one cat.
- Cat click response and small cat menu.
- System tray menu for telling the cat, quiet mode, and exit.
- Screen work-area placement and edge handling for movement.

Main files:

- `windows-shell/DesktopCatWindow.cs`
- `windows-shell/TrayIconHost.cs`
- `windows-shell/CatSprite.cs`

### Actions and behavior

Wired action IDs:

| Action ID | Prototype behavior |
|---|---|
| `idle_sit` | Sit and blink. |
| `rest_sleep` | Lie down and breathe. |
| `walk_slow` | Move across the work area. |
| `wake_stretch` | Wake transition before leaving rest. |
| `edge_stop` | Pause after reaching a desktop edge. |
| `pet_react` | Priority response to a click. |

The current art is placeholder drawing code and frame metadata, not final
animation assets.

Main files:

- `cat-core/CatBehaviorController.cs`
- `cat-assets/CatAnimationCatalog.cs`
- `windows-shell/CatSprite.cs`

### Observation and learning data

Supported observation types:

- Real cat is sleeping.
- Real cat is playing.
- Real cat is accompanying the user.

Data paths:

```text
%LOCALAPPDATA%\MyCat\events.json
%LOCALAPPDATA%\MyCat\learning-state.json
```

Learning behavior:

- Events are grouped into morning, afternoon, evening, and night buckets.
- Repeated rest/activity/accompany events bias later action choice in that
  time bucket.
- Three matching observations in one bucket can show a one-time learning hint.
- Quiet mode strongly suppresses proactive walking.

Main files:

- `cat-core/CatObservationEvent.cs`
- `cat-core/JsonCatEventStore.cs`
- `cat-core/CatHabitProfile.cs`
- `cat-core/CatLearningFeedbackTracker.cs`

## Run and package

### Local run from CMD

```cmd
cd /d D:\work\code\my_cat
set DOTNET_CLI_HOME=%CD%\.dotnet-home
set APPDATA=%CD%\.nuget-home
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj --no-restore
```

### Validation

```cmd
cd /d D:\work\code\my_cat
set DOTNET_CLI_HOME=%CD%\.dotnet-home
set APPDATA=%CD%\.nuget-home
dotnet build .\MyCat.sln -c Release --configfile .\NuGet.Config
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj -c Release --configfile .\NuGet.Config
```

### Playtest package

Default self-contained package:

```cmd
scripts\package-playtest.cmd
```

Restricted-network fallback:

```cmd
scripts\package-playtest.cmd -FrameworkDependent
```

Output:

```text
artifacts\playtest\MyCat-playtest-win-x64\
artifacts\playtest\MyCat-playtest-win-x64.zip
```

Playtest references:

- `docs/playtest-guide.md`
- `docs/playtest-feedback.md`
- `docs/playtest-release-checklist.md`
- `docs/playtest-runbook.txt`

## Validation record

Completed locally:

- .NET 9 SDK installed and recognized.
- Release solution build passed on 2026-05-22.
- Core behavior checks passed on 2026-05-22.
- Desktop shell startup probe stayed running.
- Framework-dependent playtest package was generated.
- Packaged executable startup probe stayed running.

Manual validation still needed before broad sharing:

- One uninterrupted 30-minute desktop run.
- Hands-on confirmation of click, tray, teaching, quiet mode, and exit in the
  final package used for testers.
- Feedback from 5 to 10 target users.

## Known constraints and risks

### Engineering constraints

- GitHub sync is not complete from this workspace.
- Current source work is mirrored into temporary Git metadata at
  `.publish-git`; the original repository `.git` directory still fails
  `git add` with `index.lock: Permission denied`.
- HTTPS push to GitHub from the temporary metadata returned `401`; `gh` is not
  currently available in this session.
- Self-contained packaging needs runtime pack restore from NuGet. The current
  restricted session cannot reach `api.nuget.org`, so only the
  framework-dependent fallback was verified here.

### Product risks

- Placeholder cat art is enough for system validation, but cuteness and
  attachment should be judged with improved visual assets.
- Habit learning is intentionally light and needs playtest evidence before
  being made more complex.
- The current prototype is one cat on the primary Windows desktop work area;
  multi-display and personalization depth are not finished product features.

## Suggested next iteration

Recommended order:

1. Publish the current source to GitHub and establish a normal branch/PR flow.
2. Run the M4 playtest with 5 to 10 target users.
3. Triage feedback into three buckets:
   desktop interference, cat feeling, and teach/learn clarity.
4. Improve final-ish cat visuals for the three most visible actions first:
   sit, sleep, and click reaction.
5. Decide the next product bet only after feedback:
   identity/personalization, stronger behavior learning, or desktop polish.

Do not jump straight to photo generation, multi-cat, mobile, or devices before
the playtest answers whether a single desktop cat already feels worth keeping.

## Temporary commit trail

The prepared temporary publish branch currently contains these milestones:

```text
d270116 add playtest packaging flow
4c0cd3c label playtest scope
9a9b170 prepare m4 playtest prototype
7320369 add habit learning and quiet mode
8312799 add cat observation recording flow
2d77138 verify desktop cat build setup
97b092b build windows desktop cat prototype
27040dc bootstrap repository
```

