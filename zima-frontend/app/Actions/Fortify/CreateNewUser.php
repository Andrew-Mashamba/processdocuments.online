<?php

namespace App\Actions\Fortify;

use App\Models\ChatSession;
use App\Models\Team;
use App\Models\User;
use Illuminate\Support\Facades\Cookie;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Log;
use Illuminate\Support\Facades\Validator;
use Laravel\Fortify\Contracts\CreatesNewUsers;
use Laravel\Jetstream\Jetstream;

class CreateNewUser implements CreatesNewUsers
{
    use PasswordValidationRules;

    /**
     * Create a newly registered user.
     *
     * @param  array<string, string>  $input
     */
    public function create(array $input): User
    {
        Validator::make($input, [
            'name' => ['required', 'string', 'max:255'],
            'email' => ['required', 'string', 'email', 'max:255', 'unique:users'],
            'password' => $this->passwordRules(),
            'terms' => Jetstream::hasTermsAndPrivacyPolicyFeature() ? ['accepted', 'required'] : '',
        ])->validate();

        return DB::transaction(function () use ($input) {
            return tap(User::create([
                'name' => $input['name'],
                'email' => $input['email'],
                'password' => Hash::make($input['password']),
            ]), function (User $user) {
                $this->createTeam($user);
                $this->migrateGuestSessions($user);
            });
        });
    }

    /**
     * Migrate guest sessions to the newly registered user.
     * This preserves the user's chat history from when they were a guest.
     */
    protected function migrateGuestSessions(User $user): void
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

            // Migrate all guest sessions to the new user
            $migrated = ChatSession::whereIn('id', $sessionIds)
                ->whereNull('user_id')
                ->update(['user_id' => $user->id]);

            if ($migrated > 0) {
                Log::info("Migrated {$migrated} guest sessions to user {$user->id}");

                // Queue removal of guest_sessions cookie
                Cookie::queue(Cookie::forget('guest_sessions'));
            }
        } catch (\Exception $e) {
            Log::error("Failed to migrate guest sessions: " . $e->getMessage());
        }
    }

    /**
     * Create a personal team for the user.
     */
    protected function createTeam(User $user): void
    {
        $user->ownedTeams()->save(Team::forceCreate([
            'user_id' => $user->id,
            'name' => explode(' ', $user->name, 2)[0]."'s Team",
            'personal_team' => true,
        ]));
    }
}
