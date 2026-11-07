"""NOTES-FROM-PLANNING.md entry 480 section 2: moves a workflow's iOS jobs from macos-26 and Xcode 26, with the workload held at 26.5.10318,
to GitHub's xcode-27 image, Xcode 27.1 and the iOS workload set named, and (in ios-app.yml) the iOS 27 simulators and an iPhone Duo leg.

Not applied yet: the first workload set for Xcode 27.1, 10.0.402.1, trims away members of Avalonia's iOS types (dotnet/macios issue 26969,
fixed upstream, waiting for the next .NET 10 release). When a workload set with the fix ships:

    python scripts/ios-xcode27.py .github/workflows/ios-app.yml <workload set version>

prove ios-app.yml green, then the same on .github/workflows/nightly.yml, and change IosSigningTests' runs-on line with it.
"""
import pathlib
import sys

CR = chr(13) + chr(10)
LF = chr(10)
path = pathlib.Path(sys.argv[1])
workload = sys.argv[2]
raw = path.read_bytes().decode("utf-8")
crlf = CR in raw
t = raw.replace(CR, LF)


def sub(old, new, count=None):
    global t
    n = t.count(old)
    assert n > 0 and (count is None or n == count), (old[:70], n)
    t = t.replace(old, new)
    return n


xcode_old = """      - name: Xcode 26
        run: |
          xcode=$(ls -d /Applications/Xcode_26*.app | grep -iv beta | tail -1)
          sudo xcode-select -s "$xcode"
          xcodebuild -version | head -1 | grep -q '^Xcode 26' || { echo "::error::No Xcode 26 on this runner"; exit 1; }
          xcodebuild -version | head -1
"""
xcode_new = """      # Entry 480 section 2: Xcode 27.1, the first that builds an application filling the iPhone Duo's inner screen. GitHub's xcode-27
      # image carries it; the lowest iOS the application runs on stays 26 (SupportedOSPlatformVersion).
      - name: Xcode 27.1
        run: |
          xcode=$(ls -d /Applications/Xcode_27.1*.app | grep -iv -e beta -e release_candidate | head -1)
          test -n "$xcode" || { ls -d /Applications/Xcode*.app; echo "::error::No Xcode 27.1 on this runner"; exit 1; }
          sudo xcode-select -s "$xcode"
          xcodebuild -version | head -1 | grep -q '^Xcode 27\\.1' || { echo "::error::No Xcode 27.1 on this runner"; exit 1; }
          xcodebuild -version | head -1
"""
n = sub(xcode_old, xcode_new)

work_old = """      # The newest iOS workload wants the Xcode after 26 (27.0.10722 refused Xcode 26.6), so the workload is held to the newest one made
      # for Xcode 26. The manifest's band is 10.0.100 whatever the SDK's band is.
      - name: The .NET iOS workload
        run: |
          ls -d /Applications/Xcode_26*.app | tr '\\n' ' '; echo
          dotnet workload config --update-mode manifests > /dev/null
          echo '{"microsoft.net.sdk.ios": "26.5.10318/10.0.100"}' > "$RUNNER_TEMP/ios-rollback.json"
          dotnet workload install ios --from-rollback-file "$RUNNER_TEMP/ios-rollback.json" > workload.log 2>&1 || { tail -40 workload.log; exit 1; }
          dotnet workload list | grep -i ios
"""
work_new = """      # Entry 480 section 2: a workload set Microsoft made for Xcode 27.1, with dotnet/macios issue 26969 fixed. It replaces the rollback
      # that held the workload at 26.5.10318 for Xcode 26.
      - name: The .NET iOS workload
        run: |
          dotnet --version
          dotnet workload config --update-mode workload-set > /dev/null
          dotnet workload install ios --version {WORKLOAD} > workload.log 2>&1 || { tail -40 workload.log; exit 1; }
          dotnet workload list | grep -i ios
"""
assert sub(work_old, work_new.replace("{WORKLOAD}", workload)) == n
assert sub("runs-on: macos-26", "runs-on: xcode-27") == n

if "ios-app" in path.name:
    sub("""# NOTES-FROM-PLANNING.md entry 290 section 2 items 3 and 4: the iPhone and iPad application (ios/GroupLab.iOS) built on GitHub's macos-26
# machine with Xcode 26, and proved""", """# NOTES-FROM-PLANNING.md entry 290 section 2 items 3 and 4: the iPhone and iPad application (ios/GroupLab.iOS) built on GitHub's xcode-27
# machine with Xcode 27.1 (entry 480), and proved""")
    sub("""if "iOS-26" in runtime for d in ds if d["name"].startswith("iPhone")]""",
        """if "iOS-27" in runtime for d in ds if d["name"].startswith("iPhone") and "Duo" not in d["name"]]""")
    sub("""if "iOS-26" in runtime for d in ds if d["name"].startswith("iPad")]""", """if "iOS-27" in runtime for d in ds if d["name"].startswith("iPad")]""")
    sub("- name: Boot an iOS 26 simulator", "- name: Boot an iOS 27 simulator")
    sub('echo "::error::No iOS 26 iPhone simulator"', 'echo "::error::No iOS 27 iPhone simulator"')
    sub('echo "::warning::No iOS 26 iPad simulator on this runner"', 'echo "::warning::No iOS 27 iPad simulator on this runner"')
    duo = """            xcrun simctl shutdown "$IPAD" || true
          fi
          # Entry 480 section 3: the iPhone Duo, in the simulator only (nobody on the project owns one). The same tour upright and on its side,
          # and the size of the screen each shows, so its outer and inner screens' sizes come from the simulator rather than from reports.
          duo=$(xcrun simctl list devices available -j | python3 -c '
          import json, sys
          devices = json.load(sys.stdin)["devices"]
          found = [(runtime, d) for runtime, ds in devices.items() if "iOS-27" in runtime for d in ds if "Duo" in d["name"]]
          found.sort(key=lambda p: p[0])
          print(found[-1][1]["udid"] if found else "")')
          if [ -n "$duo" ]; then
            xcrun simctl shutdown "$UDID" || true
            xcrun simctl boot "$duo"
            xcrun simctl bootstatus "$duo" -b > /dev/null
            xcrun simctl status_bar "$duo" override --time 9:41 --batteryState charged --batteryLevel 100 || true
            xcrun simctl install "$duo" "$APP"
            mkdir -p shots/duo
            xcrun simctl launch "$duo" org.grouplab.app
            sleep 20
            xcrun simctl io "$duo" screenshot shots/duo/00-first-start.png > /dev/null
            xcrun simctl terminate "$duo" org.grouplab.app || true
            data=$(xcrun simctl get_app_container "$duo" org.grouplab.app data)
            mkdir -p "$data/Documents/selftest"
            cp samples/gl-cf25-ltr-d-25-shots-600-dpi.png "$data/Documents/selftest/sample.png"
            tour "$duo" shots/duo --screens || failed=1
            tour "$duo" shots/duo-landscape --screens --landscape || failed=1
            for shot in shots/duo/00-first-start.png shots/duo/*result*.png shots/duo-landscape/*result*.png; do
              [ -f "$shot" ] && echo "$shot: $(sips -g pixelWidth -g pixelHeight "$shot" | awk '/pixel/ {printf "%s ", $2}')pixels"
            done | tee shots/duo/sizes.txt
            xcrun simctl shutdown "$duo" || true
          else
            echo "::warning::No iPhone Duo simulator on this runner"
          fi
          [ "$failed" = 0 ] || { echo "::error::A tour did not reach the sample's result"; exit 1; }
"""
    sub("""            xcrun simctl shutdown "$IPAD" || true
          fi
          [ "$failed" = 0 ] || { echo "::error::A tour did not reach the sample's result"; exit 1; }
""", duo, count=1)

out = t.replace(LF, CR) if crlf else t
path.write_bytes(out.encode("utf-8"))
print(path.name, "jobs changed:", n)
