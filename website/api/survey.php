<?php
/**
 * The receiver for the hardware survey, NOTES-FROM-PLANNING.md entries 207 and 208 and docs/SURVEY.md.
 *
 * ======================================================================
 *  THERE IS NO PASSWORD, NO TOKEN AND NO TURNSTILE, AND THAT IS DELIBERATE.
 *
 *  As with error reports, a key compiled into an open source program is public on the day it ships, so
 *  this is protected by limits: a 64 KB wall, a per-address and per-installation rate limit, a
 *  server-wide hourly cap, a disk floor, and a kill switch file.
 * ======================================================================
 *
 * **Anyone can post a fake report, and nothing in one is believed.** The report is JSON in the form field
 * `report`. It is decoded, checked against its schema, and written again from the named fields alone, each
 * checked for its type and cut to its length; anything else is dropped unread. Neither the sender's
 * address nor the installation number is stored: only salted hashes of them, the address's for the rate
 * limit and the installation's so one machine is counted once. The time is kept only as the day.
 *
 * The worker, website/server/grouplab-survey-worker.py, counts each report into the aggregate and deletes
 * it; nothing here is kept longer than thirty days whatever happens (entries 215 and 216).
 */

declare(strict_types=1);

const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';
const SURVEY       = SITE_PRIVATE . '/survey';
const INCOMING     = SURVEY . '/incoming';
const DB_PATH      = SITE_PRIVATE . '/survey.db';
const SALT_PATH    = SITE_PRIVATE . '/submissions_salt.txt';

/** While this file exists every report is refused, and the application keeps its report to try again. */
const CLOSED_PATH = SITE_PRIVATE . '/survey-closed';

/**
 * limits.json's surveyOpen, which the site's build holds this to. The server already routes this file to PHP, so until the worker that
 * counts and deletes reports is installed, the receiver itself refuses everything: nothing is stored that nothing would delete.
 */
const OPEN = false;

const SCHEMA           = 'grouplab-survey-1';
const MAX_BYTES        = 64 * 1024;
const RATE_PER_DAY     = 10;
const INSTALLATION_DAY = 3;
const GLOBAL_PER_HOUR  = 500;
const DISK_CAP_BYTES   = 100 * 1024 * 1024;
const DISK_FREE_FLOOR  = 3 * 1024 * 1024 * 1024;
const MAX_ANALYSES     = 50;
const MAX_STAGES       = 40;
const MAX_TEXT         = 120;

function respond(int $status, array $payload): never
{
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store');
    header('X-Content-Type-Options: nosniff');
    echo json_encode($payload, JSON_UNESCAPED_SLASHES);
    exit;
}

function fail(int $status, string $message, string $code, bool $retry = false): never
{
    respond($status, ['ok' => false, 'code' => $code, 'error' => $message, 'retry' => $retry]);
}

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
            file         TEXT PRIMARY KEY,
            created_ts   INTEGER NOT NULL,
            ip_hash      TEXT NOT NULL,
            installation TEXT NOT NULL,
            bytes        INTEGER NOT NULL
        )'
    );
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_survey_ip_ts ON reports(ip_hash, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_survey_install_ts ON reports(installation, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_survey_ts ON reports(created_ts)');
    return $pdo;
}

/** A one line string cut to its length, or null where it is not a string. */
function text($value, int $max = MAX_TEXT): ?string
{
    if (!is_string($value)) {
        return null;
    }
    $value = preg_replace('/[\x00-\x1F\x7F]/u', ' ', $value) ?? '';
    return mb_substr(trim($value), 0, $max);
}

/** A whole number within its bounds, or null. */
function whole($value, int $low, int $high): ?int
{
    return is_int($value) && $value >= $low && $value <= $high ? $value : null;
}

/** A number within its bounds, or null. */
function number($value, float $low, float $high): ?float
{
    return (is_int($value) || is_float($value)) && $value >= $low && $value <= $high ? (float) $value : null;
}

/** The stages of an analysis, each a name and a time, and nothing else. */
function stages($in): array
{
    $out = [];
    foreach (array_slice(is_array($in) ? $in : [], 0, MAX_STAGES) as $s) {
        $name = is_array($s) ? text($s['stage'] ?? null, 60) : null;
        $ms   = is_array($s) ? whole($s['milliseconds'] ?? null, 0, 3600000) : null;
        if ($name !== null && $name !== '' && $ms !== null) {
            $out[] = ['stage' => $name, 'milliseconds' => $ms];
        }
    }
    return $out;
}

/**
 * The report as the schema allows it, built from the named fields only, or the reason it cannot be.
 * The installation number is returned beside it, not in it: only its hash is ever written.
 * Returns [report, installation, null] or [null, null, reason].
 */
function clean(array $in): array
{
    if (($in['schema'] ?? null) !== SCHEMA) {
        return [null, null, 'the report is not one this receiver reads'];
    }
    $installation = $in['installation'] ?? null;
    if (!is_string($installation) || preg_match('/^[0-9a-f]{32}$/', $installation) !== 1) {
        return [null, null, 'the report has no installation number'];
    }
    $version = text($in['version'] ?? null, 64);
    if ($version === null || $version === '') {
        return [null, null, 'the report does not say which build sent it'];
    }
    $m = is_array($in['machine'] ?? null) ? $in['machine'] : null;
    $os = $m === null ? null : text($m['os'] ?? null);
    $cores = $m === null ? null : whole($m['cores'] ?? null, 1, 1024);
    if ($os === null || $os === '' || $cores === null) {
        return [null, null, 'the report does not describe the machine'];
    }
    $machine = ['os' => $os, 'architecture' => text($m['architecture'] ?? '') ?? '', 'cores' => $cores];
    $optional = [
        'processor'        => text($m['processor'] ?? null),
        'memoryMegabytes'  => whole($m['memoryMegabytes'] ?? null, 1, 16 * 1024 * 1024),
        'screen'           => text($m['screen'] ?? null, 40),
        'screenScale'      => number($m['screenScale'] ?? null, 0.25, 8),
        'device'           => text($m['device'] ?? null),
        'cameraMegapixels' => number($m['cameraMegapixels'] ?? null, 0.1, 1000),
    ];
    foreach ($optional as $k => $v) {
        if ($v !== null && $v !== '') {
            $machine[$k] = $v;
        }
    }
    $out = ['schema' => SCHEMA, 'version' => $version, 'machine' => $machine];
    if (is_array($in['benchmark'] ?? null)) {
        $b = $in['benchmark'];
        $workload = text($b['workload'] ?? null, 60);
        $total = whole($b['totalMilliseconds'] ?? null, 1, 3600000);
        if ($workload !== null && $workload !== '' && $total !== null) {
            $out['benchmark'] = [
                'workload'          => $workload,
                'width'             => whole($b['width'] ?? null, 1, 100000) ?? 0,
                'height'            => whole($b['height'] ?? null, 1, 100000) ?? 0,
                'totalMilliseconds' => $total,
                'stages'            => stages($b['stages'] ?? null),
                'peakMegabytes'     => whole($b['peakMegabytes'] ?? null, 0, 16 * 1024 * 1024) ?? 0,
                'holesPlaced'       => whole($b['holesPlaced'] ?? null, 0, 1000) ?? 0,
                'holesFound'        => whole($b['holesFound'] ?? null, 0, 1000) ?? 0,
            ];
        }
    }
    $analyses = [];
    foreach (array_slice(is_array($in['analyses'] ?? null) ? $in['analyses'] : [], 0, MAX_ANALYSES) as $a) {
        if (!is_array($a) || whole($a['width'] ?? null, 1, 100000) === null || whole($a['height'] ?? null, 1, 100000) === null) {
            continue;
        }
        $analyses[] = [
            'width'         => $a['width'],
            'height'        => $a['height'],
            'workingWidth'  => whole($a['workingWidth'] ?? null, 1, 100000) ?? $a['width'],
            'workingHeight' => whole($a['workingHeight'] ?? null, 1, 100000) ?? $a['height'],
            'stages'        => stages($a['stages'] ?? null),
            'peakMegabytes' => whole($a['peakMegabytes'] ?? null, 0, 16 * 1024 * 1024) ?? 0,
        ];
    }
    $out['analyses'] = $analyses;
    return [$out, $installation, null];
}

// ---------------------------------------------------------------------

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    fail(405, 'This endpoint accepts POST only.', 'method');
}

if (!OPEN || is_file(CLOSED_PATH)) {
    fail(503, 'Survey reports are not being taken just now. Yours is kept and tried again later.', 'closed', true);
}

if ((int) ($_SERVER['CONTENT_LENGTH'] ?? 0) > MAX_BYTES) {
    fail(413, 'That report is larger than the receiver takes.', 'too_large');
}

$raw = $_POST['report'] ?? '';
if (!is_string($raw) || $raw === '') {
    fail(400, 'The report arrived empty.', 'bad_report');
}
if (strlen($raw) > MAX_BYTES) {
    fail(413, 'That report is larger than the receiver takes.', 'too_large');
}
$decoded = json_decode($raw, true, 8);
if (!is_array($decoded)) {
    fail(400, 'The report could not be read.', 'bad_report');
}
[$report, $installation, $problem] = clean($decoded);
if ($report === null) {
    fail(400, 'The report was refused: ' . $problem . '.', 'bad_report');
}

if (!is_dir(INCOMING) && !@mkdir(INCOMING, 0750, true) && !is_dir(INCOMING)) {
    fail(500, 'Storage is not available right now.', 'storage', true);
}

try {
    $pdo = db();
} catch (Throwable $e) {
    error_log('[grouplab] survey database open failed: ' . $e->getMessage());
    fail(500, 'Storage is not available right now.', 'storage', true);
}

$ipHash      = ip_hash(client_ip());
$installHash = ip_hash('installation|' . $installation);
$now         = time();

$stmt = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE ip_hash = ? AND created_ts > ?');
$stmt->execute([$ipHash, $now - 86400]);
$fromAddress = (int) $stmt->fetchColumn();
$stmt = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE installation = ? AND created_ts > ?');
$stmt->execute([$installHash, $now - 86400]);
$fromInstallation = (int) $stmt->fetchColumn();
if ($fromAddress >= RATE_PER_DAY || $fromInstallation >= INSTALLATION_DAY) {
    fail(429, 'That is as many reports as this machine can send today.', 'rate_limit', true);
}
$all = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE created_ts > ?');
$all->execute([$now - 3600]);
if ((int) $all->fetchColumn() >= GLOBAL_PER_HOUR) {
    fail(429, 'The receiver is taking no more reports this hour.', 'busy', true);
}

$free = @disk_free_space(SURVEY);
$used = (int) ($pdo->query('SELECT COALESCE(SUM(bytes), 0) FROM reports WHERE created_ts > ' . ($now - 30 * 86400))->fetchColumn() ?: 0);
if ($used >= DISK_CAP_BYTES || ($free !== false && $free < DISK_FREE_FLOOR)) {
    fail(507, 'Reports cannot be taken just now because the server is short of space.', 'full', true);
}

$report['installation'] = $installHash;
$report['day'] = gmdate('Y-m-d', $now);
$json = json_encode($report, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . "\n";
$name = gmdate('Y-m-d', $now) . '_' . bin2hex(random_bytes(8)) . '.json';
$tmp  = INCOMING . '/.' . $name . '.tmp';
if (file_put_contents($tmp, $json, LOCK_EX) === false || !rename($tmp, INCOMING . '/' . $name)) {
    error_log('[grouplab] could not store a survey report');
    fail(500, 'That report could not be saved.', 'storage', true);
}
@chmod(INCOMING . '/' . $name, 0640);

try {
    $stmt = $pdo->prepare('INSERT INTO reports (file, created_ts, ip_hash, installation, bytes) VALUES (?, ?, ?, ?, ?)');
    $stmt->execute([$name, $now, $ipHash, $installHash, strlen($json)]);
    // Only what the limits need is kept, and only for thirty days.
    $pdo->prepare('DELETE FROM reports WHERE created_ts < ?')->execute([$now - 30 * 86400]);
} catch (Throwable $e) {
    error_log('[grouplab] survey index insert failed: ' . $e->getMessage());
}

respond(200, ['ok' => true]);
