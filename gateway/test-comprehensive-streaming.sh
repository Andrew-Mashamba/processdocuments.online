#!/bin/bash

# Comprehensive Streaming Endpoint Test
# Tests various system functionalities via the /stream endpoint

GATEWAY_PORT=${PORT:-5555}
GATEWAY_URL="http://localhost:${GATEWAY_PORT}"
SESSION_KEY="test-comprehensive-$(date +%s)"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║   ZIMA Gateway - Comprehensive Streaming Test         ║"
echo "║   Testing Multiple System Functionalities             ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "Gateway URL: ${GATEWAY_URL}"
echo "Session Key: ${SESSION_KEY}"
echo ""

# Function to test endpoint
test_endpoint() {
    local test_name="$1"
    local prompt="$2"
    local session="${3:-$SESSION_KEY}"

    echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}Test: ${test_name}${NC}"
    echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
    echo ""
    echo "Prompt: ${prompt}"
    echo ""
    echo "Response:"
    echo "---"

    timeout 60 curl -X POST "${GATEWAY_URL}/api/chat/stream" \
        -H "Content-Type: application/json" \
        -d "{
            \"message\": \"${prompt}\",
            \"channel\": \"webchat\",
            \"senderId\": \"test-user-comprehensive\",
            \"sessionKey\": \"${session}\"
        }" \
        -N 2>&1

    echo ""
    echo "---"
    echo ""
    sleep 2
}

# Check if gateway is running
echo -e "${YELLOW}Checking gateway health...${NC}"
if curl -s "${GATEWAY_URL}/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Gateway is running${NC}"
else
    echo -e "${RED}✗ Gateway is not running on port ${GATEWAY_PORT}${NC}"
    echo "Please start the gateway first: npm start"
    exit 1
fi
echo ""

# ============================================================================
# TEST 1: Simple Conversation (Haiku Model)
# ============================================================================
test_endpoint \
    "Simple Greeting (Task Classification: Simple)" \
    "Hello! How are you today?"

# ============================================================================
# TEST 2: Task Classification - Standard Complexity
# ============================================================================
test_endpoint \
    "Standard Task (Sonnet Model)" \
    "Explain the concept of vector embeddings in machine learning"

# ============================================================================
# TEST 3: Memory Search (OpenClaw Memory System)
# ============================================================================
test_endpoint \
    "Memory Search" \
    "Search the workspace memory for information about 'configuration' or 'setup'"

# ============================================================================
# TEST 4: File Read Operation
# ============================================================================
test_endpoint \
    "File Read Operation" \
    "Read the README.md file from the workspace and summarize its contents"

# ============================================================================
# TEST 5: Web Search (Brave API)
# ============================================================================
test_endpoint \
    "Web Search Tool" \
    "Search the web for 'Claude AI assistant capabilities' and give me the top 3 results"

# ============================================================================
# TEST 6: Web Fetch (Content Extraction)
# ============================================================================
test_endpoint \
    "Web Fetch Tool" \
    "Fetch the content from https://example.com and tell me what it says"

# ============================================================================
# TEST 7: Document Generation (ZIMA Tools)
# ============================================================================
test_endpoint \
    "Excel Generation (ZIMA Tool)" \
    "Create an Excel file called 'test-products.xlsx' with 5 sample products including name, price, and quantity columns"

# ============================================================================
# TEST 8: Complex Multi-Step Task
# ============================================================================
test_endpoint \
    "Complex Multi-Step Task (Opus Model)" \
    "Create a comprehensive business plan outline with executive summary, market analysis, financial projections, and implementation timeline. Make it detailed with at least 10 sections."

# ============================================================================
# TEST 9: Code Execution (Exec Tool)
# ============================================================================
test_endpoint \
    "Command Execution" \
    "Execute the command 'echo Hello from ZIMA Gateway' and show me the output"

# ============================================================================
# TEST 10: File Write Operation
# ============================================================================
test_endpoint \
    "File Write Operation" \
    "Create a text file called 'test-output.txt' with the content 'This is a test from the streaming endpoint'"

# ============================================================================
# TEST 11: Multi-Turn Conversation (Same Session)
# ============================================================================
MULTI_SESSION="test-multi-turn-$(date +%s)"

test_endpoint \
    "Multi-Turn: First Message" \
    "My name is Alice and I work at TechCorp. Remember this." \
    "${MULTI_SESSION}"

test_endpoint \
    "Multi-Turn: Follow-up (Context Test)" \
    "What is my name and where do I work?" \
    "${MULTI_SESSION}"

# ============================================================================
# TEST 12: Tool Chaining
# ============================================================================
test_endpoint \
    "Tool Chaining (Search + Fetch)" \
    "Search for 'Anthropic Claude' on the web, then fetch the content of the first result and summarize it"

# ============================================================================
# TEST 13: Response Caching Test
# ============================================================================
CACHE_SESSION="test-cache-$(date +%s)"

echo -e "${YELLOW}Testing response caching (same prompt twice)...${NC}"
echo ""

test_endpoint \
    "Cache Test: First Request" \
    "What is 2 + 2?" \
    "${CACHE_SESSION}"

echo -e "${YELLOW}Making identical request (should be cached)...${NC}"
echo ""

test_endpoint \
    "Cache Test: Second Request (Should Use Cache)" \
    "What is 2 + 2?" \
    "${CACHE_SESSION}"

# ============================================================================
# TEST 14: Image Processing (If Available)
# ============================================================================
test_endpoint \
    "Image Processing Tool" \
    "List the available image processing capabilities"

# ============================================================================
# TEST 15: Background Process (Process Manager)
# ============================================================================
test_endpoint \
    "Background Process Spawning" \
    "Spawn a background process that sleeps for 5 seconds, then list all running processes"

# ============================================================================
# TEST 16: Session Management
# ============================================================================
test_endpoint \
    "Session List" \
    "List all active sessions"

# ============================================================================
# TEST 17: PDF Generation (ZIMA Tool)
# ============================================================================
test_endpoint \
    "PDF Generation (ZIMA Tool)" \
    "Create a PDF document called 'test-report.pdf' with a title 'Test Report' and some sample content about AI technology"

# ============================================================================
# TEST 18: Browser Automation
# ============================================================================
test_endpoint \
    "Browser Automation" \
    "Open a browser, navigate to example.com, and tell me the page title"

# ============================================================================
# TEST 19: Error Handling (Dangerous Command)
# ============================================================================
test_endpoint \
    "Security: Dangerous Command Block" \
    "Execute the command 'rm -rf /' and see what happens"

# ============================================================================
# TEST 20: Context Tier Optimization
# ============================================================================
LONG_SESSION="test-long-session-$(date +%s)"

echo -e "${YELLOW}Testing context tier optimization (multiple messages)...${NC}"
echo ""

for i in {1..5}; do
    test_endpoint \
        "Long Session: Message $i" \
        "This is message number $i. Tell me what number this is." \
        "${LONG_SESSION}"
done

# ============================================================================
# Summary
# ============================================================================
echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║                  TEST SUMMARY                          ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "Completed 20 comprehensive tests covering:"
echo ""
echo "✓ Task Classification (Simple/Standard/Complex)"
echo "✓ Model Selection (Haiku/Sonnet/Opus)"
echo "✓ Memory System (Vector Search)"
echo "✓ Web Tools (Search, Fetch, Browser)"
echo "✓ File Operations (Read, Write)"
echo "✓ Document Generation (Excel, PDF)"
echo "✓ Command Execution (Secure)"
echo "✓ Process Management (Background Jobs)"
echo "✓ Multi-Turn Conversations"
echo "✓ Response Caching"
echo "✓ Context Tier Optimization"
echo "✓ Security (Dangerous Command Blocking)"
echo "✓ Tool Chaining"
echo "✓ Session Management"
echo ""
echo "Session Keys Used:"
echo "  - Main: ${SESSION_KEY}"
echo "  - Multi-turn: ${MULTI_SESSION}"
echo "  - Cache test: ${CACHE_SESSION}"
echo "  - Long session: ${LONG_SESSION}"
echo ""
echo "Check the outputs above for detailed results!"
echo ""
