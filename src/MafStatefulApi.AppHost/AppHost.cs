var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache resource
var cache = builder.AddRedis("cache");

// Configure secrets as parameters (defined in user secrets or appsettings.json)
// Azure Foundry Models (Azure OpenAI) configuration
var azureOpenAIEndpoint = builder.AddParameter("AzureOpenAI-Endpoint", secret: false);
var azureOpenAIDeployment = builder.AddParameter("AzureOpenAI-DeploymentName", secret: false);
var azureOpenAIApiKey = builder.AddParameter("AzureOpenAI-ApiKey", secret: true);

// Ollama configuration
var ollamaEndpoint = builder.AddParameter("Ollama-Endpoint", secret: false);
var ollamaModel = builder.AddParameter("Ollama-Model", secret: false);

// Add Ollama container with persistent volume for model caching
var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

// Add the API project with Redis reference and AI configuration
var api = builder.AddProject<Projects.MafStatefulApi_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(ollama)
    .WithEnvironment("AzureOpenAI__Endpoint", azureOpenAIEndpoint)
    .WithEnvironment("AzureOpenAI__DeploymentName", azureOpenAIDeployment)
    .WithEnvironment("AzureOpenAI__ApiKey", azureOpenAIApiKey)
    .WithEnvironment("Ollama__Endpoint", ollamaEndpoint)
    .WithEnvironment("Ollama__Model", ollamaModel);

// Add the Client project and reference the API
builder.AddProject<Projects.MafStatefulApi_Client>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
