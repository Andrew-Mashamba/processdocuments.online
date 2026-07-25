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
        Schema::table('users', function (Blueprint $table) {
            $table->string('gender')->nullable()->after('email');
            $table->string('phone')->nullable()->after('gender');
            $table->string('country')->nullable()->after('phone');
            $table->string('timezone')->nullable()->after('country');
            $table->text('bio')->nullable()->after('timezone');
            $table->json('preferences')->nullable()->after('bio');
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->dropColumn(['gender', 'phone', 'country', 'timezone', 'bio', 'preferences']);
        });
    }
};
