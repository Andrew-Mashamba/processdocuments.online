<div class="min-h-screen bg-gray-50 py-8">
    <div class="max-w-4xl mx-auto px-4">
        {{-- Header --}}
        <div class="bg-white rounded-t-lg shadow-sm p-6 border-b">
            <div class="flex justify-between items-center">
                <h2 class="text-2xl font-bold text-gray-900">ZIMA Chat</h2>
                <div class="flex items-center space-x-4">
                    <span class="text-sm text-gray-500">Session: {{ substr($sessionKey, -8) }}</span>
                    <button
                        wire:click="clearHistory"
                        class="text-sm text-red-600 hover:text-red-800"
                        wire:confirm="Are you sure you want to clear the conversation history?">
                        Clear History
                    </button>
                </div>
            </div>
        </div>

        {{-- Messages Container --}}
        <div
            id="messages-container"
            class="bg-white shadow-sm p-6 space-y-4"
            style="height: 500px; overflow-y: auto;">

            @forelse($messages as $msg)
                <div class="flex {{ $msg['role'] === 'user' ? 'justify-end' : 'justify-start' }}">
                    <div class="max-w-3xl {{ $msg['role'] === 'user' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-900' }} rounded-lg px-4 py-2">
                        <div class="text-sm font-semibold mb-1">
                            {{ $msg['role'] === 'user' ? 'You' : 'Assistant' }}
                        </div>
                        <div class="prose prose-sm max-w-none {{ $msg['role'] === 'user' ? 'text-white' : '' }}">
                            {!! nl2br(e($msg['content'])) !!}
                        </div>
                        <div class="text-xs opacity-75 mt-1">
                            {{ \Carbon\Carbon::parse($msg['timestamp'])->format('h:i A') }}
                        </div>
                    </div>
                </div>
            @empty
                <div class="text-center text-gray-500 py-12">
                    <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                    </svg>
                    <p class="mt-2">No messages yet. Start a conversation!</p>
                </div>
            @endforelse

            {{-- Streaming Message (Assistant is typing) --}}
            @if($isStreaming || $currentResponse)
                <div class="flex justify-start" id="streaming-message">
                    <div class="max-w-3xl bg-gray-100 text-gray-900 rounded-lg px-4 py-2">
                        <div class="text-sm font-semibold mb-1 flex items-center">
                            Assistant
                            <span class="ml-2 inline-flex">
                                <span class="animate-pulse">●</span>
                                <span class="animate-pulse ml-1">●</span>
                                <span class="animate-pulse ml-1">●</span>
                            </span>
                        </div>
                        <div class="prose prose-sm max-w-none" id="streaming-content">
                            {{ $currentResponse }}
                        </div>
                    </div>
                </div>
            @endif
        </div>

        {{-- Input Area --}}
        <div class="bg-white rounded-b-lg shadow-sm p-6 border-t">
            <form wire:submit="sendMessage" class="flex space-x-4">
                <input
                    type="text"
                    wire:model="message"
                    placeholder="Type your message..."
                    class="flex-1 rounded-lg border-gray-300 focus:border-blue-500 focus:ring-blue-500"
                    {{ $isStreaming ? 'disabled' : '' }}>

                <button
                    type="submit"
                    class="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                    {{ $isStreaming ? 'disabled' : '' }}>
                    @if($isStreaming)
                        <span class="flex items-center">
                            <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            Sending...
                        </span>
                    @else
                        Send
                    @endif
                </button>
            </form>

            <div class="mt-2 text-xs text-gray-500">
                Press Enter to send • Gateway: {{ $gatewayUrl }}
            </div>
        </div>
    </div>
</div>

{{-- JavaScript for SSE Streaming --}}
@script
<script>
    let eventSource = null;
    let currentStreamContent = '';

    // Listen for streaming start event from Livewire
    $wire.on('start-streaming', (data) => {
        console.log('Starting stream with data:', data);

        // Set streaming state
        $wire.set('isStreaming', true);
        $wire.set('currentResponse', '');
        currentStreamContent = '';

        // Close any existing connection
        if (eventSource) {
            eventSource.close();
        }

        // Start SSE connection
        startStreaming(data[0]);
    });

    function startStreaming({ message, sessionKey, gatewayUrl }) {
        const url = `${gatewayUrl}/api/chat/stream`;

        // Create request payload
        const payload = {
            message: message,
            sessionKey: sessionKey,
            channel: 'webchat',
            sender: {
                id: '{{ auth()->id() }}',
                name: '{{ auth()->user()->name ?? "User" }}'
            },
            attachments: []
        };

        // Use fetch with POST and streaming response
        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/event-stream'
            },
            body: JSON.stringify(payload)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();

            // Read stream
            function readStream() {
                reader.read().then(({ done, value }) => {
                    if (done) {
                        // Stream complete
                        handleStreamComplete();
                        return;
                    }

                    // Decode chunk
                    const chunk = decoder.decode(value, { stream: true });
                    processChunk(chunk);

                    // Continue reading
                    readStream();
                });
            }

            readStream();
        })
        .catch(error => {
            console.error('Streaming error:', error);
            $wire.set('isStreaming', false);
            alert('Error: ' + error.message);
        });
    }

    function processChunk(chunk) {
        // Parse SSE format (event: type\ndata: {...}\n\n)
        const lines = chunk.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();

            if (line.startsWith('data: ')) {
                try {
                    const data = JSON.parse(line.substring(6));

                    // Handle different event types
                    if (data.content) {
                        // Append content
                        currentStreamContent += data.content;

                        // Update UI
                        const streamingEl = document.getElementById('streaming-content');
                        if (streamingEl) {
                            streamingEl.textContent = currentStreamContent;

                            // Auto-scroll to bottom
                            const container = document.getElementById('messages-container');
                            if (container) {
                                container.scrollTop = container.scrollHeight;
                            }
                        }
                    }

                    // Check for completion
                    if (data.type === 'complete' || line.includes('event: complete')) {
                        handleStreamComplete();
                    }
                } catch (e) {
                    // Skip invalid JSON
                    console.warn('Could not parse SSE data:', line);
                }
            }
        }
    }

    function handleStreamComplete() {
        console.log('Stream complete, final content:', currentStreamContent);

        // Notify Livewire component
        $wire.call('streamingComplete', currentStreamContent);

        // Reset
        currentStreamContent = '';

        // Scroll to bottom after message is added
        setTimeout(() => {
            const container = document.getElementById('messages-container');
            if (container) {
                container.scrollTop = container.scrollHeight;
            }
        }, 100);
    }

    // Auto-scroll when messages update
    Livewire.hook('morph.updated', ({ el, component }) => {
        if (el.id === 'messages-container') {
            el.scrollTop = el.scrollHeight;
        }
    });
</script>
@endscript
