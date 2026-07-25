using System.Text.RegularExpressions;

namespace ZimaFileService.Services;

/// <summary>
/// File security validation service.
/// Provides file upload validation, filename sanitization, and path traversal protection.
/// </summary>
public static class FileSecurityValidator
{
    // Maximum file size (100 MB default, configurable via environment)
    public static long MaxFileSizeBytes =>
        long.TryParse(Environment.GetEnvironmentVariable("ZIMA_MAX_FILE_SIZE_MB"), out var mb)
            ? mb * 1024 * 1024
            : 100 * 1024 * 1024;

    // Allowed file extensions (whitelist approach)
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".odt", ".ods", ".odp", ".rtf", ".txt", ".md",

        // Data formats
        ".json", ".xml", ".csv", ".yaml", ".yml",

        // Images
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".tiff", ".tif",

        // Archives (for document processing)
        ".zip",

        // Code/text
        ".html", ".htm", ".css", ".js", ".ts"
    };

    // Blocked/dangerous extensions
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Executables
        ".exe", ".dll", ".so", ".dylib", ".bat", ".cmd", ".ps1", ".sh", ".bash",
        ".com", ".msi", ".app", ".dmg", ".deb", ".rpm",

        // Scripts
        ".vbs", ".vbe", ".js", ".jse", ".ws", ".wsf", ".wsc", ".wsh",
        ".scr", ".pif", ".hta", ".cpl",

        // Office macros
        ".docm", ".xlsm", ".pptm", ".dotm", ".xltm", ".potm",

        // Other dangerous
        ".jar", ".class", ".php", ".asp", ".aspx", ".jsp", ".cgi",
        ".pl", ".py", ".rb", ".lua"
    };

    // MIME type to extension mapping for validation
    private static readonly Dictionary<string, HashSet<string>> MimeTypeExtensions = new()
    {
        ["application/pdf"] = new() { ".pdf" },
        ["application/msword"] = new() { ".doc" },
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new() { ".docx" },
        ["application/vnd.ms-excel"] = new() { ".xls" },
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = new() { ".xlsx" },
        ["application/vnd.ms-powerpoint"] = new() { ".ppt" },
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = new() { ".pptx" },
        ["application/json"] = new() { ".json" },
        ["application/xml"] = new() { ".xml" },
        ["text/xml"] = new() { ".xml" },
        ["text/plain"] = new() { ".txt", ".md", ".csv", ".json", ".xml", ".yaml", ".yml" },
        ["text/csv"] = new() { ".csv" },
        ["text/html"] = new() { ".html", ".htm" },
        ["text/css"] = new() { ".css" },
        ["image/jpeg"] = new() { ".jpg", ".jpeg" },
        ["image/png"] = new() { ".png" },
        ["image/gif"] = new() { ".gif" },
        ["image/webp"] = new() { ".webp" },
        ["image/svg+xml"] = new() { ".svg" },
        ["image/bmp"] = new() { ".bmp" },
        ["image/tiff"] = new() { ".tiff", ".tif" },
        ["application/zip"] = new() { ".zip" },
        ["application/x-zip-compressed"] = new() { ".zip" }
    };

    /// <summary>
    /// Validate a file upload and return validation result
    /// </summary>
    public static FileValidationResult ValidateFile(string filename, string? contentType, long fileSize)
    {
        var result = new FileValidationResult { IsValid = true };

        // Check file size
        if (fileSize <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("File is empty");
            return result;
        }

        if (fileSize > MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.Errors.Add($"File size ({FormatFileSize(fileSize)}) exceeds maximum allowed ({FormatFileSize(MaxFileSizeBytes)})");
            return result;
        }

        // Check for null bytes in filename (path traversal indicator)
        if (filename.Contains('\0'))
        {
            result.IsValid = false;
            result.Errors.Add("Invalid filename: contains null bytes");
            result.IsSuspicious = true;
            return result;
        }

        // Check for path traversal attempts
        if (ContainsPathTraversal(filename))
        {
            result.IsValid = false;
            result.Errors.Add("Invalid filename: path traversal detected");
            result.IsSuspicious = true;
            return result;
        }

        // Get and validate extension
        var extension = Path.GetExtension(filename);
        if (string.IsNullOrEmpty(extension))
        {
            result.IsValid = false;
            result.Errors.Add("File must have an extension");
            return result;
        }

        // Check blocked extensions
        if (BlockedExtensions.Contains(extension))
        {
            result.IsValid = false;
            result.Errors.Add($"File type '{extension}' is not allowed for security reasons");
            result.IsSuspicious = true;
            return result;
        }

        // Check allowed extensions
        if (!AllowedExtensions.Contains(extension))
        {
            result.IsValid = false;
            result.Errors.Add($"File type '{extension}' is not supported");
            return result;
        }

        // Validate MIME type matches extension (if provided)
        if (!string.IsNullOrEmpty(contentType) && !ValidateMimeType(contentType, extension))
        {
            result.Warnings.Add($"MIME type '{contentType}' may not match file extension '{extension}'");
        }

        // Check for double extensions (e.g., .pdf.exe)
        var doubleExtension = GetDoubleExtension(filename);
        if (doubleExtension != null && BlockedExtensions.Contains(doubleExtension))
        {
            result.IsValid = false;
            result.Errors.Add($"File appears to have a hidden dangerous extension: {doubleExtension}");
            result.IsSuspicious = true;
            return result;
        }

        // Sanitize and provide safe filename
        result.SanitizedFilename = SanitizeFilename(filename);

        return result;
    }

    /// <summary>
    /// Check if filename contains path traversal patterns
    /// </summary>
    public static bool ContainsPathTraversal(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        // Normalize to forward slashes
        var normalized = filename.Replace('\\', '/');

        // Check for various path traversal patterns
        var patterns = new[]
        {
            "..",           // Basic traversal
            "./",           // Current directory reference
            "~/",           // Home directory reference
            "/./",          // Embedded current directory
            "/../",         // Embedded parent directory
            "%2e%2e",       // URL encoded ..
            "%252e%252e",   // Double URL encoded ..
            "..%2f",        // Mixed encoding
            "%2e%2e%2f",    // URL encoded ../
            "..%5c",        // URL encoded ..\
            "%2e%2e%5c"     // URL encoded ..\
        };

        foreach (var pattern in patterns)
        {
            if (normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check if path is absolute
        if (Path.IsPathRooted(filename))
            return true;

        // Check for drive letters (Windows)
        if (Regex.IsMatch(filename, @"^[a-zA-Z]:"))
            return true;

        return false;
    }

    /// <summary>
    /// Sanitize filename to prevent security issues
    /// </summary>
    public static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return $"file_{Guid.NewGuid():N}";

        // Get just the filename, stripping any path components
        var name = Path.GetFileName(filename);

        // Remove null bytes
        name = name.Replace("\0", "");

        // Replace path separators that might have slipped through
        name = name.Replace('/', '_').Replace('\\', '_');

        // Remove or replace dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            name = name.Replace(c, '_');
        }

        // Collapse multiple underscores
        name = Regex.Replace(name, @"_+", "_");

        // Remove leading/trailing dots and spaces
        name = name.Trim('.', ' ', '_');

        // Ensure we have something left
        if (string.IsNullOrEmpty(name))
        {
            name = $"file_{Guid.NewGuid():N}";
        }

        // Ensure extension is preserved
        var ext = Path.GetExtension(filename);
        if (!string.IsNullOrEmpty(ext) && !name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
        {
            name = Path.GetFileNameWithoutExtension(name) + ext;
        }

        // Limit filename length (255 is typical max on most filesystems)
        if (name.Length > 200)
        {
            ext = Path.GetExtension(name);
            var baseName = Path.GetFileNameWithoutExtension(name);
            name = baseName.Substring(0, Math.Min(baseName.Length, 200 - ext.Length)) + ext;
        }

        return name;
    }

    /// <summary>
    /// Validate that the file path stays within the allowed directory
    /// </summary>
    public static bool IsPathSafe(string basePath, string filePath)
    {
        try
        {
            var fullBasePath = Path.GetFullPath(basePath);
            var fullFilePath = Path.GetFullPath(filePath);

            // Ensure the resolved path starts with the base path
            return fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate MIME type matches the file extension
    /// </summary>
    private static bool ValidateMimeType(string mimeType, string extension)
    {
        // Normalize mime type
        var normalizedMime = mimeType.ToLower().Split(';')[0].Trim();

        if (MimeTypeExtensions.TryGetValue(normalizedMime, out var allowedExtensions))
        {
            return allowedExtensions.Contains(extension.ToLower());
        }

        // If we don't have a specific mapping, allow it (already passed extension check)
        return true;
    }

    /// <summary>
    /// Check for double extensions like .pdf.exe
    /// </summary>
    private static string? GetDoubleExtension(string filename)
    {
        var parts = filename.Split('.');
        if (parts.Length >= 3)
        {
            // Get the second-to-last extension
            return "." + parts[^1];
        }
        return null;
    }

    private static string FormatFileSize(long bytes)
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

/// <summary>
/// Result of file validation
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public bool IsSuspicious { get; set; }
    public string SanitizedFilename { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
