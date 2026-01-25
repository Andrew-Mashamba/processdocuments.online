<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\BelongsToMany;

class Notification extends Model
{
    protected $fillable = ['title', 'message', 'type', 'sender_id'];

    public function sender(): BelongsTo
    {
        return $this->belongsTo(Teacher::class, 'sender_id');
    }

    public function guardians(): BelongsToMany
    {
        return $this->belongsToMany(Guardian::class)->withPivot('is_read')->withTimestamps();
    }
}
