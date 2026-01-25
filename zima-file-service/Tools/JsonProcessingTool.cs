using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;

namespace ZimaFileService.Tools;

/// <summary>
/// JSON Processing Tools - format, validate, merge, convert, query, etc.
/// </summary>
public class JsonProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public JsonProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Format/beautify JSON
    /// </summary>
    public async Task<string> FormatJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var indent = GetInt(args, "indent", 2);
        var outputName = GetString(args, "output_file", "formatted.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(content);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var formatted = json?.ToJsonString(options) ?? content;
        await File.WriteAllTextAsync(outputPath, formatted);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "JSON formatted",
            output_file = outputPath,
            original_size = content.Length,
            formatted_size = formatted.Length
        });
    }

    /// <summary>
    /// Minify JSON (remove whitespace)
    /// </summary>
    public async Task<string> MinifyJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "minified.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(content);
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var minified = json?.ToJsonString(options) ?? content;
        await File.WriteAllTextAsync(outputPath, minified);

        var savings = ((double)(content.Length - minified.Length) / content.Length) * 100;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"JSON minified, saved {savings:F1}%",
            output_file = outputPath,
            original_size = content.Length,
            minified_size = minified.Length,
            savings_percent = Math.Round(savings, 1)
        });
    }

    /// <summary>
    /// Validate JSON syntax
    /// </summary>
    public async Task<string> ValidateJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);

        try
        {
            var json = JsonNode.Parse(content);
            var nodeType = json switch
            {
                JsonObject => "object",
                JsonArray arr => $"array ({arr.Count} items)",
                JsonValue => "value",
                _ => "unknown"
            };

            return JsonSerializer.Serialize(new
            {
                success = true,
                valid = true,
                message = "JSON is valid",
                file = inputPath,
                type = nodeType,
                size = content.Length
            });
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                valid = false,
                message = "JSON is invalid",
                error = ex.Message,
                file = inputPath
            });
        }
    }

    /// <summary>
    /// Merge multiple JSON files
    /// </summary>
    public async Task<string> MergeJsonAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var mode = GetString(args, "mode", "array"); // array, merge_objects
        var outputName = GetString(args, "output_file", "merged.json");

        if (files.Length < 2)
            throw new ArgumentException("At least 2 JSON files required");

        var outputPath = ResolvePath(outputName, true);
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (mode == "array")
        {
            var array = new JsonArray();
            foreach (var file in files)
            {
                var path = ResolvePath(file, false);
                var content = await File.ReadAllTextAsync(path);
                var node = JsonNode.Parse(content);
                if (node != null) array.Add(node);
            }
            await File.WriteAllTextAsync(outputPath, array.ToJsonString(options));
        }
        else // merge_objects
        {
            var merged = new JsonObject();
            foreach (var file in files)
            {
                var path = ResolvePath(file, false);
                var content = await File.ReadAllTextAsync(path);
                var obj = JsonNode.Parse(content)?.AsObject();
                if (obj != null)
                {
                    foreach (var prop in obj)
                    {
                        merged[prop.Key] = prop.Value?.DeepClone();
                    }
                }
            }
            await File.WriteAllTextAsync(outputPath, merged.ToJsonString(options));
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} JSON files",
            output_file = outputPath,
            mode = mode
        });
    }

    /// <summary>
    /// Split large JSON array
    /// </summary>
    public async Task<string> SplitJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var itemsPerFile = GetInt(args, "items_per_file", 100);
        var outputPrefix = GetString(args, "output_prefix", "part");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var array = JsonNode.Parse(content)?.AsArray();

        if (array == null)
            throw new ArgumentException("Input must be a JSON array");

        var outputFiles = new List<string>();
        var options = new JsonSerializerOptions { WriteIndented = true };
        int fileIndex = 1;

        for (int i = 0; i < array.Count; i += itemsPerFile)
        {
            var chunk = new JsonArray();
            for (int j = i; j < Math.Min(i + itemsPerFile, array.Count); j++)
            {
                chunk.Add(array[j]?.DeepClone());
            }

            var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.json");
            await File.WriteAllTextAsync(outputPath, chunk.ToJsonString(options));
            outputFiles.Add(outputPath);
            fileIndex++;
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split into {outputFiles.Count} files",
            output_files = outputFiles,
            total_items = array.Count,
            items_per_file = itemsPerFile
        });
    }

    /// <summary>
    /// Query JSON with path expressions
    /// </summary>
    public async Task<string> QueryJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var path = GetString(args, "path"); // e.g., "users[0].name" or "items.*.price"
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var root = JsonNode.Parse(content);

        var results = QueryPath(root, path);
        var options = new JsonSerializerOptions { WriteIndented = true };

        string resultJson;
        if (results.Count == 1)
            resultJson = results[0]?.ToJsonString(options) ?? "null";
        else
        {
            var array = new JsonArray();
            foreach (var r in results) array.Add(r?.DeepClone());
            resultJson = array.ToJsonString(options);
        }

        if (!string.IsNullOrEmpty(outputName))
        {
            var outputPath = ResolvePath(outputName, true);
            await File.WriteAllTextAsync(outputPath, resultJson);
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Found {results.Count} results",
                output_file = outputPath,
                path = path,
                results_count = results.Count
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Found {results.Count} results",
            path = path,
            results_count = results.Count,
            results = JsonNode.Parse(resultJson)
        });
    }

    /// <summary>
    /// Sort JSON keys
    /// </summary>
    public async Task<string> SortKeysAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var recursive = GetBool(args, "recursive", true);
        var outputName = GetString(args, "output_file", "sorted.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(content);
        var sorted = SortJsonKeys(json, recursive);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, sorted?.ToJsonString(options) ?? "null");

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "JSON keys sorted",
            output_file = outputPath,
            recursive = recursive
        });
    }

    /// <summary>
    /// Flatten nested JSON
    /// </summary>
    public async Task<string> FlattenJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var separator = GetString(args, "separator", ".");
        var outputName = GetString(args, "output_file", "flattened.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(content);
        var flattened = new JsonObject();
        FlattenNode(json, "", separator, flattened);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, flattened.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Flattened to {flattened.Count} keys",
            output_file = outputPath,
            keys_count = flattened.Count
        });
    }

    /// <summary>
    /// Convert JSON to CSV
    /// </summary>
    public async Task<string> JsonToCsvAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var delimiter = GetString(args, "delimiter", ",");
        var outputName = GetString(args, "output_file", "");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);

        if (string.IsNullOrEmpty(outputName))
            outputName = Path.GetFileNameWithoutExtension(inputFile) + ".csv";
        var outputPath = ResolvePath(outputName, true);

        var array = JsonNode.Parse(content)?.AsArray();
        if (array == null || array.Count == 0)
            throw new ArgumentException("Input must be a non-empty JSON array");

        // Collect all keys
        var keys = new HashSet<string>();
        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                foreach (var key in obj.Select(p => p.Key))
                    keys.Add(key);
            }
        }

        var headers = keys.ToList();
        var sb = new StringBuilder();

        // Write headers
        sb.AppendLine(string.Join(delimiter, headers.Select(h => EscapeCsv(h, delimiter))));

        // Write rows
        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                var values = headers.Select(h =>
                {
                    if (obj.TryGetPropertyValue(h, out var val))
                        return EscapeCsv(val?.ToString() ?? "", delimiter);
                    return "";
                });
                sb.AppendLine(string.Join(delimiter, values));
            }
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted to CSV with {array.Count} rows",
            output_file = outputPath,
            rows = array.Count,
            columns = headers.Count
        });
    }

    /// <summary>
    /// Remove specific keys from JSON
    /// </summary>
    public async Task<string> RemoveKeysAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var keysToRemove = GetStringArray(args, "keys");
        var recursive = GetBool(args, "recursive", true);
        var outputName = GetString(args, "output_file", "filtered.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(content);
        var keysSet = new HashSet<string>(keysToRemove);
        var removed = RemoveJsonKeys(json, keysSet, recursive);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, json?.ToJsonString(options) ?? "null");

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {removed} occurrences of specified keys",
            output_file = outputPath,
            keys_removed = keysToRemove,
            occurrences_removed = removed
        });
    }

    /// <summary>
    /// CSV to JSON conversion
    /// </summary>
    public async Task<string> CsvToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var delimiter = GetString(args, "delimiter", ",");
        var hasHeader = GetBool(args, "has_header", true);
        var outputName = GetString(args, "output_file", "converted.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);
        var delimChar = delimiter.Length == 1 ? delimiter[0] : ',';

        var lines = await File.ReadAllLinesAsync(inputPath);
        var result = new JsonArray();

        string[]? headers = null;
        int startIdx = 0;

        if (hasHeader && lines.Length > 0)
        {
            headers = ParseCsvLine(lines[0], delimChar);
            startIdx = 1;
        }

        for (int i = startIdx; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i], delimChar);
            var obj = new JsonObject();

            for (int j = 0; j < values.Length; j++)
            {
                var key = hasHeader && headers != null && j < headers.Length ? headers[j] : $"column{j + 1}";
                obj[key] = JsonValue.Create(values[j]);
            }

            result.Add(obj);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted CSV to JSON with {result.Count} records",
            output_file = outputPath,
            records = result.Count
        });
    }

    /// <summary>
    /// XML to JSON conversion
    /// </summary>
    public async Task<string> XmlToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var xml = await File.ReadAllTextAsync(inputPath);
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var json = XmlToJsonNode(doc.Root);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, json?.ToJsonString(options) ?? "{}");

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted XML to JSON",
            output_file = outputPath
        });
    }

    /// <summary>
    /// JSON to XML conversion
    /// </summary>
    public async Task<string> JsonToXmlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var rootElement = GetString(args, "root_element", "root");
        var outputName = GetString(args, "output_file", "converted.xml");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var json = await File.ReadAllTextAsync(inputPath);
        var node = JsonNode.Parse(json);

        var xml = new System.Xml.Linq.XElement(rootElement);
        JsonToXmlElement(node, xml);

        var doc = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XDeclaration("1.0", "utf-8", null),
            xml
        );

        await File.WriteAllTextAsync(outputPath, doc.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted JSON to XML",
            output_file = outputPath
        });
    }

    /// <summary>
    /// JSON Schema validation
    /// </summary>
    public async Task<string> ValidateSchemaAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var schemaFile = GetString(args, "schema_file", "");
        var schemaJson = GetString(args, "schema", "");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);

        // Simple schema validation (type checking)
        string? schemaContent = null;
        if (!string.IsNullOrEmpty(schemaFile))
        {
            var schemaPath = ResolvePath(schemaFile, false);
            schemaContent = await File.ReadAllTextAsync(schemaPath);
        }
        else if (!string.IsNullOrEmpty(schemaJson))
        {
            schemaContent = schemaJson;
        }

        var json = JsonNode.Parse(content);
        var schema = schemaContent != null ? JsonNode.Parse(schemaContent) : null;

        var errors = new List<string>();
        if (schema != null)
        {
            ValidateAgainstSchema(json, schema, "", errors);
        }

        return JsonSerializer.Serialize(new
        {
            success = errors.Count == 0,
            valid = errors.Count == 0,
            error_count = errors.Count,
            errors = errors.Take(10).ToArray(), // Return first 10 errors
            message = errors.Count == 0 ? "JSON is valid against schema" : $"Found {errors.Count} validation errors"
        });
    }

    /// <summary>
    /// Get JSON statistics
    /// </summary>
    public async Task<string> GetJsonStatsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);

        var json = JsonNode.Parse(content);
        var stats = new JsonStats();
        CountJsonStats(json, stats);

        var fileInfo = new FileInfo(inputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            size = $"{fileInfo.Length / 1024.0:F2} KB",
            root_type = json switch
            {
                JsonObject => "object",
                JsonArray => "array",
                _ => "value"
            },
            total_objects = stats.Objects,
            total_arrays = stats.Arrays,
            total_values = stats.Values,
            total_keys = stats.Keys,
            max_depth = stats.MaxDepth,
            null_values = stats.NullValues
        });
    }

    /// <summary>
    /// Remove duplicate objects from JSON array
    /// </summary>
    public async Task<string> RemoveDuplicatesJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var keyField = GetString(args, "key_field", ""); // Field to use for deduplication
        var outputName = GetString(args, "output_file", "deduped.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var array = JsonNode.Parse(content)?.AsArray();
        if (array == null)
            throw new ArgumentException("Input must be a JSON array");

        var seen = new HashSet<string>();
        var result = new JsonArray();
        var removed = 0;

        foreach (var item in array)
        {
            string key;
            if (!string.IsNullOrEmpty(keyField) && item is JsonObject obj && obj.TryGetPropertyValue(keyField, out var val))
            {
                key = val?.ToString() ?? "";
            }
            else
            {
                key = item?.ToJsonString() ?? "";
            }

            if (seen.Add(key))
            {
                result.Add(item?.DeepClone());
            }
            else
            {
                removed++;
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {removed} duplicates",
            output_file = outputPath,
            original_count = array.Count,
            final_count = result.Count,
            removed = removed
        });
    }

    /// <summary>
    /// Transform JSON using simple mappings
    /// </summary>
    public async Task<string> TransformJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mappings = GetStringDictionary(args, "mappings"); // {"new_key": "old_path"}
        var outputName = GetString(args, "output_file", "transformed.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var source = JsonNode.Parse(content);

        if (source is JsonArray array)
        {
            var result = new JsonArray();
            foreach (var item in array)
            {
                var transformed = TransformObject(item, mappings);
                result.Add(transformed);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Transformed {array.Count} records",
                output_file = outputPath
            });
        }
        else
        {
            var transformed = TransformObject(source, mappings);
            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(outputPath, transformed?.ToJsonString(options) ?? "{}");

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Transformed JSON object",
                output_file = outputPath
            });
        }
    }

    /// <summary>
    /// Convert YAML to JSON
    /// </summary>
    public async Task<string> YamlToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var yamlContent = await File.ReadAllTextAsync(inputPath);

        // Simple YAML to JSON conversion (basic key: value format)
        var result = new JsonObject();
        var lines = yamlContent.Split('\n');
        var currentIndent = 0;
        var stack = new Stack<(int indent, JsonObject obj, string key)>();
        stack.Push((0, result, ""));

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

            var indent = line.Length - line.TrimStart().Length;
            var content = trimmed.Trim();

            // Handle key: value pairs
            var colonIdx = content.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = content.Substring(0, colonIdx).Trim();
                var value = colonIdx < content.Length - 1 ? content.Substring(colonIdx + 1).Trim() : "";

                // Pop stack to correct level
                while (stack.Count > 1 && stack.Peek().indent >= indent)
                    stack.Pop();

                var parent = stack.Peek().obj;

                if (string.IsNullOrEmpty(value))
                {
                    // Nested object
                    var newObj = new JsonObject();
                    parent[key] = newObj;
                    stack.Push((indent, newObj, key));
                }
                else
                {
                    // Simple value
                    if (value == "true") parent[key] = true;
                    else if (value == "false") parent[key] = false;
                    else if (value == "null") parent[key] = null;
                    else if (double.TryParse(value, out var num)) parent[key] = num;
                    else parent[key] = value.Trim('"', '\'');
                }
            }
            else if (content.StartsWith("- "))
            {
                // List item - simplified handling
                var listValue = content.Substring(2).Trim();
                var parent = stack.Peek().obj;
                var lastKey = stack.Peek().key;

                if (!string.IsNullOrEmpty(lastKey) && parent[lastKey] is JsonArray arr)
                {
                    arr.Add(listValue);
                }
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted YAML to JSON",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Convert JSON to YAML
    /// </summary>
    public async Task<string> JsonToYamlAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "converted.yaml");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var json = JsonNode.Parse(jsonContent);

        var sb = new StringBuilder();
        JsonToYamlRecursive(json, sb, 0);

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted JSON to YAML",
            output_file = outputPath
        });
    }

    private void JsonToYamlRecursive(JsonNode? node, StringBuilder sb, int indent)
    {
        var indentStr = new string(' ', indent * 2);

        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                if (prop.Value is JsonObject || prop.Value is JsonArray)
                {
                    sb.AppendLine($"{indentStr}{prop.Key}:");
                    JsonToYamlRecursive(prop.Value, sb, indent + 1);
                }
                else
                {
                    var value = prop.Value?.ToString() ?? "null";
                    if (prop.Value is JsonValue v && v.TryGetValue<string>(out var str))
                        value = str.Contains(' ') || str.Contains(':') ? $"\"{str}\"" : str;
                    sb.AppendLine($"{indentStr}{prop.Key}: {value}");
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is JsonObject || item is JsonArray)
                {
                    sb.AppendLine($"{indentStr}-");
                    JsonToYamlRecursive(item, sb, indent + 1);
                }
                else
                {
                    sb.AppendLine($"{indentStr}- {item?.ToString() ?? "null"}");
                }
            }
        }
    }

    /// <summary>
    /// Convert JSON to HTML table
    /// </summary>
    public async Task<string> JsonToHtmlTableAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var title = GetString(args, "title", "Data Table");
        var includeStyles = GetBool(args, "include_styles", true);
        var outputName = GetString(args, "output_file", "table.html");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var json = JsonNode.Parse(jsonContent);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine($"<title>{System.Web.HttpUtility.HtmlEncode(title)}</title>");
        sb.AppendLine("<meta charset=\"UTF-8\">");

        if (includeStyles)
        {
            sb.AppendLine(@"<style>
table { border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; }
th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
th { background-color: #4CAF50; color: white; }
tr:nth-child(even) { background-color: #f2f2f2; }
tr:hover { background-color: #ddd; }
</style>");
        }

        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>{System.Web.HttpUtility.HtmlEncode(title)}</h1>");

        if (json is JsonArray arr && arr.Count > 0)
        {
            sb.AppendLine("<table>");

            // Get headers from first object
            var headers = new List<string>();
            if (arr[0] is JsonObject firstObj)
            {
                headers = firstObj.Select(p => p.Key).ToList();
            }

            sb.AppendLine("<thead><tr>");
            foreach (var header in headers)
            {
                sb.AppendLine($"<th>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
            }
            sb.AppendLine("</tr></thead>");

            sb.AppendLine("<tbody>");
            foreach (var item in arr)
            {
                sb.AppendLine("<tr>");
                if (item is JsonObject obj)
                {
                    foreach (var header in headers)
                    {
                        var value = obj[header]?.ToString() ?? "";
                        sb.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(value)}</td>");
                    }
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
        }
        else if (json is JsonObject singleObj)
        {
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>Property</th><th>Value</th></tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var prop in singleObj)
            {
                sb.AppendLine($"<tr><td>{System.Web.HttpUtility.HtmlEncode(prop.Key)}</td><td>{System.Web.HttpUtility.HtmlEncode(prop.Value?.ToString() ?? "")}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</body></html>");
        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted JSON to HTML table",
            output_file = outputPath
        });
    }

    /// <summary>
    /// Encrypt JSON file
    /// </summary>
    public async Task<string> EncryptJsonAsync(Dictionary<string, object> args)
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

        await using var outputStream = File.Create(outputPath);
        await outputStream.WriteAsync(aes.IV);
        await outputStream.WriteAsync(encrypted);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "JSON encrypted with AES-256",
            output_file = outputPath,
            algorithm = "AES-256-CBC"
        });
    }

    /// <summary>
    /// Decrypt JSON file
    /// </summary>
    public async Task<string> DecryptJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var outputName = GetString(args, "output_file", "decrypted.json");

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
            message = "JSON decrypted",
            output_file = outputPath
        });
    }

    /// <summary>
    /// JSON array operations (concat, slice, reverse, shuffle)
    /// </summary>
    public async Task<string> ArrayOperationsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var operation = GetString(args, "operation", "slice"); // concat, slice, reverse, shuffle, unique
        var outputName = GetString(args, "output_file", "result.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var array = JsonNode.Parse(jsonContent)?.AsArray();

        if (array == null)
            throw new ArgumentException("Input must be a JSON array");

        JsonArray result;

        switch (operation)
        {
            case "concat":
                var secondFile = GetString(args, "second_file", "");
                if (string.IsNullOrEmpty(secondFile))
                    throw new ArgumentException("second_file is required for concat operation");

                var secondPath = ResolvePath(secondFile, false);
                var secondContent = await File.ReadAllTextAsync(secondPath);
                var secondArray = JsonNode.Parse(secondContent)?.AsArray();

                result = new JsonArray();
                foreach (var item in array) result.Add(item?.DeepClone());
                if (secondArray != null)
                    foreach (var item in secondArray) result.Add(item?.DeepClone());
                break;

            case "slice":
                var start = GetInt(args, "start", 0);
                var end = GetInt(args, "end", array.Count);
                result = new JsonArray();
                for (int i = Math.Max(0, start); i < Math.Min(end, array.Count); i++)
                    result.Add(array[i]?.DeepClone());
                break;

            case "reverse":
                result = new JsonArray();
                for (int i = array.Count - 1; i >= 0; i--)
                    result.Add(array[i]?.DeepClone());
                break;

            case "shuffle":
                var random = new Random();
                var items = array.Select(x => x?.DeepClone()).OrderBy(_ => random.Next()).ToList();
                result = new JsonArray();
                foreach (var item in items) result.Add(item);
                break;

            case "unique":
                var seen = new HashSet<string>();
                result = new JsonArray();
                foreach (var item in array)
                {
                    var key = item?.ToJsonString() ?? "";
                    if (seen.Add(key))
                        result.Add(item?.DeepClone());
                }
                break;

            default:
                throw new ArgumentException($"Unknown operation: {operation}");
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Applied {operation} operation",
            output_file = outputPath,
            original_count = array.Count,
            result_count = result.Count
        });
    }

    #region Repair, SQL to JSON, JSON to PDF, Digital Signature

    /// <summary>
    /// Repair invalid/malformed JSON
    /// </summary>
    public async Task<string> RepairJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "repaired.json");

        var inputPath = ResolvePath(inputFile, false);
        var content = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var repairs = new List<string>();
        var repaired = content;

        // 1. Remove BOM if present
        if (repaired.StartsWith("\uFEFF"))
        {
            repaired = repaired.Substring(1);
            repairs.Add("Removed BOM");
        }

        // 2. Fix common issues
        // Remove trailing commas before ] or }
        var trailingCommaPattern = new Regex(@",\s*([}\]])");
        if (trailingCommaPattern.IsMatch(repaired))
        {
            repaired = trailingCommaPattern.Replace(repaired, "$1");
            repairs.Add("Removed trailing commas");
        }

        // 3. Fix single quotes to double quotes (for property names and strings)
        var singleQuoteCount = Regex.Matches(repaired, @"(?<![\\])'").Count;
        var doubleQuoteCount = Regex.Matches(repaired, @"(?<![\\])""").Count;
        if (singleQuoteCount > doubleQuoteCount)
        {
            // Replace single quotes with double quotes carefully
            repaired = Regex.Replace(repaired, @"'([^']*)'(?=\s*:)", "\"$1\""); // Keys
            repaired = Regex.Replace(repaired, @":\s*'([^']*)'", ": \"$1\""); // String values
            repairs.Add("Converted single quotes to double quotes");
        }

        // 4. Fix unquoted property names
        var unquotedKeyPattern = new Regex(@"(?<=[\{,])\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*:");
        if (unquotedKeyPattern.IsMatch(repaired))
        {
            repaired = unquotedKeyPattern.Replace(repaired, " \"$1\":");
            repairs.Add("Quoted unquoted property names");
        }

        // 5. Fix missing commas between elements
        repaired = Regex.Replace(repaired, @"(\}|\]|""|\d)\s*\n\s*([\{\[\""a-zA-Z])", "$1,\n$2");
        if (repaired != content && !repairs.Contains("Added missing commas"))
            repairs.Add("Added missing commas");

        // 6. Fix JavaScript-style comments
        if (repaired.Contains("//") || repaired.Contains("/*"))
        {
            repaired = Regex.Replace(repaired, @"//[^\n]*", ""); // Single-line comments
            repaired = Regex.Replace(repaired, @"/\*[\s\S]*?\*/", ""); // Multi-line comments
            repairs.Add("Removed JavaScript comments");
        }

        // 7. Fix undefined/NaN/Infinity
        repaired = Regex.Replace(repaired, @"\bundefined\b", "null");
        repaired = Regex.Replace(repaired, @"\bNaN\b", "null");
        repaired = Regex.Replace(repaired, @"\bInfinity\b", "null");
        repaired = Regex.Replace(repaired, @"-Infinity\b", "null");

        // 8. Try to parse and reformat
        try
        {
            var json = JsonNode.Parse(repaired);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            repaired = json?.ToJsonString(options) ?? repaired;
            repairs.Add("Validated and formatted");

            await File.WriteAllTextAsync(outputPath, repaired);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "JSON repaired successfully",
                output_file = outputPath,
                repairs = repairs,
                original_size = content.Length,
                repaired_size = repaired.Length
            });
        }
        catch (JsonException ex)
        {
            // Save the partially repaired content
            await File.WriteAllTextAsync(outputPath, repaired);

            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Partial repair - some issues remain",
                output_file = outputPath,
                repairs = repairs,
                remaining_error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convert SQL query/table definition to JSON structure
    /// </summary>
    public async Task<string> SqlToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file", "");
        var sqlQuery = GetString(args, "sql", "");
        var mode = GetString(args, "mode", "parse"); // parse, mock_data, schema
        var rowCount = GetInt(args, "row_count", 10);
        var outputName = GetString(args, "output_file", "sql_result.json");

        string sql;
        if (!string.IsNullOrEmpty(inputFile))
        {
            var inputPath = ResolvePath(inputFile, false);
            sql = await File.ReadAllTextAsync(inputPath);
        }
        else if (!string.IsNullOrEmpty(sqlQuery))
        {
            sql = sqlQuery;
        }
        else
        {
            throw new ArgumentException("Either 'file' or 'sql' parameter is required");
        }

        var outputPath = ResolvePath(outputName, true);
        sql = sql.Trim();

        JsonNode result;

        if (mode == "schema")
        {
            // Parse CREATE TABLE statement to JSON schema
            result = ParseCreateTableToSchema(sql);
        }
        else if (mode == "mock_data")
        {
            // Generate mock data based on SELECT or CREATE TABLE
            result = GenerateMockDataFromSql(sql, rowCount);
        }
        else // parse
        {
            // Parse SQL to structured JSON representation
            result = ParseSqlToJson(sql);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"SQL converted to JSON ({mode} mode)",
            output_file = outputPath,
            mode = mode
        });
    }

    private JsonObject ParseSqlToJson(string sql)
    {
        var result = new JsonObject
        {
            ["original_sql"] = sql,
            ["type"] = DetectSqlType(sql)
        };

        var sqlUpper = sql.ToUpper();

        if (sqlUpper.StartsWith("SELECT"))
        {
            result["columns"] = ExtractSelectColumns(sql);
            result["tables"] = ExtractTables(sql);
            result["where"] = ExtractWhereClause(sql);
            result["order_by"] = ExtractOrderBy(sql);
        }
        else if (sqlUpper.StartsWith("CREATE TABLE"))
        {
            var schema = ParseCreateTableToSchema(sql);
            result["table_name"] = schema["table_name"];
            result["columns"] = schema["columns"];
        }
        else if (sqlUpper.StartsWith("INSERT"))
        {
            result["table"] = ExtractInsertTable(sql);
            result["values"] = ExtractInsertValues(sql);
        }

        return result;
    }

    private string DetectSqlType(string sql)
    {
        var upper = sql.ToUpper().TrimStart();
        if (upper.StartsWith("SELECT")) return "SELECT";
        if (upper.StartsWith("INSERT")) return "INSERT";
        if (upper.StartsWith("UPDATE")) return "UPDATE";
        if (upper.StartsWith("DELETE")) return "DELETE";
        if (upper.StartsWith("CREATE TABLE")) return "CREATE_TABLE";
        if (upper.StartsWith("ALTER")) return "ALTER";
        if (upper.StartsWith("DROP")) return "DROP";
        return "UNKNOWN";
    }

    private JsonArray ExtractSelectColumns(string sql)
    {
        var columns = new JsonArray();
        var match = Regex.Match(sql, @"SELECT\s+(.+?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
            var colsPart = match.Groups[1].Value;
            foreach (var col in colsPart.Split(','))
            {
                var trimmed = col.Trim();
                var aliasMatch = Regex.Match(trimmed, @"(.+?)\s+AS\s+(.+)", RegexOptions.IgnoreCase);
                if (aliasMatch.Success)
                {
                    columns.Add(new JsonObject
                    {
                        ["expression"] = aliasMatch.Groups[1].Value.Trim(),
                        ["alias"] = aliasMatch.Groups[2].Value.Trim()
                    });
                }
                else
                {
                    columns.Add(trimmed);
                }
            }
        }
        return columns;
    }

    private JsonArray ExtractTables(string sql)
    {
        var tables = new JsonArray();
        var match = Regex.Match(sql, @"FROM\s+(.+?)(?:\s+WHERE|\s+ORDER|\s+GROUP|\s+HAVING|\s+LIMIT|;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
            var tablesPart = match.Groups[1].Value;
            foreach (var table in tablesPart.Split(','))
            {
                tables.Add(table.Trim().Split(' ')[0]);
            }
        }
        return tables;
    }

    private string? ExtractWhereClause(string sql)
    {
        var match = Regex.Match(sql, @"WHERE\s+(.+?)(?:\s+ORDER|\s+GROUP|\s+HAVING|\s+LIMIT|;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? ExtractOrderBy(string sql)
    {
        var match = Regex.Match(sql, @"ORDER\s+BY\s+(.+?)(?:\s+LIMIT|;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? ExtractInsertTable(string sql)
    {
        var match = Regex.Match(sql, @"INSERT\s+INTO\s+(\w+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private JsonArray ExtractInsertValues(string sql)
    {
        var values = new JsonArray();
        var match = Regex.Match(sql, @"VALUES\s*\((.+)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
            foreach (var val in match.Groups[1].Value.Split(','))
            {
                values.Add(val.Trim().Trim('\'', '"'));
            }
        }
        return values;
    }

    private JsonObject ParseCreateTableToSchema(string sql)
    {
        var result = new JsonObject();

        var tableMatch = Regex.Match(sql, @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(\w+)", RegexOptions.IgnoreCase);
        if (tableMatch.Success)
        {
            result["table_name"] = tableMatch.Groups[1].Value;
        }

        var columns = new JsonArray();
        var colsMatch = Regex.Match(sql, @"\((.+)\)", RegexOptions.Singleline);
        if (colsMatch.Success)
        {
            var colDefs = colsMatch.Groups[1].Value;
            var colMatches = Regex.Matches(colDefs, @"(\w+)\s+([\w\(\)]+)(?:\s+(NOT\s+NULL|NULL|PRIMARY\s+KEY|UNIQUE|DEFAULT\s+.+?))?(?:,|$)", RegexOptions.IgnoreCase);

            foreach (Match m in colMatches)
            {
                var col = new JsonObject
                {
                    ["name"] = m.Groups[1].Value,
                    ["type"] = m.Groups[2].Value.ToUpper()
                };

                if (m.Groups[3].Success)
                {
                    var constraint = m.Groups[3].Value.ToUpper();
                    if (constraint.Contains("PRIMARY")) col["primary_key"] = true;
                    if (constraint.Contains("NOT NULL")) col["nullable"] = false;
                    if (constraint.Contains("UNIQUE")) col["unique"] = true;
                }

                columns.Add(col);
            }
        }

        result["columns"] = columns;
        return result;
    }

    private JsonArray GenerateMockDataFromSql(string sql, int rowCount)
    {
        var result = new JsonArray();
        var schema = ParseCreateTableToSchema(sql);
        var columns = schema["columns"]?.AsArray();

        if (columns == null || columns.Count == 0)
        {
            // Try to extract from SELECT
            var selectCols = ExtractSelectColumns(sql);
            if (selectCols.Count > 0)
            {
                for (int i = 0; i < rowCount; i++)
                {
                    var row = new JsonObject();
                    foreach (var col in selectCols)
                    {
                        var colName = col is JsonObject obj ? obj["alias"]?.ToString() ?? obj["expression"]?.ToString() : col?.ToString();
                        row[colName ?? $"col_{i}"] = GenerateMockValue("VARCHAR", i);
                    }
                    result.Add(row);
                }
                return result;
            }
        }

        for (int i = 0; i < rowCount; i++)
        {
            var row = new JsonObject();
            if (columns != null)
            {
                foreach (var col in columns)
                {
                    if (col is JsonObject colObj)
                    {
                        var name = colObj["name"]?.ToString() ?? "column";
                        var type = colObj["type"]?.ToString() ?? "VARCHAR";
                        row[name] = GenerateMockValue(type, i);
                    }
                }
            }
            result.Add(row);
        }

        return result;
    }

    private JsonNode GenerateMockValue(string sqlType, int index)
    {
        var type = sqlType.ToUpper();
        var random = new Random(index);

        if (type.Contains("INT"))
            return JsonValue.Create(random.Next(1, 10000));
        if (type.Contains("DECIMAL") || type.Contains("FLOAT") || type.Contains("DOUBLE") || type.Contains("NUMERIC"))
            return JsonValue.Create(Math.Round(random.NextDouble() * 1000, 2));
        if (type.Contains("BOOL"))
            return JsonValue.Create(random.Next(2) == 1);
        if (type.Contains("DATE"))
            return JsonValue.Create(DateTime.Now.AddDays(-random.Next(365)).ToString("yyyy-MM-dd"));
        if (type.Contains("TIME"))
            return JsonValue.Create(DateTime.Now.AddHours(-random.Next(24)).ToString("HH:mm:ss"));

        // Default to string
        var sampleStrings = new[] { "Sample", "Test", "Data", "Value", "Item", "Record" };
        return JsonValue.Create($"{sampleStrings[random.Next(sampleStrings.Length)]}_{index + 1}");
    }

    /// <summary>
    /// Convert JSON to PDF document
    /// </summary>
    public async Task<string> JsonToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var title = GetString(args, "title", "JSON Data");
        var style = GetString(args, "style", "table"); // table, tree, cards
        var pageSize = GetString(args, "page_size", "A4");
        var landscape = GetBool(args, "landscape", false);
        var outputName = GetString(args, "output_file", "output.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(jsonContent);

        using var writer = new PdfWriter(outputPath);
        var pdfPageSize = pageSize.ToUpper() switch
        {
            "LETTER" => iText.Kernel.Geom.PageSize.LETTER,
            "LEGAL" => iText.Kernel.Geom.PageSize.LEGAL,
            "A3" => iText.Kernel.Geom.PageSize.A3,
            "A5" => iText.Kernel.Geom.PageSize.A5,
            _ => iText.Kernel.Geom.PageSize.A4
        };

        if (landscape)
            pdfPageSize = pdfPageSize.Rotate();

        using var pdfDoc = new PdfDocument(writer);
        pdfDoc.SetDefaultPageSize(pdfPageSize);
        using var document = new Document(pdfDoc);

        // Add title
        document.Add(new Paragraph(title)
            .SetFontSize(18)
            .SetBold()
            .SetMarginBottom(20));

        // Add timestamp
        document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .SetFontSize(10)
            .SetFontColor(ColorConstants.GRAY)
            .SetMarginBottom(15));

        switch (style)
        {
            case "table":
                AddJsonAsTable(document, json);
                break;
            case "tree":
                AddJsonAsTree(document, json, 0);
                break;
            case "cards":
                AddJsonAsCards(document, json);
                break;
            default:
                AddJsonAsTable(document, json);
                break;
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "JSON converted to PDF",
            output_file = outputPath,
            style = style,
            page_size = pageSize
        });
    }

    private void AddJsonAsTable(Document document, JsonNode? json)
    {
        if (json is JsonArray arr && arr.Count > 0)
        {
            // Get all unique keys
            var keys = new List<string>();
            foreach (var item in arr)
            {
                if (item is JsonObject obj)
                {
                    foreach (var key in obj.Select(p => p.Key))
                    {
                        if (!keys.Contains(key))
                            keys.Add(key);
                    }
                }
            }

            if (keys.Count == 0) return;

            var table = new Table(keys.Count).UseAllAvailableWidth();

            // Add headers
            foreach (var key in keys)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(key).SetBold())
                    .SetBackgroundColor(new DeviceRgb(66, 139, 202))
                    .SetFontColor(ColorConstants.WHITE)
                    .SetPadding(5));
            }

            // Add data rows
            bool alternate = false;
            foreach (var item in arr)
            {
                if (item is JsonObject obj)
                {
                    foreach (var key in keys)
                    {
                        var value = obj[key]?.ToString() ?? "";
                        var cell = new Cell()
                            .Add(new Paragraph(value).SetFontSize(9))
                            .SetPadding(4);

                        if (alternate)
                            cell.SetBackgroundColor(new DeviceRgb(245, 245, 245));

                        table.AddCell(cell);
                    }
                    alternate = !alternate;
                }
            }

            document.Add(table);
        }
        else if (json is JsonObject obj)
        {
            var table = new Table(2).UseAllAvailableWidth();

            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Property").SetBold())
                .SetBackgroundColor(new DeviceRgb(66, 139, 202))
                .SetFontColor(ColorConstants.WHITE));
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Value").SetBold())
                .SetBackgroundColor(new DeviceRgb(66, 139, 202))
                .SetFontColor(ColorConstants.WHITE));

            foreach (var prop in obj)
            {
                table.AddCell(new Cell().Add(new Paragraph(prop.Key).SetBold()));
                table.AddCell(new Cell().Add(new Paragraph(prop.Value?.ToString() ?? "null")));
            }

            document.Add(table);
        }
    }

    private void AddJsonAsTree(Document document, JsonNode? json, int indent)
    {
        var indentStr = new string(' ', indent * 4);

        if (json is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                if (prop.Value is JsonObject || prop.Value is JsonArray)
                {
                    document.Add(new Paragraph($"{indentStr}{prop.Key}:")
                        .SetBold()
                        .SetFontSize(10));
                    AddJsonAsTree(document, prop.Value, indent + 1);
                }
                else
                {
                    document.Add(new Paragraph($"{indentStr}{prop.Key}: {prop.Value?.ToString() ?? "null"}")
                        .SetFontSize(10));
                }
            }
        }
        else if (json is JsonArray arr)
        {
            int index = 0;
            foreach (var item in arr)
            {
                document.Add(new Paragraph($"{indentStr}[{index}]:")
                    .SetFontColor(ColorConstants.GRAY)
                    .SetFontSize(10));
                AddJsonAsTree(document, item, indent + 1);
                index++;
            }
        }
        else
        {
            document.Add(new Paragraph($"{indentStr}{json?.ToString() ?? "null"}")
                .SetFontSize(10));
        }
    }

    private void AddJsonAsCards(Document document, JsonNode? json)
    {
        if (json is JsonArray arr)
        {
            int cardNum = 1;
            foreach (var item in arr)
            {
                if (item is JsonObject obj)
                {
                    // Card container
                    var cardTable = new Table(1).UseAllAvailableWidth()
                        .SetMarginBottom(15);

                    // Card header
                    cardTable.AddCell(new Cell()
                        .Add(new Paragraph($"Record #{cardNum}").SetBold())
                        .SetBackgroundColor(new DeviceRgb(52, 73, 94))
                        .SetFontColor(ColorConstants.WHITE)
                        .SetPadding(8));

                    // Card content
                    var content = new StringBuilder();
                    foreach (var prop in obj)
                    {
                        content.AppendLine($"{prop.Key}: {prop.Value?.ToString() ?? "null"}");
                    }

                    cardTable.AddCell(new Cell()
                        .Add(new Paragraph(content.ToString()).SetFontSize(10))
                        .SetPadding(10)
                        .SetBackgroundColor(new DeviceRgb(236, 240, 241)));

                    document.Add(cardTable);
                    cardNum++;
                }
            }
        }
        else if (json is JsonObject obj)
        {
            AddJsonAsTable(document, json);
        }
    }

    /// <summary>
    /// Sign JSON with digital signature (HMAC or RSA)
    /// </summary>
    public async Task<string> SignJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var algorithm = GetString(args, "algorithm", "HMAC-SHA256"); // HMAC-SHA256, HMAC-SHA512, RSA-SHA256
        var secret = GetString(args, "secret", "");
        var privateKeyFile = GetString(args, "private_key", "");
        var includeTimestamp = GetBool(args, "include_timestamp", true);
        var outputName = GetString(args, "output_file", "signed.json");

        var inputPath = ResolvePath(inputFile, false);
        var jsonContent = await File.ReadAllTextAsync(inputPath);
        var outputPath = ResolvePath(outputName, true);

        var json = JsonNode.Parse(jsonContent);
        if (json == null)
            throw new ArgumentException("Invalid JSON content");

        // Create payload for signing
        var payload = new JsonObject
        {
            ["data"] = json.DeepClone()
        };

        if (includeTimestamp)
        {
            payload["timestamp"] = DateTime.UtcNow.ToString("O");
        }

        var payloadStr = payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var payloadBytes = Encoding.UTF8.GetBytes(payloadStr);

        string signature;
        string algorithmUsed;

        if (algorithm.ToUpper().StartsWith("HMAC"))
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret key is required for HMAC signing");

            var keyBytes = Encoding.UTF8.GetBytes(secret);

            byte[] signatureBytes;
            if (algorithm.ToUpper().Contains("512"))
            {
                using var hmac = new HMACSHA512(keyBytes);
                signatureBytes = hmac.ComputeHash(payloadBytes);
                algorithmUsed = "HMAC-SHA512";
            }
            else
            {
                using var hmac = new HMACSHA256(keyBytes);
                signatureBytes = hmac.ComputeHash(payloadBytes);
                algorithmUsed = "HMAC-SHA256";
            }

            signature = Convert.ToBase64String(signatureBytes);
        }
        else if (algorithm.ToUpper().Contains("RSA"))
        {
            if (string.IsNullOrEmpty(privateKeyFile))
                throw new ArgumentException("Private key file is required for RSA signing");

            var keyPath = ResolvePath(privateKeyFile, false);
            var keyPem = await File.ReadAllTextAsync(keyPath);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyPem);

            var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            signature = Convert.ToBase64String(signatureBytes);
            algorithmUsed = "RSA-SHA256";
        }
        else
        {
            throw new ArgumentException($"Unsupported algorithm: {algorithm}");
        }

        // Create signed document
        var signedDoc = new JsonObject
        {
            ["payload"] = payload,
            ["signature"] = new JsonObject
            {
                ["algorithm"] = algorithmUsed,
                ["value"] = signature,
                ["signed_at"] = DateTime.UtcNow.ToString("O")
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(outputPath, signedDoc.ToJsonString(options));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "JSON signed successfully",
            output_file = outputPath,
            algorithm = algorithmUsed,
            signature_length = signature.Length
        });
    }

    /// <summary>
    /// Verify JSON digital signature
    /// </summary>
    public async Task<string> VerifyJsonSignatureAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var secret = GetString(args, "secret", "");
        var publicKeyFile = GetString(args, "public_key", "");

        var inputPath = ResolvePath(inputFile, false);
        var jsonContent = await File.ReadAllTextAsync(inputPath);

        var doc = JsonNode.Parse(jsonContent)?.AsObject();
        if (doc == null)
            throw new ArgumentException("Invalid signed JSON document");

        var payload = doc["payload"];
        var sigObj = doc["signature"]?.AsObject();

        if (payload == null || sigObj == null)
            throw new ArgumentException("Invalid signed document structure. Expected 'payload' and 'signature' fields.");

        var algorithm = sigObj["algorithm"]?.ToString() ?? "";
        var signatureB64 = sigObj["value"]?.ToString() ?? "";
        var signedAt = sigObj["signed_at"]?.ToString();

        var payloadStr = payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var payloadBytes = Encoding.UTF8.GetBytes(payloadStr);
        var signatureBytes = Convert.FromBase64String(signatureB64);

        bool isValid = false;

        if (algorithm.StartsWith("HMAC"))
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret key is required for HMAC verification");

            var keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] expectedSignature;

            if (algorithm.Contains("512"))
            {
                using var hmac = new HMACSHA512(keyBytes);
                expectedSignature = hmac.ComputeHash(payloadBytes);
            }
            else
            {
                using var hmac = new HMACSHA256(keyBytes);
                expectedSignature = hmac.ComputeHash(payloadBytes);
            }

            isValid = expectedSignature.SequenceEqual(signatureBytes);
        }
        else if (algorithm.Contains("RSA"))
        {
            if (string.IsNullOrEmpty(publicKeyFile))
                throw new ArgumentException("Public key file is required for RSA verification");

            var keyPath = ResolvePath(publicKeyFile, false);
            var keyPem = await File.ReadAllTextAsync(keyPath);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyPem);

            isValid = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            valid = isValid,
            message = isValid ? "Signature is valid" : "Signature is INVALID",
            algorithm = algorithm,
            signed_at = signedAt
        });
    }

    /// <summary>
    /// Generate test RSA key pair for JSON signing
    /// </summary>
    public async Task<string> GenerateJsonSigningKeysAsync(Dictionary<string, object> args)
    {
        var keySize = GetInt(args, "key_size", 2048);
        var privateKeyFile = GetString(args, "private_key_file", "private_key.pem");
        var publicKeyFile = GetString(args, "public_key_file", "public_key.pem");

        var privateKeyPath = ResolvePath(privateKeyFile, true);
        var publicKeyPath = ResolvePath(publicKeyFile, true);

        using var rsa = RSA.Create(keySize);

        // Export private key
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        await File.WriteAllTextAsync(privateKeyPath, privateKey);

        // Export public key
        var publicKey = rsa.ExportRSAPublicKeyPem();
        await File.WriteAllTextAsync(publicKeyPath, publicKey);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Generated {keySize}-bit RSA key pair",
            private_key_file = privateKeyPath,
            public_key_file = publicKeyPath,
            key_size = keySize,
            warning = "Keep the private key secure!"
        });
    }

    #endregion

    private byte[] DeriveKey(string password, int keySize)
    {
        using var derive = new System.Security.Cryptography.Rfc2898DeriveBytes(
            password,
            new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64 },
            100000,
            System.Security.Cryptography.HashAlgorithmName.SHA256);
        return derive.GetBytes(keySize);
    }

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetInt32();
        return Convert.ToInt32(value);
    }

    private class JsonStats
    {
        public int Objects { get; set; }
        public int Arrays { get; set; }
        public int Values { get; set; }
        public int Keys { get; set; }
        public int MaxDepth { get; set; }
        public int NullValues { get; set; }
    }

    private void CountJsonStats(JsonNode? node, JsonStats stats, int depth = 0)
    {
        if (depth > stats.MaxDepth) stats.MaxDepth = depth;

        if (node is JsonObject obj)
        {
            stats.Objects++;
            stats.Keys += obj.Count;
            foreach (var prop in obj)
                CountJsonStats(prop.Value, stats, depth + 1);
        }
        else if (node is JsonArray arr)
        {
            stats.Arrays++;
            foreach (var item in arr)
                CountJsonStats(item, stats, depth + 1);
        }
        else if (node is JsonValue)
        {
            stats.Values++;
        }
        else if (node == null)
        {
            stats.NullValues++;
        }
    }

    private void ValidateAgainstSchema(JsonNode? node, JsonNode? schema, string path, List<string> errors)
    {
        if (schema == null) return;

        if (schema is JsonObject schemaObj)
        {
            var expectedType = schemaObj["type"]?.GetValue<string>();
            if (expectedType != null)
            {
                var actualType = node switch
                {
                    JsonObject => "object",
                    JsonArray => "array",
                    JsonValue v when v.TryGetValue<string>(out _) => "string",
                    JsonValue v when v.TryGetValue<double>(out _) => "number",
                    JsonValue v when v.TryGetValue<bool>(out _) => "boolean",
                    null => "null",
                    _ => "unknown"
                };

                if (expectedType != actualType && !(expectedType == "integer" && actualType == "number"))
                {
                    errors.Add($"{path}: expected {expectedType}, got {actualType}");
                }
            }

            if (node is JsonObject obj && schemaObj["properties"] is JsonObject props)
            {
                foreach (var prop in props)
                {
                    var childPath = string.IsNullOrEmpty(path) ? prop.Key : $"{path}.{prop.Key}";
                    obj.TryGetPropertyValue(prop.Key, out var childNode);
                    ValidateAgainstSchema(childNode, prop.Value, childPath, errors);
                }
            }
        }
    }

    private JsonNode? XmlToJsonNode(System.Xml.Linq.XElement? element)
    {
        if (element == null) return null;

        var obj = new JsonObject();

        foreach (var attr in element.Attributes())
        {
            obj[$"@{attr.Name.LocalName}"] = JsonValue.Create(attr.Value);
        }

        var childGroups = element.Elements().GroupBy(e => e.Name.LocalName);
        foreach (var group in childGroups)
        {
            var items = group.ToList();
            if (items.Count == 1)
            {
                obj[group.Key] = XmlToJsonNode(items[0]);
            }
            else
            {
                var arr = new JsonArray();
                foreach (var item in items)
                    arr.Add(XmlToJsonNode(item));
                obj[group.Key] = arr;
            }
        }

        if (!element.HasElements && !string.IsNullOrEmpty(element.Value))
        {
            if (obj.Count > 0)
                obj["#text"] = JsonValue.Create(element.Value);
            else
                return JsonValue.Create(element.Value);
        }

        return obj.Count > 0 ? obj : null;
    }

    private void JsonToXmlElement(JsonNode? node, System.Xml.Linq.XElement parent)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                if (prop.Key.StartsWith("@"))
                {
                    parent.SetAttributeValue(prop.Key.Substring(1), prop.Value?.ToString());
                }
                else if (prop.Key == "#text")
                {
                    parent.Value = prop.Value?.ToString() ?? "";
                }
                else if (prop.Value is JsonArray arr)
                {
                    foreach (var item in arr)
                    {
                        var child = new System.Xml.Linq.XElement(prop.Key);
                        JsonToXmlElement(item, child);
                        parent.Add(child);
                    }
                }
                else
                {
                    var child = new System.Xml.Linq.XElement(prop.Key);
                    JsonToXmlElement(prop.Value, child);
                    parent.Add(child);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var child = new System.Xml.Linq.XElement("item");
                JsonToXmlElement(item, child);
                parent.Add(child);
            }
        }
        else if (node is JsonValue val)
        {
            parent.Value = val.ToString();
        }
    }

    private JsonObject? TransformObject(JsonNode? source, Dictionary<string, string> mappings)
    {
        if (source == null) return null;

        var result = new JsonObject();
        foreach (var (newKey, oldPath) in mappings)
        {
            var values = QueryPath(source, oldPath);
            if (values.Count > 0)
            {
                result[newKey] = values[0]?.DeepClone();
            }
        }
        return result;
    }

    private Dictionary<string, string> GetStringDictionary(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return new Dictionary<string, string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, string>();
            foreach (var prop in je.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetString() ?? "";
            }
            return dict;
        }
        return new Dictionary<string, string>();
    }

    private string[] ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        values.Add(current.ToString());
        return values.ToArray();
    }

    // Helper methods
    private List<JsonNode?> QueryPath(JsonNode? node, string path)
    {
        var results = new List<JsonNode?>();
        if (node == null) return results;

        var parts = path.Split('.');
        QueryPathRecursive(node, parts, 0, results);
        return results;
    }

    private void QueryPathRecursive(JsonNode? node, string[] parts, int index, List<JsonNode?> results)
    {
        if (node == null) return;

        if (index >= parts.Length)
        {
            results.Add(node);
            return;
        }

        var part = parts[index];

        if (part == "*")
        {
            if (node is JsonArray arr)
            {
                foreach (var item in arr)
                    QueryPathRecursive(item, parts, index + 1, results);
            }
            else if (node is JsonObject obj)
            {
                foreach (var prop in obj)
                    QueryPathRecursive(prop.Value, parts, index + 1, results);
            }
        }
        else if (part.EndsWith("]"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(part, @"(.+)\[(\d+)\]");
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var idx = int.Parse(match.Groups[2].Value);

                if (node is JsonObject obj && obj.TryGetPropertyValue(key, out var arr) && arr is JsonArray array)
                {
                    if (idx < array.Count)
                        QueryPathRecursive(array[idx], parts, index + 1, results);
                }
            }
        }
        else
        {
            if (node is JsonObject obj && obj.TryGetPropertyValue(part, out var child))
                QueryPathRecursive(child, parts, index + 1, results);
        }
    }

    private JsonNode? SortJsonKeys(JsonNode? node, bool recursive)
    {
        if (node is JsonObject obj)
        {
            var sorted = new JsonObject();
            foreach (var key in obj.Select(p => p.Key).OrderBy(k => k))
            {
                var value = obj[key];
                sorted[key] = recursive ? SortJsonKeys(value?.DeepClone(), true) : value?.DeepClone();
            }
            return sorted;
        }
        else if (node is JsonArray arr && recursive)
        {
            var newArr = new JsonArray();
            foreach (var item in arr)
                newArr.Add(SortJsonKeys(item?.DeepClone(), true));
            return newArr;
        }
        return node?.DeepClone();
    }

    private void FlattenNode(JsonNode? node, string prefix, string sep, JsonObject result)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Key : $"{prefix}{sep}{prop.Key}";
                FlattenNode(prop.Value, key, sep, result);
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var key = $"{prefix}[{i}]";
                FlattenNode(arr[i], key, sep, result);
            }
        }
        else
        {
            result[prefix] = node?.DeepClone();
        }
    }

    private int RemoveJsonKeys(JsonNode? node, HashSet<string> keys, bool recursive)
    {
        int count = 0;
        if (node is JsonObject obj)
        {
            var toRemove = obj.Where(p => keys.Contains(p.Key)).Select(p => p.Key).ToList();
            foreach (var key in toRemove)
            {
                obj.Remove(key);
                count++;
            }

            if (recursive)
            {
                foreach (var prop in obj)
                    count += RemoveJsonKeys(prop.Value, keys, true);
            }
        }
        else if (node is JsonArray arr && recursive)
        {
            foreach (var item in arr)
                count += RemoveJsonKeys(item, keys, true);
        }
        return count;
    }

    private string EscapeCsv(string value, string delimiter)
    {
        if (value.Contains(delimiter) || value.Contains("\"") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
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
