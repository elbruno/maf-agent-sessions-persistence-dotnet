using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using MafStatefulApi.Api.State;

namespace MafStatefulApi.Api.Agents;

/// <summary>
/// Orchestrates agent execution with session persistence.
/// Loads chat history state, runs the agent, and saves the updated state.
/// </summary>
public class AgentRunner
{
    private readonly AgentFactory _agentFactory;
    private readonly IAgentSessionStore _sessionStore;
    private readonly ILogger<AgentRunner> _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public AgentRunner(
        AgentFactory agentFactory,
        IAgentSessionStore sessionStore,
        ILogger<AgentRunner> logger)
    {
        _agentFactory = agentFactory;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    /// <summary>
    /// Runs the agent with the given message, managing thread persistence.
    /// </summary>
    /// <param name="conversationId">The conversation ID to load/save state.</param>
    /// <param name="userMessage">The user's message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    public async Task<string> RunAsync(
        string conversationId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running agent for conversation {ConversationId}",
            conversationId);

        // Create the agent (stateless)
        var agent = _agentFactory.CreateAgent();

        // Load or create the chat history (stateful)
        var chatHistory = await LoadOrCreateHistoryAsync(conversationId, cancellationToken);
        
        // Add the user message
        chatHistory.AddUserMessage(userMessage);

        // Run the agent with the chat history
        var response = new System.Text.StringBuilder();
        var messages = new List<ChatMessageContent> { new(AuthorRole.User, userMessage) };
        var thread = new ChatHistoryAgentThread(chatHistory);
        
        await foreach (var result in agent.InvokeAsync(messages, thread, null, cancellationToken))
        {
            if (result.Message.Content is not null)
            {
                response.Append(result.Message.Content);
            }
            thread = (ChatHistoryAgentThread)result.Thread;
        }

        var answer = response.ToString();
        
        // Add assistant response to history for persistence
        chatHistory.AddAssistantMessage(answer);

        // Save the updated chat history
        await SaveHistoryAsync(conversationId, chatHistory, cancellationToken);

        _logger.LogInformation(
            "Agent response for conversation {ConversationId}: {ResponseLength} chars",
            conversationId,
            answer.Length);

        return answer;
    }

    private async Task<ChatHistory> LoadOrCreateHistoryAsync(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var serializedHistory = await _sessionStore.GetAsync(conversationId, cancellationToken);
        
        if (serializedHistory is not null)
        {
            _logger.LogDebug(
                "Deserializing existing chat history for conversation {ConversationId}",
                conversationId);
            
            try
            {
                var messages = JsonSerializer.Deserialize<List<ChatMessageData>>(serializedHistory, JsonOptions);
                if (messages is not null)
                {
                    var history = new ChatHistory();
                    foreach (var msg in messages)
                    {
                        history.Add(new ChatMessageContent(
                            new AuthorRole(msg.Role),
                            msg.Content));
                    }
                    return history;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deserialize chat history for conversation {ConversationId}, creating new history",
                    conversationId);
            }
        }

        _logger.LogDebug(
            "Creating new chat history for conversation {ConversationId}",
            conversationId);
        
        return new ChatHistory();
    }

    private async Task SaveHistoryAsync(
        string conversationId,
        ChatHistory history,
        CancellationToken cancellationToken)
    {
        var messages = history.Select(m => new ChatMessageData
        {
            Role = m.Role.Label,
            Content = m.Content ?? string.Empty
        }).ToList();
        
        var serialized = JsonSerializer.Serialize(messages, JsonOptions);
        
        _logger.LogDebug(
            "Saving chat history for conversation {ConversationId}, serialized size: {SizeBytes} bytes",
            conversationId,
            serialized.Length);
        
        await _sessionStore.SetAsync(conversationId, serialized, cancellationToken);
    }
}

/// <summary>
/// Simple DTO for serializing chat messages.
/// </summary>
internal class ChatMessageData
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
