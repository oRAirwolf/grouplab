// NOTES-FROM-PLANNING.md entry 448 section 2: what a person can tap on the home page, checked in a real browser rather than by reading
// the styles. The open menu's every link must be the element found at its left edge, its middle and its right edge, with the big wordmark
// undocked; and with the menu closed, every link and button on screen must be the element at its middle at six scroll positions, at a
// phone's width and a desktop's, so no decorative layer (the wordmark, the orbit, the rings, the ruler) ever covers something to tap.
//
//   node tests/site/taps.mjs website/_site
//
// It serves the built site itself and drives Chrome (or Edge on Windows) headless over its debugging port; CHROME names another browser.
import { spawn } from "node:child_process";
import { createServer } from "node:http";
import { readFile, mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";

const root = path.resolve(process.argv[2] ?? "website/_site");
const types = { ".html": "text/html", ".css": "text/css", ".js": "text/javascript", ".svg": "image/svg+xml", ".webp": "image/webp", ".png": "image/png", ".woff2": "font/woff2", ".json": "application/json", ".ico": "image/x-icon" };
const server = createServer(async (req, res) => {
  let file = path.join(root, decodeURIComponent(new URL(req.url, "http://x").pathname));
  if (file.endsWith(path.sep) || !path.extname(file)) file = path.join(file, "index.html");
  try {
    res.writeHead(200, { "content-type": types[path.extname(file)] ?? "application/octet-stream" });
    res.end(await readFile(file));
  } catch {
    res.writeHead(404);
    res.end();
  }
});
await new Promise((r) => server.listen(0, "127.0.0.1", r));
const base = `http://127.0.0.1:${server.address().port}`;

const browser = process.env.CHROME ?? ["C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe", "C:/Program Files/Google/Chrome/Application/chrome.exe",
  "/usr/bin/google-chrome", "/usr/bin/chromium-browser", "/usr/bin/chromium"].find((p) => existsSync(p));
if (!browser) {
  console.error("taps: no Chrome or Edge to drive; set CHROME");
  process.exit(2);
}
const profile = await mkdtemp(path.join(tmpdir(), "grouplab-taps-"));
const port = 9300 + Math.floor(Math.random() * 500);
const child = spawn(browser, ["--headless=new", "--disable-gpu", "--no-first-run", "--no-sandbox", `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let list;
// Entry 461: on a busy runner Chrome once took longer than 20 s to answer (run 39539787075), so it has a minute, and saying so.
for (let i = 0; i < 240 && !list?.some((t) => t.type === "page"); i++) {
  await sleep(250);
  try { list = await (await fetch(`http://127.0.0.1:${port}/json`)).json(); } catch {}
}
if (!list?.some((t) => t.type === "page")) {
  console.error("taps: the browser gave no page to drive within a minute");
  child.kill();
  process.exit(2);
}
const ws = new WebSocket(list.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener("open", r, { once: true }));
let id = 0;
const waiting = new Map();
ws.addEventListener("message", (e) => {
  const m = JSON.parse(e.data);
  if (m.id && waiting.has(m.id)) { waiting.get(m.id)(m); waiting.delete(m.id); }
});
const send = (method, params = {}) => new Promise((r) => { const n = ++id; waiting.set(n, r); ws.send(JSON.stringify({ id: n, method, params })); });
const run = async (expression) => (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true })).result.result.value;

// In the page: whether the element at each point is the control or inside it, and what covers it where not.
const probe = `
window.__covered = (el, xs) => {
  const r = el.getBoundingClientRect(), y = r.top + r.height / 2;
  return xs(r).map((x) => { const hit = document.elementFromPoint(x, y); return hit && (hit === el || el.contains(hit)) ? null
    : (hit ? hit.tagName + (hit.className && typeof hit.className === "string" ? "." + hit.className.split(" ")[0] : "") : "nothing"); }).filter(Boolean);
};
window.__name = (el) => (el.getAttribute("aria-label") || el.textContent || el.tagName).trim().replace(/\\s+/g, " ").slice(0, 40);
`;
const faults = [];
for (const [width, height, mobile] of [[390, 844, true], [1440, 900, false], [1280, 800, false]]) {
  await send("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile });
  await send("Page.navigate", { url: base + "/" });
  await sleep(1500);
  await run(probe);

  // The menu open with the wordmark undocked, at the top of the page.
  const menu = await run(`(async () => {
    scrollTo(0, 0); await new Promise((r) => setTimeout(r, 400));
    const m = [...document.querySelectorAll("details.tp-menu")].find((d) => d.offsetParent !== null);
    if (!m) return null;
    m.querySelector("summary").click(); await new Promise((r) => setTimeout(r, 400));
    const links = [...m.querySelectorAll(".menu-panel a")].filter((a) => a.getClientRects().length > 0);
    const out = links.map((a) => ({ name: __name(a), covered: __covered(a, (r) => [r.left + 4, r.left + r.width / 2, r.right - 4]) }));
    m.querySelector("summary").click(); await new Promise((r) => setTimeout(r, 300));
    const stillOpen = m.open;
    // It closes on Escape, on a tap outside it and on choosing a link (the link's own navigation held back for the check).
    const closes = {};
    m.open = true; document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true })); closes.escape = !m.open;
    m.open = true; document.querySelector("main").click(); closes.outside = !m.open;
    m.open = true; const a = links[0]; a.addEventListener("click", (e) => e.preventDefault(), { once: true }); a.click(); closes.link = !m.open;
    m.open = false;
    return { open: stillOpen, links: out, closes };
  })()`);
  if (menu) {
    for (const link of menu.links.filter((l) => l.covered.length > 0)) {
      faults.push(`${width} px, menu open: "${link.name}" is under ${link.covered.join(", ")}`);
    }
    if (menu.links.length === 0) faults.push(`${width} px: the menu opened with no link showing`);
    if (menu.open) faults.push(`${width} px: the menu did not close when its button was pressed again`);
    for (const [how, closed] of Object.entries(menu.closes)) {
      if (!closed) faults.push(`${width} px: the menu does not close on ${how === "link" ? "choosing a link" : how === "outside" ? "a tap outside it" : "Escape"}`);
    }
  }

  // The menu closed: every link and button on screen, at six scroll positions, outside the header's own band.
  for (const y of [0, 150, 300, 450, 700, 1000]) {
    const found = await run(`(async () => {
      scrollTo(0, ${y}); await new Promise((r) => setTimeout(r, 450));
      const header = document.querySelector(".tp-header").getBoundingClientRect();
      const out = [];
      for (const el of document.querySelectorAll("a[href], button, summary")) {
        if (el.closest("details:not([open])") && el.tagName !== "SUMMARY") continue;
        const r = el.getBoundingClientRect(), cy = r.top + r.height / 2;
        if (r.width < 2 || r.height < 2 || cy < 0 || cy > innerHeight || r.left + r.width / 2 < 0 || r.left + r.width / 2 > innerWidth) continue;
        if (!el.closest(".tp-header") && cy < header.bottom) continue;
        if (getComputedStyle(el).visibility === "hidden" || el.closest("[aria-hidden='true']")) continue;
        const c = __covered(el, (r) => [r.left + r.width / 2]);
        if (c.length) out.push(__name(el) + " under " + c[0]);
      }
      return out;
    })()`);
    for (const f of found) faults.push(`${width} px, scrolled ${y}: ${f}`);
  }
}

ws.close();
child.kill();
server.close();
await rm(profile, { recursive: true, force: true }).catch(() => {});
if (faults.length) {
  console.log(faults.join("\n"));
  console.log(`taps: ${faults.length} controls a person could not tap`);
  process.exit(1);
}
console.log("taps: every menu link and every control on the home page is the element at its own middle");
