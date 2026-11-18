@echo off
powershell.exe -NoLogo -NoProfile -Command "Get-AppxPackage -Name ThingPilot.Dev | Remove-AppxPackage -ErrorAction Stop; Write-Host 'ThingPilot removed. Your SmartThings account and routines are unchanged. Reload extensions in Command Palette.'"
pause
