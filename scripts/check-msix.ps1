<#
.SYNOPSIS
    Checks the Microsoft Store package before anyone uploads it. NOTES-FROM-PLANNING.md entry 293 section 1.

.DESCRIPTION
    1. The identity. Reads AppxManifest.xml out of the package and compares its Identity Name, its Publisher and its PublisherDisplayName
       with STORE_IDENTITY_NAME, STORE_PUBLISHER and STORE_PUBLISHER_DISPLAY_NAME from the environment, which release.yml fills from the
       repository's variables. A mismatch fails, naming the field; the values themselves are not printed.
    2. The Windows App Certification Kit, with -Certify, if it is installed. The kit installs the package to test it, and Windows installs
       only a signed package, so it tests a copy signed with a certificate made on the spot for the same publisher and trusted on this
       machine alone. The copy and the certificate are thrown away; the package the Store receives stays unsigned, as the Store signs it.
       The kit's verdict is reported, with the name of any test that failed, and does not fail the run: its report is uploaded to look at.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Msix,
    [switch] $Certify,
    [string] $Report = 'out/wack-report.xml'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'NativeCommand.ps1')

function Write-Summary([string] $line) {
    Write-Host $line
    if ($env:GITHUB_STEP_SUMMARY) { $line | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }
}

$Msix = (Resolve-Path $Msix).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($Msix)
try {
    $entry = $zip.GetEntry('AppxManifest.xml')
    if (-not $entry) { throw 'The package holds no AppxManifest.xml.' }
    $reader = New-Object IO.StreamReader($entry.Open())
    try { [xml] $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
}
finally {
    $zip.Dispose()
}

$found = @(
    @{ Field = 'Identity Name'; Variable = 'STORE_IDENTITY_NAME'; Value = $manifest.Package.Identity.Name },
    @{ Field = 'Identity Publisher'; Variable = 'STORE_PUBLISHER'; Value = $manifest.Package.Identity.Publisher },
    @{ Field = 'PublisherDisplayName'; Variable = 'STORE_PUBLISHER_DISPLAY_NAME'; Value = $manifest.Package.Properties.PublisherDisplayName }
)
$wrong = @()
foreach ($f in $found) {
    $want = [Environment]::GetEnvironmentVariable($f.Variable)
    if (-not $want) { $wrong += "$($f.Field): the variable $($f.Variable) is not set" }
    elseif ($f.Value -cne $want) { $wrong += "$($f.Field): the package's differs from the variable $($f.Variable)" }
}
if ($wrong) {
    Write-Summary "### The Store package's identity does not match the repository's variables: $($wrong -join '; ')."
    throw "The Store package's identity does not match: $($wrong -join '; ')."
}
Write-Summary "The Store package's Identity Name, Publisher and PublisherDisplayName match the repository's variables. Version $($manifest.Package.Identity.Version)."

if (-not $Certify) { return }

$appcert = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $appcert) {
    Write-Summary '### The Windows App Certification Kit is not installed on this runner, so it was not run. The Store runs its own checks on submission.'
    return
}
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' -ErrorAction SilentlyContinue | Sort-Object FullName | Select-Object -Last 1
if (-not $signtool) { throw 'signtool.exe is not here; it comes with the Windows SDK, as the certification kit does' }

$work = Join-Path ([IO.Path]::GetTempPath()) "wack-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $work | Out-Null
$signed = Join-Path $work 'grouplab-wack.msix'
Copy-Item $Msix $signed
$cert = New-SelfSignedCertificate -Type Custom -Subject $manifest.Package.Identity.Publisher -KeyUsage DigitalSignature `
    -FriendlyName 'GroupLab certification kit only' -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
$trusted = $null
try {
    $cer = Join-Path $work 'wack.cer'
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
    $trusted = Import-Certificate -FilePath $cer -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'
    $sign = Invoke-Native $signtool.FullName sign /fd SHA256 /sha1 $cert.Thumbprint /s My $signed
    if ($sign.ExitCode -ne 0) { throw "signtool could not sign the test copy:`n$(($sign.Output + $sign.Errors) -join "`n" | Select-Object -Last 20)" }

    New-Item -ItemType Directory -Force -Path (Split-Path $Report -Parent) | Out-Null
    $Report = Join-Path (Resolve-Path (Split-Path $Report -Parent)).Path (Split-Path $Report -Leaf)
    if (Test-Path $Report) { Remove-Item $Report -Force }
    Invoke-Native $appcert.FullName reset | Out-Null
    $run = Invoke-Native $appcert.FullName test -appxpackagepath $signed -reportoutputpath $Report
    if (-not (Test-Path $Report)) {
        Write-Summary "### The Windows App Certification Kit ran but wrote no report (exit $($run.ExitCode))."
        ($run.Output + $run.Errors) | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
        return
    }
    [xml] $result = Get-Content $Report -Raw
    $tests = @($result.SelectNodes('//TEST'))
    $failed = @($tests | Where-Object { $_.SelectSingleNode('RESULT').InnerText.Trim() -eq 'FAIL' } | ForEach-Object { $_.NAME })
    $overall = $result.REPORT.OVERALL_RESULT
    Write-Summary "### The Windows App Certification Kit: $overall, $($tests.Count) tests, $($failed.Count) failed$(if ($failed) { ': ' + ($failed -join '; ') })."
}
finally {
    if ($trusted) { Remove-Item "Cert:\LocalMachine\TrustedPeople\$($trusted.Thumbprint)" -ErrorAction SilentlyContinue }
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -ErrorAction SilentlyContinue
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
