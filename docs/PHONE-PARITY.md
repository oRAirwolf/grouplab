# What the phone does, feature by feature

NOTES-FROM-PLANNING.md entry 258. Alan: "The mobile application should be able to do everything that the desktop can except for features
that require a desktop or are completely unsuited for a mobile device."

Every feature on the Features page (`website/features.json`) has one row here: **on the phone**, **coming** with its stage, or **left out**
with the reason. The site build fails when a feature has no row, or when a row says "on the phone" and the feature does not list Android,
or the other way round. So a new desktop feature is not finished until it is on the phone or listed here as coming or left out.

**The screens' designs are Alan's choices** (entry 259, "A" for all six, and the scan proposal approved), from the planning session's phone
concepts; the build spec is entry 259's own words. Anything a chosen concept does not settle goes to `docs/notes/for-alan.md` under
"DESIGN NEEDED:", and is not decided here.

**How it is built** (entry 258): what the desktop computes lives in shared code, so the phone and the desktop cannot give different numbers.
The engines were already shared before this began: the analysis's figures and their explanations (`AnalysisPanel`, `FigureExplanations`),
the aimed bulls (`AimedBulls`), Shots Needed to Zero (`ShotsToZero`), comparing loads (`LoadComparison`), ballistics and hit probability
(`HitProbability` and the solver), pooling a set (`SetPool`) and a scan's printed scale (`ScaleReference`). Making saved sessions into groups to compare moved from the desktop into `CompareSessions`, and the hit chance's precision from a marked group into `HitFromGroup` (entry 258). What is left for the phone is
its screens, in entry 259's order, each shipped in its own nightly and tried at the next device sitting.

| Feature | Key | On the phone | Stage or reason | On iPhone and iPad |
|---|---|---|---|---|
| A GroupLab sheet reads itself | `reads-itself` | on the phone | entry 313 section 1: Cancel works at once from any step and keeps the picture to read again or to choose its sheet; a reading stops itself after a minute, the time the screen was locked not counted | on iOS |
| Every hole found, and every one yours to change | `every-hole` | on the phone | entry 291 section 2: changed on a page of their own, Fix holes, under a fixed crosshair with zoom and undo; the result's picture takes no touch | on iOS |
| The bulls you aimed at | `aimed-bulls` | on the phone | entry 259 screen 2, "tap the bulls on the sheet" | on iOS |
| A sheet shot off by the same amount | `whole-sheet` | on the phone | | on iOS |
| Works in your scope's unit | `scope-unit` | on the phone | entry 294 section 1: the first run asks mil or MOA with the length beside it, Units at the top of Settings, the rifle form's scope unit and click chips, and every aiming figure in the scope's unit | on iOS |
| Tap a number to switch units | `unit-tap` | on the phone | entry 273: tiles and figure rows, press and hold for every unit, the note at the bottom | on iOS |
| Check your printer once, for real inches from photos | `printer-scale` | on the phone | entry 271: the ruler card and a scan's offer on the result, the sentence on Capture; the printer is chosen by the last one kept | on a device |
| Any target you already shoot | `other-targets` | on the phone | entry 279 section 2, Marking A: the scale, the aim points and the holes under a fixed crosshair, and a template kept | on iOS |
| Open by dropping or pasting | `open-anyhow` | on the phone | entry 258: a picture shared into GroupLab or opened with it from another app, and Paste a picture on Capture | on iOS |
| Pictures from any photo app | `any-photo-app` | on the phone | entry 292: Android's photo picker, From another app, and sharing one or several from any app; the phone's own, so the computer's row is Open by dropping or pasting. On iPhone and iPad: the Photos picker, Files, and a share extension, since iOS lets no app into Google Photos' library; the simulator proves the pickers open and a shared or opened picture is read, and choosing, iCloud and the share sheet wait for the TestFlight sitting | on a device |
| The figures you read off a target | `six-figures` | on the phone | | on iOS |
| Shots Needed to Zero | `shots-to-zero` | on the phone | entry 259 screen 3, its own page | on iOS |
| CEP circles, any percent | `cep` | on the phone | entry 259 screen 1, full figures | on iOS |
| Every figure explained | `why` | on the phone | entry 259 screen 1, the explanation sheet | on iOS |
| Compare loads honestly | `compare` | on the phone | entry 259 screen 4, one figure at a time; entry 295: each name on its own line, sessions named by load, date and time, and extreme spread pointing to mean radius; entry 309 section 2: each load's group drawn in its own color, which the iOS self-test checks with two loads | on iOS |
| Sessions over time | `sessions` | on the phone | | on iOS |
| Ballistics and hit chances | `ballistics` | on the phone | entry 259 screen 5, its own tab; printing the dope card is still to come | on iOS |
| Fudd buster mode | `fudd-buster` | on the phone | entry 279 section 3 and entry 281 section 2, page A: the same three sections as the desktop's window | on iOS |
| Shots in and out as CSV | `csv` | on the phone | out through Android's share sheet since entry 258; in under Sessions since entry 278, CSV B: the group as it will be read and GroupLab's guesses, each tapped to change | on iOS |
| The Targets screen | `targets` | on the phone | the phone's Targets screen (entry 243) | on iOS |
| Made for your optic | `optic` | on the phone | on the phone's Targets screen | on iOS |
| Zeroing grids read through a scope | `zero-grids` | on the phone | in the phone's Targets library, previewed with their words | on iOS |
| Large sheets | `large-sheets` | on the phone | entry 258: the photograph detail advice on the Targets screen, and a set shared as one large page with cut lines for a plotter | on iOS |
| Real inches from a scan | `true-size` | on the phone | entry 259 screen 7, the scan pill; a phone photograph cannot measure its print size | on iOS |
| Photographs at an angle | `angle` | on the phone | | on iOS |
| Sessions between phone and computer | `share-session` | on the phone | | on iOS |
| The phone follows your region | `phone-region` | on the phone | | on iOS |
| Send a target to the project | `send-targets` | on the phone | | on iOS |
| Error reports | `error-reports` | on the phone | | on iOS |
| The hardware survey | `survey` | on the phone | | on iOS |
| Words explained where they appear | `explain-words` | on the phone | a figure's name opens its explanation by a tap (entry 259 screen 1), and a secondary line naming a glossary word explains it by a tap (entry 258) | on iOS |
| Updates that list what you skipped | `updates` | left out | Google Play updates the phone's application; the desktop's update bar has nothing to do there. The sideloaded GroupLab Dev has its own updater, `android-updates` | left out |
| GroupLab Dev updates itself | `android-updates` | on the phone | entry 288: the Android updater, in GroupLab Dev's APK only, never in the copy for Google Play | left out |
| Builds for the Mac | `mac` | left out | a platform, not something a phone can do | left out |
| Pool the sheets of a set | `pool-set` | on the phone | entry 259 screen 6, the set as a checklist: the saved sheets of the set's design shot the same day | on iOS |
| The E bull | `e-bull` | on the phone | | on iOS |
| The C bull | `c-bull` | on the phone | | on iOS |
| Large format on a home printer | `large-on-letter` | on the phone | | on a device |
| GroupLab Dev for testers | `dev-build` | on the phone | | left out |
| The sheet beside the numbers on a big screen | `big-screen` | on the phone | | on a device |
| Print a sheet from the phone | `phone-targets` | on the phone | | on a device |
| Guided or Manual on the camera | `capture-modes` | on the phone | entry 291 section 3: the guidance follows what the picture will read, and "Move back" only when the printing leaves the frame | on a device |
| Every picture checked | `picture-check` | on the phone | | on iOS |
| The 2 MOA sheets | `two-moa` | on the phone | entry 289: in the phone's Targets library with the other built-in sheets | on iOS |
| Every shot, and which count | `shots-table` | on the phone | entry 280 section 2, Shots A: the Shots page from a result; on the computer, Shots and clicks under the shot table | on iOS |
| Zero from this group | `zero-from` | on the phone | entry 280 section 2, board ZeroFrom: its own page, and the offset handed to Ballistics; a window on the computer | on iOS |
| Several aim points on one target | `aim-points` | on the phone | entry 280 section 2, board MultiAim: a chip per aim point in its own color and "+ Aim point" | on iOS |
| A picture to share | `share-picture` | on the phone | entry 280 section 2, Share A: Save to gallery (Pictures, GroupLab) and Share; Save picture on the computer | on a device |
| The one-page report | `one-page-report` | on the phone | entry 280 section 2, board Report: shared or printed through Android's own dialogs | on a device |

**iPhone and iPad** (entry 290 section 6). The last column says where each feature stands on iOS: **on iOS** where it runs there and the
simulator self-test has seen it, **not yet** while the iOS build is being made, **on a device** where it is built but only an iPhone or
iPad can prove it (the camera, the torch, the level, printing to a real printer, iCloud or Google Photos, sharing from another app), and
**left out** where the phone leaves it out too. The screens are one shared project the Android and iOS heads both link
(`mobile/GroupLab.Mobile`), so a feature on Android is most of the way to iOS; what is left is the head's own part and proving it on the
iOS Simulator. `docs/IOS-PLAN.md` has the plan, and `ios/GroupLab.iOS/SelfTest.cs` and `ios/selftest.py` say what the simulator's self-test
actually checks. The site build holds this column to those four words.

## Phones sold outside America (entry 292 section 4)

Alan: "This may be especially important for europeans that have access to a lot of phones that are not available in america." Europe and
South America buy far more of these than the US does. GroupLab writes nothing for any one brand: their galleries answer the standard
Android requests that Choose a photograph, From another app and sharing use (`docs/ANDROID.md` section 18), and their clouds hand a
photograph over through the gallery's own stream, with the same progress, offline and whole-photograph rules. Something brand-specific is
written only when a test on that brand shows it is needed. Where a phone has no Google Play services, Choose a photograph offers the apps
instead of the photo picker, and nothing else GroupLab needs depends on Google Play services.

Which brands a real person has tried, and which are reached only by the standard requests so far:

| Brand | Its gallery and cloud | Tried by a person | On iPhone and iPad |
|---|---|---|---|
| Samsung (One UI) | Samsung Gallery, Samsung Cloud and OneDrive | yes, on the Galaxy Z Fold 7 and Tab S8 Ultra; entry 292's pickers at the next sitting | not yet |
| Google Pixel | Google Photos | not yet: the standard requests only | not yet |
| Xiaomi, Redmi and POCO (HyperOS, MIUI) | Gallery, Xiaomi Cloud | not yet: the standard requests only | not yet |
| OPPO, OnePlus and realme (ColorOS, OxygenOS, realme UI) | Photos, HeyTap Cloud | not yet: the standard requests only | not yet |
| vivo (OriginOS, Funtouch OS) | Albums, vivo Cloud | not yet: the standard requests only | not yet |
| Honor (MagicOS) | Gallery, Honor Cloud | not yet: the standard requests only | not yet |
| Nubia and RedMagic | Gallery | not yet: the standard requests only | not yet |
| Motorola | Google Photos, Gallery | not yet: the standard requests only | not yet |
| Huawei (EMUI, HarmonyOS) | Gallery, Huawei Cloud; no Google Play services | not yet: the standard requests only; the Android emulator without Google Play is the stand-in | not yet |

The user guide says, one line a brand, how to let GroupLab Dev run in the background, because these phones' battery managers stop
background work; the update check also runs at every launch, so it never depends on background work alone. The device survey
(`docs/SURVEY.md`) says which of these brands testers actually use, and a tester who tries one is added to this table.
