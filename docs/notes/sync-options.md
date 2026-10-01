# Synchronization: the options (Phase 7)

NOTES-FROM-PLANNING.md entry 324 section 2. An options paper only: no code and no new dependency. Planning reads it and decides with Alan.
Written 2026-10-01.

## Where it starts from

DESIGN.md section 18 sets the shape: three tiers (geometry, always synchronised, about 5 KB a target; a proof image of about 400 KB,
synchronised by default; the full original, local by default), the person's own Google Drive, OneDrive or iCloud rather than storage the
project runs, and **GroupLab fully working with no account, forever**. What already exists and every option below reuses:

- **One file any GroupLab reads** (entry 307): Export all my data writes a `.grouplab` file with every session (its marks, its proof
  picture), rifles, barrels and loads, designed sheets, printers and units; Import data merges it without overwriting, skips what is
  already there, and lists what differs before anything changes, keeping the local copy. On the desktop, Android and iOS.
- **An identity across devices**: the import already tells "the same session" from "a different one" by when it was made, its sheet
  and its picture's hash (`DataExport.Identity`). Records carry no revision number yet; every option below adds one, so a change can be
  told from the version it replaced.

So synchronization is mostly a question of **how the changes travel** and **when the merge runs**, not of a new data model.

## Option A: a folder the person's own cloud client already syncs

GroupLab writes small change files into a folder the person chooses once (a folder inside OneDrive, Google Drive for desktop, Dropbox or
iCloud Drive), and reads the other devices' change files from it when it starts and every few minutes while open. The cloud company's
own app moves the files; GroupLab never talks to the cloud.

- **What it costs Alan:** nothing. No developer accounts, no API keys, no consent-screen reviews, no secrets in any build.
- **What it costs the person:** choosing a folder once on each device. On the desktop that is any folder. On Android, Android's own
  folder picker reaches OneDrive, Dropbox and Google Drive through their apps (Google Drive's is slow and sometimes read-only, which is
  the weakest point). On iPhone and iPad, an iCloud Drive or OneDrive folder in Files, kept by a bookmark iOS allows.
- **What it backs up:** geometry and proof pictures always (a year of 200 targets is about 80 MB with the pictures); originals only if
  the person ticks "also copy the original photographs", since one 600 dpi scan is about 25 MB.
- **Conflicts:** each device writes only its own change files (named by device and time), so two devices never write the same file and
  the cloud clients never make "conflicted copy" files. A change file holds whole records with their identity and the new revision number. Merging is
  the import's merge: a record changed on one device wins; the same record changed on two devices since they last met is kept both
  ways and listed for the person to choose, never decided silently. Deletions travel as markers, kept 90 days.
- **The stores' rules:** nothing special. Reading and writing a folder the person picked is ordinary on all three; the Play data safety
  form and Apple's privacy label say nothing leaves the device by GroupLab's hand.
- **Limits:** it syncs only when GroupLab is open (Android and iOS stop background work), and it depends on the cloud client being
  installed and signed in. Size of the work: medium; most of it is the change-file format and a merge already written.

## Option B: the cloud providers' own APIs, signed in from GroupLab

GroupLab signs in to the person's Google, Microsoft or Apple account (OIDC with PKCE and a loopback or app redirect, so no secret ships in
a build, as DESIGN.md section 18 says) and keeps its files in the provider's hidden application folder: Google Drive's app data folder,
OneDrive's app folder, and on Apple devices CloudKit's private database.

- **What it costs Alan:** a Google Cloud project with an OAuth consent screen (free, but Google reviews any app asking for Drive access;
  the narrowest scopes, the app's own folder or files it created, avoid the long "restricted scope" security assessment), a privacy
  policy page on grouplab.org, and the domain verified with Google; a Microsoft Entra app registration (free; Alan already has Entra for
  the Store); CloudKit comes with the Apple Developer Program he has. No money, but two or three review steps of his.
- **What it backs up:** the same tiers as A, without needing a cloud client on the device, and in the background where the system
  allows it.
- **Conflicts:** the same record-level merge as A; the providers' version tags let a device notice that a file changed under it before
  it writes, so a lost write cannot happen.
- **The stores' rules:** Apple asks that an app offering sign-in with a third-party account also offer Sign in with Apple (guideline
  4.8); signing in only to reach the person's own storage at that company is usually accepted as the exception, but it is a review risk
  to plan for. Google Play's data safety form declares the account and the files.
- **Limits:** iCloud has no supported API on Windows or Android, so an Apple-only person syncs only between Apple devices (or uses A).
  Size of the work: large; three providers, three sign-ins, each with its own failure cases and token refresh.

## Option C: a GroupLab sync service on grouplab.org

GroupLab's own server keeps the geometry tier (kilobytes a target) for anyone who makes a GroupLab account; pictures stay with the person.

- **What it costs Alan:** custody of other people's data, which DESIGN.md section 18 chose to avoid: accounts and password resets, a
  privacy policy that covers stored data, backups (the Oracle backups of request 46 would have to include it), and the server's upkeep
  for as long as anyone uses it. Little money at today's scale.
- **Conflicts:** the server orders the changes, so merging is simplest of the three.
- **The stores' rules:** Apple's account-deletion rule (an app with accounts must let a person delete theirs from inside it) and the Play
  equivalent.
- Listed so the comparison is whole; it goes against the project's chosen shape and is not recommended.

## What I would choose

**A first, B later where A is weak.** A gives every device sync and a backup in the person's own cloud with nothing for Alan to set up and
nothing for any store to review, and it reuses the merge that already ships. B is worth it afterwards for people without a cloud client on
the phone, or who want sync while GroupLab is closed, and it can be added provider by provider without changing A's change files.

## What Alan would judge by looking

The Settings screen for choosing the folder, what a person sees when two devices changed the same session, and the "also copy the
original photographs" choice with its size. Those come back as concepts before anything is built.
