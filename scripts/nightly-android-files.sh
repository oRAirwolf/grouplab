#!/usr/bin/env bash
# Adds a nightly's signed Android files to its own release and to the moving release, with the manifest signed again so it lists them.
# NOTES-FROM-PLANNING.md entries 450 and 454.
#
#   VERSION=<version> SHA=<commit> COMMIT=<short commit> GROUPLAB_UPDATE_SIGNING_KEY=<key> bash scripts/nightly-android-files.sh
#
# It expects the Android build's files in signed/, the desktop packages in assets/, and the publish job's notes.md and versions.json
# here. nightly.yml runs it once the Android build has made its files; android-files.yml runs it for a nightly whose Android files did not
# get added (nightly 203, entry 454 section 1), from that nightly run's own artifacts, so both go through the same steps.
set -euo pipefail

mkdir -p release
# The desktop files the published manifest lists, from the same packages, and the fingerprint library as published, byte for byte.
find assets -type f \( -name '*.zip' -o -name '*.exe' -o -name '*.tar.gz' \) -exec cp {} release/ \;
gh release download "v$VERSION" -p grouplab-target-library.json -D release
# Entry 219 item A6: the signed Android files.
cp signed/grouplab-android.apk signed/grouplab-android.aab signed/grouplab-android-dev.apk release/
# Named like the other builds on the numbered release, so a bug report names one; the plain names go on the rolling release.
# Entry 224: nightly 109 built and gathered them, and neither upload list carried them.
for ext in apk aab; do cp "release/grouplab-android.$ext" "release/grouplab-$VERSION-android-$COMMIT.$ext"; done
cp release/grouplab-android-dev.apk "release/grouplab-$VERSION-android-dev-$COMMIT.apk"
# Entry 231: the symbols belong to exactly one AAB, so they go on the numbered release only.
if [ -f signed/grouplab-android-native-symbols.zip ]; then
  cp signed/grouplab-android-native-symbols.zip "release/grouplab-$VERSION-android-native-symbols-$COMMIT.zip"
fi
ls -la release

bash scripts/nightly-manifest.sh

manifests="release/update-manifest.json release/update-manifest-2.json"
gh release upload "v$VERSION" --clobber $manifests release/grouplab-*-android-*.apk release/grouplab-*-android-*.aab $(ls release/grouplab-*-android-native-symbols-*.zip 2>/dev/null || true)
if gh release view nightly --json body --jq .body | grep -qF "**$VERSION**"; then
  gh release upload nightly --clobber $manifests release/grouplab-android.apk release/grouplab-android.aab release/grouplab-android-dev.apk
  echo "### Nightly $VERSION: its Android files are on its own release and on the moving release" >> "$GITHUB_STEP_SUMMARY"
else
  echo "::warning::the moving release has moved on to a newer build, so the Android files are on v$VERSION only"
fi
