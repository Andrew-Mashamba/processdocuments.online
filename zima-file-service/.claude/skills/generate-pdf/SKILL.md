---
name: generate-pdf
description: Generate PDF documents for invoices, reports, and formal documents
allowed-tools: create_pdf, list_files
argument-hint: "[description of PDF content]"
---

# PDF Document Generation

Generate a PDF document based on user request: $ARGUMENTS

## Guidelines

When creating PDF documents:

1. **Use for formal/final documents**
   - Invoices and receipts
   - Certificates
   - Official reports
   - Contracts and agreements
   - Presentations converted to PDF

2. **Maintain professional layout**
   - Clear headers and footers
   - Consistent margins
   - Proper page breaks
   - Company/personal branding

3. **Common PDF types**
   - Invoices (with line items, totals, payment terms)
   - Reports (formatted for printing)
   - Certificates (with borders and signatures)
   - Flyers and brochures
   - Data summaries

4. **Invoice structure example**
   - Company header with logo placeholder
   - Invoice number and date
   - Bill to / Ship to addresses
   - Line items with quantities and prices
   - Subtotal, tax, total
   - Payment terms and notes

5. **Use the create_pdf MCP tool**
   - Provide title
   - Include structured sections
   - Format tables properly

## Example Invoice Structure

```
[Company Name]
[Address Line 1]
[Address Line 2]

INVOICE #INV-001
Date: [Date]

Bill To:
[Customer Name]
[Customer Address]

| Description | Quantity | Unit Price | Amount |
|-------------|----------|------------|--------|
| Item 1      | 2        | $50.00     | $100.00|
| Item 2      | 1        | $75.00     | $75.00 |

Subtotal: $175.00
Tax (10%): $17.50
Total: $192.50

Payment Terms: Net 30
```

Always confirm the file was created successfully and provide the filename.
