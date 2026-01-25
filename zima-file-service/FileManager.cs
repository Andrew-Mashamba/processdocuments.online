using System.Text.Json;
using ClosedXML.Excel;

namespace ZimaFileService;

/// <summary>
/// Manages file storage locations and provides file operations
/// </summary>
public class FileManager
{
    private static FileManager? _instance;
    private static readonly object _lock = new();

    public string WorkingDirectory { get; private set; }
    public string GeneratedFilesPath { get; private set; }
    public string UploadedFilesPath { get; private set; }

    private FileManager(string? workingDirectory = null)
    {
        // Use provided working directory, or environment variable, or current directory
        WorkingDirectory = workingDirectory
            ?? Environment.GetEnvironmentVariable("ZIMA_WORKING_DIR")
            ?? Directory.GetCurrentDirectory();

        GeneratedFilesPath = Path.Combine(WorkingDirectory, "generated_files");
        UploadedFilesPath = Path.Combine(WorkingDirectory, "uploaded_files");

        EnsureDirectoriesExist();
    }

    public static FileManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new FileManager();
                }
            }
            return _instance;
        }
    }

    public static void Initialize(string? workingDirectory = null)
    {
        lock (_lock)
        {
            _instance = new FileManager(workingDirectory);
        }
    }

    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(GeneratedFilesPath))
        {
            Directory.CreateDirectory(GeneratedFilesPath);
            Console.Error.WriteLine($"Created generated_files directory: {GeneratedFilesPath}");
        }

        if (!Directory.Exists(UploadedFilesPath))
        {
            Directory.CreateDirectory(UploadedFilesPath);
            Console.Error.WriteLine($"Created uploaded_files directory: {UploadedFilesPath}");
        }
    }

    /// <summary>
    /// Resolves a file path, placing it in generated_files if it's just a filename
    /// </summary>
    public string ResolveGeneratedFilePath(string filePath)
    {
        // If it's an absolute path, use it as-is
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        // If it's just a filename or relative path, put it in generated_files
        return Path.Combine(GeneratedFilesPath, filePath);
    }

    /// <summary>
    /// Resolves a file path for uploaded files
    /// </summary>
    public string ResolveUploadedFilePath(string filePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }
        return Path.Combine(UploadedFilesPath, filePath);
    }

    /// <summary>
    /// Gets the upload directory for a specific session
    /// </summary>
    public string GetSessionUploadPath(string sessionId)
    {
        var sessionPath = Path.Combine(UploadedFilesPath, sessionId);
        if (!Directory.Exists(sessionPath))
        {
            Directory.CreateDirectory(sessionPath);
        }
        return sessionPath;
    }

    /// <summary>
    /// Gets the generated files directory for a specific session
    /// </summary>
    public string GetSessionGeneratedPath(string sessionId)
    {
        var sessionPath = Path.Combine(GeneratedFilesPath, sessionId);
        if (!Directory.Exists(sessionPath))
        {
            Directory.CreateDirectory(sessionPath);
        }
        return sessionPath;
    }

    /// <summary>
    /// Resolves a file path for generated files, optionally within a session
    /// </summary>
    public string ResolveGeneratedFilePath(string filePath, string? sessionId = null)
    {
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        // If session ID is provided, put in session-specific folder
        if (!string.IsNullOrEmpty(sessionId))
        {
            return Path.Combine(GetSessionGeneratedPath(sessionId), filePath);
        }

        // Fallback to general generated files path
        return Path.Combine(GeneratedFilesPath, filePath);
    }

    /// <summary>
    /// Lists generated files for a specific session
    /// </summary>
    public List<FileInfo> ListSessionGeneratedFiles(string sessionId, string? pattern = null)
    {
        var sessionPath = GetSessionGeneratedPath(sessionId);
        return ListFiles(sessionPath, pattern);
    }

    /// <summary>
    /// Gets all generated files grouped by session
    /// </summary>
    public Dictionary<string, List<FileGroupInfo>> GetGeneratedFilesGroupedBySession()
    {
        var result = new Dictionary<string, List<FileGroupInfo>>();

        // Get session directories
        if (Directory.Exists(GeneratedFilesPath))
        {
            var sessionDirs = Directory.GetDirectories(GeneratedFilesPath);
            foreach (var sessionDir in sessionDirs)
            {
                var sessionId = Path.GetFileName(sessionDir);
                var sessionFiles = GetFilesGroupedByVersionInPath(sessionDir);
                if (sessionFiles.Any())
                {
                    result[sessionId] = sessionFiles;
                }
            }

            // Also get files in the root generated_files folder (legacy/unsessioned)
            var rootFiles = Directory.GetFiles(GeneratedFilesPath);
            if (rootFiles.Any())
            {
                result["_unsorted"] = GetFilesGroupedByVersionInPath(GeneratedFilesPath, filesOnly: true);
            }
        }

        return result;
    }

    /// <summary>
    /// Get files grouped by version in a specific path
    /// </summary>
    private List<FileGroupInfo> GetFilesGroupedByVersionInPath(string path, bool filesOnly = false)
    {
        IEnumerable<string> files;
        if (filesOnly)
        {
            // Only get files directly in this folder, not in subdirectories
            files = Directory.GetFiles(path);
        }
        else
        {
            files = Directory.GetFiles(path);
        }

        var groups = new Dictionary<string, FileGroupInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);
            var info = new FileInfo(file);

            // Determine base name and version
            string baseName;
            int version;

            var versionMatch = System.Text.RegularExpressions.Regex.Match(fileNameWithoutExt, @"^(.+)_v(\d+)$");
            if (versionMatch.Success)
            {
                baseName = versionMatch.Groups[1].Value + extension;
                version = int.Parse(versionMatch.Groups[2].Value);
            }
            else
            {
                baseName = fileName;
                version = 1;
            }

            var key = baseName.ToLowerInvariant();
            if (!groups.ContainsKey(key))
            {
                groups[key] = new FileGroupInfo
                {
                    BaseName = baseName,
                    Extension = extension.TrimStart('.'),
                    Versions = new List<FileVersionInfo>()
                };
            }

            groups[key].Versions.Add(new FileVersionInfo
            {
                FileName = fileName,
                BaseName = baseName,
                Version = version,
                Size = info.Length,
                SizeFormatted = FormatFileSize(info.Length),
                Created = info.CreationTime,
                Modified = info.LastWriteTime,
                FullPath = file
            });
        }

        // Sort versions and set latest
        foreach (var group in groups.Values)
        {
            group.Versions = group.Versions.OrderByDescending(v => v.Version).ToList();
            group.LatestVersion = group.Versions.First();
            group.VersionCount = group.Versions.Count;
        }

        return groups.Values.OrderByDescending(g => g.LatestVersion.Modified).ToList();
    }

    /// <summary>
    /// Lists files uploaded for a specific session
    /// </summary>
    public List<FileInfo> ListSessionFiles(string sessionId, string? pattern = null)
    {
        var sessionPath = GetSessionUploadPath(sessionId);
        return ListFiles(sessionPath, pattern);
    }

    /// <summary>
    /// Gets all file contents for a session (for context injection)
    /// Uses smart loading based on file size to prevent context overflow
    /// </summary>
    public Dictionary<string, SessionFileContent> GetSessionFileContents(string sessionId)
    {
        var result = new Dictionary<string, SessionFileContent>();
        var sessionPath = GetSessionUploadPath(sessionId);

        if (!Directory.Exists(sessionPath))
        {
            return result;
        }

        var files = Directory.GetFiles(sessionPath);
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            var fileName = info.Name;
            var extension = info.Extension.ToLower();

            try
            {
                result[fileName] = LoadFileSmartly(file, info, extension);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading file {fileName}: {ex.Message}");
                result[fileName] = new SessionFileContent
                {
                    FileName = fileName,
                    Content = $"[Error reading file: {ex.Message}]",
                    MimeType = "application/octet-stream",
                    Size = info.Length,
                    IsText = false,
                    LoadStrategy = "error"
                };
            }
        }

        return result;
    }

    /// <summary>
    /// Smart file loading based on size and type
    /// </summary>
    private SessionFileContent LoadFileSmartly(string filePath, FileInfo info, string extension)
    {
        var fileName = info.Name;
        var textExtensions = new[] { ".txt", ".csv", ".json", ".xml", ".md", ".html", ".css", ".js", ".ts", ".py", ".cs", ".java", ".sql", ".yaml", ".yml", ".ini", ".conf", ".log", ".php", ".rb", ".go", ".rs", ".swift", ".kt" };

        // Excel files - always use summary approach
        if (extension == ".xlsx" || extension == ".xls")
        {
            return LoadExcelSmartly(filePath, info);
        }

        // PDF files - binary, provide metadata only
        if (extension == ".pdf")
        {
            return new SessionFileContent
            {
                FileName = fileName,
                Content = $"[PDF file: {fileName} ({FormatFileSize(info.Length)}) - Binary content. Use appropriate PDF tools to extract text if needed.]",
                MimeType = "application/pdf",
                Size = info.Length,
                IsText = false,
                LoadStrategy = "metadata"
            };
        }

        // Text files - smart loading based on size
        if (textExtensions.Contains(extension))
        {
            return LoadTextFileSmartly(filePath, info, extension);
        }

        // CSV files - treat specially due to data nature
        if (extension == ".csv")
        {
            return LoadCsvSmartly(filePath, info);
        }

        // Other binary files
        return new SessionFileContent
        {
            FileName = fileName,
            Content = $"[Binary file: {fileName} ({FormatFileSize(info.Length)}) - Content not directly readable]",
            MimeType = GetMimeType(extension),
            Size = info.Length,
            IsText = false,
            LoadStrategy = "metadata"
        };
    }

    /// <summary>
    /// Smart loading for text files based on size
    /// </summary>
    private SessionFileContent LoadTextFileSmartly(string filePath, FileInfo info, string extension)
    {
        var fileName = info.Name;

        // Small files: load fully
        if (info.Length <= SmartFileConfig.SmallFileThreshold)
        {
            var content = File.ReadAllText(filePath);
            return new SessionFileContent
            {
                FileName = fileName,
                Content = content,
                MimeType = GetMimeType(extension),
                Size = info.Length,
                IsText = true,
                IsTruncated = false,
                LoadStrategy = "full"
            };
        }

        // Medium files: load with truncation
        if (info.Length <= SmartFileConfig.MediumFileThreshold)
        {
            return LoadTextWithPreview(filePath, info, extension, SmartFileConfig.MaxCharactersMedium);
        }

        // Large files: summary with small preview
        return LoadTextWithPreview(filePath, info, extension, SmartFileConfig.MaxCharactersLarge);
    }

    /// <summary>
    /// Load text file with preview/truncation
    /// </summary>
    private SessionFileContent LoadTextWithPreview(string filePath, FileInfo info, string extension, int maxChars)
    {
        var fileName = info.Name;
        var lines = File.ReadLines(filePath).ToList();
        var totalLines = lines.Count;

        var contentBuilder = new System.Text.StringBuilder();
        int loadedLines = 0;
        int currentChars = 0;

        foreach (var line in lines)
        {
            if (currentChars + line.Length > maxChars)
            {
                break;
            }
            contentBuilder.AppendLine(line);
            currentChars += line.Length + 1;
            loadedLines++;
        }

        var content = contentBuilder.ToString();
        var isTruncated = loadedLines < totalLines;

        if (isTruncated)
        {
            content += $"\n\n... [TRUNCATED: Showing {loadedLines} of {totalLines} lines ({FormatFileSize(info.Length)} total). Request specific sections if needed.]";
        }

        return new SessionFileContent
        {
            FileName = fileName,
            Content = content,
            MimeType = GetMimeType(extension),
            Size = info.Length,
            IsText = true,
            IsTruncated = isTruncated,
            TotalLines = totalLines,
            LoadedLines = loadedLines,
            LoadStrategy = isTruncated ? "preview" : "full"
        };
    }

    /// <summary>
    /// Smart loading for CSV files with statistics
    /// </summary>
    private SessionFileContent LoadCsvSmartly(string filePath, FileInfo info)
    {
        var fileName = info.Name;
        var lines = File.ReadLines(filePath).ToList();
        var totalLines = lines.Count;

        // Small CSV: load fully
        if (info.Length <= SmartFileConfig.SmallFileThreshold && totalLines <= 500)
        {
            return new SessionFileContent
            {
                FileName = fileName,
                Content = File.ReadAllText(filePath),
                MimeType = "text/csv",
                Size = info.Length,
                IsText = true,
                TotalLines = totalLines,
                LoadedLines = totalLines,
                LoadStrategy = "full"
            };
        }

        // Large CSV: provide summary with preview
        var contentBuilder = new System.Text.StringBuilder();
        var previewLines = Math.Min(SmartFileConfig.PreviewLines, totalLines);

        // Header analysis
        var headers = lines.FirstOrDefault()?.Split(',') ?? Array.Empty<string>();
        contentBuilder.AppendLine($"=== CSV FILE SUMMARY: {fileName} ===");
        contentBuilder.AppendLine($"Total rows: {totalLines - 1} (excluding header)");
        contentBuilder.AppendLine($"Columns ({headers.Length}): {string.Join(", ", headers.Take(20))}");
        if (headers.Length > 20)
        {
            contentBuilder.AppendLine($"  ... and {headers.Length - 20} more columns");
        }
        contentBuilder.AppendLine($"File size: {FormatFileSize(info.Length)}");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine($"=== FIRST {previewLines} ROWS ===");

        for (int i = 0; i < previewLines && i < lines.Count; i++)
        {
            contentBuilder.AppendLine(lines[i]);
        }

        if (totalLines > previewLines)
        {
            contentBuilder.AppendLine();
            contentBuilder.AppendLine($"... [{totalLines - previewLines} more rows not shown]");
            contentBuilder.AppendLine("Use read_file_content tool with offset/limit parameters for specific sections.");
        }

        return new SessionFileContent
        {
            FileName = fileName,
            Content = contentBuilder.ToString(),
            MimeType = "text/csv",
            Size = info.Length,
            IsText = true,
            IsTruncated = totalLines > previewLines,
            TotalLines = totalLines,
            LoadedLines = previewLines,
            LoadStrategy = "summary"
        };
    }

    /// <summary>
    /// Smart loading for Excel files with summary
    /// </summary>
    private SessionFileContent LoadExcelSmartly(string filePath, FileInfo info)
    {
        var fileName = info.Name;

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
            var contentBuilder = new System.Text.StringBuilder();

            contentBuilder.AppendLine($"=== EXCEL FILE SUMMARY: {fileName} ===");
            contentBuilder.AppendLine($"File size: {FormatFileSize(info.Length)}");
            contentBuilder.AppendLine($"Sheets: {workbook.Worksheets.Count}");
            contentBuilder.AppendLine();

            foreach (var worksheet in workbook.Worksheets)
            {
                var usedRange = worksheet.RangeUsed();
                var rowCount = usedRange?.RowsUsed().Count() ?? 0;
                var colCount = usedRange?.ColumnsUsed().Count() ?? 0;

                contentBuilder.AppendLine($"--- Sheet: {worksheet.Name} ---");
                contentBuilder.AppendLine($"Rows: {rowCount}, Columns: {colCount}");

                if (usedRange != null && rowCount > 0)
                {
                    // Get headers
                    var firstRow = usedRange.RowsUsed().First();
                    var headers = firstRow.Cells().Select(c => c.GetString()).ToArray();
                    contentBuilder.AppendLine($"Headers: {string.Join(", ", headers.Take(10))}");
                    if (headers.Length > 10)
                    {
                        contentBuilder.AppendLine($"  ... and {headers.Length - 10} more columns");
                    }

                    // Preview first few rows
                    var previewRows = Math.Min(5, rowCount - 1);
                    if (previewRows > 0)
                    {
                        contentBuilder.AppendLine($"\nSample data (first {previewRows} rows):");
                        var rows = usedRange.RowsUsed().Skip(1).Take(previewRows);
                        foreach (var row in rows)
                        {
                            var values = row.Cells().Select(c => c.GetString()).Take(10);
                            contentBuilder.AppendLine($"  {string.Join(" | ", values)}");
                        }
                    }
                }
                contentBuilder.AppendLine();
            }

            contentBuilder.AppendLine("Use 'read_excel' tool with max_rows and include_stats parameters for detailed access.");

            return new SessionFileContent
            {
                FileName = fileName,
                Content = contentBuilder.ToString(),
                MimeType = GetMimeType(info.Extension.ToLower()),
                Size = info.Length,
                IsText = false,
                LoadStrategy = "summary"
            };
        }
        catch (Exception ex)
        {
            // Fallback if Excel parsing fails
            return new SessionFileContent
            {
                FileName = fileName,
                Content = $"[Excel file: {fileName} ({FormatFileSize(info.Length)}) - Use read_excel tool to access data. Parse error: {ex.Message}]",
                MimeType = GetMimeType(info.Extension.ToLower()),
                Size = info.Length,
                IsText = false,
                LoadStrategy = "metadata"
            };
        }
    }

    /// <summary>
    /// Deletes all files for a session
    /// </summary>
    public void DeleteSessionFiles(string sessionId)
    {
        var sessionPath = Path.Combine(UploadedFilesPath, sessionId);
        if (Directory.Exists(sessionPath))
        {
            Directory.Delete(sessionPath, true);
        }
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLower() switch
        {
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".md" => "text/markdown",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Lists files in a directory with metadata
    /// </summary>
    public List<FileInfo> ListFiles(string directory, string? pattern = null)
    {
        if (!Directory.Exists(directory))
        {
            return new List<FileInfo>();
        }

        var searchPattern = pattern ?? "*.*";
        return Directory.GetFiles(directory, searchPattern)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();
    }

    /// <summary>
    /// Gets file information as a dictionary
    /// </summary>
    public Dictionary<string, object> GetFileMetadata(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return new Dictionary<string, object>
        {
            ["name"] = info.Name,
            ["path"] = info.FullName,
            ["size"] = info.Length,
            ["sizeFormatted"] = FormatFileSize(info.Length),
            ["extension"] = info.Extension,
            ["created"] = info.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ["modified"] = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ["isReadOnly"] = info.IsReadOnly
        };
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    public bool DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Copies a file
    /// </summary>
    public string CopyFile(string sourcePath, string destPath, bool overwrite = false)
    {
        var resolvedDest = Path.IsPathRooted(destPath) ? destPath : ResolveGeneratedFilePath(destPath);
        File.Copy(sourcePath, resolvedDest, overwrite);
        return resolvedDest;
    }

    /// <summary>
    /// Moves a file
    /// </summary>
    public string MoveFile(string sourcePath, string destPath, bool overwrite = false)
    {
        var resolvedDest = Path.IsPathRooted(destPath) ? destPath : ResolveGeneratedFilePath(destPath);
        File.Move(sourcePath, resolvedDest, overwrite);
        return resolvedDest;
    }

    /// <summary>
    /// Reads file content as base64 (for binary files) or text
    /// </summary>
    public (string content, string encoding) ReadFileContent(string filePath, bool asBase64 = false)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        if (asBase64)
        {
            var bytes = File.ReadAllBytes(filePath);
            return (Convert.ToBase64String(bytes), "base64");
        }
        else
        {
            var text = File.ReadAllText(filePath);
            return (text, "utf-8");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Get versioned filename - if file exists, create v2, v3, etc.
    /// </summary>
    public string GetVersionedFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath) ?? GeneratedFilesPath;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        // Check if already versioned (ends with _v{number})
        var versionMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^(.+)_v(\d+)$");
        string baseName;
        int currentVersion;

        if (versionMatch.Success)
        {
            baseName = versionMatch.Groups[1].Value;
            currentVersion = int.Parse(versionMatch.Groups[2].Value);
        }
        else
        {
            baseName = fileName;
            currentVersion = 1;
        }

        // Find the next available version
        int nextVersion = currentVersion + 1;
        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{baseName}_v{nextVersion}{extension}");
            nextVersion++;
        } while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Get all versions of a file by base name
    /// </summary>
    public List<FileVersionInfo> GetFileVersions(string baseName)
    {
        var versions = new List<FileVersionInfo>();
        var files = Directory.GetFiles(GeneratedFilesPath);

        // Remove extension from baseName if present
        var baseNameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
        var extension = Path.GetExtension(baseName);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var fileExt = Path.GetExtension(file);

            // Check if this file matches the base name
            bool isMatch = false;
            int version = 1;

            // Exact match (original file, version 1)
            if (fileNameWithoutExt.Equals(baseNameWithoutExt, StringComparison.OrdinalIgnoreCase) &&
                fileExt.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                isMatch = true;
                version = 1;
            }
            else
            {
                // Check for versioned match (baseName_v{N}.ext)
                var versionMatch = System.Text.RegularExpressions.Regex.Match(
                    fileNameWithoutExt,
                    $@"^{System.Text.RegularExpressions.Regex.Escape(baseNameWithoutExt)}_v(\d+)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (versionMatch.Success && fileExt.Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    version = int.Parse(versionMatch.Groups[1].Value);
                }
            }

            if (isMatch)
            {
                var info = new FileInfo(file);
                versions.Add(new FileVersionInfo
                {
                    FileName = fileName,
                    BaseName = $"{baseNameWithoutExt}{extension}",
                    Version = version,
                    Size = info.Length,
                    SizeFormatted = FormatFileSize(info.Length),
                    Created = info.CreationTime,
                    Modified = info.LastWriteTime,
                    FullPath = file
                });
            }
        }

        return versions.OrderByDescending(v => v.Version).ToList();
    }

    /// <summary>
    /// Get all files grouped by base name with version info
    /// </summary>
    public List<FileGroupInfo> GetFilesGroupedByVersion()
    {
        var files = Directory.GetFiles(GeneratedFilesPath);
        var groups = new Dictionary<string, FileGroupInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);
            var info = new FileInfo(file);

            // Determine base name and version
            string baseName;
            int version;

            var versionMatch = System.Text.RegularExpressions.Regex.Match(fileNameWithoutExt, @"^(.+)_v(\d+)$");
            if (versionMatch.Success)
            {
                baseName = versionMatch.Groups[1].Value + extension;
                version = int.Parse(versionMatch.Groups[2].Value);
            }
            else
            {
                baseName = fileName;
                version = 1;
            }

            var key = baseName.ToLowerInvariant();
            if (!groups.ContainsKey(key))
            {
                groups[key] = new FileGroupInfo
                {
                    BaseName = baseName,
                    Extension = extension.TrimStart('.'),
                    Versions = new List<FileVersionInfo>()
                };
            }

            groups[key].Versions.Add(new FileVersionInfo
            {
                FileName = fileName,
                BaseName = baseName,
                Version = version,
                Size = info.Length,
                SizeFormatted = FormatFileSize(info.Length),
                Created = info.CreationTime,
                Modified = info.LastWriteTime,
                FullPath = file
            });
        }

        // Sort versions and set latest
        foreach (var group in groups.Values)
        {
            group.Versions = group.Versions.OrderByDescending(v => v.Version).ToList();
            group.LatestVersion = group.Versions.First();
            group.VersionCount = group.Versions.Count;
        }

        return groups.Values.OrderByDescending(g => g.LatestVersion.Modified).ToList();
    }
}

public class FileVersionInfo
{
    public string FileName { get; set; } = "";
    public string BaseName { get; set; } = "";
    public int Version { get; set; }
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string FullPath { get; set; } = "";
}

public class FileGroupInfo
{
    public string BaseName { get; set; } = "";
    public string Extension { get; set; } = "";
    public int VersionCount { get; set; }
    public FileVersionInfo LatestVersion { get; set; } = null!;
    public List<FileVersionInfo> Versions { get; set; } = new();
}

public class SessionFileContent
{
    public string FileName { get; set; } = "";
    public string Content { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long Size { get; set; }
    public bool IsText { get; set; }
    public bool IsTruncated { get; set; }
    public int? TotalLines { get; set; }
    public int? LoadedLines { get; set; }
    public string? LoadStrategy { get; set; } // "full", "preview", "summary"
}

/// <summary>
/// Configuration for smart file loading
/// </summary>
public static class SmartFileConfig
{
    // Size thresholds
    public const long SmallFileThreshold = 50_000;      // 50KB - load fully
    public const long MediumFileThreshold = 500_000;    // 500KB - load with preview
    public const long LargeFileThreshold = 5_000_000;   // 5MB - summary only

    // Content limits
    public const int MaxCharactersSmall = 50_000;       // ~12,500 tokens
    public const int MaxCharactersMedium = 20_000;      // ~5,000 tokens
    public const int MaxCharactersLarge = 5_000;        // ~1,250 tokens
    public const int PreviewLines = 100;
    public const int MaxExcelRows = 500;
}
