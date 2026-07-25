---
name: generate-word
description: Generate Word documents with formatted text, headings, and professional layouts
allowed-tools: create_word, list_files
argument-hint: "[description of document content]"
---

# Word Document Generation

Generate a Word document based on user request: $ARGUMENTS

## Guidelines

When creating Word documents:

1. **Structure with proper headings**
   - Use Heading 1 for main title
   - Use Heading 2 for major sections
   - Use Heading 3 for subsections
   - Include a clear hierarchy

2. **Professional formatting**
   - Consistent fonts (Calibri or Times New Roman)
   - Appropriate line spacing
   - Proper paragraph breaks
   - Bullet points for lists

3. **Common document types**
   - Letters (cover letters, formal correspondence)
   - Reports (business, research, progress)
   - Proposals (project, business)
   - Templates (invoices, contracts, memos)
   - Resumes and CVs

4. **Include appropriate sections**
   - Title/Header
   - Date and recipient (for letters)
   - Introduction/Summary
   - Main content
   - Conclusion
   - Signature/Footer

5. **Use the create_word MCP tool**
   - Provide document title
   - Structure content with headings array
   - Include paragraphs under each heading

## Example Structure

For a cover letter:
```
[Heading 1] Application for [Position]
[Date]
[Recipient Details]

[Heading 2] Introduction
[Paragraph about interest in the role]

[Heading 2] Qualifications
[Paragraph about relevant experience]

[Heading 2] Closing
[Paragraph with call to action]

[Signature]
```

Always confirm the file was created successfully and provide the filename.
