#!/usr/bin/env bash
# Runs the whole fiducial validation suite.  One argument: the scan directory.
set -u
SCANS="${1:?usage: run_all.sh <scan-dir>}"
P=python3

echo "===== 1. false positives on real target artwork ====="
for prof in default strict loose; do $P validate.py falsepos "$SCANS" "$prof" 0; echo; done
echo "===== 1b. same, at reduced resolution (photograph path) ====="
for ds in 1600 900; do $P validate.py falsepos "$SCANS" loose "$ds"; echo; done

echo "===== 2. geometry of the false positives ====="
$P validate.py geometry "$SCANS"; echo

echo "===== 3. minimum Hamming distance, measured ====="
$P validate.py hamming; echo

echo "===== 4. which families both toolchains can read ====="
$P validate.py families; echo

echo "===== 5. cross-family decode: does the AprilTag escape route exist? ====="
for printed in DICT_6X6_250 DICT_4X4_50 DICT_APRILTAG_36h11; do
  for fam in tag36h11 tag25h9 tag16h5; do
    timeout 60 $P validate.py escape "$printed" "$fam" 2>/dev/null \
      || echo "  $printed / $fam : detector aborted"
  done
done
echo

echo "===== 6. detection against printed size and degradation ====="
$P validate.py degrade DICT_6X6_250; echo
$P validate.py degrade DICT_APRILTAG_36h11; echo

echo "===== 7. quiet zone, with hostile artwork outside it ====="
for q in 1 2 3 4; do
  for combo in "DICT_APRILTAG_36h11 apr" "DICT_APRILTAG_36h11 ocv" "DICT_6X6_250 ocv"; do
    set -- $combo
    timeout 60 $P validate.py quiet "$1" "$q" "$2" 2>/dev/null \
      || echo "  $1 q=$q $2 : detector aborted"
  done
done
