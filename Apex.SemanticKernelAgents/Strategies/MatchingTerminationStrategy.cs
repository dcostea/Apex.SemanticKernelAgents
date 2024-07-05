using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;

namespace Apex.SemanticKernelAgents.Strategies;

public sealed class MatchingTerminationStrategy : TerminationStrategy
{
    public string MatchingMessage { get; set; } = "match";

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        string lastMessageContent = history[history.Count - 1].Content ?? string.Empty;

        var shouldTerminate = Regex.IsMatch(lastMessageContent, Regex.Escape(MatchingMessage), RegexOptions.IgnoreCase);

        return Task.FromResult(shouldTerminate);
    }
}
