## 2026-09-25, entry 219: a standing roadmap for Android and the desktop, so work does not wait on the next entry

Alan asked whether any work is planned for the Android and Windows builds. Honestly, beyond the queued entries, none was: STATE.md's next
item is "the capture screen's CameraX spike on the Fold 7, when an entry asks for it". This entry asks for it, and for what follows, as a
standing order of work. Do it after entries 215 to 218. When an item finishes, carry on to the next without waiting for a new entry; stop
only for a request to Alan (batched, as entry 212 says) or a decision that is his. Keep this list, with each item's state, in STATE.md in
place of "The next three".

## 1. Android, in order (the Fold 7 is the development phone; entry 212's rules on the older phones stand)

1. **The working resolution in Core.** From entry 209's measurements, choose the capped working size (8 MP gave 3.3 s, 373 MB, 25 of 25 and a
   2.5 thousandth mean shift on the Fold 7) and make it a Core setting the phone always uses and the desktop can use for very large images.
   Say what it costs in accuracy on the sample and on two real range photographs.
2. **The capture screen spike:** CameraX preview inside the Avalonia screen, the lens choice from entry 209's camera listing, tap and
   automatic focus and exposure with a lock, a full resolution still, and MOBILE-CAPTURE.md's live conditions (sheet in frame, markers,
   angle, focus, exposure) on the analysis stream, with its one-instruction guidance and the automatic shutter. Measured on the Fold 7 in one
   sitting, announced in advance.
3. **The real application project**, `org.grouplab.app`, replacing the spike as what CI builds: navigation, the first run window with the
   three choices of entry 208, Settings, the error report and target sending queues shared with the desktop's code.
4. **Capture to result:** capture or pick a photograph, detect at the working size, review and correct by touch (entry 199: 48 dp targets,
   pinch and pan, the offset handle with magnifier), the group figures and the composite plot, save the session.
5. **Sessions between devices, stage A:** share and open a session file (Android share sheet, Google Drive).
6. **Release builds:** a signed APK and AAB on the nightly train, and the Play internal testing track. This needs Alan's upload keystore and
   the Play Console app entry: write both as one request when the build is otherwise ready, with the exact commands, and never read the
   keystore.
7. **Milestone:** the older phones in one sitting (entry 212), then a closed test with testers from Discord.

## 2. The desktop (Windows first, macOS and Linux builds as today)

1. **The survey and benchmark, desktop part** (entries 207 and 208, `docs/SURVEY.md`), with its receiver and worker on the server as one
   install request for Alan, batched with any other server step pending.
2. **The hole center choice** (question 51) as soon as request 9's hand markings arrive; nothing before.
3. **Ongoing feedback** from Alan, Unholy and Fenix comes first whenever it arrives, as now.
4. **A plan for a first beta or stable release:** what must be true for it (a checklist in `docs/RELEASE-PLAN.md`: open defects, the
   minimums table, the privacy text, the user guide), and for Windows the signing choices (Alan intends the Microsoft Store eventually and may
   buy a code signing certificate if the cost is reasonable): research the current options and costs and put them to him as a request with a
   recommendation. Plan only; no release without Alan asking for one by name.

## 3. How to share the time

Android is the higher priority; alternate so desktop feedback never waits more than one Android item. Each item ends with its report in
plain words and the nightly it ships in.
