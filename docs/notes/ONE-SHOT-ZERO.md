# A one-shot zero on the phone: evaluated, not built

NOTES-FROM-PLANNING.md entry 250 section 4, 2026-09-28. Suggested by Jylee, a friend of Alan's: photograph the zeroing grid after **one**
shot, give the scope's click value and the distance, and GroupLab says how many clicks to adjust. Alan doubted it fits GroupLab's
insistence on sample size and asked that it be evaluated. Entry 252 section 3 ("Shots Needed to Zero", also Jylee's) answers the same
question from the other end: a one-shot zero is its n = 1 row.

## 1. Can it be done?

Most of it exists.

- **Reading the sheet.** The zeroing grids are library sheets; the phone reads them like any other, frozen designs included, and finds
  the hole and its offset from the aim in inches, and in angle once the distance is given.
- **Clicks.** Core turns an offset into whole clicks and what rounding leaves (`Clicks.For`), given a click value.
- **The rule.** Core's zero correction already refuses to dial an offset its interval cannot tell from zero (`Zeroing`, entry 53 section 3).

What is missing:

1. **The click value on the phone.** It lives on the desktop's rifle record; the phone has no rifle records. A field on Capture (0.1 mil,
   1/4 MOA, 1/8 MOA or typed), remembered, is the smallest change.
2. **A sigma for one shot.** The desktop's rule takes the rifle's spread from the shots on the sheet, which needs at least two. With one
   shot there is no spread to measure, so it has to come from somewhere else: the rifle's earlier sessions (which on the phone have no rifle
   attached yet), or a stated typical value.
3. **The zero correction on the phone's result screen.** Core has it; the phone does not show it yet.

## 2. What one shot can honestly say

A single shot's offset from the aim is the zero error plus that shot's own dispersion. With the rifle's per-axis sigma known from earlier
sessions, the offset's 95 percent interval on each axis is the offset plus or minus 1.96 sigma. So:

- **Where the offset is larger than about 2 sigma**, the error is real: give the clicks, with the interval in clicks beside them. For a rifle
  of 0.35 MOA per-axis sigma (roughly a 1 MOA five-shot group) that is any error over about 0.7 MOA, three quarter-MOA clicks.
- **Where it is within about 2 sigma**, say plainly: "this could be the rifle's own spread; fire more before adjusting".
- **With no earlier sessions**, a typical value has to stand in, and the screen says it is a typical value, not this rifle's.

One shot is good for getting on paper and for large errors. It cannot give a fine zero: to resolve half a click the center's interval has to
shrink to about half a click, which at that sigma takes dozens of shots, and entry 252's simulation shows how slowly "on the closest click"
converges.

## 3. A design that fits GroupLab: "Zero, step by step"

The one-shot case becomes the first step of the normal group zero rather than a shortcut beside it.

1. **Shot 1:** a rough correction only if the offset is clearly outside the rifle's spread; otherwise "fire another".
2. **Each further shot**, on the same grid photographed again or on the next sheet: the group's center, the suggested clicks and their
   interval update. From the second shot the sheet's own spread is used, pooled with the rifle's earlier sessions where there are some.
3. **The screen says when adding shots stops changing the answer:** when the interval on each axis is inside half a click, or when "Shots
   Needed to Zero" says the next goal needs more shots than are worth firing.

**What it would take to build:** the click value on the phone (1 above); a rifle chosen on the phone, so earlier sessions give a sigma; the
zero correction on the result screen with its interval in clicks; photographing the same sheet again and counting only the new holes, which
the marking already does by reading every hole each time; and the step-by-step screen itself. The camera flow and the sheet reading are
there.

## 4. Credit

Jylee, by name (Alan, 2026-09-28).
