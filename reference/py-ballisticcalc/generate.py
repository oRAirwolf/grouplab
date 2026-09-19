"""Reference trajectories from py-ballisticcalc, an independent point-mass solver, for GroupLab's ballistic solver gate.

docs/BALLISTICS-VALIDATION.md section 2 says what is compared and what each quantity must meet; that file was committed before these
tables were generated. This script runs only on a GitHub runner, from .github/workflows/reference-tables.yml, which installs the pinned
library. Its output, tables.json, is committed; the library is not. py-ballisticcalc is LGPL-3.0-only and GroupLab ships nothing of it.

Every case in reference/ballistics-cases.json is zeroed at its zero range on flat ground, then fired to its maximum range, sampled every
step. Each sample records the raw quantities: range and height and windage in inches from the sight line, time of flight and velocity.
The comparison turns height and windage into MOA the same way on both sides, so no difference in angular convention can enter.
"""
import json
import pathlib
import sys

import py_ballisticcalc
from py_ballisticcalc import (
    Ammo,
    Angular,
    Atmo,
    Calculator,
    Distance,
    DragModel,
    Pressure,
    Shot,
    TableG1,
    TableG7,
    Temperature,
    Velocity,
    Weapon,
    Wind,
)

root = pathlib.Path(__file__).resolve().parents[2]
spec = json.loads((root / "reference" / "ballistics-cases.json").read_text(encoding="utf-8"))
out = {
    "source": f"py-ballisticcalc {py_ballisticcalc.__version__}, default engine, Python {sys.version.split()[0]}",
    "cases": [],
}

for case in spec["cases"]:
    table = TableG1 if case["dragModel"] == "G1" else TableG7
    atmo = Atmo(
        altitude=Distance.Foot(0),
        pressure=Pressure.InHg(case["pressureInHg"]),
        temperature=Temperature.Fahrenheit(case["tempF"]),
        humidity=case["humidityPct"],
    )
    weapon = Weapon(sight_height=Distance.Inch(case["sightHeight"]))
    ammo = Ammo(DragModel(case["bc"], table), mv=Velocity.FPS(case["muzzleVelocity"]))
    # Wind from the shooter's left, 90 degrees, full value, drifting the bullet to the right.
    wind = Wind(velocity=Velocity.MPH(case["windSpeedMph"]), direction_from=Angular.Degree(90))
    calc = Calculator()

    # Zeroed with no wind, as a rifle is zeroed, then fired in the case's wind: the library's own documented pattern.
    shot = Shot(weapon=weapon, ammo=ammo, atmo=atmo)
    zero = calc.set_weapon_zero(shot, Distance.Yard(case["zeroRange"]))
    shot.winds = [wind]
    result = calc.fire(shot, trajectory_range=Distance.Yard(spec["maxRange"]), trajectory_step=Distance.Yard(spec["rangeStep"]))

    rows = []
    # 2.3.1's scheduled rows are result.trajectory; later versions call them result.samples. Only rows at a scheduled range are kept.
    scheduled = getattr(result, "samples", None) or result.trajectory
    for row in scheduled:
        range_yd = row.distance >> Distance.Yard
        if abs(range_yd - round(range_yd / spec["rangeStep"]) * spec["rangeStep"]) > 1e-6:
            continue
        rows.append({
            "rangeYd": range_yd,
            "heightIn": row.height >> Distance.Inch,
            "windageIn": row.windage >> Distance.Inch,
            "tof": row.time,
            "velocity": row.velocity >> Velocity.FPS,
            "mach": row.mach,
        })

    out["cases"].append({
        "name": case["name"],
        "densityRatio": atmo.density_ratio,
        "zeroElevationMoa": zero >> Angular.MOA,
        "rows": rows,
    })

(root / "reference" / "py-ballisticcalc" / "tables.json").write_text(json.dumps(out, indent=1) + "\n", encoding="utf-8")
print(json.dumps(out, indent=1))
