<#
.SYNOPSIS
    Proves GroupLab can sign in to the Microsoft Store, and submits nothing. NOTES-FROM-PLANNING.md entry 293 section 2.

.DESCRIPTION
    Reads the Store's credentials from the environment, as release.yml passes them from the repository's secrets and variables: TENANT
    (AZURE_AD_TENANT_ID), CLIENT (AZURE_AD_APPLICATION_CLIENT_ID), SECRET (AZURE_AD_APPLICATION_SECRET) and PRODUCT (STORE_PRODUCT_ID).
    It asks Microsoft Entra for a token for the Store submission API, then reads the product back, which is a read and changes nothing.

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
        $name = ($read.Content | ConvertFrom-Json).primaryName
        Write-Summary "### The Store login works: the Store API answered $($read.StatusCode) and the product is named $name. Nothing was submitted."
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
