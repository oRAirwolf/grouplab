<?php
/**
 * The target photo receiver for grouplab.org, NOTES-FROM-PLANNING.md entry 129.
 *
 * It is a port of the receiver that ran at pissinhot.com/targets, which was good work and is the
 * starting point rather than a thing to replace. Everything it had is kept: storage outside the
 * web root, content sniffing by magic bytes rather than by extension or by what the browser said,
 * safe stored names, a honeypot, per-address rate limits on a salted hash of CF-Connecting-IP
 * accepted only from Cloudflare's own ranges, size and count caps, a disk cap with a free-space
 * floor, a consent record written here rather than taken from the file, and a SHA-256 per file on
 * arrival.
 *
 * ======================================================================
 *  WHAT IS DIFFERENT, AND IT IS THE WHOLE POINT OF ENTRY 129.
 *
 *  The old receiver stored the uploaded bytes exactly as received, because the project reads
 *  camera facts out of the original file. That rule has been replaced, not relaxed. This receiver
 *  writes every accepted upload into a QUARANTINE folder and does nothing else with it. A separate
 *  worker, running as its own systemd service rather than as the web user, decodes each file and
 *  writes a NEW image from the decoded pixels alone, carrying over a short whitelist of camera
 *  facts as validated numbers. Then it deletes the original bytes.
 *
 *  So a payload hidden in an upload never reaches anybody: not appended data, not a polyglot, not
 *  a crafted metadata block. Nothing but pixels is carried over. PHP here never decodes an image,
 *  never calls GD or Imagick, and never opens a file for anything but sniffing its first 32 bytes
 *  and hashing it.
 * ======================================================================
 *
 * Also new, and each from entry 129's own sections:
 *  - Cloudflare Turnstile, verified server side before anything is written (section 2.1). A
 *    missing, reused, expired or failed token is refused, and so is a siteverify that cannot be
 *    reached: refusing is the safe direction when the check itself is unavailable.
 *  - No PDF. JPEG, PNG, HEIC/HEIF and TIFF only (Alan's decision 6).
 *  - A server-wide cap on submissions per hour across every address (section 2.3), so a botnet
 *    spread thinly enough to stay under the per-address limit still cannot fill the disk.
 */

declare(strict_types=1);

// ---------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------

const SITE_PRIVATE = '/home/airwolf/web/grouplab.org/private';
const QUARANTINE   = SITE_PRIVATE . '/quarantine';
const DB_PATH      = SITE_PRIVATE . '/submissions.db';
const SALT_PATH    = SITE_PRIVATE . '/submissions_salt.txt';
const TURNSTILE_SECRET_PATH = SITE_PRIVATE . '/turnstile-secret.txt';

const MAX_FILES            = 10;
const MAX_FILE_BYTES       = 30 * 1024 * 1024;   // 30 MB a file
const MAX_SUBMISSION_BYTES = 90 * 1024 * 1024;   // 90 MB a submission. Cloudflare caps a request
                                                 // body at 100 MB on Free and Pro; do not raise
                                                 // this past that without per-file uploads.

const RATE_PER_HOUR = 5;
const RATE_PER_DAY  = 20;

// NOTES-FROM-PLANNING.md entry 418 section 3: a set, sent as several requests of up to MAX_FILES files and MAX_SUBMISSION_BYTES each, and
// stored as one submission. A set's parts wait in SETS, which the intake worker never reads, until the last arrives; one not finished
// within SET_SECONDS cannot be continued and is deleted, with nothing of it kept. BYTES_PER_HOUR is what one address could already send
// in an hour before sets existed, five submissions of 90 MB, and a set counts against it, so sets let nobody send more in an hour.
const SETS            = SITE_PRIVATE . '/sets';
const MAX_SET_FILES   = 30;
const MAX_SET_BYTES   = 400 * 1024 * 1024;
const MAX_SET_PARTS   = 10;
const SET_SECONDS     = 3600;
const BYTES_PER_HOUR  = RATE_PER_HOUR * MAX_SUBMISSION_BYTES;
const MAX_LABEL_LEN   = 40;

// Entry 129 section 2.3, and the number is stated because the entry asks for it to be. Sixty an
// hour is twelve addresses at their own hourly limit, which is far more than this page has ever
// seen in a day, and it caps the worst case at 5.4 GB an hour rather than the whole disk.
const GLOBAL_PER_HOUR = 60;

// Oracle Cloud free tier boot volume. Check `df -h` before raising either of these.
const DISK_CAP_BYTES  = 20 * 1024 * 1024 * 1024;
const DISK_FREE_FLOOR = 3 * 1024 * 1024 * 1024;

// Entry 165 section 2: two levels, from the same text the page shows and the application offers. limits.json holds them and the build
// holds this file to it. A consent_v1 submission already stored stays publishable, because that is what its contributor agreed to.
const CONSENT_VERSION = 'consent_v2';
const CONSENT_TEXTS = [
    'testing'     => "I took these photos, or I have permission to share them. GroupLab may use them to test and improve its detection. They are kept by the project and never published. GPS location data is removed from every photo when it arrives.",
    'publishable' => "I took these photos, or I have permission to share them. GroupLab may use them to test and improve its detection, and I understand they may be published as part of GroupLab's public test data on GitHub and in its research articles, under the GPL-3.0 license, for anyone to download and use. GPS location data is removed from every photo before anything is published.",
];

const MAX_STEM_LEN = 100;

const TURNSTILE_VERIFY_URL = 'https://challenges.cloudflare.com/turnstile/v0/siteverify';
const TURNSTILE_TIMEOUT_SECONDS = 8;

/**
 * Accepted types, sniffed from content only. The client's MIME type and the client's extension are
 * never trusted for this decision. PDF is gone: it is not a photograph, the worker cannot rebuild
 * it from pixels, and the whole safety of this pipeline rests on being able to.
 */
const ACCEPTED = [
    'image/jpeg' => 'jpg',
    'image/png'  => 'png',
    'image/tiff' => 'tif',
    'image/heic' => 'heic',
    'image/heif' => 'heif',
];

// ---------------------------------------------------------------------
// Response helpers
// ---------------------------------------------------------------------

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

// ---------------------------------------------------------------------
// Client address
// ---------------------------------------------------------------------

/**
 * The site sits behind Cloudflare, so REMOTE_ADDR is a Cloudflare edge address. Hashing that would
 * put every visitor in the world into a handful of rate limit buckets. Use CF-Connecting-IP, but
 * only where the connection genuinely came from Cloudflare, so the header cannot be spoofed by
 * anybody reaching the origin directly.
 *
 * Refresh from https://www.cloudflare.com/ips/ if limiting ever misbehaves.
 */
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

/**
 * Raw addresses are never stored. The salt lives outside the web root and is generated once, so
 * the hashes cannot be reversed by trying every address.
 */
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

// ---------------------------------------------------------------------
// Turnstile
// ---------------------------------------------------------------------

/**
 * Entry 129 section 2.1. The secret half never enters this repository: Alan writes it into a file
 * under private/, mode 600, with grouplab-set-turnstile-secret, in his own SSH session. Nothing
 * here can print it, and a failure to read it refuses the submission rather than accepting one
 * unchecked.
 *
 * A token is single use at Cloudflare's end, so a replayed one comes back as a failure here
 * without this code having to remember anything.
 */
function turnstile_ok(string $token, string $ip, ?string &$why): bool
{
    $why = null;

    if ($token === '' || strlen($token) > 2048) {
        $why = 'missing';
        return false;
    }

    if (!is_readable(TURNSTILE_SECRET_PATH)) {
        error_log('[grouplab] the Turnstile secret is not readable; refusing rather than accepting unchecked');
        $why = 'unconfigured';
        return false;
    }

    $secret = trim((string) file_get_contents(TURNSTILE_SECRET_PATH));
    if ($secret === '') {
        $why = 'unconfigured';
        return false;
    }

    $body = http_build_query(['secret' => $secret, 'response' => $token, 'remoteip' => $ip]);
    $context = stream_context_create([
        'http' => [
            'method'        => 'POST',
            'header'        => "Content-Type: application/x-www-form-urlencoded\r\n",
            'content'       => $body,
            'timeout'       => TURNSTILE_TIMEOUT_SECONDS,
            'ignore_errors' => true,
        ],
        'ssl' => ['verify_peer' => true, 'verify_peer_name' => true],
    ]);

    $raw = @file_get_contents(TURNSTILE_VERIFY_URL, false, $context);
    if ($raw === false) {
        // Entry 129 section 2.1: "If siteverify cannot be reached, refuse rather than accept."
        error_log('[grouplab] Turnstile siteverify could not be reached');
        $why = 'unreachable';
        return false;
    }

    $parsed = json_decode($raw, true);
    if (!is_array($parsed) || ($parsed['success'] ?? false) !== true) {
        $codes = is_array($parsed['error-codes'] ?? null) ? implode(',', $parsed['error-codes']) : 'none';
        error_log('[grouplab] Turnstile refused a token: ' . $codes);
        $why = 'refused';
        return false;
    }

    return true;
}

// ---------------------------------------------------------------------
// Content sniffing
// ---------------------------------------------------------------------

/**
 * Magic byte sniffing. finfo is deliberately not the authority: several libmagic builds report
 * HEIC as application/octet-stream, which would reject every photograph taken on an iPhone, and
 * iPhones are where most of these submissions come from.
 *
 * Returns a MIME type from ACCEPTED, or null where the content is not something this endpoint
 * takes. PDF is sniffed for on purpose, so it can be refused by name rather than as "not a photo".
 */
function sniff_type(string $path, ?string &$sawPdf = null): ?string
{
    $sawPdf = null;
    $fh = @fopen($path, 'rb');
    if ($fh === false) {
        return null;
    }
    $head = (string) fread($fh, 32);
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

    if (str_starts_with($head, '%PDF-')) {
        $sawPdf = 'pdf';
        return null;
    }

    // ISO base media file format. Bytes 4..8 are 'ftyp' and the major brand follows at 8..12,
    // which is how HEIC and HEIF are identified. Anything else in that family, avif, mp4, mov and
    // so on, falls through and is refused, which is intentional.
    if (substr($head, 4, 4) === 'ftyp') {
        $brand = strtolower(substr($head, 8, 4));
        if (in_array($brand, ['heic', 'heix', 'heim', 'heis', 'hevc', 'hevx', 'hevm', 'hevs'], true)) {
            return 'image/heic';
        }
        if (in_array($brand, ['mif1', 'msf1', 'miaf', 'heif'], true)) {
            return 'image/heif';
        }
    }

    return null;
}

// ---------------------------------------------------------------------
// Filenames
// ---------------------------------------------------------------------

/**
 * Keep the original name, because it carries information worth having: PXL_ means a Pixel phone,
 * image_cropper_ means it was already cropped and is therefore less useful. Strip everything that
 * could escape the directory or confuse a shell, and set the extension from the sniffed content
 * rather than from anything the client said.
 */
function safe_stored_name(int $index, string $originalName, string $mime): string
{
    $base = basename(str_replace(['\\', '/'], '_', $originalName));
    $base = preg_replace('/[\x00-\x1F\x7F]/u', '', $base) ?? '';

    $stem = pathinfo($base, PATHINFO_FILENAME);
    $stem = preg_replace('/[^A-Za-z0-9._-]/', '_', $stem) ?? '';
    $stem = preg_replace('/_{2,}/', '_', $stem) ?? '';
    $stem = ltrim($stem, '.-_');

    if ($stem === '') {
        $stem = 'image';
    }
    if (strlen($stem) > MAX_STEM_LEN) {
        $stem = substr($stem, 0, MAX_STEM_LEN);
    }

    return sprintf('%03d_%s.%s', $index, $stem, ACCEPTED[$mime]);
}

// ---------------------------------------------------------------------
// Storage and index
// ---------------------------------------------------------------------

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
    // Entry 418 section 3.2: each stored file's SHA-256 for a day, so the same file sent again is kept once.
    $pdo->exec('CREATE TABLE IF NOT EXISTS file_hashes (sha256 TEXT NOT NULL, created_ts INTEGER NOT NULL, submission_id TEXT NOT NULL)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_hash ON file_hashes(sha256, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_sub_ip_ts ON submissions(ip_hash, created_ts)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS idx_sub_ts ON submissions(created_ts)');
    return $pdo;
}

function stored_total_bytes(PDO $pdo): int
{
    $row = $pdo->query('SELECT COALESCE(SUM(total_bytes), 0) AS t FROM submissions WHERE deleted = 0')->fetch(PDO::FETCH_ASSOC);
    return (int) ($row['t'] ?? 0);
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
    fail(500, 'Could not allocate a submission ID. Please try again.');
}

// ---------------------------------------------------------------------
// Input helpers
// ---------------------------------------------------------------------

/** The submission's index row, written by its first request and brought up to date by each later part of a set. */
function index_row(PDO $pdo, string $id, string $dir, string $createdUtc, int $createdTs, string $ipHash, int $files, int $bytes, string $level): void
{
    try {
        $stmt = $pdo->prepare(
            'INSERT INTO submissions
                (id, dir, created_utc, created_ts, ip_hash, file_count, total_bytes, exclude_public, consent_version)
             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
             ON CONFLICT(id) DO UPDATE SET file_count = excluded.file_count, total_bytes = excluded.total_bytes'
        );
        $stmt->execute([$id, $dir, $createdUtc, $createdTs, $ipHash, $files, $bytes, $level === 'testing' ? 1 : 0, CONSENT_VERSION]);
    } catch (Throwable $e) {
        // The files and meta.json are on disk and they are the source of truth. A failed index row is
        // not worth failing the submission over.
        error_log('[grouplab] index write failed for ' . $id . ': ' . $e->getMessage());
    }
}

/** A set's staging record, or null where there is none to continue. */
function open_set(string $id): ?array
{
    $raw = @file_get_contents(SETS . '/' . $id . '/set.json');
    $set = $raw === false ? null : json_decode($raw, true);
    return is_array($set) && isset($set['ip_hash'], $set['created_ts'], $set['received'], $set['parts'], $set['files']) ? $set : null;
}

/**
 * Entry 418: a set not finished within twice SET_SECONDS is deleted with everything in it, and its index row marked deleted, so nothing
 * half sent waits on the server. And a file's SHA-256 is forgotten after a day.
 */
function forget_abandoned_sets(PDO $pdo, int $now): void
{
    foreach (glob(SETS . '/*/set.json') ?: [] as $record) {
        $set = json_decode((string) @file_get_contents($record), true);
        if (!is_array($set) || $now - (int) ($set['created_ts'] ?? 0) > 2 * SET_SECONDS) {
            $folder = dirname($record);
            foreach (glob($folder . '/*') ?: [] as $f) {
                @unlink($f);
            }
            @rmdir($folder);
            $pdo->prepare('UPDATE submissions SET deleted = 1 WHERE id = ?')->execute([basename($folder)]);
        }
    }
    $pdo->prepare('DELETE FROM file_hashes WHERE created_ts < ?')->execute([$now - 86400]);
}

/** The form's answers, as the receiver writes them, never taken from inside an image. */
function answers(): array
{
    return [
        'target_backing'    => field('backing', 60),
        'attachment_method' => field('attachment', 60),
        'shot_distance'     => field('shot_distance', 40),
        'caliber'           => field('caliber', 40),
        'notes'             => field('notes', 300),
        'credit_name'       => field('credit_name', 60),
    ];
}

function field(string $name, int $maxLen): string
{
    $v = $_POST[$name] ?? '';
    if (!is_string($v)) {
        return '';
    }
    $v = preg_replace('/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/u', '', $v) ?? '';
    $v = trim($v);
    if (mb_strlen($v) > $maxLen) {
        $v = mb_substr($v, 0, $maxLen);
    }
    return $v;
}

function checked(string $name): bool
{
    $v = $_POST[$name] ?? '';
    return $v === '1' || $v === 'on' || $v === 'true';
}

function upload_error_message(int $code): string
{
    return match ($code) {
        UPLOAD_ERR_INI_SIZE, UPLOAD_ERR_FORM_SIZE => 'One of those files is larger than the 30 MB limit.',
        UPLOAD_ERR_PARTIAL => 'One of those files only uploaded part way. Please try again.',
        UPLOAD_ERR_NO_TMP_DIR, UPLOAD_ERR_CANT_WRITE => 'The server could not write the upload to disk. Please try again later.',
        UPLOAD_ERR_EXTENSION => 'The upload was stopped by the server. Please try again later.',
        default => 'Something went wrong with that upload. Please try again.',
    };
}

// ---------------------------------------------------------------------
// Request handling
// ---------------------------------------------------------------------

if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    fail(405, 'This endpoint accepts POST only.', 'method');
}

// A post larger than post_max_size arrives with $_POST and $_FILES both empty and nothing to
// explain why. Catch it and say something useful, otherwise the page appears to hang and then
// fail for no visible reason.
$contentLength = (int) ($_SERVER['CONTENT_LENGTH'] ?? 0);
if ($contentLength > 0 && empty($_POST) && empty($_FILES)) {
    fail(413, 'That submission was too large to accept in one go. Please send up to 90 MB at a time and submit the rest separately.', 'too_large');
}

// Honeypot, hidden with CSS, so a person never sees it and never fills it. A bot that fills it
// gets a response that looks exactly like success and nothing is written to disk.
if (($_POST['contact_reason'] ?? '') !== '') {
    respond(200, ['ok' => true, 'id' => bin2hex(random_bytes(4))]);
}

// Entry 165 section 2. A page loaded before the two levels arrived still posts the old pair of boxes, which mean the same two levels.
$level = field('level', 20);
if ($level === '' && checked('consent')) {
    $level = checked('exclude_public') ? 'testing' : 'publishable';
}
if (!array_key_exists($level, CONSENT_TEXTS)) {
    fail(400, 'Choose how GroupLab may use the photos before they can be accepted.', 'consent');
}

$ip = client_ip();

// NOTES-FROM-PLANNING.md entry 418 section 3: a set of up to MAX_SET_FILES files is sent as several requests, each under Cloudflare's
// cap, and stored as one submission. The first part passes the person check and is given a token; the later parts bring the token
// instead, from the same address, within the hour, in order. A part that says it continues a set but cannot show which one is refused.
$setId = field('set_id', 16);
$part  = max(1, (int) field('part', 3));
$parts = max(1, (int) field('parts', 3));
$continuing = $part > 1;
if ($parts > MAX_SET_PARTS || $part > $parts || ($continuing && preg_match('/^[0-9a-f]{8}$/', $setId) !== 1)) {
    fail(400, 'That part of a set did not say which set it belongs to. Nothing from it was kept; please send the whole set again.', 'set');
}

// Entry 129 section 2.1: before a single uploaded byte is read beyond what PHP has already
// buffered. PHP has written the temporary files by the time this script runs, which nothing here
// can change, but nothing reads or keeps them until the token has passed. A set's later parts
// present the token the first part was given instead, which only a request that passed can hold.
if (!$continuing) {
    $turnstileWhy = null;
    if (!turnstile_ok((string) ($_POST['cf-turnstile-response'] ?? ''), $ip, $turnstileWhy)) {
        $message = match ($turnstileWhy) {
            'missing' => 'The page could not prove you are a person. Please reload the page and try again.',
            'unconfigured', 'unreachable' => 'The check that proves you are a person is not answering just now. Please try again in a few minutes.',
            default => 'That verification did not pass, or it has expired. Please reload the page and try again.',
        };
        fail($turnstileWhy === 'unreachable' || $turnstileWhy === 'unconfigured' ? 503 : 403, $message, 'turnstile');
    }
}

if (!is_dir(QUARANTINE) && !@mkdir(QUARANTINE, 0750, true) && !is_dir(QUARANTINE)) {
    fail(500, 'Storage is not available right now. Please try again later.', 'storage');
}

try {
    $pdo = db();
} catch (Throwable $e) {
    error_log('[grouplab] database open failed: ' . $e->getMessage());
    fail(500, 'Storage is not available right now. Please try again later.', 'storage');
}

$ipHash = ip_hash($ip);
$now    = time();
forget_abandoned_sets($pdo, $now);

$set = null;
if ($continuing) {
    $set = open_set($setId);
    if ($set === null || !hash_equals((string) ($set['token_hash'] ?? ''), hash('sha256', field('set_token', 64)))
        || $set['ip_hash'] !== $ipHash || $now - (int) $set['created_ts'] > SET_SECONDS) {
        fail(400, 'That set could not be continued: it has expired or was started elsewhere. Nothing from it was kept; please send the whole set again.', 'set');
    }
    if ((int) $set['received'] + 1 !== $part || (int) $set['parts'] !== $parts || $set['level'] !== $level) {
        fail(400, 'That part of the set arrived out of order. Nothing from the set was kept; please send the whole set again.', 'set');
    }
} else {
    $stmt = $pdo->prepare('SELECT COUNT(*) FROM submissions WHERE ip_hash = ? AND created_ts > ?');
    $stmt->execute([$ipHash, $now - 3600]);
    $lastHour = (int) $stmt->fetchColumn();

    $stmt->execute([$ipHash, $now - 86400]);
    $lastDay = (int) $stmt->fetchColumn();

    if ($lastHour >= RATE_PER_HOUR || $lastDay >= RATE_PER_DAY) {
        fail(429, 'That is as many submissions as this connection can send for now. Thank you, genuinely. Please come back in an hour if you have more.', 'rate_limit');
    }

    $globalHour = (int) $pdo->query('SELECT COUNT(*) FROM submissions WHERE created_ts > ' . ($now - 3600))->fetchColumn();
    if ($globalHour >= GLOBAL_PER_HOUR) {
        fail(429, 'This page is taking more photos than it can handle just now. Please try again in an hour, and thank you for bearing with it.', 'busy');
    }
}

$free = @disk_free_space(QUARANTINE);
if (stored_total_bytes($pdo) >= DISK_CAP_BYTES || ($free !== false && $free < DISK_FREE_FLOOR)) {
    fail(507, 'The collection is full at the moment and cannot take new photos. Please try again in a few days, the space gets cleared regularly.', 'full');
}

// ---------------------------------------------------------------------
// Files
// ---------------------------------------------------------------------

$incoming = $_FILES['photos'] ?? null;

// A single file posted under photos rather than photos[] arrives as scalars. Normalise it.
if (is_array($incoming) && isset($incoming['name']) && !is_array($incoming['name'])) {
    $incoming = array_map(static fn ($value) => [$value], $incoming);
}

if (!is_array($incoming) || !isset($incoming['name']) || !is_array($incoming['name'])) {
    fail(400, 'No photos were attached to that submission.', 'no_files');
}

$count = count($incoming['name']);
if ($count === 0) {
    fail(400, 'No photos were attached to that submission.', 'no_files');
}
if ($count > MAX_FILES) {
    fail(400, 'That is more than ' . MAX_FILES . ' files in one send. The page splits a larger set into parts by itself; please reload it and try again.', 'too_many');
}

// Entry 418 section 3.1: each file's label, sheet and view, where the sender gave one, in the order of the files.
$labels = $_POST['labels'] ?? [];
$labels = is_array($labels) ? array_values($labels) : [];

$candidates = [];
$total      = 0;

for ($i = 0; $i < $count; $i++) {
    $err = (int) ($incoming['error'][$i] ?? UPLOAD_ERR_NO_FILE);
    if ($err === UPLOAD_ERR_NO_FILE) {
        continue;
    }

    if ($err !== UPLOAD_ERR_OK) {
        fail(400, upload_error_message($err), 'upload_error');
    }

    $tmp = (string) ($incoming['tmp_name'][$i] ?? '');
    if ($tmp === '' || !is_uploaded_file($tmp)) {
        fail(400, 'One of those files did not arrive properly. Please try again.', 'upload_error');
    }

    $size = (int) filesize($tmp);
    if ($size <= 0) {
        fail(400, 'One of those files was empty.', 'empty');
    }
    if ($size > MAX_FILE_BYTES) {
        fail(400, 'One of those files is larger than the 30 MB limit.', 'too_big');
    }

    $total += $size;
    if ($total > MAX_SUBMISSION_BYTES) {
        fail(400, 'That send is over the 90 MB limit. The page splits a larger set into parts by itself; please reload it and try again.', 'too_large');
    }

    $mime = sniff_type($tmp, $sawPdf);
    if ($sawPdf !== null) {
        fail(400, 'This page cannot take PDFs, only photographs: JPEG, PNG, HEIC and TIFF. If you scanned the sheet, save it as a JPEG or a PNG and send that.', 'pdf');
    }
    if ($mime === null) {
        fail(400, 'One of those files is not a photo this page can take. JPEG, PNG, HEIC and TIFF are accepted.', 'bad_type');
    }

    $label = is_string($labels[$i] ?? null) ? trim((string) preg_replace('/[\x00-\x1F\x7F]/u', '', $labels[$i])) : '';
    $candidates[] = [
        'tmp'      => $tmp,
        'original' => (string) ($incoming['name'][$i] ?? 'image'),
        'size'     => $size,
        'mime'     => $mime,
        'sha256'   => hash_file('sha256', $tmp),
        'label'    => mb_substr($label, 0, MAX_LABEL_LEN),
    ];
}

if ($candidates === []) {
    fail(400, 'No photos were attached to that submission.', 'no_files');
}

// Entry 418 section 3.2: the same file sent twice, in this set or in the last day, is recognised by its SHA-256 and kept once, so a
// person told something unclear can send again without making a second copy.
$seen = $set !== null ? array_column($set['files'], 'sha256') : [];
$fresh = [];
$duplicates = 0;
$duplicateOf = null;
$stmt = $pdo->prepare('SELECT submission_id FROM file_hashes WHERE sha256 = ? AND created_ts > ? LIMIT 1');
foreach ($candidates as $c) {
    $stmt->execute([$c['sha256'], $now - 86400]);
    $earlier = $stmt->fetchColumn();
    if (in_array($c['sha256'], $seen, true) || $earlier !== false) {
        $duplicates++;
        $duplicateOf ??= $earlier !== false ? (string) $earlier : null;
        continue;
    }
    $seen[] = $c['sha256'];
    $fresh[] = $c;
}

// Entry 418 section 3: what a whole set may hold, and the address's bytes in the hour, which a set may not raise past what five
// separate submissions of 90 MB could bring before sets existed (BYTES_PER_HOUR). The first part says how large the set will be. Counted after the files already sent are set aside.
$declared = $set !== null ? (int) $set['declared_bytes'] : ($parts > 1 ? (int) field('set_bytes', 12) : $total);
$setFiles = ($set !== null ? count($set['files']) : 0) + count($fresh);
$setBytes = ($set !== null ? (int) $set['bytes'] : 0) + array_sum(array_column($fresh, 'size'));
if ($setFiles > MAX_SET_FILES || $declared > MAX_SET_BYTES || $setBytes > $declared) {
    fail(400, 'That set is larger than one submission can hold: up to ' . MAX_SET_FILES . ' files and ' . intdiv(MAX_SET_BYTES, 1024 * 1024) . ' MB. Nothing from it was kept; please send it as two sets.', 'set_too_large');
}
if ($set === null) {
    $stmt = $pdo->prepare('SELECT COALESCE(SUM(total_bytes), 0) FROM submissions WHERE ip_hash = ? AND created_ts > ?');
    $stmt->execute([$ipHash, $now - 3600]);
    if ((int) $stmt->fetchColumn() + $declared > BYTES_PER_HOUR) {
        fail(429, 'That is as many megabytes as this connection can send in an hour. Thank you, genuinely. Please come back in an hour with the rest.', 'rate_limit');
    }
}

if ($parts === 1 && $fresh === []) {
    // Every file is already in: nothing new is stored and no submission is counted, and the answer says where they are.
    respond(200, ['ok' => true, 'id' => $duplicateOf ?? '', 'files' => 0, 'duplicates' => $duplicates, 'already' => true]);
}

// ---------------------------------------------------------------------
// Store, into quarantine and nowhere else, or a set's part into its own staging folder until the set is whole
// ---------------------------------------------------------------------

$stamp = gmdate('Y-m-d\TH:i:s\Z');
if ($set === null) {
    $id      = new_submission_id($pdo);
    $dirName = gmdate('Y-m-d') . '_' . $id;
    $dirPath = $parts > 1 ? SETS . '/' . $id : QUARANTINE . '/' . $dirName;
    if ((!is_dir(dirname($dirPath)) && !@mkdir(dirname($dirPath), 0750, true)) || (!@mkdir($dirPath, 0750) && !is_dir($dirPath))) {
        error_log('[grouplab] could not create ' . $dirPath);
        fail(500, 'Storage is not available right now. Please try again later.', 'storage');
    }
} else {
    $id      = $setId;
    $dirName = (string) $set['dir'];
    $dirPath = SETS . '/' . $id;
}

/**
 * Removes what this request wrote and the folder if it then stands empty: a failed write leaves nothing half-stored behind it.
 */
function abandon(string $dirPath, array $written): void
{
    foreach ($written as $f) {
        @unlink($dirPath . '/' . $f);
    }
    @rmdir($dirPath);
}

$stored  = [];
$written = [];
$index   = $set !== null ? count($set['files']) : 0;

foreach ($fresh as $c) {
    $index++;
    $storedName = safe_stored_name($index, $c['original'], $c['mime']);
    $dest       = $dirPath . '/' . $storedName;

    if (!move_uploaded_file($c['tmp'], $dest)) {
        error_log('[grouplab] move_uploaded_file failed for ' . $dest);
        abandon($dirPath, $written);
        fail(500, 'One of those files could not be saved. Please try again.', 'storage');
    }
    $written[] = $storedName;
    @chmod($dest, 0640);

    $onDisk = @filesize($dest);
    if ($onDisk !== $c['size']) {
        error_log(sprintf('[grouplab] size mismatch for %s: expected %d, on disk %d', $dest, $c['size'], $onDisk === false ? -1 : $onDisk));
        abandon($dirPath, $written);
        fail(500, 'One of those files could not be saved. Please try again.', 'storage');
    }

    $stored[] = [
        'index'         => $index,
        'stored_name'   => $storedName,
        'original_name' => $c['original'],
        'bytes'         => $c['size'],
        'sniffed_type'  => $c['mime'],
        'sha256'        => $c['sha256'],
    ] + ($c['label'] !== '' ? ['label' => $c['label']] : []);
}

if ($parts > 1 && $part < $parts) {
    // A part of a set that is not its last: kept in the set's staging folder, out of the worker's reach, and counted against the
    // address from its first part, so the rate limits see the set as one submission.
    $record = $set ?? [
        'dir'            => $dirName,
        'ip_hash'        => $ipHash,
        'created_ts'     => $now,
        'created_utc'    => $stamp,
        'parts'          => $parts,
        'level'          => $level,
        'declared_bytes' => $declared,
        'bytes'          => 0,
        'files'          => [],
        'answers'        => answers(),
        'user_agent'     => substr((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 0, 500),
    ];
    $token = $set === null ? bin2hex(random_bytes(16)) : null;
    if ($token !== null) {
        $record['token_hash'] = hash('sha256', $token);
    }
    $record['received'] = $part;
    $record['bytes']    = (int) $record['bytes'] + array_sum(array_column($stored, 'bytes'));
    $record['files']    = array_merge($record['files'], $stored);
    file_put_contents($dirPath . '/set.json', json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . "\n", LOCK_EX);
    @chmod($dirPath . '/set.json', 0640);
    // While the set is unfinished its row holds the size it said it would be, so the hour's bytes count what is still to come.
    index_row($pdo, $id, $dirName, (string) $record['created_utc'], (int) $record['created_ts'], $ipHash, count($record['files']), max((int) $record['bytes'], (int) $record['declared_bytes']), $level);
    respond(200, ['ok' => true, 'id' => $id, 'part' => $part, 'parts' => $parts, 'files' => count($stored), 'duplicates' => $duplicates]
        + ($token !== null ? ['set_token' => $token] : []));
}

$files = $set !== null ? array_merge($set['files'], $stored) : $stored;
if ($files === []) {
    abandon($dirPath, []);
    fail(400, 'Every file in that set was already sent, so there was nothing new to keep. Thank you; it is already in.', 'already');
}

$excludePublic = $level === 'testing';
$created       = $set !== null ? (string) $set['created_utc'] : $stamp;

/**
 * The consent record and the answers are written here, by the receiver, from what the form sent.
 * Nothing in this file is taken from inside an uploaded image, which matters twice over: the
 * worker is about to throw away everything in those files but the pixels, and text arriving from a
 * stranger is data rather than anything anybody acts on.
 */
$meta = [
    'schema_version' => 1,
    'submission_id'  => $id,
    'submitted_utc'  => $created,

    // Deliberately the third key, so it is visible the moment anybody opens this. Where it is true
    // the photos are for private testing only and never appear in the published data set.
    'exclude_from_public_dataset' => $excludePublic,

    // Entry 165 section 4: where it came from, the upload page or the application.
    'source' => 'web',

    'consent' => [
        'agreed'        => true,
        'version'       => CONSENT_VERSION,
        'level'         => $level,
        'agreed_at_utc' => $created,
        'text'          => CONSENT_TEXTS[$level],
    ],
    // Camera distance was deliberately dropped from the form: it is recoverable from the
    // target's apparent size and the focal length, far more accurately than anybody can
    // estimate it standing there. Every question that survives is one that cannot be read off
    // the image.
    'answers'    => $set !== null ? $set['answers'] : answers(),
    'user_agent' => $set !== null ? (string) $set['user_agent'] : substr((string) ($_SERVER['HTTP_USER_AGENT'] ?? ''), 0, 500),
    'files'      => $files,

    // What has happened to this submission so far. The worker rewrites this file with what it did.
    'stage' => 'quarantine',
] + ($parts > 1 ? ['set' => ['parts' => $parts]] : []);

$json = json_encode($meta, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
file_put_contents($dirPath . '/meta.json', $json . "\n", LOCK_EX);
@chmod($dirPath . '/meta.json', 0640);

// A second, unmissable marker. Somebody browsing the tree with ls should not have to open a JSON
// file to find out these photos are not to be published.
if ($excludePublic) {
    file_put_contents(
        $dirPath . '/DO-NOT-PUBLISH',
        "The contributor asked that these photos are not published.\n" .
        "Testing on a private machine only. Do not add to the public data set.\n"
    );
}

if ($parts > 1) {
    // The set is whole: its staging record goes and the folder moves into quarantine in one step, so the worker never sees part of it.
    @unlink($dirPath . '/set.json');
    if (!@rename($dirPath, QUARANTINE . '/' . $dirName)) {
        error_log('[grouplab] could not move set ' . $id . ' into quarantine');
        fail(500, 'The set could not be stored. Please try again later.', 'storage');
    }
}

index_row($pdo, $id, $dirName, $created, $set !== null ? (int) $set['created_ts'] : $now, $ipHash, count($files),
    array_sum(array_column($files, 'bytes')), $level);

// Remembered only once the submission is whole, so a set given up half way does not make its own files look sent when it is sent again.
$remember = $pdo->prepare('INSERT INTO file_hashes (sha256, created_ts, submission_id) VALUES (?, ?, ?)');
foreach ($files as $f) {
    $remember->execute([$f['sha256'], $now, $id]);
}

respond(200, ['ok' => true, 'id' => $id, 'files' => count($files), 'duplicates' => $duplicates] + ($parts > 1 ? ['part' => $part, 'parts' => $parts] : []));
