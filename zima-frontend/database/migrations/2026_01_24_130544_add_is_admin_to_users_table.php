<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->boolean('is_admin')->default(false)->after('email');
            $table->decimal('total_spent', 10, 6)->default(0)->after('is_admin');
            $table->integer('total_tokens_used')->default(0)->after('total_spent');
        });
    }

    public function down(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->dropColumn(['is_admin', 'total_spent', 'total_tokens_used']);
        });
    }
};
