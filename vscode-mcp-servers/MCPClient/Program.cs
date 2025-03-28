Console.WriteLine("Hello, official MCP csharp-sdk and MCP Server!");

// var message = "What is the current (CET) time in Illzach, France?";
var message = "Query all orders and show it in JSON format";

Console.WriteLine(message);

// 👇🏼 Configure the MCP client options
McpClientOptions options = new()
{
    ClientInfo = new() { Name = "Time Client", Version = "1.0.0" }
};

// 👇🏼 Configure the Model Context Protocol server to use
var config = new McpServerConfig
{
    Id = "time",
    Name = "Time MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        // 👇🏼 The command executed to start the MCP server
        ["command"] = @"..\..\..\..\MCPServer\bin\Debug\net9.0\MCPServer.exe"
    }
};

var conn = Environment.GetEnvironmentVariable("ConnectionStrings__postgres");

var result = conn?.Split(';')
                 .Select(x => x.Split('='))
                 .ToDictionary(x => x[0], x => x[1]);

var conn1 = $"postgresql://{result["Username"]}:{result["Password"]}@localhost:{result["Port"]}/postgres?connect_timeout=20";

var config1 = new McpServerConfig
{
    Id = "server-postgres",
    Name = "Server Postgres",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        // 👇🏼 The command executed to start the MCP server
        ["command"] = "npx",
        ["arguments"] = $"-y @modelcontextprotocol/server-postgres {conn1}"
    }
};

using var factory =
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// 👇🏼 Get an MCP session scope used to get the MCP tools
await using var mcpClient =
    await McpClientFactory.CreateAsync(config, options, loggerFactory: factory);

await using var mcpClient1 =
    await McpClientFactory.CreateAsync(config1, options, loggerFactory: factory);


// 👇🏼 Pass the MCP tools to the chat client
var mcpTools = await mcpClient.GetAIFunctionsAsync();
var mcpTools1 = await mcpClient1.GetAIFunctionsAsync();

// Get all available tools
Console.WriteLine("Tools available:");
IList<AITool> tools = [.. mcpTools, .. mcpTools1];
foreach (var tool in tools)
{
    Console.WriteLine($"  {tool}");
}
Console.WriteLine();

// 👇🏼 Use Ollama as the chat client
var ollamaChatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2:3b");
var client = new ChatClientBuilder(ollamaChatClient)
    // 👇🏼 Add logging to the chat client, wrapping the function invocation client 
    .UseLogging(factory)
    // 👇🏼 Add function invocation to the chat client, wrapping the Ollama client
    .UseFunctionInvocation()
    .ConfigureOptions(options => options.Tools = tools)
    .Build();

IList<ChatMessage> messages =
[
    new(ChatRole.System, """
                         You are a helpful assistant delivering time in one sentence
                         in a short format, like 'It is 10:08 in Paris, France. And you can manage the postgres database as well.'
                         """),
    new(ChatRole.User, message)
];

//var response = await client.GetResponseAsync(
//    messages,
//    new ChatOptions { Tools = tools });

var response = await client.GetResponseAsync(messages);

Console.WriteLine(response);