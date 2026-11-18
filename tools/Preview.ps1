[CmdletBinding()]
param([string]$Version = '0.1.2.0', [string]$Dotnet = '', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) { & "$PSScriptRoot/Bundle.ps1" -Version $Version -Dotnet $Dotnet }
$artifacts = Join-Path $repoRoot 'artifacts'
$zips = @()
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($architecture in @('x64', 'arm64')) {
    $build = Get-Content -LiteralPath (Join-Path $artifacts "latest-$architecture.json") -Raw | ConvertFrom-Json
    if ($build.version -ne $Version -or $build.architecture -ne $architecture) { throw 'Build metadata does not match the requested release.' }
    $staging = Join-Path $artifacts ('preview-' + $architecture + '-' + [Guid]::NewGuid().ToString('N'))
    $payload = Join-Path $staging 'App'
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    Get-ChildItem -LiteralPath $build.stage -Force | Copy-Item -Destination $payload -Recurse -Force
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'packaging/preview') -File | Copy-Item -Destination $staging
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $staging
    $zip = Join-Path $artifacts "ThingPilot-$Version-$architecture.zip"
    if (Test-Path -LiteralPath $zip) { throw "Release ZIP already exists; choose a new version or explicitly archive it before rebuilding: $zip" }
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
    $zips += $zip
}
$hashes = Get-FileHash -LiteralPath $zips -Algorithm SHA256
$hashes | ForEach-Object { $_.Hash + '  ' + (Split-Path $_.Path -Leaf) } |
    Set-Content -LiteralPath (Join-Path $artifacts 'PREVIEW-SHA256SUMS.txt') -Encoding ascii
$hashes | Format-Table -AutoSize
