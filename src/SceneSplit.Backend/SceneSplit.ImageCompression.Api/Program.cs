using Microsoft.AspNetCore.Server.Kestrel.Core;
using SceneSplit.ImageCompression.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddHealthChecks();

builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.ListenLocalhost(5163, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps();
            listenOptions.UseConnectionLogging();
        });
    }
    else
    {
        options.ListenAnyIP(8080, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseConnectionLogging();
        });
    }
});

var app = builder.Build();

app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<CompressionService>().EnableGrpcWeb();
app.MapGet("/", () => "All gRPC service are supported by default in " +
    "this example, and are callable from browser apps using the " +
    "gRPC-Web protocol");

app.MapHealthChecks("/health");

await app.RunAsync();