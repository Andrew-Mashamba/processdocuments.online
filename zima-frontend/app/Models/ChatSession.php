<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Concerns\HasUuids;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class ChatSession extends Model
{
    use HasUuids;

    protected $table = 'chat_sessions';

    protected $fillable = [
        'user_id',
        'title',
        'parent_session_id',
        'message_count',
        'prompt_tokens',
        'completion_tokens',
        'cost',
        'summary_message_id',
        'model',
        'working_directory',
    ];

    protected $casts = [
        'message_count' => 'integer',
        'prompt_tokens' => 'integer',
        'completion_tokens' => 'integer',
        'cost' => 'decimal:6',
    ];

    public function messages(): HasMany
    {
        return $this->hasMany(ChatMessage::class, 'session_id')->orderBy('created_at');
    }

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    public function parent(): BelongsTo
    {
        return $this->belongsTo(ChatSession::class, 'parent_session_id');
    }

    public function children(): HasMany
    {
        return $this->hasMany(ChatSession::class, 'parent_session_id');
    }

    public function files(): HasMany
    {
        return $this->hasMany(SessionFile::class, 'session_id');
    }

    /**
     * Get files that should be included in context
     */
    public function contextFiles(): HasMany
    {
        return $this->files()->where('include_in_context', true);
    }

    public function addMessage(string $role, string $content, ?string $model = null): ChatMessage
    {
        $message = $this->messages()->create([
            'role' => $role,
            'parts' => json_encode([['type' => 'text', 'content' => $content]]),
            'model' => $model,
        ]);

        $this->increment('message_count');

        return $message;
    }

    /**
     * Get conversation context as plain text (fallback without caching)
     */
    public function getConversationContext(): string
    {
        $context = "";
        $messages = $this->getMessagesForContext();

        foreach ($messages as $message) {
            $role = $message->role === 'user' ? 'User' : 'Assistant';
            $content = $message->getTextContent();
            $context .= "{$role}: {$content}\n\n";
        }
        return $context;
    }

    /**
     * Get structured messages for prompt caching.
     * Returns messages with role/content suitable for cache_control markers.
     * If a summary exists, starts from the summary message (skipping old messages).
     */
    public function getMessagesForCaching(): array
    {
        $messages = $this->getMessagesForContext();

        return $messages->map(function ($message) {
            return [
                'role' => $message->role === 'user' ? 'user' : 'assistant',
                'content' => $message->getTextContent(),
                'isSummary' => $message->id === $this->summary_message_id,
            ];
        })->values()->toArray();
    }

    /**
     * Get messages for context, starting from summary if available.
     */
    protected function getMessagesForContext()
    {
        $messages = $this->messages;

        // If we have a summary, start from the summary message (skip old messages)
        if ($this->summary_message_id) {
            $summaryIndex = $messages->search(fn($m) => $m->id === $this->summary_message_id);
            if ($summaryIndex !== false) {
                $messages = $messages->slice($summaryIndex);
            }
        }

        return $messages;
    }

    public function addUsage(int $inputTokens, int $outputTokens, float $cost, ?string $model = null): void
    {
        $this->increment('prompt_tokens', $inputTokens);
        $this->increment('completion_tokens', $outputTokens);
        $this->increment('cost', $cost);

        if ($model && !$this->model) {
            $this->update(['model' => $model]);
        }
    }

    /**
     * Check if session needs summarization.
     * Summarizes when message count exceeds threshold since last summary.
     */
    public function needsSummarization(int $maxMessages = 20): bool
    {
        // If no summary exists, check total count
        if (!$this->summary_message_id) {
            return $this->message_count > $maxMessages;
        }

        // If summary exists, check messages since summary
        $messagesSinceSummary = $this->messages()
            ->where('created_at', '>', function ($query) {
                $query->select('created_at')
                    ->from('chat_messages')
                    ->where('id', $this->summary_message_id)
                    ->limit(1);
            })
            ->count();

        return $messagesSinceSummary > $maxMessages;
    }

    /**
     * Set the summary message ID and mark all previous messages as summarized.
     */
    public function setSummary(string $messageId): void
    {
        $this->update(['summary_message_id' => $messageId]);
    }

    /**
     * Get the count of active messages (since last summary or all if no summary).
     */
    public function getActiveMessageCount(): int
    {
        return $this->getMessagesForContext()->count();
    }
}
