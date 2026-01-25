<?php

use App\Livewire\FileGenerator;
use App\Livewire\Admin\Dashboard as AdminDashboard;
use App\Http\Controllers\SocialAuthController;
use Illuminate\Support\Facades\Route;

Route::get('/', function () {
    return view('welcome');
});

// Social Authentication Routes
Route::get('/auth/{provider}', [SocialAuthController::class, 'redirect'])->name('auth.social');
Route::get('/auth/{provider}/callback', [SocialAuthController::class, 'callback'])->name('auth.social.callback');

Route::middleware([
    'auth:sanctum',
    config('jetstream.auth_session'),
    'verified',
])->group(function () {
    Route::get('/dashboard', function () {
        return view('dashboard');
    })->name('dashboard');

    Route::get('/files', FileGenerator::class)->name('files');
});

// Admin routes
Route::middleware([
    'auth:sanctum',
    config('jetstream.auth_session'),
    'verified',
    \App\Http\Middleware\AdminMiddleware::class,
])->prefix('admin')->name('admin.')->group(function () {
    Route::get('/', AdminDashboard::class)->name('dashboard');
});
