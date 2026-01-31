var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache resource
var cache = builder.AddRedis("cache");

// Add the API project and reference Redis
var api = builder.AddProject<Projects.MafStatefulApi_Api>("api")
    .WithReference(cache)
    .WaitFor(cache);

// Add the Client project and reference the API
builder.AddProject<Projects.MafStatefulApi_Client>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
