# Configuration

This document describes how to configure the MAF Stateful Aspire Sample.

## AI Provider Configuration

The sample supports two AI providers:
1. **Ollama** (default) - Local AI models for development, managed by Aspire
2. **Azure Foundry Models (Azure OpenAI)** - Cloud-based AI for production

### Ollama Configuration (Default)

Ollama is the default provider and runs locally via Docker container managed by Aspire.

The Aspire AppHost automatically:
- Starts an Ollama container with the `llama3.2:1b` model
- Persists downloaded models via Docker volume
- Makes the Ollama API available to the API project via service discovery

The model is defined in `AppHost.cs`:
```csharp
var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

var ollamaModel = ollama.AddModel("chat-model", "llama3.2:1b");
```

To use a different model, modify the `AddModel` call in `AppHost.cs`.

### Azure Foundry Models (Azure OpenAI) Configuration

For production workloads or when you need more powerful models, configure Azure Foundry Models (Azure OpenAI).

**Configuration Parameters:**

| Parameter | Description | Required |
|-----------|-------------|----------|
| `AzureOpenAI:Endpoint` | Azure OpenAI resource endpoint | Yes |
| `AzureOpenAI:DeploymentName` | Deployment name (e.g., gpt-4o) | Yes |
| `AzureOpenAI:ApiKey` | Azure OpenAI API key | Yes |

## Secrets Management with Aspire

Secrets are defined as parameters in the Aspire AppHost and passed to projects via environment variables.

### Configuring Secrets via User Secrets

1. Navigate to the AppHost project:
   ```bash
   cd src/MafStatefulApi.AppHost
   ```

2. Set secrets using the .NET user-secrets tool:
   ```bash
   # Azure Foundry Models (Azure OpenAI) secrets
   dotnet user-secrets set "Parameters:AzureOpenAI-Endpoint" "https://your-resource.openai.azure.com/"
   dotnet user-secrets set "Parameters:AzureOpenAI-DeploymentName" "gpt-4o"
   dotnet user-secrets set "Parameters:AzureOpenAI-ApiKey" "your-api-key"
   ```

### Configuring via appsettings.json

You can also configure non-secret values in `src/MafStatefulApi.AppHost/appsettings.json`:

```json
{
  "Parameters": {
    "AzureOpenAI-Endpoint": "",
    "AzureOpenAI-DeploymentName": "",
    "AzureOpenAI-ApiKey": ""
  }
}
```

### Environment Variables

In CI/CD pipelines or production, use environment variables with double underscores for nested keys:

```bash
# State store configuration
export Parameters__StateStore="Redis"

# Azure Foundry Models configuration
export Parameters__AzureOpenAI-ApiKey="your-api-key"
export Parameters__AzureOpenAI-Endpoint="https://your-resource.openai.azure.com/"
export Parameters__AzureOpenAI-DeploymentName="gpt-4o"
```

## State Store Configuration

The state store determines where conversation sessions are persisted. This is configured via the Aspire AppHost parameter.

### Redis (Default)

Redis is the default state store and is automatically provisioned by Aspire.

**Configuration in AppHost appsettings.json:**
```json
{
  "Parameters": {
    "StateStore": "Redis"
  }
}
```

Or via user secrets:
```bash
cd src/MafStatefulApi.AppHost
dotnet user-secrets set "Parameters:StateStore" "Redis"
```

### In-Memory (Development Only)

For simple development scenarios without Docker:

**Configuration in AppHost appsettings.json:**
```json
{
  "Parameters": {
    "StateStore": "InMemory"
  }
}
```

Or via user secrets:
```bash
cd src/MafStatefulApi.AppHost
dotnet user-secrets set "Parameters:StateStore" "InMemory"
```

> ⚠️ **Warning**: In-memory storage doesn't persist across restarts and doesn't support horizontal scaling.

## Configuration Precedence

Configuration values are resolved in this order (highest priority first):

1. Environment variables
2. User secrets (development only)
3. `appsettings.{Environment}.json`
4. `appsettings.json`

## Full Configuration Reference

### AppHost appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  },
  "Parameters": {
    "StateStore": "Redis",
    "AzureOpenAI-Endpoint": "",
    "AzureOpenAI-DeploymentName": "",
    "AzureOpenAI-ApiKey": ""
  }
}
```

### API appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MafStatefulApi": "Debug"
    }
  },
  "AllowedHosts": "*",
  "StateStore": "Redis",
  "SessionTtlMinutes": 30,
  "AzureOpenAI": {
    "Endpoint": "",
    "DeploymentName": "",
    "ApiKey": ""
  }
}
```
