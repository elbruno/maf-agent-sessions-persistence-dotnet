using System.Net.Http.Json;

namespace MafStatefulApi.Web.Services;

public class ChatApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChatApiService> _logger;

    public ChatApiService(IHttpClientFactory httpClientFactory, ILogger<ChatApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ChatResponse?> SendMessageAsync(string message, string? conversationId = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var request = new ChatRequest
            {
                Message = message,
                ConversationId = conversationId
            };

            var response = await client.PostAsJsonAsync("/chat", request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ChatResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to API");
            return null;
        }
    }

    public async Task<List<string>> GetSessionsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var response = await client.GetAsync("/sessions");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SessionsResponse>();
            return result?.Sessions?.ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions from API");
            return new List<string>();
        }
    }

    public async Task<bool> ResetSessionAsync(string conversationId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("api");
            var response = await client.PostAsync($"/reset/{conversationId}", null);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting session {ConversationId}", conversationId);
            return false;
        }
    }
}

public record ChatRequest
{
    public string? ConversationId { get; init; }
    public required string Message { get; init; }
}

public record ChatResponse
{
    public required string ConversationId { get; init; }
    public required string Answer { get; init; }
}

public record SessionsResponse
{
    public required IEnumerable<string> Sessions { get; init; }
}
