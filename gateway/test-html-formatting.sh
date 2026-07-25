#!/bin/bash

# Test script to verify monochrome HTML formatting is working
# After adding format-html skill to Gateway (2026-01-31)

SESSION_ID="test-html-format-$(date +%s)"
SESSION_KEY="agent:main:webchat:direct:${SESSION_ID}"

echo "========================================="
echo "Testing Monochrome HTML Formatting"
echo "========================================="
echo "Session Key: $SESSION_KEY"
echo ""
echo "Sending request for formatted response..."
echo ""

# Send request that should trigger HTML formatting
timeout 45 curl -s -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d "{
    \"message\": \"Create a budget summary table showing: Housing (\$1200, 40%), Food (\$900, 30%), Transport (\$600, 20%), Other (\$300, 10%). Format it nicely with a title and description.\",
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
echo "Checking Response Format"
echo "========================================="
echo ""

# Check latest response file
LATEST_RESPONSE=$(ls -t /Volumes/DATA/QWEN/gateway/logs/requests/responses/*.txt 2>/dev/null | head -1)

if [ -f "$LATEST_RESPONSE" ]; then
    echo "Latest response file: $(basename $LATEST_RESPONSE)"
    echo ""

    # Check if response contains HTML
    if grep -q "<div style=" "$LATEST_RESPONSE"; then
        echo "✅ Response contains HTML with inline styles"
    else
        echo "❌ Response does NOT contain HTML with inline styles"
    fi

    # Check for monochrome colors
    if grep -q "#1A1A1A\|#525252\|#FAFAFA\|#E5E5E5" "$LATEST_RESPONSE"; then
        echo "✅ Response uses monochrome color palette"
    else
        echo "❌ Response does NOT use monochrome colors"
    fi

    # Check for forbidden colorful elements
    if grep -q "#[0-9A-Fa-f]*[3-9a-fA-F][0-9A-Fa-f]*" "$LATEST_RESPONSE" | grep -v "#1A1A1A\|#525252\|#737373\|#A3A3A3\|#D4D4D4\|#E5E5E5\|#F5F5F5\|#FAFAFA\|#FFFFFF"; then
        echo "⚠️  Warning: Response may contain non-monochrome colors"
    else
        echo "✅ No colorful elements detected"
    fi

    # Check for table formatting
    if grep -q "<table" "$LATEST_RESPONSE"; then
        echo "✅ Response contains formatted table"
    else
        echo "ℹ️  No table found (may be using other formatting)"
    fi

    # Check for base container
    if grep -q "font-family: -apple-system" "$LATEST_RESPONSE"; then
        echo "✅ Response wrapped in base container"
    else
        echo "❌ Response missing base container"
    fi

    echo ""
    echo "First 500 characters of response:"
    echo "-----------------------------------"
    cat "$LATEST_RESPONSE" | head -c 500
    echo ""
    echo "-----------------------------------"
else
    echo "⚠️  No response file found"
fi

echo ""
echo "========================================="
echo "Checking System Prompt"
echo "========================================="
echo ""

# Check latest prompt file
LATEST_PROMPT=$(ls -t /Volumes/DATA/QWEN/gateway/logs/requests/prompts/*.txt 2>/dev/null | head -1)

if [ -f "$LATEST_PROMPT" ]; then
    echo "Latest prompt file: $(basename $LATEST_PROMPT)"

    # Check if prompt contains formatting instructions
    if grep -q "<response_formatting>" "$LATEST_PROMPT"; then
        echo "✅ System prompt contains response formatting section"
    else
        echo "❌ System prompt missing response formatting section"
    fi

    # Check for monochrome design mention
    if grep -q "Monochrome Design" "$LATEST_PROMPT"; then
        echo "✅ Monochrome Design instructions included"
    else
        echo "❌ Monochrome Design instructions missing"
    fi

    # Check for HTML formatting requirement
    if grep -q "ALWAYS return HTML with inline CSS" "$LATEST_PROMPT"; then
        echo "✅ HTML formatting requirement included"
    else
        echo "❌ HTML formatting requirement missing"
    fi
else
    echo "⚠️  No prompt file found"
fi

echo ""
echo "========================================="
echo "Test Complete"
echo "========================================="
