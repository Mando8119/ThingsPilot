[CmdletBinding()]
param(
    [string]$IdentityName = 'ThingsPilot.Dev',
    [string]$Publisher = 'CN=ThingsPilot Development',
    [string]$PublisherDisplayName = 'ThingsPilot Development',
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version = '0.1.3.0',
    [string]$Dotnet = '',
    [string]$CertificateThumbprint = '',
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$makeAppx = Get-ChildItem -LiteralPath $sdkRoot -Filter makeappx.exe -Recurse |
    Where-Object FullName -Match '\\x64\\' | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (!$makeAppx) { throw 'Install the Windows SDK with MakeAppx.' }
foreach ($architecture in @('x64', 'arm64')) {
    & "$PSScriptRoot/Package.ps1" -Architecture $architecture -IdentityName $IdentityName `
        -Publisher $Publisher -PublisherDisplayName $PublisherDisplayName -Version $Version -Dotnet $Dotnet -MakeAppx $makeAppx
}
$artifacts = Join-Path $repoRoot 'artifacts'
$bundleInput = Join-Path $artifacts ('bundle-input-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $bundleInput | Out-Null
$packages = @('x64', 'arm64') | ForEach-Object { Join-Path $artifacts "ThingsPilot-$Version-$_.msix" }
foreach ($package in $packages) { Copy-Item -LiteralPath $package -Destination $bundleInput }
$bundle = Join-Path $artifacts "ThingsPilot-$Version.msixbundle"
& $makeAppx bundle /d $bundleInput /p $bundle /bv $Version /o
if ($LASTEXITCODE -ne 0) { throw 'Bundle validation failed.' }
if ($CertificateThumbprint) {
    $signTool = Join-Path (Split-Path $makeAppx -Parent) 'signtool.exe'
    foreach ($package in @($packages) + @($bundle)) {
        & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $package
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $package" }
        & $signTool verify /pa /v $package
        if ($LASTEXITCODE -ne 0) { throw "Signature verification failed: $package" }
    }
}
$hashes = Get-FileHash -LiteralPath (@($packages) + @($bundle)) -Algorithm SHA256 -ErrorAction Stop
$hashes | ForEach-Object { $_.Hash + '  ' + (Split-Path $_.Path -Leaf) } |
    Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ascii
$hashes | Format-Table -AutoSize
Write-Output "Bundle: $bundle"
if (!$CertificateThumbprint) { Write-Output 'Unsigned development output. Sign before public distribution.' }
