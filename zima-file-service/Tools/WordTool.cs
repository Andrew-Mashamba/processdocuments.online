using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ZimaFileService.Tools;

public class WordTool : IFileTool
{
    public string Name => "word";
    public string Description => "Create Word documents";

    public async Task<string> CreateWordAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        var title = GetString(arguments, "title");
        var contentBlocks = GetContentArray(arguments, "content");

        return await Task.Run(() =>
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            int paragraphCount = 0;
            int tableCount = 0;

            // Add title if provided
            if (!string.IsNullOrEmpty(title))
            {
                var titleParagraph = CreateHeadingParagraph(title, 1);
                body.AppendChild(titleParagraph);
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
                            body.AppendChild(CreateHeadingParagraph(headingText, level));
                            paragraphCount++;
                            break;

                        case "paragraph":
                        case "text":
                        case "p":
                            var paraText = GetBlockProperty(block, "text") ?? "";
                            var isBold = GetBlockBool(block, "bold");
                            var isItalic = GetBlockBool(block, "italic");
                            body.AppendChild(CreateParagraph(paraText, isBold, isItalic));
                            paragraphCount++;
                            break;

                        case "bullet":
                        case "list":
                            var items = GetBlockArray(block, "items");
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    body.AppendChild(CreateBulletParagraph(item));
                                    paragraphCount++;
                                }
                            }
                            break;

                        case "table":
                            var headers = GetBlockArray(block, "headers");
                            var rows = GetBlockRows(block, "rows");
                            if (rows != null || headers != null)
                            {
                                body.AppendChild(CreateTable(headers, rows));
                                tableCount++;
                            }
                            break;

                        case "break":
                        case "pagebreak":
                            body.AppendChild(CreatePageBreak());
                            break;

                        default:
                            // Treat unknown types as paragraphs
                            var defaultText = GetBlockProperty(block, "text") ?? block.ToString() ?? "";
                            body.AppendChild(CreateParagraph(defaultText, false, false));
                            paragraphCount++;
                            break;
                    }
                }
            }

            mainPart.Document.Save();

            return $"Word document created successfully at: {filePath}\n" +
                   $"Title: {title ?? "(none)"}\n" +
                   $"Paragraphs: {paragraphCount}\n" +
                   $"Tables: {tableCount}";
        });
    }

    private static Paragraph CreateHeadingParagraph(string text, int level)
    {
        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        // Set heading style
        var styleId = level switch
        {
            1 => "Heading1",
            2 => "Heading2",
            3 => "Heading3",
            _ => "Heading1"
        };

        paragraphProperties.AppendChild(new ParagraphStyleId { Val = styleId });

        // Add spacing
        paragraphProperties.AppendChild(new SpacingBetweenLines
        {
            Before = "240",
            After = "120"
        });

        paragraph.AppendChild(paragraphProperties);

        var run = new Run();
        var runProperties = new RunProperties();

        // Make headings bold and set size
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize
        {
            Val = level switch
            {
                1 => "48", // 24pt
                2 => "36", // 18pt
                3 => "28", // 14pt
                _ => "48"
            }
        });

        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);

        return paragraph;
    }

    private static Paragraph CreateParagraph(string text, bool bold, bool italic)
    {
        var paragraph = new Paragraph();
        var run = new Run();

        if (bold || italic)
        {
            var runProperties = new RunProperties();
            if (bold) runProperties.AppendChild(new Bold());
            if (italic) runProperties.AppendChild(new Italic());
            run.AppendChild(runProperties);
        }

        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);

        return paragraph;
    }

    private static Paragraph CreateBulletParagraph(string text)
    {
        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        // Add numbering for bullet
        paragraphProperties.AppendChild(new NumberingProperties(
            new NumberingLevelReference { Val = 0 },
            new NumberingId { Val = 1 }
        ));

        // Add indentation
        paragraphProperties.AppendChild(new Indentation
        {
            Left = "720",
            Hanging = "360"
        });

        paragraph.AppendChild(paragraphProperties);

        var run = new Run();
        run.AppendChild(new Text("• " + text));
        paragraph.AppendChild(run);

        return paragraph;
    }

    private static Table CreateTable(string[]? headers, string[][]? rows)
    {
        var table = new Table();

        // Table properties
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tableProperties);

        // Add header row if provided
        if (headers != null && headers.Length > 0)
        {
            var headerRow = new TableRow();
            foreach (var header in headers)
            {
                var cell = new TableCell();
                var cellProperties = new TableCellProperties(
                    new Shading { Fill = "CCCCCC" }
                );
                cell.AppendChild(cellProperties);

                var paragraph = new Paragraph();
                var run = new Run();
                run.AppendChild(new RunProperties(new Bold()));
                run.AppendChild(new Text(header));
                paragraph.AppendChild(run);
                cell.AppendChild(paragraph);

                headerRow.AppendChild(cell);
            }
            table.AppendChild(headerRow);
        }

        // Add data rows
        if (rows != null)
        {
            foreach (var rowData in rows)
            {
                var dataRow = new TableRow();
                foreach (var cellValue in rowData)
                {
                    var cell = new TableCell(
                        new Paragraph(new Run(new Text(cellValue ?? "")))
                    );
                    dataRow.AppendChild(cell);
                }
                table.AppendChild(dataRow);
            }
        }

        return table;
    }

    private static Paragraph CreatePageBreak()
    {
        var paragraph = new Paragraph();
        var run = new Run(new Break { Type = BreakValues.Page });
        paragraph.AppendChild(run);
        return paragraph;
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
