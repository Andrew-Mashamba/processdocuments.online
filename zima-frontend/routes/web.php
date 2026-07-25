<?php

use App\Livewire\FileGenerator;
use App\Livewire\Admin\Dashboard as AdminDashboard;
use App\Http\Controllers\SocialAuthController;
use Illuminate\Support\Facades\Route;

// Public Pages
Route::get('/', function () {
    return view('home');
})->name('home');

Route::get('/tools', function () {
    return view('tools');
})->name('tools');

Route::get('/how-it-works', function () {
    return view('how-it-works');
})->name('how-it-works');

Route::get('/faq', function () {
    return view('faq');
})->name('faq');

Route::get('/about', function () {
    return view('about');
})->name('about');

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
    Route::get('/chat', \App\Livewire\ChatInterface::class)->name('chat');
});

// Admin routes
Route::middleware([
    'auth:sanctum',
    config('jetstream.auth_session'),
    'verified',
    \App\Http\Middleware\AdminMiddleware::class,
])->prefix('admin')->name('admin.')->group(function () {
    Route::get('/', AdminDashboard::class)->name('dashboard');
    Route::get('/gateway', \App\Livewire\Admin\GatewayMonitor::class)->name('gateway');
});
