using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using Apex.SemanticKernelAgents.Helpers;
using System.Text;

namespace Apex.SemanticKernelAgents.Controllers;

[ApiController]
[Route("[controller]")]
public class OpenAIAgentsController : ControllerBase
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

    public OpenAIAgentsController(Kernel kernel, IServiceProvider sp)
    {
        _kernel = kernel;
        _loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger));
    }

    [HttpPost("/openai/code_interpreter")]
    public async Task<IActionResult> CodeInterpreterAgent()
    {
        var jackSparrowAgent = new ChatCompletionAgent
        {
            Name = "JackSparrow",
            Description = "A chat bot that replies to the message in the voice of Jack Sparrow talking style.",
            Instructions = """
                    You are Jack Sparrow talking in Jack Sparrow style.
                    Evaluate the context and reply providing exactly one meaningful dialog line.
                    The dialog line must be only one sentence of maximum 50 words.
                    """,
            Kernel = _kernel,
            
            //Id = "JackSparrow_01",
            //EnableCodeInterpreter = true
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