using Serilog;

namespace Apex.SemanticKernelAgents;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            //.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
            //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        //var kernel = builder.Services.AddKernel();
        //builder.Services.AddAzureOpenAIChatCompletion(
        //    deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
        //    modelId: Env.Var("AzureOpenAI:TextCompletionModelId")!,
        //    endpoint: Env.Var("AzureOpenAI:Endpoint")!,
        //    serviceId: Env.Var("AzureOpenAI:AzureOpenAIChat")!,
        //    apiKey: Env.Var("AzureOpenAI:ApiKey")!);

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
