# Crystal Reports PHP Library

PHP 8.4 client library for controlling SAP Crystal Reports through a private Windows bridge.
PHP on Linux does not load or execute `.rpt` files directly. The Windows bridge uses SAP's .NET SDK and in-process RAS APIs.

## Architecture

```text
Linux / PHP 8.4 application
        | HTTPS + X-API-Key
        v
Windows Server 2022 / IIS / ASP.NET Web API 2 (.NET Framework 4.8)
        |
        v
SAP Crystal Reports .NET SDK + In-Process RAS
        |
        v
.rpt files, database drivers, PDF / Excel / Word export
```

## PHP installation

```bash
composer require guzzlehttp/guzzle:^7.9
```

For local development from this package:

```bash
composer install
```

Configure environment variables:

```dotenv
CRYSTAL_BRIDGE_URL=https://crystal-bridge.internal
CRYSTAL_BRIDGE_API_KEY=use-a-long-random-secret
```

## Usage

```php
<?php
require __DIR__ . '/vendor/autoload.php';

use CrystalReports\Config;
use CrystalReports\CrystalReports;

$client = new CrystalReports(new Config(
    baseUrl: getenv('CRYSTAL_BRIDGE_URL'),
    apiKey: getenv('CRYSTAL_BRIDGE_API_KEY'),
    timeout: 180,
));

$report = $client->report()->load('laporan_polis.rpt'); // relative to Windows ReportRoot

try {
    $metadata = $report->metadata();
    $parameters = $report->parameters();

    $report->setParameter('TanggalAwal', '2026-01-01')
           ->setParameter('TanggalAkhir', '2026-10-31')
           ->setFormula('JudulLaporan', '"LAPORAN PRODUKSI"')
           ->setPage(['orientation' => 'Landscape', 'paperSize' => 'PaperA4']);

    $report->setDatabaseConnection([
        'host' => '10.10.10.20',
        'database' => 'db_reporting',
        'username' => 'report_user',
        'password' => getenv('REPORT_DB_PASSWORD'),
    ]);

    $report->save('laporan_polis_custom.rpt');
    $report->exportPdf(__DIR__ . '/storage/laporan-polis.pdf');
    $report->export('xlsx', __DIR__ . '/storage/laporan-polis.xlsx');
} finally {
    $report->close();
}
```

## Implemented in this starter

- Open report by an allowlisted relative path.
- Read report metadata, parameters, database tables/fields, formulas, and subreports.
- Set report parameters.
- Modify an existing formula's text.
- Update database logon information for main-report and subreport tables.
- Set paper size and orientation.
- Save to a new `.rpt` file in the configured output directory.
- Export PDF, XLSX, XLS, DOC, RTF, and CSV.
- API-key authentication, report path allowlisting, output path restrictions, and idle-session cleanup.

## Not yet implemented

The initial bridge deliberately does not claim arbitrary visual designer editing. Moving/resizing report objects, creating/removing fields, editing section formatting/conditional formulas, changing links, and editing subreport layouts need additional in-process RAS controller code and verification against the exact SAP support pack installed. The API contract includes the extension points and roadmap. Formula text, page setup, parameter values, database login, save, and export are implemented in this first bridge source.

## Windows bridge

See [`windows-bridge/README.md`](windows-bridge/README.md) for SDK prerequisites, IIS deployment, permissions, and operational guidance.

## Security and production cautions

- Expose the bridge only on a private network, over HTTPS, with firewall/IP restrictions.
- Use a high-entropy API key and rotate it. API key authentication is a service-to-service baseline, not a complete per-user authorization system.
- Do not log API keys, database passwords, parameter values, or report data.
- Configure the Windows report root and output root explicitly; never allow arbitrary filesystem paths.
- A report session is in-memory and is lost on IIS application-pool recycle. PHP must reopen the report after a recycle.
- Keep IIS worker process x64 and do not enable web garden/multiple worker processes for this in-memory session store.
- Confirm SAP licensing/redistribution terms and support-pack compatibility before production deployment.
