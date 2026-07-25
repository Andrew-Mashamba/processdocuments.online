<?php

namespace App\Jobs;

use Illuminate\Bus\Queueable;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Bus\Dispatchable;
use Illuminate\Queue\InteractsWithQueue;
use Illuminate\Queue\SerializesModels;
use Illuminate\Support\Facades\Cache;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

/**
 * Phase 5: Background job for async file generation.
 *
 * This job submits generation requests to the backend job API
 * and polls for completion. Results are stored in cache for
 * the frontend to retrieve.
 */
class GenerateFileJob implements ShouldQueue
{
    use Dispatchable, InteractsWithQueue, Queueable, SerializesModels;

    /**
     * The number of times the job may be attempted.
     */
    public int $tries = 1;

    /**
     * The maximum number of seconds the job can run.
     */
    public int $timeout = 600; // 10 minutes

    protected float $journeyStartTime;
    protected string $journeyRequestId;

    /**
     * Create a new job instance.
     */
    public function __construct(
        public string $localJobId,
        public string $sessionId,
        public string $prompt,
        public array $messages = [],
        public ?string $context = null,
        public ?string $frontendJourneyId = null
    ) {
        $this->journeyStartTime = microtime(true);
        $this->journeyRequestId = $frontendJourneyId ?? substr(uniqid(), -12);
    }

    /**
     * Get elapsed time in milliseconds.
     */
    protected function getElapsedMs(): int
    {
        return (int) ((microtime(true) - $this->journeyStartTime) * 1000);
    }

    /**
     * Log a journey step.
     */
    protected function logJourneyStep(string $phase, string $message, array $data = []): void
    {
        $elapsed = $this->getElapsedMs();
        $dataJson = !empty($data) ? json_encode($data) : '';

        Log::channel('journey')->info(
            "[{$this->journeyRequestId}] [JOB:{$this->localJobId}] [{$phase}] {$message} | {$elapsed}ms | {$dataJson}"
        );
    }

    /**
     * Execute the job.
     */
    public function handle(): void
    {
        $apiUrl = config('services.zima.url', 'http://localhost:5000');

        $this->logJourneyStep('JOB_START', 'Background job starting', [
            'session' => $this->sessionId,
            'promptLength' => strlen($this->prompt),
            'messageCount' => count($this->messages),
        ]);

        try {
            // Update local status
            $this->updateLocalStatus('processing', 10, 'Submitting to backend...');
            $this->logJourneyStep('BACKEND_SUBMIT', "Submitting to {$apiUrl}/api/jobs/submit");

            $submitStartTime = microtime(true);

            // Submit to backend job API
            $response = Http::timeout(30)->post("{$apiUrl}/api/jobs/submit", [
                'prompt' => $this->prompt,
                'sessionId' => $this->sessionId,
                'messages' => $this->messages,
                'context' => $this->context,
            ]);

            $submitDuration = (int) ((microtime(true) - $submitStartTime) * 1000);
            $this->logJourneyStep('BACKEND_RESPONSE', "Status: {$response->status()}, Duration: {$submitDuration}ms");

            if (!$response->successful()) {
                throw new \Exception("Backend submission failed: " . $response->body());
            }

            $submitData = $response->json();
            $backendJobId = $submitData['jobId'] ?? null;

            if (!$backendJobId) {
                throw new \Exception("No job ID returned from backend");
            }

            $this->logJourneyStep('BACKEND_JOB_CREATED', "Backend job: {$backendJobId}", [
                'backendJobId' => $backendJobId,
                'model' => $submitData['model'] ?? 'unknown',
                'complexity' => $submitData['complexity'] ?? 'unknown',
            ]);

            $this->updateLocalStatus('processing', 20, 'Backend processing...', [
                'backendJobId' => $backendJobId,
                'model' => $submitData['model'] ?? null,
                'complexity' => $submitData['complexity'] ?? null,
            ]);

            // Poll for completion
            $this->pollForCompletion($apiUrl, $backendJobId);

        } catch (\Exception $e) {
            $this->logJourneyStep('JOB_ERROR', $e->getMessage(), [
                'exception' => get_class($e),
                'trace' => substr($e->getTraceAsString(), 0, 500),
            ]);

            $this->updateLocalStatus('failed', 0, 'Job failed', [
                'error' => $e->getMessage(),
            ]);
        }
    }

    /**
     * Poll the backend for job completion.
     */
    protected function pollForCompletion(string $apiUrl, string $backendJobId): void
    {
        $maxPolls = 120; // 10 minutes at 5 second intervals
        $pollInterval = 5;
        $polls = 0;

        $this->logJourneyStep('POLLING_START', "Starting to poll for job {$backendJobId}", [
            'maxPolls' => $maxPolls,
            'pollInterval' => $pollInterval,
        ]);

        while ($polls < $maxPolls) {
            $polls++;

            try {
                $response = Http::timeout(10)->get("{$apiUrl}/api/jobs/{$backendJobId}");

                if (!$response->successful()) {
                    $this->logJourneyStep('POLL_RETRY', "Poll {$polls} failed with status {$response->status()}");
                    sleep($pollInterval);
                    continue;
                }

                $status = $response->json();
                $jobStatus = $status['status'] ?? 'unknown';
                $progress = $status['progress'] ?? 0;
                $step = $status['currentStep'] ?? 'Processing...';

                // Log every 5th poll to avoid spam
                if ($polls % 5 === 0) {
                    $this->logJourneyStep('POLL_STATUS', "Poll {$polls}: {$jobStatus} ({$progress}%)", [
                        'progress' => $progress,
                        'step' => $step,
                    ]);
                }

                // Update local progress
                $localProgress = 20 + (int)(($progress / 100) * 70); // Map 0-100 to 20-90
                $this->updateLocalStatus('processing', $localProgress, $step);

                if ($jobStatus === 'Completed') {
                    $result = $status['result'] ?? null;

                    $this->logJourneyStep('JOB_COMPLETE', "Job completed after {$polls} polls", [
                        'backendJobId' => $backendJobId,
                        'outputLength' => strlen($result['output'] ?? ''),
                        'filesGenerated' => count($result['generatedFiles'] ?? []),
                        'backendRequestId' => $result['requestId'] ?? null,
                        'backendDurationMs' => $result['journeyDurationMs'] ?? null,
                    ]);

                    $this->updateLocalStatus('completed', 100, 'Generation complete', [
                        'result' => $result,
                    ]);
                    return;
                }

                if ($jobStatus === 'Failed') {
                    $error = $status['error'] ?? 'Backend job failed';
                    $this->logJourneyStep('JOB_FAILED', $error);
                    throw new \Exception($error);
                }

                sleep($pollInterval);

            } catch (\Exception $e) {
                $this->logJourneyStep('POLL_ERROR', "Poll {$polls} error: {$e->getMessage()}");
                sleep($pollInterval);
            }
        }

        $this->logJourneyStep('JOB_TIMEOUT', "Timeout after {$polls} polls ({$maxPolls}x{$pollInterval}s)");
        throw new \Exception("Job timed out after polling for " . ($maxPolls * $pollInterval) . " seconds");
    }

    /**
     * Update local job status in cache.
     */
    protected function updateLocalStatus(string $status, int $progress, string $step, array $extra = []): void
    {
        $cacheKey = "zima_job:{$this->localJobId}";

        $data = array_merge([
            'jobId' => $this->localJobId,
            'sessionId' => $this->sessionId,
            'status' => $status,
            'progress' => $progress,
            'currentStep' => $step,
            'updatedAt' => now()->toIso8601String(),
        ], $extra);

        // Store for 1 hour
        Cache::put($cacheKey, $data, 3600);
    }

    /**
     * Handle a job failure.
     */
    public function failed(\Throwable $exception): void
    {
        $this->logJourneyStep('JOB_FAILED', 'Job failed with exception', [
            'exception' => get_class($exception),
            'message' => $exception->getMessage(),
            'trace' => substr($exception->getTraceAsString(), 0, 500),
        ]);

        $this->updateLocalStatus('failed', 0, 'Job failed', [
            'error' => $exception->getMessage(),
        ]);
    }
}
