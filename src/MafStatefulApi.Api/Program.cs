using MafStatefulApi.Api.Agents;
using MafStatefulApi.Api.Endpoints;
using MafStatefulApi.Api.State;
using Microsoft.Agents.AI.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI support
builder.Services.AddOpenApi();

// Configure Redis distributed cache for session persistence
builder.AddRedisDistributedCache("cache");

// Also register IConnectionMultiplexer for advanced Redis operations
builder.AddRedisClient("cache");

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
        - If you know the name is the user, always use it in your responses
        - If you don't know something, say so honestly
        ");

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

