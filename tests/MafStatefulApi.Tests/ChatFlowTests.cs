using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MafStatefulApi.Tests;

/// <summary>
/// Integration tests for the chat flow.
/// These tests verify the API endpoints work correctly with session persistence.
/// </summary>
public class ChatFlowTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Chat_NewConversation_ReturnsNewConversationId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MafStatefulApi_AppHost>(cancellationToken);
        
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var request = new { message = "Hello, I am a test user." };
        using var response = await httpClient.PostAsJsonAsync("/chat", request, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("conversationId", out var conversationIdProp));
        Assert.False(string.IsNullOrEmpty(conversationIdProp.GetString()));
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("answer", out var answerProp));
        Assert.False(string.IsNullOrEmpty(answerProp.GetString()));
    }

    [Fact]
    public async Task Chat_ContinueConversation_UseSameConversationId()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MafStatefulApi_AppHost>(cancellationToken);
        
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Act - First message
        var request1 = new { message = "Hello, my name is TestUser." };
        using var response1 = await httpClient.PostAsJsonAsync("/chat", request1, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        
        var content1 = await response1.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc1 = JsonDocument.Parse(content1);
        var conversationId = jsonDoc1.RootElement.GetProperty("conversationId").GetString();
        Assert.NotNull(conversationId);

        // Act - Second message with same conversationId
        var request2 = new { conversationId, message = "What is my name?" };
        using var response2 = await httpClient.PostAsJsonAsync("/chat", request2, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        var content2 = await response2.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc2 = JsonDocument.Parse(content2);
        
        var returnedConversationId = jsonDoc2.RootElement.GetProperty("conversationId").GetString();
        Assert.Equal(conversationId, returnedConversationId);
    }

    [Fact]
    public async Task Reset_ExistingConversation_ReturnsOk()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MafStatefulApi_AppHost>(cancellationToken);
        
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Create a conversation first
        var request = new { message = "Hello!" };
        using var chatResponse = await httpClient.PostAsJsonAsync("/chat", request, cancellationToken);
        var chatContent = await chatResponse.Content.ReadAsStringAsync(cancellationToken);
        var chatJson = JsonDocument.Parse(chatContent);
        var conversationId = chatJson.RootElement.GetProperty("conversationId").GetString();

        // Act - Reset the conversation
        using var resetResponse = await httpClient.PostAsync($"/reset/{conversationId}", null, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MafStatefulApi_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        using var response = await httpClient.GetAsync("/health", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
