<?php

declare(strict_types=1);

namespace CrystalReports;

use CrystalReports\Http\BridgeClient;

final class CrystalReport
{
    private ?string $reportId = null;

    public function __construct(private readonly BridgeClient $bridge) {}

    /** Open a report already registered/accessible to the Windows bridge. */
    public function load(string $reportPath): self
    {
        $result = $this->bridge->request('POST', 'api/v1/reports/open', ['path' => $reportPath]);
        $this->reportId = (string) ($result['reportId'] ?? '');
        if ($this->reportId === '') {
            throw new \RuntimeException('Bridge did not return reportId.');
        }
        return $this;
    }

    /** @return array<string, mixed> */
    public function metadata(): array { return $this->call('GET', 'metadata'); }
    /** @return array<string, mixed> */
    public function parameters(): array { return $this->call('GET', 'parameters'); }
    /** @return array<string, mixed> */
    public function tables(): array { return $this->call('GET', 'tables'); }
    /** @return array<string, mixed> */
    public function fields(): array { return $this->call('GET', 'fields'); }
    /** @return array<string, mixed> */
    public function formulas(): array { return $this->call('GET', 'formulas'); }
    /** @return array<string, mixed> */
    public function subreports(): array { return $this->call('GET', 'subreports'); }

    public function setParameter(string $name, mixed $value): self
    {
        $this->call('PUT', 'parameters', ['name' => $name, 'value' => $value]);
        return $this;
    }

    public function setDatabaseConnection(array $connection): self
    {
        if (isset($connection['host']) && !isset($connection['server'])) {
            $connection['server'] = $connection['host'];
            unset($connection['host']);
        }
        $this->call('PUT', 'database-connection', $connection);
        return $this;
    }

    public function setFormula(string $name, string $formula): self
    {
        $this->call('PUT', 'formulas', ['name' => $name, 'formula' => $formula]);
        return $this;
    }

    public function setPage(array $settings): self
    {
        $this->call('PUT', 'page', $settings);
        return $this;
    }

    public function save(?string $destination = null): array
    {
        return $this->call('POST', 'save', ['destination' => $destination]);
    }

    public function close(): void
    {
        if ($this->reportId !== null) {
            $this->bridge->request('DELETE', 'api/v1/reports/' . rawurlencode($this->reportId));
            $this->reportId = null;
        }
    }

    public function export(string $format, string $destination): string
    {
        $format = strtolower($format);
        if (!in_array($format, ['pdf', 'xlsx', 'xls', 'doc', 'rtf', 'csv'], true)) {
            throw new \InvalidArgumentException('Unsupported export format: ' . $format);
        }
        return $this->bridge->download($this->endpoint('export'), $destination, ['format' => $format]);
    }

    public function exportPdf(string $destination): string { return $this->export('pdf', $destination); }
    public function exportExcel(string $destination): string { return $this->export('xlsx', $destination); }

    private function call(string $method, string $action, array $payload = []): array
    {
        return $this->bridge->request($method, $this->endpoint($action), $payload);
    }

    private function endpoint(string $action): string
    {
        if ($this->reportId === null) {
            throw new \LogicException('Call load() before operating on a report.');
        }
        return 'api/v1/reports/' . rawurlencode($this->reportId) . '/' . ltrim($action, '/');
    }
}
