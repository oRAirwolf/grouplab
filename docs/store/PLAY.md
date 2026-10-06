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
  the Delete data URL https://grouplab.org/support/ (entry 378 section 4 adds the "Delete your data" section that address needs).
- **Data types**, every one collected, not shared, not ephemeral, the user can choose, purpose Analytics only: Crash logs, Diagnostics,
  Device or other IDs, App interactions, Photos.
- **Privacy policy:** https://grouplab.org/research/what-grouplab-sends/
- **Target audience:** 18 and over only; not appealing to children.
- **Store settings:** App; category Sports; contact email support@grouplab.org; no phone; website https://grouplab.org/; external
  marketing on.
