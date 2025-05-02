var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddConnectionString("openai");

var apiService = builder.AddProject<Projects.Prompting_ApiService>("apiservice")
    .WithReference(openai);

builder.Build().Run();
