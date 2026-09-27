<#
.SYNOPSIS
    Puts the newest GroupLab Dev on the phone, starts it, and collects its screen, its own log and the logcat into one folder.

.DESCRIPTION
    NOTES-FROM-PLANNING.md entry 234 section 3: entry 232's steps, as one script. It connects to the phone over wireless debugging (mDNS
    first, and only if that finds nothing it asks for the address shown under Wireless debugging), installs or updates the development
    build org.grouplab.app.dev beside any copy from Google Play, starts it, takes a screenshot, and pulls the application's own log with
    run-as, which works because the development build is debuggable, and the logcat lines GroupLab writes. Everything goes in
    C:\Dev\grouplab-local\android-<build>\, which is never committed.

    The phone's address is never written to any file: adb is told it, and nothing records it.

.PARAMETER Apk
    The APK to install. By default the newest nightly's grouplab-android-dev.apk, downloaded with gh into a temporary folder.

.PARAMETER Out
    Where to put what is collected. By default C:\Dev\grouplab-local\android-<the build's version>.

.PARAMETER Screens
    How many screenshots to take, a few seconds apart, after starting.

.EXAMPLE
    pwsh scripts/android/Test-OnPhone.ps1
#>
[CmdletBinding()]
param(
    [string]$Apk,
    [string]$Out,
    [int]$Screens = 1
)

$ErrorActionPreference = 'Stop'
$Adb = 'C:\Dev\tools\android-sdk\platform-tools\adb.exe'
$Package = 'org.grouplab.app.dev'
if (-not (Test-Path $Adb)) { throw "adb is not at $Adb; entry 225 installed the Android SDK there." }

function Adb { & $Adb @args; if ($LASTEXITCODE -ne 0) { throw "adb $($args -join ' ') failed" } }

# 1. The phone. mDNS finds a phone with Wireless debugging on without anybody typing an address; the Bonjour backend is the one that works
#    on this machine (entry 232), so the server is restarted with it when no phone is attached.
$attached = (& $Adb devices) -match "`tdevice$"
if (-not $attached) {
    # The server is restarted only when nothing is attached at all, because a restart drops a phone on USB too.
    if (-not ((& $Adb devices) -match "`t")) {
        & $Adb kill-server 2>$null | Out-Null
        $env:ADB_MDNS_OPENSCREEN = '0'
        & $Adb start-server | Out-Null
        Start-Sleep -Seconds 5
    }
    $service = (& $Adb mdns services) -match '_adb-tls-connect' | Select-Object -First 1
    if ($service) {
        $target = ($service -split '\s+' | Where-Object { $_ -match ':\d+$' } | Select-Object -Last 1)
        if ($target) { & $Adb connect $target | Out-Null; Start-Sleep -Seconds 2 }
    }
    $attached = (& $Adb devices) -match "`tdevice$"
    # Asked only when somebody is there to answer; run unattended, it stops and says why instead of waiting.
    if (-not $attached -and [Environment]::UserInteractive -and -not [Console]::IsInputRedirected) {
        $address = Read-Host 'No phone was found. On the phone: Settings, Developer options, Wireless debugging; type the IP address and port shown there'
        if ($address) { & $Adb connect $address | Out-Null; $attached = (& $Adb devices) -match "`tdevice$" }
    }
}
if (-not $attached) { throw 'No phone is attached. Turn Wireless debugging on, or plug the phone in, and run this again.' }
if (@($attached).Count -gt 1) { Write-Host 'More than one device is attached; the first is used. Set ANDROID_SERIAL to choose.' }

# 2. The build to install.
if (-not $Apk) {
    $download = Join-Path ([IO.Path]::GetTempPath()) "grouplab-dev-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $download | Out-Null
    & gh release download nightly -R oRAirwolf/grouplab -p 'grouplab-android-dev.apk' -D $download
    if ($LASTEXITCODE -ne 0) { throw 'The newest nightly has no grouplab-android-dev.apk yet.' }
    $Apk = Join-Path $download 'grouplab-android-dev.apk'
}
Adb install -r $Apk | Out-Null
$version = ((& $Adb shell dumpsys package $Package) -match 'versionName=' | Select-Object -First 1) -replace '.*versionName=', ''
$version = $version.Trim()
if (-not $Out) { $Out = "C:\Dev\grouplab-local\android-$($version -replace '[^0-9A-Za-z.\-]', '_')" }
New-Item -ItemType Directory -Force -Path $Out | Out-Null
Write-Host "GroupLab Dev $version installed; collecting into $Out"

# 3. Start it, from a clean logcat, and take the screenshots.
Adb logcat -c
Adb shell monkey -p $Package -c android.intent.category.LAUNCHER 1 | Out-Null
for ($i = 1; $i -le $Screens; $i++) {
    Start-Sleep -Seconds 4
    $png = Join-Path $Out ("screen-{0:00}.png" -f $i)
    & $Adb shell screencap -p /sdcard/grouplab-screen.png | Out-Null
    & $Adb pull /sdcard/grouplab-screen.png $png | Out-Null
    & $Adb shell rm /sdcard/grouplab-screen.png | Out-Null
}

# 4. Its own log, which run-as reaches because the development build is debuggable, and the logcat's GroupLab lines.
$logs = (& $Adb shell run-as $Package find . -name '*.log') | Where-Object { $_ -match '\.log$' }
foreach ($log in $logs) {
    $name = Split-Path -Leaf $log.Trim()
    & $Adb exec-out run-as $Package cat $log.Trim() | Set-Content -Path (Join-Path $Out $name) -Encoding utf8
}
& $Adb logcat -d -s GroupLab | Set-Content -Path (Join-Path $Out 'logcat.txt') -Encoding utf8
if (Test-Path variable:download) { Remove-Item -Recurse -Force $download }
Write-Host "Done: $(@(Get-ChildItem $Out).Count) files in $Out"
