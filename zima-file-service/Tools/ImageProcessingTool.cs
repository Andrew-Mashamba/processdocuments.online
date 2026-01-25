using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using Tesseract;

namespace ZimaFileService.Tools;

/// <summary>
/// Image Processing Tools - Watermark, redact, manipulate images
/// </summary>
public class ImageProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;
    private readonly string _tessdataPath;

    public ImageProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
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
                if (File.Exists(Path.Combine(path, "eng.traineddata")))
                    return path;
            }
        }

        return Path.Combine(FileManager.Instance.WorkingDirectory, "tessdata");
    }

    #region Text Watermark

    /// <summary>
    /// Add text watermark to image
    /// </summary>
    public async Task<string> AddTextWatermarkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var watermarkText = GetString(args, "text", "WATERMARK");
        var position = GetString(args, "position", "center"); // center, top-left, top-right, bottom-left, bottom-right, tile
        var fontSize = GetInt(args, "font_size", 48);
        var color = GetString(args, "color", "gray");
        var opacity = GetFloat(args, "opacity", 0.5f);
        var rotation = GetFloat(args, "rotation", 0); // degrees
        var fontFamily = GetString(args, "font", "Arial");
        var outputName = GetString(args, "output_file", "watermarked.png");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;

        // Draw original image
        canvas.DrawBitmap(originalBitmap, 0, 0);

        // Parse color and apply opacity
        var watermarkColor = ParseColor(color);
        watermarkColor = watermarkColor.WithAlpha((byte)(opacity * 255));

        var paint = new SKPaint
        {
            Color = watermarkColor,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(fontFamily),
            TextAlign = SKTextAlign.Center
        };

        var textBounds = new SKRect();
        paint.MeasureText(watermarkText, ref textBounds);

        if (position == "tile")
        {
            // Tile watermark across entire image
            AddTiledWatermark(canvas, watermarkText, paint, originalBitmap.Width, originalBitmap.Height, rotation);
        }
        else
        {
            // Single watermark at specified position
            var (x, y) = GetWatermarkPosition(position, originalBitmap.Width, originalBitmap.Height, textBounds);

            canvas.Save();
            canvas.Translate(x, y);
            if (rotation != 0)
                canvas.RotateDegrees(rotation);
            canvas.DrawText(watermarkText, 0, textBounds.Height / 2, paint);
            canvas.Restore();
        }

        // Save output
        using var image = surface.Snapshot();
        var format = Path.GetExtension(outputPath).ToLower() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            ".gif" => SKEncodedImageFormat.Gif,
            _ => SKEncodedImageFormat.Png
        };

        using var data = image.Encode(format, 90);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Watermark added successfully",
            output_file = outputPath,
            watermark_text = watermarkText,
            position = position,
            opacity = opacity
        });
    }

    private void AddTiledWatermark(SKCanvas canvas, string text, SKPaint paint, int width, int height, float rotation)
    {
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);

        var spacing = Math.Max(textBounds.Width, textBounds.Height) * 1.5f;

        for (float y = -height; y < height * 2; y += spacing)
        {
            for (float x = -width; x < width * 2; x += spacing)
            {
                canvas.Save();
                canvas.Translate(x, y);
                if (rotation != 0)
                    canvas.RotateDegrees(rotation);
                else
                    canvas.RotateDegrees(-45); // Default diagonal for tiles
                canvas.DrawText(text, 0, 0, paint);
                canvas.Restore();
            }
        }
    }

    private (float x, float y) GetWatermarkPosition(string position, int imageWidth, int imageHeight, SKRect textBounds)
    {
        var padding = 20;
        return position.ToLower() switch
        {
            "top-left" => (textBounds.Width / 2 + padding, textBounds.Height + padding),
            "top-right" => (imageWidth - textBounds.Width / 2 - padding, textBounds.Height + padding),
            "bottom-left" => (textBounds.Width / 2 + padding, imageHeight - padding),
            "bottom-right" => (imageWidth - textBounds.Width / 2 - padding, imageHeight - padding),
            "top-center" => (imageWidth / 2f, textBounds.Height + padding),
            "bottom-center" => (imageWidth / 2f, imageHeight - padding),
            _ => (imageWidth / 2f, imageHeight / 2f) // center
        };
    }

    /// <summary>
    /// Add image watermark to another image
    /// </summary>
    public async Task<string> AddImageWatermarkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var watermarkFile = GetString(args, "watermark_image");
        var position = GetString(args, "position", "bottom-right");
        var opacity = GetFloat(args, "opacity", 0.5f);
        var scale = GetFloat(args, "scale", 0.2f); // Scale relative to main image
        var outputName = GetString(args, "output_file", "watermarked.png");

        var inputPath = ResolvePath(inputFile, false);
        var watermarkPath = ResolvePath(watermarkFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        using var watermarkStream = File.OpenRead(watermarkPath);
        using var watermarkBitmap = SKBitmap.Decode(watermarkStream);

        if (originalBitmap == null || watermarkBitmap == null)
            throw new ArgumentException("Could not decode image files");

        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;

        // Draw original image
        canvas.DrawBitmap(originalBitmap, 0, 0);

        // Calculate watermark size
        var wmWidth = (int)(originalBitmap.Width * scale);
        var wmHeight = (int)(watermarkBitmap.Height * ((float)wmWidth / watermarkBitmap.Width));

        // Get position
        var (x, y) = GetImageWatermarkPosition(position, originalBitmap.Width, originalBitmap.Height, wmWidth, wmHeight);

        // Draw watermark with opacity
        var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)(opacity * 255))
        };

        var destRect = new SKRect(x, y, x + wmWidth, y + wmHeight);
        canvas.DrawBitmap(watermarkBitmap, destRect, paint);

        // Save output
        using var image = surface.Snapshot();
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 90);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Image watermark added successfully",
            output_file = outputPath,
            position = position,
            opacity = opacity,
            scale = scale
        });
    }

    private (float x, float y) GetImageWatermarkPosition(string position, int imageWidth, int imageHeight, int wmWidth, int wmHeight)
    {
        var padding = 20;
        return position.ToLower() switch
        {
            "top-left" => (padding, padding),
            "top-right" => (imageWidth - wmWidth - padding, padding),
            "bottom-left" => (padding, imageHeight - wmHeight - padding),
            "bottom-right" => (imageWidth - wmWidth - padding, imageHeight - wmHeight - padding),
            "center" => ((imageWidth - wmWidth) / 2f, (imageHeight - wmHeight) / 2f),
            _ => (imageWidth - wmWidth - padding, imageHeight - wmHeight - padding)
        };
    }

    #endregion

    #region Redact Strings

    /// <summary>
    /// Redact text patterns from an image using OCR to find text locations
    /// </summary>
    public async Task<string> RedactStringsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var patterns = GetStringArray(args, "patterns"); // Text patterns to redact
        var useRegex = GetBool(args, "use_regex", false);
        var redactColor = GetString(args, "redact_color", "black");
        var language = GetString(args, "language", "eng");
        var outputName = GetString(args, "output_file", "redacted.png");

        if (patterns.Length == 0)
            throw new ArgumentException("At least one pattern to redact is required");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        // Validate tessdata
        if (!Directory.Exists(_tessdataPath) || !File.Exists(Path.Combine(_tessdataPath, $"{language}.traineddata")))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Language data '{language}.traineddata' not found for OCR",
                tessdata_path = _tessdataPath
            });
        }

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        // Use Tesseract to find text locations
        var redactRegions = new List<SKRect>();
        int matchCount = 0;

        try
        {
            using var engine = new TesseractEngine(_tessdataPath, language, EngineMode.Default);
            using var pix = Pix.LoadFromFile(inputPath);
            using var page = engine.Process(pix);

            // Get word-level results with bounding boxes
            using var iter = page.GetIterator();
            iter.Begin();

            do
            {
                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                {
                    var word = iter.GetText(PageIteratorLevel.Word)?.Trim() ?? "";

                    if (string.IsNullOrEmpty(word)) continue;

                    bool shouldRedact = false;

                    foreach (var pattern in patterns)
                    {
                        if (useRegex)
                        {
                            if (Regex.IsMatch(word, pattern, RegexOptions.IgnoreCase))
                            {
                                shouldRedact = true;
                                break;
                            }
                        }
                        else
                        {
                            if (word.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                            {
                                shouldRedact = true;
                                break;
                            }
                        }
                    }

                    if (shouldRedact)
                    {
                        redactRegions.Add(new SKRect(bounds.X1, bounds.Y1, bounds.X2, bounds.Y2));
                        matchCount++;
                    }
                }
            } while (iter.Next(PageIteratorLevel.Word));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"OCR failed: {ex.Message}"
            });
        }

        // Create output image with redactions
        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;

        // Draw original image
        canvas.DrawBitmap(originalBitmap, 0, 0);

        // Draw redaction boxes
        var redactPaint = new SKPaint
        {
            Color = ParseColor(redactColor),
            Style = SKPaintStyle.Fill
        };

        foreach (var region in redactRegions)
        {
            // Add small padding
            var paddedRegion = new SKRect(
                region.Left - 2,
                region.Top - 2,
                region.Right + 2,
                region.Bottom + 2
            );
            canvas.DrawRect(paddedRegion, redactPaint);
        }

        // Save output
        using var image = surface.Snapshot();
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 95);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Redacted {matchCount} text matches",
            output_file = outputPath,
            patterns_searched = patterns,
            matches_found = matchCount,
            regions_redacted = redactRegions.Count,
            use_regex = useRegex
        });
    }

    /// <summary>
    /// Redact specific regions from an image (manual coordinates)
    /// </summary>
    public async Task<string> RedactRegionsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var regions = GetRegionsArray(args, "regions"); // Array of {x, y, width, height}
        var redactColor = GetString(args, "redact_color", "black");
        var blurInstead = GetBool(args, "blur", false);
        var blurRadius = GetInt(args, "blur_radius", 20);
        var outputName = GetString(args, "output_file", "redacted.png");

        if (regions.Count == 0)
            throw new ArgumentException("At least one region to redact is required");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;

        // Draw original image
        canvas.DrawBitmap(originalBitmap, 0, 0);

        if (blurInstead)
        {
            // Apply blur to regions
            foreach (var region in regions)
            {
                var blurFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
                var blurPaint = new SKPaint { ImageFilter = blurFilter };

                canvas.Save();
                canvas.ClipRect(region);
                canvas.DrawBitmap(originalBitmap, 0, 0, blurPaint);
                canvas.Restore();
            }
        }
        else
        {
            // Fill regions with solid color
            var redactPaint = new SKPaint
            {
                Color = ParseColor(redactColor),
                Style = SKPaintStyle.Fill
            };

            foreach (var region in regions)
            {
                canvas.DrawRect(region, redactPaint);
            }
        }

        // Save output
        using var image = surface.Snapshot();
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 95);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Redacted {regions.Count} regions",
            output_file = outputPath,
            regions_count = regions.Count,
            method = blurInstead ? "blur" : "fill"
        });
    }

    /// <summary>
    /// Auto-detect and redact sensitive information (emails, phone numbers, SSN, etc.)
    /// </summary>
    public async Task<string> RedactSensitiveInfoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var redactTypes = GetStringArray(args, "types"); // email, phone, ssn, credit_card, date, all
        var redactColor = GetString(args, "redact_color", "black");
        var language = GetString(args, "language", "eng");
        var outputName = GetString(args, "output_file", "redacted.png");

        if (redactTypes.Length == 0)
            redactTypes = new[] { "all" };

        // Build regex patterns for each type
        var patterns = new List<string>();

        if (redactTypes.Contains("all") || redactTypes.Contains("email"))
            patterns.Add(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");

        if (redactTypes.Contains("all") || redactTypes.Contains("phone"))
        {
            patterns.Add(@"\+?1?[-.\s]?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
            patterns.Add(@"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}");
        }

        if (redactTypes.Contains("all") || redactTypes.Contains("ssn"))
            patterns.Add(@"\d{3}[-\s]?\d{2}[-\s]?\d{4}");

        if (redactTypes.Contains("all") || redactTypes.Contains("credit_card"))
            patterns.Add(@"\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}");

        if (redactTypes.Contains("all") || redactTypes.Contains("date"))
        {
            patterns.Add(@"\d{1,2}[/-]\d{1,2}[/-]\d{2,4}");
            patterns.Add(@"\d{4}[/-]\d{1,2}[/-]\d{1,2}");
        }

        // Use the RedactStrings method with regex patterns
        var newArgs = new Dictionary<string, object>
        {
            ["file"] = inputFile,
            ["patterns"] = JsonSerializer.SerializeToElement(patterns.ToArray()),
            ["use_regex"] = true,
            ["redact_color"] = redactColor,
            ["language"] = language,
            ["output_file"] = outputName
        };

        var result = await RedactStringsAsync(newArgs);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Add additional info to the result
        if (resultObj.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = resultObj.GetProperty("message").GetString(),
                output_file = resultObj.GetProperty("output_file").GetString(),
                redaction_types = redactTypes,
                patterns_used = patterns.Count,
                matches_found = resultObj.GetProperty("matches_found").GetInt32()
            });
        }

        return result;
    }

    #endregion

    #region Image Manipulation

    /// <summary>
    /// Resize an image
    /// </summary>
    public async Task<string> ResizeImageAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var width = GetInt(args, "width", 0);
        var height = GetInt(args, "height", 0);
        var maintainAspect = GetBool(args, "maintain_aspect", true);
        var outputName = GetString(args, "output_file", "resized.png");

        if (width == 0 && height == 0)
            throw new ArgumentException("Either width or height must be specified");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        int newWidth, newHeight;

        if (maintainAspect)
        {
            var aspectRatio = (float)originalBitmap.Width / originalBitmap.Height;
            if (width > 0 && height > 0)
            {
                // Use the dimension that results in smaller image
                var widthBased = (width, (int)(width / aspectRatio));
                var heightBased = ((int)(height * aspectRatio), height);
                (newWidth, newHeight) = widthBased.Item1 * widthBased.Item2 < heightBased.Item1 * heightBased.Item2
                    ? widthBased : heightBased;
            }
            else if (width > 0)
            {
                newWidth = width;
                newHeight = (int)(width / aspectRatio);
            }
            else
            {
                newWidth = (int)(height * aspectRatio);
                newHeight = height;
            }
        }
        else
        {
            newWidth = width > 0 ? width : originalBitmap.Width;
            newHeight = height > 0 ? height : originalBitmap.Height;
        }

        using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resizedBitmap);
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 90);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Image resized successfully",
            output_file = outputPath,
            original_dimensions = $"{originalBitmap.Width}x{originalBitmap.Height}",
            new_dimensions = $"{newWidth}x{newHeight}"
        });
    }

    /// <summary>
    /// Convert image format
    /// </summary>
    public async Task<string> ConvertImageFormatAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var targetFormat = GetString(args, "format", "png"); // png, jpg, webp, gif, bmp
        var quality = GetInt(args, "quality", 90);
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);
        if (string.IsNullOrEmpty(outputName))
            outputName = Path.GetFileNameWithoutExtension(inputPath) + "." + targetFormat;
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var bitmap = SKBitmap.Decode(inputStream);

        if (bitmap == null)
            throw new ArgumentException("Could not decode image file");

        using var image = SKImage.FromBitmap(bitmap);
        var format = targetFormat.ToLower() switch
        {
            "jpg" or "jpeg" => SKEncodedImageFormat.Jpeg,
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            "gif" => SKEncodedImageFormat.Gif,
            "bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Png
        };

        using var data = image.Encode(format, quality);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Image converted to {targetFormat.ToUpper()}",
            output_file = outputPath,
            format = targetFormat,
            quality = quality
        });
    }

    /// <summary>
    /// Crop an image
    /// </summary>
    public async Task<string> CropImageAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var x = GetInt(args, "x", 0);
        var y = GetInt(args, "y", 0);
        var width = GetInt(args, "width");
        var height = GetInt(args, "height");
        var outputName = GetString(args, "output_file", "cropped.png");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        // Validate crop region
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x + width > originalBitmap.Width) width = originalBitmap.Width - x;
        if (y + height > originalBitmap.Height) height = originalBitmap.Height - y;

        var cropRect = new SKRectI(x, y, x + width, y + height);

        using var croppedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(croppedBitmap);
        canvas.DrawBitmap(originalBitmap, cropRect, new SKRect(0, 0, width, height));

        using var image = SKImage.FromBitmap(croppedBitmap);
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 95);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Image cropped successfully",
            output_file = outputPath,
            crop_region = new { x, y, width, height },
            original_dimensions = $"{originalBitmap.Width}x{originalBitmap.Height}",
            new_dimensions = $"{width}x{height}"
        });
    }

    /// <summary>
    /// Rotate an image
    /// </summary>
    public async Task<string> RotateImageAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var degrees = GetFloat(args, "degrees", 90);
        var outputName = GetString(args, "output_file", "rotated.png");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var inputStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
            throw new ArgumentException("Could not decode image file");

        // Calculate new dimensions for rotated image
        var radians = degrees * Math.PI / 180;
        var sin = Math.Abs(Math.Sin(radians));
        var cos = Math.Abs(Math.Cos(radians));
        var newWidth = (int)(originalBitmap.Width * cos + originalBitmap.Height * sin);
        var newHeight = (int)(originalBitmap.Width * sin + originalBitmap.Height * cos);

        using var surface = SKSurface.Create(new SKImageInfo(newWidth, newHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        canvas.Translate(newWidth / 2f, newHeight / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-originalBitmap.Width / 2f, -originalBitmap.Height / 2f);
        canvas.DrawBitmap(originalBitmap, 0, 0);

        using var image = surface.Snapshot();
        var format = GetImageFormat(outputPath);
        using var data = image.Encode(format, 95);
        await using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Image rotated {degrees} degrees",
            output_file = outputPath,
            rotation = degrees,
            original_dimensions = $"{originalBitmap.Width}x{originalBitmap.Height}",
            new_dimensions = $"{newWidth}x{newHeight}"
        });
    }

    #endregion

    #region Helper Methods

    private SKColor ParseColor(string colorName)
    {
        return colorName.ToLower() switch
        {
            "black" => SKColors.Black,
            "white" => SKColors.White,
            "red" => SKColors.Red,
            "green" => SKColors.Green,
            "blue" => SKColors.Blue,
            "yellow" => SKColors.Yellow,
            "gray" or "grey" => SKColors.Gray,
            "lightgray" or "lightgrey" => SKColors.LightGray,
            "darkgray" or "darkgrey" => SKColors.DarkGray,
            "orange" => SKColors.Orange,
            "purple" => SKColors.Purple,
            "pink" => SKColors.Pink,
            "cyan" => SKColors.Cyan,
            "magenta" => SKColors.Magenta,
            "transparent" => SKColors.Transparent,
            _ when colorName.StartsWith("#") => SKColor.Parse(colorName),
            _ => SKColors.Black
        };
    }

    private SKEncodedImageFormat GetImageFormat(string path)
    {
        return Path.GetExtension(path).ToLower() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            ".gif" => SKEncodedImageFormat.Gif,
            ".bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Png
        };
    }

    private List<SKRect> GetRegionsArray(Dictionary<string, object> args, string key)
    {
        var regions = new List<SKRect>();
        if (!args.TryGetValue(key, out var value)) return regions;

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                var x = item.TryGetProperty("x", out var xProp) ? xProp.GetInt32() : 0;
                var y = item.TryGetProperty("y", out var yProp) ? yProp.GetInt32() : 0;
                var w = item.TryGetProperty("width", out var wProp) ? wProp.GetInt32() : 100;
                var h = item.TryGetProperty("height", out var hProp) ? hProp.GetInt32() : 100;
                regions.Add(new SKRect(x, y, x + w, y + h));
            }
        }

        return regions;
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

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number) return je.GetInt32();
            if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), out var parsed)) return parsed;
        }
        if (value is int i) return i;
        return int.TryParse(value?.ToString(), out var result) ? result : defaultValue;
    }

    private float GetFloat(Dictionary<string, object> args, string key, float defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number) return (float)je.GetDouble();
            if (je.ValueKind == JsonValueKind.String && float.TryParse(je.GetString(), out var parsed)) return parsed;
        }
        if (value is float f) return f;
        if (value is double d) return (float)d;
        return float.TryParse(value?.ToString(), out var result) ? result : defaultValue;
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
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        return Array.Empty<string>();
    }

    #endregion
}
