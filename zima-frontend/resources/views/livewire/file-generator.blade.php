<div>
<style>
    @keyframes progress-indeterminate {
        0% { transform: translateX(-100%); }
        50% { transform: translateX(0%); }
        100% { transform: translateX(100%); }
    }
    .animate-progress-indeterminate {
        width: 50%;
        animation: progress-indeterminate 1.5s ease-in-out infinite;
    }

    /* Spin animation for loading spinner */
    @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }

    /* AI Message Content - minimal resets for inline-styled HTML */
    .ai-message-content {
        font-size: 0.875rem;
        line-height: 1.6;
    }
    .ai-message-content > *:first-child {
        margin-top: 0 !important;
    }
    .ai-message-content > *:last-child {
        margin-bottom: 0 !important;
    }
    /* Dark mode color overrides if needed */
    .dark .ai-message-content {
        color: inherit;
    }

    /* Input textarea placeholder */
    textarea::placeholder {
        color: #a3a3a3;
    }
    textarea:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }
</style>

<div class="flex h-[calc(100vh-10rem)] gap-3" wire:poll.5s="loadFiles">
    <!-- Sessions Sidebar - Monochrome Design -->
    <div class="w-64 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden flex-shrink-0">
        <!-- Sessions Header -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-neutral-200">
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Chat History</h3>
                <p class="text-[11px] text-neutral-500">Recent conversations</p>
            </div>
            <button
                wire:click="createNewSession"
                class="w-8 h-8 bg-neutral-900 hover:bg-neutral-800 text-white rounded-xl flex items-center justify-center shadow-sm transition-all hover:shadow-md"
                title="New Chat"
            >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                </svg>
            </button>
        </div>

        <!-- Sessions List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
            @forelse($sessions as $session)
                <div
                    wire:click="switchSession('{{ $session['id'] }}')"
                    wire:key="session-{{ $session['id'] }}"
                    class="group flex items-start justify-between p-3 rounded-xl cursor-pointer transition-all {{ $session['id'] === $currentSessionId ? 'bg-neutral-100 border border-neutral-300 shadow-sm' : 'hover:bg-neutral-50' }}"
                >
                    <div class="flex-1 min-w-0">
                        <p class="text-[13px] font-medium truncate {{ $session['id'] === $currentSessionId ? 'text-neutral-900' : 'text-neutral-700' }}">
                            {{ $session['title'] }}
                        </p>
                        <p class="text-[11px] text-neutral-500 mt-0.5">
                            {{ $session['message_count'] }} messages · {{ $session['updated_at'] }}
                        </p>
                    </div>
                    <button
                        wire:click.stop="deleteSession('{{ $session['id'] }}')"
                        wire:confirm="Delete this conversation?"
                        class="p-1 text-neutral-400 hover:text-neutral-900 rounded-lg opacity-0 group-hover:opacity-100 transition"
                        title="Delete"
                    >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                        </svg>
                    </button>
                </div>
            @empty
                <div class="flex flex-col items-center justify-center py-8 text-center">
                    <div class="w-12 h-12 bg-neutral-100 rounded-xl flex items-center justify-center mb-3">
                        <svg class="w-6 h-6 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
                        </svg>
                    </div>
                    <p class="text-[11px] text-neutral-500">No conversations yet</p>
                </div>
            @endforelse
        </div>
    </div>

    <!-- Uploaded Files Sidebar - Monochrome Design -->
    <div class="w-64 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden flex-shrink-0">
        <!-- Sidebar Header -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-neutral-200">
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Uploaded Files</h3>
                <p class="text-[11px] text-neutral-500">For AI context</p>
            </div>
            <button
                wire:click="loadSessionFiles"
                class="p-1.5 text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100 rounded-xl transition"
                title="Refresh"
            >
                <svg class="w-4 h-4 {{ $isUploading ? 'animate-spin' : '' }}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                </svg>
            </button>
        </div>

        <!-- Upload Area -->
        <div class="px-3 py-3 border-b border-neutral-200">
            <form wire:submit="uploadFile">
                <label class="flex flex-col items-center justify-center w-full h-20 border-2 border-dashed border-neutral-300 rounded-xl cursor-pointer bg-neutral-50 hover:bg-neutral-100 hover:border-neutral-400 transition-all">
                    <div class="flex flex-col items-center justify-center py-2">
                        @if($isUploading)
                            <svg class="w-5 h-5 text-neutral-900 animate-spin" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            <p class="text-[11px] text-neutral-500 mt-1">Uploading...</p>
                        @else
                            <div class="w-8 h-8 bg-neutral-900 rounded-lg flex items-center justify-center mb-1">
                                <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                                </svg>
                            </div>
                            <p class="text-[11px] text-neutral-500">Click to upload · Max 10MB</p>
                        @endif
                    </div>
                    <input
                        type="file"
                        wire:model="uploadedFile"
                        class="hidden"
                        @disabled($isUploading)
                    />
                </label>
            </form>

            @if($uploadError)
                <p class="text-[11px] text-neutral-900 mt-2 bg-neutral-100 rounded-lg px-2 py-1">{{ $uploadError }}</p>
            @endif
        </div>

        <!-- Files List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
            @if(empty($sessionFiles) || count($sessionFiles) === 0)
                <div class="flex flex-col items-center justify-center h-full text-center py-8">
                    <div class="w-12 h-12 bg-neutral-100 rounded-xl flex items-center justify-center mb-3">
                        <svg class="w-6 h-6 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                        </svg>
                    </div>
                    <p class="text-[13px] text-neutral-500">No files uploaded</p>
                    <p class="text-[11px] text-neutral-400 mt-1">Upload files for AI context</p>
                </div>
            @else
                <div class="space-y-2">
                    @foreach($sessionFiles as $file)
                        @php
                            $fileName = $file['name'] ?? $file['filename'] ?? '';
                            $includeInContext = $file['includeInContext'] ?? true;
                        @endphp
                        <div class="bg-neutral-50 rounded-xl p-3 hover:bg-neutral-100 transition-all" wire:key="upload-{{ $fileName }}">
                            <div class="flex items-start space-x-3">
                                <div class="w-8 h-8 bg-neutral-900 rounded-lg flex items-center justify-center flex-shrink-0">
                                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                                        <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd"/>
                                    </svg>
                                </div>
                                <div class="flex-1 min-w-0">
                                    <p class="text-[12px] font-medium text-neutral-900 truncate" title="{{ $fileName }}">
                                        {{ $fileName }}
                                    </p>
                                    <p class="text-[11px] text-neutral-500">
                                        {{ $file['sizeFormatted'] ?? 'Unknown size' }}
                                    </p>
                                </div>
                            </div>

                            <!-- File Actions -->
                            <div class="flex items-center justify-between mt-2">
                                <!-- Context Toggle -->
                                <button
                                    wire:click="toggleFileInContext('{{ $fileName }}')"
                                    class="flex items-center space-x-1 text-[11px] {{ $includeInContext ? 'text-neutral-900' : 'text-neutral-400' }}"
                                    title="{{ $includeInContext ? 'Included in AI context' : 'Not included in context' }}"
                                >
                                    @if($includeInContext)
                                        <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                                        </svg>
                                        <span>In context</span>
                                    @else
                                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                                        </svg>
                                        <span>Excluded</span>
                                    @endif
                                </button>

                                <div class="flex items-center space-x-1">
                                    <!-- Download -->
                                    <a
                                        href="http://localhost:5000/api/files/session/{{ $currentSessionId }}/{{ $fileName }}/download"
                                        target="_blank"
                                        class="p-1 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                        title="Download"
                                    >
                                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                        </svg>
                                    </a>
                                    <!-- Delete -->
                                    <button
                                        wire:click="deleteSessionFile('{{ $fileName }}')"
                                        wire:confirm="Delete this file from the session?"
                                        class="p-1 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                        title="Delete"
                                    >
                                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                                        </svg>
                                    </button>
                                </div>
                            </div>
                        </div>
                    @endforeach
                </div>
            @endif
        </div>

        <!-- Upload Stats Footer -->
        <div class="border-t border-neutral-200 px-4 py-3 bg-neutral-50">
            <p class="text-[11px] text-neutral-500">
                @php
                    $uploadCount = is_array($sessionFiles) ? count($sessionFiles) : 0;
                @endphp
                {{ $uploadCount }} {{ $uploadCount === 1 ? 'file' : 'files' }} uploaded
            </p>
            <p class="text-[11px] text-neutral-400 mt-0.5">
                Files are included in AI context
            </p>
        </div>
    </div>

    <!-- Chat Area - Monochrome Design -->
    <div class="flex-1 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden">
        <!-- Chat Header -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-neutral-200">
            <div class="flex items-center space-x-3">
                <div class="w-10 h-10 bg-neutral-900 rounded-xl flex items-center justify-center shadow-sm">
                    <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                    </svg>
                </div>
                <div>
                    <h2 class="text-[15px] font-semibold text-neutral-900">ZIMA AI</h2>
                    <p class="text-[11px] text-neutral-500">File generation assistant</p>
                </div>
            </div>
            @if(!empty($messages) && count($messages) > 0)
                <button
                    wire:click="clearChat"
                    class="text-[13px] text-neutral-500 hover:text-neutral-900 flex items-center space-x-1 px-3 py-1.5 hover:bg-neutral-100 rounded-xl transition"
                >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                    </svg>
                    <span>Clear chat</span>
                </button>
            @endif
        </div>

        <!-- Messages Container -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6" id="chat-messages">
            @if(empty($messages) || count($messages) === 0)
                <!-- Welcome State - Monochrome Design -->
                <div class="flex flex-col items-center justify-center h-full text-center">
                    <div class="w-20 h-20 bg-neutral-900 rounded-2xl flex items-center justify-center mb-6 shadow-md">
                        <svg class="w-10 h-10 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                    </div>
                    <h3 class="text-xl font-semibold text-neutral-900 mb-2">How can I help you today?</h3>
                    <p class="text-[13px] text-neutral-500 max-w-md mb-8">
                        I can generate Excel spreadsheets, Word documents, and PDF files. Ask me to update a file and I'll create a new version.
                    </p>
                    <div class="grid grid-cols-2 gap-3 max-w-lg">
                        <button
                            wire:click="$set('prompt', 'Create an Excel file with a list of 10 countries, their capitals, and populations')"
                            class="p-4 text-left bg-white border border-neutral-200 rounded-xl hover:bg-neutral-50 hover:border-neutral-300 hover:shadow-sm transition-all"
                        >
                            <p class="text-[13px] font-medium text-neutral-900">Excel spreadsheet</p>
                            <p class="text-[11px] text-neutral-500 mt-1">Countries & capitals list</p>
                        </button>
                        <button
                            wire:click="$set('prompt', 'Create a Word document with a professional cover letter template')"
                            class="p-4 text-left bg-white border border-neutral-200 rounded-xl hover:bg-neutral-50 hover:border-neutral-300 hover:shadow-sm transition-all"
                        >
                            <p class="text-[13px] font-medium text-neutral-900">Word document</p>
                            <p class="text-[11px] text-neutral-500 mt-1">Cover letter template</p>
                        </button>
                        <button
                            wire:click="$set('prompt', 'Create a PDF invoice template with company details and line items')"
                            class="p-4 text-left bg-white border border-neutral-200 rounded-xl hover:bg-neutral-50 hover:border-neutral-300 hover:shadow-sm transition-all"
                        >
                            <p class="text-[13px] font-medium text-neutral-900">PDF invoice</p>
                            <p class="text-[11px] text-neutral-500 mt-1">Business invoice template</p>
                        </button>
                        <button
                            wire:click="$set('prompt', 'Create an Excel budget tracker with income, expenses, and monthly totals')"
                            class="p-4 text-left bg-white border border-neutral-200 rounded-xl hover:bg-neutral-50 hover:border-neutral-300 hover:shadow-sm transition-all"
                        >
                            <p class="text-[13px] font-medium text-neutral-900">Budget tracker</p>
                            <p class="text-[11px] text-neutral-500 mt-1">Income & expense tracker</p>
                        </button>
                    </div>
                </div>
            @else
                <!-- Chat Messages - Monochrome Design -->
                @foreach($messages as $index => $message)
                    <div class="flex {{ $message['role'] === 'user' ? 'justify-end' : 'justify-start' }}" wire:key="msg-{{ $index }}">
                        <div class="flex items-start space-x-3 {{ $message['role'] === 'user' ? 'max-w-[80%] flex-row-reverse space-x-reverse' : 'max-w-[95%]' }}">
                            <!-- Avatar -->
                            @if($message['role'] === 'user')
                                <div class="w-8 h-8 bg-neutral-200 rounded-xl flex items-center justify-center flex-shrink-0">
                                    <svg class="w-4 h-4 text-neutral-600" fill="currentColor" viewBox="0 0 20 20">
                                        <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
                                    </svg>
                                </div>
                            @else
                                <div class="w-8 h-8 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
                                    <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                                    </svg>
                                </div>
                            @endif

                            <!-- Message Content -->
                            <div class="{{ $message['role'] === 'user' ? 'bg-neutral-900 text-white' : 'bg-neutral-100' }} rounded-2xl px-4 py-3 {{ $message['role'] === 'user' ? 'rounded-tr-md' : 'rounded-tl-md' }}">
                                @if($message['role'] === 'user')
                                    <p class="text-[13px] whitespace-pre-wrap">{{ $message['content'] }}</p>
                                @else
                                    <div class="ai-message-content">{!! $message['content'] !!}</div>
                                @endif

                                <!-- Generated Files in Message -->
                                @if(isset($message['files']) && count($message['files']) > 0)
                                    <div class="mt-3 pt-3 border-t border-neutral-200">
                                        <p class="text-[11px] text-neutral-500 mb-2">Generated files:</p>
                                        <div class="space-y-2">
                                            @foreach($message['files'] as $file)
                                                @php
                                                    $fileName = is_array($file) ? ($file['name'] ?? $file['fileName'] ?? '') : $file;
                                                    $downloadUrl = "http://localhost:5000/api/files/generated/{$currentSessionId}/" . urlencode($fileName) . "/download";
                                                @endphp
                                                <a
                                                    href="{{ $downloadUrl }}"
                                                    target="_blank"
                                                    class="flex items-center space-x-2 p-2 bg-white hover:bg-neutral-50 rounded-xl transition-all group border border-neutral-200 hover:border-neutral-300"
                                                >
                                                    <div class="w-6 h-6 bg-neutral-900 rounded-lg flex items-center justify-center flex-shrink-0">
                                                        <svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                                                            <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd"/>
                                                        </svg>
                                                    </div>
                                                    <span class="text-[12px] font-medium text-neutral-700 flex-1">{{ $fileName }}</span>
                                                    <svg class="w-4 h-4 text-neutral-400 group-hover:text-neutral-900 transition" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                                    </svg>
                                                </a>
                                            @endforeach
                                        </div>
                                    </div>
                                @endif

                                <p class="text-[11px] {{ $message['role'] === 'user' ? 'text-neutral-400' : 'text-neutral-500' }} mt-2">{{ $message['timestamp'] }}</p>
                            </div>
                        </div>
                    </div>
                @endforeach
            @endif

            <!-- Streaming Response - Monochrome Design -->
            @if($isLoading && !empty($streamingContent))
                <div class="flex justify-start">
                    <div class="flex items-start space-x-3 max-w-[80%]">
                        <div class="w-8 h-8 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
                            <svg class="w-4 h-4 text-white animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                            </svg>
                        </div>
                        <div class="bg-neutral-100 rounded-2xl rounded-tl-md px-4 py-3">
                            <div class="ai-message-content">{!! $streamingContent !!}<span class="inline-block w-2 h-4 bg-neutral-900 animate-pulse ml-1"></span></div>
                        </div>
                    </div>
                </div>
            @endif

            <!-- Loading indicator - Monochrome Design (Phase 1: Works with streaming mode) -->
            @if($isLoading && empty($streamingContent))
            <div class="flex justify-start" x-data="loadingProgress()" x-init="start()">
                <div class="flex items-start space-x-3 w-full max-w-md">
                    <div class="w-8 h-8 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
                        <svg class="w-4 h-4 text-white animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                    </div>
                    <div class="flex-1 bg-neutral-100 rounded-2xl rounded-tl-md px-4 py-4">
                        <div class="space-y-3">
                            <!-- Current Status -->
                            <div class="flex items-center justify-between">
                                <div class="flex items-center space-x-2">
                                    <div class="w-5 h-5 border-2 border-neutral-900 border-t-transparent rounded-full animate-spin"></div>
                                    <span class="text-[13px] font-medium text-neutral-700" x-text="currentStep"></span>
                                </div>
                                <span class="text-[11px] font-mono text-neutral-500" x-text="elapsedTime"></span>
                            </div>

                            <!-- Progress Bar -->
                            <div class="w-full bg-neutral-200 rounded-full h-1.5 overflow-hidden">
                                <div class="bg-neutral-900 h-full rounded-full animate-progress-indeterminate"></div>
                            </div>

                            <!-- Step Indicators -->
                            <div class="flex items-center justify-between text-[11px]">
                                <template x-for="(step, index) in steps" :key="index">
                                    <div class="flex items-center space-x-1" :class="stepIndex >= index ? 'text-neutral-900' : 'text-neutral-400'">
                                        <div class="w-2 h-2 rounded-full" :class="stepIndex > index ? 'bg-neutral-900' : (stepIndex === index ? 'bg-neutral-600 animate-pulse' : 'bg-neutral-300')"></div>
                                        <span x-text="step.short"></span>
                                    </div>
                                </template>
                            </div>

                            <!-- Tips -->
                            <div class="mt-2 p-2 bg-neutral-200 rounded-xl">
                                <p class="text-[11px] text-neutral-600 flex items-center space-x-1">
                                    <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                                    </svg>
                                    <span x-text="currentTip"></span>
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            @endif
        </div>

        <!-- Input Area -->
        <div class="p-4 bg-neutral-50">
            <form wire:submit="generate">
                <div class="flex w-full flex-row items-center gap-2 rounded-[99px] border border-gray-900/10 bg-gray-900/5 p-2">
                  <div class="relative h-full w-full min-w-[200px]">
                    <textarea
                      rows="1"
                      placeholder="Describe the file you want to generate..."
                      wire:model="prompt"
                      @if($isLoading) disabled @endif
                      x-data="{
                          resize() {
                              $el.style.height = '40px';
                              $el.style.height = Math.min($el.scrollHeight, 150) + 'px';
                          }
                      }"
                      x-on:input="resize()"
                      x-on:keydown.enter.prevent="if(!event.shiftKey && !$wire.isLoading) $wire.generate()"
                      class="h-full min-h-[40px] w-full resize-none rounded-full bg-white border border-gray-300 px-4 py-2.5 font-sans text-sm font-normal text-gray-900 outline-none transition-all placeholder:text-gray-400 focus:border-gray-900 focus:ring-1 focus:ring-gray-900 disabled:bg-gray-100 disabled:cursor-not-allowed"></textarea>
                  </div>
                  <div class="flex items-center self-center">
                    <button
                      class="relative h-10 max-h-[40px] w-10 max-w-[40px] select-none rounded-full text-center align-middle font-sans text-xs font-medium uppercase text-gray-900 transition-all hover:bg-gray-900/10 active:bg-gray-900/20 disabled:pointer-events-none disabled:opacity-50 disabled:shadow-none"
                      type="submit"
                      @if($isLoading) disabled @endif>
                      <span class="absolute transform -translate-x-1/2 -translate-y-1/2 top-1/2 left-1/2">
                        <!-- Loading spinner (shown during streaming) -->
                        @if($isLoading)
                        <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="#90A4AE" stroke-width="3"></circle>
                            <path class="opacity-75" fill="#90A4AE" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        @else
                        <!-- Send icon -->
                        <svg width="14" height="14" viewBox="0 0 14 14" fill="none" xmlns="http://www.w3.org/2000/svg">
                          <path
                            d="M12.9576 7.71521C13.0903 7.6487 13.2019 7.54658 13.2799 7.42027C13.3579 7.29396 13.3992 7.14845 13.3992 7.00001C13.3992 6.85157 13.3579 6.70606 13.2799 6.57975C13.2019 6.45344 13.0903 6.35132 12.9576 6.28481L1.75762 0.684812C1.61875 0.615327 1.46266 0.587759 1.30839 0.605473C1.15412 0.623186 1.00834 0.685413 0.888833 0.784565C0.769325 0.883716 0.681257 1.01551 0.635372 1.16385C0.589486 1.3122 0.587767 1.4707 0.630424 1.62001L1.77362 5.62001C1.82144 5.78719 1.92243 5.93424 2.06129 6.03889C2.20016 6.14355 2.36934 6.20011 2.54322 6.20001H6.20002C6.4122 6.20001 6.61568 6.2843 6.76571 6.43433C6.91574 6.58436 7.00002 6.78784 7.00002 7.00001C7.00002 7.21218 6.91574 7.41567 6.76571 7.5657C6.61568 7.71573 6.4122 7.80001 6.20002 7.80001H2.54322C2.36934 7.79991 2.20016 7.85647 2.06129 7.96113C1.92243 8.06578 1.82144 8.21283 1.77362 8.38001L0.631223 12.38C0.588482 12.5293 0.590098 12.6877 0.635876 12.8361C0.681652 12.9845 0.769612 13.1163 0.889027 13.2155C1.00844 13.3148 1.15415 13.3771 1.30838 13.3949C1.46262 13.4128 1.61871 13.3854 1.75762 13.316L12.9576 7.71601V7.71521Z"
                            fill="#90A4AE"></path>
                        </svg>
                        @endif
                      </span>
                    </button>
                  </div>
                </div>
            </form>

            <!-- Helper Text -->
            <div class="flex items-center justify-center gap-4 mt-3">
                <div class="flex items-center gap-1.5 text-[11px] text-neutral-400">
                    <kbd class="px-1.5 py-0.5 bg-neutral-100 border border-neutral-200 rounded text-[10px] font-mono text-neutral-500">Enter</kbd>
                    <span>to send</span>
                </div>
                <div class="w-px h-3 bg-neutral-200"></div>
                <div class="flex items-center gap-1.5 text-[11px] text-neutral-400">
                    <kbd class="px-1.5 py-0.5 bg-neutral-100 border border-neutral-200 rounded text-[10px] font-mono text-neutral-500">Shift</kbd>
                    <span>+</span>
                    <kbd class="px-1.5 py-0.5 bg-neutral-100 border border-neutral-200 rounded text-[10px] font-mono text-neutral-500">Enter</kbd>
                    <span>for new line</span>
                </div>
            </div>
        </div>
    </div>

    <!-- Files Sidebar (Current Chat) - Monochrome Design -->
    <div class="w-64 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden flex-shrink-0">
        <!-- Sidebar Header -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-neutral-200">
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Generated Files</h3>
                <p class="text-[11px] text-neutral-500">This chat only</p>
            </div>
            <button
                wire:click="loadFiles"
                class="p-1.5 text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100 rounded-xl transition"
                title="Refresh"
            >
                <svg class="w-4 h-4 {{ $isLoading ? 'animate-spin' : '' }}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                </svg>
            </button>
        </div>

        <!-- Files List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
            @if(empty($fileGroups) || count($fileGroups) === 0)
                <div class="flex flex-col items-center justify-center h-full text-center py-8">
                    <div class="w-12 h-12 bg-neutral-100 rounded-xl flex items-center justify-center mb-3">
                        <svg class="w-6 h-6 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M5 19a2 2 0 01-2-2V7a2 2 0 012-2h4l2 2h4a2 2 0 012 2v1M5 19h14a2 2 0 002-2v-5a2 2 0 00-2-2H9a2 2 0 00-2 2v5a2 2 0 01-2 2z"/>
                        </svg>
                    </div>
                    <p class="text-[13px] text-neutral-500">No files yet</p>
                    <p class="text-[11px] text-neutral-400 mt-1">Generated files will appear here</p>
                </div>
            @else
                <div class="space-y-2">
                    @foreach($fileGroups as $group)
                        @php
                            $isExpanded = in_array($group['baseName'], $expandedGroups);
                            $hasVersions = ($group['versionCount'] ?? 1) > 1;
                        @endphp
                        <div class="bg-neutral-50 rounded-xl overflow-hidden" wire:key="group-{{ $group['baseName'] }}">
                            <!-- Main file row -->
                            <div class="p-3 hover:bg-neutral-100 transition">
                                <div class="flex items-start space-x-3">
                                    <div class="w-10 h-10 bg-neutral-900 rounded-xl flex items-center justify-center flex-shrink-0">
                                        <svg class="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd"/>
                                        </svg>
                                    </div>
                                    <div class="flex-1 min-w-0">
                                        <p class="text-[13px] font-medium text-neutral-900 truncate" title="{{ $group['baseName'] }}">
                                            {{ $group['baseName'] }}
                                        </p>
                                        <div class="flex items-center space-x-2 mt-0.5">
                                            <p class="text-[11px] text-neutral-500">
                                                {{ $group['latestVersion']['sizeFormatted'] ?? 'Unknown' }}
                                            </p>
                                            @if($hasVersions)
                                                <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[11px] font-medium bg-neutral-200 text-neutral-700">
                                                    v{{ $group['latestVersion']['version'] ?? 1 }}
                                                </span>
                                                <span class="text-[11px] text-neutral-400">
                                                    ({{ $group['versionCount'] }} versions)
                                                </span>
                                            @endif
                                        </div>
                                    </div>
                                </div>

                                <!-- Action buttons -->
                                <div class="flex items-center justify-between mt-2">
                                    @if($hasVersions)
                                        <button
                                            wire:click="toggleGroup('{{ $group['baseName'] }}')"
                                            class="text-[11px] text-neutral-500 hover:text-neutral-900 flex items-center space-x-1 transition"
                                        >
                                            <svg class="w-3 h-3 transition-transform {{ $isExpanded ? 'rotate-180' : '' }}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                                            </svg>
                                            <span>{{ $isExpanded ? 'Hide' : 'Show' }} versions</span>
                                        </button>
                                    @else
                                        <div></div>
                                    @endif

                                    <div class="flex items-center space-x-1">
                                        <a
                                            href="http://localhost:5000/api/files/generated/{{ $currentSessionId }}/{{ urlencode($group['latestVersion']['fileName'] ?? $group['baseName']) }}/download"
                                            target="_blank"
                                            class="p-1.5 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                            title="Download latest"
                                        >
                                            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                            </svg>
                                        </a>
                                        @if($hasVersions)
                                            <button
                                                wire:click="deleteAllVersions('{{ $group['baseName'] }}')"
                                                wire:confirm="Delete all {{ $group['versionCount'] }} versions of this file?"
                                                class="p-1.5 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                                title="Delete all versions"
                                            >
                                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                                                </svg>
                                            </button>
                                        @else
                                            <button
                                                wire:click="deleteFile('{{ $group['latestVersion']['fileName'] ?? $group['baseName'] }}')"
                                                wire:confirm="Are you sure you want to delete this file?"
                                                class="p-1.5 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                                title="Delete"
                                            >
                                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                                                </svg>
                                            </button>
                                        @endif
                                    </div>
                                </div>
                            </div>

                            <!-- Expanded versions list -->
                            @if($isExpanded && $hasVersions)
                                <div class="border-t border-neutral-200 bg-neutral-100 px-3 py-2 space-y-1">
                                    @foreach($group['versions'] as $version)
                                        <div class="flex items-center justify-between p-2 rounded-lg hover:bg-white transition">
                                            <div class="flex items-center space-x-2">
                                                <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[11px] font-medium {{ $loop->first ? 'bg-neutral-900 text-white' : 'bg-neutral-200 text-neutral-600' }}">
                                                    v{{ $version['version'] }}
                                                </span>
                                                <span class="text-[11px] text-neutral-500">
                                                    {{ $version['sizeFormatted'] }}
                                                </span>
                                                @if($loop->first)
                                                    <span class="text-[11px] text-neutral-900 font-medium">Latest</span>
                                                @endif
                                            </div>
                                            <div class="flex items-center space-x-1">
                                                <a
                                                    href="http://localhost:5000/api/files/generated/{{ $currentSessionId }}/{{ urlencode($version['fileName']) }}/download"
                                                    target="_blank"
                                                    class="p-1 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                                    title="Download v{{ $version['version'] }}"
                                                >
                                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                                    </svg>
                                                </a>
                                                <button
                                                    wire:click="deleteFile('{{ $version['fileName'] }}')"
                                                    wire:confirm="Delete version {{ $version['version'] }}?"
                                                    class="p-1 text-neutral-400 hover:text-neutral-900 rounded-lg transition"
                                                    title="Delete v{{ $version['version'] }}"
                                                >
                                                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                                                    </svg>
                                                </button>
                                            </div>
                                        </div>
                                    @endforeach
                                </div>
                            @endif
                        </div>
                    @endforeach
                </div>
            @endif
        </div>

        <!-- File Stats Footer -->
        <div class="border-t border-neutral-200 px-4 py-3 bg-neutral-50">
            <p class="text-[11px] text-neutral-500">
                @php
                    $fileCount = is_array($fileGroups) ? count($fileGroups) : 0;
                    $totalVersions = is_array($fileGroups) ? collect($fileGroups)->sum('versionCount') : 0;
                @endphp
                {{ $fileCount }} {{ $fileCount === 1 ? 'file' : 'files' }}
                @if($totalVersions > $fileCount)
                    <span class="text-neutral-400">({{ $totalVersions }} versions)</span>
                @endif
            </p>
            <p class="text-[11px] text-neutral-400 mt-0.5">
                Click to download or expand
            </p>
        </div>
    </div>
</div>

<script>
    // Auto-scroll to bottom when new messages appear
    document.addEventListener('livewire:updated', () => {
        const chatMessages = document.getElementById('chat-messages');
        if (chatMessages) {
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }
    });

    // Streaming handler using Server-Sent Events (SSE)
    class StreamingHandler {
        constructor(wire) {
            this.wire = wire;
            this.eventSource = null;
            this.content = '';
            this.usage = null;
            this.files = [];
            this.model = null;
        }

        async startStreaming(streamUrl, requestData) {
            this.content = '';
            this.usage = null;
            this.files = [];
            this.model = null;

            console.log('StreamingHandler.startStreaming called:', { streamUrl, requestData });

            try {
                // Use fetch with POST for streaming (EventSource only supports GET)
                console.log('Fetching:', streamUrl);
                const response = await fetch(streamUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'text/event-stream',
                    },
                    body: JSON.stringify(requestData)
                });
                console.log('Fetch response:', response.status, response.statusText);

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                while (true) {
                    const { done, value } = await reader.read();
                    if (done) break;

                    buffer += decoder.decode(value, { stream: true });
                    const lines = buffer.split('\n');
                    buffer = lines.pop(); // Keep incomplete line in buffer

                    for (const line of lines) {
                        this.processLine(line);
                    }
                }

                // Process any remaining buffer
                if (buffer.trim()) {
                    this.processLine(buffer);
                }

                // Complete the streaming
                this.wire.completeStreaming(this.content, this.usage || {}, this.files, this.model);

            } catch (error) {
                console.error('Streaming error:', error);
                console.error('Error details:', error.message, error.stack);
                // Call completeStreaming with empty content to clean up state
                this.wire.completeStreaming('Error: ' + error.message, {}, [], null);
            }
        }

        processLine(line) {
            if (line.startsWith('event:')) {
                this.currentEvent = line.substring(6).trim();
            } else if (line.startsWith('data:')) {
                const jsonStr = line.substring(5).trim();
                if (jsonStr) {
                    try {
                        const data = JSON.parse(jsonStr);
                        this.handleEvent(this.currentEvent, data);
                    } catch (e) {
                        console.warn('Failed to parse SSE data:', jsonStr);
                    }
                }
            }
        }

        handleEvent(eventType, data) {
            switch (eventType) {
                case 'content':
                    // Append content and update UI in real-time
                    if (data.content) {
                        this.content += data.content;
                        this.wire.updateStreamingContent(this.content);
                    }
                    break;

                case 'complete':
                    // Final event with all data
                    if (data.output) {
                        this.content = data.output;
                    }
                    if (data.usage) {
                        this.usage = data.usage;
                    }
                    if (data.files) {
                        this.files = data.files;
                    }
                    if (data.model) {
                        this.model = data.model;
                    }
                    break;

                case 'files':
                    if (data.files) {
                        this.files = data.files;
                    }
                    break;

                case 'error':
                    console.error('Stream error:', data.message);
                    this.wire.set('error', data.message);
                    break;

                case 'start':
                    // Phase 2: Log model routing info
                    console.log('Streaming started:', data.requestId, 'Model:', data.model, 'Complexity:', data.complexity);
                    break;
            }
        }

        stop() {
            if (this.eventSource) {
                this.eventSource.close();
                this.eventSource = null;
            }
        }
    }

    // Initialize streaming handler
    let streamingHandler = null;

    // Listen for streaming requests from Livewire
    document.addEventListener('livewire:initialized', () => {
        Livewire.on('startStreaming', (eventData) => {
            // Livewire 3 passes data as first element of array
            const data = Array.isArray(eventData) ? eventData[0] : eventData;
            console.log('startStreaming event received:', data);

            // Get the component - try multiple methods
            let component = Livewire.find('{{ $this->getId() }}');
            if (!component) {
                // Try to get first file-generator component
                const components = Livewire.all();
                component = components.find(c => c.$wire && c.$wire.generate);
                console.log('Found component via fallback:', component ? 'yes' : 'no');
            }

            if (!component) {
                console.error('Could not find Livewire component!');
                return;
            }

            if (!streamingHandler) {
                streamingHandler = new StreamingHandler(component);
            } else {
                streamingHandler.wire = component;
            }

            console.log('Starting streaming with handler');
            streamingHandler.startStreaming(data.streamUrl, {
                prompt: data.prompt,
                messages: data.messages,
                sessionId: data.sessionId
            });
        });
    });

    // Phase 5: Job polling handler for async generation
    class JobPollingHandler {
        constructor(wire) {
            this.wire = wire;
            this.pollingInterval = null;
            this.pollCount = 0;
            this.maxPolls = 720; // 10 minutes at 5 second intervals
        }

        startPolling(jobId) {
            console.log('Starting job polling for:', jobId);
            this.pollCount = 0;

            // Clear any existing interval
            this.stopPolling();

            // Poll every 2 seconds
            this.pollingInterval = setInterval(async () => {
                this.pollCount++;

                if (this.pollCount > this.maxPolls) {
                    console.error('Job polling timeout');
                    this.stopPolling();
                    this.wire.set('isLoading', false);
                    this.wire.set('error', 'Job timed out');
                    return;
                }

                try {
                    const result = await this.wire.checkJobStatus(jobId);

                    if (result.status === 'completed') {
                        console.log('Job completed:', result);
                        this.stopPolling();
                    } else if (result.status === 'failed') {
                        console.error('Job failed:', result.error);
                        this.stopPolling();
                    }
                    // Continue polling for 'processing' status
                } catch (error) {
                    console.error('Job polling error:', error);
                }
            }, 2000);
        }

        stopPolling() {
            if (this.pollingInterval) {
                clearInterval(this.pollingInterval);
                this.pollingInterval = null;
            }
        }
    }

    // Initialize job polling handler
    let jobPollingHandler = null;

    // Listen for job polling events
    document.addEventListener('livewire:initialized', () => {
        Livewire.on('startJobPolling', (eventData) => {
            // Livewire 3 passes data as first element of array
            const data = Array.isArray(eventData) ? eventData[0] : eventData;
            console.log('startJobPolling event received:', data);

            if (!jobPollingHandler) {
                jobPollingHandler = new JobPollingHandler(Livewire.find('{{ $this->getId() }}'));
            }
            jobPollingHandler.startPolling(data.jobId);
        });
    });

    // Loading progress component
    function loadingProgress() {
        return {
            startTime: null,
            elapsedTime: '0:00',
            stepIndex: 0,
            currentStep: 'Connecting to AI...',
            currentTip: 'Complex files may take up to a minute to generate.',
            steps: [
                { short: 'Connect', full: 'Connecting to AI...' },
                { short: 'Analyze', full: 'Analyzing your request...' },
                { short: 'Generate', full: 'Generating file content...' },
                { short: 'Finalize', full: 'Finalizing and saving...' }
            ],
            tips: [
                'Complex files may take up to a minute to generate.',
                'Excel files with formatting take a bit longer.',
                'You can create Word, Excel, PDF, and more!',
                'ZIMA is powered by Claude AI for accurate results.',
                'Tip: Be specific in your requests for better results.',
                'Prompt caching reduces costs by 50-90% on follow-up messages!'
            ],
            tipIndex: 0,
            interval: null,
            tipInterval: null,
            stepInterval: null,

            start() {
                this.startTime = Date.now();
                this.stepIndex = 0;
                this.tipIndex = 0;

                // Update elapsed time every second
                this.interval = setInterval(() => {
                    const elapsed = Math.floor((Date.now() - this.startTime) / 1000);
                    const mins = Math.floor(elapsed / 60);
                    const secs = elapsed % 60;
                    this.elapsedTime = `${mins}:${secs.toString().padStart(2, '0')}`;
                }, 1000);

                // Cycle through steps
                this.stepInterval = setInterval(() => {
                    if (this.stepIndex < this.steps.length - 1) {
                        this.stepIndex++;
                        this.currentStep = this.steps[this.stepIndex].full;
                    }
                }, 8000); // Move to next step every 8 seconds

                // Cycle tips every 5 seconds
                this.tipInterval = setInterval(() => {
                    this.tipIndex = (this.tipIndex + 1) % this.tips.length;
                    this.currentTip = this.tips[this.tipIndex];
                }, 5000);
            },

            destroy() {
                if (this.interval) clearInterval(this.interval);
                if (this.tipInterval) clearInterval(this.tipInterval);
                if (this.stepInterval) clearInterval(this.stepInterval);
            }
        }
    }
</script>
</div>
