## 2026-09-25, entry 207: minimums approved; minimums for every platform; an opt-in hardware and benchmark survey everywhere

Read with entry 206, which this answers. Alan, 2026-09-25.

## 1. Alan's decisions

1. **The phone minimums in entry 206 section 2 are approved:** Android 10, 4 GB of memory, peak memory under about 400 MB aimed at 300,
   detection about 10 s on a Galaxy A16 class phone and about 30 s on an A06 class phone with progress and cancel, camera at least 8 MP with
   autofocus, installed app under about 100 MB.
2. **No Galaxy A16 will be bought.** Alan has older test phones and will look out what they are. When he sends their models, they become the
   low end reference devices in place of the A16 and A06 classes; until then use the emulator as entry 206 says. Add a request to for-alan.md
   asking for each phone's model, Android version and whether it still charges and boots, so they can be paired the same way as the Fold 7.
3. **An opt-in hardware and benchmark survey, on every platform and operating system GroupLab runs on:** Windows, macOS, Linux and Android,
   and iOS if it ever exists. Section 3.
4. **List minimums for every platform.** Section 2.

## 2. Minimums for every platform

Write one table, in `docs/PLATFORM-SUPPORT.md`, shown on grouplab.org's download page and in the README: the lowest operating system, CPU
architecture, memory, free disk, screen and, for phones, camera, per platform. Each line names where it comes from: .NET's own support list,
Avalonia's, OpenCV's, or GroupLab's measurements.

What .NET 10 itself supports (github.com/dotnet/core, release-notes/10.0/supported-os.md), as the floor nothing can go below:

- **Windows:** Windows 10 version 1607 and later; x64, Arm64 and x86.
- **macOS:** 14 and later; Arm64 and x64.
- **Linux:** Ubuntu 22.04, Debian 12, Fedora 42, RHEL 8 and later; glibc 2.27 for x64 and Arm64; musl 1.2.3.
- **iOS:** 18 and later.
- **Android: 14 and later.** This conflicts with the approved Android 10. It is what Microsoft tests and supports, not necessarily what
  runs: the spike is built for API 24 and runs. Say plainly in `docs/ANDROID.md` what "supported" means there, test on the oldest phone Alan
  finds, and if Android 10 to 13 work, keep Android 10 as GroupLab's minimum with a note that it rests on GroupLab's own testing rather
  than Microsoft's. Android 14 or later alone would cover only about 55% of Android phones in use, which is why this matters.

Then GroupLab's own figures, measured rather than guessed: the desktop peaks at about 730 MB on the 600 dpi sample, so say what the minimum
and recommended memory are on the desktop (likely 4 GB minimum and 8 GB recommended; measure), the disk space the install and a typical
library of sessions take, and the smallest window the layout supports. Which architectures are actually built and published today, and
which are not (for example Windows Arm64 or Linux Arm64), goes in the same table; do not list a platform as supported that has no build.

## 3. The opt-in hardware and benchmark survey

1. **Consent.** Its own choice, separate from sending targets and from error reports, on the first run screen and in Settings: off until
   the person turns it on, and never preselected, the same rule as entry 203 section 3. The wording says exactly what is sent.
2. **What is sent, and nothing more:** operating system and version, CPU model, architecture and core count, total memory, GPU name if
   relevant, screen size and scale, for phones the device model and rear camera resolution, GroupLab's version, and per analysis the image
   size, the working resolution, the time of each stage and the peak memory. Never a name, account, file name, path, photograph, location, IP
   address stored on the server, or a device serial or advertising identifier. A random installation id may be used to count devices once,
   reset whenever the person asks.
3. **A short benchmark.** A fixed built in test, the sample scan or a smaller synthetic sheet, runs when the person chooses it (a button in
   Settings, and offered once after opting in), timed stage by stage. It gives every platform a comparable number, like the survey's
   Steam counterpart, and it tells the person their own result.
4. **Transport.** The same route as error reports and targets: posted to grouplab.org, checked against a schema, rate limited, stored on
   the server. It does not go to the GitHub issues repository. Queued offline like the others.
5. **Publication.** An aggregate page on grouplab.org, like Steam's hardware survey: shares of operating systems, versions, memory, CPU
   classes and phone models, and benchmark times by class, with the date range and sample size, updated from the stored reports. Never an
   individual record. Small groups are merged into "other" so no one device is identifiable.
6. **Use.** Review the minimums in section 2 against it once there are enough reports, and say in STATE.md when that is.

Design it now in a short document, `docs/SURVEY.md`, and build the desktop part with the next desktop work; the Android part comes with the
real app. The server side is a receiver and a worker like the error reports, so Alan will get one install request for it; keep that to one
sitting with the others if any are pending.

Report in plain words for Alan: the minimums table, and what the survey will ask people.
