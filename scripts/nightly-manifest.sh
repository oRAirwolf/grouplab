#!/usr/bin/env bash
# Writes, signs and checks the nightly's two update manifests from the files in release/. NOTES-FROM-PLANNING.md entries 119, 139,
# 347, 386 and 450.
#
#   VERSION=<version> SHA=<commit> GROUPLAB_UPDATE_SIGNING_KEY=<key> bash scripts/nightly-manifest.sh
#
# The manifest is what the updater in GroupLab reads: what this build is, what it offers, and the SHA-256 of every file, signed so the
# application can tell it from a forgery. Entry 450 section 2: the desktop builds are published without waiting for the Android build,
# so nightly.yml runs this twice. The publish job runs it over the desktop files; the job that adds the Android files runs it again once
# they exist, so the manifest on both releases lists the Android downloads too, and the phones' updater finds them. Both runs read the
# same notes.md and versions.json, which the publish job hands on as an artifact.
#
# The fingerprint library (entry 347) is written only when release/ does not already hold it, because the second run must list the
# copy already published, byte for byte.
set -euo pipefail

dotnet build src/GroupLab.Cli --configuration Release
base="https://github.com/oRAirwolf/grouplab/releases/download/nightly"
if [ ! -f release/grouplab-target-library.json ]; then
  dotnet run --project src/GroupLab.Cli --configuration Release --no-build -- target-reference library release/grouplab-target-library.json
fi
dotnet run --project src/GroupLab.Cli --configuration Release --no-build -- update-manifest \
  --version "$VERSION" --train nightly --commit "$SHA" --notes notes.md --versions versions.json \
  --out release/update-manifest.json \
  --asset windows installer release/grouplab-setup-win-x64.exe "$base/grouplab-setup-win-x64.exe" \
  --asset windows zip release/grouplab-win-x64.zip "$base/grouplab-win-x64.zip" \
  --asset linux tarball release/grouplab-linux-x64.tar.gz "$base/grouplab-linux-x64.tar.gz" \
  --asset library targets release/grouplab-target-library.json "$base/grouplab-target-library.json" \
  $( [ -f release/grouplab-android-dev.apk ] && echo --asset android apk-dev release/grouplab-android-dev.apk "$base/grouplab-android-dev.apk" ) \
  $( [ -f release/grouplab-android.apk ] && echo --asset android apk release/grouplab-android.apk "$base/grouplab-android.apk" )

# Checked with the key compiled into the build being published, not with the key that signed it: that is the stronger question, because
# a build that cannot verify its own updates is broken however well the manifest was signed. A commit from before the key was set fails
# here by design, and the message says NoKey.
dotnet run --project src/GroupLab.Cli --configuration Release --no-build -- update-check release/update-manifest.json
dotnet run --project src/GroupLab.Cli --configuration Release --no-build -- update-check release/update-manifest-2.json
