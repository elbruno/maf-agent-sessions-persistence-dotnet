---
title: "Never Lose Your AI Agent's Train of Thought: Persisting MAF Sessions (with Blazor Demo!)"
description: "Learn how to build a Microsoft Agent Framework chat that remembers conversations — with ASP.NET Core, Redis state persistence, and a live Blazor UI."
date: 2026-02-XX
tags: [".NET", "AI", "Microsoft Agent Framework", "MAF", "Blazor", "Aspire", "Redis", "State Management"]
author: "Bruno Capuano"
---

# 🤖 Never Lose Your AI Agent’s Train of Thought  
### Persisting Microsoft Agent Framework Sessions with ASP.NET, Redis & Blazor

Have you ever built a chat app where the AI forgets what you said *five seconds ago*? 😅  
That’s the classic stateless API problem — and today we’re fixing that in .NET using the **Microsoft Agent Framework (MAF)**, **persistent agent sessions**, and a **Blazor frontend** so you can *see memory working live*.

This post is **practical**, **educational**, and a little bit fun — because serious tech doesn’t need to be boring. 🚀

---

## 🧠 The Problem: Stateless APIs vs Real Conversations

Classic Web APIs work like this:

Request ➜ Response ➜ Bye 👋

That’s great for CRUD. It’s *terrible* for conversations.

Conversational agents are **multi‑turn**. They need context. Without it, you’ll see:

- Repeated questions  
- Inconsistent answers  
- Personality resets  
- Users rage‑quitting your app 😬

📌 **Diagram — Stateless APIs vs Multi‑Turn Chat**  
`[PLACEHOLDER: diagram-problem-framing.gif]`

In MAF, this happens because **the agent itself is stateless**.

---

## 🎒 The Mental Model: Agent ≠ Memory

This is the most important idea in this post:

- 🧠 **Agent** → stateless brain  
- 🎒 **AgentSession** → memory backpack  
- 📦 **Persisted Store** → where memory lives

If you don’t persist the backpack, your agent has **goldfish memory** 🐟.

📌 **Diagram — Agent vs AgentSession**  
`[PLACEHOLDER: diagram-maf-mental-model.gif]`

> Persist `AgentSession`, not your hopes.

MAF fully supports serializing and restoring `AgentSession`, which makes proper multi‑turn conversations possible.

---

## 🏗️ Architecture Overview

Here’s the flow we’ll build:

1. Client sends `conversationId` + message  
2. API loads `AgentSession` from a store (Redis or in‑memory)  
3. Agent runs with that session  
4. Updated session is saved back  
5. Response is returned

📌 **Diagram — Architecture Flow**  
`[PLACEHOLDER: diagram-architecture-flow.gif]`

This design survives **multiple instances**, restarts, and real production traffic.

---

## 💻 Minimal ASP.NET Web API Sample

### Request & Response Models

```csharp
public record ChatRequest(string? ConversationId, string Message);

public record ChatResponse(string ConversationId, string Answer);
```

### `/chat` Endpoint

```csharp
app.MapPost("/chat", async (
    ChatRequest req,
    IAgentSessionStore sessionStore,
    AgentRunner runner) =>
{
    string conversationId = req.ConversationId 
        ?? Guid.NewGuid().ToString("N");

    var session = await sessionStore.LoadAsync(conversationId);
    var (answer, updatedSession) =
        await runner.RunAsync(req.Message, session);

    await sessionStore.SaveAsync(conversationId, updatedSession);

    return new ChatResponse(conversationId, answer);
});
```

This is the **entire trick**:
- Load session  
- Run agent  
- Save session  

Everything else is plumbing.

---

## 🧠 Running the Agent with Memory

```csharp
public class AgentRunner
{
    private readonly AIAgent _agent;

    public AgentRunner(AIAgent agent)
    {
        _agent = agent;
    }

    public async Task<(string Answer, AgentSession Session)> RunAsync(
        string message,
        AgentSession? previousSession)
    {
        var result = await _agent.RunAsync(message, previousSession);
        return (result.Answer, result.Session);
    }
}
```

MAF automatically tracks conversation turns inside `AgentSession` — you just persist it.

---

## 🗄️ Persisting Sessions (Redis Example)

Sessions are stored as JSON using `IDistributedCache`:

```csharp
var json = session.Serialize(JsonSerializerOptions.Web).GetRawText();

await cache.SetStringAsync(
    $"maf:sessions:{conversationId}",
    json,
    new DistributedCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    });
```

Loading is just as simple:

```csharp
var json = await cache.GetStringAsync(key);

if (json is null) return null;

return await agent.DeserializeSessionAsync(
    JsonDocument.Parse(json).RootElement,
    JsonSerializerOptions.Web);
```

---

## 🖥️ Bonus: Live Blazor Frontend

To make this visual (and fun), the repo includes a **Blazor UI** that lets you:

- Chat with the agent  
- Reuse the same `conversationId`  
- Watch memory persist live  
- Reset sessions instantly  

```razor
<EditForm Model="@input" OnValidSubmit="Send">
    <InputText @bind-Value="input.Message" />
    <button>Send</button>
</EditForm>
```

This makes session persistence *obvious* instead of abstract.

---

## ▶️ Run It Yourself

```bash
git clone https://github.com/elbruno/maf-agent-sessions-persistence-dotnet
dotnet restore
dotnet run
```

Open the Blazor UI and start chatting — then refresh the page and continue the same conversation.

That’s memory. 🎯

---

## 🚀 Why This Matters

Persisting `AgentSession` gives you:

- Real multi‑turn conversations  
- Scalable APIs  
- Better UX  
- Production‑ready agents  

Your AI stops acting like a goldfish and starts acting like… well, an assistant.

---

## 🔗 Repo

👉 https://github.com/elbruno/maf-agent-sessions-persistence-dotnet

---

Happy coding — and give your agents a memory!  
**Bruno** 🚀
