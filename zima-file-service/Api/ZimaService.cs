using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZimaFileService.Api;

public class ZimaService
{
    private readonly string _claudePath;
    private readonly string _workingDirectory;
    private readonly string _generatedFilesPath;
    private readonly string _systemPromptPath;

    // Model pricing ($ per 1M tokens) - Claude Sonnet 4 pricing
    private static readonly Dictionary<string, ModelPricing> ModelPrices = new()
    {
        ["claude-sonnet-4-20250514"] = new(3.00m, 15.00m, 0.30m, 3.75m),
        ["claude-opus-4-20250514"] = new(15.00m, 75.00m, 1.50m, 18.75m),
        ["claude-3-5-sonnet-20241022"] = new(3.00m, 15.00m, 0.30m, 3.75m),
        ["claude-3-5-haiku-20241022"] = new(0.80m, 4.00m, 0.08m, 1.00m),
        ["default"] = new(3.00m, 15.00m, 0.30m, 3.75m) // Default to Sonnet pricing
    };

    // Phase 2: Multi-Model Routing - Select optimal model based on task complexity
    private const string MODEL_HAIKU = "claude-3-5-haiku-20241022";   // Fast, cheap - simple tasks
    private const string MODEL_SONNET = "claude-sonnet-4-20250514";   // Balanced - standard tasks
    private const string MODEL_OPUS = "claude-opus-4-20250514";       // Best quality - complex tasks

    /// <summary>
    /// Task complexity levels for model selection.
    /// </summary>
    public enum TaskComplexity
    {
        Simple,     // Quick responses, validation, simple questions → Haiku (73% cheaper)
        Standard,   // File generation, code, general tasks → Sonnet (default)
        Complex     // Multi-file, architecture, analysis, complex data → Opus (best quality)
    }

    /// <summary>
    /// Phase 2: Classify task complexity based on prompt analysis.
    /// Returns the appropriate complexity level for model selection.
    /// </summary>
    private static TaskComplexity ClassifyTaskComplexity(string prompt, int messageCount = 0)
    {
        var lowerPrompt = prompt.ToLower();
        var promptLength = prompt.Length;

        // === COMPLEX TASKS → Use Opus ===
        // Multi-file generation
        if (System.Text.RegularExpressions.Regex.IsMatch(lowerPrompt, @"(create|generate|make)\s+(\d+|multiple|several|many)\s+(files|documents|spreadsheets|reports)"))
            return TaskComplexity.Complex;

        // Architectural/design tasks
        var complexKeywords = new[] {
            "architecture", "design system", "complex analysis", "comprehensive report",
            "multi-sheet", "multiple sheets", "dashboard with", "full application",
            "enterprise", "production-ready", "professional grade", "detailed analysis",
            "financial model", "business plan", "project plan", "strategic"
        };
        if (complexKeywords.Any(k => lowerPrompt.Contains(k)))
            return TaskComplexity.Complex;

        // Very long prompts with detailed requirements
        if (promptLength > 1000)
            return TaskComplexity.Complex;

        // === SIMPLE TASKS → Use Haiku (73% cheaper) ===
        // Quick questions and validations
        var simplePatterns = new[] {
            "what is", "what's", "how do", "can you", "is it", "are there",
            "list", "summarize", "explain briefly", "quick", "simple",
            "yes or no", "true or false", "check if", "validate"
        };
        if (simplePatterns.Any(p => lowerPrompt.StartsWith(p) || lowerPrompt.Contains(p)))
        {
            // But not if it's asking for file creation
            if (!lowerPrompt.Contains("create") && !lowerPrompt.Contains("generate") &&
                !lowerPrompt.Contains("make") && !lowerPrompt.Contains("build"))
                return TaskComplexity.Simple;
        }

        // Very short prompts (likely simple requests)
        if (promptLength < 50 && !lowerPrompt.Contains("excel") && !lowerPrompt.Contains("word") && !lowerPrompt.Contains("pdf"))
            return TaskComplexity.Simple;

        // Follow-up questions in conversation (usually simpler)
        if (messageCount > 2 && (lowerPrompt.StartsWith("also") || lowerPrompt.StartsWith("and ") ||
            lowerPrompt.StartsWith("now ") || lowerPrompt.StartsWith("next")))
            return TaskComplexity.Simple;

        // === STANDARD TASKS → Use Sonnet (default) ===
        return TaskComplexity.Standard;
    }

    /// <summary>
    /// Phase 2: Get the appropriate model for the task complexity.
    /// </summary>
    private static string GetModelForComplexity(TaskComplexity complexity)
    {
        return complexity switch
        {
            TaskComplexity.Simple => MODEL_HAIKU,
            TaskComplexity.Complex => MODEL_OPUS,
            _ => MODEL_SONNET
        };
    }

    /// <summary>
    /// Phase 2: Get cost savings description for logging.
    /// </summary>
    private static string GetCostSavingsInfo(TaskComplexity complexity)
    {
        return complexity switch
        {
            TaskComplexity.Simple => "73% cost savings (Haiku)",
            TaskComplexity.Complex => "Best quality (Opus)",
            _ => "Balanced (Sonnet)"
        };
    }

    // ============================================================================
    // PHASE 3: TASK DECOMPOSITION & PARALLEL EXECUTION
    // ============================================================================

    /// <summary>
    /// Represents a decomposed subtask for parallel execution.
    /// </summary>
    public class SubTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Prompt { get; set; } = "";
        public string? FileType { get; set; }
        public bool HasDependencies { get; set; } = false;
        public List<string> DependsOn { get; set; } = new();
        public TaskComplexity Complexity { get; set; } = TaskComplexity.Standard;
    }

    /// <summary>
    /// Phase 3: Decompose a complex prompt into independent subtasks.
    /// Enables parallel execution for multi-file requests.
    /// </summary>
    private static List<SubTask> DecomposeTask(string prompt)
    {
        var subtasks = new List<SubTask>();
        var lowerPrompt = prompt.ToLower();

        // Pattern: "Create X files" or "Generate X documents"
        var multiFileMatch = System.Text.RegularExpressions.Regex.Match(
            lowerPrompt,
            @"(create|generate|make)\s+(\d+)\s+(files|documents|spreadsheets|reports|sheets)"
        );

        if (multiFileMatch.Success && int.TryParse(multiFileMatch.Groups[2].Value, out int fileCount))
        {
            // Decompose into individual file tasks
            for (int i = 1; i <= fileCount; i++)
            {
                subtasks.Add(new SubTask
                {
                    Prompt = $"{prompt} - File {i} of {fileCount}",
                    Complexity = TaskComplexity.Standard
                });
            }
            return subtasks;
        }

        // Pattern: "Create X and Y and Z" (multiple items with 'and')
        if (lowerPrompt.Contains(" and ") &&
            (lowerPrompt.Contains("create") || lowerPrompt.Contains("generate")))
        {
            var parts = System.Text.RegularExpressions.Regex.Split(prompt, @"\s+and\s+",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (parts.Length >= 2 && parts.Length <= 5)
            {
                foreach (var part in parts)
                {
                    var taskPrompt = part.Trim();
                    // Ensure each part has the action verb
                    if (!taskPrompt.ToLower().StartsWith("create") &&
                        !taskPrompt.ToLower().StartsWith("generate"))
                    {
                        taskPrompt = "Create " + taskPrompt;
                    }
                    subtasks.Add(new SubTask
                    {
                        Prompt = taskPrompt,
                        Complexity = ClassifyTaskComplexity(taskPrompt, 0)
                    });
                }
                return subtasks;
            }
        }

        // No decomposition needed - single task
        subtasks.Add(new SubTask
        {
            Prompt = prompt,
            Complexity = ClassifyTaskComplexity(prompt, 0)
        });

        return subtasks;
    }

    /// <summary>
    /// Phase 3: Check if a prompt can benefit from parallel execution.
    /// </summary>
    private static bool CanParallelize(string prompt)
    {
        var lowerPrompt = prompt.ToLower();

        // Multi-file patterns
        if (System.Text.RegularExpressions.Regex.IsMatch(lowerPrompt,
            @"(create|generate|make)\s+\d+\s+(files|documents|spreadsheets)"))
            return true;

        // Multiple independent items
        if (lowerPrompt.Contains(" and ") &&
            System.Text.RegularExpressions.Regex.Matches(lowerPrompt, @"\s+and\s+").Count >= 1)
            return true;

        return false;
    }

    /// <summary>
    /// Phase 3: Execute multiple subtasks in parallel for improved throughput.
    /// Returns aggregated results from all subtasks.
    /// </summary>
    public async Task<ZimaResponse> GenerateParallelAsync(string prompt, string? conversationContext = null, string? sessionId = null, List<ConversationMessage>? messages = null)
    {
        // Initialize journey logger for parallel route
        var journey = new JourneyLogger("GENERATE_PARALLEL", sessionId);
        var stopwatch = Stopwatch.StartNew();

        journey.LogPrompt(prompt, prompt.Length);

        // Check if parallelization is beneficial
        var canParallelize = CanParallelize(prompt);
        journey.LogParallelExecution(0, canParallelize);

        if (!canParallelize)
        {
            journey.LogRouteDecision("FALLBACK_TO_STANDARD", "Task not suitable for parallel execution");
            journey.Complete(true, "Redirected to standard route");
            return await GenerateAsync(prompt, conversationContext, sessionId, messages);
        }

        // Decompose into subtasks
        var subtasks = DecomposeTask(prompt);
        journey.LogStep("TASK_DECOMPOSITION", $"Decomposed into {subtasks.Count} subtasks", new Dictionary<string, object?>
        {
            ["subtaskCount"] = subtasks.Count,
            ["subtasks"] = subtasks.Select(s => new { s.Id, s.Complexity, PromptPreview = s.Prompt[..Math.Min(50, s.Prompt.Length)] }).ToList()
        });

        if (subtasks.Count <= 1)
        {
            journey.LogRouteDecision("FALLBACK_TO_STANDARD", "Single task after decomposition");
            journey.Complete(true, "Redirected to standard route");
            return await GenerateAsync(prompt, conversationContext, sessionId, messages);
        }

        // Separate independent and dependent tasks
        var independentTasks = subtasks.Where(t => !t.HasDependencies).ToList();
        var dependentTasks = subtasks.Where(t => t.HasDependencies).ToList();

        journey.LogStep("TASK_CLASSIFICATION", $"Independent: {independentTasks.Count}, Dependent: {dependentTasks.Count}");

        var allResults = new List<ZimaResponse>();
        var allFiles = new List<string>();
        var totalUsage = new TokenUsage();

        // Execute independent tasks in parallel
        if (independentTasks.Count > 0)
        {
            var parallelStartTime = stopwatch.ElapsedMilliseconds;
            journey.LogStep("PARALLEL_EXEC_START", $"Starting {independentTasks.Count} parallel tasks");

            var parallelTasks = independentTasks.Select(subtask =>
                GenerateAsync(subtask.Prompt, conversationContext, sessionId, messages)
            ).ToArray();

            var parallelResults = await Task.WhenAll(parallelTasks);
            var parallelDuration = stopwatch.ElapsedMilliseconds - parallelStartTime;

            foreach (var result in parallelResults)
            {
                allResults.Add(result);
                allFiles.AddRange(result.GeneratedFiles);
                if (result.Usage != null)
                {
                    totalUsage.InputTokens += result.Usage.InputTokens;
                    totalUsage.OutputTokens += result.Usage.OutputTokens;
                    totalUsage.CacheCreationTokens += result.Usage.CacheCreationTokens;
                    totalUsage.CacheReadTokens += result.Usage.CacheReadTokens;
                    totalUsage.Cost += result.Usage.Cost;
                }
            }

            journey.LogStep("PARALLEL_EXEC_COMPLETE", $"Completed {parallelResults.Length} tasks in {parallelDuration}ms", new Dictionary<string, object?>
            {
                ["taskCount"] = parallelResults.Length,
                ["durationMs"] = parallelDuration,
                ["successCount"] = parallelResults.Count(r => r.Success),
                ["filesGenerated"] = allFiles.Count
            });
        }

        // Execute dependent tasks sequentially
        if (dependentTasks.Count > 0)
        {
            journey.LogStep("SEQUENTIAL_EXEC_START", $"Starting {dependentTasks.Count} dependent tasks");
        }

        foreach (var subtask in dependentTasks)
        {
            var taskStartTime = stopwatch.ElapsedMilliseconds;
            journey.LogStep("DEPENDENT_TASK_START", $"Task {subtask.Id}");

            var result = await GenerateAsync(subtask.Prompt, conversationContext, sessionId, messages);
            allResults.Add(result);
            allFiles.AddRange(result.GeneratedFiles);
            if (result.Usage != null)
            {
                totalUsage.InputTokens += result.Usage.InputTokens;
                totalUsage.OutputTokens += result.Usage.OutputTokens;
                totalUsage.CacheCreationTokens += result.Usage.CacheCreationTokens;
                totalUsage.CacheReadTokens += result.Usage.CacheReadTokens;
                totalUsage.Cost += result.Usage.Cost;
            }

            journey.LogStep("DEPENDENT_TASK_COMPLETE", $"Task {subtask.Id} done in {stopwatch.ElapsedMilliseconds - taskStartTime}ms");
        }

        // Aggregate results
        var combinedOutput = string.Join("\n\n---\n\n", allResults.Select(r => r.Output).Where(o => !string.IsNullOrEmpty(o)));
        var anySuccess = allResults.Any(r => r.Success);
        var allErrors = string.Join("\n", allResults.Select(r => r.Errors).Where(e => !string.IsNullOrEmpty(e)));

        if (allFiles.Count > 0)
        {
            journey.LogFileGeneration(allFiles.Distinct().ToList());
        }

        journey.LogTokenUsage(
            totalUsage.InputTokens,
            totalUsage.OutputTokens,
            totalUsage.CacheReadTokens,
            totalUsage.CacheCreationTokens,
            totalUsage.Cost
        );

        var summary = journey.Complete(anySuccess, combinedOutput);

        return new ZimaResponse
        {
            Success = anySuccess,
            Message = $"Parallel execution completed: {allResults.Count(r => r.Success)}/{allResults.Count} tasks succeeded",
            Output = combinedOutput,
            Errors = string.IsNullOrEmpty(allErrors) ? null : allErrors,
            SessionId = sessionId,
            GeneratedFiles = allFiles.Distinct().ToList(),
            Usage = totalUsage,
            TaskComplexity = "Parallel",
            CostInfo = $"{subtasks.Count} parallel tasks",
            RequestId = journey.RequestId,
            JourneyDurationMs = summary.TotalDurationMs
        };
    }

    // ============================================================================
    // PHASE 4: ADAPTIVE CONTEXT LOADING
    // ============================================================================

    /// <summary>
    /// Context loading tiers - from minimal to full.
    /// Each tier adds more context at the cost of more tokens.
    /// </summary>
    public enum ContextTier
    {
        Minimal,    // Just the current prompt (first message, simple questions)
        Planning,   // + Recent 3 messages (follow-up questions)
        Execution,  // + Session file summaries (new file generation)
        Brownfield, // + Full content of small files (modifications)
        Full        // + Everything including large files (complex analysis)
    }

    /// <summary>
    /// Phase 4: Determine the appropriate context tier based on the request.
    /// </summary>
    private static ContextTier DetermineContextTier(string prompt, int messageCount, List<ConversationMessage>? messages)
    {
        var lowerPrompt = prompt.ToLower();

        // First message in session - minimal context
        if (messageCount == 0)
            return ContextTier.Minimal;

        // Simple follow-up questions
        if (lowerPrompt.StartsWith("what") || lowerPrompt.StartsWith("why") ||
            lowerPrompt.StartsWith("how") || lowerPrompt.StartsWith("can you explain"))
            return ContextTier.Planning;

        // File modification requests need more context
        if (lowerPrompt.Contains("update") || lowerPrompt.Contains("modify") ||
            lowerPrompt.Contains("change") || lowerPrompt.Contains("edit") ||
            lowerPrompt.Contains("fix"))
            return ContextTier.Brownfield;

        // Complex analysis needs everything
        if (lowerPrompt.Contains("analyze") || lowerPrompt.Contains("compare") ||
            lowerPrompt.Contains("review all") || lowerPrompt.Contains("comprehensive"))
            return ContextTier.Full;

        // Default for new file generation
        return ContextTier.Execution;
    }

    /// <summary>
    /// Phase 4: Get the number of recent messages to include based on tier.
    /// </summary>
    private static int GetMessageLimitForTier(ContextTier tier)
    {
        return tier switch
        {
            ContextTier.Minimal => 0,
            ContextTier.Planning => 3,
            ContextTier.Execution => 6,
            ContextTier.Brownfield => 10,
            ContextTier.Full => int.MaxValue,
            _ => 6
        };
    }

    /// <summary>
    /// Phase 4: Filter messages based on context tier.
    /// Reduces token usage by only including relevant history.
    /// </summary>
    private static List<ConversationMessage> FilterMessagesForTier(
        List<ConversationMessage>? messages,
        ContextTier tier)
    {
        if (messages == null || messages.Count == 0)
            return new List<ConversationMessage>();

        var limit = GetMessageLimitForTier(tier);
        if (limit == 0)
            return new List<ConversationMessage>();

        // Take the most recent messages up to the limit
        return messages
            .TakeLast(Math.Min(messages.Count, limit))
            .ToList();
    }

    /// <summary>
    /// Phase 4: Summarize file content based on context tier.
    /// </summary>
    private static string SummarizeFileContext(string? fileContext, ContextTier tier)
    {
        if (string.IsNullOrEmpty(fileContext))
            return "";

        // Minimal/Planning tiers get no file context
        if (tier == ContextTier.Minimal || tier == ContextTier.Planning)
            return "";

        // Execution tier gets file summaries only (first 500 chars per file)
        if (tier == ContextTier.Execution)
        {
            var lines = fileContext.Split('\n');
            var summarized = new System.Text.StringBuilder();
            var currentFileLines = 0;
            const int maxLinesPerFile = 20;

            foreach (var line in lines)
            {
                if (line.StartsWith("=== File:") || line.StartsWith("--- File:"))
                {
                    currentFileLines = 0;
                    summarized.AppendLine(line);
                }
                else if (currentFileLines < maxLinesPerFile)
                {
                    summarized.AppendLine(line);
                    currentFileLines++;
                }
                else if (currentFileLines == maxLinesPerFile)
                {
                    summarized.AppendLine("... [truncated for context optimization]");
                    currentFileLines++;
                }
            }
            return summarized.ToString();
        }

        // Brownfield/Full tiers get complete context
        return fileContext;
    }

    // ============================================================================
    // PHASE 5: JOB STATUS TRACKING (for background processing)
    // ============================================================================

    /// <summary>
    /// Job status for async processing.
    /// </summary>
    public enum JobStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    /// <summary>
    /// Job tracking for background processing.
    /// </summary>
    public class JobInfo
    {
        public string JobId { get; set; } = "";
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Model { get; set; }
        public TaskComplexity Complexity { get; set; }
        public ContextTier ContextTier { get; set; }
        public int Progress { get; set; } = 0; // 0-100
        public string? CurrentStep { get; set; }
        public ZimaResponse? Result { get; set; }
        public string? Error { get; set; }
    }

    // In-memory job storage (for demo - use Redis/DB in production)
    private static readonly Dictionary<string, JobInfo> _jobs = new();
    private static readonly object _jobLock = new();

    /// <summary>
    /// Phase 5: Create a new job for background processing.
    /// </summary>
    public static JobInfo CreateJob(string prompt, TaskComplexity complexity, ContextTier contextTier, string model)
    {
        var job = new JobInfo
        {
            JobId = Guid.NewGuid().ToString("N")[..12],
            Status = JobStatus.Pending,
            StartedAt = DateTime.UtcNow,
            Model = model,
            Complexity = complexity,
            ContextTier = contextTier,
            CurrentStep = "Initializing..."
        };

        lock (_jobLock)
        {
            _jobs[job.JobId] = job;
        }

        return job;
    }

    /// <summary>
    /// Phase 5: Update job progress.
    /// </summary>
    public static void UpdateJobProgress(string jobId, int progress, string step)
    {
        lock (_jobLock)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Progress = progress;
                job.CurrentStep = step;
            }
        }
    }

    /// <summary>
    /// Phase 5: Complete a job with result.
    /// </summary>
    public static void CompleteJob(string jobId, ZimaResponse result)
    {
        lock (_jobLock)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.Progress = 100;
                job.CurrentStep = "Completed";
                job.Result = result;
            }
        }
    }

    /// <summary>
    /// Phase 5: Fail a job with error.
    /// </summary>
    public static void FailJob(string jobId, string error)
    {
        lock (_jobLock)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.CurrentStep = "Failed";
                job.Error = error;
            }
        }
    }

    /// <summary>
    /// Phase 5: Get job status.
    /// </summary>
    public static JobInfo? GetJob(string jobId)
    {
        lock (_jobLock)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }
    }

    /// <summary>
    /// Phase 5: Clean up old jobs (older than 1 hour).
    /// </summary>
    public static void CleanupOldJobs()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        lock (_jobLock)
        {
            var oldJobs = _jobs.Where(j => j.Value.StartedAt < cutoff).Select(j => j.Key).ToList();
            foreach (var jobId in oldJobs)
            {
                _jobs.Remove(jobId);
            }
        }
    }

    public ZimaService()
    {
        _claudePath = Environment.GetEnvironmentVariable("CLAUDE_PATH")
            ?? "/Users/andrewmashamba/.npm-global/bin/claude";
        _workingDirectory = FileManager.Instance.WorkingDirectory;
        _generatedFilesPath = FileManager.Instance.GeneratedFilesPath;

        // Create cached system prompt file for better performance
        _systemPromptPath = Path.Combine(_workingDirectory, ".zima-system-prompt.md");
        EnsureSystemPromptFile();

        Log("INFO", $"ZimaService initialized");
        Log("INFO", $"  Claude Path: {_claudePath}");
        Log("INFO", $"  Working Dir: {_workingDirectory}");
        Log("INFO", $"  Generated Files: {_generatedFilesPath}");
        Log("INFO", $"  System Prompt: {_systemPromptPath}");

        // Initialize caching and connection pool
        Log("INFO", $"  Response Cache: enabled (TTL: 30min, max: 1000 entries)");
        Log("INFO", $"  Connection Pool: enabled (min: 2, max: 5)");

        // Start background warmup (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await ClaudeConnectionPool.Instance.WarmupAsync(2);
                Log("INFO", "Connection pool pre-warmed");
            }
            catch (Exception ex)
            {
                Log("WARN", $"Pool warmup failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Creates/updates the system prompt file for caching.
    /// File-based system prompts enable Claude CLI prompt caching.
    /// </summary>
    private void EnsureSystemPromptFile()
    {
        var systemPrompt = GetSystemPrompt();
        var needsUpdate = !File.Exists(_systemPromptPath) ||
                          File.ReadAllText(_systemPromptPath) != systemPrompt;

        if (needsUpdate)
        {
            File.WriteAllText(_systemPromptPath, systemPrompt);
            Log("INFO", "System prompt file updated for caching");
        }
    }

    private static void Log(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [{level}] {message}");
    }

    /// <summary>
    /// Returns optimized system prompt - balanced for speed and functionality.
    /// ~500 chars for fast processing while maintaining file creation capability.
    /// </summary>
    private static string GetSystemPrompt()
    {
        var toolsPrompt = ToolsRegistry.Instance.GetToolsPrompt();

        return $@"You are ZIMA, an AI file generation assistant with access to MCP tools.

{toolsPrompt}

## CRITICAL INSTRUCTIONS

1. **USE TOOLS IMMEDIATELY** - When asked to create a file, call the MCP tool directly with the data.
   Do NOT explore, check directories, or look for scripts first.

2. **CREATE TOOLS IF NEEDED** - If no existing tool fits the task:
   - Create a new C# tool file in the Api/ directory
   - Use appropriate NuGet packages (ClosedXML, DocumentFormat.OpenXml, QuestPDF, etc.)
   - Register it: [REGISTER_TOOL: toolname|description|filepath]
   - Then use it to complete the task

3. **HELPER TOOLS** - You can create helper utilities to extend existing tools.
   You have full source code access.

4. **FORMAT RESPONSES** - Use HTML with inline CSS:
   <div style=""background:#f0fdf4; border-left:4px solid #22c55e; padding:12px; font-family:-apple-system,sans-serif"">
   Success message here
   </div>

## EXAMPLES

User: ""Create an Excel file of all countries""
Action: Immediately call create_excel with country data (name, capital, population, etc.)

User: ""Generate a PDF invoice""
Action: Immediately call create_pdf with invoice layout and data

User: ""Create a chart from this data""
Action: If no chart tool exists, create one, register it, then use it.";
    }

    public async Task<ZimaResponse> GenerateAsync(string prompt, string? conversationContext = null, string? sessionId = null, List<ConversationMessage>? messages = null)
    {
        // Initialize journey logger for this request
        var journey = new JourneyLogger("GENERATE_ASYNC", sessionId);
        var stopwatch = Stopwatch.StartNew();

        journey.LogPrompt(prompt, prompt.Length);

        // Phase 2: Multi-Model Routing - Classify task and select optimal model
        var messageCount = messages?.Count ?? 0;
        var complexity = ClassifyTaskComplexity(prompt, messageCount);
        var selectedModel = GetModelForComplexity(complexity);
        var costInfo = GetCostSavingsInfo(complexity);

        journey.LogModelSelection(selectedModel, complexity.ToString(), costInfo);

        // === PHASE 6: Response Caching ===
        // Check if this prompt is cacheable and if we have a cached response
        var isCacheable = ResponseCache.IsCacheable(prompt);
        var cacheKey = ResponseCache.GenerateCacheKey(prompt, sessionId, messageCount);

        journey.LogStep("CACHE_CHECK", $"Cacheable: {isCacheable}, Key: {cacheKey}");

        if (isCacheable && ResponseCache.Instance.TryGet(cacheKey, out var cachedResponse) && cachedResponse != null)
        {
            journey.LogStep("CACHE_HIT", $"Returning cached response");
            cachedResponse.RequestId = journey.RequestId;
            cachedResponse.Message = "Cached response";
            var cacheSummary = journey.Complete(true, cachedResponse.Output);
            cachedResponse.JourneyDurationMs = cacheSummary.TotalDurationMs;

            // Log cache stats
            var cacheStats = ResponseCache.Instance.GetStats();
            journey.LogStep("CACHE_STATS", $"Hits: {cacheStats.CacheHits}, Misses: {cacheStats.CacheMisses}, Rate: {cacheStats.HitRate:F1}%");

            return cachedResponse;
        }

        // === PHASE 6: Connection Pool - Check session warmup status ===
        var isSessionWarm = !string.IsNullOrEmpty(sessionId) && ClaudeConnectionPool.Instance.IsSessionWarm(sessionId);
        journey.LogStep("POOL_STATUS", $"Session warm: {isSessionWarm}", new Dictionary<string, object?>
        {
            ["sessionId"] = sessionId,
            ["isWarm"] = isSessionWarm
        });

        // Trigger background session warmup for future requests (if new session)
        if (!string.IsNullOrEmpty(sessionId) && !isSessionWarm)
        {
            // Fire and forget - warmup happens in background for next request
            _ = Task.Run(async () =>
            {
                try
                {
                    await ClaudeConnectionPool.Instance.WarmupSessionAsync(sessionId, GetSystemPrompt());
                }
                catch { /* Ignore warmup failures */ }
            });
        }

        // Phase 4: Adaptive Context Loading - Determine optimal context tier
        var contextTier = DetermineContextTier(prompt, messageCount, messages);
        var filteredMessages = FilterMessagesForTier(messages, contextTier);
        var optimizedContext = SummarizeFileContext(conversationContext, contextTier);

        journey.LogContextTier(
            contextTier.ToString(),
            messageCount,
            filteredMessages.Count,
            optimizedContext?.Length ?? 0
        );

        journey.LogRouteDecision("GENERATE_ASYNC", "Standard generation path", new Dictionary<string, object?>
        {
            ["complexity"] = complexity.ToString(),
            ["model"] = selectedModel,
            ["contextTier"] = contextTier.ToString()
        });

        journey.LogStep("FILE_SCAN_START", "Scanning for existing files");

        var existingFiles = new HashSet<string>();
        var possibleDirs = new[] {
            _generatedFilesPath,
            _workingDirectory,
            Path.Combine(Directory.GetCurrentDirectory(), "generated_files")
        };

        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    existingFiles.Add(Path.GetFileName(file));
                }
            }
        }

        journey.LogStep("FILE_SCAN_COMPLETE", $"Found {existingFiles.Count} existing files");

        try
        {
            journey.LogStep("PROMPT_BUILD_START", "Building enhanced prompt");

            // Build prompt with conversation context (Phase 4: using optimized context)
            var fullPrompt = new System.Text.StringBuilder();
            fullPrompt.AppendLine(GetSystemPrompt());

            // Phase 4: Use optimized file context based on context tier
            if (!string.IsNullOrEmpty(optimizedContext))
            {
                fullPrompt.AppendLine();
                fullPrompt.AppendLine(optimizedContext);
            }

            // Phase 4: Use filtered messages based on context tier
            if (filteredMessages.Count > 0)
            {
                fullPrompt.AppendLine();
                fullPrompt.AppendLine("Previous conversation:");
                foreach (var msg in filteredMessages)
                {
                    var role = msg.Role == "user" ? "User" : "Assistant";
                    fullPrompt.AppendLine($"{role}: {msg.Content}");
                    fullPrompt.AppendLine();
                }
                fullPrompt.AppendLine("Current request:");
            }

            fullPrompt.Append(prompt);
            var enhancedPrompt = fullPrompt.ToString();

            journey.LogStep("PROMPT_BUILD_COMPLETE", $"Enhanced prompt: {enhancedPrompt.Length} chars", new Dictionary<string, object?>
            {
                ["systemPromptLength"] = GetSystemPrompt().Length,
                ["contextLength"] = optimizedContext?.Length ?? 0,
                ["messagesLength"] = filteredMessages.Sum(m => m.Content.Length),
                ["userPromptLength"] = prompt.Length,
                ["totalLength"] = enhancedPrompt.Length
            });

            // Use file-based prompt for reliability and speed (avoids shell escaping overhead)
            var promptFile = Path.Combine(Path.GetTempPath(), $"zima-prompt-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(promptFile, enhancedPrompt);
            journey.LogStep("PROMPT_FILE_CREATED", promptFile);

            try
            {
                // Phase 2: Use selected model based on task complexity
                // Removed --max-turns as it blocks file creation workflow
                var startInfo = new ProcessStartInfo
                {
                    FileName = _claudePath,
                    Arguments = $"--print --model {selectedModel} --output-format json --dangerously-skip-permissions",
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                journey.LogClaudeStart(selectedModel, enhancedPrompt.Length);

            using var process = new Process { StartInfo = startInfo };

            var output = new List<string>();
            var errors = new List<string>();
            var claudeStartTime = stopwatch.ElapsedMilliseconds;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    output.Add(e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    errors.Add(e.Data);
                    journey.LogStep("CLAUDE_STDERR", e.Data);
                }
            };

            process.Start();
            journey.LogStep("CLAUDE_PROCESS_START", $"PID: {process.Id}");

            // Pipe prompt via stdin for reliability (avoids command-line length limits)
            await process.StandardInput.WriteAsync(enhancedPrompt);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            journey.LogStep("PROMPT_PIPED", "Prompt sent via stdin");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            journey.LogStep("CLAUDE_WAITING", "Waiting for Claude CLI (timeout: 10 min)");
            var completed = await Task.Run(() => process.WaitForExit(600000));

            var claudeDuration = stopwatch.ElapsedMilliseconds - claudeStartTime;

            if (!completed)
            {
                journey.LogError("CLAUDE_TIMEOUT", $"Timeout after {claudeDuration}ms");
                process.Kill();
                journey.Complete(false, "Timeout");
                return new ZimaResponse
                {
                    Success = false,
                    Message = "Request timed out after 10 minutes",
                    Output = string.Join("\n", output)
                };
            }

            journey.LogClaudeComplete(process.ExitCode, claudeDuration);

            var fullOutput = string.Join("\n", output).Trim();

            journey.LogStep("RESPONSE_PARSE_START", "Parsing Claude response");

            // Parse JSON output to extract token usage
            var response = new ZimaResponse
            {
                Success = process.ExitCode == 0,
                Message = process.ExitCode == 0 ? "Generation completed" : "Generation failed",
                Errors = errors.Count > 0 ? string.Join("\n", errors) : null,
                // Phase 2: Include model routing info
                TaskComplexity = complexity.ToString(),
                CostInfo = costInfo,
                // Phase 4: Include context tier info
                ContextTierUsed = contextTier.ToString(),
                // Add journey tracking info
                RequestId = journey.RequestId
            };

            // Try to parse JSON response
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<ClaudeJsonResponse>(fullOutput, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonResponse != null)
                {
                    response.Output = jsonResponse.Result ?? fullOutput;
                    response.Model = jsonResponse.Model;

                    // Extract token usage
                    if (jsonResponse.Usage != null)
                    {
                        response.Usage = new TokenUsage
                        {
                            InputTokens = jsonResponse.Usage.InputTokens,
                            OutputTokens = jsonResponse.Usage.OutputTokens,
                            CacheCreationTokens = jsonResponse.Usage.CacheCreationInputTokens,
                            CacheReadTokens = jsonResponse.Usage.CacheReadInputTokens
                        };

                        // Calculate cost
                        var pricing = ModelPrices.GetValueOrDefault(jsonResponse.Model ?? "default", ModelPrices["default"]);
                        response.Usage.Cost = CalculateCost(response.Usage, pricing);

                        journey.LogTokenUsage(
                            response.Usage.InputTokens,
                            response.Usage.OutputTokens,
                            response.Usage.CacheReadTokens,
                            response.Usage.CacheCreationTokens,
                            response.Usage.Cost
                        );
                    }

                    journey.LogStep("RESPONSE_PARSE_COMPLETE", $"Output: {response.Output?.Length ?? 0} chars");
                }
                else
                {
                    response.Output = fullOutput;
                    journey.LogStep("RESPONSE_PARSE_RAW", "No JSON structure, using raw output");
                }
            }
            catch (JsonException ex)
            {
                // Not JSON format, use raw output
                response.Output = fullOutput;
                journey.LogStep("RESPONSE_PARSE_FALLBACK", $"JSON parse failed: {ex.Message}");
            }

            // Scan for new files and move to session folder
            journey.LogStep("NEW_FILE_SCAN_START", "Scanning for newly generated files");
            var newFiles = new List<string>();

            // Determine target directory - session-specific or root
            var targetDir = !string.IsNullOrEmpty(sessionId)
                ? FileManager.Instance.GetSessionGeneratedPath(sessionId)
                : _generatedFilesPath;

            foreach (var dir in possibleDirs)
            {
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        var fileName = Path.GetFileName(file);
                        if (!existingFiles.Contains(fileName))
                        {
                            var targetPath = Path.Combine(targetDir, fileName);
                            if (file != targetPath)
                            {
                                try
                                {
                                    File.Move(file, targetPath, true);
                                    newFiles.Add(fileName);
                                }
                                catch (Exception moveEx)
                                {
                                    journey.LogStep("FILE_MOVE_ERROR", $"Failed to move {fileName}: {moveEx.Message}");
                                    newFiles.Add(fileName);
                                }
                            }
                            else
                            {
                                newFiles.Add(fileName);
                            }
                        }
                    }
                }
            }

            if (newFiles.Count > 0)
            {
                journey.LogFileGeneration(newFiles);
            }

            response.GeneratedFiles = newFiles;
            response.SessionId = sessionId;

            // Complete the journey
            var summary = journey.Complete(response.Success, response.Output);
            response.JourneyDurationMs = summary.TotalDurationMs;

            // === PHASE 7: Parse for new tool registrations and scan for new tool files ===
            if (!string.IsNullOrEmpty(response.Output))
            {
                // Parse explicit [REGISTER_TOOL: ...] commands
                ToolsRegistry.Instance.ParseAndRegisterTools(response.Output);

                // Auto-scan for new tool files created by AI
                ToolsRegistry.Instance.ScanForNewTools();

                journey.LogStep("TOOL_SCAN", $"Scanned for new tools. Total: {ToolsRegistry.Instance.GetAllToolNames().Count}");
            }

            // === PHASE 6: Store response in cache (if cacheable) ===
            if (isCacheable && response.Success)
            {
                ResponseCache.Instance.Set(cacheKey, response);
                journey.LogStep("CACHE_STORE", $"Stored response in cache (key: {cacheKey})");
            }

            return response;
            }
            finally
            {
                // Cleanup temp prompt file
                try { if (File.Exists(promptFile)) File.Delete(promptFile); } catch { }
            }
        }
        catch (Exception ex)
        {
            journey.LogError("EXCEPTION", ex.Message, ex);
            journey.Complete(false, ex.Message);

            return new ZimaResponse
            {
                Success = false,
                Message = $"Error executing Claude Code: {ex.Message}",
                Output = null,
                RequestId = journey.RequestId,
                JourneyDurationMs = journey.ElapsedMs
            };
        }
    }

    /// <summary>
    /// Builds a conversation JSON with cache_control: ephemeral markers on the last 2-3 messages
    /// to enable Anthropic's prompt caching for 50-90% cost reduction on repeated context.
    /// </summary>
    private string BuildConversationWithCaching(List<ConversationMessage> messages, string currentPrompt, string requestId)
    {
        var conversationMessages = new List<object>();

        // Add system message with cache control
        conversationMessages.Add(new
        {
            role = "system",
            content = new[]
            {
                new
                {
                    type = "text",
                    text = "You are a file generation assistant. When asked to create files, save them in the 'generated_files' subdirectory.",
                    cache_control = new { type = "ephemeral" }
                }
            }
        });

        var totalMessages = messages.Count;
        var cacheThreshold = Math.Max(0, totalMessages - 3); // Cache last 3 historical messages

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            var shouldCache = i >= cacheThreshold;

            if (shouldCache)
            {
                // Add cache_control to this message
                conversationMessages.Add(new
                {
                    role = msg.Role,
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = msg.Content,
                            cache_control = new { type = "ephemeral" }
                        }
                    }
                });
                Log("DEBUG", $"[{requestId}] Cached message {i + 1}/{totalMessages} (role: {msg.Role})");
            }
            else
            {
                // Regular message without cache control
                conversationMessages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        // Add current prompt as the final user message (always cached for next turn)
        conversationMessages.Add(new
        {
            role = "user",
            content = new[]
            {
                new
                {
                    type = "text",
                    text = currentPrompt,
                    cache_control = new { type = "ephemeral" }
                }
            }
        });

        var conversation = new { messages = conversationMessages };
        return JsonSerializer.Serialize(conversation, new JsonSerializerOptions { WriteIndented = false });
    }

    public async IAsyncEnumerable<StreamEvent> GenerateStreamingAsync(string prompt, string? conversationContext = null, List<ConversationMessage>? messages = null, string? sessionId = null)
    {
        // Initialize journey logger for streaming route
        var journey = new JourneyLogger("GENERATE_STREAMING", sessionId);
        var stopwatch = Stopwatch.StartNew();

        journey.LogPrompt(prompt, prompt.Length);

        // Phase 2: Multi-Model Routing for streaming
        var messageCount = messages?.Count ?? 0;
        var complexity = ClassifyTaskComplexity(prompt, messageCount);
        var selectedModel = GetModelForComplexity(complexity);
        var costInfo = GetCostSavingsInfo(complexity);

        journey.LogModelSelection(selectedModel, complexity.ToString(), costInfo);

        // Phase 4: Adaptive Context Loading for streaming
        var contextTier = DetermineContextTier(prompt, messageCount, messages);
        var filteredMessages = FilterMessagesForTier(messages, contextTier);
        var optimizedContext = SummarizeFileContext(conversationContext, contextTier);

        journey.LogContextTier(contextTier.ToString(), messageCount, filteredMessages.Count, optimizedContext?.Length ?? 0);
        journey.LogRouteDecision("STREAMING", "SSE streaming path selected", new Dictionary<string, object?>
        {
            ["complexity"] = complexity.ToString(),
            ["model"] = selectedModel
        });

        var existingFiles = new HashSet<string>();
        var possibleDirs = new[] {
            _generatedFilesPath,
            _workingDirectory,
            Path.Combine(Directory.GetCurrentDirectory(), "generated_files")
        };

        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    existingFiles.Add(Path.GetFileName(file));
                }
            }
        }

        journey.LogStep("PROMPT_BUILD_START", "Building enhanced prompt for streaming");

        // Build prompt with conversation context (Phase 4: using optimized context)
        var fullPrompt = new System.Text.StringBuilder();
        fullPrompt.AppendLine(GetSystemPrompt());

        // Phase 4: Use optimized file context based on context tier
        if (!string.IsNullOrEmpty(optimizedContext))
        {
            fullPrompt.AppendLine();
            fullPrompt.AppendLine(optimizedContext);
        }

        // Phase 4: Use filtered messages based on context tier
        if (filteredMessages.Count > 0)
        {
            fullPrompt.AppendLine();
            fullPrompt.AppendLine("Previous conversation:");
            foreach (var msg in filteredMessages)
            {
                var role = msg.Role == "user" ? "User" : "Assistant";
                fullPrompt.AppendLine($"{role}: {msg.Content}");
                fullPrompt.AppendLine();
            }
            fullPrompt.AppendLine("Current request:");
        }

        fullPrompt.Append(prompt);
        var enhancedPrompt = fullPrompt.ToString();

        journey.LogStep("PROMPT_BUILD_COMPLETE", $"Enhanced prompt: {enhancedPrompt.Length} chars");

        // Phase 2: Use selected model based on task complexity
        // Use stdin for prompt delivery with stream-json format for true streaming
        var startInfo = new ProcessStartInfo
        {
            FileName = _claudePath,
            Arguments = $"--print --output-format stream-json --verbose --include-partial-messages --model {selectedModel}",
            WorkingDirectory = _workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        TokenUsage? finalUsage = null;
        string? model = selectedModel; // Initialize with selected model
        var claudeStartTime = stopwatch.ElapsedMilliseconds;
        int streamEventCount = 0;

        process.Start();
        journey.LogStep("CLAUDE_STREAM_START", $"PID: {process.Id}, Model: {selectedModel}");

        // Pipe prompt via stdin (same as non-streaming method)
        await process.StandardInput.WriteAsync(enhancedPrompt);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();
        journey.LogStep("PROMPT_PIPED", "Prompt sent via stdin");

        yield return new StreamEvent { Type = "start", Data = new { requestId = journey.RequestId, model = selectedModel, complexity = complexity.ToString(), contextTier = contextTier.ToString() } };
        journey.LogStreamEvent("start");

        // Read stdout line by line for streaming
        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            StreamEvent? eventToYield = null;

            // Try to parse JSON
            var parsed = TryParseStreamLine(line, model, stopwatch.ElapsedMilliseconds);

            if (parsed.Content != null)
            {
                outputBuilder.Append(parsed.Content);
                streamEventCount++;
                eventToYield = new StreamEvent
                {
                    Type = "content",
                    Data = new { content = parsed.Content, elapsed = stopwatch.ElapsedMilliseconds }
                };

                // Log every 10th content event to avoid spam
                if (streamEventCount % 10 == 0)
                {
                    journey.LogStreamEvent("content_batch", outputBuilder.Length);
                }
            }
            else if (parsed.Error != null)
            {
                journey.LogError("STREAM_ERROR", parsed.Error);
                eventToYield = new StreamEvent { Type = "error", Data = new { message = parsed.Error } };
            }

            if (parsed.Usage != null)
            {
                finalUsage = parsed.Usage;
                journey.LogTokenUsage(
                    finalUsage.InputTokens,
                    finalUsage.OutputTokens,
                    finalUsage.CacheReadTokens,
                    finalUsage.CacheCreationTokens,
                    finalUsage.Cost
                );
            }

            if (parsed.Model != null)
            {
                model = parsed.Model;
            }

            if (eventToYield != null)
            {
                yield return eventToYield;
            }
        }

        await process.WaitForExitAsync();
        var claudeDuration = stopwatch.ElapsedMilliseconds - claudeStartTime;
        journey.LogStep("CLAUDE_STREAM_COMPLETE", $"Duration={claudeDuration}ms, Events={streamEventCount}");

        // Check for new files and move to session folder
        journey.LogStep("NEW_FILE_SCAN_START", "Scanning for newly generated files");
        var newFiles = new List<string>();
        var targetDir = !string.IsNullOrEmpty(sessionId)
            ? FileManager.Instance.GetSessionGeneratedPath(sessionId)
            : _generatedFilesPath;

        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    var fileName = Path.GetFileName(file);
                    if (!existingFiles.Contains(fileName))
                    {
                        var targetPath = Path.Combine(targetDir, fileName);
                        if (file != targetPath)
                        {
                            try { File.Move(file, targetPath, true); } catch { }
                        }
                        newFiles.Add(fileName);
                    }
                }
            }
        }

        if (newFiles.Count > 0)
        {
            journey.LogFileGeneration(newFiles);
        }

        var summary = journey.Complete(process.ExitCode == 0, outputBuilder.ToString());
        journey.LogStreamEvent("complete", outputBuilder.Length);

        yield return new StreamEvent
        {
            Type = "complete",
            Data = new
            {
                success = process.ExitCode == 0,
                output = outputBuilder.ToString(),
                files = newFiles,
                usage = finalUsage,
                model = model,
                complexity = complexity.ToString(),
                contextTier = contextTier.ToString(),
                elapsed = stopwatch.ElapsedMilliseconds,
                requestId = journey.RequestId,
                journeyDurationMs = summary.TotalDurationMs
            }
        };
    }

    /// <summary>
    /// Generate a smart title for the conversation.
    /// Uses Haiku model for fast, cost-effective title generation (73% cheaper than Sonnet).
    /// </summary>
    public async Task<string> GenerateTitleAsync(string firstMessage)
    {
        var journey = new JourneyLogger("GENERATE_TITLE", null);

        journey.LogStep("TITLE_GEN_START", "Generating smart title with Haiku");
        journey.LogModelSelection(MODEL_HAIKU, "Simple", "73% cost savings");

        try
        {
            // Use a very concise prompt for quick title generation
            var titlePrompt = $"Generate a 3-5 word title for this request (output only the title, nothing else): {firstMessage.Substring(0, Math.Min(150, firstMessage.Length))}";

            journey.LogPrompt(titlePrompt, titlePrompt.Length);

            // Use Haiku for title generation - faster and 73% cheaper
            var startInfo = new ProcessStartInfo
            {
                FileName = _claudePath,
                Arguments = $"--print --model {MODEL_HAIKU} --dangerously-skip-permissions \"{EscapePrompt(titlePrompt)}\"",
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var claudeStartTime = journey.ElapsedMs;
            process.Start();
            journey.LogStep("CLAUDE_START", $"PID: {process.Id}");

            // Set a timeout for title generation (30 seconds max)
            var titleTask = process.StandardOutput.ReadToEndAsync();
            var completedTask = await Task.WhenAny(titleTask, Task.Delay(30000));

            string title;
            if (completedTask == titleTask)
            {
                title = await titleTask;
                await process.WaitForExitAsync();
                journey.LogStep("CLAUDE_COMPLETE", $"Duration: {journey.ElapsedMs - claudeStartTime}ms");
            }
            else
            {
                // Timeout - kill process and use fallback
                try { process.Kill(); } catch { }
                journey.LogError("TIMEOUT", "Title generation timed out after 30s");
                journey.Complete(false, "Timeout - using fallback");
                return firstMessage.Length > 50 ? firstMessage.Substring(0, 47) + "..." : firstMessage;
            }

            // Clean up the title
            title = title.Trim()
                .Trim('"', '\'', '*', '_')  // Remove quotes and markdown
                .Replace("Title:", "")
                .Replace("title:", "")
                .Trim();

            // Extract just the first line if there's extra text
            var lines = title.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                title = lines[0].Trim();
            }

            if (title.Length > 50)
                title = title.Substring(0, 47) + "...";

            if (string.IsNullOrWhiteSpace(title))
            {
                title = firstMessage.Length > 50 ? firstMessage.Substring(0, 47) + "..." : firstMessage;
            }

            journey.LogStep("TITLE_GENERATED", $"Title: {title}");
            journey.Complete(true, title);
            return title;
        }
        catch (Exception ex)
        {
            journey.LogError("EXCEPTION", ex.Message, ex);
            journey.Complete(false, ex.Message);
            return firstMessage.Length > 50 ? firstMessage.Substring(0, 47) + "..." : firstMessage;
        }
    }

    /// <summary>
    /// Summarize a conversation for context continuity.
    /// Uses Haiku for fast, cost-effective summarization (73% cheaper than Sonnet).
    /// </summary>
    public async Task<string> SummarizeSessionAsync(string conversationHistory)
    {
        var journey = new JourneyLogger("SUMMARIZE_SESSION", null);

        journey.LogStep("SUMMARIZE_START", $"Summarizing {conversationHistory.Length} chars with Haiku");
        journey.LogModelSelection(MODEL_HAIKU, "Simple", "73% cost savings");

        try
        {
            // Truncate very long conversations to avoid token limits
            var maxChars = 30000; // ~7,500 tokens
            var truncatedHistory = conversationHistory.Length > maxChars
                ? conversationHistory.Substring(conversationHistory.Length - maxChars)
                : conversationHistory;

            journey.LogStep("CONTEXT_PREPARED", $"Using {truncatedHistory.Length} chars (truncated: {conversationHistory.Length > maxChars})");

            var summaryPrompt = $@"Summarize this conversation concisely:
- Key topics and decisions
- Files created/modified
- Current state and next steps

Max 200 words, bullet points.

{truncatedHistory}";

            journey.LogPrompt(summaryPrompt, summaryPrompt.Length);

            // Use Haiku for summarization - faster and 73% cheaper
            var startInfo = new ProcessStartInfo
            {
                FileName = _claudePath,
                Arguments = $"--print --model {MODEL_HAIKU} --dangerously-skip-permissions \"{EscapePrompt(summaryPrompt)}\"",
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var claudeStartTime = journey.ElapsedMs;
            process.Start();
            journey.LogStep("CLAUDE_START", $"PID: {process.Id}");

            // 90 second timeout for summarization
            var summaryTask = process.StandardOutput.ReadToEndAsync();
            var completedTask = await Task.WhenAny(summaryTask, Task.Delay(90000));

            string summary;
            if (completedTask == summaryTask)
            {
                summary = await summaryTask;
                await process.WaitForExitAsync();
                journey.LogStep("CLAUDE_COMPLETE", $"Duration: {journey.ElapsedMs - claudeStartTime}ms");
            }
            else
            {
                try { process.Kill(); } catch { }
                journey.LogError("TIMEOUT", "Summarization timed out after 90s");
                journey.Complete(false, "Timeout");
                return "Summary generation timed out. Continuing without summary.";
            }

            journey.LogStep("SUMMARY_GENERATED", $"Summary: {summary.Trim().Length} chars");
            journey.Complete(true, summary.Trim());
            return summary.Trim();
        }
        catch (Exception ex)
        {
            journey.LogError("EXCEPTION", ex.Message, ex);
            journey.Complete(false, ex.Message);
            return "Summary generation failed.";
        }
    }

    private (string? Content, string? Error, TokenUsage? Usage, string? Model) TryParseStreamLine(string line, string? currentModel, long elapsed)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            switch (type)
            {
                case "system":
                    // Skip system init messages
                    return (null, null, null, null);

                case "stream_event":
                    // Handle streaming partial messages (--include-partial-messages)
                    // Format: {"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":"..."}}}
                    if (root.TryGetProperty("event", out var eventObj))
                    {
                        var eventType = eventObj.TryGetProperty("type", out var et) ? et.GetString() : null;

                        if (eventType == "content_block_delta" &&
                            eventObj.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("text", out var textDelta))
                        {
                            var streamText = textDelta.GetString();
                            if (!string.IsNullOrEmpty(streamText))
                            {
                                return (streamText, null, null, null);
                            }
                        }
                    }
                    return (null, null, null, null);

                case "assistant":
                    // Extract content from assistant message
                    if (root.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentArray) &&
                        contentArray.ValueKind == JsonValueKind.Array)
                    {
                        var textContent = new System.Text.StringBuilder();
                        foreach (var item in contentArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("type", out var itemType) &&
                                itemType.GetString() == "text" &&
                                item.TryGetProperty("text", out var text))
                            {
                                textContent.Append(text.GetString());
                            }
                        }

                        // Get model from message
                        string? model = null;
                        if (message.TryGetProperty("model", out var modelProp))
                        {
                            model = modelProp.GetString();
                        }

                        var content = textContent.ToString();
                        return (string.IsNullOrEmpty(content) ? null : content, null, null, model);
                    }
                    return (null, null, null, null);

                case "result":
                    // Extract usage from result
                    if (root.TryGetProperty("usage", out var usageObj))
                    {
                        var usage = new TokenUsage
                        {
                            InputTokens = usageObj.TryGetProperty("input_tokens", out var inp) ? inp.GetInt32() : 0,
                            OutputTokens = usageObj.TryGetProperty("output_tokens", out var outp) ? outp.GetInt32() : 0,
                            CacheCreationTokens = usageObj.TryGetProperty("cache_creation_input_tokens", out var cc) ? cc.GetInt32() : 0,
                            CacheReadTokens = usageObj.TryGetProperty("cache_read_input_tokens", out var cr) ? cr.GetInt32() : 0
                        };
                        var pricing = ModelPrices.GetValueOrDefault(currentModel ?? "default", ModelPrices["default"]);
                        usage.Cost = CalculateCost(usage, pricing);
                        return (null, null, usage, null);
                    }
                    return (null, null, null, null);

                case "error":
                    var errorMsg = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "Unknown error";
                    return (null, errorMsg, null, null);

                default:
                    return (null, null, null, null);
            }
        }
        catch
        {
            // Not JSON, treat as raw content
            return (line, null, null, null);
        }
    }

    private static decimal CalculateCost(TokenUsage usage, ModelPricing pricing)
    {
        var inputCost = (usage.InputTokens / 1_000_000m) * pricing.InputPer1M;
        var outputCost = (usage.OutputTokens / 1_000_000m) * pricing.OutputPer1M;
        var cacheCreateCost = (usage.CacheCreationTokens / 1_000_000m) * pricing.CacheWritePer1M;
        var cacheReadCost = (usage.CacheReadTokens / 1_000_000m) * pricing.CacheReadPer1M;

        return inputCost + outputCost + cacheCreateCost + cacheReadCost;
    }

    private string EscapePrompt(string prompt)
    {
        return prompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`");
    }
}

// Response models
public class ZimaResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Output { get; set; }
    public string? Errors { get; set; }
    public string? Model { get; set; }
    public string? SessionId { get; set; }
    public TokenUsage? Usage { get; set; }
    public List<string> GeneratedFiles { get; set; } = new();

    // Phase 2: Multi-model routing info
    public string? TaskComplexity { get; set; }
    public string? CostInfo { get; set; }

    // Phase 4: Context tier info
    public string? ContextTierUsed { get; set; }

    // Journey tracking info for performance analysis
    public string? RequestId { get; set; }
    public long JourneyDurationMs { get; set; }
}

public class TokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheCreationTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public decimal Cost { get; set; }
}

public class StreamEvent
{
    public string Type { get; set; } = "";
    public object? Data { get; set; }
}

public record ModelPricing(decimal InputPer1M, decimal OutputPer1M, decimal CacheWritePer1M, decimal CacheReadPer1M);

// Claude CLI JSON response models
public class ClaudeJsonResponse
{
    public string? Result { get; set; }
    public string? Model { get; set; }
    public ClaudeUsage? Usage { get; set; }
}

public class ClaudeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }
}

public class ClaudeStreamEvent
{
    public string? Type { get; set; }
    public string? Content { get; set; }
    public string? Model { get; set; }
    public string? Error { get; set; }
    public ClaudeUsage? Usage { get; set; }
}

// Conversation message for prompt caching
public class ConversationMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsSummary { get; set; } = false;
}
