# Architecture

This document describes the architecture and design principles of the MAF Stateful Aspire Sample.

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

- **Orchestration**: Starts Redis, Ollama, API, and Client in the correct order
- **Service Discovery**: Client finds API via logical name (`http://api`)
- **Redis Provisioning**: Automatically runs Redis container (no docker-compose!)
- **Ollama Provisioning**: Automatically runs Ollama container for local AI
- **Secret Management**: Parameters passed securely from AppHost to projects
- **Dashboard**: Real-time observability (logs, traces, metrics)

## 📁 Project Structure

```
/
├── MafStatefulApi.sln
├── docs/                            # Detailed documentation
│   ├── architecture.md              # This file
│   ├── configuration.md             # Configuration guide
│   └── api-reference.md             # API endpoints reference
├── src/
│   ├── MafStatefulApi.AppHost/      # Aspire orchestration
│   │   ├── AppHost.cs               # Redis + Ollama + API + Client wiring
│   │   └── MafStatefulApi.AppHost.csproj
│   ├── MafStatefulApi.ServiceDefaults/ # Shared Aspire configuration
│   │   ├── Extensions.cs            # OpenTelemetry, health checks
│   │   └── MafStatefulApi.ServiceDefaults.csproj
│   ├── MafStatefulApi.Api/          # Web API
│   │   ├── Program.cs               # DI configuration and agent registration
│   │   ├── Endpoints/ChatEndpoints.cs # POST /chat, POST /reset
│   │   ├── Models/                  # Request/Response DTOs
│   │   ├── State/                   # IAgentSessionStore implementations
│   │   ├── Agents/                  # AgentRunner
│   │   └── appsettings.json
│   └── MafStatefulApi.Client/       # Console demo client
│       ├── Program.cs               # Service discovery demo
│       └── ApiClient.cs             # Typed HttpClient
└── tests/
    └── MafStatefulApi.Tests/        # Integration tests
        └── ChatFlowTests.cs
```

## 🤖 Agent Registration

The application creates and registers the AI agent directly on startup in `Program.cs`:

- **Agent Creation**: The agent is created using `IChatClient.CreateAIAgent()` with predefined instructions
- **Singleton Registration**: The agent is registered as a singleton in the DI container using a factory function
- **Stateless Design**: The agent instance is stateless and shared across all requests
- **Thread Management**: Each conversation has its own `AgentThread` that holds the state

This approach ensures the agent is ready to use as soon as the application starts and makes the code easy to understand and explain.

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
- Secrets are managed through Aspire parameters and user secrets
