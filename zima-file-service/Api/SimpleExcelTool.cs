using System.Text.Json;
using ClosedXML.Excel;

namespace ZimaFileService.Api;

/// <summary>
/// Simple Excel creation tool for basic spreadsheet generation
/// </summary>
public class SimpleExcelTool
{
    private readonly string _generatedPath;

    public SimpleExcelTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
    }

    /// <summary>
    /// Create Excel spreadsheet with headers and rows
    /// </summary>
    public async Task<string> CreateExcelAsync(Dictionary<string, object> args)
    {
        var filePath = GetString(args, "file_path");
        var sheetName = GetString(args, "sheet_name", "Sheet1");
        var headers = GetStringArray(args, "headers");
        var rows = GetRowsData(args, "rows");
        var autoFitColumns = GetBool(args, "auto_fit_columns", true);

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("file_path is required");

        // Resolve output path
        var outputPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_generatedPath, filePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet(sheetName);

        int currentRow = 1;

        // Add headers if provided
        if (headers.Length > 0)
        {
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(currentRow, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            currentRow++;
        }

        // Add data rows
        foreach (var row in rows)
        {
            for (int col = 0; col < row.Length; col++)
            {
                worksheet.Cell(currentRow, col + 1).Value = row[col]?.ToString() ?? "";
            }
            currentRow++;
        }

        // Auto-fit columns if requested
        if (autoFitColumns)
        {
            worksheet.Columns().AdjustToContents();
        }

        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Excel file created successfully",
            output_file = outputPath,
            rows_created = rows.Length,
            headers_count = headers.Length
        });
    }

    // Helper methods
    private string GetString(Dictionary<string, object> args, string key, string defaultValue = "")
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetString() ?? defaultValue;
        return value?.ToString() ?? defaultValue;
    }

    private bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetBoolean();
        return Convert.ToBoolean(value);
    }

    private string[] GetStringArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        return Array.Empty<string>();
    }

    private object[][] GetRowsData(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<object[]>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var rows = new List<object[]>();
            foreach (var rowElement in je.EnumerateArray())
            {
                if (rowElement.ValueKind == JsonValueKind.Array)
                {
                    var row = rowElement.EnumerateArray()
                        .Select(cellElement => cellElement.ValueKind switch
                        {
                            JsonValueKind.String => (object)(cellElement.GetString() ?? ""),
                            JsonValueKind.Number => cellElement.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => cellElement.ToString() ?? ""
                        }).ToArray();
                    rows.Add(row);
                }
            }
            return rows.ToArray();
        }
        return Array.Empty<object[]>();
    }
}