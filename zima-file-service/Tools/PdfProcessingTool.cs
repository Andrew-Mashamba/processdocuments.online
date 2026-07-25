using System.Text;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Signatures;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using IOPath = System.IO.Path;

namespace ZimaFileService.Tools;

/// <summary>
/// PDF Processing Tools - merge, split, compress, rotate, extract pages, add watermarks, etc.
/// </summary>
public class PdfProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public PdfProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Merge multiple PDF files into one
    /// </summary>
    public async Task<string> MergePdfAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputName = GetString(args, "output_file", "merged.pdf");

        if (files.Length < 2)
            throw new ArgumentException("At least 2 PDF files are required for merging");

        var outputPath = ResolvePath(outputName, true);

        using var writer = new PdfWriter(outputPath);
        using var mergedPdf = new PdfDocument(writer);
        var merger = new PdfMerger(mergedPdf);

        foreach (var file in files)
        {
            var filePath = ResolvePath(file, false);
            using var reader = new PdfReader(filePath);
            using var srcPdf = new PdfDocument(reader);
            merger.Merge(srcPdf, 1, srcPdf.GetNumberOfPages());
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} PDF files",
            output_file = outputPath,
            total_pages = mergedPdf.GetNumberOfPages()
        });
    }

    /// <summary>
    /// Split a PDF into multiple files
    /// </summary>
    public async Task<string> SplitPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mode = GetString(args, "mode", "pages"); // "pages", "range", "every_n"
        var outputPrefix = GetString(args, "output_prefix", "split");

        var inputPath = ResolvePath(inputFile, false);
        var outputFiles = new List<string>();

        using var reader = new PdfReader(inputPath);
        using var srcPdf = new PdfDocument(reader);
        var totalPages = srcPdf.GetNumberOfPages();

        if (mode == "pages")
        {
            // Split into individual pages
            for (int i = 1; i <= totalPages; i++)
            {
                var outputPath = IOPath.Combine(_generatedPath, $"{outputPrefix}_page_{i}.pdf");
                using var writer = new PdfWriter(outputPath);
                using var destPdf = new PdfDocument(writer);
                srcPdf.CopyPagesTo(i, i, destPdf);
                outputFiles.Add(outputPath);
            }
        }
        else if (mode == "range")
        {
            var ranges = GetStringArray(args, "ranges"); // e.g., ["1-3", "4-6", "7-10"]
            int fileIndex = 1;
            foreach (var range in ranges)
            {
                var parts = range.Split('-');
                int start = int.Parse(parts[0]);
                int end = parts.Length > 1 ? int.Parse(parts[1]) : start;

                var outputPath = IOPath.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.pdf");
                using var writer = new PdfWriter(outputPath);
                using var destPdf = new PdfDocument(writer);
                srcPdf.CopyPagesTo(start, end, destPdf);
                outputFiles.Add(outputPath);
                fileIndex++;
            }
        }
        else if (mode == "every_n")
        {
            int n = GetInt(args, "n", 5);
            int fileIndex = 1;
            for (int i = 1; i <= totalPages; i += n)
            {
                int end = Math.Min(i + n - 1, totalPages);
                var outputPath = IOPath.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.pdf");
                using var writer = new PdfWriter(outputPath);
                using var destPdf = new PdfDocument(writer);
                srcPdf.CopyPagesTo(i, end, destPdf);
                outputFiles.Add(outputPath);
                fileIndex++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split PDF into {outputFiles.Count} files",
            output_files = outputFiles,
            original_pages = totalPages
        });
    }

    /// <summary>
    /// Extract specific pages from a PDF
    /// </summary>
    public async Task<string> ExtractPagesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var pages = GetIntArray(args, "pages"); // e.g., [1, 3, 5, 7]
        var outputName = GetString(args, "output_file", "extracted.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var srcPdf = new PdfDocument(reader);
        using var writer = new PdfWriter(outputPath);
        using var destPdf = new PdfDocument(writer);

        foreach (var page in pages.OrderBy(p => p))
        {
            if (page >= 1 && page <= srcPdf.GetNumberOfPages())
            {
                srcPdf.CopyPagesTo(page, page, destPdf);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {pages.Length} pages",
            output_file = outputPath,
            extracted_pages = pages
        });
    }

    /// <summary>
    /// Remove specific pages from a PDF
    /// </summary>
    public async Task<string> RemovePagesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var pagesToRemove = GetIntArray(args, "pages");
        var outputName = GetString(args, "output_file", "modified.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var srcPdf = new PdfDocument(reader);
        using var writer = new PdfWriter(outputPath);
        using var destPdf = new PdfDocument(writer);

        var removeSet = new HashSet<int>(pagesToRemove);
        var totalPages = srcPdf.GetNumberOfPages();
        var keptPages = 0;

        for (int i = 1; i <= totalPages; i++)
        {
            if (!removeSet.Contains(i))
            {
                srcPdf.CopyPagesTo(i, i, destPdf);
                keptPages++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {pagesToRemove.Length} pages, kept {keptPages}",
            output_file = outputPath,
            removed_pages = pagesToRemove,
            remaining_pages = keptPages
        });
    }

    /// <summary>
    /// Rotate pages in a PDF
    /// </summary>
    public async Task<string> RotatePdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var rotation = GetInt(args, "rotation", 90); // 90, 180, 270
        var pages = args.ContainsKey("pages") ? GetIntArray(args, "pages") : null;
        var outputName = GetString(args, "output_file", "rotated.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);

        var totalPages = pdfDoc.GetNumberOfPages();
        var pagesToRotate = pages ?? Enumerable.Range(1, totalPages).ToArray();

        foreach (var pageNum in pagesToRotate)
        {
            if (pageNum >= 1 && pageNum <= totalPages)
            {
                var page = pdfDoc.GetPage(pageNum);
                var currentRotation = page.GetRotation();
                page.SetRotation((currentRotation + rotation) % 360);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Rotated {pagesToRotate.Length} pages by {rotation} degrees",
            output_file = outputPath,
            rotated_pages = pagesToRotate
        });
    }

    /// <summary>
    /// Add watermark to PDF
    /// </summary>
    public async Task<string> AddWatermarkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var watermarkText = GetString(args, "text", "CONFIDENTIAL");
        var opacity = GetFloat(args, "opacity", 0.3f);
        var fontSize = GetInt(args, "font_size", 60);
        var outputName = GetString(args, "output_file", "watermarked.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);
        var document = new Document(pdfDoc);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var totalPages = pdfDoc.GetNumberOfPages();

        for (int i = 1; i <= totalPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();

            var paragraph = new Paragraph(watermarkText)
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetOpacity(opacity)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY);

            document.ShowTextAligned(paragraph,
                pageSize.GetWidth() / 2,
                pageSize.GetHeight() / 2,
                i,
                iText.Layout.Properties.TextAlignment.CENTER,
                iText.Layout.Properties.VerticalAlignment.MIDDLE,
                45); // 45 degree angle
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added watermark to {totalPages} pages",
            output_file = outputPath,
            watermark_text = watermarkText
        });
    }

    /// <summary>
    /// Add page numbers to PDF
    /// </summary>
    public async Task<string> AddPageNumbersAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var position = GetString(args, "position", "bottom-center"); // bottom-left, bottom-center, bottom-right, top-left, top-center, top-right
        var format = GetString(args, "format", "Page {0} of {1}");
        var fontSize = GetInt(args, "font_size", 10);
        var outputName = GetString(args, "output_file", "numbered.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);
        var document = new Document(pdfDoc);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var totalPages = pdfDoc.GetNumberOfPages();

        for (int i = 1; i <= totalPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();

            var pageText = string.Format(format, i, totalPages);
            var paragraph = new Paragraph(pageText)
                .SetFont(font)
                .SetFontSize(fontSize);

            float x, y;
            var textAlign = iText.Layout.Properties.TextAlignment.CENTER;

            switch (position)
            {
                case "bottom-left":
                    x = 40; y = 20; textAlign = iText.Layout.Properties.TextAlignment.LEFT;
                    break;
                case "bottom-right":
                    x = pageSize.GetWidth() - 40; y = 20; textAlign = iText.Layout.Properties.TextAlignment.RIGHT;
                    break;
                case "top-left":
                    x = 40; y = pageSize.GetHeight() - 20; textAlign = iText.Layout.Properties.TextAlignment.LEFT;
                    break;
                case "top-center":
                    x = pageSize.GetWidth() / 2; y = pageSize.GetHeight() - 20;
                    break;
                case "top-right":
                    x = pageSize.GetWidth() - 40; y = pageSize.GetHeight() - 20; textAlign = iText.Layout.Properties.TextAlignment.RIGHT;
                    break;
                default: // bottom-center
                    x = pageSize.GetWidth() / 2; y = 20;
                    break;
            }

            document.ShowTextAligned(paragraph, x, y, i, textAlign, iText.Layout.Properties.VerticalAlignment.BOTTOM, 0);
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added page numbers to {totalPages} pages",
            output_file = outputPath,
            position = position,
            format = format
        });
    }

    /// <summary>
    /// Get PDF information/metadata
    /// </summary>
    public async Task<string> GetPdfInfoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        using var reader = new PdfReader(inputPath);
        using var pdfDoc = new PdfDocument(reader);

        var info = pdfDoc.GetDocumentInfo();
        var firstPage = pdfDoc.GetFirstPage();
        var pageSize = firstPage.GetPageSize();

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            pages = pdfDoc.GetNumberOfPages(),
            title = info.GetTitle(),
            author = info.GetAuthor(),
            subject = info.GetSubject(),
            creator = info.GetCreator(),
            producer = info.GetProducer(),
            page_width = pageSize.GetWidth(),
            page_height = pageSize.GetHeight(),
            is_encrypted = reader.IsEncrypted()
        });
    }

    /// <summary>
    /// Compress/optimize PDF
    /// </summary>
    public async Task<string> CompressPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "compressed.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var originalSize = new FileInfo(inputPath).Length;

        // Create writer with compression settings
        var writerProperties = new WriterProperties()
            .SetCompressionLevel(9) // Maximum compression
            .SetFullCompressionMode(true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath, writerProperties);
        using var srcPdf = new PdfDocument(reader);
        using var destPdf = new PdfDocument(writer);

        srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), destPdf);

        destPdf.Close();
        srcPdf.Close();

        var newSize = new FileInfo(outputPath).Length;
        var savings = ((double)(originalSize - newSize) / originalSize) * 100;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Compressed PDF by {savings:F1}%",
            output_file = outputPath,
            original_size = FormatSize(originalSize),
            new_size = FormatSize(newSize),
            savings_percent = Math.Round(savings, 1)
        });
    }

    /// <summary>
    /// Protect PDF with password
    /// </summary>
    public async Task<string> ProtectPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var userPassword = GetString(args, "user_password", "");
        var ownerPassword = GetString(args, "owner_password", "owner123");
        var allowPrinting = GetBool(args, "allow_printing", true);
        var allowCopying = GetBool(args, "allow_copying", false);
        var outputName = GetString(args, "output_file", "protected.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var userPwd = string.IsNullOrEmpty(userPassword) ? null : System.Text.Encoding.UTF8.GetBytes(userPassword);
        var ownerPwd = System.Text.Encoding.UTF8.GetBytes(ownerPassword);

        int permissions = 0;
        if (allowPrinting) permissions |= iText.Kernel.Pdf.EncryptionConstants.ALLOW_PRINTING;
        if (allowCopying) permissions |= iText.Kernel.Pdf.EncryptionConstants.ALLOW_COPY;

        var writerProperties = new WriterProperties()
            .SetStandardEncryption(userPwd, ownerPwd, permissions, iText.Kernel.Pdf.EncryptionConstants.ENCRYPTION_AES_256);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath, writerProperties);
        using var srcPdf = new PdfDocument(reader);
        using var destPdf = new PdfDocument(writer);

        srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), destPdf);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "PDF protected with encryption",
            output_file = outputPath,
            user_password_set = !string.IsNullOrEmpty(userPassword),
            allow_printing = allowPrinting,
            allow_copying = allowCopying
        });
    }

    /// <summary>
    /// Unlock/decrypt a password-protected PDF
    /// </summary>
    public async Task<string> UnlockPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var outputName = GetString(args, "output_file", "unlocked.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var readerProps = new ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(password));

        using var reader = new PdfReader(inputPath, readerProps);
        using var writer = new PdfWriter(outputPath);
        using var srcPdf = new PdfDocument(reader);
        using var destPdf = new PdfDocument(writer);

        srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), destPdf);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "PDF unlocked successfully",
            output_file = outputPath,
            pages = srcPdf.GetNumberOfPages()
        });
    }

    /// <summary>
    /// Extract text from PDF
    /// </summary>
    public async Task<string> PdfToTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "extracted.txt");
        var pages = args.ContainsKey("pages") ? GetIntArray(args, "pages") : null;

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var pdfDoc = new PdfDocument(reader);

        var sb = new StringBuilder();
        var totalPages = pdfDoc.GetNumberOfPages();
        var pagesToExtract = pages ?? Enumerable.Range(1, totalPages).ToArray();

        var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();

        foreach (var pageNum in pagesToExtract)
        {
            if (pageNum >= 1 && pageNum <= totalPages)
            {
                var page = pdfDoc.GetPage(pageNum);
                var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                sb.AppendLine($"=== Page {pageNum} ===");
                sb.AppendLine(text);
                sb.AppendLine();
            }
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted text from {pagesToExtract.Length} pages",
            output_file = outputPath,
            pages_extracted = pagesToExtract.Length,
            characters = sb.Length
        });
    }

    /// <summary>
    /// Create PDF from text file
    /// </summary>
    public async Task<string> TextToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.pdf");
        var fontSize = GetInt(args, "font_size", 12);
        var title = GetString(args, "title", "");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var content = await File.ReadAllTextAsync(inputPath);

        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(writer);
        var document = new Document(pdfDoc);

        var font = PdfFontFactory.CreateFont(StandardFonts.COURIER);

        if (!string.IsNullOrEmpty(title))
        {
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            document.Add(new Paragraph(title).SetFont(titleFont).SetFontSize(18).SetMarginBottom(20));
        }

        foreach (var line in content.Split('\n'))
        {
            document.Add(new Paragraph(line).SetFont(font).SetFontSize(fontSize).SetMarginBottom(2));
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Created PDF from text",
            output_file = outputPath,
            pages = pdfDoc.GetNumberOfPages()
        });
    }

    /// <summary>
    /// Compare two PDF files
    /// </summary>
    public async Task<string> ComparePdfAsync(Dictionary<string, object> args)
    {
        var file1 = GetString(args, "file1");
        var file2 = GetString(args, "file2");

        var path1 = ResolvePath(file1, false);
        var path2 = ResolvePath(file2, false);

        using var reader1 = new PdfReader(path1);
        using var reader2 = new PdfReader(path2);
        using var pdf1 = new PdfDocument(reader1);
        using var pdf2 = new PdfDocument(reader2);

        var pages1 = pdf1.GetNumberOfPages();
        var pages2 = pdf2.GetNumberOfPages();

        var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();

        var text1 = new StringBuilder();
        var text2 = new StringBuilder();

        for (int i = 1; i <= pages1; i++)
            text1.Append(iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pdf1.GetPage(i), strategy));

        for (int i = 1; i <= pages2; i++)
            text2.Append(iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pdf2.GetPage(i), strategy));

        var identical = text1.ToString() == text2.ToString();
        var size1 = new FileInfo(path1).Length;
        var size2 = new FileInfo(path2).Length;

        return JsonSerializer.Serialize(new
        {
            success = true,
            file1 = path1,
            file2 = path2,
            file1_pages = pages1,
            file2_pages = pages2,
            file1_size = FormatSize(size1),
            file2_size = FormatSize(size2),
            file1_chars = text1.Length,
            file2_chars = text2.Length,
            text_identical = identical
        });
    }

    /// <summary>
    /// Crop PDF pages
    /// </summary>
    public async Task<string> CropPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var left = GetFloat(args, "left", 0);
        var bottom = GetFloat(args, "bottom", 0);
        var right = GetFloat(args, "right", 0);
        var top = GetFloat(args, "top", 0);
        var pages = args.ContainsKey("pages") ? GetIntArray(args, "pages") : null;
        var outputName = GetString(args, "output_file", "cropped.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);

        var totalPages = pdfDoc.GetNumberOfPages();
        var pagesToCrop = pages ?? Enumerable.Range(1, totalPages).ToArray();

        foreach (var pageNum in pagesToCrop)
        {
            if (pageNum >= 1 && pageNum <= totalPages)
            {
                var page = pdfDoc.GetPage(pageNum);
                var mediaBox = page.GetMediaBox();

                var newBox = new iText.Kernel.Geom.Rectangle(
                    mediaBox.GetLeft() + left,
                    mediaBox.GetBottom() + bottom,
                    mediaBox.GetWidth() - left - right,
                    mediaBox.GetHeight() - bottom - top
                );

                page.SetCropBox(newBox);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Cropped {pagesToCrop.Length} pages",
            output_file = outputPath,
            margins_removed = new { left, bottom, right, top }
        });
    }

    /// <summary>
    /// Set PDF metadata
    /// </summary>
    public async Task<string> SetPdfMetadataAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var title = GetString(args, "title", "");
        var author = GetString(args, "author", "");
        var subject = GetString(args, "subject", "");
        var keywords = GetString(args, "keywords", "");
        var outputName = GetString(args, "output_file", "metadata.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);

        var info = pdfDoc.GetDocumentInfo();

        if (!string.IsNullOrEmpty(title)) info.SetTitle(title);
        if (!string.IsNullOrEmpty(author)) info.SetAuthor(author);
        if (!string.IsNullOrEmpty(subject)) info.SetSubject(subject);
        if (!string.IsNullOrEmpty(keywords)) info.SetKeywords(keywords);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Updated PDF metadata",
            output_file = outputPath,
            title = title,
            author = author,
            subject = subject,
            keywords = keywords
        });
    }

    /// <summary>
    /// Convert images to PDF
    /// </summary>
    public async Task<string> ImageToPdfAsync(Dictionary<string, object> args)
    {
        var images = GetStringArray(args, "images");
        var outputName = GetString(args, "output_file", "images.pdf");
        var pageSize = GetString(args, "page_size", "A4"); // A4, Letter, fit

        var outputPath = ResolvePath(outputName, true);

        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(writer);
        var document = new Document(pdfDoc);

        foreach (var imagePath in images)
        {
            var resolvedPath = ResolvePath(imagePath, false);
            var imageData = iText.IO.Image.ImageDataFactory.Create(resolvedPath);
            var image = new iText.Layout.Element.Image(imageData);

            if (pageSize == "fit")
            {
                pdfDoc.AddNewPage(new iText.Kernel.Geom.PageSize(imageData.GetWidth(), imageData.GetHeight()));
            }
            else
            {
                var ps = pageSize == "Letter" ? iText.Kernel.Geom.PageSize.LETTER : iText.Kernel.Geom.PageSize.A4;
                pdfDoc.AddNewPage(ps);
                image.ScaleToFit(ps.GetWidth() - 72, ps.GetHeight() - 72);
            }

            document.Add(image);
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Created PDF from {images.Length} images",
            output_file = outputPath,
            pages = images.Length
        });
    }

    /// <summary>
    /// Convert HTML to PDF
    /// </summary>
    public async Task<string> HtmlToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var htmlContent = await File.ReadAllTextAsync(inputPath);

        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(writer);
        var document = new Document(pdfDoc);

        // Simple HTML to text extraction (basic conversion)
        var text = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]+>", "");
        text = System.Net.WebUtility.HtmlDecode(text);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        foreach (var line in text.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                document.Add(new Paragraph(line.Trim()).SetFont(font).SetFontSize(11));
            }
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted HTML to PDF",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Redact (black out) areas in PDF
    /// </summary>
    public async Task<string> RedactPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var redactions = GetRedactionAreas(args, "areas"); // [{page, x, y, width, height}]
        var outputName = GetString(args, "output_file", "redacted.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var reader = new PdfReader(inputPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);

        foreach (var area in redactions)
        {
            if (area.Page >= 1 && area.Page <= pdfDoc.GetNumberOfPages())
            {
                var page = pdfDoc.GetPage(area.Page);
                var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
                canvas.SetFillColor(iText.Kernel.Colors.ColorConstants.BLACK);
                canvas.Rectangle(area.X, area.Y, area.Width, area.Height);
                canvas.Fill();
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Applied {redactions.Count} redactions",
            output_file = outputPath,
            redactions_applied = redactions.Count
        });
    }

    /// <summary>
    /// Repair corrupted PDF (attempt to recover)
    /// </summary>
    public async Task<string> RepairPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "repaired.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        // Use unethicalreading mode to attempt recovery
        var readerProps = new ReaderProperties();

        try
        {
            using var reader = new PdfReader(inputPath, readerProps);
            reader.SetUnethicalReading(true);

            using var writer = new PdfWriter(outputPath);
            using var srcPdf = new PdfDocument(reader);
            using var destPdf = new PdfDocument(writer);

            srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), destPdf);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "PDF repaired successfully",
                output_file = outputPath,
                pages = srcPdf.GetNumberOfPages()
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Could not repair PDF: {ex.Message}",
                error = ex.Message
            });
        }
    }

    #region PDF Digital Signatures

    /// <summary>
    /// Sign PDF with digital certificate (PFX/P12 file)
    /// </summary>
    public async Task<string> SignPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var certificateFile = GetString(args, "certificate");
        var certificatePassword = GetString(args, "password");
        var outputName = GetString(args, "output_file", "");
        var reason = GetString(args, "reason", "Document signed digitally");
        var location = GetString(args, "location", "");
        var contact = GetString(args, "contact", "");
        var visible = GetBool(args, "visible", true);
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 50);
        var y = GetFloat(args, "y", 50);
        var width = GetFloat(args, "width", 200);
        var height = GetFloat(args, "height", 50);

        var inputPath = ResolvePath(inputFile, false);
        var certPath = ResolvePath(certificateFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_signed.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Load the certificate
            var certBytes = await File.ReadAllBytesAsync(certPath);
            var pkcs12Store = new Pkcs12StoreBuilder().Build();
            pkcs12Store.Load(new MemoryStream(certBytes), certificatePassword.ToCharArray());

            string? alias = null;
            AsymmetricKeyParameter? privateKey = null;
            IX509Certificate[]? chain = null;

            foreach (var a in pkcs12Store.Aliases)
            {
                if (pkcs12Store.IsKeyEntry(a))
                {
                    alias = a;
                    privateKey = pkcs12Store.GetKey(a).Key;
                    var certChain = pkcs12Store.GetCertificateChain(a);
                    chain = new IX509Certificate[certChain.Length];
                    for (int i = 0; i < certChain.Length; i++)
                    {
                        chain[i] = new X509CertificateBC(certChain[i].Certificate);
                    }
                    break;
                }
            }

            if (privateKey == null || chain == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Could not find private key in certificate file"
                });
            }

            // Create signature
            using var reader = new PdfReader(inputPath);
            using var outputStream = new FileStream(outputPath, FileMode.Create);

            var signer = new PdfSigner(reader, outputStream, new StampingProperties());

            // Set signature metadata
            var signatureAppearance = signer.GetSignatureAppearance();
            signatureAppearance
                .SetReason(reason)
                .SetLocation(location)
                .SetContact(contact);

            if (visible)
            {
                signatureAppearance
                    .SetPageNumber(page)
                    .SetPageRect(new Rectangle(x, y, width, height))
                    .SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
            }

            signer.SetFieldName("Signature1");

            // Sign the document
            var privateKeySignature = new PrivateKeySignature(
                new PrivateKeyBC(privateKey),
                DigestAlgorithms.SHA256);

            signer.SignDetached(privateKeySignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

            // Get certificate info for response
            var cert = chain[0];
            var x509Cert = ((X509CertificateBC)cert).GetCertificate();

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "PDF signed successfully",
                output_file = outputPath,
                signature_info = new
                {
                    signer = x509Cert.SubjectDN.ToString(),
                    issuer = x509Cert.IssuerDN.ToString(),
                    valid_from = x509Cert.NotBefore.ToString("yyyy-MM-dd"),
                    valid_to = x509Cert.NotAfter.ToString("yyyy-MM-dd"),
                    reason = reason,
                    location = location,
                    visible = visible
                }
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
    /// Verify PDF signatures
    /// </summary>
    public async Task<string> VerifyPdfSignatureAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var pdfDoc = new PdfDocument(reader);

            var signatureUtil = new SignatureUtil(pdfDoc);
            var signatureNames = signatureUtil.GetSignatureNames();

            if (signatureNames.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    signed = false,
                    message = "PDF has no digital signatures"
                });
            }

            var signatures = new List<object>();

            foreach (var name in signatureNames)
            {
                var sig = signatureUtil.GetSignature(name);
                var pkcs7 = signatureUtil.ReadSignatureData(name);

                var signingCert = pkcs7.GetSigningCertificate();
                string signerName = "Unknown";
                if (signingCert != null)
                {
                    try
                    {
                        var bcCert = ((X509CertificateBC)signingCert).GetCertificate();
                        signerName = bcCert.SubjectDN?.ToString() ?? "Unknown";
                    }
                    catch { }
                }

                var sigInfo = new
                {
                    name = name,
                    covers_whole_document = signatureUtil.SignatureCoversWholeDocument(name),
                    signer = signerName,
                    sign_date = pkcs7.GetSignDate().ToString("yyyy-MM-dd HH:mm:ss"),
                    reason = pkcs7.GetReason() ?? "",
                    location = pkcs7.GetLocation() ?? "",
                    integrity_verified = pkcs7.VerifySignatureIntegrityAndAuthenticity()
                };

                signatures.Add(sigInfo);
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                signed = true,
                signature_count = signatureNames.Count,
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

    /// <summary>
    /// Create a self-signed certificate for testing
    /// </summary>
    public async Task<string> CreateTestCertificateAsync(Dictionary<string, object> args)
    {
        var commonName = GetString(args, "name", "Test Signer");
        var organization = GetString(args, "organization", "Test Organization");
        var password = GetString(args, "password", "password123");
        var outputName = GetString(args, "output_file", "test_certificate.pfx");
        var validYears = GetInt(args, "valid_years", 1);

        var outputPath = ResolvePath(outputName, true);

        try
        {
            // Generate key pair
            var keyPairGenerator = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
            keyPairGenerator.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(
                new Org.BouncyCastle.Security.SecureRandom(), 2048));
            var keyPair = keyPairGenerator.GenerateKeyPair();

            // Create certificate
            var certGenerator = new Org.BouncyCastle.X509.X509V3CertificateGenerator();
            var serialNumber = Org.BouncyCastle.Math.BigInteger.ProbablePrime(120, new Random());

            certGenerator.SetSerialNumber(serialNumber);
            certGenerator.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name($"CN={commonName}, O={organization}"));
            certGenerator.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name($"CN={commonName}, O={organization}"));
            certGenerator.SetNotBefore(DateTime.UtcNow);
            certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(validYears));
            certGenerator.SetPublicKey(keyPair.Public);

            // Sign the certificate
            var signatureFactory = new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory(
                "SHA256WithRSA", keyPair.Private);
            var cert = certGenerator.Generate(signatureFactory);

            // Create PKCS12 store
            var store = new Pkcs12StoreBuilder().Build();
            var certEntry = new X509CertificateEntry(cert);
            store.SetKeyEntry("certificate", new AsymmetricKeyEntry(keyPair.Private), new[] { certEntry });

            using var stream = new FileStream(outputPath, FileMode.Create);
            store.Save(stream, password.ToCharArray(), new Org.BouncyCastle.Security.SecureRandom());

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Test certificate created successfully",
                output_file = outputPath,
                certificate_info = new
                {
                    subject = $"CN={commonName}, O={organization}",
                    valid_from = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    valid_to = DateTime.UtcNow.AddYears(validYears).ToString("yyyy-MM-dd"),
                    password = password
                },
                warning = "This is a self-signed certificate for testing only. Do not use in production."
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

    #region PDF Annotations

    /// <summary>
    /// Add text annotation (sticky note) to PDF
    /// </summary>
    public async Task<string> AddStickyNoteAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var title = GetString(args, "title", "Note");
        var content = GetString(args, "content", "");
        var color = GetString(args, "color", "yellow"); // yellow, green, blue, red, pink

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_annotated.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, 20, 20);

            var annotation = new PdfTextAnnotation(rect);
            annotation.SetContents(content);
            annotation.SetTitle(new iText.Kernel.Pdf.PdfString(title));
            annotation.SetColor(GetAnnotationColor(color));
            annotation.Put(iText.Kernel.Pdf.PdfName.Name, new iText.Kernel.Pdf.PdfName("Comment"));
            annotation.SetFlag(PdfAnnotation.PRINT);
            pdfPage.AddAnnotation(annotation);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Sticky note added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, title, color }
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
    /// Add highlight annotation to PDF
    /// </summary>
    public async Task<string> AddHighlightAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 200);
        var height = GetFloat(args, "height", 15);
        var color = GetString(args, "color", "yellow");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_highlighted.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            // Create highlight annotation using quad points
            var quadPoints = new float[] {
                x, y + height,           // top-left
                x + width, y + height,   // top-right
                x, y,                    // bottom-left
                x + width, y             // bottom-right
            };

            var highlight = new PdfTextMarkupAnnotation(
                rect,
                PdfName.Highlight,
                quadPoints);

            highlight.SetColor(GetAnnotationColor(color));
            highlight.SetFlag(PdfAnnotation.PRINT);

            pdfPage.AddAnnotation(highlight);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Highlight added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, width, height, color }
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
    /// Add underline annotation to PDF
    /// </summary>
    public async Task<string> AddUnderlineAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 200);
        var height = GetFloat(args, "height", 15);
        var color = GetString(args, "color", "red");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_underlined.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            var quadPoints = new float[] {
                x, y + height, x + width, y + height,
                x, y, x + width, y
            };

            var underline = new PdfTextMarkupAnnotation(
                rect,
                PdfName.Underline,
                quadPoints);

            underline.SetColor(GetAnnotationColor(color));
            underline.SetFlag(PdfAnnotation.PRINT);

            pdfPage.AddAnnotation(underline);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Underline added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, width, height, color }
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
    /// Add strikethrough annotation to PDF
    /// </summary>
    public async Task<string> AddStrikethroughAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 200);
        var height = GetFloat(args, "height", 15);
        var color = GetString(args, "color", "red");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_strikethrough.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            var quadPoints = new float[] {
                x, y + height, x + width, y + height,
                x, y, x + width, y
            };

            var strikeout = new PdfTextMarkupAnnotation(
                rect,
                PdfName.StrikeOut,
                quadPoints);

            strikeout.SetColor(GetAnnotationColor(color));
            strikeout.SetFlag(PdfAnnotation.PRINT);

            pdfPage.AddAnnotation(strikeout);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Strikethrough added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, width, height, color }
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
    /// Add free text annotation (text box) to PDF
    /// </summary>
    public async Task<string> AddFreeTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 200);
        var height = GetFloat(args, "height", 50);
        var content = GetString(args, "content", "");
        var fontSize = GetInt(args, "font_size", 12);
        var color = GetString(args, "color", "black");
        var bgColor = GetString(args, "background_color", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_text_annotated.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            var freeText = new PdfFreeTextAnnotation(rect, new iText.Kernel.Pdf.PdfString(content));
            freeText.SetDefaultAppearance(new iText.Kernel.Pdf.PdfString($"/Helv {fontSize} Tf 0 g"));
            freeText.SetColor(GetAnnotationColor(color));
            freeText.SetContents(content);
            freeText.SetFlag(PdfAnnotation.PRINT);

            pdfPage.AddAnnotation(freeText);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Free text annotation added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, width, height, content, fontSize }
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
    /// Add stamp annotation to PDF
    /// </summary>
    public async Task<string> AddStampAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 150);
        var height = GetFloat(args, "height", 50);
        var stampType = GetString(args, "stamp_type", "Approved"); // Approved, NotApproved, Draft, Final, etc.

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_stamped.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            var stamp = new PdfStampAnnotation(rect)
                .SetStampName(new iText.Kernel.Pdf.PdfName(stampType));

            stamp.SetFlag(PdfAnnotation.PRINT);
            stamp.SetContents(stampType);

            pdfPage.AddAnnotation(stamp);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"'{stampType}' stamp added successfully",
                output_file = outputPath,
                annotation = new { page, x, y, width, height, stamp_type = stampType }
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
    /// Add link annotation to PDF
    /// </summary>
    public async Task<string> AddLinkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var page = GetInt(args, "page", 1);
        var x = GetFloat(args, "x", 100);
        var y = GetFloat(args, "y", 700);
        var width = GetFloat(args, "width", 100);
        var height = GetFloat(args, "height", 20);
        var url = GetString(args, "url", "");
        var targetPage = GetInt(args, "target_page", 0); // For internal links

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_linked.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var pdfPage = pdfDoc.GetPage(page);
            var rect = new Rectangle(x, y, width, height);

            PdfLinkAnnotation link;

            if (!string.IsNullOrEmpty(url))
            {
                // External URL link
                var action = iText.Kernel.Pdf.Action.PdfAction.CreateURI(url);
                link = new PdfLinkAnnotation(rect).SetAction(action);
            }
            else if (targetPage > 0)
            {
                // Internal page link
                var destPage = pdfDoc.GetPage(targetPage);
                var action = iText.Kernel.Pdf.Action.PdfAction.CreateGoTo(
                    iText.Kernel.Pdf.Navigation.PdfExplicitDestination.CreateFit(destPage));
                link = new PdfLinkAnnotation(rect).SetAction(action);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Either 'url' or 'target_page' must be specified"
                });
            }

            link.SetBorder(new iText.Kernel.Pdf.PdfArray(new float[] { 0, 0, 0 }));
            link.SetFlag(PdfAnnotation.PRINT);

            pdfPage.AddAnnotation(link);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Link annotation added successfully",
                output_file = outputPath,
                annotation = new {
                    page, x, y, width, height,
                    url = !string.IsNullOrEmpty(url) ? url : null,
                    target_page = targetPage > 0 ? targetPage : (int?)null
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
    /// List all annotations in PDF
    /// </summary>
    public async Task<string> ListAnnotationsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var pdfDoc = new PdfDocument(reader);

            var allAnnotations = new List<object>();

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                var annotations = page.GetAnnotations();

                foreach (var annot in annotations)
                {
                    var annotType = annot.GetSubtype()?.GetValue() ?? "Unknown";
                    var rect = annot.GetRectangle()?.ToRectangle();
                    var contents = annot.GetContents()?.GetValue() ?? "";

                    allAnnotations.Add(new
                    {
                        page = i,
                        type = annotType,
                        x = rect?.GetX(),
                        y = rect?.GetY(),
                        width = rect?.GetWidth(),
                        height = rect?.GetHeight(),
                        contents = contents
                    });
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                total_annotations = allAnnotations.Count,
                annotations = allAnnotations
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
    /// Remove all annotations from PDF
    /// </summary>
    public async Task<string> RemoveAnnotationsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");
        var annotationType = GetString(args, "type", "all"); // all, highlight, text, stamp, link, etc.
        var pages = GetIntArray(args, "pages"); // Empty = all pages

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_no_annotations.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var totalPages = pdfDoc.GetNumberOfPages();
            var pagesToProcess = pages.Length > 0
                ? pages.Where(p => p >= 1 && p <= totalPages).ToArray()
                : Enumerable.Range(1, totalPages).ToArray();

            int removedCount = 0;

            foreach (var pageNum in pagesToProcess)
            {
                var page = pdfDoc.GetPage(pageNum);
                var annotations = page.GetAnnotations().ToList();

                foreach (var annot in annotations)
                {
                    var annotSubtype = annot.GetSubtype()?.GetValue() ?? "";

                    bool shouldRemove = annotationType == "all" ||
                        (annotationType == "highlight" && annotSubtype == "Highlight") ||
                        (annotationType == "text" && annotSubtype == "Text") ||
                        (annotationType == "stamp" && annotSubtype == "Stamp") ||
                        (annotationType == "link" && annotSubtype == "Link") ||
                        (annotationType == "underline" && annotSubtype == "Underline") ||
                        (annotationType == "strikeout" && annotSubtype == "StrikeOut") ||
                        (annotationType == "freetext" && annotSubtype == "FreeText");

                    if (shouldRemove)
                    {
                        page.RemoveAnnotation(annot);
                        removedCount++;
                    }
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Removed {removedCount} annotations",
                output_file = outputPath,
                removed_count = removedCount,
                type_filter = annotationType
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
    /// Flatten annotations (make them part of the PDF content)
    /// </summary>
    public async Task<string> FlattenAnnotationsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        if (string.IsNullOrEmpty(outputName))
        {
            outputName = IOPath.GetFileNameWithoutExtension(inputFile) + "_flattened.pdf";
        }
        var outputPath = ResolvePath(outputName, true);

        try
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);

            var form = iText.Forms.PdfAcroForm.GetAcroForm(pdfDoc, false);
            if (form != null)
            {
                form.FlattenFields();
            }

            // Flatten annotations on each page
            int flattenedCount = 0;
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                var annotations = page.GetAnnotations().ToList();

                foreach (var annot in annotations)
                {
                    // For text markup annotations, draw them as content
                    var subtype = annot.GetSubtype()?.GetValue();
                    if (subtype == "Highlight" || subtype == "Underline" || subtype == "StrikeOut")
                    {
                        // These are already visible, just remove the annotation object
                        page.RemoveAnnotation(annot);
                        flattenedCount++;
                    }
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Annotations flattened successfully",
                output_file = outputPath,
                flattened_count = flattenedCount
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

    private Color GetAnnotationColor(string colorName)
    {
        return colorName.ToLower() switch
        {
            "yellow" => new DeviceRgb(255, 255, 0),
            "green" => new DeviceRgb(0, 255, 0),
            "blue" => new DeviceRgb(0, 0, 255),
            "red" => new DeviceRgb(255, 0, 0),
            "pink" => new DeviceRgb(255, 192, 203),
            "orange" => new DeviceRgb(255, 165, 0),
            "cyan" => new DeviceRgb(0, 255, 255),
            "magenta" => new DeviceRgb(255, 0, 255),
            "white" => new DeviceRgb(255, 255, 255),
            "black" => new DeviceRgb(0, 0, 0),
            "gray" => new DeviceRgb(128, 128, 128),
            _ => new DeviceRgb(255, 255, 0) // Default yellow
        };
    }

    #endregion

    private class RedactionArea
    {
        public int Page { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    private List<RedactionArea> GetRedactionAreas(Dictionary<string, object> args, string key)
    {
        var result = new List<RedactionArea>();
        if (!args.TryGetValue(key, out var value)) return result;

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                result.Add(new RedactionArea
                {
                    Page = item.GetProperty("page").GetInt32(),
                    X = item.GetProperty("x").GetSingle(),
                    Y = item.GetProperty("y").GetSingle(),
                    Width = item.GetProperty("width").GetSingle(),
                    Height = item.GetProperty("height").GetSingle()
                });
            }
        }
        return result;
    }

    private bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetBoolean();
        if (value is bool b) return b;
        return bool.TryParse(value?.ToString(), out var result) ? result : defaultValue;
    }

    // Helper methods
    private string ResolvePath(string path, bool isOutput)
    {
        if (IOPath.IsPathRooted(path)) return path;

        if (isOutput)
            return IOPath.Combine(_generatedPath, path);

        // Check generated first, then uploaded
        var genPath = IOPath.Combine(_generatedPath, path);
        if (File.Exists(genPath)) return genPath;

        var upPath = IOPath.Combine(_uploadedPath, path);
        if (File.Exists(upPath)) return upPath;

        return genPath; // Default to generated path
    }

    private string GetString(Dictionary<string, object> args, string key, string defaultValue = "")
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetString() ?? defaultValue;
        return value?.ToString() ?? defaultValue;
    }

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetInt32();
        return Convert.ToInt32(value);
    }

    private float GetFloat(Dictionary<string, object> args, string key, float defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetSingle();
        return Convert.ToSingle(value);
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
}
