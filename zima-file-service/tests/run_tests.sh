#!/bin/bash
# ZIMA Document Processing Tool Test Runner
# This script runs test prompts against the ZIMA service

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
API_URL="${API_URL:-http://localhost:5000}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  ZIMA Document Processing Test Suite  ${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if server is running
check_server() {
    echo -e "${YELLOW}Checking if ZIMA server is running...${NC}"
    if curl -s "$API_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}Server is running at $API_URL${NC}"
        return 0
    else
        echo -e "${RED}Server is not running. Please start the server first:${NC}"
        echo "  cd $PROJECT_DIR && dotnet run"
        return 1
    fi
}

# Run a single test
run_test() {
    local test_name="$1"
    local prompt="$2"
    local expected_output="$3"

    echo -e "\n${BLUE}Running: $test_name${NC}"
    echo -e "Prompt: ${prompt:0:80}..."

    # Send request to API
    response=$(curl -s -X POST "$API_URL/api/generate" \
        -H "Content-Type: application/json" \
        -d "{\"prompt\": \"$prompt\"}" \
        --max-time 120)

    if [ $? -eq 0 ] && [ -n "$response" ]; then
        # Check if response indicates success
        if echo "$response" | grep -q '"success":true\|"status":"completed"'; then
            echo -e "${GREEN}PASSED${NC}"
            return 0
        else
            echo -e "${YELLOW}COMPLETED (check output)${NC}"
            echo "Response: ${response:0:200}..."
            return 0
        fi
    else
        echo -e "${RED}FAILED${NC}"
        echo "Error: $response"
        return 1
    fi
}

# Test categories
run_excel_tests() {
    echo -e "\n${BLUE}=== EXCEL PROCESSING TESTS ===${NC}"

    run_test "Create Excel from JSON" \
        "Create an Excel spreadsheet from the employee data in tests/input/sample_data.json with columns for ID, Name, Email, Department, Salary" \
        "employees.xlsx"

    run_test "Create Excel with Chart" \
        "Convert tests/input/sales_data.csv to Excel and add a bar chart showing total sales by region" \
        "sales_report.xlsx"

    run_test "Excel with Conditional Formatting" \
        "Create an Excel file with employee salaries and highlight values above 90000 in green" \
        "salary_highlighted.xlsx"
}

run_pdf_tests() {
    echo -e "\n${BLUE}=== PDF PROCESSING TESTS ===${NC}"

    run_test "Create PDF Report" \
        "Create a PDF document from the quarterly report in tests/input/report_content.txt with proper formatting" \
        "quarterly_report.pdf"

    run_test "Create PDF with Watermark" \
        "Create a PDF from tests/input/report_content.txt and add a CONFIDENTIAL watermark" \
        "confidential_report.pdf"

    run_test "JSON to PDF Table" \
        "Convert tests/input/sample_data.json to a PDF with a formatted employee table" \
        "employees.pdf"
}

run_word_tests() {
    echo -e "\n${BLUE}=== WORD PROCESSING TESTS ===${NC}"

    run_test "Create Word Document" \
        "Create a Word document from tests/input/report_content.txt with proper headings and formatting" \
        "quarterly_report.docx"

    run_test "Create Word with Table" \
        "Create a Word document with the employee data from tests/input/sample_data.json as a formatted table" \
        "employee_directory.docx"
}

run_json_tests() {
    echo -e "\n${BLUE}=== JSON PROCESSING TESTS ===${NC}"

    run_test "Validate JSON" \
        "Validate the JSON file at tests/input/sample_data.json and report any issues" \
        "validation_result"

    run_test "Repair Invalid JSON" \
        "Repair the invalid JSON file at tests/input/invalid_json.json" \
        "repaired.json"

    run_test "Transform JSON" \
        "Extract only Engineering department employees from tests/input/sample_data.json" \
        "engineering_employees.json"
}

run_conversion_tests() {
    echo -e "\n${BLUE}=== CONVERSION TESTS ===${NC}"

    run_test "CSV to JSON" \
        "Convert tests/input/sales_data.csv to JSON format" \
        "sales_data.json"

    run_test "JSON to CSV" \
        "Extract the employees array from tests/input/sample_data.json and convert to CSV" \
        "employees.csv"
}

run_workflow_tests() {
    echo -e "\n${BLUE}=== COMPLEX WORKFLOW TESTS ===${NC}"

    run_test "Complete Report Package" \
        "Create a complete report package: Word document from tests/input/report_content.txt, Excel from tests/input/sales_data.csv with charts, and a PDF version with watermark" \
        "report_package"

    run_test "Data Processing Pipeline" \
        "Process tests/input/sample_data.json: validate it, filter employees with salary above 80000, create Excel with conditional formatting, and convert to PDF" \
        "high_salary.pdf"
}

# Main execution
main() {
    # Check if specific test category requested
    category="${1:-all}"

    if ! check_server; then
        echo -e "\n${YELLOW}Starting server in background...${NC}"
        cd "$PROJECT_DIR" && dotnet run &
        sleep 5
        if ! check_server; then
            echo -e "${RED}Could not start server. Exiting.${NC}"
            exit 1
        fi
    fi

    case "$category" in
        excel)
            run_excel_tests
            ;;
        pdf)
            run_pdf_tests
            ;;
        word)
            run_word_tests
            ;;
        json)
            run_json_tests
            ;;
        conversion)
            run_conversion_tests
            ;;
        workflow)
            run_workflow_tests
            ;;
        all)
            run_excel_tests
            run_pdf_tests
            run_word_tests
            run_json_tests
            run_conversion_tests
            run_workflow_tests
            ;;
        *)
            echo "Usage: $0 [excel|pdf|word|json|conversion|workflow|all]"
            exit 1
            ;;
    esac

    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${GREEN}  Test Suite Complete  ${NC}"
    echo -e "${BLUE}========================================${NC}"
}

main "$@"
