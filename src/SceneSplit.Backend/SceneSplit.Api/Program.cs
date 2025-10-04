using Amazon.S3;
using Grpc.Net.Client.Web;
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
.AddResilienceHandler()
.ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IStorageService, S3StorageService>();

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