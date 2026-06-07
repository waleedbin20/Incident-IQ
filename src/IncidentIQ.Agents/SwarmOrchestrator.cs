using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using IncidentIQ.McpServers;

namespace IncidentIQ.Agents;

public interface ITraceBroadcaster
{
    Task BroadcastTraceAsync(string level, string action, string message);
}

public class SwarmOrchestrator
{
    private readonly ITraceBroadcaster _broadcaster;
    private readonly GitHubMcpTool _githubTool;
    private readonly FabricIQMcpTool _fabricTool;
    private readonly AppInsightsMcpTool _appInsightsTool;
    private readonly IChatClient _chatClient;
    
    private readonly string _commanderPrompt;
    private readonly string _infraPrompt;
    private readonly string _devopsPrompt;
    private readonly string _supportPrompt;

    public SwarmOrchestrator(
        ITraceBroadcaster broadcaster, 
        GitHubMcpTool githubTool, 
        FabricIQMcpTool fabricTool,
        AppInsightsMcpTool appInsightsTool,
        IChatClient chatClient)
    {
        _broadcaster = broadcaster;
        _githubTool = githubTool;
        _fabricTool = fabricTool;
        _appInsightsTool = appInsightsTool;
        _chatClient = chatClient;
        
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Prompts");
        _commanderPrompt = File.Exists(Path.Combine(baseDir, "CommanderPrompt.txt")) ? File.ReadAllText(Path.Combine(baseDir, "CommanderPrompt.txt")) : "You are an AI Commander. Output JSON.";
        _infraPrompt = File.Exists(Path.Combine(baseDir, "InfraAgentPrompt.txt")) ? File.ReadAllText(Path.Combine(baseDir, "InfraAgentPrompt.txt")) : "You are Infra Agent.";
        _devopsPrompt = File.Exists(Path.Combine(baseDir, "DevOpsAgentPrompt.txt")) ? File.ReadAllText(Path.Combine(baseDir, "DevOpsAgentPrompt.txt")) : "You are DevOps Agent.";
        _supportPrompt = File.Exists(Path.Combine(baseDir, "SupportAgentPrompt.txt")) ? File.ReadAllText(Path.Combine(baseDir, "SupportAgentPrompt.txt")) : "You are Support Agent.";
    }

    public async Task<string> InvestigateAsync(string incidentDescription = "Users are complaining about checkout timeouts.", string repository = "microsoft/TypeScript")
    {
        await _broadcaster.BroadcastTraceAsync("INFO", "ORCHESTRATOR", "Initializing Azure Foundry Live Group Chat...");

        // Setup dynamic repository for DevOps agent
        _githubTool.SetRepository(repository);

        // Support Agent no longer uses Jira, it just analyzes the initial user description
        var supportTools = new List<AITool>();
        var supportAgent = new ChatClientAgent(_chatClient, _supportPrompt, "Support-Agent", "Parses user reports directly from the orchestrator", supportTools);

        var infraTools = new List<AITool> { 
            AIFunctionFactory.Create(_fabricTool.ExecuteKqlQueryAsync),
            AIFunctionFactory.Create(_appInsightsTool.QueryAppInsightsAsync)
        };
        var infraAgent = new ChatClientAgent(_chatClient, _infraPrompt, "Infra-Agent", "Monitors server states", infraTools);

        var devopsTools = new List<AITool> { AIFunctionFactory.Create(_githubTool.GetRecentMergedPRsAsync) };
        var devopsAgent = new ChatClientAgent(_chatClient, _devopsPrompt + $"\nThe current repository under investigation is: {repository}", "DevOps-Agent", "Tracks Git merges", devopsTools);

        var commanderTools = new List<AITool>();
        var commanderAgent = new ChatClientAgent(_chatClient, _commanderPrompt, "Commander", "Coordinates agents and outputs final JSON", commanderTools);

        // Build the workflow
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 30
                })
            .AddParticipants(supportAgent, infraAgent, devopsAgent, commanderAgent)
            .Build();

        var messages = new List<ChatMessage> {
            new(ChatRole.User, $"A P1 incident has been reported. {incidentDescription} Find the root cause and generate the JSON graph output.")
        };

        var finalJson = string.Empty;
        var commanderAccumulator = new System.Text.StringBuilder();

        await _broadcaster.BroadcastTraceAsync("INFO", "ORCHESTRATOR", "Starting Group Chat inference loop...");

        try
        {
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                if (evt is AgentResponseUpdateEvent update)
                {
                    // In Agents SDK, AgentResponseUpdateEvent can be deltas or full messages.
                    // We will stream to UI.
                    AgentResponse response = update.AsResponse();
                    foreach (ChatMessage message in response.Messages)
                    {
                        if(!string.IsNullOrEmpty(message.Text))
                        {
                            await _broadcaster.BroadcastTraceAsync("INFO", update.ExecutorId, message.Text);
                            
                            if (update.ExecutorId.StartsWith("Commander", StringComparison.OrdinalIgnoreCase))
                            {
                                commanderAccumulator.Append(message.Text);
                            }
                        }
                        
                        // Check for tool calls
                        var toolCalls = message.Contents.OfType<FunctionCallContent>();
                        foreach (var toolCall in toolCalls)
                        {
                            var args = string.Join(", ", toolCall.Arguments?.Select(a => $"{a.Key}: {a.Value}") ?? Array.Empty<string>());
                            await _broadcaster.BroadcastTraceAsync("EXECUTE", update.ExecutorId, $"🔧 Calling Tool: {toolCall.Name}({args})");
                        }
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    await _broadcaster.BroadcastTraceAsync("INFO", "ORCHESTRATOR", "Workflow completed.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            await _broadcaster.BroadcastTraceAsync("ERROR", "ORCHESTRATOR", $"LLM Execution Failed: {ex.Message}");
            throw new Exception($"LLM Group Chat execution failed: {ex.Message}", ex);
        }

        // Now extract JSON from either the messages history OR the accumulator
        string commanderFullText = commanderAccumulator.ToString();
        
        // If the update was full messages (not deltas), commanderAccumulator will contain duplicates.
        // It's safer to just check the final messages list updated by the workflow.
        var commanderMessages = messages.Where(m => (m.AuthorName ?? "").StartsWith("Commander", StringComparison.OrdinalIgnoreCase) || commanderFullText.Contains("incident_id")).Select(m => m.Text).ToList();
        
        // Search through commanderMessages or the accumulator for the JSON block
        var textToSearch = string.Join("\n", commanderMessages) + "\n" + commanderFullText;
        
        // Extract the JSON block specifically looking for the last occurrence of "incident_id"
        int lastIncidentIdIdx = textToSearch.LastIndexOf("\"incident_id\"");
        if (lastIncidentIdIdx != -1)
        {
            int start = textToSearch.LastIndexOf('{', lastIncidentIdIdx);
            if (start != -1)
            {
                // We need to find the matching closing bracket for this specific object.
                // Simple heuristic: find the last '}' in the string, or count brackets.
                // Since the JSON is usually at the end of the text, finding the LastIndexOf('}') is usually safe
                // UNLESS there's conversational text after. Let's write a simple bracket matcher.
                int openBrackets = 0;
                int end = -1;
                for (int i = start; i < textToSearch.Length; i++)
                {
                    if (textToSearch[i] == '{') openBrackets++;
                    else if (textToSearch[i] == '}')
                    {
                        openBrackets--;
                        if (openBrackets == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                }

                if (end != -1 && end > start)
                {
                    finalJson = textToSearch.Substring(start, end - start + 1);
                }
            }
        }

        if(string.IsNullOrEmpty(finalJson)) 
        {
            throw new Exception("Commander failed to generate the required JSON output. Final text: " + textToSearch);
        }

        // Clean up markdown code blocks if the model wrapped it
        finalJson = finalJson.Trim();
        if(finalJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) 
        {
            finalJson = finalJson.Substring(7);
        }
        else if (finalJson.StartsWith("```"))
        {
            finalJson = finalJson.Substring(3);
        }

        if(finalJson.EndsWith("```")) 
        {
            finalJson = finalJson.Substring(0, finalJson.Length - 3);
        }
        
        finalJson = finalJson.Trim();

        // Validate JSON parsing to prevent UI crashes
        try
        {
            System.Text.Json.JsonDocument.Parse(finalJson);
        }
        catch
        {
            throw new Exception("Commander output was not valid JSON: " + finalJson);
        }

        return finalJson;
    }
}
