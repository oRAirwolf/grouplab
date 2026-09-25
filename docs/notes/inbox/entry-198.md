## 2026-09-25, entry 198: the Android application starts

Alan, 2026-09-25: start Android development now. Do this after entries 196 and 197; it is large, and this entry is its first stage only.
`docs/PLATFORM-SUPPORT.md` already calls Android planned and high priority, and `docs/MOBILE-CAPTURE.md` is the capture contract
written for it. Both hold.

## 1. Alan's decisions

1. **Scope of the first version: full analysis on the phone, offline.** Take the picture (the capture screen of MOBILE-CAPTURE.md),
   detect, review and correct, the group figures, save sessions. The same engine as the desktop, not a second one. Printing targets,
   the target library editor and Ballistics stay desktop only at first. Ranges often have no signal: nothing in the first version may
   need the network except sending a target and error reports, which queue as they do on the desktop.
2. **Distribution: Google Play testing tracks plus a nightly APK** on the GitHub release and grouplab.org for sideloading. Alan has
   paid the Google Play developer fee. A newer personal Play account must run a closed test with at least 12 testers for 14 days
   before a production listing; the Discord server is where those testers come from. Plan for it, do not promise dates.
3. **Package name: `org.grouplab.app`.** Permanent once on Play.
4. **Sessions between phone and desktop.** Alan's wish: sign in with Google, Microsoft or Apple and use that platform's own storage to
   share files between the apps, with no server of ours. If that is a lot of work, start with sharing files by hand. The planning
   session's reading, for you to confirm or correct with reasons:
   - **Stage A, first version:** share and open a session file by hand (Android share sheet, Drive, email, USB). Nothing else.
   - **Stage B, cheap and close to his wish:** a "sync folder" setting on both. On Android the user picks a folder through the Storage
     Access Framework, which Google Drive and OneDrive both provide as document providers; on the desktop the user picks the folder the
     Google Drive or OneDrive client already syncs. Both apps read and write sessions there. No sign in, no app registrations, no
     tokens, and it uses each person's own storage. Say whether SAF providers are reliable enough for this (conflicts, offline edits).
   - **Stage C, only if B falls short:** sign in and the providers' APIs (Drive app data folder, OneDrive app folder through Microsoft
     Graph). Needs a Google OAuth client and consent screen and an Entra app registration, which Alan would create. **Apple is out**
     for now: iCloud needs the paid Apple developer program, which Alan will not pay for a platform he does not own.
   - Whatever the stage, the session file format is the unit, a session edited on two devices must never silently lose one side's
     changes, and photographs keep the rules they have today: no GPS, location or time metadata read, printed or logged.

## 2. The first stage: prove the engine runs on the phone

Before any screen is designed, answer the questions that decide the architecture, with measurements, in a new `docs/ANDROID.md`:

1. **UI.** Avalonia on .NET Android, sharing `GroupLab.Core` and as much of `GroupLab.App` as fits a touch screen, is the obvious path.
   Confirm it, or say why not. No second codebase in another language unless the measurements force it.
2. **OpenCV on Android.** The detector uses OpenCvSharp, whose official runtimes are Windows, Linux and macOS. Find what runs on
   android-arm64: a community runtime (for example the Sdcb mini runtimes on NuGet), OpenCV's own Android build under the existing
   wrapper, or replacing the few OpenCV calls GroupLab actually makes with managed code. List the calls the detector uses, decide, and
   check the license of whatever is chosen against GPL-3.0.
3. **Camera.** Avalonia has no camera. Name the route: CameraX through the .NET Android bindings is likely. It must give full
   resolution stills, the lens choice of MOBILE-CAPTURE.md item on focal length, focus and exposure control, and a live preview fast
   enough for the capture conditions.
4. **Speed and memory.** A spike APK that loads the sample scan and a phone photograph from the app's own assets, runs detection,
   and prints the time and peak memory. Target devices: Alan's **Samsung Galaxy Z Fold 7** (daily phone; it has a narrow cover screen and a
   wide inner screen, so both layouts matter) and his **Galaxy Tab S8 Ultra**. Say what the desktop takes for the same image.
5. **Minimum Android version.** Choose the lowest that CameraX, the chosen OpenCV route and .NET 10 support without special cases,
   and say what share of devices that leaves out.
6. **Builds.** A CI job that builds a debug APK on every push to main that touches the app or Core, and a signed release APK and AAB
   for nightlies once signing exists (section 3). Unsigned debug APKs are fine until then; never publish one as a nightly.

Stop after the spike and the document, with a report in plain words: does detection run on the Fold 7, how fast, and what the plan is.
No screens beyond what the spike needs.

## 3. What only Alan can do, as requests in for-alan.md, when you reach them

1. **The phone for testing:** Developer options on, Wireless debugging (or USB debugging) on, and the one `adb pair` or `adb connect`
   line for this machine. He keeps Android tools in `C:\Dev\tools` for another project; use those or say what to install.
2. **The .NET Android workload** if it is missing: the exact command, since it installs software.
3. **The signing key, later:** Alan generates the upload keystore himself with `keytool`, keeps it outside the repository, and puts it
   in GitHub secrets; Play App Signing holds the app key. You never read, copy or print the keystore or its passwords, exactly as with the
   SSH key. Write the exact commands and the secret names when the release build needs them, not before.
4. **The Play Console listing,** when there is something to put on it: the app entry, the closed testing track and the testers list.

## 4. Things to keep in view, not to act on now

- **The GPL app store permission.** Alan approved a draft GPL section 7 additional permission for app stores and is having an attorney
  review it before it is committed. Internal and closed testing can go ahead; a public Play listing waits on that review. Say so in
  `docs/ANDROID.md` and in STATE.md.
- **iOS is not planned.** Alan owns an iPad Mini for testing the website only.
- The capture screen's specification, `docs/MOBILE-CAPTURE.md`, is the contract; where Android makes an item impossible, say which and why.
