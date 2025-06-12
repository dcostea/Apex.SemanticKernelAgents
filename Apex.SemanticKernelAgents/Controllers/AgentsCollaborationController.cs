using Apex.SemanticKernelAgents.Helpers;
using Apex.SemanticKernelAgents.Plugins;
using Apex.SemanticKernelAgents.Strategies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using System.Text;

namespace Apex.SemanticKernelAgents.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentsCollaborationController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILoggerFactory _loggerFactory;

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

    private readonly string InstructionsTemplate = """
        You are {{name}} talking in {{name}} style.
        Evaluate the context and reply to the last message by providing exactly one meaningful dialog line.
        The dialog line must be only one sentence of maximum {{words_number}} words.
        """;

    public AgentsCollaborationController(Kernel kernel)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger));
        _kernel = kernel;
        _kernel.ImportPluginFromType<AlertsPlugin>();
    }

    [HttpPost("/single")]
    public async Task<IActionResult> SingularAgent()
    {
        // create a chat agent for Jack Sparrow using existing kernel (along with its plugins)
        ChatCompletionAgent jackSparrowAgent = new() 
        {
            Id = "JackSparrow_01",
            Name = "JackSparrow",
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Jack Sparrow",
                ["words_count"] = "20"
            },
            Kernel = _kernel,
            LoggerFactory = _loggerFactory
        };

        ChatHistory chat = [];
        chat.AddUserMessage("""
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            """
        );

        AgentThread thread = new ChatHistoryAgentThread();

        await foreach (var content in jackSparrowAgent.InvokeAsync(chat, thread))
        {
            PrintHelper.PrintColoredLine($">>>>>>> {content.Message.Role} [{content.Message.AuthorName}] > {content.Message.Content}");
        }

        return Ok();
    }

    [HttpPost("/strategy/terminator/aggregator")]
    public async Task<IActionResult> AgentWithAggregatorTerminationStrategy()
    {
        ChatCompletionAgent jackSparrowAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Jack Sparrow",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent yodaAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Yoda",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
            LoggerFactory = _loggerFactory
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

        await foreach (var message in chat.InvokeAsync())
        {
            PrintHelper.PrintColoredLine($">>>>>>> {message.Role} [{message.AuthorName}] > {message.Content}");
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response);
    }

    [HttpPost("/strategy/terminator/threshold")]
    public async Task<IActionResult> AgentWithThresholdTerminationStrategy()
    {
        ChatCompletionAgent jackSparrowAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Jack Sparrow",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent yodaAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Yoda",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
            LoggerFactory = _loggerFactory
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
            PrintHelper.PrintColoredLine($">>>>>>> {content.Role} [{content.AuthorName}] > {content.Content}");
        }

        Console.WriteLine($"IS COMPLETE: {chat.IsComplete}\n");

        var response = await GetChatHistory(chat);

        return Ok(response);
    }

    [HttpPost("/strategy/terminator/matching")]
    public async Task<IActionResult> AgentWithMatchingTerminationStrategy()
    {
        ChatCompletionAgent jackSparrowAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Jack Sparrow",
                ["words_count"] = "20"
            },
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent yodaAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Yoda",
                ["words_count"] = "20"
            },
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
            LoggerFactory = _loggerFactory
        };

        var terminationFunction = KernelFunctionFactory.CreateFromPrompt("""
            Determine if the copy has been approved. If so, respond with a single word: yes

            History:
            {{$history}}
            """,
            new OpenAIPromptExecutionSettings { ResponseFormat = "json_object" },
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
            new OpenAIPromptExecutionSettings { ResponseFormat = "json_object" },
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
        //_kernel.ImportPluginFromFunctions("rnd", [randomMathSelectionFunction]);

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
    [HttpPost("/hierachical")]
    public async Task<IActionResult> HierarchicalAgents()
    {
         // create a chat agent for Jack Sparrow using existing kernel (along with its plugins)
        ChatCompletionAgent jackSparrowAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Jack Sparrow",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Name = "JackSparrow",
            Id = "JackSparrow_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent yodaAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Yoda",
                ["words_count"] = "10"
            },
            Description = "A chat bot that replies to the message in the voice of Yoda talking style.",
            Name = "Yoda",
            Id = "Yoda_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent shakespeareAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Shakespeare",
                ["words_count"] = "20"
            },
            Description = "A chat bot that replies to the message in the voice of Shakespeare talking style.",
            Name = "Shakespeare",
            Id = "Shakespeare_01",
            LoggerFactory = _loggerFactory
        };

        ChatCompletionAgent donQuijoteAgent = new()
        {
            Kernel = _kernel,
            Instructions = InstructionsTemplate,
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            {
                ["name"] = "Don Quijote",
                ["words_count"] = "20"
            },
            Description = "A chat bot that replies to the message in the voice of Don Quijote talking style.",
            Name = "DonQuijote",
            Id = "DonQuijote_01",
            LoggerFactory = _loggerFactory
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
            LoggerFactory = _loggerFactory
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
            Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
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
            LoggerFactory = _loggerFactory
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