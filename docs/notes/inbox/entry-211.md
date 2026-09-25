## 2026-09-25, entry 211: request 29 done; Alan's older phones become the test devices

## 1. Request 29, answered 2026-09-25

1. Folded, upside down: **turned.**
2. Unfolded, upside down: **turned.**
3. The folder picker: **Google Drive is listed, and Alan could choose a folder once he drilled down into his Drive.** He did not mention
   OneDrive; it may not be installed on the Fold 7. So `docs/ANDROID.md` section 8 stage B, the sync folder, is possible on Android through
   Google Drive. Record it, with OneDrive unknown. Whether writes into that folder sync reliably and what happens with an edit on both sides
   is the next question for stage B, when the app has sessions to move; not now.

Close request 29. **The Fold 7 is no longer needed for now**; say so in for-alan.md so Alan can turn Wireless debugging off.

## 2. Alan's other test phones

Entry 207 said Alan's older phones replace the Galaxy A16 and A06 classes as the low end references. What he has:

1. **Essential PH-1**, on **Android 10**, turns on. As the planning session understands it: Snapdragon 835 from 2017 and 4 GB of memory,
   and Android 10 was its last update. Confirm from the phone itself. If so it is exactly the floor: the approved minimum Android, the
   approved minimum memory, and a processor in the same class as the budget phones (entry 206). **It is the device that decides whether
   Android 10 can stay GroupLab's minimum** despite .NET 10 listing Android 14 (entry 207 section 2).
2. **Samsung Galaxy S20 5G**, 8 GB and 128 GB, **Android 13** with One UI 5.1. A middle reference: Android below 14, a 2020 flagship
   processor, 8 GB.
3. **A OnePlus**, model not yet known, which needs charging before it turns on. Alan will send its model and Android version later.

## 3. What to do

1. A request in for-alan.md, at the top, to get the PH-1 and the S20 onto adb in one sitting. **Android 10 has no Wireless debugging
   pairing**, so the PH-1 needs a USB cable (and possibly Google's USB driver on Windows; say if so, with the exact step). The S20 on Android
   13 can pair wirelessly like the Fold 7. Write the Developer options steps for each phone's own menus, and what `adb devices -l` should
   show.
2. When they are connected, in one sitting as entry 209 did, so the phones are not left waiting:
   - install the spike on both and confirm it starts on Android 10 and 13, which answers the .NET support question;
   - the working resolution runs of entry 209 on both, with time and peak memory, so the capped resolution is chosen against the PH-1 and not
     the Fold 7; say whether the 8 MP working size meets the approved budget (peak under about 400 MB, detection about 30 s on the floor
     device) on the PH-1;
   - the camera listing on both;
   - then say plainly that the phones can be put away.
3. Record all three phones in `docs/ANDROID.md` as the reference devices, with their measured results beside the Fold 7's, and replace the
   A16 and A06 classes as the targets with the S20 and the PH-1, keeping the market figures of entry 206 as the reason.
