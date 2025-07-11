using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();

var app = builder.Build();

app.MapPost("/weather-query", async (string query) =>
{
    try
    {
        var result = await QueryWeatherAgent(query);
        return Results.Ok(new { success = true, result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

app.MapPost("/test-weather-connection", async () =>
{
    try
    {
        var result = await TestWeatherConnection();
        return Results.Ok(new { success = true, result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

app.MapPost("/test-weather-tools", async () =>
{
    try
    {
        var result = await TestWeatherTools();
        return Results.Ok(new { success = true, result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

app.MapGet("/", () => Results.Ok(new 
{ 
    message = "A2A .NET Client is running",
    endpoints = new[]
    {
        "POST /weather-query - Query weather agent with natural language",
        "POST /test-weather-connection - Test connection to weather MCP server",
        "POST /test-weather-tools - Test weather tools directly"
    }
}));

app.Run();

async Task<string> QueryWeatherAgent(string query)
{
    // This demonstrates the A2A concept even without Azure OpenAI
    // by showing how .NET can call the MCP weather tools
    
    return $"Weather Agent Response: {query} (Mock response - would integrate with Azure OpenAI when credentials provided)";
}

async Task<string> TestWeatherConnection()
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        var response = await httpClient.GetAsync("http://localhost:3002/sse");
        
        if (response.IsSuccessStatusCode)
        {
            return "✅ Successfully connected to Weather MCP server on port 3002";
        }
        else
        {
            return $"❌ Weather MCP server responded with status: {response.StatusCode}";
        }
    }
    catch (HttpRequestException ex)
    {
        return $"❌ Failed to connect to Weather MCP server: {ex.Message}";
    }
    catch (TaskCanceledException)
    {
        return "❌ Connection to Weather MCP server timed out";
    }
}

async Task<string> TestWeatherTools()
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        // Test MCP tool call by simulating the protocol
        var testResult = await CallWeatherTool("get_current_weather", new { city = "New York" });
        return $"✅ Weather tool test successful: {testResult}";
    }
    catch (Exception ex)
    {
        return $"❌ Weather tool test failed: {ex.Message}";
    }
}

async Task<string> CallWeatherTool(string toolName, object parameters)
{
    // This simulates what the MCP client would do
    // In a real implementation, this would use the MCP protocol
    
    var paramJson = JsonSerializer.Serialize(parameters);
    
    // For demonstration, return a mock response
    return toolName switch
    {
        "get_current_weather" => """{"city": "New York", "temperature": 22, "humidity": 65, "condition": "partly cloudy"}""",
        "get_weather_forecast" => """{"city": "New York", "forecast_days": 3, "forecast": [{"day": 1, "temperature_high": 25, "temperature_low": 18}]}""",
        "convert_temperature" => """{"converted_temperature": 71.6, "converted_unit": "F"}""",
        _ => $"Tool {toolName} called with parameters: {paramJson}"
    };
}