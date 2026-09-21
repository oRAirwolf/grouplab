# 2026-09-21, entry 126: grouplab.org is live, so the support link gets its address

Alan's website chat has put grouplab.org live. The support page is **https://grouplab.org/support/** and **support@grouplab.org** is a working address (it forwards to Alan). The site's status is in the project doc `claude/grouplab-website-status.md`; the parts that matter here are below. This answers question 28 for good.

## 1. The support link

1. Replace the placeholder from entry 120 section 9 with `https://grouplab.org/support/`, still one constant in one place, opened through `IOutsideWorld` (entry 122). The "Support GroupLab" button opens it; its note under the button changes from "There is no support address yet..." to one sentence saying it opens the support page, and keeps "GroupLab will never take a payment inside the application".
2. Where the application tells a person how to send a report package (after "Report a problem..." writes one), name both ways the support page names: open an issue on GitHub, or email the package to support@grouplab.org, with the build line from the settings page.
3. Change the test that held "no other support address anywhere in the repository" so it now holds exactly these two, `https://grouplab.org/support/` and `support@grouplab.org`, and fails on any other support address or domain. The recorder test from entry 122 section 3 now expects the support button to ask for that one address instead of nothing.
4. Close question 28.

## 2. The repository points at the site

1. The README gets one line near the top naming the website, https://grouplab.org, as the place to download and read about GroupLab. The single nightly download table stays as it is (entry 119 section 7); the site links to the same nightly addresses.
2. `docs/TESTING-GUIDE.md` and `docs/UPDATES.md` ("If a new build will not start") name the support page and the support address where they tell a tester where to report or ask.
3. Keep entry 118's contents list and `ReleaseAssetTests` in step.

## 3. What the site reads from this repository

The site is built from this repository by a separate build (`C:\Dev\grouplab-site\build\build.py`, read only here). It reads the user guide and testing guide Markdown, and the screenshots in `docs/figures/screens/current/`. So:

1. Do not rename or move `docs/figures/screens/current/`, the user guide or `docs/TESTING-GUIDE.md` without saying so in your report, because the site would silently lose them.
2. Everything in `docs/figures/screens/current/` is published on the site. It must only ever show synthetic material (the Entry109Tests sheet, generated sheets, scan 3 under its consent record). Add a test that fails if any image in that folder was rendered from anything else, however you can best prove it (for example a manifest of what each render was made from, checked against the allowed sources).
3. Never read, write or run anything in `C:\Dev\grouplab-site`. It belongs to the website chat.

## 4. Report

The support button's before and after text, the test changes, and the renders of the settings page after the change.
