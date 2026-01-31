var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache resource for session persistence
var cache = builder.AddRedis("cache");

// Add Ollama container with persistent volume for model caching
var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

// Add a model to Ollama (default: llama3.2:1b)
var ollamaModel = ollama.AddModel("chat-model", "llama3.2:1b");

// Add the API project with Redis and Ollama references
var api = builder.AddProject<Projects.MafStatefulApi_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(ollamaModel)
    .WaitFor(ollamaModel);

// Add the Client project and reference the API
builder.AddProject<Projects.MafStatefulApi_Client>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
