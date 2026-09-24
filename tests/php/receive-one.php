<?php
/**
 * One submission through the real receiver, for the consent test, NOTES-FROM-PLANNING.md entry 183 section 3.4.
 *
 *     php tests/php/receive-one.php <root> <photograph> <0|1>
 *
 * <root> holds private/, which is the receiver's SITE_PRIVATE; the third argument is the "Do not include my
 * photos in the public data set" box. It prints the receiver's JSON answer and nothing else, and exits 1
 * when the receiver refused. tests/python/worker-tests.py runs it on the Linux runner and hands what it
 * wrote to the real worker, then to the pull script's own check.
 */

declare(strict_types=1);

require __DIR__ . '/receiver-harness.php';

if ($argc !== 4) {
    fwrite(STDERR, "usage: php tests/php/receive-one.php <root> <photograph> <0|1>\n");
    exit(2);
}

[, $root, $photo, $optOut] = $argv;
$source = file_get_contents(__DIR__ . '/../../website/api/upload.php');
if ($source === false || !is_file($photo)) {
    fwrite(STDERR, "could not read the receiver or the photograph\n");
    exit(2);
}

// The receiver moves the file it is given, so it gets a copy, as PHP gives it an upload in its own temporary folder.
@mkdir($root . '/uploads', 0750, true);
$tmp = $root . '/uploads/' . bin2hex(random_bytes(4)) . '-' . basename($photo);
copy($photo, $tmp);

$post = ['consent' => '1', 'cf-turnstile-response' => 'a-token', 'backing' => 'Cardboard', 'caliber' => '0.308'];
if ($optOut === '1') {
    $post['exclude_public'] = '1';
}

$files = ['photos' => ['name' => [basename($photo)], 'tmp_name' => [$tmp], 'size' => [filesize($tmp)], 'error' => [UPLOAD_ERR_OK]]];
$r = request($root, $source, $post, $files);
echo $r['json'] !== null ? json_encode($r['json']) : $r['raw'], "\n";
exit(($r['json']['ok'] ?? false) === true ? 0 : 1);
