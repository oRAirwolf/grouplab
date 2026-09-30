#!/bin/sh
# NOTES-FROM-PLANNING.md entry 290 section 2 item 7 and entry 292 section 2.3: the signed publish's argument building, proved with made-up
# values where there are no Apple secrets. scripts/ios-signing.py --properties builds the properties the nightly's "Sign and send" step gives
# dotnet publish; MSBuild then evaluates each project with them and must give the application the application's profile and the share
# extension the extension's, never one profile to both. Nothing is signed and no secret is read. Run from the repository's root on a Mac
# with the .NET iOS workload.
set -eu
app_uuid=11111111-2222-3333-4444-555555555555
share_uuid=66666666-7777-8888-9999-aaaaaaaaaaaa
properties=$(python3 scripts/ios-signing.py --properties "Apple Distribution: Made Up (ABCDE12345)" "$app_uuid" "$share_uuid")
echo "$properties" | grep -q "CodesignProvision=" && { echo "::error::The publish would give one profile to every project"; exit 1; }

# Each line is one argument; the identity holds spaces, so they are passed one by one.
evaluate() {
  project="$1"
  shift
  set --
  while IFS= read -r line; do
    set -- "$@" "$line"
  done <<EOF
$properties
EOF
  dotnet msbuild "$project" -getProperty:CodesignProvision -getProperty:CodesignKey -p:RuntimeIdentifier=ios-arm64 -p:Configuration=Release "$@"
}

app=$(evaluate ios/GroupLab.iOS/GroupLab.iOS.csproj)
share=$(evaluate ios/GroupLab.Share/GroupLab.Share.csproj)
echo "application: $(echo "$app" | tr -d '\n ')"
echo "extension: $(echo "$share" | tr -d '\n ')"
echo "$app" | grep -q "\"CodesignProvision\": \"$app_uuid\"" || { echo "::error::The application would not be signed with its own profile"; exit 1; }
echo "$share" | grep -q "\"CodesignProvision\": \"$share_uuid\"" || { echo "::error::The share extension would not be signed with its own profile"; exit 1; }
echo "$app$share" | grep -c "Apple Distribution: Made Up" | grep -q 2 || { echo "::error::The distribution identity does not reach both projects"; exit 1; }
echo "The signed publish gives the application and the share extension each its own profile."
