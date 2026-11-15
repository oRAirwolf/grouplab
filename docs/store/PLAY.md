# Google Play listing images

NOTES-FROM-PLANNING.md entry 248 section 3. Two images for the Play store listing, ready for when the listing is filled in, both written by
`scripts/android-icons.py` from the desktop's mark (`src/GroupLab.App/Assets/grouplab-mark.svg`), so a change to the mark carries here
as well as to the application's own icons:

- `play-icon-512.png`: the application icon, 512 by 512, the mark on the desktop's dark window color; Play rounds its corners itself.
- `play-feature-1024x500.png`: the feature graphic, 1024 by 500, the mark and the wordmark as the desktop's header sets them.

The phone and tablet screenshots Play asks for come from the devices, not from here.

## Google Play, App content, as declared (entry 379)

Alan filled this in, mostly for the first time, and wrote "forms updated" on 2026-10-06. Recorded here so an audit can hold the
application to it.

- **Data safety:** collects data; encrypted in transit; no account creation; no login with outside accounts; deletion on request, with
  the Delete data URL https://grouplab.org/support/ (its "Delete your data" section, https://grouplab.org/support/#delete-your-data, was added by the consistency audit of 2026-10-07, question 89).
- **Data types**, every one collected, not shared, not ephemeral, the user can choose, purpose Analytics only: Crash logs, Diagnostics,
  Device or other IDs, App interactions, Photos.
- **Privacy policy:** https://grouplab.org/research/what-grouplab-sends/
- **Target audience:** 18 and over only; not appealing to children.
- **Store settings:** App; category Sports; contact email support@grouplab.org; no phone; website https://grouplab.org/; external
  marketing on.

## Store listing and closed test, as set on 2026-11-15 (entry 487)

Set by the planning session in Play Console with Alan signed in, on 2026-11-15. The Console is the authority; this is its record.

- **Dashboard setup:** 11 of 11 tasks complete; the store listing was the last (the App content forms are above, entry 379).
- **Default store listing (en-US):** app name GroupLab; the short description (66 characters) and the full description (1,479
  characters, the Microsoft Store text reworded for the phone) are in `docs/store/play-listing.txt`, read back through the Play
  Developer API.
- **Graphics:** `play-icon-512.png` and `play-feature-1024x500.png` from `scripts/android-icons.py`; seven phone screenshots, each a
  `docs/figures/screens/phone/fold-*-dark.png` of nightly 225 scaled to 1920 tall (890 wide) and centred on a 1080 by 1920 canvas
  filled with the picture's own edge colour (RGB 19, 20, 23), in the order result, capture, fingerprint, pairing, targets,
  scale-markers, open-targets. `scripts/play-screens.py` makes the same set from the newest pictures into `docs/store/play-phone/`.
  No tablet screenshots yet.
- **AI asset declaration:** "Don't label assets": the screenshots are GroupLab's own screens, and the icon and feature graphic come
  from `src/GroupLab.App/Assets/grouplab-mark.svg`.
- **Closed testing, Alpha track** (`alpha` in the API): all 178 countries and regions; testers by Google Group only (a track takes
  email lists or groups, not both): `grouplab-testers@googlegroups.com`, which anyone can join, where only managers post and only
  owners see the members, so testers cannot see each other. Feedback URL https://grouplab.org/support/. The internal track and its
  email list are unchanged.
- **First closed release:** version code 225 (0.2.0-nightly.225), full rollout, sent for review at about 08:10 UTC on 2026-11-15
  ("Changes in review"). One warning, the deobfuscation file, which `docs/ANDROID.md` explains. The opt-in link appears on the
  track's Testers tab once Google approves.
- **Every nightly after it** goes to `internal` and `alpha` by `play-upload.yml` (entry 487 section 1), with the build's own noticed
  lines as its notes. While Google is reviewing anything, a run sends nothing rather than cancel that review, and `alpha` is sent only
  when its newest release is completed. Managed publishing is off, so later closed releases go to Google's quick review by themselves.
- **The way to production:** a personal account needs 12 testers opted in to the closed test for 14 continuous days, then an
  application for production access; Google also weighs whether testers used the app. Internal testers do not count.
