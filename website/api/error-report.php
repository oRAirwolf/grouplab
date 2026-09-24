<?php
/**
 * The receiver for error reports GroupLab sends by itself, NOTES-FROM-PLANNING.md entry 194 section 3.1.
 *
 * ======================================================================
 *  THERE IS NO PASSWORD, NO TOKEN AND NO TURNSTILE, AND THAT IS DELIBERATE.
 *
 *  The application is not a browser, and a key compiled into an open source program is public on the
 *  day it ships. So this is protected, as the crash and target receivers are, by limits: a 256 KB wall,
 *  a per-address rate limit, a server-wide hourly cap, a disk floor, and a kill switch file.
 *
 *  The GitHub token that turns reports into issues is not here and never will be. It lives in a root
 *  owned file only the worker's service is handed, and the worker, not this, talks to GitHub.
 * ======================================================================
 *
 * **Anyone can post a fake report, and nothing in one is ever believed.** The report is JSON in the form
 * field `report`. It is decoded, checked against its schema, cut down to the fields the schema names, and
 * written again from those fields alone; anything else in it is dropped unread. Its text is data, never
 * an instruction to anybody, including whoever reads the issue it becomes. The sender's address is never
 * stored: only a salted hash of it, for the rate limit, as the other receivers do.
 */

declare(strict_types=1);

const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';
const REPORTS      = SITE_PRIVATE . '/error-reports';
const INCOMING     = REPORTS . '/incoming';
const DB_PATH      = SITE_PRIVATE . '/error-reports.db';
const SALT_PATH    = SITE_PRIVATE . '/submissions_salt.txt';

/** While this file exists every report is refused, and the application keeps its reports to try again. */
const CLOSED_PATH = SITE_PRIVATE . '/error-reports-closed';

const SCHEMA          = 'grouplab-error-report-1';
const MAX_BYTES       = 256 * 1024;
const RATE_PER_HOUR   = 20;
const RATE_PER_DAY    = 60;
const GLOBAL_PER_HOUR = 300;
const DISK_CAP_BYTES  = 200 * 1024 * 1024;
const DISK_FREE_FLOOR = 3 * 1024 * 1024 * 1024;

const MAX_EXCEPTIONS  = 5;
const MAX_STACK       = 20000;
const MAX_MESSAGE     = 2000;
const MAX_ACTIONS     = 20;
const MAX_ACTION      = 100;
const MAX_DESCRIPTION = 500;

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
            report_id   TEXT PRIMARY KEY,
            file        TEXT NOT NULL,
            created_ts  INTEGER NOT NULL,
            ip_hash     TEXT NOT NULL,
            bytes       INTEGER NOT NULL
        )'
    );
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_err_ip_ts ON reports(ip_hash, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_err_ts ON reports(created_ts)');
    return $pdo;
}

/** A string field cut to its length and to one line where it has to be one, or null where it is not a string. */
function text($value, int $max, bool $oneLine = false): ?string
{
    if (!is_string($value)) {
        return null;
    }
    $value = preg_replace('/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/u', '', $value) ?? '';
    if ($oneLine) {
        $value = str_replace(["\r", "\n"], ' ', $value);
    }
    return mb_substr($value, 0, $max);
}

/**
 * The report as the schema allows it, built from the named fields only, or the reason it cannot be.
 * Returns [report, null] or [null, reason].
 */
function clean(array $in): array
{
    if (($in['schema'] ?? null) !== SCHEMA) {
        return [null, 'the report is not one this receiver reads'];
    }
    $kind = $in['kind'] ?? null;
    if ($kind !== 'survived' && $kind !== 'closed') {
        return [null, 'the report does not say whether GroupLab survived the error or closed'];
    }
    $made = $in['made'] ?? null;
    if ($made !== 'automatic' && $made !== 'by hand') {
        return [null, 'the report does not say how it was made'];
    }
    $id = $in['report_id'] ?? null;
    if (!is_string($id) || preg_match('/^[0-9a-f]{32}$/', $id) !== 1) {
        return [null, 'the report has no identifier'];
    }
    $count = $in['count'] ?? null;
    if (!is_int($count) || $count < 1 || $count > 10000) {
        return [null, 'the report does not say how many times the error happened'];
    }
    $app = is_array($in['app'] ?? null) ? $in['app'] : [];
    $version = text($app['version'] ?? null, 64, true);
    if ($version === null || $version === '') {
        return [null, 'the report does not say which build sent it'];
    }
    $exceptions = [];
    foreach (array_slice(is_array($in['exceptions'] ?? null) ? $in['exceptions'] : [], 0, MAX_EXCEPTIONS) as $e) {
        if (!is_array($e) || !is_string($e['type'] ?? null)) {
            return [null, 'an exception in the report has no type'];
        }
        $exceptions[] = [
            'type'    => text($e['type'], 200, true),
            'message' => text($e['message'] ?? '', MAX_MESSAGE) ?? '',
            'stack'   => text($e['stack'] ?? '', MAX_STACK) ?? '',
        ];
    }
    if ($exceptions === [] && $kind === 'survived') {
        return [null, 'a report of an error survived has to carry the error'];
    }
    $actions = [];
    foreach (array_slice(is_array($in['last_actions'] ?? null) ? $in['last_actions'] : [], 0, MAX_ACTIONS) as $a) {
        if (is_string($a)) {
            $actions[] = text($a, MAX_ACTION, true);
        }
    }
    $env = is_array($in['environment'] ?? null) ? $in['environment'] : [];
    $out = [
        'schema'      => SCHEMA,
        'report_id'   => $id,
        'kind'        => $kind,
        'made'        => $made,
        'count'       => $count,
        'app'         => [
            'version' => $version,
            'commit'  => text($app['commit'] ?? '', 40, true) ?? '',
            'channel' => text($app['channel'] ?? '', 40, true) ?? '',
        ],
        'environment' => [
            'os'            => text($env['os'] ?? '', 120, true) ?? '',
            'framework'     => text($env['framework'] ?? '', 120, true) ?? '',
            'renderer'      => text($env['renderer'] ?? '', 120, true) ?? '',
            'display_scale' => is_int($env['display_scale'] ?? null) || is_float($env['display_scale'] ?? null) ? $env['display_scale'] : null,
        ],
        'exceptions'   => $exceptions,
        'last_actions' => $actions,
    ];
    // Entry 194 section 2.3: an automatic report carries no free text at all; one made by hand may keep its description.
    if ($made === 'by hand' && is_string($in['description'] ?? null)) {
        $out['description'] = text($in['description'], MAX_DESCRIPTION);
    }
    return [$out, null];
}

// ---------------------------------------------------------------------

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    fail(405, 'This endpoint accepts POST only.', 'method');
}

if (is_file(CLOSED_PATH)) {
    fail(503, 'Error reports are not being taken just now. Nothing is wrong with yours; it is kept and tried again later.', 'closed', true);
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
$decoded = json_decode($raw, true, 16);
if (!is_array($decoded)) {
    fail(400, 'The report could not be read.', 'bad_report');
}
[$report, $problem] = clean($decoded);
if ($report === null) {
    fail(400, 'The report was refused: ' . $problem . '.', 'bad_report');
}

if (!is_dir(INCOMING) && !@mkdir(INCOMING, 0750, true) && !is_dir(INCOMING)) {
    fail(500, 'Storage is not available right now.', 'storage', true);
}

try {
    $pdo = db();
} catch (Throwable $e) {
    error_log('[grouplab] error report database open failed: ' . $e->getMessage());
    fail(500, 'Storage is not available right now.', 'storage', true);
}

$ipHash = ip_hash(client_ip());
$now    = time();

$seen = $pdo->prepare('SELECT 1 FROM reports WHERE report_id = ?');
$seen->execute([$report['report_id']]);
if ($seen->fetchColumn() !== false) {
    // Entry 194 section 2.4: the same report is never taken twice; the application is told it arrived, so it stops trying.
    respond(200, ['ok' => true, 'id' => $report['report_id'], 'again' => true]);
}

$stmt = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE ip_hash = ? AND created_ts > ?');
$stmt->execute([$ipHash, $now - 3600]);
$lastHour = (int) $stmt->fetchColumn();
$stmt->execute([$ipHash, $now - 86400]);
$lastDay = (int) $stmt->fetchColumn();
if ($lastHour >= RATE_PER_HOUR || $lastDay >= RATE_PER_DAY) {
    fail(429, 'That is as many reports as this connection can send for now.', 'rate_limit', true);
}
$all = $pdo->prepare('SELECT COUNT(*) FROM reports WHERE created_ts > ?');
$all->execute([$now - 3600]);
if ((int) $all->fetchColumn() >= GLOBAL_PER_HOUR) {
    fail(429, 'The receiver is taking no more reports this hour.', 'busy', true);
}

$free = @disk_free_space(REPORTS);
$used = (int) ($pdo->query('SELECT COALESCE(SUM(bytes), 0) FROM reports WHERE created_ts > ' . ($now - 30 * 86400))->fetchColumn() ?: 0);
if ($used >= DISK_CAP_BYTES || ($free !== false && $free < DISK_FREE_FLOOR)) {
    fail(507, 'Reports cannot be taken just now because the server is short of space.', 'full', true);
}

$json = json_encode($report, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . "\n";
$name = gmdate('Y-m-d\THis\Z') . '_' . $report['report_id'] . '.json';
$tmp  = INCOMING . '/.' . $name . '.tmp';
if (file_put_contents($tmp, $json, LOCK_EX) === false || !rename($tmp, INCOMING . '/' . $name)) {
    error_log('[grouplab] could not store error report ' . $report['report_id']);
    fail(500, 'That report could not be saved.', 'storage', true);
}
@chmod(INCOMING . '/' . $name, 0640);

try {
    $stmt = $pdo->prepare('INSERT INTO reports (report_id, file, created_ts, ip_hash, bytes) VALUES (?, ?, ?, ?, ?)');
    $stmt->execute([$report['report_id'], $name, $now, $ipHash, strlen($json)]);
} catch (Throwable $e) {
    error_log('[grouplab] error report index insert failed: ' . $e->getMessage());
}

respond(200, ['ok' => true, 'id' => $report['report_id']]);
