using Amazon.S3;
using Amazon.S3.Transfer;
using SceneSplit.Api.Extenstions;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.GrpcClientShared.Extenstions;
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

var compressionApiUrl = builder.Configuration[ApiConfigurationKeys.COMPRESSION_API_URL]
    ?? throw new InvalidOperationException($"{ApiConfigurationKeys.COMPRESSION_API_URL} is missing or null.");

builder.Services.AddGrpcClientWeb<Compression.CompressionClient>(compressionApiUrl);

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