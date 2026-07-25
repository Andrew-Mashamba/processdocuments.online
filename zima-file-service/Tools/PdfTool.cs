using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;

namespace ZimaFileService.Tools;

public class PdfTool : IFileTool
{
    public string Name => "pdf";
    public string Description => "Create PDF documents";

    public async Task<string> CreatePdfAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        var title = GetString(arguments, "title");
        var contentBlocks = GetContentArray(arguments, "content");
        var pageSizeStr = GetString(arguments, "page_size") ?? "A4";

        return await Task.Run(() =>
        {
            try
            {
            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Determine page size
            var pageSize = pageSizeStr.ToUpper() switch
            {
                "LETTER" => PageSize.LETTER,
                "LEGAL" => PageSize.LEGAL,
                "A3" => PageSize.A3,
                "A5" => PageSize.A5,
                _ => PageSize.A4
            };

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, pageSize);

            // Set margins
            document.SetMargins(50, 50, 50, 50);

            // Load fonts
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            int paragraphCount = 0;
            int tableCount = 0;

            // Add title if provided
            if (!string.IsNullOrEmpty(title))
            {
                var titlePara = new Paragraph(title)
                    .SetFont(boldFont)
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(titlePara);
                paragraphCount++;
            }

            // Process content blocks
            if (contentBlocks != null)
            {
                foreach (var block in contentBlocks)
                {
                    var blockType = GetBlockProperty(block, "type")?.ToLower() ?? "paragraph";

                    switch (blockType)
                    {
                        case "heading":
                        case "h1":
                        case "h2":
                        case "h3":
                            var headingText = GetBlockProperty(block, "text") ?? "";
                            var level = GetBlockLevel(block, blockType);
                            var fontSize = level switch
                            {
                                1 => 20f,
                                2 => 16f,
                                3 => 14f,
                                _ => 20f
                            };
                            var heading = new Paragraph(headingText)
                                .SetFont(boldFont)
                                .SetFontSize(fontSize)
                                .SetMarginTop(15)
                                .SetMarginBottom(10);
                            document.Add(heading);
                            paragraphCount++;
                            break;

                        case "paragraph":
                        case "text":
                        case "p":
                            var paraText = GetBlockProperty(block, "text") ?? "";
                            var isBold = GetBlockBool(block, "bold");
                            var isItalic = GetBlockBool(block, "italic");
                            var font = isBold ? boldFont : (isItalic ? italicFont : regularFont);
                            var para = new Paragraph(paraText)
                                .SetFont(font)
                                .SetFontSize(11)
                                .SetMarginBottom(10);
                            document.Add(para);
                            paragraphCount++;
                            break;

                        case "bullet":
                        case "list":
                            var items = GetBlockArray(block, "items");
                            if (items != null)
                            {
                                var list = new List()
                                    .SetSymbolIndent(12)
                                    .SetListSymbol("- ");

                                foreach (var item in items)
                                {
                                    var listItem = new ListItem();
                                    listItem.Add(new Paragraph(item)
                                        .SetFont(regularFont)
                                        .SetFontSize(11));
                                    list.Add(listItem);
                                }
                                document.Add(list);
                                paragraphCount += items.Length;
                            }
                            break;

                        case "table":
                            var headers = GetBlockArray(block, "headers");
                            var rows = GetBlockRows(block, "rows");

                            int columnCount = headers?.Length ??
                                              rows?.FirstOrDefault()?.Length ?? 0;

                            if (columnCount > 0)
                            {
                                var table = new Table(columnCount)
                                    .UseAllAvailableWidth()
                                    .SetMarginTop(10)
                                    .SetMarginBottom(10);

                                // Add headers
                                if (headers != null)
                                {
                                    foreach (var header in headers)
                                    {
                                        var headerCell = new Cell()
                                            .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(11))
                                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                                            .SetPadding(5);
                                        table.AddHeaderCell(headerCell);
                                    }
                                }

                                // Add data rows
                                if (rows != null)
                                {
                                    foreach (var row in rows)
                                    {
                                        foreach (var cellValue in row)
                                        {
                                            var cell = new Cell()
                                                .Add(new Paragraph(cellValue ?? "").SetFont(regularFont).SetFontSize(10))
                                                .SetPadding(5);
                                            table.AddCell(cell);
                                        }
                                    }
                                }

                                document.Add(table);
                                tableCount++;
                            }
                            break;

                        case "break":
                        case "pagebreak":
                            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                            break;

                        case "line":
                        case "hr":
                            var line = new Paragraph("")
                                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(1))
                                .SetMarginTop(10)
                                .SetMarginBottom(10);
                            document.Add(line);
                            break;

                        default:
                            var defaultText = GetBlockProperty(block, "text") ?? "";
                            if (!string.IsNullOrEmpty(defaultText))
                            {
                                document.Add(new Paragraph(defaultText)
                                    .SetFont(regularFont)
                                    .SetFontSize(11)
                                    .SetMarginBottom(10));
                                paragraphCount++;
                            }
                            break;
                    }
                }
            }

            var pageCount = pdf.GetNumberOfPages();
            document.Close();

            return $"PDF document created successfully at: {filePath}\n" +
                   $"Title: {title ?? "(none)"}\n" +
                   $"Page size: {pageSizeStr}\n" +
                   $"Pages: {pageCount}\n" +
                   $"Paragraphs: {paragraphCount}\n" +
                   $"Tables: {tableCount}";
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF creation failed: {ex.GetType().Name}: {ex.Message}\nInner: {ex.InnerException?.Message}\nStack: {ex.StackTrace}");
            }
        });
    }

    // Helper methods
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

    private static JsonElement[]? GetContentArray(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                return je.EnumerateArray().ToArray();
            }
        }
        return null;
    }

    private static string? GetBlockProperty(JsonElement block, string property)
    {
        if (block.TryGetProperty(property, out var value))
        {
            return value.GetString();
        }
        return null;
    }

    private static bool GetBlockBool(JsonElement block, string property)
    {
        if (block.TryGetProperty(property, out var value))
        {
            return value.ValueKind == JsonValueKind.True;
        }
        return false;
    }

    private static int GetBlockLevel(JsonElement block, string blockType)
    {
        if (block.TryGetProperty("level", out var level))
        {
            if (level.TryGetInt32(out var l)) return l;
        }
        return blockType switch
        {
            "h1" => 1,
            "h2" => 2,
            "h3" => 3,
            _ => 1
        };
    }

    private static string[]? GetBlockArray(JsonElement block, string property)
    {
        if (block.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToArray();
        }
        return null;
    }

    private static string[][]? GetBlockRows(JsonElement block, string property)
    {
        if (block.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(row => row.ValueKind == JsonValueKind.Array
                    ? row.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
                    : new[] { row.GetString() ?? "" })
                .ToArray();
        }
        return null;
    }
}
