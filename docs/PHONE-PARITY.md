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

| Feature | Key | On the phone | Stage or reason |
|---|---|---|---|
| A GroupLab sheet reads itself | `reads-itself` | on the phone | |
| Every hole found, and every one yours to change | `every-hole` | on the phone | |
| The bulls you aimed at | `aimed-bulls` | on the phone | entry 259 screen 2, "tap the bulls on the sheet" |
| A sheet shot off by the same amount | `whole-sheet` | on the phone | |
| Real inches from a photograph | `printer-scale` | on the phone | entry 271: the ruler card and a scan's offer on the result, the sentence on Capture; the printer is chosen by the last one kept |
| Any target you already shoot | `other-targets` | coming | after entry 259: bulls placed by touch, a scale drawn at each, templates |
| Open by dropping or pasting | `open-anyhow` | coming | after entry 259: a picture shared into GroupLab from another app, and pasting one |
| The figures you read off a target | `six-figures` | on the phone | |
| Shots Needed to Zero | `shots-to-zero` | on the phone | entry 259 screen 3, its own page |
| CEP circles, any percent | `cep` | on the phone | entry 259 screen 1, full figures |
| Every figure explained | `why` | on the phone | entry 259 screen 1, the explanation sheet |
| Compare loads honestly | `compare` | on the phone | entry 259 screen 4, one figure at a time; the desktop's chart, not yet in each load's colour |
| Sessions over time | `sessions` | on the phone | |
| Ballistics and hit chances | `ballistics` | on the phone | entry 259 screen 5, its own tab; printing the dope card is still to come |
| Shots in and out as CSV | `csv` | coming | after entry 259: through Android's share sheet and file picker |
| The Targets screen | `targets` | on the phone | the phone's Targets screen (entry 243) |
| Made for your optic | `optic` | on the phone | on the phone's Targets screen |
| Zeroing grids read through a scope | `zero-grids` | on the phone | in the phone's Targets library, previewed with their words |
| Large sheets | `large-sheets` | coming | after entry 259: the photograph detail advice, and tiled printing with cut lines through Android's print dialog |
| Real inches from a scan | `true-size` | on the phone | entry 259 screen 7, the scan pill; a phone photograph cannot measure its print size |
| Photographs at an angle | `angle` | on the phone | |
| Sessions between phone and computer | `share-session` | on the phone | |
| The phone follows your region | `phone-region` | on the phone | |
| Send a target to the project | `send-targets` | on the phone | |
| Error reports | `error-reports` | on the phone | |
| The hardware survey | `survey` | on the phone | |
| Words explained where they appear | `explain-words` | coming | a figure's name opens its explanation by a tap since entry 259 screen 1; glossary words inside sentences do not yet |
| Updates that list what you skipped | `updates` | left out | Google Play updates the phone's application; the desktop's own updater has nothing to do there |
| Builds for the Mac | `mac` | left out | a platform, not something a phone can do |
| Pool the sheets of a set | `pool-set` | on the phone | entry 259 screen 6, the set as a checklist: the saved sheets of the set's design shot the same day |
| The E bull | `e-bull` | on the phone | |
| The C bull | `c-bull` | on the phone | |
| Large format on a home printer | `large-on-letter` | on the phone | |
| GroupLab Dev for testers | `dev-build` | on the phone | |
| The sheet beside the numbers on a big screen | `big-screen` | on the phone | |
| Print a sheet from the phone | `phone-targets` | on the phone | |
| Guided or Manual on the camera | `capture-modes` | on the phone | |
| Every picture checked | `picture-check` | on the phone | |
