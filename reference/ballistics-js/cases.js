// Runs ballistics.js, unchanged, on every case in reference/ballistics-cases.json and on a grid of its helper functions, and prints the
// results as JSON. GroupLab's transcription test compares its port against this output (docs/BALLISTICS-VALIDATION.md section 1). CI runs it
// again on every push and fails if the output differs from tests/GroupLab.Core.Tests/Fixtures/ballistics-js.json.
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

// The file declares BallisticSolver at the top level of a browser script; evaluating it in a context of its own and reading the binding back
// leaves the file itself untouched.
const source = fs.readFileSync(path.join(__dirname, 'ballistics.js'), 'utf8');
const context = vm.createContext({ Math });
vm.runInContext(source + '\n;globalThis.__solver = BallisticSolver;', context);
const solver = context.__solver;

const spec = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'ballistics-cases.json'), 'utf8'));
const cases = spec.cases.map(c => ({
  name: c.name,
  rows: solver.solve({ ...c, maxRange: spec.maxRange, rangeStep: spec.rangeStep, angleDeg: 0 }),
}));

const machs = [];
for (let i = 0; i <= 520; i++) {
  machs.push(Math.round(i) / 100);
}

const atmospheres = [];
for (const tempF of [-10, 20, 40, 59, 75, 90, 110]) {
  for (const pressureInHg of [24.0, 26.0, 28.5, 29.92, 30.4]) {
    for (const humidityPct of [0, 30, 50, 78, 100]) {
      atmospheres.push({
        tempF, pressureInHg, humidityPct,
        densityRatio: solver.densityRatio(tempF, pressureInHg, humidityPct),
        speedOfSound: solver.speedOfSound(tempF, pressureInHg, humidityPct),
      });
    }
  }
}

const altitudes = [0, 500, 1000, 2500, 5000, 7500, 10000].map(altitudeFt => ({ altitudeFt, pressureInHg: solver.pressureFromAltitude(altitudeFt, 59) }));

const stability = [
  [8, 0.264, 1.48, 153, 2700, 59],
  [10, 0.308, 1.24, 175, 2700, 40],
  [7, 0.224, 1.05, 77, 2750, 90],
  [11.25, 0.308, 1.12, 168, 2650, 59],
].map(([twist, diameter, length, weight, velocity, tempF]) => ({ twist, diameter, length, weight, velocity, tempF, sg: solver.millerSG(twist, diameter, length, weight, velocity, tempF) }));

const spin = [];
for (const sg of [1.2, 1.5, 2.0]) {
  for (const tof of [0.1, 0.5, 1.0, 1.5, 2.0]) {
    spin.push({ sg, tof, right: solver.spinDrift(sg, tof, 1), left: solver.spinDrift(sg, tof, -1) });
  }
}

const coriolis = [];
for (const latitude of [-45, 0, 30, 45, 60]) {
  for (const [rangeFt, tof] of [[300, 0.12], [1500, 0.7], [3000, 1.6]]) {
    coriolis.push({ latitude, rangeFt, tof, horizontal: solver.coriolisHorizontal(latitude, 0, rangeFt, tof) });
  }
}

const tables = {
  G1: machs.map(m => [m, solver._getCd(m, solver._G1_TABLE)]),
  G7: machs.map(m => [m, solver._getCd(m, solver._G7_TABLE)]),
};

process.stdout.write(JSON.stringify({ source: 'reference/ballistics-js/ballistics.js', cases, tables, atmospheres, altitudes, stability, spin, coriolis }, null, 1) + '\n');
