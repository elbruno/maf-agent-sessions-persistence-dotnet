var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache resource
var cache = builder.AddRedis("cache");

// Add Ollama container with persistent volume for model caching
var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

// Add a model to Ollama (default: llama3.2:1b)
var ollamaModel = ollama.AddModel("chat-model", "llama3.2:1b");

// Configure secrets as parameters (defined in user secrets or appsettings.json)
// Azure Foundry Models (Azure OpenAI) configuration - optional
var azureOpenAIEndpoint = builder.AddParameter("AzureOpenAI-Endpoint");
var azureOpenAIDeployment = builder.AddParameter("AzureOpenAI-DeploymentName");
var azureOpenAIApiKey = builder.AddParameter("AzureOpenAI-ApiKey", secret: true);

// Add the API project with Redis reference and AI configuration
var api = builder.AddProject<Projects.MafStatefulApi_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(ollamaModel)
    .WaitFor(ollamaModel)
    .WithEnvironment("AzureOpenAI__Endpoint", azureOpenAIEndpoint)
    .WithEnvironment("AzureOpenAI__DeploymentName", azureOpenAIDeployment)
    .WithEnvironment("AzureOpenAI__ApiKey", azureOpenAIApiKey);

// Add the Client project and reference the API
builder.AddProject<Projects.MafStatefulApi_Client>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
