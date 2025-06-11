using Microsoft.SemanticKernel;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

Log.Logger = new LoggerConfiguration()
    //.MinimumLevel.Debug()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var kernel = builder.Services.AddKernel();

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.Services.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);

////builder.Services.AddOpenAIFiles(
////    //endpoint: Env.Var("AzureOpenAI:Endpoint")!,
////    apiKey: configuration["AzureOpenAI:ApiKey"]!);

builder.Services.AddAzureOpenAITextToAudio(
    deploymentName: configuration["AzureOpenAI:TextToSoundDeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint2"]!,
    apiKey: configuration["AzureOpenAI:ApiKey2"]!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
