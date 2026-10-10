---
title: "What a thermal printer is sent: the Phomemo M834, decoded"
description: "The Phomemo app printed a Letter sheet 5.3 percent small and cut its last line in half. How GroupLab recorded what the app sends to the M834, rebuilt it, found that the printer feeds about 0.7 percent short, and stretched its pages to match. Printing from a computer is built but has not been tried on a real printer."
group: How GroupLab is built
number: 34
written: 2026-10-10
data_date: "One recording of the Phomemo app's print, 2026-10-05; three check prints on one M834, 2026-10-07, 2026-10-08 and 2026-10-10"
samples: "One recorded print of one page (136,992 bytes), and three printed check pages from one printer on one roll, read with a caliper, rulers and a 600 dpi scan"
state: published
no_figure: "The findings are a recording's bytes and lengths read with a caliper and rulers; they are in the article as tables, and a picture of a thermal page would not show a 0.7 percent difference."
found: "The Phomemo app, given a Letter sheet, printed it at 94.7 percent of its size and cut the last line in half. The M834 takes its page as 255 compressed blocks over a Bluetooth serial port, and answers when the page has printed. It feeds about 0.7 percent short along the paper, so GroupLab draws its pages 0.7 percent longer. The third check print came out true both ways: 150.0 by 150.0 mm on the caliper lines, and 190 and 250 mm on the rulers."
sure: "one printer, one roll, one recording and three check prints. The printer's own settings are sent as the app sent them and what each one does is not known. Printing from a computer is built and has not been tried on a real printer."
sources:
  - "The recording and the rebuilt commands, docs/PHASE1-RESULTS.md, \"Request 73: the Phomemo M834's print commands, decoded and proved (2026-10-05)\"."
  - "The computer's route and the wait for the printer's answer, docs/PHASE1-RESULTS.md, \"Entry 389\", and the code in src/GroupLab.Core/Printing/Labels (M834Print, PrinterFinish, PhomemoLzoEncoder, Lzo1x)."
  - "The feed measurement and the stretch, docs/PHASE1-RESULTS.md, \"Entry 391: the M834 feeds short, so its pages are drawn longer along the feed\", and \"Requests 84, 85 and 88 answered (2026-10-10)\"."
---

## Why look inside a printer

A GroupLab target is a measuring instrument, and the printer is the first thing that can spoil it. [Does your printer print at true size?](/research/printer-true-size/) is the shooter's side of this. This article is the other side: what one thermal printer, the Phomemo M834, is actually sent, and what GroupLab learned by reading it.

The reason to look was a measurement. The Phomemo app, given a Letter sheet, did not print it at its size.

## What the app does to a sheet

The developer recorded one print from the Phomemo app, with Android's Bluetooth log, and GroupLab rebuilt the page from the bytes. The rebuilt page was GroupLab's C bull sheet, its identifier readable. The PDF the developer had printed was then laid over it at 300 dpi.

| | Result |
|---|---|
| Scale at which the PDF lies over the printed page | 94.675 percent |
| Black dots shared at that scale | 96.8 percent |
| All dots that differ at that scale | 0.84 percent |
| Black dots shared at 100 percent | 34 percent |

So the app printed the Letter sheet 5.3 percent small, and cut its last line in half. A target printed through that app is the wrong size whatever the scale setting said. That is the measured case for GroupLab sending the page itself.

## What the printer is sent

The recording held two devices. About 20 KB of Bluetooth Low Energy writes belonged to a watch; its own bytes name it. The page, 136,992 bytes, went to the M834 over classic Bluetooth's serial port (RFCOMM channel 1). The M834 also offers a Low Energy link, which the app did not use. That is the way an iPhone would need, and in the record it is not learnt yet.

The page itself:

- A run of status questions and settings, then a reset.
- The raster header of ESC/POS, the common receipt-printer language: 316 bytes (2,528 dots) across by 3,294 lines.
- Not raw rows. The page follows as 255 blocks compressed with LZO1X, each block the next 4,096 bytes of the page (the last, 520), with its packed length in three bytes. Unpacked, that is 1,040,904 bytes, exactly 316 by 3,294.

Which of the settings is darkness, which is speed and which is paper is not known. GroupLab sends them as the app did.

GroupLab wrote its own LZO1X compressor and a decompressor to check it. Its decompressor reads the app's whole recording to the same page as an independent Python one does, the Python one reads GroupLab's stream back to the same page, and 300 random round trips agree. GroupLab's stream for the page was 158 KB where the app's was 137 KB: its compressor packs less tightly than the app's.

## The printer sets the pace, and says when it is done

The first direct print gave a strip of a few millimetres and stopped. The cause was in the recording. Android takes a whole page into its own buffer in a few milliseconds, but the printer then draws it across the link one frame at a time, as it has room: it granted one credit per 666-byte frame, and 139 KB took 22.6 seconds to cross. GroupLab had closed the link 2 seconds after its last write, which cut the page off.

In the recording the Phomemo app kept the link open until the printer's only answer after the page, three bytes, 22.5 seconds after the last frame. GroupLab takes that answer as "printed" and holds the link until it comes, or until a time limit passes, or until the person presses Cancel.

## The printer feeds short

A page the printer receives correctly can still come out the wrong length. The M834's head is 300 dpi, so across the sheet the dots fix the size. Along the paper, only the feed rollers do.

Over two check prints, five readings of the length along the feed, against what GroupLab drew, gave 99.19, 99.19, 99.34, 99.19 and 99.49 percent, with a mean of about 99.3. GroupLab therefore uses 99.30 percent and draws every M834 page 1 / 0.9930, or 1.0070 times, longer along the feed. Across the head nothing is changed.

A third check print was made after that change. Read by caliper, by ruler and by a 600 dpi scan:

| Length | Designed | Read |
|---|---|---|
| Crosshairs across, caliper | 150.0 mm | 150.0 mm |
| Crosshairs down, caliper | 150.0 mm | 150.0 mm |
| Bottom ruler | 190 mm | 190 mm by ruler; 189.93 mm by scan |
| Side ruler | 250 mm | 250 mm by ruler; 249.93 mm by scan |
| Crosshairs across, scan | 150.0 mm | 149.98 mm |
| Crosshairs down, scan | 150.0 mm | 150.07 mm |

By every instrument the printer now prints true both ways to within 0.05 percent. Against what was drawn, the four readings along the feed on that print were 99.31, 99.32, 99.36 and 99.29 percent. With the first five, the nine average 99.30, so the stretch stays as it is.

The paper also tracks a little sideways: the top and right caliper lines meet 0.15 degrees off square on the third print and 0.22 on the second, about 0.65 mm over 250 mm of feed. That is recorded and not corrected.

## What is not known

- **Printing from a computer is built and has not been tried.** On Windows a paired M834 is a Bluetooth serial port, and GroupLab writes the same bytes into it that the phone sends. A test that sends the check sheet through a fake port finds exactly the phone route's bytes. No real M834 has printed from a computer yet, so nothing here says it works. On macOS and Linux GroupLab says printing is not available yet.
- **An iPhone has not been tried**, and the Low Energy link that route would need is not learnt.
- **One printer, one roll.** Another M834, or another roll, may feed differently. That is the reason GroupLab keeps a printer check that a person can run.
- **What each setting does** is not known, so darkness cannot yet be chosen.
- **The caliper is a spread, not a bias.** Across, it read 0.30 percent high on the first print and 0.21 percent low against the drawing on the second, from pages drawn the same, so a single caliper reading is good to a few tenths of a percent and no better.

## What this means

**For a shooter:** if you print a target through a printer's own app, measure it. An app that fits the page to the paper prints it small, and here that was 5.3 percent. How to check is in [Does your printer print at true size?](/research/printer-true-size/).

**For a developer:** a thermal printer's protocol can be rebuilt from one recording, and a rebuilt page checked against the document it was meant to print. Check the length along the feed as well as across, and wait for the printer's own answer before closing the link.
