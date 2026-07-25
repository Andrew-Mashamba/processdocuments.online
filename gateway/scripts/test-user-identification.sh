#!/bin/bash

# Test User Identification System
# This script tests the user identification and USER.md generation

set -e

echo "🧪 Testing User Identification System"
echo "======================================"
echo ""

# Test 1: Guest User (no user_id)
echo "Test 1: Guest User Session"
echo "Session Key: agent:main:webchat:direct:unknown"
export RUNTIME_CONTEXT="Session: agent:main:webchat:direct:unknown"
npx tsx scripts/identify-user.ts
echo ""

# Test 2: Numeric User ID (simulate registered user)
echo "Test 2: Authenticated User Session"
echo "Session Key: agent:main:webchat:direct:1"
export RUNTIME_CONTEXT="Session: agent:main:webchat:direct:1"
npx tsx scripts/identify-user.ts
echo ""

# Display generated USER.md
echo "📄 Generated USER.md:"
echo "------------------------------------"
cat workspace/USER.md
echo ""
echo "------------------------------------"
echo ""

echo "✅ Tests Complete!"
echo ""
echo "Next Steps:"
echo "1. Start the gateway server: npm run dev"
echo "2. Send a message from the Laravel frontend"
echo "3. Check logs to see user identification in action"
echo "4. Verify USER.md is updated with your profile"
