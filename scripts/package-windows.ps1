<#
.SYNOPSIS
    Builds GroupLab's Windows package: the zip, and the installer where Inno Setup is available.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 116 sections 1, 2 and 2a. It publishes GroupLab self-contained for win-x64, so the machine it runs on needs
    no .NET; puts the licence, the notices and a README beside it, because a loose executable loses all three; makes the samples a tester can
    open in the first minute; and zips it under a name that carries the version and the commit, so a bug report names a build.

    Nothing donated goes into the package. Both samples are Alan's own sheets, published from this repository: the shot one is
    samples/gl-cf25-ltr-d-25-shots-600-dpi.png, whose consent record is samples/PROVENANCE.md, and the unshot one is a print-quality scan
    from scans/phase0. NOTES-FROM-PLANNING.md entry 120 section 9 is where the consent was given.

    It also writes copies under stable names, grouplab-win-x64.zip and grouplab-setup-win-x64.exe, which the release workflow attaches so the
    README's download links keep working without anyone editing the README after a release.

.PARAMETER Version
    The version to name the package by. The default is the one in Directory.Build.props.

.PARAMETER Commit
    The short commit the package is built from. The default is the current HEAD.

.PARAMETER Output
    Where to put the package. The default is out/package.

.PARAMETER Train
    The update train this build belongs to: nightly, beta or release. The default, development, is what a build made on somebody's own
    machine is, and such a build never offers to update itself.

.PARAMETER SkipInstaller
    Build the zip only, even where Inno Setup is installed.

.PARAMETER RequireInstaller
    Fail where Inno Setup is not installed, rather than building the zip alone. The release workflow passes it, because both assets ship.
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Commit,
    [string]$Train = "development",
    [string]$Output = "out/package",
    [switch]$SkipInstaller,
    [switch]$RequireInstaller
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Set-Location $repository

if (-not $Version) {
    $props = Get-Content 'Directory.Build.props' -Raw
    if ($props -notmatch '<Version>([^<]+)</Version>') { throw 'Directory.Build.props has no <Version>.' }
    $Version = $Matches[1]
}

if (-not $Commit) {
    $Commit = (git rev-parse --short HEAD).Trim()
}

$date = (Get-Date).ToString('yyyy-MM-dd')
$staging = Join-Path $Output 'grouplab'
Write-Output "GroupLab $Version, commit $Commit, into $Output"

if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

# The application, self-contained: every .NET file it needs, and the OpenCV native library, travel with it.
dotnet publish src/GroupLab.App --configuration Release -r win-x64 --self-contained -o $staging "-p:Version=$Version" "-p:GroupLabTrain=$Train"
if ($LASTEXITCODE -ne 0) { throw 'The publish failed.' }

# GPL-3.0 section 4: the licence travels with the binary, and the notices say what else is in it.
Copy-Item 'LICENSE' (Join-Path $staging 'LICENSE') -Force
Copy-Item 'THIRD-PARTY-NOTICES.md' (Join-Path $staging 'THIRD-PARTY-NOTICES.md') -Force
$readme = (Get-Content 'packaging/windows/README.txt.template' -Raw).Replace('{version}', $Version).Replace('{commit}', $Commit).Replace('{date}', $date)
Set-Content -Path (Join-Path $staging 'README.txt') -Value $readme -Encoding UTF8

# Samples, so a tester with no printer, no scanner and no rifle can still see an analysis.
$samples = Join-Path $staging 'samples'
New-Item -ItemType Directory -Path $samples -Force | Out-Null
Copy-Item 'scans/phase0/gl-cf25-ltr-1-300-dpi.png' (Join-Path $samples 'gl-cf25-ltr-300-dpi.png') -Force
Copy-Item 'samples/gl-cf25-ltr-d-25-shots-600-dpi.png' (Join-Path $samples 'gl-cf25-ltr-d-25-shots-600-dpi.png') -Force
Copy-Item 'samples/PROVENANCE.md' (Join-Path $samples 'PROVENANCE.md') -Force

# What the package must carry, checked rather than assumed: the two programs, the runtime, the native OpenCV library, the sheets and the samples.
$required = @(
    'GroupLab.App.exe', 'grouplab.exe', 'hostfxr.dll', 'coreclr.dll', 'System.Private.CoreLib.dll',
    'OpenCvSharpExtern.dll', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.txt',
    'targets/GL-CF25-LTR.gltd.json', 'targets/GL-CF25-LTR-D.gltd.json',
    'samples/gl-cf25-ltr-d-25-shots-600-dpi.png', 'samples/gl-cf25-ltr-300-dpi.png', 'samples/PROVENANCE.md'
)
foreach ($file in $required) {
    $path = Join-Path $staging $file
    if (-not (Test-Path $path)) { throw "The package is missing $file." }
}

# The package's own programs, run from the package: this is the check that its OpenCV native library is there and works, and it needs no
# window. A machine with no .NET runs the same files.
# NOTES-FROM-PLANNING.md entry 120 section 9: the sample's ground truth is the shooter's own account, 25 shots one at each of the 25
# bulls, and that is what the package must read off it. A package that finds a different number has something wrong with it, and finding
# out here is the point: a tester's first minute is not the place to discover it.
$check = Join-Path $Output 'check-analysis.txt'
& (Join-Path $staging 'grouplab.exe') analyze (Join-Path $samples 'gl-cf25-ltr-d-25-shots-600-dpi.png') | Tee-Object -FilePath $check
if ($LASTEXITCODE -ne 0) { throw 'The package could not analyse its own sample.' }
if (-not (Select-String -Path $check -Pattern 'sigma' -Quiet)) { throw 'The analysis of the sample produced no figures.' }
$holes = (Select-String -Path $check -Pattern '(\d+) shots pooled about their own bulls').Matches.Groups[1].Value
if ($holes -ne '25') { throw "The sample reads as $holes shots, and the shooter says 25. samples/sample.json holds the ground truth." }

$zip = Join-Path $Output "grouplab-$Version-win-x64-$Commit.zip"
Compress-Archive -Path $staging -DestinationPath $zip -CompressionLevel Optimal -Force
Copy-Item $zip (Join-Path $Output 'grouplab-win-x64.zip') -Force

$installer = $null
if (-not $SkipInstaller) {
    $iscc = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if (-not $iscc) {
        foreach ($candidate in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
            if (Test-Path $candidate) { $iscc = Get-Command $candidate; break }
        }
    }

    if ($iscc) {
        $full = (Resolve-Path $staging).Path
        $outFull = (Resolve-Path $Output).Path
        & $iscc.Source "/DAppVersion=$Version" "/DAppCommit=$Commit" "/DSourceDir=$full" "/DOutputDir=$outFull" "/DOutputName=grouplab-setup-win-x64" 'packaging/windows/grouplab.iss'
        if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed.' }
        $installer = Join-Path $Output 'grouplab-setup-win-x64.exe'
        Copy-Item $installer (Join-Path $Output "grouplab-setup-$Version-win-x64-$Commit.exe") -Force
    }
    elseif ($RequireInstaller) {
        throw 'Inno Setup is not installed here, and the installer was required. Install Inno Setup 6, or pass -SkipInstaller.'
    }
    else {
        Write-Warning 'Inno Setup is not installed here, so the installer was not built. The zip is what the automated checks produce.'
    }
}

$files = (Get-ChildItem $staging -Recurse -File).Count
$unpacked = [math]::Round((Get-ChildItem $staging -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB)
$zipped = [math]::Round((Get-Item $zip).Length / 1MB)
Write-Output "zip: $zip, $zipped MB, from $files files and $unpacked MB unpacked"
if ($installer) { Write-Output "installer: $installer, $([math]::Round((Get-Item $installer).Length / 1MB)) MB" }
