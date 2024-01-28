using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;

namespace Apex.SemanticKernelAgents.Plugins;

public class AlertsPlugin
{
    [KernelFunction, Description("Somebody is talking about the rum")]
    public void RumAlert()
    {
        Log.Information("RUM ALERT!");
    }

    [KernelFunction, Description("Somebody is talking about the force")]
    public void ForceAlert()
    {
        Log.Information("FORCE ALERT!");
    }

    [KernelFunction, Description("Somebody is talking about coca-cola")]
    public void CocaColaAlert()
    {
        Log.Information("COCA-COLA ALERT!");
    }
}
