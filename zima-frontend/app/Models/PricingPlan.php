<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class PricingPlan extends Model
{
    protected $fillable = [
        'name',
        'display_name',
        'description',
        'price_monthly',
        'included_credits',
        'max_sessions',
        'max_messages_per_session',
        'features',
        'is_active',
    ];

    protected $casts = [
        'price_monthly' => 'decimal:2',
        'included_credits' => 'decimal:2',
        'max_sessions' => 'integer',
        'max_messages_per_session' => 'integer',
        'features' => 'array',
        'is_active' => 'boolean',
    ];

    public static function getDefaultPlans(): array
    {
        return [
            [
                'name' => 'free',
                'display_name' => 'Free',
                'description' => 'Perfect for trying out ZIMA',
                'price_monthly' => 0,
                'included_credits' => 1.00,
                'max_sessions' => 10,
                'max_messages_per_session' => 20,
                'features' => ['Basic file generation', 'Excel, Word, PDF support'],
            ],
            [
                'name' => 'basic',
                'display_name' => 'Basic',
                'description' => 'For regular users',
                'price_monthly' => 9.99,
                'included_credits' => 10.00,
                'max_sessions' => 100,
                'max_messages_per_session' => 50,
                'features' => ['All free features', 'Priority support', 'Session history'],
            ],
            [
                'name' => 'pro',
                'display_name' => 'Pro',
                'description' => 'For power users',
                'price_monthly' => 29.99,
                'included_credits' => 50.00,
                'max_sessions' => -1,
                'max_messages_per_session' => -1,
                'features' => ['All basic features', 'API access', 'Custom templates'],
            ],
            [
                'name' => 'enterprise',
                'display_name' => 'Enterprise',
                'description' => 'For teams and businesses',
                'price_monthly' => 99.99,
                'included_credits' => 200.00,
                'max_sessions' => -1,
                'max_messages_per_session' => -1,
                'features' => ['All pro features', 'Team management', 'SSO', 'Dedicated support'],
            ],
        ];
    }
}
