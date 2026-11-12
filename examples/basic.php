<?php

declare(strict_types=1);

require dirname(__DIR__) . '/vendor/autoload.php';

use CrystalReports\Config;
use CrystalReports\CrystalReports;

$crystal = new CrystalReports(new Config(
    baseUrl: getenv('CRYSTAL_BRIDGE_URL') ?: 'https://crystal-bridge.internal/',
    apiKey: getenv('CRYSTAL_BRIDGE_API_KEY') ?: '',
    timeout: 180,
));

$report = $crystal->report()->load('laporan_polis.rpt');
$report->setParameter('TanggalAwal', '2026-01-01')
       ->setParameter('TanggalAkhir', '2026-10-31');

$report->exportPdf(__DIR__ . '/laporan-polis.pdf');
