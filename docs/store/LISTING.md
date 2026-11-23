# The Microsoft Store listing, ready to paste

NOTES-FROM-PLANNING.md entry 224 section 3.5. Everything Partner Center asks for on the first submission's Store listing page, in the order
it asks. Each block is pasted as it is. The screenshots are the ones the weekly screenshot job makes from the newest build, so they are
always of the version people will download.

## Product name

GroupLab

## Short description

Measure your groups from a scan or a photo of a printed target: shot positions, group size, mean radius and more, in inches, MOA or mils.

## Description

GroupLab measures a group of shots on a printed target. Print one of its sheets, shoot it, then scan it or photograph it: GroupLab reads
the sheet's printed markers to know its exact size, finds the holes, and reports where each shot landed and how the group measures up.

It gives the figures a shooter needs to compare loads, rifles and days at the range: extreme spread, mean radius, the group's center
against the point of aim, and its circular error probable, each with how sure it is from the number of shots. Sessions are kept, compared
and plotted over time.

GroupLab is free and open source under the GPL-3.0 license. It sends nothing about you unless you choose to, and it never reads where a
photo was taken.

GroupLab 1.0 is ready to use and still in active development. New versions arrive often, and suggestions and error reports are always welcome: Settings, Help and problems, Report a problem
sends one straight from GroupLab.

## What's new in this version

Pasted from the release's own notes on github.com/oRAirwolf/grouplab/releases, the part under "What you will notice".

## Product features

Each is one line, as Partner Center takes them.

- Finds the holes on a scan or a photo of a printed GroupLab target
- Reads the sheet's printed markers to know its exact scale, even from an angled photo
- Extreme spread, mean radius, group center and circular error probable
- Every figure in inches, centimeters, MOA or mils, at the distance you shot
- Correct any hole by hand: move, add or remove one
- A composite plot of every shot on one bull, with the group's center and CEP rings
- Sessions kept, compared side by side and plotted over time
- Prints its own targets, sized exactly, from a library of sheets
- Exports a report as a PDF and the shots as a spreadsheet
- Free and open source; nothing is sent unless you choose

## Search terms

The Store takes seven; entry 369: GroupLab first, then the six most useful. store-follow.yml sends these with every submission.

GroupLab, group size, shooting target, MOA, reloading, ballistics, precision rifle

## Category

Sports. Subcategory, where asked: none.

## Privacy policy

https://grouplab.org/research/what-grouplab-sends/

## Support contact

https://grouplab.org/support/

## Website

https://grouplab.org/

## Screenshots, 1920 by 1080

From `docs/figures/screens/current/`, the dark theme first because the Store shows the first one largest:

1. `analysis-dark-1920x1080.png`: the analysis of a group
2. `analysis-light-1920x1080.png`: the same in the light theme
3. `marking-dark-1920x1080.png`: the holes found on the sheet
4. `compare-light-1920x1080.png`: two loads compared
5. `ballistics-dark-1920x1080.png`: the ballistics screen

## Age rating questionnaire

GroupLab is a measuring tool for target shooting. It shows no violence, no weapons in use, no people, no gambling, no chat and no
purchases. It connects to the internet only to check for updates in the downloaded version and to send what the person chose to send. In
the questionnaire: no to every content question; it shares no location; it has no user interaction or user generated content shown to
others. The expected rating is 3+ (IARC), everyone.

## System requirements

Windows 10 version 1809 or later, 64-bit (x64). 4 GB of memory, 8 GB recommended. The Store package needs 1809, higher than the downloaded
version's Windows 10 version 1607, because 1809 is the Store's floor for MSIX packages.

## Apple App Store Connect, App Privacy, as declared (entry 379)

Alan filled this in for the first time and wrote "forms updated" on 2026-10-06. Recorded here so an audit can hold the application to it.

- **Privacy Policy URL:** https://grouplab.org/research/what-grouplab-sends/
- **Every data type below:** not linked to the user, not used for tracking.
- **Identifiers, Device ID** (the hardware survey's random installation number): Analytics.
- **Usage Data, Product Interaction:** App Functionality, Analytics.
- **Diagnostics, Crash Data:** App Functionality. **Performance Data:** App Functionality, Analytics. **Other Diagnostic Data:** App
  Functionality.
- **User Content, Photos or Videos:** Analytics.

What sends each, in the application: the survey (the installation number), error reports with their log (crash data, diagnostic data),
the survey's benchmark (performance data), and Send everything I open or a target sent (photos). All are the person's choice, and on the
phones they were switched on by entry 379.

## Apple App Store, version 1.0, as set on 2026-11-15 (entry 487)

Set by the planning session in App Store Connect (GroupLab, Apple ID 6817635137) with Alan signed in. The console is the authority.

- **Agreements:** the updated Apple Developer Program License Agreement (dated 2026-10-29) accepted at developer.apple.com; the Free
  Apps Agreement Active from 2026-11-15 (entry 486).
- **EU Digital Services Act:** non-trader; Active in 27 countries or regions from 2026-11-15.
- **App Information:** subtitle "Measure your shot groups"; category Sports, secondary Utilities; content rights "No, it does not
  contain, show, or access third-party content" (GroupLab ships measurements of the nine store-bought targets, not pictures of them).
- **Age rating:** no or none to every question except Guns or Other Weapons: Frequent, since rifles, calibres and loads are named
  throughout; calculated 13+, set to 18+ at Alan's word to match Google Play's audience of 18 and over. Apple shows 18+ in 173
  countries or regions, 19+ in Korea, and 17+ on systems before version 26.
- **Version 1.0:** promotional text; a 1,615 character description (the Play text for iPhone and iPad, plus "Settings, General, Walk
  me through a demo"); keywords "group size,shooting,target,MOA,mils,reloading,ballistics,precision rifle,load development,CEP";
  support URL https://grouplab.org/support/; marketing URL https://grouplab.org/; copyright "2026 Alan Hayes"; sign-in not required.
- **Review notes:** no accounts; use the demo, since a reviewer has no shot sheet; any paper target can be marked by hand; no
  firearms shown, nothing sold, nothing sent unless chosen; the source is on GitHub under GPL-3.0 with the App Store permission. The
  review contact is Alan, at support@grouplab.org; the phone number stays in App Store Connect only.
- **Release:** manual for 1.0, which Alan released on 2026-11-17. From entry 513 (2026-11-23) the release type after approval is automatic: an approved version goes live with no further word (see below).
- **Screenshots:** from the simulators (entry 487 section 4, `docs/store/apple/`).
- **Next version:** the description gains "Requires iOS 26 or later. It also installs on iOS 18 to 25, without support.", set in App Store Connect with the version (entry 505 section 1.7); it went to review as 1.0.242 on 2026-11-23 (see below).

### Submissions from entry 513, 2026-11-23

Alan, entry 513: "All new builds should be submitted to all of the app stores including the apple app store, play store, and microsoft store." This replaces the rule that App Store submissions and releases wait for his word (entries 491 and 501).

- **Automatic:** after each nightly passes its checks, `scripts/app-store.py`, run by testflight.yml, submits the newest build for iPhone, iPad and Mac; the release type after approval is automatic. One review at a time per platform; a newer build waits. `STORE_SUBMISSIONS_PAUSED` stops all store submissions.
- **Submitted to App Review at 11:59 UTC on 2026-11-23:** iPhone and iPad version 1.0.242 (build 242), the first App Store version since 1.0 (build 226), carrying the iOS 18 to 25 support; and the Mac App Store's first version, 1.0 (build 240). Neither is approved yet. The App Store copy live today is still 1.0, build 226, which needs iOS 26.

### Submitted to App Review, 2026-11-15 (entry 491)

At Alan's word ("Submit now"), by the planning session at 20:36 Denver time on 2026-11-15:

- **Build:** 226, version 1.0, attached to iOS version 1.0; it carries the privacy manifest from entry 489.
- **Content rights** saved again on App Information ("No third-party content"): the first answer was set but its page never saved.
- **Pricing and Availability:** free ($0.00, base United States) in all 175 countries or regions. **Not offered on Apple silicon Macs
  or Apple Vision Pro**: both were on by default and neither has been tried; GroupLab has its own signed macOS build. Either can be
  turned on later.
- **Status:** 1.0 was Waiting for Review at submission (Apple said up to 48 hours); Alan released it on 2026-11-17 and App Store Connect says Ready for Distribution, at https://apps.apple.com/app/id6817635137. The testflight workflow reads the version's state through the API
  after every nightly and every half hour, into `docs/notes/external-status.md`; a rejection goes to `docs/notes/for-alan.md` the same
  day with Apple's words, which are copied from App Store Connect because the API does not give them.
- **TestFlight** goes on as before: each nightly still goes to both groups, and nothing in a nightly touches the released version. From nightly 232 the iPhone builds are numbered 1.0.<build>, as Android's are (entry 498 section 0).
