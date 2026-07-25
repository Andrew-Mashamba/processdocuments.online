<!-- Shared Site Header - Monochrome Design -->
<header class="bg-white shadow-sm sticky top-0 z-50" x-data="{ mobileMenuOpen: false }">
    <div class="max-w-7xl mx-auto px-4 sm:px-6">
        <div class="flex justify-between items-center h-16">
            <!-- Logo -->
            <a href="{{ route('home') }}" class="flex items-center" title="Process Documents Online - Home">
                <div class="w-10 h-10 bg-neutral-900 rounded-xl flex items-center justify-center mr-3 shadow-sm">
                    <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                    </svg>
                </div>
                <div>
                    <span class="text-[15px] font-bold text-neutral-900">Process Documents Online</span>
                    <p class="text-[11px] text-neutral-500">AI-Powered Document Processing</p>
                </div>
            </a>

            <!-- Navigation Menu -->
            <div class="hidden md:flex items-center gap-8">
                <x-nav-menu />

                <!-- Auth Links -->
                @if (Route::has('login'))
                    <div class="flex items-center gap-3 ml-4 pl-4 border-l border-neutral-200">
                        @auth
                            <a href="{{ url('/dashboard') }}" class="text-sm text-neutral-500 hover:text-neutral-900 transition-colors">
                                Dashboard
                            </a>
                            <a href="{{ url('/files') }}" class="inline-flex items-center px-4 py-2 bg-neutral-900 hover:bg-neutral-800 text-white text-sm font-medium rounded-xl shadow-sm transition-all hover:shadow-md">
                                My Files
                            </a>
                        @endauth
                        @guest
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
                        @endguest
                    </div>
                @endif
            </div>

            <!-- Mobile Menu Button -->
            <button
                @click="mobileMenuOpen = !mobileMenuOpen"
                class="md:hidden p-2 rounded-lg hover:bg-neutral-100 transition-colors"
                aria-label="Toggle menu"
            >
                <svg class="w-6 h-6 text-neutral-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path x-show="!mobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
                    <path x-show="mobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
            </button>
        </div>
    </div>

    <!-- Mobile Menu -->
    <div x-show="mobileMenuOpen"
         x-transition
         class="md:hidden border-t border-neutral-200 bg-white"
         x-cloak>
        <div class="px-4 py-4 space-y-3">
            <a href="{{ route('home') }}" class="block px-4 py-2 rounded-lg {{ request()->routeIs('home') ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-600 hover:bg-neutral-50' }} transition-colors">
                Home
            </a>
            <a href="{{ route('tools') }}" class="block px-4 py-2 rounded-lg {{ request()->routeIs('tools') ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-600 hover:bg-neutral-50' }} transition-colors">
                Tools
            </a>
            <a href="{{ route('how-it-works') }}" class="block px-4 py-2 rounded-lg {{ request()->routeIs('how-it-works') ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-600 hover:bg-neutral-50' }} transition-colors">
                How It Works
            </a>
            <a href="{{ route('faq') }}" class="block px-4 py-2 rounded-lg {{ request()->routeIs('faq') ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-600 hover:bg-neutral-50' }} transition-colors">
                FAQ
            </a>
            <a href="{{ route('about') }}" class="block px-4 py-2 rounded-lg {{ request()->routeIs('about') ? 'bg-neutral-100 text-neutral-900 font-semibold' : 'text-neutral-600 hover:bg-neutral-50' }} transition-colors">
                About
            </a>

            @auth
                <div class="pt-3 mt-3 border-t border-neutral-200 space-y-2">
                    <a href="{{ url('/dashboard') }}" class="block px-4 py-2 text-neutral-600 hover:bg-neutral-50 rounded-lg transition-colors">
                        Dashboard
                    </a>
                    <a href="{{ url('/files') }}" class="block px-4 py-2 bg-neutral-900 text-white rounded-lg hover:bg-neutral-800 transition-colors text-center font-medium">
                        My Files
                    </a>
                </div>
            @endauth
            @guest
                <div class="pt-3 mt-3 border-t border-neutral-200 space-y-2">
                    <button
                        onclick="Livewire.dispatch('openAuthModal', { mode: 'login' })"
                        class="w-full block px-4 py-2 text-neutral-600 hover:bg-neutral-50 rounded-lg transition-colors cursor-pointer text-left"
                    >
                        Log in
                    </button>
                    <button
                        onclick="Livewire.dispatch('openAuthModal', { mode: 'register' })"
                        class="w-full block px-4 py-2 bg-neutral-900 text-white rounded-lg hover:bg-neutral-800 transition-colors text-center font-medium cursor-pointer"
                    >
                        Register
                    </button>
                </div>
            @endguest
        </div>
    </div>
</header>
