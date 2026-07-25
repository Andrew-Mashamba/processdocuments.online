using ZimaFileService.Tools;
using System.Collections.Generic;

var excelTool = new ExcelTool();

var productData = new Dictionary<string, object>
{
    ["file_path"] = "/Volumes/DATA/QWEN/zima-file-service/generated_files/random_products.xlsx",
    ["headers"] = new string[] { "Product Name", "Category", "Price", "SKU", "Stock", "Supplier", "Description" },
    ["rows"] = new object[][]
    {
        new object[] { "Gaming Laptop", "Electronics", "$1,299.99", "GLT-001", "45", "TechCorp", "High-performance laptop for gaming" },
        new object[] { "Wireless Headphones", "Electronics", "$199.99", "WH-002", "120", "AudioMax", "Noise-cancelling Bluetooth headphones" },
        new object[] { "Office Chair", "Furniture", "$349.99", "OC-003", "23", "FurniPlus", "Ergonomic office chair with lumbar support" },
        new object[] { "Coffee Maker", "Appliances", "$89.99", "CM-004", "67", "KitchenPro", "Programmable drip coffee maker" },
        new object[] { "Running Shoes", "Footwear", "$129.99", "RS-005", "89", "SportFit", "Lightweight running shoes with cushioning" },
        new object[] { "Smartphone", "Electronics", "$799.99", "SP-006", "156", "MobileTech", "Latest smartphone with 5G connectivity" },
        new object[] { "Desk Lamp", "Furniture", "$59.99", "DL-007", "34", "LightCorp", "LED desk lamp with adjustable brightness" },
        new object[] { "Water Bottle", "Sports", "$24.99", "WB-008", "200", "HydroGear", "Insulated stainless steel water bottle" },
        new object[] { "Backpack", "Accessories", "$79.99", "BP-009", "78", "TravelMax", "Durable laptop backpack with multiple compartments" },
        new object[] { "Bluetooth Speaker", "Electronics", "$149.99", "BS-010", "92", "SoundWave", "Portable wireless speaker with bass boost" },
        new object[] { "Yoga Mat", "Sports", "$39.99", "YM-011", "145", "FitGear", "Non-slip exercise yoga mat" },
        new object[] { "Kitchen Knife Set", "Kitchenware", "$159.99", "KN-012", "56", "ChefPro", "Professional 8-piece kitchen knife set" },
        new object[] { "Monitor", "Electronics", "$299.99", "MON-013", "38", "DisplayTech", "24-inch 4K computer monitor" },
        new object[] { "Sunglasses", "Accessories", "$89.99", "SG-014", "112", "EyeWear", "UV protection polarized sunglasses" },
        new object[] { "Tablet", "Electronics", "$449.99", "TAB-015", "67", "TabletCorp", "10-inch tablet with stylus support" },
        new object[] { "Cookware Set", "Kitchenware", "$199.99", "CW-016", "29", "CookMaster", "Non-stick 12-piece cookware set" },
        new object[] { "Fitness Tracker", "Electronics", "$179.99", "FT-017", "93", "FitTech", "Waterproof fitness tracker with heart monitor" },
        new object[] { "Book Shelf", "Furniture", "$129.99", "BS-018", "18", "WoodCraft", "5-tier wooden bookshelf" },
        new object[] { "Electric Toothbrush", "Personal Care", "$79.99", "ET-019", "85", "DentalCare", "Rechargeable sonic toothbrush" },
        new object[] { "Camping Tent", "Outdoor", "$189.99", "CT-020", "24", "OutdoorGear", "4-person waterproof camping tent" }
    },
    ["sheet_name"] = "Products",
    ["auto_fit_columns"] = true
};

try
{
    var result = await excelTool.CreateExcelAsync(productData);
    Console.WriteLine("Success: Excel file created!");
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}