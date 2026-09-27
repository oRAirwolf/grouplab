# The hardware and benchmark survey

NOTES-FROM-PLANNING.md entries 207 section 3 and 208, 2026-09-25. **Built, and open since 2026-09-25**
(entry 219 item D1, opened by entry 223 once Alan had installed the worker): `surveyOpen` in `website/api/limits.json` is true, and the
receiver takes reports. The desktop and the Android application ask the question on the first run screen. Like Steam's hardware survey, it tells the project what GroupLab actually runs on,
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
- A random installation number, so one machine is counted once, which the person can reset in Settings at any time (section 7).

**Never**: a name, an account, a file name or path, a photograph, a location, a device serial or advertising identifier. The server does
not store the sender's address. The consent wording on the window lists exactly the items above.

## 3. The benchmark

A fixed test built into the application, timed stage by stage: the published sample scan where it is installed, and otherwise a synthetic
sheet rendered from a built-in definition, so every copy runs the same work. The desktop's reference today is `SpikeRun` in the Android
spike: the sample at 600 dpi takes 7.9 s on Alan's desktop and 17 s on the Fold 7. The person sees their own result beside the median of
machines like theirs once there are enough reports.

**As built.** `GroupLab.Core.Survey.Benchmark`: GL-CF25-LTR rendered at 300 dpi with one hole in each of its 25 bulls from a fixed
seed, analyzed as a photograph is, only the analysis timed. `SurveyReport.Keys` is every name a report may hold and
`SurveyReport.WhatIsSent` is what the question says, in the same order.

**Every run is sent, and the median counts** (entry 241 section 1). Each run is kept on the device with the date, the version that ran it
and its time, and Settings, under Sharing, lists them all. Every run made while the survey is on goes with the next report, each with its
own version (`grouplab-survey-2`); a run the receiver's three reports a day held back goes with the next one. In the published figures a
machine counts **once per version of GroupLab, by the median of its runs on that version**: not the best run, which is the same
cherry-picking as quoting a shooter's best group and would make every machine look faster than it is, and not the mean, which one run
slowed by something else in the background would move. The page says how many runs the medians rest on.

## 4. Transport

The route error reports take: posted to grouplab.org, checked against a schema, rate limited, stored on the server, queued while offline.
It never goes to the GitHub issues repository. **As built**: `website/api/survey.php` takes the named fields only, stores the
installation number only as a keyed hash (HMAC-SHA256, with a key made on the server and kept only there, entry 241 section 2.2) and the
day, never the time or the address, and limits each installation to three reports a day. Until entry 241 this section said the hash was
of the number and the day; it never was, the code hashed the number alone, with the salt the rate limit uses. It
refuses everything while `surveyOpen` is false, which the site's build holds it to, because the server routes it already;
`website/server/grouplab-survey-worker.py`, with no network, counts each report and deletes it, hourly. The application sends at most
once a week, or sooner when a benchmark is waiting. `install.py --survey` installs the worker, in the same sitting as request 31.

## 5. Publication

An aggregate page on grouplab.org: shares of operating systems and versions, memory, CPU classes and phone models, and benchmark times by
class, with the date range and the number of reports. Never an individual record. **As built** (entry 241 section 5), at
[grouplab.org/survey/](https://grouplab.org/survey/): the worker writes the aggregate to `private/survey/public.json` and a copy to
`public_html/survey/aggregate.json`, which the site sync leaves in place and the page reads. Any group smaller than 10 machines is merged
into "other" or not shown, so no one machine can be picked out. **The project's own test devices** are a section of their own, by name
with Alan's permission because they are his: their numbers are what each device showed on its own screen, from
`website/survey-devices.json`, because the server keeps no individual record to take them from.

**What the server keeps** (entry 241 section 3): for each machine's keyed hash, the hardware classes, the month of its latest report, and
for each version and workload the run count and how the times fall in quarter seconds (each stage in twentieths of a second), from
which its median is read. No time of day, no address, and no individual run once it is counted. A machine with no report for twelve
months is deleted; the thirty day limit on raw reports stays. The public figures are computed from the machines' medians, never from
runs. The worker's first version kept machines under the old hash; on the first run of the second it sets that state aside and counts
again, so the figures started over on the day entry 241 was installed.

**How long a report is kept** (entries 215 and 216): on the server, only until the worker has counted it into the aggregate, and never
longer than thirty days whatever happens; the aggregate keeps counts, not records. Like everything else people send, the rule is written
where the site says what happens to what people send, the article `what-grouplab-sends`, when the survey is built.

## 7. The installation number, and why it is not a fingerprint

Entry 241 section 2. Counting a machine once needs a number that stays the same. It is **a random number GroupLab makes the first time
it is needed**, from nothing about the hardware, the account, the phone number or the network, kept in the application's own settings.

- **An update keeps it**, because the settings are kept. **An uninstall on Android makes a new one** on reinstalling, because Android
  removes the application's settings with it. **On Windows the uninstaller leaves `%APPDATA%\GroupLab` alone** (`packaging/windows/grouplab.iss`),
  so the number survives an uninstall there, and that is left as it falls. It does not try to survive anything more: that would mean
  fingerprinting the hardware, which is exactly what would break trust. A reinstalled machine counting twice barely moves a median.
- **Reset my survey number**, in Settings under Sharing, makes a new one; runs already sent stay counted under the old.
- **Delete my survey reports** sends the number with a delete request; the worker removes everything kept under it at its next hourly
  run and the aggregate is counted again without it. A stable number is what makes a real delete possible.
- **The question says it before anything is sent**: "a random number made by GroupLab for this installation, so that repeated runs count
  once; it is not tied to your device, account or network, and you can reset it or delete your reports in Settings." Everyone who said
  yes to the earlier wording is asked again, once, and nothing is sent until they answer (`SurveyReport.WordingVersion`).

## 8. How Steam does it

Valve's own page says the Steam Hardware & Software Survey is monthly, optional and anonymous
(https://store.steampowered.com/hwsurvey/En). In practice a random sample of users is asked each month, and a user cannot choose to take
it; it reports shares of hardware and software, not benchmarks. GroupLab differs on purpose: it asks once and remembers the answer, lets
people run the benchmark again when they like, and publishes speed, which Steam does not.

## 6. Use

Once there are 200 reports from one platform, the minimums in `docs/PLATFORM-SUPPORT.md` are reviewed against it, and `docs/notes/STATE.md`
says when that happened.
