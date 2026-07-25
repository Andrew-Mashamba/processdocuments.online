#!/bin/bash

echo "Testing ZIMA Core direct connection..."

curl -s -X POST "http://localhost:5000/api/generate" \
  -H "Content-Type: application/json" \
  -d "{\"prompt\": \"test connection\", \"messages\": [], \"sessionId\": \"test\"}" \
  --max-time 5
