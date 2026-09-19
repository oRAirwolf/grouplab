/**
 * ═══════════════════════════════════════════════════════════════════════════
 *  PISSINHOT.COM — Point-Mass Ballistic Trajectory Solver
 *  G1 & G7 Standard Drag Models · ICAO Standard Atmosphere · RK4 Integration
 * ═══════════════════════════════════════════════════════════════════════════
 *
 *  Public-domain drag coefficient tables from the US Army Ballistic Research
 *  Laboratory (BRL/ARL).  Atmosphere model per ICAO Doc 7488/3.
 *
 *  Usage:
 *    const result = BallisticSolver.solve({
 *      bc: 0.310,           // Ballistic coefficient
 *      dragModel: 'G7',     // 'G1' or 'G7'
 *      muzzleVelocity: 2950,// fps
 *      bulletWeight: 230,   // grains
 *      sightHeight: 1.5,    // inches above bore
 *      zeroRange: 100,      // yards
 *      maxRange: 2000,      // yards
 *      rangeStep: 25,       // yards per step
 *      tempF: 59,           // ambient temperature °F
 *      pressureInHg: 29.92, // station pressure inHg
 *      altitudeFt: 0,       // altitude in feet (used if pressure not given)
 *      humidityPct: 50,     // relative humidity %
 *      windSpeedMph: 0,     // full-value crosswind mph
 *      angleDeg: 0,         // shooting angle (+ up, - down) degrees
 *    });
 *
 *  Returns array of objects:
 *    { range, velocity, energy, drop, dropMOA, dropMil, tof, windDrift,
 *      windMOA, windMil, mach, leadIn }
 *
 * ═══════════════════════════════════════════════════════════════════════════
 */

const BallisticSolver = (() => {
  'use strict';

  // ── CONSTANTS ──────────────────────────────────────────────────────────
  const GRAVITY    = 32.17405;   // ft/s²
  const STD_TEMP_F = 59.0;      // °F  (ICAO sea level)
  const STD_PRES   = 29.9213;   // inHg (ICAO sea level = 1013.25 hPa)
  const STD_RHO    = 0.0764742; // lb/ft³ at ICAO std
  const LAPSE_RATE = 0.00356616;// °F per foot of altitude
  const IN_PER_FT  = 12;
  const FT_PER_YD  = 3;
  const GRAINS_PER_LB = 7000;

  // ── G1 DRAG TABLE ─────────────────────────────────────────────────────
  // Mach → Cd pairs.  BRL standard projectile (flat base, 2 caliber ogive)
  const G1_TABLE = [
    [0.00, 0.2629], [0.05, 0.2558], [0.10, 0.2487], [0.15, 0.2413],
    [0.20, 0.2344], [0.25, 0.2278], [0.30, 0.2214], [0.35, 0.2155],
    [0.40, 0.2104], [0.45, 0.2061], [0.50, 0.2032], [0.55, 0.2020],
    [0.60, 0.2034], [0.70, 0.2165], [0.725, 0.2230], [0.75, 0.2313],
    [0.775, 0.2417], [0.80, 0.2546], [0.825, 0.2706], [0.85, 0.2912],
    [0.875, 0.3197], [0.90, 0.3605], [0.925, 0.4068], [0.95, 0.4519],
    [0.975, 0.4913], [1.00, 0.5210], [1.025, 0.5429], [1.05, 0.5599],
    [1.075, 0.5714], [1.10, 0.5786], [1.125, 0.5822], [1.15, 0.5832],
    [1.175, 0.5820], [1.20, 0.5790], [1.225, 0.5747], [1.25, 0.5694],
    [1.30, 0.5568], [1.35, 0.5432], [1.40, 0.5295], [1.45, 0.5163],
    [1.50, 0.5036], [1.55, 0.4916], [1.60, 0.4804], [1.65, 0.4699],
    [1.70, 0.4602], [1.75, 0.4512], [1.80, 0.4428], [1.85, 0.4350],
    [1.90, 0.4275], [1.95, 0.4205], [2.00, 0.4139], [2.05, 0.4077],
    [2.10, 0.4018], [2.15, 0.3962], [2.20, 0.3909], [2.25, 0.3858],
    [2.30, 0.3810], [2.35, 0.3763], [2.40, 0.3718], [2.45, 0.3675],
    [2.50, 0.3633], [2.60, 0.3553], [2.70, 0.3477], [2.80, 0.3405],
    [2.90, 0.3338], [3.00, 0.3274], [3.10, 0.3215], [3.20, 0.3159],
    [3.30, 0.3107], [3.40, 0.3058], [3.50, 0.3013], [3.60, 0.2970],
    [3.70, 0.2930], [3.80, 0.2893], [3.90, 0.2857], [4.00, 0.2823],
    [4.20, 0.2762], [4.40, 0.2707], [4.60, 0.2657], [4.80, 0.2612],
    [5.00, 0.2571],
  ];

  // ── G7 DRAG TABLE ─────────────────────────────────────────────────────
  // BRL standard long-range projectile (boat tail, secant ogive)
  const G7_TABLE = [
    [0.00, 0.1198], [0.05, 0.1197], [0.10, 0.1196], [0.15, 0.1194],
    [0.20, 0.1193], [0.25, 0.1194], [0.30, 0.1194], [0.35, 0.1194],
    [0.40, 0.1193], [0.45, 0.1193], [0.50, 0.1194], [0.55, 0.1193],
    [0.60, 0.1194], [0.65, 0.1197], [0.70, 0.1202], [0.725, 0.1207],
    [0.75, 0.1215], [0.775, 0.1226], [0.80, 0.1242], [0.825, 0.1266],
    [0.85, 0.1306], [0.875, 0.1368], [0.90, 0.1464], [0.925, 0.1660],
    [0.95, 0.2054], [0.975, 0.2993], [1.00, 0.3803], [1.025, 0.4015],
    [1.05, 0.4043], [1.075, 0.4034], [1.10, 0.4014], [1.125, 0.3987],
    [1.15, 0.3955], [1.20, 0.3884], [1.25, 0.3810], [1.30, 0.3732],
    [1.35, 0.3657], [1.40, 0.3580], [1.50, 0.3440], [1.55, 0.3376],
    [1.60, 0.3315], [1.65, 0.3260], [1.70, 0.3209], [1.75, 0.3160],
    [1.80, 0.3117], [1.85, 0.3078], [1.90, 0.3042], [1.95, 0.3010],
    [2.00, 0.2980], [2.05, 0.2951], [2.10, 0.2922], [2.15, 0.2892],
    [2.20, 0.2864], [2.25, 0.2835], [2.30, 0.2807], [2.35, 0.2779],
    [2.40, 0.2752], [2.45, 0.2725], [2.50, 0.2697], [2.55, 0.2670],
    [2.60, 0.2643], [2.65, 0.2615], [2.70, 0.2588], [2.75, 0.2561],
    [2.80, 0.2533], [2.85, 0.2506], [2.90, 0.2479], [2.95, 0.2451],
    [3.00, 0.2424], [3.50, 0.2157], [4.00, 0.1920], [4.50, 0.1710],
    [5.00, 0.1523],
  ];


  // ── DRAG INTERPOLATION ─────────────────────────────────────────────────
  function getCd(mach, table) {
    if (mach <= table[0][0]) return table[0][1];
    if (mach >= table[table.length - 1][0]) return table[table.length - 1][1];
    for (let i = 0; i < table.length - 1; i++) {
      if (mach >= table[i][0] && mach <= table[i + 1][0]) {
        const frac = (mach - table[i][0]) / (table[i + 1][0] - table[i][0]);
        return table[i][1] + frac * (table[i + 1][1] - table[i][1]);
      }
    }
    return table[table.length - 1][1];
  }


  // ── ATMOSPHERE MODEL ───────────────────────────────────────────────────

  /**
   * Saturation vapor pressure of water in inHg at the given temperature.
   * Herman Wobus polynomial. Extracted as a shared helper because both
   * densityRatio and the moist-air speedOfSound need this value, and
   * the formula must not drift between the two.
   */
  function _satVaporInHg(tempF) {
    const tC = (tempF - 32) * 5 / 9;
    const eso = 6.1078;
    const c0=0.99999683, c1=-0.90826951e-2, c2=0.78736169e-4,
          c3=-0.61117958e-6, c4=0.43884187e-8, c5=-0.29883885e-10,
          c6=0.21874425e-12, c7=-0.17892321e-14, c8=0.11112018e-16,
          c9=-0.30994571e-19;
    const p = c0+tC*(c1+tC*(c2+tC*(c3+tC*(c4+tC*(c5+tC*(c6+tC*(c7+tC*(c8+tC*c9))))))));
    const esHpa = eso / Math.pow(p, 8);   // saturation vapor pressure (hPa)
    return esHpa * 0.02953;                // convert hPa to inHg
  }

  /**
   * Speed of sound in ft/s.
   *
   * Dry air:    a = 49.0223 * sqrt(T_rankine)
   *
   * Moist air:  a depends on temperature, pressure, and humidity. Water
   * vapor is lighter than dry air (18.0 vs 28.96 g/mol), which raises
   * the speed of sound; water vapor also has a slightly lower heat
   * capacity ratio gamma, which partially offsets the gain. The
   * Owen-Cramer formulation combines both:
   *
   *   (a_moist / a_dry)^2 = (1 + 0.378*xw) * (1 - 0.061*xw)
   *
   * where xw = pv / p is the mole fraction of water vapor in moist air.
   *
   * Note: pressure has no direct effect on the speed of sound for an
   * ideal gas. It enters only indirectly through xw, which means the
   * same relative humidity at higher altitude (lower p) produces a
   * larger correction than the same humidity at sea level.
   *
   * Backward compatible: if pressureInHg or humidityPct is omitted or
   * humidityPct is 0, the dry-air formula is returned, so callers that
   * do not have full atmospheric data continue to work unchanged.
   */
  function speedOfSound(tempF, pressureInHg, humidityPct) {
    const tR = tempF + 459.67;
    const aDry = 49.0223 * Math.sqrt(tR);

    if (pressureInHg == null || humidityPct == null || humidityPct <= 0) {
      return aDry;
    }

    const pvInHg = (humidityPct / 100) * _satVaporInHg(tempF);
    const xw = pvInHg / pressureInHg;   // mole fraction of water vapor

    // Owen-Cramer moist-air correction:
    //   molar-mass factor:        1 + 0.378 * xw
    //   heat-capacity-ratio fac:  1 - 0.061 * xw
    const ratioSq = (1 + 0.378 * xw) * (1 - 0.061 * xw);
    return aDry * Math.sqrt(ratioSq);
  }

  /**
   * Air density ratio (ρ/ρ₀) from temperature, pressure, humidity.
   * Uses the virtual temperature correction for humidity.
   */
  function densityRatio(tempF, pressureInHg, humidityPct) {
    const tR = tempF + 459.67;
    const stdTR = STD_TEMP_F + 459.67;

    const esInHg = _satVaporInHg(tempF);
    const pv = (humidityPct / 100) * esInHg; // partial pressure of water vapor (inHg)

    // Density ratio: dry air with virtual-temperature humidity correction.
    // rho/rho0 = (P/P0) * (T0/T) * (1 - 0.3783 * Pv/P)
    const rho = (pressureInHg / STD_PRES) * (stdTR / tR) *
                (1 - 0.3783 * pv / pressureInHg);
    return rho;
  }

  /**
   * Estimate station pressure from altitude using ICAO troposphere model.
   * Returns pressure in inHg.
   */
  function pressureFromAltitude(altFt, tempF) {
    // Use the barometric formula for troposphere
    // P = P0 × (T / T0)^(g/(L×R))
    // With standard lapse rate L = 0.0019812 °C/ft
    const t0K = (STD_TEMP_F + 459.67) / 1.8;  // 288.15 K
    const tK  = (tempF + 459.67) / 1.8;
    const L   = 0.0019812;  // °C per foot (≈ 6.5 °C/km)
    const exp = GRAVITY / (L * 1.8 * 1716.49);  // g/(L*R) in imperial-ish
    // Simplified: use the standard formula
    const ratio = Math.pow(1 - (LAPSE_RATE * altFt) / (STD_TEMP_F + 459.67), 5.2561);
    return STD_PRES * ratio;
  }


  // ── RETARDATION FUNCTION ───────────────────────────────────────────────

  /**
   * Compute deceleration (ft/s²) from drag.
   *
   * F_drag = ½ × ρ × V² × Cd × A
   * a_drag = F_drag / mass
   *
   * Using the BC relationship:  BC = (mass/area) / (Cd_std × ρ₀_ratio)
   *   → a_drag = (ρ/ρ₀) × V² × Cd(M) / (BC × Cd_ref(M) × C_ballistic)
   *
   * Simplification: drag decel = (Cd/Cd_ref) × (ρ/ρ₀) × V / (BC × constant)
   *
   * Actually, the standard form is:
   *   retardation = (ρ/ρ₀) × Cd(M) × V² / (BC_sectional × reference_factor)
   *
   * The standard drag function approach:
   *   a = (ρ/ρ₀) × G(V) / BC
   * where G(V) is the drag function value for the reference projectile at velocity V.
   *
   * G(V) = Cd(M) × ρ₀ × A_ref × V² / (2 × m_ref)
   *
   * For practical computation, we use:
   *   decel = (ρ/ρ₀) × Cd(M) × V² / (BC × Cd_ref_at_M × V²) ... no
   *
   * Correct approach: The retardation coefficient at velocity V is
   *   a(V) = [ρ/ρ₀] × [Cd(M)/Cd_ref(M)] ... no, BC already encodes Cd_ref
   *
   * Let me use the direct approach:
   *   BC = i / (Cd(M)/Cd_std(M))  -- no, BC = m/(d²×i) where i = form factor
   *
   * Standard point-mass:
   *   The drag deceleration in ft/s² is:
   *   a_drag = (ρ/ρ₀) × V × DragFunc(V) / BC
   *
   *   where DragFunc(V) is tabulated for the standard projectile.
   *   Specifically: DragFunc returns the retardation per unit BC at std atmosphere.
   *
   *   The standard retardation tables give A(V) in (ft/s)/(s) per unit BC,
   *   such that: decel = A(V) / BC  at standard atmosphere.
   *
   *   With atmosphere correction: decel = (ρ/ρ₀) × A(V) / BC
   *
   * Rather than using retardation tables, use the Cd tables directly:
   *
   *   decel = (ρ × V² × S × Cd) / (2 × m)
   *
   *   where S = reference area = π/4 × d²
   *         m = bullet mass
   *         Cd = Cd(Mach) from the drag table
   *
   *   BC = m / (S × i)  where i = form factor = Cd_bullet / Cd_standard
   *   → m / S = BC × i
   *   → decel = (ρ × V² × Cd) / (2 × BC × i)
   *
   *   But since Cd_standard is in the table and Cd_bullet = i × Cd_standard:
   *   → decel = (ρ × V² × i × Cd_std) / (2 × BC × i)
   *   → decel = (ρ × V² × Cd_std) / (2 × BC)
   *
   *   Normalizing to standard density:
   *   → decel = (ρ/ρ₀) × (ρ₀ × V² × Cd_std) / (2 × BC)
   *
   *   The term (ρ₀ × Cd_std(M) × V²) / 2 is the standard drag function.
   *   Convert BC from lb/in² to lb/ft²: BC_ft = BC × 144
   *
   *   Actually let's just compute directly.
   */
  function dragDecel(velocity, mach, bc, dragTable, rhoRatio) {
    const Cd = getCd(mach, dragTable);

    // Drag deceleration for a bullet with ballistic coefficient BC:
    //
    //   a = (ρ_air × V² × Cd_actual × A) / (2 × W)
    //
    // where A = π/4 × (d/12)² = π × d² / 576  ft²
    //       BC = W / (i × d²)   (W in lb, d in inches)
    //       Cd_actual = i × Cd_std  (i = form factor)
    //
    // Substituting i × d² = W / BC:
    //   a = (ρ × V² × Cd_std × π) / (1152 × BC)
    //
    // With atmosphere correction:
    //   a = (ρ/ρ₀) × π × ρ₀ × V² × Cd(M) / (1152 × BC)
    //
    // Constant = π × 0.0764742 / 1152 = 0.00020856

    return rhoRatio * Cd * velocity * velocity * 0.00020856 / bc;
  }


  // ── TRAJECTORY SOLVER ──────────────────────────────────────────────────

  /**
   * Solve the point-mass trajectory using 4th-order Runge-Kutta integration.
   *
   * State vector: [x, y, vx, vy]
   *   x  = downrange position (ft)
   *   y  = vertical position (ft), positive up
   *   vx = horizontal velocity (ft/s)
   *   vy = vertical velocity (ft/s)
   *
   * For wind: crosswind is handled separately as a lateral deflection
   * accumulation, since the point-mass model with constant crosswind
   * gives lateral drift = wind × (tof - range/muzzleVelocity).
   *
   * @returns {Array} trajectory table
   */
  function solve(params) {
    const {
      bc,
      dragModel = 'G7',
      muzzleVelocity,
      bulletWeight,
      sightHeight = 1.5,
      zeroRange = 100,
      maxRange = 3000,
      rangeStep = 25,
      tempF = 59,
      pressureInHg = null,
      altitudeFt = 0,
      humidityPct = 50,
      windSpeedMph = 0,
      angleDeg = 0,
    } = params;

    // Validate
    if (!bc || bc <= 0) throw new Error('BC must be positive');
    if (!muzzleVelocity || muzzleVelocity <= 0) throw new Error('Muzzle velocity must be positive');
    if (!bulletWeight || bulletWeight <= 0) throw new Error('Bullet weight must be positive');

    const dragTable = dragModel === 'G1' ? G1_TABLE : G7_TABLE;

    // Atmosphere
    const pressure = pressureInHg != null ? pressureInHg :
                     pressureFromAltitude(altitudeFt, tempF);
    const rhoRatio = densityRatio(tempF, pressure, humidityPct);
    const sos      = speedOfSound(tempF, pressure, humidityPct);

    // Wind in ft/s
    const windFps = windSpeedMph * 5280 / 3600;

    // Shooting angle
    const angleRad = angleDeg * Math.PI / 180;
    const cosA = Math.cos(angleRad);
    const sinA = Math.sin(angleRad);

    // Initial velocity components along the bore
    const vBore = muzzleVelocity;  // fps

    // Sight height in feet
    const sightHtFt = sightHeight / IN_PER_FT;

    // Time step (seconds) — adaptive for stability
    const dt = 0.0005;  // 0.5ms gives good accuracy to 3000 yd

    // State: position and velocity in the vertical plane
    // x = downrange (horizontal), y = vertical
    // Initial: bullet starts at bore, not sight
    let x  = 0;
    let y  = -sightHtFt;  // bore is below sight line
    let vx = vBore * cosA;
    let vy = vBore * sinA;
    let t  = 0;

    // First pass: find zero angle
    // We need to determine the initial launch angle that zeroes at zeroRange
    // Use iterative approach: shoot flat, measure drop at zero range, adjust
    let zeroAngleRad = 0;

    // Run a preliminary trajectory to find drop at zero range with zero angle
    function runToRange(launchAngle, targetRangeYd) {
      const targetRangeFt = targetRangeYd * FT_PER_YD;
      let lx = 0, ly = -sightHtFt;
      let lvx = vBore * Math.cos(launchAngle + angleRad);
      let lvy = vBore * Math.sin(launchAngle + angleRad);
      let lt = 0;

      while (lx < targetRangeFt && lt < 10) {
        const v = Math.sqrt(lvx * lvx + lvy * lvy);
        const m = v / sos;
        const drag = dragDecel(v, m, bc, dragTable, rhoRatio);

        // Direction of velocity
        const dirX = lvx / v;
        const dirY = lvy / v;

        // Accelerations
        const ax = -drag * dirX;
        const ay = -drag * dirY - GRAVITY;

        // Simple Euler for zeroing pass (fast enough)
        lvx += ax * dt;
        lvy += ay * dt;
        lx  += lvx * dt;
        ly  += lvy * dt;
        lt  += dt;
      }
      return ly;  // vertical offset from sight line at target range
    }

    // Binary search for zero angle
    let loAngle = 0, hiAngle = 0.1; // radians
    // First check: if drop is negative at zero range with 0 angle, we need upward correction
    const dropAtZero = runToRange(0, zeroRange);
    if (zeroRange > 0 && Math.abs(dropAtZero) > 0.0001) {
      // Need angle correction
      for (let iter = 0; iter < 50; iter++) {
        const midAngle = (loAngle + hiAngle) / 2;
        const d = runToRange(midAngle, zeroRange);
        if (d < 0) {
          loAngle = midAngle;
        } else {
          hiAngle = midAngle;
        }
      }
      zeroAngleRad = (loAngle + hiAngle) / 2;
    }

    // Main trajectory run with zero angle
    x  = 0;
    y  = -sightHtFt;
    vx = vBore * Math.cos(zeroAngleRad + angleRad);
    vy = vBore * Math.sin(zeroAngleRad + angleRad);
    t  = 0;

    const maxRangeFt = maxRange * FT_PER_YD;
    const results = [];
    let nextRangeYd = 0;
    let nextRangeFt = 0;

    // Also track wind drift via the "lag time" method:
    // drift = windSpeed × (tof - range/muzzleVelocity)
    // This is a standard approximation for point-mass with constant crosswind.

    while (x <= maxRangeFt + FT_PER_YD && t < 15) {
      const rangeYd = x / FT_PER_YD;

      // Record data at each range step
      if (rangeYd >= nextRangeYd - 0.01) {
        const v = Math.sqrt(vx * vx + vy * vy);
        const energy = (bulletWeight * v * v) / 450240;
        const dropIn = y * IN_PER_FT;  // drop relative to sight line, in inches

        // Angular drop from line of sight
        const rangeIn = nextRangeYd * FT_PER_YD * IN_PER_FT;
        let dropMOA = 0, dropMil = 0;
        if (nextRangeYd > 0) {
          dropMOA = -(dropIn / rangeIn) * (180 / Math.PI) * 60;  // positive = bullet low
          dropMil = dropMOA * 0.290888;
        }

        // Wind drift (lag time method)
        const lagTime = nextRangeYd > 0 ? (t - (nextRangeYd * FT_PER_YD) / muzzleVelocity) : 0;
        const driftIn = windFps * lagTime * IN_PER_FT;  // wind drift in inches
        let windMOA = 0, windMil = 0;
        if (nextRangeYd > 0) {
          windMOA = (driftIn / rangeIn) * (180 / Math.PI) * 60;
          windMil = windMOA * 0.290888;
        }

        results.push({
          range:     nextRangeYd,
          velocity:  Math.round(v * 10) / 10,
          energy:    Math.round(energy),
          drop:      Math.round(dropIn * 100) / 100,
          dropMOA:   Math.round(dropMOA * 100) / 100,
          dropMil:   Math.round(dropMil * 100) / 100,
          tof:       Math.round(t * 1000) / 1000,
          windDrift: Math.round(driftIn * 100) / 100,
          windMOA:   Math.round(windMOA * 100) / 100,
          windMil:   Math.round(windMil * 100) / 100,
          mach:      Math.round((v / sos) * 1000) / 1000,
          leadIn:    Math.round(v > 0 ? driftIn : 0),
        });

        nextRangeYd += rangeStep;
        nextRangeFt = nextRangeYd * FT_PER_YD;

        if (nextRangeYd > maxRange) break;
      }

      // RK4 integration step
      const state = [x, y, vx, vy];

      function derivs(s) {
        const [sx, sy, svx, svy] = s;
        const v = Math.sqrt(svx * svx + svy * svy);
        if (v < 1) return [0, 0, 0, -GRAVITY]; // bullet stopped
        const m = v / sos;
        const drag = dragDecel(v, m, bc, dragTable, rhoRatio);
        const dirX = svx / v;
        const dirY = svy / v;
        return [svx, svy, -drag * dirX, -drag * dirY - GRAVITY];
      }

      const k1 = derivs(state);
      const s2 = state.map((s, i) => s + 0.5 * dt * k1[i]);
      const k2 = derivs(s2);
      const s3 = state.map((s, i) => s + 0.5 * dt * k2[i]);
      const k3 = derivs(s3);
      const s4 = state.map((s, i) => s + dt * k3[i]);
      const k4 = derivs(s4);

      x  += (dt / 6) * (k1[0] + 2*k2[0] + 2*k3[0] + k4[0]);
      y  += (dt / 6) * (k1[1] + 2*k2[1] + 2*k3[1] + k4[1]);
      vx += (dt / 6) * (k1[2] + 2*k2[2] + 2*k3[2] + k4[2]);
      vy += (dt / 6) * (k1[3] + 2*k2[3] + 2*k3[3] + k4[3]);
      t  += dt;

      // Bail if bullet is going backward or too slow
      if (vx < 10) break;
    }

    return results;
  }


  // ── UTILITY: Find range where elevation equals a given MOA ─────────────

  /**
   * Given a trajectory table, find the range at which cumulative drop
   * equals the given elevation in MOA.
   * Returns interpolated range in yards, or null if never reached.
   */
  function rangeForElevation(trajectory, elevationMOA) {
    for (let i = 1; i < trajectory.length; i++) {
      const prev = trajectory[i - 1];
      const curr = trajectory[i];
      if (curr.dropMOA >= elevationMOA && prev.dropMOA < elevationMOA) {
        // Linear interpolation
        const frac = (elevationMOA - prev.dropMOA) / (curr.dropMOA - prev.dropMOA);
        return Math.round(prev.range + frac * (curr.range - prev.range));
      }
    }
    // If the last entry still hasn't reached that elevation, check if close
    if (trajectory.length > 0) {
      const last = trajectory[trajectory.length - 1];
      if (last.dropMOA >= elevationMOA) return last.range;
    }
    return null;
  }


  // ── UTILITY: Velocity dispersion at distance ───────────────────────────

  /**
   * For a given load, compute the vertical dispersion (in inches) at each
   * distance caused by a velocity standard deviation.
   *
   * Runs three trajectories: mean, mean+sd, mean-sd.
   * Returns array of { range, spreadIn, spreadMOA } objects.
   */
  function velocityDispersion(params, velocitySD) {
    const baseTraj  = solve(params);
    const highParams = { ...params, muzzleVelocity: params.muzzleVelocity + velocitySD };
    const lowParams  = { ...params, muzzleVelocity: params.muzzleVelocity - velocitySD };
    const highTraj  = solve(highParams);
    const lowTraj   = solve(lowParams);

    const results = [];
    for (let i = 0; i < baseTraj.length; i++) {
      const range = baseTraj[i].range;
      if (range === 0) {
        results.push({ range, spreadIn: 0, spreadMOA: 0 });
        continue;
      }
      // Find matching range in high/low trajectories
      const hi = highTraj.find(r => r.range === range);
      const lo = lowTraj.find(r => r.range === range);
      if (hi && lo) {
        const spreadIn = Math.abs(hi.drop - lo.drop);
        const rangeIn  = range * 3 * 12;
        const spreadMOA = rangeIn > 0 ? (spreadIn / rangeIn) * (180 / Math.PI) * 60 : 0;
        results.push({
          range,
          spreadIn:  Math.round(spreadIn * 100) / 100,
          spreadMOA: Math.round(spreadMOA * 100) / 100,
        });
      }
    }
    return results;
  }


  // ── UTILITY: Hit probability at distance ───────────────────────────────

  /**
   * Compute P(hit) on a target of given height (inches) at each distance,
   * assuming vertical dispersion from velocity SD follows a normal distribution.
   *
   * Uses the velocity-induced vertical spread as the ±1σ band, computing
   * P(|offset| < targetHeight/2) from the cumulative normal distribution.
   *
   * Can optionally combine with mechanical dispersion (MOA from TOP Gun or
   * measured group size).
   *
   * @param {Object} params - solver params
   * @param {number} velocitySD - muzzle velocity SD in fps
   * @param {number} targetHeightIn - target height in inches
   * @param {number} [mechanicalMOA=0] - additional mechanical dispersion in MOA
   * @returns {Array} { range, pHit, vertSpreadIn, totalSpreadIn }
   */
  function hitProbability(params, velocitySD, targetHeightIn, mechanicalMOA = 0) {
    const dispersion = velocityDispersion(params, velocitySD);

    return dispersion.map(d => {
      const range = d.range;
      if (range === 0) return { range, pHit: 1.0, vertSpreadIn: 0, totalSpreadIn: 0 };

      // Velocity-induced vertical spread (1σ)
      const velSigmaIn = d.spreadIn / 2;  // ±1SD → spread is 2σ wide

      // Mechanical dispersion at this range (MOA → inches)
      const mechSigmaIn = mechanicalMOA > 0 ?
        (mechanicalMOA * 1.047 * (range / 100)) / 2 : 0;  // 1σ of the group

      // Combined sigma (RSS of independent sources)
      const totalSigma = Math.sqrt(velSigmaIn * velSigmaIn + mechSigmaIn * mechSigmaIn);

      // P(hit) = P(|offset| < targetHeight/2)
      // = 2 × Φ(targetHeight / (2 × σ)) - 1
      const halfTarget = targetHeightIn / 2;
      let pHit = 1.0;
      if (totalSigma > 0.001) {
        const z = halfTarget / totalSigma;
        pHit = 2 * normalCDF(z) - 1;
      }

      return {
        range,
        pHit:         Math.round(pHit * 10000) / 10000,
        vertSpreadIn: d.spreadIn,
        totalSpreadIn: Math.round(totalSigma * 2 * 100) / 100,  // 2σ spread
      };
    });
  }

  // Normal CDF (Abramowitz & Stegun 7.1.26)
  function normalCDF(x) {
    if (x < -8) return 0;
    if (x > 8)  return 1;
    const a1=0.254829592, a2=-0.284496736, a3=1.421413741,
          a4=-1.453152027, a5=1.061405429, p=0.3275911;
    const sgn = x < 0 ? -1 : 1;
    const ax  = Math.abs(x) / Math.SQRT2;
    const t   = 1 / (1 + p * ax);
    const y   = 1 - (((((a5*t+a4)*t)+a3)*t+a2)*t+a1)*t * Math.exp(-ax*ax);
    return Math.max(0, Math.min(1, 0.5 * (1 + sgn * y)));
  }


  // ── UTILITY: Combined group size (RSS model) ──────────────────────────

  /**
   * Predict realistic group size at distance by combining independent
   * dispersion sources via root-sum-of-squares.
   *
   * Sources:
   *   1. Mechanical dispersion (TOP Gun MOA prediction) — constant in MOA
   *   2. Velocity-induced vertical spread — from ballistic solver
   *   3. Wind sensitivity — from variable wind (optional)
   *
   * @returns {Array} { range, mechIn, velIn, windIn, totalIn, totalMOA }
   */
  function combinedGroupSize(params, velocitySD, mechanicalMOA, windVariationMph = 0) {
    const dispersion = velocityDispersion(params, velocitySD);

    // If wind variation requested, compute wind drift sensitivity
    let windSensitivity = [];
    if (windVariationMph > 0) {
      const baseWind = solve(params);
      const gustParams = { ...params, windSpeedMph: (params.windSpeedMph || 0) + windVariationMph };
      const gustTraj = solve(gustParams);
      windSensitivity = baseWind.map((b, i) => {
        const g = gustTraj[i];
        if (!g) return 0;
        return Math.abs(g.windDrift - b.windDrift);
      });
    }

    return dispersion.map((d, i) => {
      const range = d.range;
      if (range === 0) return { range, mechIn:0, velIn:0, windIn:0, totalIn:0, totalMOA:0 };

      // Mechanical (constant MOA → scales with range)
      const mechIn = mechanicalMOA * 1.047 * (range / 100);

      // Velocity vertical spread
      const velIn = d.spreadIn;

      // Wind lateral spread
      const windIn = windSensitivity[i] || 0;

      // RSS (independent sources)
      const totalIn = Math.sqrt(mechIn*mechIn + velIn*velIn + windIn*windIn);

      // Back to MOA
      const rangeIn = range * 3 * 12;
      const totalMOA = rangeIn > 0 ? (totalIn / rangeIn) * (180 / Math.PI) * 60 : 0;

      return {
        range,
        mechIn:   Math.round(mechIn * 100) / 100,
        velIn:    Math.round(velIn * 100) / 100,
        windIn:   Math.round(windIn * 100) / 100,
        totalIn:  Math.round(totalIn * 100) / 100,
        totalMOA: Math.round(totalMOA * 100) / 100,
      };
    });
  }


  // ── UTILITY: Muzzle energy ────────────────────────────────────────────

  function muzzleEnergy(bulletWeightGr, velocityFps) {
    return (bulletWeightGr * velocityFps * velocityFps) / 450240;
  }


  // ── SPIN DRIFT ─────────────────────────────────────────────────────────

  /**
   * Miller gyroscopic stability factor.
   * @param {number} twistIn    - Twist rate in inches per turn (e.g. 9.0)
   * @param {number} diameter   - Bullet diameter in inches (e.g. 0.264)
   * @param {number} lengthIn   - Bullet length in inches (e.g. 1.48)
   * @param {number} weightGr   - Bullet weight in grains (e.g. 153)
   * @param {number} velocityFps - Muzzle velocity in fps
   * @param {number} tempF      - Temperature in °F
   * @returns {number} SG (gyroscopic stability factor; > 1.0 = stable)
   */
  function millerSG(twistIn, diameter, lengthIn, weightGr, velocityFps, tempF) {
    if (!twistIn || !diameter || !lengthIn || !weightGr) return NaN;
    const t = twistIn / diameter;        // twist in calibers/turn
    const l = lengthIn / diameter;        // length in calibers
    const m = weightGr;
    // Miller's formula: SG = 30 × m / (t² × d³ × l × (1 + l²))
    const sg_raw = (30 * m) / (t * t * Math.pow(diameter, 3) * l * (1 + l * l));
    // Velocity & temperature correction
    const fv = Math.pow(velocityFps / 2800, 1.0 / 3.0);
    const ft = (tempF + 460) / (59 + 460);
    return sg_raw * fv * ft;
  }

  /**
   * Spin drift in inches at a given time of flight.
   * Uses the Litz approximation: SD = 1.25 × (SG + 1.2) × TOF^1.83
   * @param {number} sg  - Miller stability factor
   * @param {number} tof - Time of flight in seconds
   * @param {number} twistDir - 1 for right-hand twist, -1 for left-hand
   * @returns {number} Spin drift in inches (positive = right for RH twist)
   */
  function spinDrift(sg, tof, twistDir) {
    if (!isFinite(sg) || sg <= 0 || tof <= 0) return 0;
    return twistDir * 1.25 * (sg + 1.2) * Math.pow(tof, 1.83);
  }


  // ── CORIOLIS EFFECT ────────────────────────────────────────────────────

  const OMEGA = 7.2921e-5;  // Earth angular velocity, rad/s

  /**
   * Coriolis horizontal deflection (inches).
   * Positive = right in northern hemisphere (for bullets fired any direction).
   * @param {number} latDeg    - Latitude in degrees (positive = north)
   * @param {number} azDeg     - Firing azimuth in degrees (0=N, 90=E, 180=S, 270=W)
   * @param {number} rangeFt   - Range in feet
   * @param {number} tof       - Time of flight in seconds
   * @returns {number} Horizontal deflection in inches
   */
  function coriolisHorizontal(latDeg, azDeg, rangeFt, tof) {
    const latRad = latDeg * Math.PI / 180;
    // Horizontal Coriolis: acceleration = 2 × Ω × V × sin(lat)
    // Deflection = Ω × sin(lat) × range × tof (simplified for constant V)
    return OMEGA * Math.sin(latRad) * rangeFt * tof * IN_PER_FT;
  }

  /**
   * Coriolis vertical deflection (Eötvös effect) in inches.
   * Firing east increases apparent gravity (bullet drops more),
   * firing west decreases it.
   * @param {number} latDeg  - Latitude in degrees
   * @param {number} azDeg   - Firing azimuth in degrees
   * @param {number} vFps    - Average horizontal velocity in fps
   * @param {number} tof     - Time of flight in seconds
   * @returns {number} Vertical deflection in inches (positive = up)
   */
  function coriolisVertical(latDeg, azDeg, vFps, tof) {
    const latRad = latDeg * Math.PI / 180;
    const azRad  = azDeg * Math.PI / 180;
    // Eötvös: vertical accel = 2 × Ω × V × cos(lat) × sin(az)
    // Deflection = 0.5 × accel × tof²
    const accel = 2 * OMEGA * vFps * Math.cos(latRad) * Math.sin(azRad);
    return -0.5 * accel * tof * tof * IN_PER_FT;
  }


  // ── AERODYNAMIC JUMP ───────────────────────────────────────────────────

  /**
   * Simplified aerodynamic jump estimate.
   * This is the systematic vertical component caused by the interaction
   * of the bullet's spin with crosswind (yaw of repose effect).
   * AJ ≈ (crosswind / velocity) × spin_factor
   *
   * This is an approximation. The actual AJ depends on the bullet's
   * specific aerodynamic properties (CMa, CL) which require wind tunnel data.
   * For practical use, this gives an order-of-magnitude estimate.
   *
   * The crosswind-induced AJ deflection is vertical:
   *   AJ_vertical (rad) ≈ (Vw / V0) × (SG × d / twist) × empirical_factor
   *
   * Simplified: AJ (MOA) ≈ crosswind_component / muzzle_velocity × SG_factor
   *
   * @param {number} sg           - Stability factor
   * @param {number} crosswindFps - Crosswind component in fps
   * @param {number} muzzleVel    - Muzzle velocity fps
   * @returns {number} Aerodynamic jump in MOA (vertical deflection)
   */
  function aeroJump(sg, crosswindFps, muzzleVel) {
    if (!isFinite(sg) || sg <= 0 || muzzleVel <= 0) return 0;
    // Empirical: ~0.01 MOA per fps of crosswind per unit of (1/SG)
    // This is a rough approximation based on published AB data
    const factor = 0.012 / sg;
    return crosswindFps * factor;
  }


  // ── EXTENDED SOLVE ─────────────────────────────────────────────────────

  /**
   * Extended trajectory solution with spin drift, Coriolis, and
   * aerodynamic jump. Wraps the base solve() and adds extra columns.
   *
   * Additional params beyond solve():
   *   twistRate     - Barrel twist in inches/turn (e.g. 8.0)
   *   twistDir      - 1 = right-hand, -1 = left-hand (default: 1)
   *   bulletDiameter- Bullet diameter in inches (e.g. 0.264)
   *   bulletLength  - Bullet length in inches (e.g. 1.48)
   *   latitude      - Firing latitude in degrees (positive = N)
   *   azimuth       - Firing azimuth in degrees (0=N, 90=E)
   *   windDirDeg    - Wind FROM direction in degrees (0=N means headwind
   *                   when firing north; 90=E means wind from the east)
   *
   * Returns the base trajectory rows plus:
   *   sg, spinDriftIn, spinDriftMOA, coriolisHIn, coriolisVIn,
   *   aeroJumpMOA, totalHorizIn, totalHorizMOA
   */
  function solveExtended(params) {
    const {
      twistRate = 0,
      twistDir = 1,
      bulletDiameter = 0,
      bulletLength = 0,
      latitude = 0,
      azimuth = 0,
      windDirDeg = 0,
      windSpeedMph = 0,
    } = params;

    // Run base solver
    const trajectory = solve(params);

    // Stability factor (if twist/diameter/length provided)
    const hasTwist = twistRate > 0 && bulletDiameter > 0 && bulletLength > 0;
    const sg = hasTwist ? millerSG(
      twistRate, bulletDiameter, bulletLength,
      params.bulletWeight, params.muzzleVelocity,
      params.tempF || 59
    ) : 0;

    // Crosswind component for wind drift and aero jump
    // Wind direction is "from", firing direction is azimuth
    // Crosswind = windSpeed × sin(windDir - azimuth)
    const windFps = windSpeedMph * 5280 / 3600;
    const windAngleRad = ((windDirDeg - azimuth) * Math.PI) / 180;
    const crosswindFps = windFps * Math.sin(windAngleRad);
    const headwindFps  = windFps * Math.cos(windAngleRad);

    // Aero jump (constant MOA offset for the whole trajectory)
    const ajMOA = hasTwist ? aeroJump(sg, crosswindFps, params.muzzleVelocity) : 0;

    // Extend each trajectory row
    return trajectory.map(row => {
      const rangeFt = row.range * FT_PER_YD;
      const rangeIn = row.range * FT_PER_YD * IN_PER_FT;

      // Spin drift
      const sdIn = hasTwist ? spinDrift(sg, row.tof, twistDir) : 0;
      const sdMOA = rangeIn > 0 ? (sdIn / rangeIn) * (180 / Math.PI) * 60 : 0;

      // Coriolis
      const corH = latitude !== 0 ? coriolisHorizontal(latitude, azimuth, rangeFt, row.tof) : 0;
      const corV = latitude !== 0 ? coriolisVertical(latitude, azimuth,
        row.range > 0 ? rangeFt / row.tof : params.muzzleVelocity, row.tof) : 0;

      // Aero jump vertical shift at this range
      const ajIn = rangeIn > 0 ? ajMOA * (rangeIn / (60 * (180 / Math.PI))) : 0;

      // Wind drift recalculated with proper crosswind component
      const lagTime = row.range > 0 ? (row.tof - rangeFt / params.muzzleVelocity) : 0;
      const windDriftIn = Math.abs(crosswindFps) * lagTime * IN_PER_FT;
      const windDriftSign = crosswindFps >= 0 ? 1 : -1;

      // Total horizontal deflection (wind + spin + coriolis_H)
      const totalHIn = (windDriftSign * windDriftIn) + sdIn + corH;
      const totalHMOA = rangeIn > 0 ? (totalHIn / rangeIn) * (180 / Math.PI) * 60 : 0;

      // Total vertical adjustment (drop + coriolis_V + aero_jump)
      const totalVIn = row.drop + corV + ajIn;

      return {
        ...row,
        sg:             Math.round(sg * 100) / 100,
        spinDriftIn:    Math.round(sdIn * 100) / 100,
        spinDriftMOA:   Math.round(sdMOA * 100) / 100,
        coriolisHIn:    Math.round(corH * 100) / 100,
        coriolisVIn:    Math.round(corV * 100) / 100,
        aeroJumpMOA:    Math.round(ajMOA * 100) / 100,
        aeroJumpIn:     Math.round(ajIn * 100) / 100,
        windCrossIn:    Math.round(windDriftSign * windDriftIn * 100) / 100,
        windHeadFps:    Math.round(headwindFps * 10) / 10,
        totalHorizIn:   Math.round(totalHIn * 100) / 100,
        totalHorizMOA:  Math.round(totalHMOA * 100) / 100,
        totalVertIn:    Math.round(totalVIn * 100) / 100,
      };
    });
  }


  // ── PUBLIC API ─────────────────────────────────────────────────────────

  return {
    solve,
    solveExtended,
    rangeForElevation,
    velocityDispersion,
    hitProbability,
    combinedGroupSize,
    muzzleEnergy,
    millerSG,
    spinDrift,
    coriolisHorizontal,
    coriolisVertical,
    aeroJump,
    speedOfSound,
    densityRatio,
    pressureFromAltitude,
    // Expose for testing
    _getCd: getCd,
    _G1_TABLE: G1_TABLE,
    _G7_TABLE: G7_TABLE,
  };

})();
