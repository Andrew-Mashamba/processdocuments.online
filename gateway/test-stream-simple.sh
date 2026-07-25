#!/bin/bash

curl -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Just say hello","session_key":"test-simple","model":"claude-3-5-haiku-20241022","max_tokens":100}'
