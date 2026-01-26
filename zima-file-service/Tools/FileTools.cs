using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZimaFileService.Tools;

/// <summary>
/// File Tools - Compression, archiving, file analysis, comparison, etc.
/// </summary>
public class FileTools
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public FileTools()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    #region Compression

    /// <summary>
    /// Create a ZIP archive from files or folders
    /// </summary>
    public async Task<string> CreateZipAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputFile = GetString(args, "output_file", "archive.zip");
        var compressionLevel = GetString(args, "compression", "optimal"); // optimal, fastest, no_compression

        var outputPath = Path.Combine(_generatedPath, outputFile);
        var tempDir = Path.Combine(_generatedPath, $"temp_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var addedFiles = new List<string>();

            foreach (var file in files)
            {
                var sourcePath = ResolvePath(file, false);
                if (File.Exists(sourcePath))
                {
                    var destPath = Path.Combine(tempDir, Path.GetFileName(sourcePath));
                    File.Copy(sourcePath, destPath, true);
                    addedFiles.Add(Path.GetFileName(sourcePath));
                }
                else if (Directory.Exists(sourcePath))
                {
                    var dirName = Path.GetFileName(sourcePath);
                    CopyDirectory(sourcePath, Path.Combine(tempDir, dirName));
                    addedFiles.Add(dirName + "/");
                }
            }

            var level = compressionLevel switch
            {
                "fastest" => CompressionLevel.Fastest,
                "no_compression" => CompressionLevel.NoCompression,
                _ => CompressionLevel.Optimal
            };

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            ZipFile.CreateFromDirectory(tempDir, outputPath, level, false);

            var fileInfo = new FileInfo(outputPath);

            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                file_count = addedFiles.Count,
                files = addedFiles,
                size_bytes = fileInfo.Length,
                size_formatted = FormatFileSize(fileInfo.Length)
            });
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Extract a ZIP archive
    /// </summary>
    public async Task<string> ExtractZipAsync(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");
        var outputFolder = GetString(args, "output_folder", null);

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var outputPath = string.IsNullOrEmpty(outputFolder)
            ? Path.Combine(_generatedPath, Path.GetFileNameWithoutExtension(file))
            : Path.Combine(_generatedPath, outputFolder);

        Directory.CreateDirectory(outputPath);

        ZipFile.ExtractToDirectory(sourcePath, outputPath, true);

        var extractedFiles = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(outputPath, f))
            .ToList();

        return JsonSerializer.Serialize(new {
            success = true,
            output_folder = outputPath,
            file_count = extractedFiles.Count,
            files = extractedFiles.Take(50), // Limit output
            total_files = extractedFiles.Count
        });
    }

    /// <summary>
    /// Compress a file using GZIP
    /// </summary>
    public async Task<string> CompressGzipAsync(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");
        var outputFile = GetString(args, "output_file", null);

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var outputPath = string.IsNullOrEmpty(outputFile)
            ? sourcePath + ".gz"
            : Path.Combine(_generatedPath, outputFile);

        var originalSize = new FileInfo(sourcePath).Length;

        await using var inputStream = File.OpenRead(sourcePath);
        await using var outputStream = File.Create(outputPath);
        await using var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal);

        await inputStream.CopyToAsync(gzipStream);

        var compressedSize = new FileInfo(outputPath).Length;
        var ratio = (1 - (double)compressedSize / originalSize) * 100;

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            original_size = originalSize,
            compressed_size = compressedSize,
            compression_ratio = Math.Round(ratio, 2)
        });
    }

    /// <summary>
    /// Decompress a GZIP file
    /// </summary>
    public async Task<string> DecompressGzipAsync(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");
        var outputFile = GetString(args, "output_file", null);

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var outputPath = string.IsNullOrEmpty(outputFile)
            ? sourcePath.EndsWith(".gz") ? sourcePath[..^3] : sourcePath + ".decompressed"
            : Path.Combine(_generatedPath, outputFile);

        await using var inputStream = File.OpenRead(sourcePath);
        await using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        await using var outputStream = File.Create(outputPath);

        await gzipStream.CopyToAsync(outputStream);

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            decompressed_size = new FileInfo(outputPath).Length
        });
    }

    #endregion

    #region File Analysis

    /// <summary>
    /// Analyze file content and detect type
    /// </summary>
    public async Task<string> AnalyzeFileAsync(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var fileInfo = new FileInfo(sourcePath);

        // Read magic bytes for file type detection
        var magicBytes = new byte[16];
        await using (var stream = File.OpenRead(sourcePath))
        {
            await stream.ReadAsync(magicBytes, 0, Math.Min(16, (int)stream.Length));
        }

        var detectedType = DetectFileType(magicBytes, fileInfo.Extension);

        // Calculate checksums
        var md5 = await CalculateFileHashAsync(sourcePath, MD5.Create());
        var sha256 = await CalculateFileHashAsync(sourcePath, SHA256.Create());

        // Check if text or binary
        var isText = await IsTextFileAsync(sourcePath);

        // Line count for text files
        var lineCount = isText ? File.ReadLines(sourcePath).Count() : 0;

        return JsonSerializer.Serialize(new {
            success = true,
            file = sourcePath,
            name = fileInfo.Name,
            extension = fileInfo.Extension,
            detected_type = detectedType,
            size = new {
                bytes = fileInfo.Length,
                formatted = FormatFileSize(fileInfo.Length)
            },
            dates = new {
                created = fileInfo.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                modified = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                accessed = fileInfo.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            content_type = isText ? "text" : "binary",
            line_count = isText ? lineCount : (int?)null,
            checksums = new {
                md5,
                sha256
            }
        });
    }

    private string DetectFileType(byte[] magic, string extension)
    {
        // Check magic bytes
        if (magic.Length >= 4)
        {
            if (magic[0] == 0x50 && magic[1] == 0x4B) return "ZIP Archive";
            if (magic[0] == 0x89 && magic[1] == 0x50 && magic[2] == 0x4E && magic[3] == 0x47) return "PNG Image";
            if (magic[0] == 0xFF && magic[1] == 0xD8 && magic[2] == 0xFF) return "JPEG Image";
            if (magic[0] == 0x47 && magic[1] == 0x49 && magic[2] == 0x46) return "GIF Image";
            if (magic[0] == 0x25 && magic[1] == 0x50 && magic[2] == 0x44 && magic[3] == 0x46) return "PDF Document";
            if (magic[0] == 0x1F && magic[1] == 0x8B) return "GZIP Compressed";
            if (magic[0] == 0x52 && magic[1] == 0x61 && magic[2] == 0x72) return "RAR Archive";
            if (magic[0] == 0x7B) return "JSON Document";
            if (magic[0] == 0x3C) return "XML/HTML Document";
        }

        // Fall back to extension
        return extension.ToLower() switch
        {
            ".txt" => "Plain Text",
            ".csv" => "CSV Data",
            ".json" => "JSON Document",
            ".xml" => "XML Document",
            ".html" or ".htm" => "HTML Document",
            ".md" => "Markdown Document",
            ".docx" => "Word Document",
            ".xlsx" => "Excel Spreadsheet",
            ".pptx" => "PowerPoint Presentation",
            ".pdf" => "PDF Document",
            ".png" => "PNG Image",
            ".jpg" or ".jpeg" => "JPEG Image",
            ".gif" => "GIF Image",
            ".svg" => "SVG Image",
            ".mp3" => "MP3 Audio",
            ".mp4" => "MP4 Video",
            ".zip" => "ZIP Archive",
            ".gz" => "GZIP Compressed",
            _ => "Unknown"
        };
    }

    private async Task<bool> IsTextFileAsync(string path)
    {
        var buffer = new byte[8192];
        await using var stream = File.OpenRead(path);
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        // Check for null bytes (binary indicator)
        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == 0) return false;
        }

        return true;
    }

    private async Task<string> CalculateFileHashAsync(string path, HashAlgorithm algorithm)
    {
        await using var stream = File.OpenRead(path);
        var hash = await Task.Run(() => algorithm.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    #endregion

    #region File Comparison

    /// <summary>
    /// Compare two files byte-by-byte
    /// </summary>
    public async Task<string> CompareFilesAsync(Dictionary<string, object> args)
    {
        var file1 = GetString(args, "file1");
        var file2 = GetString(args, "file2");

        var path1 = ResolvePath(file1, false);
        var path2 = ResolvePath(file2, false);

        if (!File.Exists(path1) || !File.Exists(path2))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = "One or both files not found"
            });
        }

        var info1 = new FileInfo(path1);
        var info2 = new FileInfo(path2);

        var sameSize = info1.Length == info2.Length;
        var sameContent = false;
        var firstDifferenceAt = -1L;

        if (sameSize)
        {
            await using var stream1 = File.OpenRead(path1);
            await using var stream2 = File.OpenRead(path2);

            var buffer1 = new byte[81920];
            var buffer2 = new byte[81920];
            var position = 0L;

            sameContent = true;

            int bytes1, bytes2;
            while ((bytes1 = await stream1.ReadAsync(buffer1, 0, buffer1.Length)) > 0)
            {
                bytes2 = await stream2.ReadAsync(buffer2, 0, buffer2.Length);

                for (int i = 0; i < bytes1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        sameContent = false;
                        firstDifferenceAt = position + i;
                        break;
                    }
                }

                if (!sameContent) break;
                position += bytes1;
            }
        }

        // Calculate hashes for both
        var hash1 = await CalculateFileHashAsync(path1, SHA256.Create());
        var hash2 = await CalculateFileHashAsync(path2, SHA256.Create());

        return JsonSerializer.Serialize(new {
            success = true,
            identical = sameContent,
            same_size = sameSize,
            file1 = new {
                path = path1,
                size = info1.Length,
                hash = hash1
            },
            file2 = new {
                path = path2,
                size = info2.Length,
                hash = hash2
            },
            first_difference_at = firstDifferenceAt >= 0 ? firstDifferenceAt : (long?)null,
            size_difference = Math.Abs(info1.Length - info2.Length)
        });
    }

    #endregion

    #region File Splitting/Merging

    /// <summary>
    /// Split a file into smaller parts
    /// </summary>
    public async Task<string> SplitFileAsync(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");
        var partSize = GetLong(args, "part_size", 10 * 1024 * 1024); // Default 10MB
        var outputPrefix = GetString(args, "output_prefix", null);

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var prefix = outputPrefix ?? Path.GetFileNameWithoutExtension(sourcePath);
        var parts = new List<string>();

        await using var inputStream = File.OpenRead(sourcePath);
        var buffer = new byte[81920];
        var partNumber = 1;
        var bytesRemaining = inputStream.Length;

        while (bytesRemaining > 0)
        {
            var partPath = Path.Combine(_generatedPath, $"{prefix}.part{partNumber:D3}");
            var partBytesToWrite = Math.Min(partSize, bytesRemaining);

            await using (var partStream = File.Create(partPath))
            {
                var partBytesWritten = 0L;
                while (partBytesWritten < partBytesToWrite)
                {
                    var bytesToRead = (int)Math.Min(buffer.Length, partBytesToWrite - partBytesWritten);
                    var bytesRead = await inputStream.ReadAsync(buffer, 0, bytesToRead);
                    if (bytesRead == 0) break;

                    await partStream.WriteAsync(buffer, 0, bytesRead);
                    partBytesWritten += bytesRead;
                }
            }

            parts.Add(partPath);
            bytesRemaining -= partBytesToWrite;
            partNumber++;
        }

        return JsonSerializer.Serialize(new {
            success = true,
            original_file = sourcePath,
            original_size = inputStream.Length,
            part_count = parts.Count,
            part_size = partSize,
            parts
        });
    }

    /// <summary>
    /// Merge split files back together
    /// </summary>
    public async Task<string> MergeFilesAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputFile = GetString(args, "output_file", "merged_output");

        var outputPath = Path.Combine(_generatedPath, outputFile);

        await using var outputStream = File.Create(outputPath);
        var totalBytes = 0L;

        foreach (var file in files)
        {
            var sourcePath = ResolvePath(file, false);
            if (!File.Exists(sourcePath))
            {
                return JsonSerializer.Serialize(new {
                    success = false,
                    error = $"File not found: {file}"
                });
            }

            await using var inputStream = File.OpenRead(sourcePath);
            await inputStream.CopyToAsync(outputStream);
            totalBytes += inputStream.Length;
        }

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            merged_files = files.Length,
            total_size = totalBytes
        });
    }

    #endregion

    #region Duplicate Detection

    /// <summary>
    /// Find duplicate files in a directory
    /// </summary>
    public async Task<string> FindDuplicatesAsync(Dictionary<string, object> args)
    {
        var folder = GetString(args, "folder", "generated");
        var includeSubfolders = GetBool(args, "include_subfolders", true);

        var searchPath = folder == "uploaded" ? _uploadedPath : _generatedPath;
        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(searchPath, "*", searchOption);

        // Group by size first (quick filter)
        var sizeGroups = files
            .Select(f => new { Path = f, Info = new FileInfo(f) })
            .GroupBy(f => f.Info.Length)
            .Where(g => g.Count() > 1);

        var duplicates = new List<object>();

        foreach (var group in sizeGroups)
        {
            // For same-size files, compare by hash
            var hashGroups = new Dictionary<string, List<string>>();

            foreach (var file in group)
            {
                var hash = await CalculateFileHashAsync(file.Path, MD5.Create());
                if (!hashGroups.ContainsKey(hash))
                    hashGroups[hash] = new List<string>();
                hashGroups[hash].Add(file.Path);
            }

            foreach (var hashGroup in hashGroups.Where(g => g.Value.Count > 1))
            {
                duplicates.Add(new {
                    hash = hashGroup.Key,
                    size = group.Key,
                    files = hashGroup.Value
                });
            }
        }

        var totalWastedSpace = duplicates
            .Cast<dynamic>()
            .Sum(d => (long)d.size * (((IEnumerable<string>)d.files).Count() - 1));

        return JsonSerializer.Serialize(new {
            success = true,
            folder = searchPath,
            duplicate_groups = duplicates.Count,
            duplicates = duplicates.Take(50),
            wasted_space = new {
                bytes = totalWastedSpace,
                formatted = FormatFileSize(totalWastedSpace)
            }
        });
    }

    #endregion

    #region File Renaming

    /// <summary>
    /// Batch rename files with pattern
    /// </summary>
    public async Task<string> BatchRenameAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var pattern = GetString(args, "pattern", "{name}_{num}"); // {name}, {num}, {date}, {ext}
        var startNum = GetInt(args, "start_num", 1);
        var preview = GetBool(args, "preview", false);

        var renames = new List<object>();
        var num = startNum;

        foreach (var file in files)
        {
            var sourcePath = ResolvePath(file, false);
            if (!File.Exists(sourcePath)) continue;

            var name = Path.GetFileNameWithoutExtension(sourcePath);
            var ext = Path.GetExtension(sourcePath);
            var date = File.GetLastWriteTime(sourcePath).ToString("yyyyMMdd");

            var newName = pattern
                .Replace("{name}", name)
                .Replace("{num}", num.ToString("D3"))
                .Replace("{date}", date)
                .Replace("{ext}", ext.TrimStart('.'));

            if (!newName.Contains('.'))
                newName += ext;

            var newPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, newName);

            renames.Add(new {
                original = Path.GetFileName(sourcePath),
                renamed = newName
            });

            if (!preview && sourcePath != newPath && !File.Exists(newPath))
            {
                File.Move(sourcePath, newPath);
            }

            num++;
        }

        return JsonSerializer.Serialize(new {
            success = true,
            preview,
            rename_count = renames.Count,
            renames
        });
    }

    #endregion

    #region Base64 File Conversion

    /// <summary>
    /// Convert file to Base64 string
    /// </summary>
    public async Task<string> FileToBase64Async(Dictionary<string, object> args)
    {
        var file = GetString(args, "file");
        var outputFile = GetString(args, "output_file", null);

        var sourcePath = ResolvePath(file, false);
        if (!File.Exists(sourcePath))
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"File not found: {file}"
            });
        }

        var bytes = await File.ReadAllBytesAsync(sourcePath);
        var base64 = Convert.ToBase64String(bytes);

        var mimeType = Path.GetExtension(sourcePath).ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            _ => "application/octet-stream"
        };

        var dataUri = $"data:{mimeType};base64,{base64}";

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = Path.Combine(_generatedPath, outputFile);
            await File.WriteAllTextAsync(outputPath, base64);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                original_size = bytes.Length,
                base64_length = base64.Length,
                mime_type = mimeType
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            base64 = base64.Length > 10000 ? base64.Substring(0, 10000) + "..." : base64,
            data_uri = dataUri.Length > 10000 ? dataUri.Substring(0, 10000) + "..." : dataUri,
            original_size = bytes.Length,
            base64_length = base64.Length,
            mime_type = mimeType,
            truncated = base64.Length > 10000
        });
    }

    /// <summary>
    /// Convert Base64 string to file
    /// </summary>
    public async Task<string> Base64ToFileAsync(Dictionary<string, object> args)
    {
        var base64 = GetString(args, "base64");
        var outputFile = GetString(args, "output_file", "decoded_file");

        // Handle data URI format
        if (base64.StartsWith("data:"))
        {
            var commaIndex = base64.IndexOf(',');
            if (commaIndex > 0)
                base64 = base64.Substring(commaIndex + 1);
        }

        try
        {
            var bytes = Convert.FromBase64String(base64);
            var outputPath = Path.Combine(_generatedPath, outputFile);

            await File.WriteAllBytesAsync(outputPath, bytes);

            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                size = bytes.Length
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = $"Invalid Base64: {ex.Message}"
            });
        }
    }

    #endregion

    #region Helper Methods

    private string ResolvePath(string path, bool isOutput)
    {
        if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            return path;

        if (!isOutput)
        {
            var uploadedPath = Path.Combine(_uploadedPath, path);
            if (File.Exists(uploadedPath))
                return uploadedPath;

            var generatedPath = Path.Combine(_generatedPath, path);
            if (File.Exists(generatedPath))
                return generatedPath;
        }

        return Path.Combine(_generatedPath, path);
    }

    private void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F2} {sizes[order]}";
    }

    private static string GetString(Dictionary<string, object> args, string key, string? defaultValue = null)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? je.GetString() ?? defaultValue ?? "" : je.ToString();
            }
            return value?.ToString() ?? defaultValue ?? "";
        }
        return defaultValue ?? "";
    }

    private static int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetInt32() : defaultValue;
            }
            if (int.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static long GetLong(Dictionary<string, object> args, string key, long defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetInt64() : defaultValue;
            }
            if (long.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.True ||
                       (je.ValueKind == JsonValueKind.String && je.GetString()?.ToLower() == "true");
            }
            if (bool.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static string[] GetStringArray(Dictionary<string, object> args, string key, string[]? defaultValue = null)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                return je.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }
            if (value is IEnumerable<object> list)
            {
                return list.Select(x => x?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray();
            }
        }
        return defaultValue ?? Array.Empty<string>();
    }

    #endregion
}
