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
        Schema::create('session_files', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->uuid('session_id');
            $table->foreignId('user_id')->nullable()->constrained()->nullOnDelete();
            $table->string('filename');
            $table->string('original_filename');
            $table->string('mime_type')->nullable();
            $table->bigInteger('size')->default(0);
            $table->string('storage_path'); // Path in uploaded_files/{session_id}/
            $table->boolean('include_in_context')->default(true); // Whether to include in AI context
            $table->timestamps();

            $table->index('session_id');
            $table->index('user_id');
            $table->foreign('session_id')
                ->references('id')
                ->on('chat_sessions')
                ->cascadeOnDelete();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('session_files');
    }
};
