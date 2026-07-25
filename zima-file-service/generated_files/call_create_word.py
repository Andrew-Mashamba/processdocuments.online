#!/usr/bin/env python3
import json
import subprocess
import sys

# Call the create_word MCP tool
tool_call = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
        "name": "create_word",
        "arguments": {
            "file_path": "hello_world.docx",
            "content": [
                {
                    "type": "heading1",
                    "text": "Hello World Document"
                },
                {
                    "type": "paragraph",
                    "text": "Hello World"
                }
            ]
        }
    }
}

# Execute the tool call
result = subprocess.run([
    "dotnet", "run", "--project", "/Volumes/DATA/QWEN/zima-file-service",
    "--", "create_word",
    json.dumps(tool_call["params"]["arguments"])
], capture_output=True, text=True, cwd="/Volumes/DATA/QWEN/zima-file-service")

print("STDOUT:", result.stdout)
print("STDERR:", result.stderr)
print("Return code:", result.returncode)