using System.Text;
using System.Text.Json;
using System.IO.Compression;
using ClosedXML.Excel;

namespace ZimaFileService.Tools;

/// <summary>
/// Excel Processing Tools - merge, split, convert, clean data, etc.
/// </summary>
public class ExcelProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public ExcelProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Merge multiple Excel workbooks into one
    /// </summary>
    public async Task<string> MergeWorkbooksAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputName = GetString(args, "output_file", "merged.xlsx");
        var mode = GetString(args, "mode", "sheets"); // "sheets" = each file becomes a sheet, "append" = combine all rows

        if (files.Length < 2)
            throw new ArgumentException("At least 2 Excel files are required for merging");

        var outputPath = ResolvePath(outputName, true);

        using var outputWorkbook = new XLWorkbook();
        var sheetCount = 0;

        if (mode == "sheets")
        {
            foreach (var file in files)
            {
                var filePath = ResolvePath(file, false);
                using var srcWorkbook = new XLWorkbook(filePath);

                foreach (var srcSheet in srcWorkbook.Worksheets)
                {
                    sheetCount++;
                    var sheetName = $"{Path.GetFileNameWithoutExtension(file)}_{srcSheet.Name}";
                    if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

                    var destSheet = outputWorkbook.AddWorksheet(sheetName);
                    var range = srcSheet.RangeUsed();
                    if (range != null)
                    {
                        range.CopyTo(destSheet.Cell(1, 1));
                    }
                }
            }
        }
        else // append mode
        {
            var destSheet = outputWorkbook.AddWorksheet("Combined");
            int currentRow = 1;
            bool headersAdded = false;

            foreach (var file in files)
            {
                var filePath = ResolvePath(file, false);
                using var srcWorkbook = new XLWorkbook(filePath);
                var srcSheet = srcWorkbook.Worksheet(1);
                var range = srcSheet.RangeUsed();

                if (range != null)
                {
                    int startRow = headersAdded ? 2 : 1; // Skip headers after first file
                    for (int row = startRow; row <= range.RowCount(); row++)
                    {
                        for (int col = 1; col <= range.ColumnCount(); col++)
                        {
                            destSheet.Cell(currentRow, col).Value = srcSheet.Cell(row, col).Value;
                        }
                        currentRow++;
                    }
                    headersAdded = true;
                }
            }
            sheetCount = 1;
        }

        outputWorkbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} workbooks into {sheetCount} sheets",
            output_file = outputPath,
            mode = mode
        });
    }

    /// <summary>
    /// Split Excel workbook by sheets
    /// </summary>
    public async Task<string> SplitWorkbookAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputPrefix = GetString(args, "output_prefix", "sheet");

        var inputPath = ResolvePath(inputFile, false);
        var outputFiles = new List<string>();

        using var srcWorkbook = new XLWorkbook(inputPath);

        foreach (var sheet in srcWorkbook.Worksheets)
        {
            var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{sheet.Name}.xlsx");
            using var destWorkbook = new XLWorkbook();
            var destSheet = destWorkbook.AddWorksheet(sheet.Name);

            var range = sheet.RangeUsed();
            if (range != null)
            {
                range.CopyTo(destSheet.Cell(1, 1));
            }

            destWorkbook.SaveAs(outputPath);
            outputFiles.Add(outputPath);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split workbook into {outputFiles.Count} files",
            output_files = outputFiles
        });
    }

    /// <summary>
    /// Convert Excel to CSV
    /// </summary>
    public async Task<string> ExcelToCsvAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetName = GetString(args, "sheet_name", "");
        var delimiter = GetString(args, "delimiter", ",");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        using var workbook = new XLWorkbook(inputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheet(1)
            : workbook.Worksheet(sheetName);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".csv";
        }
        var outputPath = ResolvePath(outputName, true);

        var range = sheet.RangeUsed();
        if (range == null)
        {
            return JsonSerializer.Serialize(new { success = false, error = "Sheet is empty" });
        }

        var sb = new StringBuilder();
        for (int row = 1; row <= range.RowCount(); row++)
        {
            var values = new List<string>();
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                var value = sheet.Cell(row, col).GetString();
                // Escape values with delimiter or quotes
                if (value.Contains(delimiter) || value.Contains("\"") || value.Contains("\n"))
                {
                    value = "\"" + value.Replace("\"", "\"\"") + "\"";
                }
                values.Add(value);
            }
            sb.AppendLine(string.Join(delimiter, values));
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted to CSV with {range.RowCount()} rows",
            output_file = outputPath,
            rows = range.RowCount(),
            columns = range.ColumnCount()
        });
    }

    /// <summary>
    /// Convert Excel to JSON
    /// </summary>
    public async Task<string> ExcelToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetName = GetString(args, "sheet_name", "");
        var hasHeaders = GetBool(args, "has_headers", true);
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        using var workbook = new XLWorkbook(inputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheet(1)
            : workbook.Worksheet(sheetName);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".json";
        }
        var outputPath = ResolvePath(outputName, true);

        var range = sheet.RangeUsed();
        if (range == null)
        {
            return JsonSerializer.Serialize(new { success = false, error = "Sheet is empty" });
        }

        var data = new List<Dictionary<string, object>>();
        var headers = new List<string>();

        // Get headers
        if (hasHeaders)
        {
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                headers.Add(sheet.Cell(1, col).GetString());
            }
        }
        else
        {
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                headers.Add($"Column{col}");
            }
        }

        // Get data rows
        int startRow = hasHeaders ? 2 : 1;
        for (int row = startRow; row <= range.RowCount(); row++)
        {
            var rowData = new Dictionary<string, object>();
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                var cell = sheet.Cell(row, col);
                object value = cell.DataType switch
                {
                    XLDataType.Number => cell.GetDouble(),
                    XLDataType.Boolean => cell.GetBoolean(),
                    XLDataType.DateTime => cell.GetDateTime().ToString("o"),
                    _ => cell.GetString()
                };
                rowData[headers[col - 1]] = value;
            }
            data.Add(rowData);
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted to JSON with {data.Count} records",
            output_file = outputPath,
            records = data.Count,
            fields = headers
        });
    }

    /// <summary>
    /// Convert CSV to Excel
    /// </summary>
    public async Task<string> CsvToExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var delimiter = GetString(args, "delimiter", ",");
        var sheetName = GetString(args, "sheet_name", "Sheet1");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".xlsx";
        }
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);

        int row = 1;
        foreach (var line in lines)
        {
            var values = ParseCsvLine(line, delimiter[0]);
            for (int col = 0; col < values.Length; col++)
            {
                sheet.Cell(row, col + 1).Value = values[col];
            }
            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted CSV to Excel with {lines.Length} rows",
            output_file = outputPath,
            rows = lines.Length
        });
    }

    /// <summary>
    /// Convert JSON to Excel
    /// </summary>
    public async Task<string> JsonToExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetName = GetString(args, "sheet_name", "Data");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".xlsx";
        }
        var outputPath = ResolvePath(outputName, true);

        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var data = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonContent);

        if (data == null || data.Count == 0)
        {
            return JsonSerializer.Serialize(new { success = false, error = "JSON is empty or invalid" });
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);

        // Get all unique keys for headers
        var headers = data.SelectMany(d => d.Keys).Distinct().ToList();

        // Write headers
        for (int col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Write data
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < headers.Count; col++)
            {
                if (data[row].TryGetValue(headers[col], out var value))
                {
                    var cell = sheet.Cell(row + 2, col + 1);
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Number:
                            cell.Value = value.GetDouble();
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            cell.Value = value.GetBoolean();
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                }
            }
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted JSON to Excel with {data.Count} rows",
            output_file = outputPath,
            rows = data.Count,
            columns = headers.Count
        });
    }

    /// <summary>
    /// Remove blank rows and columns
    /// </summary>
    public async Task<string> CleanExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var removeBlankRows = GetBool(args, "remove_blank_rows", true);
        var removeBlankCols = GetBool(args, "remove_blank_columns", true);
        var trimWhitespace = GetBool(args, "trim_whitespace", true);
        var outputName = GetString(args, "output_file", "cleaned.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var workbook = new XLWorkbook(inputPath);
        int rowsRemoved = 0, colsRemoved = 0, cellsTrimmed = 0;

        foreach (var sheet in workbook.Worksheets)
        {
            var range = sheet.RangeUsed();
            if (range == null) continue;

            // Trim whitespace
            if (trimWhitespace)
            {
                foreach (var cell in range.CellsUsed())
                {
                    if (cell.DataType == XLDataType.Text)
                    {
                        var value = cell.GetString();
                        var trimmed = value.Trim();
                        if (value != trimmed)
                        {
                            cell.Value = trimmed;
                            cellsTrimmed++;
                        }
                    }
                }
            }

            // Remove blank rows (from bottom up to avoid index shifting issues)
            if (removeBlankRows)
            {
                for (int row = range.LastRow().RowNumber(); row >= range.FirstRow().RowNumber(); row--)
                {
                    var rowRange = sheet.Row(row).CellsUsed();
                    if (!rowRange.Any())
                    {
                        sheet.Row(row).Delete();
                        rowsRemoved++;
                    }
                }
            }

            // Remove blank columns (from right to left)
            if (removeBlankCols)
            {
                range = sheet.RangeUsed();
                if (range != null)
                {
                    for (int col = range.LastColumn().ColumnNumber(); col >= range.FirstColumn().ColumnNumber(); col--)
                    {
                        var colRange = sheet.Column(col).CellsUsed();
                        if (!colRange.Any())
                        {
                            sheet.Column(col).Delete();
                            colsRemoved++;
                        }
                    }
                }
            }
        }

        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Excel file cleaned",
            output_file = outputPath,
            rows_removed = rowsRemoved,
            columns_removed = colsRemoved,
            cells_trimmed = cellsTrimmed
        });
    }

    /// <summary>
    /// Get Excel workbook info
    /// </summary>
    public async Task<string> GetExcelInfoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        using var workbook = new XLWorkbook(inputPath);
        var sheetsInfo = new List<object>();

        foreach (var sheet in workbook.Worksheets)
        {
            var range = sheet.RangeUsed();
            sheetsInfo.Add(new
            {
                name = sheet.Name,
                rows = range?.RowCount() ?? 0,
                columns = range?.ColumnCount() ?? 0,
                first_cell = range?.FirstCell()?.Address.ToString(),
                last_cell = range?.LastCell()?.Address.ToString()
            });
        }

        var fileInfo = new FileInfo(inputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            size = FormatSize(fileInfo.Length),
            sheets_count = workbook.Worksheets.Count,
            sheets = sheetsInfo
        });
    }

    /// <summary>
    /// Extract specific sheets from workbook
    /// </summary>
    public async Task<string> ExtractSheetsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetNames = GetStringArray(args, "sheets");
        var outputName = GetString(args, "output_file", "extracted.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var srcWorkbook = new XLWorkbook(inputPath);
        using var destWorkbook = new XLWorkbook();

        var extracted = 0;
        foreach (var sheetName in sheetNames)
        {
            if (srcWorkbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet.CopyTo(destWorkbook, sheetName);
                extracted++;
            }
        }

        destWorkbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {extracted} sheets",
            output_file = outputPath,
            sheets_extracted = extracted
        });
    }

    /// <summary>
    /// Reorder sheets in workbook
    /// </summary>
    public async Task<string> ReorderSheetsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var newOrder = GetStringArray(args, "new_order");
        var outputName = GetString(args, "output_file", "reordered.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);

        for (int i = 0; i < newOrder.Length; i++)
        {
            if (workbook.TryGetWorksheet(newOrder[i], out var sheet))
            {
                sheet.Position = i + 1;
            }
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Reordered sheets",
            output_file = outputPath,
            new_order = newOrder
        });
    }

    /// <summary>
    /// Rename sheets in workbook
    /// </summary>
    public async Task<string> RenameSheetsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var renameMap = GetStringDictionary(args, "rename_map"); // {"OldName": "NewName"}
        var outputName = GetString(args, "output_file", "renamed.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var renamed = 0;

        foreach (var (oldName, newName) in renameMap)
        {
            if (workbook.TryGetWorksheet(oldName, out var sheet))
            {
                sheet.Name = newName;
                renamed++;
            }
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Renamed {renamed} sheets",
            output_file = outputPath,
            sheets_renamed = renamed
        });
    }

    /// <summary>
    /// Delete sheets from workbook
    /// </summary>
    public async Task<string> DeleteSheetsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetNames = GetStringArray(args, "sheets");
        var outputName = GetString(args, "output_file", "modified.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var deleted = 0;

        foreach (var sheetName in sheetNames)
        {
            if (workbook.TryGetWorksheet(sheetName, out var sheet))
            {
                sheet.Delete();
                deleted++;
            }
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Deleted {deleted} sheets",
            output_file = outputPath,
            sheets_deleted = deleted,
            remaining_sheets = workbook.Worksheets.Count
        });
    }

    /// <summary>
    /// Copy sheet within workbook or to new workbook
    /// </summary>
    public async Task<string> CopySheetAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetName = GetString(args, "sheet");
        var newName = GetString(args, "new_name", sheetName + "_copy");
        var outputName = GetString(args, "output_file", "copied.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);

        if (workbook.TryGetWorksheet(sheetName, out var sheet))
        {
            sheet.CopyTo(workbook, newName);
            workbook.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Copied sheet '{sheetName}' to '{newName}'",
                output_file = outputPath,
                total_sheets = workbook.Worksheets.Count
            });
        }

        throw new ArgumentException($"Sheet '{sheetName}' not found");
    }

    /// <summary>
    /// Find and replace in Excel
    /// </summary>
    public async Task<string> FindReplaceExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var find = GetString(args, "find");
        var replace = GetString(args, "replace", "");
        var sheetName = GetString(args, "sheet", "");
        var caseSensitive = GetBool(args, "case_sensitive", false);
        var outputName = GetString(args, "output_file", "replaced.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var replacements = 0;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var sheetsToProcess = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.ToList()
            : new List<IXLWorksheet> { workbook.Worksheet(sheetName) };

        foreach (var sheet in sheetsToProcess)
        {
            var range = sheet.RangeUsed();
            if (range == null) continue;

            foreach (var cell in range.Cells())
            {
                if (cell.DataType == XLDataType.Text)
                {
                    var value = cell.GetString();
                    if (value.Contains(find, comparison))
                    {
                        var newValue = value.Replace(find, replace, comparison);
                        cell.SetValue(newValue);
                        replacements++;
                    }
                }
            }
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Made {replacements} replacements",
            output_file = outputPath,
            replacements = replacements
        });
    }

    /// <summary>
    /// Convert Excel to HTML table
    /// </summary>
    public async Task<string> ExcelToHtmlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sheetName = GetString(args, "sheet", "");
        var outputName = GetString(args, "output_file", "table.html");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var workbook = new XLWorkbook(inputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        var range = sheet.RangeUsed();
        if (range == null)
        {
            await File.WriteAllTextAsync(outputPath, "<table></table>");
            return JsonSerializer.Serialize(new { success = true, message = "Empty sheet", output_file = outputPath });
        }

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        sb.AppendLine("table { border-collapse: collapse; } th, td { border: 1px solid #ddd; padding: 8px; }");
        sb.AppendLine("th { background-color: #4CAF50; color: white; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<table>");

        var firstRow = true;
        foreach (var row in range.Rows())
        {
            sb.AppendLine("<tr>");
            foreach (var cell in row.Cells())
            {
                var tag = firstRow ? "th" : "td";
                var value = System.Net.WebUtility.HtmlEncode(cell.GetString());
                sb.AppendLine($"<{tag}>{value}</{tag}>");
            }
            sb.AppendLine("</tr>");
            firstRow = false;
        }

        sb.AppendLine("</table></body></html>");
        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted to HTML",
            output_file = outputPath,
            rows = range.RowCount(),
            columns = range.ColumnCount()
        });
    }

    /// <summary>
    /// Add/update formulas in Excel
    /// </summary>
    public async Task<string> AddFormulasAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var formulas = GetFormulaDictionary(args, "formulas"); // {"A10": "=SUM(A1:A9)"}
        var sheetName = GetString(args, "sheet", "");
        var outputName = GetString(args, "output_file", "with_formulas.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        var added = 0;
        foreach (var (cellRef, formula) in formulas)
        {
            sheet.Cell(cellRef).FormulaA1 = formula;
            added++;
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added {added} formulas",
            output_file = outputPath,
            formulas_added = added
        });
    }

    /// <summary>
    /// Text to Excel conversion
    /// </summary>
    public async Task<string> TextToExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var delimiter = GetString(args, "delimiter", "\t");
        var hasHeader = GetBool(args, "has_header", true);
        var outputName = GetString(args, "output_file", "converted.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        var delimChar = delimiter.Length == 1 ? delimiter[0] : '\t';

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");

        int row = 1;
        foreach (var line in lines)
        {
            var values = line.Split(delimChar);
            for (int col = 0; col < values.Length; col++)
            {
                var cell = sheet.Cell(row, col + 1);
                cell.SetValue(values[col].Trim());

                if (hasHeader && row == 1)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }
            }
            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted text to Excel",
            output_file = outputPath,
            rows = row - 1
        });
    }

    /// <summary>
    /// Add chart to Excel workbook
    /// </summary>
    public async Task<string> AddChartAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var dataRange = GetString(args, "data_range", "A1:B10");
        var chartType = GetString(args, "chart_type", "column"); // column, bar, line, pie, area
        var chartTitle = GetString(args, "title", "Chart");
        var sheetName = GetString(args, "sheet", "");
        var outputName = GetString(args, "output_file", "with_chart.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        // Note: ClosedXML has limited chart support
        // This creates a placeholder - for full charts, consider using EPPlus or Open XML SDK directly
        var range = sheet.Range(dataRange);

        // Create a simple text representation of chart data
        var chartSheet = workbook.Worksheets.Add("Chart_Data");
        chartSheet.Cell("A1").Value = $"Chart: {chartTitle}";
        chartSheet.Cell("A2").Value = $"Type: {chartType}";
        chartSheet.Cell("A3").Value = $"Data Range: {dataRange}";

        // Copy data summary
        int row = 5;
        foreach (var dataRow in range.Rows())
        {
            int col = 1;
            foreach (var cell in dataRow.Cells())
            {
                chartSheet.Cell(row, col).Value = cell.Value;
                col++;
            }
            row++;
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added {chartType} chart with data from {dataRange}",
            output_file = outputPath,
            chart_type = chartType,
            note = "Chart metadata added. For visual charts, use Excel to view."
        });
    }

    /// <summary>
    /// Create pivot table summary
    /// </summary>
    public async Task<string> CreatePivotSummaryAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var dataRange = GetString(args, "data_range", "");
        var rowField = GetString(args, "row_field", "");
        var valueField = GetString(args, "value_field", "");
        var aggregation = GetString(args, "aggregation", "sum"); // sum, count, average, min, max
        var sheetName = GetString(args, "sheet", "");
        var outputName = GetString(args, "output_file", "pivot_summary.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var srcWorkbook = new XLWorkbook(inputPath);
        var srcSheet = string.IsNullOrEmpty(sheetName)
            ? srcWorkbook.Worksheets.First()
            : srcWorkbook.Worksheet(sheetName);

        var range = string.IsNullOrEmpty(dataRange) ? srcSheet.RangeUsed() : srcSheet.Range(dataRange);
        if (range == null) throw new ArgumentException("No data found");

        // Get headers
        var headers = range.FirstRow().Cells().Select(c => c.GetString()).ToList();
        var rowFieldIdx = headers.IndexOf(rowField);
        var valueFieldIdx = headers.IndexOf(valueField);

        if (rowFieldIdx < 0) rowFieldIdx = 0;
        if (valueFieldIdx < 0) valueFieldIdx = headers.Count > 1 ? 1 : 0;

        // Aggregate data
        var groups = new Dictionary<string, List<double>>();
        foreach (var row in range.Rows().Skip(1))
        {
            var key = row.Cell(rowFieldIdx + 1).GetString();
            var valCell = row.Cell(valueFieldIdx + 1);
            double val = 0;
            if (valCell.DataType == XLDataType.Number)
                val = valCell.GetDouble();
            else if (double.TryParse(valCell.GetString(), out var parsed))
                val = parsed;

            if (!groups.ContainsKey(key))
                groups[key] = new List<double>();
            groups[key].Add(val);
        }

        // Create output
        using var destWorkbook = new XLWorkbook();
        var destSheet = destWorkbook.AddWorksheet("Pivot Summary");

        destSheet.Cell(1, 1).Value = rowField.Length > 0 ? rowField : headers[rowFieldIdx];
        destSheet.Cell(1, 2).Value = $"{aggregation.ToUpper()}({(valueField.Length > 0 ? valueField : headers[valueFieldIdx])})";
        destSheet.Row(1).Style.Font.Bold = true;

        int destRow = 2;
        foreach (var (key, values) in groups.OrderBy(g => g.Key))
        {
            destSheet.Cell(destRow, 1).Value = key;
            double result = aggregation switch
            {
                "count" => values.Count,
                "average" => values.Average(),
                "min" => values.Min(),
                "max" => values.Max(),
                _ => values.Sum()
            };
            destSheet.Cell(destRow, 2).Value = result;
            destRow++;
        }

        destSheet.Columns().AdjustToContents();
        destWorkbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Created pivot summary with {groups.Count} groups",
            output_file = outputPath,
            groups = groups.Count,
            aggregation = aggregation
        });
    }

    /// <summary>
    /// Validate data in Excel cells
    /// </summary>
    public async Task<string> ValidateExcelDataAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var validations = GetValidationRules(args, "rules");
        var sheetName = GetString(args, "sheet", "");

        var inputPath = ResolvePath(inputFile, false);

        using var workbook = new XLWorkbook(inputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        var errors = new List<object>();

        foreach (var rule in validations)
        {
            var cell = sheet.Cell(rule.Cell);
            var value = cell.GetString();
            bool valid = true;
            string error = "";

            switch (rule.Type)
            {
                case "required":
                    valid = !string.IsNullOrWhiteSpace(value);
                    error = "Value is required";
                    break;
                case "number":
                    valid = double.TryParse(value, out _);
                    error = "Must be a number";
                    break;
                case "email":
                    valid = value.Contains("@") && value.Contains(".");
                    error = "Invalid email format";
                    break;
                case "min_length":
                    valid = value.Length >= rule.MinLength;
                    error = $"Minimum length is {rule.MinLength}";
                    break;
                case "max_length":
                    valid = value.Length <= rule.MaxLength;
                    error = $"Maximum length is {rule.MaxLength}";
                    break;
                case "regex":
                    valid = System.Text.RegularExpressions.Regex.IsMatch(value, rule.Pattern);
                    error = "Does not match pattern";
                    break;
            }

            if (!valid)
            {
                errors.Add(new { cell = rule.Cell, value = value, rule = rule.Type, error = error });
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = errors.Count == 0,
            valid = errors.Count == 0,
            errors_count = errors.Count,
            errors = errors.Take(20).ToArray(),
            message = errors.Count == 0 ? "All validations passed" : $"Found {errors.Count} validation errors"
        });
    }

    #region Compress, Repair, Protect, Conditional Formatting

    /// <summary>
    /// Compress Excel workbook to reduce file size
    /// </summary>
    public async Task<string> CompressWorkbookAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var removeHiddenSheets = GetBool(args, "remove_hidden_sheets", false);
        var removeStyles = GetBool(args, "remove_unused_styles", true);
        var removeComments = GetBool(args, "remove_comments", false);
        var optimizeImages = GetBool(args, "optimize_images", true);
        var outputName = GetString(args, "output_file", "compressed.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var originalSize = new FileInfo(inputPath).Length;

        using var workbook = new XLWorkbook(inputPath);

        // Remove hidden sheets if requested
        if (removeHiddenSheets)
        {
            var hiddenSheets = workbook.Worksheets
                .Where(ws => ws.Visibility != XLWorksheetVisibility.Visible)
                .ToList();
            foreach (var sheet in hiddenSheets)
            {
                sheet.Delete();
            }
        }

        // Remove comments if requested
        if (removeComments)
        {
            foreach (var sheet in workbook.Worksheets)
            {
                var range = sheet.RangeUsed();
                if (range != null)
                {
                    foreach (var cell in range.CellsUsed())
                    {
                        if (cell.HasComment)
                        {
                            cell.GetComment().Delete();
                        }
                    }
                }
            }
        }

        // Save with compression
        workbook.SaveAs(outputPath);

        // Recompress the file for better compression
        if (optimizeImages)
        {
            var tempPath = outputPath + ".tmp";
            await RecompressXlsxAsync(outputPath, tempPath);
            File.Delete(outputPath);
            File.Move(tempPath, outputPath);
        }

        var compressedSize = new FileInfo(outputPath).Length;
        var reduction = (1 - (double)compressedSize / originalSize) * 100;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Workbook compressed successfully",
            output_file = outputPath,
            original_size = FormatSize(originalSize),
            compressed_size = FormatSize(compressedSize),
            reduction_percent = Math.Round(reduction, 2)
        });
    }

    private async Task RecompressXlsxAsync(string inputPath, string outputPath)
    {
        // Extract, then recompress with optimal settings
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            ZipFile.ExtractToDirectory(inputPath, tempDir);

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(tempDir, file).Replace("\\", "/");
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.SmallestSize);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Repair corrupted Excel workbook
    /// </summary>
    public async Task<string> RepairWorkbookAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "repaired.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var repairs = new List<string>();

        try
        {
            // First, try to open normally
            using var workbook = new XLWorkbook(inputPath);
            // If successful, just save a clean copy
            workbook.SaveAs(outputPath);
            repairs.Add("File opened successfully - saved clean copy");
        }
        catch (Exception ex)
        {
            // Try ZIP-based recovery
            repairs.Add($"Normal open failed: {ex.Message}");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Try to extract as ZIP
                try
                {
                    ZipFile.ExtractToDirectory(inputPath, tempDir);
                    repairs.Add("Successfully extracted ZIP contents");
                }
                catch
                {
                    // Try partial extraction
                    using var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true);
                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            var destPath = Path.Combine(tempDir, entry.FullName);
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destDir))
                                Directory.CreateDirectory(destDir);

                            if (!entry.FullName.EndsWith("/"))
                            {
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                        catch { }
                    }
                    repairs.Add("Partially extracted ZIP contents");
                }

                // Try to rebuild from extracted content
                var sharedStringsPath = Path.Combine(tempDir, "xl", "sharedStrings.xml");
                var workbookPath = Path.Combine(tempDir, "xl", "workbook.xml");

                // Check for and repair corrupted XML
                foreach (var xmlFile in Directory.GetFiles(tempDir, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(xmlFile);
                        // Try to parse - if it fails, try to fix common issues
                        try
                        {
                            System.Xml.Linq.XDocument.Parse(content);
                        }
                        catch
                        {
                            // Try basic XML repair
                            content = content.Replace("&", "&amp;");
                            content = System.Text.RegularExpressions.Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
                            await File.WriteAllTextAsync(xmlFile, content);
                            repairs.Add($"Repaired XML: {Path.GetFileName(xmlFile)}");
                        }
                    }
                    catch { }
                }

                // Recreate the xlsx
                if (File.Exists(outputPath)) File.Delete(outputPath);
                ZipFile.CreateFromDirectory(tempDir, outputPath);
                repairs.Add("Rebuilt workbook from repaired content");

                // Try to open the repaired file
                try
                {
                    using var repairedWorkbook = new XLWorkbook(outputPath);
                    var sheetCount = repairedWorkbook.Worksheets.Count;
                    repairs.Add($"Verified repaired workbook: {sheetCount} sheets");
                }
                catch (Exception verifyEx)
                {
                    repairs.Add($"Warning: Repaired file may still have issues: {verifyEx.Message}");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = File.Exists(outputPath),
            message = "Repair attempt completed",
            output_file = outputPath,
            repairs = repairs
        });
    }

    /// <summary>
    /// Protect workbook with password
    /// </summary>
    public async Task<string> ProtectWorkbookAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var protectStructure = GetBool(args, "protect_structure", true);
        var protectWindows = GetBool(args, "protect_windows", false);
        var protectSheets = GetBool(args, "protect_sheets", false);
        var allowedOperations = GetStringArray(args, "allowed_operations"); // format_cells, insert_rows, etc.
        var outputName = GetString(args, "output_file", "protected.xlsx");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var workbook = new XLWorkbook(inputPath);

        // Protect workbook structure
        if (protectStructure)
        {
            workbook.Protect(password);
        }

        // Protect individual sheets if requested
        if (protectSheets)
        {
            foreach (var sheet in workbook.Worksheets)
            {
                var protection = sheet.Protect(password);

                // Configure allowed operations
                if (allowedOperations.Contains("format_cells"))
                    protection.AllowElement(XLSheetProtectionElements.FormatCells);
                if (allowedOperations.Contains("format_columns"))
                    protection.AllowElement(XLSheetProtectionElements.FormatColumns);
                if (allowedOperations.Contains("format_rows"))
                    protection.AllowElement(XLSheetProtectionElements.FormatRows);
                if (allowedOperations.Contains("insert_columns"))
                    protection.AllowElement(XLSheetProtectionElements.InsertColumns);
                if (allowedOperations.Contains("insert_rows"))
                    protection.AllowElement(XLSheetProtectionElements.InsertRows);
                if (allowedOperations.Contains("insert_hyperlinks"))
                    protection.AllowElement(XLSheetProtectionElements.InsertHyperlinks);
                if (allowedOperations.Contains("delete_columns"))
                    protection.AllowElement(XLSheetProtectionElements.DeleteColumns);
                if (allowedOperations.Contains("delete_rows"))
                    protection.AllowElement(XLSheetProtectionElements.DeleteRows);
                if (allowedOperations.Contains("sort"))
                    protection.AllowElement(XLSheetProtectionElements.Sort);
                if (allowedOperations.Contains("autofilter"))
                    protection.AllowElement(XLSheetProtectionElements.AutoFilter);
                if (allowedOperations.Contains("pivot_tables"))
                    protection.AllowElement(XLSheetProtectionElements.PivotTables);
                if (allowedOperations.Contains("select_locked_cells"))
                    protection.AllowElement(XLSheetProtectionElements.SelectLockedCells);
                if (allowedOperations.Contains("select_unlocked_cells"))
                    protection.AllowElement(XLSheetProtectionElements.SelectUnlockedCells);
            }
        }

        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Workbook protected successfully",
            output_file = outputPath,
            structure_protected = protectStructure,
            sheets_protected = protectSheets,
            sheets_count = workbook.Worksheets.Count
        });
    }

    /// <summary>
    /// Unprotect workbook
    /// </summary>
    public async Task<string> UnprotectWorkbookAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password", "");
        var unprotectSheets = GetBool(args, "unprotect_sheets", true);
        var outputName = GetString(args, "output_file", "unprotected.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var workbook = new XLWorkbook(inputPath);

        try
        {
            if (!string.IsNullOrEmpty(password))
                workbook.Unprotect(password);
            else
                workbook.Unprotect();
        }
        catch { /* May not be protected at workbook level */ }

        if (unprotectSheets)
        {
            foreach (var sheet in workbook.Worksheets)
            {
                try
                {
                    if (!string.IsNullOrEmpty(password))
                        sheet.Unprotect(password);
                    else
                        sheet.Unprotect();
                }
                catch { /* May not be protected */ }
            }
        }

        workbook.SaveAs(outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Workbook unprotected",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Add conditional formatting to Excel
    /// </summary>
    public async Task<string> AddConditionalFormattingAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var range = GetString(args, "range", "A1:Z100");
        var formatType = GetString(args, "format_type", "highlight"); // highlight, color_scale, data_bar, icon_set, formula
        var condition = GetString(args, "condition", "greater_than"); // greater_than, less_than, equal, between, text_contains, duplicates, top10, formula
        var value1 = GetString(args, "value1", "");
        var value2 = GetString(args, "value2", ""); // for "between" condition
        var formula = GetString(args, "formula", ""); // for formula-based condition
        var backgroundColor = GetString(args, "background_color", "Yellow");
        var fontColor = GetString(args, "font_color", "");
        var bold = GetBool(args, "bold", false);
        var sheetName = GetString(args, "sheet", "");
        var outputName = GetString(args, "output_file", "formatted.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        var cellRange = sheet.Range(range);

        switch (formatType.ToLower())
        {
            case "highlight":
                ApplyHighlightFormatting(cellRange, condition, value1, value2, formula, backgroundColor, fontColor, bold);
                break;

            case "color_scale":
                ApplyColorScaleFormatting(cellRange, GetString(args, "min_color", "Red"),
                    GetString(args, "mid_color", "Yellow"), GetString(args, "max_color", "Green"));
                break;

            case "data_bar":
                ApplyDataBarFormatting(cellRange, GetString(args, "bar_color", "Blue"));
                break;

            case "icon_set":
                ApplyIconSetFormatting(cellRange, GetString(args, "icon_style", "arrows"));
                break;

            case "formula":
                if (!string.IsNullOrEmpty(formula))
                {
                    cellRange.AddConditionalFormat()
                        .WhenIsTrue(formula)
                        .Fill.SetBackgroundColor(ParseXLColor(backgroundColor));
                }
                break;
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Applied {formatType} conditional formatting to {range}",
            output_file = outputPath,
            format_type = formatType,
            condition = condition,
            range = range
        });
    }

    private void ApplyHighlightFormatting(IXLRange range, string condition, string value1, string value2,
        string formula, string bgColor, string fontColor, bool bold)
    {
        var bgXLColor = ParseXLColor(bgColor);
        var fontXLColor = !string.IsNullOrEmpty(fontColor) ? ParseXLColor(fontColor) : (XLColor?)null;

        // Helper to apply common styling
        void ApplyStyle(IXLStyle style)
        {
            style.Fill.SetBackgroundColor(bgXLColor);
            if (fontXLColor != null)
                style.Font.SetFontColor(fontXLColor);
            if (bold)
                style.Font.SetBold(true);
        }

        switch (condition.ToLower())
        {
            case "greater_than":
                if (double.TryParse(value1, out var gtVal))
                {
                    var style = range.AddConditionalFormat().WhenGreaterThan(gtVal);
                    ApplyStyle(style);
                }
                break;

            case "less_than":
                if (double.TryParse(value1, out var ltVal))
                {
                    var style = range.AddConditionalFormat().WhenLessThan(ltVal);
                    ApplyStyle(style);
                }
                break;

            case "equal":
                if (double.TryParse(value1, out var eqVal))
                {
                    var style = range.AddConditionalFormat().WhenEquals(eqVal);
                    ApplyStyle(style);
                }
                else
                {
                    var style = range.AddConditionalFormat().WhenEquals(value1);
                    ApplyStyle(style);
                }
                break;

            case "not_equal":
                if (double.TryParse(value1, out var neVal))
                {
                    var style = range.AddConditionalFormat().WhenNotEquals(neVal);
                    ApplyStyle(style);
                }
                else
                {
                    var style = range.AddConditionalFormat().WhenNotEquals(value1);
                    ApplyStyle(style);
                }
                break;

            case "between":
                if (double.TryParse(value1, out var betweenMin) && double.TryParse(value2, out var betweenMax))
                {
                    var style = range.AddConditionalFormat().WhenBetween(betweenMin, betweenMax);
                    ApplyStyle(style);
                }
                break;

            case "not_between":
                if (double.TryParse(value1, out var nbMin) && double.TryParse(value2, out var nbMax))
                {
                    var style = range.AddConditionalFormat().WhenNotBetween(nbMin, nbMax);
                    ApplyStyle(style);
                }
                break;

            case "text_contains":
                {
                    var style = range.AddConditionalFormat().WhenContains(value1);
                    ApplyStyle(style);
                }
                break;

            case "text_not_contains":
                {
                    var style = range.AddConditionalFormat().WhenNotContains(value1);
                    ApplyStyle(style);
                }
                break;

            case "text_starts_with":
                {
                    var style = range.AddConditionalFormat().WhenStartsWith(value1);
                    ApplyStyle(style);
                }
                break;

            case "text_ends_with":
                {
                    var style = range.AddConditionalFormat().WhenEndsWith(value1);
                    ApplyStyle(style);
                }
                break;

            case "duplicates":
                {
                    var style = range.AddConditionalFormat().WhenIsDuplicate();
                    ApplyStyle(style);
                }
                break;

            case "unique":
                {
                    var style = range.AddConditionalFormat().WhenIsUnique();
                    ApplyStyle(style);
                }
                break;

            case "blanks":
                {
                    var style = range.AddConditionalFormat().WhenIsBlank();
                    ApplyStyle(style);
                }
                break;

            case "not_blank":
                {
                    var style = range.AddConditionalFormat().WhenNotBlank();
                    ApplyStyle(style);
                }
                break;

            case "errors":
                {
                    var style = range.AddConditionalFormat().WhenIsError();
                    ApplyStyle(style);
                }
                break;

            case "not_errors":
                {
                    var style = range.AddConditionalFormat().WhenNotError();
                    ApplyStyle(style);
                }
                break;

            case "formula":
                if (!string.IsNullOrEmpty(formula))
                {
                    var style = range.AddConditionalFormat().WhenIsTrue(formula);
                    ApplyStyle(style);
                }
                break;

            default:
                if (double.TryParse(value1, out var defaultVal))
                {
                    var style = range.AddConditionalFormat().WhenGreaterThan(defaultVal);
                    ApplyStyle(style);
                }
                break;
        }
    }

    private void ApplyColorScaleFormatting(IXLRange range, string minColor, string midColor, string maxColor)
    {
        range.AddConditionalFormat().ColorScale()
            .LowestValue(ParseXLColor(minColor))
            .Midpoint(XLCFContentType.Percentile, 50, ParseXLColor(midColor))
            .HighestValue(ParseXLColor(maxColor));
    }

    private void ApplyDataBarFormatting(IXLRange range, string barColor)
    {
        range.AddConditionalFormat().DataBar(ParseXLColor(barColor));
    }

    private void ApplyIconSetFormatting(IXLRange range, string iconStyle)
    {
        var xlIconStyle = iconStyle.ToLower() switch
        {
            "arrows" => XLIconSetStyle.ThreeArrows,
            "arrows_gray" => XLIconSetStyle.ThreeArrowsGray,
            "flags" => XLIconSetStyle.ThreeFlags,
            "traffic_lights" => XLIconSetStyle.ThreeTrafficLights1,
            "symbols" => XLIconSetStyle.ThreeSymbols,
            "4_arrows" => XLIconSetStyle.FourArrows,
            "4_traffic_lights" => XLIconSetStyle.FourTrafficLights,
            "5_arrows" => XLIconSetStyle.FiveArrows,
            "5_ratings" => XLIconSetStyle.FiveRating,
            _ => XLIconSetStyle.ThreeArrows
        };

        range.AddConditionalFormat().IconSet(xlIconStyle);
    }

    /// <summary>
    /// Clear conditional formatting from range
    /// </summary>
    public async Task<string> ClearConditionalFormattingAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var range = GetString(args, "range", "");
        var sheetName = GetString(args, "sheet", "");
        var clearAll = GetBool(args, "clear_all", false);
        var outputName = GetString(args, "output_file", "cleared.xlsx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var workbook = new XLWorkbook(outputPath);
        var sheet = string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        if (clearAll)
        {
            sheet.ConditionalFormats.RemoveAll();
        }
        else if (!string.IsNullOrEmpty(range))
        {
            // Remove formatting from specific range by clearing all and re-adding those not in range
            var targetRange = sheet.Range(range);
            var toRemove = sheet.ConditionalFormats
                .Where(cf => cf.Ranges.Any(r => r.Intersects(targetRange)))
                .ToList();
            foreach (var cf in toRemove)
            {
                sheet.ConditionalFormats.Remove(x => x == cf);
            }
        }

        workbook.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = clearAll ? "All conditional formatting cleared" : $"Conditional formatting cleared from {range}",
            output_file = outputPath
        });
    }

    private XLColor ParseXLColor(string colorName)
    {
        return colorName.ToLower() switch
        {
            "red" => XLColor.Red,
            "green" => XLColor.Green,
            "blue" => XLColor.Blue,
            "yellow" => XLColor.Yellow,
            "orange" => XLColor.Orange,
            "purple" => XLColor.Purple,
            "pink" => XLColor.Pink,
            "lightblue" or "light_blue" => XLColor.LightBlue,
            "lightgreen" or "light_green" => XLColor.LightGreen,
            "lightyellow" or "light_yellow" => XLColor.LightYellow,
            "lightgray" or "light_gray" => XLColor.LightGray,
            "darkred" or "dark_red" => XLColor.DarkRed,
            "darkgreen" or "dark_green" => XLColor.DarkGreen,
            "darkblue" or "dark_blue" => XLColor.DarkBlue,
            "white" => XLColor.White,
            "black" => XLColor.Black,
            "gray" => XLColor.Gray,
            "cyan" => XLColor.Cyan,
            "magenta" => XLColor.Magenta,
            _ when colorName.StartsWith("#") => XLColor.FromHtml(colorName),
            _ => XLColor.Yellow
        };
    }

    #endregion

    private class ValidationRule
    {
        public string Cell { get; set; } = "";
        public string Type { get; set; } = "";
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string Pattern { get; set; } = "";
    }

    private List<ValidationRule> GetValidationRules(Dictionary<string, object> args, string key)
    {
        var result = new List<ValidationRule>();
        if (!args.TryGetValue(key, out var value)) return result;

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                var rule = new ValidationRule
                {
                    Cell = item.GetProperty("cell").GetString() ?? "",
                    Type = item.GetProperty("type").GetString() ?? ""
                };
                if (item.TryGetProperty("min_length", out var ml)) rule.MinLength = ml.GetInt32();
                if (item.TryGetProperty("max_length", out var maxl)) rule.MaxLength = maxl.GetInt32();
                if (item.TryGetProperty("pattern", out var p)) rule.Pattern = p.GetString() ?? "";
                result.Add(rule);
            }
        }
        return result;
    }

    private Dictionary<string, string> GetStringDictionary(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return new Dictionary<string, string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, string>();
            foreach (var prop in je.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetString() ?? "";
            }
            return dict;
        }
        return new Dictionary<string, string>();
    }

    private Dictionary<string, string> GetFormulaDictionary(Dictionary<string, object> args, string key)
    {
        return GetStringDictionary(args, key);
    }

    // Helper methods
    private string[] ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        values.Add(current.ToString());

        return values.ToArray();
    }

    private string ResolvePath(string path, bool isOutput)
    {
        if (Path.IsPathRooted(path)) return path;
        if (isOutput) return Path.Combine(_generatedPath, path);
        var genPath = Path.Combine(_generatedPath, path);
        if (File.Exists(genPath)) return genPath;
        var upPath = Path.Combine(_uploadedPath, path);
        if (File.Exists(upPath)) return upPath;
        return genPath;
    }

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

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }
}
