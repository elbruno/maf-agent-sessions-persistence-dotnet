# API Reference

This document describes the API endpoints available in the MAF Stateful Aspire Sample.

## Endpoints

### POST /chat

Send a message to the agent and receive a response.

**Request:**

```json
{
  "conversationId": "optional-guid",
  "message": "Hello!"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `conversationId` | string | No | Existing conversation ID. Omit to start a new conversation. |
| `message` | string | Yes | The user's message to send to the agent. |

**Response:**

```json
{
  "conversationId": "generated-or-provided-guid",
  "answer": "Agent's response..."
}
```

| Field | Type | Description |
|-------|------|-------------|
| `conversationId` | string | The conversation ID (generated if not provided). |
| `answer` | string | The agent's response to the message. |

**Example:**

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
```

### POST /reset/{conversationId}

Delete a conversation's session state.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `conversationId` | string | The conversation ID to reset. |

**Response:**

```json
{
  "message": "Conversation {id} has been reset."
}
```

**Example:**

```bash
curl -X POST http://localhost:5256/reset/abc-123

# Response: {"message":"Conversation abc-123 has been reset."}
```

### GET /sessions

List all active conversation sessions.

**Response:**

```json
{
  "sessions": [
    "abc-123",
    "def-456",
    "ghi-789"
  ]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `sessions` | string[] | Array of active conversation IDs. |

**Example:**

```bash
curl http://localhost:5256/sessions

# Response: {"sessions":["abc-123","def-456"]}
```

### GET /health

Health check endpoint. Returns the health status of the application and its dependencies.

**Response:**

- `200 OK` - The application is healthy
- `503 Service Unavailable` - The application is unhealthy

### GET /alive

Liveness probe endpoint. Returns whether the application is running.

**Response:**

- `200 OK` - The application is alive

## Error Responses

All endpoints may return the following error responses:

### 500 Internal Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "An error occurred while processing your request."
}
```

## OpenAPI Documentation

When running in development mode, the API exposes OpenAPI documentation at:

- **OpenAPI JSON**: `GET /openapi/v1.json`

Access the Aspire Dashboard for a comprehensive view of all services and their endpoints.
