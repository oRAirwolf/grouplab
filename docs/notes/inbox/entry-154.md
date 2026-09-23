# 2026-09-23, entry 154: a word a shooter does not know gets an explanation where they meet it

Alan: "I think anything like the word Sigma that is not commonly understood by a layman in either the
website or application should create a tooltip popup that explains what it means."

`docs/GLOSSARY.md` already exists and already does the hard half of this: twelve figures explained in
plain words, written from one list in the source so the page and the screen cannot disagree. This entry
widens it from the figures panel to everywhere the words appear, and adds the words that are not
figures.

## 1. One source, wider scope

Keep the existing arrangement, which is right. Widen what the list holds:

1. Every entry keeps its plain sentence. Add, optionally, a second sentence with the precise definition
   for a reader who wants it, and a link to the research article that covers it where one exists.
2. Add the terms that are not figures. A starting list, which is not exhaustive and you should add to
   it as you sweep:
   sigma, Rayleigh sigma, radial standard deviation, standard deviation, mean radius, CEP, extreme
   spread, group size, MOA, mil, confidence interval, sample size, degrees of freedom, chi squared
   interval, F test, bias correction, c4, dispersion, point of aim, point of impact, zero, zero
   correction, scale, calibration, DPI, perspective correction, keystone, quarter point, doubles,
   review queue, detection, marker, code, bull, subgroup, load, string, hit probability, ballistic
   coefficient, muzzle velocity, velocity SD, drop, wind deflection.
3. A term whose plain sentence needs a term that is itself in the list is fine. Link it.

## 2. On the website

1. The first appearance of a glossary term in the body of a page gets a dotted underline and shows the
   plain sentence on hover **and on tap**. Tap matters: phones have no hover, and a tooltip that only
   works with a mouse is a tooltip that does not work for half the readers.
2. The popup is dismissible, does not cover the sentence it explains, and carries the link to the full
   entry.
3. Only the first appearance per page is marked. A page where every instance of "sigma" is underlined
   is unreadable.
4. A reader with JavaScript off still sees the term as a link to the glossary entry. The tooltip is the
   enhancement, not the mechanism.

## 3. In the application

1. The same terms in the analysis panel, the statistics table, the graph titles and axis labels, the
   review queue and the settings get an information affordance showing the same text from the same
   source.
2. It is reachable by keyboard, not only by pointer.
3. It reads from the same list the website reads from. If the two ever load from different files, this
   entry has not been done.

## 4. Where the word should not be there at all

While sweeping, some of what you find will not need a tooltip, it will need plainer words. A tooltip is
for a term the reader has to learn because it is the name of the thing. It is not a way to keep jargon
that could simply be replaced. Where the plain phrase would do, use the plain phrase and report which
ones you changed.

## 5. Tests

1. A term in the list that appears in the application or on the site with no affordance is a failure,
   and the failure message names the file and the term.
2. A term in the list that appears nowhere is reported, not failed. It may be waiting for a feature.
3. An affordance whose text does not match the list is a failure. That is the drift this is meant to
   prevent.
4. Every entry has a plain sentence that contains no other capitalised jargon and no symbols. A plain
   sentence written in jargon is the usual way a glossary fails.
