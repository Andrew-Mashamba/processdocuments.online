using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Tesseract;

namespace ZimaFileService.Tools;

/// <summary>
/// OCR Processing Tools - Extract text from scanned PDFs and images using Tesseract OCR
/// </summary>
public class OcrProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;
    private readonly string _tessdataPath;

    public OcrProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;

        // Look for tessdata in multiple locations
        _tessdataPath = FindTessdataPath();
    }

    private string FindTessdataPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(FileManager.Instance.WorkingDirectory, "tessdata"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
            "/usr/share/tesseract-ocr/4.00/tessdata",
            "/usr/share/tesseract-ocr/5/tessdata",
            "/usr/local/share/tessdata",
            "/opt/homebrew/share/tessdata",
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? ""
        };

        foreach (var path in possiblePaths)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                // Check if eng.traineddata exists
                if (File.Exists(Path.Combine(path, "eng.traineddata")))
                {
                    return path;
                }
            }
        }

        // Default to working directory tessdata (will be created if needed)
        return Path.Combine(FileManager.Instance.WorkingDirectory, "tessdata");
    }

    /// <summary>
    /// OCR a PDF file - extract text from scanned/image-based PDF
    /// </summary>
    public async Task<string> OcrPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var language = GetString(args, "language", "eng"); // eng, fra, deu, spa, etc.
        var outputFormat = GetString(args, "output_format", "text"); // text, json, searchable_pdf
        var pages = GetIntArray(args, "pages"); // empty = all pages
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);

        // Validate tessdata exists
        if (!Directory.Exists(_tessdataPath))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Tessdata directory not found. Please download language data from https://github.com/tesseract-ocr/tessdata and place in: {_tessdataPath}",
                setup_instructions = new[]
                {
                    $"1. Create directory: {_tessdataPath}",
                    $"2. Download eng.traineddata from https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata",
                    $"3. Place the file in {_tessdataPath}"
                }
            });
        }

        var langFile = Path.Combine(_tessdataPath, $"{language}.traineddata");
        if (!File.Exists(langFile))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Language data '{language}.traineddata' not found in {_tessdataPath}",
                available_languages = GetAvailableLanguages(),
                download_url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata"
            });
        }

        try
        {
            using var pdfReader = new PdfReader(inputPath);
            using var pdfDoc = new PdfDocument(pdfReader);

            var totalPages = pdfDoc.GetNumberOfPages();
            var pagesToProcess = pages.Length > 0
                ? pages.Where(p => p >= 1 && p <= totalPages).ToArray()
                : Enumerable.Range(1, totalPages).ToArray();

            var results = new List<PageOcrResult>();
            var allText = new StringBuilder();

            using var engine = new TesseractEngine(_tessdataPath, language, EngineMode.Default);

            foreach (var pageNum in pagesToProcess)
            {
                var page = pdfDoc.GetPage(pageNum);
                var pageText = new StringBuilder();

                // First try to extract existing text
                var existingText = PdfTextExtractor.GetTextFromPage(page);

                if (!string.IsNullOrWhiteSpace(existingText) && existingText.Length > 50)
                {
                    // Page already has extractable text
                    pageText.Append(existingText);
                }
                else
                {
                    // Extract images from page and OCR them
                    var images = ExtractImagesFromPage(pdfDoc, pageNum);

                    foreach (var imageBytes in images)
                    {
                        try
                        {
                            using var pix = Pix.LoadFromMemory(imageBytes);
                            using var ocrPage = engine.Process(pix);
                            var text = ocrPage.GetText();
                            var confidence = ocrPage.GetMeanConfidence();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                pageText.AppendLine(text.Trim());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"OCR error on page {pageNum}: {ex.Message}");
                        }
                    }
                }

                var pageResult = new PageOcrResult
                {
                    Page = pageNum,
                    Text = pageText.ToString().Trim(),
                    WordCount = pageText.ToString().Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length
                };

                results.Add(pageResult);
                allText.AppendLine($"--- Page {pageNum} ---");
                allText.AppendLine(pageResult.Text);
                allText.AppendLine();
            }

            // Generate output based on format
            if (outputFormat == "json")
            {
                if (string.IsNullOrEmpty(outputName)) outputName = "ocr_result.json";
                var outputPath = ResolvePath(outputName, true);

                var jsonResult = new
                {
                    source_file = inputPath,
                    total_pages = totalPages,
                    processed_pages = pagesToProcess.Length,
                    language = language,
                    pages = results.Select(r => new { r.Page, r.Text, r.WordCount })
                };

                await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions { WriteIndented = true }));

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"OCR completed for {pagesToProcess.Length} pages",
                    output_file = outputPath,
                    total_words = results.Sum(r => r.WordCount),
                    language = language
                });
            }
            else if (outputFormat == "searchable_pdf")
            {
                // Create a text file with the OCR results (searchable PDF requires more complex implementation)
                if (string.IsNullOrEmpty(outputName)) outputName = "ocr_text.txt";
                var outputPath = ResolvePath(outputName, true);
                await File.WriteAllTextAsync(outputPath, allText.ToString());

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"OCR text extracted from {pagesToProcess.Length} pages",
                    output_file = outputPath,
                    total_words = results.Sum(r => r.WordCount),
                    note = "Full searchable PDF creation requires additional libraries. Text file created instead."
                });
            }
            else // text
            {
                if (string.IsNullOrEmpty(outputName)) outputName = "ocr_result.txt";
                var outputPath = ResolvePath(outputName, true);
                await File.WriteAllTextAsync(outputPath, allText.ToString());

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"OCR completed for {pagesToProcess.Length} pages",
                    output_file = outputPath,
                    total_words = results.Sum(r => r.WordCount),
                    language = language,
                    preview = allText.ToString().Substring(0, Math.Min(500, allText.Length)) + "..."
                });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message,
                tessdata_path = _tessdataPath
            });
        }
    }

    /// <summary>
    /// OCR an image file (JPG, PNG, TIFF, BMP)
    /// </summary>
    public async Task<string> OcrImageAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var language = GetString(args, "language", "eng");
        var outputName = GetString(args, "output_file", "ocr_result.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        // Validate tessdata
        if (!Directory.Exists(_tessdataPath) || !File.Exists(Path.Combine(_tessdataPath, $"{language}.traineddata")))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Language data '{language}.traineddata' not found",
                tessdata_path = _tessdataPath,
                download_url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata"
            });
        }

        try
        {
            using var engine = new TesseractEngine(_tessdataPath, language, EngineMode.Default);
            using var img = Pix.LoadFromFile(inputPath);
            using var page = engine.Process(img);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            await File.WriteAllTextAsync(outputPath, text);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Image OCR completed",
                output_file = outputPath,
                confidence = $"{confidence * 100:F1}%",
                word_count = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length,
                preview = text.Substring(0, Math.Min(300, text.Length)) + (text.Length > 300 ? "..." : "")
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
    /// Get available OCR languages
    /// </summary>
    public async Task<string> GetOcrLanguagesAsync(Dictionary<string, object> args)
    {
        await Task.CompletedTask;

        var languages = GetAvailableLanguages();

        return JsonSerializer.Serialize(new
        {
            success = true,
            tessdata_path = _tessdataPath,
            tessdata_exists = Directory.Exists(_tessdataPath),
            available_languages = languages,
            count = languages.Length,
            common_languages = new Dictionary<string, string>
            {
                ["eng"] = "English",
                ["fra"] = "French",
                ["deu"] = "German",
                ["spa"] = "Spanish",
                ["ita"] = "Italian",
                ["por"] = "Portuguese",
                ["rus"] = "Russian",
                ["chi_sim"] = "Chinese (Simplified)",
                ["chi_tra"] = "Chinese (Traditional)",
                ["jpn"] = "Japanese",
                ["kor"] = "Korean",
                ["ara"] = "Arabic"
            },
            download_instructions = new[]
            {
                "Download language files from: https://github.com/tesseract-ocr/tessdata",
                $"Place .traineddata files in: {_tessdataPath}"
            }
        });
    }

    /// <summary>
    /// Batch OCR multiple files
    /// </summary>
    public async Task<string> BatchOcrAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var language = GetString(args, "language", "eng");
        var outputFolder = GetString(args, "output_folder", "ocr_results");

        if (files.Length == 0)
        {
            return JsonSerializer.Serialize(new { success = false, error = "No files provided" });
        }

        var outputDir = Path.Combine(_generatedPath, outputFolder);
        Directory.CreateDirectory(outputDir);

        var results = new List<object>();
        int successful = 0, failed = 0;

        foreach (var file in files)
        {
            var inputPath = ResolvePath(file, false);
            var extension = Path.GetExtension(inputPath).ToLower();
            var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + "_ocr.txt");

            try
            {
                string resultJson;
                if (extension == ".pdf")
                {
                    resultJson = await OcrPdfAsync(new Dictionary<string, object>
                    {
                        ["file"] = file,
                        ["language"] = language,
                        ["output_file"] = outputFile,
                        ["output_format"] = "text"
                    });
                }
                else
                {
                    resultJson = await OcrImageAsync(new Dictionary<string, object>
                    {
                        ["file"] = file,
                        ["language"] = language,
                        ["output_file"] = outputFile
                    });
                }

                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                if (result.GetProperty("success").GetBoolean())
                {
                    successful++;
                    results.Add(new { file = file, status = "success", output = outputFile });
                }
                else
                {
                    failed++;
                    results.Add(new { file = file, status = "failed", error = result.GetProperty("error").GetString() });
                }
            }
            catch (Exception ex)
            {
                failed++;
                results.Add(new { file = file, status = "failed", error = ex.Message });
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = failed == 0,
            message = $"Processed {files.Length} files: {successful} successful, {failed} failed",
            output_folder = outputDir,
            results = results
        });
    }

    private List<byte[]> ExtractImagesFromPage(PdfDocument pdfDoc, int pageNum)
    {
        var images = new List<byte[]>();

        try
        {
            var page = pdfDoc.GetPage(pageNum);
            var resources = page.GetResources();
            var xObjects = resources?.GetResource(iText.Kernel.Pdf.PdfName.XObject);

            if (xObjects == null) return images;

            foreach (var name in xObjects.KeySet())
            {
                try
                {
                    var xObject = xObjects.GetAsStream(name);
                    if (xObject == null) continue;

                    var subtype = xObject.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);
                    if (subtype != null && subtype.Equals(iText.Kernel.Pdf.PdfName.Image))
                    {
                        var imageBytes = xObject.GetBytes();
                        if (imageBytes != null && imageBytes.Length > 0)
                        {
                            // Try to get the decoded image
                            var imageXObject = new iText.Kernel.Pdf.Xobject.PdfImageXObject(xObject);
                            var decodedBytes = imageXObject.GetImageBytes();
                            if (decodedBytes != null && decodedBytes.Length > 0)
                            {
                                images.Add(decodedBytes);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip problematic images
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting images from page {pageNum}: {ex.Message}");
        }

        return images;
    }

    private string[] GetAvailableLanguages()
    {
        if (!Directory.Exists(_tessdataPath))
            return Array.Empty<string>();

        return Directory.GetFiles(_tessdataPath, "*.traineddata")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(l => l)
            .ToArray();
    }

    private class PageOcrResult
    {
        public int Page { get; set; }
        public string Text { get; set; } = "";
        public int WordCount { get; set; }
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
}
