var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");
var chatModelId = builder.AddConnectionString("chatModelId");
var endpoint = builder.AddConnectionString("endpoint");
var apiKey = builder.AddConnectionString("apiKey");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var ingestionCache = builder.AddSqlite("ingestionCache");//.WithSqliteWeb();

var mcpserver = builder.AddProject<Projects.ChatApp_SampleMCPServer>("samplemcpserver");
mcpserver.WithReference(chatModelId);
mcpserver.WithReference(endpoint);
mcpserver.WithReference(apiKey);

var webApp = builder.AddProject<Projects.ChatApp_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
webApp
    .WithReference(ingestionCache)
    .WaitFor(ingestionCache);
webApp
    .WithReference(mcpserver)
    .WaitFor(mcpserver);

builder.Build().Run();
