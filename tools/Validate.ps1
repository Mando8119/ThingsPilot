[CmdletBinding()]
param([string]$Dotnet = '', [switch]$ProbeSmartThings, [switch]$SkipPackaging)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$Dotnet) {
    $userDotnet = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $userDotnet) { $userDotnet } else { 'dotnet' }
}
Push-Location $repoRoot
try {
    & $Dotnet test tests/ThingPilot.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    if ($ProbeSmartThings) {
        & $Dotnet run --project tests/ThingPilot.UiProbe -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Live discovery probe failed.' }
    }
    if (!$SkipPackaging) {
        & "$PSScriptRoot/Bundle.ps1" -Dotnet $Dotnet
        $build = Get-Content artifacts/latest-x64.json -Raw | ConvertFrom-Json
        & $Dotnet run --project tests/ThingPilot.SmokeHost -c Release -- "$($build.stage)/ThingPilot.exe"
        if ($LASTEXITCODE -ne 0) { throw 'Out-of-process COM smoke failed.' }
    }
    Get-Content packaging/gallery/extension.template.json -Raw | ConvertFrom-Json | Out-Null
    Get-ChildItem -Path README.md,docs -Filter *.md -Recurse | ForEach-Object {
        $file = $_
        foreach ($link in [regex]::Matches([IO.File]::ReadAllText($file.FullName), '\[[^\]]+\]\((?!https?://|#)([^)]+)\)')) {
            $target = $link.Groups[1].Value.Split('#')[0]
            if ($target -and !(Test-Path -LiteralPath (Join-Path $file.DirectoryName $target))) {
                throw "Broken documentation link in $($file.Name): $target"
            }
        }
    }
    Write-Output 'PASS: local validation. No routine was triggered and nothing was published.'
}
finally { Pop-Location }
