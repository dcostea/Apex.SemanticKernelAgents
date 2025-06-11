namespace Apex.SemanticKernelAgents.Helpers;

public class PrintHelper
{
    public static void PrintColoredLines(string[] lines)
    {
        foreach (var line in lines)
        {
            PrintColoredLine(line);
        }

        Console.ResetColor();
    }

    public static void PrintColoredLine(string line)
    {
        if (line.Contains("assistant") && line.Contains("[Jack"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{line}");
            Console.ResetColor();
            return;
        }

        if (line.Contains("assistant") && line.Contains("[Quijote"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{line}");
            Console.ResetColor();
            return;
        }

        if (line.Contains("assistant") && line.Contains("[Shakespeare"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{line}");
            Console.ResetColor();
            return;
        }

        if (line.Contains("assistant") && line.Contains("[Yoda"))
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{line}");
            Console.ResetColor();
            return;
        }

        if (line.Contains("assistant") && line.Contains("[Dialog"))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{line}");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{line}");
        Console.ResetColor();
    }

    public static void PrintLines(string[] lines)
    {
        foreach (var line in lines)
        {
            Console.WriteLine($"{line}");
        }
    }
}
