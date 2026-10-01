<#
.SYNOPSIS
NOTES-FROM-PLANNING.md entry 337 section 4: a chosen build to the Microsoft Store as a new submission, through the Store submission API, with
the app registration request 38 already set up (Manager role in Partner Center). Started only by hand, from store-submit.yml.

.DESCRIPTION
Takes the MSIX of a store-draft-<version> draft release (made by release.yml with store_draft), and either says what it would do (DryRun,
which reads and changes nothing), or: makes a new submission cloned from the published one, marks the old package for removal and adds the
new one, uploads it, commits the submission, and reports its status. Which builds go to the Store, and how often, is Alan's decision
(for-alan.md); nothing is submitted until he agrees, and the workflow asks for the version twice.
#>
param(
    [Parameter(Mandatory)] [string] $Msix,
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'
$api = 'https://manage.devcenter.microsoft.com/v1.0/my'

function Say([string] $line) {
    Write-Host $line
    if ($env:GITHUB_STEP_SUMMARY) { $line | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }
    if ($env:STATUS_OUT) { ($line -replace '^#+\s*', '') | Out-File -Append -Encoding utf8 $env:STATUS_OUT }
}

function Call([string] $method, [string] $path, $body = $null) {
    $request = @{ Method = $method; Uri = "$api/$path"; Headers = @{ Authorization = "Bearer $script:access" }; SkipHttpErrorCheck = $true }
    if ($null -ne $body) { $request.Body = ($body | ConvertTo-Json -Depth 20); $request.ContentType = 'application/json' }
    $answer = Invoke-WebRequest @request
    if ($answer.StatusCode -ge 300) { throw "The Store API answered $($answer.StatusCode) to $method $path." }
    if ($answer.Content) { return $answer.Content | ConvertFrom-Json } else { return $null }
}

if (-not (Test-Path $Msix)) { throw "There is no package at $Msix." }
foreach ($name in 'TENANT', 'CLIENT', 'SECRET', 'PRODUCT') {
    if (-not [Environment]::GetEnvironmentVariable($name)) { throw "$name is not set (request 38's secrets and the STORE_PRODUCT_ID variable)." }
}

$token = Invoke-WebRequest -Method Post -SkipHttpErrorCheck -ContentType 'application/x-www-form-urlencoded' `
    -Uri "https://login.microsoftonline.com/$([uri]::EscapeDataString($env:TENANT))/oauth2/token" `
    -Body @{ grant_type = 'client_credentials'; client_id = $env:CLIENT; client_secret = $env:SECRET; resource = 'https://manage.devcenter.microsoft.com' }
if ($token.StatusCode -ne 200) { throw "Microsoft Entra answered $($token.StatusCode); the Store secret may have expired (request 38)." }
$script:access = ($token.Content | ConvertFrom-Json).access_token
if ($env:GITHUB_ACTIONS) { Write-Host "::add-mask::$script:access" }

$product = Call Get "applications/$env:PRODUCT"
if ($product.pendingApplicationSubmission) {
    throw "A submission is already waiting on Microsoft or in Partner Center ($($product.pendingApplicationSubmission.id)); finish or delete it there first."
}
$size = (Get-Item $Msix).Length
Say "### The Store submission of $(Split-Path $Msix -Leaf), $size bytes$(if ($DryRun) { ', a dry run that changes nothing' })"
if ($DryRun) {
    Say "Would clone the published submission $($product.lastPublishedApplicationSubmission.id), replace its package with this one, upload it and commit it."
    return
}

$submission = Call Post "applications/$env:PRODUCT/submissions"
foreach ($package in @($submission.applicationPackages)) { $package.fileStatus = 'PendingDelete' }
$new = [pscustomobject]@{ fileName = 'grouplab-win-x64.msix'; fileStatus = 'PendingUpload'; minimumDirectXVersion = 'None'; minimumSystemRam = 'None' }
$submission.applicationPackages = @($submission.applicationPackages) + $new
$null = Call Put "applications/$env:PRODUCT/submissions/$($submission.id)" $submission

# The package goes up as a zip to the submission's own upload address, an Azure blob.
$zip = Join-Path ([IO.Path]::GetTempPath()) 'store-upload.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $Msix -DestinationPath $zip
$upload = Invoke-WebRequest -Method Put -SkipHttpErrorCheck -Uri $submission.fileUploadUrl -InFile $zip -Headers @{ 'x-ms-blob-type' = 'BlockBlob' }
Remove-Item $zip
if ($upload.StatusCode -ge 300) { throw "The upload answered $($upload.StatusCode); the submission $($submission.id) is left for Partner Center." }

$null = Call Post "applications/$env:PRODUCT/submissions/$($submission.id)/commit"
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 30
    $status = Call Get "applications/$env:PRODUCT/submissions/$($submission.id)/status"
    if ($status.status -ne 'CommitStarted') { break }
}
Say "Submission $($submission.id): $($status.status)."
foreach ($e in @($status.statusDetails.errors)) { if ($e) { Say "Error $($e.code): $($e.details)" } }
