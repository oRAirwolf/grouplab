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
| A GroupLab sheet reads itself | `reads-itself` | on the phone | | not yet |
| Every hole found, and every one yours to change | `every-hole` | on the phone | | not yet |
| The bulls you aimed at | `aimed-bulls` | on the phone | entry 259 screen 2, "tap the bulls on the sheet" | not yet |
| A sheet shot off by the same amount | `whole-sheet` | on the phone | | not yet |
| Tap a number to switch units | `unit-tap` | on the phone | entry 273: tiles and figure rows, press and hold for every unit, the note at the bottom | not yet |
| Check your printer once, for real inches from photos | `printer-scale` | on the phone | entry 271: the ruler card and a scan's offer on the result, the sentence on Capture; the printer is chosen by the last one kept | not yet |
| Any target you already shoot | `other-targets` | on the phone | entry 279 section 2, Marking A: the scale, the aim points and the holes under a fixed crosshair, and a template kept | not yet |
| Open by dropping or pasting | `open-anyhow` | on the phone | entry 258: a picture shared into GroupLab or opened with it from another app, and Paste a picture on Capture | not yet |
| The figures you read off a target | `six-figures` | on the phone | | not yet |
| Shots Needed to Zero | `shots-to-zero` | on the phone | entry 259 screen 3, its own page | not yet |
| CEP circles, any percent | `cep` | on the phone | entry 259 screen 1, full figures | not yet |
| Every figure explained | `why` | on the phone | entry 259 screen 1, the explanation sheet | not yet |
| Compare loads honestly | `compare` | on the phone | entry 259 screen 4, one figure at a time; the desktop's chart, not yet in each load's colour | not yet |
| Sessions over time | `sessions` | on the phone | | not yet |
| Ballistics and hit chances | `ballistics` | on the phone | entry 259 screen 5, its own tab; printing the dope card is still to come | not yet |
| Fudd buster mode | `fudd-buster` | on the phone | entry 279 section 3 and entry 281 section 2, page A: the same three sections as the desktop's window | not yet |
| Shots in and out as CSV | `csv` | on the phone | out through Android's share sheet since entry 258; in under Sessions since entry 278, CSV B: the group as it will be read and GroupLab's guesses, each tapped to change | not yet |
| The Targets screen | `targets` | on the phone | the phone's Targets screen (entry 243) | not yet |
| Made for your optic | `optic` | on the phone | on the phone's Targets screen | not yet |
| Zeroing grids read through a scope | `zero-grids` | on the phone | in the phone's Targets library, previewed with their words | not yet |
| Large sheets | `large-sheets` | on the phone | entry 258: the photograph detail advice on the Targets screen, and a set shared as one large page with cut lines for a plotter | not yet |
| Real inches from a scan | `true-size` | on the phone | entry 259 screen 7, the scan pill; a phone photograph cannot measure its print size | not yet |
| Photographs at an angle | `angle` | on the phone | | not yet |
| Sessions between phone and computer | `share-session` | on the phone | | not yet |
| The phone follows your region | `phone-region` | on the phone | | not yet |
| Send a target to the project | `send-targets` | on the phone | | not yet |
| Error reports | `error-reports` | on the phone | | not yet |
| The hardware survey | `survey` | on the phone | | not yet |
| Words explained where they appear | `explain-words` | on the phone | a figure's name opens its explanation by a tap (entry 259 screen 1), and a secondary line naming a glossary word explains it by a tap (entry 258) | not yet |
| Updates that list what you skipped | `updates` | left out | Google Play updates the phone's application; the desktop's update bar has nothing to do there. The sideloaded GroupLab Dev has its own updater, `android-updates` | left out |
| GroupLab Dev updates itself | `android-updates` | on the phone | entry 288: the Android updater, in GroupLab Dev's APK only, never in the copy for Google Play | not yet |
| Builds for the Mac | `mac` | left out | a platform, not something a phone can do | left out |
| Pool the sheets of a set | `pool-set` | on the phone | entry 259 screen 6, the set as a checklist: the saved sheets of the set's design shot the same day | not yet |
| The E bull | `e-bull` | on the phone | | not yet |
| The C bull | `c-bull` | on the phone | | not yet |
| Large format on a home printer | `large-on-letter` | on the phone | | not yet |
| GroupLab Dev for testers | `dev-build` | on the phone | | not yet |
| The sheet beside the numbers on a big screen | `big-screen` | on the phone | | not yet |
| Print a sheet from the phone | `phone-targets` | on the phone | | not yet |
| Guided or Manual on the camera | `capture-modes` | on the phone | | not yet |
| Every picture checked | `picture-check` | on the phone | | not yet |

**iPhone and iPad** (entry 290 section 6). The last column says where each feature stands on iOS: **on iOS** where it runs there and
has been seen to, **not yet** while the iOS build is being made, **on a device** where it is built but only an iPhone or iPad can prove it
(the camera, the torch, the level, printing to a real printer), and **left out** where the phone leaves it out too. The screens are one
shared project the Android and iOS heads both link (`mobile/GroupLab.Mobile`), so a feature on Android is most of the way to iOS; what is
left is the head's own part and proving it on the iOS Simulator. `docs/IOS-PLAN.md` has the plan, and the site build holds this column
to those four words.
