var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddConnectionString("openai");
var chatModelId = builder.AddConnectionString("chatModelId");
var endpoint = builder.AddConnectionString("endpoint");
var apiKey = builder.AddConnectionString("apiKey");

var mcpserver = builder.AddProject<Projects.MCPServer>("mcpserver");
mcpserver.WithReference(chatModelId);
mcpserver.WithReference(endpoint);
mcpserver.WithReference(apiKey);

var mcpclient = builder.AddProject<Projects.MCPClient>("mcpclient")
    .WaitFor(mcpserver);
mcpclient.WithReference(chatModelId);
mcpclient.WithReference(endpoint);
mcpclient.WithReference(apiKey);

builder.Build().Run();
