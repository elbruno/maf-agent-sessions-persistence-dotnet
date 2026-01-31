using MafStatefulApi.Api.Agents;
using MafStatefulApi.Api.Endpoints;
using MafStatefulApi.Api.State;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI support
builder.Services.AddOpenApi();

// Configure state store based on configuration
var stateStore = builder.Configuration.GetValue("StateStore", "Redis");
builder.Services.AddLogging(logging => logging.AddConsole());

if (stateStore.Equals("Redis", StringComparison.OrdinalIgnoreCase))
{
    // Use Redis distributed cache via Aspire integration
    builder.AddRedisDistributedCache("cache");
    builder.Services.AddSingleton<IAgentSessionStore, RedisAgentSessionStore>();
    Console.WriteLine("Using Redis for session storage");
}
else
{
    // Use in-memory cache for development/testing
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IAgentSessionStore, InMemoryAgentSessionStore>();
    Console.WriteLine("Using InMemory for session storage");
}

// Configure Microsoft Agent Framework with Ollama or Azure Foundry Models (Azure OpenAI)
ConfigureAgentFramework(builder);

// Create and register the agent directly on startup
// The agent is stateless and shared across all requests
builder.Services.AddSingleton(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Creating AIAgent on startup");
    
    // Create the agent with predefined instructions
    var agent = chatClient.CreateAIAgent(
        instructions: """
            You are a helpful AI assistant. You help users with their questions 
            and remember the context of the conversation. Be concise but thorough 
            in your responses. If the user asks about previous messages, refer to 
            the conversation history.
            """,
        name: "AssistantAgent"
    );
    
    logger.LogInformation("AIAgent successfully created and registered");
    return agent;
});

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

/// <summary>
/// Configures Microsoft Agent Framework with the appropriate AI service.
/// Supports Ollama (via Aspire integration) or Azure Foundry Models (Azure OpenAI).
/// </summary>
static void ConfigureAgentFramework(WebApplicationBuilder builder)
{
    // Azure Foundry Models (Azure OpenAI) configuration
    var azureEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]?.Trim();
    var azureDeployment = builder.Configuration["AzureOpenAI:DeploymentName"]?.Trim();
    var azureApiKey = builder.Configuration["AzureOpenAI:ApiKey"]?.Trim();

    var hasAzureConfig = !string.IsNullOrEmpty(azureEndpoint)
        && !string.IsNullOrEmpty(azureDeployment)
        && !string.IsNullOrEmpty(azureApiKey);
    var hasValidAzureEndpoint = Uri.TryCreate(azureEndpoint, UriKind.Absolute, out var azureEndpointUri)
        && (azureEndpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || azureEndpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));

    if (hasAzureConfig && hasValidAzureEndpoint)
    {
        // Use Azure Foundry Models (Azure OpenAI)
        var azureClient = new AzureOpenAIClient(
            azureEndpointUri,
            new System.ClientModel.ApiKeyCredential(azureApiKey!));
        var chatClient = azureClient.GetChatClient(azureDeployment!);
        builder.Services.AddSingleton(chatClient);
        // Register as IChatClient using the extension method for compatibility with Microsoft.Extensions.AI
        builder.Services.AddSingleton<IChatClient>(chatClient.AsIChatClient());
        Console.WriteLine($"Using Azure Foundry Models: {azureEndpoint}");
    }
    else
    {
        if (hasAzureConfig && !hasValidAzureEndpoint)
        {
            Console.WriteLine($"Azure endpoint is invalid, falling back to Ollama: {azureEndpoint}");
        }
        // Use Ollama via Aspire integration (default for local development)
        // "chat-model" must match the model name defined in AppHost.cs (ollama.AddModel("chat-model", ...))
        builder.AddOllamaApiClient("chat-model").AddChatClient();
        Console.WriteLine("Using Ollama via Aspire integration");
    }
}

// Make Program accessible for testing
public partial class Program { }

