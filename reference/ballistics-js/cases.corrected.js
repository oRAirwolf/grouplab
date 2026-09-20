// Runs ballistics.corrected.js on every case in reference/ballistics-cases.json and prints the rows as JSON, in the shape cases.js prints
// them. GroupLab's CorrectedJavaScriptTests reads it and holds the corrected file to GroupLab's own solver, under the tolerances
// docs/BALLISTICS-VALIDATION.md section 2 committed before any comparison was run. CI writes it before the tests run, since it needs Node.
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const source = fs.readFileSync(path.join(__dirname, 'ballistics.corrected.js'), 'utf8');
const context = vm.createContext({ Math });
vm.runInContext(source + '\n;globalThis.__solver = BallisticSolver;', context);
const solver = context.__solver;

const spec = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'ballistics-cases.json'), 'utf8'));
const cases = spec.cases.map(c => ({
  name: c.name,
  rows: solver.solve({ ...c, maxRange: spec.maxRange, rangeStep: spec.rangeStep, angleDeg: 0 }),
}));

process.stdout.write(JSON.stringify({ file: 'ballistics.corrected.js', cases }, null, 2) + '\n');
