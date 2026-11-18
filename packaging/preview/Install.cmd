@echo off
echo ThingsPilot preview installer
echo Installs for your Windows account. No administrator prompt or certificate import.
echo Only run this installer if you trust the GitHub release you downloaded.
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
set "ThingsPilotExitCode=%errorlevel%"
echo.
pause
exit /b %ThingsPilotExitCode%
