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

    /* Markdown styles for AI responses */
    .ai-message-content h1, .ai-message-content h2, .ai-message-content h3 {
        font-weight: 600;
        margin-top: 1rem;
        margin-bottom: 0.5rem;
        color: #171717;
    }
    .ai-message-content h1 { font-size: 1.25rem; }
    .ai-message-content h2 { font-size: 1.125rem; }
    .ai-message-content h3 { font-size: 1rem; }

    .ai-message-content p {
        margin-bottom: 0.75rem;
    }

    .ai-message-content ul, .ai-message-content ol {
        margin: 0.5rem 0;
        padding-left: 1.5rem;
    }
    .ai-message-content li {
        margin-bottom: 0.25rem;
    }

    .ai-message-content table {
        border-collapse: collapse;
        margin: 0.75rem 0;
        font-size: 0.8125rem;
        width: 100%;
        border: 1px solid #e5e5e5;
        border-radius: 0.5rem;
        overflow: hidden;
    }
    .ai-message-content th, .ai-message-content td {
        border: 1px solid #e5e5e5;
        padding: 0.5rem 0.75rem;
        text-align: left;
    }
    .ai-message-content th {
        background-color: #f5f5f5;
        font-weight: 600;
    }
    .ai-message-content tr:nth-child(even) {
        background-color: #fafafa;
    }

    .ai-message-content code {
        background-color: #f5f5f5;
        padding: 0.125rem 0.375rem;
        border-radius: 0.25rem;
        font-size: 0.8125rem;
        font-family: ui-monospace, monospace;
    }
    .ai-message-content pre {
        background-color: #1f1f1f;
        color: #e5e5e5;
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        overflow-x: auto;
        margin: 0.75rem 0;
    }
    .ai-message-content pre code {
        background: none;
        padding: 0;
        color: inherit;
    }

    .ai-message-content blockquote {
        border-left: 3px solid #d4d4d4;
        margin: 0.75rem 0;
        padding-left: 1rem;
        color: #525252;
        font-style: italic;
    }

    .ai-message-content hr {
        border: none;
        border-top: 1px solid #e5e5e5;
        margin: 1rem 0;
    }

    .ai-message-content strong {
        font-weight: 600;
    }

    .ai-message-content a {
        color: #2563eb;
        text-decoration: underline;
    }
    .ai-message-content a:hover {
        color: #1d4ed8;
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

<div class="flex h-[calc(100vh-10rem)] gap-3" wire:poll.5s="refreshFiles">
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
    <div class="w-72 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden flex-shrink-0">
        <!-- Sidebar Header -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-neutral-200">
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Uploaded Files</h3>
                <p class="text-[11px] text-neutral-500">{{ $showAllUploads ? 'All sessions' : 'This chat only' }}</p>
            </div>
            <div class="flex items-center space-x-1">
                <!-- Toggle all uploads view -->
                <button
                    wire:click="toggleAllUploadsView"
                    class="p-1.5 {{ $showAllUploads ? 'text-neutral-900 bg-neutral-100' : 'text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100' }} rounded-xl transition"
                    title="{{ $showAllUploads ? 'Show this session only' : 'Show all sessions' }}"
                >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/>
                    </svg>
                </button>
                <!-- Refresh button -->
                <button
                    wire:click="{{ $showAllUploads ? 'loadAllUploadedFiles' : 'loadSessionFiles' }}"
                    class="p-1.5 text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100 rounded-xl transition"
                    title="Refresh"
                >
                    <svg class="w-4 h-4 {{ $isUploading ? 'animate-spin' : '' }}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                    </svg>
                </button>
            </div>
        </div>

        <!-- Upload Area -->
        <div class="px-3 py-3 border-b border-neutral-200"
             x-data="{ uploading: false, progress: 0 }"
             x-on:livewire-upload-start="uploading = true; progress = 0"
             x-on:livewire-upload-finish="uploading = false; progress = 100"
             x-on:livewire-upload-error="uploading = false; progress = 0"
             x-on:livewire-upload-progress="progress = $event.detail.progress">
            <form wire:submit="uploadFile">
                <label class="flex flex-col items-center justify-center w-full h-20 border-2 border-dashed border-neutral-300 rounded-xl cursor-pointer bg-neutral-50 hover:bg-neutral-100 hover:border-neutral-400 transition-all relative overflow-hidden">
                    <!-- Progress bar background -->
                    <div x-show="uploading || @js($isUploading)"
                         x-transition:enter="transition ease-out duration-200"
                         class="absolute inset-0 bg-neutral-100">
                        <div class="h-full bg-neutral-200 transition-all duration-300 ease-out"
                             :style="'width: ' + progress + '%'"></div>
                    </div>

                    <div class="flex flex-col items-center justify-center py-2 relative z-10">
                        <template x-if="uploading">
                            <div class="flex flex-col items-center">
                                <svg class="w-5 h-5 text-neutral-900 animate-spin" fill="none" viewBox="0 0 24 24">
                                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                                <p class="text-[11px] text-neutral-600 mt-1 font-medium">
                                    Uploading... <span x-text="Math.round(progress)"></span>%
                                </p>
                            </div>
                        </template>
                        <template x-if="!uploading">
                            <div class="flex flex-col items-center">
                                @if($isUploading)
                                    <svg class="w-5 h-5 text-neutral-900 animate-spin" fill="none" viewBox="0 0 24 24">
                                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    <p class="text-[11px] text-neutral-500 mt-1">Processing...</p>
                                @else
                                    <div class="w-8 h-8 bg-neutral-900 rounded-lg flex items-center justify-center mb-1">
                                        <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                                        </svg>
                                    </div>
                                    <p class="text-[11px] text-neutral-500">Click to upload · Max 10MB</p>
                                @endif
                            </div>
                        </template>
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
            @if($showAllUploads)
                {{-- ALL SESSIONS VIEW --}}
                @if(empty($allUploadedFiles) || count($allUploadedFiles) === 0)
                    <div class="flex flex-col items-center justify-center h-full text-center py-8">
                        <div class="w-12 h-12 bg-neutral-100 rounded-xl flex items-center justify-center mb-3">
                            <svg class="w-6 h-6 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                            </svg>
                        </div>
                        <p class="text-[13px] text-neutral-500">No uploaded files</p>
                        <p class="text-[11px] text-neutral-400 mt-1">Upload files to add context</p>
                    </div>
                @else
                    <div class="space-y-3">
                        @foreach($allUploadedFiles as $session)
                            <div wire:key="upload-session-{{ $session['sessionId'] }}" class="border border-neutral-200 rounded-xl overflow-hidden">
                                <!-- Session Header -->
                                <div class="bg-neutral-50 px-3 py-2 flex items-center justify-between">
                                    <div class="flex items-center space-x-2 min-w-0">
                                        <div class="w-5 h-5 bg-neutral-900 rounded flex items-center justify-center flex-shrink-0">
                                            <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                                            </svg>
                                        </div>
                                        <div class="min-w-0">
                                            <p class="text-[11px] font-medium text-neutral-700 truncate" title="{{ $session['sessionId'] }}">
                                                {{ Str::limit($session['sessionId'], 20) }}
                                            </p>
                                            <p class="text-[10px] text-neutral-500">{{ $session['fileCount'] }} {{ $session['fileCount'] === 1 ? 'file' : 'files' }}</p>
                                        </div>
                                    </div>
                                    @if($session['sessionId'] === $currentSessionId)
                                        <span class="text-[9px] bg-neutral-900 text-white px-1.5 py-0.5 rounded">Current</span>
                                    @endif
                                </div>
                                <!-- Session Files -->
                                <div class="p-2 space-y-1">
                                    @foreach($session['files'] as $group)
                                        <div class="flex items-center justify-between bg-white rounded-lg px-2 py-1.5 hover:bg-neutral-50 transition">
                                            <div class="flex items-center space-x-2 min-w-0 flex-1">
                                                <div class="w-6 h-6 bg-neutral-200 rounded flex items-center justify-center flex-shrink-0">
                                                    <svg class="w-3 h-3 text-neutral-600" fill="currentColor" viewBox="0 0 20 20">
                                                        <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd"/>
                                                    </svg>
                                                </div>
                                                <div class="min-w-0 flex-1">
                                                    <p class="text-[11px] font-medium text-neutral-800 truncate" title="{{ $group['baseName'] }}">{{ $group['baseName'] }}</p>
                                                    <p class="text-[10px] text-neutral-500">{{ $group['latestVersion']['sizeFormatted'] ?? 'Unknown' }}</p>
                                                </div>
                                            </div>
                                            <a
                                                href="{{ $group['latestVersion']['downloadUrl'] ?? '' }}"
                                                target="_blank"
                                                class="p-1.5 text-neutral-400 hover:text-neutral-900 rounded-lg transition flex-shrink-0"
                                                title="Download"
                                            >
                                                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                                </svg>
                                            </a>
                                        </div>
                                    @endforeach
                                </div>
                            </div>
                        @endforeach
                    </div>
                @endif
            @elseif(empty($sessionFiles) || count($sessionFiles) === 0)
                {{-- CURRENT SESSION VIEW - EMPTY --}}
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
                {{-- CURRENT SESSION VIEW - WITH FILES --}}
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
                                        href="{{ $apiUrl }}/api/files/session/{{ $currentSessionId }}/{{ $fileName }}/download"
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
            @if($showAllUploads)
                @php
                    $sessionCount = is_array($allUploadedFiles) ? count($allUploadedFiles) : 0;
                    $totalAllUploads = is_array($allUploadedFiles) ? collect($allUploadedFiles)->sum('fileCount') : 0;
                @endphp
                <p class="text-[11px] text-neutral-500">
                    {{ $totalAllUploads }} {{ $totalAllUploads === 1 ? 'file' : 'files' }} in {{ $sessionCount }} {{ $sessionCount === 1 ? 'session' : 'sessions' }}
                </p>
                <p class="text-[11px] text-neutral-400 mt-0.5">
                    All uploaded files
                </p>
            @else
                <p class="text-[11px] text-neutral-500">
                    @php
                        $uploadCount = is_array($sessionFiles) ? count($sessionFiles) : 0;
                    @endphp
                    {{ $uploadCount }} {{ $uploadCount === 1 ? 'file' : 'files' }} uploaded
                </p>
                <p class="text-[11px] text-neutral-400 mt-0.5">
                    Files are included in AI context
                </p>
            @endif
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
                    <p class="text-[15px] font-semibold text-neutral-900">Process Documents AI</p>
                    <p class="text-[11px] text-neutral-500">Document processing assistant</p>
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
                                                    $downloadUrl = "{$apiUrl}/api/files/generated/{$currentSessionId}/" . urlencode($fileName) . "/download";
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
                  <!-- Agent Mode Toggle -->
                  <button
                      type="button"
                      wire:click="toggleAgentMode"
                      class="flex-shrink-0 h-10 px-3 rounded-full flex items-center gap-1.5 transition-all {{ $agentMode ? 'bg-neutral-900 text-white' : 'bg-white text-neutral-500 hover:bg-neutral-100 border border-neutral-200' }}"
                      title="{{ $agentMode ? 'Agent Mode: ON - Autonomous tool execution' : 'Agent Mode: OFF - Standard generation' }}"
                  >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                      </svg>
                      <span class="text-xs font-medium">{{ $agentMode ? 'Agent' : 'Agent' }}</span>
                  </button>
                  <div class="relative h-full w-full min-w-[200px]">
                    <textarea
                      rows="1"
                      placeholder="{{ $agentMode ? 'Describe what you want the agent to create...' : 'Describe the file you want to generate...' }}"
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

    <!-- Files Sidebar - All Generated Files -->
    <div class="w-72 flex flex-col bg-white rounded-2xl shadow-md overflow-hidden flex-shrink-0">
        <!-- Sidebar Header with Toggle -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-neutral-200">
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Generated Files</h3>
                <p class="text-[11px] text-neutral-500">{{ $showAllFiles ? 'All sessions' : 'This chat only' }}</p>
            </div>
            <div class="flex items-center space-x-1">
                <button
                    wire:click="toggleAllFilesView"
                    class="p-1.5 text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100 rounded-xl transition {{ $showAllFiles ? 'bg-neutral-900 text-white' : '' }}"
                    title="{{ $showAllFiles ? 'Show current session' : 'Show all files' }}"
                >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/>
                    </svg>
                </button>
                <button
                    wire:click="{{ $showAllFiles ? 'loadAllGeneratedFiles' : 'loadFiles' }}"
                    class="p-1.5 text-neutral-400 hover:text-neutral-900 hover:bg-neutral-100 rounded-xl transition"
                    title="Refresh"
                >
                    <svg class="w-4 h-4 {{ $isLoading ? 'animate-spin' : '' }}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                    </svg>
                </button>
            </div>
        </div>

        <!-- Files List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
            @if($showAllFiles)
                {{-- ALL FILES VIEW --}}
                @if(empty($allGeneratedFiles) || count($allGeneratedFiles) === 0)
                    <div class="flex flex-col items-center justify-center h-full text-center py-8">
                        <div class="w-12 h-12 bg-neutral-100 rounded-xl flex items-center justify-center mb-3">
                            <svg class="w-6 h-6 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M5 19a2 2 0 01-2-2V7a2 2 0 012-2h4l2 2h4a2 2 0 012 2v1M5 19h14a2 2 0 002-2v-5a2 2 0 00-2-2H9a2 2 0 00-2 2v5a2 2 0 01-2 2z"/>
                            </svg>
                        </div>
                        <p class="text-[13px] text-neutral-500">No files yet</p>
                        <p class="text-[11px] text-neutral-400 mt-1">Generated files from all sessions will appear here</p>
                    </div>
                @else
                    <div class="space-y-3">
                        @foreach($allGeneratedFiles as $session)
                            <div class="bg-neutral-50 rounded-xl overflow-hidden" wire:key="session-{{ $session['sessionId'] }}">
                                <!-- Session Header -->
                                <div class="px-3 py-2 bg-neutral-100 border-b border-neutral-200">
                                    <div class="flex items-center justify-between">
                                        <div>
                                            <p class="text-[12px] font-medium text-neutral-700 truncate" title="{{ $session['sessionId'] }}">
                                                {{ Str::limit($session['sessionId'], 20) }}
                                            </p>
                                            <p class="text-[10px] text-neutral-500">{{ $session['fileCount'] }} files</p>
                                        </div>
                                        @if($session['sessionId'] === $currentSessionId)
                                            <span class="px-1.5 py-0.5 text-[9px] font-medium bg-neutral-900 text-white rounded">Current</span>
                                        @endif
                                    </div>
                                </div>
                                <!-- Session Files -->
                                <div class="p-2 space-y-1">
                                    @foreach($session['files'] as $group)
                                        <div class="flex items-center justify-between p-2 rounded-lg hover:bg-neutral-100 transition">
                                            <div class="flex items-center space-x-2 min-w-0 flex-1">
                                                <div class="w-8 h-8 bg-neutral-900 rounded-lg flex items-center justify-center flex-shrink-0">
                                                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                                                        <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd"/>
                                                    </svg>
                                                </div>
                                                <div class="min-w-0 flex-1">
                                                    <p class="text-[12px] font-medium text-neutral-800 truncate" title="{{ $group['baseName'] }}">{{ $group['baseName'] }}</p>
                                                    <p class="text-[10px] text-neutral-500">{{ $group['latestVersion']['sizeFormatted'] ?? 'Unknown' }}</p>
                                                </div>
                                            </div>
                                            <a
                                                href="{{ $group['latestVersion']['downloadUrl'] ?? '' }}"
                                                target="_blank"
                                                class="p-1.5 text-neutral-400 hover:text-neutral-900 rounded-lg transition flex-shrink-0"
                                                title="Download"
                                            >
                                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                                                </svg>
                                            </a>
                                        </div>
                                    @endforeach
                                </div>
                            </div>
                        @endforeach
                    </div>
                @endif
            @elseif(empty($fileGroups) || count($fileGroups) === 0)
                {{-- CURRENT SESSION VIEW - EMPTY --}}
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
                                            href="{{ $apiUrl }}/api/files/generated/{{ $currentSessionId }}/{{ urlencode($group['latestVersion']['fileName'] ?? $group['baseName']) }}/download"
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
                                                    href="{{ $apiUrl }}/api/files/generated/{{ $currentSessionId }}/{{ urlencode($version['fileName']) }}/download"
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
            @if($showAllFiles)
                @php
                    $sessionCount = is_array($allGeneratedFiles) ? count($allGeneratedFiles) : 0;
                    $totalAllFiles = is_array($allGeneratedFiles) ? collect($allGeneratedFiles)->sum('fileCount') : 0;
                @endphp
                <p class="text-[11px] text-neutral-500">
                    {{ $totalAllFiles }} {{ $totalAllFiles === 1 ? 'file' : 'files' }} in {{ $sessionCount }} {{ $sessionCount === 1 ? 'session' : 'sessions' }}
                </p>
                <p class="text-[11px] text-neutral-400 mt-0.5">
                    All generated files
                </p>
            @else
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
            @endif
        </div>
    </div>
</div>

<!-- Quick Tools Section -->
<div class="mt-4 bg-white rounded-2xl shadow-md overflow-hidden" x-data="{
    toolsOpen: true,
    activeCategory: 'pdf',
    selectedTool: null,
    toolModal: false,
    toolInputs: {},
    uploadedFiles: {},
    uploadingField: null,
    toolResult: null,
    toolLoading: false,
    toolError: null,

    categories: {
        pdf: { name: 'PDF', icon: 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z' },
        excel: { name: 'Excel', icon: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
        word: { name: 'Word', icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
        text: { name: 'Text', icon: 'M4 6h16M4 12h16m-7 6h7' },
        json: { name: 'JSON', icon: 'M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4' },
        powerpoint: { name: 'PowerPoint', icon: 'M8 13v-1m4 1v-3m4 3V8M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z' },
        ocr: { name: 'OCR', icon: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z' },
        image: { name: 'Image', icon: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z' },
        video: { name: 'Video', icon: 'M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z' },
        audio: { name: 'Audio', icon: 'M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3' },
        developer: { name: 'Developer', icon: 'M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4' },
        calculator: { name: 'Calculator', icon: 'M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z' },
        seo: { name: 'SEO', icon: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z' },
        security: { name: 'Security', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
        file: { name: 'File', icon: 'M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4' },
        conversion: { name: 'Convert', icon: 'M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4' }
    },

    tools: {
        pdf: [
            { name: 'merge_pdf', label: 'Merge PDFs', desc: 'Combine multiple PDFs', inputs: [{name: 'files', label: 'PDF Files', type: 'files', accept: '.pdf', multiple: true, required: true}] },
            { name: 'split_pdf', label: 'Split PDF', desc: 'Split by pages/ranges', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'mode', label: 'Mode (pages/ranges/every_n)', type: 'text'}] },
            { name: 'extract_pages', label: 'Extract Pages', desc: 'Extract specific pages', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'pages', label: 'Pages (e.g. 1,3,5-7)', type: 'text', required: true}] },
            { name: 'remove_pages', label: 'Remove Pages', desc: 'Remove specific pages', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'pages', label: 'Pages to remove', type: 'text', required: true}] },
            { name: 'rotate_pdf', label: 'Rotate PDF', desc: 'Rotate pages', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'rotation', label: 'Rotation (90/180/270)', type: 'number', required: true}] },
            { name: 'add_watermark', label: 'Add Watermark', desc: 'Text watermark', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'text', label: 'Watermark text', type: 'text', required: true}, {name: 'opacity', label: 'Opacity (0-1)', type: 'number'}] },
            { name: 'add_page_numbers', label: 'Page Numbers', desc: 'Add page numbers', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'position', label: 'Position (bottom/top)', type: 'text'}] },
            { name: 'compress_pdf', label: 'Compress PDF', desc: 'Reduce file size', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'get_pdf_info', label: 'PDF Info', desc: 'Get metadata', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'protect_pdf', label: 'Protect PDF', desc: 'Password protect', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'owner_password', label: 'Owner password', type: 'text', required: true}, {name: 'user_password', label: 'User password', type: 'text'}] },
            { name: 'unlock_pdf', label: 'Unlock PDF', desc: 'Remove password', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'pdf_to_text', label: 'PDF to Text', desc: 'Extract text', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'text_to_pdf', label: 'Text to PDF', desc: 'Convert text to PDF', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt', required: true}, {name: 'title', label: 'Title', type: 'text'}] },
            { name: 'compare_pdf', label: 'Compare PDFs', desc: 'Compare two PDFs', inputs: [{name: 'file1', label: 'First PDF', type: 'file', accept: '.pdf', required: true}, {name: 'file2', label: 'Second PDF', type: 'file', accept: '.pdf', required: true}] },
            { name: 'crop_pdf', label: 'Crop PDF', desc: 'Crop page margins', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'margin', label: 'Margin to crop', type: 'number'}] },
            { name: 'set_pdf_metadata', label: 'Set Metadata', desc: 'Set title, author', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'title', label: 'Title', type: 'text'}, {name: 'author', label: 'Author', type: 'text'}] },
            { name: 'html_to_pdf', label: 'HTML to PDF', desc: 'Convert HTML to PDF', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'redact_pdf', label: 'Redact PDF', desc: 'Blackout areas', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'areas', label: 'Areas JSON', type: 'textarea', required: true}] },
            { name: 'repair_pdf', label: 'Repair PDF', desc: 'Fix corrupted PDF', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'sign_pdf', label: 'Sign PDF', desc: 'Digital signature', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'certificate', label: 'Certificate (.pfx)', type: 'file', accept: '.pfx,.p12', required: true}, {name: 'password', label: 'Certificate password', type: 'text', required: true}] },
            { name: 'verify_pdf_signature', label: 'Verify Signature', desc: 'Verify PDF signatures', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'create_test_certificate', label: 'Create Certificate', desc: 'Create test certificate', inputs: [{name: 'common_name', label: 'Name', type: 'text', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'add_sticky_note', label: 'Add Sticky Note', desc: 'Add annotation', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'content', label: 'Note content', type: 'text', required: true}] },
            { name: 'add_highlight', label: 'Add Highlight', desc: 'Highlight annotation', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}] },
            { name: 'add_underline', label: 'Add Underline', desc: 'Underline annotation', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}] },
            { name: 'add_strikethrough', label: 'Add Strikethrough', desc: 'Strikethrough annotation', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}] },
            { name: 'add_free_text', label: 'Add Free Text', desc: 'Free text annotation', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'text', label: 'Text', type: 'text', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}] },
            { name: 'add_link', label: 'Add Link', desc: 'URL or page link', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'page', label: 'Page', type: 'number', required: true}, {name: 'url', label: 'URL', type: 'text', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}] },
            { name: 'add_stamp', label: 'Add Stamp', desc: 'Approved/Draft/Final', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'stamp_type', label: 'Type (Approved/Draft/Final)', type: 'text', required: true}] },
            { name: 'list_annotations', label: 'List Annotations', desc: 'List all annotations', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'remove_annotations', label: 'Remove Annotations', desc: 'Remove annotations', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'flatten_annotations', label: 'Flatten Annotations', desc: 'Flatten into content', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
        ],
        excel: [
            { name: 'merge_workbooks', label: 'Merge Workbooks', desc: 'Combine Excel files', inputs: [{name: 'files', label: 'Excel Files', type: 'files', accept: '.xlsx,.xls', multiple: true, required: true}] },
            { name: 'split_workbook', label: 'Split Workbook', desc: 'Split by sheets', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'excel_to_csv', label: 'Excel to CSV', desc: 'Convert to CSV', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'sheet_name', label: 'Sheet name', type: 'text'}] },
            { name: 'excel_to_json', label: 'Excel to JSON', desc: 'Convert to JSON', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'sheet_name', label: 'Sheet name', type: 'text'}] },
            { name: 'csv_to_excel', label: 'CSV to Excel', desc: 'Convert CSV', inputs: [{name: 'file', label: 'CSV File', type: 'file', accept: '.csv', required: true}] },
            { name: 'json_to_excel', label: 'JSON to Excel', desc: 'Convert JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'clean_excel', label: 'Clean Excel', desc: 'Remove blanks', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'get_excel_info', label: 'Excel Info', desc: 'Get workbook info', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'extract_sheets', label: 'Extract Sheets', desc: 'Extract specific sheets', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'sheets', label: 'Sheet names/indices', type: 'text', required: true}] },
            { name: 'reorder_sheets', label: 'Reorder Sheets', desc: 'Reorder sheets', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'order', label: 'New order (comma-sep)', type: 'text', required: true}] },
            { name: 'rename_sheets', label: 'Rename Sheets', desc: 'Rename sheets', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'renames', label: 'Renames JSON', type: 'textarea', required: true}] },
            { name: 'delete_sheets', label: 'Delete Sheets', desc: 'Delete sheets', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'sheets', label: 'Sheets to delete', type: 'text', required: true}] },
            { name: 'copy_sheet', label: 'Copy Sheet', desc: 'Copy sheet', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'sheet', label: 'Sheet to copy', type: 'text', required: true}, {name: 'new_name', label: 'New name', type: 'text', required: true}] },
            { name: 'find_replace_excel', label: 'Find & Replace', desc: 'Find and replace', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'find', label: 'Find text', type: 'text', required: true}, {name: 'replace', label: 'Replace with', type: 'text', required: true}] },
            { name: 'excel_to_html', label: 'Excel to HTML', desc: 'Convert to HTML table', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'add_formulas', label: 'Add Formulas', desc: 'Add formulas to cells', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'formulas', label: 'Formulas JSON', type: 'textarea', required: true}] },
            { name: 'text_to_excel', label: 'Text to Excel', desc: 'Convert text to Excel', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt', required: true}, {name: 'delimiter', label: 'Delimiter', type: 'text'}] },
            { name: 'add_chart', label: 'Add Chart', desc: 'Insert chart', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'data_range', label: 'Data range (A1:B10)', type: 'text', required: true}, {name: 'chart_type', label: 'Chart type', type: 'text'}] },
            { name: 'create_pivot_summary', label: 'Pivot Summary', desc: 'Create pivot summary', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'group_by', label: 'Group by column', type: 'text', required: true}, {name: 'aggregate', label: 'Aggregate column', type: 'text', required: true}] },
            { name: 'validate_excel_data', label: 'Validate Data', desc: 'Validate against rules', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'rules', label: 'Rules JSON', type: 'textarea', required: true}] },
            { name: 'compress_excel', label: 'Compress Excel', desc: 'Reduce file size', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'repair_excel', label: 'Repair Excel', desc: 'Repair corrupted file', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'protect_workbook', label: 'Protect Workbook', desc: 'Password protect', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'unprotect_workbook', label: 'Unprotect Workbook', desc: 'Remove protection', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'add_conditional_formatting', label: 'Conditional Format', desc: 'Add formatting rules', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}, {name: 'range', label: 'Range (A1:B10)', type: 'text', required: true}, {name: 'rule', label: 'Rule JSON', type: 'textarea', required: true}] },
            { name: 'clear_conditional_formatting', label: 'Clear Formatting', desc: 'Clear formatting', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
        ],
        word: [
            { name: 'merge_word', label: 'Merge Documents', desc: 'Combine Word files', inputs: [{name: 'files', label: 'Word Files', type: 'files', accept: '.docx,.doc', multiple: true, required: true}] },
            { name: 'split_word', label: 'Split Document', desc: 'Split by sections', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'mode', label: 'Mode (sections/headings)', type: 'text'}] },
            { name: 'extract_word_sections', label: 'Extract Sections', desc: 'Extract sections', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'sections', label: 'Section indices', type: 'text', required: true}] },
            { name: 'remove_word_sections', label: 'Remove Sections', desc: 'Remove sections', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'sections', label: 'Section indices', type: 'text', required: true}] },
            { name: 'word_to_text', label: 'Word to Text', desc: 'Extract text', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'word_to_html', label: 'Word to HTML', desc: 'Convert to HTML', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'word_to_json', label: 'Word to JSON', desc: 'Convert to JSON', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'text_to_word', label: 'Text to Word', desc: 'Convert text to Word', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt', required: true}] },
            { name: 'find_replace_word', label: 'Find & Replace', desc: 'Find and replace', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'find', label: 'Find text', type: 'text', required: true}, {name: 'replace', label: 'Replace with', type: 'text', required: true}] },
            { name: 'add_header_footer', label: 'Header/Footer', desc: 'Add header/footer', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'header', label: 'Header text', type: 'text'}, {name: 'footer', label: 'Footer text', type: 'text'}] },
            { name: 'get_word_info', label: 'Document Info', desc: 'Get metadata', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'compare_word', label: 'Compare Docs', desc: 'Compare two docs', inputs: [{name: 'file1', label: 'First Doc', type: 'file', accept: '.docx,.doc', required: true}, {name: 'file2', label: 'Second Doc', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'clean_word_formatting', label: 'Clean Formatting', desc: 'Remove formatting', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'word_to_pdf', label: 'Word to PDF', desc: 'Convert to PDF', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'mail_merge', label: 'Mail Merge', desc: 'Merge with data', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'data', label: 'Data JSON', type: 'textarea', required: true}] },
            { name: 'accept_track_changes', label: 'Track Changes', desc: 'Accept/reject changes', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'action', label: 'Action (accept/reject)', type: 'text', required: true}] },
            { name: 'add_watermark_word', label: 'Add Watermark', desc: 'Text watermark', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'text', label: 'Watermark text', type: 'text', required: true}] },
            { name: 'manage_word_tables', label: 'Manage Tables', desc: 'Extract/remove tables', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'action', label: 'Action (extract/remove/count)', type: 'text', required: true}] },
            { name: 'manage_word_comments', label: 'Manage Comments', desc: 'Extract/remove comments', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'action', label: 'Action (extract/remove)', type: 'text', required: true}] },
            { name: 'add_page_numbers_word', label: 'Page Numbers', desc: 'Add page numbers', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'position', label: 'Position (top/bottom)', type: 'text'}] },
            { name: 'compress_word', label: 'Compress Document', desc: 'Reduce file size', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'repair_word', label: 'Repair Document', desc: 'Repair corrupted doc', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'html_to_word', label: 'HTML to Word', desc: 'Convert HTML to Word', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'protect_word', label: 'Protect Document', desc: 'Password protect', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'unprotect_word', label: 'Unprotect Document', desc: 'Remove protection', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'sign_word', label: 'Sign Document', desc: 'Digital signature', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}, {name: 'certificate', label: 'Certificate (.pfx)', type: 'file', accept: '.pfx,.p12', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'verify_word_signature', label: 'Verify Signature', desc: 'Verify signature', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
        ],
        text: [
            { name: 'merge_text', label: 'Merge Files', desc: 'Combine text files', inputs: [{name: 'files', label: 'Text Files', type: 'files', accept: '.txt,.md,.log', multiple: true, required: true}] },
            { name: 'split_text', label: 'Split Text', desc: 'Split by lines/pattern', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'mode', label: 'Mode (lines/pattern/size)', type: 'text', required: true}, {name: 'value', label: 'Value', type: 'text', required: true}] },
            { name: 'find_replace', label: 'Find & Replace', desc: 'Find and replace', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'find', label: 'Find text', type: 'text', required: true}, {name: 'replace', label: 'Replace with', type: 'text', required: true}] },
            { name: 'remove_duplicates', label: 'Remove Duplicates', desc: 'Remove duplicate lines', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'sort_lines', label: 'Sort Lines', desc: 'Sort alphabetically', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'order', label: 'Order (asc/desc)', type: 'text'}] },
            { name: 'convert_case', label: 'Convert Case', desc: 'Change text case', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'case_type', label: 'Case (upper/lower/title)', type: 'text', required: true}] },
            { name: 'add_line_numbers', label: 'Add Line Numbers', desc: 'Add line numbers', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'compare_files', label: 'Compare Files', desc: 'Diff two files', inputs: [{name: 'file1', label: 'First File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'file2', label: 'Second File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'clean_whitespace', label: 'Clean Whitespace', desc: 'Remove extra spaces', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'reverse_text', label: 'Reverse Text', desc: 'Reverse lines/chars', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'mode', label: 'Mode (lines/chars)', type: 'text'}] },
            { name: 'convert_encoding', label: 'Convert Encoding', desc: 'UTF-8/ASCII/etc', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'encoding', label: 'Target encoding', type: 'text', required: true}] },
            { name: 'standardize_line_endings', label: 'Line Endings', desc: 'LF/CRLF/CR', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'style', label: 'Style (lf/crlf/cr)', type: 'text', required: true}] },
            { name: 'wrap_text', label: 'Wrap Text', desc: 'Wrap at width', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'width', label: 'Width', type: 'number', required: true}] },
            { name: 'text_to_json', label: 'Text to JSON', desc: 'Convert to JSON', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'extract_columns', label: 'Extract Columns', desc: 'Extract delimited cols', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.csv,.log', required: true}, {name: 'columns', label: 'Columns (1,2,3)', type: 'text', required: true}, {name: 'delimiter', label: 'Delimiter', type: 'text'}] },
            { name: 'filter_lines', label: 'Filter Lines', desc: 'Filter by pattern', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'pattern', label: 'Pattern', type: 'text', required: true}] },
            { name: 'get_text_stats', label: 'Text Statistics', desc: 'Word count etc', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'text_to_html', label: 'Text to HTML', desc: 'Convert to HTML', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'text_to_xml', label: 'Text to XML', desc: 'Convert to XML', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'html_to_text', label: 'HTML to Text', desc: 'Strip HTML tags', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'xml_to_text', label: 'XML to Text', desc: 'Convert XML to text', inputs: [{name: 'file', label: 'XML File', type: 'file', accept: '.xml', required: true}] },
            { name: 'compress_text', label: 'Compress Text', desc: 'GZIP compression', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}] },
            { name: 'decompress_text', label: 'Decompress', desc: 'GZIP decompress', inputs: [{name: 'file', label: 'GZIP File', type: 'file', accept: '.gz', required: true}] },
            { name: 'encrypt_text', label: 'Encrypt Text', desc: 'AES encryption', inputs: [{name: 'file', label: 'Text File', type: 'file', accept: '.txt,.md,.log', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'decrypt_text', label: 'Decrypt Text', desc: 'AES decryption', inputs: [{name: 'file', label: 'Encrypted File', type: 'file', accept: '.txt,.enc', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'calculate_checksum', label: 'Checksum', desc: 'MD5/SHA1/SHA256', inputs: [{name: 'file', label: 'File', type: 'file', accept: '*', required: true}, {name: 'algorithm', label: 'Algorithm (md5/sha1/sha256)', type: 'text'}] },
            { name: 'validate_checksum', label: 'Validate Checksum', desc: 'Verify checksum', inputs: [{name: 'file', label: 'File', type: 'file', accept: '*', required: true}, {name: 'checksum', label: 'Expected checksum', type: 'text', required: true}] },
            { name: 'word_counter', label: 'Word Counter', desc: 'Count words/chars/sentences', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}] },
            { name: 'text_to_binary', label: 'Text to Binary', desc: 'Convert text↔binary', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}, {name: 'direction', label: 'Direction (to_binary/from_binary)', type: 'text', required: true}] },
            { name: 'generate_random_string', label: 'Random String', desc: 'Generate random strings', inputs: [{name: 'length', label: 'Length', type: 'number', required: true}, {name: 'charset', label: 'Charset (alpha/numeric/both/custom)', type: 'text'}] },
            { name: 'extract_emails', label: 'Extract Emails', desc: 'Extract email addresses', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}] },
            { name: 'extract_urls', label: 'Extract URLs', desc: 'Extract URLs from text', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}] },
            { name: 'extract_numbers', label: 'Extract Numbers', desc: 'Extract numbers from text', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}] },
            { name: 'remove_html_tags', label: 'Remove HTML Tags', desc: 'Strip all HTML tags', inputs: [{name: 'text', label: 'HTML Text', type: 'textarea', required: true}] },
            { name: 'escape_html', label: 'Escape HTML', desc: 'Escape HTML entities', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}] },
            { name: 'unescape_html', label: 'Unescape HTML', desc: 'Decode HTML entities', inputs: [{name: 'text', label: 'Escaped Text', type: 'textarea', required: true}] },
        ],
        json: [
            { name: 'format_json', label: 'Format JSON', desc: 'Beautify JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'indent', label: 'Indent spaces', type: 'number'}] },
            { name: 'minify_json', label: 'Minify JSON', desc: 'Remove whitespace', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'validate_json', label: 'Validate JSON', desc: 'Check syntax', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'merge_json', label: 'Merge JSON', desc: 'Combine files', inputs: [{name: 'files', label: 'JSON Files', type: 'files', accept: '.json', multiple: true, required: true}, {name: 'mode', label: 'Mode (array/object)', type: 'text'}] },
            { name: 'split_json', label: 'Split JSON', desc: 'Split large array', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'chunk_size', label: 'Chunk size', type: 'number', required: true}] },
            { name: 'query_json', label: 'Query JSON', desc: 'JSONPath query', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'path', label: 'JSONPath query', type: 'text', required: true}] },
            { name: 'sort_json_keys', label: 'Sort Keys', desc: 'Sort object keys', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'flatten_json', label: 'Flatten JSON', desc: 'Flatten nested JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'json_to_csv', label: 'JSON to CSV', desc: 'Convert to CSV', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'remove_json_keys', label: 'Remove Keys', desc: 'Remove specific keys', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'keys', label: 'Keys to remove', type: 'text', required: true}] },
            { name: 'csv_to_json', label: 'CSV to JSON', desc: 'Convert CSV', inputs: [{name: 'file', label: 'CSV File', type: 'file', accept: '.csv', required: true}] },
            { name: 'xml_to_json', label: 'XML to JSON', desc: 'Convert XML', inputs: [{name: 'file', label: 'XML File', type: 'file', accept: '.xml', required: true}] },
            { name: 'json_to_xml', label: 'JSON to XML', desc: 'Convert to XML', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'validate_schema', label: 'Validate Schema', desc: 'Validate against schema', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'schema', label: 'Schema File', type: 'file', accept: '.json', required: true}] },
            { name: 'get_json_stats', label: 'JSON Stats', desc: 'Get statistics', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'remove_duplicates_json', label: 'Remove Duplicates', desc: 'Remove duplicate objects', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'transform_json', label: 'Transform JSON', desc: 'Transform using mappings', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'mappings', label: 'Mappings JSON', type: 'textarea', required: true}] },
            { name: 'yaml_to_json', label: 'YAML to JSON', desc: 'Convert YAML', inputs: [{name: 'file', label: 'YAML File', type: 'file', accept: '.yaml,.yml', required: true}] },
            { name: 'json_to_yaml', label: 'JSON to YAML', desc: 'Convert to YAML', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'json_to_html_table', label: 'JSON to HTML', desc: 'Convert to HTML table', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'encrypt_json', label: 'Encrypt JSON', desc: 'AES encryption', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'decrypt_json', label: 'Decrypt JSON', desc: 'AES decryption', inputs: [{name: 'file', label: 'Encrypted File', type: 'file', accept: '.json,.enc', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'array_operations', label: 'Array Operations', desc: 'Concat/slice/reverse', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'operation', label: 'Operation', type: 'text', required: true}] },
            { name: 'repair_json', label: 'Repair JSON', desc: 'Fix malformed JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json,.txt', required: true}] },
            { name: 'sql_to_json', label: 'SQL to JSON', desc: 'Convert SQL schema', inputs: [{name: 'file', label: 'SQL File', type: 'file', accept: '.sql', required: true}] },
            { name: 'json_to_pdf', label: 'JSON to PDF', desc: 'Create PDF from JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'title', label: 'Title', type: 'text'}] },
            { name: 'sign_json', label: 'Sign JSON', desc: 'Digital signature', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}, {name: 'key', label: 'Key/Secret', type: 'text', required: true}, {name: 'algorithm', label: 'Algorithm (hmac/rsa)', type: 'text'}] },
            { name: 'verify_json_signature', label: 'Verify Signature', desc: 'Verify signature', inputs: [{name: 'file', label: 'Signed JSON', type: 'file', accept: '.json', required: true}, {name: 'key', label: 'Key', type: 'text', required: true}] },
            { name: 'generate_json_signing_keys', label: 'Generate Keys', desc: 'Generate RSA keys', inputs: [{name: 'key_size', label: 'Key size (2048/4096)', type: 'number'}] },
        ],
        powerpoint: [
            { name: 'merge_ppt', label: 'Merge Presentations', desc: 'Combine PPT files', inputs: [{name: 'files', label: 'PPT Files', type: 'files', accept: '.pptx,.ppt', multiple: true, required: true}] },
            { name: 'split_ppt', label: 'Split Presentation', desc: 'Split by slides', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'slides_per_file', label: 'Slides per file', type: 'number'}] },
            { name: 'extract_slides', label: 'Extract Slides', desc: 'Extract specific slides', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'slides', label: 'Slides (1,3,5)', type: 'text', required: true}] },
            { name: 'remove_slides', label: 'Remove Slides', desc: 'Remove slides', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'slides', label: 'Slides to remove', type: 'text', required: true}] },
            { name: 'reorder_slides', label: 'Reorder Slides', desc: 'Reorder slides', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'order', label: 'New order (1,3,2)', type: 'text', required: true}] },
            { name: 'ppt_to_text', label: 'PPT to Text', desc: 'Extract text', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'ppt_to_json', label: 'PPT to JSON', desc: 'Convert to JSON', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'get_ppt_info', label: 'Presentation Info', desc: 'Get metadata', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'duplicate_slides', label: 'Duplicate Slides', desc: 'Duplicate slides', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'slides', label: 'Slides to duplicate', type: 'text', required: true}] },
            { name: 'ppt_to_pdf', label: 'PPT to PDF', desc: 'Convert to PDF', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'add_slide', label: 'Add Slide', desc: 'Add new slide', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'title', label: 'Slide title', type: 'text', required: true}, {name: 'content', label: 'Slide content', type: 'textarea'}] },
            { name: 'add_watermark_ppt', label: 'Add Watermark', desc: 'Text watermark', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'text', label: 'Watermark text', type: 'text', required: true}] },
            { name: 'extract_ppt_notes', label: 'Extract Notes', desc: 'Extract speaker notes', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'set_transitions', label: 'Set Transitions', desc: 'Set slide transitions', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'transition', label: 'Transition type', type: 'text', required: true}] },
            { name: 'find_replace_ppt', label: 'Find & Replace', desc: 'Find and replace', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'find', label: 'Find text', type: 'text', required: true}, {name: 'replace', label: 'Replace with', type: 'text', required: true}] },
            { name: 'extract_ppt_images', label: 'Extract Images', desc: 'Extract all images', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'compress_ppt', label: 'Compress PPT', desc: 'Reduce file size', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'repair_ppt', label: 'Repair PPT', desc: 'Repair corrupted PPT', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}] },
            { name: 'ppt_to_images', label: 'PPT to Images', desc: 'Export as images', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'format', label: 'Format (png/jpg)', type: 'text'}] },
            { name: 'ppt_to_video', label: 'PPT to Video', desc: 'Convert to video', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'duration', label: 'Slide duration (sec)', type: 'number'}] },
            { name: 'add_animations', label: 'Add Animations', desc: 'Add animations', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'animation', label: 'Animation type', type: 'text', required: true}] },
            { name: 'protect_ppt', label: 'Protect PPT', desc: 'Password protect', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'unprotect_ppt', label: 'Unprotect PPT', desc: 'Remove protection', inputs: [{name: 'file', label: 'PPT File', type: 'file', accept: '.pptx,.ppt', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
        ],
        ocr: [
            { name: 'ocr_pdf', label: 'OCR PDF', desc: 'Extract text from scanned PDF', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'language', label: 'Language (eng/fra/deu)', type: 'text'}] },
            { name: 'ocr_image', label: 'OCR Image', desc: 'Extract text from image', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.tiff,.bmp', required: true}, {name: 'language', label: 'Language (eng/fra/deu)', type: 'text'}] },
            { name: 'batch_ocr', label: 'Batch OCR', desc: 'OCR multiple files', inputs: [{name: 'files', label: 'Files', type: 'files', accept: '.pdf,.jpg,.jpeg,.png,.tiff', multiple: true, required: true}, {name: 'language', label: 'Language', type: 'text'}] },
            { name: 'get_ocr_languages', label: 'OCR Languages', desc: 'List available languages', inputs: [] },
        ],
        image: [
            { name: 'compress_image', label: 'Compress Image', desc: 'Reduce file size', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp', required: true}, {name: 'quality', label: 'Quality (1-100)', type: 'number'}] },
            { name: 'resize_image', label: 'Resize Image', desc: 'Change dimensions', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'width', label: 'Width', type: 'number'}, {name: 'height', label: 'Height', type: 'number'}] },
            { name: 'crop_image', label: 'Crop Image', desc: 'Crop to region', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'x', label: 'X', type: 'number', required: true}, {name: 'y', label: 'Y', type: 'number', required: true}, {name: 'width', label: 'Width', type: 'number', required: true}, {name: 'height', label: 'Height', type: 'number', required: true}] },
            { name: 'remove_background', label: 'Remove Background', desc: 'Remove image background', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp', required: true}] },
            { name: 'image_to_pdf', label: 'Image to PDF', desc: 'Convert to PDF', inputs: [{name: 'files', label: 'Image Files', type: 'files', accept: '.jpg,.jpeg,.png,.webp,.bmp', multiple: true, required: true}] },
            { name: 'pdf_to_images', label: 'PDF to Images', desc: 'Extract images from PDF', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'format', label: 'Format (png/jpg)', type: 'text'}] },
            { name: 'convert_image_format', label: 'Convert Format', desc: 'PNG↔JPG↔WebP↔GIF↔TIFF', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp,.gif,.tiff,.tif', required: true}, {name: 'format', label: 'Output format', type: 'text', required: true}] },
            { name: 'add_text_watermark', label: 'Text Watermark', desc: 'Add text watermark', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'text', label: 'Watermark text', type: 'text', required: true}, {name: 'opacity', label: 'Opacity (0-1)', type: 'number'}] },
            { name: 'add_image_watermark', label: 'Image Watermark', desc: 'Add image watermark', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'watermark', label: 'Watermark Image', type: 'file', accept: '.png,.jpg,.jpeg', required: true}] },
            { name: 'generate_qr_code', label: 'QR Code Generator', desc: 'Create QR codes', inputs: [{name: 'text', label: 'Text or URL', type: 'text', required: true}, {name: 'size', label: 'Size (pixels)', type: 'number'}, {name: 'format', label: 'Format (png/svg)', type: 'text'}] },
            { name: 'generate_barcode', label: 'Barcode Generator', desc: 'Generate barcodes', inputs: [{name: 'text', label: 'Data', type: 'text', required: true}, {name: 'type', label: 'Type (code128/ean13/upc)', type: 'text', required: true}] },
            { name: 'screenshot_to_pdf', label: 'Screenshot to PDF', desc: 'Screenshots to PDF', inputs: [{name: 'files', label: 'Screenshot Files', type: 'files', accept: '.jpg,.jpeg,.png,.webp', multiple: true, required: true}] },
            { name: 'heic_to_jpg', label: 'HEIC to JPG', desc: 'Convert iPhone photos', inputs: [{name: 'file', label: 'HEIC File', type: 'file', accept: '.heic,.heif', required: true}] },
            { name: 'svg_to_png', label: 'SVG to PNG', desc: 'Vector to raster', inputs: [{name: 'file', label: 'SVG File', type: 'file', accept: '.svg', required: true}, {name: 'width', label: 'Width', type: 'number'}, {name: 'height', label: 'Height', type: 'number'}] },
            { name: 'generate_favicon', label: 'Favicon Generator', desc: 'Create favicons', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp', required: true}] },
            { name: 'get_image_metadata', label: 'Image Metadata', desc: 'View EXIF data', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.tiff', required: true}] },
            { name: 'remove_image_metadata', label: 'Remove Metadata', desc: 'Strip EXIF data', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.tiff', required: true}] },
            { name: 'rotate_image', label: 'Rotate Image', desc: 'Rotate by degrees', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'degrees', label: 'Degrees (90/180/270)', type: 'number', required: true}] },
            { name: 'flip_image', label: 'Flip Image', desc: 'Flip horizontal/vertical', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'direction', label: 'Direction (horizontal/vertical)', type: 'text', required: true}] },
            { name: 'grayscale_image', label: 'Grayscale', desc: 'Convert to grayscale', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}] },
            { name: 'blur_image', label: 'Blur Image', desc: 'Apply blur effect', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'radius', label: 'Blur radius', type: 'number', required: true}] },
            { name: 'sharpen_image', label: 'Sharpen Image', desc: 'Enhance sharpness', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}] },
            { name: 'adjust_brightness', label: 'Adjust Brightness', desc: 'Change brightness', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'factor', label: 'Factor (-100 to 100)', type: 'number', required: true}] },
            { name: 'adjust_contrast', label: 'Adjust Contrast', desc: 'Change contrast', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}, {name: 'factor', label: 'Factor (-100 to 100)', type: 'number', required: true}] },
            { name: 'merge_images', label: 'Merge Images', desc: 'Combine images', inputs: [{name: 'files', label: 'Image Files', type: 'files', accept: '.jpg,.jpeg,.png,.webp,.bmp', multiple: true, required: true}, {name: 'direction', label: 'Direction (horizontal/vertical/grid)', type: 'text', required: true}] },
            { name: 'redact_sensitive_info', label: 'Redact Sensitive', desc: 'Auto-redact email/phone', inputs: [{name: 'file', label: 'Image File', type: 'file', accept: '.jpg,.jpeg,.png,.webp,.bmp', required: true}] },
        ],
        conversion: [
            { name: 'pdf_to_word', label: 'PDF to Word', desc: 'Convert to DOCX', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'pdf_to_excel', label: 'PDF to Excel', desc: 'Convert to XLSX', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'pdf_to_jpg', label: 'PDF to JPG', desc: 'Convert pages to JPG', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'dpi', label: 'DPI', type: 'number'}] },
            { name: 'pdf_to_png', label: 'PDF to PNG', desc: 'Convert pages to PNG', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}, {name: 'dpi', label: 'DPI', type: 'number'}] },
            { name: 'pdf_to_pdfa', label: 'PDF to PDF/A', desc: 'Convert to archival', inputs: [{name: 'file', label: 'PDF File', type: 'file', accept: '.pdf', required: true}] },
            { name: 'excel_to_pdf', label: 'Excel to PDF', desc: 'Convert to PDF', inputs: [{name: 'file', label: 'Excel File', type: 'file', accept: '.xlsx,.xls', required: true}] },
            { name: 'word_to_pdf', label: 'Word to PDF', desc: 'Convert to PDF', inputs: [{name: 'file', label: 'Word File', type: 'file', accept: '.docx,.doc', required: true}] },
            { name: 'image_to_pdf', label: 'Images to PDF', desc: 'Combine images to PDF', inputs: [{name: 'images', label: 'Image Files', type: 'files', accept: '.jpg,.jpeg,.png,.webp,.bmp', multiple: true, required: true}] },
        ],
        video: [
            { name: 'compress_video', label: 'Compress Video', desc: 'Reduce video file size', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm,.mkv', required: true}, {name: 'quality', label: 'Quality (low/medium/high)', type: 'text'}] },
            { name: 'video_to_gif', label: 'Video to GIF', desc: 'Convert to animated GIF', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm', required: true}, {name: 'start', label: 'Start time (sec)', type: 'number'}, {name: 'duration', label: 'Duration (sec)', type: 'number'}] },
            { name: 'trim_video', label: 'Trim Video', desc: 'Cut video segments', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm,.mkv', required: true}, {name: 'start', label: 'Start time (sec)', type: 'number', required: true}, {name: 'end', label: 'End time (sec)', type: 'number', required: true}] },
            { name: 'extract_audio', label: 'Extract Audio', desc: 'Extract audio from video', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm,.mkv', required: true}, {name: 'format', label: 'Format (mp3/wav)', type: 'text'}] },
            { name: 'convert_video_format', label: 'Convert Video', desc: 'MP4↔AVI↔MOV↔WebM', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm,.mkv', required: true}, {name: 'format', label: 'Output format', type: 'text', required: true}] },
            { name: 'extract_thumbnail', label: 'Video Thumbnail', desc: 'Extract frame as image', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm,.mkv', required: true}, {name: 'time', label: 'Time (sec)', type: 'number'}] },
            { name: 'extract_subtitles', label: 'Extract Subtitles', desc: 'Extract subtitles', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.mkv,.avi', required: true}] },
            { name: 'add_watermark_video', label: 'Video Watermark', desc: 'Add watermark to video', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm', required: true}, {name: 'text', label: 'Watermark text', type: 'text', required: true}] },
            { name: 'resize_video', label: 'Resize Video', desc: 'Change video dimensions', inputs: [{name: 'file', label: 'Video File', type: 'file', accept: '.mp4,.avi,.mov,.webm', required: true}, {name: 'width', label: 'Width', type: 'number', required: true}, {name: 'height', label: 'Height', type: 'number', required: true}] },
            { name: 'merge_videos', label: 'Merge Videos', desc: 'Combine video files', inputs: [{name: 'files', label: 'Video Files', type: 'files', accept: '.mp4,.avi,.mov,.webm', multiple: true, required: true}] },
        ],
        audio: [
            { name: 'compress_audio', label: 'Compress Audio', desc: 'Reduce audio file size', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac,.aac', required: true}, {name: 'bitrate', label: 'Bitrate (kbps)', type: 'number'}] },
            { name: 'convert_audio_format', label: 'Convert Audio', desc: 'MP3↔WAV↔OGG↔FLAC↔AAC', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac,.aac,.m4a', required: true}, {name: 'format', label: 'Output format', type: 'text', required: true}] },
            { name: 'trim_audio', label: 'Trim Audio', desc: 'Cut audio segments', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac,.aac', required: true}, {name: 'start', label: 'Start time (sec)', type: 'number', required: true}, {name: 'end', label: 'End time (sec)', type: 'number', required: true}] },
            { name: 'merge_audio', label: 'Merge Audio', desc: 'Combine audio files', inputs: [{name: 'files', label: 'Audio Files', type: 'files', accept: '.mp3,.wav,.ogg,.flac,.aac', multiple: true, required: true}] },
            { name: 'audio_to_text', label: 'Audio to Text', desc: 'Transcribe audio', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac,.m4a', required: true}, {name: 'language', label: 'Language (en/es/fr)', type: 'text'}] },
            { name: 'text_to_speech', label: 'Text to Speech', desc: 'Convert text to audio', inputs: [{name: 'text', label: 'Text', type: 'textarea', required: true}, {name: 'voice', label: 'Voice (male/female)', type: 'text'}, {name: 'language', label: 'Language', type: 'text'}] },
            { name: 'adjust_volume', label: 'Adjust Volume', desc: 'Change audio volume', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac', required: true}, {name: 'factor', label: 'Volume factor (0.5-2.0)', type: 'number', required: true}] },
            { name: 'normalize_audio', label: 'Normalize Audio', desc: 'Normalize volume levels', inputs: [{name: 'file', label: 'Audio File', type: 'file', accept: '.mp3,.wav,.ogg,.flac', required: true}] },
        ],
        developer: [
            { name: 'format_json', label: 'Format JSON', desc: 'Beautify JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json,.txt', required: true}, {name: 'indent', label: 'Indent spaces', type: 'number'}] },
            { name: 'minify_json', label: 'Minify JSON', desc: 'Compress JSON', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'json_to_yaml', label: 'JSON to YAML', desc: 'Convert JSON↔YAML', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'yaml_to_json', label: 'YAML to JSON', desc: 'Convert YAML to JSON', inputs: [{name: 'file', label: 'YAML File', type: 'file', accept: '.yaml,.yml', required: true}] },
            { name: 'json_to_xml', label: 'JSON to XML', desc: 'Convert JSON↔XML', inputs: [{name: 'file', label: 'JSON File', type: 'file', accept: '.json', required: true}] },
            { name: 'xml_to_json', label: 'XML to JSON', desc: 'Convert XML to JSON', inputs: [{name: 'file', label: 'XML File', type: 'file', accept: '.xml', required: true}] },
            { name: 'format_xml', label: 'Format XML', desc: 'Beautify XML', inputs: [{name: 'file', label: 'XML File', type: 'file', accept: '.xml', required: true}] },
            { name: 'format_html', label: 'Format HTML', desc: 'Beautify HTML', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'minify_html', label: 'Minify HTML', desc: 'Compress HTML', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'minify_css', label: 'Minify CSS', desc: 'Compress CSS', inputs: [{name: 'file', label: 'CSS File', type: 'file', accept: '.css', required: true}] },
            { name: 'minify_js', label: 'Minify JavaScript', desc: 'Compress JS', inputs: [{name: 'file', label: 'JavaScript File', type: 'file', accept: '.js', required: true}] },
            { name: 'format_sql', label: 'Format SQL', desc: 'Beautify SQL', inputs: [{name: 'file', label: 'SQL File', type: 'file', accept: '.sql,.txt', required: true}] },
            { name: 'base64_encode', label: 'Base64 Encode', desc: 'Encode to Base64', inputs: [{name: 'text', label: 'Text to encode', type: 'textarea', required: true}] },
            { name: 'base64_decode', label: 'Base64 Decode', desc: 'Decode Base64', inputs: [{name: 'text', label: 'Base64 text', type: 'textarea', required: true}] },
            { name: 'url_encode', label: 'URL Encode', desc: 'Encode URL', inputs: [{name: 'text', label: 'Text to encode', type: 'textarea', required: true}] },
            { name: 'url_decode', label: 'URL Decode', desc: 'Decode URL', inputs: [{name: 'text', label: 'Encoded text', type: 'textarea', required: true}] },
            { name: 'generate_hash', label: 'Hash Generator', desc: 'MD5/SHA1/SHA256', inputs: [{name: 'text', label: 'Text to hash', type: 'textarea', required: true}, {name: 'algorithm', label: 'Algorithm (md5/sha1/sha256)', type: 'text', required: true}] },
            { name: 'generate_uuid', label: 'UUID Generator', desc: 'Generate unique IDs', inputs: [{name: 'count', label: 'Count', type: 'number'}, {name: 'version', label: 'Version (1/4)', type: 'number'}] },
            { name: 'generate_lorem', label: 'Lorem Ipsum', desc: 'Generate placeholder text', inputs: [{name: 'paragraphs', label: 'Paragraphs', type: 'number', required: true}] },
            { name: 'color_converter', label: 'Color Converter', desc: 'HEX↔RGB↔HSL', inputs: [{name: 'color', label: 'Color value', type: 'text', required: true}, {name: 'from', label: 'From (hex/rgb/hsl)', type: 'text', required: true}, {name: 'to', label: 'To (hex/rgb/hsl)', type: 'text', required: true}] },
            { name: 'jwt_decode', label: 'JWT Decoder', desc: 'Decode JWT tokens', inputs: [{name: 'token', label: 'JWT Token', type: 'textarea', required: true}] },
            { name: 'markdown_to_html', label: 'Markdown to HTML', desc: 'Convert Markdown', inputs: [{name: 'file', label: 'Markdown File', type: 'file', accept: '.md,.markdown,.txt', required: true}] },
            { name: 'html_to_markdown', label: 'HTML to Markdown', desc: 'Convert to Markdown', inputs: [{name: 'file', label: 'HTML File', type: 'file', accept: '.html,.htm', required: true}] },
            { name: 'regex_tester', label: 'Regex Tester', desc: 'Test regular expressions', inputs: [{name: 'pattern', label: 'Regex pattern', type: 'text', required: true}, {name: 'text', label: 'Test text', type: 'textarea', required: true}, {name: 'flags', label: 'Flags (g/i/m)', type: 'text'}] },
            { name: 'cron_generator', label: 'Cron Generator', desc: 'Build cron expressions', inputs: [{name: 'minute', label: 'Minute', type: 'text', required: true}, {name: 'hour', label: 'Hour', type: 'text', required: true}, {name: 'day', label: 'Day', type: 'text', required: true}, {name: 'month', label: 'Month', type: 'text', required: true}, {name: 'weekday', label: 'Weekday', type: 'text', required: true}] },
        ],
        calculator: [
            { name: 'percentage_calculator', label: 'Percentage', desc: 'Calculate percentages', inputs: [{name: 'value', label: 'Value', type: 'number', required: true}, {name: 'percentage', label: 'Percentage', type: 'number', required: true}, {name: 'operation', label: 'Operation (of/increase/decrease)', type: 'text', required: true}] },
            { name: 'age_calculator', label: 'Age Calculator', desc: 'Calculate age from date', inputs: [{name: 'birthdate', label: 'Birth Date (YYYY-MM-DD)', type: 'text', required: true}] },
            { name: 'date_calculator', label: 'Date Calculator', desc: 'Add/subtract days', inputs: [{name: 'date', label: 'Start Date (YYYY-MM-DD)', type: 'text', required: true}, {name: 'days', label: 'Days to add/subtract', type: 'number', required: true}] },
            { name: 'timezone_converter', label: 'Timezone Converter', desc: 'Convert timezones', inputs: [{name: 'datetime', label: 'Date Time', type: 'text', required: true}, {name: 'from_tz', label: 'From Timezone', type: 'text', required: true}, {name: 'to_tz', label: 'To Timezone', type: 'text', required: true}] },
            { name: 'bmi_calculator', label: 'BMI Calculator', desc: 'Body mass index', inputs: [{name: 'weight', label: 'Weight (kg)', type: 'number', required: true}, {name: 'height', label: 'Height (cm)', type: 'number', required: true}] },
            { name: 'loan_calculator', label: 'Loan Calculator', desc: 'EMI/interest calculator', inputs: [{name: 'principal', label: 'Principal amount', type: 'number', required: true}, {name: 'rate', label: 'Annual interest rate (%)', type: 'number', required: true}, {name: 'months', label: 'Loan term (months)', type: 'number', required: true}] },
            { name: 'discount_calculator', label: 'Discount Calculator', desc: 'Calculate discounts', inputs: [{name: 'price', label: 'Original price', type: 'number', required: true}, {name: 'discount', label: 'Discount (%)', type: 'number', required: true}] },
            { name: 'tip_calculator', label: 'Tip Calculator', desc: 'Calculate tips', inputs: [{name: 'amount', label: 'Bill amount', type: 'number', required: true}, {name: 'tip_percent', label: 'Tip percentage', type: 'number', required: true}, {name: 'people', label: 'Split between', type: 'number'}] },
            { name: 'unit_converter', label: 'Unit Converter', desc: 'Convert units', inputs: [{name: 'value', label: 'Value', type: 'number', required: true}, {name: 'from_unit', label: 'From unit', type: 'text', required: true}, {name: 'to_unit', label: 'To unit', type: 'text', required: true}, {name: 'category', label: 'Category (length/weight/temp)', type: 'text', required: true}] },
            { name: 'currency_converter', label: 'Currency Converter', desc: 'Exchange rates', inputs: [{name: 'amount', label: 'Amount', type: 'number', required: true}, {name: 'from_currency', label: 'From (USD/EUR/GBP)', type: 'text', required: true}, {name: 'to_currency', label: 'To currency', type: 'text', required: true}] },
            { name: 'scientific_calculator', label: 'Scientific Calculator', desc: 'Advanced calculations', inputs: [{name: 'expression', label: 'Math expression', type: 'text', required: true}] },
        ],
        seo: [
            { name: 'generate_meta_tags', label: 'Meta Tag Generator', desc: 'Generate SEO meta tags', inputs: [{name: 'title', label: 'Page title', type: 'text', required: true}, {name: 'description', label: 'Description', type: 'textarea', required: true}, {name: 'keywords', label: 'Keywords', type: 'text'}, {name: 'url', label: 'Page URL', type: 'text'}] },
            { name: 'generate_robots_txt', label: 'Robots.txt Generator', desc: 'Create robots.txt', inputs: [{name: 'sitemap_url', label: 'Sitemap URL', type: 'text'}, {name: 'disallow', label: 'Disallow paths (comma-sep)', type: 'text'}] },
            { name: 'generate_sitemap', label: 'Sitemap Generator', desc: 'Generate XML sitemap', inputs: [{name: 'urls', label: 'URLs (one per line)', type: 'textarea', required: true}, {name: 'base_url', label: 'Base URL', type: 'text', required: true}] },
            { name: 'generate_og_tags', label: 'Open Graph Generator', desc: 'Generate OG tags', inputs: [{name: 'title', label: 'Title', type: 'text', required: true}, {name: 'description', label: 'Description', type: 'textarea', required: true}, {name: 'image', label: 'Image URL', type: 'text'}, {name: 'url', label: 'Page URL', type: 'text'}] },
            { name: 'generate_twitter_card', label: 'Twitter Card Generator', desc: 'Generate Twitter cards', inputs: [{name: 'title', label: 'Title', type: 'text', required: true}, {name: 'description', label: 'Description', type: 'textarea', required: true}, {name: 'image', label: 'Image URL', type: 'text'}, {name: 'card_type', label: 'Card type (summary/summary_large_image)', type: 'text'}] },
            { name: 'generate_schema', label: 'Schema Markup', desc: 'Generate JSON-LD', inputs: [{name: 'type', label: 'Schema type (Article/Product/Organization)', type: 'text', required: true}, {name: 'name', label: 'Name', type: 'text', required: true}, {name: 'description', label: 'Description', type: 'textarea'}] },
            { name: 'keyword_density', label: 'Keyword Density', desc: 'Analyze keyword usage', inputs: [{name: 'text', label: 'Text content', type: 'textarea', required: true}, {name: 'keyword', label: 'Target keyword', type: 'text', required: true}] },
            { name: 'generate_slug', label: 'Slug Generator', desc: 'URL-friendly slugs', inputs: [{name: 'text', label: 'Text', type: 'text', required: true}] },
            { name: 'utm_builder', label: 'UTM Builder', desc: 'Build tracking URLs', inputs: [{name: 'url', label: 'Base URL', type: 'text', required: true}, {name: 'source', label: 'Source', type: 'text', required: true}, {name: 'medium', label: 'Medium', type: 'text', required: true}, {name: 'campaign', label: 'Campaign', type: 'text', required: true}] },
            { name: 'check_redirect', label: 'Redirect Checker', desc: 'Check URL redirects', inputs: [{name: 'url', label: 'URL to check', type: 'text', required: true}] },
        ],
        security: [
            { name: 'generate_password', label: 'Password Generator', desc: 'Generate strong passwords', inputs: [{name: 'length', label: 'Length', type: 'number', required: true}, {name: 'include_symbols', label: 'Include symbols', type: 'text'}, {name: 'include_numbers', label: 'Include numbers', type: 'text'}] },
            { name: 'check_password_strength', label: 'Password Strength', desc: 'Check password security', inputs: [{name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'check_ssl', label: 'SSL Checker', desc: 'Verify SSL certificates', inputs: [{name: 'domain', label: 'Domain', type: 'text', required: true}] },
            { name: 'validate_email', label: 'Email Validator', desc: 'Validate email addresses', inputs: [{name: 'email', label: 'Email address', type: 'text', required: true}] },
            { name: 'ip_lookup', label: 'IP Lookup', desc: 'Get IP information', inputs: [{name: 'ip', label: 'IP Address', type: 'text', required: true}] },
            { name: 'whois_lookup', label: 'WHOIS Lookup', desc: 'Domain information', inputs: [{name: 'domain', label: 'Domain', type: 'text', required: true}] },
            { name: 'dns_lookup', label: 'DNS Lookup', desc: 'Check DNS records', inputs: [{name: 'domain', label: 'Domain', type: 'text', required: true}, {name: 'type', label: 'Record type (A/MX/TXT/CNAME)', type: 'text'}] },
            { name: 'generate_privacy_policy', label: 'Privacy Policy', desc: 'Generate privacy policy', inputs: [{name: 'company_name', label: 'Company name', type: 'text', required: true}, {name: 'website', label: 'Website URL', type: 'text', required: true}, {name: 'email', label: 'Contact email', type: 'text', required: true}] },
            { name: 'generate_terms', label: 'Terms Generator', desc: 'Generate terms of service', inputs: [{name: 'company_name', label: 'Company name', type: 'text', required: true}, {name: 'website', label: 'Website URL', type: 'text', required: true}] },
            { name: 'encrypt_text', label: 'Encrypt Text', desc: 'AES encrypt text', inputs: [{name: 'text', label: 'Text to encrypt', type: 'textarea', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
            { name: 'decrypt_text', label: 'Decrypt Text', desc: 'AES decrypt text', inputs: [{name: 'text', label: 'Encrypted text', type: 'textarea', required: true}, {name: 'password', label: 'Password', type: 'text', required: true}] },
        ],
        file: [
            { name: 'compress_files', label: 'Compress Files', desc: 'Create ZIP archive', inputs: [{name: 'files', label: 'Files', type: 'files', accept: '*', multiple: true, required: true}] },
            { name: 'extract_archive', label: 'Extract Archive', desc: 'Extract ZIP/RAR', inputs: [{name: 'file', label: 'Archive File', type: 'file', accept: '.zip,.rar,.7z,.tar,.gz', required: true}] },
            { name: 'merge_files', label: 'Merge Files', desc: 'Combine text files', inputs: [{name: 'files', label: 'Text Files', type: 'files', accept: '.txt,.csv,.md', multiple: true, required: true}] },
            { name: 'split_file', label: 'Split File', desc: 'Split large files', inputs: [{name: 'file', label: 'File', type: 'file', accept: '*', required: true}, {name: 'chunk_size', label: 'Chunk size (MB)', type: 'number', required: true}] },
            { name: 'calculate_checksum', label: 'Checksum Calculator', desc: 'MD5/SHA file verification', inputs: [{name: 'file', label: 'File', type: 'file', accept: '*', required: true}, {name: 'algorithm', label: 'Algorithm (md5/sha1/sha256)', type: 'text', required: true}] },
            { name: 'identify_file_type', label: 'File Type Identifier', desc: 'Identify file types', inputs: [{name: 'file', label: 'File', type: 'file', accept: '*', required: true}] },
            { name: 'compare_files', label: 'Compare Files', desc: 'Compare two files', inputs: [{name: 'file1', label: 'First File', type: 'file', accept: '*', required: true}, {name: 'file2', label: 'Second File', type: 'file', accept: '*', required: true}] },
        ]
    },

    executionProgress: 0,
    executionStatus: '',
    outputFileUrl: null,
    outputFileName: null,

    openTool(tool) {
        this.selectedTool = tool;
        this.toolInputs = {};
        this.uploadedFiles = {};
        this.toolResult = null;
        this.toolError = null;
        this.executionProgress = 0;
        this.executionStatus = '';
        this.outputFileUrl = null;
        this.outputFileName = null;
        tool.inputs.forEach(input => {
            this.toolInputs[input.name] = '';
        });
        this.toolModal = true;
    },

    async handleFileUpload(event, inputName, multiple = false) {
        const files = event.target.files;
        if (!files || files.length === 0) return;

        this.uploadingField = inputName;
        this.toolError = null;

        try {
            const uploadedPaths = [];
            const originalNames = [];

            for (const file of files) {
                const formData = new FormData();
                formData.append('file', file);

                const response = await fetch('{{ $apiUrl }}/api/files/upload/tools', {
                    method: 'POST',
                    headers: {
                        'X-Session-Id': '{{ $currentSessionId }}'
                    },
                    body: formData
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error.message || 'Upload failed');
                }

                const result = await response.json();
                uploadedPaths.push(result.filePath || result.path);
                originalNames.push(result.originalName || file.name);
            }

            if (multiple) {
                this.uploadedFiles[inputName] = uploadedPaths;
                this.uploadedFiles[inputName + '_names'] = originalNames;
                this.toolInputs[inputName] = uploadedPaths.join(',');
            } else {
                this.uploadedFiles[inputName] = uploadedPaths[0];
                this.uploadedFiles[inputName + '_name'] = originalNames[0];
                this.toolInputs[inputName] = uploadedPaths[0];
            }
        } catch (error) {
            this.toolError = 'Upload failed: ' + error.message;
        } finally {
            this.uploadingField = null;
        }
    },

    getUploadedFileName(inputName) {
        const path = this.uploadedFiles[inputName];
        if (!path) return null;
        if (Array.isArray(path)) {
            return path.map(p => p.split('/').pop()).join(', ');
        }
        return path.split('/').pop();
    },

    async executeTool() {
        this.toolLoading = true;
        this.toolResult = null;
        this.toolError = null;
        this.executionProgress = 0;
        this.executionStatus = 'Preparing...';
        this.outputFileUrl = null;
        this.outputFileName = null;

        // Simulate progress updates
        const progressInterval = setInterval(() => {
            if (this.executionProgress < 90) {
                this.executionProgress += Math.random() * 15;
                if (this.executionProgress > 90) this.executionProgress = 90;

                if (this.executionProgress < 30) {
                    this.executionStatus = 'Uploading files...';
                } else if (this.executionProgress < 60) {
                    this.executionStatus = 'Processing...';
                } else {
                    this.executionStatus = 'Finalizing...';
                }
            }
        }, 500);

        try {
            // Build request body
            const requestBody = {};
            for (const [key, value] of Object.entries(this.toolInputs)) {
                if (value) {
                    // Handle comma-separated paths for array fields
                    if ((key === 'files' || key === 'images') && typeof value === 'string' && value.includes(',')) {
                        requestBody[key] = value.split(',').map(s => s.trim());
                    } else if (key === 'pages' || key === 'slides') {
                        // Keep as string for the backend to parse
                        requestBody[key] = value;
                    } else if (key === 'data' || key === 'regions') {
                        try {
                            requestBody[key] = JSON.parse(value);
                        } catch {
                            requestBody[key] = value;
                        }
                    } else {
                        requestBody[key] = value;
                    }
                }
            }

            // Auto-generate output filename
            const toolName = this.selectedTool.name;
            const primaryFile = this.uploadedFiles['file_name'] ||
                               (this.uploadedFiles['files_names'] ? this.uploadedFiles['files_names'][0] : null) ||
                               (this.uploadedFiles['images_names'] ? this.uploadedFiles['images_names'][0] : null);

            if (primaryFile) {
                const baseName = primaryFile.replace(/\.[^/.]+$/, '');
                const timestamp = new Date().toISOString().slice(0,10).replace(/-/g, '');

                // Determine output extension based on tool
                const toolExtensions = {
                    'merge_pdf': '.pdf', 'split_pdf': '.pdf', 'compress_pdf': '.pdf', 'rotate_pdf': '.pdf',
                    'add_watermark': '.pdf', 'protect_pdf': '.pdf', 'unlock_pdf': '.pdf', 'add_page_numbers': '.pdf',
                    'extract_pages': '.pdf', 'pdf_to_text': '.txt', 'sign_pdf': '.pdf',
                    'merge_workbooks': '.xlsx', 'split_workbook': '.xlsx', 'excel_to_csv': '.csv',
                    'excel_to_json': '.json', 'csv_to_excel': '.xlsx', 'json_to_excel': '.xlsx',
                    'protect_workbook': '.xlsx', 'compress_excel': '.xlsx', 'add_chart': '.xlsx',
                    'find_replace_excel': '.xlsx', 'clean_excel': '.xlsx',
                    'merge_word': '.docx', 'word_to_pdf': '.pdf', 'word_to_text': '.txt', 'word_to_html': '.html',
                    'find_replace_word': '.docx', 'add_watermark_word': '.docx', 'protect_word': '.docx',
                    'compress_word': '.docx', 'mail_merge': '.docx',
                    'merge_text': '.txt', 'find_replace': '.txt', 'sort_lines': '.txt', 'remove_duplicates': '.txt',
                    'convert_case': '.txt', 'encrypt_text': '.enc', 'decrypt_text': '.txt',
                    'format_json': '.json', 'minify_json': '.json', 'merge_json': '.json',
                    'json_to_csv': '.csv', 'csv_to_json': '.json', 'json_to_pdf': '.pdf', 'repair_json': '.json',
                    'merge_ppt': '.pptx', 'ppt_to_pdf': '.pdf', 'compress_ppt': '.pptx',
                    'pdf_to_word': '.docx', 'pdf_to_excel': '.xlsx', 'pdf_to_jpg': '.jpg', 'pdf_to_png': '.png',
                    'excel_to_pdf': '.pdf', 'image_to_pdf': '.pdf',
                    'resize_image': '.png', 'crop_image': '.png', 'rotate_image': '.png', 'add_text_watermark': '.png',
                };

                let extension = toolExtensions[toolName] || '.out';

                // Handle format-specific extensions
                if (toolName === 'convert_image_format' && this.toolInputs['format']) {
                    extension = '.' + this.toolInputs['format'];
                }

                const outputFileName = `${baseName}_${toolName.replace(/_/g, '-')}_${timestamp}${extension}`;
                requestBody['output_file'] = outputFileName;
            }

            const response = await fetch(`{{ $apiUrl }}/api/tools/${this.selectedTool.name}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Session-Id': '{{ $currentSessionId }}'
                },
                body: JSON.stringify(requestBody)
            });

            clearInterval(progressInterval);
            this.executionProgress = 100;

            const result = await response.json();

            if (response.ok) {
                this.toolResult = result;
                this.executionStatus = 'Complete!';

                // Extract output file info for download
                const outputPath = result.outputFile || result.output_file || result.filePath || result.path;
                if (outputPath) {
                    this.outputFileName = outputPath.split('/').pop();
                    // Build download URL
                    this.outputFileUrl = `{{ $apiUrl }}/api/files/generated/{{ $currentSessionId }}/${encodeURIComponent(this.outputFileName)}/download`;
                }

                // Refresh files list
                @this.call('refreshFiles');
            } else {
                this.toolError = result.error || result.message || 'Tool execution failed';
                this.executionStatus = 'Failed';
            }
        } catch (error) {
            clearInterval(progressInterval);
            this.toolError = error.message;
            this.executionStatus = 'Error';
        } finally {
            this.toolLoading = false;
        }
    },

    downloadAndClose() {
        if (this.outputFileUrl) {
            window.open(this.outputFileUrl, '_blank');
            setTimeout(() => {
                this.toolModal = false;
                // Reset state
                this.toolResult = null;
                this.outputFileUrl = null;
                this.outputFileName = null;
                this.executionProgress = 0;
            }, 500);
        }
    }
}">
    <!-- Header -->
    <div class="flex items-center justify-between px-4 py-3 border-b border-neutral-200 cursor-pointer" @click="toolsOpen = !toolsOpen">
        <div class="flex items-center space-x-3">
            <div class="w-8 h-8 bg-neutral-900 rounded-lg flex items-center justify-center">
                <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                </svg>
            </div>
            <div>
                <h3 class="text-[15px] font-semibold text-neutral-900">Free Online Document Tools</h3>
                <p class="text-[11px] text-neutral-500">Convert, merge, split PDF, Excel, Word files instantly</p>
            </div>
        </div>
        <svg class="w-5 h-5 text-neutral-400 transition-transform" :class="toolsOpen ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
    </div>

    <!-- Tools Content -->
    <div x-show="toolsOpen" x-transition:enter="transition ease-out duration-200" x-transition:enter-start="opacity-0 -translate-y-2" x-transition:enter-end="opacity-100 translate-y-0" class="p-4">
        <!-- Category Tabs -->
        <div class="flex flex-wrap gap-2 mb-4">
            <template x-for="(cat, key) in categories" :key="key">
                <button
                    @click="activeCategory = key"
                    :class="activeCategory === key ? 'bg-neutral-900 text-white' : 'bg-neutral-100 text-neutral-700 hover:bg-neutral-200'"
                    class="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-[12px] font-medium transition"
                >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="cat.icon"/>
                    </svg>
                    <span x-text="cat.name"></span>
                </button>
            </template>
        </div>

        <!-- Tools Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2">
            <template x-for="tool in tools[activeCategory]" :key="tool.name">
                <button
                    @click="openTool(tool)"
                    class="flex flex-col items-center p-3 bg-neutral-50 hover:bg-neutral-100 rounded-xl transition text-center group"
                >
                    <div class="w-10 h-10 bg-white rounded-lg flex items-center justify-center mb-2 shadow-sm group-hover:shadow-md transition">
                        <svg class="w-5 h-5 text-neutral-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="categories[activeCategory].icon"/>
                        </svg>
                    </div>
                    <p class="text-[11px] font-medium text-neutral-800 leading-tight" x-text="tool.label"></p>
                    <p class="text-[10px] text-neutral-500 mt-0.5" x-text="tool.desc"></p>
                </button>
            </template>
        </div>
    </div>

    <!-- Tool Modal -->
    <div x-show="toolModal" x-cloak class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background: rgba(0,0,0,0.5)">
        <div @click.away="toolModal = false" class="bg-white rounded-2xl shadow-xl max-w-lg w-full max-h-[80vh] overflow-hidden">
            <!-- Modal Header -->
            <div class="flex items-center justify-between px-5 py-4 border-b border-neutral-200">
                <div>
                    <h4 class="text-[15px] font-semibold text-neutral-900" x-text="selectedTool?.label"></h4>
                    <p class="text-[11px] text-neutral-500" x-text="selectedTool?.desc"></p>
                </div>
                <button @click="toolModal = false" class="p-1.5 hover:bg-neutral-100 rounded-lg transition">
                    <svg class="w-5 h-5 text-neutral-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                    </svg>
                </button>
            </div>

            <!-- Modal Body -->
            <div class="p-5 overflow-y-auto max-h-[50vh]">
                <template x-if="selectedTool?.inputs?.length > 0">
                    <div class="space-y-4">
                        <template x-for="input in selectedTool.inputs" :key="input.name">
                            <div>
                                <label class="block text-[12px] font-medium text-neutral-700 mb-1">
                                    <span x-text="input.label"></span>
                                    <span x-show="input.required" class="text-red-500">*</span>
                                </label>

                                <!-- File Upload Input (single file) -->
                                <template x-if="input.type === 'file'">
                                    <div class="relative">
                                        <label class="flex flex-col items-center justify-center w-full h-24 border-2 border-dashed border-neutral-300 rounded-xl cursor-pointer bg-neutral-50 hover:bg-neutral-100 hover:border-neutral-400 transition-all">
                                            <template x-if="uploadingField === input.name">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-6 h-6 text-neutral-600 animate-spin" fill="none" viewBox="0 0 24 24">
                                                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-500 mt-1">Uploading...</p>
                                                </div>
                                            </template>
                                            <template x-if="uploadingField !== input.name && !uploadedFiles[input.name]">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-8 h-8 text-neutral-400 mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-500">Click to upload file</p>
                                                    <p class="text-[10px] text-neutral-400" x-text="input.accept"></p>
                                                </div>
                                            </template>
                                            <template x-if="uploadingField !== input.name && uploadedFiles[input.name]">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-8 h-8 text-green-600 mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-700 font-medium" x-text="getUploadedFileName(input.name)"></p>
                                                    <p class="text-[10px] text-neutral-400">Click to change</p>
                                                </div>
                                            </template>
                                            <input
                                                type="file"
                                                :accept="input.accept"
                                                @change="handleFileUpload($event, input.name, false)"
                                                class="hidden"
                                            />
                                        </label>
                                    </div>
                                </template>

                                <!-- Multiple Files Upload Input -->
                                <template x-if="input.type === 'files'">
                                    <div class="relative">
                                        <label class="flex flex-col items-center justify-center w-full h-24 border-2 border-dashed border-neutral-300 rounded-xl cursor-pointer bg-neutral-50 hover:bg-neutral-100 hover:border-neutral-400 transition-all">
                                            <template x-if="uploadingField === input.name">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-6 h-6 text-neutral-600 animate-spin" fill="none" viewBox="0 0 24 24">
                                                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-500 mt-1">Uploading...</p>
                                                </div>
                                            </template>
                                            <template x-if="uploadingField !== input.name && !uploadedFiles[input.name]">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-8 h-8 text-neutral-400 mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-500">Click to upload multiple files</p>
                                                    <p class="text-[10px] text-neutral-400" x-text="input.accept"></p>
                                                </div>
                                            </template>
                                            <template x-if="uploadingField !== input.name && uploadedFiles[input.name]">
                                                <div class="flex flex-col items-center">
                                                    <svg class="w-8 h-8 text-green-600 mb-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                                                    </svg>
                                                    <p class="text-[11px] text-neutral-700 font-medium" x-text="getUploadedFileName(input.name)"></p>
                                                    <p class="text-[10px] text-neutral-400">Click to add more</p>
                                                </div>
                                            </template>
                                            <input
                                                type="file"
                                                :accept="input.accept"
                                                multiple
                                                @change="handleFileUpload($event, input.name, true)"
                                                class="hidden"
                                            />
                                        </label>
                                    </div>
                                </template>

                                <!-- Textarea Input -->
                                <template x-if="input.type === 'textarea'">
                                    <textarea
                                        x-model="toolInputs[input.name]"
                                        class="w-full px-3 py-2 text-[13px] border border-neutral-300 rounded-lg focus:ring-1 focus:ring-neutral-900 focus:border-neutral-900 outline-none transition"
                                        rows="3"
                                        :placeholder="input.label"
                                    ></textarea>
                                </template>

                                <!-- Text/Number Input -->
                                <template x-if="input.type !== 'textarea' && input.type !== 'file' && input.type !== 'files'">
                                    <input
                                        :type="input.type || 'text'"
                                        x-model="toolInputs[input.name]"
                                        class="w-full px-3 py-2 text-[13px] border border-neutral-300 rounded-lg focus:ring-1 focus:ring-neutral-900 focus:border-neutral-900 outline-none transition"
                                        :placeholder="input.label"
                                    />
                                </template>
                            </div>
                        </template>
                    </div>
                </template>
                <template x-if="!selectedTool?.inputs?.length">
                    <p class="text-[13px] text-neutral-500">This tool requires no input parameters.</p>
                </template>

                <!-- Progress Display (during execution) -->
                <template x-if="toolLoading">
                    <div class="mt-4 p-4 bg-neutral-50 border border-neutral-200 rounded-xl">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-[12px] font-medium text-neutral-700" x-text="executionStatus"></span>
                            <span class="text-[11px] text-neutral-500" x-text="Math.round(executionProgress) + '%'"></span>
                        </div>
                        <div class="w-full bg-neutral-200 rounded-full h-2 overflow-hidden">
                            <div class="bg-neutral-900 h-full rounded-full transition-all duration-300" :style="'width: ' + executionProgress + '%'"></div>
                        </div>
                        <p class="text-[11px] text-neutral-500 mt-2 text-center">Please wait while your file is being processed...</p>
                    </div>
                </template>

                <!-- Success with Download Button -->
                <template x-if="toolResult && outputFileUrl">
                    <div class="mt-4 p-4 bg-green-50 border border-green-200 rounded-xl">
                        <div class="flex items-center space-x-3 mb-3">
                            <div class="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
                                <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                                </svg>
                            </div>
                            <div>
                                <p class="text-[13px] font-semibold text-green-800">Processing Complete!</p>
                                <p class="text-[11px] text-green-600" x-text="outputFileName"></p>
                            </div>
                        </div>
                        <button
                            @click="downloadAndClose()"
                            class="w-full flex items-center justify-center space-x-2 px-4 py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg transition font-medium"
                        >
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
                            </svg>
                            <span>Download File</span>
                        </button>
                    </div>
                </template>

                <!-- Success without file (info tools) -->
                <template x-if="toolResult && !outputFileUrl">
                    <div class="mt-4 p-3 bg-green-50 border border-green-200 rounded-lg">
                        <p class="text-[12px] font-medium text-green-800 mb-2">Result:</p>
                        <pre class="text-[11px] text-green-700 overflow-x-auto whitespace-pre-wrap max-h-40 overflow-y-auto bg-white p-2 rounded" x-text="JSON.stringify(toolResult, null, 2)"></pre>
                    </div>
                </template>

                <!-- Error Display -->
                <template x-if="toolError">
                    <div class="mt-4 p-4 bg-red-50 border border-red-200 rounded-xl">
                        <div class="flex items-center space-x-3">
                            <div class="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center flex-shrink-0">
                                <svg class="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                                </svg>
                            </div>
                            <div>
                                <p class="text-[13px] font-semibold text-red-800">Processing Failed</p>
                                <p class="text-[11px] text-red-600" x-text="toolError"></p>
                            </div>
                        </div>
                    </div>
                </template>
            </div>

            <!-- Modal Footer -->
            <div class="flex items-center justify-end space-x-2 px-5 py-4 border-t border-neutral-200 bg-neutral-50">
                <template x-if="!toolResult">
                    <button
                        @click="toolModal = false"
                        class="px-4 py-2 text-[13px] font-medium text-neutral-700 hover:bg-neutral-200 rounded-lg transition"
                        :disabled="toolLoading"
                    >
                        Cancel
                    </button>
                </template>
                <template x-if="toolResult">
                    <button
                        @click="toolModal = false; toolResult = null; outputFileUrl = null;"
                        class="px-4 py-2 text-[13px] font-medium text-neutral-700 hover:bg-neutral-200 rounded-lg transition"
                    >
                        Close
                    </button>
                </template>
                <template x-if="!toolResult && !toolLoading">
                    <button
                        @click="executeTool()"
                        :disabled="toolLoading"
                        class="px-4 py-2 text-[13px] font-medium text-white bg-neutral-900 hover:bg-neutral-800 rounded-lg transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2"
                    >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                        <span>Execute</span>
                    </button>
                </template>
                <template x-if="toolLoading">
                    <button
                        disabled
                        class="px-4 py-2 text-[13px] font-medium text-white bg-neutral-400 rounded-lg cursor-not-allowed flex items-center space-x-2"
                    >
                        <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <span>Processing...</span>
                    </button>
                </template>
            </div>
        </div>
    </div>
</div>

<!-- Marked.js for markdown parsing -->
<script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>

<script>
    // Configure marked for safe rendering
    marked.setOptions({
        breaks: true,
        gfm: true,
        sanitize: false
    });

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

                // Complete the streaming - parse markdown to HTML before saving
                const htmlContent = marked.parse(this.content);
                this.wire.completeStreaming(htmlContent, this.usage || {}, this.files, this.model);

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
                    // Append content and update UI in real-time with markdown parsing
                    if (data.content) {
                        this.content += data.content;
                        // Parse markdown to HTML for proper formatting
                        const htmlContent = marked.parse(this.content);
                        this.wire.updateStreamingContent(htmlContent);
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

            console.log('Starting streaming with handler, agentMode:', data.agentMode);

            // Build request body based on mode
            const requestBody = data.agentMode
                ? { goal: data.goal, sessionId: data.sessionId }
                : { prompt: data.prompt, messages: data.messages, sessionId: data.sessionId };

            streamingHandler.startStreaming(data.streamUrl, requestBody);
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
