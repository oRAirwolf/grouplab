# Entry 148: the Discord server, linked from the site and GitHub

Written by the planning session at 03:40 Mountain on 2026-09-23. Do this after entry 129, alongside entry 147.

Alan has created the GroupLab Discord server and its permanent invite. Use the link in section 1 exactly as written; never invent or guess an invite.

## 1. The link

The permanent invite exists and never expires:

```
https://discord.gg/jY7MrYNN5V
```

Publish `https://grouplab.org/discord` as the canonical link everywhere (site, README, release notes), and have it redirect to the invite above. The invite itself is written down in exactly one place in the repository, so replacing it later is a single edit and no published link ever breaks.

## 2. Where it goes

The same link, from one source in the repository, in these places:

1. **The website's top navigation**, as "Community" or "Discord", so it is reachable from every page.
2. **The website footer**, beside the GitHub link.
3. **The support page**, as the first option for questions, with a sentence saying the support address remains for anything private or anything involving a photograph.
4. **The repository README**, near the top with the download and website links, as a plain line rather than a badge, unless a badge fits the README's existing style.
5. **The download page**, one line: somewhere to ask if something does not work.

Do not put it in the application itself in this entry. A link inside the software is a different decision, because it outlives the server.

## 3. How to hold it

Put the URL in the same single source that entry 147 section 3.2 uses for the platform statement, or beside it: one file, rendered into the page, the README and anywhere else. A test fails if the link appears written out in more than one place, and a test fails if any published page carries an invite that is not the one in that file.

## 4. What the site says about it

Short, and honest about what it is for:

> **Discord.** Questions, bug reports, target sheets, and what people are shooting. The project's developer reads it. For anything private, or anything with a photograph attached, the support address is better.

Do not call it "official support", do not promise a response time, and do not imply it is staffed.

## 5. Not automated yet

Release announcements into Discord are a webhook from the release workflow and are worth doing, but not in this entry. Alan is using GitHub's own webhook to begin with. Note it in `docs/WEBSITE.md` as a possible later item, with one line on how it would work: the release workflow already has the plain-words release note, so posting it is a single HTTP call to a Discord webhook URL held as a repository secret.
