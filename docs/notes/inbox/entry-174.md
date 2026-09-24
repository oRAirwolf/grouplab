# 2026-09-24, entry 174: the upload page rejects every photograph, because the file field has no brackets

Alan tried the end to end test of entry 173 on the live page. He chose the test image, passed Turnstile,
pressed Send, and the page answered: **"No photos were attached to that submission."** Nothing reached
the server; `Get-TargetSubmissions.ps1` found nothing.

**Do this immediately**, ahead of whatever entry is in progress: finish the current step, commit it, fix
this, and then carry on. The page is public and every person who tries it right now is refused.

## 1. The cause, found in the code

`website/build.py` line 675 onward builds the form, and the file input is:

    <input type="file" id="photos" name="photos" multiple ...>

`website/api/upload.php` line 514 reads it:

    $incoming = $_FILES['photos'] ?? null;
    if (!is_array($incoming) || !isset($incoming['name']) || !is_array($incoming['name'])) {
        fail(400, 'No photos were attached to that submission.', 'no_files');

**PHP only builds the per file arrays when the field name ends in `[]`.** With `name="photos"`, even with
`multiple`, PHP keeps one file and sets `$_FILES['photos']['name']` to a string, so `is_array` fails and
every submission is refused as having no photos. The JavaScript `FormData(form)` path sends the field
under the same name, so it fails the same way.

## 2. The fix

1. The input becomes `name="photos[]"`.
2. The receiver also accepts the single file shape, by normalizing a string `name` into a one element
   array before the checks. That makes the receiver robust to a form that sends one file without
   brackets, rather than depending on one character in another file.
3. Check the crash receiver, `crash-report.php`, for the same pattern.

## 3. Why 31 receiver tests did not catch it, and the test that would have

The receiver tests construct `$_FILES` themselves, already in the array shape, so they test the receiver
against the input it hoped for rather than the input PHP actually produces from this form. Add a test
that closes that gap for good:

1. Build the site, read the send page's actual file input name out of the built HTML, and assert it ends
   in `[]`.
2. **Run the real receiver under PHP's built in server** (`php -S`, on the Linux runner where PHP already
   runs for the syntax check) and send it a real multipart POST built from the page's own field names,
   with one file and with two files, and with Turnstile verification stubbed the way the existing
   receiver tests stub it. Assert both are accepted into quarantine. That exercises PHP's own
   `$_FILES` parsing, which is exactly the part that failed.

## 4. Then the live test again

When the fix is published, say so in the panel with one line, and the planning session will ask Alan to
repeat the end to end test of entry 173 section 1.3. Record in the report that the page refused every
submission from the time entry 173 opened it until this fix, and that no submission was lost, because
none was accepted.
