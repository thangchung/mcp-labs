using McpAgent.XServer.Hubs;
using McpAgent.XServer.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();

// Configure MCP Agent options
builder.Services.Configure<McpAgentOptions>(options =>
{
    options.McpServerUrl = builder.Configuration.GetConnectionString("McpServer") ?? "http://localhost:3000/mcp";
    options.OpenAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    options.OpenAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    options.OpenAiEndpoint = builder.Configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1";
});

// Register MCP Agent Service
builder.Services.AddSingleton<McpAgentService>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

app.UseRouting();

app.MapHub<ChatHub>("/chathub");
app.MapFallbackToFile("index.html");

app.Run();
