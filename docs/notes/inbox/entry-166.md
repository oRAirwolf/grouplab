# 2026-09-24, entry 166: the macOS tester's answers, two defects, and the platform statement

The tester from entry 164 answered the checklist in entry 163 section 6. Together with his diagnostics
report, this is the first real evidence about macOS. Act on it after entry 164.

## 1. His answers

| check | answer |
|---|---|
| Gatekeeper | blocked on first launch; the `xattr` command on the download page cleared it |
| open the sample, detect, accept and analyse | works, and he called it fast |
| undo with Command Z | **does not work** |
| pinch zoom on the trackpad | **does not work** |
| Command Q | quits |
| print a target to PDF | works |
| text sharpness at Retina scale | sharp |
| anything odd or un-Mac-like | nothing |

He did not say whether two finger drag pans, and did not check the PDF's printed scale with a ruler.
Claim neither.

## 2. Defect: Command Z does nothing on a Mac

The planning session checked the code, and the cause is plain. `MainWindow.cs` line 3681:

    bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);

On macOS the Command key arrives as `KeyModifiers.Meta`, not `Control`. So every shortcut built on
`control` needs the Control key on a Mac, which no Mac user will press. Undo, redo and every other
modified shortcut are affected, not only undo.

1. Use the platform's own command modifier, which Avalonia exposes through the platform hotkey
   configuration, rather than hard coding Control. Every modified shortcut goes through one helper.
2. The on screen labels follow it. Line 515 hard codes "Ctrl Z". On macOS show the Command symbol or the
   word Command, from the same helper.
3. A test on each platform's modifier: the helper returns Meta on macOS and Control elsewhere, and no
   shortcut handler reads `KeyModifiers.Control` directly. A source test that fails on any direct read
   is the cheap way to hold that.

His "I wasn't sure what it would undo anyways" is also worth hearing: undo has nothing to show for it
until a mark has been changed. The undo control should be visibly disabled when there is nothing to
undo, and its tooltip should name what it will undo, such as "Undo: move shot 6".

## 3. Defect: pinch zoom was never built

There is no pinch or magnify gesture handling anywhere in `src/`. It was not broken on the Mac; it does
not exist on any platform.

1. Handle Avalonia's pinch gesture on the sheet, zooming about the point between the fingers.
2. **Resolve the conflict entry 163 section 1 created.** That section said a two finger trackpad drag
   always pans and a scroll always zooms. On a Mac trackpad, a two finger drag *is* a scroll, so those
   two rules contradict each other. The convention a Mac user expects is: two finger drag pans, pinch
   zooms, and Command with scroll zooms. A mouse wheel on Windows is expected to zoom. Find out what
   Avalonia actually delivers for a trackpad scroll against a wheel notch on each platform, measure it
   rather than assume it, and choose behaviour per input device where the platform lets you tell them
   apart. Report what you found and what you chose.
3. Check Windows precision touchpads as well, which deliver pinch differently again.

## 4. The platform statement

`docs/PLATFORM-SUPPORT.md` and every copy generated from it say the macOS builds have never been run on
a Mac. That is now false. Replace the macOS paragraph with a statement of exactly what has been checked
and nothing more, keeping entry 147's rules: no "I" or "you", and the developer is "they".

The substance, to be worded in the statement's own style:

- The Apple Silicon build, nightly 93, has been run by one tester on one MacBook Pro with an M5 Max,
  macOS 27, natively rather than under Rosetta.
- macOS blocks the first launch, and the command on the download page clears it.
- Opening, detecting and analysing the published sample, saving a session, printing to PDF, quitting
  with Command Q and the diagnostics report all worked, and text is sharp on a Retina display.
- Known problems: Command shortcuts such as Command Z do not work yet, and pinch zoom is not built on
  any platform.
- **The Intel build has never been run on a Mac.** Keep saying so.
- It remains a test build: never offered by the updater, and the developer still does not own a Mac.

Remove the known problems line when sections 2 and 3 ship, in the same commit as the fix. Put this
statement in the claims register of entry 159 with its evidence: this entry and the report in entry 164.

## 5. Thank the tester in the right place

If the project keeps a list of testers or contributors, add the macOS tester there by the name Alan
gives, or as an anonymous macOS tester if Alan gives none. Do not invent a name. Put the question in
`docs/notes/for-alan.md` rather than guessing.
