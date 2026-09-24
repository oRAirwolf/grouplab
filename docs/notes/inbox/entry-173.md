# 2026-09-24, entry 173: open the target upload page, and put it in the top bar

Alan: "when will the target page be ready to use? Can you put a link to it at the top bar on the
website?"

The server side of entry 129 is finished (entry 171 section 6 records the check). Nothing but this
repository stands between the page and a real person using it. **Do this entry immediately after entry
171, before entry 164.**

## 1. Open it

1. Before setting `open` to true, check the receiver can actually verify Turnstile: the secret file is
   present on the server. The planning session asked Alan to confirm it with a command that prints
   only "present" or "missing". **Alan ran it on 2026-09-24: present.** So this check is done; go
   straight to item 2.
2. Set `"open": true` in `website/api/limits.json`. The site build then includes the send page and the
   receivers, and the site path publishes them within minutes.
3. **End to end test on the live site**: one real submission from a browser, through Turnstile, into
   quarantine, through the worker into ready, pulled by `scripts/Get-TargetSubmissions.ps1` and removed by
   `Remove-ReadSubmissions.ps1`. Use a generated test image, never a real target, and say in its consent
   record that it is a test. Report each hop.
4. Then the planning session will ask Alan to send one real photograph from his phone, because a test
   from this machine does not prove the path works for a person on a phone with a phone's photo formats.

## 2. One address, and it goes in the top bar

1. **The canonical address is `grouplab.org/targets/`.** It is short, it is what the first outside user
   asked for, it is what entry 165 already names for the application, and it matches the old
   `pissinhot.com/targets` so that redirect can keep its path. Move the send page there.
2. `/shoot-a-target/send/` must keep working for anyone holding the old link, **without a meta refresh**,
   which entry 151's test bans. A plain page with one clear link to `/targets/` is acceptable. If you
   prefer a real 301, that is an nginx change, which means the include changes and Alan reloads nginx; say
   which you chose.
3. **The top bar.** The navigation already has "Shoot a target", which goes to the donor pack page. While
   `open` is true, that item becomes **"Send a target"** and goes to `/targets/`. The donor pack page keeps
   its place in the footer and gains a prominent "Send your target" button. While `open` is false, the
   navigation stays as it is today, so the link can never point at a page that does not answer.
4. `pissinhot.com/targets` redirects to `https://grouplab.org/targets/`, per Alan's entry 129 decision, and
   stops accepting uploads once everything waiting there has been pulled. That is the one change to
   pissinhot.com Alan approved. Give him the exact steps in the panel, since it is a server change.
5. The printed donor pack PDFs name no address, per entry 129 section 6.3. Add `grouplab.org/targets` to
   them when they are next regenerated, and not before.

## 3. Tests

1. The navigation shows "Send a target" only when `open` is true, and the page it points to is in the
   build whenever the link is.
2. The old path answers with a page that links to `/targets/`, and no page anywhere carries a meta refresh.
3. The consent text on the page is the one in `limits.json`, and the page says in plain words what
   happens to a photograph: rebuilt from its pixels, location data removed, and kept until the developer
   has read it, then deleted from the server.
