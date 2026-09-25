<#
.SYNOPSIS
    Registers GroupLab's two scheduled tasks for the person running it, NOTES-FROM-PLANNING.md entry 222 section 3.3.

.DESCRIPTION
    \GroupLab\Nightly backup  every night at 03:30: scripts\Run-Automation.ps1 -Job Nightly
    \GroupLab\Weekly check    every Sunday at 04:30: scripts\Run-Automation.ps1 -Job Weekly

    Both run as the person who registers them, only while they are logged on, so no password is stored or asked for. A run missed
    because the computer was asleep or off runs as soon as it can. Running this again replaces the two tasks; -Remove takes them away.
    It needs no administrator rights and changes nothing else.
#>
[CmdletBinding(SupportsShouldProcess)]
param([switch] $Remove)

$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'Run-Automation.ps1'
$path = '\GroupLab\'
$tasks = @(
    @{ Name = 'Nightly backup'; Job = 'Nightly'; Trigger = (New-ScheduledTaskTrigger -Daily -At '03:30') },
    @{ Name = 'Weekly check'; Job = 'Weekly'; Trigger = (New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At '04:30') }
)

foreach ($t in $tasks) {
    if ($Remove) {
        if ($PSCmdlet.ShouldProcess($t.Name, 'remove the scheduled task')) {
            Unregister-ScheduledTask -TaskPath $path -TaskName $t.Name -Confirm:$false -ErrorAction SilentlyContinue
        }
        continue
    }

    $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$runner`" -Job $($t.Job)"
    $principal = New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 2)
    if ($PSCmdlet.ShouldProcess($t.Name, 'register the scheduled task')) {
        Register-ScheduledTask -TaskPath $path -TaskName $t.Name -Action $action -Trigger $t.Trigger -Principal $principal -Settings $settings `
            -Description "GroupLab, entry 222: $($t.Job). See docs/RESTORE.md." -Force | Out-Null
        Write-Host "registered $path$($t.Name)"
    }
}
