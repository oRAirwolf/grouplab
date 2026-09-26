<#
.SYNOPSIS
    Builds GroupLab as an MSIX package for the Microsoft Store, NOTES-FROM-PLANNING.md entry 224 section 3.

.DESCRIPTION
    The same self-contained win-x64 build as the zip and the installer, stamped as the Store's copy (-p:GroupLabDistribution=store), so
    GroupLab's own updater is off in it and the Store updates it. packaging\msix\AppxManifest.xml is filled with the identity Partner Center
    gives when the name GroupLab is reserved; until that exists, CI builds it with a stand-in identity, which proves the package builds but
    could not be submitted. The Store signs the package when it is submitted, so it is left unsigned here.

    The version is four numbers with the last one 0, as the Store requires: 0.2.0 for a release, and 0.2.<patch*1000+N> for nightly N,
    which is never submitted.

    Needs makeappx.exe from the Windows SDK, which the Windows runners have.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Version,
    [string] $Train = 'development',
    [string] $Output = 'out/package',
    [string] $IdentityName = 'GroupLab.StandIn',
    [string] $Publisher = 'CN=GroupLabStandIn',
    [string] $PublisherDisplayName = 'GroupLab'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'NativeCommand.ps1')
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)(?:-nightly\.(\d+))?') { throw "$Version is not a version this script can turn into four numbers" }
    $third = if ($Matches[4]) { [int]$Matches[3] * 1000 + [int]$Matches[4] } else { [int]$Matches[3] }
    $msixVersion = "$($Matches[1]).$($Matches[2]).$third.0"

    $staging = Join-Path $Output 'msix-staging'
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $Output | Out-Null
    $built = Invoke-Native dotnet publish src/GroupLab.App --configuration Release -r win-x64 --self-contained -o $staging `
        "-p:Version=$Version" "-p:GroupLabTrain=$Train" '-p:GroupLabDistribution=store'
    if ($built.ExitCode -ne 0) { throw "dotnet publish failed:`n$(($built.Output + $built.Errors) -join "`n" | Select-Object -Last 40)" }

    # The tiles, from the application's own icon, at the sizes the manifest names.
    Add-Type -AssemblyName System.Drawing
    $assets = New-Item -ItemType Directory -Force -Path (Join-Path $staging 'Assets')
    $icon = Resolve-Path 'src/GroupLab.App/Assets/icons/grouplab.ico'
    foreach ($tile in @(@{ Name = 'StoreLogo.png'; W = 50; H = 50 }, @{ Name = 'Square44x44Logo.png'; W = 44; H = 44 },
                        @{ Name = 'Square150x150Logo.png'; W = 150; H = 150 }, @{ Name = 'Wide310x150Logo.png'; W = 310; H = 150 })) {
        $source = (New-Object System.Drawing.Icon($icon.Path, 256, 256)).ToBitmap()
        $canvas = New-Object System.Drawing.Bitmap($tile.W, $tile.H)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $side = [Math]::Min($tile.W, $tile.H)
        $g.DrawImage($source, [int](($tile.W - $side) / 2), [int](($tile.H - $side) / 2), $side, $side)
        $canvas.Save((Join-Path $assets $tile.Name), [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $canvas.Dispose(); $source.Dispose()
    }

    $manifest = (Get-Content 'packaging/msix/AppxManifest.xml' -Raw).
        Replace('{IdentityName}', [Security.SecurityElement]::Escape($IdentityName)).
        Replace('{Publisher}', [Security.SecurityElement]::Escape($Publisher)).
        Replace('{PublisherDisplayName}', [Security.SecurityElement]::Escape($PublisherDisplayName)).
        Replace('{Version}', $msixVersion)
    [IO.File]::WriteAllText((Join-Path $staging 'AppxManifest.xml'), $manifest, (New-Object System.Text.UTF8Encoding($false)))

    $makeappx = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe' -ErrorAction SilentlyContinue |
        Sort-Object FullName | Select-Object -Last 1
    if (-not $makeappx) { throw 'makeappx.exe is not here; it comes with the Windows SDK, which the Windows runners have' }
    $msix = Join-Path $Output 'grouplab-win-x64.msix'
    $packed = Invoke-Native $makeappx.FullName pack /d $staging /p $msix /o
    if ($packed.ExitCode -ne 0) { throw "makeappx failed:`n$(($packed.Output + $packed.Errors) -join "`n" | Select-Object -Last 40)" }
    Remove-Item $staging -Recurse -Force
    Write-Host ("{0}: {1:N1} MB, version {2}, identity {3}" -f $msix, ((Get-Item $msix).Length / 1MB), $msixVersion, $IdentityName)
}
finally {
    Pop-Location
}
