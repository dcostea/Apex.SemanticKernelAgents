using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Serilog;
using Microsoft.SemanticKernel.Experimental.Agents;
using Apex.SemanticKernelAgents.Plugins;
using Microsoft.SemanticKernel.Experimental.Agents.Exceptions;
using Apex.SemanticKernelAgents.Helpers;

namespace Apex.SemanticKernelAgents.Controllers;

[ApiController]
[Route("[controller]")]
public class PromptsController : ControllerBase
{
    [HttpGet("dialog/hierarchical")]
    public async Task<IActionResult> GetHierarchicalDialog()
    {
        List<IAgent> agents = [];
        IAgentThread? thread = null;

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>();

        var jackAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.JackSparrowDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var yodaAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.YodaDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var shakespeareAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.ShakespeareDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var quijoteAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.DonQuijoteDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var dialogWriterAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/HierarchicalAgents.DialogWriterAgent.yaml")
            .WithPlugin(jackAgent.AsPlugin())
            .WithPlugin(yodaAgent.AsPlugin())
            .WithPlugin(shakespeareAgent.AsPlugin())
            .WithPlugin(quijoteAgent.AsPlugin())
            .BuildAsync());

        List<string> scriptSteps =
        [
            "Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.",
            "Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.",
            "Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.",
            "Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.",
            "Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.",
            "Yoda hits Jack Sparrow with an energy blast, resulting in an epic victory.",
            "Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.",
            "Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'",
            "Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.",
        ];

        var result = new List<string>();

        try
        {
            thread = await dialogWriterAgent.NewThreadAsync();
            Log.Information($"DIALOG START (hierarchical agents, thread id: {thread.Id})");
            Log.Information("****************************************");

            foreach (var messages in scriptSteps.Select(m => thread!.InvokeAsync(dialogWriterAgent, m)))
            {
                await foreach (var message in messages)
                {
                    PrintHelper.PrintMessage(message!);
                    result.Add($"{message.Role} > {message.Content}");
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
            await Task.WhenAll(
                thread?.DeleteAsync() ?? Task.CompletedTask,
                Task.WhenAll(agents.Select(a => a.DeleteAsync())));

            Log.Information("DIALOG IS COMPLETE (hierarchical agents)");
            Log.Information("*******************************");
        }

        return Ok(result);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }

    [HttpGet("dialog/as_plugins")]
    public async Task<IActionResult> GetAgentsAsPluginsDialog()
    {
        List<IAgent> s_agents = [];

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>();

        try
        {
            var jackAgent = Track(await new AgentBuilder()
                .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.JackSparrowDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var yodaAgent = Track(await new AgentBuilder()
                .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.YodaDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var shakespeareAgent = Track(await new AgentBuilder()
                .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.ShakespeareDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var quijoteAgent = Track(await new AgentBuilder()
                .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
                .FromTemplatePath(@"Agents/AsPluginsAgents.DonQuijoteDialogAgent.yaml")
                .WithPlugin(newsPlugin)
                .BuildAsync());

            var dialogWriterAgent = Track(await new AgentBuilder()
                .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
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
            Yoda hits Jack Sparrow with an energy blast, resulting in an epic victory.
            Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.
            Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'
            Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.
            """
            ;

            Log.Information("DIALOG START (agents as plugins)");
            Log.Information("******************************");

            await foreach (IChatMessage message in dialogWriterAgent.InvokeAsync(goal))
            {
                var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{message.Role} [{message.Id}] [{message.AgentId}] >");
                Console.ResetColor();
                PrintHelper.PrintColoredLines(lines);
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
            await Task.WhenAll(s_agents.Select(a => a.DeleteAsync()));

            Log.Information("DIALOG IS COMPLETE (agents as plugins)");
            Log.Information("************************************");
        }

        return Ok();

        IAgent Track(IAgent agent)
        {
            s_agents.Add(agent);

            return agent;
        }
    }

    [HttpGet("dialog/joint")]
    public async Task<IActionResult> GetJointDialog()
    {
        List<IAgent> agents = [];
        IAgentThread? thread = null;

        var newsPlugin = KernelPluginFactory.CreateFromType<AlertsPlugin>();

        var jackAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.JackSparrowDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var yodaAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.YodaDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var shakespeareAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.ShakespeareDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var quijoteAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.DonQuijoteDialogAgent.yaml")
            .WithPlugin(newsPlugin)
            .BuildAsync());

        var narratorAgent = Track(await new AgentBuilder()
            .WithOpenAIChatCompletion(Env.Var("OpenAI:ModelId")!, Env.Var("OpenAI:ApiKey")!)
            .FromTemplatePath(@"Agents/JointAgents.NarratorAgent.yaml")
            .BuildAsync());

        List<string> scriptSteps =
        [
            "Jack Sparrow, Shakespeare, Don Quijote, and Yoda are having a feast. Don Quijote likes coca-cola! All making remarks about their favorite drinks.",
            "Jack Sparrow makes a bad joke about Don Quijote's taste in drinks.",
            "Jack Sparrow gets a threat from Don Quijote and Don Quijote is launching a fake attack.",
            "Shakespeare sends strong words to Jack Sparrow but Jack Sparrow answers bravely and provocatively.",
            "Yoda warns Jack Sparrow, but Jack Sparrow slaps Don Quijote.",
            "Yoda hits Jack Sparrow with an energy blast, resulting in an epic victory.",
            "Jack Sparrow dies and Don Quijote falls down to his knees weeping for Jack Sparrow.",
            "Shakespeare, Yoda or Don Quijote responds with 'VICTORY!'",
            "Yoda tells an epitaph for Jack Sparrow, Don Quijote says nothing.",
        ];

        var result = new List<string>();

        try
        {
            thread = await narratorAgent.NewThreadAsync();
            Log.Information($"DIALOG START (joint agents, thread id: {thread.Id})");
            Log.Information("****************************************");

            foreach (var scriptStep in scriptSteps)
            {
                var messageUser = await thread.AddUserMessageAsync(scriptStep);
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
                    result.Add($"{message.Role} > Jack Sparrow > {message.Content}");
                }

                var yodaAgentMessages = await thread.InvokeAsync(yodaAgent).ToArrayAsync();
                foreach (var message in yodaAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("    Yoda: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                    result.Add($"{message.Role} > Yoda > {message.Content}");
                }

                var shakespeareAgentMessages = await thread.InvokeAsync(shakespeareAgent).ToArrayAsync();
                foreach (var message in shakespeareAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("    Shakespeare: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                    result.Add($"{message.Role} > Shakespeare > {message.Content}");
                }

                var quijoteAgentMessages = await thread.InvokeAsync(quijoteAgent).ToArrayAsync();
                foreach (var message in quijoteAgentMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("    Don Quijote: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                    result.Add($"{message.Role} > Don Quijote > {message.Content}");
                }

                var runnerMessages = await thread.InvokeAsync(narratorAgent).ToArrayAsync();
                foreach (var message in runnerMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("    Narrator: ");
                    var lines = message.Content.Split('\n', '.', StringSplitOptions.RemoveEmptyEntries);
                    PrintHelper.PrintLines(lines);
                    Console.ResetColor();
                    result.Add($"{message.Role} > Narrator > {message.Content}");
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
            await Task.WhenAll(
                thread?.DeleteAsync() ?? Task.CompletedTask,
                Task.WhenAll(agents.Select(a => a.DeleteAsync())));

            Log.Information("DIALOG IS COMPLETE (joint agents)");
            Log.Information("************************");

        }

        return Ok(result);

        IAgent Track(IAgent agent)
        {
            agents.Add(agent);

            return agent;
        }
    }
}
