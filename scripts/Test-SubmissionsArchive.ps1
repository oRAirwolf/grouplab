<#
.SYNOPSIS
    Proves the private archive of submissions can be restored: every submission downloaded, checked and unpacked.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 216 section 6, with entry 217's release assets. For every monthly release in the archive, downloads
    its manifest and every submission's zip into a temporary folder, checks each zip's SHA-256 against the manifest, unpacks it, and
    checks every file against the submission's own meta.json with the same check the pull uses. Deletes the temporary folder whatever
    happens. Run once when the archive is first filled, and then whenever you want to know it is sound. Uses the archive's download
    allowance, so CI never runs it.

.EXAMPLE
    .\Test-SubmissionsArchive.ps1
#>
[CmdletBinding()]
param([string] $ArchiveRepo = 'oRAirwolf/grouplab-submissions-archive')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SubmissionCheck.ps1')
. (Join-Path $PSScriptRoot 'SubmissionArchive.ps1')

$ready = Test-ArchiveRepository -Repo $ArchiveRepo
if ($ready -ne $true) { throw $ready }

$work = Join-Path ([IO.Path]::GetTempPath()) ("gl-restore-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$good = 0; $bad = @()
try {
    $listed = Invoke-Native gh release list -R $ArchiveRepo --limit 1000 --json tagName
    if ($listed.ExitCode -ne 0) { throw "gh could not list the archive's releases: $($listed.Errors -join ' ')" }
    # Windows PowerShell 5.1 passes a JSON array down the pipeline as one object, so it is unrolled first (entry 222: this read 0 months).
    $releases = ($listed.Output -join "`n") | ConvertFrom-Json
    $tags = @(@($releases) | ForEach-Object { $_ } | ForEach-Object { $_.tagName } | Where-Object { $_ -like 'archive-*' })
    foreach ($tag in $tags) {
        $manifest = @(Get-ArchiveManifest -Tag $tag -Repo $ArchiveRepo)
        if (-not $manifest.Count) { $bad += "$tag : no manifest"; continue }
        foreach ($entry in $manifest) {
            $dir = Join-Path $work $tag
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
            Invoke-Native gh release download $tag -R $ArchiveRepo -p "$($entry.name).zip" -D $dir | Out-Null
            $zip = Join-Path $dir "$($entry.name).zip"
            if (-not (Test-Path $zip)) { $bad += "$($entry.name) : could not be downloaded"; continue }
            if ((Get-Sha256 $zip) -ne "$($entry.sha256)".ToLower()) { $bad += "$($entry.name) : the zip does not match the manifest"; continue }
            $unpacked = Join-Path $dir $entry.name
            Expand-Archive -Path $zip -DestinationPath $unpacked
            $r = Test-SubmissionFolder -Folder $unpacked
            if (@($r.Bad).Count) { $bad += @($r.Bad | ForEach-Object { "$($entry.name) : $_" }) } else { $good++ }
            Remove-Item $zip, $unpacked -Recurse -Force
        }
    }
}
finally { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host "$good submission(s) restored and verified, $($bad.Count) problem(s), across $($tags.Count) month(s)." -ForegroundColor $(if ($bad.Count) { 'Red' } else { 'Green' })
$bad | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
if ($bad.Count) { exit 1 }
