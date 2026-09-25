## 2026-09-25, entry 212: the older phones only when absolutely needed; the Fold 7 is the development phone

**Read before entry 211, and let this override its section 3.** Alan, 2026-09-25: "we should keep the testing on the older phones to an
absolute minimum because I dont want to keep switching phones around. Do the primary development on the Fold 7 and the other phones we will
test with once it is absolutely needed."

1. **Do not ask Alan to connect the Essential PH-1 or the Galaxy S20 now.** Entry 211 section 3 items 1 and 2 are withdrawn. If request 30
   (the older phones) asks him to set them up now, rewrite it as a note of what the phones are and when they will be needed, not as
   something to do; it should not count as open or urgent.
2. **All development and routine testing runs on the Fold 7.** Choose the capped working resolution from the Fold 7's measurements and
   entry 206's market figures, and use the emulator with limited cores and memory to estimate the floor.
3. **The older phones come out at named milestones only**, each a single sitting that does everything needing them at once, and never for
   one small check:
   - **Once before the first Play closed testing release:** the PH-1 (Android 10, the floor) and the S20 (Android 13). Install, start,
     run detection at the chosen working size, time and peak memory, the capture screen once. This is also where Android 10 is confirmed or
     the minimum is revisited.
   - Otherwise only if a problem is reported that cannot be reproduced on the Fold 7 or the emulator.
   Write these milestones in `docs/ANDROID.md` so they are not forgotten or expanded.
4. **Keep entry 211's record of the phones** (models, Android versions, memory) in `docs/ANDROID.md` as the reference devices for those
   milestones, and entry 211 section 1 (request 29's answers) stands.
5. **For the Fold 7 too, batch.** When a stage needs the phone, gather everything that needs it into one sitting and tell Alan in advance
   through for-alan.md, as entry 209 did, so he connects it once rather than repeatedly.
