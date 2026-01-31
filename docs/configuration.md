# Configuration

This document describes how to configure the MAF Stateful Aspire Sample.

## AI Provider Configuration

The sample supports two AI providers:
1. **Ollama** (default) - Local AI models for development
2. **Azure Foundry Models (Azure OpenAI)** - Cloud-based AI for production

### Ollama Configuration (Default)

Ollama is the default provider and runs locally via Docker container managed by Aspire.

The Aspire AppHost automatically:
- Starts an Ollama container
- Persists downloaded models via Docker volume
- Makes the Ollama API available to the API project

**Configuration Parameters:**

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Ollama:Endpoint` | Ollama API endpoint | `http://localhost:11434` |
| `Ollama:Model` | Model name to use | `llama3.2:1b` |

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

   # Ollama configuration (optional, has defaults)
   dotnet user-secrets set "Parameters:Ollama-Endpoint" "http://localhost:11434"
   dotnet user-secrets set "Parameters:Ollama-Model" "llama3.2:1b"
   ```

### Configuring via appsettings.json

You can also configure non-secret values in `src/MafStatefulApi.AppHost/appsettings.json`:

```json
{
  "Parameters": {
    "AzureOpenAI-Endpoint": "",
    "AzureOpenAI-DeploymentName": "",
    "AzureOpenAI-ApiKey": "",
    "Ollama-Endpoint": "http://localhost:11434",
    "Ollama-Model": "llama3.2:1b"
  }
}
```

### Environment Variables

In CI/CD pipelines or production, use environment variables with double underscores for nested keys:

```bash
export Parameters__AzureOpenAI-ApiKey="your-api-key"
export Parameters__AzureOpenAI-Endpoint="https://your-resource.openai.azure.com/"
export Parameters__AzureOpenAI-DeploymentName="gpt-4o"
```

## State Store Configuration

### Redis (Default)

Redis is the default state store and is automatically provisioned by Aspire.

**Configuration in appsettings.json:**
```json
{
  "StateStore": "Redis",
  "SessionTtlMinutes": 30
}
```

### In-Memory (Development Only)

For simple development scenarios without Docker:

```json
{
  "StateStore": "InMemory",
  "SessionTtlMinutes": 30
}
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
    "AzureOpenAI-Endpoint": "",
    "AzureOpenAI-DeploymentName": "",
    "AzureOpenAI-ApiKey": "",
    "Ollama-Endpoint": "http://localhost:11434",
    "Ollama-Model": "llama3.2:1b"
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
  },
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.2:1b"
  }
}
```
