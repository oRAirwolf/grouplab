## 2026-09-25, entry 199: Android addendum: every screen size, touch first, and QR codes as the no-account way to move data

Read with entry 198, and fold both into `docs/ANDROID.md`. Alan, 2026-09-25.

## 1. Phones, foldables and tablets, touch first

Alan: "The app should be built to work on phones, folding phones, and tablets and be dpi and screen size aware and scale itself
appropriately. The interface needs to work well with touchscreens."

1. **Layout by available width, not by device type.** Classes such as compact (phone, and the Fold 7's cover screen), medium (the Fold 7
   unfolded, small tablets) and expanded (Tab S8 Ultra, landscape). One screen rearranges; it is not three apps. The desktop keeps its
   own layout.
2. **Folding and rotating are ordinary events.** Unfolding the Fold 7 mid-review, or turning the tablet, keeps the photograph, the marks,
   the zoom, the selection and any half-finished edit, and relays out within a moment. Test it: a review in progress survives a
   configuration change from compact to medium and back. Respect the hinge if the platform reports one (a split layout must not put a
   control under the fold).
3. **Density aware.** Sizes in density independent units, text that follows the system font size (including the largest accessibility
   sizes without clipping), and images and the target drawn crisp at the screen's real density.
4. **Touch first.** Targets at least 48 dp. Pinch to zoom and two finger pan on the photograph; one finger drag moves a shot only when
   a shot is grabbed, never by accident while panning. Long press where the desktop has right click. No hover dependent information:
   everything the desktop shows on hover (the glossary tooltips included) is reachable by tap. Precise placement of a shot uses a
   magnifier or offset handle so the finger does not hide what it moves; say which, and test it on the Fold 7's cover screen, the
   hardest case.
5. **The spike in entry 198 section 2 runs on both of the Fold 7's screens and on the tablet** and reports whether Avalonia on Android
   handles the density, the fold and the rotation correctly. If it does not, that is a finding that affects the UI decision in 198 2.1.

## 2. QR codes to move data without an account

Alan asked whether a QR code could share data between devices, as a backup or no-account option. The planning session's reading, for you
to confirm or correct with measurements:

1. **A QR code cannot carry a session with its photograph.** One QR code holds at most about 2.9 KB, and far less when read reliably off
   a screen; a photograph is megabytes.
2. **It can carry the marks.** A session's shots (positions on the sheet, not in the photo), the target definition's id, the caliber, the
   distance, the load data and the choices made in review are a few hundred bytes to a few KB, compressed. GroupLab already writes compact
   binary frames into the QR codes on its printed sheets (`Gltd/Binary`, `InstanceCodec`). A **marks QR** shown on one device and scanned
   by the other rebuilds the session's figures and draws the shots on the rendered sheet, with no photograph. It works offline, with no
   network at all, phone to desktop or phone to phone. Measure how many shots fit in one code that a phone reads off a laptop screen at
   arm's length, and what happens above that (several codes in turn, or say "too large, share the file").
3. **It can pair the two devices for a full transfer.** The desktop shows a QR code with a one time key and its local address; the phone
   scans it and sends the whole session, photograph included, straight across the home or range Wi-Fi. No account, no internet, nothing
   leaves the local network, and the key means nothing else can send. Say what it needs on Windows (a firewall prompt, and how it is
   explained to the user) and what happens when the two are not on the same network.
4. **The order this sits in, with entry 198's stages:** A, share a file by hand; then these two QR routes, which need no account; then B,
   the sync folder; and C, sign in, only if needed. Say if a different order is better.
5. None of this changes the photograph rules: no GPS, location or time metadata read, printed, logged or sent.

Not part of the first stage. Record the plan in `docs/ANDROID.md`; build it when the app has sessions to move.
