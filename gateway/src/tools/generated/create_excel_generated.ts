import * as ExcelJS from 'exceljs';
import * as fs from 'fs';
import * as path from 'path';

export async function create_excel_generated(input: any): Promise<any> {
  const { filename, data, sessionKey } = input;

  // Determine output path
  const outputDir = process.env.STORAGE_ROOT || './generated_files';
  const sessionDir = sessionKey ? path.join(outputDir, sessionKey) : outputDir;

  if (!fs.existsSync(sessionDir)) {
    fs.mkdirSync(sessionDir, { recursive: true });
  }

  const filePath = path.join(sessionDir, filename);

  // Create workbook
  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet('Sheet1');

  // Add data
  if (Array.isArray(data)) {
    data.forEach(row => {
      if (Array.isArray(row)) {
        worksheet.addRow(row);
      } else if (typeof row === 'object') {
        worksheet.addRow(Object.values(row));
      }
    });
  }

  // Auto-fit columns
  worksheet.columns.forEach(column => {
    if (column) {
      let maxLength = 0;
      column.eachCell?.({ includeEmpty: true }, cell => {
        const cellValue = cell.value ? cell.value.toString() : '';
        maxLength = Math.max(maxLength, cellValue.length);
      });
      column.width = Math.min(maxLength + 2, 50);
    }
  });

  // Save file
  await workbook.xlsx.writeFile(filePath);

  return {
    success: true,
    file_path: filePath,
    file_name: filename,
    message: `Excel file created successfully using exceljs: ${filename}`
  };
}
