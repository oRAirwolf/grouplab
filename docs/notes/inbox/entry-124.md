# 2026-09-21, entry 124: questions 32 and 33 answered, and carry on with the real update

## 1. The 403: you were right and I was wrong

Entry 123 section 1 doubted your diagnosis on the grounds that a workflow's `permissions:` block raises the token above the repository default. Your two runs on the same commit, 403 under the read default and success under write, show it does not. Thank you for recording the evidence in `docs/UPDATES.md` rather than taking my word.

## 2. Question 33, rollback: option A now, option B when anyone else installs GroupLab

Agreed with your recommendation. Keep A: `docs/UPDATES.md`'s "If a new build will not start" paragraph is the answer for the nightly train, and it must also appear in the testing guide where a tester will look for it. Do not build C. Build B, the previous install kept beside the new one with a "Roll back to <version>" shortcut, at the first of: the beta train opening, or Alan saying a second person is testing. Record that trigger in `docs/UPDATES.md` and close question 33.

## 3. Question 32, the library preview's area

Agreed: a portrait page cannot fill half a landscape window, and my criterion was wrong. The tests you hold instead (nothing truncated or overlapping, the preview at least 95 percent of the height it is given, the sheet taking all the width the list does not) are the right ones. Close question 32.

One figure in it to check: fitted to the full height of a 1280 by 720 window, a letter page should be roughly 450 to 550 pixels tall after the window's own chrome, not 240 by 340. If the preview really renders at 240 by 340 at that size, it is not filling its height and the 95 percent test should be failing; if the 240 by 340 figure is a slip in the question's text, correct the text. Say which.

## 4. Carry on with entry 123 section 2.7

`v0.2.0-nightly.12` and the rolling `nightly` release are published and the README installer link now returns the file (about 102 MB). When CI on 9db6500 is green and its nightly publishes (the first carrying the update mechanism), make the second nightly with a small follow-up commit, then do the real update on this machine exactly as entry 123 section 2.7 says, and report what you saw at each step.
