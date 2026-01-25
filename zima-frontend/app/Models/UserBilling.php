<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class UserBilling extends Model
{
    protected $table = 'user_billing';

    protected $fillable = [
        'user_id',
        'plan',
        'monthly_limit',
        'current_usage',
        'billing_period_start',
        'billing_period_end',
        'is_active',
    ];

    protected $casts = [
        'monthly_limit' => 'decimal:2',
        'current_usage' => 'decimal:6',
        'billing_period_start' => 'datetime',
        'billing_period_end' => 'datetime',
        'is_active' => 'boolean',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    public function getRemainingCreditsAttribute(): float
    {
        return max(0, $this->monthly_limit - $this->current_usage);
    }

    public function getUsagePercentAttribute(): float
    {
        if ($this->monthly_limit <= 0) return 100;
        return min(100, ($this->current_usage / $this->monthly_limit) * 100);
    }

    public function hasCredits(): bool
    {
        return $this->remaining_credits > 0 && $this->is_active;
    }

    public function addUsage(float $cost): void
    {
        $this->increment('current_usage', $cost);
    }

    public function resetBillingPeriod(): void
    {
        $this->update([
            'current_usage' => 0,
            'billing_period_start' => now(),
            'billing_period_end' => now()->addMonth(),
        ]);
    }
}
