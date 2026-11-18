[CmdletBinding()]
param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
try {
    $payload = Join-Path $PSScriptRoot 'App'
    $manifest = Join-Path $payload 'AppxManifest.xml'
    if (!(Test-Path -LiteralPath $manifest)) { throw 'Extract the entire ZIP first, then run Install.cmd from the extracted folder.' }
    [xml]$package = [IO.File]::ReadAllText($manifest)
    $identity = $package.Package.Identity
    if ($identity.Name -ne 'ThingPilot.Dev' -or $identity.Publisher -ne 'CN=ThingPilot Development') {
        throw 'Unexpected package identity. Download the preview again from the official ThingPilot GitHub release.'
    }
    $version = [Version]$identity.Version
    $architecture = [string]$identity.ProcessorArchitecture
    if ($architecture -notin @('x64', 'arm64')) { throw 'Unsupported package architecture.' }
    $nativeArchitecture = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
    if ($nativeArchitecture -eq 'AMD64' -and $architecture -ne 'x64') { throw 'Download the x64 ZIP for this PC.' }
    if ($nativeArchitecture -notin @('AMD64', 'ARM64')) { throw 'ThingPilot requires 64-bit Windows 11.' }
    foreach ($file in @('ThingPilot.exe', 'ThingPilot.dll', 'ThingPilot.Core.dll', 'ThingPilot.runtimeconfig.json', 'coreclr.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $payload $file))) { throw "The download is incomplete: missing $file. Extract the entire ZIP and try again." }
    }
    if ($CheckOnly) { Write-Output "PASS: complete $architecture preview payload, version $version. No installation performed."; exit 0 }
    if ([Environment]::OSVersion.Version.Build -lt 22000) { throw 'ThingPilot requires Windows 11.' }
    $developerMode = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
    if ($developerMode -ne 1) {
        Start-Process 'ms-settings:developers'
        throw 'Enable Developer Mode in the Windows Settings window that just opened, then run Install.cmd again. This preview does not change that setting for you.'
    }
    $previous = Get-AppxPackage -Name ThingPilot.Dev
    if ($previous -and [Version]$previous.Version -gt $version) { throw "A newer ThingPilot ($($previous.Version)) is installed. Download the newest release instead." }
    $basePath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ThingPilot/Versions'
    $destination = Join-Path $basePath ("$version-$architecture-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $payload -Force | Copy-Item -Destination $destination -Recurse -Force
    # Windows can silently keep the old unpacked path on same-version registration.
    # Repair that case explicitly so deleting an old extracted download cannot break the app.
    $repair = $previous -and [Version]$previous.Version -eq $version
    if ($repair) { Remove-AppxPackage -Package $previous.PackageFullName -ErrorAction Stop }
    try {
        Add-AppxPackage -Register (Join-Path $destination 'AppxManifest.xml') -ForceApplicationShutdown -ErrorAction Stop
        $installed = Get-AppxPackage -Name ThingPilot.Dev
        if (!$installed -or ![string]::Equals($installed.InstallLocation, $destination, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Windows did not register the installed copy. Keep the previous app folder and report this installation error.'
        }
    }
    catch {
        if ($repair -and (Test-Path -LiteralPath (Join-Path $previous.InstallLocation 'AppxManifest.xml'))) {
            Add-AppxPackage -Register (Join-Path $previous.InstallLocation 'AppxManifest.xml') -ForceApplicationShutdown -ErrorAction Continue
        }
        throw
    }
    Write-Host "ThingPilot $version installed successfully." -ForegroundColor Green
    Write-Host 'Open Command Palette (Win+Alt+Space), search ThingPilot, choose Open routines, then Refresh from SmartThings.'
    Write-Host 'If it is missing: Command Palette Settings > Extensions > Installed > More options > Reload extensions.'
    Write-Host 'You can now delete the extracted ZIP folder. Routine data stays on your PC.'
    if (!(Get-AppxPackage -Name Microsoft.CommandPalette)) { Write-Host 'Install Microsoft PowerToys and enable Command Palette first: https://aka.ms/powertoys' -ForegroundColor Yellow }
    if (!(Get-AppxPackage -Name SAMSUNGELECTRONICSCO.LTD.SmartThingsWindows)) { Write-Host 'Install Samsung SmartThings from Microsoft Store, sign in, and create a manually run routine first.' -ForegroundColor Yellow }
}
catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
