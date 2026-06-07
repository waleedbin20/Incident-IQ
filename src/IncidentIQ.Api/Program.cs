using System;
using Azure;
using Azure.AI.Projects;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using IncidentIQ.Agents;
using IncidentIQ.Api.Hubs;
using IncidentIQ.Api.Services;
using IncidentIQ.McpServers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Configure DI
builder.Services.AddSingleton<ITraceBroadcaster, SignalRTraceBroadcaster>();

// Read configuration
var githubPat = builder.Configuration["GitHub:PersonalAccessToken"] ?? "";
builder.Services.AddSingleton(new GitHubMcpTool(githubPat));

var fabricWorkspaceId = builder.Configuration["FabricIQ:WorkspaceId"] ?? "";
var fabricToken = builder.Configuration["FabricIQ:AccessToken"] ?? "";
builder.Services.AddSingleton(new FabricIQMcpTool(fabricWorkspaceId, fabricToken));

var appInsightsId = builder.Configuration["AppInsights:ApplicationId"] ?? "";
var appInsightsKey = builder.Configuration["AppInsights:ApiKey"] ?? "";
builder.Services.AddSingleton(new AppInsightsMcpTool(appInsightsId, appInsightsKey));

var endpoint = builder.Configuration["AzureAI:Endpoint"];
var key = builder.Configuration["AzureAI:ApiKey"];
var deploymentName = builder.Configuration["AzureAI:DeploymentName"] ?? "gpt-4o";

builder.Services.AddSingleton<IChatClient>(sp => 
{
    if(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key)) 
    {
        return new AzureOpenAIClient(new Uri("https://mock.cognitiveservices.azure.com"), new AzureKeyCredential("mock"))
            .GetChatClient(deploymentName).AsIChatClient();
    }
    
    // Sanitize endpoint if user pasted a full completions URL instead of the base Azure endpoint
    if (endpoint.Contains("/openai/"))
    {
        endpoint = endpoint.Substring(0, endpoint.IndexOf("/openai/"));
    }
    
    return new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
        .GetChatClient(deploymentName).AsIChatClient();
});

builder.Services.AddSingleton<SwarmOrchestrator>();

var app = builder.Build();

app.UseCors();
app.UseRouting();

app.MapControllers();
app.MapHub<WarRoomHub>("/warroom");

app.Run();
