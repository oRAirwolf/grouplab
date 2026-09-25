## 2026-09-25, entry 205: request 27 done: the fold test passed, and upside down portrait does not rotate

Alan ran request 27 on the Fold 7 on 2026-09-24 at about 22:40 local time and sent two screenshots of the spike, one on the cover screen
and one on the inner screen. They are not committed; what they show is below.

## 1. What passed

- **Folding, unfolding and turning kept the app.** The Screen list keeps every earlier line through each change: compact 411 by 960 dp at
  2.625 pixels a dp on the cover screen (1080 by 2520), medium 750 by 832 dp unfolded (1968 by 2184), 832 by 750 dp turned, and back.
- **The layout rearranges as designed:** panels stacked on the cover screen, side by side unfolded.
- **Largest system font size:** Alan reports nothing cut off.
- **Detection on the phone:** the 600 dpi sample, load 537 ms, codes and naming 1504 ms, marking 16880 ms, 25 holes, total 18922 ms, peak
  memory 716 MB. The range photograph `20260920_141404.jpg`: 0 of 34 markers found, as on the desktop, peak memory 900 MB (say whether that
  figure is that image's peak or the process's peak so far; if the process's, report each image's own).

Close request 27, record these in `docs/ANDROID.md` sections 5 and 7, and treat Avalonia as confirmed for the phone unless something below
changes that.

## 2. Upside down portrait does not rotate

Alan: "the application did not turn when the phone is rotated 180 degrees so the USB C port is at the top." Android leaves reverse portrait
out unless the activity asks for it. The inner screen of a foldable is close to square and is picked up either way round, and a phone on a
bench or a tripod mount is often upside down, so all four orientations must work.

1. Set the activity's orientation to follow the sensor in all four directions while still honoring the user's rotation lock
   (`ScreenOrientation.FullUser`, rather than `FullSensor`, which ignores the lock). Say which you chose and why.
2. Check the same on the tablet when it is next to hand; not blocking.
3. **Carry this into the capture screen's design.** The photograph must be stored the right way up whichever of the four ways the phone is
   held, and the capture overlays (outline, guidance) must turn with it. Add it to `docs/MOBILE-CAPTURE.md` as a requirement with its test.

## 3. Two things the Screen list shows

1. **The start up sequence appears twice.** At 22:40:46 the list begins `1 by 1 dp, 1 pixels a dp`, then `412 by 960 dp, 1 pixels a dp`,
   then the real size. The same three lines appear again at 22:42:40, without Alan closing the app (he took screenshots around then). The
   earlier lines survived, so either the view was rebuilt inside the same activity, or the activity was recreated and the list lives in
   something static that hid it. Log the activity's own lifecycle (create, destroy, and a count) so the report can say which. If the activity
   is being recreated, the real app will lose state unless it is designed for that; say what the design is.
2. **Transient sizes.** `0 by 0 dp`, `1 by 1 dp` and `1 pixels a dp` appear during start and during a fold. The real layout must ignore
   degenerate sizes and never lay itself out, even for a frame, as compact at the wrong density.

Report in plain words for Alan: whether upside down now works, and what the start up lines meant.
