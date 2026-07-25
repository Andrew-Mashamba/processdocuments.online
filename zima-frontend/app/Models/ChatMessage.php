<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Concerns\HasUuids;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class ChatMessage extends Model
{
    use HasUuids;

    protected $table = 'chat_messages';

    protected $fillable = [
        'session_id',
        'role',
        'parts',
        'model',
        'finished_at',
    ];

    protected $casts = [
        'parts' => 'array',
        'finished_at' => 'datetime',
    ];

    public function session(): BelongsTo
    {
        return $this->belongsTo(ChatSession::class, 'session_id');
    }

    public function getTextContent(): string
    {
        $parts = $this->getParsedParts();
        $text = [];

        foreach ($parts as $part) {
            if (is_array($part) && ($part['type'] ?? '') === 'text') {
                $text[] = $part['content'] ?? '';
            }
        }

        return implode("\n", $text);
    }

    public function getToolCalls(): array
    {
        $parts = $this->getParsedParts();
        $toolCalls = [];

        foreach ($parts as $part) {
            if (is_array($part) && ($part['type'] ?? '') === 'tool_call') {
                $toolCalls[] = $part;
            }
        }

        return $toolCalls;
    }

    /**
     * Get parts as a properly parsed array
     */
    protected function getParsedParts(): array
    {
        $parts = $this->parts ?? [];

        // Handle case where parts is a string (e.g., double-encoded JSON)
        if (is_string($parts)) {
            $decoded = json_decode($parts, true);
            $parts = is_array($decoded) ? $decoded : [];
        }

        return is_array($parts) ? $parts : [];
    }

    public function hasToolCalls(): bool
    {
        return count($this->getToolCalls()) > 0;
    }

    public function appendTextContent(string $content): void
    {
        $parts = $this->getParsedParts();

        // Find existing text part or create new one
        $found = false;
        foreach ($parts as &$part) {
            if (is_array($part) && ($part['type'] ?? '') === 'text') {
                $part['content'] = ($part['content'] ?? '') . $content;
                $found = true;
                break;
            }
        }

        if (!$found) {
            $parts[] = ['type' => 'text', 'content' => $content];
        }

        $this->parts = $parts;
        $this->save();
    }

    public function markFinished(): void
    {
        $this->finished_at = now();
        $this->save();
    }
}
