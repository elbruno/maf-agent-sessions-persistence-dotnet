using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MafStatefulApi.Api.Agents;

/// <summary>
/// Factory for creating ChatCompletionAgent instances.
/// Agents are stateless - all state is managed via AgentThread.
/// </summary>
public class AgentFactory
{
    private readonly Kernel _kernel;
    private readonly ILogger<AgentFactory> _logger;

    public AgentFactory(Kernel kernel, ILogger<AgentFactory> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new ChatCompletionAgent with predefined instructions.
    /// </summary>
    public ChatCompletionAgent CreateAgent()
    {
        _logger.LogDebug("Creating new ChatCompletionAgent instance");
        
        return new ChatCompletionAgent
        {
            Name = "AssistantAgent",
            Instructions = """
                You are a helpful AI assistant. You help users with their questions 
                and remember the context of the conversation. Be concise but thorough 
                in your responses. If the user asks about previous messages, refer to 
                the conversation history.
                """,
            Kernel = _kernel
        };
    }
}
