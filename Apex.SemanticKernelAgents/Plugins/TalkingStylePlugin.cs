using Microsoft.SemanticKernel;
using Serilog;
using System.ComponentModel;

namespace Apex.SemanticKernelAgents.Plugins;

public sealed class TalkingStylePlugin
{
    [KernelFunction, Description("Provides a pirate talking style.")]
    public string GetPirateTalkingStyle()
    {
        Log.Debug("GetPirateTalkingStyle was called in TalkingStylePlugin.");
        return "Pirate talking style.";
    }

    ////[KernelFunction, Description("Provides Yoda talking style.")]
    ////public string GetYodaTalkingStyle()
    ////{
    ////    Log.Debug("GetYodaTalkingStyle was called in TalkingStylePlugin.");
    ////    return "Yoda talking style.";
    ////}
}
