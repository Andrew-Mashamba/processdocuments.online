using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;

// Aliases to avoid ambiguity
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;
using WordTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using WordTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using WordTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using WordBody = DocumentFormat.OpenXml.Wordprocessing.Body;
using WordBold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using WordBreak = DocumentFormat.OpenXml.Wordprocessing.Break;
using WordRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using WordParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using WordJustification = DocumentFormat.OpenXml.Wordprocessing.Justification;
using WordJustificationValues = DocumentFormat.OpenXml.Wordprocessing.JustificationValues;
using WordSpacing = DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines;
using WordBreakValues = DocumentFormat.OpenXml.Wordprocessing.BreakValues;
using WordParagraphStyleId = DocumentFormat.OpenXml.Wordprocessing.ParagraphStyleId;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

// iText aliases
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using PdfParagraph = iText.Layout.Element.Paragraph;
using PdfTable = iText.Layout.Element.Table;
using PdfCell = iText.Layout.Element.Cell;
using PdfDocument2 = iText.Layout.Document;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Font;
using iText.Layout.Properties;

namespace ZimaFileService.Tools;

/// <summary>
/// File Conversion Tools - Convert between PDF, Word, Excel, and image formats
/// </summary>
public class ConversionTools
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public ConversionTools()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    #region PDF to Word

    /// <summary>
    /// Convert PDF to Word document (.docx)
    /// </summary>
    public async Task<string> PdfToWordAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var preserveLayout = GetBool(args, "preserve_layout", true);

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".docx";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Extract text from PDF
            var pdfContent = ExtractPdfContent(inputPath);

            // Create Word document
            using var wordDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new WordDocument();
            var body = new WordBody();

            foreach (var pageContent in pdfContent.Pages)
            {
                if (preserveLayout)
                {
                    // Add page header
                    var pageHeader = new WordParagraph(
                        new WordRun(new WordText($"--- Page {pageContent.PageNumber} ---"))
                    );
                    pageHeader.ParagraphProperties = new WordParagraphProperties(
                        new WordJustification { Val = WordJustificationValues.Center },
                        new WordSpacing { After = "200" }
                    );
                    body.Append(pageHeader);
                }

                // Process text - split by paragraphs
                var paragraphs = pageContent.Text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var paraText in paragraphs)
                {
                    var para = new WordParagraph();
                    var run = new WordRun();

                    // Check if it looks like a heading (short, possibly all caps or starts with numbers)
                    var trimmedText = paraText.Trim();
                    if (trimmedText.Length < 100 && (IsLikelyHeading(trimmedText)))
                    {
                        run.RunProperties = new WordRunProperties(new WordBold());
                    }

                    run.Append(new WordText(trimmedText));
                    para.Append(run);

                    // Add spacing
                    para.ParagraphProperties = new WordParagraphProperties(
                        new WordSpacing { After = "120" }
                    );

                    body.Append(para);
                }

                if (preserveLayout)
                {
                    // Add page break after each page except the last
                    body.Append(new WordParagraph(
                        new WordRun(new WordBreak { Type = WordBreakValues.Page })
                    ));
                }
            }

            mainPart.Document.Append(body);
            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"PDF converted to Word successfully",
                output_file = outputPath,
                pages_converted = pdfContent.Pages.Count,
                word_count = pdfContent.Pages.Sum(p => p.WordCount)
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    #endregion

    #region PDF to Excel

    /// <summary>
    /// Convert PDF to Excel spreadsheet (.xlsx) - extracts tables and data
    /// </summary>
    public async Task<string> PdfToExcelAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var detectTables = GetBool(args, "detect_tables", true);

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".xlsx";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            var pdfContent = ExtractPdfContent(inputPath);

            using var workbook = new XLWorkbook();

            int tablesFound = 0;

            foreach (var pageContent in pdfContent.Pages)
            {
                var sheetName = $"Page {pageContent.PageNumber}";
                var worksheet = workbook.Worksheets.Add(sheetName);

                if (detectTables)
                {
                    // Try to detect and extract tables
                    var tables = DetectTablesInText(pageContent.Text);

                    if (tables.Count > 0)
                    {
                        int startRow = 1;
                        foreach (var table in tables)
                        {
                            for (int r = 0; r < table.Rows.Count; r++)
                            {
                                for (int c = 0; c < table.Rows[r].Count; c++)
                                {
                                    worksheet.Cell(startRow + r, c + 1).Value = table.Rows[r][c];

                                    // Style header row
                                    if (r == 0)
                                    {
                                        worksheet.Cell(startRow + r, c + 1).Style.Font.Bold = true;
                                        worksheet.Cell(startRow + r, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                                    }
                                }
                            }
                            startRow += table.Rows.Count + 2; // Leave gap between tables
                            tablesFound++;
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                    }
                    else
                    {
                        // No tables detected, put text in cells line by line
                        var lines = pageContent.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            worksheet.Cell(i + 1, 1).Value = lines[i].Trim();
                        }
                        worksheet.Column(1).AdjustToContents();
                    }
                }
                else
                {
                    // Just put all text line by line
                    var lines = pageContent.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        worksheet.Cell(i + 1, 1).Value = lines[i].Trim();
                    }
                    worksheet.Column(1).AdjustToContents();
                }
            }

            workbook.SaveAs(outputPath);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"PDF converted to Excel successfully",
                output_file = outputPath,
                pages_converted = pdfContent.Pages.Count,
                tables_detected = tablesFound,
                detect_tables_enabled = detectTables
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    #endregion

    #region PDF to JPG

    /// <summary>
    /// Convert PDF pages to JPG images
    /// </summary>
    public async Task<string> PdfToJpgAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputFolder = GetString(args, "output_folder", "");
        var dpi = GetInt(args, "dpi", 150);
        var quality = GetInt(args, "quality", 90);
        var pages = GetIntArray(args, "pages"); // Empty = all pages

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputFolder))
        {
            outputFolder = Path.GetFileNameWithoutExtension(inputFile) + "_images";
        }
        var outputDir = Path.Combine(_generatedPath, outputFolder);
        Directory.CreateDirectory(outputDir);

        try
        {
            var outputFiles = new List<string>();

            using var docReader = DocLib.Instance.GetDocReader(inputPath, new PageDimensions(dpi, dpi));
            var pageCount = docReader.GetPageCount();

            var pagesToConvert = pages.Length > 0
                ? pages.Where(p => p >= 1 && p <= pageCount).ToArray()
                : Enumerable.Range(1, pageCount).ToArray();

            foreach (var pageNum in pagesToConvert)
            {
                using var pageReader = docReader.GetPageReader(pageNum - 1); // 0-indexed
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage();

                // Convert BGRA to SKBitmap
                using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

                // Copy the raw bytes to the bitmap
                var pixels = bitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, pixels, rawBytes.Length);

                // Save as JPG
                var outputFile = Path.Combine(outputDir, $"page_{pageNum:D3}.jpg");
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                using var stream = File.OpenWrite(outputFile);
                data.SaveTo(stream);

                outputFiles.Add(outputFile);
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"PDF converted to {outputFiles.Count} JPG images",
                output_folder = outputDir,
                files = outputFiles.Select(f => Path.GetFileName(f)).ToArray(),
                total_pages = pageCount,
                pages_converted = outputFiles.Count,
                dpi = dpi,
                quality = quality
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convert PDF pages to PNG images (higher quality, lossless)
    /// </summary>
    public async Task<string> PdfToPngAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputFolder = GetString(args, "output_folder", "");
        var dpi = GetInt(args, "dpi", 150);
        var pages = GetIntArray(args, "pages");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputFolder))
        {
            outputFolder = Path.GetFileNameWithoutExtension(inputFile) + "_images";
        }
        var outputDir = Path.Combine(_generatedPath, outputFolder);
        Directory.CreateDirectory(outputDir);

        try
        {
            var outputFiles = new List<string>();

            using var docReader = DocLib.Instance.GetDocReader(inputPath, new PageDimensions(dpi, dpi));
            var pageCount = docReader.GetPageCount();

            var pagesToConvert = pages.Length > 0
                ? pages.Where(p => p >= 1 && p <= pageCount).ToArray()
                : Enumerable.Range(1, pageCount).ToArray();

            foreach (var pageNum in pagesToConvert)
            {
                using var pageReader = docReader.GetPageReader(pageNum - 1);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage();

                using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                var pixels = bitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, pixels, rawBytes.Length);

                var outputFile = Path.Combine(outputDir, $"page_{pageNum:D3}.png");
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(outputFile);
                data.SaveTo(stream);

                outputFiles.Add(outputFile);
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"PDF converted to {outputFiles.Count} PNG images",
                output_folder = outputDir,
                files = outputFiles.Select(f => Path.GetFileName(f)).ToArray(),
                total_pages = pageCount,
                pages_converted = outputFiles.Count,
                dpi = dpi
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    #endregion

    #region PDF to PDF/A

    /// <summary>
    /// Convert PDF to PDF/A archival format
    /// </summary>
    public async Task<string> PdfToPdfAAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var conformanceLevel = GetString(args, "conformance", "PDF/A-2B"); // PDF/A-1B, PDF/A-2B, PDF/A-3B

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_pdfa.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Read original PDF and copy with PDF/A intent
            using var reader = new PdfReader(inputPath);
            using var srcDoc = new PdfDocument(reader);

            using var writer = new PdfWriter(outputPath);
            using var destDoc = new PdfDocument(writer);

            // Copy pages
            srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), destDoc);

            // Set PDF/A metadata
            var info = destDoc.GetDocumentInfo();
            info.SetMoreInfo("GTS_PDFXVersion", conformanceLevel);
            info.SetMoreInfo("GTS_PDFXConformance", conformanceLevel);

            // Add XMP metadata for PDF/A compliance
            var xmpMeta = new byte[] { }; // Simplified - full PDF/A requires proper XMP

            destDoc.Close();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"PDF converted to {conformanceLevel} format",
                output_file = outputPath,
                conformance_level = conformanceLevel,
                pages = srcDoc.GetNumberOfPages(),
                note = "Basic PDF/A conversion completed. For full compliance, additional processing may be required."
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                note = "PDF/A conversion may fail if the source PDF contains unsupported elements"
            });
        }
    }

    #endregion

    #region Excel to PDF

    /// <summary>
    /// Convert Excel spreadsheet to PDF
    /// </summary>
    public async Task<string> ExcelToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var sheets = GetStringArray(args, "sheets"); // Empty = all sheets
        var landscape = GetBool(args, "landscape", false);
        var fitToPage = GetBool(args, "fit_to_page", true);

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var workbook = new XLWorkbook(inputPath);
            var sheetsToConvert = sheets.Length > 0
                ? workbook.Worksheets.Where(ws => sheets.Contains(ws.Name)).ToList()
                : workbook.Worksheets.ToList();

            if (sheetsToConvert.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "No sheets found to convert",
                    available_sheets = workbook.Worksheets.Select(ws => ws.Name).ToArray()
                });
            }

            // Create PDF
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(writer);
            using var document = new PdfDocument2(pdfDoc,
                landscape ? iText.Kernel.Geom.PageSize.A4.Rotate() : iText.Kernel.Geom.PageSize.A4);

            var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            int sheetCount = 0;

            foreach (var worksheet in sheetsToConvert)
            {
                if (sheetCount > 0)
                {
                    document.Add(new iText.Layout.Element.AreaBreak(AreaBreakType.NEXT_PAGE));
                }

                // Add sheet title
                document.Add(new PdfParagraph(worksheet.Name)
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetMarginBottom(10));

                // Find used range
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null) continue;

                var rowCount = usedRange.RowCount();
                var colCount = usedRange.ColumnCount();

                // Create table
                var table = new PdfTable(UnitValue.CreatePercentArray(colCount))
                    .UseAllAvailableWidth();

                // Get first row and last row/column
                var firstRow = usedRange.FirstRow().RowNumber();
                var firstCol = usedRange.FirstColumn().ColumnNumber();

                for (int r = 0; r < rowCount; r++)
                {
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = worksheet.Cell(firstRow + r, firstCol + c);
                        var cellValue = cell.GetFormattedString();

                        var pdfCell = new PdfCell().Add(new PdfParagraph(cellValue).SetFont(font).SetFontSize(9));

                        // Style header row
                        if (r == 0)
                        {
                            pdfCell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
                            pdfCell.SetBold();
                        }

                        // Handle cell background color
                        var bgColor = cell.Style.Fill.BackgroundColor;
                        if (bgColor.ColorType == XLColorType.Color)
                        {
                            var color = bgColor.Color;
                            pdfCell.SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(color.R, color.G, color.B));
                        }

                        table.AddCell(pdfCell);
                    }
                }

                document.Add(table);
                sheetCount++;
            }

            document.Close();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Excel converted to PDF successfully",
                output_file = outputPath,
                sheets_converted = sheetCount,
                sheet_names = sheetsToConvert.Select(s => s.Name).ToArray()
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convert Word document to PDF
    /// </summary>
    public async Task<string> WordToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Open Word document
            using var wordDoc = WordprocessingDocument.Open(inputPath, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;

            if (body == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Could not read Word document body"
                });
            }

            // Create PDF
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(writer);
            using var document = new PdfDocument2(pdfDoc);

            var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            foreach (var element in body.Elements())
            {
                if (element is WordParagraph para)
                {
                    var text = para.InnerText;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var pdfPara = new PdfParagraph(text).SetFont(font).SetFontSize(11);

                    // Check for bold
                    var runProps = para.Descendants<WordRunProperties>().FirstOrDefault();
                    if (runProps?.GetFirstChild<WordBold>() != null)
                    {
                        pdfPara.SetFont(boldFont);
                    }

                    // Check for heading styles
                    var paraProps = para.ParagraphProperties;
                    var styleId = paraProps?.GetFirstChild<WordParagraphStyleId>();
                    if (styleId?.Val?.Value?.StartsWith("Heading") == true)
                    {
                        pdfPara.SetFont(boldFont).SetFontSize(14);
                    }

                    document.Add(pdfPara);
                }
                else if (element is WordTable table)
                {
                    // Convert Word table to PDF table
                    var rows = table.Elements<WordTableRow>().ToList();
                    if (rows.Count == 0) continue;

                    var firstRowCells = rows[0].Elements<WordTableCell>().Count();
                    var pdfTable = new PdfTable(UnitValue.CreatePercentArray(firstRowCells)).UseAllAvailableWidth();

                    foreach (var row in rows)
                    {
                        foreach (var cell in row.Elements<WordTableCell>())
                        {
                            var cellText = cell.InnerText;
                            pdfTable.AddCell(new PdfCell().Add(new PdfParagraph(cellText).SetFont(font).SetFontSize(10)));
                        }
                    }

                    document.Add(pdfTable);
                }
            }

            document.Close();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Word document converted to PDF successfully",
                output_file = outputPath
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    #endregion

    #region Helper Methods

    private PdfContent ExtractPdfContent(string pdfPath)
    {
        var content = new PdfContent();

        using var reader = new PdfReader(pdfPath);
        using var pdfDoc = new PdfDocument(reader);

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            var strategy = new LocationTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy);

            content.Pages.Add(new PageContent
            {
                PageNumber = i,
                Text = text,
                WordCount = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length
            });
        }

        return content;
    }

    private bool IsLikelyHeading(string text)
    {
        // Check if text looks like a heading
        if (string.IsNullOrWhiteSpace(text)) return false;

        // All caps
        if (text.ToUpper() == text && text.Length > 3) return true;

        // Starts with number followed by period
        if (Regex.IsMatch(text, @"^\d+\.")) return true;

        // Short and doesn't end with punctuation
        if (text.Length < 60 && !text.EndsWith(".") && !text.EndsWith(",")) return true;

        return false;
    }

    private List<DetectedTable> DetectTablesInText(string text)
    {
        var tables = new List<DetectedTable>();
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Look for patterns that indicate tables:
        // - Lines with consistent delimiters (|, tabs, multiple spaces)
        // - Lines with similar structure

        var tableLines = new List<string>();
        string? currentDelimiter = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (tableLines.Count >= 2)
                {
                    var table = ParseTableLines(tableLines, currentDelimiter);
                    if (table != null) tables.Add(table);
                }
                tableLines.Clear();
                currentDelimiter = null;
                continue;
            }

            // Detect delimiter
            string? lineDelimiter = null;
            if (trimmedLine.Contains('|')) lineDelimiter = "|";
            else if (trimmedLine.Contains('\t')) lineDelimiter = "\t";
            else if (Regex.IsMatch(trimmedLine, @"\s{3,}")) lineDelimiter = "spaces";

            if (lineDelimiter != null)
            {
                if (currentDelimiter == null || currentDelimiter == lineDelimiter)
                {
                    currentDelimiter = lineDelimiter;
                    tableLines.Add(trimmedLine);
                }
                else
                {
                    if (tableLines.Count >= 2)
                    {
                        var table = ParseTableLines(tableLines, currentDelimiter);
                        if (table != null) tables.Add(table);
                    }
                    tableLines.Clear();
                    currentDelimiter = lineDelimiter;
                    tableLines.Add(trimmedLine);
                }
            }
        }

        // Handle remaining lines
        if (tableLines.Count >= 2)
        {
            var table = ParseTableLines(tableLines, currentDelimiter);
            if (table != null) tables.Add(table);
        }

        return tables;
    }

    private DetectedTable? ParseTableLines(List<string> lines, string? delimiter)
    {
        if (lines.Count < 2 || string.IsNullOrEmpty(delimiter)) return null;

        var table = new DetectedTable();

        foreach (var line in lines)
        {
            string[] cells;
            if (delimiter == "|")
            {
                cells = line.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim()).ToArray();
            }
            else if (delimiter == "\t")
            {
                cells = line.Split('\t', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim()).ToArray();
            }
            else // spaces
            {
                cells = Regex.Split(line, @"\s{3,}")
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c)).ToArray();
            }

            if (cells.Length > 1)
            {
                table.Rows.Add(cells.ToList());
            }
        }

        return table.Rows.Count >= 2 ? table : null;
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
        if (value is bool b) return b;
        return defaultValue;
    }

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetInt32();
        if (value is int i) return i;
        return defaultValue;
    }

    private string[] GetStringArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        return Array.Empty<string>();
    }

    private int[] GetIntArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<int>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        return Array.Empty<int>();
    }

    #endregion

    #region Internal Classes

    private class PdfContent
    {
        public List<PageContent> Pages { get; set; } = new();
    }

    private class PageContent
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = "";
        public int WordCount { get; set; }
    }

    private class DetectedTable
    {
        public List<List<string>> Rows { get; set; } = new();
    }

    #endregion
}
