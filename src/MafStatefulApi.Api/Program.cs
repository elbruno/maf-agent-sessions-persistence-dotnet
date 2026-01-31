using MafStatefulApi.Api.Agents;
using MafStatefulApi.Api.Endpoints;
using MafStatefulApi.Api.State;
using OpenAI;
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

// Configure Microsoft Agent Framework with Azure OpenAI or OpenAI
ConfigureAgentFramework(builder);

// Register agent components
builder.Services.AddSingleton<AgentFactory>();
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
/// Supports Azure OpenAI, OpenAI, or a mock for testing.
/// </summary>
static void ConfigureAgentFramework(WebApplicationBuilder builder)
{
    var azureEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
    var azureDeployment = builder.Configuration["AzureOpenAI:DeploymentName"];
    var azureApiKey = builder.Configuration["AzureOpenAI:ApiKey"];
    
    var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
    var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    
    if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureDeployment) && !string.IsNullOrEmpty(azureApiKey))
    {
        // Use Azure OpenAI
        var azureClient = new AzureOpenAIClient(
            new Uri(azureEndpoint),
            new System.ClientModel.ApiKeyCredential(azureApiKey));
        var chatClient = azureClient.GetChatClient(azureDeployment);
        builder.Services.AddSingleton(chatClient);
        Console.WriteLine($"Using Azure OpenAI: {azureEndpoint}");
    }
    else if (!string.IsNullOrEmpty(openAiApiKey))
    {
        // Use OpenAI
        var openAiClient = new OpenAIClient(openAiApiKey);
        var chatClient = openAiClient.GetChatClient(openAiModel);
        builder.Services.AddSingleton(chatClient);
        Console.WriteLine($"Using OpenAI model: {openAiModel}");
    }
    else
    {
        // For development without API keys, use a mock/echo response
        Console.WriteLine("Warning: No AI provider configured. Using mock responses.");
        Console.WriteLine("Set AzureOpenAI:* or OpenAI:* configuration to enable real AI.");
        
        // Create a mock ChatClient using a custom HttpClient with mock handler
        var httpClient = new HttpClient(new MockChatHandler());
        httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        var openAiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("mock-key"), new OpenAIClientOptions
        {
            Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
        });
        var chatClient = openAiClient.GetChatClient("mock");
        builder.Services.AddSingleton(chatClient);
    }
}

/// <summary>
/// Mock HTTP handler for development without real AI credentials.
/// Returns echo responses for testing the flow.
/// </summary>
internal class MockChatHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                    "id": "mock-response",
                    "object": "chat.completion",
                    "created": 1234567890,
                    "model": "mock",
                    "choices": [{
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": "This is a mock response. Configure AzureOpenAI:* or OpenAI:* settings for real AI responses. I received your message and the conversation history is being tracked."
                        },
                        "finish_reason": "stop"
                    }],
                    "usage": {"prompt_tokens": 10, "completion_tokens": 20, "total_tokens": 30}
                }
                """, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

// Make Program accessible for testing
public partial class Program { }

