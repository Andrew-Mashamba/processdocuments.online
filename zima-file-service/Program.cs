using ZimaFileService;
using ZimaFileService.Api;

// Check if running in MCP mode (stdio) or HTTP mode
var runMode = args.Length > 0 && args[0] == "--http" ? "http" : "mcp";

// Get working directory from args or environment
string? workingDir = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--workdir" && i + 1 < args.Length)
    {
        workingDir = args[i + 1];
    }
}

// Initialize FileManager
FileManager.Initialize(workingDir);

if (runMode == "http")
{
    // Run as HTTP API server
    var builder = WebApplication.CreateBuilder(args);

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add ZIMA service
    builder.Services.AddSingleton<ZimaService>();

    var app = builder.Build();

    // Configure pipeline
    app.UseCors("AllowAll");
    app.MapControllers();

    // Serve static files from generated_files for downloads
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            FileManager.Instance.GeneratedFilesPath),
        RequestPath = "/downloads"
    });

    var fm = FileManager.Instance;
    Console.WriteLine($"ZIMA File Service HTTP API starting...");
    Console.WriteLine($"Working directory: {fm.WorkingDirectory}");
    Console.WriteLine($"Generated files: {fm.GeneratedFilesPath}");
    Console.WriteLine($"Uploaded files: {fm.UploadedFilesPath}");
    Console.WriteLine($"API running on: http://localhost:5000");

    app.Run("http://0.0.0.0:5000");
}
else
{
    // Run as MCP server (stdio)
    var server = new McpServer();
    await server.RunAsync();
}
