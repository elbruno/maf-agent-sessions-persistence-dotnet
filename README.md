# MAF Stateful Aspire Sample

A production-ready sample demonstrating **state management for Microsoft Agent Framework (MAF) agents** hosted behind an ASP.NET Core Web API, using **.NET Aspire** for orchestration, service discovery, and Redis provisioning.

## 🎯 The Problem: Stateless APIs vs Multi-Turn Agents

AI agents need to remember conversation context across multiple user interactions. However:

- **HTTP APIs are stateless** - each request is independent
- **Agent instances are stateless** - they don't retain memory between calls
- **AgentThread holds the state** - conversation history and context

This sample shows how to bridge this gap by:
1. Serializing agent threads to a persistent store (Redis or in-memory)
2. Loading threads when continuing conversations
3. Managing session lifecycle (TTL, reset)

## 🧠 Mental Model

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────┐
│   Client    │────▶│   Web API   │────▶│  ChatCompletion │
│  (Console)  │◀────│ (ASP.NET)   │◀────│     Agent       │
└─────────────┘     └─────────────┘     └─────────────────┘
                           │                     │
                           │              ┌──────┴──────┐
                           │              │ AgentThread │ ◀── Stateful!
                           │              │  (Session)  │
                           │              └─────────────┘
                           │
                    ┌──────┴──────┐
                    │   Redis /   │
                    │  MemoryCache│
                    └─────────────┘
```

- **Agent**: Stateless. Created fresh for each request.
- **AgentThread**: Stateful. Contains conversation history.
- **Session Store**: Persists serialized threads between requests.

## 🚀 Aspire's Role

.NET Aspire provides:

- **Orchestration**: Starts Redis, API, and Client in the correct order
- **Service Discovery**: Client finds API via logical name (`http://api`)
- **Redis Provisioning**: Automatically runs Redis container (no docker-compose!)
- **Dashboard**: Real-time observability (logs, traces, metrics)

## 📁 Project Structure

```
/
├── MafStatefulApi.sln
├── src/
│   ├── MafStatefulApi.AppHost/        # Aspire orchestration
│   │   ├── Program.cs                 # Redis + API + Client wiring
│   │   └── MafStatefulApi.AppHost.csproj
│   ├── MafStatefulApi.ServiceDefaults/ # Shared Aspire configuration
│   │   ├── Extensions.cs              # OpenTelemetry, health checks
│   │   └── MafStatefulApi.ServiceDefaults.csproj
│   ├── MafStatefulApi.Api/            # Web API
│   │   ├── Program.cs                 # DI configuration
│   │   ├── Endpoints/ChatEndpoints.cs # POST /chat, POST /reset
│   │   ├── Models/                    # Request/Response DTOs
│   │   ├── State/                     # IAgentSessionStore implementations
│   │   ├── Agents/                    # AgentFactory, AgentRunner
│   │   └── appsettings.json
│   └── MafStatefulApi.Client/         # Console demo client
│       ├── Program.cs                 # Service discovery demo
│       └── ApiClient.cs               # Typed HttpClient
└── tests/
    └── MafStatefulApi.Tests/          # Integration tests
        └── ChatFlowTests.cs
```

## ⚡ Quick Start (< 10 minutes)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Redis container)
- Optional: Azure OpenAI or OpenAI API key (uses mock responses without)

### 1. Clone and Build

```bash
git clone https://github.com/elbruno/maf-agent-sessions-persistence-dotnet.git
cd maf-agent-sessions-persistence-dotnet
dotnet restore
dotnet build
```

### 2. Configure AI Provider (Optional)

Set environment variables or update `appsettings.json`:

**For Azure OpenAI:**
```bash
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__DeploymentName="gpt-4o"
export AzureOpenAI__ApiKey="your-key"
```

**For OpenAI:**
```bash
export OpenAI__ApiKey="sk-your-key"
export OpenAI__Model="gpt-4o-mini"
```

> Without credentials, the API uses mock responses for testing the flow.

### 3. Run the AppHost

```bash
dotnet run --project src/MafStatefulApi.AppHost
```

This starts:
- 🗄️ Redis container
- 🌐 API on a dynamic port
- 💻 Client console app

Open the Aspire dashboard URL shown in the console to monitor all resources.

### 4. Test with the Client

The client runs automatically and demonstrates:
1. Starting a new conversation
2. Continuing with session persistence
3. Resetting the conversation

### 5. Test with curl

```bash
# Start a new conversation
curl -X POST http://localhost:5256/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello! My name is Alice."}'

# Response: {"conversationId":"abc-123","answer":"Hello Alice! ..."}

# Continue the conversation (use the conversationId from above)
curl -X POST http://localhost:5256/chat \
  -H "Content-Type: application/json" \
  -d '{"conversationId": "abc-123", "message": "What is my name?"}'

# Response: {"conversationId":"abc-123","answer":"Your name is Alice..."}

# Reset the conversation
curl -X POST http://localhost:5256/reset/abc-123

# Response: {"message":"Conversation abc-123 has been reset."}
```

## 🔧 Configuration

### appsettings.json

```json
{
  "StateStore": "Redis",        // "Redis" or "InMemory"
  "SessionTtlMinutes": 30,      // Session expiration
  "AzureOpenAI": {
    "Endpoint": "",
    "DeploymentName": "",
    "ApiKey": ""
  },
  "OpenAI": {
    "ApiKey": "",
    "Model": "gpt-4o-mini"
  }
}
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `StateStore` | `Redis` or `InMemory` |
| `SessionTtlMinutes` | Session TTL in minutes |
| `AzureOpenAI__Endpoint` | Azure OpenAI endpoint |
| `AzureOpenAI__DeploymentName` | Azure OpenAI deployment name |
| `AzureOpenAI__ApiKey` | Azure OpenAI API key |
| `OpenAI__ApiKey` | OpenAI API key |
| `OpenAI__Model` | OpenAI model name |

## 🏗️ API Endpoints

### POST /chat

Send a message to the agent.

**Request:**
```json
{
  "conversationId": "optional-guid",  // Omit to start new conversation
  "message": "Hello!"
}
```

**Response:**
```json
{
  "conversationId": "generated-or-provided-guid",
  "answer": "Agent's response..."
}
```

### POST /reset/{conversationId}

Delete a conversation's session state.

**Response:**
```json
{
  "message": "Conversation {id} has been reset."
}
```

### GET /health

Health check endpoint.

### GET /alive

Liveness probe endpoint.

## 🔍 Observability

The sample includes full observability via Aspire ServiceDefaults:

- **OpenTelemetry Traces**: Track requests across services
- **OpenTelemetry Metrics**: HTTP, runtime, and custom metrics
- **Structured Logging**: ConversationId, cache hits/misses, serialization sizes
- **Health Checks**: Ready and live probes

View all telemetry in the Aspire Dashboard.

## 🚀 Production Considerations

### Scaling

- **Multiple API instances**: Redis store enables horizontal scaling
- **Session affinity**: Not required due to centralized state

### TTL and Memory

- Configure appropriate `SessionTtlMinutes` based on usage patterns
- Monitor Redis memory usage for high-traffic scenarios

### Store Downtime

- Implement circuit breakers (included via Aspire resilience)
- Consider fallback to in-memory cache for degraded mode

### History Trimming

- For long conversations, consider implementing history summarization
- Use `AgentThread` APIs to manage message count

### Security

- Never commit API keys to source control
- Use Azure Key Vault or similar for production secrets
- Consider rate limiting for public APIs

## 📚 Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
- [Semantic Kernel Agents](https://learn.microsoft.com/semantic-kernel/frameworks/agent/)
- [Persisting Agent Conversations](https://learn.microsoft.com/agent-framework/tutorials/agents/persisted-conversation)

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.