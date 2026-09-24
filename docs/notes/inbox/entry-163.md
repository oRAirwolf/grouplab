# 2026-09-24, entry 163: a first real user's feedback on the marking screen, and a cartridge list

Alan's friend, the one who shot `Scan_20260923.png`, used nightly 93 and sent five points. He is the
first person other than the developer to use the application for real, so his feedback outranks any
guess this project has made about what a new user needs.

**Order:** do entries 161, 162 and this one before 159. `docs/notes/STATE.md` lists 161 last because it
was rewritten before Alan asked for 161 next; Alan's instruction stands. Correct `STATE.md` when you
next rewrite it.

## 1. Pan by default, and pan without switching tools

"Pan should be the default selected tool instead of select."

He is right that a new user's first act is to look around the sheet, and the current default makes a
drag do nothing useful. But a plain swap moves the problem: then a click on a mark does nothing useful.
Fix the underlying thing:

1. **The default tool pans on a drag over empty sheet and selects on a click on a mark.** Dragging a
   mark still moves it only in the select tool, because entry 143 settled that a drag moves a shot and a
   stray drag while panning must not move a measurement.
2. **Middle button drag and a two finger trackpad drag always pan, in every tool. Scroll and pinch
   always zoom.** Check this on the trackpad specifically, since a macOS tester now exists (section 6).
3. The toolbar shows the default tool as the selected one on open.

## 2. Pan and select on neighbouring keys

"P for pan is not a good keybind because it should be close to S so you can select quickly between pan
and select."

Today pan is P and select is V (`MainWindow.cs`, the tool switch). The principle he is asking for is
right: the two tools a person alternates between belong on neighbouring keys under the left hand.

1. Put pan and select on adjacent keys on the left side of the keyboard.
2. **S is not available**: it is how a sighter bull is typed when reassigning a shot by keyboard, and
   the digits are bull numbers. Check every other candidate against the whole shortcut map and against
   the review keys, Space, Enter and N.
3. A reasonable choice is V for select, which is the convention in most design tools, with C for pan
   beside it, if C is free. Choose, and say why in the report.
4. Keep P and V working as aliases so nobody who learned them is broken.
5. The keyboard strip along the top of the marking screen shows both.

## 3. The calibre field must understand cartridge names

"When typing 6.5, it brings up .257 because it equals 6.53mm, but it should be referring to .264. We
need to research the most common rifle and pistol cartridges and add them to the list so there is no
confusion for people who are not familiar with bullet calibers."

**This reverses entry 108's decision, deliberately.** Entry 108 chose to refuse calibre designations and
ask for a diameter. A real user has now shown that a person who knows they shoot a 6.5 Creedmoor does
not know that means 0.264 in, and that matching a typed 6.5 against diameters in millimetres picks the
wrong bullet. The refusal was protecting against ambiguity; a named cartridge table removes the
ambiguity instead of refusing the input. Record the reversal where entry 108's rule is written.

### 3.1 The rules

1. **Match names before numbers.** A typed entry is first matched against cartridge names and their
   common shorthand. Only if nothing matches is it read as a diameter.
2. **A bare number that is also a cartridge family name is a name first.** "6.5" is the 6.5 mm family,
   0.264 in. "7mm" is 0.284 in. "308", "30-06", "9mm", "45" are names.
3. **A number with a leading point or a leading zero below one is a diameter in inches**: ".264",
   "0.264". A number followed by mm is a diameter in millimetres.
4. **Every suggestion shows the cartridge names and the diameter together**, in inches and millimetres:
   "6.5 Creedmoor, 6.5 PRC, 6.5x55 and others: 0.264 in (6.71 mm)". Never a bare diameter.
5. **The traps are named in the suggestion list** where a person could plausibly pick the wrong one.
   Typing 6.5 shows 0.264 first and, below it, "not the same as .25 calibre, 0.257 in (6.53 mm)".

### 3.2 The traps the table must get right

These are the cases where the name and the bullet diameter disagree or collide. Each one gets a test.

| typed or named | bullet diameter | the trap |
|---|---|---|
| 6.5 mm family: 6.5 Creedmoor, 6.5 PRC, 6.5x55, .260 Rem, 6.5 Grendel, 6.5x47 Lapua, 26 Nosler | 0.264 in | 6.5 mm by name, 6.71 mm by bullet |
| .25 calibre: .25-06, .257 Roberts, .257 Weatherby, .250 Savage, .25 Creedmoor | 0.257 in | 6.53 mm, which is what "6.5" wrongly matched |
| 6 mm: .243 Win, 6mm Creedmoor, 6 ARC, 6mm BR, 6 Dasher | 0.243 in | |
| .22 centrefire: .223 Rem, 5.56x45, .22-250, 22 ARC, .224 Valkyrie, .22 WMR, 5.7x28 | 0.224 in | |
| .22 rimfire: 22 LR, 22 Short | 0.222 in | entry 153 section 4 |
| .270 Win, 6.8 Western, 6.8 SPC | 0.277 in | named 270 and 6.8 |
| 7 mm: 7mm Rem Mag, 7mm-08, 7 PRC, .280 Rem, .284 Win, 7x57, 28 Nosler | 0.284 in | .280 Rem is 0.284 |
| .30 calibre: .308 Win, .30-06, .300 Win Mag, .300 PRC, .300 BLK, .300 Norma, .30-30, .300 WSM | 0.308 in | |
| 7.62x39, 7.62x54R, .303 British | 0.311 in | not 0.308, despite the 7.62 |
| 8mm Mauser | 0.323 in | |
| .338 Lapua, .338 Win Mag, .338 Norma, 8.6 Blackout | 0.338 in | 8.6 mm by name |
| 9mm Luger, .380 ACP, .357 SIG | 0.355 in | |
| .38 Special, .357 Magnum | 0.357 in | named 38 |
| .40 S&W, 10mm Auto | 0.400 in | |
| .44 Magnum, .44 Special | 0.429 in | named 44 |
| .45 ACP, .45 Colt | 0.451 to 0.452 in | |
| .50 BMG, 510 Whisper | 0.510 in | |

The planning session wrote this table from memory and it is a seed, not a source. **Verify every
diameter against two independent published specifications** before it goes in, the same rule the G1 and
G7 tables were held to, and record the sources beside the table. Where a cartridge has a published range
rather than one diameter, as the .45s and 7.62x39 do, carry the nominal value and note the range.

### 3.3 Coverage

Build the list out to the common rifle and pistol cartridges, not only the traps: the popular hunting,
target, precision rifle, military surplus and handgun rounds. Order suggestions so the common ones come
first, and let the whole table live in one data file with a test that every entry has a name, at least
one shorthand, a diameter and a source.

## 4. What has to be filled in goes at the top, and says so

"Required values like caliber and shot distance and rounds fired should be near the top and marked in
red until they are filled in. They are buried further down and it is not obvious that they need to be
filled in first."

The screenshot bears him out: calibre, shot distance and rounds fired sit below the review queue, the
selected shot and the whole scale explanation.

1. **A setup block at the top of the panel**, above the review queue: calibre, shot distance, rounds
   fired, then rifle, barrel and load. "Same setup as the last target" sits in it.
2. **Each empty field that matters is marked** with a red outline and the word "needed". Not colour
   alone, because colour alone fails a colour blind user.
3. **Each field says in a few words what it unlocks**: distance gives MOA and mil; rounds fired settles
   the count question; calibre names the bullet for the record and for ballistics. After entry 161,
   calibre no longer changes which holes are flagged, and the wording must not claim it does.
4. **Each can be answered "not known"** explicitly, which clears the mark. A shooter who does not know
   how many rounds they fired should be able to say so rather than being nagged forever.
5. **Accept and analyse** lists what is still needed, in one line, rather than silently proceeding or
   silently refusing.

## 5. The analysis screen explains too much by default

"On the analyze screen, there is too much information by default that is being explained in sentences
and it should be collapsed by default."

1. Every explanatory paragraph on the analysis screen is **collapsed by default** to its one line
   headline, with the number and a control to expand it. The scale panel's "why" is already this shape;
   use it everywhere.
2. The application remembers which sections a person has expanded.
3. Nothing is deleted. The explanations are good and entry 153 wants more interpretation, not less. They
   stop being in the way; they do not stop existing. Entry 154's tooltips carry the short definitions,
   and the expanded sections carry the reasoning.
4. `NothingIsCutOffTests` covers both states.

## 6. A macOS tester exists, so test what a Mac changes

A second friend has run GroupLab on a MacBook Pro with an M5 Max and reports no errors. The planning
session is collecting a structured report from him and will deliver it as an inbox entry. Meanwhile,
check these in code, because each is a known way a Windows first application goes wrong on a Mac:

1. **Shortcut modifiers.** The screen shows "Ctrl Z undo". On macOS that must be Command, both in
   behaviour and in every label on screen.
2. **Trackpad gestures**, per section 1 item 2.
3. **The application menu**: Quit on Command Q, and the standard macOS menu entries.
4. **The updater** must not offer a macOS test build an update, per entry 147.

**The platform statement is now wrong.** `docs/PLATFORM-SUPPORT.md` and everything generated from it say
the macOS builds have never been run on a Mac. That stopped being true today. Do not rewrite it yet;
the report will say exactly what was checked, and the statement should claim precisely that and no
more. Add it to the claims register under entry 159 as a claim that is known to be stale.

## 7. Tests

1. Typing 6.5 suggests 0.264 first and names the 0.257 trap. One test per row of section 3.2.
2. The default tool on open pans on a drag over empty sheet and selects on a click on a mark.
3. Pan and select are adjacent and conflict with no other shortcut, including bull number entry.
4. The setup block is first in the panel and every empty needed field carries both the outline and the
   word.
5. Every explanation on the analysis screen is collapsed on first open.
