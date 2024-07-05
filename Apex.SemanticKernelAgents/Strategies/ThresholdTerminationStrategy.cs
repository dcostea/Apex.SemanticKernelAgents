using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;

namespace Apex.SemanticKernelAgents.Strategies;

public sealed class ThresholdTerminationStrategy : TerminationStrategy
{
    public int CompletionThreshold { get; set; } = 70;

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        string lastMessageContent = history[history.Count - 1].Content ?? string.Empty;

        var regex = new Regex(@"\d+");
        var match = regex.Match(lastMessageContent);
 
        int result = match.Success 
            ? int.Parse(match.Value)
            : 0;
        
        return Task.FromResult(result >= CompletionThreshold);
    }
}
