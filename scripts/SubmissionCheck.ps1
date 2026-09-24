<#
.SYNOPSIS
    Checks one pulled submission against its own meta.json. Dot-sourced by Get-TargetSubmissions.ps1.
.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 177 section 1. The intake worker rewrites meta.json's files list with the key
    "stored"; the old pissinhot.com receiver wrote "stored_name", and the pull script read only that. The null
    name joined to the submission's own folder, Test-Path passed on the folder, and the hash of a folder is
    nothing, so the script crashed after pulling. This reads either key, and a folder never passes as a file.

    It lives in its own file so that CI can run this exact check against meta.json as the real worker writes it
    (tests/python/worker-tests.py). The worker and this were written apart and nothing held them together.
#>

function Test-SubmissionFolder {
    param([Parameter(Mandatory = $true)] [string] $Folder)

    $result = [ordered]@{ Bad = @(); Checked = 0; OptOut = $false; NotScanned = 0 }
    $name = Split-Path $Folder -Leaf
    $metaPath = Join-Path $Folder 'meta.json'
    if (-not (Test-Path $metaPath -PathType Leaf)) {
        $result.Bad += "$name : no meta.json"
        return [pscustomobject]$result
    }

    $meta = Get-Content $metaPath -Raw | ConvertFrom-Json
    # Entry 183 section 3.3: the opt out from either record. The receiver writes meta.json's flag and a DO-NOT-PUBLISH marker in the
    # same request, so either one alone withholds the submission, a missing flag is unknown rather than false, and the two disagreeing
    # is reported, because it means one of them was changed or lost after the contributor sent it.
    $marker = Test-Path (Join-Path $Folder 'DO-NOT-PUBLISH') -PathType Leaf
    $hasFlag = $meta.PSObject.Properties.Name -contains 'exclude_from_public_dataset' -and $meta.exclude_from_public_dataset -is [bool]
    if (-not $hasFlag) {
        $result.OptOut = $true
        $result.Bad += "$name : meta.json does not say whether the contributor opted out, so it is treated as opted out"
    } elseif ($meta.exclude_from_public_dataset -or $marker) {
        $result.OptOut = $true
    }
    if ($hasFlag -and [bool]$meta.exclude_from_public_dataset -ne $marker) {
        $result.Bad += "$name : the DO-NOT-PUBLISH marker and meta.json disagree about the opt out; it is withheld either way"
    }
    if ($meta.PSObject.Properties.Name -contains 'notScanned') { $result.NotScanned = [int]$meta.notScanned }

    foreach ($f in @($meta.files)) {
        $props = $f.PSObject.Properties.Name
        $stored = if ($props -contains 'stored') { $f.stored } elseif ($props -contains 'stored_name') { $f.stored_name } else { $null }
        if ([string]::IsNullOrWhiteSpace($stored)) {
            $result.Bad += "$name : a file in meta.json names no stored file"
            continue
        }

        $p = Join-Path $Folder $stored
        if (-not (Test-Path $p -PathType Leaf)) { $result.Bad += "$name/$stored : missing"; continue }
        if ([string]::IsNullOrWhiteSpace("$($f.sha256)")) { $result.Bad += "$name/$stored : meta.json has no sha256 for it"; continue }

        $h = (Get-FileHash $p -Algorithm SHA256).Hash.ToLower()
        $result.Checked++
        if ($h -ne "$($f.sha256)".ToLower()) { $result.Bad += "$name/$stored : sha256 differs" }
    }

    return [pscustomobject]$result
}
