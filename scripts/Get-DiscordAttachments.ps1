<#
.SYNOPSIS
    Copies the attachments the Discord worker kept on the server to this computer, and deletes the server's copy once each is proven
    here (NOTES-FROM-PLANNING.md entry 419 section 1.4).

.DESCRIPTION
    The worker (website/server/grouplab-discord-worker.py) keeps each message's attachments in a folder of their own with a manifest of
    their SHA-256, written whole and renamed into place. This takes every finished folder, unpacks it under C:\Dev\grouplab-discord,
    checks every file against the manifest, and only then removes the folder from the server. A folder whose files do not match stays on
    the server and is named.

    Since entry 429 the worker also puts each one on its private GitHub issue, on Alan's word of 2026-10-15 (samples/PROVENANCE.md).
    That covers the issues only: these copies are for diagnosis, never put on grouplab.org, in public test data or in a research
    article without the poster's own consent. They are not archived with the submissions, because they are not submissions.

.EXAMPLE
    .\scripts\Get-DiscordAttachments.ps1
#>
[CmdletBinding()]
param(
    [string] $ServerHost = 'ssh.pissinhot.com',
    [string] $ServerUser = 'ubuntu',
    [string] $KeyFile    = 'C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key',
    [string] $RemoteRoot = '/home/airwolf/web/grouplab.org/private/discord/attachments',
    [string] $LocalRoot  = 'C:\Dev\grouplab-discord',
    [switch] $KeepOnServer
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'NativeCommand.ps1')

if (-not (Test-Path $KeyFile)) { throw "SSH key not found at $KeyFile. Pass -KeyFile with the path to the key MobaXterm uses." }
New-Item -ItemType Directory -Force -Path $LocalRoot | Out-Null

$sshArgs = @('-i', $KeyFile, '-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=accept-new', '-o', 'LogLevel=ERROR', "$ServerUser@$ServerHost")

function Invoke-Remote([string] $Command) {
    $r = Invoke-Native ssh @sshArgs $Command
    if ($r.ExitCode -ne 0) { throw "Remote command failed (exit $($r.ExitCode)): $Command`n$($r.Errors -join "`n")" }
    return $r.Output
}

# A missing folder means the worker has kept nothing yet, which is not a failure. Hidden folders are still being written.
$listing = @(Invoke-Remote "sudo sh -c 'test -d $RemoteRoot && ls -1 $RemoteRoot || true'" | Where-Object { $_ -match '^\d+-\d+$' })
if ($listing.Count -eq 0) { Write-Host 'No Discord attachments waiting on the server.'; return }
Write-Host "$($listing.Count) Discord attachment folder(s) on the server."

$kept = @()
foreach ($dir in $listing) {
    $tmp = Join-Path $env:TEMP "discord-$dir.tar"
    $tmpErr = "$tmp.err"
    try {
        $keyQuoted = '"' + $KeyFile + '"'
        $argLine = ($sshArgs | ForEach-Object { if ($_ -eq $KeyFile) { $keyQuoted } else { $_ } }) -join ' '
        $fetched = Invoke-Native cmd /c "ssh $argLine `"sudo tar cf - -C '$RemoteRoot' '$dir'`" > `"$tmp`" 2> `"$tmpErr`""
        if ($fetched.ExitCode -ne 0 -or (Get-Item $tmp).Length -eq 0) { $kept += "$dir : it could not be fetched"; continue }
        $unpacked = Invoke-Native tar xf $tmp -C $LocalRoot
        if ($unpacked.ExitCode -ne 0) { $kept += "$dir : it would not unpack"; continue }

        $manifestPath = Join-Path $LocalRoot "$dir\manifest.json"
        if (-not (Test-Path $manifestPath)) { $kept += "$dir : no manifest"; continue }
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $bad = @($manifest.files | Where-Object {
            $path = Join-Path $LocalRoot "$dir\$($_.name)"
            -not (Test-Path $path) -or (Get-FileHash $path -Algorithm SHA256).Hash.ToLower() -ne "$($_.sha256)".ToLower()
        })
        if ($bad.Count) { $kept += "$dir : $($bad.Count) file(s) do not match the manifest"; continue }

        if (-not $KeepOnServer) { Invoke-Remote "sudo rm -rf -- '$RemoteRoot/$dir'" | Out-Null }
        Write-Host "  $dir : $(@($manifest.files).Count) file(s), checked$(if (-not $KeepOnServer) { ', removed from the server' })"
    }
    finally {
        Remove-Item -LiteralPath $tmp, $tmpErr -ErrorAction SilentlyContinue
    }
}

if ($kept.Count) {
    Write-Warning "Left on the server, because the copy here could not be proven:"
    $kept | ForEach-Object { Write-Warning "  $_" }
    exit 1
}
