<?php

declare(strict_types=1);

namespace CrystalReports;

final class Config
{
    public function __construct(
        public readonly string $baseUrl,
        public readonly string $apiKey,
        public readonly int $timeout = 120,
        public readonly bool $verifyTls = true,
    ) {
        if (!filter_var($baseUrl, FILTER_VALIDATE_URL)) {
            throw new \InvalidArgumentException('baseUrl must be a valid URL.');
        }
        if ($apiKey === '') {
            throw new \InvalidArgumentException('apiKey must not be empty.');
        }
        if ($timeout < 1) {
            throw new \InvalidArgumentException('timeout must be greater than zero.');
        }
    }
}
