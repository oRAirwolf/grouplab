# GroupLab user guide

GroupLab measures how accurately a rifle shoots, and tells you how much its figures can be trusted. This guide takes one sheet from the printer to the analysis. It describes the Windows application as it is built today, and every picture in it is a render of the build.

The rail down the left of the window is how you move around it:
- the mark at the top is the sheet you are working on;
- then Targets, where sheets are chosen and printed, the session records and the ballistics screen;
- then load comparison;
- the gear at the foot is the settings.

## 1. Print a sheet

Open **Targets** from the rail. It lists the built-in sheets by family, read only, and your own sheets after them. Choose a sheet and
everything it takes to print it is beside the list, with its artwork filling the rest of the screen.

![Targets, with a built-in sheet chosen](figures/screens/current/targets-light-1400x900.png)

- **The load block** can be left blank, to write in at the range, or filled in now from the fields shown. On a sheet with room for it, a filled block also carries an instance code, so GroupLab reads the load straight off the sheet.
- **Print** (on Windows) prints from inside GroupLab at actual size. It refuses, with the reason, when the paper is not the sheet's or ink would fall in the printer's margin. When the job is sent, a confirmation names the printer and the pages.
- **Open to print** opens the PDF in your viewer instead. Print it from there at Actual size or 100 percent, never Fit.
- **Save PDF** keeps the file.

**The zeroing grids are for sighting in by eye.** Each prints at exact scale, so at the bench you fire, read the correction off the grid, dial it and fire again. For a zero worked out from a group, and the group figures, shoot a [5x5 sheet](#2-shoot-it) instead. GroupLab still reads a scanned zeroing grid, but it cannot know the order of the shots or the dialing between them, which is what the grid was for.

**Check the size before you shoot.** Measure from the center of bull 1 to the center of bull 5 with a ruler. On the Letter 5x5 sheet it is 5.98 in (152.0 mm). If it is not, the printer scaled the sheet, and it should be printed again.

**Your own sheet.** Design your own sheet, at the top of Targets, lays out a grid of bulls:
- you choose the page, the rows and columns, the spacing, the ring, the sighters and a load block;
- a layout that cannot register or fit is refused;
- if you give your five-shot group, a spacing tight for it is warned about.

Save to your own sheets keeps a design in the list. There you can rename it, duplicate it, or delete it after GroupLab asks. Duplicate works on a built-in sheet too, as the start of one of your own. Deleting a sheet never makes a session unreadable, because every session keeps its own copy of the sheet it was analyzed against.

**A volunteer pack.** Print a volunteer pack, beside the chosen sheet, gives the sheet and one page of instructions together, for someone shooting a sheet for the project.

## 2. Shoot it

- Mount the sheet flat, supported all over.
- For a load development sheet, fire one shot per bull, in order, starting at bull 1. The sighter bulls are for sighters, and GroupLab keeps them out of the group unless you ask for them to be analyzed.
- Write only in the load block.

Some sheets break one shot a bull on purpose, such as two shots into each of bulls 1 to 10. Say so before you accept the marking: Shots per bull, in the marking screen's side panel, reads the sheet by nearest bull, or as two shots on the bulls you name.

A sheet with one scoring bull takes a group. Every shot on it goes to that bull, however far out, and the review asks nothing about how many there are; say how many you fired, in Rounds fired, and a count that disagrees names the mark most likely to hold two.

## 3. Photograph or scan it

**A flat scan at 600 dpi is the best record of a sheet.**

GroupLab reads the resolution your scanner wrote into the file and uses it as a starting point, then measures the real resolution from the sheet's own printed markers and tells you both. Where the two disagree, the markers win, because they were printed at a known size and the file's number is only what the scanner meant to do.

For photographs:
- stand about 2.5 ft (75 cm) from the sheet;
- use the phone's main camera, not its wide or zoom lens;
- keep the whole sheet and all its corner squares in the frame;
- do not crop the pictures, and do not send them through a messaging app, which shrinks them.

## 4. Mark it and settle the review queue

Open image, in the header's menu, opens a scan or a photograph. On a GroupLab sheet the rest happens on its own:
1. GroupLab reads the sheet's printed codes to name its definition.
2. It registers the page from the corner and edge markers.
3. It finds the holes by comparing the image with the sheet as printed.
4. It gives each hole to a bull.

Every result is an ordinary mark that you can move, delete or reassign. On any other target, you mark the holes by hand against a length or a rectangle of known size. To change that scale, tap its two ends or four corners again and enter the new size, or choose the length or rectangle tool and **Change the length of the scale in use**; every figure follows. Typing a caliber offers the cartridges that match, and choosing one from the list, with a click or with Enter, sets it at once.

With the rectangle tool, **Find the paper's edges** places the four corners on the paper itself when the sheet stands out from what is
behind it, still draggable, and offers a standard paper size when the photograph's shape matches one. On a white board it cannot tell the
paper from the board, and you tap the corners. A photograph taken more than 40 degrees off square to the sheet is refused, with the angle
named, and every photograph you open keeps how far off square it was and how good it is: good, usable or poor.

![The marking screen, with the review queue in the side panel](figures/screens/current/marking-light-1400x900.png)

The pill in the header counts the marks that need you. The review queue in the side panel lists each one with the choices that settle it. The items are:
- **Contested assignment:** a hole that could belong to either of two bulls, or one that the matching gave to a bull other than its nearest.
- **Possibly two holes:** a mark about the size of two holes.
- **Two shots on one bull.**
- **No bull.**
- **Count differs from rounds fired:** raised when you have said how many rounds you fired.
- **Refused candidate:** something the detector saw and did not take as a hole.
- **Bulls with nothing on them:** when you have said how many rounds you fired and GroupLab finds fewer, it names the bulls that are empty. **A shortfall is never allowed to pass as a clean result.**

**Name the caliber if you know it.** It sets the smallest hole GroupLab will accept, which matters most for small calibers, and it gives you the edge-to-edge figure. On one of the test scans it is the difference between nineteen holes found and twenty-four; on another it is the difference between missing the shot at the edge of the scan and finding it. A .22 hole in paper is much smaller than the bullet that made it, and without the caliber the detector has only the shape of a mark to go on.

**Holes between bulls, and marks off the grid.** A hole that lands between two bulls, or beside the grid rather than on it, is kept, counted and offered to you. It is never dropped for being in the wrong place. Where GroupLab is not sure which bull a hole belongs to, it says so and the figures built on that assignment carry the doubt with them until you have settled it: **a figure that rests on a guess is marked as resting on a guess.**

The queue works from the keyboard:
- **Space** goes to the next item.
- **Enter** takes its first choice.
- **Type a bull's number and press Enter** to give the shot to that bull.
- **T** takes a flagged mark as the two shots the detector says it is.
- **N** marks the selected mark as not a shot.

The tools have keys too:
- C or P pan, V select, I impact, A aim, L length and R rectangle;
- the square brackets turn the view;
- Delete removes the selected mark;
- Ctrl+Z and Ctrl+Y undo and redo, and on a Mac Command Z and Shift Command Z. The undo button's tooltip says what it will undo.

To move around the sheet, a mouse wheel zooms about the pointer, and a touchpad's two finger drag moves the sheet. A pinch zooms, on a touchpad or a touch screen, and so does Ctrl, or Command on a Mac, with any scroll. On a Mac any plain scroll moves the sheet, as it does in other Mac applications.

In the side panel you also set:
- the caliber, as the bullet's diameter;
- the shot distance;
- the rifle, barrel and load, from your records;
- the rounds fired, as a check on the count.

When the marks are right, Accept and analyze. Anything still open stays open: the analysis names it, and the sheet crumb goes back to it.

## 5. Read the analysis

The analysis has three columns:
- **On the left:** the sheet small, drawn from its definition with every shot on it, then the load and the shot table. A click on a bull in the small sheet selects its shots.
- **In the center:** the composite plot. Every scoring shot is drawn on one bull, each from its own bull's center, over the bull's rings drawn as wide gray bands. Green lines cross at the group's center and blue lines at where you aimed, both across the whole plot; the CEP circles are green, CEP 50 dotted, CEP 90 solid and CEP 95 dashed; the extreme spread is a red dashed line between the two shots furthest apart. Toggles beside the plot turn CEP 50, CEP 90, CEP 95 and the extreme spread on and off, and GroupLab remembers them; CEP 95 starts off. **Group** and **Whole target** beside them frame the group alone or the entire bull with the group inside it, also remembered. The mouse wheel or a pinch zooms, dragging empty paper moves the view, and a double click returns to the fitted view. An excluded shot is drawn hollow and is never removed.
- **On the right:** the zero correction, the figures and the two judgment cards.

![The analysis, with every "why" open](figures/screens/current/analysis-open-light-1400x900.png)

**Click a hole to edit it.** A small editor opens beside it, not a dialog over the page. From it you can move the hole with the arrow keys, a hundredth of an inch a press and a tenth with Shift held; give it to another bull, either from the list or by clicking a bull on the sheet; set its size by hand where the detector read it wrong; leave a note on it; and mark it:
- **Sighter**, which sets it aside from the group, as a shot on a sighter bull already is;
- **Flyer**, which calls it out on the sheet and in the list and **changes no figure**;
- **Leave out of the figures**, which does change them, and needs a reason;
- **Not a shot**, for a staple, a tear or a pen mark.

Flyer and "leave out" are deliberately two different things. Pointing at a shot and dropping it from the group are two different decisions, and GroupLab will not quietly make the second one for you because you made the first.

Every edit shows a small message at the bottom of the screen saying what changed, with **Undo** on it. Ctrl+Z and Ctrl+Y work everywhere, Command Z and Shift Command Z on a Mac, and the Undo on the message is the same undo.

**Every word you may not know is underlined with dots,** a figure's name or a word like sigma, MOA or bull, here and on the website. Hold the pointer over it, or tab to it, for two or three plain sentences saying what it means; click it or press Enter for the whole entry, with **More in the glossary**. For a figure the sentences say what it is good for and what the number of shots does to it. Every explanation says something about sample size, because every one of these figures depends on it, and the commonest mistake in group shooting is treating one five shot group as a measurement of a rifle.

**Six figures stay in view:** center from aim, extreme spread, group width by height, mean radius, and CEP 50 and 90. **With the shot distance set, each is an angle first,** in the unit chosen in Settings, and its size on the paper at that distance is beneath it in smaller type: an angle is what lets a group shot at 25 yards be compared with one shot at 100. MOA is the default; SMOA, an inch at 100 yards, is there for those who think in it, so a 0.422 inch group at 25.4 yards reads 1.59 MOA or 1.66 SMOA. Without a distance the figures are sizes on the paper, and the panel says an angle needs the distance, with a button to set it. A setting puts the size on the paper first, for a shooter who only shoots one distance. **Every figure carries its interval,** the range the true value is likely to lie in, and the percentage it covers: hold the pointer over a figure to see it, with its angle at the distance shot. When you have excluded a shot, the tooltip also gives the figure without the exclusion, so an exclusion is never hidden.

**Advanced** holds everything else, closed until you open it: sigma, the strips across and up and down, the order the shots were fired in, the two cards below, the full CEP table, the sighters, and carrying the correction to another distance. GroupLab remembers whether you opened it.

**Back**, top left, returns to marking with every edit as you left it. The badge beside Show work reads **Scale checked** when the sheet's own markers set the scale; Show work has the detail. **Own window** moves the figures to a window of their own, for a second monitor, and closing that window puts them back.

**The zero correction** gives the group center's offset across and up and down in your length unit, MOA and mil side by side, whichever your scope is marked in, and the distance it is for. Where your rifle records its scope's click value, the line beneath spells it out in clicks with the click value stated: "Dial 2 clicks left and 8 clicks up, at 0.1 mil a click". The clicks are never guessed: a scope that adjusts in quarter minutes and one that adjusts in tenth mils are both common, and assuming either would send you the wrong way. A metric and imperial toggle on the page switches the length unit between inches and centimeters, and changes nothing that is stored.

It says what to dial when the group's center is far enough from the aim to be told from chance. When it is not, it says so and how many shots would settle it. Dialing an offset nobody can distinguish from zero only chases noise.

**The two cards,** in Advanced:
- **Shape** says whether the group is round, as far as its shots can tell. It also says whether it strings vertically, and what that test could have detected. A test on few shots misses most real stringing, so "no evidence" is not evidence of none.
- **Worst shot** says whether the shot furthest out is further than groups of that size usually put their worst. Even when it is, that makes it worth a look, not a flyer. Whether it was called or pulled is yours to say.

**What each "why" says, in plain words:**
- **Zero correction:** the smallest offset these shots can call; how the spread was estimated; and that moving a zero between distances needs the ballistics screen.
- **CEP:** these circles come from the group's sigma, assuming the shots scatter evenly about the center.
- **Shape:** how often a truly round group would look this far from round, and the shape of the group's error ellipse.
- **Worst shot:** how far out the worst shot sits in the group's own mean radii, against where simulated round groups put theirs.
- **Decisions left unmade:** every figure here inherits the decisions still open.

**The full CEP table and the fitted ellipse** are in Advanced, one more click away, and GroupLab remembers whether you opened them. The table gives the CEP at 50, 90, 95 and 99 percent three ways. The fit gives the center and the spread on each axis with their intervals, and the error ellipse.

**Export** writes the complete record as a GroupLab file, or the shot coordinates as CSV for a spreadsheet: one row a shot, across and up from the point of aim in inches, MOA and mil, with the distance in the header. **Import shots from a CSV,** in the menu, reads coordinates exported by other software: it asks which column is across, which is up and down, and what unit they are in, then shows the analysis. There is no image with an import, so the figures are the whole of it.

## 6. Sessions and the report

Accept and analyze saves the sheet as a session: the marking with every edit, its figures, a proof image and its own copy of the sheet. Session records, in the rail, lists them newest first:
- filter them by rifle and by load;
- open one back to its analysis, which needs no image;
- delete one, after GroupLab asks.

![Session records](figures/screens/current/sessions-light-1400x900.png)

The analysis's Report button saves the session as a PDF:
- **Page 1:** the particulars, the plot, the figures with their intervals, the zero correction and the cards.
- **Page 2:** the shot table, the exclusions with their reasons, any decisions left unmade, the registration and every "why".

Every line on it is one the screen shows.

## 7. Comparing loads

Tick two or more sessions in Session records and press Compare the chosen. They appear side by side on the comparison screen, the rail's chart. Each has its plot and its figures with intervals, and below them are the tests with their verdicts.

![Two loads compared](figures/screens/current/compare-light-1400x900.png)

**The loads are never ranked by their figures alone.** When the intervals overlap, the screen says the data do not separate the loads. Every test also says what it could have detected, and the table at the foot gives the shots per load it takes to resolve a smaller difference. Sessions shot at different distances are compared as angles, and the screen says so.

## 8. The zero correction at another distance, and the dope table

The ballistics screen keeps what the solver needs on your rifle and load records:
- the sight height and zero distance;
- the muzzle velocity and its standard deviation;
- the BC, its drag model and its reference atmosphere;
- the bullet's weight, and for spin drift its length, its diameter and the twist.

All of it is optional. A record without what the solver needs says which field is missing.

![The ballistics screen](figures/screens/current/ballistics-light-1400x900.png)

**The dope table** gives drop and the wind of a 10 mph crosswind at each range, in your units and your scope's clicks, in the air you enter. Aerodynamic jump is not modeled, and the table says so.

**At another distance.** On the analysis, the zero correction can be carried to a second distance, with its uncertainty carried with it. An offset that could not be told from zero is not carried.

The ballistics screen also carries the analyzed group to another distance, as a prediction and never a measurement. With neither a velocity
spread nor a crosswind uncertainty given, it is the group scaled by angle and nothing more.

**Hit probability.** Below that, the screen works out the chance of a hit on a circle, a rectangle or an IPSC outline at a distance:
- **Rifle precision** fills itself from the group open in the analysis, or from every saved session of the chosen load pooled, or you type
  it. It is the per axis standard deviation of your shots as an angle, which is sigma, never a group size.
- The muzzle velocity's spread comes from the load, and the **zero error** starts at the uncertainty in your group's center.
- A **confidence preset** sets everything nobody can measure at once, from a known distance with the air measured to a guessed distance
  and a guessed wind. Under Advanced each figure can be edited, with a bias for something you know is off, such as a chronograph reading
  fast.
- The answer sits beside the elevation and the wind for that distance: the **first round**, and the **second round** fired after you saw
  where the first landed and dialed off its miss. Each comes with its interval, and the screen says whether the interval is mostly your
  precision's own uncertainty or the simulation's.
- **What costs the most** lists every error source by the hits it takes away, so you can tell whether to practice wind calls, work on the
  load or buy a rangefinder.
- For a string of several shots on one reading it gives the chance of at least one hit and the hits to expect.
- The simulated impacts are drawn over the target, and a curve shows the chance against distance with its interval as a band.

A wind call is drawn once for a whole string, never per shot, because every shot you fire on one reading shares its error. When the group
behind the precision is too small to say anything, the screen says so and how many shots would make it mean something. The same seed gives
the same answer.

## 9. Keeping GroupLab up to date

GroupLab checks for a new build when it starts and about once a day after that, and tells you in a bar across the top of the window when one is ready. Nothing is downloaded until you ask for it and nothing is installed without you pressing the button.

When you do, GroupLab downloads the installer, checks it against the SHA-256 the release states, and hands it to Windows. The installer is per-user: it never asks for an administrator, it installs under your own account, and it closes and reopens GroupLab around the update. After it comes back, the same bar tells you which version you are now on.

If an update ever fails, the build you had is still installed and still works. Nothing is removed until the new one is in place.

## 10. Comparing several sheets at once

Reading several sheets of one load as one group is **not built yet**. It is not a matter of adding the numbers up: several sheets have several centers, and what a pooled figure means depends on which center you measure from, which is still being decided. Meanwhile **Compare loads** puts the sessions of two or more loads side by side, each with its own figures and intervals.

## 11. If something goes wrong

**Report a problem** is in the settings. It opens the support page, which takes a description and, if you attach one, a report package: your log, the crash record and the marking you were working on. Nothing is sent until you press send, and you can see what is in the package before you do.

GroupLab can also send error reports by itself, once you say so: the first time it can, and in Settings under **Error reports**, you choose automatically, ask each time, or never. A report holds the version, the system, the error and the names of the last things done, never anything you typed, and never a photograph, file name or location. That part is built but not switched on yet. Crash records are written to your own machine whether or not you ever send them. They name the version, the build train and the line it happened on, and they never contain a photograph, a location or anything read out of one.

**Sending in targets.** The page at `grouplab.org/targets/` takes scans and photographs of targets that help GroupLab get better at reading them. You choose how they may be used: testing only, kept by the project and never published, or may be published in GroupLab's public test data and research. GroupLab can also send a target itself once you have analyzed it, with the holes it found and the ones you corrected, and it asks first every time unless you say otherwise in Settings.

## 12. Settings

The gear opens the settings:
- **Units:** length, angle and distance, each chosen on its own. They change only how figures are shown. Beneath them, a box puts a group's size on the paper before its angle.
- **Theme:** dark, light, high contrast, or follow the system.
- **Sharing:** the three things GroupLab may send, in the order the first run screen asks them. **Sending targets:** send every target you analyze to the project, ask each time, or never, and which consent goes with them. **Error reports:** send them automatically, ask each time, or never, and what a report holds. **Hardware survey:** take part or not, what a report holds, the benchmark, and a button that gives this copy of GroupLab a new random number.
- **Log:** how much the diagnostic log records, and where it is.
- **Problems:** a way to report a problem, and any crash records not yet dealt with.

![The settings](figures/screens/current/settings-light-1400x900.png)
