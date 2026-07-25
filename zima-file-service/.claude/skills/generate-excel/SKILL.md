---
name: generate-excel
description: Generate Excel spreadsheets with data, formulas, and formatting
allowed-tools: create_excel, read_excel, list_files
argument-hint: "[description of spreadsheet content]"
---

# Excel Spreadsheet Generation

Generate an Excel spreadsheet based on user request: $ARGUMENTS

## Guidelines

When creating Excel files:

1. **Structure the data logically**
   - Use clear column headers in the first row
   - Group related data together
   - Use appropriate data types (numbers, dates, text)

2. **Apply formatting**
   - Bold headers
   - Auto-fit column widths
   - Use number formatting for currencies and percentages
   - Apply borders and alternating row colors for readability

3. **Include formulas where appropriate**
   - SUM for totals
   - AVERAGE for means
   - COUNT for record counts
   - Conditional formatting for highlighting

4. **Common spreadsheet types**
   - Data lists (countries, products, contacts)
   - Financial reports (budgets, invoices, expenses)
   - Trackers (project, time, inventory)
   - Schedules (calendars, timelines)

5. **Use the create_excel MCP tool**
   - Provide sheet name
   - Include all rows with headers
   - Specify column widths for readability

## Example Output Structure

For a countries list:
```
| Country | Capital | Population | GDP (USD) | Continent |
|---------|---------|------------|-----------|-----------|
| USA     | Washington D.C. | 331M | 21.4T | North America |
```

Always confirm the file was created successfully and provide the filename.
