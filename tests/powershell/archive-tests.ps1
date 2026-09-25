<#
.SYNOPSIS
    The archive scripts against a stand-in gh, NOTES-FROM-PLANNING.md entry 220, under whichever PowerShell runs this file.

.DESCRIPTION
    Request 31's first real pull stopped at its first archive call: under Windows PowerShell 5.1, with $ErrorActionPreference set to
    Stop, gh's "release not found" on stderr for a month with no release yet became a terminating error. This runs that case for real,
    against tests/powershell/fake-gh.py, which says the same thing on stderr, and it is run under both Windows PowerShell 5.1 and
    PowerShell 7 on Windows, here and in CI:

        powershell -NoProfile -ExecutionPolicy Bypass -File tests/powershell/archive-tests.ps1
        pwsh -NoProfile -File tests/powershell/archive-tests.ps1

    Nothing reaches the network, and everything it makes is deleted at the end.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$work = Join-Path ([IO.Path]::GetTempPath()) ("gl-archive-test-" + [guid]::NewGuid().ToString('N'))
$bin = Join-Path $work 'bin'
$env:FAKE_GH_STATE = Join-Path $work 'releases'
New-Item -ItemType Directory -Path $bin, $env:FAKE_GH_STATE | Out-Null
$python = (Get-Command python -ErrorAction Stop).Source
Set-Content -Path (Join-Path $bin 'gh.cmd') -Encoding ascii -Value "@`"$python`" `"$(Join-Path $PSScriptRoot 'fake-gh.py')`" %*"
$savedPath = $env:PATH
$env:PATH = "$bin;$env:PATH"

$failed = @()
function Check([string] $What, [bool] $Ok) {
    if ($Ok) { Write-Host "ok   $What" } else { Write-Host "FAIL $What"; $script:failed += $What }
}

try {
    . (Join-Path $repo 'scripts\SubmissionArchive.ps1')
    Check 'the stand-in gh is the one found' ((Get-Command gh).Source -like "$bin*")

    $raw = Invoke-Native gh release view archive-2026-10 -R test/archive
    Check 'gh really says "release not found" on stderr, and it is not fatal' ($raw.ExitCode -eq 1 -and ($raw.Errors -join ' ') -match 'release not found')

    Check 'the archive repository is seen as private' ((Test-ArchiveRepository -Repo test/archive) -eq $true)

    $folder = Join-Path $work '2026-10-01_abcdef12'
    New-Item -ItemType Directory -Path $folder | Out-Null
    Set-Content -Path (Join-Path $folder 'meta.json') -Value '{"consent":{"version":"consent_v2","level":"testing"}}'
    Set-Content -Path (Join-Path $folder 'image.jpg') -Value 'not really a picture'

    $first = Add-ToArchive -Folder $folder -Repo test/archive
    Check 'a month with no release yet: the release is made and the submission archived and proven' ($first -eq $true)
    $month = Join-Path $env:FAKE_GH_STATE 'archive-2026-10'
    Check 'the zip and the manifest are in the month''s release' ((Test-Path (Join-Path $month '2026-10-01_abcdef12.zip')) -and (Test-Path (Join-Path $month 'manifest.json')))
    $manifest = Get-Content (Join-Path $month 'manifest.json') -Raw | ConvertFrom-Json
    Check 'the manifest lists it with its consent level' (@($manifest.submissions).Count -eq 1 -and $manifest.submissions[0].consent -eq 'testing')

    $again = Add-ToArchive -Folder $folder -Repo test/archive
    Check 'archiving it again proves the copy already there instead of adding another' ($again -eq $true -and @((Get-Content (Join-Path $month 'manifest.json') -Raw | ConvertFrom-Json).submissions).Count -eq 1)

    Check 'the month''s manifest is read back' (@(Get-ArchiveManifest -Tag archive-2026-10 -Repo test/archive).Count -eq 1)
    Check 'a month with no release reads as an empty manifest, not an error' (@(Get-ArchiveManifest -Tag archive-2027-01 -Repo test/archive).Count -eq 0)
}
catch {
    $failed += "threw: $_"
    Write-Host "FAIL threw: $_"
}
finally {
    $env:PATH = $savedPath
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "archive tests under PowerShell $($PSVersionTable.PSVersion): $(@($failed).Count) failed"
exit $(if (@($failed).Count) { 1 } else { 0 })
