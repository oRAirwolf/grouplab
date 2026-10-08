// NOTES-FROM-PLANNING.md entry 360 section 3 as amended (entry 361 section 1): the hard stop. A PreToolUse hook on every tool call, in
// workers too, reading the week's percentage as docs/notes/usage-now.json last recorded it (scripts/usage-statusline.js writes it).
// Under 85: exit 0. From 85: exit 2, which blocks the call, unless docs/notes/finishing.flag exists and is under 45 minutes old, so a
// block already under way (above all a publish) can finish. From 88: exit 2 whatever the flag says. With the file missing or unreadable
// it exits 0: a broken measure must not lock the project (section 4 covers that case). No network and nothing slow.
const fs = require("fs");
const path = require("path");

// Alan, 2026-10-07: stop at 100 for this week only, raised from 98, 95 and the 88 he set on 2026-10-06 (entry 376), to use the rest of
// the week before it resets on 2026-10-08 02:00 UTC. Until then the line is 100, and the finishing flag buys nothing; from the reset it
// is 85 again, with 88 the last line for a finishing block, and no edit needed.
const RAISED_UNTIL = Date.UTC(2026, 9, 8, 2, 0);
const RAISED = Date.now() < RAISED_UNTIL;
const BACKSTOP = RAISED ? 100 : 88;
const LIMIT = RAISED ? BACKSTOP : 85;
const FLAG_MINUTES = 45;
// Two readings of the same subscription figure: the status line's file, and Claude Code's own cache of the usage it last fetched
// (cachedUsageUtilization in ~/.claude.json), which is there even where no status line runs, as in the VS Code extension. Either at 85
// or more blocks; neither is a guess from token counts.
function readings() {
  const found = [];
  try {
    const file = process.env.GROUPLAB_USAGE_FILE || path.join(__dirname, "..", "docs", "notes", "usage-now.json");
    found.push(JSON.parse(fs.readFileSync(file, "utf8")).seven_day);
  } catch {}
  if (!process.env.GROUPLAB_USAGE_FILE) {
    try {
      const home = process.env.USERPROFILE || process.env.HOME || "";
      const cached = JSON.parse(fs.readFileSync(path.join(home, ".claude.json"), "utf8")).cachedUsageUtilization;
      found.push(cached?.utilization?.seven_day?.utilization);
    } catch {}
  }
  return found.filter((v) => typeof v === "number" && isFinite(v));
}

function finishing() {
  try {
    const flag = process.env.GROUPLAB_FINISHING_FLAG || path.join(__dirname, "..", "docs", "notes", "finishing.flag");
    return Date.now() - fs.statSync(flag).mtimeMs < FLAG_MINUTES * 60 * 1000;
  } catch {
    return false;
  }
}

const highest = Math.max(-Infinity, ...readings());

if (highest >= BACKSTOP) {
  process.stderr.write(`Weekly budget reached (${BACKSTOP}%), the last line. Alan said to stop. Stop now.\n`);
  process.exit(2);
}
if (highest >= LIMIT && !finishing()) {
  process.stderr.write(`Weekly budget reached (${LIMIT}%). Alan said to stop. Stop now.\n`);
  process.exit(2);
}
process.exit(0);
