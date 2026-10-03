// NOTES-FROM-PLANNING.md entry 360 section 1: the Claude Code status line, which is handed the subscription's own rate limits on stdin
// (rate_limits.seven_day and five_hour, used_percentage and resets_at). It writes them to docs/notes/usage-now.json, which is never
// committed, for the soft stop at 80% and the hook at 85% (scripts/usage-guard.js) to read, and prints a short line for the status bar.
// It never reaches the network and never fails loudly: a status line that throws would only blank the bar.
const fs = require("fs");
const path = require("path");

let input = "";
process.stdin.setEncoding("utf8");
process.stdin.on("data", (chunk) => (input += chunk));
process.stdin.on("end", () => {
  let line = "week ? | 5h ?";
  try {
    const data = JSON.parse(input || "{}");
    const limits = data.rate_limits || {};
    const week = limits.seven_day || {};
    const five = limits.five_hour || {};
    const pct = (v) => (typeof v === "number" && isFinite(v) ? Math.round(v * 10) / 10 : null);
    const now = {
      seven_day: pct(week.used_percentage),
      five_hour: pct(five.used_percentage),
      seven_day_resets_at: week.resets_at ?? null,
      written_utc: new Date().toISOString(),
    };
    if (now.seven_day !== null) {
      const file = path.join(__dirname, "..", "docs", "notes", "usage-now.json");
      fs.writeFileSync(file, JSON.stringify(now, null, 1) + "\n");
    }
    line = `week ${now.seven_day ?? "?"}% | 5h ${now.five_hour ?? "?"}%`;
  } catch {
    // The bar says it does not know rather than showing a stale or invented number.
  }
  process.stdout.write(line);
});
