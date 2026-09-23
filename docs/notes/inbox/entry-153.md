# 2026-09-23, entry 153: the standard every research article is held to

Alan, on the research section: three changes and one correction. The changes apply to every article,
published and unpublished, and to the twelve drafts that have not gone out yet.

## 1. The developer is not named

"For all of the research documents, I would prefer if my name is not mentioned. Just say the author or
developer."

1. Remove Alan Hayes's name from every research article, from the research index, and from the figure
   captions and data credits under `website/research/`.
2. The byline becomes: **"GroupLab project. Researched and written with Claude. Testing and data
   collection by the developer."** Use that form everywhere rather than inventing a variant per page.
3. Inside the body of an article, the person who shot the targets is "the developer". Not "I", not
   "he", not a name. Same convention as `docs/PLATFORM-SUPPORT.md`, which entry 147 already settled.
4. A test that fails if the developer's name appears anywhere under `website/research/`. Scope it to
   that directory: his name legitimately appears in the licence and in the commit history, and a test
   that bans it everywhere would be wrong and would eventually be disabled.
5. Do not rewrite provenance or consent records. `samples/PROVENANCE.md` carries his words under a
   consent record and it stays exactly as it is.

## 2. Explain what the numbers mean

"In the research articles, they should explain how to interpret the numbers or what they mean, rather
than just presenting the numbers."

1. Every article gains a section, after its results, titled **"What this means"**. It says what a
   reader should do differently, or stop believing, because of what the article measured. Not a
   restatement of the numbers in words.
2. Every statistic an article reports gets one sentence in plain words saying how to read it, at the
   place it is first reported. A confidence interval says what the interval is a claim about. A sigma
   says what a larger one would look like on paper.
3. Where an article reports a number that sounds decisive and is not, it says so. That is already the
   habit of the primer article and it is the right one.
4. A build check: an article with no "What this means" section fails the build. A check that it is not
   empty, and that it is not the same text as the abstract.

## 3. Show the evidence, do not only describe it

"These type of evidence examples should be considered for every article published on here. Show
examples, graphics, and measurements where applicable. Being able to visualize something is much easier
than just reading about it."

1. Every article carries at least one figure showing the actual thing it is about: a crop of a real
   scan or photograph, a diagram of the geometry, or a chart of the measured data. A chart of simulated
   data is not a substitute where real material exists.
2. Where a measurement is being discussed, the measurement is **drawn on the image**: the caliper line,
   the diameter, the scale bar, the reference mark. A reader should be able to see where the number
   came from.
3. Every figure has a caption that says what to look at, not what the figure is. "The shadow on the
   lower left edge is what makes this hole measure 0.31 in" is a caption. "Hole from scan 4" is not.
4. Where an article genuinely has no image to show, say so in one line rather than leaving a gap, and
   the plain titled panel from entry 143 section 1.1 is the lead image.
5. A build check: a figure with no caption fails. An article with no figure fails unless it carries an
   explicit exemption line naming why.

## 4. The rimfire diameter is 0.222, not 0.224

`website/research/photo-hole-size/`: "the common size referenced for 22LR is .222, not .224. Normally,
.224 is for centerfire 22 cartridges like 5.56x45 and 22 ARC."

1. Correct it in that article.
2. **Sweep the whole repository**, not only that article: the other articles, `docs/`, the calibre
   lists, the test fixtures and any default or example that assumes a 22 rimfire is 0.224.
3. Where a measured result was derived from rimfire data using 0.224, the derivation changes by 0.9
   percent and the result must be recomputed rather than edited. **`AutomaticMarking.HoleToCalibre`,
   currently 0.945, is the first thing to check.** Say in the report whether it moves, and if it does
   not, say why not.
4. Question 40's arithmetic in `docs/QUESTIONS-FOR-PLANNING.md` uses 0.224 against 0.308 for the
   two-calibre case. That one is correct as written, because it is about centrefire. Leave it and add
   a note saying so, or the next sweep will "fix" it.
5. A test holding the calibre table's rimfire entries to their real diameters, so this cannot drift
   back.

## 5. The photo hole size article, specifically

Add example images: at least three crops of real holes with their measurements overlaid.

- One clean hole in good light.
- One hole with a shadow on one edge, which is the whole finding of question 38 and currently exists
  only as a number.
- One torn, keyholed or overlapping pair, if the material has one.

Each crop carries its scale, its measured diameter and the nominal bullet diameter, so the reader sees
the gap the article is about rather than reading that it exists.

## 6. Apply it, in batches

Apply sections 1 to 3 to the eighteen published articles in batches, in the same rhythm entry 142 used.
Do not rewrite eighteen articles in one commit. The twelve drafts get the standard before they are
offered for review, not after.
