using MafStatefulApi.Api.Agents;
using MafStatefulApi.Api.Endpoints;
using MafStatefulApi.Api.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI support
builder.Services.AddOpenApi();

// Configure Redis distributed cache for session persistence
builder.AddRedisDistributedCache("cache");
builder.Services.AddSingleton<IAgentSessionStore, RedisAgentSessionStore>();
builder.Services.AddLogging(logging => logging.AddConsole());
Console.WriteLine("Using Redis for session storage");

// Configure Microsoft Agent Framework with Ollama
builder.AddOllamaApiClient("chat-model").AddChatClient();
Console.WriteLine("Using Ollama for AI model");

builder.AddAIAgent(
    name: "AssistantAgent",
    instructions: @"You are a friendly and helpful AI assistant.        
        Guidelines:
        - Be concise and clear in your responses
        - Remember context from previous messages in the conversation
        - When asked about prior messages, reference the conversation history
        - Use simple language that is easy to understand
        - If you don't know something, say so honestly
        ");

//// Create and register the agent directly on startup
//// The agent is stateless and shared across all requests
//builder.Services.AddSingleton<AIAgent>(sp =>
//{
//    var chatClient = sp.GetRequiredService<IChatClient>();
//    var logger = sp.GetRequiredService<ILogger<Program>>();

//    logger.LogInformation("Creating AIAgent on startup");

//    // Create the agent with predefined instructions
//    var agent = chatClient.CreateAIAgent(
//        instructions: """
//            You are a friendly and helpful AI assistant.
            
//            Guidelines:
//            - Be concise and clear in your responses
//            - Remember context from previous messages in the conversation
//            - When asked about prior messages, reference the conversation history
//            - Use simple language that is easy to understand
//            - If you don't know something, say so honestly
//            """,
//        name: "AssistantAgent"
//    );

//    logger.LogInformation("AIAgent successfully created and registered");
//    return agent;
//});

// Register AgentRunner
builder.Services.AddScoped<AgentRunner>();

var app = builder.Build();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map chat endpoints
app.MapChatEndpoints();

app.Run();

// Make Program accessible for testing
public partial class Program { }

