# 2026-09-21, entry 136: a release notes page on grouplab.org

Alan wants a **Release notes** page on the website, with each version's notes expanding and collapsing, starting from the first build that was made available and covering every version since.

## 1. The page

1. `https://grouplab.org/releases/`, linked from the site's navigation and from the Download page ("What changed in each build").
2. One block per version, **newest first**, each a collapsible section (HTML `<details>` and `<summary>`, so it works without JavaScript). The newest is open; the rest are closed. An "Expand all / Collapse all" control is a nice addition if it costs little.
3. Each block's heading shows the version, its train (release, beta, nightly), the date, and the short commit. Inside: the notes grouped under New, Fixed and Changed, in plain words with the entry reference in brackets, then a link to that version's own downloads on GitHub (`releases/tag/v<version>`), and any known issue for that build.
4. Each block has an anchor (`#v0-2-0-nightly-31` or similar) so a link can open one version. The application's update bar "Show all" (entry 119 section 4.3) links to the newest version's anchor on this page, through `IOutsideWorld`.
5. The approved design, the site's checks (no em dash, banned terms, no addresses) and fingerprinted assets, as for every other page.

## 2. Where the notes come from

1. **The source of truth is a file in the repository**, `docs/RELEASE-NOTES.md`, which also reads well on GitHub. The website builder turns it into the page; the build never fetches anything from the network.
2. **Write the history by hand now**, from the commits, `docs/NOTES-FROM-PLANNING.md` and `docs/PHASE1-RESULTS.md`, in the plain `Release-note:` style of entry 132:
   - **v0.1.0**, 2026-09-21, from commit 5a4cd07: the first build anyone could download (the Windows installer and zip, and the Linux tarball, with no .NET needed). Say it was published as a release by mistake, is now marked a pre-release, and cannot update itself.
   - **Every published nightly since**: 0.2.0-nightly.12, .14, .16, .18, .25 onward to the newest. Say plainly why the numbers skip (runs cancelled or skipped before publishing, fixed on 2026-09-21), and give each build's known issues: 12 and 14 call themselves development builds and cannot update themselves; 16 and 18 refuse their own update signature and cannot update themselves; 25 and later update themselves.
   - Do not list the hand-made draft that was deleted, and do not edit any published GitHub release.
3. **From now on**, every nightly's notes come from its `Release-note:` trailers (entry 132). When you publish the site, first bring `docs/RELEASE-NOTES.md` up to date with every nightly published since the last entry, from those trailers, so the page is complete. The nightly workflow must not commit to the repository or publish the site; this stays a step you take when you publish (add this to the publishing rule in `CLAUDE.md`).
4. A test fails if a published nightly newer than the file's newest entry exists when the site is being published (check by the tags in the repository, not the network), so the page cannot quietly fall behind.

## 3. Publish

Publish the site with the new page when this is done, following the rule in `CLAUDE.md`, and confirm `https://grouplab.org/releases/` is live with its build stamp.
