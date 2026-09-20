## 2026-09-20, entry 114: the in-app print path drops every rectangle, and a hole that is not detected

**Status: open.** **Do these before anything else.** Alan is going back to the range today, and the first one cost him most of yesterday's trip.

### 1. Every rectangle is missing from sheets printed through the application

**Alan photographed a sheet that the in-app Windows print path produced, and it is worse than "the QR codes are missing".** Comparing that photograph with the scan of a sheet printed the old way, through Save PDF and a viewer:

| Drawn as | On the paper |
|---|---|
| Bull rings and centre dots | **present** |
| Bull numbers, the title, the identifier, the load block's field labels | **present** |
| The two QR codes | **absent** |
| **Every AprilTag marker, all 38 of them** | **absent** |
| The load block's box and its dividing rules | **absent** |
| The printed sheet's paper size and layout | correct, and the text is in the right places |

**Everything the scene draws as a filled rectangle is missing. Everything drawn as a circle or as text is there.** Question 21's plan names exactly those three kinds: "Bull bands: GDI paths from two ellipses. Rectangles: markers, codes and rules, each a GDI rectangle. Text: set in Arial."

**This is not a cosmetic fault. A sheet with no markers cannot be registered or measured at all**, and it looks normal until it comes back from the range. Alan lost most of a range trip to it, and he lives over an hour away.

**The other half of the evidence:** entry 107 section 2's printed-size test prints through the same path to "Microsoft Print to PDF" and finds all 38 markers within 0.015 mm. **So the rectangles reach that driver and not the Brother.** The fault is in how they are drawn, in a way one driver tolerates and another does not, not in whether they are drawn at all. Look at the brush, the pen and the raster capabilities: `GetDeviceCaps` with `RASTERCAPS`, whether a null pen with a solid brush is being relied on, and whether anything goes through a bitmap or a raster operation rather than a drawing call.

**What to do.**
- **Find the cause and say what it was.** The pattern above is the evidence; do not stop at "it works on my printer".
- **Prefer a drawing call every driver must support.** A filled rectangle drawn as a closed path or polygon is honoured by every printer driver; raster operations are not.
- **Then prove it on more than one driver.** Print every built-in sheet through the in-app path to at least two different printer drivers available on the machine, and compare each against the same sheet from Save PDF: the count and position of every code, marker, ring, rule and text item. **That comparison becomes a permanent test.**
- **A sheet that cannot be verified must not be printed silently.** Consider what the application can check before it commits a page: it knows how many markers the scene holds.
- **Alan is printing through Save PDF today**, which is unaffected. **Until this is fixed and he has confirmed a real print, the in-app Print button must not be the one people reach for**: make it say so, or disable it, your judgement, and say which you chose.

### 2. A hole on Alan's scan is not detected

**The sheet:** 9 shots on `GL-CF25-LTR-D`, scanned flat. **Alan will put it at `C:\Dev\grouplab-range-2026-09-20\sheet1\sheet1-scan.png`.** If it is not there when you read this, do section 1 and stop.

**What he reports:** the shot above bull 2 is not picked up. On the scan, that hole sits high and left of its bull, above the row of small markers that runs above bulls 1 to 5, near the top edge of the sheet. Every shot on that sheet landed high and left, so several holes sit outside their bull's outer ring, and this one sits furthest out.

**Find where it is lost, and report before changing anything.** Run the analysis with detailed logging and say which stage drops it:
- outside the region the detector looks at;
- inside a printed-matter exclusion zone, one of the markers or codes;
- refused by a size, shape or solidity filter;
- found but left unassigned and then not shown.

**The requirement, whichever it is:** a hole anywhere on the paper is a shot the person fired. It must be found and offered, even with no bull near it. `ReviewQueue` already has an item for a shot with no bull, and that is where a hole like this belongs. **A hole that is silently dropped is the one failure mode the Phase 1 gate exists to prevent**, and it would count against the 99 percent.

**If the fix is a one-line threshold or region change, make it, with a test using this scan's hole.** If it is a design question, for example how far outside the marker lattice the detector should look, raise it in `docs/QUESTIONS-FOR-PLANNING.md` with the measurement and build nothing.

**Add the scan to the corpus only through the intake tool**, as with any image, and only if the intake tool is happy. It is Alan's own sheet, so consent is not in question, but location data and the process are.

### 3. What yesterday actually produced

For the record, so the gates are not thought to have material they do not have: one sheet, nine shots, scanned flat, no photographs. **The mounted photograph gate, the 25-shot editor gate and the blank-paper gate all still have nothing.** He is going back today.
