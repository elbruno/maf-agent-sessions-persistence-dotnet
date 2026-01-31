# MAF Stateful Aspire Sample

A production-ready sample demonstrating **state management for Microsoft Agent Framework (MAF) agents** hosted behind an ASP.NET Core Web API, using **.NET Aspire** for orchestration, service discovery, and infrastructure provisioning.

## ✨ Features

- **Session Persistence**: Conversations survive across requests using Redis
- **Local AI with Ollama**: Run AI models locally without cloud dependencies
- **Azure Foundry Models**: Production-ready Azure OpenAI integration
- **.NET Aspire Orchestration**: Simplified infrastructure with automatic service discovery
- **Observability**: Built-in logging, tracing, and metrics

## ⚡ Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run the Application

```bash
git clone https://github.com/elbruno/maf-agent-sessions-persistence-dotnet.git
cd maf-agent-sessions-persistence-dotnet
dotnet run --project src/MafStatefulApi.AppHost
```

This starts Redis, Ollama (local AI), the API, and a demo client automatically.

### Test the API

```bash
# Start a new conversation
curl -X POST http://localhost:5256/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello! My name is Alice."}'

# Continue the conversation
curl -X POST http://localhost:5256/chat \
  -H "Content-Type: application/json" \
  -d '{"conversationId": "<id-from-above>", "message": "What is my name?"}'
```

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Agent Development Guide](docs/agents.md) | Getting started, prerequisites, and development workflow |
| [Architecture](docs/architecture.md) | System design, components, and production considerations |
| [Configuration](docs/configuration.md) | AI providers, secrets management, and settings |
| [API Reference](docs/api-reference.md) | Endpoint documentation and examples |

## 🔗 Related Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
- [Ollama](https://ollama.ai/)

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.