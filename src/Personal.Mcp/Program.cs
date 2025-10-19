using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using Personal.Mcp.Tools;
using Personal.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO.Abstractions;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/personal-mcp-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Personal MCP Server...");

    var useStreamableHttp = UseStreamableHttp(Environment.GetEnvironmentVariables(), args);

    IHostApplicationBuilder builder = useStreamableHttp
                                    ? WebApplication.CreateBuilder(args)
                                    : Host.CreateApplicationBuilder(args);

    // Use Serilog for logging
    if (useStreamableHttp)
    {
        ((WebApplicationBuilder)builder).Host.UseSerilog();
    }
    else
    {
        ((HostApplicationBuilder)builder).Services.AddSerilog();
    }

    // var builder = Host.CreateApplicationBuilder(args);

    // Configure console logging to stderr so stdio transport isn't polluted
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<UserService>();

    var mcpServerBuilder = builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = "Personal.Mcp", Version = "0.2.1" };
        })
        .WithToolsFromAssembly();

    if (useStreamableHttp)
    {
        
        mcpServerBuilder.WithHttpTransport(o => o.Stateless = true);
    }
    else
    {
        mcpServerBuilder.WithStdioServerTransport();
    }

    // Register core services used by tools
    builder.Services.AddSingleton<IFileSystem, FileSystem>();
    builder.Services.AddSingleton<IVaultService, VaultService>();
    builder.Services.AddSingleton<IndexService>();
    builder.Services.AddSingleton<TagService>();
    builder.Services.AddSingleton<LinkService>();

    IHost app;
    if (useStreamableHttp)
    {
        var webApp = (builder as WebApplicationBuilder)!.Build();

        Log.Information("Personal MCP Server configured for HTTP transport");

        webApp.UseHttpsRedirection();
        webApp.MapMcp("/mcp"); // .RequireAuthorization();

        app = webApp;
    }
    else
    {
        var consoleApp = (builder as HostApplicationBuilder)!.Build();

        Log.Information("Personal MCP Server configured for STDIO transport");

        app = consoleApp;
    }

    Log.Information("Starting Personal MCP Server application...");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Personal MCP Server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}


static bool UseStreamableHttp(System.Collections.IDictionary env, string[] args)
{
    var useHttp = env.Contains("UseHttp") &&
                  bool.TryParse(env["UseHttp"]?.ToString()?.ToLowerInvariant(), out var result) && result;
    if (args.Length == 0)
    {
        return useHttp;
    }

    useHttp = args.Contains("--http", StringComparer.InvariantCultureIgnoreCase);

    return useHttp;
}
