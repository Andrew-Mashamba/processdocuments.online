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
        Schema::create('chat_sessions', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->uuid('parent_session_id')->nullable();
            $table->string('title')->default('New Chat');
            $table->integer('message_count')->default(0);
            $table->integer('prompt_tokens')->default(0);
            $table->integer('completion_tokens')->default(0);
            $table->decimal('cost', 10, 6)->default(0);
            $table->uuid('summary_message_id')->nullable();
            $table->string('working_directory')->nullable();
            $table->timestamps();

            $table->foreign('parent_session_id')
                ->references('id')
                ->on('chat_sessions')
                ->onDelete('cascade');
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('chat_sessions');
    }
};
