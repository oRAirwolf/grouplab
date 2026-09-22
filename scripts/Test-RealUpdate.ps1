<#
.SYNOPSIS
    Updates the installed GroupLab from one published nightly to the next, by pressing its own buttons, and
    checks that the new version comes back on the screen by itself.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 123 section 2.7, and the relaunch fault Alan found afterwards.

    The first time this was done it was driven by hand from a PowerShell session, and it proved the update
    worked. What it did not check is the thing that then broke: that the new version's window actually appears.
    The installer brought GroupLab back before it had finished writing its files, the new process died on its
    first line, and the test as it was run would have called that a pass, because the update itself had
    installed.

    So this script waits for a window belonging to a *new* process, with a deadline, and fails if none arrives.
    It also fails if a crash record is written during the run, because a relaunch that crashes is not a
    relaunch.

    It presses real buttons on the real window through UI Automation. Nothing here stands in for anything.

.PARAMETER WindowTimeoutSeconds
    How long the new version has to put a window on the screen by itself. An update installs in seconds and the
    relaunch follows it immediately, so this is generous rather than tight.
#>
[CmdletBinding()]
param(
    [int] $WindowTimeoutSeconds = 90,
    [string] $Installed = "$env:ProgramFiles\GroupLab\GroupLab.App.exe",
    [string] $Logs = "$env:LOCALAPPDATA\GroupLab\logs"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

function Fail($message) {
    Write-Output "FAILED: $message"
    exit 1
}

function Find-GroupLabWindow([int[]] $excludeProcessIds) {
    # Matched by process, never by title: VS Code with this repository open has "grouplab" in its title.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $windows = $root.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $windows) {
        # Not $pid: that is PowerShell's own, and assigning to it silently does nothing useful.
        $owner = $w.Current.ProcessId
        if ($excludeProcessIds -contains $owner) { continue }
        $process = Get-Process -Id $owner -ErrorAction SilentlyContinue
        if ($process -and $process.ProcessName -eq 'GroupLab.App') { return $w }
    }
    return $null
}

function Invoke-ByName($window, [string] $name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $element = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if (-not $element) { return $false }
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    return $true
}

if (-not (Test-Path $Installed)) { Fail "no installed GroupLab at $Installed" }

$crashesBefore = @(Get-ChildItem $Logs -Filter 'crash-*.json' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$versionBefore = (Get-Item $Installed).VersionInfo.ProductVersion
Write-Output "installed before: $versionBefore"

# Every GroupLab already running, so a window left over from an earlier run is never mistaken for the relaunch. The first version of this
# script excluded only the process it started, found a stale window from a previous run, and reported that the relaunch had worked when the
# installed version had not even changed.
$before = @(Get-Process -Name 'GroupLab.App' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
if ($before.Count -gt 0) { Write-Output "$($before.Count) GroupLab already running; they are excluded" }

$started = Start-Process -FilePath $Installed -PassThru
$oldPid = $started.Id
$exclude = @($before + $oldPid)
Write-Output "started process $oldPid"

# The bar appears on its own once the check has run.
$window = $null
for ($i = 0; $i -lt 60 -and -not $window; $i++) {
    Start-Sleep -Milliseconds 500
    $window = Find-GroupLabWindow -excludeProcessIds $before
}
if (-not $window) { Fail 'the first window never appeared' }

$pressed = $false
for ($i = 0; $i -lt 60 -and -not $pressed; $i++) {
    Start-Sleep -Seconds 1
    $pressed = Invoke-ByName $window 'Update now'
}
if (-not $pressed) { Fail 'the update bar never offered Update now, so there was nothing newer to install' }
Write-Output 'pressed Update now'

$pressed = $false
for ($i = 0; $i -lt 120 -and -not $pressed; $i++) {
    Start-Sleep -Seconds 1
    $pressed = Invoke-ByName $window 'Install and restart'
}
if (-not $pressed) { Fail 'the download never finished, so Install and restart never appeared' }
Write-Output 'pressed Install and restart'

# The whole point of this script: the new version has to put a window up by itself, in a new process.
$deadline = (Get-Date).AddSeconds($WindowTimeoutSeconds)
$newWindow = $null
while (-not $newWindow -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    $newWindow = Find-GroupLabWindow -excludeProcessIds $exclude
}

if (-not $newWindow) {
    $lines = Get-ChildItem $Logs -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Output "the newest log is $($lines.Name):"
    Get-Content $lines.FullName -Tail 15 | ForEach-Object { Write-Output "    $_" }
    Fail "no window from a new process within $WindowTimeoutSeconds seconds: the update installed and GroupLab did not come back"
}

Write-Output "the new version's window appeared on its own, process $($newWindow.Current.ProcessId)"

$crashesAfter = @(Get-ChildItem $Logs -Filter 'crash-*.json' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$new = @($crashesAfter | Where-Object { $crashesBefore -notcontains $_ })
if ($new.Count -gt 0) {
    foreach ($c in $new) { Write-Output "crash record written: $c" }
    Fail 'a crash record was written during the update'
}

$versionAfter = (Get-Item $Installed).VersionInfo.ProductVersion
Write-Output "installed after: $versionAfter"
if ($versionAfter -eq $versionBefore) { Fail 'the installed version did not change' }

Write-Output 'PASSED: the update installed, the new version came back on its own, and no crash was recorded.'
exit 0
