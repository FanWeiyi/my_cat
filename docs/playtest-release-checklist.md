# My Cat V0.5 Playtest Release Checklist

1. Confirm the app About dialog, README, PLAYTEST, guide, feedback form, and release checklist say `V0.5 Playtest`.
2. Run `dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj --no-restore`.
3. Run `dotnet build .\windows-shell\MyCat.WindowsShell.csproj --no-restore`.
4. Run `python .\scripts\stabilize-cat-art.py --check`.
5. Run `scripts\package-playtest.cmd`.
6. If a restricted network blocks self-contained runtime packs, run `scripts\package-playtest.cmd -FrameworkDependent`.
7. Confirm the ZIP is `artifacts\playtest\MyCat-v0.5-playtest-win-x64.zip`.
8. Extract the ZIP and start `MyCat.WindowsShell.exe`.
9. Confirm `cats\my-cat\manifest.json` and all runtime `frame_*.png` files are present in the extracted package.
10. Confirm the personalized PNG cat appears instead of a placeholder drawing.
11. Confirm click, drag and safe drop, cat menu, tray recording, quiet mode, behavior rhythm settings, About, data folder, log folder, and tray exit.
12. Confirm `%LOCALAPPDATA%\MyCat\logs` contains startup and exit logs after a smoke pass.
13. Pass the mouse near the cat and wait for one window-side stay with quiet mode off.
14. Confirm "tell it" keeps the record flow and gives an immediate cat response.
15. Watch one left walk and one right walk so the directional art sequences are visible.
16. Confirm idle sitting no longer slides side-to-side while blinking or moving the tail.
17. Confirm `%LOCALAPPDATA%\MyCat\interaction-metrics.json` updates locally after the smoke pass.
18. Leave one real desktop run open for 30 minutes before sharing broadly.
19. Create GitHub Pre-release `My Cat V0.5 Playtest` and upload the ZIP.
