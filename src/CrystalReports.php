<?php

declare(strict_types=1);

namespace CrystalReports;

use CrystalReports\Http\BridgeClient;

final class CrystalReports
{
    private BridgeClient $bridge;

    public function __construct(Config $config) { $this->bridge = new BridgeClient($config); }
    public function report(): CrystalReport { return new CrystalReport($this->bridge); }
    /** @return array<string, mixed> */
    public function health(): array { return $this->bridge->request('GET', 'api/v1/health'); }
}
