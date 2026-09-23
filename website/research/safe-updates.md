---
title: How GroupLab updates itself safely
description: An updater that can be tricked into installing someone else's file is worse than no updater. Here is what GroupLab checks, what it cannot promise, and the bug that stopped every installed build from updating at all.
group: How GroupLab is built
number: 10
written: 2026-09-22
data_date: 2026-09-22
samples: not a measurement: an account of the code, the failure it shipped with, and the tests added afterwards
state: published
found: A signature that covers a rebuilt copy of a file rather than the file itself breaks the day you add a field. That shipped, and every installed build refused every update until it was reverted.
sure: This describes the code and a fault that really happened, with the commit that reverted it. It is not a security audit.
sources:
  - "What is signed, what is checked, and what cannot be promised: `docs/UPDATES.md`."
  - "The failure and its fix: `docs/NOTES-FROM-PLANNING.md`, entries 138 and 139."
---

## What an updater has to get right

A program that downloads and runs code on your machine is the most dangerous thing in it. GroupLab checks three things before it installs anything.

**The manifest is signed.** Each build publishes a small file saying what the newest version is, and for every file its name, size and SHA-256. That file carries a signature, and GroupLab refuses a manifest whose signature does not verify.

**Every download is hashed.** A file whose SHA-256 does not match the manifest is refused, whatever the manifest said.

**Both refusals are loud.** They say what was wrong in plain words rather than failing quietly, because an updater that silently stops updating is an updater you find out about in six months.

## The bug worth writing about

The first version of the signature covered the manifest's **meaning** rather than its bytes. GroupLab read the file into a record and wrote that record back out in a canonical form, then checked those bytes against the signature.

That is a tempting design. It survives reformatting, different line endings, and a server that rewrites whitespace.

It has one fatal property: **a build that does not know about a field drops it on the way back out.** The bytes it checks are then not the bytes that were signed.

So when one field was added to the manifest, and a nightly published with it, every build already on somebody's machine read the new manifest, serialised it back without the field it had never heard of, compared the result against the signature, and refused:

```
update.check result=Refused refusal=BadSignature
```

**Every installed build lost the ability to update itself**, silently, in a way that looked exactly like "no update available" if you were not reading the log. The change was reverted the same night.

## What replaced it

The signature now covers the **exact bytes that were published**. The manifest carries its payload as an opaque blob; GroupLab verifies the signature over those bytes as received, and only then parses them. Fields it does not recognise are ignored rather than dropped and re-serialised.

That is the whole lesson, and it is not specific to this program: **sign what you send, not what you understood.** A verification step that involves rebuilding the thing you are verifying is not a verification step.

The old format is still published alongside the new one, for as long as any build that reads only the old one might still be installed, with the date it can be retired written down.

## What GroupLab cannot promise

If an update installs and then will not start, **there is no automatic roll back**.

The installer replaces the program folder in place. The previous build's files are gone once it has run. There is no second copy kept aside.

This is written down plainly rather than glossed, because the alternative, a second full copy of the program on disk, was considered and costs more than the risk is worth at present. The trigger for revisiting it is written down too: the first of a public beta opening, or a second person testing. If either happens, it gets built.

What you get instead is the previous version's installer, still downloadable, and instructions for using it.

## No secret in the client

There is no authentication on any of this, and no API key.

Any secret compiled into an open source program is public the moment it ships. A key in the installer would provide the feeling of security without the substance, and this project would rather say plainly that the protection is the signature and the hash, both of which work fine in the open.

## What the tests hold

- A manifest with **extra unknown fields** still verifies and parses on a build that has never heard of them. That is the exact case that broke before.
- A **changed byte anywhere** in the payload is refused.
- The old manifest's signed bytes are held against a recorded string, so they cannot drift by accident.
- The updater of an **older published build**, pinned at its real code, is run against freshly generated manifests and must still accept an update.

That last one exists because the fault was not in the new code. It was in the old code's ability to read what the new code produced, and no test that only runs the current version can see it.
