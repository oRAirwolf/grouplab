<#
.SYNOPSIS
    What the scheduled tasks run, NOTES-FROM-PLANNING.md entry 222: the nightly backup, and the weekly restore test, cleanup and line.

.DESCRIPTION
    -Job Nightly: scripts\backup.py, which copies the archive's submissions here, backs up, and prunes old backups.
    -Job Weekly:  scripts\backup.py --restore-test, scripts\cleanup.py, then scripts\automation-report.py for the line in for-alan.md.

    Everything each job prints goes to C:\Dev\grouplab-local\automation.log, the newest thousand lines kept. A failure is also sent as an
    error report by backup.py itself, so it reaches Code without anybody reading this log.
#>
[CmdletBinding()]
param([Parameter(Mandatory)] [ValidateSet('Nightly', 'Weekly')] [string] $Job)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'NativeCommand.ps1')

$repo = Split-Path $PSScriptRoot -Parent
$log = Join-Path (Split-Path $repo -Parent) 'grouplab-local\automation.log'
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

function Step([string] $What, [string[]] $Arguments) {
    $r = Invoke-Native python @Arguments
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm')
    Add-Content -Path $log -Encoding utf8 -Value (@("$stamp $Job, $What, exit $($r.ExitCode)") + $r.Output + $r.Errors)
}

Push-Location $repo
try {
    if ($Job -eq 'Nightly') {
        Step 'backup' @('scripts\backup.py')
    } else {
        Step 'restore test' @('scripts\backup.py', '--restore-test')
        Step 'cleanup' @('scripts\cleanup.py')
        Step 'weekly line' @('scripts\automation-report.py')
    }
}
finally {
    Pop-Location
    $lines = @(Get-Content $log)
    if ($lines.Count -gt 1000) { Set-Content -Path $log -Encoding utf8 -Value $lines[-1000..-1] }
}
