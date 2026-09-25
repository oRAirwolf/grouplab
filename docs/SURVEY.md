# The hardware and benchmark survey

NOTES-FROM-PLANNING.md entries 207 section 3 and 208, 2026-09-25. A design, not yet built: the desktop part is built with the next desktop
work, and the Android part with the real application. Like Steam's hardware survey, it tells the project what GroupLab actually runs on,
so the minimums in `docs/PLATFORM-SUPPORT.md` rest on reports rather than guesses, and it tells each person how their own machine did.

## 1. Asking

**One first run screen, three choices** (entry 208): sending targets, error reports, and the hardware survey with its benchmark, one after
another on the window that asks today, each with its own plain description of what is sent and its own answer. Saying yes to one never
turns on another. **Nothing is chosen for the person** on any of the three, the rule entry 203 section 3 set and `Entry203Tests` holds. A
long window scrolls; no choice hides behind a "more" link.

**Under the survey choice, one line offers the benchmark**: it can be run now, from a button beside it, or later from Settings. It never
starts by itself.

**Settings mirrors the window** in one section, **Sharing**, with the three in the same order and words. The Sending targets and Error
reports sections move into it; nothing else about them changes.

**People who answered before**: after the update that adds the survey, the same window opens once more with their two earlier answers
kept and only the survey unanswered. No separate prompt.

On Android the three appear together on one screen of the first run flow.

## 2. What is sent, and nothing more

- The operating system and its version; the CPU model, architecture and core count; total memory; the GPU's name where it matters; the
  screen's size and scale.
- On a phone, the device model and the rear camera's resolution.
- GroupLab's version.
- For each analysis: the image's size, the working resolution, each stage's time and the peak memory.
- A random installation id, so one machine is counted once, which the person can reset in Settings at any time.

**Never**: a name, an account, a file name or path, a photograph, a location, a device serial or advertising identifier. The server does
not store the sender's address. The consent wording on the window lists exactly the items above.

## 3. The benchmark

A fixed test built into the application, timed stage by stage: the published sample scan where it is installed, and otherwise a synthetic
sheet rendered from a built-in definition, so every copy runs the same work. The desktop's reference today is `SpikeRun` in the Android
spike: the sample at 600 dpi takes 7.9 s on Alan's desktop and 17 s on the Fold 7. The person sees their own result beside the median of
machines like theirs once there are enough reports.

## 4. Transport

The route error reports take: posted to grouplab.org, checked against a schema, rate limited, stored on the server, queued while offline.
It never goes to the GitHub issues repository. The server side is a receiver and a worker like the error reports', so Alan gets one
install request for it when it is built, in one sitting with any other pending server work.

## 5. Publication

An aggregate page on grouplab.org: shares of operating systems and versions, memory, CPU classes and phone models, and benchmark times by
class, with the date range and the number of reports. Never an individual record. Any group smaller than 10 reports is merged into
"other", so no one machine can be picked out.

**How long a report is kept** (entries 215 and 216): on the server, only until the worker has counted it into the aggregate, and never
longer than thirty days whatever happens; the aggregate keeps counts, not records. Like everything else people send, the rule is written
where the site says what happens to what people send, the article `what-grouplab-sends`, when the survey is built.

## 6. Use

Once there are 200 reports from one platform, the minimums in `docs/PLATFORM-SUPPORT.md` are reviewed against it, and `docs/notes/STATE.md`
says when that happened.
