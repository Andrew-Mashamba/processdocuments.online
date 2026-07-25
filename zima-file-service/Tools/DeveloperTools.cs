using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace ZimaFileService.Tools;

/// <summary>
/// Developer Tools - JSON formatting, CSS/HTML minification, encoding, hashing, etc.
/// </summary>
public class DeveloperTools
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public DeveloperTools()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    #region JSON Tools

    /// <summary>
    /// Format/beautify JSON with proper indentation
    /// </summary>
    public async Task<string> FormatJsonAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var indent = GetInt(args, "indent", 2);
        var outputFile = GetString(args, "output_file", null);

        string jsonContent;

        // Check if input is a file path or raw JSON
        if (input.EndsWith(".json") || File.Exists(ResolvePath(input, false)))
        {
            jsonContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            jsonContent = input;
        }

        var doc = JsonDocument.Parse(jsonContent);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var formatted = JsonSerializer.Serialize(doc, options);

        // Custom indentation if not 2
        if (indent != 2)
        {
            var indentStr = new string(' ', indent);
            formatted = Regex.Replace(formatted, @"^(\s+)", m =>
            {
                var spaces = m.Groups[1].Value.Length / 2;
                return string.Concat(Enumerable.Repeat(indentStr, spaces));
            }, RegexOptions.Multiline);
        }

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, formatted);
            return JsonSerializer.Serialize(new { success = true, output_file = outputPath });
        }

        return JsonSerializer.Serialize(new { success = true, result = formatted });
    }

    /// <summary>
    /// Minify JSON by removing whitespace
    /// </summary>
    public async Task<string> MinifyJsonAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var outputFile = GetString(args, "output_file", null);

        string jsonContent;
        if (input.EndsWith(".json") || File.Exists(ResolvePath(input, false)))
        {
            jsonContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            jsonContent = input;
        }

        var doc = JsonDocument.Parse(jsonContent);
        var minified = JsonSerializer.Serialize(doc);

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, minified);
            return JsonSerializer.Serialize(new { success = true, output_file = outputPath });
        }

        return JsonSerializer.Serialize(new { success = true, result = minified });
    }

    #endregion

    #region CSS Tools

    /// <summary>
    /// Minify CSS by removing whitespace and comments
    /// </summary>
    public async Task<string> MinifyCssAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var outputFile = GetString(args, "output_file", null);

        string cssContent;
        if (input.EndsWith(".css") || File.Exists(ResolvePath(input, false)))
        {
            cssContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            cssContent = input;
        }

        // Remove comments
        var minified = Regex.Replace(cssContent, @"/\*[\s\S]*?\*/", "");
        // Remove whitespace
        minified = Regex.Replace(minified, @"\s+", " ");
        // Remove spaces around special characters
        minified = Regex.Replace(minified, @"\s*([{};:,>~+])\s*", "$1");
        // Remove trailing semicolons before closing braces
        minified = Regex.Replace(minified, @";\s*}", "}");
        minified = minified.Trim();

        var originalSize = cssContent.Length;
        var minifiedSize = minified.Length;
        var savings = (1 - (double)minifiedSize / originalSize) * 100;

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, minified);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                original_size = originalSize,
                minified_size = minifiedSize,
                savings_percent = Math.Round(savings, 2)
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            result = minified,
            original_size = originalSize,
            minified_size = minifiedSize,
            savings_percent = Math.Round(savings, 2)
        });
    }

    /// <summary>
    /// Format/beautify CSS
    /// </summary>
    public async Task<string> FormatCssAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var indent = GetInt(args, "indent", 2);
        var outputFile = GetString(args, "output_file", null);

        string cssContent;
        if (input.EndsWith(".css") || File.Exists(ResolvePath(input, false)))
        {
            cssContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            cssContent = input;
        }

        var indentStr = new string(' ', indent);
        var sb = new StringBuilder();
        var depth = 0;
        var inString = false;
        var stringChar = '\0';

        for (int i = 0; i < cssContent.Length; i++)
        {
            var c = cssContent[i];

            if (inString)
            {
                sb.Append(c);
                if (c == stringChar && cssContent[i - 1] != '\\')
                    inString = false;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                inString = true;
                stringChar = c;
                sb.Append(c);
                continue;
            }

            switch (c)
            {
                case '{':
                    sb.Append(" {\n");
                    depth++;
                    sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                    break;
                case '}':
                    depth = Math.Max(0, depth - 1);
                    sb.Append("\n");
                    sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                    sb.Append("}\n");
                    if (depth > 0)
                        sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                    break;
                case ';':
                    sb.Append(";\n");
                    if (depth > 0)
                        sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                    break;
                case '\n':
                case '\r':
                case '\t':
                    break;
                default:
                    if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] == ' ')
                        continue;
                    sb.Append(c);
                    break;
            }
        }

        var formatted = sb.ToString().Trim();

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, formatted);
            return JsonSerializer.Serialize(new { success = true, output_file = outputPath });
        }

        return JsonSerializer.Serialize(new { success = true, result = formatted });
    }

    #endregion

    #region HTML Tools

    /// <summary>
    /// Minify HTML by removing whitespace and comments
    /// </summary>
    public async Task<string> MinifyHtmlAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var removeComments = GetBool(args, "remove_comments", true);
        var outputFile = GetString(args, "output_file", null);

        string htmlContent;
        if (input.EndsWith(".html") || input.EndsWith(".htm") || File.Exists(ResolvePath(input, false)))
        {
            htmlContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            htmlContent = input;
        }

        var minified = htmlContent;

        // Remove HTML comments
        if (removeComments)
        {
            minified = Regex.Replace(minified, @"<!--[\s\S]*?-->", "");
        }

        // Remove whitespace between tags
        minified = Regex.Replace(minified, @">\s+<", "><");
        // Remove leading/trailing whitespace
        minified = Regex.Replace(minified, @"^\s+|\s+$", "", RegexOptions.Multiline);
        // Collapse multiple spaces
        minified = Regex.Replace(minified, @"\s{2,}", " ");

        var originalSize = htmlContent.Length;
        var minifiedSize = minified.Length;
        var savings = (1 - (double)minifiedSize / originalSize) * 100;

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, minified);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                original_size = originalSize,
                minified_size = minifiedSize,
                savings_percent = Math.Round(savings, 2)
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            result = minified,
            original_size = originalSize,
            minified_size = minifiedSize,
            savings_percent = Math.Round(savings, 2)
        });
    }

    /// <summary>
    /// Format/beautify HTML
    /// </summary>
    public async Task<string> FormatHtmlAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var indent = GetInt(args, "indent", 2);
        var outputFile = GetString(args, "output_file", null);

        string htmlContent;
        if (input.EndsWith(".html") || input.EndsWith(".htm") || File.Exists(ResolvePath(input, false)))
        {
            htmlContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            htmlContent = input;
        }

        var indentStr = new string(' ', indent);
        var depth = 0;
        var sb = new StringBuilder();

        // Simple HTML formatter using regex
        var tagPattern = @"(<[^>]+>)";
        var parts = Regex.Split(htmlContent, tagPattern);

        var selfClosingTags = new HashSet<string> { "br", "hr", "img", "input", "meta", "link", "area", "base", "col", "embed", "keygen", "param", "source", "track", "wbr" };

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (part.StartsWith("<"))
            {
                var tagMatch = Regex.Match(part, @"</?(\w+)");
                var tagName = tagMatch.Success ? tagMatch.Groups[1].Value.ToLower() : "";
                var isClosing = part.StartsWith("</");
                var isSelfClosing = part.EndsWith("/>") || selfClosingTags.Contains(tagName);

                if (isClosing)
                    depth = Math.Max(0, depth - 1);

                sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                sb.Append(part);
                sb.AppendLine();

                if (!isClosing && !isSelfClosing)
                    depth++;
            }
            else
            {
                var text = part.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(string.Concat(Enumerable.Repeat(indentStr, depth)));
                    sb.Append(text);
                    sb.AppendLine();
                }
            }
        }

        var formatted = sb.ToString().TrimEnd();

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, formatted);
            return JsonSerializer.Serialize(new { success = true, output_file = outputPath });
        }

        return JsonSerializer.Serialize(new { success = true, result = formatted });
    }

    #endregion

    #region JavaScript Tools

    /// <summary>
    /// Minify JavaScript (basic - removes comments and extra whitespace)
    /// </summary>
    public async Task<string> MinifyJsAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var outputFile = GetString(args, "output_file", null);

        string jsContent;
        if (input.EndsWith(".js") || File.Exists(ResolvePath(input, false)))
        {
            jsContent = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            jsContent = input;
        }

        // Remove single-line comments (careful with URLs)
        var minified = Regex.Replace(jsContent, @"(?<!:)//[^\n]*", "");
        // Remove multi-line comments
        minified = Regex.Replace(minified, @"/\*[\s\S]*?\*/", "");
        // Remove newlines
        minified = Regex.Replace(minified, @"\n+", "\n");
        // Remove leading whitespace
        minified = Regex.Replace(minified, @"^\s+", "", RegexOptions.Multiline);
        // Collapse multiple spaces
        minified = Regex.Replace(minified, @"\s{2,}", " ");
        // Remove spaces around operators
        minified = Regex.Replace(minified, @"\s*([=+\-*/<>!&|;,{}()\[\]])\s*", "$1");
        minified = minified.Trim();

        var originalSize = jsContent.Length;
        var minifiedSize = minified.Length;
        var savings = (1 - (double)minifiedSize / originalSize) * 100;

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, minified);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                original_size = originalSize,
                minified_size = minifiedSize,
                savings_percent = Math.Round(savings, 2)
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            result = minified,
            original_size = originalSize,
            minified_size = minifiedSize,
            savings_percent = Math.Round(savings, 2)
        });
    }

    #endregion

    #region Encoding Tools

    /// <summary>
    /// Encode string to Base64
    /// </summary>
    public async Task<string> Base64EncodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var urlSafe = GetBool(args, "url_safe", false);

        string content;
        if (File.Exists(ResolvePath(input, false)))
        {
            var bytes = await File.ReadAllBytesAsync(ResolvePath(input, false));
            content = Convert.ToBase64String(bytes);
        }
        else
        {
            content = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        if (urlSafe)
        {
            content = content.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        return JsonSerializer.Serialize(new { success = true, result = content });
    }

    /// <summary>
    /// Decode Base64 string
    /// </summary>
    public async Task<string> Base64DecodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var outputFile = GetString(args, "output_file", null);

        // Handle URL-safe base64
        var base64 = input.Replace("-", "+").Replace("_", "/");
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var bytes = Convert.FromBase64String(base64);

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllBytesAsync(outputPath, bytes);
            return JsonSerializer.Serialize(new { success = true, output_file = outputPath });
        }

        var result = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Serialize(new { success = true, result });
    }

    /// <summary>
    /// URL encode a string
    /// </summary>
    public Task<string> UrlEncodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var result = HttpUtility.UrlEncode(input);
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result }));
    }

    /// <summary>
    /// URL decode a string
    /// </summary>
    public Task<string> UrlDecodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var result = HttpUtility.UrlDecode(input);
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result }));
    }

    /// <summary>
    /// HTML encode a string
    /// </summary>
    public Task<string> HtmlEncodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var result = HttpUtility.HtmlEncode(input);
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result }));
    }

    /// <summary>
    /// HTML decode a string
    /// </summary>
    public Task<string> HtmlDecodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var result = HttpUtility.HtmlDecode(input);
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result }));
    }

    /// <summary>
    /// Convert string to hexadecimal
    /// </summary>
    public Task<string> StringToHexAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var bytes = Encoding.UTF8.GetBytes(input);
        var hex = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result = hex }));
    }

    /// <summary>
    /// Convert hexadecimal to string
    /// </summary>
    public Task<string> HexToStringAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input").Replace(" ", "");
        var bytes = new byte[input.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
        }
        var result = Encoding.UTF8.GetString(bytes);
        return Task.FromResult(JsonSerializer.Serialize(new { success = true, result }));
    }

    #endregion

    #region Hashing Tools

    /// <summary>
    /// Generate hash of input (MD5, SHA1, SHA256, SHA512)
    /// </summary>
    public async Task<string> GenerateHashAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var algorithm = GetString(args, "algorithm", "sha256").ToLower();

        byte[] inputBytes;
        if (File.Exists(ResolvePath(input, false)))
        {
            inputBytes = await File.ReadAllBytesAsync(ResolvePath(input, false));
        }
        else
        {
            inputBytes = Encoding.UTF8.GetBytes(input);
        }

        byte[] hashBytes;
        using HashAlgorithm hasher = algorithm switch
        {
            "md5" => MD5.Create(),
            "sha1" => SHA1.Create(),
            "sha256" => SHA256.Create(),
            "sha384" => SHA384.Create(),
            "sha512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        hashBytes = hasher.ComputeHash(inputBytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return JsonSerializer.Serialize(new { success = true, algorithm, hash });
    }

    /// <summary>
    /// Generate HMAC hash
    /// </summary>
    public Task<string> GenerateHmacAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var key = GetString(args, "key");
        var algorithm = GetString(args, "algorithm", "sha256").ToLower();

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);

        byte[] hashBytes;
        using HMAC hmac = algorithm switch
        {
            "md5" => new HMACMD5(keyBytes),
            "sha1" => new HMACSHA1(keyBytes),
            "sha256" => new HMACSHA256(keyBytes),
            "sha384" => new HMACSHA384(keyBytes),
            "sha512" => new HMACSHA512(keyBytes),
            _ => new HMACSHA256(keyBytes)
        };

        hashBytes = hmac.ComputeHash(inputBytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return Task.FromResult(JsonSerializer.Serialize(new { success = true, algorithm = $"hmac-{algorithm}", hash }));
    }

    #endregion

    #region UUID/GUID Tools

    /// <summary>
    /// Generate UUID/GUID
    /// </summary>
    public Task<string> GenerateUuidAsync(Dictionary<string, object> args)
    {
        var count = GetInt(args, "count", 1);
        var format = GetString(args, "format", "D"); // N, D, B, P, X
        var uppercase = GetBool(args, "uppercase", false);

        var uuids = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var uuid = Guid.NewGuid().ToString(format);
            if (uppercase)
                uuid = uuid.ToUpper();
            uuids.Add(uuid);
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            uuids = count == 1 ? (object)uuids[0] : uuids
        }));
    }

    /// <summary>
    /// Validate UUID/GUID
    /// </summary>
    public Task<string> ValidateUuidAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var isValid = Guid.TryParse(input, out var guid);

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            input,
            is_valid = isValid,
            normalized = isValid ? guid.ToString("D") : null
        }));
    }

    #endregion

    #region Diff/Compare Tools

    /// <summary>
    /// Compare two texts and show differences
    /// </summary>
    public async Task<string> DiffTextAsync(Dictionary<string, object> args)
    {
        var text1 = GetString(args, "text1");
        var text2 = GetString(args, "text2");
        var outputFile = GetString(args, "output_file", null);

        string content1, content2;

        if (File.Exists(ResolvePath(text1, false)))
        {
            content1 = await File.ReadAllTextAsync(ResolvePath(text1, false));
        }
        else
        {
            content1 = text1;
        }

        if (File.Exists(ResolvePath(text2, false)))
        {
            content2 = await File.ReadAllTextAsync(ResolvePath(text2, false));
        }
        else
        {
            content2 = text2;
        }

        var lines1 = content1.Split('\n');
        var lines2 = content2.Split('\n');

        var sb = new StringBuilder();
        var maxLines = Math.Max(lines1.Length, lines2.Length);
        var additions = 0;
        var deletions = 0;
        var changes = 0;

        for (int i = 0; i < maxLines; i++)
        {
            var line1 = i < lines1.Length ? lines1[i] : null;
            var line2 = i < lines2.Length ? lines2[i] : null;

            if (line1 == line2)
            {
                sb.AppendLine($"  {line1}");
            }
            else if (line1 == null)
            {
                sb.AppendLine($"+ {line2}");
                additions++;
            }
            else if (line2 == null)
            {
                sb.AppendLine($"- {line1}");
                deletions++;
            }
            else
            {
                sb.AppendLine($"- {line1}");
                sb.AppendLine($"+ {line2}");
                changes++;
            }
        }

        var diff = sb.ToString();

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = ResolvePath(outputFile, true);
            await File.WriteAllTextAsync(outputPath, diff);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                additions,
                deletions,
                changes
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            diff,
            additions,
            deletions,
            changes
        });
    }

    #endregion

    #region Lorem Ipsum Generator

    /// <summary>
    /// Generate Lorem Ipsum placeholder text
    /// </summary>
    public Task<string> GenerateLoremIpsumAsync(Dictionary<string, object> args)
    {
        var type = GetString(args, "type", "paragraphs"); // words, sentences, paragraphs
        var count = GetInt(args, "count", 3);
        var startWithLorem = GetBool(args, "start_with_lorem", true);

        var words = new[] {
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
            "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
            "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud",
            "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo",
            "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
            "velit", "esse", "cillum", "fugiat", "nulla", "pariatur", "excepteur", "sint",
            "occaecat", "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia",
            "deserunt", "mollit", "anim", "id", "est", "laborum"
        };

        var random = new Random();
        var result = new StringBuilder();

        if (startWithLorem && type != "words")
        {
            result.Append("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ");
        }

        switch (type)
        {
            case "words":
                for (int i = 0; i < count; i++)
                {
                    if (i == 0 && startWithLorem)
                        result.Append("Lorem ");
                    else
                        result.Append(words[random.Next(words.Length)] + " ");
                }
                break;

            case "sentences":
                for (int i = 0; i < count; i++)
                {
                    var sentenceLength = random.Next(8, 15);
                    var sentence = new StringBuilder();
                    for (int j = 0; j < sentenceLength; j++)
                    {
                        var word = words[random.Next(words.Length)];
                        if (j == 0)
                            word = char.ToUpper(word[0]) + word.Substring(1);
                        sentence.Append(word);
                        if (j < sentenceLength - 1)
                            sentence.Append(" ");
                    }
                    result.Append(sentence.ToString() + ". ");
                }
                break;

            case "paragraphs":
            default:
                for (int p = 0; p < count; p++)
                {
                    var sentences = random.Next(4, 8);
                    for (int i = 0; i < sentences; i++)
                    {
                        var sentenceLength = random.Next(8, 15);
                        var sentence = new StringBuilder();
                        for (int j = 0; j < sentenceLength; j++)
                        {
                            var word = words[random.Next(words.Length)];
                            if (j == 0)
                                word = char.ToUpper(word[0]) + word.Substring(1);
                            sentence.Append(word);
                            if (j < sentenceLength - 1)
                                sentence.Append(" ");
                        }
                        result.Append(sentence.ToString() + ". ");
                    }
                    if (p < count - 1)
                        result.AppendLine().AppendLine();
                }
                break;
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            result = result.ToString().Trim()
        }));
    }

    #endregion

    #region Code Stats

    /// <summary>
    /// Count lines of code in a file or text
    /// </summary>
    public async Task<string> CountLinesOfCodeAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");

        string content;
        if (File.Exists(ResolvePath(input, false)))
        {
            content = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            content = input;
        }

        var lines = content.Split('\n');
        var totalLines = lines.Length;
        var blankLines = lines.Count(l => string.IsNullOrWhiteSpace(l));
        var commentLines = lines.Count(l => {
            var trimmed = l.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#") ||
                   trimmed.StartsWith("/*") || trimmed.StartsWith("*") ||
                   trimmed.StartsWith("<!--");
        });
        var codeLines = totalLines - blankLines - commentLines;

        return JsonSerializer.Serialize(new {
            success = true,
            total_lines = totalLines,
            code_lines = codeLines,
            blank_lines = blankLines,
            comment_lines = commentLines
        });
    }

    #endregion

    #region Timestamp Tools

    /// <summary>
    /// Convert between timestamp formats
    /// </summary>
    public Task<string> ConvertTimestampAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input", null);
        var fromFormat = GetString(args, "from", "auto"); // unix, iso, auto
        var toFormat = GetString(args, "to", "all"); // unix, iso, all

        DateTime dateTime;

        if (string.IsNullOrEmpty(input))
        {
            dateTime = DateTime.UtcNow;
        }
        else if (fromFormat == "unix" || (fromFormat == "auto" && long.TryParse(input, out _)))
        {
            var timestamp = long.Parse(input);
            // Handle both seconds and milliseconds
            if (timestamp > 10000000000)
                dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            else
                dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }
        else
        {
            dateTime = DateTime.Parse(input, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        var result = new Dictionary<string, object>
        {
            ["success"] = true,
            ["input"] = input ?? "now"
        };

        if (toFormat == "unix" || toFormat == "all")
        {
            result["unix_seconds"] = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            result["unix_milliseconds"] = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        if (toFormat == "iso" || toFormat == "all")
        {
            result["iso8601"] = dateTime.ToString("o");
            result["rfc2822"] = dateTime.ToString("R");
            result["readable"] = dateTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }

        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    #endregion

    #region Regex Tools

    /// <summary>
    /// Test regex pattern against input
    /// </summary>
    public Task<string> TestRegexAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var pattern = GetString(args, "pattern");
        var flags = GetString(args, "flags", "");

        var options = RegexOptions.None;
        if (flags.Contains("i")) options |= RegexOptions.IgnoreCase;
        if (flags.Contains("m")) options |= RegexOptions.Multiline;
        if (flags.Contains("s")) options |= RegexOptions.Singleline;

        try
        {
            var regex = new Regex(pattern, options);
            var matches = regex.Matches(input);

            var matchList = new List<object>();
            foreach (Match match in matches)
            {
                var groups = new List<object>();
                foreach (Group group in match.Groups)
                {
                    groups.Add(new { value = group.Value, index = group.Index, length = group.Length });
                }
                matchList.Add(new {
                    value = match.Value,
                    index = match.Index,
                    length = match.Length,
                    groups
                });
            }

            return Task.FromResult(JsonSerializer.Serialize(new {
                success = true,
                pattern,
                is_match = matches.Count > 0,
                match_count = matches.Count,
                matches = matchList
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = $"Invalid regex pattern: {ex.Message}"
            }));
        }
    }

    #endregion

    #region Helper Methods

    private string ResolvePath(string path, bool isOutput)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (Path.IsPathRooted(path))
            return path;

        // Check uploaded files first for input
        if (!isOutput)
        {
            var uploadedPath = Path.Combine(_uploadedPath, path);
            if (File.Exists(uploadedPath))
                return uploadedPath;

            var generatedPath = Path.Combine(_generatedPath, path);
            if (File.Exists(generatedPath))
                return generatedPath;
        }

        // For output or if not found, use generated path
        return Path.Combine(_generatedPath, path);
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

    #endregion
}
