using Amazon.S3;
using Amazon.S3.Transfer;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.Http.Resilience;
using SceneSplit.Api.Extenstions;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Interceptors;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Sdk;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var maxImageSize = int.Parse(builder.Configuration[ApiConfigurationKeys.MAX_IMAGE_SIZE] ?? (10 * 1024 * 1024).ToString());
builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = maxImageSize;
})
.AddMessagePackProtocol();

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddApplicationCors(builder.Configuration, myAllowSpecificOrigins, builder.Environment.IsDevelopment());

builder.Services.AddTransient<GrpcErrorInterceptor>();
builder.Services.AddTransient<GrpcResilienceInterceptor>();

builder.Services.AddGrpcClient<Compression.CompressionClient>(o =>
{
    o.Address = new Uri(builder.Configuration[ApiConfigurationKeys.COMPRESSION_API_URL]
        ?? throw new InvalidOperationException($"{ApiConfigurationKeys.COMPRESSION_API_URL} is missing or null."));
})
.AddInterceptor<GrpcErrorInterceptor>()
.AddInterceptor<GrpcResilienceInterceptor>()
.AddResilienceHandler(options =>
{
    options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(180)
    };

    options.AttemptTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(180),
        MinimumThroughput = 10,
        FailureRatio = 0.5,
        BreakDuration = TimeSpan.FromSeconds(30)
    };
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    return new GrpcWebHandler(GrpcWebMode.GrpcWeb, handler);
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<ITransferUtility>(sp =>
{
    var s3 = sp.GetRequiredService<IAmazonS3>();
    return new TransferUtility(s3);
});
builder.Services.AddSingleton<IStorageService, S3StorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UseCors(myAllowSpecificOrigins);

app.MapControllers();
app.MapHub<SceneSplitHub>("/hubs/scene-split");

app.MapHealthChecks("/health");

app.UsePathBase("/api");

await app.RunAsync();