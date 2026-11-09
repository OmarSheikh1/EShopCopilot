using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Forwards the configurable embedded SQLite connection to the API service.
var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=eshop.db;Cache=Shared";

var apiService = builder.AddProject<Projects.EShop_ApiService>("apiservice")
    .WithEnvironment("ConnectionStrings__DefaultConnection", databaseConnectionString)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
