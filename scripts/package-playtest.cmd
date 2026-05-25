@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0package-playtest.ps1" %*
set exit_code=%errorlevel%
endlocal
exit /b %exit_code%
