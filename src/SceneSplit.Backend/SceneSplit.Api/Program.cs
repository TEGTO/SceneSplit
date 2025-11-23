using Amazon.S3;
using Amazon.S3.Transfer;
using SceneSplit.Api.Extenstions;
using SceneSplit.Api.HostedServices;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.GrpcClientShared.Extenstions;
using SceneSplit.ImageCompression.Sdk;
using SceneSplit.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAwsOpenTelemetryMetrics(ApiConfigurationKeys.TELEMETRY_SERVICE_NAME);
}

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

var compressionApiUrl = builder.Configuration[ApiConfigurationKeys.COMPRESSION_API_URL]
    ?? throw new InvalidOperationException($"{ApiConfigurationKeys.COMPRESSION_API_URL} is missing or null.");

builder.Services.AddGrpcClientWeb<Compression.CompressionClient>(compressionApiUrl, configureChannelOptions: (sp, options) =>
{
    options.MaxReceiveMessageSize = maxImageSize;
    options.MaxSendMessageSize = maxImageSize;
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHostedService<S3ObjectImageWatcher>();

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

app.MapDefaultEndpoints();

app.UsePathBase("/api");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(myAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();
app.MapHub<SceneSplitHub>("/hubs/scene-split");
app.MapHealthChecks("/health");

await app.RunAsync();