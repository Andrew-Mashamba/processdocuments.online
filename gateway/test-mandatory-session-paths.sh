#!/bin/bash

# Test script to verify Claude follows mandatory session path instructions
# After system prompt update on 2026-01-31 at 17:52

SESSION_ID="test-session-$(date +%s)"
SESSION_KEY="agent:main:webchat:direct:${SESSION_ID}"

echo "========================================="
echo "Testing Mandatory Session Path Behavior"
echo "========================================="
echo "Session Key: $SESSION_KEY"
echo "Expected file path: /Volumes/DATA/QWEN/zima-file-service/generated_files/${SESSION_KEY}/*.xlsx"
echo "Expected download URL: http://localhost:5000/api/files/generated/${SESSION_KEY}/*.xlsx/download"
echo ""
echo "Sending request..."
echo ""

# Send request to create an Excel file
timeout 60 curl -s -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d "{
    \"message\": \"Create an Excel file with 3 programming languages: Python, JavaScript, TypeScript\",
    \"sessionKey\": \"${SESSION_KEY}\",
    \"channel\": \"webchat\",
    \"sender\": {
      \"id\": \"test-user\",
      \"name\": \"Test User\"
    }
  }" 2>&1 | while IFS= read -r line; do
    # Filter out event: lines and only show data
    if [[ $line == data:* ]]; then
        echo "$line" | sed 's/^data: //' | jq -r '.content // .text // empty' 2>/dev/null || echo "$line"
    fi
done

echo ""
echo "========================================="
echo "Checking Results"
echo "========================================="
echo ""

# Check latest prompt file
echo "1. Latest prompt file:"
LATEST_PROMPT=$(ls -t /Volumes/DATA/QWEN/gateway/logs/requests/prompts/*.txt 2>/dev/null | head -1)
if [ -f "$LATEST_PROMPT" ]; then
    echo "   File: $(basename $LATEST_PROMPT)"
    if grep -q "SESSION ID IS ALWAYS AVAILABLE" "$LATEST_PROMPT"; then
        echo "   ✅ Contains mandatory session path instructions"
    else
        echo "   ❌ Missing mandatory session path instructions"
    fi
else
    echo "   ⚠️  No prompt file found"
fi
echo ""

# Check latest response file
echo "2. Latest response file:"
LATEST_RESPONSE=$(ls -t /Volumes/DATA/QWEN/gateway/logs/requests/responses/*.txt 2>/dev/null | head -1)
if [ -f "$LATEST_RESPONSE" ]; then
    echo "   File: $(basename $LATEST_RESPONSE)"

    # Check if response contains session-specific path
    if grep -q "${SESSION_KEY}" "$LATEST_RESPONSE"; then
        echo "   ✅ Response contains session key in path"
    else
        echo "   ❌ Response does NOT contain session key in path"
    fi

    # Check if response contains session-specific download URL
    if grep -q "api/files/generated/${SESSION_KEY}" "$LATEST_RESPONSE"; then
        echo "   ✅ Response contains session-specific download URL"
    else
        echo "   ❌ Response does NOT contain session-specific download URL"
    fi

    # Show the file path and download URL from response
    echo ""
    echo "   Response content:"
    grep -E "(File location:|Download URL:|api/files|generated_files)" "$LATEST_RESPONSE" | head -10
else
    echo "   ⚠️  No response file found"
fi
echo ""

# Check if file was actually created in session folder
echo "3. File system check:"
if [ -d "/Volumes/DATA/QWEN/zima-file-service/generated_files/${SESSION_KEY}" ]; then
    echo "   ✅ Session folder exists"
    FILES=$(ls /Volumes/DATA/QWEN/zima-file-service/generated_files/${SESSION_KEY}/*.xlsx 2>/dev/null)
    if [ -n "$FILES" ]; then
        echo "   ✅ Excel file(s) created in session folder:"
        for f in $FILES; do
            echo "      - $(basename $f) ($(ls -lh "$f" | awk '{print $5}'))"
        done
    else
        echo "   ❌ No Excel files found in session folder"
    fi
else
    echo "   ❌ Session folder does NOT exist"
    echo "      (Expected: /Volumes/DATA/QWEN/zima-file-service/generated_files/${SESSION_KEY})"
fi
echo ""

echo "========================================="
echo "Test Complete"
echo "========================================="
