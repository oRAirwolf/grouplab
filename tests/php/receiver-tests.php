<?php
/**
 * Receiver tests with no network, NOTES-FROM-PLANNING.md entry 129 section 8.1.
 *
 * The receiver is PHP, so its tests are PHP: the C# suites cannot reach it, and a test that read the
 * source and asserted about its text would be checking that the code says what it says rather than
 * that it does what it should.
 *
 * Run it the way CI does:
 *
 *     php tests/php/receiver-tests.php
 *
 * **Nothing here touches the network.** The one outbound call the receiver makes is Cloudflare's
 * siteverify, and it is faked by pointing the receiver at a file:// URL that holds a canned answer,
 * which is what section 8.1 means by "with siteverify faked". Storage is a temporary directory that
 * goes when the run ends, so this leaves nothing behind and needs no server.
 */

declare(strict_types=1);

// ---------------------------------------------------------------------
// A tiny harness. Nothing here is worth a framework.
// ---------------------------------------------------------------------

$passed = 0;
$failed = [];

function check(string $what, bool $ok, string $detail = ''): void
{
    global $passed, $failed;
    if ($ok) {
        $passed++;
        return;
    }
    $failed[] = $what . ($detail !== '' ? ': ' . $detail : '');
}

function rmtree(string $path): void
{
    if (!is_dir($path)) {
        @unlink($path);
        return;
    }
    foreach (scandir($path) ?: [] as $entry) {
        if ($entry !== '.' && $entry !== '..') {
            rmtree($path . '/' . $entry);
        }
    }
    @rmdir($path);
}

$root = sys_get_temp_dir() . '/grouplab-receiver-' . bin2hex(random_bytes(4));
mkdir($root . '/private', 0750, true);
mkdir($root . '/uploads', 0750, true);

register_shutdown_function(static function () use ($root) { rmtree($root); });

// ---------------------------------------------------------------------
// The receiver, with its paths pointed at the temporary tree.
//
// It is loaded as source with its constants rewritten, rather than included, because a PHP
// constant cannot be overridden once defined and the receiver exits at the end of every request.
// Each case runs in its own php process, which is also how a real request runs it.
// ---------------------------------------------------------------------

$receiverSource = file_get_contents(__DIR__ . '/../../website/api/upload.php');
if ($receiverSource === false) {
    fwrite(STDERR, "could not read website/api/upload.php\n");
    exit(2);
}

// The receiver keeps its index in SQLite, so without the driver these tests would report the receiver refusing
// everything when what is really missing is the driver. Say which, and stop.
foreach (['pdo_sqlite', 'mbstring'] as $needed) {
    if (!extension_loaded($needed)) {
        fwrite(STDERR, "this needs PHP's $needed extension, which is not loaded. Nothing was tested.\n");
        exit(2);
    }
}

/** A canned siteverify answer, served from a file:// URL so no request leaves the machine. */
function fake_siteverify(string $root, bool $success, array $errors = []): string
{
    $path = $root . '/siteverify-' . ($success ? 'ok' : 'no') . '.json';
    file_put_contents($path, json_encode(['success' => $success, 'error-codes' => $errors]));
    return 'file://' . str_replace('\\', '/', $path);
}

/**
 * One request against the receiver, in its own process. Returns the decoded JSON answer and the
 * HTTP status the receiver set.
 */
function request(string $root, string $source, array $post, array $files, array $options = []): array
{
    $verify = $options['siteverify'] ?? fake_siteverify($root, true);
    // ?? treats an explicit null as absent, which is exactly the case this has to distinguish: "no secret on the
    // server". Written out so the null case really removes the file.
    $secret = array_key_exists('secret', $options) ? $options['secret'] : 'test-secret';

    $patched = strtr($source, [
        "const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';" => "const SITE_PRIVATE = " . var_export($root . '/private', true) . ";",
        "const TURNSTILE_VERIFY_URL = 'https://challenges.cloudflare.com/turnstile/v0/siteverify';" => "const TURNSTILE_VERIFY_URL = " . var_export($verify, true) . ";",
    ]);

    // The receiver's own POST handling reads $_FILES, and is_uploaded_file() is false for anything a
    // test wrote, so that one check is neutralised here and nowhere else. Everything it guards
    // against, a path outside the upload area, cannot happen when the harness makes the paths.
    $patched = str_replace('!is_uploaded_file($tmp)', '!is_file($tmp)', $patched);
    $patched = str_replace("move_uploaded_file(\$c['tmp'], \$dest)", "rename(\$c['tmp'], \$dest)", $patched);

    $script = $root . '/receiver-' . bin2hex(random_bytes(3)) . '.php';
    file_put_contents($script, $patched);

    if ($secret !== null) {
        file_put_contents($root . '/private/turnstile-secret.txt', $secret);
    } elseif (is_file($root . '/private/turnstile-secret.txt')) {
        unlink($root . '/private/turnstile-secret.txt');
    }

    $harness = $root . '/run-' . bin2hex(random_bytes(3)) . '.php';
    file_put_contents($harness, '<?php' . "\n"
        . '$_SERVER["REQUEST_METHOD"] = "POST";' . "\n"
        . '$_SERVER["REMOTE_ADDR"] = ' . var_export($options['remote'] ?? '203.0.113.7', true) . ';' . "\n"
        . '$_SERVER["CONTENT_LENGTH"] = "1000";' . "\n"
        . '$_POST = ' . var_export($post, true) . ';' . "\n"
        . '$_FILES = ' . var_export($files, true) . ';' . "\n"
        . 'require ' . var_export($script, true) . ';' . "\n");

    // The child needs the same interpreter settings as this process. On a Linux runner pdo_sqlite is built in and
    // nothing is needed; on a bare Windows build it is not, and GROUPLAB_PHP_FLAGS carries whatever this run was
    // started with. Without it the child loses the driver and every case fails as "storage is not available",
    // which says nothing at all about the receiver.
    $flags = (string) getenv('GROUPLAB_PHP_FLAGS');

    // The receiver writes to error_log as well as to stdout, and error_log goes to stderr off a web server.
    // Merging the two puts a log line in front of the JSON and nothing parses, so they are kept apart and the
    // log is kept for the failure message.
    $errors = $root . '/stderr-' . bin2hex(random_bytes(3)) . '.txt';
    $out = [];
    $code = 0;
    exec(escapeshellarg(PHP_BINARY) . ($flags !== '' ? ' ' . $flags : '') . ' ' . escapeshellarg($harness)
        . ' 2>' . escapeshellarg($errors), $out, $code);
    $text = trim(implode("\n", $out));
    $log = is_file($errors) ? trim((string) file_get_contents($errors)) : '';

    // The receiver prints the JSON and nothing else; http_response_code() has no effect off a web
    // server, so the body's own "ok" and "code" are what the cases below read.
    $json = json_decode($text, true);
    return ['json' => is_array($json) ? $json : null, 'raw' => $text . ($log !== '' ? ' | log: ' . $log : '')];
}

/** A file of the right shape for a type, written where the harness can hand it to the receiver. */
function make(string $root, string $name, string $kind, int $bytes = 4096): array
{
    $head = match ($kind) {
        'jpeg' => "\xFF\xD8\xFF\xE0" . str_repeat("\x00", 12),
        'png'  => "\x89PNG\r\n\x1A\n" . str_repeat("\x00", 8),
        'tiff' => "II\x2A\x00" . str_repeat("\x00", 12),
        'heic' => "\x00\x00\x00\x18ftypheic" . str_repeat("\x00", 8),
        'pdf'  => "%PDF-1.7\n" . str_repeat("\x00", 12),
        'exe'  => "MZ\x90\x00" . str_repeat("\x00", 12),
        default => str_repeat("\x00", 16),
    };
    $path = $root . '/uploads/' . bin2hex(random_bytes(4)) . '-' . $name;
    file_put_contents($path, $head . str_repeat("\x41", max(0, $bytes - strlen($head))));
    return ['name' => $name, 'tmp' => $path, 'size' => filesize($path)];
}

/** $_FILES as PHP builds it for a multiple file field. */
function files_array(array $made): array
{
    $out = ['name' => [], 'tmp_name' => [], 'size' => [], 'error' => []];
    foreach ($made as $m) {
        $out['name'][]     = $m['name'];
        $out['tmp_name'][] = $m['tmp'];
        $out['size'][]     = $m['size'];
        $out['error'][]    = $m['error'] ?? UPLOAD_ERR_OK;
    }
    return ['photos' => $out];
}

$good = ['consent' => '1', 'cf-turnstile-response' => 'a-token', 'backing' => 'Cardboard', 'caliber' => '0.264'];

// ---------------------------------------------------------------------
// The cases entry 129 section 8.1 lists
// ---------------------------------------------------------------------

// A good submission.
$r = request($root, $receiverSource, $good, files_array([make($root, 'target.jpg', 'jpeg')]));
check('a good submission is accepted', ($r['json']['ok'] ?? false) === true, $r['raw']);
$id = $r['json']['id'] ?? '';
$dir = glob($root . '/private/quarantine/*_' . $id)[0] ?? null;
check('it lands in quarantine and nowhere else', $dir !== null && is_dir($dir), 'no quarantine directory for ' . $id);
check('nothing is written outside quarantine', !is_dir($root . '/private/ready') && !is_dir($root . '/private/refused'));
if ($dir !== null) {
    $meta = json_decode((string) file_get_contents($dir . '/meta.json'), true);
    check('the consent record is written by the receiver', ($meta['consent']['version'] ?? '') === 'consent_v1');
    check('the stage says quarantine', ($meta['stage'] ?? '') === 'quarantine');
    check('each file carries its SHA-256', isset($meta['files'][0]['sha256']) && strlen($meta['files'][0]['sha256']) === 64);
    check('the answers come from the form', ($meta['answers']['target_backing'] ?? '') === 'Cardboard');
}

// PDF, refused by name.
$r = request($root, $receiverSource, $good, files_array([make($root, 'target.pdf', 'pdf')]));
check('a PDF is refused', ($r['json']['code'] ?? '') === 'pdf', $r['raw']);

// A renamed executable.
$r = request($root, $receiverSource, $good, files_array([make($root, 'target.jpg', 'exe')]));
check('an executable named .jpg is refused', ($r['json']['code'] ?? '') === 'bad_type', $r['raw']);

// A mismatched extension: real PNG bytes under a .jpg name is accepted and stored as .png, because
// the content decides. That is the rule, so it is checked rather than assumed.
$r = request($root, $receiverSource, $good, files_array([make($root, 'photo.jpg', 'png')]));
check('content decides the type, not the extension', ($r['json']['ok'] ?? false) === true, $r['raw']);
$id = $r['json']['id'] ?? '';
$dir = glob($root . '/private/quarantine/*_' . $id)[0] ?? null;
check('and it is stored with the sniffed extension', $dir !== null && count(glob($dir . '/*.png')) === 1);

// Too many files.
$many = [];
for ($i = 0; $i < 11; $i++) {
    $many[] = make($root, "t$i.jpg", 'jpeg', 1024);
}
$r = request($root, $receiverSource, $good, files_array($many));
check('more than ten files is refused', ($r['json']['code'] ?? '') === 'too_many', $r['raw']);

// One file over the per-file limit.
$r = request($root, $receiverSource, $good, files_array([['name' => 'big.jpg', 'tmp' => make($root, 'big.jpg', 'jpeg', 1024)['tmp'], 'size' => 0, 'error' => UPLOAD_ERR_INI_SIZE]]));
check('a file over the limit is refused', ($r['json']['code'] ?? '') === 'upload_error', $r['raw']);

// A partial upload, which sniffs as a valid JPEG and must still be refused.
$r = request($root, $receiverSource, $good, files_array([['name' => 'part.jpg', 'tmp' => make($root, 'part.jpg', 'jpeg')['tmp'], 'size' => 100, 'error' => UPLOAD_ERR_PARTIAL]]));
check('a part-way upload is refused even though it sniffs as a JPEG', ($r['json']['code'] ?? '') === 'upload_error', $r['raw']);

// The honeypot: it looks exactly like success and writes nothing.
$before = count(glob($root . '/private/quarantine/*') ?: []);
$r = request($root, $receiverSource, $good + ['contact_reason' => 'buy my thing'], files_array([make($root, 'bot.jpg', 'jpeg')]));
check('the honeypot answers as if it worked', ($r['json']['ok'] ?? false) === true, $r['raw']);
check('and writes nothing', count(glob($root . '/private/quarantine/*') ?: []) === $before);

// Consent.
$r = request($root, $receiverSource, ['cf-turnstile-response' => 'a-token'], files_array([make($root, 'x.jpg', 'jpeg')]));
check('no consent, no submission', ($r['json']['code'] ?? '') === 'consent', $r['raw']);

// Turnstile: missing, refused, and unreachable.
$r = request($root, $receiverSource, ['consent' => '1'], files_array([make($root, 'x.jpg', 'jpeg')]));
check('a missing token is refused', ($r['json']['code'] ?? '') === 'turnstile', $r['raw']);

$r = request($root, $receiverSource, $good, files_array([make($root, 'x.jpg', 'jpeg')]),
    ['siteverify' => fake_siteverify($root, false, ['timeout-or-duplicate'])]);
check('a reused or expired token is refused', ($r['json']['code'] ?? '') === 'turnstile', $r['raw']);

$r = request($root, $receiverSource, $good, files_array([make($root, 'x.jpg', 'jpeg')]),
    ['siteverify' => 'file:///nowhere/at/all.json']);
check('an unreachable siteverify refuses rather than accepts', ($r['json']['code'] ?? '') === 'turnstile', $r['raw']);

$r = request($root, $receiverSource, $good, files_array([make($root, 'x.jpg', 'jpeg')]), ['secret' => null]);
check('no secret on the server refuses rather than accepts', ($r['json']['code'] ?? '') === 'turnstile', $r['raw']);

// The rate limit, per address. Five an hour, so the sixth is refused.
$fresh = '198.51.100.4';
$last = null;
for ($i = 0; $i < 6; $i++) {
    $last = request($root, $receiverSource, $good, files_array([make($root, "r$i.jpg", 'jpeg', 1024)]), ['remote' => $fresh]);
}
check('the sixth submission from one address in an hour is refused', ($last['json']['code'] ?? '') === 'rate_limit', $last['raw']);

// ---------------------------------------------------------------------
// The crash receiver, entry 129 section 5. Same shape: its constants are pointed at the temporary
// tree and each request runs in its own process.
// ---------------------------------------------------------------------

$crashSource = file_get_contents(__DIR__ . '/../../website/api/crash-report.php');
if ($crashSource === false) {
    fwrite(STDERR, "could not read website/api/crash-report.php\n");
    exit(2);
}

/** A zip holding the named entries, each with a little content. */
function make_zip(string $root, array $names, int $bytesEach = 256): string
{
    $path = $root . '/uploads/report-' . bin2hex(random_bytes(4)) . '.zip';
    $zip = new ZipArchive();
    $zip->open($path, ZipArchive::CREATE | ZipArchive::OVERWRITE);
    foreach ($names as $name) {
        $zip->addFromString($name, str_repeat('x', $bytesEach));
    }
    $zip->close();
    return $path;
}

function crash_request(string $root, string $source, ?string $zip, array $options = []): array
{
    $patched = strtr($source, [
        "const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';" => "const SITE_PRIVATE = " . var_export($root . '/private', true) . ";",
    ]);
    $patched = str_replace('!is_uploaded_file($tmp)', '!is_file($tmp)', $patched);
    $patched = str_replace('move_uploaded_file($tmp, $dest)', 'rename($tmp, $dest)', $patched);

    $script = $root . '/crash-' . bin2hex(random_bytes(3)) . '.php';
    file_put_contents($script, $patched);

    $files = $zip === null ? [] : ['report' => ['name' => 'report.zip', 'tmp_name' => $zip, 'size' => filesize($zip), 'error' => UPLOAD_ERR_OK]];

    $harness = $root . '/runc-' . bin2hex(random_bytes(3)) . '.php';
    file_put_contents($harness, '<?php' . "\n"
        . '$_SERVER["REQUEST_METHOD"] = "POST";' . "\n"
        . '$_SERVER["REMOTE_ADDR"] = ' . var_export($options['remote'] ?? '203.0.113.9', true) . ';' . "\n"
        . '$_SERVER["CONTENT_LENGTH"] = ' . var_export((string) ($options['length'] ?? 1000), true) . ';' . "\n"
        . '$_POST = ' . var_export($options['post'] ?? [], true) . ';' . "\n"
        . '$_FILES = ' . var_export($files, true) . ';' . "\n"
        . 'require ' . var_export($script, true) . ';' . "\n");

    $flags = (string) getenv('GROUPLAB_PHP_FLAGS');
    $errors = $root . '/cerr-' . bin2hex(random_bytes(3)) . '.txt';
    $out = [];
    $code = 0;
    exec(escapeshellarg(PHP_BINARY) . ($flags !== '' ? ' ' . $flags : '') . ' ' . escapeshellarg($harness)
        . ' 2>' . escapeshellarg($errors), $out, $code);
    $text = trim(implode("\n", $out));
    $log = is_file($errors) ? trim((string) file_get_contents($errors)) : '';
    $json = json_decode($text, true);
    return ['json' => is_array($json) ? $json : null, 'raw' => $text . ($log !== '' ? ' | log: ' . $log : '')];
}

if (!extension_loaded('zip')) {
    check('the crash receiver cases need PHP zip, which is not loaded', false, 'install php-zip');
} else {
    $goodZip = make_zip($root, ['crash-20260923-081500-1234.json', 'grouplab-20260923-081500-1234.log', 'environment.txt', 'description.txt']);
    $r = crash_request($root, $crashSource, $goodZip);
    check('a good crash report is accepted', ($r['json']['ok'] ?? false) === true, $r['raw']);
    check('and is stored outside the web root', count(glob($root . '/private/crash-reports/*.zip') ?: []) === 1);

    $r = crash_request($root, $crashSource, make_zip($root, ['crash-20260923-081500-1234.json', 'IMG_1580.jpg']));
    check('a report carrying a photograph is refused', ($r['json']['code'] ?? '') === 'bad_package', $r['raw']);

    $r = crash_request($root, $crashSource, make_zip($root, ['settings.json']));
    check('a report carrying settings is refused', ($r['json']['code'] ?? '') === 'bad_package', $r['raw']);

    $r = crash_request($root, $crashSource, make_zip($root, ['logs/grouplab-20260923-081500-1234.log']));
    check('an entry with a path in its name is refused', ($r['json']['code'] ?? '') === 'bad_package', $r['raw']);

    // Nothing attached, but the form fields did arrive. That is a request with no report in it, not a request
    // that was too large, and telling somebody to shrink a file they never attached would send them away to fix
    // the wrong thing. The receiver said "too large" here until this case was written.
    $r = crash_request($root, $crashSource, null, ['post' => ['anything' => '1']]);
    check('no report attached is refused as no report, not as too large', ($r['json']['code'] ?? '') === 'no_file', $r['raw']);

    // A fresh zip: the receiver moves an accepted one out of the uploads folder, so $goodZip is gone by now.
    $r = crash_request($root, $crashSource, make_zip($root, ['environment.txt']), ['length' => 6 * 1024 * 1024]);
    check('a report over the 5 MB wall is refused before it is read', ($r['json']['code'] ?? '') === 'too_big', $r['raw']);

    // The kill switch, entry 129 section 5.3.
    file_put_contents($root . '/private/crash-reports-closed', 'off');
    $r = crash_request($root, $crashSource, make_zip($root, ['environment.txt']));
    check('the kill switch refuses everything and says nothing is wrong with the report',
        ($r['json']['code'] ?? '') === 'closed', $r['raw']);
    unlink($root . '/private/crash-reports-closed');

    $last = null;
    for ($i = 0; $i < 7; $i++) {
        $last = crash_request($root, $crashSource, make_zip($root, ['environment.txt']), ['remote' => '198.51.100.9']);
    }
    check('the seventh report from one address in an hour is refused', ($last['json']['code'] ?? '') === 'rate_limit', $last['raw']);
}

// ---------------------------------------------------------------------

echo "receiver tests: $passed passed, " . count($failed) . " failed\n";
foreach ($failed as $f) {
    echo "  FAILED  $f\n";
}
exit($failed === [] ? 0 : 1);
