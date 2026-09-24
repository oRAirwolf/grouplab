## 2026-09-24, entry 192: Unholy's "closed unexpectedly 5 times" is one bug, the caliber Set button, and the message is wrong

Read with entry 189 section 6, which this explains.

## 1. What arrived

Unholy saw "GroupLab closed unexpectedly 5 times, and recorded what went wrong." and said he never saw it crash. He sent a report:
`C:\Dev\grouplab-submissions\unholy\grouplab-report-20260924-174126.zip`, SHA-256
`3eb0f276e8cb7e3a9572025c67b6a1c053de8106d3fd997da99703c9b82bcec1`. It holds two logs from nightly 95, one crash record, the environment
(nightly 99 when the report was made, Windows 10.0.26200, scale 1) and the description "Uploading target image". Untrusted data; entry 190
covers its use. No paths or location data are in it; the stack paths are already `<path>`.

## 2. What the planning session found in it

All five are the same exception with the same stack, on nightly 95 (dbdb3a3), two sessions:

- `System.ArgumentOutOfRangeException` inside Avalonia's `AutoCompleteBox.set_Text` → `PopulateDropDown` → `CloseDropDown` →
  `SelectingItemsControl.set_SelectedItem` → `SelectedItems.GetEnumerator`, called from **`MainWindow.SetCalibreFromBox()` line 1508**,
  from the Set button's click handler (line 3954).
- Every one is `source=dispatcher`, and after each the log carries on normally: a second detection, review, accept, session save, and
  finally `app.exit code=0`. **GroupLab never closed.** The dispatcher handler recorded the exception and the application kept running.
- The two-click caliber problem in entry 189 section 6 is this: the first Set throws inside `set_Text` and is swallowed, so nothing is
  set; the second Set works because the drop-down state has changed. Unholy saw a button that needed two clicks; the log saw a crash each time.
- In the second session the first detection ran without a caliber, the crash came on Set, and he detected again with .338 in. Two holes
  of seven were reviewed as doubled. Worth a glance when checking the fix, nothing more.

## 3. What to do

1. **Fix the cause.** `SetCalibreFromBox` sets `calibreBox.Text` while the drop-down is open or populating. Do not set `Text` from
   inside that path: close the drop-down first, or set the value through the box's selected item, or defer the text change with
   `Dispatcher.UIThread.Post`. Pick what makes entry 189 section 6 work in one action. Write a UI test that fails today with this exact
   exception: type "6.5", let the list open, click Set; and type "6.5", click "6.5 Creedmoor".
2. **Fix the message.** A dispatcher exception that was caught and survived is not "closed unexpectedly". Record the two kinds
   separately: an exception the application survived, and a real exit without a clean shutdown. The first-run banner then says what
   happened, for example "GroupLab hit an error 5 times and kept running", and only a real crash is called closing. Keep "Make a report"
   for both.
3. **Say whether surviving is safe.** After an unhandled exception in a click handler the window's state may be half changed. State in
   the code comment, and in the report to the planning session, which state this handler could leave inconsistent, and whether the
   dispatcher handler should keep swallowing or should offer to save and restart. Do not change that policy in this entry; report it.
4. **Make these visible to you.** A report whose crashes all share one stack should be recognizable at a glance: when a report is
   made, group identical stacks and count them, so "5 crashes" reads as "1 error, 5 times, Set caliber".

## 4. The report

Plain words for Alan: the caliber Set button error is fixed and in which build, the new wording, and whether any other error in
Unholy's or Fenix's reports is still open.
