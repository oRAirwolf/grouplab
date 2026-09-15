# Crash reporting

What GroupLab records when something goes wrong, what a report contains, and exactly what it sends. The client is GPL and anybody can read what it sends anyway. This document is the difference between a project that can be trusted on this point and one that merely asks to be (NOTES-FROM-PLANNING.md entries 41 and 45).

## Privacy, in plain words

- **A report never contains a photograph, a location, or a path.**
- **What a report does contain:** error details, GroupLab's own log files, and information about the computer: the version of GroupLab, the operating system, the .NET runtime and the display scale.
- **What else a report never contains:**
  - no image of any kind, and no photograph's metadata or GPS coordinates;
  - no marking file, no settings file, and nothing from anybody's submissions;
  - no file path, because a path begins with the user's own name.
- **How a file appears in a log:** by its name and a salted hash of its path, so the log can say the same file was opened twice without saying where it lives.
- **If an image is ever wanted with a report,** that is a separate, explicit, per-file consent, and not a checkbox in a crash dialog.
- **Nothing is sent without a click.** The report dialog shows what is in the package before it goes anywhere. Saving the file is the first choice, and sending is a separate button that is never the default. Nothing is ever sent silently, on a timer or at startup.
- **The send address is empty by default,** and the Send button does not exist while it is empty. A copy of GroupLab built by anybody else never posts to anybody's server unless it is configured to.
- **The server** accepts only the file names below. It refuses a package carrying anything else, including a photograph, even if a future client puts one in by mistake.

## Where the log is

| Build | Directory |
|---|---|
| Debug, run from the repository | `<repository>/out/logs`, ignored by git |
| Windows | `%LOCALAPPDATA%\GroupLab\logs` |
| macOS | `~/Library/Logs/GroupLab` |
| Linux | `$XDG_STATE_HOME/grouplab/logs`, falling back to `~/.local/state/grouplab/logs` |

**`GROUPLAB_LOG_DIR` overrides all of them.**
- **Rotation:** one file per run, keeping the newest twenty files or 20 MB in total.
- **Crash records:** a crash also writes a `crash-*.json` beside the logs.
- **The next launch:** GroupLab offers any crash not yet dealt with, because a crashing application often cannot draw a dialog.

## What the client does

- **The package:** it builds exactly the entries in section 1 below. Over 2 MB, it leaves out the previous run's log and tries again. A package still over 2 MB is saved and not sent, and the dialog says why.
- **One attempt, with a 30 second timeout, and no background retry queue.** If sending fails, the zip is kept, the dialog says where it is, and it stops.
- **On success:** it shows the reference the server returns, so the user can quote it.
- **On failure:** it shows the server's `error` message verbatim.

---

The four sections below are NOTES-FROM-PLANNING.md entry 45 sections 1 to 4, verbatim. They are the wire contract the receiver was written and tested against.

### 1. The package, which is now a checked contract rather than a description

The receiver refuses any zip containing an entry that is not on this list. The names are matched anchored and case sensitively, and any entry containing a slash, a backslash or `..` is refused outright, so **the package is flat: no directories inside it.**

| Entry | Required | Notes |
|---|---|---|
| `crash-YYYYMMDD-HHmmss-<pid>.json` | when there was a crash | section 2 |
| `grouplab-YYYYMMDD-HHmmss-<pid>.log` | at least one | this run, and the previous run |
| `environment.txt` | yes | the expanded `app.start` block |
| `description.txt` | optional | what the user typed, may be empty |
| `contact.txt` | optional | may be empty |

At most 24 entries, at most 40 MB unpacked, and at most a 100 to 1 compression ratio. The client's own cap is 2 MB for the zip; the server's wall is 5 MB.

**Nothing else is accepted, and that is the privacy guarantee made mechanical.** There is no image type on that list, so a package carrying a photograph is refused by the server even if a future version of the client puts one in by mistake. I tested that case specifically rather than assuming it. If you ever find yourself wanting to add a file to the package, the list in the PHP has to change at the same time, and that friction is the point.

### 2. `crash-*.json`, exact shape

The pull script reads this to print one line per report saying what crashed, so the field names matter. `exceptions` is ordered outermost first.

```json
{
  "schema": 1,
  "created_utc": "2026-09-15T06:42:12.104Z",
  "app":  { "version": "0.1.0+3f9c2a1", "commit": "3f9c2a1", "channel": "debug" },
  "environment": {
    "os": "Windows 10.0.26100", "framework": "net10.0", "renderer": "Direct2D1",
    "culture": "en-US", "display_scale": 1.5
  },
  "last_action": "print.select-target",
  "exceptions": [
    { "type": "System.InvalidOperationException",
      "message": "The control TextBox already has a visual parent.",
      "stack": "   at GroupLab.App.PrintWindow.ShowFields()..." }
  ],
  "stages": []
}
```

`stages` holds the `StageRecord` set when an analysis was in flight and an empty array otherwise, per entry 41 section 5. `last_action` is a short stable identifier rather than prose, so that repeated reports group. **`schema` is 1 and it increments if any of this changes**, because a receiver reading a future format should be able to say so rather than guess.

The example above is not invented. It is the crash you fixed this evening, written in this format, and I used it as the test fixture for the pull script.

### 3. The request

- `POST` to the configured URL, `multipart/form-data`.
- File field name: **`report`**, the zip.
- Text field: **`version`**, the application version string, optional but send it.
- Nothing else is read. No headers are required. No authentication.

### 4. The response

Always JSON, always with an `ok` boolean.

```json
{ "ok": true, "reference": "2026-09-15_1a2b3c4d", "sha256": "6b80184e..." }
```

**Show the reference to the user** so they can quote it. On failure:

```json
{ "ok": false, "error": "That report package contains a file this server does not accept: IMG_1580.jpg" }
```

The `error` string is written to be shown to a person whose application has just crashed, so **display it verbatim rather than mapping status codes to your own wording.** The codes you will see are 400, 405, 413, 415, 422, 429, 500, 503 and 507, and every one of them means keep the file and do not retry automatically. Entry 41 section 7 already says one attempt and no background retry queue; this is the reason it says that.

---

**How this client departs from the example in section 2,** so a reader of a real record is not surprised:
- **`last_action`** is the name of the last event the log recorded, such as `print.select`, rather than a separately chosen name.
- **`renderer`** is `Skia`, which is what Avalonia 12 draws through on every desktop platform.
- **`framework`** is the runtime's own description, such as `.NET 10.0.5`.

All three are strings, and the receiver does not read them.
