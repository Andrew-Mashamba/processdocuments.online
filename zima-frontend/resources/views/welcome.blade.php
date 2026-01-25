<!DOCTYPE html>
<html lang="{{ str_replace('_', '-', app()->getLocale()) }}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>ZIMA AI - File Generator</title>

    <!-- Fonts -->
    <link rel="preconnect" href="https://fonts.bunny.net">
    <link href="https://fonts.bunny.net/css?family=instrument-sans:400,500,600" rel="stylesheet" />

    @vite(['resources/css/app.css', 'resources/js/app.js'])

    <!-- Tailwind CSS Browser -->
    <script src="https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4"></script>

    @livewireStyles
</head>
<body class="antialiased bg-neutral-50 min-h-screen flex flex-col">
    <!-- Compact Header - Monochrome Design -->
    <header class="bg-white shadow-sm">
        <div class="max-w-full mx-auto px-4 sm:px-6">
            <div class="flex justify-between items-center h-14">
                <!-- Logo -->
                <div class="flex items-center">
                    <div class="w-10 h-10 bg-neutral-900 rounded-xl flex items-center justify-center mr-3 shadow-sm">
                        <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                    </div>
                    <div>
                        <span class="text-[15px] font-bold text-neutral-900">ZIMA AI</span>
                        <p class="text-[11px] text-neutral-500">File Generator</p>
                    </div>
                </div>

                <!-- Auth Links -->
                @if (Route::has('login'))
                    <nav class="flex items-center gap-3">
                        @auth
                            <a href="{{ url('/dashboard') }}" class="text-sm text-neutral-500 hover:text-neutral-900 transition-colors">
                                Dashboard
                            </a>
                            <a href="{{ url('/files') }}" class="inline-flex items-center px-4 py-2 bg-neutral-900 hover:bg-neutral-800 text-white text-sm font-medium rounded-xl shadow-sm transition-all hover:shadow-md">
                                My Files
                            </a>
                        @else
                            <button
                                onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })"
                                class="text-sm text-neutral-500 hover:text-neutral-900 transition-colors cursor-pointer"
                            >
                                Log in
                            </button>
                            <button
                                onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })"
                                class="inline-flex items-center px-4 py-2 bg-neutral-900 hover:bg-neutral-800 text-white text-sm font-medium rounded-xl shadow-sm transition-all hover:shadow-md cursor-pointer"
                            >
                                Register
                            </button>
                        @endauth
                    </nav>
                @endif
            </div>
        </div>
    </header>

    <!-- Guest Notice - Monochrome Style -->
    @guest
        <div class="bg-neutral-100 border-b border-neutral-200 px-4 py-2">
            <div class="max-w-full mx-auto flex items-center justify-center">
                <div class="w-5 h-5 bg-neutral-900 rounded-lg flex items-center justify-center mr-2 flex-shrink-0">
                    <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                </div>
                <p class="text-[11px] text-neutral-500">
                    <span class="font-semibold text-neutral-900">Guest Mode</span> - Chat history won't be saved.
                    <button onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })" class="underline text-neutral-700 hover:text-neutral-900 transition-colors cursor-pointer">Log in</button> or
                    <button onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })" class="underline text-neutral-700 hover:text-neutral-900 transition-colors cursor-pointer">register</button>
                    to save conversations.
                </p>
            </div>
        </div>
    @endguest

    <!-- Main Content -->
    <main class="flex-1 p-4">
        @livewire('file-generator')
    </main>

    <!-- Auth Modal -->
    @livewire('auth-modal')

    @livewireScripts
</body>
</html>
