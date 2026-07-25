# User Identification System

## Overview

This system dynamically identifies users from their session context and updates `USER.md` with their profile information. It integrates seamlessly with Laravel authentication and runs automatically before each AI response.

## Architecture

### Components

1. **User Identification Script** (`scripts/identify-user.ts`)
   - Standalone TypeScript module
   - Extracts user ID from session key
   - Queries Laravel database (SQLite)
   - Generates USER.md content

2. **Hybrid Context Manager Hook** (`src/context/hybrid-context-manager.ts`)
   - Calls identification before building system prompt
   - Works for both streaming and non-streaming requests
   - Handles errors gracefully (falls back to guest mode)

3. **Registration System** (`zima-frontend/`)
   - Enhanced registration form with gender, phone, country fields
   - Database migration for new user profile fields
   - Updated User model with new fillable attributes

## How It Works

### Session Key Format

```
agent:main:webchat:direct:{userId}
```

Examples:
- Guest: `agent:main:webchat:direct:unknown`
- User ID 2: `agent:main:webchat:direct:2`
- User ID 123: `agent:main:webchat:direct:123`

### Execution Flow

```
1. User sends message from Laravel frontend
   ↓
2. Message router builds session key with user_id
   ↓
3. Hybrid Context Manager acquires session lock
   ↓
4. [NEW] Identify user and update USER.md
   ↓
5. Build OpenClaw system prompt (reads USER.md)
   ↓
6. Invoke agent runtime
   ↓
7. AI responds with personalized context
```

### Database Schema

```sql
-- users table (enhanced)
CREATE TABLE users (
  id INTEGER PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  password VARCHAR(255) NOT NULL,

  -- NEW FIELDS
  gender VARCHAR(50),           -- male, female, other, prefer_not_to_say
  phone VARCHAR(20),             -- optional
  country VARCHAR(100),          -- optional
  timezone VARCHAR(50),          -- auto-set to app.timezone
  bio TEXT,                      -- optional (future use)
  preferences JSON,              -- optional (future use)

  -- existing fields
  is_admin BOOLEAN DEFAULT 0,
  total_spent DECIMAL(10,6) DEFAULT 0,
  total_tokens_used INTEGER DEFAULT 0,
  provider VARCHAR(50),
  provider_id VARCHAR(255),
  avatar VARCHAR(2048),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
);
```

## USER.md Output

### For Authenticated Users

```markdown
# USER.md - Who I'm Helping

## Andrew Mashamba

**User ID**: 2
**Email**: andrew.s.mashamba@gmail.com
**Gender**: Male
**Phone**: +1234567890
**Country**: United States
**Timezone**: America/New_York
**Admin**: No

## Bio

Passionate developer interested in AI and document processing.

## Preferences

- **theme**: dark
- **language**: en
- **notifications**: enabled

## Context

This user is registered and authenticated. Treat them as a valued returning user.
Personalize interactions based on their profile information.

---

*This file is auto-generated. Last updated: 2026-02-02T08:28:11.700Z*
```

### For Guest Users

```markdown
# USER.md - Who I'm Helping

## Guest User

**Status**: Anonymous Guest
**Name**: Guest
**Context**: No user profile available (guest session)

## Notes

This is an anonymous session. The user has not logged in or registered.
Once they register or log in, this file will be updated with their profile information.

---

*This file is auto-generated. Last updated: 2026-02-02T08:27:50.076Z*
```

## Testing

### Manual Test (Standalone Script)

```bash
# Test guest user
RUNTIME_CONTEXT="Session: agent:main:webchat:direct:unknown" npx tsx scripts/identify-user.ts

# Test authenticated user (user_id = 2)
RUNTIME_CONTEXT="Session: agent:main:webchat:direct:2" npx tsx scripts/identify-user.ts

# View generated USER.md
cat workspace/USER.md
```

### Automated Test

```bash
./scripts/test-user-identification.sh
```

### Integration Test (Full Stack)

1. Start Laravel frontend: `cd ../zima-frontend && php artisan serve`
2. Start Gateway: `npm run dev`
3. Register a new user or log in
4. Send a message via the chat interface
5. Check gateway logs: You'll see "✅ Found user: {name}"
6. Check `workspace/USER.md`: Should contain your profile

## Files Modified

### Gateway (gateway/)
- `scripts/identify-user.ts` - NEW: User identification logic
- `scripts/test-user-identification.sh` - NEW: Test script
- `src/context/hybrid-context-manager.ts` - Modified: Added identification hook

### Laravel Frontend (../zima-frontend/)
- `database/migrations/2026_02_02_082330_add_user_profile_fields_to_users_table.php` - NEW
- `app/Models/User.php` - Modified: Added new fillable fields and casts
- `resources/views/auth/register.blade.php` - Modified: Added gender, phone, country fields
- `app/Actions/Fortify/CreateNewUser.php` - Modified: Handle new fields in registration

## Configuration

### Environment Variables

No new environment variables required. The system uses:

- **Database Path**: Automatically resolved to `../zima-frontend/database/database.sqlite`
- **Workspace Path**: `config.storage.workspace` or `./workspace`

### Database Connection

The identification script connects to Laravel's SQLite database using `better-sqlite3` (already installed as a dependency).

Connection details:
```typescript
const dbPath = path.resolve(__dirname, '../../zima-frontend/database/database.sqlite');
const db = new Database(dbPath, { readonly: true });
```

## Error Handling

The system is designed to **fail gracefully**:

1. **User ID not found in database** → Falls back to guest mode
2. **Database connection error** → Falls back to guest mode
3. **Invalid session key format** → Falls back to guest mode
4. **Better-sqlite3 not installed** → Falls back to guest mode

All errors are logged to console but do NOT crash the gateway.

## Future Enhancements

### Phase 1 (Current Implementation)
- ✅ Dynamic user identification from session
- ✅ Database-backed user profiles
- ✅ Enhanced registration with gender, phone, country
- ✅ Auto-generated USER.md

### Phase 2 (Planned)
- [ ] User preferences (theme, language, notification settings)
- [ ] User bio (editable in profile settings)
- [ ] Session history tracking per user
- [ ] Usage analytics per user

### Phase 3 (Future)
- [ ] Multi-language support (read from user.preferences)
- [ ] Timezone-aware responses (use user.timezone)
- [ ] Personalized greeting based on user history
- [ ] User-specific memory context

## API Integration

If you want to query user info from external services:

```typescript
// Example: Get current user from session
import { extractUserContext, queryUser } from './scripts/identify-user';

const context = extractUserContext();
if (context.userId) {
  const user = queryUser(context.userId);
  console.log('Current user:', user.name);
}
```

## Troubleshooting

### USER.md not updating?

1. Check session key format in logs
2. Verify user exists in database:
   ```bash
   sqlite3 ../zima-frontend/database/database.sqlite "SELECT * FROM users WHERE id = 2;"
   ```
3. Check workspace directory permissions
4. Review gateway console logs for errors

### Guest mode when user is logged in?

1. Verify Laravel is sending correct `sender.id` in API requests
2. Check `zima-frontend/app/Livewire/FileGenerator.php`:
   ```php
   $sender = [
       'id' => Auth::id() ?? 'unknown',
       'name' => Auth::user()?->name ?? 'Guest'
   ];
   ```

### Database connection errors?

1. Verify SQLite database exists:
   ```bash
   ls -la ../zima-frontend/database/database.sqlite
   ```
2. Check file permissions (should be readable by gateway process)
3. Ensure `better-sqlite3` is installed:
   ```bash
   npm list better-sqlite3
   ```

## Security Considerations

- Database is opened in **readonly mode** (no write operations from gateway)
- User passwords are NEVER read or exposed
- Sensitive fields (password, remember_token, two_factor_secret) are excluded
- User identification happens server-side (not client-provided)
- Session keys are validated before database queries

## Performance

- User identification adds **~5-10ms** to request latency
- Database query is cached within a single request
- SQLite read operations are extremely fast (<1ms)
- No impact on streaming performance

## Maintenance

### Adding New User Fields

1. Create Laravel migration
2. Update User model fillable array
3. Update registration form
4. Update CreateNewUser action
5. Update `identify-user.ts` SQL query
6. Update `updateUserMdForUser()` template

### Switching to MySQL/PostgreSQL

Replace `better-sqlite3` with appropriate driver:

```typescript
// For MySQL
import mysql from 'mysql2/promise';
const connection = await mysql.createConnection({
  host: process.env.DB_HOST,
  user: process.env.DB_USERNAME,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_DATABASE
});

const [rows] = await connection.execute(
  'SELECT * FROM users WHERE id = ?',
  [userId]
);
```

## Support

For issues or questions:
- Check gateway logs: `logs/gateway.log`
- Review request logs: `logs/requests/requests.jsonl`
- Test standalone script: `npx tsx scripts/identify-user.ts`
- Verify database connectivity: `sqlite3 ../zima-frontend/database/database.sqlite`

---

*Last updated: 2026-02-02*
*Version: 1.0.0*
