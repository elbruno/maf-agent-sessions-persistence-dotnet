using MafStatefulApi.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== MAF Stateful API Client Demo ===");
Console.WriteLine("This client uses Aspire service discovery to call the API.\n");

// Build the host with service defaults (including service discovery)
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Configure typed HttpClient with service discovery
// The base address "http://api" is resolved via Aspire service discovery
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http+https://api");
});

var host = builder.Build();

// Get the API client from DI
var client = host.Services.GetRequiredService<ApiClient>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    // Demo: Multi-turn conversation
    Console.WriteLine("Starting a multi-turn conversation...\n");

    // Turn 1: Start a new conversation
    Console.WriteLine("User: Hello! My name is Alice and I love hiking.");
    var response1 = await client.ChatAsync("Hello! My name is Alice and I love hiking.");
    Console.WriteLine($"Agent: {response1?.Answer}");
    Console.WriteLine($"(Conversation ID: {response1?.ConversationId})\n");

    if (response1?.ConversationId is not null)
    {
        // Wait a moment between requests
        await Task.Delay(1000);

        // Turn 2: Continue the same conversation (tests session persistence)
        Console.WriteLine("User: What is my name and what do I enjoy doing?");
        var response2 = await client.ChatAsync(
            "What is my name and what do I enjoy doing?",
            response1.ConversationId);
        Console.WriteLine($"Agent: {response2?.Answer}\n");

        await Task.Delay(1000);

        // Turn 3: Ask another contextual question
        Console.WriteLine("User: Can you suggest some hiking trails for me?");
        var response3 = await client.ChatAsync(
            "Can you suggest some hiking trails for me?",
            response1.ConversationId);
        Console.WriteLine($"Agent: {response3?.Answer}\n");

        // Reset the conversation
        Console.WriteLine($"Resetting conversation {response1.ConversationId}...");
        await client.ResetAsync(response1.ConversationId);
        Console.WriteLine("Conversation reset successfully.\n");
    }

    Console.WriteLine("=== Demo Complete ===");
    Console.WriteLine("The conversation demonstrated:");
    Console.WriteLine("1. Starting a new conversation (new session created)");
    Console.WriteLine("2. Continuing with the same conversationId (session loaded from store)");
    Console.WriteLine("3. Agent remembering context from previous messages");
    Console.WriteLine("4. Resetting the conversation (session deleted from store)");
}
catch (HttpRequestException ex)
{
    logger.LogError(ex, "Failed to connect to the API. Is the API running?");
    Console.WriteLine($"\nError: {ex.Message}");
    Console.WriteLine("Make sure the API is running via Aspire AppHost.");
}
catch (Exception ex)
{
    logger.LogError(ex, "An unexpected error occurred");
    Console.WriteLine($"\nError: {ex.Message}");
}
