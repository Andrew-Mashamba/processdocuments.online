<?php

namespace App\Http\Controllers;

use App\Models\User;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;
use Laravel\Socialite\Facades\Socialite;

class SocialAuthController extends Controller
{
    /**
     * Supported social providers
     */
    protected array $providers = ['google', 'apple', 'facebook'];

    /**
     * Redirect to the social provider
     */
    public function redirect(string $provider)
    {
        if (!in_array($provider, $this->providers)) {
            return redirect('/')->with('error', 'Unsupported authentication provider.');
        }

        return Socialite::driver($provider)->redirect();
    }

    /**
     * Handle the callback from the social provider
     */
    public function callback(string $provider)
    {
        if (!in_array($provider, $this->providers)) {
            return redirect('/')->with('error', 'Unsupported authentication provider.');
        }

        try {
            $socialUser = Socialite::driver($provider)->user();

            // Find existing user by provider ID or email
            $user = User::where('provider', $provider)
                ->where('provider_id', $socialUser->getId())
                ->first();

            if (!$user) {
                // Check if user exists with the same email
                $user = User::where('email', $socialUser->getEmail())->first();

                if ($user) {
                    // Link the social account to existing user
                    $user->update([
                        'provider' => $provider,
                        'provider_id' => $socialUser->getId(),
                        'avatar' => $socialUser->getAvatar(),
                    ]);
                } else {
                    // Create new user
                    $user = User::create([
                        'name' => $socialUser->getName() ?? $socialUser->getNickname() ?? 'User',
                        'email' => $socialUser->getEmail(),
                        'password' => Hash::make(Str::random(24)),
                        'provider' => $provider,
                        'provider_id' => $socialUser->getId(),
                        'avatar' => $socialUser->getAvatar(),
                        'email_verified_at' => now(),
                    ]);
                }
            } else {
                // Update avatar if changed
                $user->update([
                    'avatar' => $socialUser->getAvatar(),
                ]);
            }

            Auth::login($user, true);

            return redirect('/')->with('success', 'Successfully logged in!');

        } catch (\Exception $e) {
            return redirect('/')->with('error', 'Authentication failed. Please try again.');
        }
    }
}
