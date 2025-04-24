var builder = DistributedApplication.CreateBuilder(args);

var mcpserver = builder.AddProject<Projects.MCPServer>("mcpserver");

builder.AddProject<Projects.MCPClient>("mcpclient")
    .WaitFor(mcpserver);

builder.Build().Run();
