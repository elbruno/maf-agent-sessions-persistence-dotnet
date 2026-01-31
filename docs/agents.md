# Agent Development Guide

This document provides all the instructions needed to develop and run the MAF Stateful Aspire Sample.

## Prerequisites

### Required Software

1. **.NET 10 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verify installation: `dotnet --version`

2. **Docker Desktop**
   - Download from: https://www.docker.com/products/docker-desktop/
   - Required for Redis and Ollama containers
   - Ensure Docker is running before starting the application

3. **Git**
   - Download from: https://git-scm.com/downloads

### Optional Software

- **Visual Studio 2022** or **VS Code** with C# Dev Kit extension
- **Ollama** installed locally (for testing without Aspire-managed container)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/elbruno/maf-agent-sessions-persistence-dotnet.git
cd maf-agent-sessions-persistence-dotnet
```

### 2. Restore and Build

```bash
dotnet restore
dotnet build
```

### 3. Run the Application

```bash
dotnet run --project src/MafStatefulApi.AppHost
```

This command starts:
- 🗄️ **Redis container** - Session state storage
- 🤖 **Ollama container** - Local AI model server
- 🌐 **API** - ASP.NET Core Web API
- 💻 **Client** - Console demo application

### 4. Access the Aspire Dashboard

The console will display a URL for the Aspire Dashboard (typically `https://localhost:17248`). Open this URL to:
- Monitor all running resources
- View logs from all services
- Track traces and metrics
- Check health status

## Project Structure

```
/
├── docs/                            # Documentation (you are here)
│   ├── agents.md                    # This file
│   ├── architecture.md              # System architecture
│   ├── configuration.md             # Configuration guide
│   └── api-reference.md             # API endpoints
├── src/
│   ├── MafStatefulApi.AppHost/      # Aspire orchestration (entry point)
│   ├── MafStatefulApi.ServiceDefaults/ # Shared Aspire configuration
│   ├── MafStatefulApi.Api/          # Web API project
│   └── MafStatefulApi.Client/       # Console client project
└── tests/
    └── MafStatefulApi.Tests/        # Integration tests
```

## Agent Implementation

The application creates and registers the AI agent directly on program startup:

### Agent Registration

In `Program.cs`, the agent is registered as a singleton service:

```csharp
// Create and register the agent directly on startup
builder.Services.AddSingleton(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    // Create the agent with predefined instructions
    var agent = chatClient.CreateAIAgent(
        instructions: "You are a helpful AI assistant...",
        name: "AssistantAgent"
    );
    
    return agent;
});
```

This approach:
- Creates the agent on application startup
- Makes it available through dependency injection
- Keeps the code simple and easy to understand
- The agent is stateless and shared across all requests

### Agent Execution

The `AgentRunner` class receives the agent through dependency injection and manages:
- Loading conversation state (AgentThread) from the session store
- Running the agent with the user's message
- Saving the updated conversation state

This separation of concerns makes the code maintainable and testable.

## Aspire Orchestration

The AppHost project (`src/MafStatefulApi.AppHost`) orchestrates all services:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var cache = builder.AddRedis("cache");
var ollama = builder.AddOllama("ollama").WithDataVolume();
var ollamaModel = ollama.AddModel("chat-model", "llama3.2:1b");

// API with references
var api = builder.AddProject<Projects.MafStatefulApi_Api>("api")
    .WithReference(cache)
    .WithReference(ollamaModel)
    .WaitFor(ollamaModel);

// Client with API reference
builder.AddProject<Projects.MafStatefulApi_Client>("client")
    .WithReference(api);
```

## AI Provider Configuration

### Using Ollama (Local Development)

Ollama runs automatically via Aspire. The first time you run:
1. Docker pulls the Ollama image
2. Ollama downloads the configured model (default: `llama3.2:1b`)
3. Subsequent runs use the cached model (via data volume)

**To change the model:**
Modify the `AddModel` call in `src/MafStatefulApi.AppHost/AppHost.cs`:
```csharp
var ollamaModel = ollama.AddModel("chat-model", "llama3.1:latest");
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test
dotnet test --filter "Chat_NewConversation_ReturnsNewConversationId"
```

**Note**: Tests require Docker to be running for Redis and Ollama containers.

## Development Workflow

### Making Code Changes

1. **API Changes**: Edit files in `src/MafStatefulApi.Api/`
2. **Rebuild**: `dotnet build`
3. **Restart**: Stop and restart the AppHost

### Adding New Endpoints

1. Create endpoint method in `Endpoints/ChatEndpoints.cs`
2. Add DTOs in `Models/` if needed
3. Register in the endpoint group

### Modifying Agent Behavior

1. Edit `Program.cs` to change agent instructions in the agent registration code
2. The agent is created on application startup, so restart the application to see changes

## Debugging

### Using Visual Studio

1. Set `MafStatefulApi.AppHost` as the startup project
2. Press F5 to start debugging
3. Breakpoints work across all projects

### Using VS Code

1. Open the workspace
2. Use the "Debug .NET Aspire" launch configuration
3. Breakpoints work across all projects

### Viewing Logs

- **Aspire Dashboard**: Real-time logs for all services
- **Console**: Direct output from `dotnet run`
- **Docker logs**: `docker logs <container-id>`

## Troubleshooting

### Docker Not Running

```
Error: Unable to connect to Docker
```
**Solution**: Start Docker Desktop and wait for it to be ready.

### Port Already in Use

```
Error: Address already in use
```
**Solution**: Stop other applications using the port or let Aspire auto-assign ports.

### Ollama Model Download Slow

The first run downloads the AI model which can take several minutes.
**Solution**: Wait for the download to complete, or use a smaller model like `llama3.2:1b`.

### Redis Connection Failed

```
Error: Unable to connect to Redis
```
**Solution**: Ensure Docker is running and the Redis container started successfully.

## Related Documentation

- [Architecture](./architecture.md) - System design and components
- [Configuration](./configuration.md) - Configuration options
- [API Reference](./api-reference.md) - API endpoints

## External Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
- [Ollama](https://ollama.ai/)
- [OllamaSharp](https://github.com/awaescher/OllamaSharp)
