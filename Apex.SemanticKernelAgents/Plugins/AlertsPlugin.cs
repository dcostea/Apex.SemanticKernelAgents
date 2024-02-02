using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;

namespace Apex.SemanticKernelAgents.Plugins;

public class AlertsPlugin
{
    [KernelFunction, Description("Somebody is saying the word 'rum'")]
    public void RumAlert()
    {
        Log.Information("RUM ALERT!");
    }

    [KernelFunction, Description("Somebody is saying the word 'force'")]
    public void ForceAlert()
    {
        Log.Information("FORCE ALERT!");
    }

    [KernelFunction, Description("Somebody is saying the word 'coca-cola'")]
    public void CocaColaAlert()
    {
        Log.Information("COCA-COLA ALERT!");
    }
}
