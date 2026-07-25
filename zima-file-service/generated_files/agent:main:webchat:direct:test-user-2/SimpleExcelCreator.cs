using ClosedXML.Excel;
using System;
using System.IO;

namespace ZimaFileService
{
    class SimpleExcelCreator
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Creating Excel file...");

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Products");

                // Add headers
                worksheet.Cell(1, 1).Value = "Product Name";
                worksheet.Cell(1, 2).Value = "Price ($)";

                // Style headers
                var headerRange = worksheet.Range("A1:B1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Add product data
                var products = new (string name, string price)[]
                {
                    ("Wireless Bluetooth Headphones", "89.99"),
                    ("Smart Home Security Camera", "129.95"),
                    ("Organic Green Tea Blend", "24.50"),
                    ("Professional Gaming Mouse", "67.89"),
                    ("Stainless Steel Water Bottle", "35.99"),
                    ("Premium Yoga Mat", "45.75"),
                    ("LED Desk Lamp with USB Charging", "52.99"),
                    ("Bluetooth Portable Speaker", "78.99"),
                    ("Memory Foam Pillow", "39.99"),
                    ("Wireless Phone Charger", "29.99")
                };

                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cell(row, 1).Value = product.name;
                    worksheet.Cell(row, 2).Value = product.price;
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save file
                var filePath = "generated_files/random_products.xlsx";
                Directory.CreateDirectory("generated_files");
                workbook.SaveAs(filePath);

                Console.WriteLine($"Excel file created successfully: {filePath}");
                Console.WriteLine($"Total products: {products.Length}");
                Console.WriteLine($"Total rows: {products.Length + 1} (including header)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}