## 2026-09-19, entry 113: the queue after entry 112, for a long unattended run

**Status: open.**

**Alan is asleep and then at the range.** Nobody will answer a question for about twelve hours. **Do entry 112 completely first, then this entry, in the order of its sections.** The rule for the whole run: when a design question comes up that neither entry answers, write it in `docs/QUESTIONS-FOR-PLANNING.md`, build everything that does not depend on the answer, and move to the next section. **Do not stop the run for a question.** Commit and push at every clean point, so a stop at any hour leaves main green and nothing half-built.

### 1. The analysis screen's three unbuilt parts

The README lists them as not built: the sheet's thumbnail, and the full CEP table and bivariate fit behind a link.
- **The thumbnail** is the concept's top-left panel: the sheet drawn small from its definition, every shot on it, and a click on a bull selects its shot. It is drawn from the definition, not the photograph, as `DESIGN.md` section 18's archived view is.
- **The full CEP table and bivariate fit** go behind the link `DESIGN.md` section 19 describes, "one click away in a panel that remembers it was opened". Everything in it already exists in the engine. It is layout, not new statistics.
- **Regenerate the renders**, and move the README line to Done if nothing on it remains.

### 2. Comparing loads

`docs/figures/screens/compare-loads.png` is the concept. `GroupComparison` already holds the rank and dispersion tests, MANOVA and the dispersion ratio with its interval. The README marks load comparison as built in the engine.
- **A comparison screen over sessions or subgroups.** Pick two or more from the Session records list, or the subgroups of one sheet, and see them side by side. Show the composite plots, the headline figures with intervals, the tests, and each test's verdict.
- **Every negative result carries what it could have detected**, as the stringing card does. "No significant difference" between two ten-shot groups means very little, and `docs/STATISTICS.md` gives the power figures.
- **Never rank loads by point estimate alone.** When the intervals overlap, the screen says the data do not separate them. That sentence matters more than any chart on the screen.

### 3. Hit probability at distance, and distance normalisation

`DESIGN.md` section 3 promises both "propagated through the solver rather than by scaling a group linearly". Entry 112 section 4 deferred them for want of a specification. **This is it.**

**Inputs:**
- the session's sigma and its interval, from the engine, at the distance shot, d₀;
- the rifle and load's solver fields;
- **the muzzle velocity standard deviation**, a new optional field on the load;
- **the crosswind uncertainty in mph**, optional, entered at the time;
- the target's size and shape, a circle or a rectangle;
- the distance d.

**Model.** At distance d, shots are bivariate normal about the aim point plus the zero offset carried to d, from entry 112 section 4.
- σ_x(d)² = (σ_x,ang × d)² + ((∂drift/∂wind) × σ_wind)²
- σ_y(d)² = (σ_y,ang × d)² + ((∂drop/∂V) × σ_V)²
- **The partial derivatives come from the solver** by finite difference, at d.
- **The measured angular sigma already contains the velocity contribution at d₀.** When σ_V is given, subtract that contribution in quadrature before propagating. If the subtraction goes negative, the entered σ_V is too large for the group measured: refuse, and say that.
- **With neither σ_V nor σ_wind given, the result reduces exactly to angular scaling, and the screen says that is all it is.**

**Output:**
- **P(hit) at each end of the sigma interval, as well as at the point estimate.** It is a range, not a single number.
- A circle uses the engine's existing estimators. A rectangle uses the product of normal distributions when its axes align with the dispersion, and otherwise numerical integration.
- **Distance normalisation** shows a group as its solver-propagated equivalent at another distance. It is labelled as a prediction, never as a measurement.

**Tests:**
- with σ_V and σ_wind zero, the result equals angular scaling exactly;
- with σ_V positive, vertical sigma grows faster than linearly with distance;
- a centred circle with equal axes matches the Rayleigh closed form;
- the quadrature subtraction refuses when it should.

### 4. Tooling for the range material, without reading it

Entry 111 section 4 built `analyze-folder` and `timing`. Two things are still missing before Alan's material can be used, and both can be built against committed test data.

**A photograph-against-scan comparison.** Tomorrow gives, for each sheet, a flat 600 dpi scan and four photographs of the same sheet as it hung. **The scan is the truth for the photographs**: same holes, measured flat.
- **A command** that takes a sheet's scan and its photographs.
- It uses the scan's marking as truth once the person has corrected it, or the scan's detection otherwise, and says which.
- For each photograph it reports: the registration model used; bull-centre error against the scan, worst and median; holes found, missed and false against the scan's holes; and the hole-position error, median, 95th percentile and worst.
- **It reports against the gates' thresholds, 0.005 in for bulls and 0.15 in for hole matching, and does not decide the gate.** Planning reads the table.
- Test it on committed images, a synthetic photograph of a committed scan if nothing better exists.

**The doubles sheet breaks one shot per bull on purpose.** Two shots go into each of bulls 1 to 10 and none into 11 to 25. One-to-one matching will push the second shot of a bull onto an empty neighbour.
- **Make sure the way to say so exists**, so that a sheet can be marked as expecting two shots on named bulls, or analysed by nearest bull.
- **Confirm the review queue raises the doubled bulls** when it is analysed without that setting. That is the queue's job, and this sheet tests it.
- Build the setting if it is missing, and test both paths on a synthetic sheet.

**The blank sheet stays Not started.** Nothing for it yet.

### 5. The volunteer print pack

**The README's Phase 4 line, Not started.** A person who wants to contribute a target needs the sheet and one page of instructions.
- **A one-page instruction PDF generated by GroupLab**, from a Markdown source in the repository, alongside the sheet. Its content:
  - print at actual size through Print, and measure bull 1 to bull 5;
  - mount flat;
  - one shot per bull, in order;
  - write only in the load block;
  - the four photographs at about 2.5 ft on the main camera;
  - no cropping or messaging apps;
  - a flat 600 dpi scan if they have a scanner;
  - how to submit at `https://pissinhot.com/targets`.
- **Consent is not in the pack.** The upload page collects consent. The pack says that submitting means following that page's terms.
- **The print screen offers "Print a volunteer pack"**, the sheet and the page together.

### 6. A user guide

**`docs/USER-GUIDE.md`, and a PDF of it**, the project's rule for documents meant for reading. It walks through:
- printing a sheet;
- shooting it;
- photographing or scanning it;
- marking it and settling the review queue;
- reading the analysis screen, including what each "why" says in plain words;
- sessions and the report;
- comparing loads;
- the zero correction and the dope table.

**Illustrate it with the committed renders** under `docs/figures/screens/current/`, and describe only what the build actually does. **A test that every screenshot the guide references exists** keeps the two from drifting. No pseudoscience anywhere in it, as everywhere.

### 7. Housekeeping on everything built in entries 112 and 113

- **Keyboard:** every new screen fully usable without the mouse, as the marking screen is.
- **Themes:** every new screen passes the contrast tests in all four themes.
- **Enum names:** extend the test from entry 111 to every new screen.
- **Renders:** every new screen photographed in dark and light at both sizes, committed under `docs/figures/screens/current/`.

### 8. When the run ends

Whatever is done, record it in `docs/PHASE1-RESULTS.md` and fold both entries into the notes log with status lines that name every section not done. Delete an inbox file only when its entry is complete. Leave a short summary at the top of your final report: what was built, which questions were raised, and what CI says on the final commit.
