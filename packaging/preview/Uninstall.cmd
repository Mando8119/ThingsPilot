@echo off
powershell.exe -NoLogo -NoProfile -Command "Get-AppxPackage -Name ThingsPilot.Dev | Remove-AppxPackage -ErrorAction Stop; Write-Host 'ThingsPilot removed. Your SmartThings account and routines are unchanged. Reload extensions in Command Palette.'"
pause
