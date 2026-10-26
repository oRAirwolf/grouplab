# NOTES-FROM-PLANNING.md entry 475: the app clips for the launch videos, made by script from any later build.
#
#   pwsh scripts/video-clips/make-clips.ps1 [-Out C:\Dev\grouplab-local\video-clips] [-Only v2-ch05]
#
# Each clip is a test in tests/GroupLab.App.Tests/VideoClips (the computer, 2560 by 1440) or tests/GroupLab.Mobile.Tests/VideoClips (the
# phone, the Fold 7's 1080 by 2520), skipped everywhere unless GROUPLAB_VIDEO_CLIPS names the folder the clips go to, so CI never makes them.
# Frames are GroupLab's own drawing in the headless platform with a drawn pointer, or a ring where a finger taps on the phone, encoded at 60
# frames a second as H.264 MP4 by Windows' own encoder through OpenCV (so Windows only). Beside each clip is its last frame as a PNG, and
# clips.csv lists every clip with what it shows, its length and size, and the build it came from. Nothing is committed: video stays out of
# the repository.
param(
    [string]$Out = "C:\Dev\grouplab-local\video-clips",
    [string]$Only = ""
)
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force $Out | Out-Null
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$temp = Join-Path $repo "bin\alt\t"
New-Item -ItemType Directory -Force $temp | Out-Null
$env:GROUPLAB_VIDEO_CLIPS = $Out
$env:TMP = $temp
$env:TEMP = $temp
$filter = "FullyQualifiedName~VideoClips"
if ($Only) {
    # A clip's name without its dashes is in its test's name: v2-ch05 matches V2Ch05...
    $filter = "FullyQualifiedName~VideoClips&FullyQualifiedName~" + (($Only -split "-" | ForEach-Object { $_.Substring(0, 1).ToUpper() + $_.Substring(1) }) -join "")
}
foreach ($project in "tests/GroupLab.App.Tests", "tests/GroupLab.Mobile.Tests") {
    dotnet test (Join-Path $repo $project) --filter $filter 2>&1 | Select-String -Pattern "Passed!|Failed!|\[FAIL\]"
}
Remove-Item Env:\GROUPLAB_VIDEO_CLIPS
Write-Host "Clips in $Out; clips.csv lists them."
