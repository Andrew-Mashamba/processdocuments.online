<?php

namespace App\Traits;

use Illuminate\Support\Facades\Log;
use Illuminate\Support\Str;

/**
 * Journey Logger Trait for tracking prompt-to-response flow.
 * Provides consistent, structured logging for performance analysis.
 */
trait JourneyLogger
{
    protected ?string $journeyRequestId = null;
    protected ?float $journeyStartTime = null;
    protected string $journeyRoute = '';
    protected ?string $journeySessionId = null;
    protected array $journeySteps = [];

    /**
     * Start a new journey log.
     */
    protected function startJourney(string $route, ?string $sessionId = null): string
    {
        $this->journeyRequestId = Str::random(12);
        $this->journeyStartTime = microtime(true);
        $this->journeyRoute = $route;
        $this->journeySessionId = $sessionId;
        $this->journeySteps = [];

        $this->logJourneyStep('JOURNEY_START', "Route: {$route}", [
            'route' => $route,
            'sessionId' => $sessionId,
            'timestamp' => now()->toIso8601String(),
        ]);

        return $this->journeyRequestId;
    }

    /**
     * Get elapsed time in milliseconds.
     */
    protected function getJourneyElapsedMs(): int
    {
        if (!$this->journeyStartTime) {
            return 0;
        }
        return (int) ((microtime(true) - $this->journeyStartTime) * 1000);
    }

    /**
     * Log a step in the journey.
     */
    protected function logJourneyStep(string $phase, string $message, array $data = []): void
    {
        $elapsed = $this->getJourneyElapsedMs();

        $step = [
            'phase' => $phase,
            'message' => $message,
            'elapsedMs' => $elapsed,
            'data' => $data,
        ];

        $this->journeySteps[] = $step;

        $dataJson = !empty($data) ? json_encode($data) : '';
        Log::channel('journey')->info(
            "[{$this->journeyRequestId}] [{$phase}] {$message} | {$elapsed}ms | {$dataJson}"
        );
    }

    /**
     * Log route decision.
     */
    protected function logJourneyRouteDecision(string $decision, string $reason, array $context = []): void
    {
        $this->logJourneyStep('ROUTE_DECISION', "{$decision}: {$reason}", array_merge([
            'decision' => $decision,
            'reason' => $reason,
        ], $context));
    }

    /**
     * Log model selection.
     */
    protected function logJourneyModelSelection(string $model, string $complexity, string $costInfo): void
    {
        $this->logJourneyStep('MODEL_SELECTION', "Selected {$model} for {$complexity} task", [
            'model' => $model,
            'complexity' => $complexity,
            'costInfo' => $costInfo,
        ]);
    }

    /**
     * Log context tier.
     */
    protected function logJourneyContextTier(string $tier, int $totalMessages, int $filteredMessages): void
    {
        $this->logJourneyStep('CONTEXT_TIER', "Tier={$tier}, Messages={$filteredMessages}/{$totalMessages}", [
            'tier' => $tier,
            'totalMessages' => $totalMessages,
            'filteredMessages' => $filteredMessages,
        ]);
    }

    /**
     * Log prompt info.
     */
    protected function logJourneyPrompt(string $prompt): void
    {
        $preview = strlen($prompt) > 100 ? substr($prompt, 0, 100) . '...' : $prompt;
        $this->logJourneyStep('PROMPT', "Length=" . strlen($prompt), [
            'length' => strlen($prompt),
            'preview' => $preview,
        ]);
    }

    /**
     * Log API request start.
     */
    protected function logJourneyApiStart(string $endpoint, string $method = 'POST'): void
    {
        $this->logJourneyStep('API_START', "{$method} {$endpoint}");
    }

    /**
     * Log API response.
     */
    protected function logJourneyApiResponse(int $status, int $durationMs): void
    {
        $this->logJourneyStep('API_RESPONSE', "Status={$status}, Duration={$durationMs}ms", [
            'status' => $status,
            'durationMs' => $durationMs,
        ]);
    }

    /**
     * Log token usage.
     */
    protected function logJourneyTokenUsage(array $usage): void
    {
        $inputTokens = $usage['inputTokens'] ?? 0;
        $outputTokens = $usage['outputTokens'] ?? 0;
        $cacheRead = $usage['cacheReadTokens'] ?? 0;
        $cost = $usage['cost'] ?? 0;

        $cacheHitRate = $inputTokens > 0 ? round(($cacheRead / $inputTokens) * 100, 1) : 0;

        $this->logJourneyStep('TOKEN_USAGE', "In={$inputTokens}, Out={$outputTokens}, CacheHit={$cacheHitRate}%, Cost=\${$cost}", [
            'inputTokens' => $inputTokens,
            'outputTokens' => $outputTokens,
            'cacheReadTokens' => $cacheRead,
            'cacheHitRate' => $cacheHitRate,
            'cost' => $cost,
        ]);
    }

    /**
     * Log file generation.
     */
    protected function logJourneyFiles(array $files): void
    {
        $this->logJourneyStep('FILES_GENERATED', "Generated " . count($files) . " files", [
            'count' => count($files),
            'files' => $files,
        ]);
    }

    /**
     * Log streaming event.
     */
    protected function logJourneyStreamEvent(string $eventType, int $contentLength = 0): void
    {
        $this->logJourneyStep('STREAM_EVENT', "Type={$eventType}, Length={$contentLength}", [
            'eventType' => $eventType,
            'contentLength' => $contentLength,
        ]);
    }

    /**
     * Log error.
     */
    protected function logJourneyError(string $phase, string $error, ?\Throwable $exception = null): void
    {
        $this->logJourneyStep('ERROR', "[{$phase}] {$error}", [
            'phase' => $phase,
            'error' => $error,
            'exception' => $exception?->getMessage(),
            'trace' => $exception ? substr($exception->getTraceAsString(), 0, 500) : null,
        ]);
    }

    /**
     * Complete the journey and log summary.
     */
    protected function completeJourney(bool $success, ?string $outputPreview = null): array
    {
        $totalDuration = $this->getJourneyElapsedMs();

        // Calculate phase durations
        $phaseDurations = [];
        for ($i = 0; $i < count($this->journeySteps) - 1; $i++) {
            $step = $this->journeySteps[$i];
            $nextStep = $this->journeySteps[$i + 1];
            $duration = $nextStep['elapsedMs'] - $step['elapsedMs'];

            $phase = $step['phase'];
            if (!isset($phaseDurations[$phase])) {
                $phaseDurations[$phase] = 0;
            }
            $phaseDurations[$phase] += $duration;
        }

        // Identify bottlenecks (phases > 1000ms)
        $bottlenecks = array_filter($phaseDurations, fn($d) => $d > 1000);
        arsort($bottlenecks);

        $summary = [
            'requestId' => $this->journeyRequestId,
            'route' => $this->journeyRoute,
            'sessionId' => $this->journeySessionId,
            'success' => $success,
            'totalDurationMs' => $totalDuration,
            'stepCount' => count($this->journeySteps),
            'phaseDurations' => $phaseDurations,
            'bottlenecks' => $bottlenecks,
            'outputPreview' => $outputPreview ? substr($outputPreview, 0, 200) : null,
        ];

        Log::channel('journey')->info(
            "[{$this->journeyRequestId}] [JOURNEY_COMPLETE] Success={$success}, TotalTime={$totalDuration}ms"
        );

        Log::channel('journey')->info(
            "[{$this->journeyRequestId}] [JOURNEY_SUMMARY] Phases: " . json_encode($phaseDurations)
        );

        if (!empty($bottlenecks)) {
            $bottleneckStr = implode(', ', array_map(
                fn($phase, $duration) => "{$phase}={$duration}ms",
                array_keys($bottlenecks),
                array_values($bottlenecks)
            ));
            Log::channel('journey')->warning(
                "[{$this->journeyRequestId}] [BOTTLENECK] Slow phases: {$bottleneckStr}"
            );
        }

        return $summary;
    }
}
