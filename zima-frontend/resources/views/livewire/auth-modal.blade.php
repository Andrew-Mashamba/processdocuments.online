<div>
    @if($showModal)
    <!-- Modal Backdrop -->
    <div
        class="fixed inset-0 z-50 overflow-y-auto"
        x-data="{ show: @entangle('showModal') }"
        x-show="show"
        x-transition:enter="transition ease-out duration-300"
        x-transition:enter-start="opacity-0"
        x-transition:enter-end="opacity-100"
        x-transition:leave="transition ease-in duration-200"
        x-transition:leave-start="opacity-100"
        x-transition:leave-end="opacity-0"
    >
        <!-- Backdrop -->
        <div class="fixed inset-0 bg-black/50 backdrop-blur-sm" wire:click="close"></div>

        <!-- Modal Content -->
        <div class="flex min-h-full items-center justify-center p-4">
            <div
                class="relative w-full max-w-md transform rounded-2xl bg-white p-6 shadow-2xl transition-all"
                x-transition:enter="transition ease-out duration-300"
                x-transition:enter-start="opacity-0 scale-95"
                x-transition:enter-end="opacity-100 scale-100"
                x-transition:leave="transition ease-in duration-200"
                x-transition:leave-start="opacity-100 scale-100"
                x-transition:leave-end="opacity-0 scale-95"
                @click.away="$wire.close()"
            >
                <!-- Close Button -->
                <button
                    wire:click="close"
                    class="absolute right-4 top-4 text-neutral-400 hover:text-neutral-900 transition-colors"
                >
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                    </svg>
                </button>

                <!-- Header -->
                <div class="text-center mb-6">
                    <div class="w-14 h-14 bg-neutral-900 rounded-2xl flex items-center justify-center mx-auto mb-4">
                        <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                        </svg>
                    </div>
                    <h2 class="text-xl font-bold text-neutral-900">
                        @if($mode === 'login' || $mode === 'email-login')
                            Welcome Back
                        @else
                            Create Account
                        @endif
                    </h2>
                    <p class="text-sm text-neutral-500 mt-1">
                        @if($mode === 'login' || $mode === 'email-login')
                            Sign in to continue to ZIMA AI
                        @else
                            Sign up to start generating files
                        @endif
                    </p>
                </div>

                <!-- Error Message -->
                @if($error)
                    <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-xl">
                        <p class="text-sm text-red-600">{{ $error }}</p>
                    </div>
                @endif

                <!-- Success Message -->
                @if($success)
                    <div class="mb-4 p-3 bg-green-50 border border-green-200 rounded-xl">
                        <p class="text-sm text-green-600">{{ $success }}</p>
                    </div>
                @endif

                @if($mode === 'login' || $mode === 'register')
                    <!-- Social Login Buttons -->
                    <div class="space-y-3">
                        <!-- Google -->
                        <a
                            href="{{ route('auth.social', 'google') }}"
                            class="flex items-center justify-center gap-3 w-full px-4 py-3 bg-white border border-neutral-200 rounded-xl hover:bg-neutral-50 hover:border-neutral-300 transition-all"
                        >
                            <svg class="w-5 h-5" viewBox="0 0 24 24">
                                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
                            </svg>
                            <span class="text-sm font-medium text-neutral-700">Continue with Google</span>
                        </a>

                        <!-- Apple -->
                        <a
                            href="{{ route('auth.social', 'apple') }}"
                            class="flex items-center justify-center gap-3 w-full px-4 py-3 bg-black text-white rounded-xl hover:bg-neutral-800 transition-all"
                        >
                            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                                <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.81-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/>
                            </svg>
                            <span class="text-sm font-medium">Continue with Apple</span>
                        </a>

                        <!-- Facebook -->
                        <a
                            href="{{ route('auth.social', 'facebook') }}"
                            class="flex items-center justify-center gap-3 w-full px-4 py-3 bg-[#1877F2] text-white rounded-xl hover:bg-[#166FE5] transition-all"
                        >
                            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
                            </svg>
                            <span class="text-sm font-medium">Continue with Facebook</span>
                        </a>
                    </div>

                    <!-- Divider -->
                    <div class="flex items-center gap-4 my-6">
                        <div class="flex-1 h-px bg-neutral-200"></div>
                        <span class="text-xs text-neutral-400 uppercase tracking-wider">or</span>
                        <div class="flex-1 h-px bg-neutral-200"></div>
                    </div>

                    <!-- Email Button -->
                    <button
                        wire:click="switchMode('{{ $mode === 'login' ? 'email-login' : 'email-register' }}')"
                        class="flex items-center justify-center gap-3 w-full px-4 py-3 bg-neutral-100 text-neutral-700 rounded-xl hover:bg-neutral-200 transition-all"
                    >
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                        </svg>
                        <span class="text-sm font-medium">Continue with Email</span>
                    </button>

                @elseif($mode === 'email-login')
                    <!-- Email Login Form -->
                    <form wire:submit="loginWithEmail" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Email</label>
                            <input
                                type="email"
                                wire:model="email"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="you@example.com"
                            />
                            @error('email') <p class="mt-1 text-xs text-red-500">{{ $message }}</p> @enderror
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Password</label>
                            <input
                                type="password"
                                wire:model="password"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="Enter your password"
                            />
                            @error('password') <p class="mt-1 text-xs text-red-500">{{ $message }}</p> @enderror
                        </div>

                        <button
                            type="submit"
                            class="w-full px-4 py-3 bg-neutral-900 text-white rounded-xl hover:bg-neutral-800 transition-all font-medium"
                            wire:loading.attr="disabled"
                            wire:loading.class="opacity-50 cursor-not-allowed"
                        >
                            <span wire:loading.remove wire:target="loginWithEmail">Sign In</span>
                            <span wire:loading wire:target="loginWithEmail">Signing in...</span>
                        </button>
                    </form>

                    <button
                        wire:click="switchMode('login')"
                        class="w-full mt-4 text-sm text-neutral-500 hover:text-neutral-900 transition-colors"
                    >
                        &larr; Back to all options
                    </button>

                @elseif($mode === 'email-register')
                    <!-- Email Register Form -->
                    <form wire:submit="registerWithEmail" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Full Name</label>
                            <input
                                type="text"
                                wire:model="name"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="John Doe"
                            />
                            @error('name') <p class="mt-1 text-xs text-red-500">{{ $message }}</p> @enderror
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Email</label>
                            <input
                                type="email"
                                wire:model="email"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="you@example.com"
                            />
                            @error('email') <p class="mt-1 text-xs text-red-500">{{ $message }}</p> @enderror
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Password</label>
                            <input
                                type="password"
                                wire:model="password"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="Create a password (min 8 characters)"
                            />
                            @error('password') <p class="mt-1 text-xs text-red-500">{{ $message }}</p> @enderror
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-neutral-700 mb-1.5">Confirm Password</label>
                            <input
                                type="password"
                                wire:model="password_confirmation"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl focus:ring-2 focus:ring-neutral-900 focus:border-neutral-900 transition-all text-sm"
                                placeholder="Confirm your password"
                            />
                        </div>

                        <button
                            type="submit"
                            class="w-full px-4 py-3 bg-neutral-900 text-white rounded-xl hover:bg-neutral-800 transition-all font-medium"
                            wire:loading.attr="disabled"
                            wire:loading.class="opacity-50 cursor-not-allowed"
                        >
                            <span wire:loading.remove wire:target="registerWithEmail">Create Account</span>
                            <span wire:loading wire:target="registerWithEmail">Creating account...</span>
                        </button>
                    </form>

                    <button
                        wire:click="switchMode('register')"
                        class="w-full mt-4 text-sm text-neutral-500 hover:text-neutral-900 transition-colors"
                    >
                        &larr; Back to all options
                    </button>
                @endif

                <!-- Toggle Login/Register -->
                <div class="mt-6 pt-6 border-t border-neutral-100 text-center">
                    @if($mode === 'login' || $mode === 'email-login')
                        <p class="text-sm text-neutral-500">
                            Don't have an account?
                            <button wire:click="switchMode('register')" class="font-medium text-neutral-900 hover:underline">Sign up</button>
                        </p>
                    @else
                        <p class="text-sm text-neutral-500">
                            Already have an account?
                            <button wire:click="switchMode('login')" class="font-medium text-neutral-900 hover:underline">Sign in</button>
                        </p>
                    @endif
                </div>
            </div>
        </div>
    </div>
    @endif
</div>
