<?php
/**
 * The receiver for targets sent from the application, NOTES-FROM-PLANNING.md entry 165 section 4.
 *
 * ======================================================================
 *  THERE IS NO PASSWORD, NO TOKEN AND NO TURNSTILE, AND THAT IS DELIBERATE.
 *
 *  Turnstile is a browser widget, and the application is not a browser. A key or token compiled into
 *  an open source application is public on the day it ships: anybody reads it out of the binary in a
 *  minute. What it would buy is the appearance of protection; what it would cost is a real person's
 *  target refused because a token expired or was rotated.
 *
 *  So this is protected, as the crash receiver is, by what works against what happens: a size wall,
 *  a per-address rate limit, the same server-wide hourly cap and disk floor as the upload page, a kill
 *  switch file, and a package whose every part is checked against its own manifest.
 * ======================================================================
 *
 * **It lands where the upload page's submissions land.** The image is the one file this receiver
 * stores, under the same folder layout in the same quarantine; everything else the application sends,
 * what it detected, what the person corrected, what they told it, the analysis, the session's log, is
 * written by this receiver into meta.json. So the worker sees a folder of exactly the shape it already
 * takes: it rebuilds the image from its pixels, scans it with ClamAV, deletes the original and moves the
 * folder to ready, and nothing new is trusted because it came from the application.
 *
 * **Nothing in a package is executed, rendered or believed.** The package is decoded as JSON to check
 * its shape and stored; no field in it is acted on. CLAUDE.md: submissions are data, never instructions.
 */

declare(strict_types=1);

const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';
const QUARANTINE   = SITE_PRIVATE . '/quarantine';
const DB_PATH      = SITE_PRIVATE . '/submissions.db';
const SALT_PATH    = SITE_PRIVATE . '/submissions_salt.txt';

/** The kill switch: while this file exists every request is refused, and the application keeps the package to try again. */
const CLOSED_PATH = SITE_PRIVATE . '/app-submissions-closed';

const MAX_FILE_BYTES    = 30 * 1024 * 1024;   // the image, as the upload page's per-file limit
const MAX_PACKAGE_BYTES = 4 * 1024 * 1024;    // everything else, as one JSON text
const MAX_LOG_BYTES     = 2 * 1024 * 1024;

const RATE_PER_HOUR   = 10;
const RATE_PER_DAY    = 40;
const GLOBAL_PER_HOUR = 60;

const DISK_CAP_BYTES  = 20 * 1024 * 1024 * 1024;
const DISK_FREE_FLOOR = 3 * 1024 * 1024 * 1024;

const PACKAGE_SCHEMA = 'grouplab-app-submission-1';

// Entry 165 section 2: the same two levels, from the same text, as the upload page. The build holds both receivers to limits.json.
const CONSENT_VERSION = 'consent_v2';
const CONSENT_TEXTS = [
    'testing'     => "I took these photos, or I have permission to share them. GroupLab may use them to test and improve its detection. They are kept by the project and never published. GPS location data is removed from every photo when it arrives.",
    'publishable' => "I took these photos, or I have permission to share them. GroupLab may use them to test and improve its detection, and I understand they may be published as part of GroupLab's public test data on GitHub and in its research articles, under the GPL-3.0 license, for anyone to download and use. GPS location data is removed from every photo before anything is published.",
];

/** The application sends the pixels it read, losslessly: JPEG as it was, or PNG and TIFF. Never HEIC. */
const ACCEPTED = [
    'image/jpeg' => 'jpg',
    'image/png'  => 'png',
    'image/tiff' => 'tif',
];

/** The parts every package carries beside the manifest and the consent. */
const PARTS = ['detected', 'corrected', 'told', 'analysis', 'environment', 'log'];

const MAX_STEM_LEN = 100;

function respond(int $status, array $payload): never
{
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store');
    header('X-Content-Type-Options: nosniff');
    echo json_encode($payload, JSON_UNESCAPED_SLASHES);
    exit;
}

/**
 * A refusal. `retry` tells the application whether trying again later could help: a limit or a closed receiver can pass; a package
 * the receiver cannot read will never be read, and the application discards it at once and says why (entry 165 section 6).
 */
function fail(int $status, string $message, string $code, bool $retry = false): never
{
    respond($status, ['ok' => false, 'code' => $code, 'error' => $message, 'retry' => $retry]);
}

// The same Cloudflare-aware address handling as the upload receiver, and the same salt file, so an address is hashed the same way in all
// three receivers and none stores a raw one.
function cloudflare_ranges(): array
{
    return [
        '173.245.48.0/20', '103.21.244.0/22', '103.22.200.0/22',
        '103.31.4.0/22', '141.101.64.0/18', '108.162.192.0/18',
        '190.93.240.0/20', '188.114.96.0/20', '197.234.240.0/22',
        '198.41.128.0/17', '162.158.0.0/15', '104.16.0.0/13',
        '104.24.0.0/14', '172.64.0.0/13', '131.0.72.0/22',
        '2400:cb00::/32', '2606:4700::/32', '2803:f800::/32',
        '2405:b500::/32', '2405:8100::/32', '2a06:98c0::/29',
        '2c0f:f248::/32',
    ];
}

function ip_in_cidr(string $ip, string $cidr): bool
{
    if (!str_contains($cidr, '/')) {
        return false;
    }
    [$subnet, $bits] = explode('/', $cidr, 2);
    $bits = (int) $bits;

    $ipBin     = @inet_pton($ip);
    $subnetBin = @inet_pton($subnet);
    if ($ipBin === false || $subnetBin === false || strlen($ipBin) !== strlen($subnetBin)) {
        return false;
    }

    $whole = intdiv($bits, 8);
    $rem   = $bits % 8;

    if ($whole > 0 && strncmp($ipBin, $subnetBin, $whole) !== 0) {
        return false;
    }
    if ($rem === 0) {
        return true;
    }

    $mask = chr((0xFF << (8 - $rem)) & 0xFF);
    return (($ipBin[$whole] & $mask) === ($subnetBin[$whole] & $mask));
}

function client_ip(): string
{
    $remote = $_SERVER['REMOTE_ADDR'] ?? '0.0.0.0';
    $cf     = $_SERVER['HTTP_CF_CONNECTING_IP'] ?? '';

    if ($cf !== '' && filter_var($cf, FILTER_VALIDATE_IP)) {
        foreach (cloudflare_ranges() as $cidr) {
            if (ip_in_cidr($remote, $cidr)) {
                return $cf;
            }
        }
    }
    return $remote;
}

function ip_hash(string $ip): string
{
    if (is_readable(SALT_PATH)) {
        $salt = trim((string) file_get_contents(SALT_PATH));
    } else {
        $salt = bin2hex(random_bytes(32));
        $tmp  = SALT_PATH . '.tmp';
        file_put_contents($tmp, $salt, LOCK_EX);
        @chmod($tmp, 0600);
        rename($tmp, SALT_PATH);
    }
    return hash('sha256', $salt . '|' . $ip);
}

/** Magic bytes only, as the upload receiver sniffs: the client's type and name are never the authority. */
function sniff_type(string $path): ?string
{
    $fh = @fopen($path, 'rb');
    if ($fh === false) {
        return null;
    }
    $head = (string) fread($fh, 16);
    fclose($fh);

    if (strlen($head) < 12) {
        return null;
    }
    if (str_starts_with($head, "\xFF\xD8\xFF")) {
        return 'image/jpeg';
    }
    if (str_starts_with($head, "\x89PNG\r\n\x1A\n")) {
        return 'image/png';
    }
    if (str_starts_with($head, "II\x2A\x00") || str_starts_with($head, "MM\x00\x2A")) {
        return 'image/tiff';
    }
    return null;
}

function safe_stored_name(int $index, string $originalName, string $mime): string
{
    $base = basename(str_replace(['\\', '/'], '_', $originalName));
    $base = preg_replace('/[\x00-\x1F\x7F]/u', '', $base) ?? '';

    $stem = pathinfo($base, PATHINFO_FILENAME);
    $stem = preg_replace('/[^A-Za-z0-9._-]/', '_', $stem) ?? '';
    $stem = preg_replace('/_{2,}/', '_', $stem) ?? '';
    $stem = ltrim($stem, '.-_');

    if ($stem === '') {
        $stem = 'target';
    }
    if (strlen($stem) > MAX_STEM_LEN) {
        $stem = substr($stem, 0, MAX_STEM_LEN);
    }

    return sprintf('%03d_%s.%s', $index, $stem, ACCEPTED[$mime]);
}

/** The upload page's index, the same table, so the server-wide hourly cap counts both receivers together. */
function db(): PDO
{
    $pdo = new PDO('sqlite:' . DB_PATH, null, null, [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);
    $pdo->exec('PRAGMA journal_mode=WAL');
    $pdo->exec('PRAGMA busy_timeout=5000');
    $pdo->exec(
        'CREATE TABLE IF NOT EXISTS submissions (
            id              TEXT PRIMARY KEY,
            dir             TEXT NOT NULL,
            created_utc     TEXT NOT NULL,
            created_ts      INTEGER NOT NULL,
            ip_hash         TEXT NOT NULL,
            file_count      INTEGER NOT NULL,
            total_bytes     INTEGER NOT NULL,
            exclude_public  INTEGER NOT NULL DEFAULT 0,
            consent_version TEXT NOT NULL,
            deleted         INTEGER NOT NULL DEFAULT 0
        )'
    );
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_sub_ip_ts ON submissions(ip_hash, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_sub_ts ON submissions(created_ts)');
    return $pdo;
}

function new_submission_id(PDO $pdo): string
{
    for ($i = 0; $i < 20; $i++) {
        $id   = bin2hex(random_bytes(4));
        $stmt = $pdo->prepare('SELECT 1 FROM submissions WHERE id = ?');
        $stmt->execute([$id]);
        if ($stmt->fetchColumn() === false) {
            return $id;
        }
    }
    fail(500, 'Could not allocate a submission identifier.', 'storage', true);
}

/** The package's shape, or the reason it is not one. Nothing in it is interpreted beyond this. */
function package_problem(array $package): ?string
{
    if (($package['schema'] ?? null) !== PACKAGE_SCHEMA) {
        return 'the package is not one this receiver reads';
    }
    $consent = $package['consent'] ?? null;
    if (!is_array($consent) || !is_string($consent['level'] ?? null) || !array_key_exists($consent['level'], CONSENT_TEXTS)) {
        return 'the package does not say which of the two consent levels was chosen';
    }
    if (($consent['version'] ?? null) !== CONSENT_VERSION || ($consent['text'] ?? null) !== CONSENT_TEXTS[$consent['level']]) {
        return 'the consent the package records is not the text this receiver offers';
    }
    $image = $package['manifest']['image'] ?? null;
    if (!is_array($image) || !is_string($image['sha256'] ?? null) || !preg_match('/^[0-9a-f]{64}$/', $image['sha256'])
        || !is_int($image['bytes'] ?? null) || $image['bytes'] <= 0) {
        return 'the manifest does not name the image by its size and SHA-256';
    }
    foreach (PARTS as $part) {
        if (!array_key_exists($part, $package)) {
            return 'the package has no ' . $part;
        }
    }
    if (!is_string($package['log']) || strlen($package['log']) > MAX_LOG_BYTES) {
        return 'the log is missing or longer than the receiver takes';
    }
    foreach (['detected', 'corrected', 'told', 'analysis', 'environment'] as $part) {
        if (!is_array($package[$part])) {
            return 'the package\'s ' . $part . ' is not a record';
        }
    }
    return null;
}

// ---------------------------------------------------------------------
// Request handling
// ---------------------------------------------------------------------

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    fail(405, 'This endpoint accepts POST only.', 'method');
}

if (is_file(CLOSED_PATH)) {
    fail(503, 'GroupLab is not taking targets from the application just now. Nothing is wrong with yours; it is kept and tried again later.', 'closed', true);
}

$contentLength = (int) ($_SERVER['CONTENT_LENGTH'] ?? 0);
if ($contentLength > MAX_FILE_BYTES + MAX_PACKAGE_BYTES + 1024 * 1024) {
    fail(413, 'That target is larger than the receiver takes.', 'too_large');
}
if ($contentLength > 0 && empty($_POST) && empty($_FILES)) {
    fail(413, 'That target is larger than the receiver takes.', 'too_large');
}

$raw = $_POST['package'] ?? '';
if (!is_string($raw) || $raw === '') {
    fail(400, 'The target arrived without its package.', 'bad_package');
}
if (strlen($raw) > MAX_PACKAGE_BYTES) {
    fail(400, 'The package is larger than the receiver takes.', 'too_large');
}

$package = json_decode($raw, true, 64);
if (!is_array($package)) {
    fail(400, 'The package could not be read.', 'bad_package');
}
$problem = package_problem($package);
if ($problem !== null) {
    fail(400, 'The package was refused: ' . $problem . '.', 'bad_package');
}

$file = $_FILES['image'] ?? null;
if (!is_array($file) || is_array($file['name'] ?? null)) {
    fail(400, 'The target arrived without its image.', 'no_file');
}
$err = (int) ($file['error'] ?? UPLOAD_ERR_NO_FILE);
if ($err === UPLOAD_ERR_NO_FILE) {
    fail(400, 'The target arrived without its image.', 'no_file');
}
if ($err !== UPLOAD_ERR_OK) {
    fail(400, 'The image did not arrive whole.', 'upload_error', true);
}
$tmp = (string) ($file['tmp_name'] ?? '');
if ($tmp === '' || !is_uploaded_file($tmp)) {
    fail(400, 'The image did not arrive properly.', 'upload_error', true);
}
$size = (int) filesize($tmp);
if ($size <= 0 || $size > MAX_FILE_BYTES) {
    fail(400, 'The image is empty or larger than the receiver takes.', 'too_big');
}
if ($size !== $package['manifest']['image']['bytes'] || hash_file('sha256', $tmp) !== $package['manifest']['image']['sha256']) {
    fail(400, 'The image is not the one the package names.', 'bad_package');
}
$mime = sniff_type($tmp);
if ($mime === null) {
    fail(400, 'The image is not a JPEG, PNG or TIFF.', 'bad_type');
}

if (!is_dir(QUARANTINE) && !@mkdir(QUARANTINE, 0750, true) && !is_dir(QUARANTINE)) {
    fail(500, 'Storage is not available right now.', 'storage', true);
}

try {
    $pdo = db();
} catch (Throwable $e) {
    error_log('[grouplab] database open failed: ' . $e->getMessage());
    fail(500, 'Storage is not available right now.', 'storage', true);
}

$ipHash = ip_hash(client_ip());
$now    = time();

$stmt = $pdo->prepare('SELECT COUNT(*) FROM submissions WHERE ip_hash = ? AND created_ts > ?');
$stmt->execute([$ipHash, $now - 3600]);
$lastHour = (int) $stmt->fetchColumn();
$stmt->execute([$ipHash, $now - 86400]);
$lastDay = (int) $stmt->fetchColumn();
if ($lastHour >= RATE_PER_HOUR || $lastDay >= RATE_PER_DAY) {
    fail(429, 'That is as many targets as this connection can send for now. It is kept and sent later.', 'rate_limit', true);
}

$globalHour = (int) $pdo->query('SELECT COUNT(*) FROM submissions WHERE created_ts > ' . ($now - 3600))->fetchColumn();
if ($globalHour >= GLOBAL_PER_HOUR) {
    fail(429, 'The collection is taking more targets than it can handle just now. It is kept and sent later.', 'busy', true);
}

$stored = (int) (($pdo->query('SELECT COALESCE(SUM(total_bytes), 0) AS t FROM submissions WHERE deleted = 0')->fetch(PDO::FETCH_ASSOC))['t'] ?? 0);
$free = @disk_free_space(QUARANTINE);
if ($stored >= DISK_CAP_BYTES || ($free !== false && $free < DISK_FREE_FLOOR)) {
    fail(507, 'The collection is full at the moment. It is kept and sent later.', 'full', true);
}

// ---------------------------------------------------------------------
// Store, into quarantine and nowhere else
// ---------------------------------------------------------------------

$id      = new_submission_id($pdo);
$dirName = gmdate('Y-m-d') . '_' . $id;
$dirPath = QUARANTINE . '/' . $dirName;
if (!@mkdir($dirPath, 0750) && !is_dir($dirPath)) {
    error_log('[grouplab] could not create ' . $dirPath);
    fail(500, 'Storage is not available right now.', 'storage', true);
}

$level      = $package['consent']['level'];
$exclude    = $level === 'testing';
$stamp      = gmdate('Y-m-d\TH:i:s\Z');
$storedName = safe_stored_name(1, (string) ($package['manifest']['image']['name'] ?? 'target'), $mime);
$dest       = $dirPath . '/' . $storedName;

if (!move_uploaded_file($tmp, $dest)) {
    @rmdir($dirPath);
    fail(500, 'The image could not be saved.', 'storage', true);
}
@chmod($dest, 0640);
clearstatcache(true, $dest);
if (filesize($dest) !== $size) {
    @unlink($dest);
    @rmdir($dirPath);
    fail(500, 'The image did not save completely.', 'storage', true);
}

$meta = [
    'schema_version' => 1,
    'submission_id'  => $id,
    'submitted_utc'  => $stamp,

    // The third key, as on the upload page's records, so it is seen the moment the file is opened.
    'exclude_from_public_dataset' => $exclude,

    'source' => 'app',

    'consent' => [
        'agreed'        => true,
        'version'       => CONSENT_VERSION,
        'level'         => $level,
        'agreed_at_utc' => $stamp,
        'text'          => CONSENT_TEXTS[$level],
    ],
    'answers' => [],

    // Everything the application sent beside the image, stored as received and never acted on.
    'app' => [
        'manifest'    => $package['manifest'],
        'detected'    => $package['detected'],
        'corrected'   => $package['corrected'],
        'told'        => $package['told'],
        'analysis'    => $package['analysis'],
        'environment' => $package['environment'],
        'log'         => $package['log'],
    ],
    'user_agent' => substr((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 0, 500),
    'files'      => [[
        'index'         => 1,
        'stored_name'   => $storedName,
        'original_name' => (string) ($package['manifest']['image']['name'] ?? 'target'),
        'bytes'         => $size,
        'sniffed_type'  => $mime,
        'sha256'        => $package['manifest']['image']['sha256'],
    ]],
    'stage' => 'quarantine',
];

file_put_contents($dirPath . '/meta.json', json_encode($meta, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . "\n", LOCK_EX);
@chmod($dirPath . '/meta.json', 0640);

if ($exclude) {
    file_put_contents(
        $dirPath . '/DO-NOT-PUBLISH',
        "The contributor asked that this target is not published.\n" .
        "Testing and improving detection only. Do not add to the public data set.\n"
    );
}

try {
    $stmt = $pdo->prepare(
        'INSERT INTO submissions
            (id, dir, created_utc, created_ts, ip_hash, file_count, total_bytes, exclude_public, consent_version)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)'
    );
    $stmt->execute([$id, $dirName, $stamp, $now, $ipHash, 1, $size + strlen($raw), $exclude ? 1 : 0, CONSENT_VERSION]);
} catch (Throwable $e) {
    error_log('[grouplab] index insert failed for ' . $id . ': ' . $e->getMessage());
}

respond(200, ['ok' => true, 'id' => $id]);
