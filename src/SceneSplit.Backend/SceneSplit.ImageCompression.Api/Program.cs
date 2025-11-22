using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var maxMessageSizeBytes = int.Parse(
    builder.Configuration[ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE] ??
    (10 * 1024 * 1024).ToString()
);

builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = maxMessageSizeBytes;
    options.MaxSendMessageSize = maxMessageSizeBytes;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapGrpcService<CompressionService>().EnableGrpcWeb();
app.MapGet("/", () => "All gRPC service are supported by default in " +
    "this example, and are callable from browser apps using the " +
    "gRPC-Web protocol");

app.MapHealthChecks("/health");

await app.RunAsync();