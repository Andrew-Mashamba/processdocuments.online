using System.Text.Json;

namespace ZimaFileService.Tools;

public class FileManagementTool : IFileTool
{
    public string Name => "file_management";
    public string Description => "Manage generated and uploaded files";

    private readonly FileManager _fileManager;

    public FileManagementTool()
    {
        _fileManager = FileManager.Instance;
    }

    /// <summary>
    /// Lists files in generated_files or uploaded_files directory
    /// </summary>
    public Task<string> ListFilesAsync(Dictionary<string, object> arguments)
    {
        var folder = GetString(arguments, "folder") ?? "generated";
        var pattern = GetString(arguments, "pattern");

        var directory = folder.ToLower() switch
        {
            "uploaded" or "uploads" => _fileManager.UploadedFilesPath,
            _ => _fileManager.GeneratedFilesPath
        };

        var files = _fileManager.ListFiles(directory, pattern);

        if (files.Count == 0)
        {
            return Task.FromResult($"No files found in {folder} folder" +
                (pattern != null ? $" matching pattern '{pattern}'" : ""));
        }

        var result = $"Files in {folder} folder ({files.Count} files):\n\n";
        foreach (var file in files)
        {
            var size = FormatFileSize(file.Length);
            result += $"  - {file.Name} ({size}) - Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm}\n";
        }

        result += $"\nDirectory: {directory}";
        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets detailed information about a specific file
    /// </summary>
    public Task<string> GetFileInfoAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        // Try to resolve the path
        string resolvedPath = filePath;
        if (!Path.IsPathRooted(filePath))
        {
            // Check generated_files first, then uploaded_files
            var genPath = Path.Combine(_fileManager.GeneratedFilesPath, filePath);
            var upPath = Path.Combine(_fileManager.UploadedFilesPath, filePath);

            if (File.Exists(genPath))
                resolvedPath = genPath;
            else if (File.Exists(upPath))
                resolvedPath = upPath;
            else
                resolvedPath = genPath; // Will throw in GetFileMetadata
        }

        var metadata = _fileManager.GetFileMetadata(resolvedPath);

        var result = $"File Information:\n" +
            $"  Name: {metadata["name"]}\n" +
            $"  Path: {metadata["path"]}\n" +
            $"  Size: {metadata["sizeFormatted"]} ({metadata["size"]} bytes)\n" +
            $"  Type: {metadata["extension"]}\n" +
            $"  Created: {metadata["created"]}\n" +
            $"  Modified: {metadata["modified"]}\n" +
            $"  Read-only: {metadata["isReadOnly"]}";

        return Task.FromResult(result);
    }

    /// <summary>
    /// Deletes a file from generated_files
    /// </summary>
    public Task<string> DeleteFileAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");

        // Only allow deletion from generated_files for safety
        string resolvedPath;
        if (Path.IsPathRooted(filePath))
        {
            // Verify it's in generated_files directory
            if (!filePath.StartsWith(_fileManager.GeneratedFilesPath))
            {
                return Task.FromResult("Error: Can only delete files from generated_files directory for safety");
            }
            resolvedPath = filePath;
        }
        else
        {
            resolvedPath = Path.Combine(_fileManager.GeneratedFilesPath, filePath);
        }

        if (_fileManager.DeleteFile(resolvedPath))
        {
            return Task.FromResult($"Successfully deleted: {Path.GetFileName(resolvedPath)}");
        }
        else
        {
            return Task.FromResult($"File not found: {filePath}");
        }
    }

    /// <summary>
    /// Copies a file
    /// </summary>
    public Task<string> CopyFileAsync(Dictionary<string, object> arguments)
    {
        var sourcePath = GetString(arguments, "source_path")
            ?? throw new ArgumentException("source_path is required");
        var destPath = GetString(arguments, "dest_path")
            ?? throw new ArgumentException("dest_path is required");
        var overwrite = GetBool(arguments, "overwrite");

        // Resolve source path
        if (!Path.IsPathRooted(sourcePath))
        {
            var genPath = Path.Combine(_fileManager.GeneratedFilesPath, sourcePath);
            var upPath = Path.Combine(_fileManager.UploadedFilesPath, sourcePath);

            if (File.Exists(genPath))
                sourcePath = genPath;
            else if (File.Exists(upPath))
                sourcePath = upPath;
        }

        var resultPath = _fileManager.CopyFile(sourcePath, destPath, overwrite);
        return Task.FromResult($"File copied to: {resultPath}");
    }

    /// <summary>
    /// Moves/renames a file
    /// </summary>
    public Task<string> MoveFileAsync(Dictionary<string, object> arguments)
    {
        var sourcePath = GetString(arguments, "source_path")
            ?? throw new ArgumentException("source_path is required");
        var destPath = GetString(arguments, "dest_path")
            ?? throw new ArgumentException("dest_path is required");
        var overwrite = GetBool(arguments, "overwrite");

        // Resolve source path
        if (!Path.IsPathRooted(sourcePath))
        {
            var genPath = Path.Combine(_fileManager.GeneratedFilesPath, sourcePath);
            if (File.Exists(genPath))
                sourcePath = genPath;
        }

        var resultPath = _fileManager.MoveFile(sourcePath, destPath, overwrite);
        return Task.FromResult($"File moved to: {resultPath}");
    }

    /// <summary>
    /// Reads file content (for text files or as base64)
    /// </summary>
    public Task<string> ReadFileContentAsync(Dictionary<string, object> arguments)
    {
        var filePath = GetString(arguments, "file_path")
            ?? throw new ArgumentException("file_path is required");
        var asBase64 = GetBool(arguments, "as_base64");

        // Resolve the path
        if (!Path.IsPathRooted(filePath))
        {
            var genPath = Path.Combine(_fileManager.GeneratedFilesPath, filePath);
            var upPath = Path.Combine(_fileManager.UploadedFilesPath, filePath);

            if (File.Exists(genPath))
                filePath = genPath;
            else if (File.Exists(upPath))
                filePath = upPath;
        }

        var (content, encoding) = _fileManager.ReadFileContent(filePath, asBase64);

        if (asBase64)
        {
            return Task.FromResult($"File content (base64 encoded):\n{content}");
        }
        else
        {
            return Task.FromResult($"File content:\n{content}");
        }
    }

    /// <summary>
    /// Gets the paths for generated and uploaded files directories
    /// </summary>
    public Task<string> GetDirectoryInfoAsync(Dictionary<string, object> arguments)
    {
        var genCount = Directory.Exists(_fileManager.GeneratedFilesPath)
            ? Directory.GetFiles(_fileManager.GeneratedFilesPath).Length : 0;
        var upCount = Directory.Exists(_fileManager.UploadedFilesPath)
            ? Directory.GetFiles(_fileManager.UploadedFilesPath).Length : 0;

        var result = $"File Storage Directories:\n\n" +
            $"Working Directory: {_fileManager.WorkingDirectory}\n\n" +
            $"Generated Files:\n" +
            $"  Path: {_fileManager.GeneratedFilesPath}\n" +
            $"  Files: {genCount}\n\n" +
            $"Uploaded Files:\n" +
            $"  Path: {_fileManager.UploadedFilesPath}\n" +
            $"  Files: {upCount}";

        return Task.FromResult(result);
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

    private static bool GetBool(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.True;
            }
            if (value is bool b) return b;
        }
        return false;
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
}
