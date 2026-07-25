<x-guest-layout>
    <div class="min-h-screen bg-neutral-50 flex flex-col">
        <!-- Header Section (30% of screen) -->
        <header class="h-[30vh] flex flex-col items-center justify-center px-6">
            <!-- Logo -->
            <div class="w-20 h-20 bg-white rounded-2xl shadow-sm flex items-center justify-center mb-4">
                <x-application-mark class="w-16 h-16" />
            </div>

            <!-- Title -->
            <h1 class="text-xl font-bold text-neutral-900 text-center">Create Account</h1>
            <p class="text-sm text-neutral-500 text-center mt-1">Join ZIMA AI to start generating documents</p>
        </header>

        <!-- Content Section (Flexible) -->
        <main class="flex-1 min-h-0 overflow-auto px-6 pb-6">
            <div class="max-w-md mx-auto">
                <!-- Validation Errors Card -->
                @if ($errors->any())
                    <div class="bg-white rounded-2xl shadow-sm p-4 mb-4 border border-neutral-200">
                        <h3 class="text-[15px] font-semibold text-neutral-900 mb-2">Please correct the following errors:</h3>
                        <ul class="list-disc list-inside space-y-1">
                            @foreach ($errors->all() as $error)
                                <li class="text-sm text-neutral-600">{{ $error }}</li>
                            @endforeach
                        </ul>
                    </div>
                @endif

                <!-- Registration Form Card -->
                <div class="bg-white rounded-2xl shadow-md p-6">
                    <form method="POST" action="{{ route('register') }}" class="space-y-4">
                        @csrf

                        <!-- Name Field -->
                        <div>
                            <label for="name" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Full Name
                            </label>
                            <input
                                id="name"
                                type="text"
                                name="name"
                                value="{{ old('name') }}"
                                required
                                autofocus
                                autocomplete="name"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="Enter your full name"
                            />
                        </div>

                        <!-- Email Field -->
                        <div>
                            <label for="email" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Email Address
                            </label>
                            <input
                                id="email"
                                type="email"
                                name="email"
                                value="{{ old('email') }}"
                                required
                                autocomplete="username"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="your.email@example.com"
                            />
                        </div>

                        <!-- Gender Field -->
                        <div>
                            <label for="gender" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Gender
                            </label>
                            <select
                                id="gender"
                                name="gender"
                                required
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all appearance-none cursor-pointer"
                                style="background-image: url('data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 fill=%27none%27 viewBox=%270 0 20 20%27%3E%3Cpath stroke=%27%236b7280%27 stroke-linecap=%27round%27 stroke-linejoin=%27round%27 stroke-width=%271.5%27 d=%27M6 8l4 4 4-4%27/%3E%3C/svg%3E'); background-position: right 0.75rem center; background-repeat: no-repeat; background-size: 1.25rem;"
                            >
                                <option value="">Select your gender</option>
                                <option value="male" {{ old('gender') == 'male' ? 'selected' : '' }}>Male</option>
                                <option value="female" {{ old('gender') == 'female' ? 'selected' : '' }}>Female</option>
                                <option value="other" {{ old('gender') == 'other' ? 'selected' : '' }}>Other</option>
                                <option value="prefer_not_to_say" {{ old('gender') == 'prefer_not_to_say' ? 'selected' : '' }}>Prefer not to say</option>
                            </select>
                        </div>

                        <!-- Phone Field (Optional) -->
                        <div>
                            <label for="phone" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Phone Number <span class="text-neutral-400 text-xs">(Optional)</span>
                            </label>
                            <input
                                id="phone"
                                type="tel"
                                name="phone"
                                value="{{ old('phone') }}"
                                autocomplete="tel"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="+1 (555) 000-0000"
                            />
                        </div>

                        <!-- Country Field (Optional) -->
                        <div>
                            <label for="country" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Country <span class="text-neutral-400 text-xs">(Optional)</span>
                            </label>
                            <input
                                id="country"
                                type="text"
                                name="country"
                                value="{{ old('country') }}"
                                autocomplete="country"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="United States"
                            />
                        </div>

                        <!-- Password Field -->
                        <div>
                            <label for="password" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Password
                            </label>
                            <input
                                id="password"
                                type="password"
                                name="password"
                                required
                                autocomplete="new-password"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="Create a strong password"
                            />
                        </div>

                        <!-- Confirm Password Field -->
                        <div>
                            <label for="password_confirmation" class="block text-[13px] font-medium text-neutral-900 mb-1">
                                Confirm Password
                            </label>
                            <input
                                id="password_confirmation"
                                type="password"
                                name="password_confirmation"
                                required
                                autocomplete="new-password"
                                class="w-full px-4 py-3 bg-neutral-50 border border-neutral-200 rounded-xl text-sm text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-neutral-300 focus:border-transparent transition-all"
                                placeholder="Re-enter your password"
                            />
                        </div>

                        <!-- Terms and Privacy Policy -->
                        @if (Laravel\Jetstream\Jetstream::hasTermsAndPrivacyPolicyFeature())
                            <div class="pt-2">
                                <label for="terms" class="flex items-start gap-3 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        name="terms"
                                        id="terms"
                                        required
                                        class="mt-1 w-4 h-4 text-neutral-900 bg-neutral-50 border-neutral-300 rounded focus:ring-2 focus:ring-neutral-300"
                                    />
                                    <div class="text-xs text-neutral-600 leading-relaxed">
                                        {!! __('I agree to the :terms_of_service and :privacy_policy', [
                                            'terms_of_service' => '<a target="_blank" href="'.route('terms.show').'" class="underline text-neutral-900 hover:text-neutral-700 font-medium">'.__('Terms of Service').'</a>',
                                            'privacy_policy' => '<a target="_blank" href="'.route('policy.show').'" class="underline text-neutral-900 hover:text-neutral-700 font-medium">'.__('Privacy Policy').'</a>',
                                        ]) !!}
                                    </div>
                                </label>
                            </div>
                        @endif

                        <!-- Submit Button -->
                        <div class="pt-4">
                            <button
                                type="submit"
                                class="w-full min-h-[56px] bg-neutral-900 hover:bg-neutral-800 active:bg-neutral-700 text-white font-semibold text-[15px] rounded-xl shadow-md hover:shadow-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-neutral-400 focus:ring-offset-2"
                            >
                                Create Account
                            </button>
                        </div>

                        <!-- Already Registered Link -->
                        <div class="text-center pt-2">
                            <a
                                href="{{ route('login') }}"
                                class="text-sm text-neutral-600 hover:text-neutral-900 font-medium transition-colors"
                            >
                                Already registered? <span class="underline">Sign in</span>
                            </a>
                        </div>
                    </form>
                </div>

                <!-- Bottom spacing -->
                <div class="h-6"></div>
            </div>
        </main>
    </div>
</x-guest-layout>
