namespace ZimaFileService.Api;

/// <summary>
/// Minimal system prompt - Claude CLI uses its own prompting.
/// This is just for backward compatibility with the HTTP API.
/// </summary>
public static class SystemPrompt
{
    private static readonly string _defaultPrompt = @"You are ZIMA, a document processing assistant.
You have tools for creating Excel, Word, PDF, and PowerPoint files.
Generated files are saved to the generated_files/ directory.";

    public static string Build(string modelId) => _defaultPrompt;

    public static Task<string> BuildAsync(string modelId) => Task.FromResult(_defaultPrompt);

    public static string ForModel(string modelId) => _defaultPrompt;

    public static string Environment(string modelId)
    {
        return $@"<env>
Working directory: {FileManager.Instance.WorkingDirectory}
Generated files: {FileManager.Instance.GeneratedFilesPath}
</env>";
    }

    public static void ClearCache() { }
}
