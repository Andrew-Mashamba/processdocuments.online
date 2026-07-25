# ZIMA Document Processing Test Suite

This directory contains test prompts and input files to validate the 237+ document processing tools.

## Directory Structure

```
tests/
├── README.md                 # This file
├── run_tests.sh              # Automated test runner script
├── input/                    # Sample input files
│   ├── sample_data.json      # Employee/company JSON data
│   ├── sales_data.csv        # Sales transaction CSV data
│   ├── report_content.txt    # Quarterly report text
│   └── invalid_json.json     # Intentionally broken JSON for repair tests
├── prompts/                  # Test prompts by category
│   ├── test_prompts.json     # Structured test definitions
│   ├── excel_prompts.txt     # Excel tool tests
│   ├── pdf_prompts.txt       # PDF tool tests
│   ├── word_prompts.txt      # Word tool tests
│   ├── powerpoint_prompts.txt# PowerPoint tool tests
│   ├── json_prompts.txt      # JSON tool tests
│   ├── conversion_prompts.txt# Format conversion tests
│   ├── image_text_prompts.txt# Image and text processing tests
│   └── workflow_prompts.txt  # Multi-tool workflow tests
└── expected/                 # Expected output references (optional)
```

## Quick Start

### 1. Start the ZIMA Server

```bash
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

### 2. Run Automated Tests

```bash
# Run all tests
./tests/run_tests.sh all

# Run specific category
./tests/run_tests.sh excel
./tests/run_tests.sh pdf
./tests/run_tests.sh word
./tests/run_tests.sh json
./tests/run_tests.sh conversion
./tests/run_tests.sh workflow
```

### 3. Manual Testing via API

```bash
# Send a prompt to the API
curl -X POST http://localhost:5000/api/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Create an Excel file from tests/input/sample_data.json"}'
```

## Test Categories

### Excel Processing (10 tests)
Tests for creating, formatting, charting, and protecting Excel workbooks.
- Create from JSON/CSV
- Add charts and pivot tables
- Conditional formatting
- Formulas and calculations
- Protection and compression

### PDF Processing (12 tests)
Tests for creating, modifying, securing, and converting PDFs.
- Create from text/JSON
- Add watermarks and annotations
- Password protection
- Digital signatures
- Merge/split operations

### Word Processing (11 tests)
Tests for creating and manipulating Word documents.
- Create with formatting
- Tables and headers/footers
- Mail merge
- Table of contents
- Protection

### PowerPoint Processing (10 tests)
Tests for creating presentations with slides, charts, and animations.
- Create multi-slide presentations
- Add charts from data
- Animations and transitions
- Export to images
- Protection

### JSON Processing (12 tests)
Tests for validating, transforming, and converting JSON data.
- Validation and repair
- Transformation and filtering
- Format conversions
- Digital signatures

### Conversion (12 tests)
Tests for converting between document formats.
- CSV ↔ JSON
- JSON ↔ YAML
- Excel ↔ PDF
- Word ↔ PDF
- HTML ↔ Word

### Image & Text (10 tests)
Tests for image manipulation and text processing.
- Watermarks
- Redaction
- OCR
- Text statistics

### Complex Workflows (10 tests)
Tests that combine multiple tools for real-world scenarios.
- Complete report packages
- Data processing pipelines
- Multi-format exports
- Secure document distribution

## Input Files

### sample_data.json
```json
{
  "company": "Acme Corporation",
  "employees": [
    {"id": 1, "name": "John Smith", "department": "Engineering", "salary": 85000},
    ...
  ],
  "departments": ["Engineering", "Marketing", "Sales", "HR", "Finance"]
}
```

### sales_data.csv
```csv
Date,Product,Region,Quantity,Unit Price,Total
2024-01-05,Widget A,North,150,29.99,4498.50
...
```

### report_content.txt
A sample quarterly business report with sections for executive summary, highlights, challenges, and recommendations.

### invalid_json.json
Intentionally malformed JSON for testing the repair tool.

## Expected Tool Usage

Each prompt is designed to exercise specific tools. The `test_prompts.json` file contains structured test definitions with:

- `id`: Unique test identifier
- `name`: Human-readable test name
- `prompt`: The actual prompt to send
- `expected_tools`: Array of tools that should be invoked
- `input_files`: Required input files
- `expected_output`: Expected output file(s)

## Adding New Tests

1. Add input files to `tests/input/`
2. Add prompts to appropriate category file in `tests/prompts/`
3. Update `test_prompts.json` with structured definition
4. Optionally add expected output to `tests/expected/`

## Troubleshooting

### Server Not Running
```bash
# Start the server
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

### Check Available Tools
```bash
curl http://localhost:5000/api/tools
```

### View Generated Files
```bash
ls -la generated_files/
```

### Clear Generated Files
```bash
rm -rf generated_files/*
```

## Tool Count Verification

The system should report 237+ available tools. Verify with:
```bash
curl http://localhost:5000/api/tools | jq '.tools | length'
```
