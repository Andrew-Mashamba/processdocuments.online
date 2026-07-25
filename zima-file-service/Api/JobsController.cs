using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZimaFileService.Api;

/// <summary>
/// Phase 5: Job status controller for background processing.
/// Enables async job submission and status polling.
/// </summary>
[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly ZimaService _zimaService;
    private readonly FileManager _fileManager;

    public JobsController(ZimaService zimaService)
    {
        _zimaService = zimaService;
        _fileManager = FileManager.Instance;
    }

    /// <summary>
    /// Submit a new async generation job.
    /// Returns immediately with a job ID for status polling.
    /// </summary>
    [HttpPost("submit")]
    public Task<IActionResult> SubmitJob([FromBody] GenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Task.FromResult<IActionResult>(BadRequest(new { error = "Prompt is required" }));
        }

        // Classify task complexity
        var messageCount = request.Messages?.Count ?? 0;
        var complexity = ClassifyTaskComplexity(request.Prompt, messageCount);
        var selectedModel = GetModelForComplexity(complexity);
        var contextTier = DetermineContextTier(request.Prompt, messageCount);

        // Create job
        var job = ZimaService.CreateJob(request.Prompt, complexity, contextTier, selectedModel);

        // Start processing in background
        _ = Task.Run(async () =>
        {
            try
            {
                ZimaService.UpdateJobProgress(job.JobId, 10, "Preparing context...");

                // Convert messages
                List<ConversationMessage>? messages = null;
                if (request.Messages != null && request.Messages.Count > 0)
                {
                    messages = request.Messages.Select(m => new ConversationMessage
                    {
                        Role = m.Role,
                        Content = m.Content,
                        IsSummary = m.IsSummary
                    }).ToList();
                }

                // Get file context
                string? fileContext = null;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    ZimaService.UpdateJobProgress(job.JobId, 20, "Loading session files...");
                    var sessionFiles = _fileManager.GetSessionFileContents(request.SessionId);
                    if (sessionFiles.Count > 0)
                    {
                        var contextBuilder = new System.Text.StringBuilder();
                        contextBuilder.AppendLine("\n=== UPLOADED FILES FOR THIS SESSION ===");
                        foreach (var (fileName, content) in sessionFiles)
                        {
                            contextBuilder.AppendLine($"--- {fileName} ---");
                            contextBuilder.AppendLine(content.Content);
                            contextBuilder.AppendLine();
                        }
                        contextBuilder.AppendLine("=== END UPLOADED FILES ===\n");
                        fileContext = contextBuilder.ToString();
                    }
                }

                var fullContext = fileContext != null
                    ? fileContext + (request.Context ?? "")
                    : request.Context;

                ZimaService.UpdateJobProgress(job.JobId, 30, "Sending to AI model...");

                // Check if parallelizable
                bool useParallel = CanParallelize(request.Prompt);
                ZimaResponse result;

                if (useParallel)
                {
                    ZimaService.UpdateJobProgress(job.JobId, 40, "Parallel execution...");
                    result = await _zimaService.GenerateParallelAsync(
                        request.Prompt, fullContext, request.SessionId, messages);
                }
                else
                {
                    ZimaService.UpdateJobProgress(job.JobId, 50, "Processing request...");
                    result = await _zimaService.GenerateAsync(
                        request.Prompt, fullContext, request.SessionId, messages);
                }

                ZimaService.UpdateJobProgress(job.JobId, 90, "Finalizing...");
                ZimaService.CompleteJob(job.JobId, result);
            }
            catch (Exception ex)
            {
                ZimaService.FailJob(job.JobId, ex.Message);
            }
        });

        return Task.FromResult<IActionResult>(Ok(new JobSubmitResponse
        {
            JobId = job.JobId,
            Status = job.Status.ToString(),
            Model = selectedModel,
            Complexity = complexity.ToString(),
            ContextTier = contextTier.ToString(),
            Message = "Job submitted successfully. Poll /api/jobs/{jobId} for status."
        }));
    }

    /// <summary>
    /// Get the status of a job.
    /// </summary>
    [HttpGet("{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = ZimaService.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        var response = new JobStatusResponse
        {
            JobId = job.JobId,
            Status = job.Status.ToString(),
            Progress = job.Progress,
            CurrentStep = job.CurrentStep,
            Model = job.Model,
            Complexity = job.Complexity.ToString(),
            ContextTier = job.ContextTier.ToString(),
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Error = job.Error
        };

        // Include result if completed
        if (job.Status == ZimaService.JobStatus.Completed && job.Result != null)
        {
            response.Result = new JobResultDto
            {
                Success = job.Result.Success,
                Message = job.Result.Message,
                Output = job.Result.Output,
                Model = job.Result.Model,
                GeneratedFiles = job.Result.GeneratedFiles,
                Usage = job.Result.Usage != null ? new UsageDto
                {
                    InputTokens = job.Result.Usage.InputTokens,
                    OutputTokens = job.Result.Usage.OutputTokens,
                    CacheCreationTokens = job.Result.Usage.CacheCreationTokens,
                    CacheReadTokens = job.Result.Usage.CacheReadTokens,
                    Cost = job.Result.Usage.Cost
                } : null
            };
        }

        return Ok(response);
    }

    /// <summary>
    /// Long-poll for job completion (blocks until job completes or timeout).
    /// </summary>
    [HttpGet("{jobId}/wait")]
    public async Task<IActionResult> WaitForJob(string jobId, [FromQuery] int timeoutSeconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Min(timeoutSeconds, 300)); // Max 5 min

        while (DateTime.UtcNow < deadline)
        {
            var job = ZimaService.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            if (job.Status == ZimaService.JobStatus.Completed ||
                job.Status == ZimaService.JobStatus.Failed)
            {
                return await Task.FromResult(GetJobStatus(jobId));
            }

            await Task.Delay(500); // Poll every 500ms
        }

        return Ok(new { status = "timeout", message = "Job still processing" });
    }

    /// <summary>
    /// Cancel a pending or processing job.
    /// </summary>
    [HttpDelete("{jobId}")]
    public IActionResult CancelJob(string jobId)
    {
        var job = ZimaService.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        if (job.Status == ZimaService.JobStatus.Completed ||
            job.Status == ZimaService.JobStatus.Failed)
        {
            return BadRequest(new { error = "Job already finished" });
        }

        ZimaService.FailJob(jobId, "Cancelled by user");
        return Ok(new { message = "Job cancelled" });
    }

    /// <summary>
    /// Cleanup old jobs (admin endpoint).
    /// </summary>
    [HttpPost("cleanup")]
    public IActionResult CleanupOldJobs()
    {
        ZimaService.CleanupOldJobs();
        return Ok(new { message = "Old jobs cleaned up" });
    }

    // Helper methods (duplicated from ZimaService for controller use)
    private static ZimaService.TaskComplexity ClassifyTaskComplexity(string prompt, int messageCount)
    {
        var lowerPrompt = prompt.ToLower();

        if (System.Text.RegularExpressions.Regex.IsMatch(lowerPrompt,
            @"(create|generate|make)\s+(\d+|multiple|several|many)\s+(files|documents|spreadsheets|reports)"))
            return ZimaService.TaskComplexity.Complex;

        if (prompt.Length > 1000)
            return ZimaService.TaskComplexity.Complex;

        var simplePatterns = new[] { "what is", "what's", "how do", "can you", "explain" };
        if (simplePatterns.Any(p => lowerPrompt.StartsWith(p)))
            return ZimaService.TaskComplexity.Simple;

        return ZimaService.TaskComplexity.Standard;
    }

    private static string GetModelForComplexity(ZimaService.TaskComplexity complexity)
    {
        return complexity switch
        {
            ZimaService.TaskComplexity.Simple => "claude-3-5-haiku-20241022",
            ZimaService.TaskComplexity.Complex => "claude-opus-4-20250514",
            _ => "claude-sonnet-4-20250514"
        };
    }

    private static ZimaService.ContextTier DetermineContextTier(string prompt, int messageCount)
    {
        if (messageCount == 0)
            return ZimaService.ContextTier.Minimal;

        var lowerPrompt = prompt.ToLower();
        if (lowerPrompt.StartsWith("what") || lowerPrompt.StartsWith("how"))
            return ZimaService.ContextTier.Planning;

        if (lowerPrompt.Contains("update") || lowerPrompt.Contains("modify"))
            return ZimaService.ContextTier.Brownfield;

        return ZimaService.ContextTier.Execution;
    }

    private static bool CanParallelize(string prompt)
    {
        var lowerPrompt = prompt.ToLower();
        return System.Text.RegularExpressions.Regex.IsMatch(lowerPrompt,
            @"(create|generate|make)\s+\d+\s+(files|documents|spreadsheets)");
    }
}

// Job DTOs
public class JobSubmitResponse
{
    public string JobId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Model { get; set; }
    public string? Complexity { get; set; }
    public string? ContextTier { get; set; }
    public string Message { get; set; } = "";
}

public class JobStatusResponse
{
    public string JobId { get; set; } = "";
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public string? CurrentStep { get; set; }
    public string? Model { get; set; }
    public string? Complexity { get; set; }
    public string? ContextTier { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
    public JobResultDto? Result { get; set; }
}

public class JobResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Output { get; set; }
    public string? Model { get; set; }
    public List<string> GeneratedFiles { get; set; } = new();
    public UsageDto? Usage { get; set; }
}
