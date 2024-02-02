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
                PrintColoredLines(lines);
                break;

            default:
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"# Role {message.Role}: {message.Content}");
                Console.ResetColor();
                break;
        }
    }

    public static void PrintColoredLines(string[] lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains("assistant") && line.Contains("Sparrow"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{line}");
                Console.ResetColor();
                continue;
            }

            if (line.Contains("assistant") && line.Contains("Quixote"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{line}");
                Console.ResetColor();
                continue;
            }

            if (line.Contains("assistant") && line.Contains("Shakespeare"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{line}");
                Console.ResetColor();
                continue;
            }

            if (line.Contains("assistant") && line.Contains("Yoda"))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{line}");
                Console.ResetColor();
                continue;
            }

            Console.WriteLine($"{line}");
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
