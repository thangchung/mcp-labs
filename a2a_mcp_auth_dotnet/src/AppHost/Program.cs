var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.McpServer>(nameof(Projects.McpServer));

var pong = builder.AddProject<Projects.PongService>(nameof(Projects.PongService))
    .WithReference(mcp);

var ping = builder.AddProject<Projects.PingService>(nameof(Projects.PingService))
    .WithReference(pong);

builder.Build().Run();
