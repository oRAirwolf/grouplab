# GroupLab fiducial validation

Measures the four things FIDUCIAL-DECISION.md could otherwise only argue about.
Needs `opencv-contrib-python` and, for the escape-route and quiet-zone checks,
`pupil-apriltags`. Neither needs a printer or a scanner.

```
./run_all.sh ../../scans
```

## What each check answers

| Command | Question |
|---|---|
| `falsepos <scan-dir> [profile] [downscale]` | How many markers does the detector hallucinate from real printed target artwork? Every scan in the corpus carries none, so every detection is a false positive |
| `geometry <scan-dir>` | Are the false positives degenerate quads? If so a shape gate kills them independently of the dictionary |
| `hamming` | Minimum inter-marker Hamming distance, computed from rendered markers rather than quoted |
| `families` | Which families can both OpenCV and libapriltag read? That intersection is the choice |
| `escape <printed> <detector-family>` | Can a BSD-licensed AprilTag detector read an ArUco marker? |
| `degrade <family>` | Smallest printed size each detector still reads, under blur, noise and tilt |
| `quiet <family> <modules> <detector>` | Is the 1.0 mm quiet zone enough with hostile artwork right outside it? |

## Results, as of 13 September 2026

- **Zero false positives at default detector parameters**, across sixteen files and
  nine dictionaries, with 665 quad candidates rejected per dictionary.
- Under a deliberately permissive detector, `DICT_4X4_100` gives 3 false positives
  and `DICT_5X5_100` gives 1. `DICT_6X6_250`, `DICT_7X7_250` and `APRILTAG_36h11`
  give none. Every false positive is a fragment of a printed ring arc.
- Those false positives are slivers: worst side ratio 4.57, largest area 1366 px2
  against a real 4.0 mm marker's 8928 px2 at 600 DPI.
- **libapriltag 3.1.0 ships no ArUco family.** The escape route FIDUCIAL-DECISION.md
  originally relied on does not exist, which is why the family changed to `tag36h11`.

## A note on the Hamming computation

`hamming` reads the bits back out of a rendered marker image. That is slower than
unpacking `Dictionary.bytesList` and it is the reason the numbers are right: the
packed representation is rotation-interleaved, and an earlier attempt at unpacking
it returned a minimum distance of 1 for `DICT_4X4_50`, whose true distance is 4.

## Reproducibility

Run against OpenCV 5.0.0 and libapriltag 3.1.0. The `escape` and `quiet` checks take
one family combination per process because the AprilTag binding aborts when several
family tables are allocated in one process; `run_all.sh` drives that from the shell.
