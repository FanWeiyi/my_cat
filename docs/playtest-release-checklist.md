# Playtest Release Checklist

1. Build the solution and run the behavior checks.
2. Run `python .\scripts\stabilize-cat-art.py --check`.
3. Run `scripts\package-playtest.cmd`.
4. If a restricted network blocks self-contained runtime packs, run
   `scripts\package-playtest.cmd -FrameworkDependent`.
5. Open the packaged folder and start `MyCat.WindowsShell.exe`.
6. Confirm the personalized PNG cat appears instead of the placeholder drawing.
7. Confirm click, drag and safe drop, cat menu, tray recording, quiet mode, and tray exit.
8. Pass the mouse near the cat and wait for one window-side stay with quiet mode off.
9. Confirm "tell it" keeps the record flow and now gives an immediate cat response.
10. Watch one left walk and one right walk so the directional art sequences are visible.
11. Confirm idle sitting no longer slides side-to-side while blinking or moving the tail.
12. Confirm `%LOCALAPPDATA%\MyCat\interaction-metrics.json` updates locally after the smoke pass.
13. Leave one real desktop run open for 30 minutes before sharing broadly.
14. Send the ZIP with the playtest guide and feedback form.
