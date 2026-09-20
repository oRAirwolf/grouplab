# GroupLab's storage schema

**Schema version 2.** `DESIGN.md` section 15: "Storage is SQLite with a documented schema and full JSON export." This is the document; `src/GroupLab.Core/Records/SessionStore.cs` creates the database, and `SessionSchemaTests` fails if the statements below differ from what it creates.

## Where it lives

One SQLite file, `grouplab.db`, in the application's data folder beside `settings.json`.

On the first open, a record book from before the database, `records.json`, is copied into it and renamed `records.pre-database.json`. It is kept as a backup and never deleted. The `meta` table records that the move was made, so it is made once.

## The statements

```sql
CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE rifles (name TEXT PRIMARY KEY COLLATE NOCASE, click_value REAL NOT NULL, click_unit TEXT NOT NULL, sight_height_in REAL, zero_distance_yd REAL, twist_in REAL, twist_direction INTEGER);
CREATE TABLE barrels (name TEXT PRIMARY KEY COLLATE NOCASE, rifle TEXT, rounds INTEGER NOT NULL);
CREATE TABLE loads (name TEXT PRIMARY KEY COLLATE NOCASE, components TEXT, muzzle_velocity_fps REAL, muzzle_velocity_sd_fps REAL, ballistic_coefficient REAL, drag_model TEXT, bc_reference TEXT, bullet_weight_gr REAL, bullet_length_in REAL, bullet_diameter_in REAL, muzzle_velocity_sd_from TEXT);
CREATE TABLE sessions (id INTEGER PRIMARY KEY, created_utc TEXT NOT NULL, shot_date TEXT, sheet_name TEXT NOT NULL, definition_id TEXT, definition_json TEXT, distance_in REAL, rifle TEXT, barrel TEXT, load TEXT, calibre_in REAL, marking_json TEXT NOT NULL, shot_count INTEGER NOT NULL, mean_radius_in REAL, mean_radius_lower_in REAL, mean_radius_upper_in REAL, image_path TEXT, image_sha256 TEXT, proof_image BLOB, proof_image_type TEXT);
CREATE TABLE chronograph_strings (id INTEGER PRIMARY KEY, session_id INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE, source TEXT NOT NULL, recorded_utc TEXT);
CREATE TABLE chronograph_shots (string_id INTEGER NOT NULL REFERENCES chronograph_strings(id) ON DELETE CASCADE, ordinal INTEGER NOT NULL, velocity_fps REAL NOT NULL, PRIMARY KEY (string_id, ordinal));
CREATE TABLE shot_velocities (session_id INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE, shot_id INTEGER NOT NULL, string_id INTEGER NOT NULL, ordinal INTEGER NOT NULL, PRIMARY KEY (session_id, shot_id), FOREIGN KEY (string_id, ordinal) REFERENCES chronograph_shots(string_id, ordinal) ON DELETE CASCADE);
CREATE INDEX sessions_rifle ON sessions(rifle);
CREATE INDEX sessions_load ON sessions(load);
```

## What each table holds

- **`meta`:** `schema_version`, and `records_migrated` once the old record file has been read in.
- **`rifles`, `barrels`, `loads`:** the person's records, keyed by name without regard to case, as a person refers to them. `muzzle_velocity_sd_from` is in words, and is set only where GroupLab worked the SD out from a chronograph string. The solver's fields are all optional (`NOTES-FROM-PLANNING.md` entry 112 section 4). A record without them simply cannot use the solver.
- **`sessions`:** one analysed sheet each (entry 112 section 1), in `DESIGN.md` section 18's three tiers.
  - **Geometry, always:** `marking_json`, the marking with every edit and exclusion, in the marking file's own format.
  - **The figures as computed:** in the marking, with the list's columns `shot_count` and `mean_radius_*` beside it.
  - **The definition:** `definition_json` keeps the sheet the session was analysed against, so a person's sheet deleted later leaves the session readable.
  - **A proof image of about 150 dpi,** by default: `proof_image` and its type.
  - **The full-resolution original only by path and SHA-256:** `image_path` and `image_sha256`, never a copy.
- **`chronograph_strings` and `chronograph_shots`:** a chronograph's readings as their own ordered list, per session.
- **`shot_velocities`:** the explicit mapping from a session's shot to one reading of one string.

**Section 15's rule is why these last three exist before Xero import does:** "the shot sequence and the chronograph sequence are separate ordered lists that get reconciled, never assumed to align." Adding import needs no migration of any session.

## The JSON export

`SessionStore.Export` writes everything as one document:
- `format` and `schemaVersion`;
- the rifles, barrels and loads;
- every session, with its proof image in base64;
- the chronograph strings and the mapping.

`SessionStore.Import` reads it back into an empty database, keeping every id. `SessionStoreTests` holds the round trip exact, byte for byte, and an export of another schema version is refused with the reason.

## Changing the schema

A change raises the version and adds a migration from the version before. It changes this document in the same commit, and the test holds the two together.

**A database is brought up to this version when it is opened**, by `SessionStore.Upgrades`, oldest step first, in one transaction with the version itself. A database of a newer version is refused with its number, because this GroupLab cannot know what it holds.

| From | To | What it does | Why |
|---|---|---|---|
| 1 | 2 | `ALTER TABLE loads ADD COLUMN muzzle_velocity_sd_from TEXT` | A velocity SD worked out from a chronograph string says where it came from, rather than looking like a figure somebody typed (`NOTES-FROM-PLANNING.md` entry 115 section 3). |
