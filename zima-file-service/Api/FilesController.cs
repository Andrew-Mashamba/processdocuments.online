using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ZimaFileService.Services;

namespace ZimaFileService.Api;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly FileManager _fileManager;

    public FilesController()
    {
        _fileManager = FileManager.Instance;
    }

    /// <summary>
    /// List all generated files (flat list)
    /// </summary>
    [HttpGet]
    public IActionResult ListFiles([FromQuery] string? pattern = null, [FromQuery] bool grouped = false)
    {
        try
        {
            if (grouped)
            {
                // Return files grouped by version
                var groups = _fileManager.GetFilesGroupedByVersion();
                var result = groups.Select(g => new FileGroupDto
                {
                    BaseName = g.BaseName,
                    Extension = g.Extension,
                    VersionCount = g.VersionCount,
                    LatestVersion = new FileVersionDto
                    {
                        FileName = g.LatestVersion.FileName,
                        Version = g.LatestVersion.Version,
                        Size = g.LatestVersion.Size,
                        SizeFormatted = g.LatestVersion.SizeFormatted,
                        Created = g.LatestVersion.Created,
                        Modified = g.LatestVersion.Modified,
                        DownloadUrl = $"/api/files/{Uri.EscapeDataString(g.LatestVersion.FileName)}/download"
                    },
                    Versions = g.Versions.Select(v => new FileVersionDto
                    {
                        FileName = v.FileName,
                        Version = v.Version,
                        Size = v.Size,
                        SizeFormatted = v.SizeFormatted,
                        Created = v.Created,
                        Modified = v.Modified,
                        DownloadUrl = $"/api/files/{Uri.EscapeDataString(v.FileName)}/download"
                    }).ToList()
                }).ToList();

                return Ok(new { files = result, count = result.Count, totalVersions = result.Sum(r => r.VersionCount) });
            }
            else
            {
                // Return flat list
                var files = _fileManager.ListFiles(_fileManager.GeneratedFilesPath, pattern);
                var result = files.Select(f => new FileDto
                {
                    Name = f.Name,
                    Size = f.Length,
                    SizeFormatted = FormatFileSize(f.Length),
                    Extension = f.Extension.TrimStart('.'),
                    Created = f.CreationTime,
                    Modified = f.LastWriteTime,
                    DownloadUrl = $"/api/files/{Uri.EscapeDataString(f.Name)}/download"
                }).ToList();

                return Ok(new { files = result, count = result.Count });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get versions of a specific file
    /// </summary>
    [HttpGet("{filename}/versions")]
    public IActionResult GetFileVersions(string filename)
    {
        try
        {
            var versions = _fileManager.GetFileVersions(filename);
            var result = versions.Select(v => new FileVersionDto
            {
                FileName = v.FileName,
                Version = v.Version,
                Size = v.Size,
                SizeFormatted = v.SizeFormatted,
                Created = v.Created,
                Modified = v.Modified,
                DownloadUrl = $"/api/files/{Uri.EscapeDataString(v.FileName)}/download"
            }).ToList();

            return Ok(new { baseName = filename, versions = result, count = result.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get file info
    /// </summary>
    [HttpGet("{filename}")]
    public IActionResult GetFileInfo(string filename)
    {
        try
        {
            var filePath = Path.Combine(_fileManager.GeneratedFilesPath, filename);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var metadata = _fileManager.GetFileMetadata(filePath);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a file
    /// </summary>
    [HttpGet("download/{filename}")]
    [HttpGet("{filename}/download")]
    public IActionResult DownloadFile(string filename)
    {
        try
        {
            // Sanitize filename to prevent path traversal
            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var filePath = Path.Combine(_fileManager.GeneratedFilesPath, safeFilename);

            // Verify path is safe
            if (!FileSecurityValidator.IsPathSafe(_fileManager.GeneratedFilesPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in download: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var contentType = GetContentType(safeFilename);
            var bytes = System.IO.File.ReadAllBytes(filePath);

            SecurityAuditLogger.Instance.LogFileOperation(GetClientIp(), "DOWNLOAD", safeFilename, true);
            return File(bytes, contentType, safeFilename);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{filename}")]
    public IActionResult DeleteFile(string filename)
    {
        try
        {
            // Sanitize filename to prevent path traversal
            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var filePath = Path.Combine(_fileManager.GeneratedFilesPath, safeFilename);

            // Verify path is safe
            if (!FileSecurityValidator.IsPathSafe(_fileManager.GeneratedFilesPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in delete: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            System.IO.File.Delete(filePath);
            SecurityAuditLogger.Instance.LogFileOperation(GetClientIp(), "DELETE", safeFilename, true);
            return Ok(new { message = $"File '{safeFilename}' deleted successfully" });
        }
        catch (Exception ex)
        {
            SecurityAuditLogger.Instance.LogFileOperation(GetClientIp(), "DELETE", filename, false, ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload a file to uploaded_files
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var clientIp = GetClientIp();

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Validate the file
            var validation = FileSecurityValidator.ValidateFile(file.FileName, file.ContentType, file.Length);

            if (!validation.IsValid)
            {
                SecurityAuditLogger.Instance.LogBlockedUpload(clientIp, file.FileName,
                    string.Join("; ", validation.Errors));

                return BadRequest(new
                {
                    error = "File validation failed",
                    details = validation.Errors,
                    code = validation.IsSuspicious ? "SUSPICIOUS_FILE" : "VALIDATION_FAILED"
                });
            }

            // Use sanitized filename
            var safeFilename = validation.SanitizedFilename;
            var filePath = Path.Combine(_fileManager.UploadedFilesPath, safeFilename);

            // Verify path is safe (defense in depth)
            if (!FileSecurityValidator.IsPathSafe(_fileManager.UploadedFilesPath, filePath))
            {
                SecurityAuditLogger.Instance.LogBlockedUpload(clientIp, file.FileName, "Path traversal attempt detected");
                return BadRequest(new { error = "Invalid file path", code = "PATH_TRAVERSAL" });
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            SecurityAuditLogger.Instance.LogFileOperation(clientIp, "UPLOAD", safeFilename, true);

            return Ok(new
            {
                message = "File uploaded successfully",
                filename = safeFilename,
                originalFilename = file.FileName,
                size = file.Length,
                warnings = validation.Warnings.Count > 0 ? validation.Warnings : null
            });
        }
        catch (Exception ex)
        {
            SecurityAuditLogger.Instance.LogFileOperation(clientIp, "UPLOAD", file?.FileName ?? "unknown", false, ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List generated files grouped by session
    /// </summary>
    [HttpGet("generated/by-session")]
    public IActionResult ListGeneratedFilesBySession()
    {
        try
        {
            var groupedFiles = _fileManager.GetGeneratedFilesGroupedBySession();

            var result = groupedFiles.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(g => new FileGroupDto
                {
                    BaseName = g.BaseName,
                    Extension = g.Extension,
                    VersionCount = g.VersionCount,
                    LatestVersion = new FileVersionDto
                    {
                        FileName = g.LatestVersion.FileName,
                        Version = g.LatestVersion.Version,
                        Size = g.LatestVersion.Size,
                        SizeFormatted = g.LatestVersion.SizeFormatted,
                        Created = g.LatestVersion.Created,
                        Modified = g.LatestVersion.Modified,
                        DownloadUrl = $"/api/files/generated/{kvp.Key}/{Uri.EscapeDataString(g.LatestVersion.FileName)}/download"
                    },
                    Versions = g.Versions.Select(v => new FileVersionDto
                    {
                        FileName = v.FileName,
                        Version = v.Version,
                        Size = v.Size,
                        SizeFormatted = v.SizeFormatted,
                        Created = v.Created,
                        Modified = v.Modified,
                        DownloadUrl = $"/api/files/generated/{kvp.Key}/{Uri.EscapeDataString(v.FileName)}/download"
                    }).ToList()
                }).ToList()
            );

            return Ok(new
            {
                sessions = result,
                sessionCount = result.Count,
                totalFiles = result.Values.Sum(v => v.Count)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List generated files for a specific session
    /// </summary>
    [HttpGet("generated/{sessionId}")]
    public IActionResult ListSessionGeneratedFiles(string sessionId, [FromQuery] bool grouped = true)
    {
        try
        {
            var files = _fileManager.ListSessionGeneratedFiles(sessionId);

            if (grouped)
            {
                var groups = files
                    .GroupBy(f =>
                    {
                        var name = Path.GetFileNameWithoutExtension(f.Name);
                        var ext = f.Extension;
                        var match = System.Text.RegularExpressions.Regex.Match(name, @"^(.+)_v(\d+)$");
                        return match.Success ? match.Groups[1].Value + ext : f.Name;
                    })
                    .Select(g => new FileGroupDto
                    {
                        BaseName = g.Key,
                        Extension = g.First().Extension.TrimStart('.'),
                        VersionCount = g.Count(),
                        LatestVersion = new FileVersionDto
                        {
                            FileName = g.OrderByDescending(f => f.LastWriteTime).First().Name,
                            Version = 1,
                            Size = g.OrderByDescending(f => f.LastWriteTime).First().Length,
                            SizeFormatted = FormatFileSize(g.OrderByDescending(f => f.LastWriteTime).First().Length),
                            Created = g.OrderByDescending(f => f.LastWriteTime).First().CreationTime,
                            Modified = g.OrderByDescending(f => f.LastWriteTime).First().LastWriteTime,
                            DownloadUrl = $"/api/files/generated/{sessionId}/{Uri.EscapeDataString(g.OrderByDescending(f => f.LastWriteTime).First().Name)}/download"
                        },
                        Versions = g.OrderByDescending(f => f.LastWriteTime).Select((f, i) => new FileVersionDto
                        {
                            FileName = f.Name,
                            Version = g.Count() - i,
                            Size = f.Length,
                            SizeFormatted = FormatFileSize(f.Length),
                            Created = f.CreationTime,
                            Modified = f.LastWriteTime,
                            DownloadUrl = $"/api/files/generated/{sessionId}/{Uri.EscapeDataString(f.Name)}/download"
                        }).ToList()
                    })
                    .OrderByDescending(g => g.LatestVersion.Modified)
                    .ToList();

                return Ok(new { sessionId, files = groups, count = groups.Count });
            }
            else
            {
                var result = files.Select(f => new FileDto
                {
                    Name = f.Name,
                    Size = f.Length,
                    SizeFormatted = FormatFileSize(f.Length),
                    Extension = f.Extension.TrimStart('.'),
                    Created = f.CreationTime,
                    Modified = f.LastWriteTime,
                    DownloadUrl = $"/api/files/generated/{sessionId}/{Uri.EscapeDataString(f.Name)}/download"
                }).ToList();

                return Ok(new { sessionId, files = result, count = result.Count });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a generated file from a session
    /// </summary>
    [HttpGet("generated/{sessionId}/{filename}/download")]
    public IActionResult DownloadSessionGeneratedFile(string sessionId, string filename)
    {
        try
        {
            if (!IsValidSessionId(sessionId))
            {
                return BadRequest(new { error = "Invalid session ID format" });
            }

            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var sessionPath = _fileManager.GetSessionGeneratedPath(sessionId);
            var filePath = Path.Combine(sessionPath, safeFilename);

            if (!FileSecurityValidator.IsPathSafe(sessionPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in session download: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var contentType = GetContentType(safeFilename);
            var bytes = System.IO.File.ReadAllBytes(filePath);

            return File(bytes, contentType, safeFilename);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a generated file from a session
    /// </summary>
    [HttpDelete("generated/{sessionId}/{filename}")]
    public IActionResult DeleteSessionGeneratedFile(string sessionId, string filename)
    {
        try
        {
            if (!IsValidSessionId(sessionId))
            {
                return BadRequest(new { error = "Invalid session ID format" });
            }

            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var sessionPath = _fileManager.GetSessionGeneratedPath(sessionId);
            var filePath = Path.Combine(sessionPath, safeFilename);

            if (!FileSecurityValidator.IsPathSafe(sessionPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in session delete: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            System.IO.File.Delete(filePath);
            return Ok(new { message = $"File '{filename}' deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List uploaded files (legacy - all uploads)
    /// </summary>
    [HttpGet("uploaded")]
    public IActionResult ListUploadedFiles([FromQuery] string? pattern = null)
    {
        try
        {
            var files = _fileManager.ListFiles(_fileManager.UploadedFilesPath, pattern);
            var result = files.Select(f => new FileDto
            {
                Name = f.Name,
                Size = f.Length,
                SizeFormatted = FormatFileSize(f.Length),
                Extension = f.Extension.TrimStart('.'),
                Created = f.CreationTime,
                Modified = f.LastWriteTime,
                DownloadUrl = null
            }).ToList();

            return Ok(new { files = result, count = result.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload a file for a specific session
    /// </summary>
    [HttpPost("upload/{sessionId}")]
    public async Task<IActionResult> UploadSessionFile(string sessionId, IFormFile file)
    {
        var clientIp = GetClientIp();

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            // Validate session ID format (UUID)
            if (!IsValidSessionId(sessionId))
            {
                return BadRequest(new { error = "Invalid session ID format", code = "INVALID_SESSION_ID" });
            }

            // Validate the file
            var validation = FileSecurityValidator.ValidateFile(file.FileName, file.ContentType, file.Length);

            if (!validation.IsValid)
            {
                SecurityAuditLogger.Instance.LogBlockedUpload(clientIp, file.FileName,
                    string.Join("; ", validation.Errors));

                return BadRequest(new
                {
                    error = "File validation failed",
                    details = validation.Errors,
                    code = validation.IsSuspicious ? "SUSPICIOUS_FILE" : "VALIDATION_FAILED"
                });
            }

            // Use sanitized filename
            var safeFilename = validation.SanitizedFilename;
            var sessionPath = _fileManager.GetSessionUploadPath(sessionId);
            var filePath = Path.Combine(sessionPath, safeFilename);

            // Verify path is safe (defense in depth)
            if (!FileSecurityValidator.IsPathSafe(sessionPath, filePath))
            {
                SecurityAuditLogger.Instance.LogBlockedUpload(clientIp, file.FileName, "Path traversal attempt detected");
                return BadRequest(new { error = "Invalid file path", code = "PATH_TRAVERSAL" });
            }

            // If file exists, create versioned name
            if (System.IO.File.Exists(filePath))
            {
                filePath = _fileManager.GetVersionedFilePath(filePath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var info = new FileInfo(filePath);
            SecurityAuditLogger.Instance.LogFileOperation(clientIp, "UPLOAD", $"{sessionId}/{info.Name}", true);

            return Ok(new
            {
                success = true,
                message = "File uploaded successfully",
                file = new FileDto
                {
                    Name = info.Name,
                    Size = info.Length,
                    SizeFormatted = FormatFileSize(info.Length),
                    Extension = info.Extension.TrimStart('.'),
                    Created = info.CreationTime,
                    Modified = info.LastWriteTime,
                    DownloadUrl = $"/api/files/session/{sessionId}/{Uri.EscapeDataString(info.Name)}/download"
                },
                warnings = validation.Warnings.Count > 0 ? validation.Warnings : null
            });
        }
        catch (Exception ex)
        {
            SecurityAuditLogger.Instance.LogFileOperation(clientIp, "UPLOAD", $"{sessionId}/{file?.FileName ?? "unknown"}", false, ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List files for a specific session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public IActionResult ListSessionFiles(string sessionId, [FromQuery] string? pattern = null)
    {
        try
        {
            var files = _fileManager.ListSessionFiles(sessionId, pattern);
            var result = files.Select(f => new FileDto
            {
                Name = f.Name,
                Size = f.Length,
                SizeFormatted = FormatFileSize(f.Length),
                Extension = f.Extension.TrimStart('.'),
                Created = f.CreationTime,
                Modified = f.LastWriteTime,
                DownloadUrl = $"/api/files/session/{sessionId}/{Uri.EscapeDataString(f.Name)}/download"
            }).ToList();

            return Ok(new { sessionId, files = result, count = result.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a session file
    /// </summary>
    [HttpGet("session/{sessionId}/{filename}/download")]
    public IActionResult DownloadSessionFile(string sessionId, string filename)
    {
        try
        {
            if (!IsValidSessionId(sessionId))
            {
                return BadRequest(new { error = "Invalid session ID format" });
            }

            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var sessionPath = _fileManager.GetSessionUploadPath(sessionId);
            var filePath = Path.Combine(sessionPath, safeFilename);

            if (!FileSecurityValidator.IsPathSafe(sessionPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in session file download: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var contentType = GetContentType(safeFilename);
            var bytes = System.IO.File.ReadAllBytes(filePath);

            return File(bytes, contentType, safeFilename);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a session file
    /// </summary>
    [HttpDelete("session/{sessionId}/{filename}")]
    public IActionResult DeleteSessionFile(string sessionId, string filename)
    {
        try
        {
            if (!IsValidSessionId(sessionId))
            {
                return BadRequest(new { error = "Invalid session ID format" });
            }

            var safeFilename = FileSecurityValidator.SanitizeFilename(filename);
            var sessionPath = _fileManager.GetSessionUploadPath(sessionId);
            var filePath = Path.Combine(sessionPath, safeFilename);

            if (!FileSecurityValidator.IsPathSafe(sessionPath, filePath))
            {
                SecurityAuditLogger.Instance.LogSecurityIncident(GetClientIp(), "PATH_TRAVERSAL",
                    $"Attempted path traversal in session file delete: {filename}");
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            System.IO.File.Delete(filePath);
            SecurityAuditLogger.Instance.LogFileOperation(GetClientIp(), "DELETE", $"{sessionId}/{safeFilename}", true);
            return Ok(new { message = $"File '{safeFilename}' deleted successfully" });
        }
        catch (Exception ex)
        {
            SecurityAuditLogger.Instance.LogFileOperation(GetClientIp(), "DELETE", $"{sessionId}/{filename}", false, ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all file contents for a session (for context injection)
    /// </summary>
    [HttpGet("session/{sessionId}/context")]
    public IActionResult GetSessionContext(string sessionId)
    {
        try
        {
            var contents = _fileManager.GetSessionFileContents(sessionId);

            // Build context string for prompt injection
            var contextParts = new List<object>();
            var textContext = new System.Text.StringBuilder();

            if (contents.Count > 0)
            {
                textContext.AppendLine("=== UPLOADED FILES CONTEXT ===");
                textContext.AppendLine($"Session has {contents.Count} uploaded file(s):\n");

                foreach (var (fileName, fileContent) in contents)
                {
                    contextParts.Add(new
                    {
                        fileName,
                        mimeType = fileContent.MimeType,
                        size = fileContent.Size,
                        isText = fileContent.IsText,
                        content = fileContent.Content
                    });

                    if (fileContent.IsText)
                    {
                        textContext.AppendLine($"--- File: {fileName} ---");
                        textContext.AppendLine(fileContent.Content);
                        textContext.AppendLine();
                    }
                    else
                    {
                        textContext.AppendLine($"--- File: {fileName} ({fileContent.MimeType}) ---");
                        textContext.AppendLine(fileContent.Content);
                        textContext.AppendLine();
                    }
                }

                textContext.AppendLine("=== END UPLOADED FILES ===");
            }

            return Ok(new
            {
                sessionId,
                fileCount = contents.Count,
                files = contextParts,
                textContext = textContext.ToString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete all files for a session
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public IActionResult DeleteAllSessionFiles(string sessionId)
    {
        try
        {
            _fileManager.DeleteSessionFiles(sessionId);
            return Ok(new { message = $"All files for session '{sessionId}' deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static string GetContentType(string filename)
    {
        var ext = Path.GetExtension(filename).ToLower();
        return ext switch
        {
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
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

    /// <summary>
    /// Get the client IP address from the request
    /// </summary>
    private string GetClientIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Validate session ID format (should be UUID)
    /// </summary>
    private static bool IsValidSessionId(string sessionId)
    {
        // Allow UUID format (with or without hyphens)
        if (Guid.TryParse(sessionId, out _))
            return true;

        // Also allow alphanumeric session IDs (max 64 chars)
        if (sessionId.Length <= 64 && System.Text.RegularExpressions.Regex.IsMatch(sessionId, @"^[a-zA-Z0-9_-]+$"))
            return true;

        return false;
    }
}

public class FileDto
{
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = "";
    public string Extension { get; set; } = "";
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string? DownloadUrl { get; set; }
}

public class FileVersionDto
{
    public string FileName { get; set; } = "";
    public int Version { get; set; }
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = "";
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string DownloadUrl { get; set; } = "";
}

public class FileGroupDto
{
    public string BaseName { get; set; } = "";
    public string Extension { get; set; } = "";
    public int VersionCount { get; set; }
    public FileVersionDto LatestVersion { get; set; } = null!;
    public List<FileVersionDto> Versions { get; set; } = new();
}
