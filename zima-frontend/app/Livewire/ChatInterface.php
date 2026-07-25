<?php

namespace App\Livewire;

use Livewire\Component;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Str;

class ChatInterface extends Component
{
    public string $message = '';
    public array $messages = [];
    public string $sessionKey;
    public bool $isStreaming = false;
    public string $currentResponse = '';

    // Gateway configuration
    public string $gatewayUrl;

    public function mount()
    {
        $this->gatewayUrl = config('services.gateway.url', 'http://localhost:18790');
        $this->sessionKey = 'agent:main:webchat:direct:' . auth()->id();

        // Load existing conversation history
        $this->loadHistory();
    }

    public function loadHistory()
    {
        try {
            // Load from gateway memory
            $response = Http::get("{$this->gatewayUrl}/api/admin/memory/sessions/{$this->sessionKey}");

            if ($response->successful()) {
                $data = $response->json();
                $this->messages = collect($data['messages'] ?? [])
                    ->filter(fn($m) => $m['type'] === 'message')
                    ->map(fn($m) => [
                        'role' => $m['role'],
                        'content' => is_string($m['content']) ? $m['content'] : json_encode($m['content']),
                        'timestamp' => $m['timestamp']
                    ])
                    ->toArray();
            }
        } catch (\Exception $e) {
            // Silently fail - start with empty history
        }
    }

    public function sendMessage()
    {
        if (empty(trim($this->message))) {
            return;
        }

        $userMessage = trim($this->message);

        // Add user message to UI immediately
        $this->messages[] = [
            'role' => 'user',
            'content' => $userMessage,
            'timestamp' => now()->toISOString()
        ];

        // Clear input
        $this->message = '';

        // Dispatch browser event to start streaming
        $this->dispatch('start-streaming', [
            'message' => $userMessage,
            'sessionKey' => $this->sessionKey,
            'gatewayUrl' => $this->gatewayUrl
        ]);
    }

    // Called from JavaScript when streaming completes
    public function streamingComplete($response)
    {
        $this->messages[] = [
            'role' => 'assistant',
            'content' => $response,
            'timestamp' => now()->toISOString()
        ];

        $this->currentResponse = '';
        $this->isStreaming = false;
    }

    public function clearHistory()
    {
        $this->messages = [];
        $this->currentResponse = '';
    }

    public function render()
    {
        return view('livewire.chat-interface')->layout('layouts.app');
    }
}
