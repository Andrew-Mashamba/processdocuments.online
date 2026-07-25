<?php

use Livewire\Component;

new class extends Component
{
    public int $count = 0;

    public function increment()
    {
        $this->count++;
    }

    public function decrement()
    {
        $this->count--;
    }
};
?>

<div class="p-6 bg-white rounded-lg shadow-lg max-w-md mx-auto">
    <h2 class="text-2xl font-bold mb-4 text-gray-800">Counter Component</h2>

    <div class="flex items-center justify-center gap-4 mb-6">
        <button
            wire:click="decrement"
            class="px-4 py-2 bg-red-500 hover:bg-red-600 text-white rounded-lg font-semibold transition">
            -
        </button>

        <span class="text-4xl font-bold text-gray-900">{{ $count }}</span>

        <button
            wire:click="increment"
            class="px-4 py-2 bg-green-500 hover:bg-green-600 text-white rounded-lg font-semibold transition">
            +
        </button>
    </div>

    <div class="text-center text-gray-600">
        <p>Current count: {{ $count }}</p>
    </div>
</div>
