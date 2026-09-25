<#
.SYNOPSIS
    The one way these scripts run another program, NOTES-FROM-PLANNING.md entry 220.

.DESCRIPTION
    Dot-sourced by Get-TargetSubmissions.ps1, SubmissionArchive.ps1, Remove-ReadSubmissions.ps1 and Test-SubmissionsArchive.ps1.

    Windows PowerShell 5.1, the shell Alan runs these from, turns anything a program writes to stderr into a terminating error while
    $ErrorActionPreference is Stop, even with 2>$null. Programs talk on stderr when nothing is wrong: ssh says it added a host, and gh says
    "release not found" when asked about a month that has no release yet, which is how request 31's first real pull stopped. So every
    program runs through Invoke-Native: stderr is collected and never fatal, and the exit code alone says whether it worked.

    Invoke-Native is deliberately a plain function, not an advanced one, so every argument after the program's name reaches the program
    exactly as written, a leading dash included.

.EXAMPLE
    $r = Invoke-Native gh release view $tag -R $Repo
    if ($r.ExitCode -ne 0) { ... $r.Errors ... }
#>

function Invoke-Native {
    $program = $args[0]
    $rest = @($args | Select-Object -Skip 1)
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $all = @(& $program @rest 2>&1)
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $saved
    }

    [pscustomobject]@{
        ExitCode = $code
        Output   = @($all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
        Errors   = @($all | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
    }
}

function Get-Sha256 {
    <#
    A file's SHA-256 in lower case hex, read with .NET. Entry 222: Get-FileHash honors -WhatIf in Windows PowerShell 5.1 and returns
    nothing under a dry run, so a dry run of the pull could not check a single file.
    #>
    param([Parameter(Mandatory)] [string] $Path)
    $stream = [IO.File]::OpenRead($Path)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try { return -join ($sha.ComputeHash($stream) | ForEach-Object { $_.ToString('x2') }) }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}
