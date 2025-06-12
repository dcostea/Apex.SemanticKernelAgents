using System.ComponentModel;
using Apex.SemanticKernelAgents.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToAudio;
using Serilog;

namespace Apex.SemanticKernelAgents.Plugins;

public class AlertsPlugin
{
    private readonly ITextToAudioService _textToAudioService;
    
    public AlertsPlugin(ITextToAudioService textToAudioService)
    {
        _textToAudioService = textToAudioService;
    }

    [KernelFunction, Description("Somebody is going to die.")]
    public async Task DiedAlertAsync([Description("The name of the person who is going to die.")] string agentName)
    {
        await TryPlayTextAsync($"{agentName} WILL DIE!");
        Console.WriteLine($"{agentName} WILL DIE!");
    }

    [KernelFunction, Description("Somebody is being sarcastic.")]
    public async Task SarcasmAlertAsync([Description("The name of the person who is getting sarcastic.")] string agentName)
    {
        await TryPlayTextAsync($"{agentName} BECOMES SARCASTIC!");
        Console.WriteLine($"{agentName} BECOMES SARCASTIC!");
    }

    [KernelFunction, Description("Somebody is taking actions which puts a life in danger.")]
    public async Task DangerAlertAsync([Description("The name of the person who is producing an imminent danger.")] string agentName)
    {
        await TryPlayTextAsync($"{agentName} IS PRODUCING IMMINENT DANGER!");
        Console.WriteLine($"{agentName} IS PRODUCING IMMINENT DANGER!");
    }

    [KernelFunction, Description("Somebody said the word 'rum'.")]
    public async Task RumAlertAsync([Description("The name of the person who said the word 'rum'.")] string agentName)
    {
        await TryPlayTextAsync($"{agentName} IS TRIGGERING RUM ALERT!");
        Console.WriteLine($"{agentName} IS TRIGGERING RUM ALERT!");
    }

    private async Task TryPlayTextAsync(string message)
    {
        //return;
        OpenAITextToAudioExecutionSettings executionSettings = new()
        {
            Voice = "alloy", // Supported voices are alloy, echo, fable, onyx, nova, and shimmer.
            ResponseFormat = "mp3", // Supported formats are mp3, opus, aac, and flac.
            Speed = 1.0f // Select a value from 0.25 to 4.0. 1.0 is the default.
        };

        var audioContent = await _textToAudioService.GetAudioContentAsync(message, executionSettings);

        if (audioContent?.Data is null)
        {
            return;
        }
        else 
        {
            try
            {
                var speech = new AudioHelper(audioContent.Data!.Value.ToArray());
                speech.Play();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to play audio.");
            }
        }
    }
}
