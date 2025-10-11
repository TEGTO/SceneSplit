using SceneSplit.ImageCompression.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<CompressionService>().EnableGrpcWeb();
app.MapGet("/", () => "All gRPC service are supported by default in " +
    "this example, and are callable from browser apps using the " +
    "gRPC-Web protocol");

app.MapHealthChecks("/health");

await app.RunAsync();