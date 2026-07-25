#!/bin/bash

# Comprehensive Stream Test Runner
# Runs both shell and TypeScript versions of the comprehensive tests

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║   ZIMA Gateway - Comprehensive Stream Test Runner     ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# Check if gateway is running
GATEWAY_PORT=${PORT:-5555}
GATEWAY_URL="http://localhost:${GATEWAY_PORT}"

echo "Checking gateway at ${GATEWAY_URL}..."
if curl -s "${GATEWAY_URL}/health" > /dev/null 2>&1; then
    echo "✅ Gateway is running"
else
    echo "❌ Gateway is not running"
    echo ""
    echo "Please start the gateway first:"
    echo "  cd /Volumes/DATA/QWEN/gateway"
    echo "  npm start"
    echo ""
    echo "Or in a separate terminal:"
    echo "  cd /Volumes/DATA/QWEN/gateway"
    echo "  PORT=5555 npm start"
    echo ""
    exit 1
fi

echo ""
echo "Choose test version:"
echo "  1) Shell Script (Quick, Basic)"
echo "  2) TypeScript (Detailed, Structured)"
echo "  3) Both"
echo ""
read -p "Enter choice (1-3): " choice

case $choice in
    1)
        echo ""
        echo "Running Shell Script Tests..."
        echo ""
        ./test-comprehensive-streaming.sh
        ;;
    2)
        echo ""
        echo "Running TypeScript Tests..."
        echo ""
        ts-node tests/integration/comprehensive-stream.test.ts
        ;;
    3)
        echo ""
        echo "Running Shell Script Tests First..."
        echo ""
        ./test-comprehensive-streaming.sh

        echo ""
        echo "Press Enter to continue with TypeScript tests..."
        read

        echo ""
        echo "Running TypeScript Tests..."
        echo ""
        ts-node tests/integration/comprehensive-stream.test.ts
        ;;
    *)
        echo "Invalid choice. Exiting."
        exit 1
        ;;
esac

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║              COMPREHENSIVE TESTS COMPLETE              ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "Reports generated:"
echo "  - COMPREHENSIVE_STREAM_TEST_REPORT.md (if TypeScript was run)"
echo ""
