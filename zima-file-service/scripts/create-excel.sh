#!/bin/bash

cd "$(dirname "$0")/.."

# Create a temporary C# script to create the Excel file
cat > temp_excel_creator.csx << 'EOF'
#r "nuget:ClosedXML,0.102.1"

using ClosedXML.Excel;
using System;
using System.IO;

Console.WriteLine("Creating Excel file with employee data...");

try
{
    // Create generated_files directory if it doesn't exist
    var generatedDir = "generated_files";
    Directory.CreateDirectory(generatedDir);

    // Create a new workbook and worksheet
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Employee Data");

    // Add headers
    worksheet.Cell(1, 1).Value = "ID";
    worksheet.Cell(1, 2).Value = "Name";
    worksheet.Cell(1, 3).Value = "Email";
    worksheet.Cell(1, 4).Value = "Department";
    worksheet.Cell(1, 5).Value = "Salary";

    // Format headers
    var headerRange = worksheet.Range(1, 1, 1, 5);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
    headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

    // Add employee data from the JSON
    var employees = new[] {
        new { Id = 1, Name = "John Smith", Email = "john.smith@acme.com", Department = "Engineering", Salary = 85000 },
        new { Id = 2, Name = "Jane Doe", Email = "jane.doe@acme.com", Department = "Marketing", Salary = 72000 },
        new { Id = 3, Name = "Bob Wilson", Email = "bob.wilson@acme.com", Department = "Engineering", Salary = 95000 },
        new { Id = 4, Name = "Alice Brown", Email = "alice.brown@acme.com", Department = "Sales", Salary = 68000 },
        new { Id = 5, Name = "Charlie Davis", Email = "charlie.davis@acme.com", Department = "Engineering", Salary = 110000 }
    };

    // Add data rows
    int row = 2;
    foreach (var employee in employees)
    {
        worksheet.Cell(row, 1).Value = employee.Id;
        worksheet.Cell(row, 2).Value = employee.Name;
        worksheet.Cell(row, 3).Value = employee.Email;
        worksheet.Cell(row, 4).Value = employee.Department;
        worksheet.Cell(row, 5).Value = employee.Salary;
        row++;
    }

    // Auto-fit columns
    worksheet.Columns().AdjustToContents();

    // Save the file
    var outputPath = Path.Combine(generatedDir, "employee_data.xlsx");
    workbook.SaveAs(outputPath);

    Console.WriteLine($"✅ Excel file created successfully: {outputPath}");
    Console.WriteLine($"📊 Data: {employees.Length} employees with ID, Name, Email, Department, and Salary columns");

    // Show file info
    var fileInfo = new FileInfo(outputPath);
    Console.WriteLine($"📁 File size: {fileInfo.Length} bytes");
    Console.WriteLine($"📅 Created: {fileInfo.CreationTime}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
EOF

echo "Running C# script to create Excel file..."

# Run the C# script using dotnet script
dotnet script temp_excel_creator.csx

# Clean up
rm temp_excel_creator.csx

echo "Done!"