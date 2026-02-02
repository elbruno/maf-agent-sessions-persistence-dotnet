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
    private readonly AIAgent _agent;
    private readonly IAgentSessionStore _sessionStore;
    private readonly ILogger<AgentRunner> _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public AgentRunner(
        AIAgent agent,
        IAgentSessionStore sessionStore,
        ILogger<AgentRunner> logger)
    {
        _agent = agent;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    /// <summary>
    /// Runs the agent with the given message, managing session persistence.
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

        // Load or create the agent session (stateful)
        var session = await LoadOrCreateSessionAsync(conversationId, cancellationToken);

        // Run the agent with the user message and session
        var response = await _agent.RunAsync(userMessage, session, cancellationToken: cancellationToken);
        var answer = response.Text ?? string.Empty;

        // Save the updated session
        await SaveSessionAsync(conversationId, session, cancellationToken);

        _logger.LogInformation(
            "Agent response for conversation {ConversationId}: {ResponseLength} chars",
            conversationId,
            answer.Length);

        return answer;
    }

    private async Task<AgentSession> LoadOrCreateSessionAsync(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var serializedSession = await _sessionStore.GetAsync(conversationId, cancellationToken);

        if (serializedSession is not null)
        {
            _logger.LogDebug(
                "Deserializing existing session for conversation {ConversationId}",
                conversationId);

            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(serializedSession, JsonOptions);
                return await _agent.DeserializeSessionAsync(jsonElement, JsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deserialize session for conversation {ConversationId}, creating new session",
                    conversationId);
            }
        }

        _logger.LogDebug(
            "Creating new session for conversation {ConversationId}",
            conversationId);

        return await _agent.GetNewSessionAsync(cancellationToken);
    }

    private async Task SaveSessionAsync(
        string conversationId,
        AgentSession session,
        CancellationToken cancellationToken)
    {
        var serializedElement = session.Serialize(JsonOptions);
        var serialized = serializedElement.GetRawText();

        _logger.LogDebug(
            "Saving session for conversation {ConversationId}, serialized size: {SizeBytes} bytes",
            conversationId,
            serialized.Length);

        await _sessionStore.SetAsync(conversationId, serialized, cancellationToken);
    }
}
