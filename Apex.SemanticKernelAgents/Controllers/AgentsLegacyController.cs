using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Serilog;
using Microsoft.SemanticKernel.Experimental.Agents;
using Microsoft.SemanticKernel.Experimental.Agents.Exceptions;
using Apex.SemanticKernelAgents.Plugins;
using Apex.SemanticKernelAgents.Helpers;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Threading;

namespace Apex.SemanticKernelAgents.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentsLegacyController : ControllerBase
{
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

    public AgentsLegacyController(IServiceProvider sp)
    {
        _sp = sp;
    }

    [HttpPost("/legacy/hierarchical")]
    public async Task<IActionResult> GetHierarchicalDialog(List<string>? scriptLines)
    {
        List<IAgent> agents = [];
        IAgentThread? thread = null;
        IReadOnlyList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        var jackAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.JackSparrowDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var yodaAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.YodaDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var shakespeareAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.ShakespeareDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var quijoteAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.DonQuijoteDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var dialogWriterAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.DialogWriterAgent.yaml")
            .WithPlugin(jackAgent.AsPlugin())
            .WithPlugin(yodaAgent.AsPlugin())
            .WithPlugin(shakespeareAgent.AsPlugin())
            .WithPlugin(quijoteAgent.AsPlugin())
            .BuildAsync());

        scriptLines ??= ScriptLines;
        scriptLines = scriptLines!.First().Equals("string")
            ? ScriptLines
            : scriptLines;

        try
        {
            thread = await dialogWriterAgent.NewThreadAsync();
            Log.Information($"DIALOG START (hierarchical agents, thread id: {thread.Id})");
            Log.Information("****************************************");

            foreach (var messages in scriptLines.Select(m => thread!.InvokeAsync(dialogWriterAgent, m)))
            {
                await foreach (var message in messages)
                {
                    PrintHelper.PrintMessage(message!);
                }
            }

            string? scriptLine;
            do
            {
                Console.WriteLine("Enter a script line (or just hit Enter to quit):");
                scriptLine = Console.ReadLine();
                if (scriptLine is not null)
                {
                    var messages = thread!.InvokeAsync(dialogWriterAgent, scriptLine!);

                    await foreach (var message in messages)
                    {
                        PrintHelper.PrintMessage(message!);
                    }
                }
            } 
            while (!string.IsNullOrEmpty(scriptLine));
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (HttpOperationException ex)
        {
            Log.Error("Exception: {message}." , ex.ResponseContent);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException} {responseContent}", ex.Message, ex.InnerException?.Message, ex.Message);
        }
        finally
        {
            history = await thread!.GetMessagesAsync();

            await Task.WhenAll(
                thread?.DeleteAsync() ?? Task.CompletedTask,
                Task.WhenAll(agents.Select(a => a.DeleteAsync())));

            Log.Information("DIALOG IS COMPLETE (hierarchical agents)");
            Log.Information("*******************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/single")]
    public async Task<IActionResult> GetSingleAgent()
    {
        List<IAgent> agents = [];
        IList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        try
        {
            var jackAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.JackSparrowDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var goal = """
            [Verbal actions]
            Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.
            Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.
            Yoda hurts Jack Sparrow with an energy blast, resulting in an epic victory.
            Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.
            Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'
            Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.
            """
            ;

            Log.Information("DIALOG START (single agent)");
            Log.Information("******************************");

            await foreach (IChatMessage message in jackAgent.InvokeAsync(goal))
            {
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{message.Role} [{message.Id}] [{message.AgentId}] >");
                Console.ResetColor();
                PrintHelper.PrintColoredLines(lines);
                history.Add(message);
            }
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            await Task.WhenAll(agents.Select(a => a.DeleteAsync()));

            Log.Information("DIALOG IS COMPLETE (single agent)");
            Log.Information("************************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/single/thread")]
    public async Task<IActionResult> GetSingleAgentWithThread()
    {
        List<IAgent> agents = [];
        IList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        try
        {
            var jackAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.JackSparrowDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var goal = """
            [Verbal actions]
            Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.
            Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.
            Yoda hurts Jack Sparrow with an energy blast, resulting in an epic victory.
            Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.
            Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'
            Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.
            """
            ;

            Log.Information("DIALOG START (single agent)");
            Log.Information("******************************");

            var thread = await jackAgent.NewThreadAsync();

            //await foreach (var message in jackAgent.InvokeAsync(goal))
            //await foreach (var message in thread.InvokeAsync(jackAgent))
            await foreach (var message in thread.InvokeAsync(jackAgent, goal))
            {
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{message.Role} [{message.Id}] [{message.AgentId}] >");
                Console.ResetColor();
                PrintHelper.PrintColoredLines(lines);
                history.Add(message);
            }
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            await Task.WhenAll(agents.Select(a => a.DeleteAsync()));

            Log.Information("DIALOG IS COMPLETE (single agent)");
            Log.Information("************************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/plugin")]
    public async Task<IActionResult> GetSingleAgentWithAgentAsPluginDialog()
    {
        List<IAgent> agents = [];
        IList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        try
        {
            var jackAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.JackSparrowDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var dialogWriterAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.DialogWriterAgent.yaml")
                .WithPlugin(jackAgent.AsPlugin())
                .BuildAsync());

            var goal = """
            [Verbal actions]
            Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.
            Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.
            Yoda hurts Jack Sparrow with an energy blast, resulting in an epic victory.
            Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.
            Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'
            Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.
            """;

            Log.Information("DIALOG START (single agent with one agent as plugin)");
            Log.Information("******************************");

            await foreach (IChatMessage message in dialogWriterAgent.InvokeAsync(goal))
            {
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{message.Role} [{message.Id}] [{message.AgentId}] >");
                Console.ResetColor();
                PrintHelper.PrintColoredLines(lines);
                history.Add(message);
            }
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            await Task.WhenAll(agents.Select(a => a.DeleteAsync()));

            Log.Information("DIALOG IS COMPLETE (single agent with one agent as plugin)");
            Log.Information("************************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/plugins")]
    public async Task<IActionResult> GetSingleAgentWithAgentsAsPluginsDialog()
    {
        List<IAgent> agents = [];
        IList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        try
        {
            var jackAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.JackSparrowDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var yodaAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.YodaDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var shakespeareAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.ShakespeareDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var quijoteAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.DonQuijoteDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var dialogWriterAgent = Track(await new AgentBuilder()
                //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.DialogWriterAgent.yaml")
                .WithPlugin(jackAgent.AsPlugin())
                .WithPlugin(yodaAgent.AsPlugin())
                .WithPlugin(shakespeareAgent.AsPlugin())
                .WithPlugin(quijoteAgent.AsPlugin())
                .BuildAsync());

            var goal = """
            [Verbal actions]
            Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.
            Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.
            Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.
            Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.
            Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.
            Yoda hurts Jack Sparrow with an energy blast, resulting in an epic victory.
            Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.
            Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'
            Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.
            """;

            Log.Information("DIALOG START (agents as plugins)");
            Log.Information("******************************");

            await foreach (IChatMessage message in dialogWriterAgent.InvokeAsync(goal))
            {
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{message.Role} [{message.Id}] [{message.AgentId}] >");
                Console.ResetColor();
                PrintHelper.PrintColoredLines(lines);
                history.Add(message);
            }
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            await Task.WhenAll(agents.Select(a => a.DeleteAsync()));

            Log.Information("DIALOG IS COMPLETE (agents as plugins)");
            Log.Information("************************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/joint/for")]
    public async Task<IActionResult> GetJointDialogWithForLoop(List<string>? scriptLines)
    {
        List<IAgent> agents = [];
        IAgentThread? thread = null;
        IReadOnlyList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        var jackAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.JackSparrowDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var yodaAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.YodaDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var shakespeareAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.ShakespeareDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var quijoteAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.DonQuijoteDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var narratorAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.NarratorAgent.yaml")
            .BuildAsync());

        scriptLines ??= ScriptLines;
        scriptLines = scriptLines!.First().Equals("string")
            ? ScriptLines
            : scriptLines;

        try
        {
            thread = await narratorAgent.NewThreadAsync();
            Log.Information($"DIALOG START (joint agents - for, thread id: {thread.Id})");
            Log.Information("****************************************");

            foreach (var scriptLine in scriptLines)
            {
                var messageUser = await thread.AddUserMessageAsync(scriptLine);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[SCRIPT: {messageUser.Content}]");
                Console.ResetColor();

                var jackAgentMessages = await thread.InvokeAsync(jackAgent).ToArrayAsync();
                foreach (var message in jackAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("    Jack Sparrow: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var yodaAgentMessages = await thread.InvokeAsync(yodaAgent).ToArrayAsync();
                foreach (var message in yodaAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("    Yoda: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var shakespeareAgentMessages = await thread.InvokeAsync(shakespeareAgent).ToArrayAsync();
                foreach (var message in shakespeareAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("    Shakespeare: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var quijoteAgentMessages = await thread.InvokeAsync(quijoteAgent).ToArrayAsync();
                foreach (var message in quijoteAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("    Don Quijote: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var narratorMessages = await thread.InvokeAsync(narratorAgent).ToArrayAsync();
                foreach (var message in narratorMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("    Narrator: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }
            }
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            history = await thread!.GetMessagesAsync();

            await Task.WhenAll(
                thread?.DeleteAsync() ?? Task.CompletedTask,
                Task.WhenAll(agents.Select(a => a.DeleteAsync())));

            Log.Information("DIALOG IS COMPLETE (joint agents - for)");
            Log.Information("************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpPost("/legacy/joint/dowhile")]
    public async Task<IActionResult> GetJointDialogWithDoWhileLoop(string? initialMessage = null)
    {
        List<IAgent> agents = [];
        IAgentThread? thread = null;
        IReadOnlyList<IChatMessage> history = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>(serviceProvider: _sp);

        var jackAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.JackSparrowDialogAgent.yaml")
            .BuildAsync());

        var yodaAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.YodaDialogAgent.yaml")
            .BuildAsync());

        var shakespeareAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.ShakespeareDialogAgent.yaml")
            .BuildAsync());

        var quijoteAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.DonQuijoteDialogAgent.yaml")
            .BuildAsync());

        var moderatorAgent = Track(await new AgentBuilder()
            //.WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .WithAzureOpenAIChatCompletion(Env.Var("AzureOpenAI:Endpoint")!, Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!, Env.Var("AzureOpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.ModeratorAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        try
        {
            thread = await moderatorAgent.NewThreadAsync();
            Log.Information($"DIALOG START (joint agents - do while, thread id: {thread.Id})");
            Log.Information("****************************************");

            initialMessage = string.IsNullOrWhiteSpace(initialMessage)
                ? """
                Jack Sparrow, Shakespeare, Don Quijote, and Yoda are starting a debate about the best drink in the world.
                Each of them is trying to convince the others for the prefered drink.
                Soon some of them are getting convinced and they are starting to support the new drink.
                """
                : initialMessage;

            var messageUser = await thread.AddUserMessageAsync(initialMessage);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[SCRIPT: {messageUser.Content}]");
            Console.ResetColor();
            var shouldContinue = true;
            do
            {
                var jackAgentMessages = await thread.InvokeAsync(jackAgent).ToArrayAsync();
                foreach (var message in jackAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("    Jack Sparrow: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var yodaAgentMessages = await thread.InvokeAsync(yodaAgent).ToArrayAsync();
                foreach (var message in yodaAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("    Yoda: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var shakespeareAgentMessages = await thread.InvokeAsync(shakespeareAgent).ToArrayAsync();
                foreach (var message in shakespeareAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("    Shakespeare: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var quijoteAgentMessages = await thread.InvokeAsync(quijoteAgent).ToArrayAsync();
                foreach (var message in quijoteAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("    Don Quijote: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                }

                var moderatorMessages = await thread.InvokeAsync(moderatorAgent).ToArrayAsync();
                foreach (var message in moderatorMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("    Moderator: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                    if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.Contains("CHEERS", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldContinue = false;
                    }
                }
            }
            while (shouldContinue);
        }
        catch (AgentException ex)
        {
            Log.Error("Agent Exception: {message}.", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {message}. InnerException: {innerException}", ex.Message, ex.InnerException?.Message);
        }
        finally
        {
            history = await thread!.GetMessagesAsync();

            await Task.WhenAll(
                thread?.DeleteAsync() ?? Task.CompletedTask,
                Task.WhenAll(agents.Select(a => a.DeleteAsync())));

            Log.Information("DIALOG IS COMPLETE (joint agents - do while)");
            Log.Information("************************");
        }

        return Ok(history);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }
}
