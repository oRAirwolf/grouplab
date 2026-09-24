<#
.SYNOPSIS
    Pull new target photo submissions, or GroupLab crash reports, from
    pissinhot.com to this machine.

.DESCRIPTION
    Incremental. Lists the directories on the server, works out which ones are
    not here yet, and pulls only those. Running it twice in a row does nothing
    the second time, so it is safe to run whenever and safe to schedule.

    TWO COLLECTIONS, ONE SCRIPT
    The server holds target photo submissions and, with -CrashReports, GroupLab
    crash reports. They have the same shape on disk: one directory per item, a
    meta.json beside the payload, and a SHA-256 written by the receiver the
    moment the item arrived. So this is one script with a mode switch rather
    than two scripts, because two scripts to remember means one of them quietly
    stops being run.

    Uses the OpenSSH client and tar that ship with Windows 10 1803 and later, so
    it needs nothing installed and does not depend on MobaXterm. It reuses the
    same SSH key MobaXterm uses; the default -KeyFile is
    C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key. That key must be in
    OpenSSH format rather than PuTTY .ppk format; the script checks and says so.

    WHY TAR OVER SSH RATHER THAN SCP -r
    The uploads live in the HestiaCP private directory and are owned by the web
    user, so the ubuntu account cannot read them directly. Piping tar through
    ssh lets sudo do the reading on the far side, which works whatever the
    permissions are, and avoids keeping a second copy on a server whose disk is
    capped. See NOTES at the bottom for making it unattended.

    WHAT IT VERIFIES
    Every submission carries a meta.json with a SHA-256 per file, written when
    the upload arrived. After extracting, this script recomputes those hashes
    locally and reports any that disagree. That closes the loop from the
    contributor's phone all the way to this disk: if a byte changed anywhere in
    between, it says so rather than leaving you to find out later.

.EXAMPLE
    .\Get-TargetSubmissions.ps1

.EXAMPLE
    .\Get-TargetSubmissions.ps1 -WhatIf
    Lists what is new without pulling anything.

.EXAMPLE
    .\Get-TargetSubmissions.ps1 -VerifyAll
    Re-checks the hashes of everything already local, not just the new arrivals.

.EXAMPLE
    .\Get-TargetSubmissions.ps1 -CrashReports
    Pulls crash reports instead of submissions, into C:\Dev\grouplab-crashreports,
    and prints what crashed in each one.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    # ssh.pissinhot.com is a DNS-only record, so it bypasses the Cloudflare proxy
    # and reaches the server. pissinhot.com itself does not: see NOTES.
    # The server's own address is deliberately not written here, because this
    # file is going into a public repository. Pass -ServerHost to override.
    [string] $ServerHost = 'ssh.pissinhot.com',
    [string] $ServerUser = 'ubuntu',
    [string] $KeyFile    = 'C:\Users\Airwolf\Documents\ssh-key-2026-03-25.key',
    # Left empty on purpose: the defaults depend on -CrashReports and are
    # resolved just below, so that passing either one still overrides.
    [string] $RemoteRoot,
    [string] $LocalRoot,

    # Pull crash reports rather than target photo submissions.
    [switch] $CrashReports,

    # Re-verify everything already on disk, not just the new arrivals.
    [switch] $VerifyAll
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SubmissionCheck.ps1')

# ------------------------------------------------------------------- mode --
if ($CrashReports) {
    $itemNoun  = 'crash report'
    $defRemote = '/home/airwolf/web/pissinhot.com/private/crash_reports'
    $defLocal  = 'C:\Dev\grouplab-crashreports'
} else {
    $itemNoun  = 'submission'
    $defRemote = '/home/airwolf/web/pissinhot.com/private/target_uploads'
    $defLocal  = 'C:\Dev\grouplab-submissions'
}
if (-not $RemoteRoot) { $RemoteRoot = $defRemote }
if (-not $LocalRoot)  { $LocalRoot  = $defLocal  }

# ---------------------------------------------------------------- preflight --
foreach ($exe in 'ssh', 'tar') {
    if (-not (Get-Command $exe -ErrorAction SilentlyContinue)) {
        throw "$exe was not found on PATH. On Windows 10 1803 and later both ship in C:\Windows\System32\OpenSSH and C:\Windows\System32. Add the OpenSSH Client optional feature if ssh is missing."
    }
}
if (-not (Test-Path $KeyFile)) {
    throw "SSH key not found at $KeyFile. Pass -KeyFile with the path to the key MobaXterm uses."
}

# MobaXterm can hold either an OpenSSH key or a PuTTY .ppk. ssh.exe reads only
# the first kind, and the error it gives for the second is "invalid format",
# which tells you nothing. Say the useful thing here instead.
$firstLine = (Get-Content $KeyFile -TotalCount 1)
if ($firstLine -match '^PuTTY-User-Key-File') {
    throw "$KeyFile is a PuTTY format key and ssh.exe cannot read it. Open it in PuTTYgen, choose Conversions then Export OpenSSH key, save it beside the original, and point -KeyFile at the exported file."
}
if ($firstLine -notmatch 'BEGIN .*PRIVATE KEY') {
    Write-Warning "$KeyFile does not look like a private key. If this is the public half, the private one is usually the same name without .pub."
}

# Windows OpenSSH refuses a key that other accounts can read. Fixing it is two
# commands, so check rather than letting ssh fail with a wall of asterisks.
$acl = Get-Acl $KeyFile
$others = @($acl.Access | Where-Object {
    $_.IdentityReference.Value -notmatch '\\(Airwolf|SYSTEM|Administrators)$' -and
    $_.IdentityReference.Value -ne $env:USERNAME -and
    $_.IdentityReference.Value -ne "$env:USERDOMAIN\$env:USERNAME"
})
if ($others.Count) {
    Write-Warning "$KeyFile is readable by accounts other than you, and ssh may refuse it. If it does, run these two lines in this window:"
    Write-Warning "  icacls `"$KeyFile`" /inheritance:r"
    Write-Warning "  icacls `"$KeyFile`" /grant:r `"$($env:USERNAME):(R)`""
}
if (-not (Test-Path $LocalRoot)) {
    New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
    Write-Host "Created $LocalRoot"
}

# Does the name resolve at all, and does it resolve to the actual server?
# pissinhot.com sits behind Cloudflare, and Cloudflare proxies web traffic only.
# A name on the orange cloud resolves to a Cloudflare edge address, so ssh to it
# reaches Cloudflare rather than the Oracle box and times out or is refused.
# Better to say that here than to let it look like a network fault.
function Test-CloudflareAddress {
    param([string] $Ip)
    # Cloudflare's published IPv4 ranges, as CIDR.
    $ranges = @(
        '173.245.48.0/20','103.21.244.0/22','103.22.200.0/22','103.31.4.0/22',
        '141.101.64.0/18','108.162.192.0/18','190.93.240.0/20','188.114.96.0/20',
        '197.234.240.0/22','198.41.128.0/17','162.158.0.0/15','104.16.0.0/13',
        '104.24.0.0/14','172.64.0.0/13','131.0.72.0/22'
    )
    # Held as Int64 rather than UInt32 on purpose. PowerShell parses the literal
    # 0xFFFFFFFF as Int32, which is -1, and casting that to an unsigned type
    # throws. Building the value a byte at a time into a signed 64 bit integer
    # keeps every intermediate positive and sidesteps the whole question.
    function ConvertTo-AddressNumber {
        param([string] $Dotted)
        $b = [System.Net.IPAddress]::Parse($Dotted).GetAddressBytes()   # big endian
        ([int64]$b[0] -shl 24) -bor ([int64]$b[1] -shl 16) -bor ([int64]$b[2] -shl 8) -bor [int64]$b[3]
    }
    $addr = ConvertTo-AddressNumber $Ip
    foreach ($r in $ranges) {
        $parts = $r.Split('/')
        $base  = ConvertTo-AddressNumber $parts[0]
        $bits  = [int] $parts[1]
        # Compare the leading $bits by discarding the rest, rather than building
        # a mask. Same test, nothing to get wrong.
        $shift = 32 - $bits
        if ($shift -lt 0 -or $shift -gt 31) { continue }
        if (($addr -shr $shift) -eq ($base -shr $shift)) { return $true }
    }
    return $false
}

$resolved = @()
try {
    $resolved = @([System.Net.Dns]::GetHostAddresses($ServerHost) |
                  Where-Object { $_.AddressFamily -eq 'InterNetwork' } |
                  ForEach-Object { $_.IPAddressToString })
}
catch {
    throw "$ServerHost does not resolve. If the DNS record is new, give it a few minutes. Otherwise pass -ServerHost with the address MobaXterm uses."
}
if ($resolved.Count -eq 0) {
    throw "$ServerHost has no IPv4 address. Pass -ServerHost with the name or IP address you use in MobaXterm."
}
if (@($resolved | Where-Object { Test-CloudflareAddress $_ }).Count -eq $resolved.Count) {
    throw @"
$ServerHost resolves only to Cloudflare edge addresses ($($resolved -join ', ')),
so ssh to that name reaches Cloudflare rather than your server. Cloudflare's
proxy carries web traffic, not SSH.

Use whatever MobaXterm has in its Remote host box instead. That is usually the
Oracle Cloud public IP address, or a DNS-only subdomain that bypasses the proxy.
Then either pass it once:

    .\Get-TargetSubmissions.ps1 -ServerHost <that value>

or edit the `$ServerHost default at the top of this script so it is permanent.
"@
}

$sshArgs = @(
    '-i', $KeyFile
    '-o', 'BatchMode=yes'              # fail rather than hang waiting for a password
    '-o', 'StrictHostKeyChecking=accept-new'
    '-o', 'LogLevel=ERROR'             # see the comment on Invoke-Remote below
    "$ServerUser@$ServerHost"
)

<#
    WHY THIS IS MORE CAREFUL THAN IT LOOKS

    ssh writes ordinary progress and warnings to stderr, not just failures. The
    first connection to a new host prints "Permanently added ... to the list of
    known hosts" there, which is ssh reporting success.

    PowerShell treats any stderr output from a native command as an error
    record, and with $ErrorActionPreference set to Stop it turns that record
    into a terminating error. So a successful connection would kill the script,
    which is exactly what happened on the first working run.

    Two defences, because either alone is fragile:
      1. LogLevel=ERROR above, so ssh stops narrating.
      2. This function separates stderr from stdout itself, decides by exit code
         whether anything actually failed, and returns only the real output.
#>
function Invoke-Remote {
    param([string] $Command)

    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'      # stderr must not be fatal in here
    try {
        $raw = & ssh @sshArgs $Command 2>&1
    }
    finally {
        $ErrorActionPreference = $prev
    }

    $errLines = @($raw |
        Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } |
        ForEach-Object { $_.ToString() })
    $outLines = @($raw |
        Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] })

    if ($LASTEXITCODE -ne 0) {
        $detail = $errLines -join "`n"
        # The two failures worth naming, because their own wording is cryptic.
        if ($detail -match 'no tty present|a terminal is required|a password is required') {
            $detail += "`n`nsudo wants a password and there is no terminal to type it into. Either add the sudoers rule described in the NOTES at the bottom of this script, or run this from a session where sudo does not prompt."
        }
        elseif ($detail -match 'Permission denied') {
            $detail += "`n`nThe key was refused. Check that $KeyFile is the key this server knows and that -ServerUser is right; it is currently '$ServerUser'."
        }
        throw "Remote command failed (exit $LASTEXITCODE): $Command`n$detail"
    }
    # Exit code says it worked, so anything on stderr was commentary. Keep it
    # available under -Verbose rather than throwing it away entirely.
    foreach ($line in $errLines) { Write-Verbose "ssh: $line" }

    return $outLines
}

# ------------------------------------------------------------ what is there --
Write-Host "Listing ${itemNoun}s on $ServerHost ..." -ForegroundColor Cyan

# One name per line. No pipe and no 2>/dev/null on the remote side on purpose:
# a shell pipeline reports the exit code of its LAST command, so "sudo ls | sort"
# returns success even when sudo was refused, and the discarded stderr would take
# the reason with it. That combination turns a permissions failure into the
# cheerful and wrong message "No submissions on the server yet". Ask for the bare
# listing, let the exit code mean what it says, and sort here instead.
$remote = @(Invoke-Remote "sudo ls -1 '$RemoteRoot'" |
            Where-Object { $_ -match '^\d{4}-\d{2}-\d{2}_[0-9a-f]{8}$' } |
            Sort-Object)

# NOTES-FROM-PLANNING.md entry 176 section 5.2: what is still waiting for the worker, and for how long.
# "No submissions on the server yet" was untrue on the night the worker was being killed: there were two,
# stuck in quarantine. grouplab.org's ready folder has a quarantine beside it; each submission's folder
# time is when it arrived, in seconds since 1970, and the age is worked out here.
if ($RemoteRoot -match '/ready$') {
    $quarantine = $RemoteRoot -replace '/ready$', '/quarantine'
    $stamps = @(Invoke-Remote "sudo find '$quarantine' -mindepth 1 -maxdepth 1 -type d -printf '%T@\n'" |
                Where-Object { $_ -match '^\d+(\.\d+)?$' } | ForEach-Object { [double]$_ })
    if ($stamps.Count -gt 0) {
        $oldest = [DateTimeOffset]::FromUnixTimeSeconds([long]($stamps | Measure-Object -Minimum).Minimum)
        $age = [DateTimeOffset]::UtcNow - $oldest
        Write-Host ("{0} waiting in quarantine for the worker, the oldest for {1:N0} minutes." -f $stamps.Count, $age.TotalMinutes) -ForegroundColor Yellow
        if ($age.TotalMinutes -gt 10) {
            Write-Host "  The worker runs every two minutes, so anything older than ten is stuck: read its journal on the server." -ForegroundColor Yellow
        }
    }
}

if ($remote.Count -eq 0) {
    Write-Host "No ${itemNoun}s ready on the server." -ForegroundColor Yellow
    return
}

$local = @(Get-ChildItem -Path $LocalRoot -Directory -ErrorAction SilentlyContinue |
           Select-Object -ExpandProperty Name)
$new   = @($remote | Where-Object { $local -notcontains $_ })

Write-Host ("{0} on the server, {1} already here, {2} new." -f $remote.Count, ($remote.Count - $new.Count), $new.Count)

if ($new.Count -eq 0 -and -not $VerifyAll) {
    Write-Host "Nothing to do." -ForegroundColor Green
    return
}

# ------------------------------------------------------------------- pull it --
$pulled = @()
foreach ($dir in $new) {
    if (-not $PSCmdlet.ShouldProcess($dir, 'pull')) { continue }

    Write-Host "  pulling $dir" -NoNewline

    # tar writes to stdout on the server; we capture the bytes and hand them to
    # the local tar. Redirecting through a temp file rather than a pipe, because
    # PowerShell's pipeline mangles binary streams between native commands.
    $tmp    = Join-Path $env:TEMP "$dir.tar"
    $tmpErr = Join-Path $env:TEMP "$dir.tar.err"
    try {
        # cmd /c so the redirection is byte-exact rather than going through
        # PowerShell's text encoding. stderr is redirected to its own file for
        # the same reason Invoke-Remote separates it: ssh talks on stderr when
        # nothing is wrong, and PowerShell would otherwise treat that as fatal.
        $keyQuoted = '"' + $KeyFile + '"'
        $argLine   = ($sshArgs | ForEach-Object { if ($_ -eq $KeyFile) { $keyQuoted } else { $_ } }) -join ' '
        $sshLine   = "ssh $argLine `"sudo tar cf - -C '$RemoteRoot' '$dir'`" > `"$tmp`" 2> `"$tmpErr`""
        & cmd /c $sshLine
        if ($LASTEXITCODE -ne 0) {
            $why = if (Test-Path $tmpErr) { (Get-Content $tmpErr -Raw).Trim() } else { '' }
            throw "ssh/tar returned $LASTEXITCODE`n$why"
        }
        if ((Get-Item $tmp).Length -eq 0) {
            $why = if (Test-Path $tmpErr) { (Get-Content $tmpErr -Raw).Trim() } else { '' }
            throw "the archive came back empty, so nothing was pulled`n$why"
        }

        $prev = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'   # tar warns on stderr too
        try   { & tar xf $tmp -C $LocalRoot 2>&1 | ForEach-Object { Write-Verbose "tar: $_" } }
        finally { $ErrorActionPreference = $prev }
        if ($LASTEXITCODE -ne 0) { throw "local tar returned $LASTEXITCODE" }

        $pulled += $dir
        $size = (Get-ChildItem (Join-Path $LocalRoot $dir) -File | Measure-Object Length -Sum).Sum
        Write-Host ("  ok, {0:N1} MB" -f ($size / 1MB)) -ForegroundColor Green
    }
    catch {
        Write-Host "  FAILED: $_" -ForegroundColor Red
        # Leave no half-extracted directory behind, or the next run will skip it
        # as though it were complete.
        $partial = Join-Path $LocalRoot $dir
        if (Test-Path $partial) { Remove-Item $partial -Recurse -Force }
    }
    finally {
        if (Test-Path $tmp)    { Remove-Item $tmp -Force }
        if (Test-Path $tmpErr) { Remove-Item $tmpErr -Force }
    }
}

# --------------------------------------------------------------- verify them --
# The checksums were computed on the server the moment each file arrived. If one
# disagrees now, something changed the bytes between there and here.
$toCheck = if ($VerifyAll) { $remote | Where-Object { Test-Path (Join-Path $LocalRoot $_) } } else { $pulled }

$bad = @(); $checked = 0; $optOut = @(); $notScanned = 0
foreach ($dir in $toCheck) {
    $metaPath = Join-Path $LocalRoot "$dir\meta.json"
    if (-not (Test-Path $metaPath)) {
        $bad += "$dir : no meta.json"
        continue
    }
    $meta = Get-Content $metaPath -Raw | ConvertFrom-Json

    if ($CrashReports) {
        # A crash report is one zip carrying one hash, written by the receiver
        # the moment it arrived. Same promise as a submission, fewer files.
        $p = Join-Path $LocalRoot "$dir\report.zip"
        if (-not (Test-Path $p -PathType Leaf)) { $bad += "$dir/report.zip : missing"; continue }
        $checked++
        $h = (Get-FileHash $p -Algorithm SHA256).Hash.ToLower()
        if ($h -ne "$($meta.sha256)".ToLower()) { $bad += "$dir/report.zip : sha256 differs" }
        continue
    }

    # Entry 177: one check, shared with CI's run of the real worker, so the two cannot drift apart again.
    $r = Test-SubmissionFolder -Folder (Join-Path $LocalRoot $dir)
    $bad += $r.Bad
    $checked += $r.Checked
    if ($r.OptOut) { $optOut += $dir }
}

# ------------------------------------------------------------------ summary --
Write-Host ""
Write-Host "Pulled $($pulled.Count) $itemNoun(s); verified $checked file(s)." -ForegroundColor Cyan

if ($bad.Count) {
    Write-Host "PROBLEMS:" -ForegroundColor Red
    $bad | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
} else {
    Write-Host "All checksums match." -ForegroundColor Green
}

# Entry 176 section 9.2: a scanner that did not complete a scan is a broken installation, and the worker
# records it per file. It is said here so it cannot go unnoticed for weeks.
foreach ($dir in $toCheck) {
    $metaPath = Join-Path $LocalRoot "$dir\meta.json"
    if (Test-Path $metaPath -PathType Leaf) {
        $m = Get-Content $metaPath -Raw | ConvertFrom-Json
        if ($m.PSObject.Properties.Name -contains 'notScanned') { $notScanned += [int]$m.notScanned }
    }
}
if ($notScanned -gt 0) {
    Write-Host ""
    Write-Host "The scanner did not run on $notScanned file(s). They were rebuilt from their pixels, but ClamAV on the server is not working: read the worker's journal." -ForegroundColor Red
}

if ($optOut.Count) {
    Write-Host ""
    Write-Host "$($optOut.Count) $itemNoun(s) are marked DO NOT PUBLISH:" -ForegroundColor Yellow
    $optOut | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host "  Use them for testing; never commit them to the public data set." -ForegroundColor Yellow
}

if ($CrashReports -and @($toCheck).Count) {
    # The whole point of pulling these is finding out what broke, so say it here
    # rather than making somebody open six zips to find out.
    Write-Host ""
    Write-Host "What crashed:" -ForegroundColor Cyan
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($dir in @($toCheck)) {
        $zipPath = Join-Path $LocalRoot "$dir\report.zip"
        if (-not (Test-Path $zipPath)) { continue }

        $version = ''
        try { $version = (Get-Content (Join-Path $LocalRoot "$dir\meta.json") -Raw | ConvertFrom-Json).app_version } catch { }
        $what = 'no crash record in the package'

        try {
            $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
            try {
                $e = $zip.Entries | Where-Object { $_.Name -like 'crash-*.json' } | Select-Object -First 1
                if ($e) {
                    $reader = New-Object System.IO.StreamReader($e.Open())
                    try { $crash = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
                    $top  = @($crash.exceptions)[0]
                    $what = "$($top.type): $($top.message)"
                    if ($crash.last_action) { $what += "  (during $($crash.last_action))" }
                }
            }
            finally { $zip.Dispose() }
        }
        catch { $what = "the package could not be read: $_" }

        Write-Host ("  {0}  {1,-16}  {2}" -f $dir, $version, $what)
    }
}

Write-Host ""
Write-Host "Local copy: $LocalRoot"

<#
NOTES

WHERE THIS SCRIPT LIVES, AND WHERE THE FILES DO NOT
The script sits inside the repository at C:\Dev\grouplab\scripts\ and is meant
to be committed, because it is a tool other people working on GroupLab may want.
It contains no secret: the key path is a parameter and the key itself stays
outside the repository at C:\Users\Airwolf\Documents\. Do not move the key into
C:\Dev\grouplab for convenience, because the repository is going public.

WHERE THE FILES LAND
Deliberately outside the GroupLab repository, at C:\Dev\grouplab-submissions.
Whether donated images go into that repository at all, or into a separate data
repository, is still open (NOTES-FROM-PLANNING.md entry 22). Keeping the pulled
copy separate leaves that decision open and stops a few hundred megabytes of
binaries wandering into git by accident.

GPS
These are the untouched originals and most of them carry GPS. That is correct:
scrubbing happens at publication, not at intake. Run
tools/scan_analysis/scrub_exif.py before anything is published.

Crash reports are the opposite case and carry no photographs and no metadata at
all, by design. See docs/CRASH-REPORTING.md. If a crash package ever turns up
with an image in it, the client has a defect: the receiver refuses those, so it
should not be possible, and it is worth finding out how it happened.

MAKING IT UNATTENDED
As written, sudo may prompt for a password, which BatchMode turns into a
failure rather than a hang. To run this from Task Scheduler, add a sudoers rule
on the server limited to exactly the two commands it uses:

    sudo visudo -f /etc/sudoers.d/target-uploads

    ubuntu ALL=(root) NOPASSWD: /usr/bin/ls -1 /home/airwolf/web/pissinhot.com/private/target_uploads
    ubuntu ALL=(root) NOPASSWD: /usr/bin/tar cf - -C /home/airwolf/web/pissinhot.com/private/target_uploads *
    ubuntu ALL=(root) NOPASSWD: /usr/bin/ls -1 /home/airwolf/web/pissinhot.com/private/crash_reports
    ubuntu ALL=(root) NOPASSWD: /usr/bin/tar cf - -C /home/airwolf/web/pissinhot.com/private/crash_reports *

That grants read access to one directory tree and nothing else. It is narrower
than it looks, but it is still a privilege grant, so decide deliberately rather
than by default. Without it, run the script by hand and type the password once.

CLOUDFLARE AND THE HOSTNAME
pissinhot.com is proxied by Cloudflare, and the Cloudflare proxy carries web
traffic only. A name on the orange cloud resolves to a Cloudflare edge address,
so ssh to that name reaches Cloudflare and never touches the Oracle box. The
preflight detects this and says so, rather than letting it look like a network
fault. ssh.pissinhot.com is a DNS-only record, grey cloud rather than orange,
which is why it works where the bare domain does not.

One consequence of a DNS-only record, worth knowing rather than worrying about:
anyone can look it up and learn the server's real address, which is the address
Cloudflare otherwise hides. For a site this size that is a small thing, and it
is the same exposure any mail or ssh subdomain creates. It is only worth
revisiting if the site is ever attacked, in which case the answer is to delete
the record and pass -ServerHost by hand.

The server's address is intentionally not written into this file, since the
repository is going public and there is no reason to hand it to a crawler.

IF THE SERVER MOVES
Everything configurable is a parameter at the top. Nothing is hard coded in the
body.
#>
