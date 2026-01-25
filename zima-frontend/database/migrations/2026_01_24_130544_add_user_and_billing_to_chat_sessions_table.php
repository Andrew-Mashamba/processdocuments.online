<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('chat_sessions', function (Blueprint $table) {
            // Link sessions to users
            $table->foreignId('user_id')->nullable()->after('id')->constrained()->nullOnDelete();

            // Model used for the session
            $table->string('model')->nullable()->after('summary_message_id');

            // Add index for user queries
            $table->index(['user_id', 'updated_at']);
        });
    }

    public function down(): void
    {
        Schema::table('chat_sessions', function (Blueprint $table) {
            $table->dropForeign(['user_id']);
            $table->dropColumn(['user_id', 'model']);
        });
    }
};
