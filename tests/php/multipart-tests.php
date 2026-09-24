<?php
/**
 * The receiver under PHP's own web server, with a real multipart POST. NOTES-FROM-PLANNING.md entry 174.
 *
 * receiver-tests.php builds $_FILES itself, already in the shape the receiver hoped for, so it could not see that the page's file
 * input was named "photos" where PHP needs "photos[]" to build the per-file arrays: every real submission was refused as having no
 * photos while all 31 of those checks passed. This one lets PHP do the parsing. It serves the receiver with `php -S`, sends it the
 * body a browser would send, built from the file input's name as the page source writes it, and checks one file and two files both
 * reach quarantine. Turnstile is faked with a file:// answer exactly as receiver-tests.php fakes it, so nothing leaves the machine.
 *
 * Run: php tests/php/multipart-tests.php
 */

declare(strict_types=1);

$failed = 0;
function check(string $what, bool $ok, string $detail = ''): void
{
    global $failed;
    echo ($ok ? 'ok      ' : 'FAILED  ') . $what . ($ok || $detail === '' ? '' : "\n        " . $detail) . "\n";
    if (!$ok) {
        $failed++;
    }
}

function rmtree(string $path): void
{
    if (!file_exists($path)) {
        return;
    }
    if (is_file($path) || is_link($path)) {
        unlink($path);
        return;
    }
    foreach (scandir($path) ?: [] as $entry) {
        if ($entry !== '.' && $entry !== '..') {
            rmtree($path . '/' . $entry);
        }
    }
    rmdir($path);
}

foreach (['pdo_sqlite', 'mbstring'] as $needed) {
    if (!extension_loaded($needed)) {
        fwrite(STDERR, "this needs PHP's $needed extension, which is not loaded. Nothing was tested.\n");
        exit(2);
    }
}

$repo = dirname(__DIR__, 2);
$root = sys_get_temp_dir() . '/grouplab-multipart-' . bin2hex(random_bytes(4));
mkdir($root . '/private', 0777, true);
mkdir($root . '/public', 0777, true);
register_shutdown_function(static function () use ($root) { rmtree($root); });

// The field name, as the page source writes it. The site build checks the built page says the same.
$build = (string) file_get_contents($repo . '/website/build.py');
if (!preg_match('/<input type="file" id="photos" name="([^"]+)"/', $build, $m)) {
    fwrite(STDERR, "could not find the send page's file input in website/build.py\n");
    exit(2);
}
$field = $m[1];
check('the page names its file input with []', str_ends_with($field, '[]'), 'it is named ' . $field);

// The receiver, pointed at the temporary tree, with Turnstile answered by a file. Nothing else is changed: is_uploaded_file and
// move_uploaded_file stay as they are, because under a real server the uploads really are uploads.
$verify = $root . '/siteverify-ok.json';
file_put_contents($verify, json_encode(['success' => true, 'error-codes' => []]));
file_put_contents($root . '/private/turnstile-secret.txt', 'test-secret');
$source = (string) file_get_contents($repo . '/website/api/upload.php');
$patched = strtr($source, [
    "const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';" => "const SITE_PRIVATE = " . var_export($root . '/private', true) . ";",
    "const TURNSTILE_VERIFY_URL = 'https://challenges.cloudflare.com/turnstile/v0/siteverify';" => "const TURNSTILE_VERIFY_URL = " . var_export('file://' . str_replace('\\', '/', $verify), true) . ";",
]);
check('the receiver could be pointed at the test tree', $patched !== $source);
file_put_contents($root . '/public/upload.php', $patched);

// PHP's own server, on a free port.
$probe = stream_socket_server('tcp://127.0.0.1:0');
$port = (int) substr((string) stream_socket_get_name($probe, false), strrpos((string) stream_socket_get_name($probe, false), ':') + 1);
fclose($probe);
$flags = array_values(array_filter(explode(' ', (string) getenv('GROUPLAB_PHP_FLAGS'))));
$server = proc_open(array_merge([PHP_BINARY], $flags, ['-S', "127.0.0.1:$port", '-t', $root . '/public']), [1 => ['file', $root . '/server-out.txt', 'w'], 2 => ['file', $root . '/server-err.txt', 'w']], $pipes);
register_shutdown_function(static function () use ($server) {
    proc_terminate($server);
});
$up = false;
for ($i = 0; $i < 50 && !$up; $i++) {
    $socket = @fsockopen('127.0.0.1', $port, $errno, $errstr, 0.2);
    if ($socket !== false) {
        fclose($socket);
        $up = true;
    } else {
        usleep(100000);
    }
}
check('PHP\'s built in server started', $up, (string) @file_get_contents($root . '/server-err.txt'));
if (!$up) {
    exit(1);
}

/** A multipart POST as a browser sends it: the form's fields, then each file under the file input's name. */
function post(int $port, string $field, array $files, string $remote): array
{
    $boundary = '----grouplab' . bin2hex(random_bytes(8));
    $body = '';
    foreach (['consent' => '1', 'cf-turnstile-response' => 'a-token', 'backing' => 'Cardboard', 'notes' => 'TEST, entry 174'] as $name => $value) {
        $body .= "--$boundary\r\nContent-Disposition: form-data; name=\"$name\"\r\n\r\n$value\r\n";
    }
    foreach ($files as $name => $bytes) {
        $body .= "--$boundary\r\nContent-Disposition: form-data; name=\"$field\"; filename=\"$name\"\r\nContent-Type: image/jpeg\r\n\r\n$bytes\r\n";
    }
    $body .= "--$boundary--\r\n";
    $context = stream_context_create(['http' => [
        'method' => 'POST',
        'header' => "Content-Type: multipart/form-data; boundary=$boundary\r\nX-Forwarded-For: $remote\r\n",
        'content' => $body,
        'ignore_errors' => true,
        'timeout' => 20,
    ]]);
    $text = (string) @file_get_contents("http://127.0.0.1:$port/upload.php", false, $context);
    $json = json_decode($text, true);
    return ['json' => is_array($json) ? $json : null, 'raw' => $text];
}

$jpeg = static fn (int $bytes) => "\xFF\xD8\xFF\xE0" . str_repeat("\x00", 12) . str_repeat("\x41", $bytes);

$one = post($port, $field, ['target.jpg' => $jpeg(4096)], '203.0.113.21');
check('one photo through the real form is accepted', ($one['json']['ok'] ?? false) === true, $one['raw'] . ' | ' . @file_get_contents($root . '/server-err.txt'));

$two = post($port, $field, ['first.jpg' => $jpeg(4096), 'second.jpg' => $jpeg(5000)], '203.0.113.22');
check('two photos through the real form are accepted', ($two['json']['ok'] ?? false) === true, $two['raw']);

$dirs = glob($root . '/private/quarantine/*') ?: [];
check('both submissions are in quarantine', count($dirs) === 2, count($dirs) . ' directories');
$stored = array_sum(array_map(static fn ($d) => count(glob($d . '/*.jpg') ?: []), $dirs));
check('and they hold three photos between them', $stored === 3, $stored . ' photos');

// The receiver also takes the single file shape, a field named without brackets, rather than refusing it as no photos.
$bare = post($port, 'photos', ['plain.jpg' => $jpeg(4096)], '203.0.113.23');
check('a single photo under the plain name is accepted too', ($bare['json']['ok'] ?? false) === true, $bare['raw']);

echo $failed === 0 ? "all checks passed\n" : "$failed failed\n";
exit($failed === 0 ? 0 : 1);
