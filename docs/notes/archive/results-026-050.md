# Phase 1 results, entries 026 to 050

Archived from `docs/PHASE1-RESULTS.md` under entry 160, exactly as written.

## Entry 28. Questions 12 to 14 answered, and the real upload schema

`docs/NOTES-FROM-PLANNING.md` entry 28 answers questions 12, 13 section 1 and section 3, and 14.

**Reproduce:** `dotnet test tests/GroupLab.Core.Tests --filter "ShotGroupsFixture|Publication|ImageMetadataTests"`

**Section 1: intake reads the page's real `meta.json`.** Every name the tool had guessed was wrong. It now reads schema 1 exactly as the first real submission has it.
- **Snake case throughout:** `submission_id`, `submitted_utc`, `exclude_from_public_dataset`, `consent`, `answers`, and `files` as objects with `index`, `stored_name`, `original_name`, `bytes`, `sniffed_type` and `sha256`.
- **Refused, with nothing written:**
  - any other `schema_version`;
  - an opt-out, or an opt-out field that is missing, which is unknown and not false;
  - a consent not agreed, or missing its version, time or text;
  - a file whose byte count or hash differs from the upload's, or whose `stored_name` is not a safe file name.
- **No sentinel file.** The page writes none, so the tool no longer looks for `DO-NOT-PUBLISH`.
- **Recorded in `provenance.json`:** the consent text verbatim, since a later version will say something else. Also `original_name`, which is never used as a path.
- **Empty answers are accepted.** They are the normal case.
- **The tests:** `IntakeTests` builds its submissions from entry 28's file, field for field, and refuses eleven variations of it.

**Section 2: question 12, five shots.** The panel already does it. Section 15.4 item 13 now carries the coverage table for extreme spread's two interval forms, so the number no longer lives only in a test.

**Section 3: question 14, and the difference was the fixtures' precision.**
- **Planning's probe:** `compareGroups` hands the test exactly `shots.xPOA`, and neither the Fligner-Killeen formula nor `coin` accounts for the gap.
- **What GroupLab found:** from the same written vector, its statistic was 10.073187875409229 against R's 10.075167218103388. Its medians and counts matched the probe exactly, and its normal quantiles matched an independent incomplete-gamma computation to 1e-15.
- **The cause.** `sg_dump.R` writes the JSON at 15 digits, which does not round-trip R's doubles. The aimed coordinates carry the noise of `point.x - aim.x` in exactly the bits lost, and those bits make or break ties among the absolute deviations. On raw coordinates, which are short decimals, GroupLab already matched R to 2e-13.
- **The fix.** The harness recovers each aim to six decimals from `shots.x` and `shots.xPOA`, and redoes the subtraction. All four statistics then match: `DFinch` x to 1.2e-13 and y to 3e-14, `DFcm` x to 3.4e-14 and y to 5.5e-13. Recorded as section 15.4 item 15.
- **The probe keys:** every one of the 4,414 is now compared by `sg_dump.R`'s own definitions. On `DFlandy01` that meant reproducing the misaligned pasting of section 5, below.

**The harness now.**
- **Compared:** 45,476 keys plus 600 checks that GroupLab's CorrNormal CEP is a root of its distribution.
- **Otherwise:** nothing outside tolerance, disputed or pending.

| Dataset | Compared, with root checks | Excluded |
|---|---|---|
| `DF300BLK` | 324 | 282 |
| `DFscar17` | 314 | 212 |
| `DFcciHV` | 1,181 | 694 |
| `DF300BLKhl` | 1,749 | 973 |
| `DFcm` | 5,775 | 4,778 |
| `DFinch` | 5,775 | 4,778 |
| `DFsavage` | 4,424 | 2,614 |
| `DFlandy04` | 3,351 | 2,186 |
| `DFlandy01` | 23,183 | 11,063 |

- **A slip the new keys exposed.** The rule excluding robust estimates matched any key containing "rob", and so every key of `flignerProbe`. It now leaves the probe alone.

**Section 4: question 13 section 1, a separate GPL-3.0 data repository.** The README has a "Test data" section.
- **What it says:** donated photographs live in `grouplab-testdata`, under GPL-3.0 as the consent text says. The only way in is `grouplab intake`.
- **What reads it:** `PublicationTests` reads a checkout beside this repository, or the one `GROUPLAB_TESTDATA` names. Without either it does nothing and says so in its output.
- **Not done:** the repository does not exist yet, so no URL or commit is pinned. Creating it is Alan's.

**Section 5: `compareGroups` pastes coordinates beside the wrong labels.** Recorded as section 15.4 item 14, with the binding order the probe confirmed. GroupLab pairs every coordinate with its own label.

**Also: a stated digital zoom of 0 is 1.** Every Pixel photograph in `scans/mounted/` states a DigitalZoomRatio of 0, which the EXIF standard defines as digital zoom not used. `ImageMetadata.EffectiveDigitalZoom` reads it as 1 for the lens key and the joint fit, and a missing tag stays unknown.

**Tests:** Core 708, App 4, all passing.

---

## Entry 33. The first end-to-end run

`docs/NOTES-FROM-PLANNING.md` entry 33 section 1: every stage was green and nothing had joined them.

**`grouplab analyze <image> --target <definition>` is the whole path.** `src/GroupLab.Core/Analysis/SheetAnalysis.cs` composes the same code the marking screen uses, `AutomaticMarking` and `GroupAnalysis`, so the command line and the screen cannot disagree. The stages are:
1. decode;
2. register from the printed markers;
3. detect holes inside the registered sheet;
4. assign each hole to its bull;
5. pool the offsets into one group, and compute its statistics.

**The trace.** Every stage files a `StageRecord`, and the command prints DETECTION-PIPELINE section 6.3's console form, so the console exists before any analysis screen does. Detection records each rejected candidate at its page position, and assignment records every ambiguous shot. `--json` writes the result as a marking file the marking screen opens.

**`--target` is required for now.** The sheet's identifier is printed and encoded in its codes, but nothing reads either. Guessing among built-in definitions that share marker ids is not safe.

**The gate against synthetic truth,** `EndToEndTests`.
- **The image:** GL-CF25-LTR rendered at 300 DPI, turned 0.7 degrees, scaled by 1.001 and shifted inside a larger frame, with one hole per bull at an offset the test chose.
- **The run:** the image is written to a PNG, and the command's own analysis runs on the file.
- **The result:**
  - **Recovered:** all 28 placed shots, each within the brief's 0.15 in and assigned to the bull it was placed beside, with no strays.
  - **Centre error:** median 0.0019 in, worst 0.0047 in.
  - **Pooled group:** 25 scoring shots, mean radius 0.1951 in against the placed shots' 0.1935 in.
  - **Time:** the whole analysis took 1.7 s.

**It found two integration faults that no stage test could.**
1. **A printed sheet whose definition fails today's validator could not be analysed at all.**
   - **The cause:** the Phase 0 sheets fail test 26f, which entry 13 made an error. The renderer refused them, and the hole detector indexed an empty page list and crashed.
   - **The fix:** the detector now draws the expected artwork of a sheet that already exists whatever the validator says about printing new ones. It records that decision in the trace, and fails with a reason instead of an index error. The automatic path reports it as a failed stage.
2. **Sighter shots were pooled into the group.** GL-CF25-LTR's three sighters went into its 25-shot group, because `GroupAnalysis` never read a bull's `Scoring` flag.
   - **The fix:** a bull now carries it, the marking file records it, and shots on a sighter are reported and left out, counted in `SighterShots`.
   - **Where it shows:** the marking screen shares the fix.

**On the committed Phase 0 images,** which are unshot sheets:

| Image | Registration | Holes | Stages that dominate | Total |
|---|---|---|---|---|
| `gl-cf25-ltr-1-600-dpi.png` | 34 of 34 markers, 136 of 136 corners, printed at 100.04 percent | 0, none rejected | holes 4.3 s, bull location 2.5 s, decode 0.7 s | 7.7 s |
| `main_flat1.jpg` | 34 of 34 markers, homography with radial distortion | 0, 54 candidates rejected | holes 6.6 s, bull location 1.2 s | 8.0 s |

**Where it is slow.**
- **Hole detection,** 4 to 7 s at these resolutions, is most of every run.
- **Bull location** is Phase 0's measurement locator, 1 to 2.5 s. Analysis uses its recovered centres, and it could be skipped when only the group is wanted.
- **Decoding** reads the image twice, once as grey and once as value.

Nothing has been tuned for speed, and nothing here needs to be before the weekend.

**What it is for.** This weekend's mounted sheets go straight into `grouplab analyze` on Monday.

**Entry 33 section 3: the README guard and CI.**
- **`ReadmeTests`** checks README.md's facts. Every relative link and image must resolve. The number of built-in sheets between `<!--count:sheets-->` markers must match `targets/`. The framework between `<!--framework-->` markers must match `Directory.Build.props`. No em dash may appear. Each failure names the line and says what to change.
- **`.github/workflows/ci.yml`** builds and tests on Windows, Linux and macOS on every push and pull request, and writes each platform's test counts to the run summary. Windows is required. Linux and macOS may fail until they pass.

**Entry 33 section 4 and entry 32 section 1: OpenCV's native runtime per platform.**
- **The fix:** the CLI referenced `OpenCvSharp4.runtime.win` unconditionally, so nothing restored off Windows. Each runtime is now conditioned on its platform, at the wrapper's 4.13.0.20260627:
  - `OpenCvSharp4.runtime.win` on Windows;
  - `OpenCvSharp4.official.runtime.linux-x64` on Linux;
  - `OpenCvSharp4.runtime.osx.x64` and `OpenCvSharp4.runtime.osx.arm64` on macOS.
- **The ids were checked on nuget.org.** The older `osx.10.15-x64` and `osx_arm64` packages stop at 4.6 and 4.8.
- **The first CI run passed on all three,** for the commit these changes landed in:

  | Platform | Build | Core | App |
  |---|---|---|---|
  | `windows-latest` | passed | 714 of 714, 7 m 33 s | 4 of 4 |
  | `ubuntu-latest` | passed | 714 of 714, 7 m 27 s | 4 of 4 |
  | `macos-latest` | passed | 714 of 714, 6 m 50 s | 4 of 4 |

- **`GroupLab.App`'s `WinExe`** builds, and its headless tests pass, on Linux and macOS, so it is harmless there as documented.
- **Linux and macOS are now required** in the workflow, as entry 32 section 4 asks once they pass.
- **Not yet claimed: that the Phase 0 gate record reproduces off Windows.** The suite includes conformance test 43 and every stage test, at their tolerances, and all of them pass on all three. That is agreement within tolerance, not the byte-identical comparison, or explained difference, that entry 32 section 3 requires before macOS or Linux is offered to anybody.

**Tests:** Core 714 passing, App 4 passing, none skipped.

---

## Entry 34. grouplab-testdata populated

`docs/NOTES-FROM-PLANNING.md` entry 34. [grouplab-testdata](https://github.com/oRAirwolf/grouplab-testdata) at commit `1544f1d` holds its README, `CONTRIBUTORS.md`, one donated submission and the owner's photographs, about 60 MB.

**The README answers section 1's questions in order:**
- what the photographs are and what was done to them;
- the consent text `consent_v1`, verbatim;
- what is not there;
- the size;
- how to cite it;
- how a contributor asks for removal.

"What is not there" states that opted-out submissions exist, are used for testing only and are never published. It also covers files held at intake and the owner's two held photographs.

**Donated submissions are named as the upload page names them.**
- **The rule:** `grouplab intake` named the published directory by the identifier alone. It now uses the UTC date of submission and the identifier, `Intake.DirectoryName`.
- **The check:** the first submission's own directory, `2026-09-14_1a8f39ad` from `2026-09-14T20:41:55Z`, has the same form.
- **The first submission:** it is published as a provenance record with no image. Triage decoded 0 GroupLab markers on all three photographs, so all three are held until a person looks at them and accepts them, as entry 27 section 1 requires.

**The owner's photographs, `owner/`, through a new `grouplab publish-owner`.**
- **Scrubbed like a donation:** every file goes through the same scrubber, with the original name, both hashes and what was removed recorded.
- **An owner's record, not a submission's:** the record says who took the photographs and on what terms they are published. It has no submission identifier and no consent record, as section 2 asks.
- **Refusals:** it refuses to write without both statements, and never overwrites.
- **What was removed:** every file had GPS, and the Pixel motion photographs carried 2.5 to 4.1 MB of appended video each, removed with the rest.
- **The totals:** 26 photographs published, 58 MB.

**Two photographs are held rather than published.** Entry 34 describes `scans/mounted/` as 23 of Alan's own photographs. The directory holds 28 files, and two of them cannot show who took them:
- `Screenshot_20231029-170033.png` is a phone screenshot of a photograph of a target;
- `signal-2023-07-25-20-37-40-354-1.jpg` was saved from the Signal messenger.

I looked at both, and at the four `~2` files entry 20 could not open. All six show only targets and backers, with nothing personal in view. Publishing someone else's photograph under GPL-3.0 cannot be undone, and holding one costs nothing, so each is in `owner/provenance.json` with the reason until Alan confirms taking it. `publish-owner` refuses an existing directory, so publishing them later is a new `owner/` run, or a manual addition that the data test would then check.

**The originals.** They are still in `scans/mounted/` in this working tree, untracked, which is where they were. Section 2 says Alan keeps the originals outside both repositories. Moving them is Alan's to do, and until then nothing stops them being committed here by accident.

**The data test, section 5.** `PublicTestDataCarriesNoLocationNoOptOutAndFullProvenance` still does nothing without a checkout. With one, it now requires:
- a complete provenance record for every donated submission, the directory named from its date and identifier, and publication cleared;
- a provenance record for `owner/` saying who took the photographs and on what terms;
- for every image, no location, and an entry in its record at the hash it was published at;
- every file recorded as published to be present;
- no image anywhere a record does not cover;
- every credit name given to be in `CONTRIBUTORS.md`.

It passes against the checkout. `OwnerPublicationTests` covers `publish-owner` without any data.

**Wiring.** The URL and the pinned commit are in the README's "Test data" and in `CONTRIBUTING.md`. `docs/QUESTIONS-FOR-PLANNING.md` question 13 section 1 is settled by this.

**Removal requests.** The data README says a removal takes the photographs out of the current contents, that removing them from history is decided for each request, and that GPL-3.0 copies already downloaded cannot be recalled. Whether to promise a history rewrite is Alan's to decide, so the README does not promise one.

**Tests:** Core 716 passing, App 4 passing, none skipped, with the data checkout present.

---

## Entry 35. Camera originals, the ignore rule, and two steps waiting on Alan

`docs/NOTES-FROM-PLANNING.md` entry 35.

**Section 1: a file that is not a camera original is held by default.** `CameraOriginal.Problem` is the rule.
- **Two signals, either enough:**
  - a name a screenshot tool or messaging app writes, under any prefix the upload page adds: `Screenshot` or `Screen Shot`, `signal-`, WhatsApp's `IMG-yyyymmdd-WAnnnn` and Facebook's `FB_IMG_`;
  - no camera make in the file.
- **Where it applies:** both `grouplab intake`, where a person can still accept a held file by name, and `grouplab publish-owner`.
- **No false holds on the real files:**
  - all 26 published owner photographs pass, including the four edited `~2` Pixel copies, which keep their camera make;
  - none of the donated submission's three files is flagged, and they stay held for triage's reason alone.
- **The two held photographs:** `scans/mounted/` is no longer in this working tree, so the rule could not be rerun on them. `CameraOriginalTests` checks both of their exact names.
- **A small fix found on the way:** `publish-owner` crashed on a missing source directory, and now refuses with the reason.

**The two held photographs are held permanently,** at `grouplab-testdata` commit `c80055c`. `owner/provenance.json` and the data README give the reason: neither is a camera original, so neither can serve the corpus's purpose, whoever took it. If an original turns up, the original is what would be published.

**Section 2: not done.** The README's removal wording stands, with no promise of a history rewrite. This session's permission check refused the edit that adds a warning that the data repository's history may be rewritten, so the warning waits for Alan.

**Section 3: not done, and waiting on Alan.**
- **Why:** this session's permission check refused deleting the five `refs/original` refs as irreversible local destruction. Nothing was deleted, and the risk entry 35 names is still present.
- **What was checked first:**
  - **The backup:** `C:\Dev\grouplab-backup-2026-09-15.bundle` exists, 166,677,290 bytes. `git bundle verify` reports a complete history. It holds sixteen refs, among them all five `refs/original` refs at the same commits as the local ones.
  - **The pack before any change:** 159.74 MiB in 3 packs, plus 30.50 MiB loose.
- **Once allowed,** the steps are:
  1. Delete each `refs/original/*` ref.
  2. Expire every reflog.
  3. Garbage collect, pruning unreachable objects immediately.
  4. Confirm the five old commits are no longer in the object store.
  5. Report the pack size again.

**Section 4:** `scans/mounted/` is in `.gitignore`.

**Section 5:** `CONTRIBUTING.md` now says that a stage passing its own tests is not evidence that the pipeline is right, and that `EndToEndTests` speaks for the product.
- **The example it gives:** the sighter fault it found, and what that fault had survived.
- **What it asks:** when a stage is added, or what its output means changes, extend that test as well as the stage's own.

**Section 6, items 2 and 3:** reported in "Entry 35 section 6" below.

**Tests:** Core 724 passing, App 4 passing, none skipped.

---

## Entry 37. The opt-out file restored, and consent that wins by content hash

`docs/NOTES-FROM-PLANNING.md` entry 37 sections 1 and 2. Sections 3 to 5 wait, as its section 6 orders.

**Section 1: the `DO-NOT-PUBLISH` check is back.**
- **Its history:** the intake gate of entries 22 and 27 checked for it. Entry 28 section 1 said the page writes no such file, and the check was removed. The first opted-out submission, `2026-09-15_eac0bae6`, carries one.
- **Either signal withholds:** a submission is refused when the file is present or `exclude_from_public_dataset` is true. No agreement between them is required.
- **A disagreement is named:** a file present beside a field reading false is refused with a statement that the two signals disagree.
- **Order:** both signals are checked before the schema version, so an opted-out submission is always reported as opted out.
- **Unchanged:** a missing field is still refused as unknown.

**Section 2: an opt-out wins by content hash, across every submission.**
- **The set:** `Intake.WithheldHashes` reads every submission directory before anything is published. It maps every file hash in each submission that is not plainly publishable to that submission's identifier, counting both the bytes on disk and the hashes `meta.json` recorded.
- **What counts as withheld:** a submission whose opt-out is set by either signal, is missing, or whose `meta.json` cannot be read. Withholding costs nothing, and publishing under ambiguous consent cannot be undone.
- **Required, not optional:** `Intake.Run` takes that set as a required argument, so no caller can publish without it.
- **What a match does:** a file whose bytes are in the set is held whatever triage says, and accepting it by name does not override that. Its entry in the provenance record carries `optedOutIn` with the withheld submission's identifier, beside the record's own `submissionId`.
- **Amended 2026-09-16, entry 58 sections 3 and 4:** the set holds a second key beside the bytes, what each withheld file scrubs to, so another export of the same photograph is held too. A byte hash alone missed that, and the four uploads of 16 September are four hashes of one picture. See "Entry 58 sections 3 and 4".
- **Where the opt-outs are read from:** `grouplab intake` reads them from the directory holding the submission, or from `--submissions`, and prints how many hashes it withheld.

**On the four real submissions**, run into a scratch directory:

| Submission | Result |
|---|---|
| `2026-09-15_eac0bae6` | Refused: `exclude_from_public_dataset` is true and a `DO-NOT-PUBLISH` file is present. Its 9 files are the withheld hashes |
| `2026-09-15_5068047f` | Its one photograph, `001_IMG_1580.jpg`, held for a consent conflict naming `eac0bae6` |
| `2026-09-15_bf6d885d` | Its one photograph, `001_IMG_1696.jpg`, held for a consent conflict naming `eac0bae6` |
| `2026-09-14_1a8f39ad` | Unchanged: its three photographs are held by triage |

**Nothing was published to `grouplab-testdata`.**
- **What publishing would add:** each new publishable submission would be a provenance record with no photograph, carrying the contributor's answers and credit name.
- **Why not yet:** a public record of a submission whose consent is in question waits until Alan has asked the contributor which they meant.

**Worth knowing for when consent is settled.** Triage decoded no GroupLab markers on either photograph, because both are commercial Action Target sheets. Entry 27 section 1's triage therefore holds them until a person accepts them by name, even without the conflict. So frames entry 37 calls the case the project has never had can reach the public data only by a person's acceptance.

**Tests:** Core 725 passing, App 4 passing, none skipped.

---

## Entry 36. The shotGroups fixtures at full double precision

`docs/NOTES-FROM-PLANNING.md` entry 36. Planning regenerated the fixtures at 17 significant digits, and this session verified them independently, removed the harness's workaround and committed them. `tools/` was not edited.

**The regenerated values, against the committed fixtures, on all ten files rather than entry 36's three:**

| | |
|---|---|
| Keys, nine datasets | 73,086 |
| Keys missing or added | 0 |
| Stored numbers whose bits changed | 18,491 |
| Largest relative change of any stored number | 5.53e-16 |
| Numbers moved by more than 1e-14 relative | 0 |

**Every difference is digits appearing, not a value moving,** as entry 36 section 3 measured.

**CSV and JSON agree bit for bit** on 71,056 numeric values across the nine datasets, with three exceptions. All three are negative zeros in `DFlandy01`, such as `shots.y.303`, which the CSV writes as `-0` and the JSON as `0`. They compare equal as numbers, and no statistic here can tell them apart.

**The reconstruction is removed, and the harness passes from the fixture alone.**
- **The change:** `ShotGroupsFixtureTests` now reads `shots.xPOA` and `shots.yPOA` directly. All 67 statistics tests pass, including the four Fligner-Killeen keys question 14 was about.
- **How the reconstruction compares with the true stored values, entry 36 section 4 point 2:**
  - identical on 3,775 of 3,978 coordinates, across every dataset but one;
  - off on 203 coordinates, all in `DFcm`, by at most 3.6e-15.
- **Why `DFcm`:** the reconstruction assumed each aim is a number with at most six decimals. `DFcm` is in centimetres, where that does not hold.
- **Which was right:** the stored values are R's own doubles, so they were right, and the reconstruction was an approximation that happened to be close enough for every key it served.

**A defect in the regeneration: `shotGroups_DFdistr.json` holds its table as strings.**
- **The cause:** `sg_distr.R` formats the table's double columns with `sprintf("%.17g")` for the CSV. It then builds the JSON from the same data frame, so all 8,850 numeric table values are strings, such as `"ES_M": "1.7727261017613225"`. Only `inSection15_3Gate`, an integer column, stayed a number. `sg_dump.R` avoided this by keeping the numeric values aside before formatting, and `sg_distr.R` needs the same.
- **The CSV** is correct at 17 digits.
- **What was committed:** nothing in the test suite reads `DFdistr`, and `tools/` is planning's to change. So `sg_distr.R` and both `DFdistr` files are left out of this commit, at their committed 15-digit versions, and `docs/STATISTICS.md` section 15.4 item 15 says so.

**`docs/STATISTICS.md` section 15.4 item 15** is amended, not deleted. It records:
- what the defect was;
- that it was fixed on 2026-09-15;
- that `digits = NA` would not have fixed it;
- the checks above;
- that question 14's four keys were its only known casualty.

**Tests:** Core 725 passing, App 4 passing, none skipped.

---

## Entry 38. `DFdistr` numeric, and no negative zeros

`docs/NOTES-FROM-PLANNING.md` entry 38, which closes entry 36's one exception. Planning corrected `sg_distr.R` and regenerated `DFdistr` and `DFlandy01`. This session verified the files independently and committed them.

**`shotGroups_DFdistr`:**
- **Types:** 590 rows and 9,440 table values, 2,360 integers and 7,080 doubles, with no strings.
- **Agreement:** the CSV and the JSON agree bit for bit on every one of the 9,440 values.
- **Against the committed 15-digit file:** 1,947 values gained digits, and the largest relative change is 4.44e-16. No other field changed.

**`shotGroups_DFlandy01`:**
- **CSV:** exactly three keys changed, each from `-0` to `0`: `shots.y.303`, `shots.yPOA.303` and `flignerProbe.FlignerY.input.243`.
- **JSON:** no value changed, because jsonlite had already written those three as `0`. Only the generation time moved.
- **Agreement:** the CSV and the JSON now agree bit for bit on every numeric value.

**No fixture CSV holds a `-0`.** All 67 statistics tests pass on the regenerated files, and nothing else in the suite reads them. `docs/STATISTICS.md` section 15.4 item 15 now describes both corrections.

---

## Entry 42. The styling pass

`docs/NOTES-FROM-PLANNING.md` entry 42: the concept screens' palette, type, density and marks, written into code. It changes no number and no behaviour. Every test that existed before it passes unchanged.

**Where it lives,** as section 6 asks:
- **`src/GroupLab.App/Theme/Tokens.cs`:** both palettes, the type scale, the spacing scale and the mark colours. No colour literal appears anywhere else in the application, and `ThemeTests.NoColourLiteralAppearsOutsideTheTokens` fails if one does.
- **`src/GroupLab.App/Theme/AppStyles.cs`:** the control styles, and the Fluent theme's own state colours, pointer over, pressed, checked and focused, set from the palette so hovering a button cannot bring back a Fluent grey. The styles are rebuilt whenever the resolved theme changes.
- **`src/GroupLab.App/Theme/Marks.cs`:** how every mark on the image is drawn. The composite plot of entry 43 will use the same code.

**Themes.** Dark, light and follow system, chosen under Theme and remembered with the units. High contrast is `DESIGN.md` section 19's fourth theme, and later work.

**Contrast.** Six text colours from the concept screens fall below 4.5:1 on the surfaces text sits on: `bg`, `panel` and `panel2`. Section 2 says to adjust the value rather than the role, so each is moved along its own hue, toward white in the dark theme and toward black in the light, by the smallest step that reaches 4.5:1:

| Theme | Role | Concept screen | Now | Worst ratio now |
|---|---|---|---|---|
| dark | `faint` | #697079 | #858b92 | 4.53 |
| dark | `alert` | #e0604a | #e1634d | 4.52 |
| light | `faint` | #868c94 | #666a70 | 4.52 |
| light | `amber` | #b46f16 | #965d12 | 4.51 |
| light | `teal` | #3f8873 | #367462 | 4.56 |
| light | `alert` | #bf4531 | #b8422f | 4.52 |

**Two light values were left unstated by entry 42, and are chosen here.**
- **Primary button text:** white on the adjusted amber (5.4:1), because #17120a on it is 3.4:1.
- **Selected and good tints:** amber and teal nine tenths of the way to white, on which amber and teal text reach 4.7:1 and 4.8:1.

`ThemeTests` checks every pair.

**Type.** IBM Plex Sans, Sans Condensed and Mono are embedded as resources, with the SIL Open Font License shipped beside the application and listed in `THIRD-PARTY-NOTICES.md`.
- **Section labels:** uppercase at 10 point semibold, spaced, in `faint`.
- **Figures:** mean radius leads at 29 point mono and sigma follows at 21. Every interval line is mono at 11.5 in `dim`.
- **Shot rows:** mono.

**Layout.**
- **Structure:** the toolbar sits on a bar with a one pixel separator; the right column is 372 wide; a status line runs along the bottom.
- **Spacing:** section padding is 12 by 14, and every spacing value is from the scale.
- **Buttons:** radius 4, padding 6 by 12, 12 point at weight 500. The current tool is amber on its tint.
- **The scale's pill:** teal when the sheet registered, amber for a reference drawn by hand. The navigation rail is not built, as section 4 says.

**The marks,** section 5:
- **The stroke:** every mark is a two tone stroke, 3 pixels of near black at 55 percent under its colour at 1.6 pixels, in screen pixels at any zoom.
- **An impact:** a ring at the true hole diameter once the calibre and the scale are known, with a one pixel pip, in `impact`. Selected is amber at 2 pixels; excluded is `dim` and dashed.
- **Other marks:**
  - the scale reference is a teal line with a filled circle at each end;
  - the point of aim is a teal cross, not a circle;
  - a bull centre is a small `faint` cross;
  - a missing marker keeps its cross, in `alert`.

**Checked by eye, on screenshots the test writes to `out/screens`,** section 7 points 2 and 3:
- **`marking-dark.png` and `marking-light.png`** show the whole window in each theme.
- **`marks-closeup-dark.png` and `marks-closeup-light.png`** show bull 13 of `gl-cf25-ltr-1-600-dpi.png`, detected, with a .308 calibre and three impacts: on bare paper, on the inner printed ring, and on the printed "13".
  - **Readability:** all three rings read, as does the point of aim over the bull.
  - **The warning:** the size check on the ring's impact now says it sits on the printed target, because the window holds the detected artwork.
- **`photo-marks-dark.png`** is marks on the donated `001_IMG_1696.jpg`, in hard sunlight, on paper, printed rings and the dark backer. That photograph has no shadow across it, so the shadow case section 7 names is not yet looked at.

**The screenshots are for planning to set beside `docs/figures/screens`.** The content differs, and entry 43 is that difference. They are in the working tree's `out/screens`, which git ignores.

**Where this departs from entry 42,** and why:
1. **Units are not yet set smaller than their figures.** "0.1046 in" is still one run of text at the figure size. Setting the unit at 13 point means splitting the text into runs, and the figure text is what the existing tests read, which section 1 says must pass unchanged. The figure stack of entry 43 is the place to do it.
2. **The image no longer colour codes a shot's provenance.** Section 5 gives one impact colour, where automatic, corrected and manual shots were gold, orange and green. Provenance is still in the selected shot's panel and the report, but not on the image or in the shot list's rows. Whether it should come back is a design question.
3. **Mark labels are light text on a dark plate, with a bar in the mark's colour.** The first screenshots drew a label in the mark's own colour on the halo, and red on near black at 11.5 point could not be read.

**How the frames are captured.** The headless tests now draw through Skia rather than the null renderer, so every app test renders for real. CI on Linux and macOS is the check that Skia's native libraries load there.

**Tests:** App 14 passing, none skipped. The 10 that existed are unchanged, and the new ones are contrast, colour literals, the theme choice, and the screenshots. Core is unchanged at 729.

---

## Entry 46. The alert ring at the measured size, and a real run's log

`docs/NOTES-FROM-PLANNING.md` entry 46 sections 0 to 2. The rest of the entry is reported below as it lands.

**Section 1: the impact ring is the calibre.**
- **What the code did:** `MarkingCanvas` drew every impact ring at the calibre's diameter at the image's local scale. So every ring was the same size, whatever the hole measured, and entry 42 section 5's "true hole diameter" was not what it drew.
- **The close-up, measured by the screenshot test:** the three impacts on bull 13 draw at 0.308 in. Shot 2 reads 0.508 in across. Its alert ring sat 6 screen pixels outside its impact ring, which at the close-up's zoom is a few hundredths of an inch and says nothing about size.
- **The change:** the impact ring stays at the calibre, which planning offered to write into the entries. A flagged hole's alert ring is drawn at the extent it reads, half of it as the radius, and never closer than 6 pixels outside the impact ring. On shot 2 that is 0.508 in against 0.308, so two holes marked as one look wrong on the image.
- **Why only flagged holes:** the size check reads a hole's extent only where it finds a dark region on paper within two diameters. On ink or a dark backer it reads nothing, so a ring at the measured size for every shot would fall back to the calibre on exactly the marks where the question matters least clearly.
- **Test:** `ScreenshotTests` asserts every impact ring at 0.308 in and the flagged ring at the size its flag reads, and writes the figures to `out/screens/marks-closeup-rings.txt`.

**Section 2: a real run writes its log.** The window screenshots come from the headless test, which starts no log, so the panel there says logging has not started. The explicit check was a real run:
- **The build:** a Debug build run from its place in the repository.
- **The run:** driven through UI Automation. It clicked Open image, typed the path of `scans/phase0/gl-cf25-ltr-1-600-dpi.png` into the real file dialog, waited, and closed the window.
- **The result:** `out/logs/grouplab-20260915-180437-32256.log`, which reads in full:
  - `app.start`, with `channel=debug` and `logdir=<repository>/out/logs`;
  - `app.window`;
  - `dialog.open` and `dialog.result chosen=True`;
  - `image.open file=gl-cf25-ltr-1-600-dpi.png pathid=ab0b3ee4`, with format, size and resolution;
  - `app.exit code=0 seconds=25.5`.
- **Why the directory was empty:** `out/logs` had not existed until this run. The only build that had been run from the repository was the copy Alan had open, built before logging existed.

**Tests:** App 31 passing, none skipped. Core unchanged.

---

## Entry 35 section 6. The sheet names its own definition, and the gate record on three platforms

`docs/NOTES-FROM-PLANNING.md` entry 35 section 6 items 2 and 3, the order entry 46 section 4 gives.

### Item 3: the definition read off the sheet

`grouplab analyze <image>` no longer needs `--target`, and the marking screen's Detect asks for a definition only when the sheet's codes cannot give one.

**How.**
- **The codes, not the markers:** every GroupLab sheet prints its definition as a GLTD-B frame in each QR code, and the identifier is computed from the frame's body. So a frame that passes its CRC names exactly one definition. The built-in definitions share marker ids, which is why nothing is chosen from the markers.
- **`SheetIdentification`, in Core:**
  - it reads the codes at full, half and quarter resolution and stops at the first that yields a valid frame;
  - it finds the definition with that identifier among the candidates;
  - it records `S0.identify` in the trace.
- **What it refuses, with the reason:** codes naming two definitions or two tiles, a damaged frame, and an identifier not among the candidates.
- **Where the candidates come from:**
  - **The command:** every `*.gltd.json` under `--library` directories, by default `targets`, frozen definitions included.
  - **The application:** the definitions shipped beside it, which now include the three frozen Phase 0 definitions, so the sheets Alan has already printed are recognised. The print screen's list does not look in the frozen directory.

**Reading binary QR codes through OpenCV took two detectors.** GLTD-B frames are binary, and both of OpenCvSharp's QR decoders return strings. Measured on the Phase 0 scans before choosing:
- **The WeChat detector,** without its neural network models, finds codes reliably. But it returns the payload through UTF-8, so every byte above 0x7F came back as U+FFFD and no frame passed its CRC.
- **The plain `QRCodeDetector`** returns one byte per character, which Latin-1 turns back into the frame exactly. But on its own it missed the codes on the 600 DPI scan, a tile and a photograph it was tried on.
- **So the backend locates with WeChat and decodes with the plain decoder at the corners WeChat found,** and adds whatever the plain detector finds alone.

**On the 37 Phase 0 images, 29 name the definition they were printed from, and none names a wrong one.**

| Images | Identified | At |
|---|---|---|
| Letter reference sheets, 300 and 600 DPI, including the 96.2 percent and blank data block renders | 10 of 13 | mostly full resolution; 600 DPI sheets 1 and 3 at quarter and half |
| `GL-LR300-T` tiles, 300 and 600 DPI | 8 of 8, each with its own tile index | full |
| The four frames of 13 September | 4 of 4 | full |
| `main1-3`, `main_flat1-3` | 6 of 6 | full |
| `telephoto1-3`, `ultrawide1-3` | 1 of 6 | full |

- **The eight that are not identified:**
  - the 600 DPI scan turned 180 degrees;
  - the 300 DPI 96.2 percent scan;
  - the 300 DPI filled data block scan;
  - five of the six wall photographs.
- **What happens to them:** they fall back to naming the definition, with the reason. Nothing was tuned to raise the count, which would fit the reader to the frames it is reported on.
- **Speed:** a readable code at full resolution costs 0.2 to 3.4 s. A 600 DPI scan that needs every resolution costs about 8.5 s.

**Checked on a real run.** The Debug application was driven through UI Automation: open `gl-cf25-ltr-1-600-dpi.png`, press Detect, close. The log reads:
- `detect.identify definition=GL-YCSK-DZZ1-R0VJ-4T5Y tile=0 codes=1`;
- then `detect.run`, with 34 of 34 markers and registration RMS 0.0022 in.

No picker opened.

**Tests.**
- **`SheetIdentificationTests`:**
  - three printed scans and a photograph, one of them a tile;
  - four fresh renders of built-ins, one of them tile 3 of `GL-LR300-T`, which CI decodes on all three platforms;
  - refusals through a fake backend: two definitions, two tiles, an identifier not among the candidates, a damaged frame and no codes.
- **`EndToEndTests`:** reruns the rendered sheet without a definition and requires the identical shots.

### Item 2: the Phase 0 gate record on three platforms

**The workflow.** `.github/workflows/gate-record.yml` runs on every push, on Windows, Linux and macOS:
- it reruns the eight Phase 0 spike commands;
- it compares every record byte for byte with the commit, leaving out only `threshold.json`'s `detectMs`, a detection time;
- it hashes each console table into the run summary.

**Before the first run, locally on Windows.**
- **Seven records** reproduced exactly.
- **`photos.json`** lacked the two camera fields the code has written since the lens work, and is regenerated. No measured value in it changed.
- **The newline:** the records were written with the platform's newline, CRLF on Windows and LF elsewhere. They are now written with LF everywhere, so the bytes on disk are comparable.

**The first run: Windows reproduces the record exactly; Linux and macOS do not.** The step that should have said how they differ stopped at the first difference, because the runner's shell ends a step on a failing pipeline. It now reports how many values differ, the largest difference under each field name, and anything that is not a number. The second run is the comparison below. Every run now also uploads each platform's records and console tables, so the comparison can be checked.

**Windows, `windows-latest`:** all eight records byte for byte, as locally.

**Linux, `ubuntu-latest`, x64: every console table is identical to Windows, line for line.** The records differ in every file, all below what any table reports:

| Record | Values that differ | Largest difference |
|---|---|---|
| `sheets.json` | 254 | 0.0001 dmm in a bull error; scales below 1e-10 |
| `photos.json` | 285 | 0.0006 dmm in a bull position; the lens model's frame edge 0.0018 px |
| `markers.json` | 289 | 0.0071 dmm, one bull in one random marker subset |
| `refinement.json` | 1 | 0.0001 dmm |
| `threshold.json` | 3 | 0.0001 dmm |
| `scale.json` | 62 | below 1e-10 |
| `field.json` | 85 | 0.0001 dmm; a correlation by 4e-13 |
| `detectors.json` | 100 | 0.0001 dmm |

**macOS, `macos-latest`, arm64: the paper gate and photograph gate tables, `sheets` and `photos`, are identical to Windows.** 20 console lines differ, all in two studies:
- **Measurement 1, marker count:** one figure, 0.00412 against 0.00411 in, for 12 markers on sheet 3.
- **Measurement 2, corner refinement, on paper:** three rows change in the fourth or fifth decimal place of an inch. The largest change is the 300 DPI quarter-module window, 0.00836 against 0.00887 in.
- **Measurement 2, on the synthetic raster:** all 16 rows change. The windows of 1.5 and 2 modules, which the study shows are broken, change most, up to 0.01195 against 0.00845 in. The shipped window changes least: corner RMS 0.159 against 0.158 px at 600 DPI. At 300 DPI its bias is -0.120 against -0.119 px and one bull figure 0.00028 against 0.00029 in.

**In the records, macOS differs in more values than Linux.**
- **Counts:** 330 in `sheets.json`, 541 in `photos.json`, 710 in `markers.json` and 15,813 in `refinement.json`.
- **Most of the last:** the synthetic raster's corners listed in a different order, which is the marker detector returning the markers in another order.
- **The largest move of a bull anywhere:** `telephoto3.jpg`'s scoring bull 24, 6.7277 against 6.4236 dmm (0.0265 against 0.0253 in), where one ray lost its edge point, 30 to 29. That frame is excluded from the gate because the sheet overflows it, which is why no table shows it.

**What this establishes, and what it does not.**
- **Every figure the Phase 0 gates report reproduces to its printed precision on all three platforms.**
- **The records are not byte-identical off Windows,** so entry 32 section 3's first condition is not met. Whether the differences count as explained is planning's decision. Until then the workflow fails on Linux and macOS, which is the truthful state.
- **Two sources, not separated here.** The differences can come from the native OpenCV in each platform's runtime package, which is a different build on each, or from each platform's maths library, which .NET's `Math` functions call. This comparison cannot tell them apart. The reordered markers on macOS can only have come from OpenCV.

**Tests:** Core 740 passing, App 31 passing, none skipped.

---

## Entry 37 sections 3 to 5. What the donated submissions showed, and the sheet size the contributor stated

`docs/NOTES-FROM-PLANNING.md` entry 37 sections 3 to 5. Nothing is published: the consent conflict of section 2 still holds both photographs.

### Section 3: the wording of the request, measured

The four submissions' `meta.json` answers, counted:

| Submission | When | Answers filled | Photographs | What happened to them |
|---|---|---|---|---|
| `2026-09-14_1a8f39ad` | Before the post was edited | 0 of 6 | 3 | Held by triage |
| `2026-09-15_5068047f` | After | **6 of 6** | 1 | Held for the consent conflict |
| `2026-09-15_bf6d885d` | After | **6 of 6** | 1 | Held for the consent conflict |
| `2026-09-15_eac0bae6` | After, opted out | 3 of 6 | 9 | Withheld |

The two complete submissions give backing, attachment, distance, calibre (5.56 NATO and 8.6 Blackout), a credit name, and in the notes the exact commercial target. Planning read both photographs as meeting the brief: whole targets, still stapled to the backer, with all four edges in frame. **The difference between a useless submission and a good one was the wording of the request, not the contributor.**

### Section 4: what an iPhone upload keeps

**The browser was Chrome for iOS, not Safari.** All three later submissions carry the same user agent: iOS 18.7.10, `CriOS/152`. Chrome on iOS uses WebKit, so the upload page's `accept` attribute is answered for WebKit's file picker on an iPhone. Safari itself has still not been used.

**Amended 2026-09-16, entry 58 section 1: read nothing from a user agent on iOS.** DuckDuckGo reports itself as Safari, so the field cannot tell those two apart, and no absence of a browser can be concluded from it. Four deliberate uploads later settled what actually happens: every iOS browser preserves the file, and a photograph captured inside the page is what loses the camera data. See "Entry 58 sections 3 and 4".

**The metadata survives the upload.** Checked on both publishable photographs through `grouplab scrub`, into a scratch copy that was then deleted, printing names and never values:
- **Present in each original:** a `LensModel` tag, and a GPS block.
- **What scrubbing removes:** the GPS block, APP10, the thumbnail, and 36 other EXIF fields.
- **What scrubbing keeps:** ten fields, `Make`, `Model`, `Orientation`, `ExposureTime`, `FNumber`, `ISOSpeedRatings`, `FocalLength`, `PixelXDimension`, `PixelYDimension` and `FocalLengthIn35mmFilm`.

**`LensModel` is not among them.** The scrubber's keep list is entry 29's, and `LensModel` is not on it, so a published iPhone photograph will not carry "iPhone XS Max back dual camera 4.25mm f/1.8".
- **Lens grouping is unaffected:** it is built from the focal length, f-number, 35 mm equivalent, digital zoom and image size, which are kept.
- **What is lost:** only the lens's name. The received files, which are never published, keep it.
- **The keep list is unchanged.** It is entry 29's decision, so it stays until planning asks for `LensModel`.

### Section 5: the stated sheet size, structured

**`statedSheetSize` in the provenance record.** `grouplab intake` reads the notes with `StatedSheetSize.Parse` and writes the size beside the answers, which stay as given: `source` (`answers.notes`), `text`, `width`, `height` and `unit`.
- **What counts as a size:** two numbers joined by x or ×, followed by a unit. The unit is in, inch, inches, an inch mark, mm or cm.
- **The inch mark:** both real notes end in U+201D, the curly quote an iPhone keyboard types for `"`, and it is read as one.
- **Nothing is written for:**
  - a note with no size, or with two;
  - a size with no unit, because a guessed unit is a scale error of 2.54 or 25.4 times.
- **On the real notes:** `5068047f` gives 17.5 by 23 in, and `bf6d885d` gives 23 by 35 in.

**The marking path offers it.**
- **When it appears:** opening an image that sits beside a provenance record listing it by name, as `grouplab-testdata` publishes it, says the record gives the sheet's size. The rectangle tool then offers "Use the stated sheet size" beside its own boxes.
- **What the button does:** it fills the width and height in the user's unit, in the order the contributor wrote them. It asks the user to tap the sheet's own corners, and to swap the numbers if the first side tapped is the other one.
- **What it never does:** set a scale by itself.
- **No lookup table:** there is none of third-party target sizes, and the size is used as the contributor stated it.

**Tests.**
- **`StatedSheetSizeTests`, Core:** seven sizes read, six notes that give nothing, the metric conversions, and the record read beside an image.
- **`IntakeTests`:** the structured size beside the notes as given, and none from empty notes.
- **`StatedSheetSizeTests`, App:** the offer fills 17.5 and 23 in inches, and an image without a record is offered nothing.
- **Counts:** Core 756 passing, App 32 passing, none skipped.

---

## Entry 46 section 3. Shot provenance in the panel, not on the image

`docs/NOTES-FROM-PLANNING.md` entry 46 section 3, planning's decision on the provenance colours entry 42 dropped from the image.

- **Not on the image.** The marks are unchanged: colour there means selected and excluded, and amber means "this one".
- **A count line above the figures.** It names only the kinds of placement present, for example "12 shots: 9 detected, 2 corrected, 1 placed by hand." Any exclusions and shots marked not a shot follow on the same line. The "Placed: automatic, corrected, by hand" line that sat at the foot of the panel is removed, because it said the same thing below the figures instead of above them.
- **A provenance column in the shot list.** Each row carries "detected", "corrected" or "by hand" in faint. That uses a new `faint` text style over the palette's faint colour, which the contrast test already holds to 4.5:1 on every surface in both themes.
- **Why it matters, as planning put it:** provenance is the record of where a human judgement entered a measurement. A group of nine detected shots and one of nine placed by hand deserve the same figures and a different amount of confidence.
- **Tests:** `ProvenanceTests` checks the count line names only the kinds present. It then builds two detected shots, moves one so it counts as corrected, places one by hand, and requires the count line, no "Placed:" line, and a faint word on each row in order. App 34 passing, none skipped.

---

## Entry 47. Build and test red on Linux and macOS: the cause, and identification measured per platform

`docs/NOTES-FROM-PLANNING.md` entry 47.

**What went red.** `build and test` was green on all three platforms at `c622591`, and red on Linux and macOS at `a97bcb0` and `b0091ad`. Three tests were involved, all about identifying a sheet from its codes:
- **Linux:** the clean 300 DPI render of GL-CF25-LTR, and the end-to-end test's rerun without `--target`.
- **macOS:** the end-to-end rerun alone.

It is green again on all three at `eddee00`, with Core 756 and App 34 on each.

**Not a missing contrib module.** Planning's diagnosis was that the Linux and macOS runtime packages lack `opencv_contrib`'s `wechat_qrcode`. Checked two ways:
- **The packages, opened.** `libOpenCvSharpExtern.so` in `OpenCvSharp4.official.runtime.linux-x64` 4.13.0.20260627 and `libOpenCvSharpExtern.dylib` in `OpenCvSharp4.runtime.osx.arm64` both carry the `wechat_qrcode_WeChatQRCode` exports, as `OpenCvSharpExtern.dll` in the Windows package does.
- **The logs.** Each failure read "no code on the sheet could be read", not a missing entry point or a type initializer. On the same runs, identification passed on Linux and macOS for three printed scans, one of them a tile, and for a photograph. That could not have happened had the constructor thrown.

**The cause: detection, on images whose code modules are under five pixels.** A 300 DPI code module is about 4.7 px. The WeChat and plain detectors in the Windows build read both synthetic images at full resolution; the Linux and macOS builds did not. Half and quarter resolution only shrink the modules.

**The fix, `b315853`.**
- **Double resolution is tried second,** upscaled linearly.
- **A failing identification test now says what went wrong:** for each resolution, it reports the WeChat boxes and texts, the plain decoder at those boxes, and the plain detector alone. A failure that happens only on a runner then says which step came back empty.

**Doubling had a cost on large images, now bounded.** On the first Windows sweep after the fix:
- **`gl-cf25-ltr-1-600-dpi.png`:** 52.7 s. It doubled a 4958 by 6458 px scan before reading at quarter resolution.
- **Where no code is read:** 16 to 47 s.

A resolution that would make the image longer than 8000 px, a 4000 px photograph doubled, is now skipped and recorded as skipped. The same sweep then names the same images, and the 600 DPI scans take 0.7 to 9.0 s.

**Identification measured per platform.** `grouplab identify sweep` runs every Phase 0 image through identification against the whole of `targets`, and counts against the definition and tile each was printed from. It runs in the gate record workflow on each platform and puts its table in the run summary. It fails only on a wrong name, since a refusal is the safe outcome.

**On Windows: 33 of 37 named correctly, 0 wrongly, 4 not named.** That is up from 29 before double resolution:

| Images | Named | Read at |
|---|---|---|
| Letter reference sheets, 600 DPI | 6 of 7 | full; sheets 1 and 3 at quarter and half |
| Letter reference sheets, 300 DPI | 6 of 6 | full; the 96.2 percent and filled data block scans at double |
| `GL-LR300-T` tiles, 300 and 600 DPI | 8 of 8, each with its tile | full |
| The four frames of 13 September | 4 of 4 | full |
| `main1-3`, `main_flat1-3` | 6 of 6 | full |
| `ultrawide1-3` | 3 of 3 | full; `ultrawide1` and `ultrawide2` at double |
| `telephoto1-3` | 0 of 3 | none |

- **Not named:** the 600 DPI scan turned 180 degrees, and the three telephoto frames.
- **Time for a readable sheet:** 0.2 to 9.0 s, except `ultrawide2` at 16.5 s.
- **Time where no code is read:** 8.5 s for the rotated scan and 15.5 to 17.9 s for the telephoto frames, which a 4000 px photograph spends at double resolution.
- **Measured on the three CI runners at `826bd15`, none naming a wrong definition:**

  | Platform | Named correctly | Not named |
  |---|---|---|
  | `windows-latest` | 33 of 37 | the rotated 600 DPI scan, `telephoto1-3` |
  | `macos-latest`, arm64 | 33 of 37 | the same four |
  | `ubuntu-latest`, x64 | **32 of 37** | the same four, and `gl-cf25-ltr-1-600-dpi.png` |

- **The one platform difference:** Linux does not name `gl-cf25-ltr-1-600-dpi.png`, which Windows and macOS read only at quarter resolution. Named with `--target`, that scan registers on Linux as it does on Windows: the sheets table of the gate record is identical there. So identification works on all three platforms, and is one sheet short on Linux; it is not the same on all three.
- **The runners are about twice as slow as this machine:** an image that gives no code took 4.1 to 45.4 s there.

**The rule of entry 47 section 4 is taken.** When a required check goes red, the next commit makes it green or deliberately reverts, and an expected red, such as the gate record's, is named in the commit message. `fd05dac` and `eddee00` went up in the same push as the fix, before its CI result was known. Under the rule, the fix would have gone alone.

**Tests:** Core 756 passing, App 34 passing, none skipped, on all three platforms at `eddee00`.

---

## Entry 48. The gate record difference localised, and the lens model kept

`docs/NOTES-FROM-PLANNING.md` entry 48. **How the difference was localised:** every gate record run uploads each platform's records, and the records hold each stage's quantities. Matching them marker by marker and corner by corner, rather than by position in a list, gives the first stage whose output differs from Windows. The comparison is of the records from `826bd15`.

### Section 2, Linux: explained

- **a. Every gate verdict is identical.** Every console table is identical to Windows, line for line, so every verdict on every frame is too.
- **b. The stage is S3, the homography.**
  - **Identical to Windows in every record:** the S2 corner positions, to the last recorded digit, and the corner sets and inlier flags.
  - **The first quantity that differs:** the homography `Cv2.FindHomography` returns over those corners. For a scan it is the only computation between the corners and the mapping.
  - **Downstream of it:** everything else that differs is computed from that mapping: the scale, a photograph's lens coefficients, and the bull and corner errors.
- **c. The mechanism, and its bound.**
  - **The mechanism:** `findHomography` ends its RANSAC with an iterative Levenberg-Marquardt refinement over the inliers. The inputs and the inliers are identical on both platforms, so what differs is the arithmetic of the iterations in a different native build, not which minimum is found.
  - **On the scans:** the homographies differ by at most 3.4e-7 dmm anywhere within the markers' extent.
  - **Bull figures:** a bull figure differs only where its value sits on a rounding boundary of the 0.0001 dmm the records keep.
  - **Where it is amplified:** the largest effect is 0.0071 dmm, on one bull in one random subset of four to six markers in measurement 1, where a poorly conditioned fit magnifies it. On a photograph it is 0.0006 dmm.
  - **Why it cannot grow into a verdict:** a difference that starts at 3.4e-7 dmm stays under a hundredth of a micrometre unless the fit is ill-conditioned, and the gated frames have 9 to 34 markers.

### Section 2, macOS: localised, not yet explained, nothing changed

- **a holds.** The paper gate and photograph gate tables are identical to Windows.
- **b: the first stage that differs is S2, corner refinement.** Planning's guess was an iterative fit; the corners already differ before any fit runs.

**What differs at S2, measured against Windows' corners matched by marker and corner:**

| Images | Refinement | Corners that move | Largest move | Largest bull difference |
|---|---|---|---|---|
| Scans and photographs | subpixel, the shipped window | 0 to 2 of 104 to 136 per image | 0.0005 px | 0.0003 dmm |
| Letter scans | contour | every corner | 0.098 px at 600 DPI, 0.044 px at 300 | 0.0003 dmm |
| Letter scans, 300 DPI | subpixel, quarter module | 1 to 5 per image | 1.4 px | 0.28 dmm |
| Synthetic raster | subpixel, the shipped window | every corner | 0.024 px | 0.0026 dmm |
| Synthetic raster | subpixel, 2 modules | every corner | 0.43 px | 2.68 dmm |

**On the synthetic raster the markers also come back in a different order:** 52 of the 136 corners at 600 DPI and 80 at 300 DPI sit at a different position in the list, with the same set of corners. The order is the order RANSAC samples from, so it can change which subsets the homography fit draws.

**S3 differs as on Linux, more.** On the scans the homographies differ by up to 1.1e-5 dmm within the markers' extent.

**One amplification, and it is the one that matters.**
- **The frame:** `telephoto3.jpg`, excluded from the gate because the sheet overflows the frame, has four markers.
- **What is identical on macOS:** all 16 of its corners, and its inlier flags.
- **What moved:** its scoring bull 24, by 0.30 dmm, because the edge fit kept 29 edge points instead of 30.
- **Why that matters:** a difference of the S3 size tipped the edge fit's acceptance of one edge point. That is how a marginal frame could change a verdict. The step is a threshold in the bull locator, not an iterative fit.

**c: the mechanism is not yet named.**
- **The candidates in S2:** OpenCV's `cornerSubPix` and the ArUco contour refinement in the arm64 build, which is the only native code there, and the order in which the ArUco detector returns its candidates.
- **What would separate them:** feeding macOS Windows' corners in Windows' order, from the records, and rerunning S3 onward. If the bulls then agree as closely as Linux's do, S2 is the cause apart from the edge fit's threshold.
- **The obvious deterministic step:** sort the detected markers by identifier before anything uses them. That removes the ordering effect whatever the detector does.

None of this is changed, as section 2 asks.

### Section 3: the lens model kept

- **The rule, now in `ImageScrubber`:** keep what describes the camera and the exposure, and drop everything that describes where, when, who, or anything a person typed. `LensModel` is kept.
- **The two whitelists agree.**
  - **What the log records now:** the metadata reader now reads the lens model, ISO and exposure time, so `ImageFacts` records the same camera and exposure facts the scrubber keeps.
  - **What holds them together:** `WhitelistTests` requires every camera fact logged to be a kept field and every kept field to be logged. `CameraFieldsTests` checks that scrubbing keeps the lens model, ISO and exposure, and still drops the capture date and the location.
- **Republished.** The owner's photographs were run through `grouplab publish-owner` again from the originals, with the same two photographs held and their published wording kept. Now at `grouplab-testdata` commit `d35ef99`:
  - **Changed:** 13 of the 26 published photographs, all Pixel frames, each only in keeping `LensModel`.
  - **Unchanged:** the other 13 come out byte-identical.
  - **The provenance record:** it carries the new published hashes and kept lists, the new intake time, and the `optedOutIn` field the record format gained under entry 37.
  - **No donated photographs:** none are published, so none needed republishing.
  - **Its README:** it states the rule.
- **Not edited: `tools/scan_analysis/scrub_exif.py`.** It says to keep its whitelist identical to the scrubber's, and it does not keep `LensModel`. It is planning's file.
- **Not done: `LensModel` in the lens grouping key,** as section 3 asks.

### Section 4

`docs/DETECTION-PIPELINE.md` now says the number that must stay at zero is the wrong names, and that every refusal is kept by anyone raising the hit rate.

**Tests:** Core 757 passing, App 35 passing, none skipped.

---

## Entry 49 section 2 and entry 51. The marker sort, held for a question, and the check's fixes

**The gate record under entry 49 section 1's rule, measured at `e03415c`:** the printed tables are identical on Windows and Linux, which pass, and differ on macOS, which fails. Build and test is green on all three.

**Entry 49 section 2: implemented and measured, not committed.**
- **What sorting does on Windows:** it changes every printed Phase 0 table and no gate verdict.
- **The largest effect:** on the mounted photographs, where RANSAC settles on a different consensus set in a different sample order. `ultrawide3.jpg` keeps 51 corners of 128 instead of 42, and has 18 scoring bulls over the gate instead of 21.
- **Why it is held:** committing the sort means regenerating committed records and the figures documents quote from them, including the mounted benchmark of `docs/PHASE0-RESULTS.md` section 4.5.
- **Where it went:** `docs/QUESTIONS-FOR-PLANNING.md` question 15, with the options.
- **Also waiting on it:** the macOS rerun from Windows' corners, whose order the sort decides.

**Entry 51 section 6:**
- `DESIGN.md` section 20 now says .NET 10.
- `docs/PHASE0-SPIKE-BRIEF.md` now cites `DESIGN.md` section 19.
- Entries 36 and 31 have their stale exceptions closed.
- Five paths in entries 41 to 43 now start with `src/`.

---

## Entry 49 section 2. The macOS rerun from Windows' corners: refinement is the first of two, not the only one

`docs/NOTES-FROM-PLANNING.md` entry 49 section 2, ordered by entry 52 section 5 item 4. **The question:** whether corner refinement is the only place the platforms diverge, or the first of two.

**Reproduce:** `grouplab spike corners --export <file>` on one platform and `--replay <file>` on another. In the gate record workflow the `windows corners` job exports the journal and the `macos-latest, from windows corners` job reruns from it, into the run summary. Both report and neither gates.

**The method.** The export records every detection the `markers` and `refinement` measurements ask for, in the order they ask: the image by hash, the settings it was given, and the corners it returned at full precision. The replay hands those back instead of detecting, and reruns the same two measurements with the homography, the warp and the bull fit still the replaying platform's own. Those two are the tables macOS prints differently. The hash is what makes the answer readable: it separates an input this platform built differently from corners this platform refined differently.

**The control, on Windows:** 78 detections replayed, all 78 images identical, and both tables reprint exactly as committed.

| | macOS, detecting for itself | macOS, from Windows' corners |
|---|---|---|
| `markers`, lines differing from the committed Windows table | 2 | 1 |
| `refinement`, lines differing | 19: 3 paper, 16 synthetic | 1, synthetic |
| Images identical to Windows before detection | | 62 of 78: all 14 of `markers`, 48 of 64 of `refinement` |

**1. Sixteen of the nineteen `refinement` differences were never detection.** The 16 synthetic calls, eight refinement variants at each of 600 and 300 DPI, are handed an image macOS built differently; every paper call and every `markers` call is handed a byte-identical one. The synthetic scan is rendered and then warped by the same native library that detects it, so the difference enters before detection is asked anything. Replaying Windows' corners collapses those 16 rows to one: 300 DPI, subpix 1.5 modules, worst bull 0.00225 in against 0.00226 here, which the bull fit reads off macOS's own raster.

**2. The three differing paper rows were detection, and they come right.** 600 contour, 300 contour and 300 subpix 0.25 module all agree once Windows' corners are used, on byte-identical images. Entry 48 localised this by matching records corner by corner; this shows it directly.

**3. One row differs with the image identical and the corners identical.** `markers`, sheet 3 at 300 DPI, four markers of 34: the 90th percentile worst bull is 0.03100 in on Windows and 0.03099 here. The 600 DPI row above it, which also differed, comes right. Nothing about that row's input differs, so **corner refinement is not the only place the platforms diverge.** It is the first of two, which is what entry 49 section 2 said the rerun would be worth knowing either way.

**What the second divergence is not yet known to be.** That row is a percentile over 40 random four-marker subsets, each refitted by the native homography solve and then read through the bull fit, and both are native code built separately for each platform. Which of the two moves is the same experiment one stage lower, replaying the homography as this replays detection, and it is not claimed here. The difference is one in the last printed digit, 0.00001 in, on the least supported fit in the table.

**What this does not change.** The gate record still fails on macOS, and nothing about the gate is touched (entry 49 section 1). This says where the difference enters, which is what was asked.

**Tests:** Core 758 passing, App 35 passing, none skipped.

---

