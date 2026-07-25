---
name: analyze-file
description: Analyze uploaded files and extract insights or transform data
allowed-tools: read_excel, read_file_content, list_files, create_excel, create_word, create_pdf
argument-hint: "[what to analyze or extract from the file]"
---

# File Analysis and Transformation

Analyze or transform files based on user request: $ARGUMENTS

## Guidelines

When analyzing files:

1. **First, understand the file**
   - Use read_excel for spreadsheets
   - Use read_file_content for text files
   - Identify the data structure and content

2. **Common analysis tasks**
   - Summarize data (counts, averages, totals)
   - Find patterns or anomalies
   - Extract specific information
   - Compare datasets
   - Generate reports from data

3. **Data transformation tasks**
   - Convert between formats (Excel to PDF, CSV to Excel)
   - Restructure data (pivot, transpose)
   - Filter and sort data
   - Combine multiple sources
   - Add calculations or formulas

4. **Best practices**
   - Describe findings clearly
   - Highlight key insights
   - Suggest improvements if applicable
   - Offer to generate new files with results

5. **When creating output**
   - Match output format to use case
   - Excel for data that needs further analysis
   - PDF for final reports or summaries
   - Word for narrative reports

## Analysis Workflow

1. List available files with list_files
2. Read the target file(s)
3. Analyze content based on request
4. Present findings to user
5. Offer to generate output file if helpful

## Example Responses

For data analysis:
"I analyzed the sales data and found:
- Total revenue: $125,000
- Best month: March ($18,500)
- Top product: Widget A (45% of sales)
Would you like me to create a summary report?"

For transformation:
"I've converted your Excel data to a formatted PDF report with charts and summaries."

Always explain what you found and ask if additional analysis is needed.
