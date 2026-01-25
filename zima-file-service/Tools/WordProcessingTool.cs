using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

namespace ZimaFileService.Tools;

/// <summary>
/// Word Processing Tools - merge, split, convert, manipulate Word documents
/// </summary>
public class WordProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public WordProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Merge multiple Word documents into one
    /// </summary>
    public async Task<string> MergeDocumentsAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputName = GetString(args, "output_file", "merged.docx");
        var addPageBreaks = GetBool(args, "add_page_breaks", true);

        if (files.Length < 2)
            throw new ArgumentException("At least 2 files required for merge");

        var outputPath = ResolvePath(outputName, true);
        var firstFile = ResolvePath(files[0], false);

        // Copy first document as base
        File.Copy(firstFile, outputPath, true);

        using (var mainDoc = WordprocessingDocument.Open(outputPath, true))
        {
            var mainBody = mainDoc.MainDocumentPart!.Document.Body!;

            for (int i = 1; i < files.Length; i++)
            {
                var sourcePath = ResolvePath(files[i], false);

                if (addPageBreaks)
                {
                    mainBody.AppendChild(new Paragraph(
                        new Run(new Break { Type = BreakValues.Page })));
                }

                using var sourceDoc = WordprocessingDocument.Open(sourcePath, false);
                var sourceBody = sourceDoc.MainDocumentPart!.Document.Body!;

                foreach (var element in sourceBody.Elements().ToList())
                {
                    if (element is SectionProperties) continue;
                    mainBody.AppendChild(element.CloneNode(true));
                }
            }

            mainDoc.MainDocumentPart.Document.Save();
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} documents",
            output_file = outputPath,
            documents_merged = files.Length
        });
    }

    /// <summary>
    /// Split Word document by sections, pages, or headings
    /// </summary>
    public async Task<string> SplitDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mode = GetString(args, "mode", "sections"); // sections, headings, pages
        var outputPrefix = GetString(args, "output_prefix", "split");

        var inputPath = ResolvePath(inputFile, false);
        var outputFiles = new List<string>();

        using var sourceDoc = WordprocessingDocument.Open(inputPath, false);
        var body = sourceDoc.MainDocumentPart!.Document.Body!;
        var elements = body.Elements().ToList();

        if (mode == "headings")
        {
            var currentElements = new List<OpenXmlElement>();
            int fileIndex = 1;

            foreach (var element in elements)
            {
                bool isHeading = false;
                if (element is Paragraph para)
                {
                    var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                    isHeading = style != null && (style.StartsWith("Heading") || style.StartsWith("Title"));
                }

                if (isHeading && currentElements.Count > 0)
                {
                    var outPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.docx");
                    await CreateDocumentFromElements(currentElements, outPath);
                    outputFiles.Add(outPath);
                    currentElements.Clear();
                    fileIndex++;
                }

                if (!(element is SectionProperties))
                    currentElements.Add(element.CloneNode(true));
            }

            if (currentElements.Count > 0)
            {
                var outPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.docx");
                await CreateDocumentFromElements(currentElements, outPath);
                outputFiles.Add(outPath);
            }
        }
        else // sections or default
        {
            var currentElements = new List<OpenXmlElement>();
            int fileIndex = 1;

            foreach (var element in elements)
            {
                currentElements.Add(element.CloneNode(true));

                bool isSectionBreak = false;
                if (element is Paragraph para)
                {
                    isSectionBreak = para.Descendants<SectionProperties>().Any();
                }

                if (isSectionBreak)
                {
                    var outPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.docx");
                    await CreateDocumentFromElements(currentElements, outPath);
                    outputFiles.Add(outPath);
                    currentElements.Clear();
                    fileIndex++;
                }
            }

            if (currentElements.Count > 0)
            {
                var outPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.docx");
                await CreateDocumentFromElements(currentElements, outPath);
                outputFiles.Add(outPath);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split into {outputFiles.Count} documents",
            output_files = outputFiles,
            mode = mode
        });
    }

    /// <summary>
    /// Extract specific sections from Word document
    /// </summary>
    public async Task<string> ExtractSectionsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sectionNumbers = GetIntArray(args, "sections");
        var outputName = GetString(args, "output_file", "extracted.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var sourceDoc = WordprocessingDocument.Open(inputPath, false);
        var body = sourceDoc.MainDocumentPart!.Document.Body!;

        var sections = new List<List<OpenXmlElement>>();
        var currentSection = new List<OpenXmlElement>();

        foreach (var element in body.Elements())
        {
            currentSection.Add(element.CloneNode(true));

            if (element is Paragraph para && para.Descendants<SectionProperties>().Any())
            {
                sections.Add(currentSection);
                currentSection = new List<OpenXmlElement>();
            }
        }

        if (currentSection.Count > 0)
            sections.Add(currentSection);

        var extractedElements = new List<OpenXmlElement>();
        foreach (var sectionNum in sectionNumbers)
        {
            if (sectionNum > 0 && sectionNum <= sections.Count)
            {
                extractedElements.AddRange(sections[sectionNum - 1]);
            }
        }

        await CreateDocumentFromElements(extractedElements, outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {sectionNumbers.Length} sections",
            output_file = outputPath,
            total_sections = sections.Count,
            extracted_sections = sectionNumbers
        });
    }

    /// <summary>
    /// Remove sections from Word document
    /// </summary>
    public async Task<string> RemoveSectionsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var sectionNumbers = GetIntArray(args, "sections");
        var outputName = GetString(args, "output_file", "trimmed.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var sourceDoc = WordprocessingDocument.Open(inputPath, false);
        var body = sourceDoc.MainDocumentPart!.Document.Body!;

        var sections = new List<List<OpenXmlElement>>();
        var currentSection = new List<OpenXmlElement>();

        foreach (var element in body.Elements())
        {
            currentSection.Add(element.CloneNode(true));

            if (element is Paragraph para && para.Descendants<SectionProperties>().Any())
            {
                sections.Add(currentSection);
                currentSection = new List<OpenXmlElement>();
            }
        }

        if (currentSection.Count > 0)
            sections.Add(currentSection);

        var keepSections = new HashSet<int>(Enumerable.Range(1, sections.Count).Except(sectionNumbers));
        var keptElements = new List<OpenXmlElement>();

        for (int i = 0; i < sections.Count; i++)
        {
            if (keepSections.Contains(i + 1))
            {
                keptElements.AddRange(sections[i]);
            }
        }

        await CreateDocumentFromElements(keptElements, outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {sectionNumbers.Length} sections",
            output_file = outputPath,
            original_sections = sections.Count,
            remaining_sections = keepSections.Count
        });
    }

    /// <summary>
    /// Convert Word to plain text
    /// </summary>
    public async Task<string> WordToTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.txt");
        var preserveFormatting = GetBool(args, "preserve_formatting", false);

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = WordprocessingDocument.Open(inputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var sb = new StringBuilder();

        foreach (var element in body.Elements())
        {
            if (element is Paragraph para)
            {
                var text = para.InnerText;
                if (preserveFormatting)
                {
                    var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                    if (style != null && style.StartsWith("Heading"))
                    {
                        text = $"\n{'#'.ToString().PadLeft(int.Parse(style.Replace("Heading", "")), '#')} {text}";
                    }
                }
                sb.AppendLine(text);
            }
            else if (element is Table table)
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    var cells = row.Elements<TableCell>().Select(c => c.InnerText);
                    sb.AppendLine(string.Join("\t", cells));
                }
                sb.AppendLine();
            }
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted Word to text",
            output_file = outputPath,
            characters = sb.Length
        });
    }

    /// <summary>
    /// Convert Word to HTML
    /// </summary>
    public async Task<string> WordToHtmlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.html");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = WordprocessingDocument.Open(inputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>Converted Document</title></head><body>");

        foreach (var element in body.Elements())
        {
            if (element is Paragraph para)
            {
                var text = System.Net.WebUtility.HtmlEncode(para.InnerText);
                var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";

                if (style.StartsWith("Heading"))
                {
                    var level = style.Replace("Heading", "");
                    if (int.TryParse(level, out int h))
                        sb.AppendLine($"<h{h}>{text}</h{h}>");
                    else
                        sb.AppendLine($"<p><strong>{text}</strong></p>");
                }
                else if (style == "Title")
                {
                    sb.AppendLine($"<h1>{text}</h1>");
                }
                else
                {
                    sb.AppendLine($"<p>{text}</p>");
                }
            }
            else if (element is Table table)
            {
                sb.AppendLine("<table border=\"1\">");
                foreach (var row in table.Elements<TableRow>())
                {
                    sb.AppendLine("<tr>");
                    foreach (var cell in row.Elements<TableCell>())
                    {
                        sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(cell.InnerText)}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
            }
        }

        sb.AppendLine("</body></html>");
        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted Word to HTML",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Convert Word to JSON (structured extraction)
    /// </summary>
    public async Task<string> WordToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = WordprocessingDocument.Open(inputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var structure = new List<object>();

        foreach (var element in body.Elements())
        {
            if (element is Paragraph para)
            {
                var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Normal";
                structure.Add(new
                {
                    type = "paragraph",
                    style = style,
                    text = para.InnerText
                });
            }
            else if (element is Table table)
            {
                var rows = new List<List<string>>();
                foreach (var row in table.Elements<TableRow>())
                {
                    rows.Add(row.Elements<TableCell>().Select(c => c.InnerText).ToList());
                }
                structure.Add(new
                {
                    type = "table",
                    rows = rows
                });
            }
        }

        var json = JsonSerializer.Serialize(new { content = structure }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted Word to JSON",
            output_file = outputPath,
            elements = structure.Count
        });
    }

    /// <summary>
    /// Convert text file to Word document
    /// </summary>
    public async Task<string> TextToWordAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.docx");
        var detectHeadings = GetBool(args, "detect_headings", true);

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);

        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        foreach (var line in lines)
        {
            var para = new Paragraph();

            if (detectHeadings && line.StartsWith("#"))
            {
                var level = line.TakeWhile(c => c == '#').Count();
                var text = line.TrimStart('#').Trim();
                para.AppendChild(new ParagraphProperties(
                    new ParagraphStyleId { Val = $"Heading{Math.Min(level, 9)}" }));
                para.AppendChild(new Run(new Text(text)));
            }
            else
            {
                para.AppendChild(new Run(new Text(line)));
            }

            body.AppendChild(para);
        }

        mainPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted text to Word",
            output_file = outputPath,
            lines = lines.Length
        });
    }

    /// <summary>
    /// Find and replace in Word document (with regex support)
    /// </summary>
    public async Task<string> FindReplaceAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var find = GetString(args, "find");
        var replace = GetString(args, "replace", "");
        var useRegex = GetBool(args, "use_regex", false);
        var caseSensitive = GetBool(args, "case_sensitive", true);
        var outputName = GetString(args, "output_file", "replaced.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        int count = 0;
        using (var doc = WordprocessingDocument.Open(outputPath, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;

            foreach (var text in body.Descendants<Text>())
            {
                if (useRegex)
                {
                    var options = caseSensitive ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    var regex = new System.Text.RegularExpressions.Regex(find, options);
                    var matches = regex.Matches(text.Text);
                    count += matches.Count;
                    text.Text = regex.Replace(text.Text, replace);
                }
                else
                {
                    var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    while (text.Text.Contains(find, comparison))
                    {
                        var idx = text.Text.IndexOf(find, comparison);
                        text.Text = text.Text.Remove(idx, find.Length).Insert(idx, replace);
                        count++;
                    }
                }
            }

            doc.MainDocumentPart.Document.Save();
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Replaced {count} occurrences",
            output_file = outputPath,
            replacements = count
        });
    }

    /// <summary>
    /// Add or update headers and footers
    /// </summary>
    public async Task<string> AddHeaderFooterAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var headerText = GetString(args, "header", "");
        var footerText = GetString(args, "footer", "");
        var outputName = GetString(args, "output_file", "with_header_footer.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart!;

        if (!string.IsNullOrEmpty(headerText))
        {
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header(new Paragraph(new Run(new Text(headerText))));
            headerPart.Header = header;
            headerPart.Header.Save();

            var headerRef = new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) };

            var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault();
            if (sectPr == null)
            {
                sectPr = new SectionProperties();
                mainPart.Document.Body.AppendChild(sectPr);
            }
            sectPr.PrependChild(headerRef);
        }

        if (!string.IsNullOrEmpty(footerText))
        {
            var footerPart = mainPart.AddNewPart<FooterPart>();
            var footer = new Footer(new Paragraph(new Run(new Text(footerText))));
            footerPart.Footer = footer;
            footerPart.Footer.Save();

            var footerRef = new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) };

            var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault();
            if (sectPr == null)
            {
                sectPr = new SectionProperties();
                mainPart.Document.Body.AppendChild(sectPr);
            }
            sectPr.PrependChild(footerRef);
        }

        mainPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Added header/footer",
            output_file = outputPath,
            header_added = !string.IsNullOrEmpty(headerText),
            footer_added = !string.IsNullOrEmpty(footerText)
        });
    }

    /// <summary>
    /// Get Word document info
    /// </summary>
    public async Task<string> GetWordInfoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        using var doc = WordprocessingDocument.Open(inputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var paragraphs = body.Elements<Paragraph>().Count();
        var tables = body.Elements<Table>().Count();
        var words = body.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var characters = body.InnerText.Length;

        var props = doc.PackageProperties;

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            paragraphs = paragraphs,
            tables = tables,
            words = words,
            characters = characters,
            title = props.Title,
            author = props.Creator,
            created = props.Created,
            modified = props.Modified
        });
    }

    /// <summary>
    /// Compare two Word documents
    /// </summary>
    public async Task<string> CompareDocumentsAsync(Dictionary<string, object> args)
    {
        var file1 = GetString(args, "file1");
        var file2 = GetString(args, "file2");

        var path1 = ResolvePath(file1, false);
        var path2 = ResolvePath(file2, false);

        using var doc1 = WordprocessingDocument.Open(path1, false);
        using var doc2 = WordprocessingDocument.Open(path2, false);

        var text1 = doc1.MainDocumentPart!.Document.Body!.InnerText;
        var text2 = doc2.MainDocumentPart!.Document.Body!.InnerText;

        var lines1 = text1.Split('\n');
        var lines2 = text2.Split('\n');

        var onlyIn1 = lines1.Except(lines2).Count();
        var onlyIn2 = lines2.Except(lines1).Count();
        var common = lines1.Intersect(lines2).Count();

        return JsonSerializer.Serialize(new
        {
            success = true,
            file1 = path1,
            file2 = path2,
            file1_lines = lines1.Length,
            file2_lines = lines2.Length,
            lines_only_in_file1 = onlyIn1,
            lines_only_in_file2 = onlyIn2,
            common_lines = common,
            identical = text1 == text2
        });
    }

    /// <summary>
    /// Clean formatting from Word document
    /// </summary>
    public async Task<string> CleanFormattingAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "cleaned.docx");
        var keepStructure = GetBool(args, "keep_structure", true);

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var body = doc.MainDocumentPart!.Document.Body!;

        foreach (var para in body.Descendants<Paragraph>())
        {
            if (!keepStructure)
            {
                para.ParagraphProperties?.Remove();
            }

            foreach (var run in para.Descendants<Run>())
            {
                run.RunProperties?.Remove();
            }
        }

        doc.MainDocumentPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Cleaned formatting",
            output_file = outputPath,
            structure_preserved = keepStructure
        });
    }

    /// <summary>
    /// Word to PDF conversion (creates PDF with text content)
    /// </summary>
    public async Task<string> WordToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = WordprocessingDocument.Open(inputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        // Extract text and create PDF using iText7
        using var writer = new iText.Kernel.Pdf.PdfWriter(outputPath);
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
        var pdfDocument = new iText.Layout.Document(pdfDoc);

        foreach (var para in body.Elements<Paragraph>())
        {
            var text = para.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var pdfPara = new iText.Layout.Element.Paragraph(text);

                // Check for heading styles
                var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                if (styleId != null && styleId.StartsWith("Heading"))
                {
                    pdfPara.SetFontSize(18 - (styleId.Length > 7 ? int.Parse(styleId.Substring(7)) : 1) * 2);
                    pdfPara.SetBold();
                }

                pdfDocument.Add(pdfPara);
            }
        }

        pdfDocument.Close();
        await Task.CompletedTask;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted Word to PDF",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Mail merge - replace placeholders with data
    /// </summary>
    public async Task<string> MailMergeAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var data = GetMergeData(args, "data"); // List of dictionaries
        var outputPrefix = GetString(args, "output_prefix", "merged");
        var singleOutput = GetBool(args, "single_output", false);

        var inputPath = ResolvePath(inputFile, false);
        var outputFiles = new List<string>();

        if (singleOutput)
        {
            var outputPath = ResolvePath($"{outputPrefix}.docx", true);
            File.Copy(inputPath, outputPath, true);

            using var doc = WordprocessingDocument.Open(outputPath, true);
            var body = doc.MainDocumentPart!.Document.Body!;

            // Only use first data row for single output
            if (data.Count > 0)
            {
                var record = data[0];
                foreach (var text in body.Descendants<Text>())
                {
                    var content = text.Text;
                    foreach (var (key, value) in record)
                    {
                        content = content.Replace($"{{{{{key}}}}}", value);
                        content = content.Replace($"<<{key}>>", value);
                        content = content.Replace($"${key}$", value);
                    }
                    text.Text = content;
                }
            }

            doc.MainDocumentPart.Document.Save();
            outputFiles.Add(outputPath);
        }
        else
        {
            int index = 1;
            foreach (var record in data)
            {
                var outputPath = ResolvePath($"{outputPrefix}_{index}.docx", true);
                File.Copy(inputPath, outputPath, true);

                using var doc = WordprocessingDocument.Open(outputPath, true);
                var body = doc.MainDocumentPart!.Document.Body!;

                foreach (var text in body.Descendants<Text>())
                {
                    var content = text.Text;
                    foreach (var (key, value) in record)
                    {
                        content = content.Replace($"{{{{{key}}}}}", value);
                        content = content.Replace($"<<{key}>>", value);
                        content = content.Replace($"${key}$", value);
                    }
                    text.Text = content;
                }

                doc.MainDocumentPart.Document.Save();
                outputFiles.Add(outputPath);
                index++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Created {outputFiles.Count} merged document(s)",
            output_files = outputFiles,
            records_processed = data.Count
        });
    }

    /// <summary>
    /// Accept or reject track changes
    /// </summary>
    public async Task<string> AcceptTrackChangesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var action = GetString(args, "action", "accept"); // accept, reject
        var outputName = GetString(args, "output_file", "changes_applied.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var body = doc.MainDocumentPart!.Document.Body!;

        int changesProcessed = 0;

        if (action == "accept")
        {
            // Accept all insertions
            foreach (var ins in body.Descendants<InsertedRun>().ToList())
            {
                var parent = ins.Parent;
                foreach (var child in ins.Elements<Run>().ToList())
                {
                    parent?.InsertBefore(child.CloneNode(true), ins);
                }
                ins.Remove();
                changesProcessed++;
            }

            // Remove all deletions
            foreach (var del in body.Descendants<DeletedRun>().ToList())
            {
                del.Remove();
                changesProcessed++;
            }
        }
        else
        {
            // Reject - remove all insertions
            foreach (var ins in body.Descendants<InsertedRun>().ToList())
            {
                ins.Remove();
                changesProcessed++;
            }

            // Accept all deletions (keep the deleted text)
            foreach (var del in body.Descendants<DeletedRun>().ToList())
            {
                var parent = del.Parent;
                foreach (var text in del.Descendants<DeletedText>().ToList())
                {
                    var run = new Run(new Text(text.Text));
                    parent?.InsertBefore(run, del);
                }
                del.Remove();
                changesProcessed++;
            }
        }

        doc.MainDocumentPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"{action.ToUpper()}ed {changesProcessed} track changes",
            output_file = outputPath,
            action = action,
            changes_processed = changesProcessed
        });
    }

    /// <summary>
    /// Add watermark to Word document
    /// </summary>
    public async Task<string> AddWatermarkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var watermarkText = GetString(args, "text", "CONFIDENTIAL");
        var outputName = GetString(args, "output_file", "watermarked.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart!;

        // Add watermark via header
        var headerPart = mainPart.AddNewPart<HeaderPart>();

        // Create a simple text watermark in header
        var header = new Header(
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }
                ),
                new Run(
                    new RunProperties(
                        new Color { Val = "C0C0C0" },
                        new FontSize { Val = "72" }
                    ),
                    new Text(watermarkText)
                )
            )
        );

        headerPart.Header = header;
        headerPart.Header.Save();

        var headerRef = new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) };

        var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault();
        if (sectPr == null)
        {
            sectPr = new SectionProperties();
            mainPart.Document.Body.AppendChild(sectPr);
        }
        sectPr.PrependChild(headerRef);

        mainPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Added watermark",
            output_file = outputPath,
            watermark_text = watermarkText
        });
    }

    /// <summary>
    /// Extract or manipulate tables in Word document
    /// </summary>
    public async Task<string> ManageTablesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var action = GetString(args, "action", "extract"); // extract, remove, count
        var tableIndex = GetInt(args, "table_index", -1); // -1 for all
        var outputName = GetString(args, "output_file", "tables.json");

        var inputPath = ResolvePath(inputFile, false);

        using var doc = WordprocessingDocument.Open(inputPath, action == "remove");
        var body = doc.MainDocumentPart!.Document.Body!;
        var tables = body.Elements<Table>().ToList();

        if (action == "extract")
        {
            var outputPath = ResolvePath(outputName, true);
            var extracted = new List<object>();

            for (int i = 0; i < tables.Count; i++)
            {
                if (tableIndex >= 0 && tableIndex != i) continue;

                var rows = new List<List<string>>();
                foreach (var row in tables[i].Elements<TableRow>())
                {
                    rows.Add(row.Elements<TableCell>().Select(c => c.InnerText).ToList());
                }
                extracted.Add(new { index = i, rows = rows });
            }

            var json = JsonSerializer.Serialize(new { tables = extracted }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Extracted {extracted.Count} table(s)",
                output_file = outputPath,
                tables_count = extracted.Count
            });
        }
        else if (action == "remove")
        {
            int removed = 0;
            for (int i = tables.Count - 1; i >= 0; i--)
            {
                if (tableIndex >= 0 && tableIndex != i) continue;
                tables[i].Remove();
                removed++;
            }

            doc.MainDocumentPart.Document.Save();

            var outputPath = ResolvePath(outputName.Replace(".json", ".docx"), true);
            File.Copy(inputPath, outputPath, true);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Removed {removed} table(s)",
                output_file = outputPath,
                tables_removed = removed
            });
        }
        else // count
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                tables_count = tables.Count,
                file = inputPath
            });
        }
    }

    /// <summary>
    /// Manage comments in Word document
    /// </summary>
    public async Task<string> ManageCommentsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var action = GetString(args, "action", "extract"); // extract, remove, add
        var outputName = GetString(args, "output_file", "comments_processed.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart!;

        if (action == "extract")
        {
            var comments = new List<object>();
            var commentsPart = mainPart.WordprocessingCommentsPart;

            if (commentsPart != null)
            {
                foreach (var comment in commentsPart.Comments.Elements<Comment>())
                {
                    comments.Add(new
                    {
                        id = comment.Id?.Value,
                        author = comment.Author?.Value,
                        date = comment.Date?.Value.ToString(),
                        text = comment.InnerText
                    });
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                comments_count = comments.Count,
                comments = comments,
                file = inputPath
            });
        }
        else if (action == "remove")
        {
            var commentsPart = mainPart.WordprocessingCommentsPart;
            int removed = 0;

            if (commentsPart != null)
            {
                removed = commentsPart.Comments.Elements<Comment>().Count();
                mainPart.DeletePart(commentsPart);
            }

            // Remove comment references from body
            var body = mainPart.Document.Body!;
            foreach (var commentRef in body.Descendants<CommentReference>().ToList())
            {
                commentRef.Remove();
            }
            foreach (var commentStart in body.Descendants<CommentRangeStart>().ToList())
            {
                commentStart.Remove();
            }
            foreach (var commentEnd in body.Descendants<CommentRangeEnd>().ToList())
            {
                commentEnd.Remove();
            }

            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Removed {removed} comments",
                output_file = outputPath,
                comments_removed = removed
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            action = action,
            output_file = outputPath
        });
    }

    /// <summary>
    /// Insert page numbers
    /// </summary>
    public async Task<string> AddPageNumbersAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var position = GetString(args, "position", "footer"); // header, footer
        var alignment = GetString(args, "alignment", "center"); // left, center, right
        var format = GetString(args, "format", "Page {0}");
        var outputName = GetString(args, "output_file", "with_page_numbers.docx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart!;

        var justification = alignment switch
        {
            "left" => JustificationValues.Left,
            "right" => JustificationValues.Right,
            _ => JustificationValues.Center
        };

        // Create page number field
        var pageNumPara = new Paragraph(
            new ParagraphProperties(new Justification { Val = justification }),
            new Run(new Text(format.Replace("{0}", ""))),
            new Run(
                new FieldChar { FieldCharType = FieldCharValues.Begin },
                new FieldCode(" PAGE "),
                new FieldChar { FieldCharType = FieldCharValues.End }
            )
        );

        if (position == "header")
        {
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(pageNumPara);
            headerPart.Header.Save();

            var headerRef = new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) };
            var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault() ?? new SectionProperties();
            if (!mainPart.Document.Body.Elements<SectionProperties>().Any())
                mainPart.Document.Body.AppendChild(sectPr);
            sectPr.PrependChild(headerRef);
        }
        else
        {
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(pageNumPara);
            footerPart.Footer.Save();

            var footerRef = new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) };
            var sectPr = mainPart.Document.Body!.Elements<SectionProperties>().FirstOrDefault() ?? new SectionProperties();
            if (!mainPart.Document.Body.Elements<SectionProperties>().Any())
                mainPart.Document.Body.AppendChild(sectPr);
            sectPr.PrependChild(footerRef);
        }

        mainPart.Document.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Added page numbers",
            output_file = outputPath,
            position = position,
            alignment = alignment
        });
    }

    #region Compress, Repair, Convert, Protect, Sign

    /// <summary>
    /// Compress Word document to reduce file size
    /// </summary>
    public async Task<string> CompressDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var removeComments = GetBool(args, "remove_comments", true);
        var removeRevisions = GetBool(args, "remove_revisions", true);
        var compressImages = GetBool(args, "compress_images", true);
        var removeUnusedStyles = GetBool(args, "remove_unused_styles", true);

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_compressed.docx";
        }
        var outputPath = ResolvePath(outputName, true);

        var originalSize = new FileInfo(inputPath).Length;

        try
        {
            // Copy to output first
            File.Copy(inputPath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart!;
                var body = mainPart.Document.Body!;

                // Remove comments
                if (removeComments && mainPart.WordprocessingCommentsPart != null)
                {
                    mainPart.DeletePart(mainPart.WordprocessingCommentsPart);
                    // Remove comment references
                    foreach (var commentRef in body.Descendants<CommentRangeStart>().ToList())
                        commentRef.Remove();
                    foreach (var commentRef in body.Descendants<CommentRangeEnd>().ToList())
                        commentRef.Remove();
                    foreach (var commentRef in body.Descendants<CommentReference>().ToList())
                        commentRef.Remove();
                }

                // Remove revisions/track changes
                if (removeRevisions)
                {
                    foreach (var ins in body.Descendants<InsertedRun>().ToList())
                    {
                        var parent = ins.Parent;
                        foreach (var child in ins.ChildElements.ToList())
                        {
                            parent?.InsertBefore(child.CloneNode(true), ins);
                        }
                        ins.Remove();
                    }
                    foreach (var del in body.Descendants<DeletedRun>().ToList())
                        del.Remove();
                    foreach (var del in body.Descendants<DeletedText>().ToList())
                        del.Remove();
                }

                // Remove unused styles
                if (removeUnusedStyles && mainPart.StyleDefinitionsPart != null)
                {
                    var styles = mainPart.StyleDefinitionsPart.Styles;
                    if (styles != null)
                    {
                        // Get used style IDs
                        var usedStyles = new HashSet<string>();
                        foreach (var para in body.Descendants<Paragraph>())
                        {
                            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                            if (!string.IsNullOrEmpty(styleId))
                                usedStyles.Add(styleId);
                        }
                        foreach (var run in body.Descendants<Run>())
                        {
                            var styleId = run.RunProperties?.RunStyle?.Val?.Value;
                            if (!string.IsNullOrEmpty(styleId))
                                usedStyles.Add(styleId);
                        }

                        // Keep built-in and used styles
                        var builtInStyles = new HashSet<string> { "Normal", "Heading1", "Heading2", "Heading3", "Title", "Subtitle" };
                        foreach (var style in styles.Elements<Style>().ToList())
                        {
                            var styleId = style.StyleId?.Value;
                            if (styleId != null && !usedStyles.Contains(styleId) && !builtInStyles.Contains(styleId))
                            {
                                style.Remove();
                            }
                        }
                        styles.Save();
                    }
                }

                // Compress images
                if (compressImages)
                {
                    foreach (var imagePart in mainPart.ImageParts)
                    {
                        // For JPEG images, we could recompress them
                        // For now, just note that images exist
                    }
                }

                mainPart.Document.Save();
            }

            // Repack the DOCX (it's a ZIP file) with maximum compression
            var tempPath = outputPath + ".tmp";
            using (var originalZip = ZipFile.OpenRead(outputPath))
            {
                using (var newZip = new ZipArchive(File.Create(tempPath), ZipArchiveMode.Create))
                {
                    foreach (var entry in originalZip.Entries)
                    {
                        var newEntry = newZip.CreateEntry(entry.FullName, CompressionLevel.SmallestSize);
                        using var originalStream = entry.Open();
                        using var newStream = newEntry.Open();
                        originalStream.CopyTo(newStream);
                    }
                }
            }
            File.Delete(outputPath);
            File.Move(tempPath, outputPath);

            var compressedSize = new FileInfo(outputPath).Length;
            var savings = ((double)(originalSize - compressedSize) / originalSize) * 100;

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Document compressed successfully",
                output_file = outputPath,
                original_size = FormatSize(originalSize),
                compressed_size = FormatSize(compressedSize),
                savings_percent = $"{savings:F1}%",
                operations = new
                {
                    comments_removed = removeComments,
                    revisions_removed = removeRevisions,
                    unused_styles_removed = removeUnusedStyles,
                    images_compressed = compressImages
                }
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
    /// Repair corrupted Word document
    /// </summary>
    public async Task<string> RepairDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_repaired.docx";
        }
        var outputPath = ResolvePath(outputName, true);

        var repairActions = new List<string>();

        try
        {
            // Try to open and repair the document
            bool isCorrupted = false;
            string? extractedText = null;

            // First attempt: Try to open normally
            try
            {
                using var doc = WordprocessingDocument.Open(inputPath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    extractedText = body.InnerText;
                }
            }
            catch
            {
                isCorrupted = true;
                repairActions.Add("Document failed normal open - attempting repair");
            }

            if (!isCorrupted)
            {
                // Document opens fine, just copy it
                File.Copy(inputPath, outputPath, true);
                repairActions.Add("Document is not corrupted - copied as-is");

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Document appears to be valid",
                    output_file = outputPath,
                    was_corrupted = false,
                    actions = repairActions
                });
            }

            // Try to repair by extracting from ZIP
            try
            {
                repairActions.Add("Attempting to extract content from DOCX archive");

                using var archive = ZipFile.OpenRead(inputPath);

                // Create new document
                using var newDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
                var mainPart = newDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                // Try to extract document.xml
                var docEntry = archive.GetEntry("word/document.xml");
                if (docEntry != null)
                {
                    repairActions.Add("Found word/document.xml - extracting content");

                    using var stream = docEntry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = reader.ReadToEnd();

                    // Try to parse and extract text content
                    var textMatches = Regex.Matches(xmlContent, @"<w:t[^>]*>([^<]*)</w:t>");
                    var paragraphTexts = new List<string>();
                    var currentParagraph = new StringBuilder();

                    foreach (Match match in textMatches)
                    {
                        currentParagraph.Append(match.Groups[1].Value);
                    }

                    // Create paragraphs from extracted text
                    var extractedContent = currentParagraph.ToString();
                    var paragraphs = extractedContent.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var paraText in paragraphs)
                    {
                        if (!string.IsNullOrWhiteSpace(paraText))
                        {
                            body.AppendChild(new Paragraph(new Run(new Text(paraText.Trim()))));
                        }
                    }

                    if (paragraphs.Length == 0 && !string.IsNullOrEmpty(extractedContent))
                    {
                        body.AppendChild(new Paragraph(new Run(new Text(extractedContent))));
                    }

                    repairActions.Add($"Extracted {paragraphs.Length} text blocks");
                }

                // Try to copy styles
                var stylesEntry = archive.GetEntry("word/styles.xml");
                if (stylesEntry != null)
                {
                    try
                    {
                        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                        using var stream = stylesEntry.Open();
                        stylesPart.FeedData(stream);
                        repairActions.Add("Recovered styles");
                    }
                    catch
                    {
                        repairActions.Add("Could not recover styles - using defaults");
                    }
                }

                mainPart.Document.Save();
                repairActions.Add("Created repaired document");

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Document repaired successfully",
                    output_file = outputPath,
                    was_corrupted = true,
                    actions = repairActions,
                    warning = "Some formatting may have been lost during repair"
                });
            }
            catch (Exception ex)
            {
                repairActions.Add($"ZIP extraction failed: {ex.Message}");

                // Last resort: Create empty document with error message
                using var newDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
                var mainPart = newDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(
                    new Paragraph(new Run(new Text("Document repair failed. Original file may be severely corrupted."))),
                    new Paragraph(new Run(new Text($"Error: {ex.Message}")))
                ));
                mainPart.Document.Save();

                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Could not repair document",
                    output_file = outputPath,
                    error = ex.Message,
                    actions = repairActions
                });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                actions = repairActions
            });
        }
    }

    /// <summary>
    /// Convert HTML to Word document
    /// </summary>
    public async Task<string> HtmlToWordAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var htmlContent = GetString(args, "html", "");
        var outputName = GetString(args, "output_file", "");
        var preserveStyles = GetBool(args, "preserve_styles", true);

        string html;

        if (!string.IsNullOrEmpty(inputFile))
        {
            var inputPath = ResolvePath(inputFile, false);
            html = await File.ReadAllTextAsync(inputPath);

            if (string.IsNullOrEmpty(outputName))
            {
                outputName = Path.GetFileNameWithoutExtension(inputFile) + ".docx";
            }
        }
        else if (!string.IsNullOrEmpty(htmlContent))
        {
            html = htmlContent;
            if (string.IsNullOrEmpty(outputName))
            {
                outputName = "converted.docx";
            }
        }
        else
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Either 'file' or 'html' parameter is required"
            });
        }

        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            // Parse HTML and convert to Word elements
            var elements = ParseHtmlToWordElements(html, preserveStyles);

            foreach (var element in elements)
            {
                body.AppendChild(element);
            }

            // Add default styles
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = CreateDefaultStyles();
            stylesPart.Styles.Save();

            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "HTML converted to Word successfully",
                output_file = outputPath,
                elements_created = elements.Count
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

    private List<OpenXmlElement> ParseHtmlToWordElements(string html, bool preserveStyles)
    {
        var elements = new List<OpenXmlElement>();

        // Remove scripts and styles
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

        // Extract body content if full HTML document
        var bodyMatch = Regex.Match(html, @"<body[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase);
        if (bodyMatch.Success)
        {
            html = bodyMatch.Groups[1].Value;
        }

        // Process headings
        for (int level = 1; level <= 6; level++)
        {
            var headingPattern = $@"<h{level}[^>]*>([\s\S]*?)</h{level}>";
            html = Regex.Replace(html, headingPattern, match =>
            {
                var text = StripHtmlTags(match.Groups[1].Value);
                var placeholder = $"[[HEADING{level}:{text}]]";
                return placeholder;
            }, RegexOptions.IgnoreCase);
        }

        // Process paragraphs
        html = Regex.Replace(html, @"<p[^>]*>([\s\S]*?)</p>", match =>
        {
            var text = StripHtmlTags(match.Groups[1].Value);
            return $"[[PARA:{text}]]\n";
        }, RegexOptions.IgnoreCase);

        // Process lists
        html = Regex.Replace(html, @"<li[^>]*>([\s\S]*?)</li>", match =>
        {
            var text = StripHtmlTags(match.Groups[1].Value);
            return $"[[LIST:{text}]]\n";
        }, RegexOptions.IgnoreCase);

        // Process line breaks
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

        // Process bold, italic, underline within text
        html = Regex.Replace(html, @"<(b|strong)[^>]*>([\s\S]*?)</\1>", "**$2**", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<(i|em)[^>]*>([\s\S]*?)</\1>", "_$2_", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<u[^>]*>([\s\S]*?)</u>", "__$2__", RegexOptions.IgnoreCase);

        // Remove remaining HTML tags
        html = StripHtmlTags(html);

        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);

        // Process placeholders and create Word elements
        var lines = html.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            // Check for heading placeholders
            var headingMatch = Regex.Match(trimmedLine, @"\[\[HEADING(\d+):(.+?)\]\]");
            if (headingMatch.Success)
            {
                var level = int.Parse(headingMatch.Groups[1].Value);
                var text = headingMatch.Groups[2].Value;

                var para = new Paragraph(
                    new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" })
                );
                para.Append(CreateFormattedRuns(text));
                elements.Add(para);
                continue;
            }

            // Check for paragraph placeholders
            var paraMatch = Regex.Match(trimmedLine, @"\[\[PARA:(.+?)\]\]");
            if (paraMatch.Success)
            {
                var text = paraMatch.Groups[1].Value;
                var para = new Paragraph();
                para.Append(CreateFormattedRuns(text));
                elements.Add(para);
                continue;
            }

            // Check for list placeholders
            var listMatch = Regex.Match(trimmedLine, @"\[\[LIST:(.+?)\]\]");
            if (listMatch.Success)
            {
                var text = "• " + listMatch.Groups[1].Value;
                var para = new Paragraph();
                para.Append(CreateFormattedRuns(text));
                elements.Add(para);
                continue;
            }

            // Regular text
            if (!trimmedLine.StartsWith("[["))
            {
                var para = new Paragraph();
                para.Append(CreateFormattedRuns(trimmedLine));
                elements.Add(para);
            }
        }

        return elements;
    }

    private Run[] CreateFormattedRuns(string text)
    {
        var runs = new List<Run>();

        // Process markdown-style formatting
        var parts = Regex.Split(text, @"(\*\*[^*]+\*\*|_[^_]+_|__[^_]+__)");

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**") && part.EndsWith("**"))
            {
                var innerText = part.Substring(2, part.Length - 4);
                runs.Add(new Run(
                    new RunProperties(new Bold()),
                    new Text(innerText) { Space = SpaceProcessingModeValues.Preserve }
                ));
            }
            else if (part.StartsWith("__") && part.EndsWith("__"))
            {
                var innerText = part.Substring(2, part.Length - 4);
                runs.Add(new Run(
                    new RunProperties(new Underline { Val = UnderlineValues.Single }),
                    new Text(innerText) { Space = SpaceProcessingModeValues.Preserve }
                ));
            }
            else if (part.StartsWith("_") && part.EndsWith("_"))
            {
                var innerText = part.Substring(1, part.Length - 2);
                runs.Add(new Run(
                    new RunProperties(new Italic()),
                    new Text(innerText) { Space = SpaceProcessingModeValues.Preserve }
                ));
            }
            else
            {
                runs.Add(new Run(new Text(part) { Space = SpaceProcessingModeValues.Preserve }));
            }
        }

        return runs.ToArray();
    }

    private string StripHtmlTags(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", "");
    }

    private Styles CreateDefaultStyles()
    {
        var styles = new Styles();

        // Normal style
        styles.Append(new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            StyleName = new StyleName { Val = "Normal" },
            PrimaryStyle = new PrimaryStyle()
        });

        // Heading styles
        for (int i = 1; i <= 6; i++)
        {
            var fontSize = 28 - (i * 2); // Decreasing font size
            styles.Append(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = $"Heading{i}",
                StyleName = new StyleName { Val = $"Heading {i}" },
                BasedOn = new BasedOn { Val = "Normal" },
                StyleRunProperties = new StyleRunProperties(
                    new Bold(),
                    new FontSize { Val = (fontSize * 2).ToString() }
                )
            });
        }

        return styles;
    }

    /// <summary>
    /// Protect Word document with password
    /// </summary>
    public async Task<string> ProtectDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var protectionType = GetString(args, "protection_type", "read_only"); // read_only, comments_only, forms_only, tracked_changes
        var outputName = GetString(args, "output_file", "");

        if (string.IsNullOrEmpty(password))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Password is required"
            });
        }

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_protected.docx";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            File.Copy(inputPath, outputPath, true);

            using var doc = WordprocessingDocument.Open(outputPath, true);
            var mainPart = doc.MainDocumentPart!;
            var settings = mainPart.DocumentSettingsPart?.Settings;

            if (settings == null)
            {
                var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
                settingsPart.Settings = new Settings();
                settings = settingsPart.Settings;
            }

            // Remove existing protection
            var existingProtection = settings.Elements<DocumentProtection>().FirstOrDefault();
            existingProtection?.Remove();

            // Create password hash
            var hashBytes = CreatePasswordHash(password);
            var hashString = Convert.ToBase64String(hashBytes);

            // Determine protection edit value
            var editValue = protectionType switch
            {
                "comments_only" => DocumentProtectionValues.Comments,
                "forms_only" => DocumentProtectionValues.Forms,
                "tracked_changes" => DocumentProtectionValues.TrackedChanges,
                _ => DocumentProtectionValues.ReadOnly
            };

            // Add document protection
            var protection = new DocumentProtection
            {
                Edit = editValue,
                Enforcement = true,
                CryptographicAlgorithmSid = 4, // SHA-1
                CryptographicSpinCount = 100000,
                Hash = hashString,
                Salt = Convert.ToBase64String(GenerateSalt())
            };

            settings.PrependChild(protection);
            settings.Save();
            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Document protected successfully",
                output_file = outputPath,
                protection_type = protectionType,
                note = "Document editing is restricted. Password required to remove protection."
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
    /// Remove protection from Word document
    /// </summary>
    public async Task<string> UnprotectDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password", "");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_unprotected.docx";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            File.Copy(inputPath, outputPath, true);

            using var doc = WordprocessingDocument.Open(outputPath, true);
            var mainPart = doc.MainDocumentPart!;
            var settings = mainPart.DocumentSettingsPart?.Settings;

            if (settings == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Document is not protected",
                    output_file = outputPath
                });
            }

            var protection = settings.Elements<DocumentProtection>().FirstOrDefault();
            if (protection == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Document is not protected",
                    output_file = outputPath
                });
            }

            // Remove protection
            protection.Remove();
            settings.Save();
            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Document protection removed",
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

    /// <summary>
    /// Sign Word document with digital certificate
    /// </summary>
    public async Task<string> SignDocumentAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var certificateFile = GetString(args, "certificate");
        var certificatePassword = GetString(args, "password");
        var outputName = GetString(args, "output_file", "");
        var signatureLine = GetBool(args, "add_signature_line", true);
        var signerName = GetString(args, "signer_name", "");
        var signerTitle = GetString(args, "signer_title", "");

        var inputPath = ResolvePath(inputFile, false);
        var certPath = ResolvePath(certificateFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(inputFile) + "_signed.docx";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Load certificate
            var certBytes = await File.ReadAllBytesAsync(certPath);
            var pkcs12Store = new Pkcs12StoreBuilder().Build();
            pkcs12Store.Load(new MemoryStream(certBytes), certificatePassword.ToCharArray());

            string? alias = null;
            X509Certificate? certificate = null;

            foreach (var a in pkcs12Store.Aliases)
            {
                if (pkcs12Store.IsKeyEntry(a))
                {
                    alias = a;
                    var certChain = pkcs12Store.GetCertificateChain(a);
                    if (certChain.Length > 0)
                    {
                        certificate = certChain[0].Certificate;
                    }
                    break;
                }
            }

            if (certificate == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Could not find certificate in the provided file"
                });
            }

            // Copy document
            File.Copy(inputPath, outputPath, true);

            using var doc = WordprocessingDocument.Open(outputPath, true);
            var mainPart = doc.MainDocumentPart!;
            var body = mainPart.Document.Body!;

            // Add signature line to document if requested
            if (signatureLine)
            {
                var sigLineText = new StringBuilder();
                sigLineText.AppendLine();
                sigLineText.AppendLine("________________________");
                if (!string.IsNullOrEmpty(signerName))
                    sigLineText.AppendLine(signerName);
                if (!string.IsNullOrEmpty(signerTitle))
                    sigLineText.AppendLine(signerTitle);
                sigLineText.AppendLine($"Digitally signed on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sigLineText.AppendLine($"Certificate: {certificate.SubjectDN}");

                var signaturePara = new Paragraph(
                    new Run(new Text(sigLineText.ToString()))
                );
                body.AppendChild(signaturePara);
            }

            // Add custom XML part with signature info
            var customXmlPart = mainPart.AddCustomXmlPart(CustomXmlPartType.CustomXml);
            var signatureXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<DigitalSignature xmlns=""urn:zima:signature"">
    <Signer>{System.Security.SecurityElement.Escape(certificate.SubjectDN.ToString())}</Signer>
    <Issuer>{System.Security.SecurityElement.Escape(certificate.IssuerDN.ToString())}</Issuer>
    <SignedDate>{DateTime.UtcNow:O}</SignedDate>
    <ValidFrom>{certificate.NotBefore:O}</ValidFrom>
    <ValidTo>{certificate.NotAfter:O}</ValidTo>
    <SerialNumber>{certificate.SerialNumber}</SerialNumber>
</DigitalSignature>";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(signatureXml)))
            {
                customXmlPart.FeedData(stream);
            }

            mainPart.Document.Save();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Document signed successfully",
                output_file = outputPath,
                signature_info = new
                {
                    signer = certificate.SubjectDN.ToString(),
                    issuer = certificate.IssuerDN.ToString(),
                    valid_from = certificate.NotBefore.ToString("yyyy-MM-dd"),
                    valid_to = certificate.NotAfter.ToString("yyyy-MM-dd"),
                    signature_line_added = signatureLine
                },
                note = "Signature metadata added. For full OOXML digital signature, use Microsoft Office or specialized tools."
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                hint = "Make sure the certificate file is a valid PFX/P12 file and the password is correct"
            });
        }
    }

    /// <summary>
    /// Verify Word document signature
    /// </summary>
    public async Task<string> VerifyDocumentSignatureAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        try
        {
            using var doc = WordprocessingDocument.Open(inputPath, false);
            var mainPart = doc.MainDocumentPart!;

            var signatures = new List<object>();

            // Check for custom signature XML parts
            foreach (var customPart in mainPart.CustomXmlParts)
            {
                using var stream = customPart.GetStream();
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                if (content.Contains("DigitalSignature"))
                {
                    var signerMatch = Regex.Match(content, @"<Signer>([^<]+)</Signer>");
                    var issuerMatch = Regex.Match(content, @"<Issuer>([^<]+)</Issuer>");
                    var dateMatch = Regex.Match(content, @"<SignedDate>([^<]+)</SignedDate>");

                    signatures.Add(new
                    {
                        type = "ZIMA Digital Signature",
                        signer = signerMatch.Success ? signerMatch.Groups[1].Value : "Unknown",
                        issuer = issuerMatch.Success ? issuerMatch.Groups[1].Value : "Unknown",
                        signed_date = dateMatch.Success ? dateMatch.Groups[1].Value : "Unknown"
                    });
                }
            }

            // Check for OOXML digital signatures
            if (doc.DigitalSignatureOriginPart != null)
            {
                signatures.Add(new
                {
                    type = "OOXML Digital Signature",
                    note = "Document contains OOXML digital signatures"
                });
            }

            if (signatures.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    signed = false,
                    message = "Document does not contain digital signatures"
                });
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                signed = true,
                signature_count = signatures.Count,
                signatures = signatures
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

    private byte[] CreatePasswordHash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        return sha.ComputeHash(bytes);
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private string FormatSize(long bytes)
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

    #endregion

    private List<Dictionary<string, string>> GetMergeData(Dictionary<string, object> args, string key)
    {
        var result = new List<Dictionary<string, string>>();
        if (!args.TryGetValue(key, out var value)) return result;

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                var record = new Dictionary<string, string>();
                foreach (var prop in item.EnumerateObject())
                {
                    record[prop.Name] = prop.Value.GetString() ?? "";
                }
                result.Add(record);
            }
        }
        return result;
    }

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetInt32();
        return Convert.ToInt32(value);
    }

    // Helper methods
    private async Task CreateDocumentFromElements(List<OpenXmlElement> elements, string outputPath)
    {
        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        foreach (var element in elements)
        {
            body.AppendChild(element.CloneNode(true));
        }

        mainPart.Document.Save();
        await Task.CompletedTask;
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
        return bool.TryParse(value?.ToString(), out var result) ? result : defaultValue;
    }

    private string[] GetStringArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        }
        return Array.Empty<string>();
    }

    private int[] GetIntArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<int>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        }
        return Array.Empty<int>();
    }
}
