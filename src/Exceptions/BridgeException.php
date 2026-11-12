<?php

declare(strict_types=1);

namespace CrystalReports\Exceptions;

final class BridgeException extends CrystalReportsException
{
    public function __construct(string $message, public readonly int $httpStatus = 0, ?\Throwable $previous = null)
    {
        parent::__construct($message, $httpStatus, $previous);
    }
}
