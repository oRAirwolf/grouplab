## 2026-09-25, entry 225: request 35 step 3 done: the server's boot volume has a daily backup policy

Alan set it up in the Oracle Cloud console on 2026-09-25, with the planning session. What his screenshot shows:

- The instance's only volume is its 47 GB boot volume (Ubuntu 24.04 Minimal, aarch64, compartment "spetsnaz (root)", region US West,
  San Jose). No block volumes are attached. There were no earlier volume backups, so the Always Free allowance of five is untouched.
- A custom backup policy `grouplab-daily`: incremental daily kept 2 days, full weekly on Sunday kept 2 weeks, both at 09:00 UTC. At most
  four backups exist at a time.
- The boot volume's **Backup policy** reads `grouplab-daily`, and **Upcoming scheduled backups** lists 2026-09-26 09:00 UTC and
  2026-09-27 09:00 UTC (twice, the daily and the weekly full).

So request 35 is complete except for proof. The first backup should exist after 2026-09-26 09:00 UTC. Alan will confirm it from the
console; until he does, keep sudo to GroupLab's own files. After he confirms, widen it as entry 222 section 6.2 allows, and:

1. Close request 35.
2. In `docs/RESTORE.md`, the whole-server restore: create a boot volume from a boot volume backup in the console, then **Replace boot
   volume** on the instance (the console's own button, which Alan's screenshot shows on the instance's Storage tab), with what is lost
   (anything written since the backup) and what to check after (both sites, the workers, the timers).
3. Record in `docs/RESTORE.md` that these backups are crash consistent, like pulling the power: fine for the web sites and HestiaCP, and
   MySQL recovers on start as it would after a power cut.
4. The weekly check in the automation report cannot see the Oracle console. Say so in `docs/RESTORE.md`, and add a line to the weekly
   report reminding Alan that the Oracle backups are checked by him in the console if he ever wants to, not by the report.
