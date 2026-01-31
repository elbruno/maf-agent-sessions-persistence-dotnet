using System.Text.Json;
using Microsoft.Agents.AI;
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

        // Load or create the agent thread (stateful)
        var thread = await LoadOrCreateThreadAsync(agent, conversationId, cancellationToken);

        // Run the agent with the user message and thread
        var response = await agent.RunAsync(userMessage, thread, cancellationToken: cancellationToken);
        var answer = response.Text ?? string.Empty;

        // Save the updated thread
        await SaveThreadAsync(conversationId, thread, cancellationToken);

        _logger.LogInformation(
            "Agent response for conversation {ConversationId}: {ResponseLength} chars",
            conversationId,
            answer.Length);

        return answer;
    }

    private async Task<AgentThread> LoadOrCreateThreadAsync(
        AIAgent agent,
        string conversationId,
        CancellationToken cancellationToken)
    {
        var serializedThread = await _sessionStore.GetAsync(conversationId, cancellationToken);
        
        if (serializedThread is not null)
        {
            _logger.LogDebug(
                "Deserializing existing thread for conversation {ConversationId}",
                conversationId);
            
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(serializedThread, JsonOptions);
                return agent.DeserializeThread(jsonElement, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deserialize thread for conversation {ConversationId}, creating new thread",
                    conversationId);
            }
        }

        _logger.LogDebug(
            "Creating new thread for conversation {ConversationId}",
            conversationId);
        
        return agent.GetNewThread();
    }

    private async Task SaveThreadAsync(
        string conversationId,
        AgentThread thread,
        CancellationToken cancellationToken)
    {
        var serializedElement = thread.Serialize(JsonOptions);
        var serialized = serializedElement.GetRawText();
        
        _logger.LogDebug(
            "Saving thread for conversation {ConversationId}, serialized size: {SizeBytes} bytes",
            conversationId,
            serialized.Length);
        
        await _sessionStore.SetAsync(conversationId, serialized, cancellationToken);
    }
}
