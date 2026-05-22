# Playtest Release Checklist

1. Build the solution and run the behavior checks.
2. Run `scripts\package-playtest.cmd`.
3. If a restricted network blocks self-contained runtime packs, run
   `scripts\package-playtest.cmd -FrameworkDependent`.
4. Open the packaged folder and start `MyCat.WindowsShell.exe`.
5. Confirm click, drag and safe drop, cat menu, tray recording, quiet mode, and tray exit.
6. Pass the mouse near the cat and wait for one window-side stay with quiet mode off.
7. Confirm "tell it" keeps the record flow and now gives an immediate cat response.
8. Confirm `%LOCALAPPDATA%\MyCat\interaction-metrics.json` updates locally after the smoke pass.
9. Leave one real desktop run open for 30 minutes before sharing broadly.
10. Send the ZIP with the playtest guide and feedback form.
