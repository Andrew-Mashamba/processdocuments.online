<?php

namespace App\Http\Controllers;

use App\Models\Notification;
use Illuminate\Http\Request;

class NotificationController extends Controller
{
    public function index()
    {
        return Notification::with(['sender', 'guardians'])->latest()->get();
    }

    public function store(Request $request)
    {
        $validated = $request->validate([
            'title' => 'required|string|max:255',
            'message' => 'required|string',
            'type' => 'required|in:announcement,message,event',
            'sender_id' => 'required|exists:teachers,id',
            'guardian_ids' => 'required|array',
            'guardian_ids.*' => 'exists:guardians,id',
        ]);

        $notification = Notification::create([
            'title' => $validated['title'],
            'message' => $validated['message'],
            'type' => $validated['type'],
            'sender_id' => $validated['sender_id'],
        ]);

        $notification->guardians()->attach($validated['guardian_ids']);

        return response()->json($notification->load(['sender', 'guardians']), 201);
    }
}
