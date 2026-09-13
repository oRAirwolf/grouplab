#!/usr/bin/env python3
"""GroupLab fiducial validation harness.

Answers the four questions FIDUCIAL-DECISION.md could only reason about, with
measurements rather than argument.  Requires opencv-contrib-python and, for the
escape-route and quiet-zone checks, pupil-apriltags.

  python3 validate.py falsepos  <scan-dir> [profile] [downscale]
  python3 validate.py geometry  <scan-dir>
  python3 validate.py hamming
  python3 validate.py families
  python3 validate.py escape    <printed> <detector-family>
  python3 validate.py degrade   <family>
  python3 validate.py quiet     <family> <quiet-modules> <detector>

The AprilTag detector allocates a family table per Detector and the binding
crashes if several are created in one process, so `escape` and `quiet` take one
combination per invocation and are driven from the shell.  `run_all.sh` does
that and collects the output.
"""
import sys, os, glob, json
import numpy as np
import cv2

DICTS = {
    "DICT_4X4_50":         cv2.aruco.DICT_4X4_50,
    "DICT_4X4_100":        cv2.aruco.DICT_4X4_100,
    "DICT_5X5_100":        cv2.aruco.DICT_5X5_100,
    "DICT_6X6_250":        cv2.aruco.DICT_6X6_250,
    "DICT_7X7_250":        cv2.aruco.DICT_7X7_250,
    "DICT_APRILTAG_16h5":  cv2.aruco.DICT_APRILTAG_16h5,
    "DICT_APRILTAG_25h9":  cv2.aruco.DICT_APRILTAG_25h9,
    "DICT_APRILTAG_36h10": cv2.aruco.DICT_APRILTAG_36h10,
    "DICT_APRILTAG_36h11": cv2.aruco.DICT_APRILTAG_36h11,
}

# families the AprilTag reference implementation (libapriltag 3.1.0) ships
LIBAPRILTAG = {"tag16h5", "tag25h9", "tag36h11", "tagCircle21h7",
               "tagCircle49h12", "tagCustom48h12", "tagStandard41h12",
               "tagStandard52h13"}


def params(profile):
    p = cv2.aruco.DetectorParameters()
    if profile == "strict":
        p.errorCorrectionRate = 0.0
        p.maxErroneousBitsInBorderRate = 0.0
        p.polygonalApproxAccuracyRate = 0.02
    elif profile == "loose":
        p.errorCorrectionRate = 1.0
        p.maxErroneousBitsInBorderRate = 0.6
        p.adaptiveThreshWinSizeMax = 53
    return p


def load(path, downscale=0):
    img = cv2.imread(path, cv2.IMREAD_GRAYSCALE)
    if img is None:
        return None
    if downscale and max(img.shape) > downscale:
        f = downscale / max(img.shape)
        img = cv2.resize(img, None, fx=f, fy=f, interpolation=cv2.INTER_AREA)
    return img


def scans(d):
    return sorted(glob.glob(os.path.join(d, "*.jpg"))) + \
           sorted(glob.glob(os.path.join(d, "*.png")))


# ---------------------------------------------------------------- false positives
def cmd_falsepos(argv):
    """Every detection is a false positive: no scan in the corpus carries a marker."""
    d = argv[0]
    profile = argv[1] if len(argv) > 1 else "default"
    ds = int(argv[2]) if len(argv) > 2 else 0
    rows = []
    for dname, dval in DICTS.items():
        det = cv2.aruco.ArucoDetector(cv2.aruco.getPredefinedDictionary(dval),
                                      params(profile))
        for p in scans(d):
            img = load(p, ds)
            if img is None:
                continue
            c, ids, rej = det.detectMarkers(img)
            rows.append(dict(file=os.path.basename(p), dict=dname,
                             accepted=0 if ids is None else len(ids),
                             ids=[] if ids is None else sorted(int(i) for i in ids.ravel()),
                             candidates=len(rej)))
    print(f"profile={profile} downscale={ds or 'none'}")
    print(f"{'dictionary':22s} {'files':>5s} {'accepted':>9s} {'candidates':>11s} {'files w/ FP':>12s}")
    for dname in DICTS:
        sub = [r for r in rows if r["dict"] == dname]
        print(f"{dname:22s} {len(sub):5d} {sum(r['accepted'] for r in sub):9d} "
              f"{sum(r['candidates'] for r in sub):11d} "
              f"{sum(1 for r in sub if r['accepted']):12d}")
    for r in rows:
        if r["accepted"]:
            print(f"  FP  {r['dict']:22s} {r['file']:58s} {r['accepted']:3d} ids {r['ids'][:12]}")
    return rows


# ---------------------------------------------------------------- FP quad geometry
def quadstats(q):
    s = [float(np.linalg.norm(q[i] - q[(i + 1) % 4])) for i in range(4)]
    d = [float(np.linalg.norm(q[0] - q[2])), float(np.linalg.norm(q[1] - q[3]))]
    a = 0.5 * abs(float(np.cross(np.append(q[2] - q[0], 0), np.append(q[3] - q[1], 0))[2]))
    return min(s), max(s), max(s) / max(min(s), 1e-6), min(d) / max(d), a


def cmd_geometry(argv):
    """Are the false positives degenerate quads?  If so, a shape gate kills them."""
    d = argv[0]
    print(f"{'file':40s} {'dict':20s} {'minside':>8s} {'sideratio':>9s} {'diagratio':>9s} {'area':>8s}")
    worst = []
    for dname in ("DICT_4X4_50", "DICT_4X4_100", "DICT_5X5_100"):
        det = cv2.aruco.ArucoDetector(cv2.aruco.getPredefinedDictionary(DICTS[dname]),
                                      params("loose"))
        for p in scans(d):
            img = load(p)
            if img is None:
                continue
            c, ids, _ = det.detectMarkers(img)
            if ids is None:
                continue
            for k in range(len(ids)):
                mn, mx, sr, dr, a = quadstats(c[k][0])
                worst.append((sr, dr, a, mn))
                print(f"{os.path.basename(p)[:40]:40s} {dname:20s} {mn:8.1f} {sr:9.2f} {dr:9.2f} {a:8.0f}")
    if worst:
        print(f"\nworst side ratio {max(w[0] for w in worst):.2f}, "
              f"worst diagonal ratio {min(w[1] for w in worst):.2f}, "
              f"largest area {max(w[2] for w in worst):.0f} px2, "
              f"largest minimum side {max(w[3] for w in worst):.1f} px")
    print("a real 4.0 mm marker at 600 DPI is %.1f px on a side, area %.0f px2"
          % (4.0 / 25.4 * 600, (4.0 / 25.4 * 600) ** 2))


# ---------------------------------------------------------------- code distance
def marker_bits(dd, i):
    ms, cell = dd.markerSize, 8
    img = cv2.aruco.generateImageMarker(dd, i, (ms + 2) * cell)
    return np.array([[1 if img[(r + 1) * cell + cell // 2,
                                (c + 1) * cell + cell // 2] > 127 else 0
                      for c in range(ms)] for r in range(ms)], np.uint8)


def min_hamming(dd):
    n = dd.bytesList.shape[0]
    B = [marker_bits(dd, i) for i in range(n)]
    F = np.array([b.flatten() for b in B])
    best = 10 ** 6
    for r in range(4):
        R = np.array([np.rot90(b, r).flatten() for b in B])
        for i in range(n):
            dv = (F[i] != R).sum(axis=1)
            dv[i] = 10 ** 6
            best = min(best, int(dv.min()))
    return best


def cmd_hamming(argv):
    """Minimum inter-marker Hamming distance, computed from rendered markers.

    Read from the rendered image rather than from bytesList, because the packed
    representation is rotation-interleaved and easy to unpack wrongly: an earlier
    attempt gave 1 for DICT_4X4_50, whose true distance is 4.
    """
    print(f"{'dictionary':22s} {'codes':>6s} {'data':>7s} {'total':>7s} {'minH':>5s} {'correctable':>12s}")
    for name, d in DICTS.items():
        dd = cv2.aruco.getPredefinedDictionary(d)
        h = min_hamming(dd)
        print(f"{name:22s} {dd.bytesList.shape[0]:6d} {dd.markerSize}x{dd.markerSize:<5} "
              f"{dd.markerSize+2}x{dd.markerSize+2:<5} {h:5d} {(h-1)//2:12d}")


def cmd_families(argv):
    """Which families can both toolchains read?  That intersection is the choice."""
    print("OpenCV aruco AprilTag families vs libapriltag 3.1.0")
    print(f"{'family':22s} {'codes':>6s} {'total':>7s} {'minH':>5s} {'OpenCV':>8s} {'libapriltag':>12s}")
    for name, d in DICTS.items():
        if "APRILTAG" not in name:
            continue
        dd = cv2.aruco.getPredefinedDictionary(d)
        tag = "tag" + name.split("_")[-1]
        print(f"{name:22s} {dd.bytesList.shape[0]:6d} {dd.markerSize+2}x{dd.markerSize+2:<5} "
              f"{min_hamming(dd):5d} {'yes':>8s} {('yes' if tag in LIBAPRILTAG else 'NO'):>12s}")
    print("\nlibapriltag 3.1.0 ships:", ", ".join(sorted(LIBAPRILTAG)))
    print("It ships NO ArUco family.  The claim that it carries tagAruco6x6_250 is false;")
    print("the containment runs the other way, OpenCV's aruco module ships AprilTag families.")


# ---------------------------------------------------------------- cross decode
def render(dict_id, mid, side_px=400, quiet_frac=0.2):
    d = cv2.aruco.getPredefinedDictionary(dict_id)
    m = cv2.aruco.generateImageMarker(d, mid, side_px)
    q = int(side_px * quiet_frac)
    return cv2.copyMakeBorder(m, q, q, q, q, cv2.BORDER_CONSTANT, value=255)


def cmd_escape(argv):
    """One printed family, one AprilTag detector family, per process."""
    import pupil_apriltags as at
    printed, fam = argv[0], argv[1]
    img = render(DICTS[printed], 7)
    r = at.Detector(families=fam).detect(img)
    print(f"{printed:22s} decoded by AprilTag {fam:18s}: "
          f"{[x.tag_id for x in r] if r else 'none'}")


def cmd_degrade(argv):
    """Smallest printed size each detector still reads, under blur, noise and tilt."""
    which = argv[0]
    use_apr = which.startswith("DICT_APRILTAG")
    if use_apr:
        import pupil_apriltags as at
    rng = np.random.default_rng(0)

    def make(mid, side, blur, noise, tilt):
        m = cv2.aruco.generateImageMarker(
            cv2.aruco.getPredefinedDictionary(DICTS[which]), mid, side)
        q = int(side * 0.6)
        img = cv2.copyMakeBorder(m, q, q, q, q, cv2.BORDER_CONSTANT, value=255)
        h, w = img.shape
        if tilt:
            dx = w * tilt
            src = np.float32([[0, 0], [w, 0], [w, h], [0, h]])
            dst = np.float32([[dx, 0], [w, 0], [w - dx, h], [0, h]])
            img = cv2.warpPerspective(img, cv2.getPerspectiveTransform(src, dst),
                                      (w, h), borderValue=255)
        if blur:
            img = cv2.GaussianBlur(img, (int(blur) * 2 + 1,) * 2, blur / 2.0)
        if noise:
            img = np.clip(img.astype(np.int16) +
                          rng.normal(0, noise, img.shape).astype(np.int16),
                          0, 255).astype(np.uint8)
        return img

    det = cv2.aruco.ArucoDetector(cv2.aruco.getPredefinedDictionary(DICTS[which]),
                                  cv2.aruco.DetectorParameters())
    print(f"{which}   (4.0 mm marker: 95 px at 600 DPI, 47 at 300, ~37 in a phone frame)")
    print(f"{'side px':>7s} {'blur':>5s} {'noise':>6s} {'tilt':>5s} {'OpenCV':>8s}")
    for side in (95, 64, 47, 37, 28, 20, 16, 12):
        for blur, noise, tilt in ((0, 0, 0.0), (2, 4, 0.0), (2, 4, 0.15), (4, 8, 0.25)):
            ok = 0
            for mid in (0, 7, 42, 130):
                c, ids, _ = det.detectMarkers(make(mid, side, blur, noise, tilt))
                ok += ids is not None and mid in [int(i) for i in ids.ravel()]
            print(f"{side:7d} {blur:5d} {noise:6d} {tilt:5.2f} {ok:6d}/4")


def cmd_quiet(argv):
    """Is the 1.0 mm quiet zone enough, with hostile artwork right outside it?"""
    which, qmod, kind = argv[0], int(argv[1]), argv[2]
    if kind == "apr":
        import pupil_apriltags as at
        det_apr = at.Detector(families="tag36h11")
    ok = tot = 0
    for mid in (0, 7, 42, 130):
        for cell in (12, 8, 6):
            dd = cv2.aruco.getPredefinedDictionary(DICTS[which])
            m = cv2.aruco.generateImageMarker(dd, mid, 8 * cell)
            q = qmod * cell
            img = cv2.copyMakeBorder(m, q, q, q, q, cv2.BORDER_CONSTANT, value=255)
            img = cv2.copyMakeBorder(img, 3 * cell, 3 * cell, 3 * cell, 3 * cell,
                                     cv2.BORDER_CONSTANT, value=0)
            img = cv2.GaussianBlur(img, (3, 3), 0.8)
            tot += 1
            if kind == "apr":
                ok += any(x.tag_id == mid for x in det_apr.detect(img))
            else:
                d2 = cv2.aruco.ArucoDetector(dd, cv2.aruco.DetectorParameters())
                c, ids, _ = d2.detectMarkers(img)
                ok += ids is not None and mid in [int(i) for i in ids.ravel()]
    print(f"{which:22s} quiet {qmod} modules, detector {kind:4s}: {ok}/{tot}")


CMDS = {"falsepos": cmd_falsepos, "geometry": cmd_geometry, "hamming": cmd_hamming,
        "families": cmd_families, "escape": cmd_escape, "degrade": cmd_degrade,
        "quiet": cmd_quiet}

if __name__ == "__main__":
    if len(sys.argv) < 2 or sys.argv[1] not in CMDS:
        print(__doc__)
        sys.exit(1)
    CMDS[sys.argv[1]](sys.argv[2:])
