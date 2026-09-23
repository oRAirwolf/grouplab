# 2026-09-23, entry 158: two research programs, and how to decide what is worth an article

Alan: "When more research is performed by claude, it should be evaluated whether the conclusions from
that research are significant and worth writing a research article about. The goal is to advance the
science of shooting, so anything that can help other shooters or developers is worth writing about."

That is a standing rule, and it comes with two specific programs he has the material for.

## 1. The standing rule

After any piece of research, measurement or investigation, decide whether it is worth an article, and
record the decision either way.

**The test:** would this change what another shooter does, or what another developer builds? If yes,
write it. Being a negative result is not a reason to skip it. "We measured this and it turned out not
to matter" is one of the more useful things a shooter can read, because the belief it corrects is
usually costing somebody ammunition.

**Record it.** `docs/RESEARCH.md` gains a short list of investigations considered and not written, each
with one line saying why, so a rejected idea is not reconsidered from scratch every time somebody reads
the results file. The two reasons that should appear most often are "the data cannot separate the
effect from the confounds" and "already covered by article N".

Three things are already sitting in the results files that may pass this test, and should be assessed
under it: question 38's finding that a photographed hole has no size constant, question 44's held out
marker result showing the bent sheet model predicts an unseen marker as well as a fitted one, and
entry 130's photograph against scan agreement. Say for each whether it is an article, and if not, why.

## 2. Program A: the traditional 5 shot and 10 shot groups

Alan: "For the 100 yard precision rifle target pictures, most of the groups were 5 shots with a 6.5
Creedmoor. Some were 10 shots. I think this is worth studying and drawing conclusions from since they
are groups that were shot in a traditional method where a single point of aim was used for 5 or 10
shots with often overlapping holes. I can answer questions about the photos, point out where the point
of aim was, describe the scaling on the target, and verify how many shots were shot at each group.
These are very good examples of traditional groups."

This is the most valuable material in the project, for two reasons. Overlapping holes at a single point
of aim are the hardest case for automatic detection and the most common case in real life. And the
5 shot group is what nearly every shooter uses and nearly every shooter over-reads.

**Step 1, and the answers are already being collected.** The twenty six owner photographs in
`C:\\Dev\\grouplab-testdata\\owner` are the material. The planning session is collecting the shot count,
the point of aim, the scale reference, the cartridge and load, and anything unusual, one line per
photograph, and will deliver them as an inbox entry. Write the request into `docs/notes/for-alan.md`
per entry 149 section 5 and carry on; do not ask him in the panel and do not wait.

**Step 2, detection performance on overlapping holes.** With his counts as ground truth, measure misses
and false positives per group. Report it the way the scan table in question 35 is reported. This is the
first honest measurement of the case that matters most, and whatever it says is worth knowing.

**Step 3, what 5 shots tell you.** On the same load, compare 5 shot and 10 shot groups: extreme spread
growth with sample size against the expected growth, mean radius stability, and the width of the
confidence interval at each. `docs/STATISTICS.md` already carries the theory. This puts real groups
against it.

**Step 4, the article.** What a 5 shot group actually tells you, with these targets as the evidence and
the images in it, per entry 153 section 3. This is probably the single most useful article the section
will carry.

## 3. Program B: hole size against velocity and nose shape

Alan: "Some of these conclusions drawn are about supersonic bullets creating holes in paper. It should
probably be researched how the speed and shape of the bullet affect the size of the hole on the paper."

**What is in hand:**

| cartridge | muzzle velocity | nose | nominal diameter |
|---|---|---|---|
| 22 LR | about 1080 fps | round nose | 0.222 in |
| 6 ARC | about 2500 fps | spitzer | 0.243 in |
| 6.5 Creedmoor | 2845 fps | spitzer | 0.264 in |

**What could be shot later:** subsonic 22 LR, 300 Blackout, 8.6 Blackout and 510 Whisper, all at
roughly 1000 to 1080 fps. That set is the useful one, because it puts large diameter subsonic holes
beside small diameter subsonic holes and beside supersonic holes, which is what separates diameter from
velocity.

**Step 1.** From the existing scans, measure hole diameter against nominal bullet diameter for each
cartridge and report the ratio with its spread. That is a fact worth having regardless of what causes
it.

**Step 2, and this is the part that keeps it honest.** State plainly what is confounded. Paper, backing
material, distance, lighting, scanner against camera, and angle of incidence all differ between these
groups, and every one of them plausibly changes a hole. Do not claim a velocity effect that this data
cannot separate from those. Question 38 already showed that light and shadow, not resolution, drive
the measured size in a photograph, which is exactly the kind of confound at issue.

**Step 3.** Design the test that would separate them and write it out so Alan can shoot it: the same
paper, the same backing, the same distance, the same imaging, varying only the cartridge, with the
subsonic set as the key comparison. Say how many holes per cartridge are needed for the comparison to
mean anything, using the sample size work in `docs/STATISTICS.md`.

**Step 4.** Hold the article until the data supports it. An article that says "here is what we can
measure, here is what we cannot yet separate, and here is the experiment that would settle it" is
publishable and is better than a guess. If that is the article, write that article.

## 4. Where this sits in the queue

After entries 149 to 157. Step 1 of each program is the part that needs Alan, so it goes in the entry
149 section 5 list at the start of the run even though the work itself comes last.
