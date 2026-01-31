using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

namespace MafStatefulApi.Api.Agents;

/// <summary>
/// Factory for creating AIAgent instances using Microsoft Agent Framework.
/// Agents are stateless - all state is managed via AgentThread.
/// </summary>
public class AgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<AgentFactory> _logger;

    public AgentFactory(IChatClient chatClient, ILogger<AgentFactory> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new AIAgent with predefined instructions.
    /// </summary>
    public AIAgent CreateAgent()
    {
        _logger.LogDebug("Creating new AIAgent instance");
        
        // Create AI agent from IChatClient
        return _chatClient.CreateAIAgent(
            instructions: """
                You are a helpful AI assistant. You help users with their questions 
                and remember the context of the conversation. Be concise but thorough 
                in your responses. If the user asks about previous messages, refer to 
                the conversation history.
                """,
            name: "AssistantAgent"
        );
    }
}
