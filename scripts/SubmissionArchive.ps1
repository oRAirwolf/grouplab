<#
.SYNOPSIS
    The private archive of submissions: one release a month in oRAirwolf/grouplab-submissions-archive, one zip asset a submission.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entries 216 and 217. Dot-sourced by Get-TargetSubmissions.ps1 and Test-SubmissionsArchive.ps1.

    Release assets rather than Git LFS, because an asset can be deleted to free space and LFS storage cannot (entry 217 section 1).
    Each submission is a zip of its folder exactly as pulled; each release carries a manifest.json listing every submission in it with
    its size, its zip's SHA-256 and its consent level, rewritten whenever the release changes. The repository is private, is created by
    Alan, and is never made public; nothing is published from it without an entry saying so. Its tags are its own and never v*.

    Everything goes through the gh command line as the person signed in to it. Nothing here reads or prints a token.
#>

$script:ArchiveRepo = 'oRAirwolf/grouplab-submissions-archive'

# Entry 220: every call to gh goes through Invoke-Native, so a line gh writes to stderr, such as "release not found" for a month with
# no release yet, is never fatal under Windows PowerShell 5.1; the exit code alone decides.
. (Join-Path $PSScriptRoot 'NativeCommand.ps1')

function Test-ArchiveRepository {
    <# True when the archive exists and is private; otherwise the reason, as a string. #>
    param([string] $Repo = $script:ArchiveRepo)
    $r = Invoke-Native gh repo view $Repo --json isPrivate
    $json = $r.Output -join "`n"
    if ($r.ExitCode -ne 0 -or -not $json) { return "the archive $Repo does not exist, or gh cannot see it" }
    if (-not ($json | ConvertFrom-Json).isPrivate) { return "the archive $Repo is not private, so nothing is put in it" }
    return $true
}

function Get-ArchiveTag {
    <# The month's release a submission belongs to, from its folder name, which starts with the date it arrived. #>
    param([Parameter(Mandatory)] [string] $Name)
    if ($Name -notmatch '^(\d{4})-(\d{2})-\d{2}_[0-9a-f]{8}$') { throw "$Name is not a submission folder name" }
    return "archive-$($Matches[1])-$($Matches[2])"
}

function Get-ArchiveManifest {
    <# The month's manifest as a list of entries, or an empty list when the release or its manifest does not exist yet. #>
    param([Parameter(Mandatory)] [string] $Tag, [string] $Repo = $script:ArchiveRepo)
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("gl-manifest-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    try {
        $r = Invoke-Native gh release download $Tag -R $Repo -p 'manifest.json' -D $tmp
        $file = Join-Path $tmp 'manifest.json'
        if ($r.ExitCode -ne 0 -or -not (Test-Path $file)) { return @() }
        return @((Get-Content $file -Raw | ConvertFrom-Json).submissions)
    }
    finally { Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue }
}

function Get-ConsentLevel {
    <# The consent level the pull writes into CONSENT.txt, read the same way: testing where the contributor opted out. #>
    param([Parameter(Mandatory)] [string] $Folder)
    if (Test-Path (Join-Path $Folder 'DO-NOT-PUBLISH')) { return 'testing' }
    $meta = Get-Content (Join-Path $Folder 'meta.json') -Raw | ConvertFrom-Json
    if ($meta.consent -and ($meta.consent.PSObject.Properties.Name -contains 'level')) { return "$($meta.consent.level)" }
    if ($meta.PSObject.Properties.Name -contains 'exclude_from_public_dataset' -and $meta.exclude_from_public_dataset) { return 'testing' }
    return 'publishable'
}

function Add-ToArchive {
    <#
    Puts one verified local submission into the archive and proves it is there: the zip is uploaded, downloaded back and compared by
    SHA-256. Returns $true only when the archive holds it, byte for byte; a submission already in the manifest is downloaded and checked
    against the manifest's hash instead of being uploaded again.
    #>
    param([Parameter(Mandatory)] [string] $Folder, [string] $Repo = $script:ArchiveRepo)
    $name = Split-Path $Folder -Leaf
    $tag = Get-ArchiveTag -Name $name
    $asset = "$name.zip"
    $work = Join-Path ([IO.Path]::GetTempPath()) ("gl-archive-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work | Out-Null
    try {
        if ((Invoke-Native gh release view $tag -R $Repo).ExitCode -ne 0) {
            $made = Invoke-Native gh release create $tag -R $Repo --title "Submissions, $($tag.Substring(8))" --notes "Private. One zip a submission, exactly as pulled, and manifest.json. Never published from here."
            if ($made.ExitCode -ne 0) { Write-Warning "could not create the release $tag`: $($made.Errors -join ' ')"; return $false }
        }

        $manifest = @(Get-ArchiveManifest -Tag $tag -Repo $Repo)
        $listed = $manifest | Where-Object { $_.name -eq $name } | Select-Object -First 1
        $zip = Join-Path $work $asset
        if (-not $listed) {
            Compress-Archive -Path (Join-Path $Folder '*') -DestinationPath $zip -CompressionLevel Optimal
            $hash = Get-Sha256 $zip
            $up = Invoke-Native gh release upload $tag $zip -R $Repo --clobber
            if ($up.ExitCode -ne 0) { Write-Warning "$name could not be uploaded: $($up.Errors -join ' ')"; return $false }
            $listed = [pscustomobject]@{ name = $name; bytes = (Get-Item $zip).Length; sha256 = $hash; consent = (Get-ConsentLevel -Folder $Folder) }
            $manifest = @($manifest | Where-Object { $_.name -ne $name }) + $listed
        }

        # The proof: what GitHub holds, downloaded back, against the hash recorded for it.
        $back = Join-Path $work 'back'
        New-Item -ItemType Directory -Path $back | Out-Null
        $down = Invoke-Native gh release download $tag -R $Repo -p $asset -D $back
        $got = Join-Path $back $asset
        if ($down.ExitCode -ne 0 -or -not (Test-Path $got)) { Write-Warning "$name could not be downloaded back"; return $false }
        if ((Get-Sha256 $got) -ne "$($listed.sha256)".ToLower()) {
            Write-Warning "$name in the archive does not match its SHA-256"
            return $false
        }

        # The manifest, rewritten with the month's whole list.
        $manifestFile = Join-Path $work 'manifest.json'
        [pscustomobject]@{
            format      = 'grouplab-submissions-archive-1'
            release     = $tag
            written     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            submissions = @($manifest | Sort-Object name)
        } | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestFile -Encoding utf8
        if ((Invoke-Native gh release upload $tag $manifestFile -R $Repo --clobber).ExitCode -ne 0) { Write-Warning "the manifest of $tag could not be written"; return $false }
        return $true
    }
    finally { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }
}
