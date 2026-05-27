@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-shortcuts.ps1" %*
set exit_code=%errorlevel%
echo.
if %exit_code% EQU 0 (
  echo My Cat shortcuts are ready.
) else (
  echo My Cat shortcut setup failed with exit code %exit_code%.
)
pause
endlocal
exit /b %exit_code%
