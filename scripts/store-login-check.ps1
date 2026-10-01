<#
.SYNOPSIS
    Proves GroupLab can sign in to the Microsoft Store, and submits nothing. NOTES-FROM-PLANNING.md entry 293 section 2.

.DESCRIPTION
    Reads the Store's credentials from the environment, as release.yml passes them from the repository's secrets and variables: TENANT
    (AZURE_AD_TENANT_ID), CLIENT (AZURE_AD_APPLICATION_CLIENT_ID), SECRET (AZURE_AD_APPLICATION_SECRET) and PRODUCT (STORE_PRODUCT_ID).
    It asks Microsoft Entra for a token for the Store submission API, then reads the product back, and the status of any submission waiting on
    Microsoft (request 38 Part B), which are reads and change nothing.

    It prints the two HTTP statuses and the product's name, and nothing else: no identifier, no token and never the secret. When Entra
    refuses, the error says in plain words that the Store secret may have expired or be wrong, with Entra's own error code, which names
    the reason without naming anything private.

    The client secret was made on 2026-09-29 and expires about 2028-09-28 (docs/RELEASE-PLAN.md).
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$renew = 'Make a new client secret for grouplab-store-publisher in Microsoft Entra, then set it with: gh secret set AZURE_AD_APPLICATION_SECRET -R oRAirwolf/grouplab'

function Write-Summary([string] $line) {
    Write-Host $line
    if ($env:GITHUB_STEP_SUMMARY) { $line | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }
    # Entries 336 and 337: the same lines, without their headings' marks, for docs/notes/external-status.md.
    if ($env:STATUS_OUT) { ($line -replace '^#+\s*', '' -replace '^-\s*', '') | Out-File -Append -Encoding utf8 $env:STATUS_OUT }
}

# Request 38 Part B: where the Store's submissions stand, read and never changed. The first submission was made by hand in Partner Center on
# 2026-09-30, so this is how its certification result reaches the run's summary. A failure here is reported and never fails the login check.
function Write-SubmissionState($product, [string] $access) {
    Write-Summary "Published in the Store: $(if ($product.lastPublishedApplicationSubmission) { 'yes' } else { 'not yet' })."
    # Entries 336 and 337: which build the Store carries, from the published submission's packages.
    if ($product.lastPublishedApplicationSubmission) {
        try {
            $published = Invoke-WebRequest -Method Get -SkipHttpErrorCheck -Headers @{ Authorization = "Bearer $access" } `
                -Uri "https://manage.devcenter.microsoft.com/v1.0/my/$($product.lastPublishedApplicationSubmission.resourceLocation)"
            if ($published.StatusCode -eq 200) {
                $versions = @(($published.Content | ConvertFrom-Json).applicationPackages | Where-Object { $_.fileStatus -ne 'PendingDelete' } | ForEach-Object { $_.version }) | Sort-Object -Unique
                Write-Summary "The Store carries package version $(if ($versions) { $versions -join ', ' } else { 'not stated' })."
            }
        }
        catch { }
    }
    $pending = $product.pendingApplicationSubmission
    if (-not $pending) {
        Write-Summary 'No submission is waiting on Microsoft.'
        return
    }
    try {
        $state = Invoke-WebRequest -Method Get -SkipHttpErrorCheck -Headers @{ Authorization = "Bearer $access" } `
            -Uri "https://manage.devcenter.microsoft.com/v1.0/my/$($pending.resourceLocation)/status"
        if ($state.StatusCode -ne 200) {
            Write-Summary "A submission is waiting on Microsoft; its status could not be read (HTTP $($state.StatusCode))."
            return
        }
        $status = $state.Content | ConvertFrom-Json
        Write-Summary "### The waiting submission's status: $($status.status)."
        foreach ($e in @($status.statusDetails.errors)) { if ($e) { Write-Summary "- Error $($e.code): $($e.details)" } }
        foreach ($r in @($status.statusDetails.certificationReports)) { if ($r) { Write-Summary "- Certification report of $($r.date)" } }
    }
    catch {
        Write-Summary 'A submission is waiting on Microsoft; the Store API could not be reached for its status. Run it again.'
    }
}

$missing = @{ TENANT = 'AZURE_AD_TENANT_ID'; CLIENT = 'AZURE_AD_APPLICATION_CLIENT_ID'; SECRET = 'AZURE_AD_APPLICATION_SECRET'; PRODUCT = 'STORE_PRODUCT_ID' }.GetEnumerator() |
    Where-Object { -not [Environment]::GetEnvironmentVariable($_.Key) } | ForEach-Object { $_.Value } | Sort-Object
if ($missing) { throw "The Store login cannot be checked: $($missing -join ', ') $(if (@($missing).Count -gt 1) { 'are' } else { 'is' }) not set in the repository (request 38)." }

# 1. The token. Entra's answer on a refusal can name the tenant and the application, so only its error code is ever printed.
try {
    $token = Invoke-WebRequest -Method Post -SkipHttpErrorCheck -ContentType 'application/x-www-form-urlencoded' `
        -Uri "https://login.microsoftonline.com/$([uri]::EscapeDataString($env:TENANT))/oauth2/token" `
        -Body @{ grant_type = 'client_credentials'; client_id = $env:CLIENT; client_secret = $env:SECRET; resource = 'https://manage.devcenter.microsoft.com' }
}
catch {
    throw 'The Store login could not reach Microsoft Entra at all; that is the network, not the secret. Run it again.'
}
if ($token.StatusCode -ne 200) {
    $code = ''
    try {
        $answer = $token.Content | ConvertFrom-Json
        $sts = [regex]::Match([string]$answer.error_description, 'AADSTS\d+').Value
        $code = " ($($answer.error)$(if ($sts) { ", $sts" }))"
    }
    catch { }
    Write-Summary "### The Store login failed: Microsoft Entra answered $($token.StatusCode)$code."
    throw "The Store login failed: the Store secret may have expired, or it or the tenant or client ID may be wrong. Entra answered $($token.StatusCode)$code. AADSTS7000222 means the secret has expired and AADSTS7000215 that it is wrong. $renew"
}
$access = ($token.Content | ConvertFrom-Json).access_token
if ($env:GITHUB_ACTIONS) { Write-Host "::add-mask::$access" }
Write-Summary "Microsoft Entra gave the Store API a token: HTTP $($token.StatusCode)."

# 2. One read. GET the product; nothing here can create, change or submit anything.
try {
    $read = Invoke-WebRequest -Method Get -SkipHttpErrorCheck -Headers @{ Authorization = "Bearer $access" } `
        -Uri "https://manage.devcenter.microsoft.com/v1.0/my/applications/$([uri]::EscapeDataString($env:PRODUCT))"
}
catch {
    throw 'The Store login got a token but could not reach the Store API; that is the network, not the secret. Run it again.'
}
switch ($read.StatusCode) {
    200 {
        $product = $read.Content | ConvertFrom-Json
        Write-Summary "### The Store login works: the Store API answered $($read.StatusCode) and the product is named $($product.primaryName). Nothing was submitted."
        Write-SubmissionState $product $access
    }
    { $_ -in 401, 403 } {
        Write-Summary "### The Store API refused the product: HTTP $($read.StatusCode)."
        throw "The Store login got a token, but the Store refused it ($($read.StatusCode)). The application grouplab-store-publisher needs the Manager role in Partner Center, under Account settings, User management, Microsoft Entra applications, and its tenant must be associated with the account."
    }
    404 {
        Write-Summary "### The Store API does not know the product: HTTP 404."
        throw 'The Store login works, but the Store has no product with the ID in STORE_PRODUCT_ID, or this account does not own it. Check it against Product identity in Partner Center.'
    }
    default {
        Write-Summary "### The Store API answered $($read.StatusCode)."
        throw "The Store login got a token, and the Store API answered $($read.StatusCode), which is its side, not the secret. Run it again later."
    }
}
