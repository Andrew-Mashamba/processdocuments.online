#!/usr/bin/env tsx

/**
 * User Identification Script
 *
 * Extracts user information from the session context and updates USER.md
 * This runs at session start to dynamically identify the current user.
 */

import Database from 'better-sqlite3';
import * as fs from 'fs';
import * as path from 'path';

interface User {
  id: number;
  name: string;
  email: string;
  gender: string | null;
  phone: string | null;
  country: string | null;
  timezone: string | null;
  bio: string | null;
  is_admin: boolean;
  preferences: string | null;
}

interface UserContext {
  userId: string | null;
  userName: string | null;
  sessionKey: string | null;
}

/**
 * Extract user context from session key
 * Session key format: agent:main:webchat:direct:{userId}
 */
function extractUserContext(): UserContext {
  // Check runtime context from environment or process arguments
  const runtimeContext = process.env.RUNTIME_CONTEXT || '';

  // Extract session key from runtime context
  const sessionMatch = runtimeContext.match(/Session:\s*([^\s|]+)/i);
  const sessionKey = sessionMatch ? sessionMatch[1] : null;

  if (!sessionKey) {
    return { userId: null, userName: null, sessionKey: null };
  }

  // Parse session key: agent:main:webchat:direct:{userId}
  const parts = sessionKey.split(':');
  const userId = parts.length >= 5 ? parts[4] : null;

  // Check if it's a numeric user ID (not "unknown" or other placeholder)
  const isNumericId = userId && /^\d+$/.test(userId);

  return {
    userId: isNumericId ? userId : null,
    userName: null,
    sessionKey,
  };
}

/**
 * Query user from Laravel database
 */
function queryUser(userId: string): User | null {
  const dbPath = path.resolve(__dirname, '../../zima-frontend/database/database.sqlite');

  if (!fs.existsSync(dbPath)) {
    console.error(`Database not found at: ${dbPath}`);
    return null;
  }

  try {
    const db = new Database(dbPath, { readonly: true });

    const user = db.prepare(`
      SELECT
        id,
        name,
        email,
        gender,
        phone,
        country,
        timezone,
        bio,
        is_admin,
        preferences
      FROM users
      WHERE id = ?
    `).get(userId) as User | undefined;

    db.close();

    return user || null;
  } catch (error) {
    console.error('Database query error:', error);
    return null;
  }
}

/**
 * Generate USER.md content
 */
function generateUserMd(user: User | null): string {
  if (!user) {
    return `# USER.md - Who I'm Helping

## Guest User

**Status**: Anonymous Guest
**Name**: Guest
**Context**: No user profile available (guest session)

## Notes

This is an anonymous session. The user has not logged in or registered.
Once they register or log in, this file will be updated with their profile information.

---

*This file is auto-generated. Last updated: ${new Date().toISOString()}*
`;
  }

  // Parse preferences if it's a JSON string
  let preferences: Record<string, any> = {};
  if (user.preferences) {
    try {
      preferences = typeof user.preferences === 'string'
        ? JSON.parse(user.preferences)
        : user.preferences;
    } catch {
      preferences = {};
    }
  }

  const genderLabel = user.gender
    ? user.gender.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase())
    : 'Not specified';

  return `# USER.md - Who I'm Helping

## ${user.name}

**User ID**: ${user.id}
**Email**: ${user.email}
**Gender**: ${genderLabel}
${user.phone ? `**Phone**: ${user.phone}` : ''}
${user.country ? `**Country**: ${user.country}` : ''}
${user.timezone ? `**Timezone**: ${user.timezone}` : ''}
**Admin**: ${user.is_admin ? 'Yes' : 'No'}

${user.bio ? `## Bio\n\n${user.bio}\n` : ''}

## Preferences

${Object.keys(preferences).length > 0
  ? Object.entries(preferences)
      .map(([key, value]) => `- **${key}**: ${value}`)
      .join('\n')
  : 'No preferences set yet.'
}

## Context

This user is registered and authenticated. Treat them as a valued returning user.
Personalize interactions based on their profile information.

---

*This file is auto-generated. Last updated: ${new Date().toISOString()}*
`;
}

/**
 * Update USER.md file
 */
function updateUserMd(content: string): void {
  const userMdPath = path.resolve(__dirname, '../workspace/USER.md');

  // Ensure workspace directory exists
  const workspaceDir = path.dirname(userMdPath);
  if (!fs.existsSync(workspaceDir)) {
    fs.mkdirSync(workspaceDir, { recursive: true });
  }

  fs.writeFileSync(userMdPath, content, 'utf-8');
  console.log(`✅ USER.md updated: ${userMdPath}`);
}

/**
 * Main execution
 */
function main() {
  console.log('🔍 Identifying user from session context...');

  const context = extractUserContext();
  console.log('Session context:', context);

  if (!context.userId) {
    console.log('👤 Guest user detected (no user_id in session)');
    const content = generateUserMd(null);
    updateUserMd(content);
    return;
  }

  console.log(`🔎 Querying user ${context.userId} from database...`);
  const user = queryUser(context.userId);

  if (!user) {
    console.error(`❌ User ${context.userId} not found in database`);
    const content = generateUserMd(null);
    updateUserMd(content);
    return;
  }

  console.log(`✅ Found user: ${user.name} (${user.email})`);
  const content = generateUserMd(user);
  updateUserMd(content);
}

// Run if executed directly
if (require.main === module) {
  main();
}

export { extractUserContext, queryUser, generateUserMd, updateUserMd };
