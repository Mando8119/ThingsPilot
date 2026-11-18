[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')][string]$Architecture = 'x64',
    [string]$IdentityName = 'ThingsPilot.Dev',
    [string]$Publisher = 'CN=ThingsPilot Development',
    [string]$PublisherDisplayName = 'ThingsPilot Development',
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version = '0.1.3.0',
    [string]$Dotnet = '',
    [string]$MakeAppx = ''
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$Dotnet) {
    $userDotnet = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $userDotnet) { $userDotnet } else { 'dotnet' }
}
if (!$MakeAppx) {
    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
    $MakeAppx = Get-ChildItem -LiteralPath $sdkRoot -Filter makeappx.exe -Recurse |
        Where-Object FullName -Match '/x64/|\\x64\\' | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (!$MakeAppx -or !(Test-Path -LiteralPath $MakeAppx)) { throw 'Install the Windows SDK, or pass -MakeAppx.' }
# Unique staging paths avoid deleting or overwriting an existing package staging tree.
$artifacts = Join-Path $repoRoot 'artifacts'
$stage = Join-Path $artifacts ("stage-$Architecture-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stage | Out-Null
& $Dotnet publish (Join-Path $repoRoot 'src/ThingsPilot.Extension/ThingsPilot.Extension.csproj') -c Release -r "win-$Architecture" --self-contained true -o $stage -p:GenerateAppxPackageOnBuild=false -p:AppxPackageSigningEnabled=false
if ($LASTEXITCODE -ne 0) { throw 'Extension publish failed.' }
[xml]$manifest = Get-Content -LiteralPath (Join-Path $repoRoot 'src/ThingsPilot.Extension/Package.appxmanifest')
$manifest.Package.Identity.Name = $IdentityName
$manifest.Package.Identity.Publisher = $Publisher
$manifest.Package.Identity.Version = $Version
$manifest.Package.Identity.ProcessorArchitecture = $Architecture
$manifest.Package.Properties.PublisherDisplayName = $PublisherDisplayName
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
New-Item -ItemType Directory -Force (Join-Path $stage 'Public') | Out-Null
'ThingsPilot Command Palette extension' | Set-Content -LiteralPath (Join-Path $stage 'Public/extension.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') -Destination $stage
$package = Join-Path $artifacts "ThingsPilot-$Version-$Architecture.msix"
& $MakeAppx pack /d $stage /p $package /o /v 2>&1 | Set-Content -LiteralPath (Join-Path $artifacts "makeappx-$Architecture.log")
if ($LASTEXITCODE -ne 0) { throw 'MSIX validation/packaging failed.' }
@{ stage = $stage; package = $package; version = $Version; architecture = $Architecture } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts "latest-$Architecture.json") -Encoding UTF8
Get-FileHash -LiteralPath $package -Algorithm SHA256
Write-Output "Unsigned package: $package"
