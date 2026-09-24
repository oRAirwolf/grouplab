# 2026-09-24, entry 162: consent for the 2026-09-23 friend scan, and what it was shot on

A short entry that amends entry 161. Fold it in beside 161, or after it if 161 is already actioned.

## 1. Consent

Alan, on 2026-09-24, relaying his friend's answer: "Yes he is willing to have his target used as a test
fixture and yes it can be published."

So `Scan_20260923.png` may be:

1. used as a test fixture, and
2. published, including in `samples/` and in research articles.

Write a consent record for it in `samples/PROVENANCE.md` in the same form as scan 3's, with Alan's
words above quoted exactly, the date 2026-09-24, and the fact that consent was relayed by Alan on the
friend's behalf. The friend is not named. Entry 161 section 8 item 1 said to keep it local until this
existed; that restriction is now lifted.

**This does not change the rule for the 2026-09-16 friend scan.** That one is still never published.
Two scans from the same friend with different consent is exactly the case where one gets published by
mistake, so make the provenance record for each say which one it is by filename and by date.

Strip every metadata block from the published copy before it goes anywhere, as the research build
already enforces. Never read, print or log any GPS, location or timestamp metadata from the original.

## 2. What it was shot on

- **Paper:** card stock.
- **Backing:** cardboard.
- **Cartridge:** 6.5 Creedmoor, 0.264 in.
- **Printing:** no scaling selected; GroupLab reported 100.3 percent.

Record these against the scan wherever its ground truth lives.

## 3. What this means for the ratio

Alan: "I dont think the constant you have measured is actually a constant and it depends on the
material that is being shot and the weight of the paper."

That is the position entry 161 section 5 already takes, and his information strengthens it. Heavy card
stock over a cardboard backer is a plausible explanation for a hole measuring 1.14 times the bullet, and
the earlier scans that measured 0.76 to 0.95 were on different paper and backing. The finding to record
is that **hole size over calibre depends on paper weight and backing at least**, with velocity and
nose shape not yet separable from them.

Two consequences:

1. **No constant replaces 0.945.** Entry 161 already says so. Where any code path still needs a
   relationship between calibre and hole in the absence of the sheet's own marks, it uses a range wide
   enough to include both 0.76 and 1.14, and says that it is doing so.
2. **Paper and backing become recorded fields.** Add them to the capture record for a target, as
   optional fields with plain choices such as copy paper, card stock and other, and a backing field
   with cardboard, foam board, none and other. Without them, every future measurement of this ratio is
   confounded in the same way the existing ones are, and entry 158 program B's controlled test needs
   exactly these two recorded for every sheet.
