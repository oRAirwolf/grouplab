// The site's page audit, entry 462 section 1: every page the builder made, opened in a real browser at a phone's width and a
// desktop's, in both themes. It reports what a reader would hit: the page wider than the screen, text cut off inside its box, a picture
// or file that does not load, a link to a page that is not there, and every accessibility finding axe-core makes (WCAG 2.1 A and AA,
// colour contrast included). It changes nothing.
//
//   node scripts/site-audit.cjs [--site website/_site] [--widths 390,1440] [--themes light,dark] [--only a11y] [--shots DIR] [--json FILE]
//
// website/a11y-pending.json lists accessibility findings that wait on a look decision sent to Alan as DESIGN NEEDED: each is
// {rule, pages (a path prefix), theme, why}. They are printed as waiting and do not fail the run; anything else does.
//
// Needs playwright-core and @axe-core/playwright where Node can require them (NODE_PATH), and Chrome: CHROME names it, or the
// installed stable channel is used. --only a11y runs axe alone, which is what checks.yml runs on every push. Exit 1 on any finding.
"use strict";
const fs = require("fs");
const http = require("http");
const path = require("path");
const { chromium } = require("playwright-core");
const { AxeBuilder } = require("@axe-core/playwright");

function arg(name, fallback) {
  const i = process.argv.indexOf(`--${name}`);
  return i > 0 && i + 1 < process.argv.length ? process.argv[i + 1] : fallback;
}

const site = path.resolve(arg("site", path.join(__dirname, "..", "website", "_site")));
const widths = arg("widths", "390,1440").split(",").map(Number);
const themes = arg("themes", "light,dark").split(",");
const only = arg("only", "");
const shots = arg("shots", "");
const jsonOut = arg("json", "");

const types = { ".html": "text/html; charset=utf-8", ".css": "text/css", ".js": "text/javascript", ".svg": "image/svg+xml",
  ".webp": "image/webp", ".png": "image/png", ".jpg": "image/jpeg", ".json": "application/json", ".pdf": "application/pdf",
  ".woff2": "font/woff2", ".ico": "image/x-icon", ".xml": "application/xml", ".txt": "text/plain" };

// What a request path is on disk: a folder is its index.html, as the server answers it.
function onDisk(urlPath) {
  let p = decodeURIComponent(urlPath.split("?")[0].split("#")[0]);
  let f = path.join(site, p);
  if (!f.startsWith(site)) return null;
  if (fs.existsSync(f) && fs.statSync(f).isDirectory()) f = path.join(f, "index.html");
  return fs.existsSync(f) ? f : null;
}

function pages() {
  const out = [];
  (function walk(dir) {
    for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
      const f = path.join(dir, e.name);
      if (e.isDirectory()) walk(f);
      else if (e.name.endsWith(".html")) out.push("/" + path.relative(site, f).split(path.sep).join("/").replace(/index\.html$/, ""));
    }
  })(site);
  return out.sort();
}

async function main() {
  const server = http.createServer((req, res) => {
    const f = onDisk(req.url);
    if (!f) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { "content-type": types[path.extname(f)] || "application/octet-stream" });
    fs.createReadStream(f).pipe(res);
  });
  await new Promise(r => server.listen(0, "127.0.0.1", r));
  const base = `http://127.0.0.1:${server.address().port}`;
  const browser = await chromium.launch(process.env.CHROME ? { executablePath: process.env.CHROME } : { channel: "chrome" });
  const findings = [];
  const seenAxe = new Set();
  const say = (page, where, what, rule) => findings.push({ page, where, what, rule });
  const pendingFile = path.join(__dirname, "..", "website", "a11y-pending.json");
  const pending = fs.existsSync(pendingFile) ? JSON.parse(fs.readFileSync(pendingFile, "utf8")) : [];
  const waits = f => f.rule && pending.some(w => w.rule === f.rule && f.page.startsWith(w.pages) && (!w.theme || w.theme === f.where));
  const all = pages();
  if (!all.length) throw new Error(`no pages in ${site}; build the site first`);
  const internal = new Set();

  for (const p of all) {
    for (const theme of themes) {
      for (const width of widths) {
        if (only === "a11y" && width !== widths[0]) continue;
        const ctx = await browser.newContext({ viewport: { width, height: 900 }, colorScheme: theme, reducedMotion: "reduce", deviceScaleFactor: 1 });
        // Only the built site is audited: anything from elsewhere (a store badge, the person check) is refused, so a slow outside
        // server cannot hold a page open and the result is the same on every machine.
        await ctx.route("**/*", r => r.request().url().startsWith(base) ? r.continue() : r.abort());
        const page = await ctx.newPage();
        const failed = [];
        // The survey's totals are written on the server by its worker and kept in place by the sync, so the build never has them.
        page.on("response", r => { if (r.status() >= 400 && r.url().startsWith(base) && !r.url().endsWith("/survey/aggregate.json")) failed.push(`${r.status()} ${r.url().slice(base.length)}`); });
        page.on("requestfailed", r => { if (r.url().startsWith(base) && r.failure().errorText !== "net::ERR_ABORTED") failed.push(`failed ${r.url().slice(base.length)}`); });
        await page.goto(base + p, { waitUntil: "load" });
        const where = `${width} px, ${theme}`;
        if (only !== "a11y") {
          // Lazy pictures load as they come into view; load them all so a missing one is found.
          await page.evaluate(() => document.querySelectorAll("img[loading=lazy]").forEach(i => { i.loading = "eager"; }));
          await page.waitForLoadState("networkidle").catch(() => {});
          const seen = await page.evaluate(() => {
            const doc = document.documentElement;
            const wide = doc.scrollWidth > doc.clientWidth + 1 ? doc.scrollWidth : 0;
            const sticking = [];
            const cut = [];
            const broken = [];
            for (const el of document.body.querySelectorAll("*")) {
              const r = el.getBoundingClientRect();
              if (!r.width || !r.height) continue;
              const cs = getComputedStyle(el);
              if (cs.visibility === "hidden") continue;
              const name = el.tagName.toLowerCase() + (el.id ? "#" + el.id : "") + (el.className && typeof el.className === "string" ? "." + el.className.trim().split(/\s+/).join(".") : "");
              if (wide && r.right > doc.clientWidth + 1 && el.parentElement.getBoundingClientRect().right <= doc.clientWidth + 1) {
                // Only what really reaches past the page: an ancestor that clips or scrolls sideways holds it in.
                let held = false;
                for (let a = el.parentElement; a && a !== document.body && !held; a = a.parentElement)
                  held = getComputedStyle(a).overflowX !== "visible";
                if (!held) sticking.push(name + " \"" + el.textContent.trim().slice(0, 40) + "\"");
              }
              // Cut off: words reaching past a box that hides what overflows it. A box that scrolls is not cutting anything, a box of
              // one pixel is text kept for screen readers, and data-clips marks a box that clips on purpose (the home page's ticker).
              const clips = /(hidden|clip)/.test(cs.overflowX) || cs.textOverflow === "ellipsis";
              if (clips && r.width > 2 && el.scrollWidth > el.clientWidth + 2 && !el.closest("[aria-hidden=true],[data-clips]")) {
                const edge = r.right + 1;
                const words = [...el.querySelectorAll("*")].concat([el]).some(t =>
                  [...t.childNodes].some(n => n.nodeType === 3 && n.textContent.trim()) && t.getBoundingClientRect().right > edge
                  && !t.closest("[aria-hidden=true]"));
                if (words || cs.textOverflow === "ellipsis") cut.push(name);
              }
              if (el.tagName === "IMG" && el.complete && el.naturalWidth === 0) broken.push(el.getAttribute("src"));
            }
            // Words over words: two boxes of visible text, neither inside the other, sharing more than a few pixels.
            const texts = [...document.body.querySelectorAll("*")].filter(el => [...el.childNodes].some(n => n.nodeType === 3 && n.textContent.trim())
              && !el.closest("[aria-hidden=true],[data-clips]") && !(el.closest("thead") && el.closest("thead").getBoundingClientRect().width <= 2) && el.checkVisibility({ opacityProperty: true, visibilityProperty: true, contentVisibilityAuto: true }))
              // A link wrapped over two lines has one box covering both; its line boxes are what is drawn.
              // Text scrolled out of sight inside a box that scrolls or clips is not drawn there, so only what shows inside it counts.
              .map(el => {
                let box = null;
                for (let a = el.parentElement; a && a !== document.body && !box; a = a.parentElement)
                  if (getComputedStyle(a).overflowY !== "visible" || getComputedStyle(a).overflowX !== "visible") box = a.getBoundingClientRect();
                const shown = r => !box || (r.bottom > box.top + 2 && r.top < box.bottom - 2 && r.right > box.left + 2 && r.left < box.right - 2);
                return { el, rs: [...el.getClientRects()].filter(r => r.width > 2 && r.height > 2 && shown(r)) };
              }).filter(x => x.rs.length);
            // A tall line box (the home page's word mark) reaches past its letters, so a share of the shorter line has to be covered.
            const meet = (r, s) => Math.min(r.right, s.right) - Math.max(r.left, s.left) > 4
              && Math.min(r.bottom, s.bottom) - Math.max(r.top, s.top) > Math.max(4, Math.min(r.height, s.height) / 3);
            const over = [];
            for (let i = 0; i < texts.length && over.length < 5; i++) for (let j = i + 1; j < texts.length; j++) {
              const a = texts[i], b = texts[j];
              if (a.el.contains(b.el) || b.el.contains(a.el)) continue;
              if (a.rs.some(r => b.rs.some(s => meet(r, s)))) { over.push(`"${a.el.textContent.trim().slice(0, 30)}" and "${b.el.textContent.trim().slice(0, 30)}"`); break; }
            }
            const links = [...document.querySelectorAll("a[href]")].map(a => a.getAttribute("href"));
            return { wide, sticking: [...new Set(sticking)].slice(0, 5), cut: [...new Set(cut)].slice(0, 5), broken, links, over };
          });
          if (seen.wide) say(p, where, `wider than the screen (${seen.wide} px); sticking out: ${seen.sticking.join(", ")}`);
          for (const c of seen.cut) say(p, where, `text cut off inside ${c}`);
          for (const o of seen.over) say(p, where, `words over words: ${o}`);
          for (const b of seen.broken) say(p, where, `picture does not load: ${b}`);
          for (const f of [...new Set(failed)]) say(p, where, `does not load: ${f}`);
          for (const h of seen.links) if (h.startsWith("/") && !h.startsWith("//")) internal.add(`${p}\t${h}`);
          if (shots) {
            const file = path.join(shots, `${(p.replace(/\//g, "_").replace(/^_|_$/g, "") || "home")}-${width}-${theme}.png`);
            fs.mkdirSync(shots, { recursive: true });
            await page.screenshot({ path: file, fullPage: true });
          }
        }
        if (width === widths[0]) {
          const axe = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
          for (const v of axe.violations) {
            for (const n of v.nodes) {
              const key = `${p}|${v.id}|${n.target.join(" ")}|${theme}`;
              if (seenAxe.has(key)) continue;
              seenAxe.add(key);
              say(p, theme, `${v.id}: ${n.target.join(" ")}${v.id === "color-contrast" ? " " + (n.any[0] && n.any[0].message || "").replace(/\s+/g, " ").slice(0, 160) : ""}`, v.id);
            }
          }
        }
        await ctx.close();
      }
    }
  }

  for (const pair of internal) {
    const [from, href] = pair.split("\t");
    if (!onDisk(href)) say(from, "link", `goes nowhere: ${href}`);
  }

  await browser.close();
  server.close();
  const open = findings.filter(f => !waits(f));
  for (const f of open) console.log(`${f.page}  [${f.where}]  ${f.what}`);
  const waiting = findings.length - open.length;
  console.log(`site audit: ${all.length} pages, ${open.length} findings` + (waiting ? `, ${waiting} more waiting on a look decision (website/a11y-pending.json)` : ""));
  if (jsonOut) fs.writeFileSync(jsonOut, JSON.stringify(findings, null, 1));
  process.exitCode = open.length ? 1 : 0;
}

main().catch(e => { console.error(e); process.exitCode = 2; });
