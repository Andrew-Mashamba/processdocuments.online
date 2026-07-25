using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZimaFileService.Tools;

/// <summary>
/// Text Processing Tools - merge, split, find/replace, encoding conversion, etc.
/// </summary>
public class TextProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public TextProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Merge multiple text files
    /// </summary>
    public async Task<string> MergeTextFilesAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputName = GetString(args, "output_file", "merged.txt");
        var separator = GetString(args, "separator", "\n\n---\n\n");

        if (files.Length < 2)
            throw new ArgumentException("At least 2 files required for merging");

        var outputPath = ResolvePath(outputName, true);
        var contents = new List<string>();

        foreach (var file in files)
        {
            var filePath = ResolvePath(file, false);
            var content = await File.ReadAllTextAsync(filePath);
            contents.Add(content);
        }

        var merged = string.Join(separator, contents);
        await File.WriteAllTextAsync(outputPath, merged);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} files",
            output_file = outputPath,
            total_length = merged.Length
        });
    }

    /// <summary>
    /// Split text file by lines, pattern, or size
    /// </summary>
    public async Task<string> SplitTextFileAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mode = GetString(args, "mode", "lines"); // lines, pattern, size
        var outputPrefix = GetString(args, "output_prefix", "part");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputFiles = new List<string>();

        if (mode == "lines")
        {
            var linesPerFile = GetInt(args, "lines_per_file", 1000);
            var lines = content.Split('\n');
            int fileIndex = 1;

            for (int i = 0; i < lines.Length; i += linesPerFile)
            {
                var chunk = string.Join('\n', lines.Skip(i).Take(linesPerFile));
                var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.txt");
                await File.WriteAllTextAsync(outputPath, chunk);
                outputFiles.Add(outputPath);
                fileIndex++;
            }
        }
        else if (mode == "pattern")
        {
            var pattern = GetString(args, "pattern", @"\n\n");
            var parts = Regex.Split(content, pattern);
            int fileIndex = 1;

            foreach (var part in parts.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.txt");
                await File.WriteAllTextAsync(outputPath, part.Trim());
                outputFiles.Add(outputPath);
                fileIndex++;
            }
        }
        else if (mode == "size")
        {
            var maxSize = GetInt(args, "max_size_kb", 100) * 1024;
            var bytes = Encoding.UTF8.GetBytes(content);
            int fileIndex = 1;

            for (int i = 0; i < bytes.Length; i += maxSize)
            {
                var chunk = bytes.Skip(i).Take(maxSize).ToArray();
                var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.txt");
                await File.WriteAllBytesAsync(outputPath, chunk);
                outputFiles.Add(outputPath);
                fileIndex++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split into {outputFiles.Count} files",
            output_files = outputFiles
        });
    }

    /// <summary>
    /// Find and replace text (supports regex)
    /// </summary>
    public async Task<string> FindReplaceAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var find = GetString(args, "find");
        var replace = GetString(args, "replace", "");
        var useRegex = GetBool(args, "use_regex", false);
        var caseSensitive = GetBool(args, "case_sensitive", true);
        var outputName = GetString(args, "output_file", "replaced.txt");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        string result;
        int count;

        if (useRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(find, options);
            count = regex.Matches(content).Count;
            result = regex.Replace(content, replace);
        }
        else
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            count = CountOccurrences(content, find, comparison);
            result = ReplaceString(content, find, replace, comparison);
        }

        await File.WriteAllTextAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Replaced {count} occurrences",
            output_file = outputPath,
            replacements = count
        });
    }

    /// <summary>
    /// Remove duplicate lines
    /// </summary>
    public async Task<string> RemoveDuplicateLinesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var caseSensitive = GetBool(args, "case_sensitive", true);
        var preserveOrder = GetBool(args, "preserve_order", true);
        var outputName = GetString(args, "output_file", "deduped.txt");

        var inputPath = ResolvePath(inputFile, false);
        var lines = await File.ReadAllLinesAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        IEnumerable<string> uniqueLines;
        if (preserveOrder)
        {
            var seen = new HashSet<string>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            uniqueLines = lines.Where(line => seen.Add(line));
        }
        else
        {
            uniqueLines = caseSensitive
                ? lines.Distinct()
                : lines.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        var result = uniqueLines.ToArray();
        await File.WriteAllLinesAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {lines.Length - result.Length} duplicate lines",
            output_file = outputPath,
            original_lines = lines.Length,
            unique_lines = result.Length,
            duplicates_removed = lines.Length - result.Length
        });
    }

    /// <summary>
    /// Sort lines alphabetically
    /// </summary>
    public async Task<string> SortLinesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var order = GetString(args, "order", "asc"); // asc, desc
        var caseSensitive = GetBool(args, "case_sensitive", false);
        var numeric = GetBool(args, "numeric", false);
        var outputName = GetString(args, "output_file", "sorted.txt");

        var inputPath = ResolvePath(inputFile, false);
        var lines = await File.ReadAllLinesAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        IEnumerable<string> sorted;
        if (numeric)
        {
            sorted = lines.OrderBy(l =>
            {
                if (double.TryParse(l.TrimStart(), out var num)) return num;
                return double.MaxValue;
            });
        }
        else
        {
            var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            sorted = lines.OrderBy(l => l, comparer);
        }

        if (order == "desc")
            sorted = sorted.Reverse();

        await File.WriteAllLinesAsync(outputPath, sorted);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Sorted {lines.Length} lines",
            output_file = outputPath,
            order = order,
            lines = lines.Length
        });
    }

    /// <summary>
    /// Convert case (upper, lower, title, sentence)
    /// </summary>
    public async Task<string> ConvertCaseAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var caseType = GetString(args, "case", "lower"); // upper, lower, title, sentence
        var outputName = GetString(args, "output_file", "converted.txt");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        string result = caseType switch
        {
            "upper" => content.ToUpperInvariant(),
            "lower" => content.ToLowerInvariant(),
            "title" => ToTitleCase(content),
            "sentence" => ToSentenceCase(content),
            _ => content
        };

        await File.WriteAllTextAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted to {caseType} case",
            output_file = outputPath,
            case_type = caseType
        });
    }

    /// <summary>
    /// Add line numbers
    /// </summary>
    public async Task<string> AddLineNumbersAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var format = GetString(args, "format", "{0:D4}: {1}"); // {0} = line number, {1} = content
        var startAt = GetInt(args, "start_at", 1);
        var outputName = GetString(args, "output_file", "numbered.txt");

        var inputPath = ResolvePath(inputFile, false);
        var lines = await File.ReadAllLinesAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var numbered = lines.Select((line, i) => string.Format(format, i + startAt, line));
        await File.WriteAllLinesAsync(outputPath, numbered);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added line numbers to {lines.Length} lines",
            output_file = outputPath,
            lines = lines.Length
        });
    }

    /// <summary>
    /// Compare two text files (diff)
    /// </summary>
    public async Task<string> CompareFilesAsync(Dictionary<string, object> args)
    {
        var file1 = GetString(args, "file1");
        var file2 = GetString(args, "file2");
        var outputName = GetString(args, "output_file", "diff.txt");

        var path1 = ResolvePath(file1, false);
        var path2 = ResolvePath(file2, false);
        var outputPath = ResolvePath(outputName, true);

        var lines1 = await File.ReadAllLinesAsync(path1);
        var lines2 = await File.ReadAllLinesAsync(path2);

        var diff = new StringBuilder();
        var maxLines = Math.Max(lines1.Length, lines2.Length);
        int additions = 0, deletions = 0, unchanged = 0;

        for (int i = 0; i < maxLines; i++)
        {
            var l1 = i < lines1.Length ? lines1[i] : null;
            var l2 = i < lines2.Length ? lines2[i] : null;

            if (l1 == l2)
            {
                diff.AppendLine($"  {l1}");
                unchanged++;
            }
            else if (l1 == null)
            {
                diff.AppendLine($"+ {l2}");
                additions++;
            }
            else if (l2 == null)
            {
                diff.AppendLine($"- {l1}");
                deletions++;
            }
            else
            {
                diff.AppendLine($"- {l1}");
                diff.AppendLine($"+ {l2}");
                additions++;
                deletions++;
            }
        }

        await File.WriteAllTextAsync(outputPath, diff.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Files compared",
            output_file = outputPath,
            file1_lines = lines1.Length,
            file2_lines = lines2.Length,
            additions = additions,
            deletions = deletions,
            unchanged = unchanged
        });
    }

    /// <summary>
    /// Remove extra whitespace
    /// </summary>
    public async Task<string> CleanWhitespaceAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var trimLines = GetBool(args, "trim_lines", true);
        var removeBlankLines = GetBool(args, "remove_blank_lines", false);
        var normalizeSpaces = GetBool(args, "normalize_spaces", true);
        var outputName = GetString(args, "output_file", "cleaned.txt");

        var inputPath = ResolvePath(inputFile, false);
        var lines = await File.ReadAllLinesAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var processed = lines.AsEnumerable();

        if (trimLines)
            processed = processed.Select(l => l.Trim());

        if (removeBlankLines)
            processed = processed.Where(l => !string.IsNullOrWhiteSpace(l));

        if (normalizeSpaces)
            processed = processed.Select(l => Regex.Replace(l, @"\s+", " "));

        var result = processed.ToArray();
        await File.WriteAllLinesAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Whitespace cleaned",
            output_file = outputPath,
            original_lines = lines.Length,
            result_lines = result.Length
        });
    }

    // Helper methods
    private string ToTitleCase(string text)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    private string ToSentenceCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        return string.Join(" ", sentences.Select(s =>
        {
            if (s.Length == 0) return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLower();
        }));
    }

    private int CountOccurrences(string text, string pattern, StringComparison comparison)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, comparison)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private string ReplaceString(string text, string oldValue, string newValue, StringComparison comparison)
    {
        var sb = new StringBuilder();
        int previousIndex = 0;
        int index = text.IndexOf(oldValue, comparison);

        while (index != -1)
        {
            sb.Append(text, previousIndex, index - previousIndex);
            sb.Append(newValue);
            previousIndex = index + oldValue.Length;
            index = text.IndexOf(oldValue, previousIndex, comparison);
        }
        sb.Append(text, previousIndex, text.Length - previousIndex);

        return sb.ToString();
    }

    /// <summary>
    /// Reverse text order (lines or entire content)
    /// </summary>
    public async Task<string> ReverseTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mode = GetString(args, "mode", "lines"); // lines, characters
        var outputName = GetString(args, "output_file", "reversed.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var content = await File.ReadAllTextAsync(inputPath);
        string result;

        if (mode == "lines")
        {
            var lines = content.Split('\n');
            result = string.Join("\n", lines.Reverse());
        }
        else
        {
            var chars = content.ToCharArray();
            Array.Reverse(chars);
            result = new string(chars);
        }

        await File.WriteAllTextAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Reversed text ({mode})",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Convert encoding (UTF-8, ASCII, UTF-16, etc.)
    /// </summary>
    public async Task<string> ConvertEncodingAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var fromEncoding = GetString(args, "from_encoding", "utf-8");
        var toEncoding = GetString(args, "to_encoding", "utf-8");
        var outputName = GetString(args, "output_file", "converted.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var srcEncoding = GetEncoding(fromEncoding);
        var destEncoding = GetEncoding(toEncoding);

        var bytes = await File.ReadAllBytesAsync(inputPath);
        var text = srcEncoding.GetString(bytes);
        var newBytes = destEncoding.GetBytes(text);

        await File.WriteAllBytesAsync(outputPath, newBytes);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted from {fromEncoding} to {toEncoding}",
            output_file = outputPath,
            original_bytes = bytes.Length,
            new_bytes = newBytes.Length
        });
    }

    /// <summary>
    /// Standardize line endings (CRLF, LF, CR)
    /// </summary>
    public async Task<string> StandardizeLineEndingsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var lineEnding = GetString(args, "line_ending", "lf"); // lf, crlf, cr
        var outputName = GetString(args, "output_file", "standardized.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var content = await File.ReadAllTextAsync(inputPath);

        // Normalize all line endings first
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Then convert to desired format
        var newLineEnding = lineEnding.ToLower() switch
        {
            "crlf" => "\r\n",
            "cr" => "\r",
            _ => "\n"
        };

        content = content.Replace("\n", newLineEnding);
        await File.WriteAllTextAsync(outputPath, content);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Standardized line endings to {lineEnding.ToUpper()}",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Wrap text at specified width
    /// </summary>
    public async Task<string> WrapTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var width = GetInt(args, "width", 80);
        var outputName = GetString(args, "output_file", "wrapped.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (line.Length <= width)
            {
                result.Add(line);
            }
            else
            {
                var remaining = line;
                while (remaining.Length > width)
                {
                    var breakPoint = remaining.LastIndexOf(' ', width);
                    if (breakPoint <= 0) breakPoint = width;
                    result.Add(remaining.Substring(0, breakPoint).TrimEnd());
                    remaining = remaining.Substring(breakPoint).TrimStart();
                }
                if (remaining.Length > 0) result.Add(remaining);
            }
        }

        await File.WriteAllLinesAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Wrapped text at {width} characters",
            output_file = outputPath,
            original_lines = lines.Length,
            new_lines = result.Count
        });
    }

    /// <summary>
    /// Text to JSON conversion
    /// </summary>
    public async Task<string> TextToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var format = GetString(args, "format", "lines"); // lines, key_value
        var outputName = GetString(args, "output_file", "converted.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        object result;

        if (format == "key_value")
        {
            var dict = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':', '=' }, 2);
                if (parts.Length == 2)
                {
                    dict[parts[0].Trim()] = parts[1].Trim();
                }
            }
            result = dict;
        }
        else
        {
            result = lines;
        }

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted text to JSON",
            output_file = outputPath,
            lines = lines.Length
        });
    }

    /// <summary>
    /// Extract columns from delimited text
    /// </summary>
    public async Task<string> ExtractColumnsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var columns = GetIntArray(args, "columns"); // 1-based column indices
        var delimiter = GetString(args, "delimiter", "\t");
        var outputName = GetString(args, "output_file", "columns.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        var delimChar = delimiter.Length == 1 ? delimiter[0] : '\t';
        var result = new List<string>();

        foreach (var line in lines)
        {
            var parts = line.Split(delimChar);
            var selected = columns
                .Where(c => c > 0 && c <= parts.Length)
                .Select(c => parts[c - 1])
                .ToArray();
            result.Add(string.Join(delimChar.ToString(), selected));
        }

        await File.WriteAllLinesAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {columns.Length} columns",
            output_file = outputPath,
            columns_extracted = columns
        });
    }

    /// <summary>
    /// Filter lines by pattern
    /// </summary>
    public async Task<string> FilterLinesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var pattern = GetString(args, "pattern");
        var useRegex = GetBool(args, "use_regex", false);
        var invert = GetBool(args, "invert", false); // Exclude matching lines
        var outputName = GetString(args, "output_file", "filtered.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);
        var result = new List<string>();

        Regex? regex = useRegex ? new Regex(pattern, RegexOptions.Compiled) : null;

        foreach (var line in lines)
        {
            bool matches = useRegex
                ? regex!.IsMatch(line)
                : line.Contains(pattern);

            if (invert ? !matches : matches)
            {
                result.Add(line);
            }
        }

        await File.WriteAllLinesAsync(outputPath, result);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Filtered {result.Count} lines from {lines.Length}",
            output_file = outputPath,
            original_lines = lines.Length,
            filtered_lines = result.Count
        });
    }

    /// <summary>
    /// Get text file statistics
    /// </summary>
    public async Task<string> GetTextStatsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        var content = await File.ReadAllTextAsync(inputPath);
        var lines = content.Split('\n');
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        var fileInfo = new FileInfo(inputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            size = $"{fileInfo.Length / 1024.0:F2} KB",
            lines = lines.Length,
            words = words.Length,
            characters = content.Length,
            characters_no_spaces = content.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Length,
            average_line_length = lines.Length > 0 ? content.Length / lines.Length : 0
        });
    }

    private Encoding GetEncoding(string name)
    {
        return name.ToLower() switch
        {
            "utf-8" or "utf8" => Encoding.UTF8,
            "ascii" => Encoding.ASCII,
            "utf-16" or "utf16" or "unicode" => Encoding.Unicode,
            "utf-32" or "utf32" => Encoding.UTF32,
            "latin1" or "iso-8859-1" => Encoding.Latin1,
            _ => Encoding.UTF8
        };
    }

    /// <summary>
    /// Convert text to HTML
    /// </summary>
    public async Task<string> TextToHtmlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var title = GetString(args, "title", "Document");
        var preserveNewlines = GetBool(args, "preserve_newlines", true);
        var outputName = GetString(args, "output_file", "converted.html");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var content = await File.ReadAllTextAsync(inputPath);
        content = System.Web.HttpUtility.HtmlEncode(content);

        if (preserveNewlines)
        {
            content = content.Replace("\r\n", "<br>\n").Replace("\n", "<br>\n");
        }

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>{System.Web.HttpUtility.HtmlEncode(title)}</title>
    <style>
        body {{ font-family: 'Courier New', monospace; padding: 20px; line-height: 1.6; }}
    </style>
</head>
<body>
<pre>{content}</pre>
</body>
</html>";

        await File.WriteAllTextAsync(outputPath, html);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted text to HTML",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Convert text to XML
    /// </summary>
    public async Task<string> TextToXmlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var rootElement = GetString(args, "root_element", "document");
        var lineElement = GetString(args, "line_element", "line");
        var outputName = GetString(args, "output_file", "converted.xml");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var lines = await File.ReadAllLinesAsync(inputPath);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<{rootElement}>");

        int lineNum = 1;
        foreach (var line in lines)
        {
            var escapedLine = System.Security.SecurityElement.Escape(line);
            sb.AppendLine($"  <{lineElement} number=\"{lineNum}\">{escapedLine}</{lineElement}>");
            lineNum++;
        }

        sb.AppendLine($"</{rootElement}>");
        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted text to XML",
            output_file = outputPath,
            lines = lines.Length
        });
    }

    /// <summary>
    /// Strip HTML to plain text
    /// </summary>
    public async Task<string> HtmlToTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var preserveLinks = GetBool(args, "preserve_links", false);
        var outputName = GetString(args, "output_file", "converted.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var html = await File.ReadAllTextAsync(inputPath);

        // Remove scripts and styles
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Replace common elements with text equivalents
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<p[^>]*>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<div[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<li[^>]*>", "\n• ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (preserveLinks)
        {
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<a[^>]*href=[""']([^""']*)[""'][^>]*>([^<]*)</a>", "$2 ($1)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Remove all remaining tags
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");

        // Decode HTML entities
        html = System.Web.HttpUtility.HtmlDecode(html);

        // Clean up whitespace
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\n\s*\n\s*\n", "\n\n");
        html = html.Trim();

        await File.WriteAllTextAsync(outputPath, html);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted HTML to text",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Parse XML to plain text
    /// </summary>
    public async Task<string> XmlToTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var includeAttributes = GetBool(args, "include_attributes", false);
        var outputName = GetString(args, "output_file", "converted.txt");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var xml = await File.ReadAllTextAsync(inputPath);

        // Extract text content from XML elements
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var sb = new StringBuilder();

        void ExtractText(System.Xml.Linq.XElement element, int depth)
        {
            var indent = new string(' ', depth * 2);

            if (includeAttributes && element.HasAttributes)
            {
                var attrs = string.Join(", ", element.Attributes().Select(a => $"{a.Name}={a.Value}"));
                sb.AppendLine($"{indent}[{element.Name}: {attrs}]");
            }

            if (!string.IsNullOrWhiteSpace(element.Value) && !element.HasElements)
            {
                sb.AppendLine($"{indent}{element.Value.Trim()}");
            }

            foreach (var child in element.Elements())
            {
                ExtractText(child, depth + 1);
            }
        }

        if (doc.Root != null)
        {
            ExtractText(doc.Root, 0);
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted XML to text",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Compress text file using GZIP
    /// </summary>
    public async Task<string> CompressTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);
        if (string.IsNullOrEmpty(outputName))
            outputName = Path.GetFileName(inputPath) + ".gz";
        var outputPath = ResolvePath(outputName, true);

        var originalSize = new FileInfo(inputPath).Length;

        await using var inputStream = File.OpenRead(inputPath);
        await using var outputStream = File.Create(outputPath);
        await using var gzipStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionLevel.Optimal);

        await inputStream.CopyToAsync(gzipStream);

        var compressedSize = new FileInfo(outputPath).Length;
        var ratio = (1 - (double)compressedSize / originalSize) * 100;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Compressed file (saved {ratio:F1}%)",
            output_file = outputPath,
            original_size = FormatSize(originalSize),
            compressed_size = FormatSize(compressedSize),
            compression_ratio = $"{ratio:F1}%"
        });
    }

    /// <summary>
    /// Decompress GZIP file
    /// </summary>
    public async Task<string> DecompressTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);
        if (string.IsNullOrEmpty(outputName))
            outputName = Path.GetFileNameWithoutExtension(inputPath);
        var outputPath = ResolvePath(outputName, true);

        await using var inputStream = File.OpenRead(inputPath);
        await using var gzipStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
        await using var outputStream = File.Create(outputPath);

        await gzipStream.CopyToAsync(outputStream);

        var decompressedSize = new FileInfo(outputPath).Length;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Decompressed file",
            output_file = outputPath,
            decompressed_size = FormatSize(decompressedSize)
        });
    }

    /// <summary>
    /// Encrypt text file using AES
    /// </summary>
    public async Task<string> EncryptTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var outputName = GetString(args, "output_file", "encrypted.enc");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required for encryption");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var content = await File.ReadAllBytesAsync(inputPath);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = DeriveKey(password, 32);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(content, 0, content.Length);

        // Write IV + encrypted data
        await using var outputStream = File.Create(outputPath);
        await outputStream.WriteAsync(aes.IV);
        await outputStream.WriteAsync(encrypted);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "File encrypted with AES-256",
            output_file = outputPath,
            algorithm = "AES-256-CBC"
        });
    }

    /// <summary>
    /// Decrypt text file
    /// </summary>
    public async Task<string> DecryptTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var outputName = GetString(args, "output_file", "decrypted.txt");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required for decryption");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var allBytes = await File.ReadAllBytesAsync(inputPath);

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = DeriveKey(password, 32);
        aes.IV = allBytes[..16];

        var encrypted = allBytes[16..];

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

        await File.WriteAllBytesAsync(outputPath, decrypted);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "File decrypted",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Calculate file checksum (MD5, SHA1, SHA256, SHA512)
    /// </summary>
    public async Task<string> CalculateChecksumAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var algorithm = GetString(args, "algorithm", "sha256"); // md5, sha1, sha256, sha512

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllBytesAsync(inputPath);

        byte[] hash;
        using (var hasher = algorithm.ToLower() switch
        {
            "md5" => (System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.MD5.Create(),
            "sha1" => System.Security.Cryptography.SHA1.Create(),
            "sha512" => System.Security.Cryptography.SHA512.Create(),
            _ => System.Security.Cryptography.SHA256.Create()
        })
        {
            hash = hasher.ComputeHash(content);
        }

        var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            algorithm = algorithm.ToUpper(),
            checksum = checksum,
            file_size = FormatSize(content.Length)
        });
    }

    /// <summary>
    /// Validate checksum against expected value
    /// </summary>
    public async Task<string> ValidateChecksumAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var expected = GetString(args, "expected");
        var algorithm = GetString(args, "algorithm", "sha256");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllBytesAsync(inputPath);

        byte[] hash;
        using (var hasher = algorithm.ToLower() switch
        {
            "md5" => (System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.MD5.Create(),
            "sha1" => System.Security.Cryptography.SHA1.Create(),
            "sha512" => System.Security.Cryptography.SHA512.Create(),
            _ => System.Security.Cryptography.SHA256.Create()
        })
        {
            hash = hasher.ComputeHash(content);
        }

        var actual = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        var isValid = actual.Equals(expected.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(new
        {
            success = true,
            valid = isValid,
            file = inputPath,
            algorithm = algorithm.ToUpper(),
            expected = expected.ToLowerInvariant(),
            actual = actual,
            message = isValid ? "Checksum matches" : "Checksum mismatch"
        });
    }

    private byte[] DeriveKey(string password, int keySize)
    {
        using var derive = new System.Security.Cryptography.Rfc2898DeriveBytes(
            password,
            new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64 },
            100000,
            System.Security.Cryptography.HashAlgorithmName.SHA256);
        return derive.GetBytes(keySize);
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }

    private int[] GetIntArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<int>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        return Array.Empty<int>();
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
        if (value is JsonElement je) return je.GetInt32();
        return Convert.ToInt32(value);
    }

    private bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetBoolean();
        return Convert.ToBoolean(value);
    }

    private string[] GetStringArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        return Array.Empty<string>();
    }
}
