"""Behind the curtain: how GroupLab works, NOTES-FROM-PLANNING.md entry 284.

Alan asked for a page, reached from the tour, that shows how the parts fit together, what each is built from, how OpenCV is used and how
the hole detector was developed. He chose concept B (a clickable map) with concept C's drawings and approved the refined design. This
module builds the four pages from website/how-it-works.json and the drawings in website/how-it-works/, for build.py to write:

- /tour/how-it-works/                 the map, the page the tour links to
- /tour/how-it-works/opencv/          how GroupLab uses OpenCV
- /tour/how-it-works/hole-detection/  how the hole detector was built
- /tour/how-it-works/pipeline/        a target, stage by stage

Three rules from the entry shape it:

1. It works without JavaScript. Every part's summary is a plain section with an anchor, every step of "Follow a target" a plain section,
   and the map's blocks are links to them. A small script turns the same markup into the clickable map; it adds, it never replaces.
2. The numbers come from one place. Every figure the pages show is in the data file's "figures", with the document or command it comes
   from, and the text refers to it as {fig:key}. A figure changed there changes on every page, and problems() fails the build on a text that
   names a figure the file does not have.
3. The drawings are SVG and follow the theme: they draw in currentColor and the site's own variables, never a fixed dark palette.
"""

from __future__ import annotations

import html
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
DATA = HERE / "how-it-works.json"
DRAWINGS = HERE / "how-it-works"
BASE = "/tour/how-it-works/"
FIG = re.compile(r"\{fig:([a-z0-9-]+)\}")


def data() -> dict:
    return json.loads(DATA.read_text(encoding="utf-8"))


def esc(s: str) -> str:
    return html.escape(s, quote=True)


def text(s: str, d: dict) -> str:
    """Escaped words with every {fig:key} replaced by the figure's value, and **bold** kept."""
    figures = d.get("figures", {})
    out = esc(FIG.sub(lambda m: figures.get(m.group(1), {}).get("value", "{fig:" + m.group(1) + "}"), s))
    return re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", out)


def plain(s: str, d: dict) -> str:
    """Words for an attribute: every {fig:key} replaced and the bold marks taken out, escaped."""
    figures = d.get("figures", {})
    return esc(FIG.sub(lambda m: figures.get(m.group(1), {}).get("value", ""), s).replace("**", ""))


def drawing(name: str, label: str, d: dict) -> str:
    """An inline SVG drawing, so it takes the page's colors, with its label for a screen reader."""
    svg = (DRAWINGS / name).read_text(encoding="utf-8").strip()
    svg = re.sub(r"<\?xml[^>]*\?>", "", svg)
    return f'<figure class="hiw-drawing" role="img" aria-label="{plain(label, d)}">{svg}</figure>'


def links_row(items: list[dict], d: dict) -> str:
    return '<p class="hiw-links">' + "".join(f'<a href="{esc(i["href"])}">&#8594; {text(i["label"], d)}</a>' for i in items) + "</p>"


def block(b: dict, d: dict) -> str:
    """One piece of a deep-dive section: paragraphs, a table, cards, drawings, a timeline, steps or a record."""
    kind = b["type"]
    if kind == "paragraphs":
        return "".join(f"<p>{text(p, d)}</p>" for p in b["items"])
    if kind == "list":
        return "<ul>" + "".join(f"<li>{text(p, d)}</li>" for p in b["items"]) + "</ul>"
    if kind == "table":
        head = "".join(f'<th scope="col">{text(h, d)}</th>' for h in b["head"])
        rows = "".join("<tr>" + "".join(f'<td data-label="{plain(b["head"][i], d)}">{text(c, d)}</td>' for i, c in enumerate(r)) + "</tr>" for r in b["rows"])
        caption = f"<caption>{text(b['caption'], d)}</caption>" if b.get("caption") else ""
        return f'<div class="hiw-table"><table>{caption}<thead><tr>{head}</tr></thead><tbody>{rows}</tbody></table></div>'
    if kind == "cards":
        cards = "".join(
            f'<div class="hiw-card{" hiw-card-hi" if c.get("highlight") else ""}">'
            + (f'<p class="hiw-badge">{text(c["badge"], d)}</p>' if c.get("badge") else "")
            + f'<h3>{text(c["title"], d)}</h3>'
            + "".join(f"<p>{text(p, d)}</p>" for p in (c["text"] if isinstance(c["text"], list) else [c["text"]]))
            + (f'<p class="hiw-file"><code>{esc(c["file"])}</code></p>' if c.get("file") else "")
            + "</div>"
            for c in b["cards"])
        return f'<div class="hiw-cards hiw-cols-{min(b.get("columns", 3), 6)}">{cards}</div>'
    if kind == "drawings":
        items = "".join(f'<div class="hiw-panel">{drawing(i["svg"], i["caption"], d)}<p class="small">{text(i["caption"], d)}</p></div>' for i in b["items"])
        return f'<div class="hiw-cards hiw-cols-{min(b.get("columns", 3), 6)}">{items}</div>'
    if kind == "timeline":
        items = "".join(
            f'<li><p class="hiw-date">{text(i["date"], d)}</p><div><h3>{text(i["title"], d)}</h3>'
            f'<p><strong>What happened.</strong> {text(i["happened"], d)}</p><p><strong>The rule now.</strong> {text(i["rule"], d)}</p>'
            + (f'<p class="hiw-file"><code>{esc(i["file"])}</code></p>' if i.get("file") else "") + "</div></li>"
            for i in b["items"])
        return f'<ol class="hiw-timeline">{items}</ol>'
    if kind == "numbers":
        return key_numbers(b["items"], d)
    if kind == "record":
        note = f'<p class="small faint">{text(b["note"], d)}</p>' if b.get("note") else ""
        return f'<pre class="hiw-record">{esc(b["text"])}</pre>{note}'
    raise ValueError(f"how-it-works.json: no block type {kind!r}")


def key_numbers(items: list[dict], d: dict) -> str:
    rows = "".join(f'<dt>{text(i["value"], d)}</dt><dd>{text(i["label"], d)}</dd>' for i in items)
    return f'<dl class="hiw-numbers">{rows}</dl>'


def part_section(part: dict, layer: dict, d: dict) -> str:
    """One part of the map as a plain section: readable with scripts off, and the panel's content with them on."""
    chips = "".join(f'<li>{text(c, d)}</li>' for c in part.get("builtFrom", []))
    where = "".join(f'<li><a href="{esc(w["href"])}"><code>{esc(w["label"])}</code></a></li>' for w in part.get("where", []))
    read = "".join(f'<li><a href="{esc(r["href"])}">{text(r["label"], d)}</a></li>' for r in part.get("read", []))
    deep = (f'<p><a class="btn btn-primary hiw-deep" href="{BASE}{part["deep"]}/">{text(part.get("deepLabel", "The deep dive"), d)} &#8595;</a></p>'
            if part.get("deep") else "")
    return (
        f'<section class="hiw-part" id="part-{esc(part["key"])}" data-part="{esc(part["key"])}">'
        f'<p class="hiw-kicker">{text(layer["label"], d)}</p>'
        f'<h2>{text(part["name"], d)}</h2>'
        + "".join(f"<p>{text(p, d)}</p>" for p in part["summary"])
        + (f'<h3 class="hiw-h4">Key numbers</h3>{key_numbers(part["numbers"], d)}' if part.get("numbers") else "")
        + (f'<h3 class="hiw-h4">Built from</h3><ul class="hiw-chips">{chips}</ul>' if chips else "")
        + (f'<h3 class="hiw-h4">Where it lives</h3><ul class="hiw-where">{where}</ul>' if where else "")
        + deep
        + (f'<h3 class="hiw-h4">Read more</h3><ul class="hiw-read">{read}<li><a href="/features/">Every feature</a></li></ul>' if read else "")
        + "</section>"
    )


def step_section(step: dict, d: dict) -> str:
    stages = "".join(f'<li><span class="hiw-code">{esc(s["code"])}</span> <strong>{text(s["title"], d)}.</strong> {text(s["text"], d)}</li>'
                     for s in step["stages"])
    return (
        f'<section class="hiw-step" id="step-{step["n"]}" data-step="{step["n"]}" data-parts="{esc(" ".join(step.get("parts", [])))}">'
        f'{drawing(step["drawing"], step["name"], d)}'
        f'<div><p class="hiw-kicker">Step {step["n"]} of {len(d["steps"])} &#183; {esc(step["codes"])}</p>'
        f'<h2>{text(step["name"], d)}</h2><p>{text(step["text"], d)}</p><ul class="hiw-stages">{stages}</ul>'
        f'<p><a href="{BASE}pipeline/#stage-{esc(step["stages"][0]["code"])}">Every stage in full &#8595;</a></p></div>'
        "</section>"
    )


def page_map(d: dict) -> str:
    m = d["map"]
    layers = []
    for layer in m["layers"]:
        blocks = "".join(
            f'<a class="hiw-block" href="#part-{esc(p["key"])}" data-part="{esc(p["key"])}">'
            f'<span class="hiw-name">{text(p["name"], d)}</span><span class="hiw-sub">{text(p["sub"], d)}</span>'
            f'<span class="hiw-path">{esc(p.get("path", ""))}</span></a>'
            for p in layer["parts"])
        layers.append(
            f'<div class="hiw-layer hiw-layer-{esc(layer["style"])}"><p class="hiw-layer-label"><span>{text(layer["label"], d)}</span> '
            f'{text(layer["note"], d)}</p><div class="hiw-blocks hiw-cols-{len(layer["parts"]) if len(layer["parts"]) <= 4 else 3}">{blocks}</div></div>')
    parts = "".join(part_section(p, layer, d) for layer in m["layers"] for p in layer["parts"])
    steps = "".join(step_section(s, d) for s in d["steps"])
    return f"""
<section class="wrap hiw" data-hiw-map>
<p class="small"><a href="/tour/">Tour</a> &#8250; Behind the curtain</p>
<div class="hiw-head"><div><h1>{text(m["title"], d)}</h1><p class="lead">{text(m["lead"], d)}</p></div>
<div class="hiw-toggle" role="group" aria-label="View" hidden><button type="button" data-view="parts" aria-pressed="true">The parts</button><button type="button" data-view="follow" aria-pressed="false">Follow a target</button></div></div>
<div class="hiw-grid">
<div class="hiw-map"><ol class="hiw-stepper" hidden></ol>{"".join(layers)}<p class="small faint">{text(m["footnote"], d)}</p></div>
<aside class="hiw-aside" aria-live="polite" hidden></aside>
</div>
<div class="hiw-plain">
<h2 class="hiw-list-head">The parts</h2>
{parts}
<h2 class="hiw-list-head" id="follow">Follow a target</h2>
<p>{text(m["followLead"], d)}</p>
{steps}
</div>
{links_row(m["links"], d)}
</section>
<script src="/assets/js/how-it-works.js" defer></script>
"""


def page_deep(key: str, d: dict) -> str:
    p = d["deep"][key]
    strip = "".join(f'<li{" aria-current=\"true\"" if c == p["layer"] else ""}>{esc(c)}</li>' for c in p["strip"])
    stats = "".join(f'<div class="hiw-stat"><p class="hiw-big">{text(s["value"], d)}</p><p class="small">{text(s["caption"], d)}</p></div>'
                    for s in p["stats"])
    sections = []
    for s in p["sections"]:
        anchor = f' id="{esc(s["id"])}"' if s.get("id") else ""
        sections.append(
            f'<section class="hiw-section"{anchor}><p class="hiw-kicker">{text(s["kicker"], d)}</p><h2>{text(s["heading"], d)}</h2>'
            + (f'<p class="hiw-intro">{text(s["intro"], d)}</p>' if s.get("intro") else "")
            + "".join(block(b, d) for b in s.get("blocks", []))
            + "</section>")
    return f"""
<nav class="hiw-strip" aria-label="Where this is on the map"><div class="wrap"><a href="{BASE}">&#8592; The map</a><ul>{strip}</ul></div></nav>
<article class="wrap hiw hiw-deep-page">
<p class="hiw-kicker">{text(p["kicker"], d)}</p>
<h1>{text(p["title"], d)}</h1>
<p class="lead">{text(p["lead"], d)}</p>
<div class="hiw-stats">{stats}</div>
{"".join(sections)}
{links_row(p["links"], d)}
</article>
"""


def pages() -> list[tuple[str, str, str, str]]:
    """Each page as (path, title, description, body), for build.py's shell."""
    d = data()
    out = [(BASE, d["map"]["pageTitle"], d["map"]["description"], page_map(d))]
    for key, p in d["deep"].items():
        out.append((f"{BASE}{key}/", p["pageTitle"], p["description"], page_deep(key, d)))
    return out


def problems(published: set[str], exists) -> list[str]:
    """What would publish something untrue or broken: a figure named in text but not in the file, a figure without its source, a drawing
    that is not there, a research link to an article not published, a repository path that does not exist."""
    d = data()
    found: list[str] = []
    figures = d.get("figures", {})
    for key, f in figures.items():
        if not f.get("value") or not f.get("source"):
            found.append(f"how-it-works.json: figure {key!r} needs a value and the document or command it comes from")
    # Section 2.6: each figure names where it comes from, and a named repository file must be there, so the audit can follow it.
    for key, f in figures.items():
        for rel in re.findall(r"(?:^|[\s(])((?:docs|src|android|scripts|tests|targets|website|tools|samples)/[\w./-]+\.(?:md|csproj|cs|py|json|sh|yml|txt|csv))(?![\w])", f.get("source", "")):
            if not exists(rel):
                found.append(f"how-it-works.json: figure {key!r} cites {rel}, which is not in the repository")
    raw = DATA.read_text(encoding="utf-8")
    for key in sorted(set(FIG.findall(raw)) - set(figures)):
        found.append(f"how-it-works.json: {{fig:{key}}} is used and not defined")
    for name in sorted(set(re.findall(r'"(?:drawing|svg)"\s*:\s*"([^"]+)"', raw))):
        if not (DRAWINGS / name).is_file():
            found.append(f"how-it-works.json: no drawing website/how-it-works/{name}")
    for slug in sorted(set(re.findall(r'"/research/([a-z0-9-]+)/', raw))):
        if slug not in published:
            found.append(f"how-it-works.json: links to research article {slug!r}, which is not published")
    for rel in sorted(set(re.findall(r'"https://github\.com/oRAirwolf/grouplab/(?:blob|tree)/main/([^"#]+)', raw))):
        if not exists(rel):
            found.append(f"how-it-works.json: links to {rel}, which is not in the repository")
    for svg in DRAWINGS.glob("*.svg"):
        colours = re.findall(r'(?:fill|stroke)="(#[0-9a-fA-F]{3,6})"', svg.read_text(encoding="utf-8"))
        if colours:
            found.append(f"website/how-it-works/{svg.name}: fixed colours {sorted(set(colours))[:4]}; use currentColor or the site's variables so it follows the theme")
    return found


CSS = r"""
/* Entry 284: Behind the curtain. */
.hiw [hidden]{display:none !important}
.hiw h1{font-size:clamp(34px,5vw,52px);line-height:1.08;max-width:900px}
.hiw-kicker{font-family:"IBM Plex Mono",monospace;font-size:12px;letter-spacing:.08em;text-transform:uppercase;color:var(--amber);margin:0 0 6px}
.hiw-head{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:flex-end;gap:16px}
.hiw-toggle{display:inline-flex;border:1px solid var(--line2);border-radius:4px;overflow:hidden}
.hiw-toggle button{min-height:44px;padding:0 16px;background:transparent;color:var(--text);border:0;font:inherit;cursor:pointer}
.hiw-toggle button[aria-pressed="true"]{background:var(--amber);color:var(--on-amber);font-weight:600}
.hiw-grid{display:grid;grid-template-columns:minmax(0,1fr);gap:28px;margin-top:24px}
.hiw-grid.hiw-live{grid-template-columns:minmax(0,1fr) minmax(0,440px)}
.hiw-layer{margin-bottom:14px}
.hiw-layer-label{font-size:12px;color:var(--dim);margin:0 0 6px}
.hiw-layer-label span{font-family:"IBM Plex Mono",monospace;text-transform:uppercase;letter-spacing:.06em;color:var(--amber);margin-right:8px}
.hiw-blocks{display:grid;gap:10px}
.hiw-cols-1{grid-template-columns:minmax(0,1fr)}.hiw-cols-2{grid-template-columns:repeat(2,minmax(0,1fr))}.hiw-cols-3{grid-template-columns:repeat(3,minmax(0,1fr))}
.hiw-cols-4{grid-template-columns:repeat(4,minmax(0,1fr))}.hiw-cols-5{grid-template-columns:repeat(5,minmax(0,1fr))}.hiw-cols-6{grid-template-columns:repeat(6,minmax(0,1fr))}
.hiw-block{display:flex;flex-direction:column;gap:2px;min-height:70px;padding:10px 12px;border-radius:8px;border:1px solid var(--line2);background:var(--panel2);color:var(--text);text-decoration:none}
.hiw-block:hover{text-decoration:none;border-color:var(--amber)}
.hiw-layer-core .hiw-block{background:var(--amber-tint);border-color:var(--amber-tint-b)}
.hiw-layer-check .hiw-block{background:transparent;border:1px dashed var(--teal-tint-b)}
.hiw-block[aria-pressed="true"]{border:2px solid var(--amber);padding:9px 11px}
.hiw-block.hiw-on-path{border-color:var(--amber)}
.hiw-name{font-size:15px;font-weight:600}.hiw-sub{font-size:12px;color:var(--dim)}.hiw-path{font-family:"IBM Plex Mono",monospace;font-size:11px;color:var(--faint);overflow-wrap:anywhere}
.hiw-aside{background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:24px;align-self:start;position:sticky;top:88px;max-height:calc(100vh - 110px);overflow-y:auto}
.hiw-plain{margin-top:40px}
.hiw-live-on .hiw-plain{display:none}
.hiw-part,.hiw-step{border-top:1px solid var(--line);padding:24px 0;scroll-margin-top:88px}
.hiw-aside .hiw-part,.hiw-aside .hiw-step{border:0;padding:0}
.hiw-step{display:grid;grid-template-columns:minmax(0,340px) minmax(0,1fr);gap:24px}
.hiw-aside .hiw-step{display:block}
.hiw-h4{font-size:13px;text-transform:uppercase;letter-spacing:.06em;color:var(--dim);margin:18px 0 6px}
.hiw-numbers{display:grid;grid-template-columns:110px minmax(0,1fr);gap:6px 12px;margin:0;padding:12px;background:var(--sunk);border-radius:8px}
.hiw-numbers dt{font-family:"IBM Plex Mono",monospace;color:var(--amber);font-weight:500}.hiw-numbers dd{margin:0;font-size:14px}
.hiw-chips{display:flex;flex-wrap:wrap;gap:6px;list-style:none;padding:0;margin:0}.hiw-chips li{background:var(--panel2);border-radius:999px;padding:2px 10px;font-size:13px}
.hiw-where,.hiw-read,.hiw-stages{padding-left:18px}.hiw-where code{font-size:12px}
.hiw-code{font-family:"IBM Plex Mono",monospace;color:var(--amber)}
.hiw-deep{display:block;text-align:center}
.hiw-stepper{display:grid;grid-template-columns:repeat(8,minmax(0,1fr));gap:6px;list-style:none;padding:8px;margin:0 0 14px;background:var(--panel);border-radius:12px}
.hiw-stepper button{width:100%;min-height:44px;background:transparent;border:1px solid var(--line2);border-radius:6px;color:var(--text);font:inherit;font-size:12px;cursor:pointer;padding:4px}
.hiw-stepper button[aria-current="step"]{border:2px solid var(--amber)}
.hiw-stepper .hiw-code{display:block;font-size:11px}
.hiw-drawing{margin:0;color:var(--text)}.hiw-drawing svg{width:100%;height:auto;display:block}
.hiw-strip{background:var(--sunk);border-bottom:1px solid var(--line);padding:12px 0;font-size:14px}
.hiw-strip .wrap{display:flex;flex-wrap:wrap;align-items:center;gap:12px}.hiw-strip ul{display:flex;flex-wrap:wrap;gap:6px;list-style:none;margin:0;padding:0}
.hiw-strip li{border:1px solid var(--line2);border-radius:999px;padding:2px 10px;font-size:12px;color:var(--dim)}
.hiw-strip li[aria-current="true"]{border-color:var(--amber);color:var(--amber)}
.hiw-stats{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;margin:24px 0 8px}
.hiw-stat{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px}.hiw-big{font-family:"IBM Plex Mono",monospace;font-size:30px;color:var(--amber);margin:0}
.hiw-section{margin-top:72px;max-width:1100px}.hiw-intro{max-width:880px}
.hiw-table{overflow-x:auto;border:1px solid var(--line);border-radius:12px;margin:16px 0}
.hiw-table table{border-collapse:collapse;width:100%;font-size:14px}.hiw-table th{background:var(--panel2);font-size:12px;text-transform:uppercase;letter-spacing:.04em;text-align:left;padding:10px 12px}
.hiw-table td{border-top:1px solid var(--line);padding:10px 12px;vertical-align:top}
.hiw-cards{display:grid;gap:12px;margin:16px 0}
.hiw-card,.hiw-panel{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px}
.hiw-card h3{font-size:16px;margin:0 0 6px}.hiw-card-hi{background:var(--amber-tint);border-color:var(--amber-tint-b)}
.hiw-badge{display:inline-block;font-size:11px;font-weight:600;border-radius:999px;padding:1px 8px;background:var(--panel2);margin:0 0 6px}
.hiw-file code{font-size:12px}
.hiw-timeline{list-style:none;padding:0;margin:16px 0;border-left:2px solid var(--line2)}
.hiw-timeline li{display:grid;grid-template-columns:150px minmax(0,1fr);gap:16px;padding:0 0 22px 18px;position:relative}
.hiw-timeline li::before{content:"";position:absolute;left:-7px;top:6px;width:12px;height:12px;border-radius:50%;background:var(--amber)}
.hiw-date{font-family:"IBM Plex Mono",monospace;color:var(--amber);margin:0}.hiw-timeline h3{margin:0 0 6px;font-size:17px}
.hiw-record{background:var(--sunk);border:1px solid var(--line);border-radius:8px;padding:14px;overflow-x:auto;font-size:12.5px;line-height:1.5}
.hiw-links{display:flex;flex-wrap:wrap;gap:8px 20px;border-top:1px solid var(--line2);padding-top:16px;margin-top:56px}
.hiw-tour-card{display:flex;flex-wrap:wrap;gap:16px;align-items:center;justify-content:space-between}
@media (max-width:900px){
.hiw-grid.hiw-live{grid-template-columns:minmax(0,1fr)}
.hiw-aside{position:static;max-height:none}
.hiw-cols-3,.hiw-cols-4,.hiw-cols-5,.hiw-cols-6{grid-template-columns:repeat(2,minmax(0,1fr))}
.hiw-stats{grid-template-columns:repeat(2,minmax(0,1fr))}
.hiw-step{grid-template-columns:minmax(0,1fr)}
.hiw-stepper{grid-template-columns:repeat(4,minmax(0,1fr))}
.hiw-timeline li{grid-template-columns:minmax(0,1fr);gap:4px}
.hiw-section{margin-top:52px}
/* PhoneDeep: a table becomes one card per row, each cell with its column's name. */
.hiw-table{border:0}.hiw-table table,.hiw-table tbody,.hiw-table tr,.hiw-table td{display:block;width:100%}
.hiw-table thead{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0)}
.hiw-table tr{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:10px 12px;margin-bottom:10px}
.hiw-table td{border:0;padding:3px 0}.hiw-table td::before{content:attr(data-label);display:block;font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:var(--dim)}
}
@media (max-width:520px){.hiw-cols-2,.hiw-cols-3,.hiw-cols-4,.hiw-cols-5,.hiw-cols-6{grid-template-columns:minmax(0,1fr)}.hiw-blocks.hiw-cols-3,.hiw-blocks.hiw-cols-4{grid-template-columns:repeat(2,minmax(0,1fr))}}
"""

JS = r"""
// Entry 284: the map made clickable. Without this script the page is a plain list of parts and steps; with it, the same sections move into
// the side panel. Nothing here fetches anything.
(function () {
  var root = document.querySelector("[data-hiw-map]");
  if (!root) return;
  var grid = root.querySelector(".hiw-grid"), aside = root.querySelector(".hiw-aside"), toggle = root.querySelector(".hiw-toggle");
  var stepper = root.querySelector(".hiw-stepper"), blocks = root.querySelectorAll(".hiw-block");
  var parts = {}, steps = [];
  root.querySelectorAll(".hiw-part").forEach(function (s) { parts[s.dataset.part] = s; });
  root.querySelectorAll(".hiw-step").forEach(function (s) { steps.push(s); });
  root.classList.add("hiw-live-on"); grid.classList.add("hiw-live"); aside.hidden = false; toggle.hidden = false; stepper.hidden = false;
  var view = "parts", current = "holes", step = 5;
  steps.forEach(function (s, i) {
    var li = document.createElement("li"), b = document.createElement("button");
    b.type = "button"; b.innerHTML = '<span class="hiw-code">' + s.querySelector(".hiw-kicker").textContent.split("·")[1] + "</span>" + s.querySelector("h2").textContent;
    b.addEventListener("click", function () { step = i + 1; show("follow"); });
    li.appendChild(b); stepper.appendChild(li);
  });
  function show(v) {
    view = v;
    toggle.querySelectorAll("button").forEach(function (b) { b.setAttribute("aria-pressed", String(b.dataset.view === view)); });
    stepper.hidden = view !== "follow";
    var onPath = view === "follow" ? (steps[step - 1].dataset.parts || "").split(" ") : [];
    blocks.forEach(function (b) {
      b.setAttribute("aria-pressed", String(view === "parts" && b.dataset.part === current));
      b.classList.toggle("hiw-on-path", onPath.indexOf(b.dataset.part) >= 0);
    });
    stepper.querySelectorAll("button").forEach(function (b, i) { if (i === step - 1) b.setAttribute("aria-current", "step"); else b.removeAttribute("aria-current"); });
    var source = view === "parts" ? parts[current] : steps[step - 1];
    aside.innerHTML = source ? source.innerHTML : "";
  }
  toggle.addEventListener("click", function (e) { var b = e.target.closest("button"); if (b) show(b.dataset.view); });
  blocks.forEach(function (b) {
    b.addEventListener("click", function (e) { e.preventDefault(); current = b.dataset.part; show("parts"); if (window.innerWidth <= 900) aside.scrollIntoView({ behavior: "smooth", block: "start" }); });
  });
  if (location.hash.indexOf("#part-") === 0 && parts[location.hash.slice(6)]) current = location.hash.slice(6);
  if (location.hash.indexOf("#step-") === 0) { step = +location.hash.slice(6) || 5; view = "follow"; }
  if (!parts[current]) current = Object.keys(parts)[0];
  show(location.hash === "#follow" ? "follow" : view);
})();
"""
