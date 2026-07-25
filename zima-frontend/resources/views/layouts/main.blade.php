<!DOCTYPE html>
<html lang="{{ str_replace('_', '-', app()->getLocale()) }}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="csrf-token" content="{{ csrf_token() }}">

    <!-- Primary SEO Meta Tags -->
    <title>{{ $title ?? 'Process Documents Online - AI-Powered Document Processing' }}</title>
    <meta name="description" content="{{ $description ?? 'Process documents online with AI. Create, convert, merge, and edit PDF, Excel, Word, and PowerPoint files. Free document processing tools.' }}">
    <meta name="author" content="Process Documents Online">
    <meta name="robots" content="index, follow">

    <!-- Canonical URL -->
    <link rel="canonical" href="{{ url()->current() }}">

    <!-- Open Graph / Facebook -->
    <meta property="og:type" content="website">
    <meta property="og:url" content="{{ url()->current() }}">
    <meta property="og:title" content="{{ $ogTitle ?? $title ?? 'Process Documents Online' }}">
    <meta property="og:description" content="{{ $ogDescription ?? $description ?? 'AI-powered document processing platform' }}">
    <meta property="og:site_name" content="Process Documents Online">

    <!-- Theme Color -->
    <meta name="theme-color" content="#171717">

    <!-- Favicon -->
    <link rel="icon" type="image/x-icon" href="{{ asset('favicon.ico') }}">

    <!-- Fonts -->
    <link href="https://fonts.bunny.net/css?family=instrument-sans:400,500,600,700&display=swap" rel="stylesheet" />

    <!-- Styles -->
    @vite(['resources/css/app.css', 'resources/js/app.js'])
    <script src="https://cdn.tailwindcss.com" defer></script>

    @livewireStyles

    <!-- Additional Head Content -->
    @stack('head')

    <style>
        body { font-family: 'Instrument Sans', system-ui, -apple-system, sans-serif; }
        .antialiased { -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }
        [x-cloak] { display: none !important; }
    </style>
</head>
<body class="antialiased bg-neutral-50 min-h-screen flex flex-col">
    <!-- Site Header -->
    <x-site-header />

    <!-- Guest Notice Banner (shown only for non-authenticated users) -->
    @guest
        <div class="bg-neutral-100 border-b border-neutral-200 px-4 py-2">
            <div class="max-w-7xl mx-auto flex items-center justify-center">
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
    <main class="flex-1">
        {{ $slot }}
    </main>

    <!-- Site Footer -->
    <x-site-footer />

    <!-- Auth Modal -->
    @livewire('auth-modal')

    @livewireScripts

    <!-- Additional Scripts -->
    @stack('scripts')
</body>
</html>
