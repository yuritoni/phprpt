<?php

declare(strict_types=1);

namespace CrystalReports\Http;

use CrystalReports\Config;
use CrystalReports\Exceptions\BridgeException;
use GuzzleHttp\Client;
use GuzzleHttp\Exception\GuzzleException;

final class BridgeClient
{
    private Client $http;

    public function __construct(private readonly Config $config)
    {
        $this->http = new Client([
            'base_uri' => rtrim($config->baseUrl, '/') . '/',
            'timeout' => $config->timeout,
            'connect_timeout' => 10,
            'verify' => $config->verifyTls,
            'http_errors' => false,
            'headers' => [
                'Accept' => 'application/json',
                'X-API-Key' => $config->apiKey,
                'User-Agent' => 'crystal-reports-php/0.2.0',
            ],
        ]);
    }

    /** @return array<string, mixed> */
    public function request(string $method, string $path, array $payload = []): array
    {
        try {
            $options = [];
            if ($payload !== []) {
                $options['json'] = $payload;
            }
            $response = $this->http->request($method, ltrim($path, '/'), $options);
            $status = $response->getStatusCode();
            $body = (string) $response->getBody();
            $data = $body !== '' ? json_decode($body, true) : [];

            if ($status < 200 || $status >= 300) {
                $message = is_array($data) ? ($data['message'] ?? 'Crystal Reports bridge request failed.') : 'Crystal Reports bridge returned an invalid response.';
                throw new BridgeException((string) $message, $status);
            }
            if (!is_array($data)) {
                throw new BridgeException('Bridge response is not valid JSON.', $status);
            }
            return $data;
        } catch (GuzzleException $e) {
            throw new BridgeException('Unable to communicate with Crystal Reports bridge: ' . $e->getMessage(), 0, $e);
        }
    }

    public function download(string $path, string $destination, array $payload = []): string
    {
        try {
            $response = $this->http->post(ltrim($path, '/'), [
                'json' => $payload,
                'headers' => ['Accept' => 'application/octet-stream'],
            ]);
            if ($response->getStatusCode() < 200 || $response->getStatusCode() >= 300) {
                throw new BridgeException('Report export failed.', $response->getStatusCode());
            }
            $directory = dirname($destination);
            if (!is_dir($directory) && !mkdir($directory, 0775, true) && !is_dir($directory)) {
                throw new BridgeException('Unable to create export directory.');
            }
            file_put_contents($destination, (string) $response->getBody());
            return $destination;
        } catch (GuzzleException $e) {
            throw new BridgeException('Unable to download report: ' . $e->getMessage(), 0, $e);
        }
    }
}
