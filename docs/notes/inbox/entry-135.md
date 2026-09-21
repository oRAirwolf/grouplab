# 2026-09-21, entry 135: tonight's overnight queue, interface first, run under /loop

Alan was disappointed that last night produced little he can see. Tonight is for the interface. Alan starts you with Claude Code's `/loop` command, so if a turn ends you are woken again and continue; every wake-up receives the same short prompt, so this entry and a progress file carry the state between wake-ups.

## 0. How each wake-up works

1. **Keep a progress file**, `docs/OVERNIGHT-2026-09-21.md`: the queue below as a checklist, each item marked not started, in progress (with where you got to) or done (with the commit), and a short "next step" line at the top. Update it and commit it after every item.
2. **On every wake-up**: read the progress file, check CI and the nightly, and continue with the first unfinished item. Never start an item over that is already done.
3. **Do not end a turn while any item is unfinished and workable.** Waiting for CI or a nightly is not a reason; work on the next item meanwhile. When the whole queue is done, write the morning report (section 3) at the top of the progress file and in your final message, and tell `/loop` to stop.
4. **Rules for tonight**: no SSH or SCP, no server changes, no repository settings, no `v*` tags other than the nightly workflow's, and do not delete anything in Alan's folders. You may publish the website once at the very end if the guides or screenshots changed, following the rule in `CLAUDE.md`. Push in batches; never push while a nightly you need is running; keep main green; the gate record stays identical. Every user-visible change has a plain `Release-note:` trailer. Questions become questions with your recommendation, and you move on.

## 1. The queue, in this order

**The interface first: this is what Alan wants to see in the morning.**

1. **Entry 131 section 1 for every screen**: before and after renders at 1280 by 720 and 2560 by 1440, dark and light, checked against the section 1 checklist, and fixed where they fail. Start with the analysis page, the screen Alan uses most. Use Claude Design through `/design` if you have it; otherwise design by rendering.
2. **Entry 131 section 6.2**: the zero-offset picture.
3. **Entry 131 section 7**: the Equipment screen (rifles, barrels, loads, autocomplete), with the old "rounds or components" box gone and old records moved over.
4. **Entry 131 section 8**: the ballistics page, rebuilt with grouped inputs, the trajectory graph and the dope table.
5. **Entry 131 section 10**: Compare loads, rebuilt with charts.
6. **Question 37's control**: saying which bulls were aimed at, and where a sight change happened, on the sheet itself (click bulls, row presets), if it is not already built.
7. **Anything from entry 131 sections 2 to 9 not finished**, and a final pass over every screen against the section 1 checklist.

**Then the remaining fixes, if not already done today:**

8. Entry 134, the installer icon.
9. Entry 130 section 2b.1 (scan 1, bull 2) and the extra hole on scan 4.
10. Entry 130 section 2c (photographs against scans) and section 6b (the mounted photograph gate on today's photos).
11. Entry 130 section 6 (performance), then section 7 (guides and screenshots current).

## 2. Morning handover

Before stopping, make sure the newest nightly carries tonight's interface work, so Alan's installed GroupLab (nightly 31 or later, which updates itself) offers it as an update in the morning.

## 3. The morning report

At the top of the progress file and in your last message, under the `CLAUDE.md` status rules: first what Alan must do (ideally only: open GroupLab and accept the update); then, screen by screen, what changed, with the paths of the before and after renders; then every other item, done or not and why; then open questions with your recommendations.
