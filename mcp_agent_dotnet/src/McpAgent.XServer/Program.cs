using McpAgent.XServer.Hubs;
using McpAgent.XServer.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();

// Add CORS to allow client connections
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClientPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:5173", "https://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure MCP Agent options
builder.Services.Configure<McpAgentOptions>(options =>
{
    options.McpServerUrl = builder.Configuration.GetConnectionString("McpServer") ?? "http://localhost:3000/mcp";
    options.OpenAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    options.OpenAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    options.OpenAiEndpoint = builder.Configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1";
});

// Register MCP Services with full feature support
builder.Services.AddSingleton<McpSamplingService>();
builder.Services.AddSingleton<McpNotificationService>();
builder.Services.AddSingleton<McpProgressService>();
builder.Services.AddSingleton<McpElicitationService>();

// Register notification and progress handlers
builder.Services.AddSingleton<ConsoleNotificationHandler>();
builder.Services.AddSingleton<SignalRNotificationHandler>();
builder.Services.AddSingleton<ConsoleProgressHandler>();
builder.Services.AddSingleton<SignalRProgressHandler>();

// Register enhanced MCP Agent Service
builder.Services.AddSingleton<McpAgentService>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

// Initialize MCP services with handlers
var notificationService = app.Services.GetRequiredService<McpNotificationService>();
var progressService = app.Services.GetRequiredService<McpProgressService>();

notificationService.RegisterHandler(app.Services.GetRequiredService<ConsoleNotificationHandler>());
notificationService.RegisterHandler(app.Services.GetRequiredService<SignalRNotificationHandler>());
progressService.RegisterHandler(app.Services.GetRequiredService<ConsoleProgressHandler>());
progressService.RegisterHandler(app.Services.GetRequiredService<SignalRProgressHandler>());

app.UseResponseCompression();

// Configure CORS
app.UseCors("BlazorClientPolicy");

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
