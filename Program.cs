using ManidocMCP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders(); // stdout をJSON-RPC専用に保つためログを無効化

var config = builder.Configuration;
VideoService.Configure(
    ffmpegPath: config["Video:FfmpegPath"],
    outDir: config["Video:OutDir"]
);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ManidocTools>();

await builder.Build().RunAsync();
