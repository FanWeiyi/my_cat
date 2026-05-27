# My Cat V0.5 Playtest Release Checklist

1. Confirm the app About dialog, README, PLAYTEST, guide, feedback form, and release checklist say `V0.5 Playtest`.
2. Run `dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj --no-restore`.
3. Run `dotnet build .\windows-shell\MyCat.WindowsShell.csproj --no-restore`.
4. Run `python .\scripts\stabilize-cat-art.py --check`.
5. Run `scripts\package-playtest.cmd`.
6. If a restricted network blocks self-contained runtime packs, run `scripts\package-playtest.cmd -FrameworkDependent`.
7. Confirm the ZIP is `artifacts\playtest\MyCat-v0.5-playtest-win-x64.zip`.
8. Confirm the ZIP contains `Install-MyCat.cmd`, `install-shortcuts.ps1`, `PLAYTEST.txt`, the guide, and the feedback form.
9. Extract the ZIP and run `Install-MyCat.cmd`.
10. Confirm desktop and Start Menu shortcuts are created and launch My Cat.
11. Confirm the exe, shortcuts, and tray icon use the My Cat app icon.
12. Confirm `cats\my-cat\manifest.json` and all runtime `frame_*.png` files are present in the extracted package.
13. Confirm the personalized PNG cat appears instead of a placeholder drawing.
14. Confirm click, drag and safe drop, cat menu, tray recording, quiet mode, behavior rhythm settings, About, data folder, log folder, and tray exit.
15. Confirm `%LOCALAPPDATA%\MyCat\logs` contains startup and exit logs after a smoke pass.
16. Pass the mouse near the cat and wait for one window-side stay with quiet mode off.
17. Confirm "tell it" keeps the record flow and gives an immediate cat response.
18. Watch one left walk and one right walk so the directional art sequences are visible.
19. Confirm idle sitting no longer slides side-to-side while blinking or moving the tail.
20. Confirm `%LOCALAPPDATA%\MyCat\interaction-metrics.json` updates locally after the smoke pass.
21. Leave one real desktop run open for 30 minutes before sharing broadly.
22. Create GitHub Pre-release `My Cat 可试玩预发布版` and upload the ZIP.
