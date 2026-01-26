<?php

namespace App\Livewire;

use App\Models\ChatSession;
use App\Models\ChatMessage;
use App\Models\SessionFile;
use App\Models\UsageLog;
use App\Jobs\GenerateFileJob;
use App\Traits\JourneyLogger;
use Livewire\Component;
use Livewire\WithFileUploads;
use Illuminate\Support\Facades\Cache;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Cookie;
use Illuminate\Support\Str;

class FileGenerator extends Component
{
    use WithFileUploads;
    use JourneyLogger;

    public string $prompt = '';
    public bool $isLoading = false;
    public ?string $error = null;
    public array $messages = [];
    public array $fileGroups = [];
    public array $expandedGroups = [];
    public array $sessions = [];
    public array $allGeneratedFiles = [];  // All files across all sessions
    public bool $showAllFiles = false;     // Toggle between session files and all files
    public ?string $currentSessionId = null;
    public bool $showSessionList = false;

    // Usage tracking display
    public ?array $lastUsage = null;
    public float $sessionCost = 0;
    public int $sessionTokens = 0;

    // Streaming state - Phase 1: streaming enabled by default for better UX
    public bool $useStreaming = true;
    public string $streamingContent = '';
    public bool $streamingMode = true; // Re-enabled with fixed streaming backend

    // Agent mode - Uses autonomous agent with tool execution
    public bool $agentMode = true; // Default to agent mode for better autonomous file generation

    // File upload state
    public $uploadedFile;
    public array $sessionFiles = [];
    public bool $isUploading = false;
    public ?string $uploadError = null;
    public array $allUploadedFiles = [];  // All uploaded files across all sessions
    public bool $showAllUploads = false;  // Toggle between session uploads and all uploads

    // Phase 5: Async job state
    public ?string $currentJobId = null;
    public int $jobProgress = 0;
    public ?string $jobStep = null;
    public bool $useAsyncMode = false; // Enable async mode for long-running tasks

    public string $apiUrl = 'http://localhost:5000';

    // File upload validation rules
    protected array $fileValidationRules = [
        'uploadedFile' => 'max:10240', // 10MB max
    ];

    /**
     * Automatically trigger upload when file is selected
     */
    public function updatedUploadedFile()
    {
        if ($this->uploadedFile) {
            $this->uploadFile();
        }
    }

    public function mount()
    {
        $this->loadSessions();
        $this->loadFiles();
        $this->loadAllGeneratedFiles();  // Load all files on mount

        // Try to load the most recent session (don't auto-create)
        if ($this->currentSessionId) {
            $this->loadSession($this->currentSessionId);
        } elseif (!empty($this->sessions)) {
            // Load the most recent session from the list
            $this->loadSession($this->sessions[0]['id']);
        }
        // If no sessions exist, stay in empty state - user can start chatting or click "New Chat"
    }

    /**
     * Get guest session IDs from cookie
     */
    protected function getGuestSessionIds(): array
    {
        $cookieValue = Cookie::get('guest_sessions', '');
        if (empty($cookieValue)) {
            return [];
        }
        return array_filter(explode(',', $cookieValue));
    }

    /**
     * Save guest session IDs to cookie
     */
    protected function saveGuestSessionIds(array $sessionIds): void
    {
        // Keep only the last 10 sessions
        $sessionIds = array_slice($sessionIds, -10);
        $cookieValue = implode(',', $sessionIds);

        // Set cookie for 30 days
        Cookie::queue('guest_sessions', $cookieValue, 60 * 24 * 30);
    }

    /**
     * Add a session ID to guest cookie
     */
    protected function addGuestSessionId(string $sessionId): void
    {
        $sessionIds = $this->getGuestSessionIds();
        if (!in_array($sessionId, $sessionIds)) {
            $sessionIds[] = $sessionId;
            $this->saveGuestSessionIds($sessionIds);
        }
    }

    /**
     * Remove a session ID from guest cookie
     */
    protected function removeGuestSessionId(string $sessionId): void
    {
        $sessionIds = $this->getGuestSessionIds();
        $sessionIds = array_filter($sessionIds, fn($id) => $id !== $sessionId);
        $this->saveGuestSessionIds(array_values($sessionIds));
    }

    public function loadSessions()
    {
        $query = ChatSession::orderByDesc('updated_at')->take(20);

        if (Auth::check()) {
            // For authenticated users, show their sessions
            $query->where('user_id', Auth::id());
        } else {
            // For guests, only show sessions stored in their cookie
            $guestSessionIds = $this->getGuestSessionIds();
            if (!empty($guestSessionIds)) {
                $query->whereIn('id', $guestSessionIds)->whereNull('user_id');
            } else {
                // No guest sessions stored, return empty
                $this->sessions = [];
                return;
            }
        }

        $this->sessions = $query->get()
            ->map(fn($s) => [
                'id' => $s->id,
                'title' => $s->title,
                'message_count' => $s->message_count,
                'cost' => $s->cost,
                'updated_at' => $s->updated_at->diffForHumans(),
            ])
            ->toArray();
    }

    public function loadSession(string $sessionId)
    {
        $session = ChatSession::find($sessionId);
        if (!$session) {
            $this->createNewSession();
            return;
        }

        // Check ownership
        if (Auth::check()) {
            // Authenticated user can only access their own sessions
            if ($session->user_id && $session->user_id !== Auth::id()) {
                $this->createNewSession();
                return;
            }
        } else {
            // Guest can only access sessions in their cookie
            $guestSessionIds = $this->getGuestSessionIds();
            if (!in_array($sessionId, $guestSessionIds)) {
                $this->createNewSession();
                return;
            }
        }

        $this->currentSessionId = $session->id;
        $this->sessionCost = (float) $session->cost;
        $this->sessionTokens = $session->prompt_tokens + $session->completion_tokens;

        $this->messages = $session->messages->map(fn($m) => [
            'id' => $m->id,
            'role' => $m->role,
            'content' => $m->getTextContent(),
            'timestamp' => $m->created_at->format('H:i'),
            'files' => [],
            'isError' => false,
        ])->toArray();

        // Load session files
        $this->loadSessionFiles();

        Log::info("Loaded session {$sessionId} with " . count($this->messages) . " messages");
    }

    public function createNewSession()
    {
        $session = ChatSession::create([
            'user_id' => Auth::id(),
            'title' => 'New Chat',
        ]);

        $this->currentSessionId = $session->id;
        $this->messages = [];
        $this->sessionCost = 0;
        $this->sessionTokens = 0;
        $this->lastUsage = null;
        $this->sessionFiles = [];
        $this->uploadError = null;

        // For guests, save session ID to cookie
        if (!Auth::check()) {
            $this->addGuestSessionId($session->id);
        }

        $this->loadSessions();

        Log::info("Created new session: {$session->id}" . (!Auth::check() ? ' (guest)' : ''));
    }

    public function switchSession(string $sessionId)
    {
        $this->loadSession($sessionId);
        $this->showSessionList = false;
    }

    public function deleteSession(string $sessionId)
    {
        $session = ChatSession::find($sessionId);
        if (!$session) return;

        // Check permission
        if (Auth::check()) {
            // Authenticated user can only delete their own sessions
            if ($session->user_id !== Auth::id()) return;
        } else {
            // Guest can only delete sessions in their cookie
            $guestSessionIds = $this->getGuestSessionIds();
            if (!in_array($sessionId, $guestSessionIds)) return;

            // Remove from guest cookie
            $this->removeGuestSessionId($sessionId);
        }

        $session->delete();
        $this->loadSessions();

        if ($sessionId === $this->currentSessionId) {
            $this->createNewSession();
        }
    }

    public function toggleSessionList()
    {
        $this->showSessionList = !$this->showSessionList;
    }

    /**
     * Load files uploaded to the current session
     */
    public function loadSessionFiles()
    {
        if (!$this->currentSessionId) {
            $this->sessionFiles = [];
            return;
        }

        try {
            $response = Http::get("{$this->apiUrl}/api/files/session/{$this->currentSessionId}");
            if ($response->successful()) {
                $this->sessionFiles = $response->json()['files'] ?? [];
            }
        } catch (\Exception $e) {
            Log::error("Failed to load session files: " . $e->getMessage());
            $this->sessionFiles = [];
        }
    }

    /**
     * Upload a file to the current session
     */
    public function uploadFile()
    {
        if (!$this->uploadedFile) {
            return;
        }

        // Create session on-demand if none exists
        if (!$this->currentSessionId) {
            $this->createNewSession();
        }

        $this->isUploading = true;
        $this->uploadError = null;

        try {
            $response = Http::attach(
                'file',
                file_get_contents($this->uploadedFile->getRealPath()),
                $this->uploadedFile->getClientOriginalName()
            )->post("{$this->apiUrl}/api/files/upload/{$this->currentSessionId}");

            if ($response->successful()) {
                $data = $response->json();

                // Also save to local database for tracking
                SessionFile::create([
                    'session_id' => $this->currentSessionId,
                    'user_id' => Auth::id(),
                    'filename' => $data['file']['name'] ?? $this->uploadedFile->getClientOriginalName(),
                    'original_filename' => $this->uploadedFile->getClientOriginalName(),
                    'mime_type' => $this->uploadedFile->getMimeType(),
                    'size' => $this->uploadedFile->getSize(),
                    'storage_path' => "uploaded_files/{$this->currentSessionId}/" . ($data['file']['name'] ?? $this->uploadedFile->getClientOriginalName()),
                    'include_in_context' => true,
                ]);

                $this->loadSessionFiles();
                Log::info("File uploaded: " . $this->uploadedFile->getClientOriginalName());
            } else {
                $this->uploadError = $response->json()['error'] ?? 'Upload failed';
            }
        } catch (\Exception $e) {
            $this->uploadError = 'Error uploading file: ' . $e->getMessage();
            Log::error("File upload error: " . $e->getMessage());
        }

        $this->uploadedFile = null;
        $this->isUploading = false;
    }

    /**
     * Delete a file from the current session
     */
    public function deleteSessionFile(string $filename)
    {
        if (!$this->currentSessionId) {
            return;
        }

        try {
            $response = Http::delete("{$this->apiUrl}/api/files/session/{$this->currentSessionId}/{$filename}");
            if ($response->successful()) {
                // Also delete from local database
                SessionFile::where('session_id', $this->currentSessionId)
                    ->where('filename', $filename)
                    ->delete();

                $this->loadSessionFiles();
                Log::info("Session file deleted: {$filename}");
            } else {
                $this->uploadError = 'Failed to delete file';
            }
        } catch (\Exception $e) {
            $this->uploadError = 'Error deleting file: ' . $e->getMessage();
        }
    }

    /**
     * Toggle whether a file should be included in AI context
     */
    public function toggleFileInContext(string $filename)
    {
        $file = SessionFile::where('session_id', $this->currentSessionId)
            ->where('filename', $filename)
            ->first();

        if ($file) {
            $file->update(['include_in_context' => !$file->include_in_context]);
            $this->loadSessionFiles();
        }
    }

    public function generate()
    {
        if (empty(trim($this->prompt))) {
            return;
        }

        // Check user credits if authenticated
        if (Auth::check()) {
            $user = Auth::user();
            if (!$user->hasCredits()) {
                $this->error = 'You have exceeded your usage limit. Please upgrade your plan.';
                return;
            }
        }

        // Phase 1 Optimization: Use streaming by default for better UX
        // Streaming shows progress immediately instead of blocking for 5-10 minutes
        if ($this->streamingMode) {
            $this->generateWithStreaming();
            return;
        }

        // Start journey logging for non-streaming route
        $this->startJourney('FRONTEND_GENERATE', $this->currentSessionId);
        $this->logJourneyRouteDecision('NON_STREAMING', 'streamingMode=false');

        set_time_limit(0);
        ini_set('max_execution_time', '0');

        $userPrompt = $this->prompt;
        $this->logJourneyPrompt($userPrompt);

        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->logJourneyStep('SESSION_CREATE', 'Creating new session');
            $this->createNewSession();
            $session = ChatSession::find($this->currentSessionId);
        }

        $userMessage = $session->addMessage('user', $userPrompt);
        $this->logJourneyStep('USER_MESSAGE_SAVED', "Message ID: {$userMessage->id}");

        $this->messages[] = [
            'id' => $userMessage->id,
            'role' => 'user',
            'content' => $userPrompt,
            'timestamp' => now()->format('H:i'),
        ];

        $this->prompt = '';
        $this->isLoading = true;
        $this->error = null;

        try {
            $session->refresh();

            // Get structured messages for prompt caching (50-90% cost reduction)
            $cachedMessages = $session->getMessagesForCaching();
            $this->logJourneyContextTier('CACHED', count($session->messages), count($cachedMessages));

            $this->logJourneyApiStart("{$this->apiUrl}/api/generate");
            $apiStartTime = microtime(true);

            $response = Http::timeout(300)->post("{$this->apiUrl}/api/generate", [
                'prompt' => $userPrompt,
                'messages' => $cachedMessages, // Structured messages with cache_control support
                'sessionId' => $this->currentSessionId,
            ]);

            $apiDuration = (int) ((microtime(true) - $apiStartTime) * 1000);
            $this->logJourneyApiResponse($response->status(), $apiDuration);

            if ($response->successful()) {
                $data = $response->json();
                $this->logJourneyStep('RESPONSE_PARSE', 'Parsing successful response');

                // Track usage
                $usage = $data['usage'] ?? null;
                $model = $data['model'] ?? null;
                $backendRequestId = $data['requestId'] ?? null;

                // Phase 2: Log multi-model routing info
                $taskComplexity = $data['taskComplexity'] ?? 'Standard';
                $costInfo = $data['costInfo'] ?? 'Balanced';
                $contextTierUsed = $data['contextTierUsed'] ?? 'Unknown';

                $this->logJourneyModelSelection($model ?? 'unknown', $taskComplexity, $costInfo);
                $this->logJourneyStep('BACKEND_REQUEST_ID', "Backend: {$backendRequestId}", [
                    'backendRequestId' => $backendRequestId,
                    'contextTierUsed' => $contextTierUsed,
                    'journeyDurationMs' => $data['journeyDurationMs'] ?? null,
                ]);

                if ($usage) {
                    $this->lastUsage = $usage;
                    // Add complexity info to usage for display
                    $this->lastUsage['taskComplexity'] = $taskComplexity;
                    $this->lastUsage['costInfo'] = $costInfo;

                    $this->sessionCost += (float) ($usage['cost'] ?? 0);
                    $this->sessionTokens += ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0);

                    $this->logJourneyTokenUsage($usage);

                    // Update session with usage
                    $session->addUsage(
                        $usage['inputTokens'] ?? 0,
                        $usage['outputTokens'] ?? 0,
                        $usage['cost'] ?? 0,
                        $model
                    );

                    // Log usage for billing
                    if (Auth::check()) {
                        UsageLog::logUsage(
                            Auth::id(),
                            $this->currentSessionId,
                            null,
                            $usage,
                            'generate'
                        );

                        // Update user's total usage
                        Auth::user()->addUsage(
                            $usage['cost'] ?? 0,
                            ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0)
                        );
                    }
                }

                $assistantContent = $data['output'] ?? 'Generation completed';
                $generatedFiles = $data['generatedFiles'] ?? [];

                if (!empty($generatedFiles)) {
                    $this->logJourneyFiles($generatedFiles);
                }

                if (!$data['success']) {
                    $this->error = $data['errors'] ?? $data['message'] ?? 'Generation failed';
                    $this->logJourneyError('GENERATION', $this->error);
                    $errorContent = 'Sorry, there was an error: ' . $this->error;
                    $assistantMessage = $session->addMessage('assistant', $errorContent);

                    $this->messages[] = [
                        'id' => $assistantMessage->id,
                        'role' => 'assistant',
                        'content' => $errorContent,
                        'timestamp' => now()->format('H:i'),
                        'files' => [],
                        'isError' => true,
                    ];

                    $this->completeJourney(false, $errorContent);
                } else {
                    $assistantMessage = $session->addMessage('assistant', $assistantContent, $model);
                    $this->logJourneyStep('ASSISTANT_MESSAGE_SAVED', "Message ID: {$assistantMessage->id}");

                    $this->messages[] = [
                        'id' => $assistantMessage->id,
                        'role' => 'assistant',
                        'content' => $assistantContent,
                        'timestamp' => now()->format('H:i'),
                        'files' => $generatedFiles,
                        'isError' => false,
                    ];

                    // Generate smart title for first message
                    if ($session->message_count <= 2 && $session->title === 'New Chat') {
                        $this->logJourneyStep('TITLE_GEN_TRIGGER', 'First message - generating smart title');
                        $this->generateSmartTitle($session, $userPrompt);
                    }

                    // Check if summarization is needed
                    if ($session->needsSummarization(20)) {
                        $this->logJourneyStep('SUMMARIZE_TRIGGER', 'Message count > 20 - summarizing session');
                        $this->summarizeSession($session);
                    }

                    $this->completeJourney(true, substr($assistantContent, 0, 200));
                }

                $this->loadFiles();
            } else {
                $this->logJourneyError('API', "Request failed with status: {$response->status()}");
                $this->handleError($session, 'API request failed: ' . $response->status());
                $this->completeJourney(false, 'API error');
            }
        } catch (\Exception $e) {
            $this->logJourneyError('EXCEPTION', $e->getMessage(), $e);
            $this->handleError($session, $e->getMessage());
            $this->completeJourney(false, $e->getMessage());
        }

        $this->isLoading = false;
        $this->loadSessions();
    }

    protected function handleError(ChatSession $session, string $message)
    {
        $this->error = $message;
        $errorContent = 'Sorry, an error occurred: ' . $message;
        $assistantMessage = $session->addMessage('assistant', $errorContent);

        $this->messages[] = [
            'id' => $assistantMessage->id,
            'role' => 'assistant',
            'content' => $errorContent,
            'timestamp' => now()->format('H:i'),
            'files' => [],
            'isError' => true,
        ];
    }

    /**
     * Phase 1 Optimization: Streaming generation for better UX.
     * Shows progress immediately instead of blocking for 5-10 minutes.
     * Dispatches 'startStreaming' event to JavaScript StreamingHandler.
     */
    protected function generateWithStreaming()
    {
        // Start journey logging for streaming route
        $this->startJourney('FRONTEND_STREAMING', $this->currentSessionId);
        $this->logJourneyRouteDecision('STREAMING', 'streamingMode=true (default)');

        $userPrompt = $this->prompt;
        $this->logJourneyPrompt($userPrompt);

        // Create session on-demand if needed
        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->logJourneyStep('SESSION_CREATE', 'Creating new session');
            $this->createNewSession();
            $session = ChatSession::find($this->currentSessionId);
        }

        // Add user message to database and UI
        $userMessage = $session->addMessage('user', $userPrompt);
        $this->logJourneyStep('USER_MESSAGE_SAVED', "Message ID: {$userMessage->id}");

        $this->messages[] = [
            'id' => $userMessage->id,
            'role' => 'user',
            'content' => $userPrompt,
            'timestamp' => now()->format('H:i'),
        ];

        // Clear prompt and set loading state
        $this->prompt = '';
        $this->isLoading = true;
        $this->streamingContent = '';
        $this->error = null;

        // Get cached messages for the streaming request
        $session->refresh();
        $cachedMessages = $session->getMessagesForCaching();
        $this->logJourneyContextTier('CACHED', count($session->messages), count($cachedMessages));

        $this->logJourneyStep('DISPATCH_STREAMING', 'Dispatching to JavaScript StreamingHandler', [
            'streamUrl' => $this->getStreamingUrl(),
            'messageCount' => count($cachedMessages),
        ]);

        // Dispatch event to JavaScript StreamingHandler
        // Note: Journey will be completed in completeStreaming() method
        $requestData = [
            'sessionId' => $this->currentSessionId,
            'streamUrl' => $this->getStreamingUrl(),
            'journeyRequestId' => $this->journeyRequestId, // Pass journey ID to JavaScript
            'agentMode' => $this->agentMode,
        ];

        // Agent mode uses 'goal' property, standard mode uses 'prompt' and 'messages'
        if ($this->agentMode) {
            $requestData['goal'] = $userPrompt;
        } else {
            $requestData['prompt'] = $userPrompt;
            $requestData['messages'] = $cachedMessages;
        }

        $this->dispatch('startStreaming', $requestData);
    }

    protected function generateSmartTitle(ChatSession $session, string $firstPrompt)
    {
        try {
            $response = Http::timeout(30)->post("{$this->apiUrl}/api/generate/title", [
                'message' => $firstPrompt,
            ]);

            if ($response->successful()) {
                $title = $response->json()['title'] ?? substr($firstPrompt, 0, 50);
                $session->update(['title' => $title]);
                $this->loadSessions();
                Log::info("Generated smart title: {$title}");
            }
        } catch (\Exception $e) {
            // Fallback to truncated prompt
            $title = substr($firstPrompt, 0, 50);
            if (strlen($firstPrompt) > 50) $title .= '...';
            $session->update(['title' => $title]);
            $this->loadSessions();
        }
    }

    /**
     * Summarize the session to prevent hitting context window limits.
     * Creates a summary of the conversation and marks it as the new starting point.
     * Future requests will start from this summary, skipping old messages.
     */
    protected function summarizeSession(ChatSession $session)
    {
        try {
            // Get all messages since last summary for summarization
            $conversationHistory = $session->getConversationContext();
            $activeMessageCount = $session->getActiveMessageCount();

            Log::info("Summarizing session {$session->id} with {$activeMessageCount} active messages");

            $response = Http::timeout(60)->post("{$this->apiUrl}/api/generate/summarize", [
                'conversationHistory' => $conversationHistory,
            ]);

            if ($response->successful()) {
                $summary = $response->json()['summary'] ?? '';

                // Create a summary message as an assistant message (so it shows in chat)
                $summaryContent = "📋 **Conversation Summary**\n\n" . $summary . "\n\n---\n*Previous messages have been summarized to optimize context.*";
                $summaryMessage = $session->addMessage('assistant', $summaryContent);
                $session->setSummary($summaryMessage->id);

                // Add summary to messages array for display
                $this->messages[] = [
                    'id' => $summaryMessage->id,
                    'role' => 'assistant',
                    'content' => $summaryContent,
                    'timestamp' => now()->format('H:i'),
                    'files' => [],
                    'isError' => false,
                    'isSummary' => true,
                ];

                Log::info("Session {$session->id} summarized successfully. New context starts from message {$summaryMessage->id}");
            }
        } catch (\Exception $e) {
            Log::error("Summarization failed: " . $e->getMessage());
        }
    }

    public function loadFiles()
    {
        // Load files for current session only
        if (!$this->currentSessionId) {
            $this->fileGroups = [];
            return;
        }

        try {
            $response = Http::get("{$this->apiUrl}/api/files/generated/{$this->currentSessionId}", ['grouped' => 'true']);
            if ($response->successful()) {
                $this->fileGroups = $response->json()['files'] ?? [];
            } else {
                $this->fileGroups = [];
            }
        } catch (\Exception $e) {
            $this->fileGroups = [];
        }
    }

    /**
     * Load ALL generated files across all sessions
     */
    public function loadAllGeneratedFiles()
    {
        try {
            $response = Http::get("{$this->apiUrl}/api/files/generated/by-session");
            if ($response->successful()) {
                $this->allGeneratedFiles = $response->json()['sessions'] ?? [];
            } else {
                $this->allGeneratedFiles = [];
            }
        } catch (\Exception $e) {
            Log::error("Failed to load all generated files: " . $e->getMessage());
            $this->allGeneratedFiles = [];
        }
    }

    /**
     * Toggle between session files and all files view
     */
    public function toggleAllFilesView()
    {
        $this->showAllFiles = !$this->showAllFiles;
        if ($this->showAllFiles) {
            $this->loadAllGeneratedFiles();
        }
    }

    /**
     * Refresh files - called by polling, handles both views
     */
    public function refreshFiles()
    {
        $this->loadFiles();  // Always refresh current session files
        $this->loadSessionFiles();  // Also refresh uploaded files
        if ($this->showAllFiles) {
            $this->loadAllGeneratedFiles();  // Also refresh all files if in that view
        }
        if ($this->showAllUploads) {
            $this->loadAllUploadedFiles();  // Also refresh all uploads if in that view
        }
    }

    /**
     * Load all uploaded files from all sessions
     */
    public function loadAllUploadedFiles()
    {
        try {
            $response = Http::get("{$this->apiUrl}/api/files/uploaded/by-session");
            if ($response->successful()) {
                $this->allUploadedFiles = $response->json()['sessions'] ?? [];
            } else {
                $this->allUploadedFiles = [];
            }
        } catch (\Exception $e) {
            Log::error("Failed to load all uploaded files: " . $e->getMessage());
            $this->allUploadedFiles = [];
        }
    }

    /**
     * Toggle between session uploads and all uploads view
     */
    public function toggleAllUploadsView()
    {
        $this->showAllUploads = !$this->showAllUploads;
        if ($this->showAllUploads) {
            $this->loadAllUploadedFiles();
        }
    }

    public function toggleGroup(string $baseName)
    {
        if (in_array($baseName, $this->expandedGroups)) {
            $this->expandedGroups = array_values(array_filter($this->expandedGroups, fn($g) => $g !== $baseName));
        } else {
            $this->expandedGroups[] = $baseName;
        }
    }

    public function deleteFile(string $filename)
    {
        if (!$this->currentSessionId) {
            $this->error = 'No active session';
            return;
        }

        try {
            $response = Http::delete("{$this->apiUrl}/api/files/generated/{$this->currentSessionId}/{$filename}");
            if ($response->successful()) {
                $this->loadFiles();
            } else {
                $this->error = 'Failed to delete file';
            }
        } catch (\Exception $e) {
            $this->error = 'Error deleting file: ' . $e->getMessage();
        }
    }

    public function deleteAllVersions(string $baseName)
    {
        if (!$this->currentSessionId) {
            $this->error = 'No active session';
            return;
        }

        try {
            foreach ($this->fileGroups as $group) {
                if ($group['baseName'] === $baseName) {
                    foreach ($group['versions'] as $version) {
                        Http::delete("{$this->apiUrl}/api/files/generated/{$this->currentSessionId}/{$version['fileName']}");
                    }
                    break;
                }
            }
            $this->loadFiles();
        } catch (\Exception $e) {
            $this->error = 'Error deleting files: ' . $e->getMessage();
        }
    }

    public function clearChat()
    {
        if ($this->currentSessionId) {
            $session = ChatSession::find($this->currentSessionId);
            if ($session) {
                $session->messages()->delete();
                $session->update([
                    'message_count' => 0,
                    'prompt_tokens' => 0,
                    'completion_tokens' => 0,
                    'cost' => 0,
                    'summary_message_id' => null,
                ]);
            }
        }
        $this->messages = [];
        $this->error = null;
        $this->sessionCost = 0;
        $this->sessionTokens = 0;
        $this->lastUsage = null;
    }

    /**
     * Toggle agent mode
     */
    public function toggleAgentMode()
    {
        $this->agentMode = !$this->agentMode;
        Log::info("Agent mode toggled: " . ($this->agentMode ? 'ON' : 'OFF'));
    }

    /**
     * Get the streaming URL for JavaScript EventSource
     */
    public function getStreamingUrl(): string
    {
        // Use full backend URL to avoid proxy issues
        // Return agent URL when in agent mode, otherwise standard generate
        if ($this->agentMode) {
            return "{$this->apiUrl}/api/agent/stream";
        }
        return "{$this->apiUrl}/api/generate/stream";
    }

    /**
     * Prepare data for streaming request (called from JavaScript)
     */
    public function prepareStreamingRequest(): array
    {
        if (empty(trim($this->prompt))) {
            return ['error' => 'Prompt is required'];
        }

        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->createNewSession();
            $session = ChatSession::find($this->currentSessionId);
        }

        // Add user message
        $userMessage = $session->addMessage('user', $this->prompt);
        $userPrompt = $this->prompt;

        $this->messages[] = [
            'id' => $userMessage->id,
            'role' => 'user',
            'content' => $userPrompt,
            'timestamp' => now()->format('H:i'),
        ];

        $this->prompt = '';
        $this->isLoading = true;
        $this->streamingContent = '';

        // Get cached messages for the request
        $session->refresh();
        $cachedMessages = $session->getMessagesForCaching();

        return [
            'prompt' => $userPrompt,
            'messages' => $cachedMessages,
            'sessionId' => $this->currentSessionId,
            'streamUrl' => $this->getStreamingUrl(),
        ];
    }

    /**
     * Handle streaming completion (called from JavaScript)
     */
    public function completeStreaming(string $content, array $usage = [], array $files = [], ?string $model = null, ?string $backendRequestId = null, ?int $backendDurationMs = null)
    {
        // Resume journey logging
        $this->logJourneyStep('STREAMING_COMPLETE', 'Received streaming completion from JavaScript', [
            'contentLength' => strlen($content),
            'filesCount' => count($files),
            'backendRequestId' => $backendRequestId,
            'backendDurationMs' => $backendDurationMs,
        ]);

        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->logJourneyError('SESSION', 'Session not found');
            $this->completeJourney(false, 'Session not found');
            return;
        }

        // Add assistant message
        $assistantMessage = $session->addMessage('assistant', $content, $model);
        $this->logJourneyStep('ASSISTANT_MESSAGE_SAVED', "Message ID: {$assistantMessage->id}");

        $this->messages[] = [
            'id' => $assistantMessage->id,
            'role' => 'assistant',
            'content' => $content,
            'timestamp' => now()->format('H:i'),
            'files' => $files,
            'isError' => false,
        ];

        if (!empty($files)) {
            $this->logJourneyFiles($files);
        }

        // Track usage
        if (!empty($usage)) {
            $this->lastUsage = $usage;
            $this->sessionCost += (float) ($usage['cost'] ?? 0);
            $this->sessionTokens += ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0);

            $this->logJourneyTokenUsage($usage);
            $this->logJourneyModelSelection($model ?? 'unknown', $usage['taskComplexity'] ?? 'Standard', $usage['costInfo'] ?? 'Balanced');

            $session->addUsage(
                $usage['inputTokens'] ?? 0,
                $usage['outputTokens'] ?? 0,
                $usage['cost'] ?? 0,
                $model
            );

            // Log usage for billing
            if (Auth::check()) {
                UsageLog::logUsage(
                    Auth::id(),
                    $this->currentSessionId,
                    null,
                    $usage,
                    'generate_stream'
                );

                Auth::user()->addUsage(
                    $usage['cost'] ?? 0,
                    ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0)
                );
            }
        }

        // Generate smart title for first message
        if ($session->message_count <= 2 && $session->title === 'New Chat') {
            $this->logJourneyStep('TITLE_GEN_TRIGGER', 'First message - generating smart title');
            $this->generateSmartTitle($session, $content);
        }

        // Check if summarization is needed
        if ($session->needsSummarization(20)) {
            $this->logJourneyStep('SUMMARIZE_TRIGGER', 'Message count > 20 - summarizing session');
            $this->summarizeSession($session);
        }

        $this->completeJourney(true, substr($content, 0, 200));

        $this->isLoading = false;
        $this->streamingContent = '';
        $this->loadFiles();
        $this->loadSessions();
    }

    /**
     * Update streaming content (called from JavaScript)
     */
    public function updateStreamingContent(string $content)
    {
        $this->streamingContent = $content;
    }

    /**
     * Phase 5: Submit generation as async background job.
     * Good for long-running multi-file requests.
     */
    public function generateAsync()
    {
        if (empty(trim($this->prompt))) {
            return;
        }

        // Check user credits if authenticated
        if (Auth::check()) {
            $user = Auth::user();
            if (!$user->hasCredits()) {
                $this->error = 'You have exceeded your usage limit. Please upgrade your plan.';
                return;
            }
        }

        // Start journey logging for async route
        $this->startJourney('FRONTEND_ASYNC', $this->currentSessionId);
        $this->logJourneyRouteDecision('ASYNC_JOB', 'Background job requested');

        $userPrompt = $this->prompt;
        $this->logJourneyPrompt($userPrompt);

        // Create session on-demand if needed
        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->logJourneyStep('SESSION_CREATE', 'Creating new session');
            $this->createNewSession();
            $session = ChatSession::find($this->currentSessionId);
        }

        // Add user message
        $userMessage = $session->addMessage('user', $userPrompt);
        $this->logJourneyStep('USER_MESSAGE_SAVED', "Message ID: {$userMessage->id}");

        $this->messages[] = [
            'id' => $userMessage->id,
            'role' => 'user',
            'content' => $userPrompt,
            'timestamp' => now()->format('H:i'),
        ];

        $this->prompt = '';
        $this->isLoading = true;
        $this->error = null;

        // Get cached messages
        $session->refresh();
        $cachedMessages = $session->getMessagesForCaching();
        $this->logJourneyContextTier('CACHED', count($session->messages), count($cachedMessages));

        // Generate local job ID
        $localJobId = Str::uuid()->toString();
        $this->currentJobId = $localJobId;
        $this->jobProgress = 0;
        $this->jobStep = 'Submitting job...';

        $this->logJourneyStep('JOB_DISPATCH', "Dispatching job: {$localJobId}");

        // Dispatch to queue
        GenerateFileJob::dispatch(
            $localJobId,
            $this->currentSessionId,
            $userPrompt,
            $cachedMessages,
            null, // context
            $this->journeyRequestId // pass journey ID to job
        );

        $this->logJourneyStep('JOB_DISPATCHED', 'Job queued successfully', [
            'jobId' => $localJobId,
        ]);

        // Start polling for status updates
        // Note: Journey will be completed in handleJobCompletion()
        $this->dispatch('startJobPolling', [
            'jobId' => $localJobId,
            'journeyRequestId' => $this->journeyRequestId,
        ]);
    }

    /**
     * Phase 5: Check async job status.
     * Called by JavaScript polling.
     */
    public function checkJobStatus(string $jobId): array
    {
        $cacheKey = "zima_job:{$jobId}";
        $status = Cache::get($cacheKey);

        if (!$status) {
            return [
                'found' => false,
                'error' => 'Job not found',
            ];
        }

        // Update local state
        $this->jobProgress = $status['progress'] ?? 0;
        $this->jobStep = $status['currentStep'] ?? 'Processing...';

        // If completed, handle the result
        if ($status['status'] === 'completed' && isset($status['result'])) {
            $this->handleJobCompletion($status['result']);
            return [
                'found' => true,
                'status' => 'completed',
                'result' => $status['result'],
            ];
        }

        // If failed, handle the error
        if ($status['status'] === 'failed') {
            $this->error = $status['error'] ?? 'Job failed';
            $this->isLoading = false;
            $this->currentJobId = null;
            return [
                'found' => true,
                'status' => 'failed',
                'error' => $status['error'] ?? 'Unknown error',
            ];
        }

        return [
            'found' => true,
            'status' => $status['status'],
            'progress' => $status['progress'] ?? 0,
            'step' => $status['currentStep'] ?? 'Processing...',
        ];
    }

    /**
     * Phase 5: Handle completed async job.
     */
    protected function handleJobCompletion(array $result)
    {
        $this->logJourneyStep('JOB_COMPLETE', 'Processing job completion', [
            'success' => $result['success'] ?? false,
            'backendRequestId' => $result['requestId'] ?? null,
            'backendDurationMs' => $result['journeyDurationMs'] ?? null,
        ]);

        $session = ChatSession::find($this->currentSessionId);
        if (!$session) {
            $this->logJourneyError('SESSION', 'Session not found');
            $this->completeJourney(false, 'Session not found');
            return;
        }

        $content = $result['output'] ?? 'Generation completed';
        $files = $result['generatedFiles'] ?? [];
        $usage = $result['usage'] ?? null;
        $model = $result['model'] ?? null;

        // Add assistant message
        $assistantMessage = $session->addMessage('assistant', $content, $model);
        $this->logJourneyStep('ASSISTANT_MESSAGE_SAVED', "Message ID: {$assistantMessage->id}");

        $this->messages[] = [
            'id' => $assistantMessage->id,
            'role' => 'assistant',
            'content' => $content,
            'timestamp' => now()->format('H:i'),
            'files' => $files,
            'isError' => !($result['success'] ?? true),
        ];

        if (!empty($files)) {
            $this->logJourneyFiles($files);
        }

        // Track usage
        if ($usage) {
            $this->lastUsage = $usage;
            $this->sessionCost += (float) ($usage['cost'] ?? 0);
            $this->sessionTokens += ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0);

            $this->logJourneyTokenUsage($usage);

            $session->addUsage(
                $usage['inputTokens'] ?? 0,
                $usage['outputTokens'] ?? 0,
                $usage['cost'] ?? 0,
                $model
            );

            if (Auth::check()) {
                UsageLog::logUsage(
                    Auth::id(),
                    $this->currentSessionId,
                    null,
                    $usage,
                    'generate_async'
                );

                Auth::user()->addUsage(
                    $usage['cost'] ?? 0,
                    ($usage['inputTokens'] ?? 0) + ($usage['outputTokens'] ?? 0)
                );
            }
        }

        // Generate smart title if needed
        if ($session->message_count <= 2 && $session->title === 'New Chat') {
            $this->logJourneyStep('TITLE_GEN_TRIGGER', 'First message - generating smart title');
            $firstPrompt = $session->messages()->where('role', 'user')->first()?->getTextContent() ?? '';
            $this->generateSmartTitle($session, $firstPrompt);
        }

        $this->completeJourney($result['success'] ?? true, substr($content, 0, 200));

        // Reset job state
        $this->isLoading = false;
        $this->currentJobId = null;
        $this->jobProgress = 0;
        $this->jobStep = null;
        $this->loadFiles();
        $this->loadSessions();
    }

    /**
     * Phase 5: Cancel current async job.
     */
    public function cancelJob()
    {
        if (!$this->currentJobId) return;

        Log::info("Cancelling job: {$this->currentJobId}");

        // Mark as cancelled in cache
        $cacheKey = "zima_job:{$this->currentJobId}";
        Cache::put($cacheKey, [
            'status' => 'cancelled',
            'progress' => 0,
            'currentStep' => 'Cancelled by user',
        ], 3600);

        // Try to cancel on backend
        try {
            Http::delete("{$this->apiUrl}/api/jobs/{$this->currentJobId}");
        } catch (\Exception $e) {
            // Ignore errors
        }

        $this->isLoading = false;
        $this->currentJobId = null;
        $this->jobProgress = 0;
        $this->jobStep = null;
    }

    /**
     * Invoke a tool directly without AI processing
     * Uses the new /api/tools/{tool_name} endpoint
     */
    public function invokeTool(string $toolName, array $arguments)
    {
        $this->isLoading = true;
        $this->error = null;

        try {
            $response = Http::timeout(120)->post("{$this->apiUrl}/api/tools/{$toolName}", $arguments);

            if ($response->successful()) {
                $data = $response->json();

                // Add result to messages
                $resultMessage = "Tool `{$toolName}` executed successfully.\n\n";
                if (isset($data['result'])) {
                    $resultMessage .= $data['result'];
                } else {
                    $resultMessage .= json_encode($data, JSON_PRETTY_PRINT);
                }

                $this->messages[] = [
                    'id' => uniqid(),
                    'role' => 'assistant',
                    'content' => $resultMessage,
                    'timestamp' => now()->format('H:i'),
                    'isError' => false,
                ];

                // Refresh file list
                $this->loadFiles();

                return $data;
            } else {
                throw new \Exception($response->body());
            }
        } catch (\Exception $e) {
            $this->error = "Tool error: " . $e->getMessage();
            $this->messages[] = [
                'id' => uniqid(),
                'role' => 'assistant',
                'content' => "Error executing tool: " . $e->getMessage(),
                'timestamp' => now()->format('H:i'),
                'isError' => true,
            ];
        } finally {
            $this->isLoading = false;
        }
    }

    /**
     * Quick action: Create Excel from simple data
     */
    public function quickCreateExcel(string $filename, array $headers, array $rows)
    {
        return $this->invokeTool('create_excel', [
            'file_path' => $filename,
            'headers' => $headers,
            'rows' => $rows,
        ]);
    }

    /**
     * Quick action: Create Word document
     */
    public function quickCreateWord(string $filename, string $title, array $content)
    {
        return $this->invokeTool('create_word', [
            'file_path' => $filename,
            'title' => $title,
            'content' => $content,
        ]);
    }

    /**
     * Quick action: Create PDF
     */
    public function quickCreatePdf(string $filename, string $title, array $content)
    {
        return $this->invokeTool('create_pdf', [
            'file_path' => $filename,
            'title' => $title,
            'content' => $content,
        ]);
    }

    /**
     * Get list of available tools from API
     */
    public function getAvailableTools()
    {
        try {
            $response = Http::get("{$this->apiUrl}/api/tools");
            if ($response->successful()) {
                return $response->json()['tools'] ?? [];
            }
        } catch (\Exception $e) {
            Log::error("Failed to fetch tools: " . $e->getMessage());
        }
        return [];
    }

    public function render()
    {
        return view('livewire.file-generator')
            ->layout('layouts.app');
    }
}
