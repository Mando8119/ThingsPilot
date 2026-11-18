[CmdletBinding()]
param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
try {
    $payload = Join-Path $PSScriptRoot 'App'
    $manifest = Join-Path $payload 'AppxManifest.xml'
    if (!(Test-Path -LiteralPath $manifest)) { throw 'Extract the entire ZIP first, then run Install.cmd from the extracted folder.' }
    [xml]$package = [IO.File]::ReadAllText($manifest)
    $identity = $package.Package.Identity
    if ($identity.Name -ne 'ThingsPilot.Dev' -or $identity.Publisher -ne 'CN=ThingsPilot Development') {
        throw 'Unexpected package identity. Download the preview again from the official ThingsPilot GitHub release.'
    }
    $version = [Version]$identity.Version
    $architecture = [string]$identity.ProcessorArchitecture
    if ($architecture -notin @('x64', 'arm64')) { throw 'Unsupported package architecture.' }
    $nativeArchitecture = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
    if ($nativeArchitecture -eq 'AMD64' -and $architecture -ne 'x64') { throw 'Download the x64 ZIP for this PC.' }
    if ($nativeArchitecture -notin @('AMD64', 'ARM64')) { throw 'ThingsPilot requires 64-bit Windows 11.' }
    foreach ($file in @('ThingsPilot.exe', 'ThingsPilot.dll', 'ThingsPilot.Core.dll', 'ThingsPilot.runtimeconfig.json', 'coreclr.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $payload $file))) { throw "The download is incomplete: missing $file. Extract the entire ZIP and try again." }
    }
    if ($CheckOnly) { Write-Output "PASS: complete $architecture preview payload, version $version. No installation performed."; exit 0 }
    if ([Environment]::OSVersion.Version.Build -lt 22000) { throw 'ThingsPilot requires Windows 11.' }
    $developerMode = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
    if ($developerMode -ne 1) {
        Start-Process 'ms-settings:developers'
        throw 'Enable Developer Mode in the Windows Settings window that just opened, then run Install.cmd again. This preview does not change that setting for you.'
    }
    $previous = Get-AppxPackage -Name ThingsPilot.Dev
    # Migrate the preview identity that omitted the final s in Things.
    # Keep its files intact so a failed registration can be rolled back.
    $legacyName = ([string]$identity.Name).Replace('Things', 'Thing')
    $legacy = Get-AppxPackage -Name $legacyName
    if ($previous -and [Version]$previous.Version -gt $version) { throw "A newer ThingsPilot ($($previous.Version)) is installed. Download the newest release instead." }
    $localData = [Environment]::GetFolderPath('LocalApplicationData')
    $cacheDirectory = Join-Path $localData 'ThingsPilot'
    $legacyCache = Join-Path $localData ($legacyName.Replace('.Dev', '') + '/routines.json')
    $cache = Join-Path $cacheDirectory 'routines.json'
    if (!(Test-Path -LiteralPath $cache) -and (Test-Path -LiteralPath $legacyCache)) {
        New-Item -ItemType Directory -Path $cacheDirectory -Force | Out-Null
        Copy-Item -LiteralPath $legacyCache -Destination $cache -ErrorAction Stop
    }
    $basePath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ThingsPilot/Versions'
    $destination = Join-Path $basePath ("$version-$architecture-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $payload -Force | Copy-Item -Destination $destination -Recurse -Force
    # Windows can silently keep the old unpacked path on same-version registration.
    # Repair that case explicitly so deleting an old extracted download cannot break the app.
    $repair = $previous -and [Version]$previous.Version -eq $version
    if ($repair) { Remove-AppxPackage -Package $previous.PackageFullName -ErrorAction Stop }
    try {
        if ($legacy) { Remove-AppxPackage -Package $legacy.PackageFullName -ErrorAction Stop }
        Add-AppxPackage -Register (Join-Path $destination 'AppxManifest.xml') -ForceApplicationShutdown -ErrorAction Stop
        $installed = Get-AppxPackage -Name ThingsPilot.Dev
        if (!$installed -or ![string]::Equals($installed.InstallLocation, $destination, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Windows did not register the installed copy. Keep the previous app folder and report this installation error.'
        }
    }
    catch {
        if ($repair -and (Test-Path -LiteralPath (Join-Path $previous.InstallLocation 'AppxManifest.xml'))) {
            Add-AppxPackage -Register (Join-Path $previous.InstallLocation 'AppxManifest.xml') -ForceApplicationShutdown -ErrorAction Continue
        }
        if ($legacy -and !(Get-AppxPackage -Name $legacyName) -and (Test-Path -LiteralPath (Join-Path $legacy.InstallLocation 'AppxManifest.xml'))) {
            Get-AppxPackage -Name ThingsPilot.Dev | Remove-AppxPackage -ErrorAction Continue
            Add-AppxPackage -Register (Join-Path $legacy.InstallLocation 'AppxManifest.xml') -ForceApplicationShutdown -ErrorAction Continue
        }
        throw
    }
    Write-Host "ThingsPilot $version installed successfully." -ForegroundColor Green
    Write-Host 'Open Command Palette (Win+Alt+Space), search ThingsPilot, choose Open routines, then Refresh from SmartThings.'
    Write-Host 'If it is missing: Command Palette Settings > Extensions > Installed > More options > Reload extensions.'
    Write-Host 'You can now delete the extracted ZIP folder. Routine data stays on your PC.'
    if (!(Get-AppxPackage -Name Microsoft.CommandPalette)) { Write-Host 'Install Microsoft PowerToys and enable Command Palette first: https://aka.ms/powertoys' -ForegroundColor Yellow }
    if (!(Get-AppxPackage -Name SAMSUNGELECTRONICSCO.LTD.SmartThingsWindows)) { Write-Host 'Install Samsung SmartThings from Microsoft Store, sign in, and create a manually run routine first.' -ForegroundColor Yellow }
}
catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }
