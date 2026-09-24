# What the words mean

Every figure GroupLab shows, and every word a shooter may not know, in plain words. The same text appears wherever the word
does, in the application and on the website, so no two places can say different things: all are written from one list,
`src/GroupLab.Core/Marking/glossary.json`.

**Every one of these is an estimate from the shots you fired.** That is not a disclaimer, it is the single most useful thing
to know about them. One five shot group is not a measurement of a rifle; it is one sample of what the rifle does, and every
figure below moves about from group to group. Where an interval is shown beside a figure, that interval is the honest width
of what you actually know.

## Ballistic coefficient

<a id="ballistic-coefficient"></a>

A number that says how well a bullet keeps its speed against the air. A higher one slows less, drops less and drifts less in the wind. It is printed on the box or in the maker's data.

## Bias correction

<a id="bias-correction"></a>

A small adjustment that stops a spread figure worked out from a few shots from reading too small on average. Without it, small groups would look slightly better than the rifle really shoots.

## Bull

<a id="bull"></a>

One aiming mark on a target, usually a set of rings. A load development sheet has many, one shot fired at each, and GroupLab measures every shot from its own bull.

## Bullet drop

<a id="bullet-drop"></a>

How far the bullet falls below the line of the barrel on its way to the target. The ballistics screen gives it at each distance in your units and your scope's clicks.

## CEP

<a id="cep"></a>

The radius of a circle that would hold that share of your shots: the 50 percent circle holds half of them, the 90 percent circle nine in ten. It answers where the next shot will go rather than how big this group was. It is estimated from your shots, so it is less certain with fewer of them.

*Precisely:* Circular error probable: the radius about the group's center that the given share of shots falls inside, reckoned from sigma under a circular normal model.

## Calibration

<a id="calibration"></a>

Checking a measurement against something of known size, and correcting for any difference. GroupLab checks the scale against the sheet's own printed markers every time it reads one.

## Center from aim

<a id="centre-from-aim"></a>

How far the middle of your group is from where you aimed, and in which direction. This is what a zero correction is worked out from. With few shots the center itself is uncertain, so a small offset may be the group moving about rather than the rifle being off.

## Chi squared interval

<a id="chi-squared-interval"></a>

The way the interval for a spread figure such as sigma is worked out. It accounts for the spread of a small group being uncertain in a lopsided way: the true value is more likely to be larger than what you measured than smaller.

## Circularity test

<a id="likelihood-ratio-test"></a>

The check of whether a group is really stretched or only looks it by chance. It compares how well a round pattern and a stretched one each explain the shots.

## Comparing loads

<a id="load-comparison"></a>

The check of whether two or more loads put their groups in different places, beyond what chance would give. Like every comparison on few shots, it can only see large differences.

## Confidence preset

<a id="confidence-preset"></a>

One choice that sets every uncertainty you cannot measure at once, from a known distance with the air measured to a guessed distance with a guessed wind. Change any one of them and the choice becomes custom.

## Correlated normal and Grubbs-Patnaik

<a id="correlated-normal"></a>

Two ways of working out a circle's size for a group that is not round. They follow the group's own shape, where the plain circle assumes it is round, and neither gives an interval here.

## DPI

<a id="dpi"></a>

Dots per inch: how many pixels a scanner records for each inch of paper. A scan at 600 dots per inch shows a bullet hole clearly enough to measure its center to a few thousandths of an inch.

More in [the research article](https://grouplab.org/research/scans-against-photos/).

## Degrees of freedom

<a id="degrees-of-freedom"></a>

How many independent pieces of information an estimate rests on, which is usually a little fewer than the number of shots. Working out the group's own center uses some of them up. Fewer of them means a wider interval.

## Detection

<a id="detection"></a>

GroupLab finding the holes on a sheet by itself. It compares the scan with what the sheet should look like when unshot, so anything printed is ignored and anything new is a candidate hole.

More in [the research article](https://grouplab.org/research/how-grouplab-reads-a-target/).

## Dispersion

<a id="dispersion"></a>

How scattered the shots are, whatever figure is used to measure it. It is the property of a rifle and load that all the group figures try to capture.

## Doubles

<a id="doubles"></a>

Two shots that went through nearly the same place and left one ragged hole. GroupLab flags a mark that is larger than one hole should be, and asks you to settle whether it is one shot or two.

More in [the research article](https://grouplab.org/research/one-hole-or-two/).

## Error ellipse

<a id="error-ellipse"></a>

An oval drawn to hold a given share of the shots, stretched and turned to follow the group's own shape. It describes a group that is not round better than a circle can.

## Extreme spread

<a id="extreme-spread"></a>

The distance between the two shots furthest apart. It is the number most people quote, and the least reliable one, because it uses only two shots and throws the rest away. Adding shots can only make it larger, so groups of different sizes cannot be compared by it at all.

More in [the research article](https://grouplab.org/research/wind-or-rifle/).

## F test

<a id="f-test"></a>

A comparison of the spread of two groups, asking whether one load is really tighter than the other or the difference is chance. On small groups it can only see large differences, which is why it also says what size of difference it could have detected.

## First and second round

<a id="first-and-second-round"></a>

The first round is the shot fired on your first reading of the wind and the range. The second is fired after you saw where the first one landed and moved your aim by its whole miss, which takes out the reading's errors but carries the first shot's own spread with it.

## Flyer

<a id="flyer"></a>

A shot well away from the rest of the group, often from a known cause such as a pulled trigger. Only you can say whether a shot was a flyer; GroupLab only says whether it is unusual for a group of that size.

## Group size

<a id="group-size"></a>

How big a group of shots is, which can mean several different measurements. GroupLab reports the mean radius first because it is the steadiest, and the extreme spread beside it because it is the one most people know.

More in [the research article](https://grouplab.org/research/wind-or-rifle/).

## Group width by height

<a id="width-by-height"></a>

How wide and how tall the group is, from the leftmost shot to the rightmost and the lowest to the highest. It shows at a glance whether a group is stretched sideways or up and down. Like the extreme spread it rests on the outermost shots, so it grows as you fire more.

## Hit probability

<a id="hit-probability"></a>

How likely a shot is to land inside a target of a given size at a given distance, worked out from the group's spread, the rifle's predicted path and the errors you cannot measure. It is a range, because the spread it starts from is an estimate.

## Keystone

<a id="keystone"></a>

The shape a rectangle takes when photographed from off to one side: wider at one end than the other. It is one of the distortions perspective correction removes.

## Load

<a id="load"></a>

One recipe of cartridge: the bullet, the powder and its charge, the primer and the case. Load development is shooting several of them to find the one a rifle groups best.

## MOA

<a id="moa"></a>

A minute of angle, a sixtieth of a degree. At 100 yards it covers about 1.047 inches, and it grows in proportion to the distance, so it lets groups shot at different distances be compared. Many scopes adjust in quarter or eighth minutes.

*Precisely:* One sixtieth of a degree of arc.

## Marker

<a id="marker"></a>

One of the small black and white squares printed on a GroupLab sheet. GroupLab finds them in a scan or photograph to work out exactly where the sheet is and how big it is.

More in [the research article](https://grouplab.org/research/choosing-the-markers/).

## Mean radius

<a id="mean-radius"></a>

The average distance from each shot to the center of the group. It uses every shot, so it is the steadiest measure of how well a rifle and load shoot, and it changes less from group to group than the extreme spread does. With few shots it is still an estimate: the interval beside it says how much it could move if you shot the same group again.

*Precisely:* The arithmetic mean of each shot's radial distance from the group's own center.

## Mil

<a id="mil"></a>

A milliradian, a thousandth of the distance to the target. At 100 meters it covers 10 centimeters, and at 100 yards about 3.6 inches. Many scopes adjust in tenths of a mil.

*Precisely:* One thousandth of a radian of arc.

## Muzzle velocity

<a id="muzzle-velocity"></a>

How fast the bullet leaves the barrel, usually measured with a chronograph a few feet in front of the muzzle. Together with the ballistic coefficient it decides how far the bullet drops at each distance.

## Per shot and per string

<a id="per-string"></a>

Whether an error changes from one shot to the next or stays the same for a whole string. The rifle's own spread and the muzzle velocity change every shot. A wind call, a range estimate and the zero are the same for every shot fired on them, so they make a string miss together.

## Perspective correction

<a id="perspective-correction"></a>

Undoing the way a photograph taken at an angle makes the near side of a target look bigger than the far side. GroupLab does it from the printed markers, so every part of the sheet is measured at the same scale.

## Point of aim

<a id="point-of-aim"></a>

Where you aimed on the target. Each shot is measured from its own bull's point of aim, which is how shots on many bulls become one group.

## Point of impact

<a id="point-of-impact"></a>

Where a shot actually lands. The difference between where the shots land on average and where you aimed is what a zero correction removes.

## Printed code

<a id="printed-code"></a>

The square pattern printed on a GroupLab sheet that says which sheet it is. GroupLab reads it so it knows where every bull is without being told.

## Quarter point

<a id="quarter-point"></a>

The size a quarter of the way up from the smallest marks on a sheet. GroupLab uses it to judge what one hole looks like on that sheet, because it is steadier than the smallest mark and not pulled up by the marks that are really two holes.

## Range estimation error

<a id="range-error"></a>

How far the distance you dial for may be from the true distance to the target. A rangefinder makes it small and a guess makes it large, and at long range it moves the shot up or down.

## Registration

<a id="registration"></a>

Lining the picture up with the sheet's own layout, from its printed markers. Once it is registered, GroupLab knows where every bull is and how big every distance is.

More in [the research article](https://grouplab.org/research/how-grouplab-reads-a-target/).

## Review queue

<a id="review-queue"></a>

The list of things GroupLab was not sure about on a sheet, such as a mark that may be two holes or a shot that may belong to another bull. It keeps them until you settle each one, and every figure that rests on one says so.

## Rifle precision

<a id="rifle-precision"></a>

How tightly a rifle and load group, as the spread of the shots on each axis stated as an angle, so it holds at any distance. GroupLab fills it from a group you measured, which is the same figure as sigma, rather than from a group size remembered from a good day.

*Precisely:* The per-axis standard deviation of the shots about their own center, in mrad; for circular dispersion it is the Rayleigh sigma, and a radial figure such as the mean radius is about 1.25 times it.

## SMOA

<a id="smoa"></a>

An inch at 100 yards, two inches at 200: the way many shooters think of a minute of angle. A true minute is 1.047 inches at 100 yards, so this unit reads about 5 percent higher. A 0.422 inch group shot at 25.4 yards is 1.66 in it, and 1.59 true minutes.

*Precisely:* One inch of height at 100 yards, 1/3600 of the distance.

## Sample size

<a id="sample-size"></a>

How many shots a figure was worked out from. It decides how far any figure can be trusted, more than anything else does. Five shots is a small sample, and twenty or more is where most figures start to settle.

## Scale

<a id="scale"></a>

How GroupLab turns distances in the picture into inches on the target. On a GroupLab sheet it comes from the printed markers; on any other target you set it with a ruler length or a known rectangle. Every measurement depends on it.

## Shape

<a id="aspect-ratio"></a>

Whether the group is round or stretched in one direction, and by how much. A stretched group can mean wind, a bipod loading unevenly, or nothing at all. With few shots a round pattern often looks stretched by chance, which is why the test beside it says how surprising the shape actually is.

## Sighter

<a id="sighter"></a>

A bull for shots fired to check where the rifle is hitting before the shots that count. GroupLab keeps them apart from the group unless you ask it to measure them.

## Sigma

<a id="sigma"></a>

The spread of the shots around their center, in the same units as the group. It describes the pattern the shots are drawn from rather than the particular shots you fired. Like every figure here it is estimated from the shots you have, so fewer shots means a wider interval.

*Precisely:* The scale of the circular normal distribution fitted to the shots, estimated from the sum of squared radii with a small sample bias correction.

## Standard deviation

<a id="standard-deviation"></a>

A measure of how far values typically sit from their average. For shots it is worked out along one direction at a time, across or up and down. A small one means the values bunch together.

## String

<a id="string"></a>

A run of shots fired one after another and recorded together, usually on a chronograph. GroupLab lines a string's speeds up with the shots on the sheet only where you confirm the order.

## Stringing

<a id="stringing"></a>

Shots spread out mostly in one direction, usually up and down. It can come from changing speeds, a barrel heating, or how the rifle is held. With few shots it is hard to tell from chance, which is why GroupLab says what the test could have seen.

## Stringing test

<a id="pitman-morgan-test"></a>

The check of whether the shots spread more up and down than across, beyond what chance would give. It is a different question from whether the group is round, so it is reported separately.

## Subgroup

<a id="subgroup"></a>

A part of the shots on a sheet treated as a group of its own, such as the shots of one load when a sheet carries several. Each is measured separately.

## The interval

<a id="confidence-interval"></a>

The range the true value is likely to lie in, given how many shots you fired. A wide interval does not mean the measurement is wrong; it means a handful of shots cannot pin it down. Firing more shots narrows it, and nothing else does.

## Trials and seed

<a id="trials"></a>

How many strings the chance is worked out from, each with its own random errors of the sizes you gave, counting how often the shot lands on the target. More trials make the answer steadier, and the same seed repeats a run exactly, so a result can be checked.

## True size range

<a id="true-size"></a>

What the group would likely measure if you fired it again, given what these shots show. It is wider than the group you actually shot, because the group you shot is one sample of what the rifle does. This is the honest answer to how well it shoots.

## Velocity spread

<a id="velocity-sd"></a>

How much the speed changes from shot to shot, as a standard deviation. Small differences in speed become differences in height at long range. On ten shots or fewer it is itself very uncertain.

More in [the research article](https://grouplab.org/research/primer-comparison/).

## Wind call

<a id="wind-call"></a>

Your reading of the crosswind before you fire, and how far the true wind may be from it. You make it once and fire on it, so every shot on that reading shares the same error, and at long range it usually costs more hits than anything else.

## Wind deflection

<a id="wind-deflection"></a>

How far a crosswind pushes the bullet sideways by the time it reaches the target. It grows quickly with distance, and faster for a bullet that slows more.

More in [the research article](https://grouplab.org/research/wind-or-rifle/).

## Worst shot

<a id="worst-shot"></a>

How far the furthest shot was from the center, measured in mean radii. It says whether one shot was unusual for this group rather than whether it was a flyer, which is a judgment only you can make. Expect the worst of twenty shots to be further out than the worst of five, simply because there are more of them.

## Zero

<a id="zero"></a>

The distance at which your scope's aim and the bullet's path meet, so a shot lands where you aimed. A rifle zeroed at 100 yards lands low beyond it and slightly high before it.

More in [the research article](https://grouplab.org/research/blank-sheet-zero/).

## Zero correction

<a id="zero-correction"></a>

How far to move your scope so the group's center lands where you aimed, in the units your scope adjusts in. It is worked out from where the group is now, so it is only as good as the center it came from: with a handful of shots, dialing a small correction can easily move you further from where you want to be.

More in [the research article](https://grouplab.org/research/blank-sheet-zero/).

## Zero error

<a id="zero-error"></a>

How far the rifle's zero may be from where you believe it is. GroupLab starts it at the uncertainty in the measured center of your group, because that is how well the group can tell where the rifle points, and every shot shares it.

