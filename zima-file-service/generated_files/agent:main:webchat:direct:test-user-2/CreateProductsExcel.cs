using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZimaFileService.Tools;

namespace ZimaFileService
{
    class CreateProductsExcel
    {
        static async Task Main(string[] args)
        {
            // Initialize FileManager with current directory
            FileManager.Initialize("/Volumes/DATA/QWEN/zima-file-service");

            var excelTool = new ExcelTool();

            var arguments = new Dictionary<string, object>
            {
                ["file_path"] = Path.Combine(FileManager.Instance.GeneratedFilesPath, "random_products.xlsx"),
                ["sheet_name"] = "Products",
                ["headers"] = new List<object> { "Product Name", "Price ($)" },
                ["rows"] = new List<object>
                {
                    new List<object> { "Wireless Bluetooth Headphones", "89.99" },
                    new List<object> { "Smart Home Security Camera", "129.95" },
                    new List<object> { "Organic Green Tea Blend", "24.50" },
                    new List<object> { "Professional Gaming Mouse", "67.89" },
                    new List<object> { "Stainless Steel Water Bottle", "35.99" },
                    new List<object> { "Premium Yoga Mat", "45.75" },
                    new List<object> { "LED Desk Lamp with USB Charging", "52.99" },
                    new List<object> { "Bluetooth Portable Speaker", "78.99" },
                    new List<object> { "Memory Foam Pillow", "39.99" },
                    new List<object> { "Wireless Phone Charger", "29.99" }
                },
                ["auto_fit_columns"] = true
            };

            try
            {
                string result = await excelTool.CreateExcelAsync(arguments);
                Console.WriteLine("Excel file created successfully:");
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Excel file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}