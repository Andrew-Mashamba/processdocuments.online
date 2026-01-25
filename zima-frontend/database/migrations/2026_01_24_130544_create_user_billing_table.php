<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        // User billing/subscription info
        Schema::create('user_billing', function (Blueprint $table) {
            $table->id();
            $table->foreignId('user_id')->constrained()->cascadeOnDelete();
            $table->string('plan')->default('free'); // free, basic, pro, enterprise
            $table->decimal('monthly_limit', 10, 2)->default(5.00); // $ limit per month
            $table->decimal('current_usage', 10, 6)->default(0); // $ used this period
            $table->timestamp('billing_period_start')->nullable();
            $table->timestamp('billing_period_end')->nullable();
            $table->boolean('is_active')->default(true);
            $table->timestamps();

            $table->unique('user_id');
        });

        // Usage tracking per request
        Schema::create('usage_logs', function (Blueprint $table) {
            $table->id();
            $table->foreignId('user_id')->constrained()->cascadeOnDelete();
            $table->uuid('session_id')->nullable();
            $table->uuid('message_id')->nullable();
            $table->string('model')->nullable();
            $table->integer('input_tokens')->default(0);
            $table->integer('output_tokens')->default(0);
            $table->integer('cache_creation_tokens')->default(0);
            $table->integer('cache_read_tokens')->default(0);
            $table->decimal('cost', 10, 6)->default(0);
            $table->string('request_type')->default('generate'); // generate, title, summarize
            $table->timestamps();

            $table->index(['user_id', 'created_at']);
            $table->index('session_id');
        });

        // Pricing plans
        Schema::create('pricing_plans', function (Blueprint $table) {
            $table->id();
            $table->string('name')->unique();
            $table->string('display_name');
            $table->text('description')->nullable();
            $table->decimal('price_monthly', 10, 2)->default(0);
            $table->decimal('included_credits', 10, 2)->default(0); // $ worth of API credits
            $table->integer('max_sessions')->default(100);
            $table->integer('max_messages_per_session')->default(50);
            $table->json('features')->nullable(); // Additional features as JSON
            $table->boolean('is_active')->default(true);
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('pricing_plans');
        Schema::dropIfExists('usage_logs');
        Schema::dropIfExists('user_billing');
    }
};
