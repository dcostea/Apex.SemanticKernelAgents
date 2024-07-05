using Microsoft.SemanticKernel;
using Serilog;

namespace Apex.SemanticKernelAgents;

public class Program
{
    public static void Main(string[] args)
    {
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

        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
            endpoint: Env.Var("AzureOpenAI:Endpoint")!,
            apiKey: Env.Var("AzureOpenAI:ApiKey")!);

        builder.Services.AddAzureOpenAIFiles(
            endpoint: Env.Var("AzureOpenAI:Endpoint")!,
            apiKey: Env.Var("AzureOpenAI:ApiKey")!);

        builder.Services.AddAzureOpenAITextToAudio(
            deploymentName: Env.Var("AzureOpenAI:TextToSoundDeploymentName")!,
            endpoint: Env.Var("AzureOpenAI:Endpoint2")!,
            apiKey: Env.Var("AzureOpenAI:ApiKey2")!);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}
