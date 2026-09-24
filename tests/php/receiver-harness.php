<?php
/**
 * The receiver run as a request would run it, with no network: shared by the receiver's own tests and by
 * the consent test that follows one submission from the receiver through the real worker, NOTES-FROM-PLANNING.md
 * entry 183 section 3.4. One copy, so the two cannot drift apart the way the worker and the pull script did.
 *
 * Nothing here runs by itself. It is required by tests/php/receiver-tests.php and tests/php/receive-one.php.
 */

declare(strict_types=1);

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
