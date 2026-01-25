<?php

namespace App\Livewire;

use Livewire\Component;
use App\Models\User;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\Rules\Password;

class AuthModal extends Component
{
    public bool $showModal = false;
    public string $mode = 'login'; // 'login', 'register', 'email-login', 'email-register'

    // Email auth fields
    public string $name = '';
    public string $email = '';
    public string $password = '';
    public string $password_confirmation = '';

    public ?string $error = null;
    public ?string $success = null;

    protected $listeners = ['openAuthModal' => 'open', 'closeAuthModal' => 'close'];

    public function open(string $mode = 'login')
    {
        $this->mode = $mode;
        $this->showModal = true;
        $this->resetForm();
    }

    public function close()
    {
        $this->showModal = false;
        $this->resetForm();
    }

    public function resetForm()
    {
        $this->name = '';
        $this->email = '';
        $this->password = '';
        $this->password_confirmation = '';
        $this->error = null;
        $this->success = null;
    }

    public function switchMode(string $mode)
    {
        $this->mode = $mode;
        $this->resetForm();
    }

    public function loginWithEmail()
    {
        $this->validate([
            'email' => 'required|email',
            'password' => 'required|min:6',
        ]);

        if (Auth::attempt(['email' => $this->email, 'password' => $this->password])) {
            session()->regenerate();
            $this->close();
            return redirect()->intended('/');
        }

        $this->error = 'Invalid email or password.';
    }

    public function registerWithEmail()
    {
        $this->validate([
            'name' => 'required|string|max:255',
            'email' => 'required|email|unique:users,email',
            'password' => ['required', 'confirmed', Password::min(8)],
        ]);

        $user = User::create([
            'name' => $this->name,
            'email' => $this->email,
            'password' => Hash::make($this->password),
        ]);

        Auth::login($user);

        $this->close();
        return redirect('/');
    }

    public function render()
    {
        return view('livewire.auth-modal');
    }
}
