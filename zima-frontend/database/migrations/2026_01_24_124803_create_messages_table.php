<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('chat_messages', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->uuid('session_id');
            $table->string('role'); // 'user', 'assistant', 'tool', 'system'
            $table->json('parts'); // Polymorphic content parts (text, tool_call, tool_result, etc.)
            $table->string('model')->nullable(); // Which model generated this
            $table->timestamp('finished_at')->nullable();
            $table->timestamps();

            $table->foreign('session_id')
                ->references('id')
                ->on('chat_sessions')
                ->onDelete('cascade');

            $table->index(['session_id', 'created_at']);
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('chat_messages');
    }
};
