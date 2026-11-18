# GroupLab for Mac on the Mac App Store

NOTES-FROM-PLANNING.md entry 501 (2026-11-18, on Alan's word): the desktop application, the same one as the notarized Mac download, on
the Mac App Store, as far as TestFlight's internal group. **Nothing is submitted for review without Alan's word**, as 1.0 for the iPhone
waited for his "Submit now" (entry 491). The README, the site and the guides say nothing about it until the Mac App Store copy is released.

## The two copies

| | The download (unchanged) | The Mac App Store copy |
|---|---|---|
| Bundle identifier | `org.grouplab.GroupLab` | `org.grouplab.app`, the iPhone and iPad app's |
| Signed with | Developer ID Application, notarized and stapled | Apple Distribution (request 55's certificate), in an installer signed with Mac Installer Distribution (request 110) |
| Sandbox | none (hardened runtime only) | App Sandbox, `scripts/macos/GroupLab.store.entitlements` |
| Updates | GroupLab's own updater offers the download page | the Mac App Store; GroupLab's updater is off (`GroupLabDistribution=store`, as the Microsoft Store copy) |
| Its records | `~/.config/GroupLab` | the same path inside the sandbox's container, `~/Library/Containers/org.grouplab.app/Data` |
| Built by | `package.yml`, in every nightly | `mac-store.yml`, after every nightly, Apple silicon only |

The two identifiers differ on purpose: both copies can be on one Mac, each with its own records. A person moving from the download to the
store copy brings their records with Settings, Sync and your data, **Export all my data**, and **Import** in the store copy; the store copy
starts with none, because the sandbox cannot see the download's folder.

## The App Store record (entry 501 section 2)

**Universal purchase: macOS added to GroupLab's existing record.** One listing, one set of App Privacy answers, ratings and the name
shared, and a person who has GroupLab on the iPhone sees it as already theirs on the Mac. It needs the same bundle identifier, which is why
the store copy is `org.grouplab.app`. A separate Mac record would need its own name (GroupLab is taken by the iPhone record), its own
privacy answers kept in step by hand, and gives nothing in return. The App Privacy answers given for the iPhone hold for the Mac copy: it
sends the same error reports and the same opt-in survey, and nothing else (the target library is read, not sent to).

What the App Store Connect API cannot do, or what needs a certificate's private key, was request 110, done by Alan on 2026-11-18 (now in `docs/notes/for-alan-archive.md`): macOS
added to the record, the Mac Installer Distribution certificate, and the Mac App Store profile. The Apple Distribution certificate and the
API key are request 55's.

## The sandbox audit (entry 501 section 1)

Read from the code on 2026-11-18. Each line: what GroupLab does on a Mac, what the sandbox needs for it, and what was done.

### Files

| What | Needs | Result |
|---|---|---|
| Settings, `grouplab.db`, `records.json`, `sheets/`, `copies/`, trash, `pending-targets/`, `shared-sessions/`, `demo/` (`AppSettings` beside `settings.json`, `~/.config/GroupLab`) | nothing: in the sandbox the home folder is the container | **works as is**, in the container |
| `updates/`, `library/`, `pasted/`, `blanked/` (`~/Library/Application Support/GroupLab`) | nothing | **works as is**, in the container |
| Logs, crash records (`~/Library/Logs/GroupLab`, `LogDirectory`) | nothing | **works as is**, in the container |
| Temporary files: report zips, print PDFs (`Path.GetTempPath()`) | nothing: the sandbox gives the container its own | **works as is** |
| The bundled targets (`AppContext.BaseDirectory/targets`), read only | nothing | **works as is**; nothing writes beside the executable |
| A picture the person opens (dialog, drag and drop, paste) | the user-selected files entitlement; drag and drop grants the dropped file | **works as is** while the choice holds |
| **A session's picture, reopened later by path** (`SessionRecord.ImagePath`: Open a session, pools, sync's originals, sending, error reports) | access after the choice is gone | **changed (small):** in the sandbox the picture is copied into GroupLab's own `originals/` folder when it opens, named by its SHA-256 so each picture is kept once, and the session points at the copy (`MainWindow.KeptHere`). Elsewhere nothing changes. |
| **The sync folder**, reopened by path every five minutes | a security-scoped bookmark | **changed (medium):** the bookmark is made when the folder is chosen and resolved, with access started, before the first run (`MacSandbox.Bookmark`, `MacSandbox.Resolve`, `syncBookmark` in the settings) |
| Open marking's photo, beside the marking file | access to the photo | **works as is**: a photo the sandbox will not show counts as missing, and GroupLab asks for it (entry 383) |
| **Save for a printer app**, a picture a page and a PDF beside the one chosen | access to the files beside it | **changed (small):** in the sandbox it asks for a folder instead |
| Exports, reports, Save marking, scale markers, shared session files | the user-selected files entitlement | **works as is**: each is a file or folder chosen in a dialog |
| Existing Mac users' records | none reachable | the store copy starts fresh; Export all my data and Import move them (above) |

### Devices

| What | Result |
|---|---|
| A camera | the desktop has none; no entitlement |
| The M834 over Bluetooth | Windows only (`WindowsSerialPrinter`); a Mac says it is not here. No Bluetooth entitlement |
| The M220 | files only, chosen in a dialog; **works as is** |
| Printing | a Mac opens the PDF in its viewer to print; the print entitlement is asked for so the viewer's printing works from the container's file |

### Network

The target library refresh (github.com and grouplab.org), the update manifest (not read in the store copy), error reports, Send to GroupLab,
target submissions and the survey are all outbound HTTPS: **the network client entitlement**, and nothing listens on a port.

### .NET and native code

The JIT needs `com.apple.security.cs.allow-jit`, which the sandbox allows and the store accepts. The download's other two hardened
runtime exceptions are left out: .NET writes no executable memory it does not map for the JIT, and every library in the store copy is signed
by the same team, so library validation holds. OpenCV, Skia, HarfBuzz and SQLite are inside the bundle and signed there; nothing is loaded
from outside it but `/usr/lib/libobjc.dylib`, which the sandbox allows.

### Opening links and files

The download opens a link, a PDF or a folder by starting `open`. In the store copy they go through NSWorkspace (`MacSandbox.Open`, installed
as `TheOutsideWorld.OpensThings`), which the sandbox allows.

### The updater

Off in the store copy (`AppInfo.FromStore`): no check, no download, no offer, and Settings says "This copy of GroupLab came from the Mac App
Store, which keeps it up to date, so GroupLab's own updater is off."

### Temporary exceptions

**None.** Nothing GroupLab does needs one.

## The build (entry 501 section 3)

`mac-store.yml` runs after each nightly on GitHub's Mac machines, for Apple silicon (the Intel download is untested and stays a download):

1. `dotnet publish` with `-p:GroupLabDistribution=store`.
2. `scripts/macos-bundle.py --store-build <nightly number>`: identifier `org.grouplab.app`, version 1.0 and the nightly's number as the
   build (entry 488's numbering for the App Store), `ITSAppUsesNonExemptEncryption` false, and the profile inside the bundle.
3. **Inside the sandbox, every time:** signed ad hoc with the sandbox's own keys, the copy runs `--self-check` (`SelfCheck`): it makes the
   demo's sample sheet, opens it, waits for the sheet to be read, accepts and analyzes it, checks a session was saved, that the picture was
   kept in `originals/`, and that a bookmark of a folder leads back to it, and writes `self-check.json` in the container. The job fails when
   any step does, and the summary shows the file.
   **Found on the first runs, 2026-11-18:** signed ad hoc, the copy starts in the sandbox (macOS logs the AppSandbox set up and makes the
   container) and is then stopped: the sandbox refuses it the connection to LaunchServices that every window application needs ("Process
   unable to create connection because the sandbox denied the right to lookup com.apple.coreservices.launchservicesd", then "unable to get
   application ASN from launchservicesd ... aborting"). A copy signed for the store cannot be run on the runner either, since it runs only
   once installed from TestFlight or the store. So the step reports what happened in the summary and does not turn the run red; the
   proof that the copy reads a sheet and saves a session inside the sandbox is either a development signature (an Apple Development
   certificate and a Mac development profile, a later request) or the TestFlight build opened on a real Mac.
4. **With request 110's secrets:** signed for the store with the hardened runtime, packaged with `productbuild` and signed with the Mac
   Installer Distribution certificate, and sent to TestFlight with `xcrun altool --upload-app --type macos`. TestFlight's internal group
   only; nothing is submitted for review. .NET's `createdump` is left out of the store copy (a second executable the store refuses without its own sandbox, and one that only writes a crash dump when a setting nobody sets asks for one), and the command line `grouplab` inside the bundle is signed into the app's sandbox with `com.apple.security.inherit`, so it runs only when GroupLab starts it. `testflight.py --mac-internal` then gives the build to GroupLab Team; the half-hourly TestFlight step reads the iPhone and iPad builds only, so a Mac build never reaches Public Beta. **First done for nightly 233, 2026-11-18:** Apple processed it as valid and GroupLab Team has it.

Store screenshots for the Mac, the listing's Mac text and a last look at the App Privacy answers come with the submission entry, once a
TestFlight build has been installed and opened on a real Mac.
