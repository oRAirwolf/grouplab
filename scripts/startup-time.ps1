# NOTES-FROM-PLANNING.md entry 388 section 4, Phase 9's baseline: the desktop's start-up to a usable window, as entry 331 measured it. The
# Release build is started, timed from starting the process to its main window showing (not the splash, entry 507), and closed; one start is thrown away for the disk's
# cache and five are timed. It prints the median, fastest and slowest in milliseconds; -Baseline writes the median into
# docs/performance-baseline.json under "start-up", and -Gate fails when it is more than a quarter and 20 ms slower than that (BenchGate's
# rule). Run on the machine the baseline names, with nothing else working.
#
# Usage: pwsh scripts/startup-time.ps1 [-Exe <GroupLab.App.exe>] [-Runs 5] [-Baseline | -Gate]
param(
    [string]$Exe = (Join-Path $PSScriptRoot "..\src\GroupLab.App\bin\Release\net10.0\GroupLab.App.exe"),
    [int]$Runs = 5,
    [switch]$Baseline,
    [switch]$Gate
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Exe)) { throw "No Release build at $Exe; build it with: dotnet build src/GroupLab.App -c Release" }

# Entry 507 section 2: a splash ("GroupLab is starting") shows first, so the main window is found by its own title among the process's
# windows, not as Windows' idea of its main window, which is the splash while the splash is up. The splash's own time is printed too.
Add-Type @"
using System; using System.Text; using System.Runtime.InteropServices;
public static class StartupWindows {
    private delegate bool Each(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Each each, IntPtr data);
    [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(IntPtr window, out int process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int most);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    public static bool Shows(int process, string title) {
        bool found = false;
        EnumWindows((w, _) => {
            int owner; GetWindowThreadProcessId(w, out owner);
            if (owner != process || !IsWindowVisible(w)) return true;
            var text = new StringBuilder(256); GetWindowText(w, text, 256);
            found = text.ToString() == title;
            return !found;
        }, IntPtr.Zero);
        return found;
    }
}
"@
$script:splashTimes = @()

function Start-Once {
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $Exe -PassThru
    try {
        $splash = $null
        while (-not [StartupWindows]::Shows($process.Id, "GroupLab") -and $clock.Elapsed.TotalSeconds -lt 60) {
            if ($null -eq $splash -and [StartupWindows]::Shows($process.Id, "GroupLab is starting")) { $splash = $clock.Elapsed.TotalMilliseconds }
            Start-Sleep -Milliseconds 5
        }
        $ms = $clock.Elapsed.TotalMilliseconds
        if (-not [StartupWindows]::Shows($process.Id, "GroupLab")) { throw "GroupLab showed no window in a minute" }
        if ($null -ne $splash) { $script:splashTimes += $splash }
        return $ms
    }
    finally {
        if (-not $process.HasExited) { $null = $process.CloseMainWindow(); if (-not $process.WaitForExit(10000)) { $process.Kill() } }
    }
}

$null = Start-Once
$times = @(1..$Runs | ForEach-Object { Start-Once }) | Sort-Object
$median = $times[[int][Math]::Floor($Runs / 2)]
"start-up to a usable window: {0:0} ms ({1:0} to {2:0}), {3} runs" -f $median, $times[0], $times[-1], $Runs
if ($script:splashTimes.Count -gt 0) {
    $shown = @($script:splashTimes | Sort-Object)
    "the splash first showing: {0:0} ms median" -f $shown[[int][Math]::Floor($shown.Count / 2)]
}
$path = Join-Path $PSScriptRoot "..\docs\performance-baseline.json"
$json = Get-Content $path -Raw | ConvertFrom-Json -AsHashtable
$key = "start-up to a usable window"
if ($Gate) {
    $was = [double]$json["start-up"][$key]
    $slow = $median -gt $was * 1.25 -and $median - $was -gt 20
    "{0} against the baseline's {1:0} ms" -f ($(if ($slow) { "FAILED" } else { "ok" })), $was
    if ($slow) { exit 1 }
}
if ($Baseline) {
    $json["start-up"][$key] = [Math]::Round($median, 1)
    [System.IO.File]::WriteAllText((Resolve-Path $path), (($json | ConvertTo-Json -Depth 5) -replace "`r`n", "`n") + "`n")
}
