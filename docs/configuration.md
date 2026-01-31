# Configuration

This document describes how to configure the MAF Stateful Aspire Sample.

## AI Provider Configuration

The sample uses **Ollama** for local AI models, managed automatically by .NET Aspire.

### Ollama Configuration

Ollama runs locally via Docker container managed by Aspire.

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

## Session Storage

The application uses **Redis** for session persistence, automatically provisioned by .NET Aspire.

Redis configuration in `AppHost.cs`:
```csharp
var cache = builder.AddRedis("cache");
```

The API project automatically connects to Redis via Aspire service discovery. Sessions are configured with a Time-To-Live (TTL) of 30 minutes by default.

To change the session TTL, modify `SessionTtlMinutes` in `src/MafStatefulApi.Api/appsettings.json`:
```json
{
  "SessionTtlMinutes": 30
}
```

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
  "SessionTtlMinutes": 30
}
```
