# Entry 146: a tour of the application, one page per screen

Written by the planning session at 00:20 Mountain on 2026-09-23. Do this after entries 144 and 145, and before the remainder of entry 143.

Alan: "I think there should be a separate page for screenshots of each section of the application that explains what is happening instead of just a few screenshots on the main page."

A handful of pictures on the home page shows that GroupLab exists. It does not show what using it is like, and that is the question somebody has before they download an unknown program. The screenshot job from entry 144 section 4 already produces the pictures; this entry gives them somewhere to live and something to say.

## 1. The section

`https://grouplab.org/tour/`, linked in the top navigation between Download and Guides. An index page, then one page per screen.

The index gives each screen a card: its name, one sentence saying what it is for, and its own screenshot. A reader should be able to understand the shape of the application from the index alone, and go deeper where they care.

## 2. One page per screen

Eleven screens exist as renders today: analysis, analysis with a sheet open, marking, library, print, equipment, compare, ballistics, sessions, settings, and whatever the twelfth becomes as the interface grows. Give every one its own page, driven by the same list that drives the screenshot job, so a new screen cannot appear in one and not the other.

Each page carries:

1. **The screenshot, large**, light and dark, following the reader's theme, at the desktop size.
2. **What this screen is for**, one short paragraph in plain words, from the shooter's side.
3. **What you are looking at**: a numbered list keyed to the picture, naming the parts and saying what each does. Use a numbered overlay on the image, or a labelled list beneath it where an overlay would crowd the picture. A reader must be able to match every item to something they can see.
4. **What you would do here**, two to five steps, in order, as a person would do them.
5. **Where it fits**, a line linking the screens before and after it in the ordinary flow: print a sheet, shoot it, open the image, mark it, read the analysis, record the session, compare loads.
6. **Links to the related guide and any research article**, where one exists.

## 3. The words

Written for somebody who has never opened GroupLab, and never for somebody who has read the code. No class names, no file paths. Name what is on the screen using the same words the screen uses, so a reader can follow along with the application open beside the page.

Keep each page short: a picture, a paragraph, a numbered list, a few steps. Where a screen needs a long explanation, that explanation belongs in a guide or a research article, and the tour page links to it.

Nothing on these pages may claim a feature that is not in the published build the screenshots came from. Say which build the pictures are from, and let the screenshot job keep that current.

## 4. Keeping it true

1. The pages are built from the same screen list as the screenshot job in entry 144 section 4, so a screen that gains or loses a render fails the build rather than going stale quietly.
2. A test fails if a tour page references a screenshot that is not produced, or if a produced screenshot has no tour page.
3. The screenshots use generated data only: a generated sheet, invented rifles and loads, no material from Alan's range folder and nothing from a submission.
4. When the interface changes enough that a picture is wrong, the screenshot job replaces the picture and the entry that changed the interface should say whether the words need changing too.

## 5. The home page

Once the tour exists, the home page keeps one or two pictures at most and links to the tour rather than trying to be it. Say in your report what you removed.

## 6. Order and effort

This is a page-building job, not an application job, and it must not displace entry 143's queue. If it runs long, publish the index and the three screens that matter most to a newcomer (analysis, marking, print), and add the rest in a second pass.
