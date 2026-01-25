<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Concerns\HasUuids;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class SessionFile extends Model
{
    use HasUuids;

    protected $table = 'session_files';

    protected $fillable = [
        'session_id',
        'user_id',
        'filename',
        'original_filename',
        'mime_type',
        'size',
        'storage_path',
        'include_in_context',
    ];

    protected $casts = [
        'size' => 'integer',
        'include_in_context' => 'boolean',
    ];

    public function session(): BelongsTo
    {
        return $this->belongsTo(ChatSession::class, 'session_id');
    }

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    /**
     * Get the download URL for this file
     */
    public function getDownloadUrlAttribute(): string
    {
        return "/api/files/session/{$this->session_id}/{$this->filename}/download";
    }

    /**
     * Get formatted file size
     */
    public function getSizeFormattedAttribute(): string
    {
        $bytes = $this->size;
        $units = ['B', 'KB', 'MB', 'GB'];
        $index = 0;

        while ($bytes >= 1024 && $index < count($units) - 1) {
            $bytes /= 1024;
            $index++;
        }

        return round($bytes, 2) . ' ' . $units[$index];
    }

    /**
     * Check if this is a text file
     */
    public function isTextFile(): bool
    {
        $textMimes = [
            'text/plain',
            'text/csv',
            'text/html',
            'text/css',
            'text/markdown',
            'application/json',
            'application/xml',
            'application/javascript',
        ];

        return in_array($this->mime_type, $textMimes) ||
               str_starts_with($this->mime_type ?? '', 'text/');
    }

    /**
     * Get file extension
     */
    public function getExtensionAttribute(): string
    {
        return strtolower(pathinfo($this->filename, PATHINFO_EXTENSION));
    }
}
