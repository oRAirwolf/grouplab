// NOTES-FROM-PLANNING.md entry 360 section 3: the hard stop. A PreToolUse hook on every tool call, in workers too: at 85% or more of the
// weekly allowance, as docs/notes/usage-now.json last recorded it (scripts/usage-statusline.js writes it), it says so and exits 2, which
// blocks the call. Under 85, or with the file missing or unreadable, it exits 0: a broken measure must not lock the project (section 4
// covers that case). No network and nothing slow, since it runs before every tool call.
const fs = require("fs");
const path = require("path");

const LIMIT = 85;
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

const blocked = readings().some((v) => v >= LIMIT);

if (blocked) {
  process.stderr.write("Weekly budget reached (85%). Alan said to stop. Commit nothing more; stop.\n");
  process.exit(2);
}
process.exit(0);
