<#
.SYNOPSIS
    Deletes from the server the submissions and crash reports that have been pulled, verified and ingested.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 129 section 4. Alan's decision: once a submission has been read,
    the server keeps nothing. A photograph somebody sent sitting on a web server for months is a
    risk nobody agreed to, and the copy that matters is the one on Alan's machine.

    **It deletes only what the ledger says was ingested, and only after re-verifying the local copy.**
    The ledger is the record of what happened to each submission: pulled, hashes verified, ingested.
    If the local copy does not still match its own meta.json, the server's copy is the only good one
    left and this refuses to touch it.

    Deletion is by exact ID path, one directory at a time. There is no wildcard anywhere in this
    script, because a wildcard against a remote path run as root is how a whole folder disappears.

.PARAMETER Ledger
    The ledger, outside the repository. Defaults to C:\Dev\grouplab-submissions\ledger.json.

.PARAMETER CrashReports
    Do the same for crash reports rather than submissions.

.PARAMETER RemoteRoot
    The folder on the server to remove from. grouplab.org's is /home/airwolf/web/grouplab.org/private/ready.
.PARAMETER WhatIf
    Say what would be deleted and delete nothing.

.EXAMPLE
    ./scripts/Remove-ReadSubmissions.ps1 -WhatIf
    ./scripts/Remove-ReadSubmissions.ps1
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$Ledger = 'C:\Dev\grouplab-submissions\ledger.json',
    [string]$Local = 'C:\Dev\grouplab-submissions',
    [switch]$CrashReports,
    [string]$SshHost = 'ssh.pissinhot.com',
    [string]$SshUser = 'ubuntu',
    [string]$KeyPath = 'C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key',
    [string]$LogPath = 'C:\Dev\grouplab-submissions\removed.log',
    # Entry 173: grouplab.org's receiver keeps what it has read in /home/airwolf/web/grouplab.org/private/ready. Name it here to
    # remove from there; left out, the two pissinhot.com stores are used as before.
    [string]$RemoteRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The two stores on the server. Nothing else is ever touched.
$remoteRoot = if ($RemoteRoot) {
    $RemoteRoot
} elseif ($CrashReports) {
    '/home/airwolf/web/pissinhot.com/private/crash_reports'
} else {
    '/home/airwolf/web/pissinhot.com/private/target_uploads'
}

$kind = if ($CrashReports) { 'crash report' } else { 'submission' }

function Write-Line([string]$message) {
    $line = '{0} {1}' -f (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK'), $message
    Write-Host $line
    $folder = Split-Path -Parent $LogPath
    if ($folder -and -not (Test-Path $folder)) { New-Item -ItemType Directory -Path $folder -Force | Out-Null }
    Add-Content -Path $LogPath -Value $line -Encoding UTF8
}

if (-not (Test-Path $Ledger)) {
    Write-Line "there is no ledger at $Ledger, so nothing is known to have been ingested. Nothing was deleted."
    exit 1
}

if (-not (Test-Path $KeyPath)) {
    Write-Line "the SSH key is not at the path given. Nothing was deleted."
    exit 1
}

$entries = Get-Content -Path $Ledger -Raw -Encoding UTF8 | ConvertFrom-Json
if ($null -eq $entries) { $entries = @() }

# An ID is a date and eight hex characters, and nothing else is ever sent to the server. This is
# the one check that makes "delete by exact path" true: no dots, no slashes, no wildcards.
$shape = '^[0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9a-f]{8}$'

$removed = @()
$skipped = @()

foreach ($entry in $entries) {
    $id = [string]$entry.id

    if ($id -notmatch $shape) {
        $skipped += "$id (not an identifier this script will send anywhere)"
        continue
    }

    if (-not $entry.ingested) {
        $skipped += "$id (not ingested yet)"
        continue
    }

    if ($CrashReports -ne [bool]$entry.crashReport) {
        continue
    }

    # Re-verify the local copy before removing the remote one. If the local copy has rotted, the
    # server's is the only good one left, and deleting it would lose the submission for good.
    $folder = Join-Path $Local $id
    $meta = Join-Path $folder 'meta.json'
    if (-not (Test-Path $meta)) {
        $skipped += "$id (no local meta.json, so its hashes cannot be checked)"
        continue
    }

    $ok = $true
    $record = Get-Content -Path $meta -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($file in $record.files) {
        $path = Join-Path $folder ([string]$file.stored)
        if (-not (Test-Path $path)) { $ok = $false; break }
        $hash = (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne ([string]$file.sha256).ToLowerInvariant()) { $ok = $false; break }
    }

    if (-not $ok) {
        $skipped += "$id (the local copy does not match its own meta.json, so the server's copy is kept)"
        continue
    }

    $remote = "$remoteRoot/$id"
    if ($PSCmdlet.ShouldProcess($remote, "delete the $kind from the server")) {
        # One directory, named in full, with no wildcard and no recursion into anything else.
        $command = "sudo rm -rf -- '$remote'"
        & ssh -i $KeyPath "$SshUser@$SshHost" $command
        if ($LASTEXITCODE -ne 0) {
            $skipped += "$id (the server refused, exit $LASTEXITCODE)"
            continue
        }
        $removed += $id
        Write-Line "removed $kind $id from the server"
    } else {
        Write-Line "would remove $kind $id from the server"
    }
}

Write-Line ("done: {0} removed, {1} left alone" -f $removed.Count, $skipped.Count)
foreach ($s in $skipped) { Write-Line "  left alone: $s" }

if ($removed.Count -gt 0) {
    Write-Host ''
    Write-Host 'Removed from the server:'
    $removed | ForEach-Object { Write-Host "  $_" }
}
