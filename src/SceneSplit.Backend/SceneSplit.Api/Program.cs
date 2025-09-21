using SceneSplit.Api.Extenstions;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Sevices.ImagePersistent;
using SceneSplit.Api.Sevices.SceneImageProcessor;
using SceneSplit.Configuration;

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

builder.Services.AddSingleton<IImagePersistentService, ImagePersistentService>();
builder.Services.AddSingleton<ISceneImageProcessor, SceneImageProcessor>();

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

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

await app.RunAsync();
