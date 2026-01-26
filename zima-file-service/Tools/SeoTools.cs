using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZimaFileService.Tools;

/// <summary>
/// SEO Tools - Meta tags, sitemap, keyword analysis, robots.txt, etc.
/// </summary>
public class SeoTools
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public SeoTools()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    #region Meta Tags Generator

    /// <summary>
    /// Generate SEO meta tags for a webpage
    /// </summary>
    public Task<string> GenerateMetaTagsAsync(Dictionary<string, object> args)
    {
        var title = GetString(args, "title");
        var description = GetString(args, "description");
        var keywords = GetString(args, "keywords", null);
        var url = GetString(args, "url", null);
        var image = GetString(args, "image", null);
        var siteName = GetString(args, "site_name", null);
        var twitterHandle = GetString(args, "twitter_handle", null);
        var author = GetString(args, "author", null);
        var locale = GetString(args, "locale", "en_US");
        var type = GetString(args, "type", "website"); // website, article, product

        var sb = new StringBuilder();

        // Basic Meta Tags
        sb.AppendLine("<!-- Primary Meta Tags -->");
        sb.AppendLine($"<title>{HtmlEncode(title)}</title>");
        sb.AppendLine($"<meta name=\"title\" content=\"{HtmlEncode(title)}\">");
        sb.AppendLine($"<meta name=\"description\" content=\"{HtmlEncode(description)}\">");

        if (!string.IsNullOrEmpty(keywords))
            sb.AppendLine($"<meta name=\"keywords\" content=\"{HtmlEncode(keywords)}\">");

        if (!string.IsNullOrEmpty(author))
            sb.AppendLine($"<meta name=\"author\" content=\"{HtmlEncode(author)}\">");

        sb.AppendLine();

        // Open Graph / Facebook
        sb.AppendLine("<!-- Open Graph / Facebook -->");
        sb.AppendLine($"<meta property=\"og:type\" content=\"{type}\">");
        sb.AppendLine($"<meta property=\"og:title\" content=\"{HtmlEncode(title)}\">");
        sb.AppendLine($"<meta property=\"og:description\" content=\"{HtmlEncode(description)}\">");
        sb.AppendLine($"<meta property=\"og:locale\" content=\"{locale}\">");

        if (!string.IsNullOrEmpty(url))
            sb.AppendLine($"<meta property=\"og:url\" content=\"{url}\">");

        if (!string.IsNullOrEmpty(image))
            sb.AppendLine($"<meta property=\"og:image\" content=\"{image}\">");

        if (!string.IsNullOrEmpty(siteName))
            sb.AppendLine($"<meta property=\"og:site_name\" content=\"{HtmlEncode(siteName)}\">");

        sb.AppendLine();

        // Twitter
        sb.AppendLine("<!-- Twitter -->");
        sb.AppendLine($"<meta property=\"twitter:card\" content=\"{(string.IsNullOrEmpty(image) ? "summary" : "summary_large_image")}\">");
        sb.AppendLine($"<meta property=\"twitter:title\" content=\"{HtmlEncode(title)}\">");
        sb.AppendLine($"<meta property=\"twitter:description\" content=\"{HtmlEncode(description)}\">");

        if (!string.IsNullOrEmpty(url))
            sb.AppendLine($"<meta property=\"twitter:url\" content=\"{url}\">");

        if (!string.IsNullOrEmpty(image))
            sb.AppendLine($"<meta property=\"twitter:image\" content=\"{image}\">");

        if (!string.IsNullOrEmpty(twitterHandle))
        {
            var handle = twitterHandle.StartsWith("@") ? twitterHandle : "@" + twitterHandle;
            sb.AppendLine($"<meta property=\"twitter:site\" content=\"{handle}\">");
            sb.AppendLine($"<meta property=\"twitter:creator\" content=\"{handle}\">");
        }

        // Character counts for validation
        var titleLength = title.Length;
        var descLength = description.Length;

        var warnings = new List<string>();
        if (titleLength > 60)
            warnings.Add($"Title is {titleLength} characters (recommended: 50-60)");
        if (descLength > 160)
            warnings.Add($"Description is {descLength} characters (recommended: 150-160)");

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            meta_tags = sb.ToString(),
            validation = new {
                title_length = titleLength,
                description_length = descLength,
                title_optimal = titleLength <= 60,
                description_optimal = descLength <= 160,
                warnings = warnings.Count > 0 ? warnings : null
            }
        }));
    }

    #endregion

    #region Sitemap Generator

    /// <summary>
    /// Generate XML sitemap
    /// </summary>
    public async Task<string> GenerateSitemapAsync(Dictionary<string, object> args)
    {
        var baseUrl = GetString(args, "base_url").TrimEnd('/');
        var urls = GetStringArray(args, "urls");
        var includeLastMod = GetBool(args, "include_lastmod", true);
        var defaultPriority = GetDouble(args, "default_priority", 0.8);
        var defaultChangeFreq = GetString(args, "default_changefreq", "weekly");
        var outputFile = GetString(args, "output_file", "sitemap.xml");

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urlset = new XElement(ns + "urlset",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute("{http://www.w3.org/2001/XMLSchema-instance}schemaLocation",
                "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"));

        foreach (var url in urls)
        {
            var fullUrl = url.StartsWith("http") ? url : $"{baseUrl}/{url.TrimStart('/')}";

            var urlElement = new XElement(ns + "url",
                new XElement(ns + "loc", fullUrl));

            if (includeLastMod)
                urlElement.Add(new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")));

            urlElement.Add(new XElement(ns + "changefreq", defaultChangeFreq));

            // Homepage gets priority 1.0
            var priority = (url == "/" || url == "" || fullUrl == baseUrl) ? 1.0 : defaultPriority;
            urlElement.Add(new XElement(ns + "priority", priority.ToString("F1")));

            urlset.Add(urlElement);
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), urlset);
        var outputPath = ResolvePath(outputFile, true);

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        await writer.WriteAsync(urlset.ToString());

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            url_count = urls.Length,
            base_url = baseUrl
        });
    }

    /// <summary>
    /// Generate sitemap index for multiple sitemaps
    /// </summary>
    public async Task<string> GenerateSitemapIndexAsync(Dictionary<string, object> args)
    {
        var baseUrl = GetString(args, "base_url").TrimEnd('/');
        var sitemaps = GetStringArray(args, "sitemaps");
        var outputFile = GetString(args, "output_file", "sitemap_index.xml");

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var sitemapindex = new XElement(ns + "sitemapindex");

        foreach (var sitemap in sitemaps)
        {
            var fullUrl = sitemap.StartsWith("http") ? sitemap : $"{baseUrl}/{sitemap.TrimStart('/')}";

            sitemapindex.Add(new XElement(ns + "sitemap",
                new XElement(ns + "loc", fullUrl),
                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
        }

        var outputPath = ResolvePath(outputFile, true);

        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        await writer.WriteAsync(sitemapindex.ToString());

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            sitemap_count = sitemaps.Length
        });
    }

    #endregion

    #region Robots.txt Generator

    /// <summary>
    /// Generate robots.txt file
    /// </summary>
    public async Task<string> GenerateRobotsTxtAsync(Dictionary<string, object> args)
    {
        var sitemapUrl = GetString(args, "sitemap_url", null);
        var disallowPaths = GetStringArray(args, "disallow", Array.Empty<string>());
        var allowPaths = GetStringArray(args, "allow", Array.Empty<string>());
        var userAgent = GetString(args, "user_agent", "*");
        var crawlDelay = GetInt(args, "crawl_delay", 0);
        var outputFile = GetString(args, "output_file", "robots.txt");

        var sb = new StringBuilder();

        sb.AppendLine($"User-agent: {userAgent}");

        foreach (var path in allowPaths)
        {
            sb.AppendLine($"Allow: {path}");
        }

        foreach (var path in disallowPaths)
        {
            sb.AppendLine($"Disallow: {path}");
        }

        if (crawlDelay > 0)
            sb.AppendLine($"Crawl-delay: {crawlDelay}");

        if (!string.IsNullOrEmpty(sitemapUrl))
        {
            sb.AppendLine();
            sb.AppendLine($"Sitemap: {sitemapUrl}");
        }

        var outputPath = ResolvePath(outputFile, true);
        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new {
            success = true,
            output_file = outputPath,
            content = sb.ToString()
        });
    }

    #endregion

    #region Keyword Analysis

    /// <summary>
    /// Analyze text for keyword density and SEO metrics
    /// </summary>
    public async Task<string> AnalyzeKeywordsAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var topN = GetInt(args, "top_n", 20);

        string content;
        if (File.Exists(ResolvePath(input, false)))
        {
            content = await File.ReadAllTextAsync(ResolvePath(input, false));
        }
        else
        {
            content = input;
        }

        // Strip HTML tags
        content = Regex.Replace(content, @"<[^>]+>", " ");
        // Normalize whitespace
        content = Regex.Replace(content, @"\s+", " ").Trim();

        // Extract words
        var words = Regex.Matches(content.ToLower(), @"[a-z]+")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(w => w.Length >= 3)
            .ToList();

        var totalWords = words.Count;

        // Count word frequency
        var wordFreq = words
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());

        // Stop words to exclude
        var stopWords = new HashSet<string> {
            "the", "and", "for", "are", "but", "not", "you", "all", "can", "had",
            "her", "was", "one", "our", "out", "has", "have", "been", "were", "being",
            "their", "there", "this", "that", "with", "from", "they", "will", "would",
            "could", "should", "what", "which", "when", "where", "how", "why", "about",
            "into", "your", "also", "than", "them", "then", "only", "more", "some",
            "such", "like", "just", "over", "most", "other"
        };

        // Top keywords
        var topKeywords = wordFreq
            .Where(kv => !stopWords.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value)
            .Take(topN)
            .Select(kv => new {
                keyword = kv.Key,
                count = kv.Value,
                density = Math.Round((double)kv.Value / totalWords * 100, 2)
            })
            .ToList();

        // Extract 2-word phrases
        var phrases = new List<string>();
        for (int i = 0; i < words.Count - 1; i++)
        {
            if (!stopWords.Contains(words[i]) && !stopWords.Contains(words[i + 1]))
            {
                phrases.Add($"{words[i]} {words[i + 1]}");
            }
        }

        var topPhrases = phrases
            .GroupBy(p => p)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new { phrase = g.Key, count = g.Count() })
            .ToList();

        // Calculate readability metrics
        var sentences = Regex.Split(content, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).Count();
        var avgWordsPerSentence = sentences > 0 ? Math.Round((double)totalWords / sentences, 1) : 0;

        return JsonSerializer.Serialize(new {
            success = true,
            statistics = new {
                total_words = totalWords,
                unique_words = wordFreq.Count,
                sentences,
                avg_words_per_sentence = avgWordsPerSentence,
                character_count = content.Length
            },
            top_keywords = topKeywords,
            top_phrases = topPhrases,
            seo_recommendations = GetSeoRecommendations(totalWords, avgWordsPerSentence, topKeywords.Any())
        });
    }

    private List<string> GetSeoRecommendations(int wordCount, double avgWordsPerSentence, bool hasKeywords)
    {
        var recommendations = new List<string>();

        if (wordCount < 300)
            recommendations.Add("Content is short. Aim for 300+ words for better SEO.");
        else if (wordCount < 1000)
            recommendations.Add("Good content length. Consider expanding to 1000+ words for competitive topics.");
        else if (wordCount > 2500)
            recommendations.Add("Content is long. Consider breaking into sections with headers.");

        if (avgWordsPerSentence > 25)
            recommendations.Add("Sentences are long. Aim for 15-20 words per sentence for readability.");

        if (!hasKeywords)
            recommendations.Add("No significant keywords detected. Add relevant keywords naturally.");

        return recommendations;
    }

    #endregion

    #region Slug Generator

    /// <summary>
    /// Generate URL-friendly slug from text
    /// </summary>
    public Task<string> GenerateSlugAsync(Dictionary<string, object> args)
    {
        var text = GetString(args, "text");
        var separator = GetString(args, "separator", "-");
        var lowercase = GetBool(args, "lowercase", true);
        var maxLength = GetInt(args, "max_length", 100);

        // Remove accents
        var normalized = text.Normalize(NormalizationForm.FormD);
        var withoutAccents = new StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                withoutAccents.Append(c);
        }

        var slug = withoutAccents.ToString().Normalize(NormalizationForm.FormC);

        // Replace non-alphanumeric with separator
        slug = Regex.Replace(slug, @"[^a-zA-Z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", separator);
        slug = slug.Trim(separator[0]);

        if (lowercase)
            slug = slug.ToLower();

        if (slug.Length > maxLength)
            slug = slug.Substring(0, maxLength).TrimEnd(separator[0]);

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            original = text,
            slug
        }));
    }

    #endregion

    #region Schema.org Generator

    /// <summary>
    /// Generate Schema.org structured data (JSON-LD)
    /// </summary>
    public Task<string> GenerateSchemaAsync(Dictionary<string, object> args)
    {
        var schemaType = GetString(args, "type", "WebPage");
        var name = GetString(args, "name");
        var description = GetString(args, "description", null);
        var url = GetString(args, "url", null);
        var image = GetString(args, "image", null);

        object schema = schemaType.ToLower() switch
        {
            "organization" => new {
                context = "https://schema.org",
                type = "Organization",
                name,
                description,
                url,
                logo = image
            },
            "article" => new {
                context = "https://schema.org",
                type = "Article",
                headline = name,
                description,
                url,
                image,
                datePublished = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                dateModified = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                author = new { type = "Person", name = GetString(args, "author", "Unknown") }
            },
            "product" => new {
                context = "https://schema.org",
                type = "Product",
                name,
                description,
                image,
                offers = new {
                    type = "Offer",
                    price = GetString(args, "price", "0"),
                    priceCurrency = GetString(args, "currency", "USD"),
                    availability = "https://schema.org/InStock"
                }
            },
            "localbusiness" => new {
                context = "https://schema.org",
                type = "LocalBusiness",
                name,
                description,
                url,
                image,
                telephone = GetString(args, "phone", null),
                email = GetString(args, "email", null),
                address = new {
                    type = "PostalAddress",
                    streetAddress = GetString(args, "street", null),
                    addressLocality = GetString(args, "city", null),
                    addressRegion = GetString(args, "region", null),
                    postalCode = GetString(args, "postal_code", null),
                    addressCountry = GetString(args, "country", null)
                }
            },
            "faq" => GenerateFaqSchema(args),
            _ => new {
                context = "https://schema.org",
                type = "WebPage",
                name,
                description,
                url
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(schema, options);

        // Replace "context" with "@context" and "type" with "@type"
        json = json.Replace("\"context\":", "\"@context\":");
        json = json.Replace("\"type\":", "\"@type\":");

        var scriptTag = $"<script type=\"application/ld+json\">\n{json}\n</script>";

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            schema_type = schemaType,
            json_ld = json,
            script_tag = scriptTag
        }));
    }

    private object GenerateFaqSchema(Dictionary<string, object> args)
    {
        var questions = GetStringArray(args, "questions");
        var answers = GetStringArray(args, "answers");

        var faqItems = new List<object>();
        for (int i = 0; i < Math.Min(questions.Length, answers.Length); i++)
        {
            faqItems.Add(new {
                type = "Question",
                name = questions[i],
                acceptedAnswer = new {
                    type = "Answer",
                    text = answers[i]
                }
            });
        }

        return new {
            context = "https://schema.org",
            type = "FAQPage",
            mainEntity = faqItems
        };
    }

    #endregion

    #region Open Graph Preview

    /// <summary>
    /// Preview how a URL will appear on social media
    /// </summary>
    public Task<string> PreviewOpenGraphAsync(Dictionary<string, object> args)
    {
        var title = GetString(args, "title");
        var description = GetString(args, "description");
        var image = GetString(args, "image", null);
        var url = GetString(args, "url", null);
        var siteName = GetString(args, "site_name", null);

        // Truncate for preview
        var truncatedTitle = title.Length > 60 ? title.Substring(0, 57) + "..." : title;
        var truncatedDesc = description.Length > 160 ? description.Substring(0, 157) + "..." : description;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            preview = new {
                facebook = new {
                    site_name = siteName,
                    title = truncatedTitle,
                    description = truncatedDesc,
                    image = image ?? "[No image - Facebook may not display link preview]",
                    url
                },
                twitter = new {
                    card_type = string.IsNullOrEmpty(image) ? "summary" : "summary_large_image",
                    title = title.Length > 70 ? title.Substring(0, 67) + "..." : title,
                    description = description.Length > 200 ? description.Substring(0, 197) + "..." : description,
                    image = image ?? "[No image]"
                },
                linkedin = new {
                    title = title.Length > 70 ? title.Substring(0, 67) + "..." : title,
                    description = truncatedDesc,
                    image
                }
            },
            validation = new {
                title_length = title.Length,
                description_length = description.Length,
                has_image = !string.IsNullOrEmpty(image),
                issues = GetOgIssues(title, description, image)
            }
        }));
    }

    private List<string> GetOgIssues(string title, string description, string? image)
    {
        var issues = new List<string>();

        if (title.Length < 30)
            issues.Add("Title is too short (min 30 characters recommended)");
        if (title.Length > 60)
            issues.Add("Title is too long (will be truncated on some platforms)");
        if (description.Length < 70)
            issues.Add("Description is too short (min 70 characters recommended)");
        if (description.Length > 160)
            issues.Add("Description is too long (will be truncated)");
        if (string.IsNullOrEmpty(image))
            issues.Add("No image provided - social shares may have reduced engagement");

        return issues;
    }

    #endregion

    #region Canonical URL Generator

    /// <summary>
    /// Generate canonical and alternate URLs
    /// </summary>
    public Task<string> GenerateCanonicalUrlsAsync(Dictionary<string, object> args)
    {
        var baseUrl = GetString(args, "base_url").TrimEnd('/');
        var path = GetString(args, "path", "/").TrimEnd('/');
        var alternateLanguages = GetStringArray(args, "languages", Array.Empty<string>());
        var includeWww = GetBool(args, "include_www_alternate", false);

        var canonicalUrl = $"{baseUrl}{path}";
        if (string.IsNullOrEmpty(path)) canonicalUrl = baseUrl;

        var sb = new StringBuilder();

        // Canonical tag
        sb.AppendLine($"<link rel=\"canonical\" href=\"{canonicalUrl}\">");

        // Language alternates
        foreach (var lang in alternateLanguages)
        {
            var langPath = lang == "en" || lang == "default" ? path : $"/{lang}{path}";
            sb.AppendLine($"<link rel=\"alternate\" hreflang=\"{lang}\" href=\"{baseUrl}{langPath}\">");
        }

        if (alternateLanguages.Length > 1)
        {
            sb.AppendLine($"<link rel=\"alternate\" hreflang=\"x-default\" href=\"{canonicalUrl}\">");
        }

        // WWW alternate
        if (includeWww && !baseUrl.Contains("://www."))
        {
            var wwwUrl = baseUrl.Replace("://", "://www.");
            sb.AppendLine($"<link rel=\"alternate\" href=\"{wwwUrl}{path}\">");
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            canonical_url = canonicalUrl,
            link_tags = sb.ToString().Trim(),
            alternates = alternateLanguages.Select(lang => new {
                language = lang,
                url = $"{baseUrl}/{(lang == "en" ? "" : lang + "/")}{path.TrimStart('/')}"
            })
        }));
    }

    #endregion

    #region Word/Character Counter

    /// <summary>
    /// Count words, characters, and estimate reading time
    /// </summary>
    public async Task<string> CountWordsAsync(Dictionary<string, object> args)
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

        // Strip HTML
        var textOnly = Regex.Replace(content, @"<[^>]+>", " ");

        var characters = content.Length;
        var charactersNoSpaces = content.Replace(" ", "").Replace("\n", "").Replace("\r", "").Length;
        var words = Regex.Matches(textOnly, @"[\w]+").Count;
        var sentences = Regex.Split(textOnly, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).Count();
        var paragraphs = Regex.Split(content, @"\n\s*\n").Where(p => !string.IsNullOrWhiteSpace(p)).Count();

        // Reading time (average 200 words per minute)
        var readingTimeMinutes = Math.Ceiling((double)words / 200);

        return JsonSerializer.Serialize(new {
            success = true,
            counts = new {
                characters,
                characters_no_spaces = charactersNoSpaces,
                words,
                sentences,
                paragraphs
            },
            averages = new {
                words_per_sentence = sentences > 0 ? Math.Round((double)words / sentences, 1) : 0,
                characters_per_word = words > 0 ? Math.Round((double)charactersNoSpaces / words, 1) : 0
            },
            reading_time = new {
                minutes = (int)readingTimeMinutes,
                text = readingTimeMinutes == 1 ? "1 minute" : $"{readingTimeMinutes} minutes"
            }
        });
    }

    #endregion

    #region Helper Methods

    private string ResolvePath(string path, bool isOutput)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (Path.IsPathRooted(path))
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

    private static string HtmlEncode(string text)
    {
        return System.Web.HttpUtility.HtmlEncode(text);
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

    private static double GetDouble(Dictionary<string, object> args, string key, double defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetDouble() : defaultValue;
            }
            if (double.TryParse(value?.ToString(), out var result))
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
