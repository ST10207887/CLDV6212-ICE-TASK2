using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register a single TableServiceClient for dependency injection.
// Locally "UseDevelopmentStorage=true" points at Azurite.
var connectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage")
                       ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new TableServiceClient(connectionString));

builder.Build().Run();
