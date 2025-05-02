using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var openai = builder.AddAzureOpenAIClient("openai");

var chatBuilder = openai.AddChatClient("gpt-4o-mini")
    //.ConfigureOptions(o =>
    //{
    //    o.Temperature = 0.1f;
    //    o.MaxOutputTokens = 5;
    //})
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/zeroshot", async (IChatClient chatClient) =>
{
    using var factory =
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

    chatClient = chatClient.AsBuilder()
        .ConfigureOptions(o =>
        {
            o.Temperature = 0.1f;
            o.MaxOutputTokens = 5;
        })
        .Build();

    var message = """
        Classify movie reviews as POSITIVE, NEUTRAL or NEGATIVE.
        Review: "Her" is a disturbing study revealing the direction
        humanity is headed if AI is allowed to keep evolving,
        unchecked. I wish there were more movies like this masterpiece.
        Sentiment:
    """;

    IList<ChatMessage> messages =
    [
        new(ChatRole.User, message)
    ];

    var response = await chatClient.GetResponseAsync<Sentiment>(messages);

    return Results.Json(new
    {
        Sentiment = response.Text
    });
});

app.MapPost("/oneshot-fewshot", async (IChatClient chatClient) =>
{
    using var factory =
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

    chatClient = chatClient.AsBuilder()
        .ConfigureOptions(o =>
        {
            o.Temperature = 0.1f;
            o.MaxOutputTokens = 250;
        })
        .Build();

    var message = """
        Parse a customer's pizza order into valid JSON

        EXAMPLE 1:
        I want a small pizza with cheese, tomato sauce, and pepperoni.
        JSON Response:
        ```
        {
            "size": "small",
            "type": "normal",
            "ingredients": ["cheese", "tomato sauce", "pepperoni"]
        }
        ```

        EXAMPLE 2:
        Can I get a large pizza with tomato sauce, basil and mozzarella.
        JSON Response:
        ```
        {
            "size": "large",
            "type": "normal",
            "ingredients": ["tomato sauce", "basil", "mozzarella"]
        }
        ```

        Now, I would like a large pizza, with the first half cheese and mozzarella.
        And the other tomato sauce, ham and pineapple.
    """;

    IList<ChatMessage> messages =
    [
        new(ChatRole.User, message)
    ];

    var response = await chatClient.GetResponseAsync(messages);

    return Results.Json(new
    {
        Data = response.Text
    });
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

enum Sentiment
{
    POSITIVE,
    NEUTRAL,
    NEGATIVE
}
