# Playtest Release Checklist

1. Build the solution and run the behavior checks.
2. Run `scripts\package-playtest.cmd`.
3. If a restricted network blocks self-contained runtime packs, run
   `scripts\package-playtest.cmd -FrameworkDependent`.
4. Open the packaged folder and start `MyCat.WindowsShell.exe`.
5. Confirm click, cat menu, tray recording, quiet mode, and tray exit.
6. Leave one real desktop run open for 30 minutes before sharing broadly.
7. Send the ZIP with the playtest guide and feedback form.
