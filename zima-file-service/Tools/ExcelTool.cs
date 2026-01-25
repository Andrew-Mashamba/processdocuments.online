using System.Text.Json;
using ClosedXML.Excel;

namespace ZimaFileService.Tools;

public class ExcelTool : IFileTool
{
    public string Name => "excel";
    public string Description => "Create and read Excel spreadsheets";

    public async Task<string> CreateExcelAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        var sheetName = GetString(arguments, "sheet_name") ?? "Sheet1";
        var headers = GetStringArray(arguments, "headers");
        var rows = GetRowsArray(arguments, "rows");
        var autoFitColumns = GetBool(arguments, "auto_fit_columns", true);

        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            int currentRow = 1;

            // Add headers if provided
            if (headers != null && headers.Length > 0)
            {
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cell(currentRow, col + 1);
                    cell.Value = headers[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                currentRow++;
            }

            // Add data rows
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    for (int col = 0; col < row.Length; col++)
                    {
                        var cell = worksheet.Cell(currentRow, col + 1);
                        SetCellValue(cell, row[col]);
                    }
                    currentRow++;
                }
            }

            // Auto-fit columns if requested
            if (autoFitColumns)
            {
                worksheet.Columns().AdjustToContents();
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            workbook.SaveAs(filePath);

            int totalRows = currentRow - 1;
            int totalCols = headers?.Length ?? (rows?.FirstOrDefault()?.Length ?? 0);

            return $"Excel file created successfully at: {filePath}\n" +
                   $"Sheet: {sheetName}\n" +
                   $"Rows: {totalRows} (including {(headers != null ? "1 header row" : "no headers")})\n" +
                   $"Columns: {totalCols}";
        });
    }

    public async Task<string> ReadExcelAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        var sheetName = GetString(arguments, "sheet_name");
        var hasHeaders = GetBool(arguments, "has_headers", true);
        var maxRows = GetInt(arguments, "max_rows", 0); // 0 = unlimited
        var includeStats = GetBool(arguments, "include_stats", false);

        return await Task.Run(() =>
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            }

            using var workbook = new XLWorkbook(filePath);
            var worksheet = string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheets.First()
                : workbook.Worksheet(sheetName);

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
            {
                return JsonSerializer.Serialize(new {
                    file = filePath,
                    sheet = worksheet.Name,
                    message = "Sheet is empty",
                    data = Array.Empty<object>()
                });
            }

            var rows = usedRange.RowsUsed().ToList();
            var result = new List<Dictionary<string, object>>();
            string[]? headers = null;

            int startRow = 0;
            if (hasHeaders && rows.Count > 0)
            {
                headers = rows[0].Cells().Select(c => c.GetString()).ToArray();
                startRow = 1;
            }

            int totalDataRows = rows.Count - startRow;
            int rowsToRead = maxRows > 0 ? Math.Min(maxRows, totalDataRows) : totalDataRows;
            bool isTruncated = maxRows > 0 && totalDataRows > maxRows;

            for (int i = startRow; i < startRow + rowsToRead; i++)
            {
                var cells = rows[i].Cells().ToList();
                var rowData = new Dictionary<string, object>();

                for (int j = 0; j < cells.Count; j++)
                {
                    var key = headers != null && j < headers.Length ? headers[j] : $"Column{j + 1}";
                    var cell = cells[j];

                    rowData[key] = cell.DataType switch
                    {
                        XLDataType.Number => cell.GetDouble(),
                        XLDataType.Boolean => cell.GetBoolean(),
                        XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
                        _ => cell.GetString()
                    };
                }
                result.Add(rowData);
            }

            // Calculate column statistics if requested
            Dictionary<string, object>? statistics = null;
            if (includeStats && headers != null)
            {
                statistics = CalculateColumnStatistics(rows, headers, startRow);
            }

            var response = new Dictionary<string, object>
            {
                ["file"] = filePath,
                ["sheet"] = worksheet.Name,
                ["totalRows"] = totalDataRows,
                ["loadedRows"] = rowsToRead,
                ["columns"] = headers ?? result.FirstOrDefault()?.Keys.ToArray() ?? Array.Empty<string>(),
                ["data"] = result
            };

            if (isTruncated)
            {
                response["truncated"] = true;
                response["message"] = $"Showing first {maxRows} of {totalDataRows} rows. Use max_rows parameter to load more.";
            }

            if (statistics != null)
            {
                response["statistics"] = statistics;
            }

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        });
    }

    /// <summary>
    /// Get Excel file summary without loading all data (for large files)
    /// </summary>
    public async Task<string> GetExcelSummaryAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            using var workbook = new XLWorkbook(filePath);

            var sheets = new List<Dictionary<string, object>>();
            foreach (var worksheet in workbook.Worksheets)
            {
                var usedRange = worksheet.RangeUsed();
                var rowCount = usedRange?.RowsUsed().Count() ?? 0;
                var colCount = usedRange?.ColumnsUsed().Count() ?? 0;

                // Get headers from first row
                string[]? headers = null;
                if (usedRange != null && rowCount > 0)
                {
                    var firstRow = usedRange.RowsUsed().First();
                    headers = firstRow.Cells().Select(c => c.GetString()).ToArray();
                }

                sheets.Add(new Dictionary<string, object>
                {
                    ["name"] = worksheet.Name,
                    ["rowCount"] = rowCount,
                    ["columnCount"] = colCount,
                    ["headers"] = headers ?? Array.Empty<string>()
                });
            }

            return JsonSerializer.Serialize(new
            {
                file = filePath,
                fileSize = fileInfo.Length,
                fileSizeFormatted = FormatFileSize(fileInfo.Length),
                sheetCount = workbook.Worksheets.Count,
                sheets = sheets
            }, new JsonSerializerOptions { WriteIndented = true });
        });
    }

    private static Dictionary<string, object> CalculateColumnStatistics(
        List<IXLRangeRow> rows, string[] headers, int startRow)
    {
        var stats = new Dictionary<string, object>();

        for (int col = 0; col < headers.Length; col++)
        {
            var columnValues = new List<object>();
            var numericValues = new List<double>();

            for (int i = startRow; i < rows.Count; i++)
            {
                var cells = rows[i].Cells().ToList();
                if (col < cells.Count)
                {
                    var cell = cells[col];
                    if (cell.DataType == XLDataType.Number)
                    {
                        numericValues.Add(cell.GetDouble());
                    }
                    columnValues.Add(cell.GetString());
                }
            }

            var colStats = new Dictionary<string, object>
            {
                ["totalValues"] = columnValues.Count,
                ["uniqueValues"] = columnValues.Distinct().Count(),
                ["emptyValues"] = columnValues.Count(v => string.IsNullOrWhiteSpace(v?.ToString()))
            };

            if (numericValues.Count > 0)
            {
                colStats["isNumeric"] = true;
                colStats["min"] = numericValues.Min();
                colStats["max"] = numericValues.Max();
                colStats["sum"] = numericValues.Sum();
                colStats["average"] = numericValues.Average();
            }
            else
            {
                colStats["isNumeric"] = false;
                // Sample values for text columns
                colStats["sampleValues"] = columnValues.Take(5).ToArray();
            }

            stats[headers[col]] = colStats;
        }

        return stats;
    }

    private static int GetInt(Dictionary<string, object> args, string key, int defaultValue)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number)
                    return je.GetInt32();
            }
            if (value is int i) return i;
            if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value == null)
        {
            cell.Value = "";
            return;
        }

        switch (value)
        {
            case JsonElement jsonElement:
                SetCellValueFromJson(cell, jsonElement);
                break;
            case string s:
                cell.Value = s;
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case double d:
                cell.Value = d;
                break;
            case float f:
                cell.Value = f;
                break;
            case decimal dec:
                cell.Value = (double)dec;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "yyyy-MM-dd";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static void SetCellValueFromJson(IXLCell cell, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                cell.Value = element.GetString() ?? "";
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longVal))
                    cell.Value = longVal;
                else
                    cell.Value = element.GetDouble();
                break;
            case JsonValueKind.True:
                cell.Value = true;
                break;
            case JsonValueKind.False:
                cell.Value = false;
                break;
            case JsonValueKind.Null:
                cell.Value = "";
                break;
            default:
                cell.Value = element.ToString();
                break;
        }
    }

    private static string? GetString(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
                return je.GetString();
            return value?.ToString();
        }
        return null;
    }

    private static bool GetBool(Dictionary<string, object> args, string key, bool defaultValue)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True) return true;
                if (je.ValueKind == JsonValueKind.False) return false;
            }
            if (value is bool b) return b;
        }
        return defaultValue;
    }

    private static string[]? GetStringArray(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                return je.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .ToArray();
            }
        }
        return null;
    }

    private static object[][]? GetRowsArray(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                return je.EnumerateArray()
                    .Select(row => row.ValueKind == JsonValueKind.Array
                        ? row.EnumerateArray().Cast<object>().ToArray()
                        : new object[] { row })
                    .ToArray();
            }
        }
        return null;
    }
}
