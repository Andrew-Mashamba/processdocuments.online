<?php

namespace App\Providers;

use App\Models\ChatSession;
use Illuminate\Auth\Events\Login;
use Illuminate\Support\Facades\Cookie;
use Illuminate\Support\Facades\Event;
use Illuminate\Support\Facades\Log;
use Illuminate\Support\ServiceProvider;

class AppServiceProvider extends ServiceProvider
{
    /**
     * Register any application services.
     */
    public function register(): void
    {
        //
    }

    /**
     * Bootstrap any application services.
     */
    public function boot(): void
    {
        // Migrate guest sessions when user logs in
        Event::listen(Login::class, function (Login $event) {
            $this->migrateGuestSessions($event->user);
        });
    }

    /**
     * Migrate guest sessions to the logged in user.
     * This preserves the user's chat history from when they were a guest.
     */
    protected function migrateGuestSessions($user): void
    {
        try {
            $guestSessionsCookie = request()->cookie('guest_sessions', '');
            if (empty($guestSessionsCookie)) {
                return;
            }

            $sessionIds = array_filter(explode(',', $guestSessionsCookie));
            if (empty($sessionIds)) {
                return;
            }

            // Migrate all guest sessions to the logged-in user
            $migrated = ChatSession::whereIn('id', $sessionIds)
                ->whereNull('user_id')
                ->update(['user_id' => $user->id]);

            if ($migrated > 0) {
                Log::info("Migrated {$migrated} guest sessions to user {$user->id} on login");

                // Queue removal of guest_sessions cookie
                Cookie::queue(Cookie::forget('guest_sessions'));
            }
        } catch (\Exception $e) {
            Log::error("Failed to migrate guest sessions on login: " . $e->getMessage());
        }
    }
}
