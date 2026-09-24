<?php
/**
 * One target from the application through the real receiver, NOTES-FROM-PLANNING.md entry 165 section 4.
 *
 *     php tests/php/receive-app-one.php <root> <photograph> <testing|publishable>
 *
 * <root> holds private/, the receiver's SITE_PRIVATE. The package is built as the application builds it: the image named by its size and
 * SHA-256, the consent in limits.json's words for the level, and every part present. It prints the receiver's JSON answer and nothing
 * else, and exits 1 when the receiver refused. tests/python/worker-tests.py hands what it wrote to the real worker, which has to take it
 * exactly as it takes the upload page's.
 */

declare(strict_types=1);

require __DIR__ . '/receiver-harness.php';

if ($argc !== 4) {
    fwrite(STDERR, "usage: php tests/php/receive-app-one.php <root> <photograph> <testing|publishable>\n");
    exit(2);
}

[, $root, $photo, $level] = $argv;
$source = file_get_contents(__DIR__ . '/../../website/api/app-submission.php');
$limits = json_decode((string) file_get_contents(__DIR__ . '/../../website/api/limits.json'), true);
if ($source === false || !is_file($photo) || !is_array($limits)) {
    fwrite(STDERR, "could not read the receiver, limits.json or the photograph\n");
    exit(2);
}

@mkdir($root . '/uploads', 0750, true);
$tmp = $root . '/uploads/' . bin2hex(random_bytes(4)) . '-' . basename($photo);
copy($photo, $tmp);

$post = ['package' => app_package($limits, $tmp, basename($photo), $level)];
$files = ['image' => ['name' => basename($photo), 'tmp_name' => $tmp, 'size' => filesize($tmp), 'error' => UPLOAD_ERR_OK]];
$r = request($root, $source, $post, $files);
echo $r['json'] !== null ? json_encode($r['json']) : $r['raw'], "\n";
exit(($r['json']['ok'] ?? false) === true ? 0 : 1);
