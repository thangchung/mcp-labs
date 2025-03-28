var builder = DistributedApplication.CreateBuilder(args);

// !External params
var username = builder.AddParameter("db-username");
var password = builder.AddParameter("db-password");

var postgresQL = builder.AddPostgres("postgresQL", userName: username, password: password)
                        .WithLifetime(ContainerLifetime.Persistent)
                        .WithBindMount("./Scripts", "/docker-entrypoint-initdb.d")
                        .WithEndpoint("tcp", (e) =>
                        {
                            e.Port = 5432;
                            e.IsProxied = false;
                        })
                        .WithPgWeb();

var postgres = postgresQL.AddDatabase("postgres");

//var mcpServer = builder.AddProject<Projects.MCPServer>("mcp-server");

//var mcpClient = builder.AddProject<Projects.MCPClient>("mcp-client")
//                        .WithReference(postgres).WaitFor(postgres)
//                        .WaitFor(mcpServer);

builder.Build().Run();
