<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class UsageLog extends Model
{
    protected $fillable = [
        'user_id',
        'session_id',
        'message_id',
        'model',
        'input_tokens',
        'output_tokens',
        'cache_creation_tokens',
        'cache_read_tokens',
        'cost',
        'request_type',
    ];

    protected $casts = [
        'input_tokens' => 'integer',
        'output_tokens' => 'integer',
        'cache_creation_tokens' => 'integer',
        'cache_read_tokens' => 'integer',
        'cost' => 'decimal:6',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    public function session(): BelongsTo
    {
        return $this->belongsTo(ChatSession::class, 'session_id');
    }

    public function getTotalTokensAttribute(): int
    {
        return $this->input_tokens + $this->output_tokens;
    }

    public static function logUsage(
        int $userId,
        ?string $sessionId,
        ?string $messageId,
        array $usage,
        string $requestType = 'generate'
    ): self {
        return self::create([
            'user_id' => $userId,
            'session_id' => $sessionId,
            'message_id' => $messageId,
            'model' => $usage['model'] ?? null,
            'input_tokens' => $usage['inputTokens'] ?? 0,
            'output_tokens' => $usage['outputTokens'] ?? 0,
            'cache_creation_tokens' => $usage['cacheCreationTokens'] ?? 0,
            'cache_read_tokens' => $usage['cacheReadTokens'] ?? 0,
            'cost' => $usage['cost'] ?? 0,
            'request_type' => $requestType,
        ]);
    }
}
