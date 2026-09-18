## 2026-09-18, entry 103: the analysis screen split from the editor, the composite plot, two concept errors not to inherit, and questions 15 and 18 answered

**Status: open.**

Alan cannot print, shoot, scan or photograph anything before the weekend, so this entry is deliberately all work that needs none of that. He has also said the concept screenshots are the design guideline and that the assignment editor is the beginning of the process and what to work towards.

**Order for this run.** Sections 1, 2 and 5 are the work. Section 4 is documents only and is cheap. **Section 3 regenerates committed evidence and belongs in its own commit, after the rest, and it is fine for it to wait for the next run.** Do not let the regeneration sweep eat a run that was meant for the screen.

### 1. The one screen becomes the two the concept shows, and the composite plot goes in the space that makes

**What is there now.** `MainWindow` is one 2161-line screen that marks, reviews and reports figures at the same time. It already carries the concept's chrome from entry 93: the rail, the breadcrumb header, the review pill, the tool strip with keycaps, the paper sheet on dark chrome and the accents.

**What the concept has instead.** Two screens behind one rail destination. `docs/figures/screens/assignment-editor.png` is the first: the sheet, the tool strip, the needs-a-decision card, the selected detection, the review queue, and **Discard edits** and **Accept and analyse** at the top right. `docs/figures/screens/analysis-dark.png` is the second: the composite plot in the centre, the sheet thumbnail and the load block on the left, the figure stack and the judgement cards on the right, and **Show work**, **Export** and **Report** at the top right with a registration pill where the review count was.

**Both concepts show the rail's first icon active, so this is one destination in two states, not two destinations.** The rail stays as entry 93 built it.

**What to build.**

| | |
|---|---|
| **The split** | The marking and review controls are the editor state. The figures, the plot and the judgements are the analysis state. One window, one document, two states |
| **Forward** | **Accept and analyse**, amber primary, top right of the editor |
| **Back** | The breadcrumb's sheet crumb returns to the editor with every edit intact. A person who sees a figure they distrust must be able to go and look at the mark behind it in one click |
| **Discard edits** | Beside it, and it asks before discarding |
| **The pill** | Review count in the editor, registration and its residual in the analysis, as the two concepts show |

**One rule the concept does not show and the screen needs.** A person may accept with items still open. When they do, **the analysis state carries an amber line naming how many decisions were left unmade**, because every figure below it inherits them. A figure whose inputs were never settled must not be presented as though they were. Test it.

**The composite plot.** This is the centre of the analysis state and the largest thing in the concept that does not exist.

- **One bull's artwork drawn from the definition**, centred on the aim point, with every scoring shot's offset from **its own** bull's aim point plotted on it.
- **Sighter bulls are not in it, and a test asserts that.** The sighter pooling defect was precisely this mistake one layer up, and the plot is a second place it can be made.
- **Each shot is a circle at the calibre's diameter when a calibre is set, and a point when it is not**, with the legend saying which. This follows the rule edge-to-edge extreme spread already uses: print the reason in place of the thing rather than a plausible substitute.
- **Excluded shots are drawn hollow and dim, not removed.** Excluding a shot is a human judgement, and a plot that hides it makes the group look better than the evidence does. Marks set to not-a-shot are absent, because they are not shots. Test both.
- **The group centre is marked**, and **CEP 50 and CEP 90 are dashed circles about it**.
- **Extreme spread is drawn as the line between the two shots that produce it, not as a circle.** This is a deliberate departure from the concept and the reason is that a circle of that diameter reads as a containment region, which extreme spread is not: it is the distance between two particular shots. Drawing those two shots joined is the honest depiction, and clicking the line should select them both. If the concept's circle is wanted anyway, say so and it goes back, but the reason belongs in the record either way.
- **Framing.** The view frames the shots with a margin and draws the bull's rings behind them at true relative scale, running off the frame when the group is much smaller than the bull. A group of 0.10 in sigma scaled to fit a one inch ring is a dot, and a dot tells the reader nothing.
- **A shot clicked on the plot selects it**, the same selection the shot list drives.
- **A legend**, as the concept has one.

**Two figures the stack is missing**, both already in the engine: **CEP**, with 90 as the row's figure and 50 and 95 beneath it, from `GroupStatistics.Cep`, and **group width by height** with the per-axis standard deviations.

**Do not reorder the existing figures to match the concept.** The concept leads with Rayleigh sigma; the screen leads with mean radius at `Tokens.LeadFigureSize`, which came from entries 73 and 92 and is written down. The concept was drawn before those entries. Add the two missing rows and leave the order alone.

### 2. The two judgement cards, and two errors in the concept that must not be inherited

The README's caption promises "two plain-language judgements: whether the group is round, and whether that one wide shot is really a flyer". Both judgements exist in the engine and both are currently prose lines inside the **More figures** expander. The concept puts them in the main column as cards, and that is right: they are the two lines a shooter actually reads.

**Promote both.** A card is a bold verdict sentence and then its evidence, and the verdict never appears without the evidence in the same card.

**Error one in the concept: it names the wrong test.** The card reads "Circular within tolerance. Pitman-Morgan p = 0.476, so there is no evidence of vertical stringing in these 25 shots." Pitman-Morgan is not the circularity test. `docs/STATISTICS.md` section 7 separates them and says why:

> **Question A, is the group circular?** `H0: Σ = σ²I`. This is rotation-invariant, so it also rejects a group elongated diagonally.

> **Question B, is the group stringing vertically?** `H0: σ_x = σ_y` in **target coordinates**, with the correlation left free. A group tilted 45 degrees is non-circular but is not stringing, and a shooter told "your group is not circular" when the elongation is diagonal has been told something true and useless.

and then, in the same section:

> Use the **Bartlett-corrected likelihood-ratio test for circularity at `n ≥ 20`**, and a parametric bootstrap calibration below that. Use **Pitman-Morgan for vertical stringing at any `n`**. Label them differently in the interface, because they answer different questions.

`ShapeTests.Circularity` and `ShapeTests.VerticalStringing` are both built and they are different methods. **The card takes the circularity verdict and names the circularity test.** If the stringing answer is wanted as well it is a second card or a second line, labelled as stringing. The concept's wording is the exact conflation the document warns against, and it reached a screenshot, which is how it would reach the product.

**Error two in the concept: the stringing card states a negative result without its power, which the document forbids.** Same section:

> A 25-shot group has roughly 49 percent power against 1.5 times stringing. **Half the time, a group that really is stringing by fifty percent will not be flagged.** That belongs in the interface next to the result, not in a footnote, because "no significant stringing detected" from 25 shots means very little and users will read it as meaning a lot.

So **any card reporting no evidence of stringing carries, in the card, what the group's shot count could have detected.** The table in section 7 gives 155 shots for 1.25 times, 50 for 1.50 and 19 for 2.00; interpolate or state the nearest honest sentence, but do not print a bare negative.

**The flyer card keeps its hedge.** The concept's headline is "Shot 22 is not a flyer." The line in `MainWindow` today ends "so a shot there is not a flyer **by that measure alone**". The headline may be short, but that qualification survives into the card's body. A test should assert each card names the test behind it.

### 3. Question 15 answered: commit the sort, and this is the cheap moment to do it

`docs/QUESTIONS-FOR-PLANNING.md` question 15 asks whether to commit `MarkerDetection.InIdentifierOrder()` when doing so changes every printed table on Windows and no gate verdict. **Option A. Commit the sort and regenerate everything it touches.**

**Three reasons, one of which is new since the question was written.**

1. **No verdict changes**, measured on Windows, and a record that follows one stated order is worth more than figures that came out of an arbitrary one.
2. **The regeneration path is warm and the records are all currently consistent.** Entry 101 regenerated every record a command writes, the eight committed Windows tables and the documents that print them, four days after the question was raised. The afternoon that option A costs is mostly that sweep, and the sweep has just been rehearsed. Deferring it means running it twice.
3. **The sort makes the three-platform gate record a cleaner instrument.** An order dependence is exactly the kind of thing that differs between two platforms for reasons that have nothing to do with arithmetic, and the record now passes on all three, so this is the moment to remove a confound rather than add one.

**What the commit must carry.**
- **The mounted baseline moves and it is quoted as a benchmark.** `docs/PHASE0-RESULTS.md` section 4.5 states "the figures to beat: worst scoring bull 0.015 to 0.091 in, 8 to 21 of 25 scoring bulls over the gate", and `ultrawide3.jpg` goes from 21 to 18. Amend section 4.5 in the same commit, with dated before and after figures, and put the new figure beside the old wherever Phase 1 surface work reports against the old one.
- **Dated before and after, verdicts shown unchanged**, as the question proposes.
- **Its own commit**, separate from section 1's screen work. One changes evidence and the other changes the interface, and a reader six months from now should not have to separate them.

**Option C is the real question and it is not scheduled.** Replacing RANSAC's random consensus with a deterministic robust fit answers the fragility rather than one ordering of it, and entry 101 left RANSAC's inlier choice native, so nothing in that work touched this. It is not scheduled now for two reasons: it moves the figures again, and the case where the instability bit hardest is the mounted one, whose gate has no proven material yet. **Record it as a named risk in `DESIGN.md` section 22**, pointing at question 15, so the decision is deferred in writing rather than forgotten. Revisit it when the mounted gate has real material, which is the weekend session at the earliest.

### 4. Question 18 answered: what "assisted" means, where blank paper goes, and that bought targets and the designer are one item

Four answers, to the three things section 6 of the question asks to settle plus the option it asks to refuse.

**1. "Assisted" means the snap, and section 3 should say so.** Option A. `Snapping.ToHole` takes artwork as an optional argument and snaps to the dark centroid within a calibre-sized radius when there is none. That is assistance, it works with no definition, and it is built. Make `DESIGN.md` section 3's bullet say what it means, in one sentence, and give the README a feature line with a state. A promise that is already kept and never defined is the worst state for a promise to be in.

**2. Blank-paper detection is worth a phase and the phase is 4.** Not 5. It belongs in the desktop application where the secondary mode already lives, and Phase 5 is chronograph and solver work with nothing to do with it. It starts **Not started**, and **its gate names the material it needs**: one photograph, at a known scale, of a sheet of plain paper with real holes in it. No such image is in the corpus. Naming the material in the gate is what stops it being marked done without it, which is how the mounted gate is already handled. It goes on the range list and it blocks nothing.

**3. A user-traced definition is the intended route to the full detector on bought targets, and it is the same item as the deferred designer.** Say so in both places: the deferral entry gains the sentence, and the bullet points at it. **The consequence is worth writing down explicitly: the designer's deferral now carries two promises rather than one.** A deferral carrying two promises is a different object from one carrying one, and the moment anybody asks for either, both arrive together.

**4. Option C is refused for now, and option D is refused outright.** Unmodelled artwork in the detector is the direct route to the false positives gate G2 exists to forbid, and there is no corpus material to measure it on. Refused until somebody asks, and then as research with a gate of its own rather than as a feature. D is refused because nothing about the promise turned out to be wrong; the word was simply never defined, and section 3's first answer defines it.

Set question 18's status to answered, pointing at this entry.

### 5. Two claims in the README that are no longer true

Both have survived several commits that touched the file, which is how a public page ends up describing a build that no longer exists.

**Claim one: macOS does not reproduce the Phase 0 record.** The Platforms table's macOS row reads "not yet", and the paragraph under it reads "macOS differs on a small number of measurement rows, traced to corner refinement inside the native imaging library and to one further divergence below it."

**That was fixed by entry 101 and confirmed twice.** The `phase 0 gate record` workflow succeeded on commit `39facee` and again on `60fffc6`, and on the second run every job succeeded: `windows-latest`, `ubuntu-latest`, `macos-latest`, `windows corners`, and `macos-latest, from windows corners`. Under the workflow's own definition in its header, the record reproduces on a platform when every gate verdict and every printed table is identical, so macOS reproduces it.

- **The row becomes yes.**
- **The paragraph is rewritten.** What stands between Linux and macOS and a download is no longer the record. Say what it actually is, which is packaging and the fact that nobody uses either day to day, and keep the distinction the workflow makes: the printed tables are gated and identical, and the raw records are compared and reported rather than gated, so differences below printed precision may remain and are not failures.

**Claim two: there is no navigation rail.** The Concept screens paragraph says "there is no navigation rail, no composite plot and no analysis screen yet." **The rail was built in `5dad2e2` and has a test**, `TheRailIsBuiltAndItsOtherDestinationsAreNot`. Rewrite the sentence to say what is true now: the rail exists with one destination built and four naming the phase that builds them, and what is absent is the composite plot and the analysis screen. Section 1 changes that sentence again, so make this edit in the same commit as section 1 rather than separately.

**And a small guard, if it can be made cheaply.** `ReadmeTests` already fails when a phase and its state disagree between the README and `DESIGN.md` section 21, and prose drifted anyway for three commits. A cheap version of the check: every feature the Concept screens paragraph names as absent must appear in the Planned section with a state that is not **Done**. That catches this exact class. **If it turns out awkward or brittle, say so and skip it rather than building something elaborate to enforce one sentence.**
