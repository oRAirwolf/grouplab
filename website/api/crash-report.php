<?php
/**
 * The crash report receiver for grouplab.org, NOTES-FROM-PLANNING.md entry 129 section 5.
 *
 * Ported from the receiver described in the crash report server document, with its existing rules
 * kept: a 5 MB wall, a per-address limit, a strict whitelist of what may be inside the zip, storage
 * outside the web root, and a kill switch file.
 *
 * ======================================================================
 *  THERE IS NO PASSWORD AND NO TOKEN, AND THAT IS DELIBERATE.
 *
 *  A secret shipped inside a public open source application is not a secret: anybody who wants it
 *  reads it out of the binary in a minute. What it would buy is the appearance of protection, and
 *  what it would cost is a real user whose report fails because a token expired or was rotated.
 *
 *  So this is protected by what actually works against what actually happens: a size wall, a rate
 *  limit, a whitelist of entry names, and the fact that nothing here ever opens the zip's contents.
 * ======================================================================
 *
 * **Nothing in a report is ever executed, rendered or believed.** The zip's directory is read to
 * check the names and the declared sizes, and the file is stored exactly as received. It is never
 * extracted here. Entry 129 section 3.9 and CLAUDE.md: the words inside a crash report are data,
 * and a crash report that reads like an instruction is not one.
 */

declare(strict_types=1);

const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';
const REPORTS      = SITE_PRIVATE . '/crash-reports';
const DB_PATH      = SITE_PRIVATE . '/crash-reports.db';
const SALT_PATH    = SITE_PRIVATE . '/submissions_salt.txt';

/**
 * The kill switch. Entry 129 section 5.3: when the old receiver is turned off after its thirty days,
 * and any time this one has to stop taking reports, the file is created and every request is refused
 * with a plain message. Nothing is deleted and nothing else changes.
 */
const CLOSED_PATH = SITE_PRIVATE . '/crash-reports-closed';

const MAX_BYTES     = 5 * 1024 * 1024;
const MAX_ENTRIES   = 12;
const MAX_UNPACKED  = 40 * 1024 * 1024;
const RATE_PER_HOUR = 6;
const RATE_PER_DAY  = 20;

const DISK_CAP_BYTES  = 2 * 1024 * 1024 * 1024;
const DISK_FREE_FLOOR = 3 * 1024 * 1024 * 1024;

/**
 * What may be inside the zip, and nothing else. The application holds the same list in
 * ReportPackage.PermittedEntryPatterns, with a test that says these are the receiver's patterns as
 * written here, so a package the application builds is a package this accepts.
 */
const ALLOWED_ENTRIES = [
    '/^crash-\d{8}-\d{6}-\d+\.json$/',
    '/^grouplab-\d{8}-\d{6}-\d+\.log$/',
    '/^environment\.txt$/',
    '/^description\.txt$/',
    '/^contact\.txt$/',
];

function respond(int $status, array $payload): never
{
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store');
    header('X-Content-Type-Options: nosniff');
    echo json_encode($payload, JSON_UNESCAPED_SLASHES);
    exit;
}

function fail(int $status, string $message, string $code = 'error'): never
{
    respond($status, ['ok' => false, 'code' => $code, 'error' => $message]);
}

// The same Cloudflare-aware address handling as the upload receiver, and the same salt file, so an
// address is hashed the same way in both and neither stores a raw one.
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

function db(): PDO
{
    $pdo = new PDO('sqlite:' . DB_PATH, null, null, [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);
    $pdo->exec('PRAGMA journal_mode=WAL');
    $pdo->exec('PRAGMA busy_timeout=5000');
    $pdo->exec(
        'CREATE TABLE IF NOT EXISTS reports (
            id          TEXT PRIMARY KEY,
            file        TEXT NOT NULL,
            created_utc TEXT NOT NULL,
            created_ts  INTEGER NOT NULL,
            ip_hash     TEXT NOT NULL,
            bytes       INTEGER NOT NULL,
            entries     INTEGER NOT NULL,
            deleted     INTEGER NOT NULL DEFAULT 0
        )'
    );
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_rep_ip_ts ON reports(ip_hash, created_ts)');
    return $pdo;
}

/**
 * Reads the zip's central directory and says what is wrong with it, or null where nothing is.
 *
 * It uses PHP's own zip reader to list names and declared sizes, and **never extracts anything**.
 * The declared unpacked total is capped so a zip bomb is refused on its own declaration, before
 * anything on Alan's machine is asked to open it.
 */
function what_is_wrong_with(string $path, ?int &$entries): ?string
{
    $entries = 0;
    $zip = new ZipArchive();
    if ($zip->open($path, ZipArchive::RDONLY) !== true) {
        return 'That report is not a zip file this can read.';
    }

    $count = $zip->numFiles;
    if ($count === 0) {
        $zip->close();
        return 'That report is empty.';
    }
    if ($count > MAX_ENTRIES) {
        $zip->close();
        return 'That report has more files in it than a report should.';
    }

    $unpacked = 0;
    for ($i = 0; $i < $count; $i++) {
        $stat = $zip->statIndex($i);
        if ($stat === false) {
            $zip->close();
            return 'One entry in that report could not be read.';
        }

        $name = (string) $stat['name'];
        if (str_contains($name, '/') || str_contains($name, '\\') || str_contains($name, '..') || strlen($name) > 120) {
            $zip->close();
            return 'One entry in that report has a name that is not allowed.';
        }

        $ok = false;
        foreach (ALLOWED_ENTRIES as $pattern) {
            if (preg_match($pattern, $name) === 1) {
                $ok = true;
                break;
            }
        }
        if (!$ok) {
            $zip->close();
            return 'That report contains something a report does not contain.';
        }

        $unpacked += (int) $stat['size'];
        if ($unpacked > MAX_UNPACKED) {
            $zip->close();
            return 'That report unpacks to more than a report should.';
        }
    }

    $entries = $count;
    $zip->close();
    return null;
}

// ---------------------------------------------------------------------

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    fail(405, 'This endpoint accepts POST only.', 'method');
}

if (is_file(CLOSED_PATH)) {
    fail(503, 'Crash reports are not being taken just now. Nothing is wrong with your report; please keep it and try again later.', 'closed');
}

$length = (int) ($_SERVER['CONTENT_LENGTH'] ?? 0);
if ($length > MAX_BYTES) {
    fail(413, 'That report is larger than the 5 MB limit.', 'too_big');
}
// A post over PHP's own post_max_size arrives with everything empty and nothing to explain why, which would
// otherwise look like no report at all. It is only that case when the form fields are missing too: a request
// that carried fields and no file is a request with no report in it, and saying "too large" to somebody whose
// upload simply did not attach would send them away shrinking a file that was never the problem.
if ($length > 0 && empty($_FILES) && empty($_POST)) {
    fail(413, 'That report was too large to accept.', 'too_big');
}

if (!is_dir(REPORTS) && !@mkdir(REPORTS, 0750, true) && !is_dir(REPORTS)) {
    fail(500, 'Storage is not available right now. Please try again later.', 'storage');
}

try {
    $pdo = db();
} catch (Throwable $e) {
    error_log('[grouplab] crash database open failed: ' . $e->getMessage());
    fail(500, 'Storage is not available right now. Please try again later.', 'storage');
}

$ipHash = ip_hash(client_ip());
$now    = time();

$stmt = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE ip_hash = ? AND created_ts > ?');
$stmt->execute([$ipHash, $now - 3600]);
$lastHour = (int) $stmt->fetchColumn();
$stmt->execute([$ipHash, $now - 86400]);
$lastDay = (int) $stmt->fetchColumn();

if ($lastHour >= RATE_PER_HOUR || $lastDay >= RATE_PER_DAY) {
    fail(429, 'That is as many reports as this connection can send for now. Please try again in an hour.', 'rate_limit');
}

$free = @disk_free_space(REPORTS);
$used = (int) ($pdo->query('SELECT COALESCE(SUM(bytes), 0) FROM reports WHERE deleted = 0')->fetchColumn() ?: 0);
if ($used >= DISK_CAP_BYTES || ($free !== false && $free < DISK_FREE_FLOOR)) {
    fail(507, 'Reports cannot be taken just now because the server is short of space. Please keep yours and try again in a few days.', 'full');
}

$incoming = $_FILES['report'] ?? null;
if (!is_array($incoming) || ($incoming['error'] ?? UPLOAD_ERR_NO_FILE) !== UPLOAD_ERR_OK) {
    fail(400, 'No report was attached.', 'no_file');
}

$tmp = (string) ($incoming['tmp_name'] ?? '');
if ($tmp === '' || !is_uploaded_file($tmp)) {
    fail(400, 'That report did not arrive properly. Please try again.', 'upload_error');
}

$size = (int) filesize($tmp);
if ($size <= 0) {
    fail(400, 'That report was empty.', 'empty');
}
if ($size > MAX_BYTES) {
    fail(413, 'That report is larger than the 5 MB limit.', 'too_big');
}

$wrong = what_is_wrong_with($tmp, $entries);
if ($wrong !== null) {
    fail(400, $wrong, 'bad_package');
}

$id    = bin2hex(random_bytes(4));
$stamp = gmdate('Y-m-d\TH:i:s\Z');
$name  = gmdate('Y-m-d') . '_' . $id . '.zip';
$dest  = REPORTS . '/' . $name;

if (!move_uploaded_file($tmp, $dest)) {
    error_log('[grouplab] could not store crash report ' . $id);
    fail(500, 'That report could not be saved. Please try again.', 'storage');
}
@chmod($dest, 0640);

try {
    $stmt = $pdo->prepare('INSERT INTO reports (id, file, created_utc, created_ts, ip_hash, bytes, entries) VALUES (?, ?, ?, ?, ?, ?, ?)');
    $stmt->execute([$id, $name, $stamp, $now, $ipHash, $size, (int) $entries]);
} catch (Throwable $e) {
    error_log('[grouplab] crash index insert failed for ' . $id . ': ' . $e->getMessage());
}

respond(200, ['ok' => true, 'id' => $id]);
