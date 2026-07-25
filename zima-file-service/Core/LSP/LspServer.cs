using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ZimaFileService.Core.LSP;

/// <summary>
/// LSP Server definitions for various languages.
/// Modeled after OpenCode's server.ts
/// </summary>
public static class LspServers
{
    private static readonly Dictionary<string, LspServerInfo> _servers = new();

    static LspServers()
    {
        // C# - csharp-ls
        Register(new LspServerInfo
        {
            Id = "csharp",
            Name = "C# Language Server",
            Extensions = new[] { ".cs" },
            RootPatterns = new[] { ".sln", ".csproj", "global.json" },
            SpawnCommand = "csharp-ls",
            SpawnArgs = Array.Empty<string>(),
            InstallCommand = "dotnet tool install --global csharp-ls"
        });

        // TypeScript/JavaScript
        Register(new LspServerInfo
        {
            Id = "typescript",
            Name = "TypeScript Language Server",
            Extensions = new[] { ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs" },
            RootPatterns = new[] { "package.json", "tsconfig.json" },
            SpawnCommand = "typescript-language-server",
            SpawnArgs = new[] { "--stdio" },
            InstallCommand = "npm install -g typescript-language-server typescript"
        });

        // Python - Pyright
        Register(new LspServerInfo
        {
            Id = "python",
            Name = "Pyright",
            Extensions = new[] { ".py", ".pyi" },
            RootPatterns = new[] { "pyproject.toml", "setup.py", "requirements.txt" },
            SpawnCommand = "pyright-langserver",
            SpawnArgs = new[] { "--stdio" },
            InstallCommand = "npm install -g pyright"
        });

        // Go - gopls
        Register(new LspServerInfo
        {
            Id = "go",
            Name = "gopls",
            Extensions = new[] { ".go" },
            RootPatterns = new[] { "go.mod", "go.sum" },
            SpawnCommand = "gopls",
            SpawnArgs = Array.Empty<string>(),
            InstallCommand = "go install golang.org/x/tools/gopls@latest"
        });

        // Rust - rust-analyzer
        Register(new LspServerInfo
        {
            Id = "rust",
            Name = "rust-analyzer",
            Extensions = new[] { ".rs" },
            RootPatterns = new[] { "Cargo.toml", "Cargo.lock" },
            SpawnCommand = "rust-analyzer",
            SpawnArgs = Array.Empty<string>(),
            InstallCommand = "rustup component add rust-analyzer"
        });

        // JSON
        Register(new LspServerInfo
        {
            Id = "json",
            Name = "JSON Language Server",
            Extensions = new[] { ".json", ".jsonc" },
            RootPatterns = new[] { "package.json" },
            SpawnCommand = "vscode-json-language-server",
            SpawnArgs = new[] { "--stdio" },
            InstallCommand = "npm install -g vscode-langservers-extracted"
        });

        // HTML
        Register(new LspServerInfo
        {
            Id = "html",
            Name = "HTML Language Server",
            Extensions = new[] { ".html", ".htm" },
            RootPatterns = new[] { "package.json", "index.html" },
            SpawnCommand = "vscode-html-language-server",
            SpawnArgs = new[] { "--stdio" },
            InstallCommand = "npm install -g vscode-langservers-extracted"
        });

        // CSS
        Register(new LspServerInfo
        {
            Id = "css",
            Name = "CSS Language Server",
            Extensions = new[] { ".css", ".scss", ".less" },
            RootPatterns = new[] { "package.json" },
            SpawnCommand = "vscode-css-language-server",
            SpawnArgs = new[] { "--stdio" },
            InstallCommand = "npm install -g vscode-langservers-extracted"
        });
    }

    public static void Register(LspServerInfo server)
    {
        _servers[server.Id] = server;
    }

    public static LspServerInfo? Get(string id)
    {
        return _servers.TryGetValue(id, out var server) ? server : null;
    }

    public static LspServerInfo? GetForExtension(string extension)
    {
        return _servers.Values.FirstOrDefault(s =>
            s.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
    }

    public static List<LspServerInfo> List()
    {
        return _servers.Values.ToList();
    }

    public static bool IsAvailable(string id)
    {
        var server = Get(id);
        if (server == null) return false;

        try
        {
            var which = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = server.SpawnCommand,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            which?.WaitForExit();
            return which?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// LSP Server configuration.
/// </summary>
public class LspServerInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("extensions")]
    public string[] Extensions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("rootPatterns")]
    public string[] RootPatterns { get; set; } = Array.Empty<string>();

    [JsonPropertyName("spawnCommand")]
    public string SpawnCommand { get; set; } = "";

    [JsonPropertyName("spawnArgs")]
    public string[] SpawnArgs { get; set; } = Array.Empty<string>();

    [JsonPropertyName("installCommand")]
    public string? InstallCommand { get; set; }

    [JsonPropertyName("initialization")]
    public Dictionary<string, object>? Initialization { get; set; }
}
