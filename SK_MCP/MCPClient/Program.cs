// Copyright (c) Microsoft. All rights reserved.

using MCPClient.Samples;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

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

app.MapPost("/AgentAvailableAsMCPToolSample", async () =>
{
    await AgentAvailableAsMCPToolSample.RunAsync();

    return Results.Accepted();
});

app.MapPost("/MCPToolsSample", async () =>
{
    await MCPToolsSample.RunAsync();

    return Results.Accepted();
});

app.MapPost("/", async () =>
{

    return Results.Accepted();
});

app.Run();

//internal sealed class Program
//{
//    /// <summary>
//    /// Main method to run all the samples.
//    /// </summary>
//    public static async Task Main(string[] args)
//    {
//        //await MCPToolsSample.RunAsync();

//        //await MCPPromptSample.RunAsync();

//        //await MCPResourcesSample.RunAsync();

//        //await MCPResourceTemplatesSample.RunAsync();

//        //await MCPSamplingSample.RunAsync(); // not working, cannot inject IMcpServer when sampling

//        //await ChatCompletionAgentWithMCPToolsSample.RunAsync();

//        //await AzureAIAgentWithMCPToolsSample.RunAsync(); // not working

//        await AgentAvailableAsMCPToolSample.RunAsync();
//    }
//}
