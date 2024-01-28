using Microsoft.SemanticKernel.Experimental.Agents;

namespace Apex.SemanticKernelAgents.Helpers;

public class PrintHelper
{
    public static void PrintMessage(IChatMessage message)
    {
        switch (message.Role)
        {
            case "user":
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[SCRIPT: {message.Content}]");
                Console.ResetColor();
                break;

            case "assistant":
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                PrintLines(lines);
                break;

            default:
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"# Role {message.Role}: {message.Content}");
                Console.ResetColor();
                break;
        }
    }

    public static void PrintLines(string[] lines)
    {
        foreach (var line in lines)
        {
            Console.WriteLine($"{line}");
        }
    }

}
