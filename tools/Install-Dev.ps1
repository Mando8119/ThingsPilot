[CmdletBinding()]
param([string]$StagingDirectory = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$StagingDirectory) {
    $architecture = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'arm64' } else { 'x64' }
    $StagingDirectory = Get-ChildItem (Join-Path $repoRoot 'artifacts') -Directory -Filter "stage-$architecture-*" |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (!$StagingDirectory) { throw 'Run tools/Validate.ps1 to build the development packages first.' }
$manifest = Join-Path $StagingDirectory 'AppxManifest.xml'
if (!(Test-Path -LiteralPath $manifest)) { throw 'The staging directory must contain AppxManifest.xml.' }
[xml]$identity = Get-Content -LiteralPath $manifest
if ($identity.Package.Identity.Name -ne 'ThingPilot.Dev') { throw 'This helper only registers the ThingPilot.Dev development identity.' }
$developerMode = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
if ($developerMode -ne 1) { throw 'Enable Windows Developer Mode in Settings, then run this helper again.' }
Add-AppxPackage -Register $manifest -ErrorAction Stop
Get-AppxPackage ThingPilot.Dev | Select-Object Name, Version, PackageFamilyName, Status
Write-Output 'Registered ThingPilot locally. Reload extensions in Command Palette settings. Keep the staging directory in place.'
