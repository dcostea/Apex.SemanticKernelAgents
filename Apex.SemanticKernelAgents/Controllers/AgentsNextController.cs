using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Apex.SemanticKernelAgents.Plugins;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Apex.SemanticKernelAgents.Helpers;
using System.Text;
using Apex.SemanticKernelAgents.Strategies;
using Azure.AI.OpenAI;

namespace Apex.SemanticKernelAgents.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentsNextController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _sp;

    private readonly List<string> ScriptLines =
     [
        "Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.",
        "Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.",
        "Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.",
        "Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.",
        "Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.",
        "Yoda hurts Jack Sparrow with an energy blast, resulting in an epic victory.",
        "Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.",
        "Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'",
        "Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.",
    ];

    public AgentsNextController(Kernel kernel, IServiceProvider sp)
    {
        _kernel = kernel;
        _loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger));
        _sp = sp;
    }

    [HttpPost("/next/single")]
    public async Task<IActionResult> SingularAgent()
    {
        // add some plugins to the kernel
        _kernel.Plugins.AddFromType<AlertsPlugin>(serviceProvider: _sp);

        // create a chat agent for Jack Sparrow using existing kernel (along with its plugins)
        ChatCompletionAgent jackSparrowAgent = new() 
        { 
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Jack Sparrow talking in Jack Sparrow style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
        };

        // create a chat group for agents
        AgentGroupChat chat = new() 
        {
            LoggerFactory = _loggerFactory
        };

        var goal = """
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            """;

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, goal));

        // check jackSparrow agent reaction to the chat
        await foreach (var content in chat.InvokeAsync(jackSparrowAgent))
        {
            if (content.Role == AuthorRole.Assistant)
            {
                PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
            }
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response.ToString());
    }

    [HttpPost("/next/strategy/terminator/aggregator")]
    public async Task<IActionResult> AgentWithAggregatorTerminationStrategy()
    {
        _kernel.Plugins.AddFromType<AlertsPlugin>(serviceProvider: _sp);

        ChatCompletionAgent jackSparrowAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
            You are Jack Sparrow talking in Jack Sparrow style.
            Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
            The dialog line must be only one sentence of maximum 10 words.
            """,
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
        };

        ChatCompletionAgent yodaAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Yoda talking in Yoda style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
        };

        AgentGroupChat chat = new(yodaAgent, jackSparrowAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new AggregatorTerminationStrategy(
                    new RegexTerminationStrategy(@"\b([Rr]um)\b")
                    {
                        Agents = [jackSparrowAgent]
                    },
                    new RegexTerminationStrategy(@"\b([Tt]ea)\b")
                    {
                        Agents = [yodaAgent]
                    }
                )
                {
                    Condition = AggregateTerminationCondition.Any,
                    MaximumIterations = 20,
                },
                SelectionStrategy = new SequentialSelectionStrategy
                {
                },
            },
            LoggerFactory = _loggerFactory
        };

        var goal = "Jack Sparrow makes a bad joke about Yoda's taste in drinks.";
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, goal));

        Console.WriteLine($">>>>>>> {AuthorRole.User} > {goal}.");

        await foreach (var content in chat.InvokeAsync())
        {
            if (content.Role == AuthorRole.Assistant) 
            {
                PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
            }
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response);
    }

    [HttpPost("/next/strategy/terminator/threshold")]
    public async Task<IActionResult> AgentWithThresholdTerminationStrategy()
    {
        _kernel.Plugins.AddFromType<AlertsPlugin>(serviceProvider: _sp);

        ChatCompletionAgent jackSparrowAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
            You are Jack Sparrow talking in Jack Sparrow style.
            Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
            The dialog line must be only one sentence of maximum 10 words.
            """,
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
        };

        ChatCompletionAgent yodaAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Yoda talking in Yoda style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
        };

        AgentGroupChat chat = new(jackSparrowAgent, yodaAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new ThresholdTerminationStrategy
                {
                    CompletionThreshold = 80,
                    MaximumIterations = 50,
                    Agents = [jackSparrowAgent, yodaAgent]
                },
                SelectionStrategy = new SequentialSelectionStrategy
                {
                },
            },
            LoggerFactory = _loggerFactory
        };

        var goal = "Jack Sparrow makes a bad joke about Yoda's taste in drinks.";
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, goal));

        Console.WriteLine($">>>>>>> {AuthorRole.User} > {goal}.");

        await foreach (var content in chat.InvokeAsync())
        {
            if (content.Role == AuthorRole.Assistant)
            {
                PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
            }
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response);
    }

    [HttpPost("/next/strategy/terminator/matching")]
    public async Task<IActionResult> AgentWithMatchingTerminationStrategy()
    {
        _kernel.Plugins.AddFromType<AlertsPlugin>(serviceProvider: _sp);

        ChatCompletionAgent jackSparrowAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
            You are Jack Sparrow talking in Jack Sparrow style.
            Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
            The dialog line must be only one sentence of maximum 10 words.
            """,
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
        };

        ChatCompletionAgent yodaAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Yoda talking in Yoda style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
        };

        var terminationFunction = KernelFunctionFactory.CreateFromPrompt("""
            Determine if the copy has been approved. If so, respond with a single word: yes

            History:
            {{$history}}
            """,
            new OpenAIPromptExecutionSettings { ResponseFormat = ChatCompletionsResponseFormat.JsonObject },
            description: "Determines if the copy has been approved.",
            functionName: "semantic_copy_approved",
            loggerFactory: _loggerFactory
            );

        var randomSemanticSelectionFunction = KernelFunctionFactory.CreateFromPrompt("""
            Your job is to determine which participant takes the next turn in a conversation.
            State only the name of the participant to take the next turn.
            
            Choose only from these participants: {{$agents}}

            Always follow these rules when selecting the next participant:
            - After user input, choose a participant randomly.
            - Each participant can be selected once or twice in a row.
            - Never let a participant be selected three times or more in a row!

            History:
            {{$history}}
            """,
            new OpenAIPromptExecutionSettings { ResponseFormat = ChatCompletionsResponseFormat.JsonObject },
            description: "Selects randomly the next participant in a conversation.",
            functionName: "random_semantic_selection",
            loggerFactory: _loggerFactory
            );

        var randomMathSelectionFunction = KernelFunctionFactory.CreateFromMethod(() =>
            {
                Random random = new();
                var agentIndex = random.Next(0, 2);
                return agentIndex == 0 ? jackSparrowAgent.Name : yodaAgent.Name;
            },
            functionName: "random_math_selection",
            description: "Randomly selects 0 or 1.",
            loggerFactory: _loggerFactory
        );
        //_kernel.Plugins.AddFromFunctions("rnd", [randomMathSelectionFunction]);

        AgentGroupChat chat = new(jackSparrowAgent, yodaAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new MatchingTerminationStrategy 
                {
                    MatchingMessage = "ship",
                    MaximumIterations = 50,
                    Agents = [jackSparrowAgent, yodaAgent],
                },
                //TerminationStrategy = new MatchingTerminationStrategy
                //{
                //    MatchingMessage = "approved",
                //    MaximumIterations = 50,
                //    Agents = [jackSparrowAgent, yodaAgent]
                //},
                ////TerminationStrategy = new ThresholdTerminationStrategy
                ////{
                ////    CompletionThreshold = 80,
                ////    MaximumIterations = 50,
                ////    Agents = [jackSparrowAgent, yodaAgent]
                ////},


                //SelectionStrategy = new KernelFunctionSelectionStrategy(randomSemanticSelectionFunction, _kernel)
                SelectionStrategy = new KernelFunctionSelectionStrategy(randomMathSelectionFunction, _kernel)
                {
                    // Returns the entire result value as a string.
                    ResultParser = (result) => result.GetValue<string>() ?? jackSparrowAgent.Name,
                    // The prompt variable name for the agents argument.
                    AgentsVariableName = "agents",
                    // The prompt variable name for the history argument.
                    HistoryVariableName = "history",
                },
            },
            LoggerFactory = _loggerFactory
        };

        var goal = "Jack Sparrow makes a bad joke about Yoda's taste in drinks.";
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, goal));

        Console.WriteLine($">>>>>>> {AuthorRole.User} > {goal}.");

        await foreach (var content in chat.InvokeAsync())
        {
            if (content.Role == AuthorRole.Assistant)
            {
                PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
            }
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response);
    }

    //AggregatorAgent
    [HttpPost("/next/hierachical")]
    public async Task<IActionResult> HierarchicalAgents()
    {
         // create a chat agent for Jack Sparrow using existing kernel (along with its plugins)
        ChatCompletionAgent jackSparrowAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Jack Sparrow talking in Jack Sparrow style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
        };

        ChatCompletionAgent yodaAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Yoda talking in Yoda style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
        };

        ChatCompletionAgent shakespeareAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Shakespeare talking in Shakespeare style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Shakespeare talking style.",
            Name = "Shakespeare",
            Id = "Shakespeare_01",
        };

        ChatCompletionAgent donQuijoteAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are Don Quijote talking in Don Quijote style.
                Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
                The dialog line must be only one sentence of maximum 10 words.
                """,
            Description = "A chat bot that replies to the message in the voice of Don Quijote talking style.",
            Name = "DonQuijote",
            Id = "DonQuijote_01",
        };

        // create a chat group for characters' agents
        AgentGroupChat chat = new(jackSparrowAgent, yodaAgent, shakespeareAgent, donQuijoteAgent)
        {
            LoggerFactory = _loggerFactory
        };

        AggregatorAgent aggregatorAgent = new(() => chat) 
        {
            ////Mode = AggregatorMode.Nested,
            Mode = AggregatorMode.Flat,
            Description = "An agent that aggregates the responses of other agents in a chat.",
            Name = "Aggregator",
            Id = "Aggregator_01",
        };

        AgentGroupChat aggregatedChat = new(aggregatorAgent)
        {
            ExecutionSettings = new()
            {
                SelectionStrategy = new SequentialSelectionStrategy
                {
                },
            },
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent dialogWriterAgent = new()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
            Kernel = _kernel,
            Instructions = """
                You are an expert playwright specialized in drama and comedy. 
                Carefully analyse the messages and use only the applicable tools.
                If no tool is applicable, respond with "..." only.

                Respond in JSON format. The JSON schema can include only:
                [
                  {
                    "assistant": "string (the assistant which responds)",
                    "dialog_line": "string (the response of the assistant)"
                  }
                ]
                """,
            Description = "Determines if a tool can be utilized to achieve a result.",
            Name = "DialogWriter",
            Id = "DialogWriter_01",
        };


        ////chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Don Quijote's new horse is called Rocket."));
        ////await foreach (var content in chat.InvokeAsync(dialogWriterAgent))
        ////{
        ////    if (content.Role == AuthorRole.Assistant)
        ////    {
        ////        PrintHelper.PrintColoredLine($"******* {content.Role} [{content.AuthorName}] > {content.Content}");
        ////    }
        ////}


        foreach (var scriptLine in ScriptLines)
        {
            aggregatedChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, scriptLine));
            PrintHelper.PrintColoredLine($">>>>>>> {AuthorRole.User} > {scriptLine}");
            await foreach (var content in aggregatedChat.InvokeAsync(dialogWriterAgent))
            {
                if (content.Role == AuthorRole.Assistant)
                {
                    PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
                }
            }
        }

        //Console.WriteLine("-------- chat ---------");

        //var response0 = await GetChatHistory(chat);
        //Console.WriteLine(response0);

        //Console.WriteLine("--------- agg chat --------");

        var response = await GetChatHistory(aggregatedChat);
        Console.WriteLine(response);

        //Console.WriteLine("--------- done --------");

        return Ok(response.ToString());
    }

    //////////////////////////// ComplexChat_NestedShopper

    private static async Task<string> GetChatHistory(AgentGroupChat chat)
    {
        var response = new StringBuilder();
        await foreach (var chatMessage in chat.GetChatMessagesAsync())
        {
            response.AppendLine($"[{chatMessage.Role}] > {chatMessage.Content}");
        }

        return response.ToString();
    }
}