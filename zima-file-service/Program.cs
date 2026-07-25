using ZimaFileService;
using ZimaFileService.Api;
using ZimaFileService.Middleware;

// Check if we should create a test Excel file
if (args.Length > 0 && args[0] == "--create-excel-test")
{
    // Initialize FileManager
    FileManager.Initialize(null);

    try
    {
        var excelTool = new ZimaFileService.Tools.ExcelTool();

        var arguments = new Dictionary<string, object>
        {
            ["file_path"] = Path.Combine(FileManager.Instance.GeneratedFilesPath, "random_products.xlsx"),
            ["sheet_name"] = "Products",
            ["headers"] = new List<object> { "Product Name", "Price ($)" },
            ["rows"] = new List<object>
            {
                new List<object> { "Wireless Bluetooth Headphones", "89.99" },
                new List<object> { "Smart Home Security Camera", "129.95" },
                new List<object> { "Organic Green Tea Blend", "24.50" },
                new List<object> { "Professional Gaming Mouse", "67.89" },
                new List<object> { "Stainless Steel Water Bottle", "35.99" },
                new List<object> { "Premium Yoga Mat", "45.75" },
                new List<object> { "LED Desk Lamp with USB Charging", "52.99" },
                new List<object> { "Bluetooth Portable Speaker", "78.99" },
                new List<object> { "Memory Foam Pillow", "39.99" },
                new List<object> { "Wireless Phone Charger", "29.99" }
            },
            ["auto_fit_columns"] = true
        };

        string result = await excelTool.CreateExcelAsync(arguments);
        Console.WriteLine("Excel file created successfully:");
        Console.WriteLine(result);
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating Excel file: {ex.Message}");
        return;
    }
}

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

    // Configure CORS - use environment variable for allowed origins
    var allowedOrigins = Environment.GetEnvironmentVariable("ZIMA_ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
        ?? new[] { "*" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ConfiguredCors", policy =>
        {
            if (allowedOrigins.Length == 1 && allowedOrigins[0] == "*")
            {
                // Development mode - allow all (with warning)
                Console.Error.WriteLine("[SECURITY WARNING] CORS is set to allow all origins. Set ZIMA_ALLOWED_ORIGINS for production.");
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production mode - restrict to specific origins
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

    // Configure request size limits
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Max request body size: 100 MB (for file uploads)
        var maxSizeMb = int.TryParse(Environment.GetEnvironmentVariable("ZIMA_MAX_REQUEST_SIZE_MB"), out var mb) ? mb : 100;
        options.Limits.MaxRequestBodySize = maxSizeMb * 1024 * 1024;

        // Request header limits
        options.Limits.MaxRequestHeaderCount = 100;
        options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
    });

    // Add ZIMA service
    builder.Services.AddSingleton<ZimaService>();

    var app = builder.Build();

    // Security middleware pipeline (order matters!)
    // 1. Security headers - always add security headers first
    app.UseSecurityHeaders();

    // 2. Rate limiting - protect against abuse before processing
    app.UseRateLimiting();

    // 3. API Key Authentication - verify identity before accessing resources
    app.UseApiKeyAuthentication();

    // 4. CORS - allow configured origins
    app.UseCors("ConfiguredCors");

    // Map controllers
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
