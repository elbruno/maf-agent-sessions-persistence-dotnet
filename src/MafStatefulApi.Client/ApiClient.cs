using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace MafStatefulApi.Client;

/// <summary>
/// Typed HttpClient for calling the MafStatefulApi.
/// Uses service discovery to resolve the API address.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a chat message to the API.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="conversationId">Optional conversation ID for continuing a conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response from the API.</returns>
    public async Task<ChatResponse?> ChatAsync(
        string message, 
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending chat request. ConversationId: {ConversationId}",
            conversationId ?? "(new)");

        var request = new ChatRequest
        {
            ConversationId = conversationId,
            Message = message
        };

        var response = await _httpClient.PostAsJsonAsync("/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
        
        _logger.LogInformation(
            "Received response for conversation {ConversationId}",
            result?.ConversationId);
        
        return result;
    }

    /// <summary>
    /// Resets a conversation by deleting its session state.
    /// </summary>
    /// <param name="conversationId">The conversation ID to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ResetAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting conversation {ConversationId}", conversationId);
        
        var response = await _httpClient.PostAsync($"/reset/{conversationId}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

/// <summary>
/// Chat request model matching the API.
/// </summary>
public record ChatRequest
{
    public string? ConversationId { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Chat response model matching the API.
/// </summary>
public record ChatResponse
{
    public required string ConversationId { get; init; }
    public required string Answer { get; init; }
}
